# GF3 deployment guide: Docker + nginx

This guide describes the recommended production deployment path for GF3:

- one ASP.NET Core container serves the API and the already-built React frontend;
- nginx is the only public entrypoint;
- SQLite lives in a persistent Docker volume;
- secrets are stored in `deploy/.env`, never in Git;
- releases can be rebuilt locally or pulled from a registry later.

## 1. Production architecture

```text
Internet / LAN
  |
  v
nginx container :80 / :443
  |
  v
gf3-app container :8080
  |
  v
Docker volume gf3-data (/data/SQLite.db + app backups)
```

The frontend is not served by Vite in production. During Docker build:

1. Node builds `FrontEnd/dist`.
2. .NET publishes `GF3.WebApi`.
3. The frontend dist is copied into the published API `wwwroot`.
4. ASP.NET Core serves static frontend files and API routes from one process.

## 2. Files added for deployment

- `Dockerfile` - multi-stage production image build.
- `.dockerignore` - keeps local build/test/cache files out of Docker context.
- `deploy/docker-compose.yml` - local/server HTTP deployment.
- `deploy/docker-compose.https.yml` - optional HTTPS overlay for nginx.
- `deploy/.env.example` - template for server secrets/settings.
- `deploy/nginx/http-templates/gf3.conf.template` - HTTP nginx reverse proxy, mounted as nginx `default.conf`.
- `deploy/nginx/https-templates/gf3.conf.template` - HTTPS nginx reverse proxy, mounted as nginx `default.conf`.
- `.github/workflows/ci.yml` - backend/frontend verification and Docker image build.
- `.github/workflows/docker-publish.yml` - manual GHCR Docker image publishing.

## 3. Local Docker smoke test

From the repository root:

```powershell
Copy-Item deploy\.env.example deploy\.env
```

Edit `deploy/.env`:

```env
GF3_SERVER_NAME=_
GF3_HTTP_PORT=8088
Jwt__SigningKey=replace_with_a_long_local_test_secret_at_least_64_chars
GF3_ADMIN_ENABLED=false
GF3_ADMIN_ALLOW_REMOTE=false
GF3_ADMIN_ALLOW_WRITE=false
GF3_REQUIRE_EXISTING_DATABASE=false
```

Build and start:

```powershell
docker compose --env-file deploy/.env -f deploy/docker-compose.yml up --build -d
```

Check containers:

```powershell
docker compose --env-file deploy/.env -f deploy/docker-compose.yml ps
docker compose --env-file deploy/.env -f deploy/docker-compose.yml logs -f --tail=100
```

Smoke-check API health:

```powershell
curl http://localhost:8088/api/health
```

Expected response shape:

```json
{
  "status": "ok",
  "canConnect": true
}
```

Then open:

```text
http://localhost:8088
```

Useful local checks:

```powershell
curl -I http://localhost:8088
curl -I http://localhost:8088/assets/
curl http://localhost:8088/api/health
```

Stop local deployment:

```powershell
docker compose --env-file deploy/.env -f deploy/docker-compose.yml down
```

Remove the local SQLite volume only when you intentionally want to delete local deployment data:

```powershell
docker volume rm gf3-data
```

## 4. Required production settings

Create `deploy/.env` on the server from `deploy/.env.example`.

Minimum production values:

```env
GF3_IMAGE=gf3-web:local
GF3_SERVER_NAME=your-domain.com
GF3_HTTP_PORT=80
GF3_HTTPS_PORT=443
GF3_DATA_VOLUME=gf3-data
GF3_REQUIRE_EXISTING_DATABASE=true
GF3_BACKUP_BEFORE_MIGRATE=true

Jwt__SigningKey=CHANGE_THIS_TO_A_LONG_RANDOM_SECRET

GF3_ADMIN_ENABLED=false
GF3_ADMIN_ALLOW_REMOTE=false
GF3_ADMIN_ALLOW_WRITE=false
```

Generate a JWT secret on Linux:

```bash
openssl rand -base64 64
```

Generate one in PowerShell:

```powershell
[Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Maximum 256 }))
```

SMTP is only needed if password recovery emails should work:

```env
Smtp__Host=smtp.gmail.com
Smtp__Port=587
Smtp__EnableSsl=true
Smtp__UserName=your-smtp-user
Smtp__Password=your-smtp-password-or-app-password
Smtp__FromAddress=no-reply@your-domain.com
Smtp__FromDisplayName=GF3
```

Keep `deploy/.env` private. It is ignored by Git.

## 5. Server setup: first deployment

Example target:

- Ubuntu 24.04 LTS or similar;
- Docker Engine + Docker Compose plugin installed;
- domain DNS `A` record points to the server;
- ports `80` and `443` are open in firewall/security group.

Install Docker on the server following Docker's official guide for your distro.

Create app directory:

```bash
sudo mkdir -p /opt/gf3
sudo chown "$USER":"$USER" /opt/gf3
cd /opt/gf3
```

Copy project files to the server. For a simple first deploy, from your local machine:

```powershell
scp -r Dockerfile .dockerignore deploy GF3.WebApi BusinessLogicLayer DataAccessLayer FrontEnd your-user@your-server:/opt/gf3/
```

On the server:

```bash
cd /opt/gf3
cp deploy/.env.example deploy/.env
nano deploy/.env
```

Set at least:

```env
GF3_SERVER_NAME=your-domain.com
GF3_REQUIRE_EXISTING_DATABASE=false
Jwt__SigningKey=<generated secret>
```

Start HTTP deployment:

```bash
docker compose --env-file deploy/.env -f deploy/docker-compose.yml up --build -d
```

Check:

```bash
docker compose --env-file deploy/.env -f deploy/docker-compose.yml ps
docker compose --env-file deploy/.env -f deploy/docker-compose.yml logs -f --tail=100
curl http://127.0.0.1/api/health
```

From your browser:

```text
http://your-domain.com
```

## 6. HTTPS with nginx

The HTTPS compose overlay expects certificate files mounted into nginx.

Recommended simple certificate path on the server:

```text
/opt/gf3/deploy/certs/fullchain.pem
/opt/gf3/deploy/certs/privkey.pem
```

Set in `deploy/.env`:

```env
GF3_TLS_CERT_PATH=./certs/fullchain.pem
GF3_TLS_KEY_PATH=./certs/privkey.pem
```

Start with HTTPS overlay:

```bash
docker compose --env-file deploy/.env \
  -f deploy/docker-compose.yml \
  -f deploy/docker-compose.https.yml \
  up --build -d
```

Check:

```bash
curl -I https://your-domain.com
curl https://your-domain.com/api/health
```

If you use Certbot on the host, copy or symlink the generated files into `deploy/certs`, then restart nginx:

```bash
docker compose --env-file deploy/.env \
  -f deploy/docker-compose.yml \
  -f deploy/docker-compose.https.yml \
  restart nginx
```

## 7. Updating the app

For an existing production installation, keep these values in `deploy/.env`:

```env
GF3_DATA_VOLUME=gf3-data
GF3_REQUIRE_EXISTING_DATABASE=true
GF3_BACKUP_BEFORE_MIGRATE=true
```

`GF3_DATA_VOLUME` must stay exactly the same as on the current server. A different name creates a new empty volume; the existing-database guard intentionally refuses to start in that case.

Build the replacement image before the maintenance window:

```bash
cd /opt/gf3
git pull
docker compose --env-file deploy/.env -f deploy/docker-compose.yml build gf3-app
```

Take an off-container volume snapshot while the app is stopped, then deploy:

```bash
mkdir -p backups
docker compose --env-file deploy/.env -f deploy/docker-compose.yml stop nginx gf3-app
docker volume inspect gf3-data
docker run --rm \
  -v gf3-data:/data:ro \
  -v "$PWD/backups:/backup" \
  alpine \
  tar czf /backup/gf3-data-pre-release-$(date +%Y%m%d-%H%M%S).tar.gz -C /data .
docker compose --env-file deploy/.env -f deploy/docker-compose.yml up -d
```

Use the same two compose files for HTTPS deployments. Wait for `gf3-app` to become healthy before nginx starts, then verify:

```bash
docker compose --env-file deploy/.env -f deploy/docker-compose.yml ps
docker compose --env-file deploy/.env -f deploy/docker-compose.yml logs --tail=200 gf3-app
curl --fail https://your-domain.com/api/health
```

At startup GF3 performs `PRAGMA quick_check`, creates a consistent SQLite backup inside `/data/backups` when migrations are pending, applies migrations, and checks integrity again. A migration or integrity failure keeps the container unhealthy and prevents nginx from routing traffic to it.

Registry-based update after running the manual `Publish Docker image` GitHub Actions workflow:

```env
GF3_IMAGE=ghcr.io/YOUR_ACCOUNT/gf3:2026-05-22-1
```

Then on the server:

```bash
cd /opt/gf3
docker compose --env-file deploy/.env -f deploy/docker-compose.yml pull
docker compose --env-file deploy/.env -f deploy/docker-compose.yml up -d
curl https://your-domain.com/api/health
```

EF Core migrations run automatically when `gf3-app` starts, so the database schema is updated on boot.

## 8. Backups and restore

GF3 creates automatic SQLite backups inside the database workspace. In Docker, that workspace is the `gf3-data` volume mounted at `/data`.

Inspect the volume:

```bash
docker volume inspect gf3-data
```

Create an immediate manual copy from the app UI if admin database tooling is enabled only for trusted maintenance windows.

For server-grade backups, also back up the Docker volume outside the server. A simple archive:

```bash
docker run --rm \
  -v gf3-data:/data:ro \
  -v "$PWD/backups:/backup" \
  alpine \
  tar czf /backup/gf3-data-$(date +%Y%m%d-%H%M%S).tar.gz -C /data .
```

Restore from an archive during maintenance:

```bash
docker compose --env-file deploy/.env -f deploy/docker-compose.yml down
docker run --rm -v gf3-data:/data alpine sh -c "rm -rf /data/*"
docker run --rm \
  -v gf3-data:/data \
  -v "$PWD/backups:/backup" \
  alpine \
  tar xzf /backup/YOUR_BACKUP_FILE.tar.gz -C /data
docker compose --env-file deploy/.env -f deploy/docker-compose.yml up -d
```

For real production, schedule off-server backups with `restic`, `rclone`, S3, Backblaze B2, or your hosting provider snapshots.

## 9. Rollback

If the new image fails before migrations are applied, return to the previous image/commit. If migrations were applied, restore the matching pre-release volume archive before starting the previous application version; old code is not guaranteed to understand the newer schema.

If you deploy from source and the new version fails:

```bash
cd /opt/gf3
git log --oneline -5
git checkout <previous-good-commit>
docker compose --env-file deploy/.env -f deploy/docker-compose.yml up --build -d
curl https://your-domain.com/api/health
```

If you deploy from a registry later, pin image tags in `GF3_IMAGE`, for example:

```env
GF3_IMAGE=ghcr.io/your-account/gf3:2026-05-22-1
```

Rollback then becomes:

```bash
nano deploy/.env
docker compose --env-file deploy/.env -f deploy/docker-compose.yml pull
docker compose --env-file deploy/.env -f deploy/docker-compose.yml up -d
```

## 10. Operational checklist

Before first public launch:

- `deploy/.env` exists on the server and is not committed.
- `Jwt__SigningKey` is long and random.
- `GF3_ADMIN_ENABLED=false` unless you are in a short trusted maintenance window.
- `GF3_SERVER_NAME` matches the real domain.
- `GF3_DATA_VOLUME` matches the existing production volume exactly.
- `GF3_REQUIRE_EXISTING_DATABASE=true` for every update of an existing installation.
- A fresh off-server volume archive exists before deployment.
- DNS points to the server.
- Firewall allows `80` and `443`.
- `curl https://your-domain.com/api/health` returns `canConnect: true`.
- Login, export, SignalR/presence, and password recovery are tested.
- Off-server backup is scheduled.

During maintenance windows:

- Enable admin tooling only when needed.
- Prefer keeping `GF3_ADMIN_ALLOW_REMOTE=false` on public servers.
- Disable admin tooling again after the task.
- Take a volume backup before risky database operations.

## 11. Troubleshooting

View app logs:

```bash
docker compose --env-file deploy/.env -f deploy/docker-compose.yml logs -f gf3-app
```

View nginx logs:

```bash
docker compose --env-file deploy/.env -f deploy/docker-compose.yml logs -f nginx
```

Rebuild from scratch:

```bash
docker compose --env-file deploy/.env -f deploy/docker-compose.yml build --no-cache
docker compose --env-file deploy/.env -f deploy/docker-compose.yml up -d
```

Check generated nginx config inside the container:

```bash
docker compose --env-file deploy/.env -f deploy/docker-compose.yml exec nginx nginx -T
```

Common issues:

- `Jwt__SigningKey` is missing: login tokens are not safe for production until this is set.
- `canConnect=false`: SQLite volume/path permissions or migration failure.
- Static app loads but API fails: check `/api/health` and app logs.
- SignalR/presence problems: confirm nginx has `Upgrade` and `Connection` proxy headers; the provided templates already include them.
- HTTPS redirect loop: confirm `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` and nginx sends `X-Forwarded-Proto`.

## 12. References

- Microsoft: [Host and deploy ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/).
- Microsoft: [ASP.NET Core with Linux and nginx](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx).
- Microsoft: [Configure ASP.NET Core behind proxies and load balancers](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer).
- Docker: [Environment variables in Compose](https://docs.docker.com/compose/environment-variables/).
- Docker: [Volumes](https://docs.docker.com/engine/storage/volumes/).
- nginx: [WebSocket proxying](https://nginx.org/en/docs/http/websocket.html).
