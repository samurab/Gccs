# TC-4.1 No-CUI Acknowledgement Verification

Executed at: 2026-06-15T00:39:00Z

## Setup Data

- Repository: `/Users/devups/Development/CodexProjects/Gccs`
- API: ASP.NET Core API started with `dotnet run --project apps/api`
- API URL: `http://localhost:5062`
- Frontend: Vite app started with `npm --workspace apps/web run dev -- --host localhost --port 3000`
- Frontend URL: `http://localhost:3000`
- Local dependencies: `docker compose -f infra/docker/docker-compose.yml up -d`
- Database migration: `dotnet tool run dotnet-ef database update --project src/Gccs.Infrastructure/Gccs.Infrastructure.csproj --startup-project apps/api/Gccs.Api.csproj --context GccsDbContext`
- Development tenant/user headers: tenant `11111111-1111-1111-1111-111111111111`, user `22222222-2222-2222-2222-222222222222`
- Notice version: `no-cui-mvp-v1`

## Results

| Test case | Expected result | Actual result | Outcome |
| --- | --- | --- | --- |
| TC-4.1.1 | With no acknowledgement, opening upload workflow shows No-CUI notice before upload. | Browser Evidence route showed `No-CUI acknowledgement`, notice copy including `compliance management only` and `not ready to store CUI`, and the disabled-upload warning. API `GET /api/no-cui-acknowledgement` returned `isAcknowledged=false`. | Pass |
| TC-4.1.2 | Upload is disabled until acknowledgement; UI and API block upload. | Browser check showed file input disabled and `Upload evidence` button disabled before acknowledgement. API `POST /api/evidence-items/{id}/upload-intents` returned `428 Precondition Required` with `no_cui_acknowledgement_required`. | Pass |
| TC-4.1.3 | Acknowledgement stores user, tenant, timestamp, and notice version. | API `POST /api/no-cui-acknowledgement` returned `200 OK` with tenant ID, user ID, `acknowledgedAt`, and `noticeVersion=no-cui-mvp-v1`. Browser showed `Acknowledgement saved.` and enabled the file input. | Pass |
| TC-4.1.4 | Acknowledgement creates audit event and copy states MVP is compliance management only and not ready to store CUI. | PostgreSQL query found `AuditAction=Created`, `EntityType=NoCuiAcknowledgement`, metadata with notice version, timestamp, correlation ID, and the required No-CUI copy. | Pass |

## Exact Commands Executed

```bash
docker compose -f infra/docker/docker-compose.yml up -d
dotnet tool run dotnet-ef database update --project src/Gccs.Infrastructure/Gccs.Infrastructure.csproj --startup-project apps/api/Gccs.Api.csproj --context GccsDbContext
dotnet run --project apps/api
npm --workspace apps/web run dev -- --host localhost --port 3000
curl -i -sS -H 'X-Gccs-Dev-Auth: true' -H 'X-Gccs-Dev-Permissions: ViewEvidence' http://localhost:5062/api/no-cui-acknowledgement
curl -i -sS -X POST -H 'X-Gccs-Dev-Auth: true' -H 'X-Gccs-Dev-Permissions: ManageEvidence' -H 'Content-Type: application/json' -d '{"fileName":"policy.pdf"}' http://localhost:5062/api/evidence-items/00000000-0000-0000-0000-000000000041/upload-intents
curl -i -sS -X POST -H 'X-Gccs-Dev-Auth: true' -H 'X-Gccs-Dev-Permissions: ManageEvidence' -H 'Content-Type: application/json' -d '{"acknowledged":true,"noticeVersion":"no-cui-mvp-v1"}' http://localhost:5062/api/no-cui-acknowledgement
docker exec docker-postgres-1 psql -U gccs -d gccs -c "select action, entity_type, entity_id, summary, metadata_json from gccs.audit_log_entries where entity_type='NoCuiAcknowledgement' order by occurred_at desc limit 1;"
```

## Notes

- Evidence file storage remains intentionally placeholder-only; Story 4.2 owns file type, file size, scan status, and storage guardrails.
- The API stores tenant and user IDs from authenticated context and indexes the tenant/user/notice-version tuple. The acknowledgement and audit tables do not require a physical tenant row so development auth and future tenant lifecycle edge cases can still record the acknowledgement and audit event.
