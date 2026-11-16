from contextlib import closing, nullcontext
from pathlib import Path
import json
import sqlite3
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import Mock, patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import gf3_maintenance as maintenance
import gf3_release as release


BEFORE = ("20260101000000_Initial",)
AFTER = BEFORE + ("20260102000000_AddComment",)
COLUMNS = (("employee", "comment", "TEXT"),)


class MaintenanceTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()
        self.live = self.root / "live.db"
        self.snapshot = self.root / "snapshot.db"
        self.clone = self.root / "clone.db"
        with closing(sqlite3.connect(self.live)) as db, db:
            db.executescript("""
                CREATE TABLE employee(id INTEGER PRIMARY KEY, name TEXT, note BLOB,
                                      CONSTRAINT ck_employee_id CHECK (id > 0));
                INSERT INTO employee VALUES (1, 'Fixture Employee', X'00FF');
                CREATE TABLE __EFMigrationsHistory(MigrationId TEXT PRIMARY KEY, ProductVersion TEXT NOT NULL);
            """)
            db.execute("INSERT INTO __EFMigrationsHistory VALUES (?, '10.0.0')", BEFORE)

    def apply(self, db):
        db.execute('ALTER TABLE employee ADD COLUMN "comment" TEXT NULL')
        db.execute("INSERT INTO __EFMigrationsHistory VALUES (?, '10.0.1')", AFTER[-1:])

    def test_online_backup_includes_committed_wal_and_clone_preserves_data(self):
        with closing(sqlite3.connect(self.live)) as writer:
            writer.execute("PRAGMA journal_mode=WAL")
            writer.execute("INSERT INTO employee VALUES (2, 'WAL fixture', NULL)")
            writer.commit()
            maintenance.snapshot(self.live, self.snapshot, BEFORE)
        maintenance.snapshot(self.snapshot, self.clone, BEFORE)
        with closing(sqlite3.connect(self.clone)) as db, db:
            self.apply(db)
        maintenance.verify_migration(self.snapshot, self.clone, BEFORE, AFTER, COLUMNS)
        with closing(sqlite3.connect(self.live)) as db:
            self.assertEqual(maintenance.verify(db, BEFORE)[0][0], BEFORE[0])
            self.assertEqual(db.execute("SELECT COUNT(*) FROM employee").fetchone(), (2,))

    def test_quiesced_backup_refuses_an_existing_writer_and_cleans_up(self):
        with closing(sqlite3.connect(self.live)) as writer:
            writer.execute("BEGIN IMMEDIATE")
            writer.execute("DELETE FROM employee")
            with self.assertRaisesRegex(sqlite3.OperationalError, "locked"):
                maintenance.snapshot(self.live, self.snapshot, BEFORE, quiesced=True)
            self.assertFalse(self.snapshot.exists())
        maintenance.snapshot(self.live, self.snapshot, BEFORE, quiesced=True)
        # Recover into a fresh file. No live writer or in-place replacement is needed.
        maintenance.snapshot(self.snapshot, self.clone, BEFORE)
        with closing(sqlite3.connect(self.clone)) as recovered:
            self.assertEqual(recovered.execute("SELECT name, note FROM employee").fetchall(),
                             [("Fixture Employee", b"\x00\xff")])
            self.assertEqual(maintenance.verify(recovered, BEFORE), ((BEFORE[0], "10.0.0"),))

    def test_missing_corrupt_and_wrong_history_leave_no_backup(self):
        corrupt = self.root / "corrupt.db"
        corrupt.write_bytes(b"this is not SQLite")
        for source, ids in ((self.root / "missing.db", BEFORE), (corrupt, BEFORE), (self.live, AFTER)):
            with self.subTest(source=source.name):
                with self.assertRaises((OSError, sqlite3.Error, maintenance.MaintenanceError)):
                    maintenance.snapshot(source, self.snapshot, ids)
                self.assertFalse(self.snapshot.exists())

    def test_rehearsal_rejects_extra_schema_changes_and_modified_rows(self):
        maintenance.snapshot(self.live, self.snapshot, BEFORE)
        for sql in ("CREATE INDEX extra ON employee(name)", "UPDATE employee SET name = 'changed'",
                    "UPDATE employee SET comment = 'new value'"):
            with self.subTest(sql=sql):
                maintenance.snapshot(self.snapshot, self.clone, BEFORE)
                with closing(sqlite3.connect(self.clone)) as db, db:
                    self.apply(db)
                    db.execute(sql)
                with self.assertRaises(maintenance.MaintenanceError):
                    maintenance.verify_migration(self.snapshot, self.clone, BEFORE, AFTER, COLUMNS)
                self.clone.unlink()

    def test_interruption_and_startup_failure_remove_container_and_clone(self):
        maintenance.snapshot(self.live, self.snapshot, BEFORE)
        for failure in (KeyboardInterrupt(), subprocess.TimeoutExpired("docker", 1), ValueError("startup")):
            with self.subTest(failure=type(failure).__name__):
                with (patch.object(maintenance, "start_clone", side_effect=failure),
                      patch.object(maintenance, "remove_clone") as remove,
                      self.assertRaises(type(failure))):
                    maintenance.rehearse(Mock(), Mock(), "image", self.root, self.snapshot, BEFORE, AFTER, COLUMNS)
                remove.assert_called_once()
                self.assertEqual(list(self.root.glob("clone-*")), [])
                with closing(sqlite3.connect(self.live)) as db:
                    maintenance.verify(db, BEFORE)

    def test_health_probe_does_not_exec_into_container_with_failed_startup(self):
        command = Mock()
        inspect = Mock(side_effect=[
            [{"State": {"Running": True, "Health": {"Status": "starting"}}}],
            [{"State": {"Running": False, "Health": {"Status": "unhealthy"}}}],
        ])
        with (patch.object(maintenance.time, "sleep"),
              self.assertRaisesRegex(maintenance.MaintenanceError, "Candidate stopped")):
            maintenance.probe(command, inspect, "candidate")
        command.assert_not_called()

    def test_health_probe_requires_expected_application_response(self):
        command = Mock(return_value='{"status":"ok", "canConnect":false}')
        inspect = Mock(return_value=[{"State": {"Running": True, "Health": {"Status": "healthy"}}}])
        with self.assertRaisesRegex(maintenance.MaintenanceError, "health failed"):
            maintenance.probe(command, inspect, "candidate")
        command.return_value = '{"status":"ok", "canConnect":true}'
        maintenance.probe(command, inspect, "candidate")
        self.assertNotIn("check", command.call_args.kwargs)

    def test_changed_sha_and_duplicate_review_keys_fail(self):
        def review(target):
            return json.dumps({"before": "before", "target": target, "add_nullable_columns": COLUMNS})

        self.assertEqual(maintenance.review_plan(review("target"), "before", "target"), COLUMNS)
        with self.assertRaisesRegex(maintenance.MaintenanceError, "SHAs"):
            maintenance.review_plan(review("other"), "before", "target")
        with self.assertRaisesRegex(maintenance.MaintenanceError, "Duplicate"):
            maintenance.review_plan('{"target": "old", "target": "new"}', "before", "new")

    def test_only_complete_regular_append_only_migrations_pass(self):
        migration = f"{maintenance.MIGRATIONS}/{AFTER[-1]}"

        def record(status, path, new="100644", old="000000"):
            return f":{old} {new} {'0' * 40} {'a' * 40} {status}\0{path}\0"

        def git(*args):
            if args[0] == "diff":
                return diff
            if args[0] == "grep":
                ids = BEFORE if args[-3] == "before" else AFTER
                return "".join(f'    [Migration("{value}")]\n' for value in ids)
            self.assertEqual(args, ("merge-base", "--is-ancestor", "before", "target"))
            return ""

        valid = record("A", migration + ".cs") + record("A", migration + ".Designer.cs")
        diff = valid
        self.assertEqual(maintenance.classify(git, "before", "target")[1:], (BEFORE, AFTER))
        diff = valid + record("M", "FrontEnd/app.txt", old="100644")
        self.assertEqual(maintenance.classify(git, "before", "target", lambda path: path.startswith("FrontEnd/"))[0][-1],
                         "FrontEnd/app.txt")
        for diff in (record("A", migration + ".cs"), valid + record("M", "Dockerfile", old="100644"),
                     valid + record("A", migration + "Link.cs", new="120000"),
                     valid + record("D", "DataAccessLayer/Migrations/old.cs", new="000000", old="100644")):
            with self.subTest(diff=diff), self.assertRaises(maintenance.MaintenanceError):
                maintenance.classify(git, "before", "target")

    def test_final_backup_and_once_only_recovery_preserve_rows(self):
        bundle = self.root / "bundle"
        bundle.mkdir()
        journal = maintenance.Journal(self.root, bundle, lambda path, body: path.write_text(json.dumps(body)))
        journal.begin({"before": BEFORE[0], "target": AFTER[-1]})
        journal.advance("FENCING")
        journal.advance("FENCED")
        quiet = Mock()
        backup = maintenance.final_backup(self.live, bundle, BEFORE, quiet,
                                          lambda metadata: journal.advance("BACKED_UP", backup=metadata))
        journal.advance("MIGRATING")
        with closing(sqlite3.connect(self.live)) as db, db:
            self.apply(db)
        maintenance.verify_no_data_drift(bundle / "final.db", self.live, COLUMNS, AFTER)
        maintenance.restore_database(self.live, bundle, backup, quiet, lambda: journal.advance("RESTORING"))
        with closing(sqlite3.connect(self.live)) as db:
            maintenance.verify(db, BEFORE)
            self.assertEqual(db.execute("SELECT name FROM employee").fetchone(), ("Fixture Employee",))
        with closing(sqlite3.connect(bundle / "failed-database" / "live.db")) as db:
            maintenance.verify(db, AFTER)
        journal.advance("RECOVERY_READY")
        journal.advance("WRITES_MAY_RESUME")
        restarted = maintenance.Journal(self.root, bundle, journal.store)
        self.assertTrue(restarted.read()["writes_may_resume"])
        with self.assertRaises(maintenance.MaintenanceError):
            restarted.advance("RESTORING")
        restarted.fail("interrupted")
        self.assertTrue(restarted.read()["writes_may_resume"])
        with self.assertRaises(maintenance.MaintenanceError):
            restarted.begin({})

    def test_failed_or_tampered_backup_and_new_writes_fail_closed(self):
        bundle = self.root / "backup"
        bundle.mkdir()
        with patch.object(maintenance, "snapshot", side_effect=OSError("no space")):
            record = Mock()
            with self.assertRaises(OSError):
                maintenance.final_backup(self.live, bundle, BEFORE, Mock(), record)
            record.assert_not_called()
        metadata = maintenance.final_backup(self.live, bundle, BEFORE, Mock(), Mock())
        with closing(sqlite3.connect(self.live)) as db, db:
            db.execute("INSERT INTO employee VALUES (2, 'Later write', NULL)")
        with self.assertRaisesRegex(maintenance.MaintenanceError, "writes may have occurred"):
            maintenance.verify_no_data_drift(bundle / "final.db", self.live)
        with closing(sqlite3.connect(self.live)) as db, db:
            db.execute("DELETE FROM employee WHERE id = 2")
            self.apply(db)
            db.execute("UPDATE employee SET comment = 'new data'")
        with self.assertRaisesRegex(maintenance.MaintenanceError, "data in a new column"):
            maintenance.verify_no_data_drift(bundle / "final.db", self.live, COLUMNS, AFTER)
        (bundle / "final.db").write_bytes(b"damaged")
        with self.assertRaisesRegex(maintenance.MaintenanceError, "checksum"):
            maintenance.verify_final_backup(bundle, metadata)

    def test_final_backup_accepts_empty_wal_sidecars_but_detects_committed_writes(self):
        bundle = self.root / "backup"
        bundle.mkdir()
        with closing(sqlite3.connect(self.live)) as writer:
            writer.execute("PRAGMA journal_mode=WAL")
            writer.execute("INSERT INTO employee VALUES (2, 'Committed before the fence', NULL)")
            writer.commit()
        backup = maintenance.final_backup(self.live, bundle, BEFORE, Mock(), Mock())
        self.assertEqual(maintenance.database_stamp(self.live), backup["source"])
        with closing(sqlite3.connect(self.live)) as writer:
            writer.execute("INSERT INTO employee VALUES (3, 'Unexpected later write', NULL)")
            writer.commit()
            self.assertNotEqual(maintenance.database_stamp(self.live), backup["source"])
        with closing(maintenance.open_db(bundle / "final.db", "ro")) as snapshot:
            self.assertEqual(snapshot.execute("SELECT id FROM employee ORDER BY id").fetchall(), [(1,), (2,)])

    def test_process_death_keeps_the_persisted_reopen_boundary(self):
        source = """
import sys, time
from pathlib import Path
sys.path.insert(0, sys.argv[1])
import gf3_maintenance as maintenance
import gf3_release as release
state = Path(sys.argv[2])
journal = maintenance.Journal(state, state / "bundle", release.store)
journal.begin({})
for phase in ("FENCING", "FENCED", "BACKED_UP", "MIGRATING", "VERIFYING",
              "ACTIVATING_GIT", "READY_TO_REOPEN", "WRITES_MAY_RESUME"):
    journal.advance(phase)
print("ready", flush=True)
time.sleep(10)
"""
        with subprocess.Popen([sys.executable, "-c", source, str(Path(maintenance.__file__).parent), str(self.root)],
                              stdout=subprocess.PIPE, text=True) as process:
            self.assertEqual(process.stdout.readline().strip(), "ready")
            process.kill()
            process.wait(timeout=5)
        with patch.object(release, "STATE", self.root), self.assertRaisesRegex(release.ReleaseError, "unfinished"):
            release.no_active_maintenance()
        journal = maintenance.Journal(self.root, self.root / "bundle", release.store)
        self.assertTrue(journal.read()["writes_may_resume"])
        with self.assertRaises(maintenance.MaintenanceError):
            journal.advance("RESTORING")

    def test_release_engine_checks_exact_authorization_and_consumes_it_once(self):
        target = "a" * 40
        before = "b" * 40
        nonce = "c" * 64
        image = "sha256:" + "d" * 64
        bundle = self.root / f"maintenance-{target[:12]}-fixture"
        bundle.mkdir(mode=0o700)
        capability = bundle / "capability.json"
        consumed = bundle / "capability-consumed.json"
        reviewed = (before, None, None, (), b"env", b"review", (), (), b"compose", (34, 2))
        permit = {"nonce": nonce, "target": target, "before": before, "review": "review-digest", "image": image,
                  "migrations": "migration-digest", "expires": 1010}
        audit = {"stage": "passed", "target": target, "before": before, "image": image, "ci": [34, 2]}
        release.store(bundle / "rehearsal.json", audit)

        def load_helper(_, namespace):
            namespace.update(maintenance_state=lambda *_args, **_kwargs: reviewed,
                             review_digest=lambda _: "review-digest",
                             maintenance_changes=lambda *_: ([], BEFORE, AFTER, COLUMNS),
                             candidate=lambda *_: nullcontext(self.root))

        with (patch.object(release, "STATE", self.root),
              patch.object(release, "protected_json", side_effect=lambda path: json.loads(path.read_bytes())),
              patch.object(release, "safe_path"),
              patch.object(release.time, "time", return_value=1000),
              patch.object(release, "git", side_effect=lambda op, *_: (
                  "https://github.com/OlehProtsun/GF3\n" if op == "remote" else target + "\n")) as git,
              patch.object(release, "maintenance_tool", return_value=maintenance),
              patch.object(release, "exec", side_effect=load_helper, create=True),
              patch.object(release, "migration_fingerprint", side_effect=lambda _, sha: (
                  "migration-digest" if sha == target else "prior-digest")),
              patch.object(release, "docker_json", return_value=[{
                  "Id": image, "Config": {"Labels": {"org.opencontainers.image.revision": target}},
              }]),
              patch.object(release, "preflight", side_effect=release.ReleaseError("staging stopped for test"))
              as preflight):
            with self.assertRaises(FileNotFoundError):
                release.maintenance_deploy(bundle, target, nonce)
            for stale in ({"nonce": "e" * 64}, {"target": before}, {"expires": 999}):
                with self.subTest(stale=tuple(stale)):
                    release.store(capability, {**permit, **stale})
                    with self.assertRaisesRegex(release.ReleaseError, "authorization does not match"):
                        release.maintenance_deploy(bundle, target, nonce)
            git.assert_not_called()
            release.store(capability, permit)
            release.store(bundle / "rehearsal.json", {**audit, "ci": [34, 1]})
            with self.assertRaisesRegex(release.ReleaseError, "state changed after maintenance approval"):
                release.maintenance_deploy(bundle, target, nonce)
            self.assertFalse(consumed.exists())
            preflight.assert_not_called()
            release.store(bundle / "rehearsal.json", audit)
            with self.assertRaisesRegex(release.ReleaseError, "staging stopped for test"):
                release.maintenance_deploy(bundle, target, nonce)
            self.assertEqual(json.loads(consumed.read_bytes()), permit)
            self.assertFalse(capability.exists())
            release.store(capability, permit)
            with self.assertRaisesRegex(release.ReleaseError, "already consumed"):
                release.maintenance_deploy(bundle, target, nonce)
            preflight.assert_called_once()


if __name__ == "__main__":
    unittest.main()
