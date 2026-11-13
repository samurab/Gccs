

Publication classification: **Sanitized public reference**. Complete originals and original SHA-256 hashes remain in protected `release-scope-lookups-20261018-v1/primary-source-snapshot-v1`. This counterpart records scope and traceability; it does not establish workforce control effectiveness, review completion or acceptance.

# FeDril SOC 2 Readiness Reference

Status: **Internal readiness assessment; not an assurance report and not evidence of SOC 2 compliance**

Assessment cutoff: 2026-09-27 23:16 EDT (`UTC-04:00`)

Repository state reviewed: commit `885ecc2a6e10a8479ddc0c23ba31763bd73995cf` on `codex/local-restored-readiness`, plus the uncommitted documentation state identified in the repository status at collection time.

## Purpose And Limits

This document is the durable, sanitized index for the evidence-focused review of Stories 39.1 through 39.23. It records repository implementation or configuration, documented policy or design, point-in-time live configuration observations, historical operating evidence, and explicit gaps. It does not authorize remediation, establish control operating effectiveness, appoint an owner, approve a scope, start an examination period, or represent that FeDril is SOC 2 certified, SOC 2 compliant, audit ready, government approved, or authorized to process CUI.

The current product posture remains **No-CUI / compliance management only**. Real customer CUI, classified information, export-controlled technical data, credentials, and other prohibited sensitive data remain outside the approved product posture.

## Sources Inspected

- Stories 39.1 through 39.23 and their 144 stable task IDs in `docs/development-story-prompts.md`.
- `AGENTS.md`, `README.md`, `docs/architecture.md`, `docs/database-models.md`, `docs/product-strategy.md`, `docs/mvp-roadmap.md`, `docs/definition-of-ready.md`, `docs/design-flow-diagrams.md`, `docs/product-readiness-note.md`, and relevant production-readiness and support records.
- Existing SOC 2 records under `docs/soc2/`, including the draft boundary, Stories 39.1 through 39.4 setup records, and the September 19 Azure inventory.
- Relevant application security, tenant isolation, RBAC, audit, incident-readiness, No-CUI, backup, release, workflow, test, and Terraform sources.
- Sanitized, read-only Azure, Microsoft Entra, and GitHub configuration metadata from the currently authenticated sessions. Only narrow counts, states, protection flags, regions, service families, and non-secret settings were retained.

No customer records, uploaded evidence, database contents, secret values, tokens, credentials, connection strings, private keys, workflow logs, security finding details, or raw account/configuration exports were read or retained.

## Access And Evidence Limitations
Protected evidence reference; individual observations and source details are retained in the protected original.

## Sanitized Architecture And System Boundaries

The candidate scope includes the FeDril browser application, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, object storage, background workers, malware-scanning path, monitoring dependencies, customer and workforce identity planes, source control, CI/CD, deployment workflows, and the people and procedures that operate or support them. Relevant external dependencies include Microsoft Azure, Microsoft Entra, GitHub, Azure communication services, and any enabled customer-support or external-integration provider.

Live Azure metadata showed web hosting, PostgreSQL, storage, cache, private-networking components, monitoring resources, communication services, and customer identity resources across commercial U.S. regions. This observation confirms service families and point-in-time settings only; it does not prove the approved production boundary or deployment equivalence with the reviewed repository.

Production and staging must remain separate evidence scopes. The repository contains distinct environment definitions and deployment workflows, but staging configuration or restore evidence cannot be used as proof of production operation.

Workforce identity and customer authentication are distinct control concerns. Customer tenant isolation and server-side permissions are implemented in application sources, while workforce access governance depends on Microsoft Entra, Azure RBAC, GitHub access, endpoint security, and recurring human review. Evidence for one plane does not prove the other.

## Verified Findings By Control Area

### Scope, posture, and claims

- **Implemented in repository:** No-CUI product boundaries and prohibited-claim guardrails are documented in `AGENTS.md`, `README.md`, `docs/product-strategy.md`, and the SOC 2 story contract.
- **Partially implemented:** A sanitized draft boundary and Stories 39.1 through 39.4 setup records exist. They are expressly unapproved and do not establish an examination-ready scope.
- **Do not claim:** No issued SOC 2 report, independent examination, approved observation period, or favorable auditor opinion was found.

### Application security and tenant boundaries

- **Implemented in repository:** Server-side tenant context, RBAC, standard error handling, audit abstractions, No-CUI upload controls, incident-readiness workflows, and focused security tests exist.
- **Partially verified:** The sources support design and implementation. The focused tests were not rerun for this documentation-only review, and current deployed behavior was not exercised. Complete endpoint inventory, cross-tenant negative testing, production release equivalence, and period operation remain unverified.

### Source control and change management

- **Live configuration observation:** The GitHub repository is public. The default branch blocks force pushes and deletion, enforces administrators, dismisses stale reviews, and has three required status checks. Secret scanning and push protection are enabled.
- **Live configuration observation:** Required approving reviews are set to zero, code-owner review is not required, no `CODEOWNERS` file exists, and the collaborator query returned one account. One deployment environment has a required-reviewer rule, with one configured reviewer.
- **Repository configuration:** Deployment workflows use short-lived GitHub OIDC permissions, explicit environment gates, and read-only default workflow permissions. GitHub Actions permits all actions, and repository-level OIDC subject customization uses the default subject.
- **Historical operating sample:** The latest 100 workflow runs comprised 57 successful, 10 failed, 6 cancelled, and 27 skipped runs. This sample is evidence that workflows ran, not that every required check governed every change or deployment.

### Azure hosting, network, encryption, and recovery

- **Live configuration observation:** Web, database, storage, cache, private endpoint, DNS, virtual network, monitoring, communication, and identity resource families were visible. The reviewed PostgreSQL servers reported public network access enabled, seven-day backup retention, geo-redundant backup disabled, and high availability disabled.
- **Live configuration observation:** Reviewed storage accounts required HTTPS and TLS 1.2 and disallowed public blob access. At least one account had public network access enabled and shared-key access enabled. These settings require resource-specific scope and use review before a control conclusion.
- **Live configuration observation:** The reviewed Redis instance disabled non-TLS access and public network access. Web-app metadata did not return conclusive `httpsOnly` or public-network values through the selected resource query.
- **Documented historical evidence:** A synthetic staging point-in-time restore was documented as passed on July 5, 2026. It does not prove production restore, regional recovery, current recoverability, or repeatable recovery within approved RPO/RTO targets.
- **Access limitation:** Diagnostic-setting queries were blocked for the selected service families. One metric alert and two action groups were visible, but delivery, ownership, response, retention, and operating history were not verified.

### Workforce identity and privileged access
Protected evidence reference; individual observations and source details are retained in the protected original.

### Vulnerability, monitoring, incident, vendor, and data lifecycle

- **Partially implemented:** CI includes dependency and security checks; GitHub secret scanning and push protection are enabled. Dependabot security updates were disabled in live repository metadata. No complete vulnerability population, response-SLA history, penetration-test record, or independent testing evidence was collected.
- **Partially implemented:** Application audit and incident-readiness code, tests, and support runbooks exist. No complete live audit population, incident exercise, alert receipt, or response evidence was collected.
- **Not verified:** approved vendor register, subprocessor reviews, contracts, service-organization reports, shared-responsibility approvals, and recurring vendor monitoring. HubSpot-dependent evidence was out of scope.
- **Partially implemented:** data classification and No-CUI rules exist in application and product documents. An approved organizational retention schedule, legal-hold process, disposal authorization, backup-expiry handling, and disposal operating evidence were not verified.

## Architectural And Readiness Failure Risks

1. **Single-person control concentration will fail separation-of-duties and continuity expectations.** The live GitHub configuration allows zero required PR approvals, has no code-owner requirement, returned one collaborator, and has one deployment reviewer. The same person can plausibly author, approve, deploy, collect evidence, and review evidence. This makes independent review, emergency coverage, access recertification, and evidence credibility structurally weak.
2. **Public source control cannot be the protected SOC 2 evidence system.** The public repository currently tracks a database dump path. Its contents were not opened, but public visibility plus Git history creates a plausible exposure requiring authorized classification and containment. Storing sensitive assurance records, personnel evidence, access exports, vulnerability details, or reports in this repository would create durable disclosure and retention conflicts.
3. **Configuration snapshots and tests will not scale into operating-effectiveness evidence.** The current approach has many designs, tests, Terraform resources, and self-authored readiness records but lacks approved owners, complete populations, independent review, protected evidence custody, failure retention, and an agreed review period. Without those operating mechanisms, evidence collection becomes selective, stale, and non-repeatable and cannot support Type II conclusions.

## Story-By-Story Assessment

Results apply to the story-level acceptance criterion and the identified stable task range. They are proposed assessments for management review; they do not change the original story status.

| Story | Stable tasks | Criterion result | Evidence-backed assessment | Proposed story disposition |
| --- | --- | --- | --- | --- |
| 39.1 | T01-T07 | Partially verified | Draft scope, boundary, matrix, and demand records exist; no authorized scope approval, current licensed-criteria review, qualified review, complete owner mapping, or proceed/defer approval was verified. | Retain Planned; setup evidence exists. |
| 39.2 | T01-T07 | Partially verified | Setup records and an evidence approach exist; protected storage, approved retention, complete 144-task register, evidence calendar, reviewer population, and operating-quality review are not established. | Retain Planned. |
| 39.3 | T01-T07 | Partially verified | Draft examination and claims governance exists; no independent CPA engagement, approved claims owner, report, disclosure process operation, or management assertion was verified. | Retain Planned. |
| 39.4 | T01-T06 | Blocked | The public repository and tracked database-dump path are verified. Classification, affected-data determination, access history, containment, notification, legal review, and independent closure remain unavailable or unauthorized. | Retain Planned; immediate authorized triage remains required. |
| 39.5 | T01-T06 | Not verified | Product and security guidance exists, but approved enterprise risk assessment, policy set, management review, exception governance, and owner approvals were not verified. | Retain Planned. |
| 39.6 | T01-T07 | Not verified | Protected evidence reference; individual observations and source details are retained in the protected original. | Retain Planned. |
| 39.7 | T01-T06 | Partially verified | Tenant context, RBAC, No-CUI, audit, error, and isolation test sources exist. Current test execution and deployed production enforcement were not verified. | Retain Planned. |
| 39.8 | T01-T06 | Partially verified | CI, required checks, branch restrictions, OIDC deployments, and environment gates exist. Zero required approvals, no CODEOWNERS, single collaborator, and single deployment reviewer leave separation of duties unproven. | Retain Planned. |
| 39.9 | T01-T06 | Partially verified | Terraform contains private-network design and drift workflow definitions. Live database public access was enabled, diagnostics were inaccessible, and no imported zero-drift baseline or recurring drift review was proven. | Retain Planned. |
| 39.10 | T01-T06 | Partially verified | TLS and storage encryption-related settings were observed. Shared-key access remained enabled for at least one storage account; no complete secret inventory, workload-identity coverage, key ownership, rotation, recovery, or exception evidence was verified. | Retain Planned. |
| 39.11 | T01-T06 | Partially verified | CI security checks and GitHub secret scanning/push protection are present. Dependabot security updates were disabled; Defender metadata was inaccessible; vulnerability populations, remediation SLAs, patch evidence, and independent test results were not verified. | Retain Planned. |
| 39.12 | T01-T06 | Partially verified | Application audit design plus alert resources exist. Resource diagnostics, log destinations/retention, alert delivery, triage populations, immutable protection, and repeated management review were not verified. | Retain Planned. |
| 39.13 | T01-T06 | Partially verified | Incident-readiness application code, tests, and support documentation exist. Approved organizational plan, roles, contacts, exercise results, live incidents, postmortems, and spill-handling operation were not verified. | Retain Planned. |
| 39.14 | T01-T06 | Partially verified | A historical synthetic staging restore is documented. Production recovery, current backup recoverability, approved RPO/RTO, geo recovery, continuity ownership, repeat exercises, and failure handling remain unverified. | Retain Planned. |
| 39.15 | T01-T06 | Not verified | Service dependencies are documented and some live service families were observed. Approved vendor inventory, due diligence, contracts, subprocessor change review, inherited-control evidence, and recurring review were not verified. | Retain Planned. |
| 39.16 | T01-T06 | Partially verified | No-CUI and application classification rules exist. Organizational retention decisions, legal hold, secure disposal, customer-responsibility approval, backup expiry, and disposal evidence were not verified. | Retain Planned. |
| 39.17 | T01-T06 | Not verified | No approved applicability decision for Availability, Confidentiality, Processing Integrity, or Privacy, and no CPA concurrence, was verified. | Retain Planned. |
| 39.18 | T01-T06 | Not verified | An integrated readiness decision cannot be supported while material scope, exposure, workforce, change-approval, diagnostics, vendor, and evidence-custody gaps remain open. | Retain Planned; do not pass the gate. |
| 39.19 | T01-T06 | Blocked | No approved Type I business decision, CPA engagement, management assertion, fieldwork, or issued report was found. | Retain Planned; conditional future work. |
| 39.20 | T01-T06 | Blocked | No approved Type II scope, start/end dates, control populations, owners, sampling approach, evidence entry gate, or CPA agreement was found. | Retain Planned. |
| 39.21 | T01-T07 | Blocked | No approved Type II period was in effect, so repeated operation across an agreed period cannot be claimed. Historical technical artifacts are not a complete control population. | Retain Planned. |
| 39.22 | T01-T06 | Blocked | No sufficient Type II period, examination engagement, auditor-selected samples, management assertion, or issued report exists. | Retain Planned. |
| 39.23 | T01-T07 | Partially verified | Claim guardrails and future disclosure concepts exist. No issued report, approved disclosure owner, disclosure log, renewal decision, bridge-letter process, or continuous evidence calendar was verified. | Retain Planned; continue readiness controls without report claims. |

## Documentation And Implementation Discrepancies

- The backlog references `docs/soc2-readiness-assessment.md`, but that file is absent. Existing September 19 records summarize it and cite historical commits; the missing artifact prevents direct validation of its 36 checklist items and 15 gap definitions.
- Stories 39.21, 39.22, and 39.23 contain embedded orchestration/delegation text inside their reusable prompts. That text is execution-environment instruction rather than SOC 2 requirement and may conflict with future runtimes. It should be separated from the canonical story requirement in a separately authorized backlog-cleanup task.
- The repository and existing SOC 2 records describe strong application controls, but current live workforce-access, diagnostic-retention, vulnerability, vendor, and operating-period evidence is incomplete or inaccessible.
- Terraform contains private endpoint resources, while the point-in-time database metadata showed public network access enabled. This may be an intentional migration state, a parallel environment, or drift; the reviewed data cannot safely resolve which without resource identifiers and approved detailed evidence.
- Existing backup/restore evidence proves a dated synthetic staging exercise, while the live database configuration showed seven-day retention, geo backup disabled, and high availability disabled. The two facts are compatible but do not support production continuity or regional recovery claims.

## Prioritized Gaps And Next Actions
Protected evidence reference; individual observations and source details are retained in the protected original.

## Outstanding Human Decisions And Evidence

- Management-approved candidate scope, system commitments, exclusions, and No-CUI boundary.
- Named sponsor, owner, operators, evidence custodian, independent reviewers, deputies, and disclosure authority.
- Approved business case, budget, qualified CPA/advisor selection, and Type I-first versus direct-Type-II decision.
- Authorized response to the public tracked database-dump path.
- Business-controlled evidence workspace selection and authorization to configure and test it.
- Approval of workforce MFA, emergency-access, access-review, endpoint, and training controls, plus permission to collect sanitized operating evidence.
- Approval of PR review/code-owner/separation-of-duties policy and reviewer staffing.
- Resource-specific Azure access sufficient to validate diagnostics, security posture, network paths, identity, backup, and recovery without exposing identifiers or secrets.
- Vendor contracts, due diligence, subprocessor facts, and shared-responsibility approvals from authorized business systems.
- Approved risk assessment, policy set, retention/disposal schedule, incident exercise, RPO/RTO, control frequencies, and evidence calendar.

## Repeatable Refresh Procedure

1. Record the repository commit, branch, local change state, time zone, evidence cutoff, collector role, and source-access limitations.
2. Confirm all 23 story headings and all stable task IDs remain present. Do not infer missing or renamed criteria.
3. Read the current boundary, release manifest, architecture, product posture, security guidance, Story 39 records, and prior inventory before collecting new evidence.
4. Collect only narrow sanitized metadata. Suppress tenant, subscription, resource, user, network, repository, and endpoint identifiers at command construction and output. Never collect secret values or sensitive payloads.
5. Reconcile repository design, live configuration, and historical operating evidence separately. Do not treat one classification as another.
6. Append new evidence rows; never overwrite historical rows. Give each observation or gap a stable ID, date, scope alias, source locator, result, limitation, and confidence.
7. Reassess each story criterion as Verified, Partially verified, Not verified, Blocked, or Not applicable. Explain every Not applicable result and require reviewer approval before changing story status.
8. Validate local paths, CSV parsing, story coverage, prohibited-identifier scans, unsupported-claim scans, and the authorized-file-only diff.
9. Preserve raw sensitive evidence only in the approved protected workspace. Keep this repository document sanitized and summary-level.

## Pre-Publication Checklist

- Does the current UI expose the described flow?
- Does the API or domain service enforce the stated rule?
- Is there a current test or operating record that proves the claim?
- Does the wording avoid certification, compliance, approval, guarantee, legal-advice, audit-ready, government-approval, and secure-CUI-storage overclaims?
- Does the claim identify whether it is Implemented, Partially implemented, Planned, or Do not claim?
- Does the content preserve the No-CUI posture?
- Is every enforcement word supported by server-side or domain enforcement rather than a UI warning?

The evidence index is maintained in `docs/soc2/soc2-evidence-inventory.csv`.
