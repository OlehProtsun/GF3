#!/usr/bin/env bash
set -euo pipefail
umask 077

if [[ $EUID != 0 || $# != 2 || ! $2 =~ ^[0-9a-f]{40}$ ]]; then
    printf 'Usage (as root): %s /absolute/approved/checkout FULL_PROD2_SHA\n' "$0" >&2
    exit 2
fi
root=/opt/gf
source_dir=$1
sha=$2
[[ $source_dir = /* ]]
for program in git docker python3 curl openssl logrotate flock systemctl systemd-analyze fail2ban-client; do
    command -v "$program" >/dev/null
done
test -r /etc/ssl/certs/ca-certificates.crt
docker compose version
exec 9>/run/lock/gf3-prod-deploy.lock
flock -w 120 9

python3 - "$source_dir" "$root" "$0" <<'PY'
from pathlib import Path
import stat
import sys

for name in sys.argv[1:]:
    path = Path(name).absolute()
    if path.resolve() != path:
        raise SystemExit(f"Symlinks are not permitted in approved source paths: {path}")
    for entry in [*path.parents, path]:
        info = entry.lstat()
        if info.st_uid != 0 or info.st_mode & 0o022 or stat.S_ISLNK(info.st_mode):
            raise SystemExit(f"Require protected root-owned source: {entry}")
source, root, _ = map(Path, sys.argv[1:])
env = root / "deploy/.env"
if env.is_symlink() or not env.is_file() or env.stat().st_uid != 0 or env.stat().st_mode & 0o077:
    raise SystemExit("deploy/.env must be a root-owned private regular file")
for directory in (root / "deploy", root / "deploy/security", root / "deploy/security/runtime",
                  Path("/var/www/certbot"), Path("/var/www/certbot/.well-known"),
                  Path("/var/www/certbot/.well-known/acme-challenge"), Path("/var/lib/gf3-deploy"),
                  Path("/usr/local/libexec"), Path("/usr/local/libexec/gf3")):
    if directory.is_symlink():
        raise SystemExit(f"Unexpected symlink: {directory}")
    if directory.exists() and (not directory.is_dir() or directory.stat().st_uid != 0
                               or directory.stat().st_mode & 0o022):
        raise SystemExit(f"Unsafe directory: {directory}")
runtime = root / "deploy/security/runtime"
for name in ("nginx", "logs", "state"):
    directory = runtime / name
    if directory.is_symlink() or (directory.exists() and
            (not directory.is_dir() or directory.stat().st_uid != 0 or directory.stat().st_mode & 0o022)):
        raise SystemExit(f"Unsafe runtime directory: {directory}")
for path in (runtime / "logs/access.jsonl", runtime / "state/guard.lock"):
    if path.is_symlink() or (path.exists() and (not path.is_file() or path.stat().st_uid != 0)):
        raise SystemExit(f"Unsafe runtime file: {path}")
PY

[[ $(git -C "$root" branch --show-current) = PROD2 ]]
[[ -z $(git -C "$root" status --porcelain) ]]
[[ $(git -C "$source_dir" rev-parse HEAD) = "$sha" ]]
[[ -z $(git -C "$source_dir" status --porcelain) ]]
git -C "$root" fetch origin refs/heads/PROD2:refs/remotes/origin/PROD2
[[ $(git -C "$root" rev-parse refs/remotes/origin/PROD2) = "$sha" ]]
git -C "$root" merge-base --is-ancestor HEAD "$sha"
if systemctl is-active --quiet gf3-guard.service || systemctl is-active --quiet gf3-guard.timer ||
   systemctl is-enabled --quiet gf3-guard.timer; then
    printf 'Existing active guard requires operator review\n' >&2
    exit 1
fi

files=(gf3_release.py gf3_guard.py gf3-guard.service gf3-guard.timer install-gf3-security.sh)
for file in "${files[@]}"; do
    path="$source_dir/deploy/security/$file"
    [[ -f "$path" && ! -L "$path" && $(stat -c %u "$path") = 0 ]]
    mode=$(stat -c %a "$path")
    (( (8#$mode & 022) == 0 ))
    [[ $(git -C "$source_dir" hash-object "$path") = $(git -C "$root" rev-parse "$sha:deploy/security/$file") ]]
done
[[ $(git -C "$root" hash-object "$0") = $(git -C "$root" rev-parse "$sha:deploy/security/install-gf3-security.sh") ]]
grep -Fxq 'WorkingDirectory=/opt/gf' "$source_dir/deploy/security/gf3-guard.service"
grep -Fxq 'ExecStart=/usr/bin/python3 /usr/local/libexec/gf3/gf3_guard.py run' "$source_dir/deploy/security/gf3-guard.service"
grep -Fxq 'Environment=GF3_GUARD_ACTIVE_COMPOSE=/var/lib/gf3-deploy/active/compose.json' "$source_dir/deploy/security/gf3-guard.service"
PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s "$source_dir/deploy/security/tests" -v

destinations=(
    /usr/local/libexec/gf3/gf3_release.py
    /usr/local/libexec/gf3/gf3_guard.py
    /etc/systemd/system/gf3-guard.service
    /etc/systemd/system/gf3-guard.timer
    /etc/logrotate.d/gf3-security
    /etc/letsencrypt/renewal-hooks/deploy/50-gf3-nginx
)
for path in "${destinations[@]}"; do
    if [[ -e "$path" || -L "$path" ]]; then
        printf 'Review existing configuration before installing: %s\n' "$path" >&2
        exit 1
    fi
done
test -d /etc/letsencrypt/renewal-hooks/deploy
exclude=$(git -C "$root" rev-parse --path-format=absolute --git-path info/exclude)
[[ ! -L "$exclude" && ! -L "${exclude%/*}" && $(stat -c %u "${exclude%/*}") = 0 ]]
if [[ -e "$exclude" ]]; then
    [[ -f "$exclude" && $(stat -c %u "$exclude") = 0 ]]
fi
patterns=('/deploy/security/runtime/nginx/*' '/deploy/security/runtime/logs/*' '/deploy/security/runtime/state/*')
for pattern in "${patterns[@]}"; do
    if ! grep -Fxq "$pattern" "$exclude" 2>/dev/null; then
        printf '\n%s\n' "$pattern" >> "$exclude"
    fi
done
if ! grep -Fxq '!/deploy/security/runtime/*/.gitkeep' "$exclude" 2>/dev/null; then
    printf '%s\n' '!/deploy/security/runtime/*/.gitkeep' >> "$exclude"
fi
install -d -o root -g root -m 0700 /var/lib/gf3-deploy /usr/local/libexec/gf3 \
    "$root"/deploy/security/runtime/{nginx,logs,state}
install -d -m 0755 /var/www/certbot/.well-known/acme-challenge
for path in "$root"/deploy/security/runtime/{logs/access.jsonl,state/guard.lock}; do
    touch "$path"
    chmod 0600 "$path"
done
git -C "$root" check-ignore --quiet deploy/security/runtime/logs/access.jsonl
git -C "$root" check-ignore --quiet deploy/security/runtime/state/guard.lock
[[ -z $(git -C "$root" status --porcelain) ]]

created=()
pending=
stage=$(mktemp -d /var/lib/gf3-deploy/.install-XXXXXXXX)
cleanup() {
    code=$?
    trap - EXIT
    if (( code != 0 )); then
        if [[ -n $pending ]]; then rm -f -- "$pending"; fi
        if (( ${#created[@]} )); then rm -f -- "${created[@]}"; fi
        systemctl daemon-reload || true
    fi
    rm -f -- "$stage"/{logrotate,renew}
    rmdir -- "$stage"
    exit "$code"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
cat > "$stage/logrotate" <<'EOF'
/opt/gf/deploy/security/runtime/logs/access.jsonl {
    daily
    rotate 7
    compress
    copytruncate
    missingok
    notifempty
    su root root
}
EOF
cat > "$stage/renew" <<'EOF'
#!/bin/sh
exec /usr/bin/python3 /usr/local/libexec/gf3/gf3_release.py renew
EOF

install_new() {
    local source=$1 target=$2 mode=$3
    pending=$(mktemp "${target%/*}/.gf3-install-XXXXXXXX")
    install -o root -g root -m "$mode" "$source" "$pending"
    ln -- "$pending" "$target"
    created+=("$target")
    rm -- "$pending"
    pending=
}
for index in 0 1 2 3; do
    install_new "$source_dir/deploy/security/${files[$index]}" "${destinations[$index]}" 0644
done
install_new "$stage/logrotate" "${destinations[4]}" 0644
install_new "$stage/renew" "${destinations[5]}" 0750
systemd-analyze verify "${destinations[2]}" "${destinations[3]}"
logrotate -d "${destinations[4]}"
systemctl daemon-reload
printf 'Installed GF3 host integration for %s. Guard remains disabled.\n' "$sha"
