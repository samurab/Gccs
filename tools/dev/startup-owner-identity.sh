#!/bin/bash
# Read kernel listener identity even when the owner is suspended. Never signal it.
export LC_ALL=C
lsof=$(command -v lsof) || exit 0
listener_pid() {
  "$lsof" -nP -a "-i4TCP@127.0.0.1:$1" -sTCP:LISTEN -Fp 2>/dev/null | /usr/bin/sed -n 's/^p//p'
}
owner=$(listener_pid "$1")
case "$owner" in ''|*[!0-9]*) exit 0;; esac
started=$(/bin/ps -p "$owner" -o lstart=) || exit 0
[ -n "$started" ] && [ "$(listener_pid "$1")" = "$owner" ] || exit 0
printf '%s|%s\n' "$owner" "$started"
