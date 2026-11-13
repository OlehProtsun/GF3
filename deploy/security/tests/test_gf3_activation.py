import contextlib
import io
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import gf3_release as release


class ActivationTests(unittest.TestCase):
    def test_each_activation_failure_restores_prior_stack_and_timer(self):
        candidate, previous = Path("candidate"), Path("previous")
        baseline = ("network", "172.18.0.0/16", "volume")
        cases = (
            ("guard-initial-run", "initial"),
            ("guard-status", "status"),
            ("post-guard-health", "health"),
            ("guard-timer-enable", "enable"),
            ("guard-timer-start", "start"),
            ("git-fast-forward", "merge"),
        )
        for stage, operation in cases:
            for original in ((False, False), (True, True)):
                with self.subTest(stage=stage, original=original):
                    state = list(original)
                    events = []
                    failed = False

                    def check(action):
                        nonlocal failed
                        events.append(action)
                        if action == operation and not failed:
                            failed = True
                            raise release.ReleaseError("synthetic failure")

                    def command(*args, **_):
                        action, unit = args[1:]
                        if unit.endswith(".timer"):
                            check(action)
                            if action in ("enable", "disable"):
                                state[0] = action == "enable"
                            else:
                                state[1] = action == "start"
                        else:
                            check("drain")

                    def git(action, *_):
                        if action == "rev-parse":
                            return "a" * 40
                        if action == "merge":
                            check("merge")
                        return "PROD2" if action == "branch" else ""

                    with (patch.object(release, "command", command), patch.object(release, "git", git),
                          patch.object(release, "verify_tooling"), patch.object(release, "lock", return_value=contextlib.nullcontext()),
                          patch.object(release, "verify_guard_quiet"),
                          patch.object(release, "timer_state", side_effect=lambda: tuple(state)),
                          patch.object(release, "deploy", return_value=(candidate, previous, baseline)),
                          patch.object(release, "run_initial_guard", side_effect=lambda _: check("initial")),
                          patch.object(release, "approve_guard", side_effect=lambda *_: check("status")),
                          patch.object(release, "verify_web", side_effect=lambda _: check("health")),
                          patch.object(release, "topology", return_value=baseline),
                          patch.object(release, "STATE", Path("nonexistent-gf3-test-state")),
                          patch.object(release, "recover", side_effect=lambda *_, **__: check("recover")),
                          contextlib.redirect_stdout(io.StringIO())):
                        with self.assertRaisesRegex(release.ReleaseError, f"{stage}:.*prior release restored"):
                            release.deploy_and_activate("a" * 40, "test")
                    self.assertEqual(tuple(state), original)
                    self.assertEqual(1, events.count("recover"))
                    self.assertEqual(1, events.count("initial"))
                    if operation != "merge":
                        self.assertNotIn("merge", events)

    def test_skipped_or_stale_guard_cannot_pass(self):
        bundle = Path("candidate")
        with patch.object(Path, "is_file", return_value=True), patch.object(
                release, "guard_result", return_value={"status": "another guard run holds the lock"}):
            with self.assertRaisesRegex(release.ReleaseError, "completed initial run"):
                release.run_initial_guard(bundle)
        for state in ({}, {"last_error": None, "last_run_id": "old"},
                      {"last_error": "failed", "last_run_id": "current"}):
            with patch.object(release, "guard_result", return_value=state):
                with self.assertRaisesRegex(release.ReleaseError, "Persisted Guard status"):
                    release.approve_guard(bundle, "current")

    def test_git_diagnostic_does_not_print_stderr(self):
        result = subprocess.CompletedProcess([], 128, "", "cannot create index.lock: secret-token")
        with patch.object(subprocess, "run", return_value=result):
            with self.assertRaisesRegex(release.ReleaseError, "Git index lock") as failure:
                release.git("merge", "--ff-only", "a" * 40)
        self.assertNotIn("secret-token", str(failure.exception))

    def test_preflight_rejects_existing_git_index_lock(self):
        with tempfile.TemporaryDirectory() as directory:
            lock_file = Path(directory) / "index.lock"
            with patch.object(release, "git", return_value=str(lock_file)):
                release.check_git_write_ready()
                lock_file.touch()
                with self.assertRaisesRegex(release.ReleaseError, "identify its owner"):
                    release.check_git_write_ready()

    def test_failed_automatic_recovery_retains_bundle_and_original_stage(self):
        with (patch.object(release, "git", return_value="a" * 40), patch.object(release, "verify_tooling"),
              patch.object(release, "verify_guard_quiet"),
              patch.object(release, "timer_state", return_value=(False, False)), patch.object(release, "command"),
              patch.object(release, "lock", return_value=contextlib.nullcontext()),
              patch.object(release, "deploy", return_value=(Path("candidate"), Path("rollback-retained"), ())),
              patch.object(release, "finish_activation", side_effect=release.ReleaseError("guard-status: failed")),
              patch.object(release, "recover", side_effect=release.ReleaseError("Nginx recovery failed")),
              patch.object(release, "STATE", Path("nonexistent-gf3-test-state")),
              contextlib.redirect_stdout(io.StringIO())):
            with self.assertRaisesRegex(release.ReleaseError, "guard-status.*activation-restore.*rollback-retained"):
                release.deploy_and_activate("a" * 40, "test")


if __name__ == "__main__":
    unittest.main()
