#!/bin/bash
# A separate child survives SIGSTOP of its startup owner. EOF disarms it.
owner=$1
timeout=$2
parent=$(/bin/ps -o ppid= -p "$$")
[ "${parent//[[:space:]]/}" = "$owner" ] || exit 2
while true; do
  deadline=$((SECONDS + timeout))
  IFS= read -r -t "$timeout" message
  status=$?
  if [ "$status" -eq 0 ]; then
    [ "$message" = waiting ] && continue
    exit 0
  fi
  # macOS Bash 3.2 returns 1 for both EOF and timeout; elapsed time distinguishes them.
  [ "$SECONDS" -ge "$deadline" ] || exit 0
  # Reparenting means the original owner exited; never signal a recycled PID.
  parent=$(/bin/ps -o ppid= -p "$$")
  [ "${parent//[[:space:]]/}" = "$owner" ] || exit 0
  echo "FeDril startup watchdog: owner $owner did not become healthy within ${timeout}s; terminating it so a waiter can take over." >&2
  kill -KILL "$owner"
  exit $?
done
