#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
project_dir="$(cd "$script_dir/.." && pwd)"
mode="${1:-scenes}"

case "$mode" in
  scenes)
    node --experimental-strip-types "$project_dir/narration/generate-narration.ts" --mode scenes
    ;;
  auditions)
    node --experimental-strip-types "$project_dir/narration/generate-narration.ts" --mode auditions
    ;;
  all)
    node --experimental-strip-types "$project_dir/narration/generate-narration.ts" --mode auditions
    node --experimental-strip-types "$project_dir/narration/generate-narration.ts" --mode scenes
    ;;
  *)
    echo "Usage: ./scripts/generate-narration.sh [scenes|auditions|all]" >&2
    exit 2
    ;;
esac
