from contextlib import closing
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
        for diff in (record("A", migration + ".cs"), valid + record("M", "Dockerfile", old="100644"),
                     valid + record("A", migration + "Link.cs", new="120000"),
                     valid + record("D", "DataAccessLayer/Migrations/old.cs", new="000000", old="100644")):
            with self.subTest(diff=diff), self.assertRaises(maintenance.MaintenanceError):
                maintenance.classify(git, "before", "target")


if __name__ == "__main__":
    unittest.main()
