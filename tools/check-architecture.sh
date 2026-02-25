#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"

errors=0

if rg -n "ProjectReference Include=\"..\\DataAccessLayer\\DataAccessLayer.csproj\"" WPFApp/WPFApp.csproj >/dev/null; then
  echo "[ARCH] WPFApp.csproj still references DataAccessLayer.csproj"
  errors=$((errors+1))
else
  echo "[ARCH] OK: WPFApp.csproj has no direct DataAccessLayer reference"
fi

if rg -n "using DataAccessLayer|DataAccessLayer\." WPFApp --glob '*.cs' >/dev/null; then
  echo "[ARCH] WPFApp still contains DataAccessLayer namespace usage"
  rg -n "using DataAccessLayer|DataAccessLayer\." WPFApp --glob '*.cs'
  errors=$((errors+1))
else
  echo "[ARCH] OK: WPFApp has no DataAccessLayer usage"
fi

if [[ $errors -gt 0 ]]; then
  echo "[ARCH] FAILED with $errors violation(s)."
  exit 1
fi

echo "[ARCH] PASSED"
