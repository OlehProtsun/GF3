"""Disposable deployment/recovery exercise. Run only on a local or CI Docker engine."""

import ast
import importlib.util
from contextlib import closing, nullcontext
import json
import os
from pathlib import Path
import secrets
import shutil
import socket
import sqlite3
import subprocess
import tempfile
import time
from unittest.mock import patch


HERE = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("gf3_release", HERE / "gf3_release.py")
release = importlib.util.module_from_spec(spec)
spec.loader.exec_module(release)
spec = importlib.util.spec_from_file_location("gf3_guard", HERE / "gf3_guard.py")
guard = importlib.util.module_from_spec(spec)
spec.loader.exec_module(guard)
spec = importlib.util.spec_from_file_location("gf3_maintenance", HERE / "gf3_maintenance.py")
maintenance = importlib.util.module_from_spec(spec)
spec.loader.exec_module(maintenance)
if os.name == "nt":
    release.ENV = dict(os.environ)
    release.ENV.pop("DOCKER_HOST", None)


def diagnostic_command(*args, cwd=None, timeout=120, capture=True, check=True, data=None, extra_env=None):
    if args[0] == "git":
        return production_command(*args, cwd=cwd, timeout=timeout, capture=capture, check=check,
                                  data=data, extra_env=extra_env)
    result = subprocess.run(args, cwd=cwd, env={**release.ENV, **(extra_env or {})}, timeout=timeout, text=True, input=data,
                            capture_output=capture)
    if check and result.returncode:
        raise release.ReleaseError(f"{args[0]} {args[1]} failed (exit {result.returncode}): {result.stderr or ''}")
    return result.stdout if capture else ""


production_command = release.command
HISTORICAL_PRIOR = "353f8e23dbd3f6c9989e7df2198346b72c700487"
HISTORICAL_TARGET = "1175c39461e149a7bb35f428babed28279727899"
HISTORICAL_TARGET_IMAGE = "gf3-web:maintenance-target"


def free_port():
    with socket.socket() as listener:
        listener.bind(("127.0.0.1", 0))
        return listener.getsockname()[1]


def rehearse_migrations(root, snapshot_path):
    """Exercise the exact pinned PROD2-to-candidate migration on a seeded synthetic DB."""
    repo = HERE.parents[1]

    def git(*args):
        return release.git(*args, root=repo)

    revision = git("rev-parse", "HEAD").strip()
    target = maintenance.migration_ids(git, revision)
    baseline = maintenance.REVIEWED_SCHEMA_PREFIX_CONTRACTS[0]["migrationId"]
    cutoff = target.index(baseline) + 1
    before, added = target[:cutoff], target[cutoff:]
    assert added == maintenance.REVIEWED_RELEASE_MIGRATIONS
    columns = maintenance.migration_plan(git, maintenance.RELEASE_BASELINE, revision, added)
    image, = release.docker_json("image", "inspect", "gf3-web:ci")
    source_dir = repo / "GF3.Tests/TestResults/release-baseline"
    source_db = source_dir / "SQLite.db"
    source_media = source_dir / maintenance.MEDIA_DIRECTORY
    assert source_db.is_file() and source_media.is_dir(), "Synthetic pinned PROD2 fixture artifact is missing"
    with closing(sqlite3.connect(source_db)) as db:
        maintenance.verify(db, before)
        assert db.execute("PRAGMA foreign_key_check").fetchall() == []
        assert db.execute("SELECT COUNT(*) FROM employee").fetchone()[0] >= 2
        assert db.execute("SELECT COUNT(*) FROM shift_swap_request").fetchone()[0] >= 1
        assert db.execute("SELECT COUNT(*) FROM shift_correction_request").fetchone()[0] >= 1
        assert db.execute("SELECT COUNT(*) FROM workflow_log_entry").fetchone()[0] >= 1
        assert db.execute("SELECT COUNT(*) FROM communication_message").fetchone()[0] >= 1
        assert dict(db.execute("SELECT name,seq FROM sqlite_sequence")).get("shift_correction_request", 0) >= 9001
    frozen = root / "maintenance-backup.db"
    maintenance.snapshot(source_db, frozen, before, quiesced=True)
    shutil.copytree(source_media, root / maintenance.MEDIA_DIRECTORY)

    # Exercise protected prefix classification and data-preserving rollback checks from each
    # independently generated EF state, including the correction AUTOINCREMENT high-water mark.
    prefix_directory = source_dir / "prefixes"
    for prefix in range(len(added) + 1):
        prefix_db = prefix_directory / f"prefix-{prefix}.db"
        assert prefix_db.is_file(), f"Synthetic EF prefix {prefix} is missing"
        assert maintenance.verify_no_data_drift(frozen, prefix_db, columns, target) == prefix
    recovered = root / "maintenance-recovered.db"
    maintenance.snapshot(frozen, recovered, before)
    assert maintenance.verify_no_data_drift(frozen, recovered, columns, target) == 0

    dirty = root / "maintenance-dirty.db"
    shutil.copyfile(source_dir / "prefixes/prefix-1.db", dirty)
    with closing(sqlite3.connect(dirty)) as db, db:
        db.execute("UPDATE employee SET first_name = first_name || ' drift' WHERE id = (SELECT MIN(id) FROM employee)")
    try:
        maintenance.verify_no_data_drift(frozen, dirty, columns, target)
    except maintenance.MaintenanceError as error:
        assert "Database writes may have occurred" in str(error)
    else:
        raise AssertionError("Recovery accepted changed legacy employee data")
    unknown = root / "maintenance-unknown.db"
    shutil.copyfile(source_db, unknown)
    with closing(sqlite3.connect(unknown)) as db, db:
        db.execute("ALTER TABLE employee_account ADD COLUMN unreviewed TEXT")
    try:
        maintenance.verify_no_data_drift(frozen, unknown, columns, target)
    except maintenance.MaintenanceError as error:
        assert "reviewed schema prefix" in str(error)
    else:
        raise AssertionError("Recovery accepted an unknown partial schema")

    maintenance.rehearse(release.command, release.docker_json, image["Id"], root, frozen, before, target, columns)
    maintenance.rehearse(release.command, release.docker_json, image["Id"], root, frozen, before, target, columns,
                         verify_modes=True)
    print("Maintenance rehearsal passed: full seven-migration chain, eight schema prefixes, row preservation, media, and safe rollback")

    # Make the first new-column migration fail after the swap-index prefix has committed.
    table, name, affinity = columns.nullable_columns[0]
    fixture = root / "maintenance-failed-prefix.db"
    shutil.copyfile(frozen, fixture)
    with closing(sqlite3.connect(fixture)) as db, db:
        db.execute(f'ALTER TABLE "{table}" ADD COLUMN "{name}" {affinity} NULL')
    try:
        maintenance.rehearse(release.command, release.docker_json, image["Id"], root, fixture, before, target, columns)
    except maintenance.MaintenanceError as error:
        assert "Candidate stopped before rehearsal health passed" in str(error)
    else:
        raise AssertionError("Injected full-chain migration failure passed the isolated rehearsal")
    finally:
        assert not list(root.glob("clone-*"))
    with closing(sqlite3.connect(frozen)) as db:
        maintenance.verify(db, before)
    assert maintenance.media_stamp(root / maintenance.MEDIA_DIRECTORY) == maintenance.media_stamp(source_media)
    print("Maintenance rehearsal passed: injected mid-chain failure stayed isolated; verified baseline and media remained recoverable")


def maintenance_scenario(root, previous, candidate, baseline, git_dir, before, sha, scenario, sql, tags, columns, added):
    """Run the real maintenance engine on the runner's disposable Docker volume and nftables."""
    database = release.maintenance_database(baseline[2])
    with closing(sqlite3.connect(database)) as db:
        historical = tuple(row[0] for row in db.execute('SELECT MigrationId FROM "__EFMigrationsHistory" ORDER BY MigrationId'))
    assert not set(historical).intersection(added)
    expected = historical + added
    media_before = maintenance.media_stamp(database.parent / maintenance.MEDIA_DIRECTORY)
    data = root / "recovery"
    data.mkdir(mode=0o700)
    journal = maintenance.Journal(release.STATE, data, release.store)
    journal.begin({"before": before, "target": sha})
    release.activate(previous)
    timer = [True, True]
    real_command = release.command
    original_git = release.git
    original_verify = release.verify_web
    final_verify_failed = False
    real_migrate = release.migrate_fenced
    real_restore = maintenance.restore_database
    assert subprocess.run(["nft", "list", "table", "inet", "gf3_maintenance"], capture_output=True).returncode != 0

    def local_command(*args, **kwargs):
        if args[0] != "systemctl":
            return real_command(*args, **kwargs)
        if args[1] == "show" and args[2].endswith(".service"):
            return "inactive\n"
        if args[1] == "show":
            return (f"ActiveState={'active' if timer[1] else 'inactive'}\n"
                    f"UnitFileState={'enabled' if timer[0] else 'disabled'}\n")
        if args[2].endswith(".timer"):
            if args[1] in ("enable", "disable"):
                timer[0] = args[1] == "enable"
            else:
                timer[1] = args[1] == "start"
        return ""

    def git(*args, root=None):
        if scenario == "git-failure" and "merge" in args:
            raise release.ReleaseError("synthetic Git activation failure")
        return original_git(*args, root=root or git_dir)

    def guard_result(bundle, *args):
        if scenario == "guard-failure":
            raise release.ReleaseError("synthetic Guard failure")
        def fixture_feed(_):
            raise OSError("offline synthetic fixture")

        updater = guard.Guard(release.RUNTIME, root, fetch=fixture_feed,
                              run_command=lambda args, **options: subprocess.run(args, env=release.ENV, **options))
        updater.compose = ["docker", *release.compose_args(), "-f", str(bundle / "compose.json"), "exec", "-T", "nginx"]
        if args[0] == "run":
            state = updater.state()
            state["bans"]["8.8.8.8"] = {"expires": time.time() + 3600, "reason": "manual"}
            updater.save_state(state)
        return updater.execute(guard.arguments(args))

    def observe_web(bundle, acme=True, address=None):
        nonlocal final_verify_failed
        if scenario == "failure-after-open" and not final_verify_failed:
            journal_state = journal.read()
            result = subprocess.run(["nft", "list", "table", "inet", "gf3_maintenance"], capture_output=True)
            if journal_state["writes_may_resume"] and result.returncode != 0:
                sql("INSERT INTO rehearsal_marker VALUES ('arrived-after-reopen')")
                final_verify_failed = True
                raise release.ReleaseError("synthetic failure after reopening")
        return original_verify(bundle, acme, address)

    def simulate_restart(*args):
        if scenario == "migration-before-start":
            raise release.ReleaseError("Synthetic failure before the first migration")
        if scenario == "fence-lost":
            # Exercise the same hazards as a reboot without rebooting the GitHub runner:
            # the kernel table disappears and the Docker daemon re-reads restart policies.
            real_command("nft", "delete", "table", "inet", "gf3_maintenance")
            real_command("systemctl", "restart", "docker")
        result = real_migrate(*args)
        if scenario in {"migration-failure", "partial-restore"}:
            raise release.ReleaseError("Synthetic failure after the migration chain")
        return result

    def interrupt_restore(database, bundle, metadata, assert_quiet, consume_restore, expected_stamp, record_progress):
        def progress(value):
            record_progress(value)
            if scenario == "partial-restore" and value["stage"] == "database-installed":
                raise OSError("synthetic interruption before media installation")

        return real_restore(database, bundle, metadata, assert_quiet, consume_restore, expected_stamp, progress)

    try:
        with (patch.object(release, "command", local_command), patch.object(release, "git", git),
              patch.object(release, "guard_result", guard_result), patch.object(release, "verify_web", observe_web),
              patch.object(release, "migrate_fenced", simulate_restart),
              patch.object(maintenance, "restore_database", interrupt_restore)):
            try:
                release.maintenance_cutover(maintenance, journal, candidate, previous, before, sha, baseline,
                                            historical, expected, columns)
            except release.ReleaseError as error:
                if scenario in {"migration-before-start", "migration-failure", "git-failure", "guard-failure"}:
                    assert "were recovered" in str(error), str(error)
                elif scenario == "fence-lost":
                    assert journal.read()["phase"] == "RECOVERY_REQUIRED" and not (data / "failed-database").exists()
                    assert all(not release.container(name)["State"]["Running"] for name in ("gf3-app", "nginx"))
                    with closing(sqlite3.connect(database)) as db:
                        maintenance.verify(db, historical)
                    print("Maintenance transaction remained offline after losing the fence and restarting Docker")
                    return
                elif scenario == "partial-restore":
                    state = journal.read()
                    assert state["phase"] == "RECOVERY_REQUIRED" and state["failed_in"] == "RESTORING"
                    assert state["restore_progress"]["stage"] == "database-installed"
                    assert (data / "failed-database" / maintenance.MEDIA_DIRECTORY).is_dir()
                    assert Path(state["restore_progress"]["staged_media"]).is_dir()
                    assert all(not release.container(name)["State"]["Running"] for name in ("gf3-app", "nginx"))
                    print("Interrupted database/media restore retained its evidence and stayed offline")
                    return
                elif scenario == "failure-after-open":
                    assert journal.read()["writes_may_resume"] and not (data / "failed-database").exists()
                    assert final_verify_failed, str(error)
                else:
                    raise
            else:
                assert scenario == "success"
        assert release.topology() == baseline
        recovered = scenario in {"migration-before-start", "migration-failure", "git-failure", "guard-failure"}
        activated = previous if recovered else candidate
        assert release.active_bundle() == activated
        assert maintenance.media_stamp(database.parent / maintenance.MEDIA_DIRECTORY) == media_before
        if scenario != "failure-after-open":
            maintenance.verify_migration(data / "final.db", database, historical,
                                         historical if recovered else expected, () if recovered else columns)
        with closing(sqlite3.connect(database)) as db:
            maintenance.verify(db, historical if recovered else expected)
            values = [row[0] for row in db.execute("SELECT value FROM rehearsal_marker")]
            assert len(values) == (2 if scenario == "failure-after-open" else 1)
            assert ("arrived-after-reopen" in values) == (scenario == "failure-after-open")
        if recovered:
            assert json.loads((data / "transaction.json").read_text())["phase"] == "RECOVERED"
            assert release.container("gf3-app")["Image"] == release.docker_json("image", "inspect", tags[0])[0]["Id"]
        print(f"Maintenance transaction passed on Linux Docker: {scenario}")
    finally:
        # This test operates only on a disposable runner and must clean up any fenced failure case.
        subprocess.run(["nft", "delete", "table", "inet", "gf3_maintenance"], capture_output=True)


def main(scenario=None, historical_pair=False):
    release.command = diagnostic_command
    project = "gf3-rehearsal-" + secrets.token_hex(5)
    network = project + "_gf3-internal"
    volume = project + "-data"
    app = f"{project}-gf3-app-1"
    marker = secrets.token_hex(16)
    # The engine rejects active bundles below world-writable parents. Use the real
    # entrypoint's permissions in the root-only hosted integration fixture.
    with tempfile.TemporaryDirectory(prefix=project, dir="/var/lib" if scenario else None) as temporary:
        root = Path(temporary)
        release.ROOT = root
        release.STATE = root / "releases"
        release.RUNTIME = root / "deploy/security/runtime"
        release.DOMAIN = "localhost"
        release.PROJECT = project
        release.NETWORK = network
        release.STATE.mkdir(mode=0o700)
        for name in ("nginx", "logs", "state"):
            (release.RUNTIME / name).mkdir(parents=True)
        deploy = root / "deploy"
        web = root / "certbot"
        (web / ".well-known/acme-challenge").mkdir(parents=True)
        for directory in (web, web / ".well-known", web / ".well-known/acme-challenge"):
            directory.chmod(0o755)  # The Nginx worker serves the ACME proof.
        for filename in ("docker-compose.yml", "docker-compose.https.yml"):
            shutil.copyfile(HERE.parent / filename, deploy / filename)
        shutil.copytree(HERE.parent / "nginx", deploy / "nginx")
        cert = root / "fullchain.pem"
        key = root / "privkey.pem"
        subprocess.run(["openssl", "req", "-x509", "-nodes", "-newkey", "rsa:2048", "-days", "2",
                        "-subj", "/CN=localhost", "-addext", "subjectAltName=DNS:localhost",
                        "-keyout", str(key), "-out", str(cert)], check=True, capture_output=True)
        env_file = deploy / ".env"
        previous_image = ("gf3-web:maintenance-prior" if historical_pair else HISTORICAL_TARGET_IMAGE) if scenario else "gf3-web:ci"
        env_file.write_text("\n".join([
            f"GF3_IMAGE={previous_image}", "GF3_SERVER_NAME=localhost",
            "GF3_REQUIRE_EXISTING_DATABASE=false",
            "GF3_BACKUP_BEFORE_MIGRATE=true", f"GF3_DATA_VOLUME={volume}",
            "GF3_ADMIN_DEVELOPER_PASSWORD=disposable-only", "GF3_BOOTSTRAP_MANAGER_PASSWORD=743961",
            f"Jwt__SigningKey={secrets.token_hex(48)}", f"GF3_TLS_CERT_PATH={cert.as_posix()}",
            f"GF3_TLS_KEY_PATH={key.as_posix()}", f"GF3_ACME_WEBROOT={web.as_posix()}",
            f"GF3_HTTP_PORT=127.0.0.1:{free_port()}", f"GF3_HTTPS_PORT=127.0.0.1:{free_port()}",
        ]) + "\n")
        base = ("docker", "compose", "-p", project, "--env-file", str(env_file), "-f",
                str(deploy / "docker-compose.yml"), "-f", str(deploy / "docker-compose.https.yml"))
        # Use ephemeral loopback ports in the rehearsal while testing the production cutover functions.
        tags = []

        def curl(scheme, path, *args):
            bundle = current[0]
            internal = "443" if scheme == "https" else "80"
            port = release.compose(bundle, "port", "nginx", internal).strip().rsplit(":", 1)[1]
            return release.command("curl", "--noproxy", "*", "--cacert", str(cert), "-sS", "--max-time", "10",
                                   *args, f"{scheme}://localhost:{port}{path}")

        real_verify = release.verify_web

        def verify(bundle, acme=True, address=None):
            current[0] = bundle
            if address is not None:
                real_curl_at = release.curl_at

                def trusted_curl(address, scheme, path, *args):
                    return real_curl_at(address, scheme, path, "--cacert", str(cert), *args)

                with patch.object(release, "curl_at", trusted_curl):
                    return real_verify(bundle, acme, address=address)
            # The ephemeral port is different from production's port. Keep the real certificate, HTTP and ACME checks.
            def command(*args, **kwargs):
                if args[:2] == ("openssl", "s_client"):
                    port = release.compose(bundle, "port", "nginx", "443").strip().rsplit(":", 1)[1]
                    args = tuple(f"127.0.0.1:{port}" if arg == "127.0.0.1:443" else arg for arg in args)
                return real_command(*args, **kwargs)
            with patch.object(release, "command", command), patch.object(release, "curl", curl):
                real_verify(bundle, acme)

        current = [None]
        activated = []
        real_activate = release.activate
        real_command = release.command
        real_lock = release.lock
        git_dir = root / "checkout"
        git_dir.mkdir()
        release.git("init", "-b", "PROD2", root=git_dir)
        (git_dir / "release-marker").write_text("original\n")
        release.git("add", "release-marker", root=git_dir)
        release.git("-c", "user.name=GF3 Fixture", "-c", "user.email=gf3@example.test",
                    "commit", "--allow-empty", "-m", "before", root=git_dir)
        before = release.git("rev-parse", "HEAD", root=git_dir).strip()
        release.git("switch", "-c", "candidate", root=git_dir)
        (git_dir / "release-marker").write_text(marker)
        release.git("add", "release-marker", root=git_dir)
        release.git("-c", "user.name=GF3 Fixture", "-c", "user.email=gf3@example.test",
                    "commit", "-m", "candidate", root=git_dir)
        sha = release.git("rev-parse", "HEAD", root=git_dir).strip()
        release.git("switch", "PROD2", root=git_dir)
        real_git = release.git
        if os.name != "nt":
            # A read-only status must not write the index or run its local hook.
            hooks = git_dir / ".git/hooks"
            hook = hooks / "post-index-change"
            hook.write_text("#!/bin/sh\n"
                            "if test -f .git/fixture-armed; then\n"
                            "  rm .git/fixture-armed\n"
                            "  printf 'temporary\\n' > release-marker\n"
                            "fi\n")
            hook.chmod(0o755)
            armed = git_dir / ".git/fixture-armed"
            armed.touch()
            os.utime(git_dir / "release-marker", (1000000000, 1000000000))
            assert not real_git("status", "--porcelain", root=git_dir)
            try:
                real_git("merge", "--ff-only", sha, root=git_dir)
            except release.GitMergeError as error:
                assert error.reason == "tracked changes would be overwritten"
            else:
                raise AssertionError("Index refresh hook did not reproduce Git rejection")
            (git_dir / "release-marker").write_text("original\n")
            armed.touch()
            assert not release.git_status("--porcelain", root=git_dir)
            assert armed.is_file() and (git_dir / "release-marker").read_text() == "original\n"
            armed.unlink()
            hook.unlink()
            print("Rehearsal passed: read-only status prevents index-refresh hook side effects")
        try:
            release.command(*base, "create", "--no-build", "--pull", "never", capture=False)
            bridge, = release.docker_json("network", "inspect", network)
            cidr = next(row["Subnet"] for row in bridge["IPAM"]["Config"] if ":" not in row["Subnet"])
            with env_file.open("a") as output:
                output.write(f"GF3_TRUSTED_PROXY_CIDR={cidr}\n")
            if scenario and not historical_pair:
                fixture = HERE.parents[1] / "GF3.Tests/TestResults/release-baseline"
                assert (fixture / "SQLite.db").is_file() and (fixture / "communication-gifs").is_dir()
                release.command("docker", "run", "--rm", "--network", "none", "-v", f"{volume}:/data",
                                "-v", f"{fixture.as_posix()}:/fixture:ro", "mirror.gcr.io/library/python:3.12-alpine",
                                "python3", "-c", "import os,shutil; shutil.copyfile('/fixture/SQLite.db','/data/SQLite.db'); "
                                "shutil.copytree('/fixture/communication-gifs','/data/communication-gifs',dirs_exist_ok=True); "
                                "os.chown('/data',1654,1654); os.chmod('/data',0o755); "
                                "os.chown('/data/SQLite.db',1654,1654); os.chmod('/data/SQLite.db',0o600); "
                                "[(os.chown(os.path.join(r,n),1654,1654),os.chmod(os.path.join(r,n),0o755)) "
                                "for r,ds,fs in os.walk('/data/communication-gifs') for n in ds]; "
                                "[(os.chown(os.path.join(r,n),1654,1654),os.chmod(os.path.join(r,n),0o644)) "
                                "for r,ds,fs in os.walk('/data/communication-gifs') for n in fs]")
            release.command(*base, "up", "-d", "--no-build", "--pull", "never", "--wait", "--wait-timeout", "100")
            baseline = release.topology()

            def sql(statement):
                return release.command("docker", "run", "--rm", "--network", "none", "-v", f"{volume}:/data",
                                       "mirror.gcr.io/library/python:3.12-alpine", "python3", "-c",
                                       "import sqlite3,sys; c=sqlite3.connect('/data/SQLite.db'); "
                                       "print(c.execute(sys.argv[1]).fetchall()); c.commit()", statement)

            def rows(statement):
                return ast.literal_eval(sql(statement).strip())

            sql("CREATE TABLE rehearsal_marker(value TEXT)")
            sql(f"INSERT INTO rehearsal_marker VALUES ('{marker}')")
            migration_history = rows("SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId")
            assert migration_history, "rehearsal database has no EF migration history"
            sql("CREATE TABLE IF NOT EXISTS employee "
                "(id INTEGER PRIMARY KEY, contract_type TEXT NOT NULL, first_name TEXT NOT NULL, "
                "last_name TEXT NOT NULL, is_deleted INTEGER NOT NULL DEFAULT 0)")
            employee_id = rows("SELECT COALESCE(MAX(id),0)+1 FROM employee")[0][0]
            sql("INSERT INTO employee(id,contract_type,first_name,last_name,is_deleted) "
                f"VALUES ({employee_id},'zlecenie','GF3 rehearsal','Employee',0)")
            employee_rows = rows(f"SELECT id,contract_type,first_name,last_name,is_deleted FROM employee WHERE id={employee_id}")
            assert employee_rows == [(employee_id, "zlecenie", "GF3 rehearsal", "Employee", 0)]
            assert rows("PRAGMA integrity_check") == [("ok",)]
            previous = release.STATE / ("rollback-" + project)
            previous.mkdir(mode=0o700)
            with patch.object(release, "safe_path"):
                release.snapshot(previous, volume)
            tags.extend(service["image"] for service in json.loads((previous / "compose.json").read_text())["services"].values())
            release.nginx_test(previous)
            verify(previous)

            if scenario:
                candidate = release.STATE / "release-maintenance"
                candidate.mkdir(mode=0o700)
                config = release.render(root)
                release.freeze(config, volume)
                app_service = config["services"]["gf3-app"]
                app_service["image"] = HISTORICAL_TARGET_IMAGE if historical_pair else "gf3-web:ci"
                app_service["environment"]["DatabaseStartup__RequireExistingDatabase"] = "true"
                release.save_compose(candidate, config)
                (candidate / "guard-enabled").touch()
                def repository_git(*args):
                    return release.git(*args, root=HERE.parents[1])

                revision = repository_git("rev-parse", "HEAD").strip()
                if historical_pair:
                    added = ("20261114225324_AddShiftCorrectionComment", "20261115135735_RefreshManualShiftOffer")
                    plan = maintenance.migration_plan(repository_git, HISTORICAL_PRIOR, HISTORICAL_TARGET, added)
                else:
                    assert migration_history[-1][0] == maintenance.REVIEWED_SCHEMA_PREFIX_CONTRACTS[0]["migrationId"]
                    added = maintenance.REVIEWED_RELEASE_MIGRATIONS
                    plan = maintenance.migration_plan(repository_git, maintenance.RELEASE_BASELINE, revision, added)
                with patch.object(release, "verify_web", verify):
                    maintenance_scenario(root, previous, candidate, baseline, git_dir, before, sha,
                                         scenario, sql, tags, plan, added)
                return
            # Certbot replaces symlinks under live/. Recreate Nginx so its file bind follows the replacement.
            for name in (cert, key):
                name.rename(name.with_suffix(".previous"))
            subprocess.run(["openssl", "req", "-x509", "-nodes", "-newkey", "rsa:2048", "-days", "2",
                            "-subj", "/CN=localhost", "-addext", "subjectAltName=DNS:localhost",
                            "-keyout", str(key), "-out", str(cert)], check=True, capture_output=True)
            release.nginx_test(previous)
            release.start_nginx(previous)
            verify(previous)
            print("Rehearsal passed: certificate replacement and served fingerprint")
            db = root / "test-backup.db"
            # Copy a consistent online backup out of the VM for the actual backup validator.
            release.command("docker", "exec", app, "test", "-f", "/data/SQLite.db")
            release.command("docker", "run", "--rm", "--network", "none", "-v", f"{volume}:/data",
                            "-v", f"{root.as_posix()}:/backup", "mirror.gcr.io/library/python:3.12-alpine",
                            "python3", "-c", "import sqlite3; src=sqlite3.connect('file:/data/SQLite.db?mode=ro',uri=True); "
                            "src.backup(sqlite3.connect('/backup/test-backup.db'))")
            release.backup(previous, volume, db)
            with closing(sqlite3.connect(previous / "SQLite.db")) as backup:
                assert backup.execute("SELECT value FROM rehearsal_marker").fetchall() == [(marker,)]
                assert backup.execute("SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory "
                                      "ORDER BY MigrationId").fetchall() == migration_history
                assert backup.execute("SELECT id,contract_type,first_name,last_name,is_deleted FROM employee "
                                      "WHERE id=?", (employee_id,)).fetchall() == employee_rows
                assert backup.execute("PRAGMA integrity_check").fetchall() == [("ok",)]

            if (os.environ.get("GITHUB_ACTIONS") == "true"
                    or (HERE.parents[1] / "GF3.Tests/TestResults/release-baseline/SQLite.db").is_file()):
                rehearse_migrations(root, previous / "SQLite.db")

            template = deploy / "nginx/https-templates/gf3.conf.template"
            for scenario in ("nginx-syntax", "https-failure", "activation-failure", "transient-worktree", "success"):
                candidate = release.STATE / f"release-{scenario}"
                candidate.mkdir()
                config = release.render(root)
                release.freeze(config, volume)
                tag = release.local_image("gf3-web:ci", f"{project}-app:{scenario}")
                tags.append(tag)
                config["services"]["gf3-app"]["image"] = tag
                variant = candidate / "site.template"
                data = template.read_text()
                if scenario == "nginx-syntax":
                    data += "\ninvalid_gf3_rehearsal_directive;\n"
                if scenario == "https-failure":
                    data = data.replace("    location ^~ /api/ {", "    location = /api/health { return 503; }\n\n"
                                        "    location ^~ /api/ {")
                variant.write_text(data)
                release.mounts(config["services"]["nginx"])[release.TEMPLATE]["source"] = str(variant)
                release.save_compose(candidate, config)
                (candidate / "guard-enabled").touch()
                if os.name != "nt":
                    release.activate(previous)
                if scenario == "nginx-syntax":
                    try:
                        release.nginx_test(candidate)
                    except release.ReleaseError:
                        pass
                    else:
                        raise AssertionError("Invalid Nginx config passed staging")
                    verify(previous)
                else:
                    release.nginx_test(candidate)
                    with patch.object(release, "verify_web", verify), patch.object(
                            release, "activate", side_effect=lambda bundle: activated.append(bundle)
                            if os.name == "nt" else real_activate(bundle)):
                        if scenario == "https-failure":
                            try:
                                release.cutover(candidate, previous, baseline)
                            except release.ReleaseError as error:
                                assert "prior release restored" in str(error)
                            else:
                                raise AssertionError("HTTP 503 passed the cutover")
                        else:
                            timer = {"enabled": False, "active": False}
                            runs = []
                            geo = release.RUNTIME / "nginx/blocked-compiled.geo"

                            def activation_command(*args, **kwargs):
                                if args[0] == "systemctl":
                                    if args[1] == "show":
                                        if args[2] == "gf3-guard.service":
                                            return "inactive\n"
                                        return ("ActiveState=" + ("active" if timer["active"] else "inactive")
                                                + "\nUnitFileState=" + ("enabled" if timer["enabled"] else "disabled"))
                                    if args[-1] == "gf3-guard.timer":
                                        if args[1] in ("enable", "disable"):
                                            timer["enabled"] = args[1] == "enable"
                                        else:
                                            timer["active"] = args[1] == "start"
                                    return ""
                                return real_command(*args, **kwargs)

                            def fixture_feed(_):
                                raise OSError("offline synthetic fixture")

                            def guard_result(bundle, *args):
                                updater = guard.Guard(release.RUNTIME, root, fetch=fixture_feed,
                                                      run_command=lambda args, **options:
                                                      subprocess.run(args, env=release.ENV, **options))
                                updater.compose = ["docker", *release.compose_args(), "-f", str(bundle / "compose.json"),
                                                   "exec", "-T", "nginx"]
                                if args[0] == "run":
                                    runs.append(args)
                                    state = updater.state()
                                    state["bans"]["8.8.8.8"] = {"expires": time.time() + 3600, "reason": "manual"}
                                    updater.save_state(state)
                                if os.name == "nt":
                                    with patch.object(guard, "guard_lock", return_value=nullcontext(True)):
                                        return updater.execute(guard.arguments(args))
                                return updater.execute(guard.arguments(args))

                            def git(*args, root=None):
                                if args[0] == "fetch":
                                    return ""
                                if args == ("rev-parse", "refs/remotes/origin/PROD2"):
                                    return sha
                                is_merge = "merge" in args
                                if is_merge and scenario == "transient-worktree":
                                    path = git_dir / "release-marker"
                                    # Simulate a writer after the clean pre-merge observation.
                                    path.write_text("temporary synthetic value\n")
                                    try:
                                        return real_git(*args, root=git_dir)
                                    finally:
                                        path.write_text("original\n")
                                return real_git(*args, root=root or git_dir)

                            def cutover(*_):
                                release.cutover(candidate, previous, baseline)
                                return candidate, previous, baseline

                            def recover_with_audit(bundle, acme):
                                audit = json.loads((candidate / "git-activation.json").read_text())
                                assert audit["result"] == "failed"
                                real_recover(bundle, acme)

                            def current_bundle():
                                if os.name == "nt":
                                    return activated[-1] if activated else previous
                                return (release.STATE / "active").resolve(strict=True)

                            if scenario == "activation-failure":
                                index_lock = git_dir / ".git/index.lock"
                                index_lock.touch()
                            real_recover = release.recover
                            with (patch.object(release, "command", activation_command),
                                  patch.object(release, "git", git), patch.object(release, "verify_tooling"),
                                  patch.object(release, "deploy", cutover),
                                  patch.object(release, "guard_result", guard_result),
                                  patch.object(release, "lock", side_effect=lambda path:
                                               nullcontext() if os.name == "nt" else real_lock(path)),
                                  patch.object(release, "active_bundle", current_bundle)):
                                if scenario in ("activation-failure", "transient-worktree"):
                                    try:
                                        with patch.object(release, "recover", recover_with_audit):
                                            release.deploy_and_activate(sha, "fixture")
                                    except release.ReleaseError as error:
                                        assert "git-fast-forward:" in str(error) and "prior release restored" in str(error)
                                    else:
                                        raise AssertionError("Unsafe Git update passed activation")
                                    finally:
                                        if scenario == "activation-failure":
                                            index_lock.unlink()
                                    assert real_git("rev-parse", "HEAD", root=git_dir).strip() == before
                                    assert not real_git("--no-optional-locks", "status", "--porcelain", root=git_dir)
                                    assert not any(timer.values()) and not geo.exists()
                                    audit_path = candidate / "git-activation.json"
                                    audit = json.loads(audit_path.read_text())
                                    assert audit["result"] == "failed" and not audit["before"]["tracked"]
                                    if os.name != "nt":
                                        assert audit_path.stat().st_mode & 0o777 == 0o600
                                    if scenario == "transient-worktree":
                                        assert audit["failure"]["reason"] == "tracked changes would be overwritten"
                                        assert audit["failure"]["reported_paths"] == ["release-marker"]
                                        assert not audit["after_failure"]["tracked"]
                                        assert "temporary synthetic value" not in audit_path.read_text()
                                else:
                                    release.deploy_and_activate(sha, "fixture")
                                    assert real_git("rev-parse", "HEAD", root=git_dir).strip() == sha
                                    assert (git_dir / "release-marker").read_text() == marker
                                    assert all(timer.values()) and geo.is_file()
                                    assert json.loads((candidate / "git-activation.json").read_text())["result"] == "merged"
                                assert runs == [("run", "--require-lock")]
                                assert current_bundle() == (candidate if scenario == "success" else previous)
                                with patch.dict(os.environ, {"RENEWED_LINEAGE": "/etc/letsencrypt/live/app-gf.com"}):
                                    if os.name == "nt":
                                        # Windows cannot create the privileged directory symlink used by production.
                                        with patch.object(Path, "exists", side_effect=lambda: True):
                                            release.renew()
                                    else:
                                        release.renew()
                                if scenario == "success":
                                    release.recover(previous, acme=True)
                assert release.topology() == baseline
                if os.name != "nt":
                    assert (release.STATE / "active").resolve() == previous
                elif scenario != "nginx-syntax":
                    assert activated[-1] == previous
                assert rows("SELECT value FROM rehearsal_marker") == [(marker,)]
                assert rows("SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId") == migration_history
                assert rows(f"SELECT id,contract_type,first_name,last_name,is_deleted FROM employee WHERE id={employee_id}") == employee_rows
                assert rows("PRAGMA integrity_check") == [("ok",)]
                for service, desired in json.loads((previous / "compose.json").read_text())["services"].items():
                    if scenario != "nginx-syntax":
                        expected = release.docker_json("image", "inspect", desired["image"])[0]["Id"]
                        assert release.container(service)["Image"] == expected
                print(f"Rehearsal passed: {scenario}, original volume and SQLite marker retained")
        finally:
            release.command(*base, "down", "--remove-orphans", "--volumes", check=False)
            for tag in tags:
                release.command("docker", "image", "rm", tag, check=False)


def build_rehearsal_image(repo, revision, image):
    subprocess.run(["git", "fetch", "--no-tags", "--depth=1", "origin", revision], cwd=repo, check=True)
    with tempfile.TemporaryDirectory(prefix="gf3-rehearsal-source-") as worktree:
        subprocess.run(["git", "worktree", "add", "--detach", worktree, revision], cwd=repo, check=True)
        try:
            subprocess.run(["docker", "build", "-t", image, worktree], check=True)
        finally:
            subprocess.run(["git", "worktree", "remove", "--force", worktree], cwd=repo, check=True)


if __name__ == "__main__":
    import sys

    scenarios = ("success", "migration-failure", "git-failure", "guard-failure", "failure-after-open", "fence-lost")
    reviewed_scenarios = (*scenarios, "migration-before-start", "partial-restore")
    if len(sys.argv) in (2, 3) and sys.argv[1] in reviewed_scenarios:
        assert os.geteuid() == 0 and os.environ.get("GF3_DISPOSABLE_CI_RUNNER") == "true"
        os.umask(0o077)
        historical_pair = len(sys.argv) == 3
        assert not historical_pair or (sys.argv[2] == "historical" and sys.argv[1] in scenarios)
        main(sys.argv[1], historical_pair)
    else:
        assert len(sys.argv) == 1
        main()
        if os.environ.get("GITHUB_ACTIONS") == "true":
            if not shutil.which("nft"):
                subprocess.run(["sudo", "-n", "apt-get", "update"], check=True)
                subprocess.run(["sudo", "-n", "apt-get", "install", "-y", "nftables"], check=True)
            repo = HERE.parents[1]
            build_rehearsal_image(repo, HISTORICAL_PRIOR, "gf3-web:maintenance-prior")
            build_rehearsal_image(repo, HISTORICAL_TARGET, HISTORICAL_TARGET_IMAGE)
            for case in reviewed_scenarios:
                subprocess.run(["sudo", "-n", "env", "GF3_DISPOSABLE_CI_RUNNER=true",
                                "/usr/bin/python3", "-u", str(Path(__file__).resolve()), case], check=True)
            for case in scenarios:
                subprocess.run(["sudo", "-n", "env", "GF3_DISPOSABLE_CI_RUNNER=true",
                                "/usr/bin/python3", "-u", str(Path(__file__).resolve()), case, "historical"], check=True)
