#!/usr/bin/env python3
"""Discover and approve an ordinary PROD2 release for the installed release engine."""

import argparse
from contextlib import contextmanager
import importlib.util
import json
import os
from pathlib import Path
import re
import signal
import stat
import subprocess
import sys
import tempfile
from urllib.error import URLError
from urllib.parse import urlencode
from urllib.request import HTTPRedirectHandler, ProxyHandler, Request, build_opener


ENGINE = Path(__file__).with_name("gf3_release.py")
ENGINE_SPEC = importlib.util.spec_from_file_location("gf3_release", ENGINE)
release = importlib.util.module_from_spec(ENGINE_SPEC)
ENGINE_SPEC.loader.exec_module(release)

REPOSITORY = "OlehProtsun/GF3"
BRANCH = "PROD2"
REF = f"refs/heads/{BRANCH}"
TRACKING_REF = f"refs/remotes/origin/{BRANCH}"
WORKFLOW_ID = 306281689
WORKFLOW_PATH = ".github/workflows/ci.yml"
JOBS = {"Build and test", "Docker image"}
APPROVAL = Path("/etc/gfctl/approved-release.conf")
DEPLOY_LOCK = Path("/run/lock/gf3-prod-deploy.lock")
SYSTEM_PATH = "/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"
ALLOWED_ORIGINS = {
    f"{scheme}{REPOSITORY.lower()}{suffix}"
    for scheme in ("https://github.com/", "git@github.com:", "ssh://git@github.com/")
    for suffix in ("", ".git")
}
MAINTENANCE_DIRS = (
    ".github/workflows/", ".github/actions/", "deploy/", "ops/", "infra/", "infrastructure/", "systemd/",
    "scripts/", "nginx/", "certbot/",
    "dataaccesslayer/migrations/", "dataaccesslayer/models/databasecontext/", "dataaccesslayer/administration/",
    "gf3.webapi/infrastructure/",
)
MAINTENANCE_FILES = {
    ".dockerignore", ".gitattributes", ".gitignore", ".gitmodules", "dockerfile", "gf3.webapi/program.cs",
}
HELPER_PATH = "deploy/security/gf3_prepare_release.py"
GIT_OPERATIONS = (
    "MERGE_HEAD", "CHERRY_PICK_HEAD", "REVERT_HEAD", "REBASE_HEAD", "rebase-apply", "rebase-merge", "sequencer",
    "BISECT_START", "info/grafts",
)


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


def github(path, **query):
    url = f"https://api.github.com/repos/{REPOSITORY}/actions/{path}"
    if query:
        url += "?" + urlencode(query)
    request = Request(url, headers={"Accept": "application/vnd.github+json", "User-Agent": "gf3-prod2",
                                    "X-GitHub-Api-Version": "2022-11-28", "Cache-Control": "no-cache"})
    try:
        with build_opener(ProxyHandler({}), NoRedirect()).open(request, timeout=15) as response:
            return json.load(response)
    except (URLError, OSError, ValueError, UnicodeError):
        raise release.ReleaseError("GitHub CI verification unavailable") from None


def github_rows(document, key):
    rows = document.get(key) if isinstance(document, dict) else None
    release.require(isinstance(rows, list) and document.get("total_count") == len(rows)
                    and all(isinstance(row, dict) for row in rows),
                    "GitHub CI returned incomplete or invalid results")
    return rows


def positive_number(value):
    return type(value) is int and value > 0


def verify_ci(sha):
    runs = github_rows(github(f"workflows/{WORKFLOW_ID}/runs", branch=BRANCH, event="push",
                              head_sha=sha, per_page=100), "workflow_runs")
    release.require(runs, "No PROD2 push CI run exists for the target SHA")
    for run in runs:
        release.require(
            run.get("workflow_id") == WORKFLOW_ID and run.get("name") == "CI"
            and run.get("path") in (WORKFLOW_PATH, f"{WORKFLOW_PATH}@{REF}")
            and run.get("head_branch") == BRANCH and run.get("event") == "push" and run.get("head_sha") == sha
            and all(isinstance(run.get(key), dict) and run[key].get("full_name") == REPOSITORY
                    for key in ("repository", "head_repository"))
            and all(positive_number(run.get(key)) for key in ("id", "run_number", "run_attempt")),
            "GitHub returned a CI run with an unexpected identity, event, branch or SHA",
        )
    release.require(len({run["run_number"] for run in runs}) == len(runs), "GitHub returned ambiguous CI runs")
    latest = max(runs, key=lambda run: run["run_number"])
    run_id, attempt = latest["id"], latest["run_attempt"]
    release.require(latest.get("status") == "completed" and latest.get("conclusion") == "success",
                    f"Latest PROD2 CI run {run_id} attempt {attempt} is pending or unsuccessful")
    jobs = github_rows(github(f"runs/{run_id}/attempts/{attempt}/jobs", per_page=100), "jobs")
    for name in sorted(JOBS):
        matching = [job for job in jobs if job.get("name") == name]
        release.require(len(matching) == 1 and all(
            matching[0].get(key) == value for key, value in {
                "run_id": run_id, "run_attempt": attempt, "head_sha": sha, "head_branch": BRANCH,
                "workflow_name": "CI", "status": "completed", "conclusion": "success",
            }.items()), f"CI job {name} lacks a successful result for the latest exact attempt")
    return run_id, attempt


def protected_file(path):
    release.safe_path(path, private=True)
    info = path.lstat()
    release.require(stat.S_ISREG(info.st_mode) and stat.S_IMODE(info.st_mode) == 0o600,
                    f"Expected a protected mode-0600 file: {path}")
    return path.read_bytes()


def approval_value(contents, key, pattern):
    # The existing menu uses unquoted assignments. Preserve unrelated bytes, including CRLF and comments.
    assignments = re.findall(rb"(?m)^[ \t]*(?:export[ \t]+)?" + key + rb"[ \t]*=([^\r\n]*)", contents)
    exact = list(re.finditer(rb"(?m)^" + key + rb"=(" + pattern + rb")(?=\r?$)", contents))
    release.require(len(assignments) == len(exact) == 1, f"Invalid or duplicate {key.decode()} in release approval")
    return exact[0]


def read_approval():
    contents = protected_file(APPROVAL)
    sha = approval_value(contents, b"APPROVED_SHA", rb"[0-9a-f]{40}")
    pin = approval_value(contents, b"NGINX_IMAGE", rb"nginx@sha256:[0-9a-f]{64}")
    return contents, sha.span(1), pin[1].decode()


def sync_directory(path):
    descriptor = os.open(path, os.O_RDONLY | os.O_DIRECTORY)
    try:
        os.fsync(descriptor)
    finally:
        os.close(descriptor)


def approve(approval, target):
    contents, (start, end), _ = approval
    backup = Path(tempfile.mkdtemp(prefix="approval-", dir=release.STATE)) / APPROVAL.name
    release.write(backup, contents)
    sync_directory(backup.parent)
    sync_directory(release.STATE)
    print(f"Previous approval saved to {backup}", flush=True)
    release.write(APPROVAL, contents[:start] + target.encode("ascii") + contents[end:])
    sync_directory(APPROVAL.parent)


def check_origin():
    urls = release.git("remote", "get-url", "--all", "origin").splitlines()
    release.require(len(urls) == 1 and urls[0].lower() in ALLOWED_ORIGINS,
                    "origin must fetch directly from the approved GitHub repository")


def check_checkout():
    git = release.git
    git_dir = release.ROOT / ".git"
    release.safe_path(git_dir)
    release.require(git_dir.is_dir() and Path(git("rev-parse", "--show-toplevel").rstrip("\n")) == release.ROOT
                    and Path(git("rev-parse", "--absolute-git-dir").rstrip("\n")) == git_dir
                    and git("rev-parse", "--is-shallow-repository").strip() == "false",
                    "Expected the main, complete production Git checkout")
    release.require(git("symbolic-ref", "HEAD").strip() == REF, "Expected PROD2 checkout")
    check_origin()
    release.check_git_write_ready()
    release.require(not any(os.path.lexists(git_dir / name) for name in GIT_OPERATIONS)
                    and not git("for-each-ref", "--format=%(refname)", "refs/replace"),
                    "Production checkout has a Git operation in progress or replacement objects")
    release.require(not any(git_dir.glob("*.lock")) and not any((git_dir / "refs").rglob("*.lock")),
                    "Git metadata lock exists; identify its owner before retrying")
    flags = git("ls-files", "-v", "-z").split("\0")
    release.require(all(row.startswith("H ") for row in flags if row),
                    "Unexpected Git index flags (including assume-unchanged or skip-worktree)")
    release.require(not git("-c", "core.fsmonitor=false", "-c", "core.untrackedCache=false", "--no-optional-locks",
                            "status", "--porcelain", "--untracked-files=all", "--ignore-submodules=none"),
                    "Production checkout is dirty")
    head = git("rev-parse", "--verify", "HEAD").strip()
    release.require(release.SHA.fullmatch(head), "Expected a full Git SHA at HEAD")
    return head


def fetch_tip():
    check_origin()
    release.git("fetch", "--no-tags", "origin", f"{REF}:{TRACKING_REF}")
    sha = release.git("rev-parse", "--verify", f"{TRACKING_REF}^{{commit}}").strip()
    release.require(release.SHA.fullmatch(sha), "Expected the full PROD2 tip SHA")
    return sha


def active_release():
    bundle = release.active_bundle()
    release.safe_path(bundle / "compose.json", private=True)
    release.require((bundle / "compose.json").is_file(), "Active release is missing its Compose manifest")
    manifest = json.loads(protected_file(bundle / "release.json"))
    sha = manifest.get("sha") if isinstance(manifest, dict) else None
    release.require(isinstance(sha, str) and release.SHA.fullmatch(sha), "Invalid active release manifest SHA")
    return bundle, manifest


def rollback_status(manifest):
    previous = manifest.get("previous")
    if isinstance(previous, str):
        bundle = Path(previous)
        if bundle.parent == release.STATE and bundle.name.startswith("rollback-") and bundle.exists():
            release.safe_path(bundle, private=True)
            database = bundle / "SQLite.db"
            if (bundle / "compose.json").is_file() and database.is_file() and database.stat().st_size:
                return f"Retained rollback bundle and SQLite snapshot present at {bundle}."
    return "Active manifest has no retained rollback bundle with a SQLite snapshot."


def changed_paths(before, target):
    try:
        release.git("merge-base", "--is-ancestor", before, target)
    except release.ReleaseError:
        raise release.ReleaseError("Cannot establish a fast-forward from the current release to PROD2") from None
    changes = release.git("diff", "--raw", "--no-ext-diff", "--no-renames", "-z", before, target, "--").split("\0")
    paths = []
    maintenance = []
    for header, name in zip(changes[:-1:2], changes[1::2], strict=True):
        path = name.lower()
        old_mode, new_mode, _, _, status = header[1:].split()
        paths.append((status, name))
        if name == HELPER_PATH and new_mode == "100644" and (
                release.git("hash-object", "--no-filters", str(Path(__file__).absolute())).strip()
                == release.git("rev-parse", f"{target}:{HELPER_PATH}").strip()):
            # The reviewed helper can enter the live tree once it has been installed explicitly.
            continue
        if ((path.startswith(MAINTENANCE_DIRS) and not path.startswith("deploy/security/tests/"))
                or path in MAINTENANCE_FILES
                or path.startswith(("gf3.webapi/appsettings", "dockerfile.", "docker-compose", "compose."))
                or path.rsplit("/", 1)[-1].startswith(("gf3_release", "gf3_guard", "databasemigration"))
                or path.endswith((".service", ".timer", ".socket"))
                or {old_mode, new_mode}.intersection(("120000", "160000"))):
            maintenance.append(name)
    if maintenance:
        print("Changed paths requiring maintenance:", flush=True)
        for name in maintenance:
            print(f"  {ascii(name)}", flush=True)
        raise release.ReleaseError("Separate manual maintenance approval required")
    return paths


def engine(*arguments):
    # Keep the engine's own stage, Git audit and rollback diagnostics on the operator's terminal.
    with subprocess.Popen(("/usr/bin/python3", "-I", str(ENGINE), *map(str, arguments)),
                          env=release.ENV, start_new_session=True, stdin=subprocess.DEVNULL) as process:
        interrupted = False
        while True:
            try:
                return process.wait()
            except KeyboardInterrupt:
                if not interrupted:
                    # Let the engine handle recovery, and keep waiting until it reports the result.
                    process.send_signal(signal.SIGTERM)
                    interrupted = True


def source_env():
    path = release.ROOT / "deploy/.env"
    release.safe_path(path, private=True)
    release.require(path.is_file(), "Production deploy/.env must be a regular file")
    return path.read_bytes()


@contextmanager
def candidate(sha, environment):
    with tempfile.TemporaryDirectory(prefix="preflight-", dir=release.STATE) as directory:
        worktree = Path(directory) / "source"
        env_file = worktree / "deploy/.env"
        release.git("worktree", "add", "--detach", str(worktree), sha)
        copied = False
        try:
            release.safe_path(env_file.parent)
            release.require(not os.path.lexists(env_file), "Target already contains deploy/.env")
            release.write(env_file, environment)
            copied = True
            yield worktree
        finally:
            try:
                if copied:
                    env_file.unlink(missing_ok=True)
            finally:
                release.git("worktree", "remove", "--force", str(worktree))


def verify_health(expected_sha, baseline, nginx_image):
    release.require(release.git("rev-parse", "HEAD").strip() == expected_sha, "Git HEAD does not match the expected SHA")
    bundle, manifest = active_release()
    release.require(manifest["sha"] == expected_sha, "Active manifest does not match the expected SHA")
    release.require(release.topology() == baseline
                    and tuple(manifest.get("baseline", baseline)) == baseline,
                    "Production network or database volume differs from baseline")
    app = release.container("gf3-app")["State"]
    nginx = release.container("nginx")
    release.require(app["Running"] and app.get("Health", {}).get("Status") == "healthy", "App container is unhealthy")
    release.require(nginx["State"]["Running"], "Nginx is not running")
    pinned, = release.docker_json("image", "inspect", nginx_image)
    release.require(nginx["Image"] == pinned["Id"], "Running Nginx does not match the approved image digest")
    health = json.loads(release.curl("https", "/api/health", "--fail"))
    release.require(health.get("status") == "ok" and health.get("canConnect") is True, "HTTPS health check failed")
    release.require(release.timer_state() == (True, True), "Guard timer is not enabled and active")
    release.require((bundle / "guard-enabled").is_file(), "Active release is missing the Guard marker")


def confirm(sha):
    release.require(sys.stdin.isatty(), "Deployment confirmation requires an interactive terminal")
    try:
        return input(f"Deploy PROD2 {sha}? [y/N] ").strip().lower() in ("y", "yes")
    except EOFError:
        return False


def prepare(check_only):
    with release.lock(DEPLOY_LOCK):
        current = check_checkout()
        bundle, manifest = active_release()
        release.require(manifest["sha"] == current,
                        "Git HEAD differs from the active release. Reconcile the managed rollback before deploying.")
        target = fetch_tip()
        if current == target and not check_only:
            print(f"PROD2 is already up to date at {current}.", flush=True)
            return 0
        paths = changed_paths(current, target)
        approval = read_approval()
        environment = source_env()
        baseline = release.topology()
        verify_health(current, baseline, approval[2])
        rollback = rollback_status(manifest)
    ci = verify_ci(target)
    with candidate(target, environment) as worktree:
        release.require(engine("preflight", worktree, target) == 0, "Release engine preflight failed")

    subject = release.git("show", "-s", "--format=%s", target).strip()
    print(f"Current SHA: {current}\nTarget SHA:  {target}\nCommit: {ascii(subject)}")
    print(f"Changed files ({len(paths)}):")
    for status, name in paths:
        print(f"  {status} {ascii(name)}")
    print(f"GitHub CI: success (run {ci[0]}, attempt {ci[1]}; Build and test, Docker image)\n"
          f"Preflight: passed\nPinned Nginx image: {approval[2]}\n"
          f"Active release bundle: {bundle}\nBackup and rollback: {rollback}\n"
          "The engine verifies a fresh SQLite backup and rollback bundle before cutover.",
          flush=True)
    if check_only:
        print("Check complete. Approval and running release were left in place.")
        return 0
    if not confirm(target):
        print("Deployment declined.")
        return 0

    with release.lock(DEPLOY_LOCK):
        release.require(check_checkout() == current and active_release() == (bundle, manifest),
                        "Production release changed after review")
        release.require(read_approval() == approval and source_env() == environment,
                        "Approval settings or production environment changed after review")
        verify_health(current, baseline, approval[2])
        release.verify_tooling(target)
        release.require(verify_ci(target) == ci, "CI run or attempt changed after review")
        release.require(fetch_tip() == target, "Remote PROD2 tip changed after review")
        # Fetch is a Git write; check local state again before committing the approval.
        release.require(check_checkout() == current, "Production checkout changed after review")
        approve(approval, target)

    status = engine("deploy", target, approval[2])
    if status:
        print(f"Managed deployment failed (engine exit {status}). See engine stage and recovery output above.",
              file=sys.stderr, flush=True)
    with release.lock(DEPLOY_LOCK):
        if status:
            try:
                verify_health(current, baseline, approval[2])
                print(f"Production healthy on prior release {current}.", flush=True)
            except (release.ReleaseError, OSError, ValueError, KeyError, AttributeError, TypeError,
                    subprocess.TimeoutExpired) as error:
                detail = str(error) if isinstance(error, release.ReleaseError) else type(error).__name__
                print(f"Prior production health could not be confirmed: {detail}", file=sys.stderr)
            return 1
        print("Verifying deployed production release.", flush=True)
        try:
            verify_health(target, baseline, approval[2])
        except release.ReleaseError as error:
            raise release.ReleaseError(f"Post-deployment verification failed: {error}") from None
    print(f"Deployed PROD2 {target}. Git, manifest, Docker, HTTPS, Guard and topology verified.", flush=True)
    return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check-only", action="store_true", help="Verify CI and preflight without approving or deploying")
    options = parser.parse_args()
    os.umask(0o077)
    signal.signal(signal.SIGTERM, lambda *_: signal.raise_signal(signal.SIGINT))
    release.ENV.update(PATH=SYSTEM_PATH, HOME="/root")
    try:
        release.require(os.geteuid() == 0, "Run with the approved root deployment account")
        release.safe_path(Path(__file__).absolute())
        release.safe_path(ENGINE)
        release.safe_path(release.ROOT)
        release.safe_path(release.STATE, private=True)
        with release.lock(release.STATE / "gfctl-prepare.lock"):
            return prepare(options.check_only)
    except (release.ReleaseError, OSError, ValueError, KeyError, AttributeError, TypeError,
            subprocess.TimeoutExpired, KeyboardInterrupt) as error:
        detail = str(error) if isinstance(error, release.ReleaseError) else f"{type(error).__name__}; inspect local release state"
        print(f"GF3 deployment preparation: {detail}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
