# Production Readiness Staging Security Evidence

Story: PR-3.3 - Verify Tenant Isolation And RBAC In Staging.

Evidence status: Blocked for full role-matrix staging execution; automated backend/API coverage passed and live Owner-role staging probes passed.

Review date: 2026-07-02.

Staging API: `https://gccs-api-staging-19984.azurewebsites.net`.

Staging health result: passed. `GET /health` returned `status: ok`, `dataPosture: No-CUI / compliance management only`, and dependency signals for `background-jobs`, `object-storage`, `postgresql`, and `redis`.

This artifact does not close PR-3.3. It records the current evidence, the failed prerequisite for complete authenticated staging authorization checks, and the exact work required before the production readiness sequence can proceed to PR-3.4.

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
- The in-app browser was signed in to the GCCS Staging workspace as the current Owner user.
- Browser-contained API calls used the active session token without printing or storing the token.
- Owner-context calls to `/api/me/access`, `/api/tenant-members`, missing direct resource IDs for contracts, evidence, tasks, and evidence-package reports, and `/api/reports/exports/audit-log` completed with expected status codes and no stack-trace leakage.
- Sanitized Owner-session output is attached at `output/playwright/production-readiness/pr-3.3/owner-session-probes.json`.
- Azure CLI management-plane calls for app settings did not return within the run window, so a non-interactive smoke credential or staging database connection could not be discovered safely.

Owner-session live staging evidence is useful but insufficient for PR-3.3 closure. It does not prove Admin, Compliance Manager, Contributor, Auditor, or Advisor direct API denials, and the random missing-ID checks do not prove denial against known records owned by a different tenant.

## Admin Cycle Attempt

An attempted controlled Admin-role cycle used SCIM provisioning to map a temporary smoke group to `Admin` and then restore `Owner`.

Result: not valid PR-3.3 role evidence.

Reason: SCIM provisioning created a separate provisioned user instead of changing the signed-in user's active membership. The signed-in browser session remained `Owner`, so the intended Admin-denial probe was executed as Owner and created an unintended staging tenant named `PR-3.3 blocked admin tenant`.

Cleanup completed:

- The temporary SCIM token was revoked.
- The separate SCIM-created smoke member was deactivated.
- The original signed-in user remained active as `Owner`.
- The unintended orphan tenant named `PR-3.3 blocked admin tenant` was located in staging PostgreSQL with exactly one matching row, zero memberships, and `NoCui` posture.
- The orphan tenant was archived through staging database maintenance, and a sanitized `Archived` audit entry was inserted with correlation id `pr-3.3-orphan-tenant-cleanup`.
- A temporary PostgreSQL firewall rule for the current operator IP was removed after cleanup; verification returned zero remaining rules with that name.

Sanitized evidence is attached at:

- `output/playwright/production-readiness/pr-3.3/admin-cycle-and-cleanup.json`
- `output/playwright/production-readiness/pr-3.3/orphan-tenant-verify.json`
- `output/playwright/production-readiness/pr-3.3/orphan-tenant-cleanup.json`
- `output/playwright/production-readiness/pr-3.3/orphan-tenant-firewall-cleanup.json`

## Blocker

| Blocker ID | Summary | Owner | Severity | Required resolution | Current status |
| --- | --- | --- | --- | --- | --- |
| PR33-STAGE-001 | Authenticated staging PR-3.3 tenant isolation and RBAC checks cannot run for the full role matrix with only the current Owner browser session. | Security owner | High | Provide staging-only tokens or smoke identities for Admin, Compliance Manager, Contributor, Auditor, and Advisor role contexts; run direct API cross-tenant and role-denial checks; attach sanitized outputs. | Open |
| PR33-STAGE-002 | An unintended orphan staging tenant was created during an invalid Admin-cycle probe. | Engineering lead | Medium | Use database or Azure/admin access to locate and archive tenant `PR-3.3 blocked admin tenant`; attach cleanup evidence. | Closed on 2026-07-02; tenant archived, audit entry inserted, and temporary firewall rule removed. |

## Launch Disposition

PR-3.3 remains blocked. Do not proceed to PR-3.4, PR-4.1, or later production readiness stories until full authenticated staging tenant isolation and RBAC evidence is attached.

The blocker does not expand the No-CUI posture and does not authorize real CUI handling. All future staging execution must use synthetic or non-sensitive data only.
