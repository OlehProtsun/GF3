"""Disposable deployment/recovery exercise. Run only on a local or CI Docker engine."""

import importlib.util
from contextlib import closing
import json
import os
from pathlib import Path
import secrets
import shutil
import socket
import sqlite3
import subprocess
import tempfile
from unittest.mock import patch


HERE = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("gf3_release", HERE / "gf3_release.py")
release = importlib.util.module_from_spec(spec)
spec.loader.exec_module(release)
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
            for scenario in ("nginx-syntax", "https-failure", "success"):
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
                        try:
                            release.cutover(candidate, previous, baseline)
                        except release.ReleaseError as error:
                            assert scenario == "https-failure" and "prior release restored" in str(error)
                        else:
                            assert scenario == "success"
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
