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

Expected output includes `Installed GF3 host integration ... Guard remains disabled`, `Preflight OK`, and `disabled` for the guard timer. The installer refuses existing conflicting files and removes files it installed on a handled failure. An abrupt interruption may leave private preparation directories or installed files. Inspect those before retrying. The installer leaves running services and firewall rules alone.

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

## GO / NO-GO

- GO only with green CI for the final PROD2 SHA, a reviewed image digest, a clean compatible checkout, and a tested previous image/config/volume bundle.
- GO only when the current webroot and renewal hooks are verified, the dedicated bridge matches the effective CIDR, and port 8080 is unpublished.
- GO only when SSH recovery access works, provider exposure on IPv4/IPv6 is understood, and no additional proxy masks client addresses.
- NO-GO if the live `gfctl` option 11 integration has not been reviewed, either renewal or rollback fails, or the candidate includes unapproved migrations.
- Never run volume removal, Compose `down -v`, a firewall flush, or SSH changes from this workflow.
