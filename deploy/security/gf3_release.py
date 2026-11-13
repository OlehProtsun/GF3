#!/usr/bin/env python3
"""Manage GF3's production Compose release without replacing its network or database."""

import argparse
from contextlib import closing, contextmanager
import hashlib
import ipaddress
import io
import json
import os
from pathlib import Path
import re
import shutil
import signal
import socket
import sqlite3
import stat
import subprocess
import sys
import tarfile
import tempfile
import time


ROOT = Path("/opt/gf")
STATE = Path("/var/lib/gf3-deploy")
RUNTIME = ROOT / "deploy/security/runtime"
DOMAIN = "app-gf.com"
PROJECT = "gf3"
NETWORK = PROJECT + "_gf3-internal"
GUARD_TIMER_UNIT = Path("/etc/systemd/system/gf3-guard.timer")
CERT = "/etc/nginx/certs/fullchain.pem"
KEY = "/etc/nginx/certs/privkey.pem"
WEB = "/var/www/certbot"
GEO = "/etc/nginx/gf3-security"
LOG = "/var/log/nginx/gf3-security"
TEMPLATE = "/etc/nginx/templates/default.conf.template"
CONFIG = "/etc/nginx"
SHA = re.compile(r"[0-9a-f]{40}")
IMAGE_DIGEST = re.compile(r"nginx@sha256:[0-9a-f]{64}")
ENV = {key: os.environ[key] for key in ("HOME", "PATH", "LANG", "LC_ALL", "SSH_AUTH_SOCK")
       if key in os.environ}
ENV.update(GIT_TERMINAL_PROMPT="0", DOCKER_HOST="unix:///var/run/docker.sock")


class ReleaseError(Exception):
    pass


def require(condition, message):
    if not condition:
        raise ReleaseError(message)


def command(*args, cwd=None, timeout=120, capture=True, check=True, data=None, extra_env=None):
    result = subprocess.run(args, cwd=cwd, env={**ENV, **(extra_env or {})}, timeout=timeout, text=True,
                            input=data, capture_output=capture)
    if check and result.returncode:
        if args[0] == "git" and "merge" in args:
            diagnostic = (result.stderr or "").lower()
            reasons = (
                ("index.lock", "Git index lock or another writer"),
                ("cannot lock ref", "Git ref lock or another writer"),
                ("untracked working tree", "untracked files would be overwritten"),
                ("local changes", "tracked changes would be overwritten"),
                ("permission denied", "check checkout and Git metadata permissions"),
                ("not possible to fast-forward", "checkout no longer fast-forwards"),
                ("dubious ownership", "check Git checkout ownership"),
            )
            reason = next((message for token, message in reasons if token in diagnostic),
                          "check branch, checkout status and Git write access")
            raise ReleaseError(f"git merge --ff-only failed (exit {result.returncode}): {reason}")
        if args[0] == "systemctl":
            unit = next((arg for arg in args if arg in ("gf3-guard.timer", "gf3-guard.service")), "")
            raise ReleaseError(f"systemctl {args[1]} {unit} failed (exit {result.returncode}); "
                               "check systemctl status and the unit journal")
        if args[0] == "/usr/bin/python3":
            hint = ("Guard lock is busy; let the other Guard command finish"
                    if (result.stderr or "").strip() == "GF3 guard: Deployment guard could not acquire its lock"
                    else "check Guard state, filesystem permissions and the active Nginx configuration")
            raise ReleaseError(f"Guard {args[2]} failed (exit {result.returncode}); {hint}")
        raise ReleaseError(f"{Path(args[0]).name} {args[1]} failed (exit {result.returncode})")
    return result.stdout if capture else ""


def docker_json(*args):
    return json.loads(command("docker", *args))


def git(*args, root=ROOT):
    return command("git", "-c", f"safe.directory={root}", *args, cwd=root, timeout=180)


def safe_path(path, private=False):
    path = Path(path)
    require(path.is_absolute(), "Expected absolute path")
    for entry in [*reversed(path.parents), path]:
        info = entry.lstat()
        require(not stat.S_ISLNK(info.st_mode) and info.st_uid == 0 and not info.st_mode & 0o022,
                f"Root ownership and protected path required: {entry}")
    if private:
        require(not path.stat().st_mode & 0o077, f"Private permissions required: {path}")


def verified_checkout(source, revision):
    require(git("rev-parse", "HEAD", root=source).strip() == revision, "Candidate checkout is at a different revision")
    require(not git("status", "--porcelain", root=source), "Candidate checkout is dirty")


def write(path, contents):
    descriptor, filename = tempfile.mkstemp(dir=path.parent, prefix=".pending-")
    pending = Path(filename)
    try:
        with os.fdopen(descriptor, "wb") as handle:
            handle.write(contents)
            handle.flush()
            os.fsync(handle.fileno())
        os.replace(pending, path)
    finally:
        pending.unlink(missing_ok=True)


def store(path, value):
    write(path, (json.dumps(value, indent=2) + "\n").encode())


def save_compose(bundle, config):
    # Rendered credentials can contain literal dollars. Compose interpolates the saved manifest again.
    def literal(value):
        if isinstance(value, str):
            return value.replace("$", "$$")
        if isinstance(value, dict):
            return {key: literal(item) for key, item in value.items()}
        if isinstance(value, list):
            return [literal(item) for item in value]
        return value

    store(bundle / "compose.json", literal(config))


@contextmanager
def lock(path):
    import fcntl

    with os.fdopen(os.open(path, os.O_RDWR | os.O_CREAT | os.O_NOFOLLOW, 0o600), "r+b") as handle:
        deadline = time.monotonic() + 120
        while True:
            try:
                fcntl.flock(handle, fcntl.LOCK_EX | fcntl.LOCK_NB)
                break
            except BlockingIOError:
                require(time.monotonic() < deadline, f"Lock timeout: {path}")
                time.sleep(0.25)
        yield


def compose(bundle, *args, **kwargs):
    return command("docker", *compose_args(), "-f", str(bundle / "compose.json"), *args, **kwargs)


def compose_args():
    return ["compose", "-p", PROJECT, "--env-file", os.devnull]


def render(source):
    deploy = source / "deploy"
    return docker_json("compose", "-p", PROJECT, "--env-file", str(deploy / ".env"),
                       "-f", str(deploy / "docker-compose.yml"),
                       "-f", str(deploy / "docker-compose.https.yml"), "config", "--format", "json")


def container(service):
    item, = docker_json("inspect", f"{PROJECT}-{service}-1")
    require(item["Config"]["Labels"]["com.docker.compose.project"] == PROJECT, "Wrong Docker project")
    return item


def topology():
    bridge, = docker_json("network", "inspect", NETWORK)
    app, nginx = container("gf3-app"), container("nginx")
    require(set(bridge["Containers"]) == {app["Id"], nginx["Id"]}, "GF3 bridge must contain only app and Nginx")
    cidrs = [ipaddress.ip_network(row["Subnet"]) for row in bridge["IPAM"]["Config"] if "Subnet" in row]
    ipv4, = [entry for entry in cidrs if entry.version == 4]
    for item in (app, nginx):
        endpoint = item["NetworkSettings"]["Networks"][NETWORK]
        require(ipaddress.ip_address(endpoint["IPAddress"]) in ipv4, "Container peer outside GF3 bridge")
    require(not app["HostConfig"]["PortBindings"], "Backend has published ports")
    mounts = [entry for entry in app["Mounts"] if entry["Destination"] == "/data"]
    require(len(mounts) == 1 and mounts[0]["Type"] == "volume", "Backend /data must use the existing named volume")
    return bridge["Id"], str(ipv4), mounts[0]["Name"]


def verify_ipv6():
    addresses = socket.getaddrinfo(DOMAIN, 443, type=socket.SOCK_STREAM)
    if any(ipaddress.ip_address(row[4][0]).version == 6 for row in addresses):
        bridge, = docker_json("network", "inspect", NETWORK)
        require(bridge["EnableIPv6"], "Advertised AAAA requires a native IPv6 Docker bridge to preserve client IP")


def mounts(service):
    return {row["target"]: row for row in service.get("volumes", [])}


def validate(config, cidr, volume, domain=DOMAIN):
    app, nginx = (config["services"][name] for name in ("gf3-app", "nginx"))
    require(set(config["services"]) == {"gf3-app", "nginx"}, "Unexpected candidate services")
    require(all(set(service["networks"]) == {"gf3-internal"} for service in (app, nginx)),
            "Candidate uses a different network")
    require(set(mounts(app)) == {"/data"}, "Unexpected backend mounts")
    env = app["environment"]
    require(env.get("GF3_TRUSTED_PROXY_CIDR") == cidr, "Effective trusted CIDR differs from the Docker subnet")
    network = ipaddress.ip_network(cidr)
    require(network.version == 4 and 16 <= network.prefixlen <= 30 and any(
        network.subnet_of(ipaddress.ip_network(private))
        for private in ("10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16")), "Invalid private proxy subnet")
    require(env.get("AllowedHosts") == f"{domain};localhost;127.0.0.1" and
            nginx["environment"].get("GF3_SERVER_NAME") == domain, "Incorrect effective hostname")
    require(env.get("ASPNETCORE_ENVIRONMENT") == env.get("DOTNET_ENVIRONMENT") == "Production"
            and not env.get("ASPNETCORE_FORWARDEDHEADERS_ENABLED") and not env.get("FORWARDEDHEADERS_ENABLED"),
            "Backend production environment or forwarded-header settings are unsafe")
    require(env.get("DatabaseStartup__RequireExistingDatabase") == "true"
            and env.get("DatabaseStartup__BackupBeforeMigrate") == "true", "SQLite startup protection is disabled")
    require(not app.get("ports"), "Backend must not publish a port")
    require({(row["target"], str(row["published"])) for row in nginx["ports"]} == {(80, "80"), (443, "443")},
            "Unexpected Nginx port mappings")
    require(config["volumes"][mounts(app)["/data"]["source"]]["name"] == volume, "Candidate changes the data volume")
    bound = mounts(nginx)
    for target in (CERT, KEY, WEB, GEO, LOG, TEMPLATE):
        require(bound[target]["type"] == "bind", f"Expected bind mount: {target}")
    for target in (CERT, KEY, WEB, GEO, TEMPLATE):
        require(bound[target].get("read_only"), f"Expected read-only mount: {target}")
    for target in (CERT, KEY):
        require(Path(bound[target]["source"]).is_file(), f"Missing TLS source: {target}")
    cert, key = (bound[target]["source"] for target in (CERT, KEY))
    require(command("openssl", "x509", "-in", cert, "-noout", "-pubkey") ==
            command("openssl", "pkey", "-in", key, "-pubout"), "TLS certificate and key differ")
    command("openssl", "x509", "-in", cert, "-checkend", "86400", "-noout")
    require("does match" in command("openssl", "x509", "-in", cert, "-noout", "-checkhost", domain),
            "Certificate hostname does not match")
    require(Path(bound[WEB]["source"], ".well-known/acme-challenge").is_dir(), "ACME challenge directory missing")
    if domain == "app-gf.com":
        for path in (cert, key):
            require(Path(path).resolve().is_relative_to(Path("/etc/letsencrypt/archive")),
                    "TLS files must resolve to the active Certbot archive")
        require(Path(cert) == Path("/etc/letsencrypt/live") / domain / "fullchain.pem"
                and Path(key) == Path("/etc/letsencrypt/live") / domain / "privkey.pem",
                "Effective mounts must follow the app-gf.com Certbot lineage")
    return bound


def freeze(config, volume):
    config["networks"] = {"gf3-internal": {"name": NETWORK, "external": True}}
    config["volumes"] = {"gf3-data": {"name": volume, "external": True}}
    for service in config["services"].values():
        service.pop("build", None)
        service.pop("env_file", None)


def local_image(image, tag):
    command("docker", "image", "tag", image, tag)
    return tag


def current_config():
    """Capture existing Compose plus the actual images, environment and mount sources."""
    active = STATE / "active/compose.json"
    config = docker_json(*compose_args(), "-f", str(active), "config", "--format", "json") if active.exists() else render(ROOT)
    require(set(config["services"]) == {"gf3-app", "nginx"}, "Review extra Compose services before recovery")
    for name, service in config["services"].items():
        live = container(name)
        if not active.exists():
            files = live["Config"]["Labels"].get("com.docker.compose.project.config_files", "").split(",")
            expected_files = [str(ROOT / "deploy" / file)
                              for file in ("docker-compose.yml", "docker-compose.https.yml")]
            require([Path(file).as_posix() for file in files] == [Path(file).as_posix() for file in expected_files],
                    "Review additional live Compose files before automating rollback")
        for row in live["Mounts"]:
            if row["Type"] not in ("bind", "volume"):
                raise ReleaseError("Review unsupported live mount before automating rollback")
            target = row["Destination"]
            known = mounts(service).get(target)
            if row["Type"] == "volume":
                require(known and target == "/data", f"Review live volume: {target}")
            else:
                replacement = {"type": "bind", "source": row["Source"], "target": target,
                               "read_only": not row["RW"], "bind": {"create_host_path": False}}
                if known:
                    known.update(replacement)
                else:
                    service.setdefault("volumes", []).append(replacement)
        live_targets = {row["Destination"] for row in live["Mounts"]}
        service["volumes"] = [row for row in service.get("volumes", []) if row["target"] in live_targets]
        expected = {(row["target"], str(row["published"]), row.get("host_ip") or "0.0.0.0", row.get("protocol", "tcp"))
                    for row in service.get("ports", [])}
        published = {(int(target.split("/")[0]), item["HostPort"], item["HostIp"] or "0.0.0.0", target.split("/")[1])
                     for target, items in (live["HostConfig"]["PortBindings"] or {}).items() for item in items}
        require(expected == published, f"Live port bindings differ from the recovery model for {name}")
        require(set(live["NetworkSettings"]["Networks"]) == {NETWORK}, f"Extra live networks for {name}")
        service["environment"] = dict(value.split("=", 1) for value in live["Config"]["Env"])
        if name == "gf3-app":
            require(not live["HostConfig"]["Privileged"], "Review privileged backend before automating recovery")
            service["entrypoint"] = live["Config"]["Entrypoint"]
            service["command"] = live["Config"]["Cmd"]
            if live["Config"]["User"]:
                service["user"] = live["Config"]["User"]
        live_logging = live["HostConfig"]["LogConfig"]
        service["logging"] = {"driver": live_logging["Type"]}
        if live_logging["Config"]:
            service["logging"]["options"] = live_logging["Config"]
    return config


def saved_geo(bundle):
    directory = bundle / "geo"
    directory.mkdir()
    for source in (RUNTIME / "nginx").glob("*.geo"):
        safe_path(source)
        shutil.copyfile(source, directory / source.name)
    state = RUNTIME / "state/state.json"
    if state.is_file():
        safe_path(state, private=True)
        shutil.copyfile(state, bundle / "guard-state.json")


def restore_geo(bundle):
    directory = bundle / "geo"
    for target in (RUNTIME / "nginx").glob("*.geo"):
        if not (directory / target.name).exists():
            target.unlink()
    for source in directory.glob("*.geo"):
        target = RUNTIME / "nginx" / source.name
        write(target, source.read_bytes())
        target.chmod(0o644)
    state = RUNTIME / "state/state.json"
    saved_state = bundle / "guard-state.json"
    if saved_state.is_file():
        write(state, saved_state.read_bytes())
    else:
        state.unlink(missing_ok=True)


def snapshot(bundle, volume):
    config = current_config()
    freeze(config, volume)
    for name in config["services"]:
        config["services"][name]["image"] = local_image(container(name)["Image"], f"gf3-{name}:{bundle.name}")
    nginx = config["services"]["nginx"]
    live_mounts = mounts(nginx)
    for target, filename in ((CERT, "fullchain.pem"), (KEY, "privkey.pem")):
        if target in live_mounts:
            expected = Path("/etc/letsencrypt/live") / DOMAIN / filename
            if DOMAIN == "app-gf.com":
                require(Path(live_mounts[target]["source"]).resolve() == expected.resolve(),
                        f"Saved TLS mount does not use the active lineage: {target}")
                # Docker may report the resolved inode of a file bind. Rebind through the renewing lineage.
                live_mounts[target]["source"] = str(expected)
    source = bundle / "nginx-config"
    source.mkdir()
    require(all(row["target"] != CONFIG for row in nginx["volumes"]),
            "Review whole-directory Nginx config mounts before automating recovery")
    archive = subprocess.run(["docker", "exec", f"{PROJECT}-nginx-1", "tar", "-C", CONFIG,
                              "--exclude=certs", "--exclude=gf3-security", "--exclude=templates", "--exclude=modules",
                              "-cf", "-", "."], env=ENV, capture_output=True, timeout=30)
    require(archive.returncode == 0, "Could not capture the running Nginx configuration")
    with tarfile.open(fileobj=io.BytesIO(archive.stdout)) as config_archive:
        for member in config_archive:
            destination = (source / member.name).resolve()
            require(destination.is_relative_to(source), "Unsafe path in Nginx configuration snapshot")
            if member.isdir():
                destination.mkdir(parents=True, exist_ok=True)
            else:
                require(member.isfile(), "Unsupported link or special file in the Nginx configuration")
                destination.parent.mkdir(parents=True, exist_ok=True)
                with config_archive.extractfile(member) as contents:
                    write(destination, contents.read())
    # Mount the rendered config and bypass entrypoint template rendering during recovery.
    saved_targets = {f"{CONFIG}/{path.name}" for path in source.iterdir()}
    nginx["volumes"] = [row for row in nginx["volumes"] if row["target"] != CONFIG
                        and not row["target"].startswith(("/etc/nginx/conf.d", "/etc/nginx/templates"))
                        and row["target"] not in saved_targets]
    for path in sorted(source.iterdir()):
        if f"{CONFIG}/{path.name}" not in saved_targets:
            continue
        nginx["volumes"].append({"type": "bind", "source": str(path), "target": f"{CONFIG}/{path.name}",
                                 "read_only": True, "bind": {"create_host_path": False}})
    nginx["entrypoint"] = ["nginx", "-g", "daemon off;"]
    nginx["command"] = []
    shutil.copyfile(ROOT / "deploy/.env", bundle / "deploy.env")
    saved_geo(bundle)
    save_compose(bundle, config)
    if (STATE / "active/guard-enabled").is_file():
        (bundle / "guard-enabled").touch()


def nginx_test(bundle):
    output = compose(bundle, "run", "--rm", "--no-deps", "--pull", "never", "-T",
                     "--entrypoint", "/bin/sh", "nginx", "-ec",
                     'if [ -r /etc/nginx/templates/default.conf.template ] && [ "$1" = candidate ]; then '
                     '/docker-entrypoint.sh nginx -t; else nginx -t; fi',
                     "sh", "candidate" if bundle.name.startswith("release-") else "recovery")
    print("Nginx configuration verified")
    return output


def backup(bundle, volume, db_path=None):
    db = db_path or Path(docker_json("volume", "inspect", volume)[0]["Mountpoint"]) / "SQLite.db"
    require(db.is_file(), "SQLite database missing in the existing volume")
    destination = bundle / "SQLite.db"
    with closing(sqlite3.connect(db.as_uri() + "?mode=ro", uri=True, timeout=10)) as source:
        with closing(sqlite3.connect(destination)) as copy:
            deadline = time.monotonic() + 120

            def progress(*_):
                require(time.monotonic() < deadline, "SQLite backup timed out")

            source.backup(copy, pages=256, progress=progress)
            require(copy.execute("PRAGMA quick_check").fetchall() == [("ok",)], "SQLite backup failed integrity check")
    print("SQLite online backup verified")


def backend(bundle):
    compose(bundle, "up", "-d", "--no-deps", "--no-build", "--pull", "never",
            "--wait", "--wait-timeout", "100", "gf3-app", timeout=120)
    result = compose(bundle, "exec", "-T", "gf3-app", "curl", "-fsS", "--max-time", "5",
                     "http://127.0.0.1:8080/api/health")
    require(json.loads(result).get("canConnect") is True, "Backend health failed")


def start_nginx(bundle):
    compose(bundle, "up", "-d", "--no-deps", "--no-build", "--pull", "never", "--force-recreate", "nginx")
    compose(bundle, "exec", "-T", "nginx", "nginx", "-t")


def curl(scheme, path, *args):
    port = 443 if scheme == "https" else 80
    return command("curl", "--noproxy", "*", "-sS", "--max-time", "8", "--retry", "2", "--retry-connrefused",
                   "--resolve", f"{DOMAIN}:{port}:127.0.0.1", *args, f"{scheme}://{DOMAIN}{path}", timeout=35)


def verify_web(bundle, acme=True):
    result = curl("https", "/api/health", "--fail")
    require(json.loads(result).get("canConnect") is True, "HTTPS health failed")
    config = docker_json(*compose_args(), "-f", str(bundle / "compose.json"), "config", "--format", "json")
    bound = mounts(config["services"]["nginx"])
    cert = bound.get(CERT)
    if cert:
        expected = command("openssl", "x509", "-in", cert["source"], "-noout", "-fingerprint", "-sha256")
        chain = command("openssl", "s_client", "-connect", "127.0.0.1:443", "-servername", DOMAIN, data="", timeout=15)
        served = command("openssl", "x509", "-noout", "-fingerprint", "-sha256", data=chain)
        require(served == expected, "Nginx serves a different TLS certificate")
    if acme:
        require(WEB in bound, "Active release has no ACME webroot mount")
        challenge = Path(bound[WEB]["source"], ".well-known/acme-challenge")
        with tempfile.NamedTemporaryFile(dir=challenge, prefix="gf3-check-", delete=False) as fixture:
            path = Path(fixture.name)
            fixture.write(b"gf3-acme-proof")
        try:
            path.chmod(0o644)
            result = curl("http", "/.well-known/acme-challenge/" + path.name, "--fail", "-w", "\n%{http_code}")
            require(result == "gf3-acme-proof\n200", "HTTP-01 response was not the exact challenge")
        finally:
            path.unlink(missing_ok=True)
    print("HTTPS and ACME verified" if acme else "HTTPS verified")


def activate(bundle):
    link = STATE / ".active-next"
    link.unlink(missing_ok=True)
    link.symlink_to(bundle, target_is_directory=True)
    os.replace(link, STATE / "active")


def active_bundle():
    bundle = (STATE / "active").resolve(strict=True)
    safe_path(bundle, private=True)
    require(bundle.parent == STATE, "Unexpected active release path")
    return bundle


def migration_fingerprint(root, revision):
    return hashlib.sha256(git("ls-tree", "-r", revision, "--", "DataAccessLayer/Migrations",
                              "GF3.WebApi/Infrastructure/DatabaseMigrationStartup.cs", root=root).encode()).hexdigest()


def stage(sha, before, cidr, volume, nginx_image):
    bundle = Path(tempfile.mkdtemp(prefix=f"release-{sha[:12]}-", dir=STATE))
    work = bundle / "source"
    git("worktree", "add", "--detach", str(work), sha)
    try:
        require(migration_fingerprint(ROOT, before) == migration_fingerprint(work, sha),
                "Release changes SQLite migrations or startup migration code. Rehearse and approve that separately.")
        shutil.copyfile(ROOT / "deploy/.env", work / "deploy/.env")
        config = render(work)
        bound = validate(config, cidr, volume)
        template = bundle / "gf3.conf.template"
        shutil.copyfile(bound[TEMPLATE]["source"], template)
        template.chmod(0o644)
        bound[TEMPLATE]["source"] = str(template)
        bound[GEO]["source"] = str(RUNTIME / "nginx")
        bound[LOG]["source"] = str(RUNTIME / "logs")
        try:
            command("docker", "image", "inspect", nginx_image)
        except ReleaseError:
            command("docker", "pull", nginx_image, timeout=600)
        config["services"]["nginx"]["image"] = local_image(nginx_image, f"gf3-nginx:{bundle.name}")
        tag = f"gf3-app:{bundle.name}"
        command("docker", "build", "-t", tag, str(work), timeout=1800, capture=False)
        config["services"]["gf3-app"]["image"] = tag
        freeze(config, volume)
        save_compose(bundle, config)
        shutil.copyfile(ROOT / "deploy/.env", bundle / "deploy.env")
        (bundle / "guard-enabled").touch()
        nginx_test(bundle)
        return bundle
    finally:
        git("worktree", "remove", "--force", str(work))


def recover(bundle, acme):
    restore_geo(bundle)
    nginx_test(bundle)
    backend(bundle)
    start_nginx(bundle)
    verify_web(bundle, acme)
    activate(bundle)


def guard_result(bundle, *args):
    result = command("/usr/bin/python3", str(Path(__file__).with_name("gf3_guard.py")), *args, timeout=380,
                     extra_env={"GF3_GUARD_ROOT": str(ROOT), "GF3_GUARD_RUNTIME": str(RUNTIME),
                                "GF3_GUARD_ACTIVE_COMPOSE": str(bundle / "compose.json")})
    try:
        result = json.loads(result)
    except ValueError:
        raise ReleaseError("Guard returned invalid JSON") from None
    require(isinstance(result, dict), "Guard returned an invalid result")
    return result


def run_initial_guard(bundle):
    require((bundle / "guard-enabled").is_file(), "Active release is missing the Guard marker")
    result = guard_result(bundle, "run", "--require-lock")
    run_id = result.get("last_run_id")
    require(result.get("outcome") == "completed" and isinstance(run_id, str)
            and re.fullmatch(r"[0-9a-f]{32}", run_id) and result.get("last_error") is None,
            "Guard did not report a completed initial run")
    return run_id


def approve_guard(bundle, run_id):
    result = guard_result(bundle, "status")
    require(result.get("last_run_id") == run_id and "last_error" in result and result["last_error"] is None,
            "Persisted Guard status does not confirm this activation run")


def timer_state():
    result = command("systemctl", "show", "gf3-guard.timer", "--property=ActiveState,UnitFileState")
    values = dict(line.split("=", 1) for line in result.splitlines() if "=" in line)
    require(values.get("UnitFileState") in ("enabled", "disabled")
            and values.get("ActiveState") in ("active", "inactive"),
            "Guard timer must be enabled or disabled and active or inactive; inspect its unit state")
    return values["UnitFileState"] == "enabled", values["ActiveState"] == "active"


def restore_timer(previous):
    enabled, active = previous
    if not active:
        command("systemctl", "stop", "gf3-guard.timer")
    command("systemctl", "enable" if enabled else "disable", "gf3-guard.timer")
    if active:
        command("systemctl", "start", "gf3-guard.timer")
    require(timer_state() == previous, "Guard timer state was not restored")


def verify_guard_quiet():
    state = command("systemctl", "show", "gf3-guard.service", "--property=ActiveState", "--value").strip()
    require(state in ("inactive", "failed"), "Guard service did not drain")


def check_git_target(sha):
    require(git("branch", "--show-current").strip() == "PROD2", "Expected PROD2 checkout")
    require(not git("status", "--porcelain"), "Production checkout is dirty")
    git("merge-base", "--is-ancestor", "HEAD", sha)


@contextmanager
def activation_stage(name):
    print(f"[{name}]")
    try:
        yield
    except (Exception, KeyboardInterrupt) as error:
        if isinstance(error, ReleaseError):
            detail = str(error)
        elif isinstance(error, subprocess.TimeoutExpired):
            detail = "command timed out; inspect the corresponding unit or service"
        else:
            detail = f"{type(error).__name__}; inspect local service and Git state"
        raise ReleaseError(f"{name}: {detail}") from None


def finish_activation(candidate, baseline, sha):
    with activation_stage("guard-initial-run"):
        run_id = run_initial_guard(candidate)
    with activation_stage("guard-activation-lock"), lock(RUNTIME / "state/guard.lock"):
        with activation_stage("guard-status"):
            approve_guard(candidate, run_id)
        with activation_stage("post-guard-health"):
            verify_web(candidate)
            require(topology() == baseline, "Network or database changed after Guard activation")
    with activation_stage("guard-timer-enable"):
        command("systemctl", "enable", "gf3-guard.timer")
    with activation_stage("guard-timer-start"):
        command("systemctl", "start", "gf3-guard.timer")
        require(timer_state() == (True, True), "Guard timer is not enabled and active")
    with activation_stage("git-fast-forward"):
        check_git_target(sha)
        git("merge", "--ff-only", sha)


def deploy_and_activate(sha, nginx_image):
    with activation_stage("approved-tooling"):
        require(SHA.fullmatch(sha), "Full approved SHA required")
        git("fetch", "origin", "refs/heads/PROD2:refs/remotes/origin/PROD2")
        require(git("rev-parse", "refs/remotes/origin/PROD2").strip() == sha,
                "Approved SHA is not the current PROD2 tip")
        verify_tooling(sha)
    with activation_stage("guard-timer-state"):
        previous_timer = timer_state()
    candidate = None
    previous_active = None
    try:
        with activation_stage("guard-timer-pause"):
            command("systemctl", "stop", "gf3-guard.timer")
        with lock(RUNTIME / "state/guard.lock"):
            # Drain an in-flight Guard before stopping any queued oneshot.
            with activation_stage("guard-service-drain"):
                command("systemctl", "stop", "gf3-guard.service")
                verify_guard_quiet()
            if (STATE / "active").exists():
                previous_active = active_bundle()
            with activation_stage("stage-and-cutover"):
                candidate, previous, baseline = deploy(sha, nginx_image)
        finish_activation(candidate, baseline, sha)
    except (Exception, KeyboardInterrupt) as failure:
        if candidate is not None:
            try:
                with activation_stage("activation-restore"):
                    command("systemctl", "stop", "gf3-guard.timer")
                    with lock(RUNTIME / "state/guard.lock"):
                        command("systemctl", "stop", "gf3-guard.service")
                        recover(previous, acme=True)
                        require(topology() == baseline, "Recovered network or data volume differs from baseline")
                        if previous_active is not None:
                            activate(previous_active)
            except ReleaseError as recovery:
                raise ReleaseError(f"{failure}; {recovery}. Retained bundle: {previous}") from None
        try:
            with activation_stage("guard-timer-restore"):
                restore_timer(previous_timer)
        except ReleaseError as recovery:
            raise ReleaseError(f"{failure}; {recovery}") from None
        if candidate is not None:
            raise ReleaseError(f"{failure}; prior release restored. Retained bundle: {previous}") from None
        raise
    print(f"Deployed PROD2 {sha}; guard active; recovery bundle {previous}")


def verify_ssh_and_certbot():
    command("fail2ban-client", "status", "sshd")
    command("systemctl", "is-active", "--quiet", "snap.certbot.renew.timer")


def cutover(candidate, previous, baseline, previous_active=None):
    try:
        backend(candidate)
        verify_backend_env(candidate)
        start_nginx(candidate)
        require(topology() == baseline, "Network ID, CIDR or database volume changed during deploy")
        verify_web(candidate)
        activate(candidate)
    except (Exception, KeyboardInterrupt) as failure:
        print(f"Cutover failed: {failure}. Restoring {previous.name}.", file=sys.stderr)
        try:
            recover(previous, acme=True)
            require(topology() == baseline, "Recovery network or volume differs from baseline")
            if previous_active is not None:
                activate(previous_active)
        except (Exception, KeyboardInterrupt) as rollback:
            raise ReleaseError(f"Automatic recovery failed. Retained bundle: {previous}") from rollback
        raise ReleaseError("Cutover failed; prior release restored") from failure


def verify_backend_env(bundle):
    candidate = docker_json(*compose_args(), "-f", str(bundle / "compose.json"), "config", "--format", "json")
    expected = candidate["services"]["gf3-app"]["environment"]
    live = dict(value.split("=", 1) for value in container("gf3-app")["Config"]["Env"])
    keys = ("GF3_TRUSTED_PROXY_CIDR", "AllowedHosts", "ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT")
    require(all(live.get(key) == expected.get(key) for key in keys)
            and not live.get("ASPNETCORE_FORWARDEDHEADERS_ENABLED")
            and not live.get("FORWARDEDHEADERS_ENABLED"), "Live proxy environment differs from the approved candidate")


def verify_tooling(sha):
    for path in (Path(__file__), Path(__file__).with_name("gf3_guard.py"), GUARD_TIMER_UNIT):
        safe_path(path)
        require(git("hash-object", "--no-filters", str(path)).strip() ==
                git("rev-parse", f"{sha}:deploy/security/{path.name}").strip(),
                f"Installed {path.name} differs from the approved release")
    result = command("systemctl", "show", "gf3-guard.timer", "--property=NeedDaemonReload,FragmentPath,DropInPaths")
    values = dict(line.split("=", 1) for line in result.splitlines() if "=" in line)
    require(values.get("NeedDaemonReload") == "no" and values.get("FragmentPath") == str(GUARD_TIMER_UNIT)
            and values.get("DropInPaths") == "", "Check the loaded Guard timer and drop-ins, then run daemon-reload")


def deploy(sha, nginx_image):
    require(IMAGE_DIGEST.fullmatch(nginx_image), "Approved Nginx digest required")
    check_git_target(sha)
    safe_path(ROOT / "deploy/.env", private=True)
    before = git("rev-parse", "HEAD").strip()
    previous_active = active_bundle() if (STATE / "active").exists() else None
    if previous_active is not None and (previous_active / "release.json").is_file():
        before = json.loads((previous_active / "release.json").read_text())["sha"]
    git("merge-base", "--is-ancestor", before, sha)
    baseline = topology()
    verify_ipv6()
    verify_ssh_and_certbot()
    candidate = stage(sha, before, baseline[1], baseline[2], nginx_image)
    previous = Path(tempfile.mkdtemp(prefix="rollback-", dir=STATE))
    snapshot(previous, baseline[2])
    store(previous / "baseline.json", baseline)
    store(previous / "release.json", {"sha": before})
    nginx_test(previous)
    verify_web(previous)
    backup(previous, baseline[2])
    require(not git("status", "--porcelain"), "Production checkout changed during staging")
    require(topology() == baseline, "Production network or volume changed during staging")
    store(candidate / "release.json", {"sha": sha, "previous": str(previous), "baseline": baseline})
    if previous_active is None:
        activate(previous)
    cutover(candidate, previous, baseline, previous_active)
    return candidate, previous, baseline


def renew():
    if os.environ.get("RENEWED_LINEAGE") != "/etc/letsencrypt/live/app-gf.com":
        return
    if not (STATE / "active").exists():
        return
    bundle = active_bundle()
    nginx_test(bundle)
    start_nginx(bundle)
    verify_web(bundle)


def main():
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="action", required=True)
    release = sub.add_parser("deploy")
    release.add_argument("sha")
    release.add_argument("nginx_image")
    preflight = sub.add_parser("preflight")
    preflight.add_argument("source", type=Path)
    preflight.add_argument("sha")
    sub.add_parser("renew")
    recovery = sub.add_parser("rollback")
    recovery.add_argument("bundle", type=Path)
    args = parser.parse_args()
    os.umask(0o077)
    signal.signal(signal.SIGTERM, lambda *_: signal.raise_signal(signal.SIGINT))
    try:
        require(os.geteuid() == 0, "Run with the approved root deployment account")
        safe_path(ROOT)
        safe_path(STATE, private=True)
        safe_path(RUNTIME / "state/guard.lock", private=True)
        with lock(Path("/run/lock/gf3-prod-deploy.lock")):
            if args.action == "deploy":
                deploy_and_activate(args.sha, args.nginx_image)
                return 0
            with lock(RUNTIME / "state/guard.lock"):
                if args.action == "preflight":
                    baseline = topology()
                    verify_ipv6()
                    verify_ssh_and_certbot()
                    safe_path(args.source)
                    require(SHA.fullmatch(args.sha)
                            and git("rev-parse", "refs/remotes/origin/PROD2").strip() == args.sha,
                            "Expected the full approved PROD2 SHA")
                    verified_checkout(args.source, args.sha)
                    safe_path(args.source / "deploy/.env", private=True)
                    verify_tooling(args.sha)
                    validate(render(args.source), baseline[1], baseline[2])
                    command("systemd-analyze", "verify", "/etc/systemd/system/gf3-guard.service",
                            "/etc/systemd/system/gf3-guard.timer")
                    print(f"Preflight OK: {baseline[1]}, volume {baseline[2]}")
                elif args.action == "renew":
                    renew()
                else:
                    safe_path(args.bundle, private=True)
                    require(args.bundle.parent == STATE and args.bundle.name.startswith("rollback-"),
                            "Use a retained rollback bundle")
                    config = json.loads((args.bundle / "compose.json").read_text())
                    recorded = json.loads((args.bundle / "baseline.json").read_text())
                    bridge, = docker_json("network", "inspect", NETWORK)
                    require(bridge["Id"] == recorded[0] and config["volumes"]["gf3-data"]["name"] == recorded[2],
                            "Wrong recovery network or volume")
                    docker_json("volume", "inspect", recorded[2])
                    recover(args.bundle, acme=True)
                    require(topology() == tuple(recorded), "Recovered topology differs from backup")
                    if not (args.bundle / "guard-enabled").is_file():
                        command("systemctl", "disable", "--now", "gf3-guard.timer")
    except (ReleaseError, OSError, ValueError, KeyError, subprocess.TimeoutExpired, sqlite3.Error, tarfile.TarError,
            KeyboardInterrupt) as error:
        detail = str(error) if isinstance(error, ReleaseError) else f"{type(error).__name__}; check local release state"
        print(f"GF3 release: {detail}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
