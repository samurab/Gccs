# Tenant Onboarding Guide

Review date: 2026-07-22.

Scope: internal onboarding of No-CUI pilot and paid tenants. This is an operator runbook, not a customer self-service guide.

## Decision

- Use the current `POST /api/admin/pilot-tenants` endpoint only for controlled pilot setup in local development or an explicitly approved, operator-controlled environment.
- Do not use the current endpoint as the permanent paid-tenant onboarding path. It is protected by tenant-level `ManageTenant`, which is also granted to customer Owners.
- Before paid onboarding, implement a platform control plane with a separate `ProvisionTenants` authorization policy, invitation-based owner activation, idempotency, subscription state, and an internal admin form.
- All MVP tenants start in `NoCui`. Neither pilot nor payment authorizes CUI storage or processing.

## Current Implementation Status

| Capability | Status | Evidence or limitation |
| --- | --- | --- |
| Create a No-CUI pilot tenant, owner membership, role, permissions, mode history, and audit entries in one transaction | Implemented | `PilotTenantProvisioningService` and `EfPilotTenantProvisioningRepository` |
| Validate tenant name, owner identity fields, and Owner/Admin role | Implemented | Application service validation |
| Require authorization on the provisioning endpoint | Partially implemented | Endpoint requires `ManageTenant`, but this is a customer tenant permission rather than a platform-operator permission |
| Local authorized request using development headers | Implemented | Development only; must remain disabled outside local development |
| Real invitation delivery to the initial owner | Partially implemented | Invitation workflow exists, but notification is a local placeholder; pilot provisioning activates the supplied user ID directly |
| Tenant selection/switching after provisioning | Planned | The web application has no complete operator-to-new-tenant or multi-membership switching flow |
| Internal tenant-provisioning admin form | Planned | No form calls `/api/admin/pilot-tenants` |
| Paid plan, subscription, payment, renewal, suspension, and cancellation state | Planned | No product-backed billing/subscription lifecycle was found |
| Duplicate-request prevention | Planned | Repeating the POST can create duplicate tenants |

## Roles

| Role | Responsibility |
| --- | --- |
| Platform Operator | Approves and executes tenant provisioning. This must become a non-customer platform role before paid onboarding. |
| Customer Success Owner | Confirms scope, onboarding contacts, training, support routing, and first-use monitoring. |
| Customer Tenant Owner | Accepts access, manages tenant users, and acknowledges the No-CUI posture. |
| Security/Support Owner | Handles access incidents, suspected CUI, tenant exposure, and prohibited-data escalation. |
| Billing Operator | Confirms paid entitlement and manages subscription lifecycle. This role and lifecycle are not yet implemented in the application. |

## Shared Entry Checklist

Complete these checks before creating either tenant type:

1. Assign a non-sensitive internal onboarding ID, such as `PILOT-003` or `CUSTOMER-014`. Do not put customer data or contract contents in repository evidence.
2. Confirm the tenant is approved for the No-CUI product boundary.
3. Give the customer the prohibited-data guidance and support route from `docs/production-readiness-pilot-onboarding.md`.
4. Confirm the initial owner email, display name, and identity-provider object ID. For Microsoft Entra, the application currently maps the token `oid` claim to the GCCS user ID.
5. Confirm the owner role. Use `Owner` unless an approved access design requires `Admin`.
6. Confirm production health, database connectivity, object storage, malware scanning, audit logging, alerts, and support ownership.
7. Stop if the customer requires CUI, classified data, ITAR/export-controlled technical data, sensitive government-furnished information, secrets, payroll data, SSNs, health data, or unrestricted security logs.

## Controlled Pilot Onboarding: Current Operator Procedure

### 1. Start and verify the local stack

From the repository root:

```bash
cd /Users/devups/Development/CodexProjects/Gccs
npm run dev
```

Expected services:

- API: `http://localhost:5062`
- Web: `http://127.0.0.1:5173`

Verify API health:

```bash
curl -i http://localhost:5062/health
```

Expected result: `200 OK`. Do not provision when health is degraded or the database is unavailable.

### 2. Verify the operator permission

```bash
curl -s http://localhost:5062/api/me/access \
  -H 'X-Gccs-Dev-Auth: true' \
  -H 'X-Gccs-Dev-Tenant: 11111111-1111-1111-1111-111111111111' \
  -H 'X-Gccs-Dev-User: 22222222-2222-2222-2222-222222222222' \
  -H 'X-Gccs-Dev-Permissions: ManageTenant'
```

Confirm the response contains the expected tenant ID, user ID, and `ManageTenant` permission. These headers are development-only credentials and must never be enabled in staging or production.

### 3. Prepare the request data

Use approved values:

| Field | Example | Rule |
| --- | --- | --- |
| `displayName` | `Aegis Pilot Workspace` | Required; 240 characters or fewer |
| `ownerUserId` | `70707070-7070-7070-7070-7070707070c1` | Required UUID; use the real identity-provider object ID outside synthetic testing |
| `ownerEmail` | `pilot.owner@example.com` | Required; must match the intended owner |
| `ownerDisplayName` | `Pilot Owner` | Required; 200 characters or fewer |
| `ownerRoleName` | `Owner` | Only `Owner` or `Admin` |
| `trialEndsAt` | `2026-08-31` | Pilot end date; optional in the API but required by this operating process |
| `setupReason` | `Provision approved No-CUI pilot PILOT-003.` | Use the non-sensitive onboarding ID; do not include contract contents |

### 4. Provision the pilot tenant

```bash
curl -i -X POST http://localhost:5062/api/admin/pilot-tenants \
  -H 'Content-Type: application/json' \
  -H 'X-Gccs-Dev-Auth: true' \
  -H 'X-Gccs-Dev-Tenant: 11111111-1111-1111-1111-111111111111' \
  -H 'X-Gccs-Dev-User: 22222222-2222-2222-2222-222222222222' \
  -H 'X-Gccs-Dev-Permissions: ManageTenant' \
  --data '{
    "displayName": "Aegis Pilot Workspace",
    "ownerUserId": "70707070-7070-7070-7070-7070707070c1",
    "ownerEmail": "pilot.owner@example.com",
    "ownerDisplayName": "Pilot Owner",
    "ownerRoleName": "Owner",
    "trialEndsAt": "2026-08-31",
    "setupReason": "Provision approved No-CUI pilot PILOT-003."
  }'
```

Expected result: `201 Created` with a response containing:

- A new tenant ID.
- `status: Active`.
- `dataHandlingMode: NoCui`.
- The intended owner user ID, email, and role.
- The approved setup reason.

Do not retry a timed-out request blindly. Check the database or audit history first because the current endpoint has no idempotency key.

### 5. Record and verify the result

Record only the internal onboarding ID, generated tenant ID, request correlation ID, operator, timestamp, and result. Do not record bearer tokens, invitation tokens, customer files, or sensitive contract data.

Verify all of the following before handing off access:

1. Tenant mode is `NoCui`.
2. Tenant status is `Active`.
3. Trial end date is correct.
4. Owner membership is active and has the intended role.
5. Tenant creation and membership creation audit entries exist.
6. No duplicate tenant was created.
7. Support routing and first-use monitoring are assigned.

### 6. Validate first use

The current provisioning endpoint does not complete production identity routing or browser tenant switching. Before inviting a real pilot user, prove that the user's authenticated `oid` maps to the stored `ownerUserId` and that the request resolves to the generated GCCS tenant ID.

After access is proven, validate this No-CUI workflow with synthetic or approved non-sensitive data:

1. Sign in as the tenant Owner.
2. Confirm `/api/me/access` returns the new tenant ID and expected role.
3. Acknowledge the No-CUI notice.
4. Enter company and contract metadata without uploading customer contract contents unless separately approved as non-sensitive.
5. Review clauses and obligations.
6. Assign an obligation owner and status.
7. Add allowed evidence metadata and, only when approved, a non-sensitive file.
8. Generate a current report artifact.
9. Confirm the relevant actions appear in tenant-scoped audit history.
10. Record first-use monitoring using the non-sensitive onboarding ID.

## Paid Tenant Onboarding: Required Production Procedure

Paid onboarding is not currently product-complete. Do not treat a successful pilot-provisioning response as proof of billing entitlement, production identity readiness, or paid-service activation.

Implement and enforce this sequence before onboarding a paid tenant:

1. **Commercial approval:** Confirm signed terms, selected plan, billing contact, effective date, renewal date, and payment state in the authoritative billing system.
2. **Platform authorization:** Require a dedicated `ProvisionTenants` app role or policy issued only to internal platform operators. Do not reuse customer `ManageTenant`.
3. **Idempotent request:** Send an immutable commercial customer ID and idempotency key. Enforce uniqueness so retries return the original result rather than creating another tenant.
4. **Create pending tenant:** Create the tenant as `PendingActivation`, with `NoCui`, plan, subscription ID, entitlement dates, and audit metadata.
5. **Invite the owner:** Create a single-use, expiring invitation bound to the verified email. Do not activate an arbitrary client-supplied user ID.
6. **Bind identity:** On acceptance, verify authenticated email and identity-provider subject/object ID, then create the active owner membership.
7. **Activate entitlement:** Change the tenant to `Active` only after subscription and owner activation checks pass.
8. **Verify access:** Confirm tenant context, role, permissions, No-CUI acknowledgement, audit entries, and tenant isolation.
9. **Operational handoff:** Assign customer success, support, security escalation, monitoring, and renewal owners.
10. **First-use acceptance:** Complete the same synthetic/non-sensitive workflow used for the pilot and record the result.

Required paid lifecycle states should include at least `PendingActivation`, `Active`, `PastDue`, `Suspended`, `Cancelled`, and `Archived`. Authorization must deny tenant writes when the commercial state does not allow service, while preserving controlled read/export access according to contract and retention policy.

## Internal Admin Form: Best Implementation

Build an internal route such as `/platform/tenants/new`, separate from customer Tenant Settings.

The form should collect:

- Onboarding type: Pilot or Paid.
- Internal customer/onboarding ID.
- Tenant display name.
- Verified owner email and display name.
- Pilot end date, or paid plan and subscription identifier.
- Setup reason.
- Explicit No-CUI confirmation.

The form must:

1. Require the platform `ProvisionTenants` policy on both the page and API. API authorization is authoritative.
2. Use an owner invitation instead of accepting an arbitrary owner UUID from the browser.
3. Send an idempotency key and disable duplicate submission while a request is running.
4. Show validation, authorization, conflict, dependency-failure, and success states.
5. Display the generated tenant ID and onboarding status after success.
6. Never display or log access tokens, invitation tokens, sensitive customer data, or raw uploaded content.
7. Audit provisioning, invitation, activation, suspension, cancellation, and operator overrides.

## Stop Conditions

Stop onboarding and escalate when:

- The operator lacks the correct platform authorization.
- Production development authentication is enabled.
- The owner identity cannot be verified.
- The tenant already exists or a prior request has an unknown result.
- Billing or contract state is not approved for a paid tenant.
- Any dependency required for login, persistence, audit, upload controls, reporting, or monitoring is unhealthy.
- The customer requests prohibited data handling or CUI capability.
- Tenant isolation, RBAC, audit history, or first-use verification fails.

## Principal Risks

1. **Privilege escalation:** Customer Owners currently receive `ManageTenant`; exposing provisioning through that permission allows customer-controlled tenant creation. Replace it with platform authorization.
2. **Identity mismatch:** Directly accepting `ownerUserId` can create an owner who cannot authenticate or can overwrite an existing user's profile. Use verified invitation acceptance and immutable external identity mapping.
3. **Duplicate and inconsistent state:** The endpoint lacks idempotency and subscription state. Retries can create duplicate tenants, and payment changes cannot reliably suspend or reactivate service.
4. **Tenant-context gap:** A generated GCCS tenant ID is not automatically present in a user's token or browser context. Implement explicit membership-based tenant selection and server-validated context switching.
5. **Operational scaling:** Manual `curl` onboarding provides no queue, approval record, searchable status, or safe retry workflow. Use an internal admin UI backed by an idempotent orchestration service.

## Pre-Publication Checklist

Before treating this guide as a production paid-tenant runbook, verify:

- The UI exposes only the platform-admin flow described here.
- The API enforces `ProvisionTenants`, idempotency, invitation binding, tenant isolation, and subscription state.
- Tests prove platform RBAC denial, duplicate-request handling, owner identity binding, cross-tenant isolation, audit logging, and paid lifecycle transitions.
- Development authentication is disabled outside local development.
- Wording does not claim certification, legal advice, government approval, guaranteed compliance, secure CUI storage, or audit readiness.
- Every tenant remains within the No-CUI product posture unless a separately reviewed and implemented capability explicitly changes that boundary.
