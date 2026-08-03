#!/usr/bin/env bash
set -euo pipefail
umask 077

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
repository_root="$(cd "${project_root}/../.." && pwd)"

export VITE_API_BASE_URL=http://127.0.0.1:5064
export VITE_DEMO_CAPTURE=true
export VITE_GCCS_DEV_EMAIL=priya.shah@northstar.example
export VITE_GCCS_DEV_ROLE=ComplianceManager
export VITE_GCCS_DEV_USER_ID=22222222-2222-2222-2222-222222222243

cd "${repository_root}"
exec npm --workspace apps/web run dev -- --host 127.0.0.1 --port 5175 --strictPort \
  > "${project_root}/.runtime/web.log" 2>&1
