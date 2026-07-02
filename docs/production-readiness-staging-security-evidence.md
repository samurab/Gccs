# Production Readiness Staging Security Evidence

Story: PR-3.3 - Verify Tenant Isolation And RBAC In Staging.

Evidence status: Blocked for live authenticated staging API execution; automated backend/API coverage passed.

Review date: 2026-07-02.

Staging API: `https://gccs-api-staging-19984.azurewebsites.net`.

Staging health result: passed. `GET /health` returned `status: ok`, `dataPosture: No-CUI / compliance management only`, and dependency signals for `background-jobs`, `object-storage`, `postgresql`, and `redis`.

This artifact does not close PR-3.3. It records the current evidence, the failed prerequisite for authenticated staging authorization checks, and the exact work required before the production readiness sequence can proceed to PR-3.4.

## Required PR-3.3 Scope

PR-3.3 requires authenticated staging verification for:

- Cross-tenant reads for contracts, evidence, tasks, reports, exports, and audit logs.
- Cross-tenant update and delete attempts with no mutation.
- Owner, admin, compliance manager, contributor, auditor, and advisor direct API calls against allowed and restricted actions.
- Direct API denial where the UI hides restricted actions.
- Consistent permission failure responses.
- Attached tenant isolation and RBAC output in the launch package.

## Automated Coverage

Command:

```bash
dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~SecurityIsolationVerificationTests|FullyQualifiedName~RoleBasedPermissionTests"
```

Result: passed on 2026-07-02. Ten tests passed with zero failures.

Coverage:

- `SecurityIsolationVerificationTests` covers direct cross-tenant record access denial, tenant-scoped collection filtering, report filtering, direct role-restricted API denials, standard authorization error shape, and mapped repository/service tenant filtering coverage.
- `RoleBasedPermissionTests` covers role catalog permissions for owner, admin, compliance manager, contributor, auditor, and advisor; direct server-side permission checks; standard authorization problem details; auditor read-only behavior; denied direct mutations; and denied-action audit events.

The automated tests use synthetic, in-memory tenant data only. They do not use production customer data, real CUI, secrets, or customer uploads.

## Live Staging Smoke Attempt

Commands:

```bash
curl --fail --show-error --silent https://gccs-api-staging-19984.azurewebsites.net/health
env | rg '^GCCS_STAGING|^STAGING|AZURE|ConnectionStrings__GccsDatabase'
```

Result:

- Health check passed against the live staging API.
- No `GCCS_STAGING_ACCESS_TOKEN`, scoped smoke-test token, or equivalent authenticated staging API credential was present in the shell environment.
- Azure CLI management-plane calls for app settings did not return within the run window, so a non-interactive smoke credential could not be discovered safely.

## Blocker

| Blocker ID | Summary | Owner | Severity | Required resolution | Current status |
| --- | --- | --- | --- | --- | --- |
| PR33-STAGE-001 | Authenticated staging PR-3.3 tenant isolation and RBAC checks cannot run without a scoped staging API token or approved smoke-test identity. | Security owner | High | Provide a staging-only token or smoke identity for owner, admin, compliance manager, contributor, auditor, and advisor role contexts; run direct API cross-tenant and role-denial checks; attach sanitized outputs. | Open |

## Launch Disposition

PR-3.3 remains blocked. Do not proceed to PR-3.4, PR-4.1, or later production readiness stories until authenticated staging tenant isolation and RBAC evidence is attached.

The blocker does not expand the No-CUI posture and does not authorize real CUI handling. All future staging execution must use synthetic or non-sensitive data only.
