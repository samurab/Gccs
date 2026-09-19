# FeDril SOC 2 Readiness Assessment

Assessment date: 2026-09-19 UTC  
Repository: `samurab/Gccs`  
Branch and commit reviewed: `main` at `6e38c20392c806c9c1355ea94f63954b0bdfca6e`  
Assessment type: Repository- and evidence-based readiness review; not an independent SOC 2 examination  
Candidate criteria: Security baseline; Availability and Confidentiality deferred pending verified customer or contractual demand

## Executive Decision

FeDril has a stronger technical-control foundation than a typical early-stage SaaS application, but it is not ready to represent itself as SOC 2 ready or to begin a formal Type 1 examination. The correct immediate decision is **remediate and collect evidence**, while engaging a qualified CPA firm only for preliminary scoping/readiness advice if customer demand justifies the expense.

The repository supports the following conclusion:

- Application controls are comparatively mature: authenticated APIs, tenant-scoped authorization, RBAC, append-only audit behavior, No-CUI guardrails, security-focused tests, controlled deployment workflows, dependency and secret scans, production monitoring, and staging restore evidence exist.
- Organizational controls are materially incomplete or unverified: enterprise risk assessment, internal security-policy set, workforce joiner/mover/leaver procedures, recurring access reviews, security awareness training, vendor/subprocessor governance, vulnerability-remediation SLAs, penetration testing, evidence-retention policy, management review, and SOC 2 control ownership are missing from the reviewed repository or lack operating evidence.
- Several technical risks require immediate review: the repository is public; a PostgreSQL dump is committed under `.codex/db-backups`; production infrastructure exposes detailed operational metadata; PostgreSQL public network access is enabled in Terraform despite a private endpoint; storage shared-key access remains enabled; production drift monitoring was skipped; and exact branch-protection and environment-approval rules were not available to this review.
- Existing production-readiness records repeatedly limit approval to a solo-controlled pilot and state that broader production separation of duties remains required. That is incompatible with claiming mature SOC 2 governance without remediation.

### Recommended immediate scope

Include the FeDril SaaS production system and the people/processes that operate it:

- React web application, ASP.NET Core API, PostgreSQL, Redis, Azure object storage, App Service, Static Web Apps, Application Insights, malware-scanning service, CI/CD, source control, identity providers, backups, monitoring, email delivery, and production support.
- FeDril personnel and contractors with production, source-control, identity, support, evidence, or deployment access.
- Material subprocessors such as Azure, GitHub, identity services, Azure Communication Services, and HubSpot where production data or operations depend on them.

Exclude customer systems, customer CUI, C3PAO activities, MSP-managed customer infrastructure, legal/compliance determinations, and any future CUI enclave until separately approved.

### Five highest-priority actions

1. Inspect and remove the committed database dump from the public repository and history when appropriate; determine whether it contains anything other than approved synthetic data and open an incident if exposure is possible.
2. Approve the SOC 2 system boundary, Security-category scope, control owners, evidence owners, and explicit Type 1/Type 2 decision gates.
3. Create the missing organizational control set: risk management, access lifecycle/reviews, security awareness, incident response, vendor management, vulnerability management, change management, data retention, and management review.
4. Harden and independently verify production configuration, including PostgreSQL network exposure, shared-key use, telemetry ingestion, drift detection, privileged access, MFA/conditional access, environment approvals, and separation of duties.
5. Establish an evidence calendar and operate the controls before selecting a Type 2 period.

## Assessment Method

Each control is evaluated separately for:

- **Design**: the control is suitably designed for the candidate scope.
- **Implementation**: the control is placed in operation.
- **Operating evidence**: dated evidence demonstrates execution and review.

Statuses are `Verified Implemented`, `Partially Implemented`, `Documented But Not Verified`, `Planned Only`, `Missing`, `Not Applicable`, or `Requires Verification`.

## Current FeDril Control Inventory

| Priority | Control area | Existing control | Design | Implementation | Operating evidence | Evidence and limitation | Type relevance | Recommendation |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| P0 | Repository data handling | Gitleaks secret scan runs in CI | Verified Implemented | Verified Implemented | Partially Implemented | `.github/workflows/ci.yml`; latest reviewed main check passed. A secret scan does not validate committed database contents. | Both | Add binary/data-file scanning and remove database backups from source control. |
| P0 | Tenant isolation | Server-derived tenant context, membership authorization, tenant-scoped repositories, and cross-tenant tests | Verified Implemented | Verified Implemented | Partially Implemented | `ApiSecurityExtensions.cs`, `HttpTenantContext.cs`, `SecurityIsolationVerificationTests.cs`, production-readiness role-matrix evidence. Repository evidence does not prove all live routes. | Both | Maintain complete endpoint inventory and periodic production authorization tests. |
| P0 | No-CUI boundary | No-CUI posture, upload acknowledgement, classification checks, and prohibited-content guardrails | Verified Implemented | Verified Implemented | Partially Implemented | `README.md`, `AGENTS.md`, marketing policy drafts, upload tests, production smoke records. Customer training and incident operation require verification. | Both | Keep boundary in scope and test every intake/export surface. |
| P1 | Authentication | JWT validation, issuer/audience/lifetime/signature validation, production development-auth exclusion | Verified Implemented | Verified Implemented | Partially Implemented | `ApiSecurityExtensions.cs`, production deployment evidence. MFA and conditional-access enforcement were not verified. | Both | Verify MFA, conditional access, session controls, break-glass access, and identity logs. |
| P1 | RBAC and least privilege | Server-authoritative role/permission catalog and denied-action tests | Verified Implemented | Verified Implemented | Partially Implemented | `RolePermissionClaimsTransformation.cs`, `RoleBasedPermissionTests.cs`, real-stack RBAC job. Workforce privileged-role review is absent. | Both | Add recurring internal and customer privileged-access reviews. |
| P1 | Audit trail | Append-only audit records, atomic mutation/audit boundaries, denied-action logging | Verified Implemented | Verified Implemented | Partially Implemented | `AuditAppendOnlyTests.cs`, `EfAuditLogRepository.cs`, architecture and production-readiness evidence. Retention and periodic review are unverified. | Both | Define retention, integrity monitoring, reviewer cadence, and export access. |
| P1 | Change and release control | CI build/test gates, migrations, release-candidate manifest, protected production environment workflow | Verified Implemented | Verified Implemented | Partially Implemented | CI and production workflows; successful checks at reviewed SHA; branch reports `protected=true`. Exact required reviews and environment approvers were inaccessible. | Both | Record branch/environment rules, independent approvers, emergency-change process, and rollback review. |
| P1 | Secure SDLC | Backend/frontend tests, real-stack RBAC, dependency scans, secret scan, Terraform validation | Verified Implemented | Verified Implemented | Partially Implemented | `.github/workflows/ci.yml`; latest reviewed jobs passed. No SAST/DAST, container/image scan, penetration test, or remediation SLA was found. | Both | Add risk-based scanning, triage SLAs, annual penetration testing, and exception governance. |
| P1 | Infrastructure as code | Production Terraform, private endpoints, TLS minimums, managed identity, monitoring resources | Partially Implemented | Documented But Not Verified | Requires Verification | `infra/terraform/environments/production/main.tf`; Terraform was validated but current live drift was not verified. | Both | Reconcile live state, enable drift workflow, and approve deviations. |
| P1 | Network security | Redis/storage private access and private endpoints; App Service HTTPS | Partially Implemented | Documented But Not Verified | Requires Verification | Terraform enables PostgreSQL public networking and also declares a private endpoint; live firewall state was not reviewed. | Both | Disable unnecessary public access and verify all ingress/egress paths. |
| P1 | Secrets and keys | GitHub environment secrets, managed identity for some services, signing-key validation | Partially Implemented | Partially Implemented | Requires Verification | Deployment workflows validate secret presence without printing values. Azure credential uses a stored credential; storage shared keys remain enabled; no Key Vault resource was found. | Both | Prefer workload identity/managed identity and Key Vault; rotate and review access. |
| P1 | Monitoring and alerting | Application Insights, health checks, 5xx alert, action-group delivery evidence | Partially Implemented | Verified Implemented | Partially Implemented | Terraform, production smoke, and alert evidence. Security-specific alerts and telemetry coverage are limited; `sampling_percentage = 0` requires live verification. | Both | Verify ingestion, define security alerts, on-call ownership, response SLAs, and evidence retention. |
| P1 | Backup and restore | Seven-day PostgreSQL backup and successful staging point-in-time restore rehearsal | Partially Implemented | Verified Implemented | Partially Implemented | `production-readiness-backup-restore-evidence.md`; evidence explicitly excludes production restore and regional recovery. | Both | Test production-equivalent restore, define RPO/RTO, retention, ownership, and regional-risk decision. |
| P1 | Incident response | Product incident-readiness model and production support runbooks | Partially Implemented | Partially Implemented | Documented But Not Verified | Application records can track playbooks/tabletops, but this does not prove FeDril operated its own corporate incident-response program. | Both | Approve FeDril IR plan, contacts, severity model, notification rules, tabletop, and lessons learned. |
| P1 | Risk management | Security findings and accepted-risk records exist in product/release artifacts | Partially Implemented | Partially Implemented | Documented But Not Verified | No enterprise-wide SOC 2 risk assessment, risk register, review cadence, or management approval was found. | Both | Create and approve an organization-level risk program. |
| P1 | Access lifecycle | Tenant membership and invitation lifecycle features exist | Partially Implemented | Verified Implemented | Requires Verification | Product features do not prove FeDril workforce onboarding/offboarding or periodic production-access reviews. | Both | Establish JML process, termination SLA, quarterly reviews, and evidence. |
| P1 | Vendor management | Application contains a vendor domain model | Missing | Missing | Missing | No subprocessor inventory, due diligence, contract/security review, SOC report review, or recurring vendor monitoring was found. | Both | Create vendor register and tiered review process. |
| P2 | Data classification | No-CUI, FCI, synthetic-CUI, prohibited, and unknown classifications are enforced in application workflows | Verified Implemented | Verified Implemented | Partially Implemented | Code, tests, and data-boundary documents provide strong design evidence. Operational review and customer behavior remain unverified. | Both | Add periodic classification-control review and customer acknowledgement evidence. |
| P2 | Data retention/disposal | AI output retention worker and object cleanup workflows exist | Partially Implemented | Partially Implemented | Requires Verification | No organization-wide retention schedule, legal-hold process, or disposal evidence was found. | Both | Approve retention matrix and verify deletion across application, logs, backups, support, and vendors. |
| P2 | Business continuity | Deployment, rollback, health, and support procedures exist | Partially Implemented | Partially Implemented | Documented But Not Verified | No approved business-continuity plan, business-impact analysis, or exercise evidence was found. | Availability if scoped; Security support | Create lightweight BCP and exercise it. |
| P2 | Security awareness | No relevant internal training evidence found | Missing | Missing | Missing | Repository contains no workforce security/privacy training program or completion records. | Both | Implement onboarding and annual training with completion evidence. |
| P2 | Management oversight | Production readiness approvals and launch decisions exist | Partially Implemented | Partially Implemented | Documented But Not Verified | Records are solo-controlled and explicitly defer broader separation of duties. | Both | Establish quarterly management security review and independent approval. |
| P2 | Customer claims | Conservative No-CUI and non-certification language is documented | Verified Implemented | Partially Implemented | Partially Implemented | Customer-claims review and marketing drafts exist; publication approvals and recurring review require verification. | Both | Maintain claims register with owner, approval, evidence, and expiration. |
| P3 | Availability | Health checks, backup, monitoring, and deployment controls exist | Partially Implemented | Partially Implemented | Partially Implemented | No verified SLA, capacity plan, production restore test, regional resilience, or BCP. | Availability only | Keep category out of initial scope unless demanded; close gaps before adding. |
| P3 | Confidentiality | No-CUI boundary, classification, RBAC, encryption-oriented configuration | Partially Implemented | Partially Implemented | Requires Verification | No confidentiality commitments, confidentiality policy, retention matrix, or complete key-management evidence. | Confidentiality only | Defer category until data commitments and controls justify it. |

## Prioritized Readiness Checklist

| Rank | Priority | Checklist item | Current state | Required action and completion criterion | Type |
| ---: | --- | --- | --- | --- | --- |
| 1 | P0 | Public repository database dump | Requires Verification | Determine data provenance without broadly exposing contents. Remove the dump from the current tree and history if prohibited or unnecessary; rotate affected credentials and execute incident response if exposure is possible. | Both |
| 2 | P0 | SOC 2 boundary and criteria decision | Missing | Approve system boundary, Security scope, exclusions, locations, services, subprocessors, customer responsibilities, owner, version, and review date. | Both |
| 3 | P0 | SOC 2 control matrix | Missing | Map every in-scope criterion to control, risk, owner, frequency, evidence, reviewer, and gap disposition. | Both |
| 4 | P0 | Control and evidence ownership | Missing | Assign accountable control owners and separate evidence reviewers; prohibit owner self-approval for material controls where practicable. | Both |
| 5 | P0 | Enterprise risk assessment | Missing | Complete and approve a FeDril organizational/security risk assessment and risk register covering system, people, vendors, public repository, and No-CUI obligations. | Both |
| 6 | P0 | Internal policy baseline | Missing | Approve security, access control, change management, incident response, vendor management, vulnerability management, acceptable use, retention, backup, and business-continuity policies. | Both |
| 7 | P0 | Examination decision gate | Planned Only | Define measurable proceed/defer conditions, budget, auditor selection, scope stability, open-gap tolerance, and customer trigger. | Both |
| 8 | P1 | Workforce MFA and conditional access | Requires Verification | Produce live identity-policy evidence for all workforce and privileged users, including break-glass governance. | Both |
| 9 | P1 | Joiner/mover/leaver process | Missing | Approve workflow, termination SLA, manager approval, asset/access removal, and sampled evidence. | Both |
| 10 | P1 | Periodic access reviews | Missing | Review source control, Azure, identity, production, support, database, monitoring, HubSpot, and evidence access at an approved cadence. | Both |
| 11 | P1 | Privileged-access inventory | Missing | Inventory human/service/admin identities, privileges, owner, purpose, last review, authentication method, and expiration. | Both |
| 12 | P1 | Separation of duties | Partially Implemented | Replace solo-controlled broader-production approval with independent change, production, risk, and evidence review; document unavoidable founder-stage conflicts. | Both |
| 13 | P1 | Repository exposure governance | Missing | Decide whether the repository remains public; remove unnecessary operational evidence and infrastructure detail; document approved public-information boundary. | Both |
| 14 | P1 | Production network hardening | Requires Verification | Reconcile live PostgreSQL ingress with Terraform, disable unnecessary public access, and document approved exceptions. | Both |
| 15 | P1 | Secrets and key management | Partially Implemented | Inventory secrets, move toward workload identity/managed identity and governed vaulting, disable shared keys where feasible, rotate, and review access. | Both |
| 16 | P1 | Infrastructure drift detection | Partially Implemented | Configure remote state and make scheduled drift checks execute successfully with reviewed findings. | Type 2 especially |
| 17 | P1 | Vulnerability-management program | Missing | Define sources, severity model, remediation SLAs, ownership, exception approval, rescans, metrics, and evidence. | Both |
| 18 | P1 | Security testing | Partially Implemented | Add SAST/DAST or equivalent risk-based testing, container/image scanning, and qualified penetration testing before examination. | Both |
| 19 | P1 | Incident-response program | Partially Implemented | Approve FeDril IR plan, communications, evidence handling, legal/customer notification decision process, tabletop, and action closure. | Both |
| 20 | P1 | Vendor/subprocessor management | Missing | Inventory subprocessors, classify risk, review contracts and assurance reports, track issues, and perform annual reassessment. | Both |
| 21 | P1 | Evidence repository and integrity | Missing | Establish access-controlled evidence storage, naming, retention, reviewer signoff, versioning, and prohibition on secrets/raw customer data. | Both |
| 22 | P1 | Logging and security monitoring | Partially Implemented | Verify telemetry ingestion/retention and add auth, privilege, deployment, cross-tenant, storage, malware, and anomalous-error alerts with response evidence. | Both |
| 23 | P1 | Backup and recovery governance | Partially Implemented | Approve RPO/RTO and retention; test production-equivalent restore; record reviewer, exceptions, and corrective actions. | Both |
| 24 | P2 | Change-management procedure | Documented But Not Verified | Formalize normal/emergency changes, reviewers, test evidence, migration/rollback, segregation, post-change review, and exceptions. | Both |
| 25 | P2 | Branch and environment protection evidence | Requires Verification | Export and review exact branch checks, review count, dismissal/bypass rules, environment approvers, and admin bypass privileges. | Both |
| 26 | P2 | Security awareness training | Missing | Deliver onboarding and annual training; retain attendance, content version, due dates, exceptions, and follow-up. | Type 2 especially |
| 27 | P2 | Data retention and disposal | Missing | Approve record-by-record retention schedule and verify deletion from primary systems, logs, exports, backups, and subprocessors. | Both |
| 28 | P2 | Business continuity | Missing | Complete business-impact analysis, continuity procedures, contact tree, recovery dependencies, and exercise. | Security support; Availability if scoped |
| 29 | P2 | Cloud shared-responsibility mapping | Missing | Map Azure/GitHub/identity inherited controls, customer responsibilities, assurance reports, complementary controls, and exceptions. | Both |
| 30 | P2 | Customer security claims register | Partially Implemented | Approve exact claims tied to evidence, owner, reviewer, scope, expiration, and supersession. | Both |
| 31 | P3 | Recurring evidence calendar | Missing | Schedule every manual and automated control with owner, reviewer, cadence, source, retention, and escalation. | Type 2 |
| 32 | P3 | Control failure and exception workflow | Partially Implemented | Standardize detection, severity, owner, due date, approval, corrective action, retest, closure, and residual risk. | Type 2 |
| 33 | P3 | Quarterly management review | Missing | Review risks, incidents, access, changes, vulnerabilities, vendors, backups, monitoring, exceptions, metrics, and scope changes. | Type 2 |
| 34 | P3 | Continuous readiness metrics | Missing | Track overdue evidence, failed controls, aging vulnerabilities, access-review completion, incidents, restore results, vendor reviews, and corrective actions. | Type 2 |
| 35 | P4 | Availability category | Deferred | Add only after commitments, SLOs, capacity, BCP, resilience, and recovery evidence are mature. | Future |
| 36 | P4 | Confidentiality category | Deferred | Add only after contractual confidentiality commitments, classification, retention, encryption, and disclosure controls are mature. | Future |

## Type 1 Readiness Gate

A Type 1 report evaluates control design and implementation as of a specified date. Repository artifacts can support this gate, but source code alone cannot establish that organizational controls were placed in operation.

| Requirement | Current state | Gate to proceed |
| --- | --- | --- |
| Approved scope and system description | Missing | Management and prospective auditor agree on system, services, boundaries, subprocessors, commitments, and exclusions. |
| Current licensed criteria/control mapping | Missing | Every scoped criterion maps to a control, risk, owner, and evidence source. |
| Policies approved and communicated | Missing | Required internal policies have owners, approval dates, versions, and acknowledgement evidence. |
| Controls implemented | Partial | No material design gap remains; technical and manual controls are operating as of the target date. |
| Workforce access governance | Missing | MFA, JML, privileged inventory, and access-review controls are evidenced. |
| Risk and vendor governance | Missing | Risk register and material vendor reviews are approved. |
| Incident and vulnerability programs | Partial/Missing | Plans, SLAs, exercises/tests, and open findings are governed. |
| Production configuration verified | Requires Verification | Live cloud, identity, source-control, monitoring, backup, and network configuration is reconciled. |
| Evidence index reviewed | Missing | Evidence is current, access-controlled, traceable, and independently reviewed. |
| Management assertion and examination decision | Missing | Management accepts residual risk and formally approves the as-of date and engagement. |

Minimum decision: **Do not schedule the Type 1 examination yet.** First close the P0 items and the material P1 governance gaps. A CPA readiness/scoping consultation may occur earlier and should not be represented as an examination.

## Type 2 Readiness Gate

A Type 2 report additionally evaluates operating effectiveness over a defined period. The period and sampling approach must be agreed with the service auditor; no universal period is assumed here.

| Recurring control | Proposed cadence | Expected operating evidence | Current maturity |
| --- | --- | --- | --- |
| Workforce onboarding/offboarding | Per event | Approved request, provisioning/removal timestamps, exception follow-up | Missing |
| Privileged and user access review | Quarterly initially | Population, reviewer, decisions, removals, completion approval | Missing |
| Change approval | Per change | Ticket/PR, reviewer, tests, deployment, rollback, exceptions | Partial |
| Vulnerability scanning/remediation | Continuous/weekly scan; SLA by severity | Scan result, ticket, owner, due date, rescan, exception | Partial |
| Security monitoring | Continuous; daily/weekly review by risk | Alert, triage, escalation, response, closure | Partial |
| Incident response | Per event; annual tabletop minimum as management chooses | Incident record/tabletop, decisions, communications, corrective actions | Documented But Not Verified |
| Backup monitoring | Daily/automated | Success/failure records, escalation, corrective action | Requires Verification |
| Restore testing | At approved risk-based cadence | Scope, RPO/RTO result, reviewer, gaps, retest | Partial; staging only |
| Vendor review | Before onboarding and annually for material vendors | Due diligence, contract/assurance review, findings, approval | Missing |
| Risk assessment | Annual and on significant change | Risk register, treatment decisions, approval, follow-up | Missing |
| Security training | On hire and annually | Completion records, overdue follow-up, content version | Missing |
| Evidence quality review | Monthly/quarterly | Completeness/freshness review, rejected evidence, correction | Missing |
| Management security review | Quarterly | Agenda, metrics, decisions, owners, due dates, closure | Missing |
| Claims review | Before publication and at least annually | Approved wording, evidence reference, owner, expiration | Partial |

Minimum decision: **Do not start a Type 2 observation period until the Type 1 design/implementation gate is substantially satisfied and recurring controls have owners, cadences, evidence templates, and failure handling.**

## Material Gap Register

| ID | Severity | Gap | Owner role | Required remediation | Closure evidence | Residual risk |
| --- | --- | --- | --- | --- | --- | --- |
| SOC2-GAP-001 | Critical pending verification | Database dump committed to public repository | Security/Engineering | Validate provenance, contain exposure, remove current/history as appropriate, rotate affected credentials, document incident decision | Investigation record, history remediation, scan, approvals | Prior clones/forks may retain data |
| SOC2-GAP-002 | High | No approved SOC 2 scope/control matrix | Executive/Security | Complete Story 39.1 artifacts | Approved scope, matrix, decision record | Scope change risk |
| SOC2-GAP-003 | High | Missing organizational policy baseline | Security/Executive | Draft, approve, communicate, and review policies | Versioned policies and acknowledgements | Policies may not operate consistently |
| SOC2-GAP-004 | High | No enterprise risk assessment | Security/Executive | Establish risk methodology/register and treatment process | Approved assessment and action log | Emerging risks remain |
| SOC2-GAP-005 | High | Workforce access lifecycle and reviews unverified | IT/Security | Implement JML, MFA evidence, privileged inventory, periodic reviews | Event samples and completed review | Identity-provider dependencies |
| SOC2-GAP-006 | High | Vendor/subprocessor governance missing | Security/Legal | Inventory and review material vendors | Register, due diligence, contracts, assurance review | Fourth-party risk |
| SOC2-GAP-007 | High | Separation of duties incomplete | Executive/Engineering | Add independent approvals and conflict handling | Branch/environment rules and approval samples | Founder-stage concentration risk |
| SOC2-GAP-008 | High | Production network/secrets configuration needs reconciliation | Engineering/Security | Verify and harden live state | Approved configuration export and retest | Cloud misconfiguration risk |
| SOC2-GAP-009 | High | Vulnerability program incomplete | Engineering/Security | Add policy, SLAs, coverage, triage, retest, pen test | Scan/pen-test reports and closure records | Zero-day and supply-chain risk |
| SOC2-GAP-010 | High | Corporate incident response not proven | Security/Executive | Approve plan and run tabletop | Plan, exercise, findings, closure | Real incidents may differ |
| SOC2-GAP-011 | Medium-High | Production-equivalent recovery unproven | Engineering/Security | Approve RPO/RTO and execute test | Restore report and corrective actions | Regional/provider failure |
| SOC2-GAP-012 | Medium-High | Monitoring scope and telemetry ingestion unverified | Engineering/Security | Validate telemetry and add security alerts | Live queries, alert tests, reviews | Detection gaps |
| SOC2-GAP-013 | Medium | Retention/disposal program missing | Security/Legal | Approve retention matrix and disposal verification | Policy, deletion tests, vendor alignment | Backup/vendor remnants |
| SOC2-GAP-014 | Medium | Security training missing | Executive/Security | Implement onboarding/annual training | Content and completion records | Human-error risk |
| SOC2-GAP-015 | Medium | Type 2 evidence calendar missing | SOC 2 owner | Build and operate evidence calendar | Completed cycles and review log | Missed/stale evidence |

## Sustainable Operating Model

- Executive sponsor approves scope, risk tolerance, examination timing, exceptions, and report distribution.
- SOC 2 program owner maintains the control matrix, evidence calendar, issue register, and auditor coordination.
- Control owners execute controls; evidence custodians collect artifacts; reviewers independently validate evidence where practicable.
- Engineering owns secure SDLC, changes, vulnerabilities, infrastructure, backup/recovery, logging, and remediation.
- Security owns risk, access reviews, incidents, vendors, training, monitoring requirements, and control testing.
- Legal/privacy review customer commitments, retention, subprocessors, confidentiality, and report-distribution terms.
- Management reviews the program quarterly and after significant system, vendor, data, or organizational changes.
- Evidence stays in access-controlled storage; backlog items contain references, not secrets, raw customer files, or sensitive vulnerability detail.

## Comparison With Stories 39.1-39.3

### Story 39.1

What works: it correctly treats scope as governance, starts with Security, preserves No-CUI boundaries, requires owners/evidence sources, and prevents premature audit claims.

Modification recommended: add an explicit current-state control matrix with separate design, implementation, and operating-evidence conclusions; record assessment commit/date and evidence freshness; require repository/public-information risk review; and define measurable Type 1/Type 2 engagement gates.

Suggested addition:

> The scope decision must include a current-state control matrix that separately rates control design, implementation, and operating evidence; records the reviewed repository commit, environment, evidence period, freshness, and limitations; evaluates repository visibility and exposed operational artifacts; and defines objective Type 1 and Type 2 proceed/defer gates.

### Story 39.2

What works: it already requires gap severity, owners, due dates, evidence requirements, exceptions, accepted risks, corrective actions, and an evidence calendar.

Modification recommended: add evidence-quality rules, population/sample traceability, stale-evidence handling, recurring-control failure escalation, and separate closure validation for design versus operating effectiveness.

Suggested addition:

> Each evidence item must identify the complete population or sampling basis when applicable, execution date or period, source system, environment, custodian, independent reviewer, freshness, exceptions, and control conclusion. Gap closure must state whether it closes a design, implementation, or operating-effectiveness deficiency and must include retest evidence.

### Story 39.3

What works: it clearly separates readiness from an issued report, governs auditor independence and report distribution, controls public claims, and includes renewal planning.

Modification recommended: no structural rewrite. Add a pre-engagement gate requiring scope stability, resolved material gaps, management assertion readiness, auditor independence/conflicts review, evidence-index approval, budget, and customer/business justification.

Suggested addition:

> Before engagement execution, record an approved pre-engagement gate covering scope stability, material open gaps, management assertion readiness, auditor licensing/independence and conflicts, evidence-index approval, budget, proposed as-of date or period, and verified customer, partner, or procurement justification.

## Roadmap

| Phase | Objective | Exit criteria |
| --- | --- | --- |
| 1. Scope and contain | Resolve public-repository risk; approve scope, owner, criteria, and control matrix | Critical exposure decision closed; scope and accountability approved |
| 2. Build governance | Approve policies, risk, access, vendors, incidents, vulnerabilities, retention, and training | No material design gaps without approved remediation/exception |
| 3. Verify implementation | Reconcile live identity, Azure, GitHub, monitoring, backups, CI/CD, and evidence storage | Controls placed in operation with current evidence |
| 4. Type 1 decision | Perform readiness review with CPA input | Management proceed/defer decision and stable as-of date |
| 5. Operate controls | Execute evidence calendar and remediate failures | Sufficient clean operating history for agreed period |
| 6. Type 2 decision | Confirm scope, period, exceptions, and evidence completeness | Management and auditor accept examination readiness |
| 7. Continuous readiness | Quarterly reviews, annual scope/risk/vendor review, ongoing evidence and claims governance | Recurring controls remain current and failures close on time |

### Next 30 days

- Resolve the committed database dump and public-repository boundary.
- Approve Story 39.1 scope and appoint program/control owners.
- Build the control matrix, risk register, policy inventory, vendor inventory, privileged-access inventory, and evidence calendar.
- Verify branch/environment protections, MFA, production networking, telemetry, drift, secrets/key practices, and backup design.
- Select a CPA firm for a scoping/readiness conversation only if customer demand or procurement justifies it.

### Next 60-90 days

- Approve and operate missing policies/processes.
- Complete access review, vendor reviews, incident tabletop, security training, vulnerability cycle, production-equivalent restore, and management review.
- Perform independent penetration testing and close material findings.
- Reassess Type 1 readiness using current evidence.

## Sources And Evidence Index

Authoritative framework sources, accessed 2026-09-19:

- AICPA & CIMA, [System and Organization Controls: SOC Suite of Services](https://www.aicpa-cima.com/resources/landing/system-and-organization-controls-soc-suite-of-services).
- AICPA & CIMA, [2017 Trust Services Criteria (With Revised Points of Focus - 2022)](https://www.aicpa-cima.com/resources/download/2017-trust-services-criteria-with-revised-points-of-focus-2022).

Principal repository evidence at reviewed commit:

- `README.md`, `AGENTS.md`, and `docs/architecture.md`
- `apps/api/Security/ApiSecurityExtensions.cs`, `HttpTenantContext.cs`, `PlatformAuthorization.cs`, and `RolePermissionClaimsTransformation.cs`
- `src/Gccs.Application/Audit/*` and `src/Gccs.Infrastructure/Audit/*`
- `tests/Gccs.Api.Tests/AuthenticationBoundaryTests.cs`, `AuditAppendOnlyTests.cs`, `SecurityIsolationVerificationTests.cs`, and `RoleBasedPermissionTests.cs`
- `.github/workflows/ci.yml`, `production.yml`, `staging.yml`, and `production-infrastructure-drift.yml`
- `infra/terraform/environments/production/main.tf`
- Production-readiness checklist, deployment, monitoring, backup/restore, support, staging-security, and customer-claims records under `docs/`
- GitHub branch metadata showing `main` is protected and reviewed check runs showing successful CI/security jobs for the assessed SHA
- Public repository metadata and `.codex/db-backups/gccs-20260615235232.dump` file metadata; dump contents were intentionally not exposed or relied upon

## Limitations Register

- No live Azure, Entra, GitHub settings, HubSpot, email, support-ticket, HR/personnel, contract, or evidence-repository console was inspected.
- Classic branch-protection details were inaccessible to the integration; only `protected=true` was verified. Repository rulesets returned none.
- No licensed full Trust Services Criteria mapping or selected CPA firm's examination procedures were available.
- Repository documents may be self-authored and are not independent assurance.
- Successful tests prove specific tested behavior, not complete production operation.
- Production deployment evidence exists, but recurring Type 2 operation was not established.
- No secrets, database dump contents, private customer data, or sensitive vulnerability details were inspected.
- This report is professional readiness analysis, not legal advice, CPA attestation, certification, or authorization to process CUI.

Repository inspection alone cannot establish SOC 2 readiness or Type 2 operating effectiveness.
