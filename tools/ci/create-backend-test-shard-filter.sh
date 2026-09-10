#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 3 ]]; then
  echo "usage: $0 <shard-index> <shard-count> <output-file>" >&2
  exit 64
fi

shard_index=$1
shard_count=$2
output_file=$3

if ! [[ $shard_index =~ ^[0-9]+$ && $shard_count =~ ^[1-9][0-9]*$ ]] || (( shard_index >= shard_count )); then
  echo "shard index must be a non-negative integer smaller than shard count" >&2
  exit 64
fi

discovery_output=$(mktemp)
trap 'rm -f "$discovery_output"' EXIT

dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj \
  --no-build \
  --configuration Release \
  --list-tests \
  --filter "Category!=LocalDocker&Category!=PostgresIntegration" \
  --logger "console;verbosity=normal" >"$discovery_output"

test_classes=()
while IFS= read -r test_class; do
  test_classes+=("$test_class")
done < <(
  sed -nE 's/^[[:space:]]+(Gccs\.Api\.Tests\.[^.[:space:]]+)\..*$/\1/p' "$discovery_output" |
    LC_ALL=C sort -u
)

if (( ${#test_classes[@]} == 0 )); then
  echo "test discovery returned no Gccs.Api.Tests classes" >&2
  exit 1
fi

selected_classes=()
for test_class in "${test_classes[@]}"; do
  checksum=$(printf '%s' "$test_class" | cksum | awk '{print $1}')
  if (( checksum % shard_count == shard_index )); then
    selected_classes+=("$test_class")
  fi
done

if (( ${#selected_classes[@]} == 0 )); then
  echo "shard $shard_index of $shard_count contains no test classes" >&2
  exit 1
fi

filter=""
for test_class in "${selected_classes[@]}"; do
  term="FullyQualifiedName~${test_class}."
  if [[ -n $filter ]]; then
    filter+="|"
  fi
  filter+="$term"
done

printf '(%s)&Category!=LocalDocker&Category!=PostgresIntegration\n' "$filter" >"$output_file"
printf 'Selected %d of %d test classes for shard %d of %d.\n' \
  "${#selected_classes[@]}" "${#test_classes[@]}" "$shard_index" "$shard_count"
