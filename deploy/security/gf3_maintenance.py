"""SQLite migration rehearsal. All candidate writes are confined to disposable copies."""

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


def classify(git, before, target):
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
            require(match and (status, old, new) == ("A", "000000", "100644"),
                    f"Migration rehearsal refuses path {ascii(path)}")
            pairs.setdefault(match[1], set()).add(bool(match[2]))
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
        response = command("docker", "exec", name, "curl", "-fsS", "--max-time", "3",
                           "http://127.0.0.1:8080/api/health", timeout=10, check=False)
        if response:
            health = json.loads(response)
            require(health.get("status") == "ok" and health.get("canConnect") is True,
                    "Candidate rehearsal health failed")
            return
        time.sleep(1)
    raise MaintenanceError("Candidate rehearsal health timed out")


def start_clone(command, inspect, name, image, data, *, require_existing=True):
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
    command("docker", "create", "--name", name, "--pull", "never", "--network", "none",
            "--cap-drop", "ALL", "--security-opt", "no-new-privileges", "--log-driver", "none",
            "--read-only", "--tmpfs", "/tmp:rw,noexec,nosuid,nodev", "--user", f"{uid}:{gid}",
            "--mount", f"type=bind,source={data},destination=/data",
            *(arg for key, value in environment.items() for arg in ("--env", f"{key}={value}")), image)
    config, = inspect("container", "inspect", name)
    host = config["HostConfig"]
    mounts = config["Mounts"]
    require(config["Image"] == image and host["NetworkMode"] == "none" and not host["PortBindings"]
            and not host["Privileged"] and host["ReadonlyRootfs"] and set(host["CapDrop"]) == {"ALL"}
            and len(mounts) == 1 and mounts[0]["Source"] == str(data) and mounts[0]["Destination"] == "/data"
            and mounts[0]["Type"] == "bind", "Candidate container is not isolated from production")
    command("docker", "start", name)
    probe(command, inspect, name)


def remove_clone(command, name):
    ids = command("docker", "ps", "-aq", "--filter", f"name=^/{name}$").splitlines()
    require(len(ids) <= 1 and all(re.fullmatch(r"[0-9a-f]{12,64}", item) for item in ids),
            "Could not identify the disposable rehearsal container")
    if ids:
        command("docker", "rm", "-f", ids[0])


def rehearse(command, inspect, image, bundle, snapshot_path, before_ids, target_ids, columns):
    data = Path(tempfile.mkdtemp(prefix="clone-", dir=bundle))
    clone = data / "SQLite.db"
    name = "gf3-db-rehearsal-" + secrets.token_hex(12)
    try:
        with closing(open_db(snapshot_path, "ro")) as snapshot_db:
            verify(snapshot_db, before_ids)
        shutil.copyfile(snapshot_path, clone)
        clone.chmod(0o600)
        start_clone(command, inspect, name, image, data)
        command("docker", "stop", "--time", "15", name, timeout=25)
        verify_migration(snapshot_path, clone, before_ids, target_ids, columns)
    finally:
        # If Docker cannot prove removal, retain its private bind source for diagnosis.
        remove_clone(command, name)
        shutil.rmtree(data)
