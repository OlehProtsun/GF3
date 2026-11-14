# Install the one-click PROD2 menu

This is a one-time Ubuntu host maintenance procedure for an existing managed release with a healthy app and an enabled, active Guard timer. Use the root shell and keep a second SSH session available. Wait for the reviewed PR to merge and for the exact PROD2 merge commit to pass the push-triggered CI and both required jobs. Record that full commit SHA from GitHub. Direct access to the public GitHub REST API is required. The helper uses no GitHub token.

The host-local menu accepts one change to the shared deploy function. The helper requires Git HEAD and the active manifest to agree and requires the running Nginx image to match the configured digest. Bring any mismatched or partially recovered host through the existing production runbook before proceeding. [GF3 production operations](security-production.md)

## Verify the source and back up the menu

Run the blocks in the same Bash session as root. Stop on any error. Replace the placeholder with the reviewed merge SHA. Fetch updates the remote-tracking ref only.

```bash
set -euo pipefail
umask 077
test "$(id -u)" -eq 0
cd /opt/gf
reviewed_sha='<full reviewed PROD2 merge SHA>'
[[ "$reviewed_sha" =~ ^[0-9a-f]{40}$ ]]
git fetch --no-tags origin refs/heads/PROD2:refs/remotes/origin/PROD2
git merge-base --is-ancestor "$reviewed_sha" refs/remotes/origin/PROD2

menu=/opt/gf/ops/gfctl
helper=/usr/local/libexec/gf3/gf3_prepare_release.py
approval=/etc/gfctl/approved-release.conf
state=/var/lib/gf3-deploy
deploy_lock=/run/lock/gf3-prod-deploy.lock
backup=$(mktemp -d /root/gfctl-prod2-backup.XXXXXXXX)
stage=$(mktemp -d /root/gfctl-prod2-stage.XXXXXXXX)
printf 'Backup directory: %s\nStaging directory: %s\n' "$backup" "$stage"

for dir in /opt/gf/ops /usr/local/libexec/gf3 /etc/gfctl "$state"; do
    test ! -L "$dir"
    test "$(stat -c %u "$dir")" = 0
    test -z "$(find "$dir" -maxdepth 0 -perm /022 -print)"
done
test "$(stat -c '%u:%a' "$state")" = 0:700
test -f "$deploy_lock"
test ! -L "$deploy_lock"
test "$(stat -c %u "$deploy_lock")" = 0
exec 8<>"$state/gfctl-prepare.lock"
flock -w 120 8
exec 9<>"$deploy_lock"
flock -w 120 9

test -f "$menu"
test ! -L "$menu"
test "$(stat -c '%u:%g' "$menu")" = 0:0
test -x "$menu"
test -z "$(find "$menu" -perm /022 -print)"
test -f "$approval"
test ! -L "$approval"
test "$(stat -c '%u:%a' "$approval")" = 0:600
git check-ignore --quiet ops/gfctl
test -z "$(git ls-files -- ops/gfctl)"
printf '%s  %s\n' 557ddb6dbfb56a884be1be8bae53574b42ed923a6928bb8bf42d8a3ae89d9c25 "$menu" | sha256sum -c -

menu_mode=$(stat -c %a "$menu")
printf '%s\n' "$menu_mode" > "$backup/menu.mode"
install -o root -g root -m 0600 "$menu" "$backup/gfctl"
install -o root -g root -m 0600 "$approval" "$backup/approved-release.conf"
if [[ -e "$helper" || -L "$helper" ]]; then
    test -f "$helper"
    test ! -L "$helper"
    test "$(stat -c '%u:%g' "$helper")" = 0:0
    test -z "$(find "$helper" -perm /022 -print)"
    stat -c %a "$helper" > "$backup/helper.mode"
    install -o root -g root -m 0600 "$helper" "$backup/gf3_prepare_release.py"
    helper_backup=(helper.mode gf3_prepare_release.py)
else
    touch "$backup/helper-was-absent"
    helper_backup=(helper-was-absent)
fi
(cd "$backup"; sha256sum gfctl approved-release.conf menu.mode "${helper_backup[@]}" > SHA256SUMS)
(cd "$backup"; sha256sum -c SHA256SUMS)

for file in gf3_prepare_release.py gfctl-prod2-menu.patch; do
    source_path="deploy/security/$file"
    git show "$reviewed_sha:$source_path" > "$stage/$file"
    test "$(git hash-object --no-filters "$stage/$file")" = "$(git rev-parse "$reviewed_sha:$source_path")"
done
for pair in \
    /usr/local/libexec/gf3/gf3_release.py \
    /usr/local/libexec/gf3/gf3_guard.py \
    /etc/systemd/system/gf3-guard.service \
    /etc/systemd/system/gf3-guard.timer; do
    test "$(git hash-object --no-filters "$pair")" = "$(git rev-parse "$reviewed_sha:deploy/security/${pair##*/}")"
done

patch --batch --forward --fuzz=0 -o "$stage/gfctl" "$backup/gfctl" "$stage/gfctl-prod2-menu.patch"
bash -n "$stage/gfctl"
python3 -m py_compile "$stage/gf3_prepare_release.py"
printf '%s  %s\n' 582200f62900359d7e7593ba430c3d35ce10129e47842826bd585bab9b7b995a "$stage/gfctl" | sha256sum -c -
sha256sum "$stage/gf3_prepare_release.py"
```

If the original menu checksum differs, stop and compare it with the supplied menu. Get a patch reviewed against the actual host copy before installing.

## Install and check

Install the helper first and then atomically replace the menu. Both temporary files are created in the destination directories. Keep the locks until both replacements and checksum comparisons finish. If a command fails during replacement, use the rollback below before using the menu.

```bash
replace_file() {
    local src=$1 dst=$2 mode=$3 pending
    pending=$(mktemp "${dst%/*}/.gfctl-install.XXXXXXXX") || return
    if install -o root -g root -m "$mode" -- "$src" "$pending" && mv -fT -- "$pending" "$dst"; then
        return 0
    fi
    rm -f -- "$pending"
    return 1
}

replace_file "$stage/gf3_prepare_release.py" "$helper" 0644
replace_file "$stage/gfctl" "$menu" "$menu_mode"
cmp -s "$helper" "$stage/gf3_prepare_release.py"
cmp -s "$menu" "$stage/gfctl"
cmp -s "$approval" "$backup/approved-release.conf"
git --no-optional-locks status --short
exec 9>&-
exec 8>&-

/usr/bin/python3 -I "$helper" --check-only
cmp -s "$approval" "$backup/approved-release.conf"
```

Expect the exact target SHA, its CI attempt, passing preflight and the pinned Nginx digest. Check-only also verifies CI and preflight when production is already current. It fetches Git refs and creates a private temporary worktree, removed on exit. It neither writes approval nor starts a release. Stop after the check during installation.

## Routine operation and failure handling

On a later release, start `gfctl`, select `[11] Deploy PROD2`, review the SHA, changed paths, CI, preflight, pinned digest and rollback baseline, and enter `y` if acceptable. Empty input defaults to No. The helper rechecks state and CI before atomically updating the approval SHA and invoking the installed engine. The current approval is backed up under `/var/lib/gf3-deploy/approval-*/`. The engine performs a fresh SQLite backup before cutover.

Changes to deployment and security tooling, CI, Compose, proxy or TLS configuration, and database startup or migrations require reviewed manual maintenance. The helper itself can enter the live tree when it exactly matches the explicitly installed copy. If a deployment fails, use the printed stage, retained bundle and Git activation audit to investigate, and verify the reported production health before deciding on any next action. The approved SHA records the attempt. Git and the active manifest identify the running release. For application recovery use the existing managed rollback procedure.

## Roll back the menu upgrade

Restore the menu before removing or restoring its helper. Run this block as root in a fresh Bash session if the install failed. Set `backup` to the root-only directory printed above. This rolls back the host tools only. An earlier deployment and its approval record remain in place.

```bash
set -euo pipefail
umask 077
backup='<root-only backup directory printed during installation>'
menu=/opt/gf/ops/gfctl
helper=/usr/local/libexec/gf3/gf3_prepare_release.py
deploy_lock=/run/lock/gf3-prod-deploy.lock
test "$(id -u)" -eq 0
test ! -L "$backup"
test "$(stat -c '%u:%a' "$backup")" = 0:700
(cd "$backup"; sha256sum -c SHA256SUMS)
bash -n "$backup/gfctl"
menu_mode=$(cat "$backup/menu.mode")
[[ "$menu_mode" =~ ^[0-7]{3,4}$ ]]

test -f "$deploy_lock"
test ! -L "$deploy_lock"
test "$(stat -c %u "$deploy_lock")" = 0
exec 8<>/var/lib/gf3-deploy/gfctl-prepare.lock
flock -w 120 8
exec 9<>"$deploy_lock"
flock -w 120 9

pending=$(mktemp "/opt/gf/ops/.gfctl-restore.XXXXXXXX")
install -o root -g root -m "$menu_mode" "$backup/gfctl" "$pending"
mv -fT -- "$pending" "$menu"
cmp -s "$menu" "$backup/gfctl"
if [[ -f "$backup/helper-was-absent" ]]; then
    rm -f -- "$helper"
else
    helper_mode=$(cat "$backup/helper.mode")
    [[ "$helper_mode" =~ ^[0-7]{3,4}$ ]]
    pending=$(mktemp "/usr/local/libexec/gf3/.gfctl-restore.XXXXXXXX")
    install -o root -g root -m "$helper_mode" "$backup/gf3_prepare_release.py" "$pending"
    mv -fT -- "$pending" "$helper"
    cmp -s "$helper" "$backup/gf3_prepare_release.py"
fi
exec 9>&-
exec 8>&-
```
