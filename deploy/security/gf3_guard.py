#!/usr/bin/env python3
import argparse
from collections import OrderedDict
from contextlib import contextmanager
import copy
from datetime import datetime
import gzip
import hashlib
from http.client import HTTPException
import io
import ipaddress
import json
import math
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import time
from urllib.parse import urlsplit
from urllib.request import HTTPRedirectHandler, Request, build_opener
import zlib


ROOT = Path(os.environ.get("GF3_GUARD_ROOT", Path(__file__).resolve().parents[2]))
RUNTIME = Path(os.environ.get("GF3_GUARD_RUNTIME", Path(__file__).resolve().parent / "runtime"))
BASE_COMPOSE = "deploy/docker-compose.yml"
HTTPS_COMPOSE = "deploy/docker-compose.https.yml"
SOURCES = {
    "spamhaus-v4": ("https://www.spamhaus.org/drop/drop_v4.json", "feed-spamhaus-v4.json", 4),
    "spamhaus-v6": ("https://www.spamhaus.org/drop/drop_v6.json", "feed-spamhaus-v6.json", 6),
    "blocklist-strongips": ("https://lists.blocklist.de/lists/strongips.txt", "feed-blocklist-strongips.txt", None),
}
DAY = 86400
FEED_TTL = 7 * DAY
PROBE_WINDOW = 600
AUTO_BAN = 12 * 3600
MAX_OFFENDERS = 20_000
MAX_DOWNLOAD = 4 * 1024 * 1024
MAX_DECODED = 8 * 1024 * 1024
MAX_ENTRIES = 50_000
MIN_ENTRIES = 10
MAX_LINE = 8192
NGINX_FILES = ("blocked-compiled.geo", "allow-compiled.geo")
DNS_NAME = re.compile(r"(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z](?:[a-z0-9-]{0,61}[a-z0-9])?", re.ASCII)
LOG_FIELDS = {"ts", "ip", "method", "path", "status", "probe", "vhost", "request_time"}


class GuardError(Exception):
    pass


def reject_constant(_):
    raise ValueError("non-finite JSON number")


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("duplicate JSON key")
        result[key] = value
    return result


def parse_json(data):
    return json.loads(data, parse_constant=reject_constant, object_pairs_hook=unique_object)


def strict_network(value):
    if not isinstance(value, str) or "%" in value:
        raise ValueError("invalid network")
    return ipaddress.ip_network(value, strict=True)


def public_ip(value):
    if not isinstance(value, str) or "%" in value:
        raise ValueError("invalid address")
    address = ipaddress.ip_address(value)
    if isinstance(address, ipaddress.IPv6Address) and address.ipv4_mapped:
        address = address.ipv4_mapped
    if not address.is_global or address.is_multicast or address.is_reserved:
        raise ValueError("nonpublic address")
    if address.version == 6 and (address.is_site_local or address in ipaddress.ip_network("3fff::/20")):
        raise ValueError("nonpublic address")
    return address


def feed_network(value, version):
    if version is None:
        # strongips is a list of individual addresses, never a CIDR list.
        return strict_network(str(public_ip(value)))
    if not isinstance(value, str) or "/" not in value:
        raise ValueError("missing CIDR prefix")
    network = strict_network(value)
    if network.version != version or network.prefixlen < (12 if version == 4 else 24):
        raise ValueError("invalid feed prefix")
    public_ip(str(network.network_address))
    public_ip(str(network.broadcast_address))
    excluded = ("100.64.0.0/10", "192.0.0.0/24", "192.0.2.0/24", "198.18.0.0/15",
                "198.51.100.0/24", "203.0.113.0/24") if version == 4 else (
                    "2001::/23", "2001:db8::/32", "2002::/16", "3fff::/20")
    if any(network.overlaps(ipaddress.ip_network(prefix)) for prefix in excluded):
        raise ValueError("special-purpose feed network")
    return network


def parse_feed(data, version):
    if len(data) > MAX_DECODED:
        raise ValueError("oversized feed")
    networks = set()
    metadata = None
    entries = 0
    for line in data.decode("utf-8-sig").splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        if version is None:
            value = line
        else:
            record = parse_json(line)
            if not isinstance(record, dict) or metadata is not None:
                raise ValueError("invalid Spamhaus record order")
            if record.get("type") == "metadata" or ("cidr" not in record and "copyright" in record):
                if "cidr" in record:
                    raise ValueError("invalid Spamhaus metadata")
                metadata = record
                continue
            value = record.get("cidr")
        entries += 1
        if entries > MAX_ENTRIES:
            raise ValueError("too many feed entries")
        networks.add(feed_network(value, version))
    if metadata is not None and "records" in metadata:
        if type(metadata["records"]) is not int or metadata["records"] != entries:
            raise ValueError("incomplete Spamhaus feed")
    if len(networks) < MIN_ENTRIES:
        raise ValueError("feed has too few unique entries")
    return networks


class SameOriginRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        old = urlsplit(req.full_url)
        new = urlsplit(newurl)
        if (new.scheme != "https" or new.hostname != old.hostname
                or (new.port or 443) != (old.port or 443) or new.username is not None):
            raise ValueError("feed redirect outside approved origin")
        return super().redirect_request(req, fp, code, msg, headers, newurl)


def download(url):
    request = Request(url, headers={"User-Agent": "gf3-guard/1.0"})
    with build_opener(SameOriginRedirect()).open(request, timeout=10) as response:
        data = response.read(MAX_DOWNLOAD + 1)
    if len(data) > MAX_DOWNLOAD:
        raise ValueError("oversized download")
    if data.startswith(b"\x1f\x8b"):
        with gzip.GzipFile(fileobj=io.BytesIO(data)) as compressed:
            data = compressed.read(MAX_DECODED + 1)
    if len(data) > MAX_DECODED:
        raise ValueError("oversized decoded feed")
    return data


def replace_file(path, data, mode=0o600, timestamp=None):
    if data is None:
        path.unlink(missing_ok=True)
        return
    descriptor, name = tempfile.mkstemp(dir=path.parent, prefix=f".{path.name}.", suffix=".tmp")
    temporary = Path(name)
    try:
        with os.fdopen(descriptor, "wb") as output:
            output.write(data)
            output.flush()
            if hasattr(os, "fchmod"):
                os.fchmod(output.fileno(), mode)
            if timestamp is not None:
                os.utime(temporary, (timestamp, timestamp))
            os.fsync(output.fileno())
        os.replace(temporary, path)
    finally:
        temporary.unlink(missing_ok=True)


def read_bytes(path):
    try:
        return path.read_bytes()
    except FileNotFoundError:
        return None


def initial_state():
    return {"cursor": None, "recent": {}, "bans": {}, "allows": [], "overrides": {}, "attempts": {},
            "last_success": None, "last_error": None}


def render_geo(networks):
    ordered = sorted(networks, key=lambda net: (net.version, int(net.network_address), net.prefixlen))
    return "".join(f"{net} 1;\n" for net in ordered).encode("ascii")


def parse_event(line):
    event = parse_json(line.decode("utf-8"))
    if not isinstance(event, dict) or event.keys() != LOG_FIELDS:
        raise ValueError("invalid log object")
    if any(type(event[key]) is not str for key in ("ts", "ip", "method", "path", "vhost")):
        raise ValueError("invalid log strings")
    if type(event["status"]) is not int or not 100 <= event["status"] <= 599:
        raise ValueError("invalid log status")
    if type(event["probe"]) is not int or event["probe"] not in (0, 1):
        raise ValueError("invalid log probe flag")
    duration = event["request_time"]
    if type(duration) not in (int, float) or duration < 0 or not math.isfinite(duration):
        raise ValueError("invalid log duration")
    stamp = datetime.fromisoformat(event["ts"])
    if stamp.utcoffset() is None:
        raise ValueError("missing log timestamp offset")
    if "%" in event["ip"]:
        raise ValueError("invalid log IP")
    ipaddress.ip_address(event["ip"])
    return stamp.timestamp(), event


def checkpoint(stream, offset):
    stream.seek(max(0, offset - 128))
    digest = hashlib.sha256(stream.read(min(128, offset))).hexdigest()
    return digest


def process_logs(path, state, now, canonical):
    recent = OrderedDict(sorted(state["recent"].items(), key=lambda entry: entry[1][-1]))
    counts = {"events": 0, "malformed": 0, "auto_bans": 0}

    def process(stream, offset):
        stream.seek(offset)
        while line := stream.readline(MAX_LINE + 1):
            oversized = len(line) > MAX_LINE
            if oversized:
                while line and not line.endswith(b"\n"):
                    line = stream.readline(MAX_LINE + 1)
            if not line.endswith(b"\n"):
                break
            offset = stream.tell()
            try:
                if oversized:
                    raise ValueError("oversized log line")
                stamp, event = parse_event(line)
            except (ValueError, OverflowError, RecursionError):
                counts["malformed"] += 1
                continue
            counts["events"] += 1
            if event["probe"] != 1 or event["vhost"].lower() not in ("_", canonical):
                continue
            if not now - AUTO_BAN - PROBE_WINDOW <= stamp <= now:
                continue
            try:
                ip = str(public_ip(event["ip"]))
            except ValueError:
                continue
            timestamps = sorted([*recent.pop(ip, []), stamp])
            timestamps = [item for item in timestamps if timestamps[-1] - item <= PROBE_WINDOW]
            recent[ip] = timestamps[-3:]
            if len(recent) > MAX_OFFENDERS:
                recent.popitem(last=False)
            if len(timestamps) < 4 or timestamps[-1] + AUTO_BAN <= now:
                continue
            expires = timestamps[-1] + AUTO_BAN
            ban = state["bans"].get(ip)
            if ban and ban["expires"] >= expires:
                continue
            if not ban and len(state["bans"]) >= MAX_OFFENDERS:
                continue
            if not ban:
                counts["auto_bans"] += 1
            state["bans"][ip] = {"expires": expires, "reason": ban["reason"] if ban else "auto"}
        return offset

    try:
        stream = path.open("rb")
    except FileNotFoundError:
        return counts
    with stream:
        stat = os.fstat(stream.fileno())
        cursor = state["cursor"]
        offset = 0
        if cursor:
            previous_id = (cursor["device"], cursor["inode"])
            if previous_id == (stat.st_dev, stat.st_ino):
                if stat.st_size >= cursor["offset"] and checkpoint(stream, cursor["offset"]) == cursor["tail"]:
                    offset = cursor["offset"]
            else:
                rotated = path.with_name(path.name + ".1")
                try:
                    with rotated.open("rb") as old:
                        old_stat = os.fstat(old.fileno())
                        if previous_id == (old_stat.st_dev, old_stat.st_ino):
                            process(old, cursor["offset"])
                except FileNotFoundError:
                    pass
        offset = process(stream, offset)
        state["cursor"] = {"device": stat.st_dev, "inode": stat.st_ino, "offset": offset,
                           "tail": checkpoint(stream, offset)}
    state["recent"] = {ip: stamps for ip, stamps in recent.items() if stamps[-1] >= now - PROBE_WINDOW}
    return counts


def canonical_host(env_file):
    # Read only the host setting and never print dotenv values.
    host = "localhost"
    if env_file.is_file():
        for line in env_file.read_text(encoding="utf-8-sig").splitlines():
            match = re.fullmatch(r"""\s*(?:export\s+)?GF3_SERVER_NAME\s*=\s*(['"]?)([^'"`\s#]*)\1(?:\s+#.*)?\s*""", line)
            if match:
                host = match[2].lower() or "localhost"
            elif re.match(r"\s*(?:export\s+)?GF3_SERVER_NAME\s*=", line):
                raise GuardError("GF3_SERVER_NAME must contain one literal DNS hostname")
    if host != "localhost" and (len(host) > 253 or not DNS_NAME.fullmatch(host)):
        raise GuardError("GF3_SERVER_NAME must contain one literal DNS hostname")
    return host


@contextmanager
def guard_lock(path):
    import fcntl

    with os.fdopen(os.open(path, os.O_CREAT | os.O_RDWR, 0o600), "r+b") as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            yield False
        else:
            yield True


class Guard:
    def __init__(self, runtime=RUNTIME, root=ROOT, compose_files=(BASE_COMPOSE, HTTPS_COMPOSE),
                 clock=time.time, fetch=download, run_command=subprocess.run, sleep=time.sleep):
        self.runtime = runtime
        self.root = root
        self.private = runtime / "state"
        self.geo = runtime / "nginx"
        self.log = runtime / "logs" / "access.jsonl"
        self.state_file = self.private / "state.json"
        self.clock = clock
        self.fetch = fetch
        self.run_command = run_command
        self.sleep = sleep
        active = os.environ.get("GF3_GUARD_ACTIVE_COMPOSE")
        if active:
            self.compose = ["docker", "compose", "-p", "gf3", "--env-file", "/dev/null", "-f", active]
        else:
            self.compose = ["docker", "compose", "--env-file", "deploy/.env"]
            for filename in compose_files:
                self.compose.extend(("-f", filename))
        self.compose.extend(("exec", "-T", "nginx"))

    def state(self):
        data = read_bytes(self.state_file)
        return initial_state() if data is None else parse_json(data.decode("utf-8"))

    def save_state(self, state):
        replace_file(self.state_file, (json.dumps(state, sort_keys=True, allow_nan=False) + "\n").encode("utf-8"))

    def feeds(self, state, now, refresh=False):
        active = set()
        summaries = {}
        for name, (url, filename, version) in SOURCES.items():
            path = self.private / filename
            fetched = None
            networks = set()
            cache_error = None
            try:
                fetched = path.stat().st_mtime
                with path.open("rb") as cache:
                    networks = parse_feed(cache.read(MAX_DECODED + 1), version)
            except FileNotFoundError:
                fetched = None
            except (OSError, ValueError, RecursionError):
                cache_error = "invalid cache"
            attempt = state["attempts"].get(name, {})
            last_attempt = max(attempt.get("at", -math.inf), fetched if networks else -math.inf)
            if refresh and now - last_attempt >= DAY:
                attempt = state["attempts"][name] = {"at": now}
                try:
                    candidate = self.fetch(url)
                    verified = parse_feed(candidate, version)
                    replace_file(path, candidate, timestamp=now)
                    fetched, networks, cache_error = now, verified, None
                except (OSError, ValueError, EOFError, RecursionError, HTTPException, zlib.error) as error:
                    attempt["error"] = f"feed refresh failed ({type(error).__name__})"
            age = max(0, int(now - fetched)) if fetched is not None else None
            fresh = age is not None and age < FEED_TTL and bool(networks)
            if fresh:
                active.update(networks)
            summaries[name] = {"age_seconds": age, "active": len(networks) if fresh else 0,
                               "stale": age is not None and age >= FEED_TTL,
                               "last_error": attempt.get("error") or cache_error}
        return active, summaries

    def command(self, *args):
        try:
            result = self.run_command([*self.compose, *args], cwd=self.root, shell=False,
                                      timeout=20, check=False, capture_output=True, text=True)
        except FileNotFoundError as error:
            raise GuardError("Docker CLI unavailable") from error
        except (OSError, subprocess.TimeoutExpired) as error:
            raise GuardError(f"Nginx command could not run ({type(error).__name__})") from error
        if result.returncode:
            raise GuardError(f"Nginx command failed ({' '.join(args[:2])}, exit {result.returncode}); check Compose and nginx -t")
        return result.stdout

    def workers(self):
        output = self.command("ps", "-o", "pid,args")
        return set(re.findall(r"^\s*(\d+)\s+nginx: worker process\s*$", output, re.MULTILINE))

    def reload(self, previous):
        self.command("nginx", "-s", "reload")
        # A successful signal alone does not prove the master applied the rules.
        for _ in range(10):
            if self.workers() - previous:
                return
            self.sleep(0.5)
        raise GuardError("Nginx did not start new workers after reload")

    def apply(self, candidates, state):
        previous = {name: read_bytes(self.geo / name) for name in NGINX_FILES}
        changes = [name for name in NGINX_FILES if candidates[name] != (previous[name] or b"")]
        if not changes:
            self.save_state(state)
            return False
        reload_attempted = False
        written = []
        try:
            for name in NGINX_FILES:
                replace_file(self.private / (name + ".previous"), previous[name] or b"")
            for name in changes:
                replace_file(self.geo / name, candidates[name], 0o644)
                written.append(name)
            self.command("nginx", "-t")
            workers = self.workers()
            if not workers:
                raise GuardError("Nginx has no running worker to reload")
            reload_attempted = True
            self.reload(workers)
            state["last_success"] = int(self.clock())
            self.save_state(state)
        except (OSError, GuardError) as error:
            try:
                for name in written:
                    replace_file(self.geo / name, previous[name], 0o644)
                if reload_attempted:
                    self.command("nginx", "-t")
                    self.reload(self.workers())
            except (OSError, GuardError) as rollback_error:
                raise GuardError(f"{error}; rollback verification failed: {rollback_error}") from error
            raise
        return True

    def execute(self, args):
        if args.command == "status" or getattr(args, "dry_run", False):
            return self.update(args, read_only=True)
        for directory in (self.runtime, self.private, self.geo, self.log.parent):
            directory.mkdir(mode=0o700, parents=True, exist_ok=True)
        with guard_lock(self.private / "guard.lock") as acquired:
            if not acquired:
                return {"status": "another guard run holds the lock"}
            return self.update(args, read_only=False)

    def update(self, args, read_only):
        prior = self.state()
        state = copy.deepcopy(prior)
        now = self.clock()
        state["bans"] = {ip: ban for ip, ban in state["bans"].items() if ban["expires"] > now}
        state["overrides"] = {ip: until for ip, until in state["overrides"].items() if until > now}
        state["recent"] = {ip: stamps for ip, stamps in state["recent"].items() if stamps[-1] >= now - PROBE_WINDOW}
        counts = {}
        if args.command == "run":
            counts = process_logs(self.log, state, now, canonical_host(self.root / "deploy/.env"))
        feeds, summaries = self.feeds(state, now, refresh=args.command == "run" and not read_only)
        if args.command == "ban":
            state["bans"][args.ip] = {"expires": now + args.hours * 3600, "reason": "manual"}
        elif args.command == "unban":
            state["bans"].pop(args.ip, None)
            state["recent"].pop(args.ip, None)
            address = ipaddress.ip_address(args.ip)
            if any(address in network for network in feeds) and not any(
                    address in strict_network(network) for network in state["allows"]):
                state["overrides"][args.ip] = now + DAY
        elif args.command == "allow":
            state["allows"] = sorted(set(state["allows"]) | {args.network})
        elif args.command == "unallow":
            state["allows"] = sorted(set(state["allows"]) - {args.network})
            network = strict_network(args.network)
            if network.prefixlen == network.max_prefixlen:
                state["overrides"].pop(str(network.network_address), None)

        blocked = feeds | {strict_network(ip) for ip in state["bans"]}
        allowed = {strict_network(value) for value in (*state["allows"], *state["overrides"])}
        candidates = dict(zip(NGINX_FILES, (render_geo(blocked), render_geo(allowed))))
        result = {"sources": summaries, "blocked": len(blocked), "allowed": len(allowed),
                  "auto": sum(ban["reason"] == "auto" for ban in state["bans"].values()),
                  "manual": sum(ban["reason"] == "manual" for ban in state["bans"].values()), **counts}
        if read_only:
            if args.command == "run":
                result["would_change"] = any(value != (read_bytes(self.geo / name) or b"")
                                             for name, value in candidates.items())
        else:
            try:
                state["last_error"] = None
                result["reloaded"] = self.apply(candidates, state)
            except (OSError, GuardError) as error:
                prior["attempts"] = state["attempts"]
                prior["last_error"] = str(error) if isinstance(error, GuardError) else type(error).__name__
                self.save_state(prior)
                raise
        result["last_success"] = state["last_success"]
        result["last_error"] = state["last_error"]
        return result


def arguments(argv=None):
    parser = argparse.ArgumentParser(description="Compile and safely reload GF3 Nginx IP rules")
    commands = parser.add_subparsers(dest="command", required=True)
    for name in ("run", "status", "ban", "unban", "allow", "unallow"):
        command = commands.add_parser(name)
        command.add_argument("--compose-files", nargs="+", default=[BASE_COMPOSE, HTTPS_COMPOSE],
                             choices=(BASE_COMPOSE, HTTPS_COMPOSE))
        if name == "run":
            command.add_argument("--dry-run", action="store_true")
        if name in ("ban", "unban"):
            command.add_argument("ip")
        if name == "ban":
            command.add_argument("--hours", type=int, default=168)
        if name in ("allow", "unallow"):
            command.add_argument("network")
    args = parser.parse_args(argv)
    if args.compose_files not in ([BASE_COMPOSE], [BASE_COMPOSE, HTTPS_COMPOSE]):
        parser.error("use base Compose alone or base Compose followed by HTTPS overlay")
    try:
        if hasattr(args, "ip"):
            args.ip = str(public_ip(args.ip))
        if hasattr(args, "hours") and not 1 <= args.hours <= 24 * 365:
            raise ValueError("hours outside supported range")
        if hasattr(args, "network"):
            network = strict_network(args.network)
            if network.prefixlen < (24 if network.version == 4 else 64):
                raise ValueError("allow network is too broad")
            args.network = str(network)
    except ValueError:
        parser.error("use a valid public IP for bans, at most /24 IPv4 or /64 IPv6 for allows, and 1–8760 hours")
    return args


def main():
    args = arguments()
    try:
        print(json.dumps(Guard(compose_files=args.compose_files).execute(args), sort_keys=True))
    except GuardError as error:
        print(f"GF3 guard: {error}", file=sys.stderr)
        return 1
    except (OSError, ValueError, KeyError, TypeError, RecursionError) as error:
        print(f"GF3 guard: {type(error).__name__}; check runtime permissions and private state",
              file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
