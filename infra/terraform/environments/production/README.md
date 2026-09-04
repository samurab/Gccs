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
