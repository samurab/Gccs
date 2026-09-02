#!/usr/bin/env bash
set -euo pipefail
umask 077

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
runtime_dir="${project_root}/.runtime"
mkdir -p "${runtime_dir}"

if ! docker info >/dev/null 2>&1; then
  printf 'Docker Desktop is not running. Start Docker Desktop, wait until the engine is ready, then rerun npm run demo:video:start.\n' >&2
  exit 1
fi

api_pid=""
web_pid=""

cleanup() {
  trap - EXIT INT TERM
  if [[ -n "${web_pid}" ]] && kill -0 "${web_pid}" 2>/dev/null; then
    kill "${web_pid}" 2>/dev/null || true
  fi
  if [[ -n "${api_pid}" ]] && kill -0 "${api_pid}" 2>/dev/null; then
    kill "${api_pid}" 2>/dev/null || true
  fi
}
trap cleanup EXIT INT TERM

bash "${project_root}/scripts/start-api.sh" &
api_pid=$!
printf '%s\n' "${api_pid}" > "${runtime_dir}/api.pid"

wait_for_url() {
  local url="$1"
  local label="$2"
  local attempts=0
  until curl --fail --silent --show-error --max-time 2 "${url}" >/dev/null 2>&1; do
    attempts=$((attempts + 1))
    if (( attempts > 420 )); then
      printf 'FeDril demo %s did not become ready. Review the untracked runtime logs.\n' "${label}" >&2
      exit 1
    fi
    sleep 1
  done
}

wait_for_url http://127.0.0.1:5064/health API
node --experimental-strip-types "${project_root}/scripts/seed-demo.ts"

# Start the capture-facing web server only after the isolated API is healthy and
# the fictional scenario is verified. Playwright uses the web URL as its ready
# signal, so this ordering prevents capture from racing API migration or seed.
bash "${project_root}/scripts/start-web.sh" &
web_pid=$!
printf '%s\n' "${web_pid}" > "${runtime_dir}/web.pid"

wait_for_url http://127.0.0.1:5175/ web

printf 'FeDril marketing demo is ready for browser capture.\n'
while kill -0 "${api_pid}" 2>/dev/null && kill -0 "${web_pid}" 2>/dev/null; do
  sleep 2
done
exit 1
