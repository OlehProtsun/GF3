import contextlib
import io
import json
import os
from pathlib import Path
import shutil
import stat
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
from urllib.error import URLError


sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import gf3_prepare_release as prepare


release = prepare.release
github_function = prepare.github
BASELINE = ("network-id", "172.18.0.0/16", "production-volume")
PIN = "nginx@sha256:" + "f" * 64


class ManagedDeploymentTests(unittest.TestCase):
    def setUp(self):
        self.contexts = contextlib.ExitStack()
        self.addCleanup(self.contexts.close)
        self.enterContext = self.contexts.enter_context
        fixture = self.enterContext(tempfile.TemporaryDirectory())
        self.base = Path(fixture).resolve()
        self.remote = self.base / "origin.git"
        self.root = self.base / "root"
        self.state = self.base / "state"
        self.state.mkdir(mode=0o700)
        self.approval = self.base / "approved-release.conf"
        self.deploy_lock = self.base / "deploy.lock"
        self.original_git = release.git
        self.git("init", "--bare", str(self.remote), root=self.base)
        self.git("init", "-b", "PROD2", str(self.root), root=self.base)
        self.git("config", "user.name", "Disposable CI")
        self.git("config", "user.email", "ci@example.invalid")
        self.git("config", "core.autocrlf", "false")
        self.git("remote", "add", "origin", str(self.remote))
        self.commit_file("FrontEnd/app.txt", "prior")
        self.current = self.commit_file("deploy/.gitignore", ".env\n")
        self.git("push", "origin", "PROD2")
        self.writer = self.base / "writer"
        self.git("clone", "-b", "PROD2", str(self.remote), str(self.writer), root=self.base)
        self.git("config", "user.name", "Disposable CI", root=self.writer)
        self.git("config", "user.email", "ci@example.invalid", root=self.writer)
        self.env = self.root / "deploy/.env"
        self.env.write_bytes(b"SECRET=never-print-this\n")
        self.env.chmod(0o600)
        self.approval_contents = (f"# Existing approval\r\nAPPROVED_SHA={self.current}\r\n"
                                  f"NGINX_IMAGE={PIN}\r\nOTHER=untouched").encode()
        self.approval.write_bytes(self.approval_contents)
        self.approval.chmod(0o600)
        self.previous = self.make_bundle("release-prior", self.current)
        rollback = self.make_bundle("rollback-prior", self.current)
        (rollback / "SQLite.db").write_bytes(b"disposable backup marker")
        release.store(self.previous / "release.json", {"sha": self.current, "baseline": BASELINE,
                                                       "previous": str(rollback)})
        self.active_directory = self.previous
        if os.name == "nt":
            # Windows runners may lack the privilege to create symlinks.
            self.enterContext(patch.object(release, "active_bundle", side_effect=lambda: self.active_directory))
        self.set_active(self.previous)
        self.target = self.current
        self.status = "completed"
        self.conclusion = "success"
        self.newer_run = None
        self.run_overrides = {}
        self.job_overrides = {}
        self.timer = [True, True]
        self.deployment_calls = 0
        self.preflight_calls = 0
        self.failed_preflight = False
        self.failed_activation = False
        self.app_health = "healthy"
        self.post_deploy_unhealthy = False
        self.recovered = False
        self.locked = False
        self.output = io.StringIO()
        self.do_confirm = lambda _: True

        self.enterContext(contextlib.redirect_stdout(self.output))
        self.enterContext(contextlib.redirect_stderr(self.output))
        self.enterContext(patch.multiple(prepare, APPROVAL=self.approval, DEPLOY_LOCK=self.deploy_lock,
                                         ALLOWED_ORIGINS={str(self.remote).lower()}))
        self.enterContext(patch.multiple(release, ROOT=self.root, STATE=self.state, RUNTIME=self.state,
                                         safe_path=self.check_fixture_path, docker_json=self.docker,
                                         curl=lambda *_: '{"status":"ok","canConnect":true}', command=self.command,
                                         verify_tooling=lambda _: None, lock=self.lock))
        self.engine = self.enterContext(patch.object(prepare, "engine", side_effect=self.engine_call))
        self.api = self.enterContext(patch.object(prepare, "github", side_effect=self.github))
        self.confirm = self.enterContext(patch.object(prepare, "confirm", side_effect=lambda sha: self.do_confirm(sha)))
        if os.name == "nt":
            self.enterContext(patch.object(prepare, "sync_directory"))
            self.enterContext(patch.object(prepare, "protected_file", side_effect=Path.read_bytes))

    def git(self, *args, root=None):
        return subprocess.check_output(["git", *args], cwd=root or self.root, stderr=subprocess.PIPE).decode()

    def check_fixture_path(self, path, private=False):
        for entry in (path, *path.parents):
            if not entry.is_relative_to(self.base):
                break
            info = entry.lstat()
            self.assertFalse(stat.S_ISLNK(info.st_mode), entry)
        if private and os.name != "nt":
            release.require(not path.stat().st_mode & 0o077, f"Private permissions required: {path}")

    def make_bundle(self, name, sha):
        bundle = self.state / name
        bundle.mkdir(mode=0o700)
        release.store(bundle / "release.json", {"sha": sha, "baseline": BASELINE})
        release.store(bundle / "compose.json", {})
        (bundle / "guard-enabled").touch()
        return bundle

    def set_active(self, bundle):
        if os.name == "nt":
            self.active_directory = bundle
        else:
            release.activate(bundle)

    def commit_file(self, path, contents, *, root=None):
        root = root or self.root
        file = root / path
        file.parent.mkdir(parents=True, exist_ok=True)
        file.write_text(contents, encoding="utf-8")
        self.git("add", path, root=root)
        self.git("commit", "-m", path, root=root)
        return self.git("rev-parse", "HEAD", root=root).strip()

    def push(self, path="FrontEnd/app.txt", contents="candidate"):
        self.target = self.commit_file(path, contents, root=self.writer)
        self.git("push", "origin", "PROD2", root=self.writer)
        return self.target

    @contextlib.contextmanager
    def lock(self, path):
        if path != self.deploy_lock:
            yield
            return
        self.assertFalse(self.locked)
        actual_lock = self.real_lock(path) if os.name != "nt" else contextlib.nullcontext()
        with actual_lock:
            self.locked = True
            try:
                yield
            finally:
                self.locked = False

    real_lock = staticmethod(release.lock)

    def command(self, *args, **kwargs):
        if args[0] == "git":
            try:
                return self.git(*args[1:], root=kwargs.get("cwd"))
            except subprocess.CalledProcessError as error:
                raise release.ReleaseError(f"git {args[2]} failed (exit {error.returncode})") from None
        self.assertEqual(args[0], "systemctl", args)
        operation, unit = args[1:3]
        if operation == "show":
            enabled, active = self.timer
            return f"ActiveState={'active' if active else 'inactive'}\nUnitFileState={'enabled' if enabled else 'disabled'}\n"
        if unit == "gf3-guard.timer":
            if operation in ("enable", "disable"):
                self.timer[0] = operation == "enable"
            else:
                self.timer[1] = operation == "start"
        return ""

    def docker(self, *args):
        if args[:2] == ("image", "inspect"):
            self.assertEqual(args[2], PIN)
            return [{"Id": "pinned-image"}]
        if args[:2] == ("network", "inspect"):
            return [{"Id": BASELINE[0], "Containers": {"app": {}, "nginx": {}},
                     "IPAM": {"Config": [{"Subnet": BASELINE[1]}]}}]
        app = args[1].endswith("gf3-app-1")
        state = {"Running": True, "Health": {"Status": self.app_health}}
        return [{"Id": "app" if app else "nginx", "Image": "pinned-image", "State": state,
                 "Config": {"Labels": {"com.docker.compose.project": release.PROJECT}},
                 "NetworkSettings": {"Networks": {release.NETWORK: {"IPAddress": "172.18.0.2"}}},
                 "Mounts": [{"Destination": "/data", "Type": "volume", "Name": BASELINE[2]}],
                 "HostConfig": {"PortBindings": {}}}]

    def github(self, path, **query):
        run = {"workflow_id": prepare.WORKFLOW_ID, "name": "CI", "path": prepare.WORKFLOW_PATH,
               "head_branch": "PROD2", "event": "push", "head_sha": self.target,
               "repository": {"full_name": prepare.REPOSITORY}, "head_repository": {"full_name": prepare.REPOSITORY},
               "run_number": 12, "run_attempt": 2, "id": 34, "status": self.status, "conclusion": self.conclusion}
        if path.startswith("workflows/"):
            rows = [{**run, **self.run_overrides}]
            if self.newer_run:
                rows.append({**run, **self.newer_run})
            return {"total_count": len(rows), "workflow_runs": rows}
        self.assertEqual(path, "runs/34/attempts/2/jobs")
        common = {"run_id": 34, "run_attempt": 2, "head_sha": self.target, "head_branch": "PROD2",
                  "workflow_name": "CI", "status": "completed", "conclusion": "success"}
        jobs = [{"name": name, **common, **self.job_overrides.get(name, {})} for name in sorted(prepare.JOBS)]
        return {"total_count": len(jobs), "jobs": jobs}

    def engine_call(self, operation, *args):
        self.assertFalse(self.locked, f"{operation} would deadlock the installed engine")
        if operation == "preflight":
            self.preflight_calls += 1
            work, revision = args
            self.assertEqual(self.git("rev-parse", "HEAD", root=work).strip(), revision)
            self.assertEqual(self.git("branch", "--show-current", root=work), "")
            self.assertEqual(work.parent.parent, self.state)
            self.assertEqual((work / "deploy/.env").read_bytes(), self.env.read_bytes())
            if os.name != "nt":
                self.assertEqual(stat.S_IMODE(work.parent.stat().st_mode), 0o700)
                self.assertEqual(stat.S_IMODE((work / "deploy/.env").stat().st_mode), 0o600)
            return int(self.failed_preflight)

        self.deployment_calls += 1
        sha, image = args
        self.assertEqual(image, PIN)
        self.assertIn(f"APPROVED_SHA={sha}".encode(), self.approval.read_bytes())
        bundle = self.make_bundle("release-target", sha)

        def deploy(*_):
            self.set_active_for_recovery(bundle)
            return bundle, self.previous, BASELINE

        def verify_web(_):
            if self.failed_activation:
                raise release.ReleaseError("synthetic HTTPS failure")

        def recover(previous, acme):
            self.assertEqual(previous, self.previous)
            self.assertTrue(acme)
            self.recovered = True

        with patch.multiple(release, deploy=deploy, run_initial_guard=lambda _: "completed",
                            approve_guard=lambda *_: None, verify_web=verify_web, recover=recover,
                            verify_guard_quiet=lambda: None, activate=self.set_active_for_recovery):
            if os.name == "nt":
                (self.state / "active").touch()
            try:
                with release.lock(self.deploy_lock):
                    release.deploy_and_activate(sha, image)
            except release.ReleaseError as error:
                print(error)
                return 1
        if self.post_deploy_unhealthy:
            self.app_health = "unhealthy"
        return 0

    def set_active_for_recovery(self, bundle):
        if os.name == "nt":
            self.set_active(bundle)
        else:
            link = self.state / "active"
            link.unlink(missing_ok=True)
            link.symlink_to(bundle, target_is_directory=True)

    def assert_preparation_failed(self, pattern):
        with self.assertRaisesRegex(release.ReleaseError, pattern):
            prepare.prepare(False)
        self.assertEqual(self.deployment_calls, 0)
        self.assertEqual(self.approval.read_bytes(), self.approval_contents)
        self.assertEqual(list(self.state.glob("preflight-*")), [])

    def test_success_preflights_confirms_approves_and_activates(self):
        self.push()
        self.assertEqual(prepare.prepare(False), 0)
        self.assertEqual(self.deployment_calls, 1)
        self.assertEqual(self.preflight_calls, 1)
        self.assertEqual(self.git("rev-parse", "HEAD").strip(), self.target)
        self.assertEqual(self.approval.read_bytes(), self.approval_contents.replace(
            f"APPROVED_SHA={self.current}".encode(), f"APPROVED_SHA={self.target}".encode()))
        backup, = self.state.glob("approval-*/approved-release.conf")
        self.assertEqual(backup.read_bytes(), self.approval_contents)
        if os.name != "nt":
            self.assertEqual(stat.S_IMODE(backup.parent.stat().st_mode), 0o700)
            self.assertEqual(stat.S_IMODE(backup.stat().st_mode), 0o600)
            self.assertEqual(stat.S_IMODE(self.approval.stat().st_mode), 0o600)
            self.assertEqual(self.approval.stat().st_uid, os.geteuid())
        self.assertTrue((self.state / "release-target/git-activation.json").is_file())
        self.assertEqual(list(self.state.glob("preflight-*")), [])
        self.assertNotIn("never-print-this", self.output.getvalue())
        self.assertIn("Retained rollback bundle and SQLite snapshot present", self.output.getvalue())

    def test_already_up_to_date_and_check_only(self):
        self.assertEqual(prepare.prepare(False), 0)
        self.api.assert_not_called()
        self.engine.assert_not_called()
        self.assertEqual(prepare.prepare(True), 0)
        self.assertEqual(self.preflight_calls, 1)
        self.confirm.assert_not_called()
        self.assertEqual(self.approval.read_bytes(), self.approval_contents)
        self.assertEqual(list(self.state.glob("preflight-*")), [])

    def test_pending_failed_cancelled_or_superseded_ci(self):
        self.push()
        for status, conclusion in (("in_progress", None), ("completed", "failure"), ("completed", "cancelled")):
            with self.subTest(status=status, conclusion=conclusion):
                self.newer_run = {"id": 35, "run_number": 13, "status": status, "conclusion": conclusion}
                self.assert_preparation_failed("pending or unsuccessful")
        self.engine.assert_not_called()

    def test_wrong_workflow_sha_branch_event_and_job_attempt(self):
        self.push()
        for key, value in (("head_sha", self.current), ("head_branch", "DEV2"), ("event", "pull_request"),
                           ("workflow_id", 99), ("path", ".github/workflows/other.yml")):
            with self.subTest(key=key):
                self.run_overrides = {key: value}
                self.assert_preparation_failed("unexpected identity")
        self.run_overrides = {}
        self.job_overrides = {"Build and test": {"run_attempt": 1}}
        self.assert_preparation_failed("lacks a successful result")
        self.engine.assert_not_called()

    def test_missing_ci_and_api_outage(self):
        self.push()
        with patch.object(prepare, "github", return_value={"total_count": 0, "workflow_runs": []}):
            self.assert_preparation_failed("No PROD2 push CI run")
        with patch.object(prepare, "github", wraps=github_function), patch.object(
                prepare, "build_opener") as opener:
            opener.return_value.open.side_effect = URLError("synthetic outage")
            self.assert_preparation_failed("verification unavailable")
        self.engine.assert_not_called()

    def test_dirty_and_unexpected_git_flags_and_lock(self):
        self.push()
        source = self.root / "FrontEnd/app.txt"
        source.write_text("dirty")
        self.assert_preparation_failed("checkout is dirty")
        self.git("restore", "FrontEnd/app.txt")
        self.git("update-index", "--assume-unchanged", "FrontEnd/app.txt")
        self.assert_preparation_failed("Unexpected Git index flags")
        self.git("update-index", "--no-assume-unchanged", "FrontEnd/app.txt")
        self.git("update-index", "--skip-worktree", "FrontEnd/app.txt")
        self.assert_preparation_failed("Unexpected Git index flags")
        self.git("update-index", "--no-skip-worktree", "FrontEnd/app.txt")
        (self.root / ".git/index.lock").touch()
        self.assert_preparation_failed("index lock")

    def test_non_fast_forward_and_manifest_mismatch(self):
        self.push()
        divergent = self.commit_file("FrontEnd/local.txt", "divergent")
        self.assert_preparation_failed("HEAD differs from the active release")
        release.store(self.previous / "release.json", {"sha": divergent})
        self.assert_preparation_failed("Cannot establish a fast-forward")

    def test_sensitive_changes_require_maintenance(self):
        for path in ("DataAccessLayer/Migrations/M.cs", "GF3.WebApi/Program.cs",
                     "deploy/security/gf3_guard.py", "deploy/docker-compose.https.yml",
                     "GF3.WebApi/Infrastructure/TrustedProxyConfiguration.cs", "misc/example.service"):
            with self.subTest(path=path):
                self.push(path)
                self.assert_preparation_failed("Separate manual maintenance approval")
                self.assertIn(path, self.output.getvalue())
        self.api.assert_not_called()
        self.engine.assert_not_called()

    def test_new_helper_requires_the_exact_installed_blob(self):
        self.push(prepare.HELPER_PATH, "different helper")
        self.assert_preparation_failed("Separate manual maintenance approval")
        file = self.writer / prepare.HELPER_PATH
        shutil.copyfile(prepare.__file__, file)
        self.git("add", prepare.HELPER_PATH, root=self.writer)
        self.git("commit", "-m", "Install reviewed helper", root=self.writer)
        self.target = self.git("rev-parse", "HEAD", root=self.writer).strip()
        self.git("push", "origin", "PROD2", root=self.writer)
        self.assertEqual(prepare.prepare(True), 0)
        self.confirm.assert_not_called()

    def test_remote_moves_after_confirmation(self):
        target = self.push()

        def move(sha):
            self.assertEqual(sha, target)
            self.push(contents="new remote tip")
            self.target = target  # CI still returns the reviewed run.
            return True

        self.do_confirm = move
        self.assert_preparation_failed("Remote PROD2 tip changed")
        self.assertEqual(self.preflight_calls, 1)

    def test_ci_attempt_changes_after_confirmation(self):
        self.push()

        def rerun(_):
            self.newer_run = {"run_number": 13, "status": "queued", "conclusion": None}
            return True

        self.do_confirm = rerun
        self.assert_preparation_failed("pending or unsuccessful")

    def test_declined_confirmation_and_failed_preflight(self):
        self.push()
        self.do_confirm = lambda _: False
        self.assertEqual(prepare.prepare(False), 0)
        self.assertEqual(self.approval.read_bytes(), self.approval_contents)
        self.assertEqual(self.deployment_calls, 0)
        self.confirm.reset_mock()
        self.failed_preflight = True
        self.assert_preparation_failed("engine preflight failed")
        self.confirm.assert_not_called()

    def test_failed_activation_reports_prior_health_and_recovery_bundle(self):
        self.push()
        self.failed_activation = True
        self.assertEqual(prepare.prepare(False), 1)
        self.assertEqual(self.deployment_calls, 1)
        self.assertTrue(self.recovered)
        self.assertEqual(self.timer, [True, True])
        self.assertEqual(self.git("rev-parse", "HEAD").strip(), self.current)
        output = self.output.getvalue()
        self.assertIn("post-guard-health: synthetic HTTPS failure; prior release restored", output)
        self.assertIn(f"Production healthy on prior release {self.current}", output)

    def test_post_deploy_health_failure_is_reported_without_a_second_deploy(self):
        self.push()
        self.post_deploy_unhealthy = True
        with self.assertRaisesRegex(release.ReleaseError, "App container is unhealthy"):
            prepare.prepare(False)
        self.assertEqual(self.deployment_calls, 1)

    def test_approval_file_permissions_duplicate_values_and_lock(self):
        self.push()
        if os.name != "nt":
            self.approval.chmod(0o640)
            with self.assertRaisesRegex(release.ReleaseError, "Private permissions required"):
                prepare.read_approval()
            self.approval.chmod(0o600)
        self.approval.write_bytes(self.approval_contents + b"\nAPPROVED_SHA=" + b"f" * 40 + b"\n")
        with self.assertRaisesRegex(release.ReleaseError, "duplicate APPROVED_SHA"):
            prepare.read_approval()
        self.approval.write_bytes(self.approval_contents)

        original = prepare.approve

        def assert_locked(*args):
            self.assertTrue(self.locked)
            if os.name != "nt":
                import fcntl

                with self.deploy_lock.open("rb") as handle:
                    with self.assertRaises(BlockingIOError):
                        fcntl.flock(handle, fcntl.LOCK_EX | fcntl.LOCK_NB)
            original(*args)

        with patch.object(prepare, "approve", side_effect=assert_locked):
            self.assertEqual(prepare.prepare(False), 0)
