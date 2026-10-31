#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
runtime_dir="${project_root}/.runtime"
runtime_env="${runtime_dir}/demo.env"

umask 077
mkdir -p "${runtime_dir}"

if [[ ! -f "${runtime_env}" ]]; then
  database_password="$(openssl rand -hex 32)"
  storage_key="$(openssl rand -base64 32 | tr -d '\n')"
  {
    printf 'FEDRIL_DEMO_DB_PASSWORD=%s\n' "${database_password}"
    printf 'FEDRIL_DEMO_STORAGE_KEY=%s\n' "${storage_key}"
  } > "${runtime_env}"
  chmod 600 "${runtime_env}"
fi

printf '%s\n' "${runtime_env}"
