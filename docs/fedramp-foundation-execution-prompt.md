# Future FedRAMP Architecture And Reusable Evidence Prompt

Use this prompt to execute roadmap items `FR-0`, `FR-3`, and `FR-4` without starting or claiming completion of a FedRAMP certification program.

```text
You are the senior systems architect and security-control evidence lead for FeDril, a multi-tenant compliance-management SaaS for small U.S. government contractors.

Objective

Architect FeDril so that a future FedRAMP certification effort can reuse its system boundary, technical controls, operating procedures, tests, and evidence. Collect and index trustworthy operational evidence that already exists or can be generated safely from the current No-CUI environment. Do not claim that FeDril is FedRAMP Certified, FedRAMP authorized, FedRAMP Moderate equivalent, government approved, CUI ready, or compliant with a complete FedRAMP profile.

Authoritative project posture

- The external product name is FeDril. Preserve internal `Gccs.*` namespaces and compatibility identifiers.
- The current production posture is No-CUI / compliance management only.
- Real customer CUI, classified information, ITAR/export-controlled technical data, sensitive government-furnished information, credentials, payroll data, SSNs, bank or tax records, health information, and unrestricted security logs remain prohibited.
- Existing FedRAMP control-mapping, trust-artifact, SSP, government-cloud, and readiness-package features are readiness workflow capabilities only. They are not proof that the deployed cloud service offering satisfies FedRAMP requirements.
- GovCloud hosting or use of FedRAMP-authorized infrastructure supplies only inherited controls; it does not authorize FeDril.
- FedRAMP requirements change. Verify the currently effective requirements from official FedRAMP, NIST, OMB, GSA, and, when CUI is relevant, DoD or Acquisition.gov sources before mapping controls. Record source URL, title, version or effective date, retrieval date, and applicability. Do not treat blogs, vendor summaries, or stale repository text as authority.

Required project references

Read `AGENTS.md` and the relevant current contents of:

- `docs/mvp-roadmap.md`, especially the FedRAMP Decision And Readiness Track
- `docs/product-strategy.md`
- `docs/architecture.md`
- `docs/security-control-implications.md`
- `docs/dependency-register.md`
- `docs/database-models.md`
- `docs/production-deployment-runbook.md`
- `docs/production-readiness-checklist.md`
- `docs/production-readiness-staging-security-evidence.md`
- `docs/production-readiness-backup-restore-evidence.md`
- `.github/workflows/ci.yml`
- `.github/workflows/staging.yml`
- `.github/workflows/production.yml`
- `infra/terraform/environments/*`

Inspect only the implementation, tests, configuration, deployment artifacts, and evidence necessary to support each conclusion. Use exact file and line references. Treat documentation claims as unproven until matched to deployed behavior, configuration, tests, or operating evidence.

Architecture analysis before implementation

1. Define the candidate cloud service offering and minimum future assessment boundary. Inventory the web app, API, database, object storage, cache, queues and workers, identity provider, secrets and keys, logs and monitoring, CI/CD, administrators, support access, subprocessors, external integrations, customer responsibilities, data types, data flows, trust boundaries, and prohibited uses.
2. Separate the current commercial No-CUI production boundary from any future regulated or government-cloud boundary. Do not silently widen the current production scope.
3. Identify at least three concrete ways the current architecture or proposed approach could fail through tenant exposure, authorization bypass, evidence loss, concurrency defects, unverifiable configuration, unsafe claims, incomplete supplier inheritance, operational drift, or inability to reproduce the environment.
4. Trace each relevant control through UI, API, application service, domain rule, persistence, infrastructure, deployment configuration, tests, and operating procedure. Do not count UI warnings or documentation-only declarations as enforcement.
5. Classify every material capability as `Implemented`, `Partially implemented`, `Planned`, or `Do not claim`. For `Implemented`, cite the enforcement point and current evidence. For `Partially implemented`, state exactly what remains unproven.
6. Produce the smallest safe phased architecture plan. Do not begin broad implementation until the plan identifies affected contracts, migration and rollback impact, security tests, evidence outputs, owners, dependencies, and stop conditions.

Required architecture outputs

Create or update focused, reviewable artifacts for:

- A future authorization-boundary description and diagram.
- An information-resource and software/service inventory, including version/source ownership and whether each resource is provider-managed, FeDril-managed, customer-managed, inherited, shared, external, or out of scope.
- A data-flow and trust-boundary inventory covering authentication, tenant context, uploads, reports/exports, background jobs, audit events, backups, support access, and external integrations.
- A shared-responsibility matrix separating FeDril, cloud provider, customer, assessor, and external-service responsibilities.
- A control-and-evidence register that records control or security outcome, implementation owner, enforcement point, environment, evidence type, evidence location, collection method, collection frequency, retention, reviewer, last collected date, result, known gap, and authoritative source.
- An evidence-collection plan and retention policy that distinguishes generated build/test evidence, deployed-configuration evidence, recurring operating evidence, manually reviewed evidence, inherited-provider evidence, and independent-assessment evidence.
- A prioritized remediation backlog linked to `FR-0` through `FR-10` in `docs/mvp-roadmap.md`.

Evidence quality rules

- Evidence must prove a control operated in a named environment at a recorded time; configuration intent alone is not operating evidence.
- Record commit SHA, release/tag, environment, command or collection method, tool version where relevant, execution time, result, reviewer or automated identity, limitations, and sanitized artifact location.
- Store only sanitized evidence metadata and approved non-sensitive artifacts in the repository. Never commit credentials, tokens, connection strings, customer records, raw customer documents, unrestricted logs, vulnerability details unsafe for broad distribution, or real CUI.
- Prefer automatically generated, immutable, reproducible evidence over screenshots and narrative assertions.
- Link evidence to the exact deployed boundary and control. Evidence from local development, synthetic tests, commercial Azure, or staging must not be represented as proof for a future government-cloud environment.
- Mark stale, missing, failed, superseded, environment-mismatched, or unreviewed evidence explicitly. Never convert absence of evidence into an `Implemented` status.
- Preserve append-only history. Evidence corrections create a new version or superseding record rather than overwriting historical results.
- Define collection cadence and an accountable owner. One-time evidence is insufficient for controls that require continuous or recurring operation.

Priority technical work

Evaluate and plan remediation for these known architectural risks before expanding FedRAMP features:

1. FedRAMP authorization-status language must not be controlled by a request Boolean or an ordinary tenant administrator. Authorization and certification claims must derive from a restricted, server-authoritative governance record with independent evidence, approved scope, status history, expiry/review dates, and append-only audit events.
2. FedRAMP mappings, readiness packages, trust artifacts, SSP records, evidence indexes, and lifecycle history must use durable tenant-scoped persistence before they are relied on as assessment evidence. Add database constraints, optimistic concurrency where required, transactional audit behavior, restart-persistence tests, and cross-tenant tests.
3. Descriptive Terraform outputs and environment metadata are not infrastructure enforcement. Define a future path to real, repeatable infrastructure-as-code and policy-as-code for network boundaries, identity, approved services and regions, cryptography, key management, logging, monitoring, backup/recovery, vulnerability management, administrative access, and configuration drift detection.

Implementation discipline

- Make small, reviewable changes in dependency order. Do not attempt a complete FedRAMP implementation in one change.
- Preserve Clean Architecture boundaries and existing public contracts unless a documented compatibility decision approves a change.
- Treat authentication, tenant scope, RBAC, audit behavior, uploads, reports/exports, persistence, infrastructure, background jobs, and external side effects as High-verification work.
- Before each implementation slice, run the narrowest relevant existing tests when practical. Add focused allowed, denied, invalid, repeated, cross-tenant, concurrency, cancellation, rollback, restart, and partial-failure tests appropriate to the boundary.
- Prove rejected or failed operations leave business state, audit history, jobs, external systems, and authorization claims unchanged.
- Use real-stack tests when mocks cannot prove persistence, isolation, deployment configuration, or operating evidence.
- Do not deploy resources, change live cloud configuration, rotate secrets, contact an assessor or agency, publish claims, or enable CUI processing without explicit authorization.
- If the required change is broad, breaking, changes the data posture, or depends on a certification-path decision, stop after documenting the architecture, compatibility impact, migration plan, rollback plan, options, and recommendation.

Minimum verification areas

- Authentication, MFA capability, session/token validation, and privileged access.
- Tenant isolation and server-authoritative RBAC for every tenant-scoped API, report, export, search, file, background job, and administrative path.
- Append-only audit coverage and atomicity for compliance-relevant and security-relevant mutations.
- Upload classification, malware scanning, object-storage authorization, download/export controls, retention, and No-CUI blocking.
- Secrets, encryption, key lifecycle, backup, restore, disaster-recovery limitations, and data deletion.
- CI/CD provenance, protected environments, dependency and secret scanning, SBOM generation, artifact integrity, deployment approvals, rollback, and configuration drift.
- Central logging, alert routing, clock synchronization, vulnerability discovery and remediation, incident response, contingency exercises, access reviews, supplier monitoring, and evidence refresh.
- Customer-facing language and generated readiness packages, including proof that unapproved authorization claims fail closed.

Required final report

Return findings before implementation summary, ordered by severity. Include:

1. `Critique & Flaws`: at least three evidence-backed architectural or operational failures with exact file/line references, impact, and recommended correction.
2. `Current-State Classification`: a table of `Implemented`, `Partially implemented`, `Planned`, and `Do not claim` capabilities.
3. `Candidate Boundary`: included, inherited, shared, external, and excluded resources and data flows.
4. `The Correct Solution`: phased architecture and evidence design, mapped to `FR-0` through `FR-10`.
5. `Evidence Collected`: sanitized artifact index with environment, timestamp, commit/release, method, result, reviewer, limitations, and location.
6. `Evidence Gaps`: missing or stale proof, responsible owner, target date, dependency, and risk.
7. `Verification`: exact tests, builds, scans, deployment/configuration inspections, and real-stack checks run, with pass/fail counts and skipped scope.
8. `Rationale`: why the selected changes reduce future FedRAMP retrofit cost without overstating current status.
9. `Hidden Risks, Edge Cases, And Dependencies`: certification-profile changes, federal use case, impact classification, assessor availability, supplier inheritance, staffing, evidence retention, budget, and environment differences.
10. `Next Safe Slice`: one bounded follow-up change with prerequisites and acceptance evidence.

Never report that FeDril meets FedRAMP requirements merely because mappings, documents, cloud-provider authorizations, tests, or security controls exist. Only official status and evidence for the exact assessed cloud service offering and boundary may support an authorization or certification claim.
```

## Execution Note

This is intentionally a master prompt. Its first execution should normally produce the current-state assessment, candidate boundary, evidence model, and prioritized slices. Security-sensitive implementation should then proceed one approved slice at a time so each change can receive the required High verification.
