# Tenant Onboarding Guide

Review date: 2026-08-15.

Scope: internal onboarding of No-CUI pilot and paid tenants. This is an operator runbook, not customer self-service.

## Decision

- Use the internal admin form at `/platform/tenants/new` for new tenant onboarding.
- Do not use customer `ManageTenant` permission for tenant creation.
- Do not ask a customer to provide or invent an `ownerUserId`. GCCS creates a pending invitation and binds the authenticated identity when the Owner accepts it.
- All MVP tenants start in `NoCui`. Pilot status or payment does not authorize CUI storage or processing.

## Current Implementation Status

| Capability | Status | Evidence or limitation |
| --- | --- | --- |
| Dedicated internal admin route | Implemented | `/platform/tenants/new` |
| Platform customer directory | Implemented | `/platform/customers` provides bounded server-side search, filters, sorting, attention states, and customer details containing operational metadata only |
| Least-privilege platform authorization | Implemented | `ViewPlatformCustomers`, `ManageTenantOnboarding`, and `ManageTenantSubscriptions` are separate policies; `ProvisionTenants` remains a compatibility alias and `Gccs.PlatformOperator` retains all platform operations |
| Platform-only tenant provisioning authorization | Implemented | `Platform.ManageTenantOnboarding` policy; customer roles do not receive it |
| Pilot and Paid form modes with conditional fields | Implemented | Paid mode requires plan, subscription reference, and commercial approval confirmation |
| Provider-independent pilot subscription lifecycle | Implemented | Provisioning creates a versioned subscription; activation, extension, grace-period entry, cancellation, and commercial conversion are audited |
| Request-time pilot enforcement | Implemented | Active pilots have full access; grace-period pilots retain safe reads and GET-based exports while tenant mutations are rejected; expired or cancelled pilots are rejected |
| Explicit No-CUI confirmation | Implemented | Enforced in the UI and application service |
| Pending tenant, Owner role, invitation, mode history, and audit entries | Implemented | Created in one EF Core save transaction |
| Idempotent submission and duplicate reference protection | Implemented | Unique request key, request fingerprint, customer reference, and subscription reference |
| Platform cancellation of pending onboarding | Implemented | Requires a reason; revokes delivery, archives the inactive tenant, preserves the record, and writes cancellation audit entries atomically |
| Initial Owner activation through authenticated invitation acceptance | Implemented | Acceptance validates authenticated email, creates membership, and activates the tenant |
| External invitation email delivery | Implemented; configuration required | Durable invitation queue, bounded retries, Azure Communication Services adapter, and delivery audit records are implemented; Azure resource and sender configuration are deployment dependencies |
| Owner invitation-acceptance page | Implemented | `/invitations/accept` receives the emailed invitation parameter, requires authentication, and validates the signed-in email |
| Membership-based tenant selection after sign-in | Implemented | `GET /api/me/tenants` returns only the authenticated user's memberships; `POST /api/me/tenant-selection` revalidates active user, membership, and tenant state before persisting the preference |
| Workspace selector | Implemented | The sidebar selector shows the user's memberships, disables unavailable tenants, and reloads tenant-scoped state after a successful switch |
| Automated billing verification and paid lifecycle | Partially implemented | The form records a confirmed subscription reference; no billing provider validates or updates it |
| Legacy tenant subscription classification | Partially implemented | The migration backfills platform-onboarded tenants; older tenants without a platform onboarding remain temporarily grandfathered until explicitly classified |

## Roles

| Role | Responsibility |
| --- | --- |
| Customer Operations Viewer | Internal operator authorized by `ViewPlatformCustomers`. Can read operational customer metadata but cannot create tenants or mutate subscriptions. |
| Onboarding Operator | Internal operator authorized by `ManageTenantOnboarding` or legacy `ProvisionTenants`. Creates and manages pending tenants. |
| Subscription Operator | Internal operator authorized by `ManageTenantSubscriptions` or legacy `ProvisionTenants`. Manages Pilot lifecycle transitions. |
| Platform Operator | Approved internal account assigned `Gccs.PlatformOperator`; receives all implemented platform operations. |
| Customer Success Owner | Confirms scope, contacts, training, support routing, and first-use monitoring. |
| Customer Tenant Owner | Named customer administrator who accepts the Owner invitation and manages the workspace. |
| Security/Support Owner | Handles access incidents, suspected CUI, tenant exposure, and prohibited-data escalation. |
| Billing Operator | Confirms paid entitlement in the billing system of record. Automated billing integration is not implemented. |

## Before Onboarding

1. Assign a non-sensitive reference such as `PILOT-003` or `CUSTOMER-014`.
2. Confirm the customer accepts the No-CUI product boundary.
3. Deliver the prohibited-data guidance and support route in `docs/production-readiness-pilot-onboarding.md`.
4. Confirm the initial Owner's individual work email and display name. Do not use a shared administrator mailbox.
5. For a pilot, confirm the pilot end date.
6. For a paid tenant, confirm plan code and subscription reference in the billing system of record.
7. Confirm API, database, audit logging, authentication, alerts, and support ownership are operational.
8. Stop if the customer requires CUI, classified information, ITAR/export-controlled data, sensitive government-furnished information, secrets, payroll data, SSNs, health data, or unrestricted security logs.

Pilot dates use UTC end-exclusive semantics: a displayed end date remains active through that calendar date and enters the configured grace period at `00:00:00Z` on the following day. The default maximum pilot duration is 90 days and the default grace period is 7 days; deployment configuration can adjust them within the implemented guardrails.

## Customer Directory And Pilot Subscription Operations

Open `/platform/customers` to list Pilot and Paid customers. The directory exposes tenant, onboarding, initial Owner invitation, and provider-independent subscription metadata. It does not expose tenant evidence, contracts, reports, workspace audit contents, or invitation tokens.

Open a customer detail record to use these subscription actions when authorized by `ManageTenantSubscriptions`:

1. **Extend** moves the end date later, recalculates the grace-period end, and requires a reason.
2. **Start grace period** ends full access immediately and retains safe reads and GET-based exports for the configured grace period.
3. **Cancel pilot** denies workspace access immediately without deleting tenant data or audit history.
4. **Convert to commercial** removes the pilot dates, records the approved commercial plan and external subscription reference, and preserves tenant data, memberships, audit history, and `NoCui` posture.

Each request includes the displayed subscription version and an `Idempotency-Key` header. Replaying the same key and payload returns the original transition result without another mutation or audit event; reusing the key for different input or submitting a stale version returns `409 Conflict`. `ViewPlatformCustomers` alone cannot use these operations.

## Local Development Procedure

### 1. Start the application

From the repository root:

```bash
cd /Users/devups/Development/CodexProjects/Gccs
npm run dev
```

Expected services:

- API: `http://localhost:5062`
- Web: `http://localhost:5173`

Verify health:

```bash
curl -i http://localhost:5062/health
```

Expected result: `200 OK`.

### 2. Open the internal admin form

Open:

```text
http://localhost:5173/platform/tenants/new
```

The default local configuration grants no platform permission. Start Vite with an explicit development-only permission, for example `VITE_GCCS_DEV_PLATFORM_PERMISSIONS=ViewPlatformCustomers,ManageTenantOnboarding,ManageTenantSubscriptions`. Development authentication must remain disabled outside local development.

### 3. Enter Pilot values

1. Select **Pilot**.
2. Enter a unique customer reference, for example `PILOT-003`.
3. Enter the tenant display name.
4. Enter the pilot end date.
5. Enter a non-sensitive setup reason.
6. Enter the designated Owner's email and display name.
7. Select **No-CUI boundary confirmed**.
8. Select **Create pending tenant**.

Expected result:

- Tenant status: `PendingActivation`.
- Onboarding status: `PendingOwnerAcceptance`.
- Data handling: `NoCui`.
- Owner invitation: `Pending`.
- Email delivery: `Queued`, then `Sent` when the provider completes delivery.
- No user or membership is created before invitation acceptance.

### 4. Enter Paid values

1. Select **Paid**.
2. Enter a unique customer reference, for example `CUSTOMER-014`.
3. Enter the tenant display name.
4. Enter the approved plan code.
5. Enter the unique subscription reference.
6. Enter a non-sensitive setup reason.
7. Enter the designated Owner's email and display name.
8. Select **No-CUI boundary confirmed**.
9. Select **Commercial approval confirmed** only after checking the billing system of record.
10. Select **Create pending tenant**.

The application records the operator's confirmation. It does not independently verify payment or subscription state.

### 5. Record the result

Record only:

- Customer reference.
- Tenant ID.
- Onboarding ID.
- Operator identity.
- Request correlation ID.
- Timestamp and result.

Do not record bearer tokens, invitation tokens, customer files, or sensitive contract data.

### 6. Verify delivery and activation

1. Confirm **Email delivery** changes to `Sent`. If it is `Failed`, resolve provider configuration before selecting **Resend invitation**.
2. The Owner opens the emailed activation link and signs in through Microsoft Entra using the exact invited email address.
3. The Owner enters their display name and selects **Accept invitation**.
4. Confirm the activation page reports **Workspace activated**.
5. Confirm Pilot tenant status is `Trialing`, or Paid tenant status is `Active`.
6. Confirm the Owner membership and invitation-delivery/acceptance audit entries exist.

### 7. Retry safely

The form retains one request key while a submission is unresolved. A retry with the same key and same values returns the original tenant. Reusing the key with different values returns `409 Conflict`.

Do not select **Provision another tenant** until the current result is known; that action generates a new request key.

### 8. Cancel an incorrect pending onboarding

Cancellation is available only while onboarding is `PendingOwnerAcceptance`, the tenant is `PendingActivation`, and the Owner invitation is `Pending`.

1. Under **Pending tenant onboardings**, locate the incorrect tenant by display name, customer reference, and Owner email.
2. Select the cancel icon for that row.
3. Enter a specific, non-sensitive cancellation reason.
4. Select **Confirm cancellation**.
5. Confirm the onboarding no longer appears in the pending list.
6. Verify the preserved record has onboarding status `Cancelled`, tenant status `Archived`, invitation status `Revoked`, and email delivery `Cancelled`.
7. Verify audit history records the platform operator, reason, timestamp, onboarding transition, and invitation revocation.

Cancellation clears an unused activation token and prevents the delivery worker from claiming the invitation. If the provider accepted an email before cancellation, its link remains unusable because the invitation is revoked. Activated onboarding cannot be cancelled through this operation.

## API Procedure

Use the API only when the form is unavailable. The platform endpoint is:

```text
POST /api/platform/tenants
```

Local pilot example:

```bash
curl -i -X POST http://localhost:5062/api/platform/tenants \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: pilot-003-initial-provisioning' \
  -H 'X-Gccs-Dev-Auth: true' \
  -H 'X-Gccs-Dev-User: 22222222-2222-2222-2222-222222222222' \
  -H 'X-Gccs-Dev-Tenant: none' \
  -H 'X-Gccs-Dev-Platform-Permissions: ProvisionTenants' \
  --data '{
    "onboardingType": "Pilot",
    "customerReference": "PILOT-003",
    "displayName": "Aegis Pilot Workspace",
    "ownerEmail": "pilot.owner@example.com",
    "ownerDisplayName": "Pilot Owner",
    "trialEndsAt": "2026-08-31",
    "planCode": null,
    "subscriptionReference": null,
    "setupReason": "Provision approved No-CUI pilot PILOT-003.",
    "confirmsNoCui": true,
    "commercialApprovalConfirmed": false
  }'
```

Expected initial response: `201 Created`. An identical replay returns `200 OK` with `isReplay: true`.

List pending onboarding:

```text
GET /api/platform/tenant-onboardings?page=1&pageSize=25&status=PendingOwnerAcceptance
```

Cancel pending onboarding:

```text
POST /api/platform/tenant-onboardings/{onboardingId}/cancel
```

Request body:

```json
{
  "reason": "Duplicate pilot onboarding superseded before Owner activation."
}
```

Only an authenticated operator with `ManageTenantOnboarding`, legacy `ProvisionTenants`, or `Gccs.PlatformOperator` can list or cancel platform onboarding. Unknown IDs return `404`; non-pending or activated records return `409`.

## Staging and Production Authorization

1. Define the Microsoft Entra application role `Gccs.PlatformOperator` for the GCCS API.
2. Assign it only to approved internal platform operators through a controlled group.
3. Require MFA and normal access-review controls for that group.
4. Configure `Authentication:Authority` and `Authentication:Audience` for the API.
5. Configure `VITE_MSAL_CLIENT_ID`, `VITE_MSAL_TENANT_ID`, and `VITE_MSAL_API_SCOPE` for the web app.
6. Confirm `Security:DevelopmentAuth:Enabled=false`.
7. Apply the `AddPlatformTenantOnboarding`, `AddInvitationDeliveryWorkflow`, and `AddPlatformTenantCancellation` database migrations before deploying the API.
8. Verify an operator receives `canProvisionTenants: true` from `GET /api/platform/me/access`.
9. Verify a customer Owner with `ManageTenant` receives `403 Forbidden` from `POST /api/platform/tenants`.
10. Verify that same customer receives `403 Forbidden` from the platform onboarding list and cancellation endpoints.

## Invitation Email Configuration

The delivery worker is disabled by default. Enabling it without complete configuration causes API startup to fail.

1. Provision Azure Communication Services and a connected Email Communication Services resource.
2. Configure an Azure-managed or verified custom sending domain and record its `MailFrom` sender address.
3. Prefer the API App Service managed identity. Grant it only the Azure Communication Services email permissions required to send.
4. Configure these API App Service settings:

```text
InvitationDelivery__Enabled=true
InvitationDelivery__Provider=AzureCommunicationServices
InvitationDelivery__PublicWebBaseUrl=https://<web-host>
InvitationDelivery__Endpoint=https://<communication-resource>.communication.azure.com
InvitationDelivery__UseManagedIdentity=true
InvitationDelivery__SenderAddress=DoNotReply@<verified-domain>
InvitationDelivery__PollIntervalSeconds=5
InvitationDelivery__LeaseMinutes=5
InvitationDelivery__MaximumAttempts=5
```

Use `InvitationDelivery__ConnectionString` with `UseManagedIdentity=false` only when managed identity is unavailable. Store that secret in approved secret storage; do not commit or print it.

For local activation testing, set `VITE_GCCS_DEV_EMAIL` to the exact invited Owner email and `VITE_GCCS_DEV_USER_ID` to a stable test UUID before starting Vite. This development shortcut must not be deployed.

## Owner Activation

The API activation sequence is:

1. Owner authenticates through Microsoft Entra.
2. GCCS reads the validated user ID and email claims.
3. Owner submits the single-use invitation token.
4. GCCS verifies that the authenticated email matches the invitation.
5. GCCS creates the user and Owner membership.
6. Pilot tenant changes to `Trialing`; Paid tenant changes to `Active`.
7. GCCS records invitation acceptance and onboarding activation in audit history.

The admin result intentionally does not expose the invitation token. The database stores only its SHA-256 hash; the raw single-use token exists only in the outbound activation URL.

## Verification Checklist

Before handoff, verify:

1. Tenant mode is `NoCui`.
2. Tenant status is appropriate for the onboarding stage.
3. Pilot end date or paid subscription reference is correct.
4. Owner invitation email matches the approved individual.
5. Tenant creation and invitation audit entries exist.
6. No duplicate customer, subscription, or tenant record exists.
7. After acceptance, Owner membership and activation audit entries exist.
8. Support routing and first-use monitoring are assigned.
9. Tenant isolation and RBAC denial tests pass.
10. Incorrect pending onboarding is cancelled through the platform operation, not deleted or modified directly in the database.

## Stop Conditions

Stop onboarding when:

- The operator lacks `ProvisionTenants`.
- Development authentication is enabled outside local development.
- Owner email cannot be verified.
- A customer or subscription reference already exists.
- A previous request has an unresolved result.
- Paid commercial approval cannot be confirmed.
- Authentication, persistence, audit, reporting, upload controls, or monitoring is unhealthy.
- The customer requests prohibited data handling or CUI capability.
- Tenant isolation, RBAC, audit history, or first-use verification fails.

## Remaining Risks

1. Azure Communication Services, a sender domain, managed-identity permission, and production App Service settings are external deployment dependencies; code deployment alone does not send email.
2. Provider acceptance does not prove inbox placement. Bounce and complaint webhook processing is not implemented; operators must use provider telemetry for delivery incidents.
3. The legacy `users.tenant_id` home-tenant column remains for compatibility with tenant-scoped user lookup and SCIM flows. Tenant switching authorization uses `tenant_memberships`; removal of the legacy ownership column requires a separate identity-model migration.
4. Paid provisioning trusts an internal operator confirmation; billing-provider verification, renewal, past-due, suspension, cancellation, and archival automation remain planned.
5. The web production bundle still reports a size warning; route-level code splitting should be completed as the internal control plane grows.

## Pre-Publication Checklist

- The admin route is available only to platform operators.
- The API enforces `ProvisionTenants`, idempotency, invitation binding, No-CUI posture, and audit logging.
- Tests prove customer-role denial, duplicate-request behavior, validated cancellation, queue exclusion, cancellation audit history, Owner activation, token replay rejection, retry behavior, tenant selection validation, and tenant isolation.
- Development authentication is disabled outside local development.
- Documentation does not claim certification, legal advice, government approval, guaranteed compliance, secure CUI storage, or audit readiness.
