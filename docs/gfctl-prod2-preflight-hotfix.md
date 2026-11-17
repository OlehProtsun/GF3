# Install the PROD2 preflight hotfix

Schedule a reviewed root tooling window on the managed Ubuntu host. Review the full diff from the actual active release to the hotfix merge commit. Record both full SHAs in the maintenance ticket. Wait for that exact merge commit to be the PROD2 tip and for its **push** CI jobs **Build and test** and **Docker image** to succeed.

This procedure applies to an ordinary release from the already-installed database maintenance tooling. The ordinary helper accepts only the regular-file engine transition from blob `5d24a4a03ae6ed3ed354bfed5c27e812954483b9` to `45aecfd170368512a92371f1916157147fd208c3`, with the new bytes installed. Changes to other protected paths follow the existing review rules.

If the reviewed diff includes pending supported migrations or the original database maintenance bootstrap, use the [database maintenance tooling installation and rollback](gfctl-prod2-database-maintenance.md#install-the-reviewed-host-tools) from the hotfix revision. Prepare its separately reviewed SHA and checksum manifest from that revision. The root-only `maintenance-tooling-review.json` must name the actual active SHA as `before`, the hotfix merge SHA as `target`, and map each changed bootstrap path in `blobs` to its exact target Git blob. Include both changed Python tools when applicable. After that installation, expect `passed maintenance preflight` from `--check-only`. The interactive migration rehearsal and full-SHA approval remain required for deployment.

## Install

Use a root Bash session. First run the shared tooling setup block in [the maintenance runbook](gfctl-prod2-database-maintenance.md#install-the-reviewed-host-tools), which defines `protected`, `lock_tools`, `unlock_tools`, and `replace_file`. Confirm the installed menu is one of that runbook's two reviewed menu checksums and [11] invokes the libexec helper. Then run this block with the two independently reviewed SHAs. Keep the window exclusive until the installed check finishes.

```bash
before='<full reviewed active PROD2 SHA>'
sha='<full reviewed hotfix merge SHA>'
[[ "$before" =~ ^[0-9a-f]{40}$ && "$sha" =~ ^[0-9a-f]{40}$ && "$before" != "$sha" ]]
lock_tools
case "$(git remote get-url --all origin)" in
    https://github.com/OlehProtsun/GF3|https://github.com/OlehProtsun/GF3.git|git@github.com:OlehProtsun/GF3|git@github.com:OlehProtsun/GF3.git|ssh://git@github.com/OlehProtsun/GF3|ssh://git@github.com/OlehProtsun/GF3.git) ;;
    *) printf 'Unreviewed Git origin\n' >&2; exit 1 ;;
esac
git fetch --no-tags origin refs/heads/PROD2:refs/remotes/origin/PROD2
test "$(git rev-parse refs/remotes/origin/PROD2)" = "$sha"
test "$(git symbolic-ref HEAD)" = refs/heads/PROD2
test "$(git rev-parse HEAD)" = "$before"
git merge-base --is-ancestor "$before" "$sha"
active=$(readlink -e "$state/active")
case "$active" in "$state"/*) ;; *) exit 1 ;; esac
protected "$active/release.json"
test "$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["sha"])' "$active/release.json")" = "$before"
test -z "$(git --no-optional-locks status --porcelain --untracked-files=all)"
test -z "$(git diff --name-only "$before" "$sha" -- DataAccessLayer/Migrations)"

engine=deploy/security/gf3_release.py
test "$(git rev-parse "$before:$engine")" = 5d24a4a03ae6ed3ed354bfed5c27e812954483b9
test "$(git rev-parse "$sha:$engine")" = 45aecfd170368512a92371f1916157147fd208c3
hotfix_tools=(gf3_release.py gf3_prepare_release.py)
approval=/etc/gfctl/approved-release.conf
protected "$approval"
test "$(stat -c '%u:%a' "$approval")" = 0:600
backup=$(mktemp -d /root/gf3-preflight-backup.XXXXXXXX)
stage=$(mktemp -d /root/gf3-preflight-stage.XXXXXXXX)
printf 'Tool rollback bundle: %s\n' "$backup"
printf '%s\n' "$before" > "$backup/HEAD"
printf '%s\n' "$sha" > "$backup/target"
sha256sum "$approval" > "$backup/approval.sha256"
for name in "${hotfix_tools[@]}"; do
    protected "$lib/$name"
    test -f "$lib/$name"
    test "$(stat -c '%u:%g:%a' "$lib/$name")" = 0:0:644
    test "$(git hash-object --no-filters "$lib/$name")" = "$(git rev-parse "$before:deploy/security/$name")"
    install -m 0600 "$lib/$name" "$backup/$name"
    git show "$sha:deploy/security/$name" > "$stage/$name"
    test "$(git hash-object --no-filters "$stage/$name")" = "$(git rev-parse "$sha:deploy/security/$name")"
done
(cd "$backup" && sha256sum HEAD target approval.sha256 "${hotfix_tools[@]}" > SHA256SUMS)
for dst in "$lib/gf3_maintenance.py" "$lib/gf3_guard.py" /etc/systemd/system/gf3-guard.{service,timer}; do
    protected "$dst"
    test -f "$dst"
    test "$(git hash-object --no-filters "$dst")" = "$(git rev-parse "$sha:deploy/security/${dst##*/}")"
done
python3 -m py_compile "$stage/gf3_release.py" "$stage/gf3_prepare_release.py"
(cd "$backup" && sha256sum -c --strict SHA256SUMS)
for name in "${hotfix_tools[@]}"; do
    replace_file "$stage/$name" "$lib/$name" 0644
    test "$(git hash-object --no-filters "$lib/$name")" = "$(git rev-parse "$sha:deploy/security/$name")"
done
unlock_tools
```

## Validate and stop before approval

```bash
/usr/bin/python3 -I "$lib/gf3_prepare_release.py" --check-only
sha256sum -c --strict "$backup/approval.sha256"
test "$(git rev-parse HEAD)" = "$before"
test -z "$(git --no-optional-locks status --porcelain --untracked-files=all)"
systemctl is-enabled gf3-guard.timer
systemctl is-active gf3-guard.timer
```

Expect the reviewed target, the successful CI attempt, `Preflight OK`, and a retained rollback bundle with a SQLite snapshot. For an interactive validation, open `gfctl`, select [11], verify the same target and preflight result, and answer `n`. If installation or validation fails, keep the window open and roll back the tools before another deployment attempt.

## Roll back the tools before deploying

In a fresh root Bash session, rerun the shared tooling setup block linked above. Use the recorded backup directory. If the active release advanced, approval changed, or a maintenance transaction began, use the [managed recovery runbook](security-production.md) for the actual release state.

```bash
backup='<recorded /root/gf3-preflight-backup directory>'
protected "$backup"
test "$(stat -c '%u:%a' "$backup")" = 0:700
lock_tools
hotfix_tools=(gf3_release.py gf3_prepare_release.py)
for name in HEAD target approval.sha256 SHA256SUMS "${hotfix_tools[@]}"; do
    protected "$backup/$name"
    test "$(stat -c '%u:%a' "$backup/$name")" = 0:600
done
(cd "$backup" && sha256sum -c --strict SHA256SUMS)
before=$(cat "$backup/HEAD")
sha=$(cat "$backup/target")
[[ "$before" =~ ^[0-9a-f]{40}$ && "$sha" =~ ^[0-9a-f]{40}$ ]]
test "$(git rev-parse HEAD)" = "$before"
active=$(readlink -e "$state/active")
case "$active" in "$state"/*) ;; *) exit 1 ;; esac
protected "$active/release.json"
test "$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["sha"])' "$active/release.json")" = "$before"
sha256sum -c --strict "$backup/approval.sha256"
for name in "${hotfix_tools[@]}"; do
    protected "$lib/$name"
    old=$(git rev-parse "$before:deploy/security/$name")
    new=$(git rev-parse "$sha:deploy/security/$name")
    test "$(git hash-object --no-filters "$backup/$name")" = "$old"
    installed=$(git hash-object --no-filters "$lib/$name")
    [[ "$installed" = "$old" || "$installed" = "$new" ]]
done
for name in gf3_prepare_release.py gf3_release.py; do
    replace_file "$backup/$name" "$lib/$name" 0644
    cmp -s "$backup/$name" "$lib/$name"
done
unlock_tools
systemctl is-active gf3-guard.timer
curl -fsS https://app-gf.com/api/health
```

Restoring the earlier engine also restores the preflight failure. The running release and its database remain in place.
