# Azure environment remediation — September 26, 2026

Status: internal operational evidence; live changes are listed separately from
unmerged repository changes and pending work. No compliance certification claim.

## Implemented live

- Added `github-staging-environment` and `github-production-environment`
  federated credentials to the existing deployment applications. Issuer is
  `https://token.actions.githubusercontent.com`; audience is
  `api://AzureADTokenExchange`; subjects are respectively
  `repo:samurab/Gccs:environment:staging` and
  `repo:samurab/Gccs:environment:Production`. Readback matches canonical GitHub
  environment casing. Existing identity privileges were not expanded.
- Added `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` in both
  environments. Retained legacy deployment secrets pending a successful reviewed
  OIDC workflow run. Metadata validation does not prove token exchange.
- Added staging `STAGING_API_APP_NAME`, `STAGING_RESOURCE_GROUP`,
  `STAGING_MSAL_CLIENT_ID`, `STAGING_MSAL_TENANT_ID`, `STAGING_MSAL_API_SCOPE`,
  `STAGING_CUSTOMER_MSAL_CLIENT_ID`, `STAGING_CUSTOMER_MSAL_TENANT_ID`,
  `STAGING_CUSTOMER_MSAL_TENANT_SUBDOMAIN`, `STAGING_CUSTOMER_MSAL_API_SCOPE`,
  `STAGING_CUSTOMER_AUTHORITY`, `STAGING_CUSTOMER_AUDIENCE` from observed current
  deployment settings/runtime metadata. The seven public MSAL variables were
  also added at repository scope for preview generation; no credentials copied.
- Added production `PRODUCTION_POSTGRES_SERVER_NAME`,
  `PRODUCTION_APP_INSIGHTS_NAME`, `PRODUCTION_SERVICE_PLAN_ID` using existing
  resources. Existing host/resource names and IDs were preserved.
- Set staging App Service `healthCheckPath` to `/health` (previously null).
  Before and after, HTTP health reported `ok`, identical release commit, and
  healthy PostgreSQL, Redis, background jobs and object storage. Production
  platform configuration was not changed or restarted.
- Subsequently, with explicit authorization, provisioned the separate production
  External ID resource `fedrilcustomersproduction` in the production resource
  group, United States, Base/A0 MAU billing. Provisioning state is `Succeeded`.
  Tenant ID: `34072c06-0ccc-4007-9b04-97198233f1b9`. Created customer API
  `2f8a5df1-4528-483d-a896-9993a9cc52ec` with enabled delegated
  `access_as_customer` scope and access-token version 2; created public SPA
  `8a07586a-acb4-4087-9365-44c9fc4d3369`, its required delegated permission,
  API preauthorization, and both service principals. No app secrets created.
  SPA redirects cover only the production SWA host and `www.fedril.com`, each
  at root, `/app`, and `/invitations/accept`.
- Created Email OTP signup/sign-in flow
  `a77ba4ec-1244-48f9-bba7-a2467c2ecf01`, scoped only to that production SPA,
  matching staging's email-only customer flow. Initially set all six production
  customer GitHub variables below. Following the user's revised preparation-only
  instruction, removed those six newly added variables to prevent queued
  activation; retained the prepared tenant and registrations. OpenID discovery
  and a synthetic PKCE authorize GET
  returned the sign-in page with HTTP 200 and no observed AADSTS error. No email
  submitted, customer created, authorization code redeemed or login completed.

## Prepared repository changes

Deployment OIDC login, one authoritative staging deployment path, strict paired
customer/workforce configuration validation, and public settings checks are
prepared in the remediation branch. They take effect only after review/merge.
Production promotion requires complete dedicated customer identity metadata;
legacy rollback compatibility does not authorize an incomplete new release.

The weekly public-settings job is partial observation, not fully operational
continuous drift detection: its Production job uses the protected environment
approval gate and the existing deployment identity's Contributor privileges.
The script performs reads only; this is not a dedicated least-privilege reader
identity. Terraform planning remains blocked until its state baseline exists.

`tools/release/check-azure-settings.mjs` queries seven allowlisted public keys,
rejects malformed/duplicate provider records, and suppresses provider values and
diagnostics. Terraform still excludes secret application settings. Eight checker
tests pass, including malformed JSON, non-array, duplicate and invalid-value
cases. Planner independently reported all five changed workflow files pass
checksum-verified actionlint 1.7.12. Re-run the combined release suite on the final
commit rather than treating an earlier count as final-commit verification.

## Pending decisions and operational limits

1. **Production customer identity activation:** the separate realm was created
   under the earlier explicit provisioning authorization, before the user revised
   the sequence to verification/proposal first. It is a prepared resource, not
   live product authentication and not an unexecuted proposal. The following
   variables were removed from GitHub pending the final activation decision:
   `PRODUCTION_CUSTOMER_MSAL_CLIENT_ID`, `PRODUCTION_CUSTOMER_MSAL_TENANT_ID`,
   `PRODUCTION_CUSTOMER_MSAL_TENANT_SUBDOMAIN`,
   `PRODUCTION_CUSTOMER_MSAL_API_SCOPE`, `PRODUCTION_CUSTOMER_AUTHORITY`, and
   `PRODUCTION_CUSTOMER_AUDIENCE`. Redirects, exposed scope, SPA preauthorization,
   issuer discovery and audience metadata were read back. The API validator
   accepts both `api://<application-id>` and the bare v2 audience GUID. A paired
   reviewed API/frontend deployment and real customer/workforce login remain
   pending. Live production remains workforce-only. Preserve rollback based on
   live realm state. Staging authentication and subscription ownership unchanged.

   Prepared public values (not deployment approval):

   | Variable suffix after `PRODUCTION_CUSTOMER_` | Prepared value |
   | --- | --- |
   | `MSAL_CLIENT_ID` | `8a07586a-acb4-4087-9365-44c9fc4d3369` |
   | `MSAL_TENANT_ID` | `34072c06-0ccc-4007-9b04-97198233f1b9` |
   | `MSAL_TENANT_SUBDOMAIN` | `fedrilcustomersproduction` |
   | `MSAL_API_SCOPE` | `api://2f8a5df1-4528-483d-a896-9993a9cc52ec/access_as_customer` |
   | `AUTHORITY` | `https://fedrilcustomersproduction.ciamlogin.com/34072c06-0ccc-4007-9b04-97198233f1b9/v2.0` |
   | `AUDIENCE` | `api://2f8a5df1-4528-483d-a896-9993a9cc52ec` |

   Geography is `United States` (CIAM geography, not an East US deployment region).
   Base/A0 MAU billing is linked to existing subscription
   `aed211a7-3460-46db-a47f-6145e6b5e6e4`, effective September 26 at
   15:50:27 UTC. No subscription transfer or premium add-on was performed. MAU
   billing metadata is not a bill estimate, free-tier eligibility proof or a
   guarantee of zero charges. No monthly-price claim is made. Only one accessible
   subscription was enumerated; its CIAM inventory contains staging and the new
   production resource. No previously existing production CIAM resource was found
   in that scope; directories outside accessible scope are not ruled out.
2. **Terraform:** no backend or dedicated drift identity created and no readiness
   flag enabled. See `infra/terraform/environments/production/README.md` for the
   prepared private state/ephemeral runner option and 27 current import targets.
   Configure `AZURE_PRODUCTION_TERRAFORM_CLIENT_ID`,
   `PRODUCTION_ALERT_EMAIL_ADDRESS`, `PRODUCTION_TF_STATE_RESOURCE_GROUP`,
   `PRODUCTION_TF_STATE_STORAGE_ACCOUNT`, `PRODUCTION_TF_STATE_CONTAINER`,
   `PRODUCTION_TF_STATE_KEY`; set `PRODUCTION_TERRAFORM_STATE_READY` only after
   restricted remote state, reviewed import-only adoption and a zero-drift plan.
   Do not use the audit account: its global lifecycle deletes blobs after 365 days.
3. **Production health check:** `/health` is anonymous and returns 503 for failed
   dependencies, but configuring the platform can restart the app. Schedule the
   production change and repeat readiness/release checks before and after.
4. **Audit export:** existing Activity/AuditLogs export preserved. Sign-in export
   licensing was not established; directory assigned-plan inventory was empty.
   No added export categories, irreversible locks or storage access changes.
5. **Efficiency/cleanup:** retain current compute size; observed utilization did
   not show exhaustion. Shared staging/production plan is a known failure-domain
   dependency. No resource deletion, email cleanup, secret removal or firewall
   narrowing performed. Database public-access removal requires a proven private
   migration runner first. Duplicate rules/workflows/resources require usage
   confirmation before destructive cleanup.

## Verification and next execution

```sh
node --test tools/release/*.test.mjs
terraform -chdir=infra/terraform/environments/production fmt -check
git diff --check
# With approved environment variables and authenticated Azure CLI:
node tools/release/check-azure-settings.mjs staging
```

The live staging seven-key contract passed. Authentication transactions, OIDC
exchange, uploads/email, remote-state import, production restart and production
deployment were not executed. No customer data, tokens, secret values or raw
customer logs were collected in this artifact. Health checks prove connectivity,
not authorization or complete functional correctness.

## Rollback

To reverse staging platform health monitoring, restore its previous null
`healthCheckPath` and verify health/release identity. If abandoning OIDC before
use, delete only the two named federation records and newly added variable names
listed above; do not delete deployment identities or existing credentials/RBAC.
Legacy secrets remain available until a reviewed OIDC deployment is proven.
Revert reviewed repository commits through normal Git review; no production
application or database rollback is needed for this preparation itself.
