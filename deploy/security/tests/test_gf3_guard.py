from contextlib import redirect_stderr
from datetime import datetime, timezone
import gzip
import io
import ipaddress
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
from urllib.request import Request

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import gf3_guard as guard


def spamhaus(version=4, count=10):
    prefix = "45.10.{}.0/24" if version == 4 else "2606:4700:{:x}::/48"
    rows = [json.dumps({"cidr": prefix.format(index)}) for index in range(count)]
    return ("\n".join(rows + [json.dumps({"type": "metadata", "records": count,
                                           "copyright": "Synthetic data"})]) + "\n").encode()


def strongips(first=1):
    return ("\n".join(f"45.10.1.{index}" for index in range(first, first + 10)) + "\n").encode()


class FeedTests(unittest.TestCase):
    def test_normalization_metadata_duplicates_and_text_comments(self):
        for version in (4, 6):
            self.assertEqual(10, len(guard.parse_feed(spamhaus(version), version)))
        source = b"\xef\xbb\xbf# comment\r\n" + strongips().replace(b"\n", b"\r\n")
        source += b"2606:4700:0:0::1111\r\n2606:4700::1111\r\n"
        networks = guard.parse_feed(source, None)
        self.assertEqual(11, len(networks))
        self.assertIn(ipaddress.ip_network("2606:4700::1111/128"), networks)
        duplicate = b'{"cidr":"45.10.0.0/24"}\n'
        self.assertEqual(10, len(guard.parse_feed(duplicate + spamhaus().replace(b'"records": 10', b'"records": 11'), 4)))

    def test_rejects_malformed_incomplete_nonpublic_and_injection(self):
        for value in ("0.0.0.0/0", "45.0.0.0/8", "10.0.0.0/24", "192.0.2.0/24",
                      "127.0.0.0/24", "224.0.0.0/24", "169.254.0.0/16", "45.10.0.1/24",
                      "45.10.0.0/24 1; include /etc/passwd;", "2606:4700::/48", "45.10.0.1"):
            with self.subTest(value=value), self.assertRaises(ValueError):
                guard.parse_feed(b'{"cidr":' + json.dumps(value).encode() + b'}\n' + spamhaus(), 4)
        for value in (b"", b"45.10.1.1\n", strongips() + b"192.168.1.1\n",
                      strongips() + b"8.8.8.8/32\n", strongips() + b"\xff\n",
                      strongips() + b"8.8.8.8;\n"):
            with self.assertRaises(ValueError):
                guard.parse_feed(value, None)
        for value in (b"{bad}\n", b'{"cidr":"45.10.0.0/24","cidr":"45.10.0.0/24"}\n'):
            with self.assertRaises(ValueError):
                guard.parse_feed(value + spamhaus(), 4)
        with self.assertRaises(ValueError):
            guard.parse_feed(spamhaus().replace(b'"records": 10', b'"records": 11'), 4)
        with patch.object(guard, "MAX_ENTRIES", 9), self.assertRaises(ValueError):
            guard.parse_feed(strongips(), None)

    def test_download_bounds_and_gzip(self):
        for data in (strongips(), gzip.compress(strongips())):
            with patch.object(guard, "build_opener") as opener:
                opener.return_value.open.return_value = io.BytesIO(data)
                self.assertEqual(strongips(), guard.download("https://example.test/feed"))
                args, kwargs = opener.return_value.open.call_args
                self.assertEqual("https://example.test/feed", args[0].full_url)
                self.assertEqual("gf3-guard/1.0", args[0].get_header("User-agent"))
                self.assertEqual({"timeout": 10}, kwargs)
        with patch.object(guard, "build_opener") as opener:
            opener.return_value.open.return_value = io.BytesIO(b"x" * (guard.MAX_DOWNLOAD + 1))
            with self.assertRaisesRegex(ValueError, "oversized download"):
                guard.download("https://example.test/feed")
        with patch.object(guard, "build_opener") as opener, patch.object(guard, "MAX_DECODED", 100):
            opener.return_value.open.return_value = io.BytesIO(gzip.compress(b"x" * 101))
            with self.assertRaisesRegex(ValueError, "oversized decoded"):
                guard.download("https://example.test/feed")

    def test_redirect_stays_on_same_https_origin(self):
        handler = guard.SameOriginRedirect()
        origin = Request("https://example.test/feed")
        for target in ("http://example.test/feed", "https://other.test/feed", "https://example.test:444/feed"):
            with self.subTest(target=target), self.assertRaises(ValueError):
                handler.redirect_request(origin, None, 302, "", {}, target)
        redirect = handler.redirect_request(origin, None, 302, "", {}, "https://example.test/new")
        self.assertEqual("https://example.test/new", redirect.full_url)


class GuardTests(unittest.TestCase):
    def setUp(self):
        directory = tempfile.TemporaryDirectory()
        self.addCleanup(directory.cleanup)
        self.root = Path(directory.name)
        (self.root / "deploy").mkdir()
        (self.root / "deploy/.env").write_text('GF3_SERVER_NAME="app.example.test" # synthetic\n', encoding="utf-8")
        self.now = 1_750_000_000
        self.pid = 100
        self.commands = []
        self.fail = []
        self.stall_reload = False
        self.downloaded = []
        self.feed_data = {}
        self.guard = self.make_guard()

    def make_guard(self):
        return guard.Guard(self.root / "runtime", self.root, clock=lambda: self.now, fetch=self.fetch,
                           run_command=self.run_command, sleep=lambda _: None)

    def fetch(self, url):
        self.downloaded.append(url)
        value = self.feed_data.get(url)
        if value is None:
            raise OSError("synthetic outage")
        return value

    def run_command(self, args, **options):
        self.assertEqual(self.root, options["cwd"])
        self.assertFalse(options["shell"])
        self.assertFalse(options["check"])
        self.assertEqual(20, options["timeout"])
        action = tuple(args[args.index("nginx", args.index("exec")) + 1:])
        self.commands.append(action)
        if self.fail and action == self.fail[0]:
            self.fail.pop(0)
            return subprocess.CompletedProcess(args, 1, "", "synthetic credential must not be exposed")
        if action == ("nginx", "-s", "reload") and not self.stall_reload:
            self.pid += 1
        return subprocess.CompletedProcess(args, 0, f"PID COMMAND\n{self.pid} nginx: worker process\n", "")

    def execute(self, *args):
        return self.guard.execute(guard.arguments(args))

    def event(self, ip="45.10.1.1", ago=0, **fields):
        event = {"ts": datetime.fromtimestamp(self.now - ago, timezone.utc).isoformat(), "ip": ip,
                 "method": "GET", "path": "/.env", "status": 404, "probe": 1,
                 "vhost": "app.example.test", "request_time": 0.001}
        event.update(fields)
        return json.dumps(event).encode() + b"\n"

    def write_logs(self, *entries, mode="ab"):
        self.guard.log.parent.mkdir(parents=True, exist_ok=True)
        with self.guard.log.open(mode) as log:
            log.writelines(entries)

    @unittest.skipUnless(os.name == "posix", "advisory locks run on the Ubuntu host")
    def test_source_isolation_refresh_and_seven_day_expiry(self):
        v4, v6, txt = [item[0] for item in guard.SOURCES.values()]
        self.feed_data = {v4: spamhaus(), v6: spamhaus(6), txt: strongips()}
        result = self.execute("run")
        self.assertEqual(30, result["blocked"])
        self.assertEqual(3, len(self.downloaded))
        self.now += guard.DAY - 1
        self.assertFalse(self.execute("run")["reloaded"])
        self.assertEqual(3, len(self.downloaded))
        self.feed_data = {v6: spamhaus(6), txt: b"truncated"}
        self.now += 1
        result = self.execute("run")
        self.assertEqual(30, result["blocked"])
        self.assertEqual(guard.DAY, result["sources"]["spamhaus-v4"]["age_seconds"])
        self.assertTrue(result["sources"]["blocklist-strongips"]["last_error"])
        self.assertEqual(3, len(self.downloaded) - 3)
        self.now += 6 * guard.DAY
        result = self.execute("run")
        self.assertEqual(10, result["blocked"])
        self.assertTrue(result["sources"]["spamhaus-v4"]["stale"])
        self.assertTrue(result["sources"]["blocklist-strongips"]["stale"])

    def test_dry_run_is_read_only_and_logs_exclude_sensitive_fields(self):
        self.assertFalse(self.execute("run", "--dry-run")["would_change"])
        self.assertFalse(self.guard.runtime.exists())
        self.write_logs(self.event(path='/escaped/"quote'), self.event(access_token="secret"),
                        self.event(probe=True), b'{"path":"bad\x01"}\n', b"x" * (guard.MAX_LINE + 1) + b"\n")
        result = self.execute("run", "--dry-run")
        self.assertEqual(1, result["events"])
        self.assertEqual(4, result["malformed"])
        self.assertNotIn("secret", json.dumps(result))
        self.assertFalse(self.guard.private.exists())
        self.assertEqual([], self.downloaded + self.commands)

    def test_cursor_append_rotation_truncation_and_partial_record(self):
        state = guard.initial_state()
        parse = lambda: guard.process_logs(self.guard.log, state, self.now, "app.example.test")
        event = self.event()
        self.write_logs(event[:-4])
        self.assertEqual(0, parse()["events"])
        self.write_logs(event[-4:])
        self.assertEqual(1, parse()["events"])
        self.assertEqual(0, parse()["events"])
        self.write_logs(event)
        self.guard.log.rename(self.guard.log.with_name("access.jsonl.1"))
        self.write_logs(event)
        self.assertEqual(2, parse()["events"])
        self.write_logs(self.event(method="HEAD"), self.event(method="HEAD"), mode="wb")
        self.assertEqual(2, parse()["events"])

    def test_probe_threshold_timestamp_expiry_default_host_and_ipv6(self):
        state = guard.initial_state()
        self.write_logs(self.event(ago=700), self.event(ago=650), self.event(ago=640),
                        self.event(ago=30), self.event("45.10.2.2"),
                        *[self.event("45.10.2.3", probe=0, status=401) for _ in range(5)],
                        *[self.event("172.17.0.5") for _ in range(5)],
                        *[self.event("2606:4700::1111", ago=ago, vhost="_") for ago in (300, 200, 100, 5)],
                        *[self.event("45.10.1.1", ago=ago) for ago in (20, 15, 10)])
        counts = guard.process_logs(self.guard.log, state, self.now, "app.example.test")
        self.assertEqual(2, counts["auto_bans"])
        self.assertEqual({"45.10.1.1", "2606:4700::1111"}, state["bans"].keys())
        self.assertEqual(self.now - 5 + guard.AUTO_BAN, state["bans"]["2606:4700::1111"]["expires"])
        self.guard.private.mkdir()
        self.guard.save_state(state)
        self.now += guard.AUTO_BAN
        self.assertEqual(0, self.execute("status")["auto"])

    def test_offender_keys_are_bounded(self):
        state = guard.initial_state()
        with patch.object(guard, "MAX_OFFENDERS", 3):
            self.write_logs(*[self.event(f"45.10.1.{index}") for index in range(1, 6)])
            guard.process_logs(self.guard.log, state, self.now, "app.example.test")
        self.assertEqual({"45.10.1.3", "45.10.1.4", "45.10.1.5"}, state["recent"].keys())

    @unittest.skipUnless(os.name == "posix", "advisory locks run on the Ubuntu host")
    def test_manual_ban_allow_unallow_unban_and_restart(self):
        strongips_url = guard.SOURCES["blocklist-strongips"][0]
        self.feed_data = {strongips_url: strongips()}
        self.execute("run")
        self.execute("ban", "45.10.1.1", "--hours", "168")
        self.guard = self.make_guard()
        self.assertEqual("manual", self.guard.state()["bans"]["45.10.1.1"]["reason"])
        self.execute("allow", "45.10.1.0/24")
        self.assertEqual(b"45.10.1.0/24 1;\n", (self.guard.geo / guard.NGINX_FILES[1]).read_bytes())
        self.execute("unban", "45.10.1.1")
        self.assertEqual({}, self.guard.state()["overrides"])
        self.execute("unallow", "45.10.1.0/24")
        self.execute("unban", "45.10.1.1")
        self.assertEqual(self.now + guard.DAY, self.guard.state()["overrides"]["45.10.1.1"])
        self.assertEqual(b"45.10.1.1/32 1;\n", (self.guard.geo / guard.NGINX_FILES[1]).read_bytes())
        self.assertEqual(strongips(), (self.guard.private / "feed-blocklist-strongips.txt").read_bytes())
        self.execute("ban", "193.47.62.109")
        self.now += 7 * guard.DAY
        self.assertEqual(0, self.execute("run")["manual"])
        for command in (("ban", "10.0.0.1"), ("ban", "45.10.0.0/16"), ("allow", "0.0.0.0/0")):
            with redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
                guard.arguments(command)

    @unittest.skipUnless(os.name == "posix", "advisory locks run on the Ubuntu host")
    def test_atomic_noop_validation_and_reload_rollback(self):
        self.execute("ban", "2606:4700::1111")
        self.execute("ban", "45.10.1.1")
        path = self.guard.geo / guard.NGINX_FILES[0]
        previous = b"45.10.1.1/32 1;\n2606:4700::1111/128 1;\n"
        self.assertEqual(previous, path.read_bytes())
        inode = path.stat().st_ino
        self.commands.clear()
        self.assertFalse(self.execute("run")["reloaded"])
        self.assertEqual([], self.commands)
        self.assertEqual(inode, path.stat().st_ino)
        original = path.open("rb")
        self.addCleanup(original.close)
        for action in (("nginx", "-t"), ("nginx", "-s", "reload")):
            self.fail = [action]
            with self.assertRaisesRegex(guard.GuardError, "Nginx command failed"):
                self.execute("ban", "45.10.1.2")
            self.assertEqual(previous, path.read_bytes())
            self.assertNotIn("45.10.1.2", self.guard.state()["bans"])
            self.assertNotIn("credential", self.execute("status")["last_error"])
            self.assertEqual([], list(path.parent.glob("*.tmp")))
        self.assertNotEqual(inode, path.stat().st_ino)

    @unittest.skipUnless(os.name == "posix", "advisory locks run on the Ubuntu host")
    def test_missing_docker_and_lock_contention(self):
        self.execute("run")
        with guard.guard_lock(self.guard.private / "guard.lock"):
            self.assertIn("holds the lock", self.execute("ban", "45.10.1.1")["status"])
        with patch.object(self.guard, "run_command", side_effect=FileNotFoundError):
            with self.assertRaisesRegex(guard.GuardError, "Docker CLI unavailable"):
                self.execute("ban", "45.10.1.1")
        self.assertFalse((self.guard.geo / guard.NGINX_FILES[0]).exists())
        self.assertFalse(self.guard.state()["bans"])


if __name__ == "__main__":
    unittest.main()
