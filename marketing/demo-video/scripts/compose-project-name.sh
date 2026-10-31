#!/usr/bin/env bash
set -euo pipefail

project_root="${1:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"
project_hash="$(printf '%s' "${project_root}" | shasum -a 256 | cut -c1-12)"

printf 'fedril-marketing-demo-%s\n' "${project_hash}"
