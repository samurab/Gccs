# Production Terraform Adoption

Status: configuration implemented; remote-state bootstrap and imports not run.

This directory models the existing commercial No-CUI production environment.
It does not describe a FedRAMP-authorized boundary and must not be used to make
FedRAMP, CUI, government-approval, or audit-readiness claims.

## Safe adoption sequence

1. Rotate the production Redis credential exposed during the sanitized export
   attempt and update the API setting through the protected deployment path.
2. Create a dedicated encrypted Azure Storage backend with private access,
   versioning, soft delete, restricted state-reader roles, and state locking.
3. Run `terraform init` with the approved backend settings. Never commit state,
   backend credentials, application settings, tokens, or generated plan files.
4. Supply `subscription_id`, the approved alert mailbox, and the current App
   Service plan ID through protected CI variables.
5. Review every `import` target, then run a plan that performs imports only.
   Keep plans outside Git in the approved protected workspace: they can contain
   sensitive provider values. Inspect plan JSON there and permit only `no-op`
   resource actions with import metadata. Reject create, update, delete and
   replacement actions during initial adoption; import blocks alone do not make
   apply safe.
6. Reject any plan containing delete or replacement actions. Reconcile drift in
   configuration, one resource class at a time, before enabling scheduled plans.
7. Set `PRODUCTION_TERRAFORM_STATE_READY=true` only after the imported state and
   a non-destructive plan have received infrastructure and security approval.

The current API App Service uses a plan located in the staging resource group.
This file preserves that dependency through `production_service_plan_id`; moving
it to a production-owned plan requires a separate capacity, migration, downtime,
and rollback decision.

The existing PostgreSQL server permits public network access and password
authentication. Those properties are represented as current state, not approved
future posture. Closing public access requires proving migration, deployment,
backup, support, and runtime connectivity through the private path first.

## September 26, 2026 adoption boundary

The remote backend and dedicated drift identity are still unconfigured. Do not
set the GitHub readiness flag to bypass this gate or use local production state.
Do not reuse the operational audit-log store as Terraform state: its 365-day
deletion rule applies to every blob and it has a different authorization purpose.
Choose a dedicated backend and a runner able to reach its private endpoint
before provisioning. The current hosted GitHub runner cannot be assumed to have
that network path. Provisioning, identity grants, import and a reviewed zero-drift
baseline remain separate from the deployment OIDC migration.

Terraform intentionally ignores application settings. From the repository root,
run `node tools/release/check-azure-settings.mjs production` with the public
production environment variables to check explicit environment, host, CORS and
workforce/customer authentication overrides. It reports only mismatching key
names, never values. It requires explicit overrides, does not infer effective
packaged defaults, and does not replace authenticated smoke tests or secret
rotation verification.

### Smallest private adoption option (prepared, not provisioned)

Use a dedicated Standard LRS state account, one private blob endpoint in the
existing `gccs-production-vnet/default2` subnet, and the existing
`privatelink.blob.core.windows.net` zone. Disable public and shared-key access;
enable versioning and soft delete; grant container-scoped Blob Data Contributor
to the state operator and a dedicated drift identity with Reader on production
resources and the shared service plan. Do not grant the deployment identity new
state access. An ephemeral trusted runner in the currently undelegated `default`
subnet can reach state and the existing private database endpoints. No VM runner
currently exists. Do not run untrusted PR jobs on this runner or store its
registration token in state. Prefer ephemeral runner registration and teardown
after the adoption/drift job; retain approved logs separately.

This adds a private-endpoint hourly/data charge, small LRS storage/transaction
charges, and runner compute/disk charges while allocated. No price quote or
purchase is implied. Select the account name, runner mechanism and spending
limit before provisioning; the existing app-data and 365-day log accounts are
not substitutes for a separately scoped state boundary.

After provisioning and verifying network access, run from the repository root
on that trusted runner (all values supplied by approved environment variables):

```sh
terraform -chdir=infra/terraform/environments/production init \
  -backend-config="resource_group_name=$PRODUCTION_TF_STATE_RESOURCE_GROUP" \
  -backend-config="storage_account_name=$PRODUCTION_TF_STATE_STORAGE_ACCOUNT" \
  -backend-config="container_name=$PRODUCTION_TF_STATE_CONTAINER" \
  -backend-config="key=$PRODUCTION_TF_STATE_KEY" \
  -backend-config="use_azuread_auth=true"
terraform -chdir=infra/terraform/environments/production plan \
  -lock-timeout=5m -out="$PROTECTED_EVIDENCE_DIRECTORY/production-adoption.tfplan"
terraform -chdir=infra/terraform/environments/production show -json \
  "$PROTECTED_EVIDENCE_DIRECTORY/production-adoption.tfplan" |
  jq -e 'all(.resource_changes[]?; .change.actions == ["no-op"])'
```

`imports.tf` contains the exact current resource names for the 27 adoption
targets. A nonzero check means reconcile configuration and regenerate the plan;
do not apply it. After independent review of the protected plan, apply only that
saved import-only plan, then run `plan -detailed-exitcode` again. Only exit code 0
from the subsequent plan supports the readiness flag. The JSON check alone is
not approval and does not check provider-side behavior or state access policy.
