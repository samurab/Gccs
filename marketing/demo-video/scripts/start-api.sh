#!/usr/bin/env bash
set -euo pipefail
umask 077

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
repository_root="$(cd "${project_root}/../.." && pwd)"
runtime_env="$(bash "${project_root}/scripts/prepare-runtime.sh")"
compose_project_name="$(bash "${project_root}/scripts/compose-project-name.sh")"

set -a
source "${runtime_env}"
set +a

docker compose \
  --project-name "${compose_project_name}" \
  --env-file "${runtime_env}" \
  -f "${project_root}/infra/docker-compose.yml" \
  up -d --wait >/dev/null

export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS=http://127.0.0.1:5064
export ConnectionStrings__GccsDatabase="Host=127.0.0.1;Port=15434;Database=fedril_demo;Username=fedril_demo;Password=${FEDRIL_DEMO_DB_PASSWORD}"
# The design-time context factory used by `dotnet ef` reads GCCS_DATABASE,
# while the running API reads ConnectionStrings__GccsDatabase. Keep both on
# the same isolated demo database so migration and runtime cannot diverge.
export GCCS_DATABASE="${ConnectionStrings__GccsDatabase}"
export ConnectionStrings__AzureStorage="DefaultEndpointsProtocol=http;AccountName=fedrildemo;AccountKey=${FEDRIL_DEMO_STORAGE_KEY};BlobEndpoint=http://127.0.0.1:19002/fedrildemo;"
export Cors__AllowedOrigins__0=http://127.0.0.1:5175
export ExtractionProcessing__Enabled=false
export InvitationDelivery__Enabled=false
export LocalDependencies__Enabled=true
export LocalDependencies__SeedData__Enabled=true
export LocalDependencies__Redis__ConnectionString=127.0.0.1:16381
export LocalDependencies__MalwareScanner__Host=127.0.0.1
export LocalDependencies__MalwareScanner__Port=13312
export MalwareScanning__Enabled=true
export MalwareScanning__Provider=ClamAV
export MalwareScanning__Host=127.0.0.1
export MalwareScanning__Port=13312
export MarketingDemo__Enabled=true
export Security__DevelopmentAuth__Enabled=true
export Security__DevelopmentAuth__DefaultTenantId=11111111-1111-1111-1111-111111111113
export Security__DevelopmentAuth__DefaultUserId=22222222-2222-2222-2222-222222222242
export Security__DevelopmentAuth__DefaultEmail=alex.morgan.northstar@example.com
export Security__DevelopmentTesting__Enabled=true
export Security__MembershipAuthorization__Enforce=true
export Logging__LogLevel__Default=Warning

cd "${repository_root}"
dotnet tool restore > "${project_root}/.runtime/tool-restore.log" 2>&1
dotnet tool run dotnet-ef database update \
  --project src/Gccs.Infrastructure/Gccs.Infrastructure.csproj \
  --startup-project apps/api/Gccs.Api.csproj \
  --context GccsDbContext \
  > "${project_root}/.runtime/migrations.log" 2>&1

exec dotnet run --project apps/api/Gccs.Api.csproj --no-launch-profile \
  > "${project_root}/.runtime/api.log" 2>&1
