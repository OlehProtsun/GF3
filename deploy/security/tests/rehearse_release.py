"""Disposable deployment/recovery exercise. Run only on a local or CI Docker engine."""

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
if os.name == "nt":
    release.ENV = dict(os.environ)
    release.ENV.pop("DOCKER_HOST", None)


def diagnostic_command(*args, cwd=None, timeout=120, capture=True, check=True, data=None, extra_env=None):
    result = subprocess.run(args, cwd=cwd, env={**release.ENV, **(extra_env or {})}, timeout=timeout, text=True, input=data,
                            capture_output=capture)
    if check and result.returncode:
        raise release.ReleaseError(f"{args[0]} {args[1]} failed (exit {result.returncode}): {result.stderr or ''}")
    return result.stdout if capture else ""


def free_port():
    with socket.socket() as listener:
        listener.bind(("127.0.0.1", 0))
        return listener.getsockname()[1]


def main():
    release.command = diagnostic_command
    project = "gf3-rehearsal-" + secrets.token_hex(5)
    network = project + "_gf3-internal"
    volume = project + "-data"
    app = f"{project}-gf3-app-1"
    marker = secrets.token_hex(16)
    with tempfile.TemporaryDirectory(prefix=project) as temporary:
        root = Path(temporary)
        release.ROOT = root
        release.STATE = root / "releases"
        release.RUNTIME = root / "deploy/security/runtime"
        release.DOMAIN = "localhost"
        release.PROJECT = project
        release.NETWORK = network
        release.STATE.mkdir()
        for name in ("nginx", "logs", "state"):
            (release.RUNTIME / name).mkdir(parents=True)
        deploy = root / "deploy"
        web = root / "certbot"
        (web / ".well-known/acme-challenge").mkdir(parents=True)
        for filename in ("docker-compose.yml", "docker-compose.https.yml"):
            shutil.copyfile(HERE.parent / filename, deploy / filename)
        shutil.copytree(HERE.parent / "nginx", deploy / "nginx")
        cert = root / "fullchain.pem"
        key = root / "privkey.pem"
        subprocess.run(["openssl", "req", "-x509", "-nodes", "-newkey", "rsa:2048", "-days", "2",
                        "-subj", "/CN=localhost", "-addext", "subjectAltName=DNS:localhost",
                        "-keyout", str(key), "-out", str(cert)], check=True, capture_output=True)
        env_file = deploy / ".env"
        env_file.write_text("\n".join([
            "GF3_IMAGE=gf3-web:ci", "GF3_SERVER_NAME=localhost", "GF3_REQUIRE_EXISTING_DATABASE=false",
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

        def verify(bundle, acme=True):
            current[0] = bundle
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
        try:
            release.command(*base, "create", "--no-build", "--pull", "never", capture=False)
            bridge, = release.docker_json("network", "inspect", network)
            cidr = next(row["Subnet"] for row in bridge["IPAM"]["Config"] if ":" not in row["Subnet"])
            with env_file.open("a") as output:
                output.write(f"GF3_TRUSTED_PROXY_CIDR={cidr}\n")
            release.command(*base, "up", "-d", "--no-build", "--pull", "never", "--wait", "--wait-timeout", "100")
            baseline = release.topology()

            def sql(statement):
                return release.command("docker", "run", "--rm", "--network", "none", "-v", f"{volume}:/data",
                                       "mirror.gcr.io/library/python:3.12-alpine", "python3", "-c",
                                       "import sqlite3,sys; c=sqlite3.connect('/data/SQLite.db'); "
                                       "print(c.execute(sys.argv[1]).fetchall()); c.commit()", statement)

            sql("CREATE TABLE rehearsal_marker(value TEXT)")
            sql(f"INSERT INTO rehearsal_marker VALUES ('{marker}')")
            previous = release.STATE / "rollback-rehearsal"
            previous.mkdir()
            with patch.object(release, "safe_path"):
                release.snapshot(previous, volume)
            tags.extend(service["image"] for service in json.loads((previous / "compose.json").read_text())["services"].values())
            release.nginx_test(previous)
            verify(previous)
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

            template = deploy / "nginx/https-templates/gf3.conf.template"
            for scenario in ("nginx-syntax", "https-failure", "activation-failure", "success"):
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

                            def git(*args):
                                if args[0] == "fetch":
                                    return ""
                                if args == ("rev-parse", "refs/remotes/origin/PROD2"):
                                    return sha
                                return real_git(*args, root=git_dir)

                            def cutover(*_):
                                release.cutover(candidate, previous, baseline)
                                return candidate, previous, baseline

                            def current_bundle():
                                if os.name == "nt":
                                    return activated[-1] if activated else previous
                                return (release.STATE / "active").resolve(strict=True)

                            if scenario == "activation-failure":
                                index_lock = git_dir / ".git/index.lock"
                                index_lock.touch()
                            with (patch.object(release, "command", activation_command),
                                  patch.object(release, "git", git), patch.object(release, "verify_tooling"),
                                  patch.object(release, "deploy", cutover),
                                  patch.object(release, "guard_result", guard_result),
                                  patch.object(release, "lock", side_effect=lambda path:
                                               nullcontext() if os.name == "nt" else real_lock(path)),
                                  patch.object(release, "active_bundle", current_bundle)):
                                if scenario == "activation-failure":
                                    try:
                                        release.deploy_and_activate(sha, "fixture")
                                    except release.ReleaseError as error:
                                        assert "git-fast-forward:" in str(error) and "prior release restored" in str(error)
                                    else:
                                        raise AssertionError("Git index lock passed activation")
                                    finally:
                                        index_lock.unlink()
                                    assert real_git("rev-parse", "HEAD", root=git_dir).strip() == before
                                    assert not any(timer.values()) and not geo.exists()
                                else:
                                    release.deploy_and_activate(sha, "fixture")
                                    assert real_git("rev-parse", "HEAD", root=git_dir).strip() == sha
                                    assert (git_dir / "release-marker").read_text() == marker
                                    assert all(timer.values()) and geo.is_file()
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
                assert marker in sql("SELECT value FROM rehearsal_marker")
                for service, desired in json.loads((previous / "compose.json").read_text())["services"].items():
                    if scenario != "nginx-syntax":
                        expected = release.docker_json("image", "inspect", desired["image"])[0]["Id"]
                        assert release.container(service)["Image"] == expected
                print(f"Rehearsal passed: {scenario}, original volume and SQLite marker retained")
        finally:
            release.command(*base, "down", "--remove-orphans", "--volumes", check=False)
            for tag in tags:
                release.command("docker", "image", "rm", tag, check=False)


if __name__ == "__main__":
    main()
