# GF3 PROD2 security deployment

This procedure applies to the root-owned checkout at `/opt/gf`, Docker project `gf3`, and `app-gf.com`. The existing local menu and SSH jail stay under operator control. The version-controlled entrypoint uses an approved PROD2 SHA and a digest-pinned stock Nginx image.

## Release approval

1. Review the security commit on DEV2. Cherry-pick it onto a branch created from the current PROD2 tip and review the complete release diff.
2. Require CI, including both ingress modes, ACME fixtures, and the rollback rehearsal. After merge, record the exact PROD2 tip and confirm its CI. Keep the previously running images and the matching effective Compose configuration available.
3. If the migration tree or startup migration code differs from the deployed revision, stop. The entrypoint rejects that release. Rehearse a migration with a disposable copy, test the old binary against the migrated copy, and agree on separate database recovery before promotion.

## Prepare the host once

Run as root on the production Docker host after independently confirming a second SSH session and provider console access. Keep Certbot and `sshd` Fail2ban enabled. The installer requires Git, Bash, Docker Engine and Compose v2, Python 3, curl, OpenSSL, CA certificates, systemd, logrotate and util-linux. Install missing packages only.

```bash
sudo -i
cd /opt/gf
umask 077
approved_sha='<full reviewed PROD2 commit>'
nginx_image='nginx@sha256:<reviewed nginx image digest>'
git fetch origin PROD2
test "$(git rev-parse origin/PROD2)" = "$approved_sha"
install -d -m 0700 /root/gf3-security-baseline
release_dir="/root/gf3-security-baseline/release-$approved_sha"
git worktree add --detach "$release_dir" "$approved_sha"
install -m 0600 deploy/.env "/root/gf3-security-baseline/deploy.env.$(date -u +%Y%m%dT%H%M%SZ)"
```

Review existing Certbot renewal hooks and the `app-gf.com` lineage. The installer adds a deploy hook, so reconcile any existing hook that also replaces Nginx. Keep `snap.certbot.renew.timer` enabled. File binds require Nginx container recreation after Certbot updates the lineage symlinks. The new hook uses the same locks and pinned active manifest as deployment.

Set these values in the private live environment file. Keep mode 0600, root ownership, current credentials and the actual existing volume name. Remove operator-supplied forwarded-header shortcuts. Inspect the subnet instead of assuming the example remains current.

```dotenv
GF3_SERVER_NAME=app-gf.com
GF3_TRUSTED_PROXY_CIDR=172.18.0.0/16
GF3_TLS_CERT_PATH=/etc/letsencrypt/live/app-gf.com/fullchain.pem
GF3_TLS_KEY_PATH=/etc/letsencrypt/live/app-gf.com/privkey.pem
GF3_ACME_WEBROOT=/var/www/certbot
GF3_REQUIRE_EXISTING_DATABASE=true
GF3_BACKUP_BEFORE_MIGRATE=true
```

```bash
git check-ignore ops/gfctl || printf '%s\n' '/ops/gfctl' >> .git/info/exclude
bash "$release_dir/deploy/security/install-gf3-security.sh" "$release_dir" "$approved_sha"
install -m 0600 deploy/.env "$release_dir/deploy/.env"
/usr/bin/python3 /usr/local/libexec/gf3/gf3_release.py preflight "$release_dir" "$approved_sha"
systemctl is-enabled gf3-guard.timer
fail2ban-client status sshd
systemctl is-active snap.certbot.renew.timer
```

Expected output includes `Installed GF3 host integration ... Guard remains disabled`, `Preflight OK`, and `disabled` for the guard timer. The installer adds local Git excludes so generated logs and state remain ignored even before the hardening commit reaches the live checkout. It refuses conflicting files and removes installed unit and hook files on a handled failure. An abrupt interruption may leave private preparation directories or installed files. Inspect those before retrying. The installer leaves running services and firewall rules alone.

## Integrate the local menu

Back up and inspect `/opt/gf/ops/gfctl`. Replace only the old deploy body reached by case `11` with a call to the installed command below. Keep menu prompts and dispatch intact. Preserve its exit status. Remove the old backup, pull, build and Compose calls from that case so only one transaction owns them.

```bash
/usr/bin/python3 /usr/local/libexec/gf3/gf3_release.py deploy "$approved_sha" "$nginx_image"
```

Read those two values from a root-owned, mode-0600 approval file or the existing approved release selection. Pass them as quoted arguments. The entrypoint checks that the SHA is the exact fetched PROD2 tip, the live branch can fast-forward to it, and the installed deployment entrypoint matches that commit. A change to installed tooling requires an explicit reviewed host update. Avoid running the old code-only rollback action or direct `docker compose up` over a managed release.

## Verify deployment and renewal

The deployment stages Nginx on the existing bridge without publishing ports, checks and retains a rollback bundle, verifies an online SQLite backup, starts the new app, confirms the live proxy environment, recreates Nginx, and checks HTTPS and exact HTTP-01 contents. It then activates the guard and records the approved SHA. During cutover WebSockets may reconnect. A failed cutover restores pinned images, captured environment and mounts, and prior geo files. The database and Docker network are external to the active manifest.

```bash
systemctl list-timers --all gf3-guard.timer snap.certbot.renew.timer
journalctl -u gf3-guard.service -n 20 --no-pager
tail -n 5 /opt/gf/deploy/security/runtime/logs/access.jsonl
curl -fsS --resolve app-gf.com:443:127.0.0.1 https://app-gf.com/api/health
fail2ban-client status sshd
/snap/bin/certbot renew --cert-name app-gf.com --dry-run --run-deploy-hooks
```

Expected health is `{"status":"ok","canConnect":true}`. Certbot should report a successful simulated renewal. Run it outside the deployment lock. The hook verifies the active certificate fingerprint after recreation. From independent IPv4 and any advertised IPv6 client, verify HTTP-01 reachability, HTTPS, and the public peer address in JSONL. IPv6-to-IPv4 translation or an extra upstream proxy can hide the actual source address. Do not enable the web guard on that topology until the edge address contract is corrected. Verify manager/employee login, SignalR, Web Push and export on the real system.

## Recovery

```bash
/usr/bin/python3 /usr/local/libexec/gf3/gf3_release.py rollback /var/lib/gf3-deploy/rollback-REPLACE_WITH_SAVED_BUNDLE
```

Use the bundle printed by the failed or successful deployment. The command checks the recorded network and volume, restores the snapshot, and verifies HTTPS and HTTP-01. After manual rollback the Git checkout may still contain the promoted revision. The active manifest remains authoritative for the guard and Certbot. Verify the active release before another promotion. Keep the database backup off-host. Restoration of SQLite is a separate operator decision that requires a tested restore and explicit accounting for writes since the backup.

## Upgrade tools already installed on the host

Use this procedure for the activation fix after its PR is reviewed and merged and CI passes on the resulting PROD2 SHA. The original installer refuses to replace installed files. The steps below preserve the current stack, the active recovery bundle and the existing `gfctl` menu. Keep the Guard timer disabled throughout this maintenance. The installer-owned Certbot hook continues to invoke `gf3_release.py renew`. It shares the deployment lock and uses the active manifest for both deployed and recovered stacks.

### Incident finding

Release `79133621dc6960d1114b37330e00c7b4f0268238` started a Guard oneshot, took the Guard file lock, then enabled and started the timer under that lock before the Git fast-forward. On a host already up for five minutes, the old timer's `OnBootSec=5min` can fire immediately. The extra Guard process exits successfully with a lock-contention summary. It cannot throw into the deploy process, and the old deploy code did not read that summary. If the journal entry came from that timer start, the timer stage was reached and Git is the next failure candidate. The message alone does not establish the second job's origin, the timer command's exit code or a Git failure.

The old catch covered the initial service start, Guard lock acquisition, HTTPS/TLS/ACME recheck, Guard status, Docker topology, timer enable/start and Git fast-forward. It suppressed the stage and failing command on each path. The original failed stage is therefore undetermined from the supplied evidence. A disposable run reproduced a late Git write failure despite a clean fast-forwardable checkout, and a second run reached the approved Git revision. Check Git write permissions and any existing Git locks before retrying. Preserve the original journal. Do not delete a lock file without establishing which process owns it.

The fixed preflight rejects an existing Git index lock before staging. The final Git check still handles a lock or checkout change that appears during deployment.

On the host as root, use the same shell for the maintenance steps. Check the live HTTPS/ACME baseline and confirm `readlink -f /var/lib/gf3-deploy/active` returns the expected recovery bundle. Do not edit files inside that bundle.

```bash
cd /opt/gf
set -euo pipefail
umask 077
approved_sha='<full reviewed and tested PROD2 SHA>'
[[ "$approved_sha" =~ ^[0-9a-f]{40}$ ]]
git fetch origin PROD2
test "$(git rev-parse origin/PROD2)" = "$approved_sha"
test "$(git branch --show-current)" = PROD2
test -z "$(git status --porcelain)"
test ! -e "$(git rev-parse --git-path index.lock)"
git merge-base --is-ancestor HEAD "$approved_sha"
stage=$(mktemp -d /root/gf3-tool-upgrade.XXXXXXXX)
backup=$(mktemp -d /root/gf3-tool-backup.XXXXXXXX)
tool_dir=/usr/local/libexec/gf3
unit=/etc/systemd/system/gf3-guard.timer
approval=/etc/gfctl/approved-release.conf
exec 9>/run/lock/gf3-prod-deploy.lock
flock -w 120 9
systemctl show gf3-guard.timer --property=ActiveState,UnitFileState
test "$(systemctl show gf3-guard.timer --property=ActiveState --value)" = inactive
test "$(systemctl show gf3-guard.timer --property=UnitFileState --value)" = disabled
exec 8<>/opt/gf/deploy/security/runtime/state/guard.lock
flock -w 120 8
systemctl stop gf3-guard.service
cp -p "$tool_dir"/{gf3_release.py,gf3_guard.py} "$unit" "$approval" "$backup/"
(cd "$backup"; sha256sum gf3_release.py gf3_guard.py gf3-guard.timer approved-release.conf > SHA256SUMS)
for file in gf3_release.py gf3_guard.py gf3-guard.timer; do
    git show "$approved_sha:deploy/security/$file" > "$stage/$file"
    test "$(git hash-object --no-filters "$stage/$file")" = "$(git rev-parse "$approved_sha:deploy/security/$file")"
done
python3 -m py_compile "$stage"/{gf3_release.py,gf3_guard.py}
systemd-analyze verify "$stage/gf3-guard.timer" /etc/systemd/system/gf3-guard.service
```

Stop if a command fails. Install by same-directory rename. If the process fails partway, restore the three saved files by the same method before retrying.

```bash
replace_file() {
    local src=$1 dst=$2 mode=$3 tmp
    tmp=$(mktemp "${dst%/*}/.gf3-update.XXXXXXXX") || return
    install -o root -g root -m "$mode" "$src" "$tmp" && mv -fT -- "$tmp" "$dst"
}
replace_file "$stage/gf3_guard.py" "$tool_dir/gf3_guard.py" 0644
replace_file "$stage/gf3_release.py" "$tool_dir/gf3_release.py" 0644
replace_file "$stage/gf3-guard.timer" "$unit" 0644
systemctl daemon-reload
for path in "$tool_dir/gf3_release.py" "$tool_dir/gf3_guard.py" "$unit"; do
    test "$(git hash-object --no-filters "$path")" = "$(git rev-parse "$approved_sha:deploy/security/${path##*/}")"
    stat -c '%a %U:%G %n' "$path"
done
systemctl show gf3-guard.timer --property=FragmentPath,DropInPaths,NeedDaemonReload,ActiveState,UnitFileState
```

Expect root-owned mode 0644, the approved fragment path, empty `DropInPaths`, `NeedDaemonReload=no`, `ActiveState=inactive` and `UnitFileState=disabled`. Verify the unchanged local menu reads `/etc/gfctl/approved-release.conf`, then edit the SHA value in that file using its existing key and quoting. Preserve the approved Nginx digest and other settings. The repository does not contain the local file, so confirm its actual key name in `gfctl` before editing. Check the file stays root-owned mode 0600 and that its new SHA equals `git rev-parse origin/PROD2`. Keep the backup path.

Release the two locks before the preflight. Use a protected detached checkout for the approved SHA, with a private copy of the live `deploy/.env`. Run the installed preflight against that checkout and SHA as above. Expect `Preflight OK`. It verifies the installed Git blob hashes and the loaded timer fragment before permitting the retry. Review the HTTPS, ACME, Certbot and SSH checks before selecting `[11]`. During the retry, `guard-initial-run`, `guard-status`, `post-guard-health`, `guard-timer-enable`, `guard-timer-start` and `git-fast-forward` identify the final activation stages. Expect the deployed SHA in the success message and verify `git rev-parse HEAD`, the active bundle's `release.json`, and the enabled/active timer. On failure, retain the stage diagnostic and recovery bundle. Do not rerun blindly or reset Git.

```bash
flock -u 8
exec 8>&-
flock -u 9
exec 9>&-
release_dir="$stage/checkout"
git worktree add --detach "$release_dir" "$approved_sha"
install -o root -g root -m 0600 deploy/.env "$release_dir/deploy/.env"
/usr/bin/python3 "$tool_dir/gf3_release.py" preflight "$release_dir" "$approved_sha"
```

To undo the host-tool update before another deployment, take the same locks while the timer is disabled, verify the backup with `(cd "$backup"; sha256sum -c SHA256SUMS)`, restore both Python files and the timer with `replace_file` as above, and restore `approved-release.conf` with mode 0600. Run `systemctl daemon-reload`. Check HTTPS and leave the existing recovery bundle active.

## GO / NO-GO

- GO only with green CI for the final PROD2 SHA, a reviewed image digest, a clean compatible checkout, and a tested previous image/config/volume bundle.
- GO only when the current webroot and renewal hooks are verified, the dedicated bridge matches the effective CIDR, and port 8080 is unpublished.
- GO only when SSH recovery access works, provider exposure on IPv4/IPv6 is understood, and no additional proxy masks client addresses.
- NO-GO if the live `gfctl` option 11 integration has not been reviewed, either renewal or rollback fails, or the candidate includes unapproved migrations.
- Never run volume removal, Compose `down -v`, a firewall flush, or SSH changes from this workflow.
