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

# Retain discovery beside the exact output; never overwrite an earlier attempt.
set -o noclobber
discovery_output="${output_file}.discovery.txt"
# The workflow dependency supplies this identity only after exact App/run-attempt admission.
# An environment string alone is not independent authentication or provider evidence.
[[ ${FEDRIL_PROTECTED_ASSERTIONS_ADMITTED_SHA:-} =~ ^[a-f0-9]{40}$ ]]
test "$(git --no-replace-objects rev-parse HEAD)" = "$FEDRIL_PROTECTED_ASSERTIONS_ADMITTED_SHA"
historical_filter="FullyQualifiedName!~Gccs.Api.Tests.ProductionReadinessChecklistTests.&FullyQualifiedName!~Gccs.Api.Tests.ProductionReadinessHighRiskObligationContentTests."
base_filter="Category!=LocalDocker&Category!=PostgresIntegration&${historical_filter}"

dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj \
  --no-build \
  --configuration Release \
  --list-tests \
  --filter "$base_filter" \
  --logger "console;verbosity=normal" >"$discovery_output"

test_classes=()
while IFS= read -r test_class; do
  test_classes+=("$test_class")
done < <(
  sed -nE 's/^[[:space:]]+(Gccs\.Api\.Tests\.[^.[:space:]]+)\..*$/\1/p' "$discovery_output" |
    sed '/^Gccs\.Api\.Tests\.ProductionReadinessChecklistTests$/d; /^Gccs\.Api\.Tests\.ProductionReadinessHighRiskObligationContentTests$/d' |
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

printf '(%s)&%s\n' "$filter" "$base_filter" >"$output_file"
printf 'Selected %d of %d test classes for shard %d of %d.\n' \
  "${#selected_classes[@]}" "${#test_classes[@]}" "$shard_index" "$shard_count"
