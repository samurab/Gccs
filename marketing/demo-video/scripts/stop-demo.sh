#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
runtime_env="${project_root}/.runtime/demo.env"
compose_project_name="$(bash "${project_root}/scripts/compose-project-name.sh")"

stop_recorded_process() {
  local pid_file="$1"
  if [[ ! -f "${pid_file}" ]]; then
    return
  fi
  local process_id
  process_id="$(tr -cd '0-9' < "${pid_file}")"
  if [[ -n "${process_id}" ]] && kill -0 "${process_id}" 2>/dev/null; then
    local command_line
    command_line="$(ps -p "${process_id}" -o command= 2>/dev/null || true)"
    if [[ "${command_line}" == *"Gccs.Api.csproj"* ]] ||
       [[ "${command_line}" == *"vite"*"5175"* ]] ||
       [[ "${command_line}" == *"marketing/demo-video/scripts/start-"* ]]; then
      kill "${process_id}" 2>/dev/null || true
    fi
  fi
  rm -f "${pid_file}"
}

stop_recorded_process "${project_root}/.runtime/web.pid"
stop_recorded_process "${project_root}/.runtime/api.pid"

if [[ -f "${runtime_env}" ]] && docker info >/dev/null 2>&1; then
  docker compose \
    --project-name "${compose_project_name}" \
    --env-file "${runtime_env}" \
    -f "${project_root}/infra/docker-compose.yml" \
    down >/dev/null
elif [[ -f "${runtime_env}" ]]; then
  printf 'Docker is not running; skipped container cleanup. No demo application processes were left running.\n'
fi

printf 'FeDril demo services stopped. The isolated database volume was preserved.\n'
