"""SQLite migration rehearsal and maintenance recovery primitives."""

from contextlib import closing
import hashlib
import json
import os
from pathlib import Path
import re
import secrets
import shutil
import sqlite3
import stat
import tempfile
import time


MIGRATIONS = "DataAccessLayer/Migrations"
PAIR = re.compile(r"([0-9]{14}_[A-Za-z][A-Za-z0-9_]*)(\.Designer)?\.cs")
RELATED = {
    f"{MIGRATIONS}/AppDbContextModelSnapshot.cs",
    "DataAccessLayer/Models/DataBaseContext/AppDbContext.cs",
    "DataAccessLayer/Models/ShiftCorrectionRequestModel.cs",
    "DataAccessLayer/Models/ShiftSwapRequestModel.cs",
    "GF3.Tests/DatabaseMigrationStartupTests.cs",
}
BOOTSTRAP_PATHS = {
    "GF3.WebApi/Program.cs",
    "deploy/security/gf3_prepare_release.py",
    "deploy/security/gf3_release.py",
    "deploy/security/gf3_maintenance.py",
    "deploy/security/gfctl-prod2-maintenance-menu.patch",
}
IDENTIFIER = re.compile(r"[A-Za-z_][A-Za-z0-9_]*")
HISTORY = "__EFMigrationsHistory"
LOCK = "__EFMigrationsLock"


class MaintenanceError(Exception):
    pass


def require(condition, message):
    if not condition:
        raise MaintenanceError(message)


def migration_ids(git, sha):
    # EF has older migrations without a Designer file. Read their attributes as well.
    output = git("grep", "-h", "-E", r'^[[:space:]]*\[Migration\("[^"]+"\)\]', sha, "--", MIGRATIONS)
    ids = re.findall(r'^\s*\[Migration\("([0-9]{14}_[A-Za-z][A-Za-z0-9_]*)"\)\]\s*$', output, re.M)
    require(len(ids) == len(output.splitlines()) and len(ids) == len(set(ids)) and ids,
            "Invalid or duplicate EF migration attributes in Git")
    return tuple(sorted(ids))


def classify(git, before, target, ordinary=None):
    git("merge-base", "--is-ancestor", before, target)
    diff = git("diff", "--raw", "--no-abbrev", "--no-renames", "--no-ext-diff", "-z",
               before, target, "--").split("\0")
    paths = []
    pairs = {}
    for header, path in zip(diff[:-1:2], diff[1::2], strict=True):
        old, new, _, _, status = header.removeprefix(":").split()
        if path in RELATED:
            require((status, old, new) == ("M", "100644", "100644"), "Invalid related file change")
        else:
            name = path.removeprefix(MIGRATIONS + "/")
            match = PAIR.fullmatch(name) if name != path else None
            if match:
                require((status, old, new) == ("A", "000000", "100644"),
                        f"Migration rehearsal refuses path {ascii(path)}")
                pairs.setdefault(match[1], set()).add(bool(match[2]))
            else:
                require(name == path and ordinary and ordinary(path)
                        and (status, old, new) in {("A", "000000", "100644"), ("M", "100644", "100644"),
                                                   ("D", "100644", "000000")},
                        f"Migration rehearsal refuses path {ascii(path)}")
        paths.append(path)
    before_ids, target_ids = migration_ids(git, before), migration_ids(git, target)
    require(pairs and all(parts == {False, True} for parts in pairs.values())
            and target_ids == before_ids + tuple(sorted(pairs))
            and all(migration > before_ids[-1] for migration in pairs),
            "Expected complete, append-only EF migration pairs")
    return paths, before_ids, target_ids


def review_plan(raw, before, target):
    def unique_keys(pairs):
        result = dict(pairs)
        require(len(pairs) == len(result), "Duplicate maintenance review key")
        return result

    plan = json.loads(raw, object_pairs_hook=unique_keys)
    require(isinstance(plan, dict) and set(plan) == {"before", "target", "add_nullable_columns"}
            and plan["before"] == before and plan["target"] == target,
            "Maintenance review does not match the running and target SHAs")
    columns = plan["add_nullable_columns"]
    require(isinstance(columns, list) and 0 < len(columns) <= 32, "Review must list the exact nullable columns")
    reviewed = []
    keys = set()
    for entry in columns:
        require(isinstance(entry, list) and len(entry) == 3 and all(isinstance(value, str) for value in entry),
                "Invalid maintenance review column")
        table, column, kind = entry
        key = table.lower(), column.lower()
        require(IDENTIFIER.fullmatch(table) and IDENTIFIER.fullmatch(column)
                and table.lower() not in (HISTORY.lower(), LOCK.lower()) and not table.lower().startswith("sqlite_")
                and kind in ("TEXT", "INTEGER") and key not in keys, "Invalid or duplicate reviewed column")
        keys.add(key)
        reviewed.append(tuple(entry))
    return tuple(reviewed)


def bootstrap_paths(git, before, target, raw):
    if not raw:
        return set()
    record = json.loads(raw)
    if record.get("target") != target:
        return set()
    require(set(record) == {"before", "target", "blobs"} and record["before"] == before
            and isinstance(record["blobs"], dict), "Tool installation review does not match this transition")
    blobs = record["blobs"]
    require(set(blobs) <= BOOTSTRAP_PATHS, "Tool installation review contains an unsupported path")
    for path, blob in blobs.items():
        require(re.fullmatch(r"[0-9a-f]{40}", blob) and git("rev-parse", f"{target}:{path}").strip() == blob,
                "Reviewed installation blob differs from the candidate")
    return set(blobs)


ADD_COLUMN = re.compile(
    r'migrationBuilder\.AddColumn<(string|long)>\s*\(\s*'
    r'name:\s*"([A-Za-z_][A-Za-z0-9_]*)",\s*table:\s*"([A-Za-z_][A-Za-z0-9_]*)",\s*'
    r'type:\s*"(TEXT|INTEGER)",\s*(?:maxLength:\s*[1-9][0-9]*,\s*)?nullable:\s*true\s*\);'
)


def migration_plan(git, before, target, pending):
    columns = []
    for migration in pending:
        source = git("show", f"{target}:{MIGRATIONS}/{migration}.cs")
        up = re.search(r"protected override void Up\(MigrationBuilder migrationBuilder\)\s*\{([^{}]*)\}", source)
        require(up is not None, "Maintenance supports generated nullable column migrations")
        operations = ADD_COLUMN.findall(up[1])
        require(operations and not ADD_COLUMN.sub("", up[1]).strip(),
                "Maintenance migration contains an unsupported operation")
        for clr, column, table, affinity in operations:
            require((clr, affinity) in {("string", "TEXT"), ("long", "INTEGER")},
                    "Unexpected SQLite affinity in maintenance migration")
            columns.append((table, column, affinity))
    return review_plan(json.dumps({"before": before, "target": target, "add_nullable_columns": columns}),
                       before, target)


def open_db(path, mode, timeout=5):
    info = path.lstat()
    require(stat.S_ISREG(info.st_mode) and info.st_size > 0 and info.st_nlink == 1,
            "Expected an existing regular SQLite database")
    return sqlite3.connect(path.as_uri() + f"?mode={mode}", uri=True, timeout=timeout)


def verify(db, expected_ids):
    deadline = time.monotonic() + 120
    db.set_progress_handler(lambda: int(time.monotonic() >= deadline), 10000)
    try:
        require(db.execute("PRAGMA integrity_check").fetchall() == [("ok",)], "SQLite integrity check failed")
    finally:
        db.set_progress_handler(None, 0)
    history = tuple(db.execute(f'SELECT MigrationId, ProductVersion FROM "{HISTORY}" ORDER BY MigrationId'))
    require(tuple(row[0] for row in history) == expected_ids and all(row[1] for row in history),
            "SQLite migration history differs from the approved revision")
    if db.execute("SELECT 1 FROM sqlite_schema WHERE type='table' AND name=?", (LOCK,)).fetchone():
        require(not db.execute(f'SELECT 1 FROM "{LOCK}" LIMIT 1').fetchone(), "SQLite EF migration lock is held")
    return history


def snapshot(source, destination, expected_ids, *, quiesced=False):
    # The destination is private and new. Successful callers may retain it as a recovery artifact.
    descriptor = os.open(destination, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    os.close(descriptor)
    try:
        deadline = time.monotonic() + 120

        def progress(*_):
            require(time.monotonic() < deadline, "SQLite snapshot timed out")

        with closing(open_db(source, "rw" if quiesced else "ro", timeout=0 if quiesced else 5)) as reader:
            if quiesced:
                # Hold a reserved writer lock through the copy using a separate read connection.
                reader.execute("BEGIN IMMEDIATE")
                with closing(open_db(source, "ro")) as copy_reader, closing(sqlite3.connect(destination)) as copy:
                    copy_reader.backup(copy, pages=256, progress=progress)
                    verify(copy, expected_ids)
            else:
                with closing(sqlite3.connect(destination)) as copy:
                    reader.backup(copy, pages=256, progress=progress)
                    verify(copy, expected_ids)
        with destination.open("r+b") as backup:
            os.fsync(backup.fileno())
        if os.name != "nt":
            descriptor = os.open(destination.parent, os.O_RDONLY | os.O_DIRECTORY)
            try:
                os.fsync(descriptor)
            finally:
                os.close(descriptor)
    except BaseException:
        destination.unlink(missing_ok=True)
        raise


def digest_file(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def sqlite_files(path):
    return (path, *(path.with_name(path.name + suffix) for suffix in ("-wal", "-shm", "-journal")))


def database_stamp(path):
    result = {}
    for entry in sqlite_files(path):
        try:
            info = entry.lstat()
        except FileNotFoundError:
            require(entry != path, "SQLite database is missing")
            continue
        require(stat.S_ISREG(info.st_mode) and info.st_nlink == 1, "Unexpected SQLite file type")
        if entry != path and (entry.name.endswith("-shm") or info.st_size == 0):
            # Readers of a checkpointed WAL database can create empty journal files.
            # Their presence and the shared-memory index do not represent data changes.
            continue
        result[entry.name] = [info.st_dev, info.st_ino, info.st_size, digest_file(entry)]
    return result


def checkpoint_for_read_only_health(path):
    # The health process uses an immutable SQLite URI on a read-only mount. It must see
    # a checkpointed main file, including the final committed WAL pages.
    with closing(open_db(path, "rw", timeout=0)) as database:
        if database.execute("PRAGMA journal_mode").fetchone() == ("wal",):
            require(database.execute("PRAGMA wal_checkpoint(TRUNCATE)").fetchone() == (0, 0, 0),
                    "SQLite WAL could not be fully checkpointed for health verification")
    wal = sqlite_files(path)[1]
    require(not wal.exists() or wal.stat().st_size == 0, "SQLite contains uncheckpointed WAL changes")


def final_backup(database, bundle, history, assert_quiet, record):
    assert_quiet()
    backup = bundle / "final.db"
    snapshot(database, backup, history, quiesced=True)
    assert_quiet()
    metadata = {"sha256": digest_file(backup), "source": database_stamp(database), "history": list(history)}
    # Prove the checkpoint includes every committed row at the same recovery point.
    verify_migration(backup, database, history, history, ())
    assert_quiet()
    require(database_stamp(database) == metadata["source"], "Database changed during final backup")
    record(metadata)
    return metadata


def verify_final_backup(bundle, metadata):
    backup = bundle / "final.db"
    info = backup.lstat()
    require(stat.S_ISREG(info.st_mode) and info.st_nlink == 1
            and (os.name == "nt" or (info.st_uid == os.geteuid() and stat.S_IMODE(info.st_mode) == 0o600)),
            "Final backup permissions are unsafe")
    require(digest_file(backup) == metadata["sha256"], "Final backup checksum mismatch")
    with closing(open_db(backup, "ro")) as database:
        verify(database, tuple(metadata["history"]))
    return backup


def restore_database(database, bundle, metadata, assert_quiet, consume_restore, expected_stamp=None):
    """Replace a fenced database once and preserve the failed database and its journals."""
    backup = verify_final_backup(bundle, metadata)
    assert_quiet()
    files = database_stamp(database)
    require(expected_stamp is None or expected_stamp == files, "Database changed after recovery verification")
    saved_main = metadata["source"][database.name]
    require(files[database.name][:2] == saved_main[:2], "Recovery database was replaced outside maintenance")
    failed = bundle / "failed-database"
    failed.mkdir(mode=0o700)
    descriptor, pending = tempfile.mkstemp(dir=database.parent, prefix=".gf3-restore-")
    os.close(descriptor)
    replacement = Path(pending)
    try:
        info = database.lstat()
        shutil.copyfile(backup, replacement)
        require(digest_file(replacement) == metadata["sha256"], "Recovery copy checksum mismatch")
        if os.name != "nt":
            os.chown(replacement, info.st_uid, info.st_gid)
        replacement.chmod(stat.S_IMODE(info.st_mode))
        with replacement.open("r+b") as stream:
            os.fsync(stream.fileno())
        assert_quiet()
        require(database_stamp(database) == files, "Database changed immediately before recovery")
        consume_restore()
        # Keep a byte-for-byte forensic copy, including any uncheckpointed data. Copying
        # across filesystems is safe here because the writer fence has been verified.
        for entry in sqlite_files(database):
            if entry.exists():
                shutil.copyfile(entry, failed / entry.name)
                (failed / entry.name).chmod(0o600)
                with (failed / entry.name).open("r+b") as stream:
                    os.fsync(stream.fileno())
        sync_dir(failed)
        sync_dir(bundle)
        assert_quiet()
        require(database_stamp(database) == files, "Database changed while preserving recovery evidence")
        for entry in sqlite_files(database)[1:]:
            entry.unlink(missing_ok=True)
        os.replace(replacement, database)
        sync_dir(database.parent)
        with closing(open_db(database, "ro")) as restored:
            verify(restored, tuple(metadata["history"]))
        verify_migration(backup, database, tuple(metadata["history"]), tuple(metadata["history"]), ())
    finally:
        replacement.unlink(missing_ok=True)


def sync_dir(directory):
    if os.name != "nt":
        descriptor = os.open(directory, os.O_RDONLY | os.O_DIRECTORY)
        try:
            os.fsync(descriptor)
        finally:
            os.close(descriptor)


TRANSITIONS = {
    "REHEARSED": {"FENCING"},
    "FENCING": {"FENCED"},
    "FENCED": {"BACKED_UP"},
    "BACKED_UP": {"MIGRATING"},
    "MIGRATING": {"VERIFYING", "RESTORING"},
    "VERIFYING": {"RESTORING", "ACTIVATING_GIT"},
    "ACTIVATING_GIT": {"READY_TO_REOPEN", "RESTORING"},
    "READY_TO_REOPEN": {"WRITES_MAY_RESUME"},
    "RESTORING": {"RECOVERY_READY"},
    "RECOVERY_READY": {"WRITES_MAY_RESUME"},
    "WRITES_MAY_RESUME": {"COMPLETED", "RECOVERED"},
}


class Journal:
    """Root-owned durable journal. The active record blocks every managed mutator."""

    def __init__(self, state, bundle, store):
        self.bundle = bundle
        self.path = state / "maintenance-active.json"
        self.store = store

    def begin(self, identity):
        require(not os.path.lexists(self.path), "An unfinished maintenance transaction already exists")
        self.write({**identity, "bundle": self.bundle.name, "phase": "REHEARSED", "writes_may_resume": False})

    def read(self):
        state = json.loads(self.path.read_bytes())
        require(state["bundle"] == self.bundle.name, "Maintenance journal belongs to a different transaction")
        return state

    def write(self, state):
        self.store(self.path, state)
        sync_dir(self.path.parent)

    def advance(self, phase, **fields):
        state = self.read()
        prior = state["phase"]
        require(phase in TRANSITIONS.get(prior, ()), f"Invalid maintenance transition {prior} -> {phase}")
        require(phase != "RESTORING" or state["writes_may_resume"] is False,
                "Automatic database recovery is unsafe after writers may resume")
        state.update(fields, phase=phase,
                     writes_may_resume=state["writes_may_resume"] or phase == "WRITES_MAY_RESUME")
        self.write(state)

    def require_phase(self, phase):
        require(self.read()["phase"] == phase, f"Expected maintenance phase {phase}")

    def fail(self, stage):
        state = self.read()
        self.write({**state, "phase": "RECOVERY_REQUIRED", "failed_in": state["phase"], "failure": stage})

    def finish(self):
        state = self.read()
        require(state["phase"] in {"COMPLETED", "RECOVERED"}, "Do not release an incomplete maintenance transaction")
        self.store(self.bundle / "transaction.json", state)
        sync_dir(self.bundle)
        self.path.unlink()
        sync_dir(self.path.parent)


def quoted(value):
    return '"' + value.replace('"', '""') + '"'


def schema(db):
    return {(kind, name): sql for kind, name, sql in
            db.execute("SELECT type, name, sql FROM sqlite_schema WHERE sql IS NOT NULL")}


def expected_schema(before, columns):
    expected = before.copy()
    with closing(sqlite3.connect(":memory:")) as scratch:
        for table in sorted({entry[0] for entry in columns}):
            require(("table", table) in before, "Reviewed table is missing")
            scratch.execute(before["table", table])
        for table, name, affinity in columns:
            scratch.execute(f"ALTER TABLE {quoted(table)} ADD COLUMN {quoted(name)} {affinity} NULL")
        expected.update(schema(scratch))
    return expected


def row_digest(db, table, fields):
    # Include order by storage class and byte representation, so collation and physical layout
    # do not affect the comparison. Keep employee data out of reports.
    names = list(map(quoted, fields))
    order = ", ".join(f"typeof({name}), {name} COLLATE BINARY" for name in names)
    digest = hashlib.sha256()
    deadline = time.monotonic() + 120
    db.set_progress_handler(lambda: int(time.monotonic() >= deadline), 10000)
    try:
        for row in db.execute(f"SELECT {', '.join(names)} FROM {quoted(table)} ORDER BY {order}"):
            digest.update(repr(row).encode("utf-8"))
            digest.update(b"\n")
    finally:
        db.set_progress_handler(None, 0)
    return digest.digest()


def verify_migration(snapshot_path, clone, before_ids, target_ids, columns):
    with closing(open_db(snapshot_path, "ro")) as before, closing(open_db(clone, "ro")) as after:
        # Keep sorter output containing employee data out of host temporary files.
        before.execute("PRAGMA temp_store=MEMORY")
        after.execute("PRAGMA temp_store=MEMORY")
        prior_history = verify(before, before_ids)
        history = verify(after, target_ids)
        require(history[:len(prior_history)] == prior_history, "Existing migration history changed")
        before_schema, after_schema = schema(before), schema(after)
        # EF can initialize its own empty migration lock table on first use.
        if ("table", LOCK) not in before_schema:
            after_schema.pop(("table", LOCK), None)
        require(expected_schema(before_schema, columns) == after_schema,
                "SQLite schema differs from the reviewed column additions")
        for kind, table in before_schema:
            if kind != "table" or table == HISTORY:
                continue
            fields = before.execute(f"PRAGMA table_xinfo({quoted(table)})").fetchall()
            actual = after.execute(f"PRAGMA table_xinfo({quoted(table)})").fetchall()
            additions = [(len(fields) + i, name, affinity, 0, None, 0, 0)
                         for i, (_, name, affinity) in enumerate(item for item in columns if item[0] == table)]
            require(actual == fields + additions, "SQLite column metadata differs from review")
            for _, name, *_ in additions:
                require(not after.execute(f"SELECT 1 FROM {quoted(table)} WHERE {quoted(name)} IS NOT NULL LIMIT 1")
                        .fetchone(), "A new nullable column contains unexpected values")
            names = [field[1] for field in fields]
            require(row_digest(before, table, names) == row_digest(after, table, names),
                    "Existing SQLite rows changed during rehearsal")


def verify_no_data_drift(backup, database, columns=(), target_ids=None):
    """Refuse automatic restore if rows might have changed outside the migration."""
    with closing(open_db(backup, "ro")) as before, closing(open_db(database, "ro")) as after:
        before.execute("PRAGMA temp_store=MEMORY")
        after.execute("PRAGMA temp_store=MEMORY")
        before_schema, after_schema = schema(before), schema(after)
        if ("table", LOCK) not in before_schema:
            after_schema.pop(("table", LOCK), None)
        query = f'SELECT MigrationId, ProductVersion FROM "{HISTORY}" ORDER BY MigrationId'
        original_history, current_history = tuple(before.execute(query)), tuple(after.execute(query))
        expected_ids = tuple(row[0] for row in original_history) if target_ids is None else target_ids
        require(len(current_history) >= len(original_history) and current_history[:len(original_history)] == original_history
                and tuple(row[0] for row in current_history) == expected_ids[:len(current_history)],
                "SQLite history changed outside the reviewed migrations")
        present = []
        for kind, table in before_schema:
            if kind == "table" and table not in {HISTORY, LOCK}:
                names = [row[1] for row in before.execute(f"PRAGMA table_xinfo({quoted(table)})")]
                updated = [row[1] for row in after.execute(f"PRAGMA table_xinfo({quoted(table)})")]
                for item in columns:
                    if item[0] == table and item[1] not in names and item[1] in updated:
                        present.append(item)
                        require(not after.execute(f"SELECT 1 FROM {quoted(table)} "
                                                  f"WHERE {quoted(item[1])} IS NOT NULL LIMIT 1").fetchone(),
                                "There is data in a new column. Automatic recovery would discard it.")
                require(row_digest(before, table, names) == row_digest(after, table, names),
                        "Database writes may have occurred since the final backup")
        require(expected_schema(before_schema, present) == after_schema,
                "Database schema changed outside the reviewed migration")


def image_id(command, source, bundle, target):
    path = bundle / "app.iid"
    command("docker", "build", "--iidfile", str(path), "--label", f"org.opencontainers.image.revision={target}",
            str(source), timeout=1800)
    image = path.read_text(encoding="ascii").strip()
    require(re.fullmatch(r"sha256:[0-9a-f]{64}", image), "Expected an immutable candidate Docker image ID")
    return image


def probe(command, inspect, name):
    deadline = time.monotonic() + 120
    while time.monotonic() < deadline:
        state = inspect("container", "inspect", name)[0]["State"]
        require(state["Running"], "Candidate stopped before rehearsal health passed")
        if state["Health"]["Status"] == "healthy":
            response = command("docker", "exec", name, "curl", "-fsS", "--max-time", "3",
                               "http://127.0.0.1:8080/api/health", timeout=10)
            health = json.loads(response)
            require(isinstance(health, dict) and health.get("status") == "ok" and health.get("canConnect") is True,
                    "Candidate rehearsal health failed")
            return
        time.sleep(1)
    raise MaintenanceError("Candidate rehearsal health timed out")


def start_clone(command, inspect, name, image, data, *, require_existing=True, mode=None):
    uid, gid = os.getuid() or 1654, os.getgid() or 1654
    if os.geteuid() == 0:
        os.chown(data, uid, gid)
        if (data / "SQLite.db").exists():
            os.chown(data / "SQLite.db", uid, gid)
    environment = {
        "ASPNETCORE_ENVIRONMENT": "Production", "DOTNET_ENVIRONMENT": "Production",
        "ConnectionStrings__Default": "Data Source=/data/SQLite.db",
        "GF3_TRUSTED_PROXY_CIDR": "172.31.254.0/24", "AllowedHosts": "localhost;127.0.0.1",
        "DatabaseStartup__RequireExistingDatabase": str(require_existing).lower(),
        "DatabaseStartup__BackupBeforeMigrate": "true", "Push__Enabled": "false",
        "Jwt__SigningKey": secrets.token_hex(48),
    }
    arguments = [f"--gf3-maintenance-{mode}"] if mode else []
    readonly = ",readonly" if mode == "health" else ""
    command("docker", "create", "--name", name, "--pull", "never", "--network", "none",
            "--cap-drop", "ALL", "--security-opt", "no-new-privileges", "--log-driver", "none",
            "--read-only", "--tmpfs", "/tmp:rw,noexec,nosuid,nodev", "--user", f"{uid}:{gid}",
            "--health-interval", "1s", "--health-timeout", "4s",
            "--mount", f"type=bind,source={data},destination=/data{readonly}",
            *(arg for key, value in environment.items() for arg in ("--env", f"{key}={value}")), image, *arguments)
    config, = inspect("container", "inspect", name)
    host = config["HostConfig"]
    mounts = config["Mounts"]
    require(config["Image"] == image and host["NetworkMode"] == "none" and not host["PortBindings"]
            and not host["Privileged"] and host["ReadonlyRootfs"] and set(host["CapDrop"]) == {"ALL"}
            and len(mounts) == 1 and mounts[0]["Source"] == str(data) and mounts[0]["Destination"] == "/data"
            and mounts[0]["Type"] == "bind" and mounts[0]["RW"] == (mode != "health"),
            "Candidate container is not isolated from production")
    command("docker", "start", name)
    if mode == "migrate":
        require(command("docker", "wait", name, timeout=120).strip() == "0", "Isolated EF migration failed")
    else:
        probe(command, inspect, name)


def remove_clone(command, name):
    ids = command("docker", "ps", "-aq", "--filter", f"name=^/{name}$").splitlines()
    require(len(ids) <= 1 and all(re.fullmatch(r"[0-9a-f]{12,64}", item) for item in ids),
            "Could not identify the disposable rehearsal container")
    if ids:
        command("docker", "rm", "-f", ids[0])


def rehearse(command, inspect, image, bundle, snapshot_path, before_ids, target_ids, columns, *, verify_modes=False):
    data = Path(tempfile.mkdtemp(prefix="clone-", dir=bundle))
    clone = data / "SQLite.db"
    name = "gf3-db-rehearsal-" + secrets.token_hex(12)
    try:
        with closing(open_db(snapshot_path, "ro")) as snapshot_db:
            verify(snapshot_db, before_ids)
        shutil.copyfile(snapshot_path, clone)
        clone.chmod(0o600)
        if verify_modes:
            start_clone(command, inspect, name, image, data, mode="migrate")
            remove_clone(command, name)
            checkpoint_for_read_only_health(clone)
            start_clone(command, inspect, name, image, data, mode="health")
            # A legacy image that ignores the mode must not qualify for the live path.
            code = command("docker", "exec", name, "curl", "-sS", "-o", "/dev/null", "-w", "%{http_code}",
                           "--max-time", "5", "http://127.0.0.1:8080/")
            require(code == "404", "Maintenance verification host serves ordinary application routes")
        else:
            start_clone(command, inspect, name, image, data)
        command("docker", "stop", "--time", "15", name, timeout=25)
        verify_migration(snapshot_path, clone, before_ids, target_ids, columns)
    finally:
        # If Docker cannot prove removal, retain its private bind source for diagnosis.
        remove_clone(command, name)
        shutil.rmtree(data)
