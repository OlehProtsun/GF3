# GF3 production ingress hardening

For the `/opt/gf` PROD2 server and `gfctl` option 11, follow the [production integration runbook](security-production.md).

## Inspect and back up the current deployment

Run from the real Compose project root. Adjust the Compose file list to match the live deployment and keep a working SSH session and provider rescue console available.

```bash
cd /opt/gf3
compose=(docker compose --env-file deploy/.env -f deploy/docker-compose.yml -f deploy/docker-compose.https.yml)
umask 077
backup=$(mktemp -d "$HOME/gf3-pre-hardening.XXXXXX")
git rev-parse HEAD > "$backup/revision"
"${compose[@]}" ps
"${compose[@]}" config --no-interpolate --no-env-resolution > "$backup/compose.redacted.yml"
cp deploy/.env deploy/docker-compose.yml deploy/docker-compose.https.yml "$backup/"
cp -a deploy/nginx "$backup/"
"${compose[@]}" exec -T nginx nginx -T > "$backup/nginx.rendered.conf" 2>&1
"${compose[@]}" images
docker network inspect gf3_gf3-internal
df -h /opt/gf3 "$backup"
sudo ss -lntp
openssl x509 -in deploy/certs/fullchain.pem -noout -subject -dates
```

Review the redacted config privately. If the live definitions embed literal secrets, remove those from the redacted copy before sharing. Keep the protected original environment and rendered config private. Record and retain previous image digests. Back up the SQLite volume using the [existing deployment procedure](deploy.md#7-updating-the-app). Verify the backup restores on a disposable system and keep a recoverable copy off-server. Save existing `runtime/nginx/*.geo` if present. Store all backups outside the webroot.

Review the provider firewall and Docker firewall rules for IPv4 and IPv6. Restrict inbound traffic to the administration SSH path and ports 80/443. Check for unintended listeners on 8080, database ports and the Docker TCP API. Docker-published ports can bypass UFW's INPUT path. Review the provider firewall and Docker forwarding rules, including `DOCKER-USER` with the iptables backend. Update the OS, Docker and stock Nginx. Verify a separate key-based SSH session before disabling password or root SSH. Fail2ban for SSH is optional. Keep existing access until alternate access is confirmed. [Docker firewall behavior](https://docs.docker.com/engine/network/packet-filtering-firewalls/)

## Prepare the staged release

On a test clone use a disposable volume, synthetic credentials and a test certificate. For a new project, run the relevant Compose command with `create` to allocate its bridge without starting the new application. Determine the actual network name from the project and inspect it.

```bash
docker network inspect gf3_gf3-internal --format '{{range .IPAM.Config}}{{println .Subnet}}{{end}}'
python3 --version
docker compose version
systemctl --version
sudo install -d -o root -g root -m 0700 deploy/security/runtime/{nginx,logs,state}
python3 -m unittest discover -s deploy/security/tests -v
sudo python3 deploy/security/gf3_guard.py run --dry-run
```

Set `GF3_TRUSTED_PROXY_CIDR` to the verified private IPv4 network prefix (/16 through /30) before starting the new app. Remove any operator-defined `ASPNETCORE_FORWARDEDHEADERS_ENABLED` shortcut from the environment. Set `GF3_SERVER_NAME` to one literal DNS hostname, or `localhost` for local smoke tests. Use the same hostname in the environment file and Compose. Recheck proxy trust before placing any additional proxy or CDN in front of Nginx. Replace example manager and admin passwords. If a JWT key must change, schedule session invalidation and recovery explicitly.

Use root ownership for the runtime directories with the stock root Nginx master and the default systemd service. State files are mode 0600 and compiled `geo` files are mode 0644 inside the restricted runtime directory. Review available space, log ownership and permissions before opening the site. Run the guard once on staging with the base Compose file for HTTP only, or with both files for HTTPS. Inspect `status` for feed freshness and verify local defenses. If feeds are unavailable, record the operator decision to start with static path blocking and local detection until feeds become available.

## Deploy and verify

Validate both Compose configurations with synthetic values on the test clone, including both rendered `nginx -t` checks and the [CI smoke suite](../.github/workflows/ci.yml). On the live host confirm the effective config, Nginx image, certificate paths and renewal path, volume name and dedicated bridge before rollout. Use the existing backup and migration procedure, deploy the app and Nginx during the maintenance window and verify the new Nginx config and app health before enabling the timer. Use the current patched `nginx:stable-alpine` image with support for wildcard `geo` includes.

```bash
"${compose[@]}" exec -T nginx nginx -t
domain=app-gf.com
server_ip='<verified-server-ip>'
curl --fail --resolve "$domain:443:$server_ip" "https://$domain/api/health"
curl --head --resolve "$domain:80:$server_ip" "http://$domain/login"
curl --path-as-is --resolve "$domain:443:$server_ip" "https://$domain/tmp/%252eenv"
curl --head -H 'Host: invalid.example' "http://$server_ip/"
```

Verify the canonical redirect and valid certificate, and test the real hostname, any previous hostname, and the direct IPv4 and IPv6 addresses. An unrecognized Host or direct-IP Host gets connection closure (444 in Nginx). A connection made directly to the IP with the canonical Host and TLS server name still uses the normal GF3 guards.

Verify manager and employee login, authorization and admin restrictions, frontend deep links, real JS/CSS assets, `/sw.js`, `/version.json`, Web Push registration and delivery on a test device, file exports, SignalR negotiate and a real WebSocket connection. Check SQLite health/migration logs and routine traffic from a shared NAT. Repeat the CI probe matrix using `curl --path-as-is`. Expect 404 for recognized probes and 400 for invalid paths rejected by Nginx. Verify synthetic IP deny/allow and forged forwarding only on the disposable instance.

## Enable and monitor the guard

Install the units only after the web checks pass. The default service expects `/opt/gf3` and uses both Compose files. For an HTTP-only clone, edit its `ExecStart` to pass `--compose-files deploy/docker-compose.yml` only. The service runs as root to access Docker and private runtime files. Membership in the Docker group is root-equivalent.

```bash
sudo install -m 0644 deploy/security/gf3-guard.{service,timer} /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl start gf3-guard.service
sudo python3 deploy/security/gf3_guard.py status
sudo systemctl enable --now gf3-guard.timer
sudo python3 deploy/security/gf3_guard.py ban 193.47.62.109 --hours 168
journalctl -u gf3-guard.service --since today
"${compose[@]}" logs --tail=100 nginx
```

The incident IP ban expires after seven days. The guard only updates Nginx web rules. SSH filtering remains under operator control. Inspect feed ages, 403/404/429/444 ratios, ban volume, latency, active users and TLS renewal. An Nginx-generated 403 identifies an IP block. The JSON `probe` flag distinguishes scanner 404s from ordinary 404s. A 429 represents the site or API traffic budget and carries retry guidance. Verify a synthetic query token never appears in stdout or `access.jsonl`, including on a WebSocket handshake. Low-level Nginx request diagnostics can include raw URLs, so the templates keep request error logging at `crit` on stderr. Paths with invalid UTF-8 bytes use a fixed marker in the JSON logs.

Add `/etc/logrotate.d/gf3-security` with the actual checkout path and root ownership. `copytruncate` preserves the live file handle and can lose a small number of records during the copy/truncate window. The guard detects truncation, including rapid regrowth.

```text
/opt/gf3/deploy/security/runtime/logs/access.jsonl {
    daily
    rotate 7
    compress
    copytruncate
    missingok
    notifempty
    su root root
}
```

Use `chown root:root` and `chmod 0600` on the current log after Nginx creates it. `copytruncate` preserves its mode and owner. Recheck permissions after replacing the log or recreating the container. Inspect the rotation policy with `sudo logrotate -d /etc/logrotate.d/gf3-security`.

## Allow overrides and roll back

```bash
sudo python3 deploy/security/gf3_guard.py unban '<public-ip>'
sudo python3 deploy/security/gf3_guard.py allow '<staff-egress-ip-or-cidr>'
sudo python3 deploy/security/gf3_guard.py unallow '<staff-egress-ip-or-cidr>'
sudo systemctl disable --now gf3-guard.timer
```

`unban` removes the local ban and creates a 24-hour IP override if an active external feed still matches. An explicit allow persists until removed. IPv4 allows are limited to /24 or narrower and IPv6 allows to /64 or narrower. Path probes remain blocked for allowed clients.

Before restoring rules, let any running guard invocation finish and keep the timer disabled. Copy the previous snapshots into temporary files beside the targets and rename them. If the first deployment had empty snapshots, installing empty files is equivalent. Use the saved pre-release snapshot if any automatic rollback verification failed.

```bash
runtime=deploy/security/runtime
for name in blocked-compiled.geo allow-compiled.geo; do
    if sudo test -f "$runtime/state/$name.previous"; then
        sudo install -m 0644 "$runtime/state/$name.previous" "$runtime/nginx/$name.tmp"
        sudo mv "$runtime/nginx/$name.tmp" "$runtime/nginx/$name"
    fi
done
"${compose[@]}" exec -T nginx nginx -t
"${compose[@]}" exec -T nginx nginx -s reload
"${compose[@]}" exec -T nginx ps -o pid,args
```

Check health and the edge behavior again. If a release rollback is required, restore the previous Compose and templates, select the saved app image and Nginx image, and recreate the affected containers using the same persistent volume. Restore a database snapshot only if the app/database changed and the tested rollback requires it.

## Feed policy and incident triage

- Spamhaus DROP and DROPv6 JSON Lines and Blocklist.de `strongips` are refreshed at most daily per source. Each verified cache expires after seven days. DROP data is copyright The Spamhaus Project SLU. Its attribution and terms also accompany the original cached data. [Spamhaus DROP](https://www.spamhaus.org/blocklists/do-not-route-or-peer/) · [DROP fair use](https://www.spamhaus.org/blocklists/drop-fair-use-policy/) · [Blocklist.de exports and terms](https://www.blocklist.de/en/export.html)
- Blocklist.de `apache` and `all` are research only. Aggressive bulk feeds and a CrowdSec Lua bouncer on stock Alpine Nginx were excluded from this rollout. Review false positives and obtain separate approval before changing the feed or runtime.
- Four canonical/default-host probe events from a public IP within ten minutes trigger a 12-hour ban. The guard retains only the recent IP counters, cursor, bans and feed state. Nginx uses the TCP peer for its rules, and ASP.NET trusts only one forwarded hop on the verified bridge. [Nginx map](https://nginx.org/en/docs/http/ngx_http_map_module.html) · [Nginx geo](https://nginx.org/en/docs/http/ngx_http_geo_module.html) · [Nginx limit_req](https://nginx.org/en/docs/http/ngx_http_limit_req_module.html) · [ASP.NET proxy trust](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer)
- The incident's ten HTTP 200 entries establish status codes only. On a disposable instance compare the response hash against the matching SPA `index.html`. Review the actual live mounts and logs for potentially exposed resources. If disclosure is substantiated, preserve an incident snapshot, rotate the affected credentials, invalidate affected sessions, review the exposed data and follow the existing RODO incident procedure.
