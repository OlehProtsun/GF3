# PROD2 database maintenance deployment status

## Decision

**Live installation requires separate review and authorization.** The repository implements the maintenance transaction behind the existing `[11] Deploy PROD2` action. A green disposable rehearsal qualifies the tested revision for review. Production compatibility and installation are distinct operator actions.

## Managed deployment through [11]

The helper detects appended EF migration pairs from the exact PROD2 diff. The ordinary deployment engine retains its migration fingerprint guard. Maintenance accepts ordinary application changes alongside complete generated migration pairs for nullable SQLite `TEXT` and `INTEGER` columns. It independently derives the reviewed column plan from the checked-out migration and compares schema, data and migration history after rehearsal. Unsupported critical files, existing migration edits and unsafe Git file modes block the update.

The helper builds the exact image and runs both the EF startup process and a read-only health host on a private database copy. It prints the SHA, the number of pending migrations, the rehearsal result and the downtime warning. The operator then confirms the maintenance and types the complete target SHA. The engine consumes a protected capability once after checking the exact tip, current release, installed tools, review digest, migration fingerprint, CI attempt and image ID again.

The one-time tooling transition may require a separate `/etc/gfctl/maintenance-tooling-review.json` record. It binds `before` and `target` SHAs to a `blobs` object containing the exact reviewed Git object ID of each changed member of `BOOTSTRAP_PATHS`. It must be root-owned and mode 0600. Routine schema updates derive the column plan from Git and require no per-deploy review file. The optional [13] operation retains its independent clone-only review file.

## Fence and recovery boundary

1. The release engine records a root-only journal at `/var/lib/gf3-deploy/maintenance-active.json`, takes the shared deployment lock and installs a dedicated `inet gf3_maintenance` nftables table. It blocks public inbound HTTP/HTTPS and forwarded access to the existing Docker bridge. GF3 stops Nginx first to close HTTP and SignalR connections, drains Guard, disables container restart policies and stops the old app.
2. The engine checks the kernel rule set, container mounts, restart policies and host SQLite file descriptors and mappings. It then takes a SQLite backup while holding a reserved writer lock. The backup, migration history, original file identities and SHA-256 are recorded in the journal. The full integrity check and logical row comparison must pass before migration. The original named volume and bridge are retained.
3. The candidate runs existing EF startup with no network, no published ports and no hosted background jobs. A separate read-only process then serves only health, using the same volume mounted read-only behind an Nginx instance with no published ports. The engine verifies schema, history, existing rows, TLS, ACME, topology, Guard and Git before it permits ordinary application startup.
4. The durable `WRITES_MAY_RESUME` transition is written **before** starting the ordinary writable application. The kernel fence stays in place through its health, HTTPS and Guard timer checks. The engine removes the fence only after these succeed and checks HTTPS again. Once the marker is written, a failed deployment cannot automatically restore SQLite, even if public ports were still blocked.
5. Before that boundary, a failure can restore the final backup if Git is still at the previous release, the kernel fence is intact, all writers are stopped and logical comparison with the backup finds no unrelated data changes. The journal consumes the restore transition before modifying the database. The engine retains the failed database and its journals, atomically installs the checksum-verified replacement, checks the original history and integrity, then verifies and reopens the prior release. Failed or unprovable recovery leaves `RECOVERY_REQUIRED` and the evidence on disk. [12] is code rollback only and refuses to cross a migration boundary.

An unfinished journal blocks managed deploy, code rollback and certificate renewal after process exit or reboot. A reboot can clear nftables state. The pre-open containers have restart policy `no`, and the engine never infers a safe rollback from the journal alone. Reconcile the stored phase, actual firewall, Git, Docker and SQLite state through a controlled incident procedure. In particular, do not delete the journal or restore a retained SQLite snapshot after the write boundary. The Linux disposable rehearsal simulates kernel fence loss and Docker restart separately from ordinary failure recovery.

## Why the ordinary path stays guarded

The prepare helper used to stop database-sensitive changes during diff review. The release engine separately rejects a changed migration fingerprint during ordinary staging. Normal cutover uses an online SQLite backup and restores the previous app and proxy on failure. The normal recovery code cannot undo startup EF migrations. The maintenance transaction uses a separate quiesced backup, its own recovery rules, and verifies the candidate before public cutover.

The first supported PROD2 migration pair adds `shift_correction_request.comment` (`TEXT`) and `shift_swap_request.refreshed_at_utc` (`INTEGER`). Additional releases qualify when the target contains complete append-only generated migration pairs for supported nullable columns, changes to the related model files listed in `gf3_maintenance.py`, and regular files allowed by the complete-diff review. Changes to existing migrations, database startup logic, deployment or infrastructure outside the bootstrap allowlist, and unsafe Git modes stop deployment.

## Install the reviewed host tools

Schedule a separate authorized root tooling window. The existing one-click runbook must already be installed with [11] invoking the protected libexec helper. Either reviewed menu checksum below is accepted. If the current menu uses a checkout-local helper or has another checksum, start with a separately reviewed transition using [the one-click rollout](gfctl-prod2-rollout.md). The commands below expect the managed Ubuntu layout, nftables, local Docker volumes, an active Guard timer, a known running release, and a clean checkout. Coordinate any other privileged automation before starting.

Wait for a reviewed merge and successful **PROD2 push CI** on the exact currently fetched target. Verify all three required jobs: Build and test, Playwright smoke, and Docker image. Draft PR CI is only rehearsal evidence. Separately provide `/root/gf3-maintenance-reviewed` (root-owned, mode 0700) containing these three root-owned mode-0600 files:

- `PROD2.sha` with one line containing the full reviewed target.
- `maintenance-tooling-review.json` with `before`, `target` and `blobs` as described above. Include each changed bootstrap path and its exact target Git blob ID. Compare against the actual active release, including any application changes that accompany the migrations.
- `SHA256SUMS` with one standard `sha256sum` text entry for each of `gf3_release.py`, `gf3_prepare_release.py`, `gf3_maintenance.py`, `Program.cs`, `gfctl-prod2-maintenance-menu.patch` and `maintenance-tooling-review.json`. Obtain it independently from the tooling review.

Use a root Bash session. Run this shared block for either installation or tool rollback.

```bash
set -euo pipefail
umask 077
test "$(id -u)" -eq 0
cd /opt/gf
state=/var/lib/gf3-deploy
lib=/usr/local/libexec/gf3
policy=/etc/gfctl/maintenance-tooling-review.json
tools=(gf3_release.py gf3_prepare_release.py gf3_maintenance.py)
paths=("$lib/gf3_release.py" "$lib/gf3_prepare_release.py" "$lib/gf3_maintenance.py" "$policy")
protected() {
    test "$(readlink -e -- "$1")" = "$1" &&
        test "$(stat -c %u "$1")" = 0 &&
        test -z "$(find "$1" -maxdepth 0 -perm /022 -print)"
}
lock_tools() {
    for p in /opt /opt/gf /etc /etc/gfctl /usr /usr/local /usr/local/libexec "$lib" /var /var/lib "$state" "$state/gfctl-prepare.lock" /run/lock/gf3-prod-deploy.lock; do
        protected "$p"
    done
    test "$(stat -c '%u:%a' "$state")" = 0:700
    test -f "$state/gfctl-prepare.lock"
    test -f /run/lock/gf3-prod-deploy.lock
    exec 8<>"$state/gfctl-prepare.lock"
    flock -w 120 8
    exec 9<>/run/lock/gf3-prod-deploy.lock
    flock -w 120 9
    test ! -e "$state/maintenance-active.json" && test ! -L "$state/maintenance-active.json"
}
unlock_tools() { exec 9>&-; exec 8>&-; }
replace_file() {
    local source=$1 destination=$2 mode=$3 pending
    if [[ -e "$destination" || -L "$destination" ]]; then
        protected "$destination" && test -f "$destination" || return 1
    fi
    pending=$(mktemp "$(dirname "$destination")/.gf3-install.XXXXXXXX") || return 1
    if install -o root -g root -m "$mode" -- "$source" "$pending" && mv -fT -- "$pending" "$destination"; then
        return 0
    fi
    rm -f -- "$pending"
    return 1
}
```

Run the installation block in that same shell. It checks the staged helper against the live layout before replacing existing Python tools. Dry runs may fetch Git refs, make and remove a detached worktree and query external health. They do not approve or deploy a release. The menu, Guard definitions and existing approval file stay in place. If any check fails after the rollback bundle is created, stop and use the rollback section before allowing deployment.

```bash
review=/root/gf3-maintenance-reviewed
for p in "$review" "$review/PROD2.sha" "$review/SHA256SUMS" "$review/maintenance-tooling-review.json"; do protected "$p"; done
test "$(stat -c '%u:%a' "$review")" = 0:700
for p in "$review/PROD2.sha" "$review/SHA256SUMS" "$review/maintenance-tooling-review.json"; do
    test -f "$p"
    test "$(stat -c '%u:%a' "$p")" = 0:600
done
read -r sha < "$review/PROD2.sha"
[[ "$sha" =~ ^[0-9a-f]{40}$ ]]
command -v nft >/dev/null
for p in /usr /usr/local /usr/local/bin; do protected "$p"; done
test "$(stat -c %u /usr/local/bin/gfctl)" = 0
menu=$(readlink -e /usr/local/bin/gfctl)
case "$menu" in
    /opt/gf/ops/gfctl)
        for p in /opt /opt/gf /opt/gf/ops; do protected "$p"; done ;;
    /usr/local/bin/gfctl) ;;
    *) printf 'Unreviewed gfctl path\\n' >&2; exit 1 ;;
esac
protected "$menu"
bash -n "$menu"
case "$(sha256sum "$menu" | cut -d' ' -f1)" in
    582200f62900359d7e7593ba430c3d35ce10129e47842826bd585bab9b7b995a|b54f28e0e4e4a36c2296e96e069d0f4cb0a179eb18847fda91eb8d88e6759313) ;;
    *) printf 'Unreviewed gfctl menu\n' >&2; exit 1 ;;
esac
lock_tools
case "$(git remote get-url --all origin)" in
    https://github.com/OlehProtsun/GF3|https://github.com/OlehProtsun/GF3.git|git@github.com:OlehProtsun/GF3|git@github.com:OlehProtsun/GF3.git|ssh://git@github.com/OlehProtsun/GF3|ssh://git@github.com/OlehProtsun/GF3.git) ;;
    *) printf 'Unreviewed Git origin\n' >&2; exit 1 ;;
esac
test -z "$(git --no-optional-locks status --porcelain --untracked-files=all)"
git fetch --no-tags origin refs/heads/PROD2:refs/remotes/origin/PROD2
test "$(git rev-parse refs/remotes/origin/PROD2)" = "$sha"
test "$(git symbolic-ref HEAD)" = refs/heads/PROD2
approval_before=$(sha256sum /etc/gfctl/approved-release.conf)
stage=$(mktemp -d /root/gf3-maintenance-stage.XXXXXXXX)
backup=$(mktemp -d /root/gf3-maintenance-backup.XXXXXXXX)
printf 'Tool rollback bundle: %s\n' "$backup"
git rev-parse HEAD > "$backup/HEAD"
for dst in "${paths[@]}"; do
    name=$(basename "$dst")
    if [[ -e "$dst" || -L "$dst" ]]; then
        protected "$dst"
        test -f "$dst"
        stat -c %a "$dst" > "$backup/$name.mode"
        install -o root -g root -m 0600 "$dst" "$backup/$name"
    else
        touch "$backup/$name.absent"
    fi
done
(cd "$backup" && sha256sum -- * > SHA256SUMS && sha256sum -c --strict SHA256SUMS)
for file in "${tools[@]}" gfctl-prod2-maintenance-menu.patch; do
    path="deploy/security/$file"
    git show "$sha:$path" > "$stage/$file"
    test "$(git hash-object --no-filters "$stage/$file")" = "$(git rev-parse "$sha:$path")"
done
git show "$sha:GF3.WebApi/Program.cs" > "$stage/Program.cs"
install -m 0600 "$review/maintenance-tooling-review.json" "$stage/maintenance-tooling-review.json"
test "$(awk 'END { print NR }' "$review/SHA256SUMS")" -eq 6
for name in "${tools[@]}" Program.cs gfctl-prod2-maintenance-menu.patch maintenance-tooling-review.json; do
    test "$(awk -v file="$name" '$2 == file { count++ } END { print count+0 }' "$review/SHA256SUMS")" -eq 1
done
(cd "$stage" && sha256sum -c --strict "$review/SHA256SUMS")
for dst in "$lib/gf3_guard.py" /etc/systemd/system/gf3-guard.service /etc/systemd/system/gf3-guard.timer; do
    protected "$dst"
    test "$(git hash-object --no-filters "$dst")" = "$(git rev-parse "$sha:deploy/security/$(basename "$dst")")"
done
install -m 0644 "$lib/gf3_guard.py" "$stage/gf3_guard.py"
systemd-analyze verify /etc/systemd/system/gf3-guard.service /etc/systemd/system/gf3-guard.timer
python3 -m py_compile "$stage/gf3_release.py" "$stage/gf3_prepare_release.py" "$stage/gf3_maintenance.py"
replace_file "$stage/maintenance-tooling-review.json" "$policy" 0600
unlock_tools
/usr/bin/python3 -I "$stage/gf3_prepare_release.py" --check-only
lock_tools
test "$(git rev-parse HEAD)" = "$(cat "$backup/HEAD")"
test "$(git rev-parse refs/remotes/origin/PROD2)" = "$sha"
test -z "$(git --no-optional-locks status --porcelain --untracked-files=all)"
cmp -s "$policy" "$stage/maintenance-tooling-review.json"
(cd "$stage" && sha256sum -c --strict "$review/SHA256SUMS")
for file in "${tools[@]}"; do
    replace_file "$stage/$file" "$lib/$file" 0644
    cmp -s "$stage/$file" "$lib/$file"
done
unlock_tools
/usr/bin/python3 -I "$lib/gf3_prepare_release.py" --check-only
test "$(sha256sum /etc/gfctl/approved-release.conf)" = "$approval_before"
```

### Roll back the tooling installation

Use a fresh root Bash session with the shared block above. Select the recorded rollback bundle. This procedure is only for a failed installation before the production release advances or a maintenance transaction begins. An unfinished journal or changed Git release stops it. A release rollback requires the recovery procedure below.

```bash
backup=/root/gf3-maintenance-backup.REPLACE_WITH_RECORDED_BUNDLE
protected "$backup"
test "$(stat -c '%u:%a' "$backup")" = 0:700
lock_tools
test "$(git rev-parse HEAD)" = "$(cat "$backup/HEAD")"
(cd "$backup" && sha256sum -c --strict SHA256SUMS)
for dst in "${paths[@]}"; do
    name=$(basename "$dst")
    if [[ -f "$backup/$name.absent" ]]; then
        if [[ -e "$dst" || -L "$dst" ]]; then protected "$dst"; rm -- "$dst"; fi
    else
        mode=$(cat "$backup/$name.mode")
        [[ "$mode" =~ ^[0-7]{3,4}$ ]]
        replace_file "$backup/$name" "$dst" "$mode"
        cmp -s "$backup/$name" "$dst"
    fi
done
unlock_tools
```

## Deploy and inspect the result

In an approved deployment window, launch `sudo /usr/local/bin/gfctl` and choose `[11] Deploy PROD2`. Ordinary releases use the existing prompts and cutover. For a supported migration, review the exact SHA, pending migrations, isolated rehearsal result and downtime warning. Confirm and enter the full SHA. Downtime begins when production ingress is fenced after approval. Its duration depends on SQLite size, the health checks and any recovery. Keep the second operator session available.

Verify the reported SHA and HTTPS health, then compare Git HEAD with the root-only active release manifest under `/var/lib/gf3-deploy/active`. The final backup is `final.db` in the printed maintenance bundle. The completed or recovered bundle stores the recorded backup digest and original EF history in `transaction.json`. For an unfinished transaction, the root-only `/var/lib/gf3-deploy/maintenance-active.json` is the active record. Use disposable copies for offline backup inspection. Compare SHA-256 with the journal and run `PRAGMA integrity_check;` and `SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;`. Limit output to integrity results, migration IDs and release metadata because the backups contain production employee data.

For a failed or interrupted deployment, start with the engine outcome, active journal, actual firewall, Docker process and mount state, Git HEAD and the retained bundle. Pause competing privileged automation. A verified automatic recovery reopens the previous app and keeps evidence of the attempt. `RECOVERY_REQUIRED` or an unfinished journal needs incident review before reopening traffic. If `writes_may_resume` is true, use controlled forward repair or a data recovery plan that accounts for subsequent writes. Do not remove the journal, restart writers or manually restore the backup as a shortcut. The optional [13] remains a clone-only diagnostic with its own review file and cannot approve a live release.

The disposable Linux CI uses real Docker, nftables, Nginx and SQLite with prior and candidate images. Guard timer operations use a test double. The fence-loss rehearsal restarts Docker, and process-death coverage verifies the journal persists. A full guest OS reboot and production-specific installed Guard/host compatibility require separate verification.

## Required sequence for the reviewed seven-migration candidate

This is an operator handoff. The coding session did not connect to PROD2, install host tools, or write to a live database.

1. Before merge or deployment, an operator reads the protected active-release manifest on the host and records its full Git SHA, image and named volume, installed tool hashes, EF history, application health, and available disk space. Validate a separately retained backup outside the affected volume. Use a private clone or sanitized disposable fixture for rehearsal. Stop if the host baseline SHA or schema differs from `1175c39461e149a7bb35f428babed28279727899`; do not infer the workstation database as host state.
2. After separate authorization and reviewed merge to `PROD2`, require successful PROD2 **push** CI for the exact final merge SHA: Build and test, Playwright smoke, and Docker image. Build the protected tool package from that full SHA. Independently review the exact `before`, `target`, migration source/Designer blobs, complete critical-input diff and hashes, and package `SHA256SUMS`; use host-derived values and do not reuse workstation guesses.
3. In a separately authorized tooling window, install the reviewed package with the existing protected installer and verify its hashes and health. Run the existing rehearsal on a private clone of the actual baseline. Confirm the full seven-migration schema/data contract, combined SQLite/media checkpoint, rollback before writes resume, no rollback after the write boundary, isolated migration/health modes, HTTPS and Guard checks. Only after every gate passes should the operator provide the full target SHA and exact confirmation to `[11]`.
4. `[11]` fences ingress and writers, creates and validates the final full media/database snapshot, runs isolated EF migrate and read-only health, records the write-resume boundary, then reopens the app. Check Git/image/database history/media hashes and permissions, HTTPS and Guard. Before the boundary, recovery is allowed only under the verified fence. After it, never restore an older snapshot automatically. Keep old snapshots pending an approved retention policy. A code review or CI result is not live deployment authorization.
