# MVP Production Readiness Checklist

Story 17.4 defines the launch gate for the No-CUI / compliance management only MVP. This checklist must be reviewed before any production launch. It is a release-control artifact, not a claim that production is approved.

## Launch Gate

Launch gate status: blocked until all required items are complete and approved.

Required approvals before production launch:

- Product owner approval.
- Engineering lead approval.
- Security owner approval.
- Compliance content owner approval.
- Customer success/support owner approval.
- Legal or contracting advisor approval for customer-facing compliance claims.

| Area | Required item | Evidence | Owner | Approver | Current status |
| --- | --- | --- | --- | --- | --- |
| No-CUI posture | Customer-facing data handling notice is visible in onboarding, upload flows, docs, and support scripts. Real CUI upload remains prohibited until a future approved `CuiReady` posture is implemented. | `README.md`, `docs/product-strategy.md`, `docs/mvp-execution-plan.md` | Product owner | Legal or contracting advisor | Ready for approval |
| Terms and claims | Product copy avoids legal advice, certification, CMMC approval, assessment success, and government endorsement claims. | `docs/software-delivery-plan.md`, `docs/compliance-content-governance.md` | Product owner | Legal or contracting advisor | Ready for approval |
| Support path | Support path exists for prohibited uploads, access issues, evidence upload failures, tenant isolation concerns, and compliance content corrections. | `docs/mvp-execution-plan.md`, `docs/software-delivery-plan.md` | Customer success/support owner | Product owner | Ready for approval |
| Prohibited uploads | Prohibited upload guidance covers CUI, classified data, export-controlled technical data, SSNs, payroll, protected health or disability data, credentials, unrestricted security logs, and sensitive incident details. | `docs/mvp-execution-plan.md` | Security owner | Legal or contracting advisor | Ready for approval |
| Backups and restore | Backup policy, restore runbook, and staging restore evidence are available. | `docs/software-delivery-plan.md`, `docs/staging-environment.md` | Engineering lead | Security owner | Needs staging restore evidence |
| Logs and alerts | API, web, migration, upload/storage, queue, job failure, health, and error alerts are routed to the launch support owner. | `docs/staging-environment.md` | Engineering lead | Security owner | Ready for approval |
| Rollback plan | Production rollback plan is documented and simulated in staging before launch. | This checklist, `docs/staging-environment.md` | Engineering lead | Product owner | Simulated for staging |
| Malware scanning | MVP limitation is documented when scanner is placeholder-only; production launch requires an enabled malware scanning path or explicit launch exception. | `README.md`, `docs/software-delivery-plan.md`, `docs/mvp-execution-plan.md` | Security owner | Product owner | Needs launch decision |
| Expert-reviewed content | Customer-facing launch content has source URL, last reviewed date, confidence, review owner, and review state. | `packages/compliance-content/obligations/mvp.json` | Compliance content owner | Legal or contracting advisor | Needs high-risk review completion |
| Release notes | Release notes call out tenant data handling posture, known limitations, source-backed content scope, support path, rollback plan, and staging smoke results. | Release notes draft for the launch tag | Product owner | Customer success/support owner | Pending launch tag |

## Known Limitations

- The MVP is No-CUI / compliance management only.
- The MVP must not store CUI until a future approved `CuiReady` posture is implemented.
- The MVP must not store classified data, ITAR/export-controlled technical data, SSNs, payroll records, bank or tax details, protected health or disability data, credentials, unrestricted security logs, or sensitive incident details unless a separately approved deployment posture exists.
- Malware scanning is represented by a local placeholder in development. Production launch requires an enabled scanner integration or a documented launch exception accepted by the product owner and security owner.
- CMMC, FAR, DFARS, SBA, labor, and reporting content is workflow guidance, not legal, accounting, certification, assessment, or contracting-officer advice.
- Some high-risk launch obligations are marked `needs_review` in the content package and must be approved or hidden from customer-facing production views before production launch.
- The staging Terraform file is an environment contract until a cloud provider target is selected and provider-specific resources are attached.
- AI features remain draft-only and source-cited unless an expert-reviewed workflow explicitly approves production use.

## Support Path

Support intake must route these cases before launch:

- Accidental prohibited upload or suspected CUI upload.
- Tenant isolation, access, RBAC, or evidence exposure concern.
- Evidence upload, malware scanning, or storage failure.
- Report generation or export failure.
- Compliance content correction, disputed obligation, or source page change.
- Security incident or suspicious account activity.

Severity targets follow `docs/software-delivery-plan.md`: Severity 1 within 30 minutes, Severity 2 within 4 business hours, Severity 3 within 1 business day, and Severity 4 within 3 business days.

## Launch Content Metadata

The launch obligation package is `packages/compliance-content/obligations/mvp.json`.

Every launch obligation must include:

- `source`
- `source_url`
- `last_reviewed_at`
- `confidence`
- `review_owner`
- `review_state`
- `requires_expert_review`
- `trigger_condition`
- `required_actions`
- `evidence_examples`

High-risk records with `requires_expert_review: true` must be approved or withheld from customer-facing production views before production launch.

## Staging Rollback Verification

Simulation date: 2026-06-15.

Rollback scenario:

1. Deploy staging from `.github/workflows/staging.yml`.
2. Generate and preserve the idempotent EF Core migration script.
3. Run staging smoke tests against `/health`.
4. Simulate a failed release by marking the staging health check degraded.
5. Re-deploy the previous known-good API and web artifacts.
6. Confirm `/health` returns API status `ok` with database, cache, storage, and background job signals.
7. Record timing, commands, migration state, and outcome in the release notes for the launch tag.

Expected timing:

- Detection target: 5 minutes from failed smoke test.
- Decision target: 10 minutes from failed smoke test.
- Recovery target: 30 minutes for application rollback when no destructive migration is involved.

Outcome:

- Simulation result: documented.
- Production launch gate: remains blocked until the release owner attaches staging workflow evidence and migration rollback notes for the launch candidate.
