

Publication classification: **Sanitized public reference**. Complete originals and original SHA-256 hashes remain in protected `release-scope-lookups-20261018-v1/primary-source-snapshot-v1`. This counterpart records scope and traceability; it does not establish workforce control effectiveness, review completion or acceptance.

# Story 39.1 SOC 2 Scope And Readiness Record

Status: **Setup gate met / defer; final acceptance blocked**
Scope version: `SOC2-SCOPE-DRAFT-0.1`
Assessment timestamp: 2026-09-28 EDT (`UTC-04:00`), refreshing the original 2026-09-19 assessment
Repository state assessed: commit `885ecc2a6e10a8479ddc0c23ba31763bd73995cf` on `codex/local-restored-readiness`, 10 commits behind and 3 commits ahead of `origin/main`, plus preserved uncommitted story-prompt and SOC 2 record changes
Production evidence cutoff: sanitized read-only GitHub and Azure capability observations through 2026-09-28; no Story 39.1 deployment was required or performed
Management-input cutoff: 2026-09-28 EDT
Historical assessment reconciled: `docs/soc2-readiness-assessment.md` from commit `1a5160c2` (assessment commit `6e38c20392c806c9c1355ea94f63954b0bdfca6e`)
Work type: governance assessment; no application, infrastructure, account, or production changes were authorized or made

This is a sanitized repository index and candidate decision record. It is not the protected SOC 2 workspace, a management assertion, qualified CPA advice, an examination, an issued report, a certification, a legal conclusion, or authorization to process CUI.

Management role appointment: `Founder authority FR-01` is assigned under [the founder role concentration decision](founder-role-concentration-decision.md). Explicit acknowledgement `FR01-ACK-2026-09-19` was supplied in the execution thread; protected preservation/integrity evidence and all independent or professionally qualified reviewers remain requirements.

## Current-State Reconciliation — 2026-10-11

The current authoritative Story 39 source SHA-256 is
`33fb117b4081db28cd4ddbb939077e2bca38d9b9ace3fd4b94ce89151da6be67`.
The reviewed repository state is `main` at
`548a6e80153de0d19d0630e5e35e614a4f071c8c`, synchronized with
`origin/main` at the reconciliation, plus preserved user-owned working changes.
GitHub metadata collected on 2026-10-11 shows that the repository remains public
and PR #129 was closed without merge on 2026-10-10. Closure did not classify,
contain, remove, or independently validate the historical exposure, so Story
39.4 remains open and final Story 39.1 acceptance remains blocked.

Under `SOC2-ROLE-FR-01`, FR-01 may complete the management-side preparation,
operation, evidence custody, decisions, and explicitly labeled management
self-review permitted by Stories 39.1 through 39.23. This update did not perform
FR-01's management self-review. The current task boundary is:

| Tasks | Management self-review result | External dependency |
| --- | --- | --- |
| `SOC2-39.1-T01`–`T03`, `T05` | Drafts are available for FR-01 review; result pending until FR-01 actually reviews and records a decision | Applicable evidence, Security, Legal/Privacy, operations, and CPA review remain unassigned and deferred |
| `SOC2-39.1-T04` | Cannot complete through management self-review | Authorized current TSC/Description Criteria access and qualified CPA review remain missing and deferred |
| `SOC2-39.1-T06` | Internal route, demand, budget, and gate planning may continue; management decision remains defer | Qualified CPA and applicable Legal/Procurement input remain missing and deferred |
| `SOC2-39.1-T07` | Not currently executable | Stories 39.5–39.17 outputs, approved mapping, and qualified review remain pending |

These deferred dependencies are not fulfilled and are not `Not Applicable`.
Their continuing unavailability does not require repeated appointment requests
during otherwise safe authorized management work, but every existing
independent, qualified, protected-approval, Story 39.4, readiness, and final
acceptance gate remains in force. The setup gate remains met; all final
acceptance conditions remain unmet.

## Executive Decision

**Decision: defer any Type I engagement, Type II observation period, or readiness claim.** Founder authority FR-01 is appointed at the management-role level. A preliminary CPA scoping consultation may occur after the protected identity/appointment record and funding are approved, but it must not be represented as an examination.

The Story 39.1 **setup gate is met**: a versioned candidate boundary exists, Founder authority FR-01 is the acknowledged management sponsor and SOC 2 owner, Security is provisionally selected as the baseline category, the four additional categories have explicit defer/revisit decisions, a criterion-indexed draft matrix exists, and unanswered facts are explicit. This setup result authorizes later scoped program work only; it is not an approved examination boundary or readiness claim.

Final acceptance is blocked. The exact unresolved gate is: Founder authority FR-01 must preserve and approve the candidate scope decision in the protected workspace, and an appointed qualified independent CPA reviewer must review the current authorized criteria, category decisions, system description and scoped mapping. The final mapping also depends on accepted or approved-not-applicable outputs from Stories 39.5 through 39.17 through `SOC2-39.1-T07`. No such qualified review, protected approval record or final reconciliation is available.

Immediate P0 condition: GitHub reports the production source repository as public, and a tracked database-backup artifact exists in commit history. Its identifier and contents are intentionally omitted here, and its contents were not opened. Story 39.4 exposure triage must start immediately under separate authority. History rewriting, deletion, credential rotation, repository visibility changes, notification, and incident decisions were not performed by this story.

Setup implementation references:

- [Story 39.2 program setup](story-39.2-program-setup-record.md)
- [Story 39.3 examination and claims governance](story-39.3-examination-governance-setup.md)
- [Story 39.4 exposure investigation](story-39.4-exposure-investigation.md)

These records implement the safe setup and preventive portions of the stage sequence. They do not replace named appointments, protected evidence storage, qualified review, authorized containment or final approval.

## Critique And Failure Modes

- Public Git cannot serve as the protected evidence repository. The tracked database dump creates a plausible disclosure path, and repository clones or forks can survive later deletion.
- Source code, passing tests, Terraform, and self-authored production-readiness records do not prove that workforce, vendor, risk, access-review, incident, training, and management controls operated over a period.
- A scope without named accountable people, licensed-criteria review, customer or procurement justification, budget, and qualified review is a planning artifact, not an authorized examination boundary.

The required pattern is a protected, access-controlled evidence workspace with a sanitized Git index; named management, control, evidence-custodian, reviewer, and approver assignments; current authorized criteria review; live-system reconciliation; and separate design, implementation, and operating-evidence conclusions.

## Evidence Inventory

| Evidence ID | Source and date | What was verified | Reliability and freshness | Limitation / unavailable evidence |
| --- | --- | --- | --- | --- |
| `S39.1-E01` | Historical assessment from commit `1a5160c2`, dated 2026-09-19 | Original 36-row checklist, 15 gap IDs, candidate Security scope, and defer recommendation | Repository history; same-day historical baseline | Self-authored; not independent; assessed older commit and did not inspect live systems |
| `S39.1-E02` | `README.md`, `AGENTS.md`, `docs/architecture.md`, `docs/security-control-implications.md` at assessed commit | Product, architecture, No-CUI, tenant, RBAC, audit, and evidence-traceability design claims | Current repository design evidence | Does not prove deployed configuration or operation |
| `S39.1-E03` | `infra/terraform/environments/production/main.tf` and README | Declared Azure boundary, regions, services, seven-day database backups, private endpoints, monitoring, and known adoption gaps | Current declarative configuration | Remote state/import adoption is incomplete; app settings are ignored; Terraform is not proof of live state |
| `S39.1-E04` | Read-only Azure CLI observations, 2026-09-19 | Live service families and selected web, database, storage, cache, network, monitoring, identity, RBAC, policy and recovery configurations were inventoried | Direct live control-plane observation at one point in time | Point-in-time configuration only; operating effectiveness, inaccessible query areas and non-Azure controls remain unverified |
| `S39.1-E05` | GitHub API observations, 2026-09-19 | Repository public; main blocks force-push/deletion, enforces admins, requires Backend/Frontend/Secret checks, dismisses stale reviews, but requires zero approvals; production environment has one required reviewer and protected-branch policy | Direct live GitHub observation at one point in time | Protected evidence reference; individual observations and source details are retained in the protected original. |
| `S39.1-E06` | `.github/workflows/ci.yml`, production/release/drift workflows and GitHub run metadata | CI/security jobs and protected deployment workflow exist; main CI at `52ac621...` passed; current assessed commit CI was still in progress; latest drift run was skipped | Current workflow design plus run metadata | No operating-period population; skipped drift run and incomplete current-commit CI prevent current-state reliance |
| `S39.1-E07` | Production-readiness deployment, smoke, backup/restore, claims, support, launch, and scanner records | Sanitized evidence exists for selected technical deployments and synthetic-only tests | Repository evidence with dates and recorded limitations | Mostly self-authored; some records are stale or limited to staging/solo-controlled pilot; no independent broad-launch approval |
| `S39.1-E08` | Security-focused API and frontend test sources | Tests cover selected tenant isolation, RBAC, audit atomicity, No-CUI, upload, report, portal, and error-contract behavior | Direct test-design evidence | Tests were not rerun for this governance-only execution; test presence is not production operation |
| `S39.1-E09` | AICPA & CIMA official SOC resource pages accessed 2026-09-19 | Current official listings identify 2017 TSC with revised points of focus (2022) and 2018 Description Criteria with revised implementation guidance (2022) | Authoritative publisher landing pages | Full TSC download requires account access; no authorized licensed copy or qualified reviewer mapping was provided |
| `S39.1-E10` | Repository search for customer, partner, and procurement demand, 2026-09-19 | No verified SOC 2 customer/partner/procurement requirement found; roadmap says SOC 2 is not an MVP gate absent approved demand | Direct repository search | Contracts, CRM, questionnaires, sales communications, and procurement records were not provided |
| `S39.1-E11` | Git metadata for tracked database dump; contents intentionally unopened | Public-repository exposure is plausible and requires triage | Direct Git/GitHub metadata | Data classification, provenance, affected parties, prior access, and credential impact are unknown |
| `S39.1-E12` | `FR01-COMPETENCE-2026-09-19` | Protected evidence reference; individual observations and source details are retained in the protected original. Recorded status: planned. | Management self-attestation | Professional history, training, certifications, direct compliance tenure and independent review unavailable |
| `S39.1-E13` | `AZ-SOC2-INV-2026-09-19` | Sanitized Azure inventory records accessible scope, configuration positives, control gaps and unavailable queries without identifiers, secrets, customer content or raw logs | Direct read-only control-plane evidence collected 2026-09-20 02:00 UTC | Raw export not preserved because the protected workspace is unavailable; no independent review or period evidence |
| `S39.1-E15` | `SOC2-20260927-E001` and `SOC2-20260927-E002`, collected 2026-09-27 | Current assessment inventory independently reconfirmed the Story 39.1 artifacts and No-CUI boundary as only partially verified | Sanitized repository evidence inventory with explicit source paths and limitations | Authorized scope, owners, qualified review and final proceed/defer approval remained unresolved |
| `S39.1-E16` | Read-only GitHub metadata, 2026-09-28 | Repository remains public; `main` enforces administrators and three required checks but requires zero approving reviews; recent `main` CI and staging runs succeeded | Direct point-in-time provider metadata | Does not prove historical operation, reviewer independence, access populations or Story 39.1 acceptance; an unrelated artifact-policy run was still in progress at cutoff |
| `S39.1-E17` | Current source and orchestration reconciliation, 2026-09-28 | Authoritative working-copy SHA-256 is `72129c1acd3825ae71d85c6879cf2b57761bcb9802bc8a140980cb9926559248`; committed-HEAD SHA-256 is `9f61e499cfcdfb2676a11418b164135c000c4cf00cd9566c437abfc366def06c`; the current Story 39.1 acceptance wording is unchanged by the local prompt edits | Direct Git and file-hash comparison | Working-copy prompt edits are uncommitted and must remain preserved; later acceptance must re-reconcile if the authoritative document changes |
| `S39.1-E18` | Source, repository and provider reconciliation, 2026-10-11 | Current source SHA-256 `33fb117b4081db28cd4ddbb939077e2bca38d9b9ace3fd4b94ce89151da6be67`; `main` at `548a6e80153de0d19d0630e5e35e614a4f071c8c` synchronized with `origin/main`; repository public; PR #129 closed unmerged on 2026-10-10 | Direct Git/file hash and authorized read-only GitHub metadata collected 2026-10-11 | Point-in-time metadata only; PR closure is not containment, the working tree remains user-owned and dirty, and no management self-review, qualified review, scope approval or final reconciliation occurred |

Additional evidence `S39.1-E14`: [SOC2-BOUNDARY-DRAFT-0.1](system-boundary-index.md), assessed 2026-09-19 EDT, reconciles architecture, source registration, test sources and the local release manifest. Reliability: repository design/implementation evidence; limitations: no tests rerun, no deployed-release verification, protected full description and independent approval unavailable.

Unavailable evidence is not treated as proof that a control is absent. It is treated as `unverified` until an authorized collector and reviewer supply protected evidence.

## Candidate System Boundary

The canonical sanitized architecture reconciliation is [SOC2-BOUNDARY-DRAFT-0.1](system-boundary-index.md), evidence `S39.1-E14`. It supplements the candidate summary below with component/data flows, supporting environments, source references, release reconciliation, responsibilities and approval/change history. It is not the approved protected system description. The assessed checkout differs from the approved release manifest; production feature enablement remains unverified.

### Included services and production assets

| Boundary area | Candidate inclusion | Current evidence conclusion |
| --- | --- | --- |
| Customer-facing service | Authenticated FeDril React/Vite workspace and ASP.NET Core API | Included; design and selected deployment evidence available |
| Application functions | Tenant-scoped compliance profiles, contracts, obligations, tasks, evidence, reporting, readiness workflows, portals, notifications, and governed/draft assistance | Included where deployed and used to deliver FeDril; exact production feature inventory requires release reconciliation |
| Data services | Azure PostgreSQL Flexible Server, Azure Blob Storage, Azure Cache for Redis/coordination, and application-managed records | Included; live high-level state partly verified |
| Runtime and delivery | Azure App Service, Azure Static Web Apps, Azure Container Instances malware scanner, GitHub source control/Actions/releases | Included; GitHub and Azure are material dependencies |
| Identity | Workforce Microsoft Entra identity and configured customer identity boundary | Protected evidence reference; individual observations and source details are retained in the protected original. |
| Monitoring and operations | Application Insights, Azure Monitor alerting, health checks, production support, incident handling, backups, restore, and deployment rollback | Included; selected configuration/evidence exists, operating program incomplete |
| Communications and integrations | Azure Communication Services email, HubSpot demo-request synchronization, SAM.gov lookup where enabled, and any enabled external API/provider | Included when production-enabled; contracts, flows, retention, and vendor reviews unverified |
| Security services | ClamAV-compatible malware scanning, GitHub dependency/secret scanning, authentication/RBAC, audit, No-CUI controls | Included; selected technical controls exist, period operation incomplete |

### Software, people, procedures, locations, and environments
Protected evidence reference; individual observations and source details are retained in the protected original.

### Candidate subprocessors and supporting providers

| Provider/dependency | Candidate role | Treatment |
| --- | --- | --- |
| Microsoft Azure | Hosting, database, storage, cache, runtime, monitoring, email, identity-related services | Material subservice organization; contract, shared responsibility, regions, assurance reports, and complementary controls require review |
| GitHub | Public source repository, CI/CD, release artifacts, environments, secrets, and approvals | Material production-support provider; public-repository boundary and access governance are in scope |
| Microsoft Entra / customer identity provider | Workforce/customer authentication | Material dependency; policies and populations require live review |
| HubSpot | Demo-request/contact synchronization when enabled | Candidate subprocessor; data fields, retention, contract, security review, and production use require verification |
| ClamAV container/image supply chain | Malware-scanning engine and image updates | Material security dependency; image provenance, update, alert, capacity, and failure operation require review |
| SAM.gov and other public authoritative sources | Lookup/source data | External dependency; availability, integrity handling, terms, and caching require verification |

Protected evidence reference; individual observations and source details are retained in the protected original.

### Customer responsibilities

- Provide accurate tenant/user information and maintain authorized customer users and roles.
- Keep credentials secure and promptly remove users who no longer require access.
- Use FeDril only for the approved No-CUI compliance-management posture and do not upload CUI, classified information, export-controlled/ITAR technical data, credentials, sensitive government-furnished information, or other prohibited data.
- Classify and review customer-supplied records and generated drafts, validate compliance/legal conclusions with qualified professionals, and retain responsibility for government submissions and assessor decisions.
- Configure and operate customer/MSP systems, enclaves, endpoints, networks, backups, and C3PAO interactions outside FeDril.
- Report suspected incidents, prohibited-data uploads, access problems, and inaccurate records through the approved support path.

Customer responsibility language requires contract/legal review and must be reconciled with the actual UI, API enforcement, terms, privacy notice, and support procedures before publication.

### Explicit exclusions and boundary rules

- Real customer CUI, classified information, export-controlled/ITAR technical data, and sensitive government-furnished information are excluded and prohibited under the current No-CUI posture.
- Customer-owned or MSP-managed networks, endpoints, identity, backups, enclaves, and C3PAO assessment work are excluded from FeDril operation, but FeDril's interfaces, commitments, dependencies, and shared-responsibility statements remain in scope.
- Legal advice, accounting advice, labor determinations, CMMC certification, FedRAMP authorization, government approval/endorsement, and assessor conclusions are excluded service commitments.
- Future CUI enclaves, government-cloud environments, customer-managed-key deployments, or private deployments are excluded until separately approved and actually delivered. Current code or roadmap scaffolding does not place those future services in this candidate boundary.
- Customer applications and third-party portals are excluded except to the extent FeDril sends, receives, stores, or relies on data or access from them.

## Service Commitments And System Requirements

### Verified current commitments

- FeDril is a multi-tenant compliance-management SaaS for small U.S. government contractors.
- The current production posture is No-CUI / compliance management only.
- Customer-facing claims must avoid certification, general compliance, government approval, legal advice, and audit-readiness claims.
- Authenticated tenant-scoped workflows, server-authoritative permissions, audit traceability, evidence/source traceability, and controlled releases are product requirements documented in repository governance.
- Production uses commercial Azure and is not represented as FedRAMP authorized or approved for CUI.

### Requirements needing management or contract validation

- Availability targets, support hours, response times, maintenance commitments, recovery objectives, data retention, deletion, confidentiality, breach-notification terms, and data-residency promises.
- Exact privacy obligations, personal-data categories, data-subject processes, and controller/processor roles.
- Which integrations and roadmap functions are contractual production services.
- Complementary customer controls and subservice-organization treatment.

No SLA, DPA, customer contract, security addendum, procurement questionnaire, or verified SOC 2 demand was supplied. No undocumented commitment is inferred.

## Trust Services Category Decision

| Category | Candidate decision | Rationale | Decision owner / reviewer | Revisit gate |
| --- | --- | --- | --- | --- |
| Security / common criteria | **Provisional in scope** | Baseline relevant to the SaaS service and all supporting people/processes | Unassigned / qualified CPA review required | Approve scope and authorized criterion matrix |
| Availability | **Deferred, separately assessed** | Technical health, backup, and monitoring controls exist, but no verified SLA, capacity/resilience commitment, approved RPO/RTO, or production recovery program was provided | Unassigned | Story 39.14 evidence plus verified customer/contract trigger and reviewer advice |
| Confidentiality | **Deferred, separately assessed** | No-CUI, RBAC, and encryption-oriented controls exist, but contractual confidentiality commitments, classification scope, retention, disclosure, and key governance are incomplete/unverified | Unassigned | Stories 39.10, 39.15, 39.16 and verified commitments |
| Processing Integrity | **Deferred, separately assessed** | FeDril tracks compliance data and produces draft/report artifacts, but no approved processing-integrity commitments, completeness/accuracy objectives, or full input-processing-output control set was supplied | Unassigned | Story 39.17 decision after commitments and processing controls are mapped |
| Privacy | **Deferred, separately assessed** | Personal data exists in identity, customer, support, demo, and workforce contexts, but privacy notice, inventory, roles, rights handling, retention, and legal review were not supplied | Unassigned | Story 39.17 decision after privacy inventory and qualified review |

Deferral is not a finding that a category is irrelevant. It blocks omission from the final scope until management documents commitments, risks, and qualified review.

## Current-State Control Conclusions

Detailed provisional Security/common-criteria mapping is in `story-39.1-common-criteria-matrix.csv`. Criterion identifiers are used only as an index; the matrix does not reproduce AICPA criteria text and is not represented as an authorized licensed review.

| Control family | Design | Implementation | Operating evidence | Evidence basis | Material limitation / gap |
| --- | --- | --- | --- | --- | --- |
| Governance, ethics, competence, accountability | Gap | Partial | Unavailable | FR-01 appointment/acknowledgement and self-attested competence baseline; solo-controlled launch approvals and project instructions | Protected appointment/professional history, approved training, management review and independent approval unavailable |
| Information and communication | Partial | Partial | Partial | Product notices, No-CUI copy, support and claims records | Internal policy distribution, customer commitments, escalation populations, and recurring reviews unavailable |
| Risk assessment | Gap | Not implemented or unverified | Unavailable | Historical assessment and release risk records | No approved enterprise risk methodology/register or fraud/significant-change review |
| Monitoring and remediation | Partial | Partial | Partial | CI, alert, launch, test and exception artifacts; `AZ-SOC2-INV-2026-09-19` verified one alert, two action groups, a 30-day log workspace and partial diagnostic coverage | No complete control-monitoring program, alert-delivery test, resource-specific diagnostic baseline, independent evidence review, or period population |
| Control activities and policy deployment | Partial | Partial | Partial | RBAC, audit, No-CUI, release, validation, and test evidence | Corporate policy baseline, workforce operation, and consistent period execution absent |
| Logical access, boundary protection, data protection | Partial | Partial | Partial | Code/tests, GitHub observations, deployment records and `AZ-SOC2-INV-2026-09-19` | Protected evidence reference; individual observations and source details are retained in the protected original. |
| Operations, vulnerability, incident, recovery | Partial | Partial | Partial | Health, alert, malware, support, restore, incident-readiness artifacts and `AZ-SOC2-INV-2026-09-19` | Corporate IR/vulnerability programs, diagnostic and alert-review coverage, storage recovery, geo-redundant database backup, HA, production recovery, recurring operation and closure evidence incomplete |
| Change management | Adequate for selected technical design; governance gap | Partial | Partial | CI/release workflows, protected checks, production reviewer | Main requires zero PR approvals; solo-control conflict, emergency-change operation, current-commit CI, and drift evidence incomplete |
| Vendor and business-risk mitigation | Gap | Unverified | Unavailable | Candidate provider inventory only | No approved subprocessor register, due diligence, contract/assurance review, BCP/BIA, or recurring monitoring |

Repository exposure is a cross-cutting limitation. It prevents an adequate confidentiality/security conclusion for the affected source/evidence boundary until Story 39.4 determines provenance, impact, containment, and residual risk.

## Proceed / Defer Gates

### Type I gate

Proceed only when all conditions are evidenced and approved:

Protected evidence reference; individual observations and source details are retained in the protected original.

Current result: **defer**. Conditions 1 through 9 are incomplete or unverified.

### Type II entry gate

In addition to the Type I design/implementation conditions:

1. The service auditor and management agree on the route, review period, populations, sampling approach, and evidence expectations; no universal duration is assumed.
2. Story 39.20 defines complete populations, control frequencies, no-event handling, evidence collection/review deadlines, failure alerts, retention, and significant-change rules.
3. At least one successful reviewed cycle exists for every recurring control before the proposed period; known exceptions have approved treatment and retest plans.
4. Evidence can demonstrate complete period operation, including failures, late/missing evidence, corrective actions, access changes, vendor events, incidents, vulnerabilities, changes, monitoring, backups, and management review.
5. Budget, staffing, owner capacity, protected storage, reviewer independence, and examination support are approved.

Current result: **do not start a Type II observation period**.

### Route, budget, triggers, approvers, and review date

| Decision field | Current record |
| --- | --- |
| Route | Deferred. Type I-first versus direct-Type II must be selected with the qualified service auditor after design/implementation readiness; no route is approved. |
| Budget | Historical context: the $15/month core-storage planning ceiling and FR-01's 2026-09-28 no-provisioning/no-spend direction are superseded only for the bounded workspace setup. Current workspace authorization, dated 2026-10-06, permits up to $55/month total recurring and $0 upfront for a separate new FeDril LLC corporate target; the target remains unimplemented/unaccepted and this is not examination funding. `SOC2-CAPACITY-BUDGET-0.1` preserves the expired 50-hour/week period and records FR-01's self-attested 40 hours/week for internal Type I readiness through 2026-10-30. Capacity after that window, external-review funding, category estimates and the overall examination budget remain unapproved. |
| Commercial trigger | No verified customer, partner, or procurement requirement found in supplied evidence. Do not treat roadmap interest as a commitment. |
| Approvers | Founder authority FR-01 is the acknowledged management decision-maker; protected acknowledgement preservation and independent qualified review remain unavailable. |
| Qualified reviewer | Unassigned licensed/qualified CPA advisor; independence/conflicts not assessed. |
| Review date | Proposed 2026-10-02 for candidate-scope review, subject to protected preservation of the sponsor appointment, reviewer appointments and P0 triage. Not an approved commitment. |

## Shared Task Register

Founder authority FR-01 holds the consolidated management roles recorded in `SOC2-ROLE-FR-01`. External security, legal/privacy, evidence and CPA roles remain vacancies. Proposed dates are planning estimates and require FR-01 approval in the protected record.

| Task | Status | Owner | Reviewer / approver | Proposed due date | Priority | Prerequisites | Criterion/control mapping | Type impact | Evidence / verification outcome |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `SOC2-39.1-T01` | Ready For Review | Founder authority FR-01 | External evidence reviewer unassigned | 2026-09-23 (**overdue**) | P0 | P0 exposure triage starts immediately | All scoped criteria; assessment reconciliation | Both | Historical assessment and the 2026-09-27 evidence inventory were reconciled with sanitized live metadata on 2026-09-28. Story 39.4 content/impact and authorized containment remain outside this story; non-Azure and operating-period evidence remains incomplete |
| `SOC2-39.1-T02` | Ready For Review | Founder authority FR-01 | External security/legal/operations reviewers unassigned | 2026-09-25 (**overdue**) | P1 | Provider and contract inventory; qualified review | Description Criteria / system boundary | Both | `S39.1-E14` / `SOC2-BOUNDARY-DRAFT-0.1` reconciles services, flows, supporting systems, people, procedures, providers, responsibilities and exclusions. Protected legal/registrar evidence, asset crosswalk, running-release reconciliation, commitments and approval remain pending |
| `SOC2-39.1-T03` | Ready For Review | Founder authority FR-01 | Qualified CPA unassigned | 2026-09-25 (**overdue**) | P1 | T02 draft; commitment review | Security baseline; A/PI/C/P category decisions | Both | Security is provisionally in; Availability, Processing Integrity, Confidentiality and Privacy are separately deferred with explicit revisit gates; No-CUI/customer/MSP/C3PAO boundaries are preserved |
| `SOC2-39.1-T04` | Blocked | Founder authority FR-01 | Qualified CPA unassigned | 2026-10-02 | P1 | Authorized AICPA access; reviewer appointment | Every scoped criterion; Description Criteria | Both | Official current resource listings verified; login-gated licensed material and qualified review unavailable; provisional identifier matrix only |
| `SOC2-39.1-T05` | Ready For Review | Founder authority FR-01 | Independent evidence reviewer unassigned | 2026-09-30 | P1 | T04 review; protected evidence index | Security/common criteria | Both | Design, point-in-time implementation and operating evidence are separated; Azure freshness, source reliability, unavailable query areas, repository exposure and live-verification needs are recorded in `AZ-SOC2-INV-2026-09-19`; independent review and protected raw evidence remain unavailable |
| `SOC2-39.1-T06` | Blocked | Founder authority FR-01 | Qualified CPA and qualified Legal/Procurement reviewers unassigned | 2026-10-02 estimate passed; no replacement commitment invented | P1 | Customer trigger, renewed capacity, approved examination budget, protected appointment, T04/T05 | Examination decision gate | Both | Historical context: the expired initial commitment, $15/month core-storage planning ceiling and 2026-09-28 no-provisioning/no-spend direction are retained but superseded for bounded workspace setup. Current 2026-10-06 authority permits only a separate new FeDril LLC corporate target within $55/month recurring and $0 upfront; implementation and acceptance remain pending. Internal route, demand and budget planning may continue, but commercial demand, post-2026-10-30 capacity, overall examination funding, route approval and qualified reviewer evidence remain unavailable or unapproved, so the decision remains defer. |
| `SOC2-39.1-T07` | Blocked | Founder authority FR-01 | Qualified CPA unassigned | After 39.5-39.17 and before 39.18/39.20 | P1 | Accepted outputs from 39.5-39.17; significant-change log | Final scoped matrix and description | Both | Cannot execute now; requires later evidence and authorized reconciliation |

## Acceptance-Criteria Validation

| Test case / criterion | Result | Evidence-based conclusion |
| --- | --- | --- |
| `TC-39.1.1` Scope completeness | **Partial pass / setup only** | Candidate boundary, management sponsor, included/excluded services, responsibilities and explicit unknowns are documented. Protected approval, complete provider/contract inventory and qualified criteria review are unavailable. |
| `TC-39.1.2` Ownership and evidence map | **Partial pass / setup only** | Every provisional Security identifier has a matrix row and FR-01 owns the management decision. Independent reviewers, control operators and several evidence sources remain unassigned or unavailable. |
| `TC-39.1.3` Governed decision | **Pass for current defer decision; not final acceptance** | The record defines measurable Type I and Type II gates and conservatively defers commitment. Budget, verified commercial trigger, qualified approvers, route and final review date remain missing, so a proceed decision is prohibited. |
| `TC-39.1.4` Assurance boundary | **Partial pass** | The artifact explicitly prevents report/certification/legal/CUI claims and records missing qualified review/conflict assessment. Qualified input itself is unavailable. |
| Setup gate | **Met** | `SOC2-SCOPE-DRAFT-0.1`, `SOC2-BOUNDARY-DRAFT-0.1`, Founder authority FR-01, the provisional category decision, the criterion-indexed matrix and explicit unknowns satisfy the setup artifact gate. This does not satisfy qualified review or final approval. |
| Final acceptance | **Not met** | No authorized scope approval, qualified review, final licensed mapping, named ownership, or post-39.5-39.17 reconciliation exists. |

## Protected Workspace Handoff

Story 39.2 must establish or identify an approved protected workspace before raw evidence is collected. The sanitized Story 39.2 setup register now exists, but it does not establish protected storage. Create protected logical records named `scope-decision`, `system-description`, `control-matrix`, and `customer-demand-register`, then store only their opaque IDs, versions, dates, status, and non-sensitive summaries in Git.

Each protected evidence record must include: evidence ID, control, source, environment, date/period, complete population or sample basis, collector, reviewer, result, freshness/expiry, integrity/version reference, retention, access restriction, and protected location. Do not copy personnel records, access exports, contracts, customer documents, vulnerability details, reports, credentials, database contents, or sensitive system configuration into this repository.

## Authorized Framework Sources Located

- AICPA & CIMA, [2017 Trust Services Criteria with revised points of focus (2022)](https://www.aicpa-cima.com/resources/download/2017-trust-services-criteria-with-revised-points-of-focus-2022). The official page requires account access for the download; no login or licensed-copy review was available to this execution.
- AICPA & CIMA, [System and Organization Controls resource library](https://www.aicpa-cima.com/resources/landing/system-and-organization-controls-soc-suite-of-services), which lists the 2018 SOC 2 Description Criteria with revised implementation guidance (2022) and the current authoritative SOC 2 guide.

These links establish the publisher and currently listed editions only. They do not establish that the provisional matrix was checked against the complete authorized texts.

## Human And External Actions Required

1. Authorize and execute Story 39.4 triage for the public tracked database dump; do not inspect or redistribute it casually.
2. Preserve the supplied named sponsor/SOC 2 owner appointment in the protected workspace; appoint actual named control owners, evidence custodians, independent reviewers and qualified approvers; record conflicts and separation-of-duties limitations.
3. Execute Story 39.2 setup for the protected workspace/register/evidence calendar, including the business-controlled Azure boundary in `SOC2-AZURE-BUSINESS-TRANSITION-0.1`; do not transfer the production subscription through this setup action.
4. Obtain authorized current AICPA TSC and Description Criteria access and qualified CPA review without copying proprietary text into Git.
5. Supply contracts, SLAs, DPAs, privacy terms, customer/partner/procurement requests, vendor inventory, HR/workforce records, policies, access populations, and budget through protected channels.
6. Reconcile Stories 39.5 through 39.17, then run `SOC2-39.1-T07` before handing an approved description/mapping to Stories 39.18 and 39.20.

## Residual Risks And Dependencies

- The database dump may contain synthetic, sensitive, credential, customer, or other data; classification and access history are unknown.
- Public clones/forks may preserve historical artifacts after later remediation.
- Current production uses public API ingress and PostgreSQL public network access; necessity, firewall population, and compensating controls require Story 39.9 review.
- Main branch protection requires no approving review; production has one required reviewer, leaving founder/solo-control conflict risk.
- The assessed local commit was not validated by a current CI run in this execution; successful recent `main` runs do not test the dirty working copy or establish the deployed production revision.
- Terraform remote state/import/drift adoption remains incomplete, and the latest drift workflow was skipped.
- Seven-day, non-geo-redundant database backup and staging-only restore evidence do not prove approved production RPO/RTO or regional recovery.
- Vendor, workforce, privacy, retention, incident, vulnerability, training, and management evidence is unavailable.
- A qualified CPA may change the boundary, criterion mapping, category decisions, route, evidence expectations, and proposed dates.

## Next Unblocked Work

- **Immediate:** complete Story 39.4 authorized custodian inspection and obtain the Security/Legal containment decision; metadata triage is complete and uncommitted recurrence-control candidates are ready for review, but activation and the finding remain open.
- **Governance setup:** approve the Story 39.2 register, identify named owners, and implement/test only the bounded separate new FeDril LLC-controlled corporate target authorized on 2026-10-06 under `SOC2-AZURE-BUSINESS-TRANSITION-0.1`; do not transfer or attach Production. Story 39.3 claims/examination templates are ready for management self-review, while qualified and independent approvals remain deferred.
- **Story 39.1:** management review of T01-T03/T05 drafts; T04 and T06 remain blocked pending authorized criteria access, qualified review, commercial/budget evidence, and named approvals.

The next eligible numbered story under the normal sequence is Story 39.2 setup because the Story 39.1 setup gate is met. Story 39.4 triage remains an immediate exception, but this execution did not start or implement either story.

Coordinator handoff on 2026-09-28: PR #129 was reported to have failed its initial branch-push Sensitive artifact policy because a prohibited legacy database-backup artifact remains in reachable ancestor history; its PR-diff policy reportedly passed. This was not independently re-queried during Story 39.1 and is recorded as verification pending. The result activates the Story 39.4 immediate-triage exception. Story 39.1 stopped before any push, PR, merge, deployment, history change, artifact-content inspection, secret access, containment or notification action.

Do not mark Story 39.1 accepted because these drafts exist.
