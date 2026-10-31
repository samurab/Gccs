#!/usr/bin/env bash
set -euo pipefail

story_script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
story_repo_root="$(cd "$story_script_dir/.." && pwd)"
story_compose_file="$story_repo_root/infra/docker/docker-compose.yml"
story_appsettings="$story_repo_root/apps/api/appsettings.Development.json"

docker compose -f "$story_compose_file" up -d --wait postgres azurite clamav

export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_ENVIRONMENT=Development
export GCCS_TEST_POSTGRES_CONNECTION="$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["ConnectionStrings"]["GccsDatabase"])' "$story_appsettings")"
export ConnectionStrings__AzureStorage="$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["ConnectionStrings"]["AzureStorage"])' "$story_appsettings")"
export MalwareScanning__Host=127.0.0.1
export MalwareScanning__Port=13310

dotnet test "$story_repo_root/tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj" \
  --filter 'FullyQualifiedName~LaborApplicabilityWageDeterminationTests|FullyQualifiedName~LaborApplicabilityRealStackTests' \
  --no-restore
