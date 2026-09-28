# Story 39.1: Define SOC 2 Scope And Readiness Decision

Status: **In progress — setup gate met; final acceptance blocked**
Scope version: `SOC2-SCOPE-DRAFT-0.2`
Record updated: 2026-09-28 (America/New_York)
Evidence cutoff: 2026-09-28
Repository baseline: `9dbd825b26f12c7b3dde635734bca23e4da15593`
Authoritative source: local current `docs/development-story-prompts.md`, SHA-256 `72129c1acd3825ae71d85c6879cf2b57761bcb9802bc8a140980cb9926559248`
Reviewer role: internal senior systems architecture and evidence review; not an independent CPA, attorney, or management approver

Implementation branch: `codex/soc2-39-1-scope-readiness`
Initial record commit: `890597941d6cc3edd3ea56dc933f36553326247f`
Pull request: `#130`

This public record is a sanitized coordination artifact. It is not a protected evidence workspace, management assertion, legal conclusion, examination, issued report, certification, general compliance claim, audit-readiness claim, or authorization to process CUI.

## Critique And Failure Modes

- Treating the September 19 draft as current would rely on a different repository revision and point-in-time observations that have not been re-performed for every live system.
- Publishing detailed infrastructure, business-ownership, incident, or evidence-custody facts in Git would weaken the protected-evidence boundary and could create a second uncontrolled evidence store.
- Treating generic criterion identifiers or repository controls as a complete mapping would bypass authorized current AICPA criteria, qualified review, actual control ownership, and operating-period evidence.
- Equating a draft, merge, configured control, or deployment with acceptance would collapse design, implementation, operating evidence, governance approval, staging, and production into a false readiness conclusion.

The required pattern is a small sanitized index in Git, protected source evidence elsewhere, opaque accountable roles, a versioned draft matrix with explicit unknowns, and separate decisions for setup, final scope approval, examination commitment, and operating evidence.

## Current Decision

**Defer any examination commitment, Type II observation-period start, and customer-facing readiness claim.**

The Story 39.1 setup gate is met because a candidate boundary, an opaque accountable management sponsor (`Founder authority FR-01`), a provisional Security/common-criteria decision, a versioned draft matrix, and explicit unanswered facts exist. This setup result permits the next gated setup or urgent triage work; it does not accept Story 39.1.

Final acceptance is blocked because authorized current AICPA criteria and Description Criteria were not reviewed, criterion-complete ownership and evidence mapping is unavailable, qualified CPA review is absent, protected appointment/evidence preservation is unavailable, commercial demand and budget are unverified, and the required reconciliation after Stories 39.5–39.17 has not occurred.

## Assessment Metadata And Sources

| Evidence ID | Source | Date / period | Conclusion | Limitation |
| --- | --- | --- | --- | --- |
| `SOC2-39.1-E01` | Current authoritative Story 39 contract and companion prompt | 2026-09-28 | Governing order, gates, required tasks, and claim boundaries reconciled | Working-copy source is uncommitted and must be preserved by its owner |
| `SOC2-39.1-E02` | `AGENTS.md`, `README.md`, architecture and security-control documentation | Repository baseline above | No-CUI posture, Clean Architecture, tenant/RBAC/audit design obligations identified | Documentation is not live operation evidence |
| `SOC2-39.1-E03` | Current source and focused test inventory | Repository baseline above | Server-side tenant, permission, No-CUI, audit, release, and evidence controls have implementation/test sources | Tests were not rerun for this documentation-only execution; inventory is not complete operating evidence |
| `SOC2-39.1-E04` | Current release manifest and deployment workflow definitions | 2026-09-28 | Staging is triggered by `main` push or dispatch; production promotion is manual and manifest/tag governed; legacy workflow is rollback-only | Workflow design and metadata do not prove a successful Story 39.1 deployment |
| `SOC2-39.1-E05` | Read-only GitHub capability and protection metadata | 2026-09-28 | Repository and workflow metadata were accessible; main has required checks but no approving review requirement; environment gates differ | Point-in-time metadata only; access population, bypass history, MFA, and recurring operation were not reviewed |
| `SOC2-39.1-E06` | Read-only Azure session capability check | 2026-09-28 | An authenticated session was available | No resource identifiers, configuration exports, secrets, customer content, or live control conclusions were collected |
| `SOC2-39.1-E07` | Prior local Story 39.1 draft and supporting sanitized records | 2026-09-19 through 2026-09-20 | Reusable candidate scope, boundary, defer logic, and opaque management-role input exist | Records were untracked, stale relative to the current baseline, and not qualified or independently approved |
| `SOC2-39.1-E08` | Product strategy, roadmap, and repository search | Through 2026-09-28 | No verified customer, partner, contractual, or procurement requirement was available in repository evidence | CRM, contracts, questionnaires, partner records, and customer identities were unavailable |

Unavailable evidence is `unverified`; it is not proof that a control or commitment is absent.

## Candidate Scope

The detailed sanitized candidate boundary is [`SOC2-SCOPE-DRAFT-0.2`](system-boundary-index.md). It includes the production SaaS, supporting source control and delivery systems, relevant application and data services, production-affecting environments, workforce and service identities, operational procedures, material providers, and customer responsibilities. Supporting systems remain in scope when they can affect production code, access, data, evidence, releases, or recovery even when they are not customer-facing.

The boundary preserves FeDril's No-CUI posture. Customer-managed environments, future regulated enclaves, independent assessor work, government systems, and professional legal/accounting/assurance conclusions are outside the delivered service, while FeDril's interfaces, dependencies, commitments, and shared responsibilities remain subject to review.

## Trust Services Category Decision

| Category | Draft decision | Evidence-based reason | Approval state / revisit gate |
| --- | --- | --- | --- |
| Security / common criteria | Provisional candidate baseline | The SaaS and its supporting people, processes, technology, and providers require a security control baseline | Not approved; requires authorized criteria and qualified CPA review |
| Availability | Deferred for separate decision | No verified contractual availability commitment, approved recovery objectives, or operating recovery evidence was available | Revisit with Story 39.14, contracts, customer demand, and qualified review |
| Confidentiality | Deferred for separate decision | No-CUI and technical protections do not establish the contractual classification, retention, disclosure, and key-governance scope | Revisit with Stories 39.10, 39.15, 39.16, commitments, and qualified review |
| Processing Integrity | Deferred for separate decision | Product workflows do not establish an approved processing-integrity commitment or complete input-processing-output objective set | Revisit in Story 39.17 |
| Privacy | Deferred for separate decision | Personal-data roles, notices, inventory, rights handling, retention, and qualified privacy review were unavailable | Revisit in Story 39.17 |

Deferral does not mean a category is inapplicable. It blocks a final exclusion decision until commitments, risks, and qualified review are recorded.

## Required Deliverables

| Deliverable | Public sanitized record | Current state | Protected or external dependency |
| --- | --- | --- | --- |
| Scope decision | This file | Draft defer decision; setup gate met | Management approval, qualified review, protected appointment and decision evidence |
| System description | `system-boundary-index.md` | Versioned candidate boundary ready for review | Detailed protected description, live configuration, contracts, provider and legal review |
| Control matrix | `story-39.1-common-criteria-matrix.csv` | Provisional domain matrix; design/implementation/operation separated | Authorized current criteria, criterion-complete mapping, owners, evidence, qualified review |
| Customer-demand register | `story-39.1-customer-demand-register.csv` | No verified demand in available repository sources | Protected CRM, contracts, questionnaires, procurement and partner records |

## Task Register

| Task | Status | Accountable owner | Reviewer / approver | Priority | Evidence and disposition |
| --- | --- | --- | --- | --- | --- |
| `SOC2-39.1-T01` | In Progress | Founder authority FR-01 | Independent evidence reviewer unassigned | P0 | Historical draft reconciled to current source, repository, workflow, release, and capability metadata; complete live and governance evidence remains unavailable |
| `SOC2-39.1-T02` | Ready For Review | Founder authority FR-01 | Security, legal/contract, and operations reviewers unassigned | P1 | Candidate boundary is versioned and explicit about services, environments, people, procedures, providers, responsibilities, and exclusions |
| `SOC2-39.1-T03` | Ready For Review | Founder authority FR-01 | Qualified CPA and privacy/legal reviewers unassigned | P1 | Security is provisional; four additional categories are separately deferred with evidence gates; No-CUI boundary preserved |
| `SOC2-39.1-T04` | Blocked | Founder authority FR-01 | Qualified CPA unassigned | P1 | Authorized current criteria/Description Criteria and qualified review unavailable; no criterion-complete claim permitted |
| `SOC2-39.1-T05` | Ready For Review | Founder authority FR-01 | Independent evidence reviewer unassigned | P1 | Matrix separates design, implementation, and operating evidence and records source limits and freshness |
| `SOC2-39.1-T06` | Blocked | Founder authority FR-01 | Qualified CPA plus contract/procurement and budget reviewers unassigned | P1 | Objective defer gates exist; verified demand, approved budget/capacity, route, qualified input, conflicts, and approved review date remain incomplete |
| `SOC2-39.1-T07` | Blocked—dependency | Founder authority FR-01 | Qualified CPA unassigned | P1 | Requires accepted or approved-N/A outputs from Stories 39.5–39.17 and significant-change reconciliation before 39.18/39.20 handoff |

Proposed next management review: 2026-10-02. This is a planning date, not an approved commitment.

## Proceed / Defer Gates

### Type I or direct-Type-II readiness consultation

Remain in remediation until all of the following are evidenced and approved:

1. Urgent repository-exposure triage has a governed disposition and residual risk.
2. Protected evidence storage and opaque public indexing are approved and tested.
3. Named management, control, evidence, review, and approval roles are preserved with conflicts and compensating review.
4. Candidate boundary, system description, criteria/category decisions, subservice treatment, and customer responsibilities receive authorized qualified review.
5. Every scoped criterion has a risk, control or design gap, accountable owner, evidence source, frequency, reviewer, and disposition.
6. Material design and implementation gaps have approved remediation or explicit time-bound risk decisions.
7. Live production-affecting configuration and access evidence is reconciled to approved baselines without copying protected details into Git.
8. Verified commercial demand, capacity, budget, route, and consultation/examination trigger are approved.
9. Story 39.18 records the later integrated decision; internal work cannot predict an auditor's conclusion.

### Type II period entry

Do not start an observation period until Story 39.20 defines the agreed route, period, populations, frequencies, evidence rules, failure handling, retention, significant-change handling, and reviewer expectations with management and the selected service auditor. No universal period length is asserted.

## Acceptance Criteria

| Criterion | Result | Evidence-based disposition |
| --- | --- | --- |
| `TC-39.1.1` Scope completeness | Draft pass / final approval blocked | Version, owner, proposed review date, boundary, services, environments, providers, responsibilities, exclusions, and provisional categories are present; qualified approval and protected detail are unavailable |
| `TC-39.1.2` Ownership and evidence map | Fail / incomplete | Domain rows identify accountable management and evidence classes, but authorized criterion-complete mapping, control owners, custodians, reviewers, and protected evidence are incomplete |
| `TC-39.1.3` Governed decision | Partial | Defer decision, rationale, assumptions, unresolved risks, opaque management owner, and proposed review date are recorded; verified demand, approved full budget/capacity, route, and qualified approval are unavailable |
| `TC-39.1.4` Assurance boundary | Partial | Prohibited claims and conflicts are explicit; qualified reviewer input is absent |
| Setup gate | **Met** | Candidate boundary, accountable sponsor, provisional criteria decision, versioned draft matrix, and explicit unanswered facts exist |
| Final acceptance | **Not met** | Authorized scope approval, qualified review, complete scoped mapping, later-story reconciliation, and measurable approved gates are not all evidenced |

## Deployment And Release Disposition

Story 39.1 changes governance documentation only. Staging and production deployment are **N/A** because no application artifact, configuration, account, infrastructure, or runtime behavior changed. Any publishing/merge workflow still requires its normal checks and review. A merge does not accept the scope or prove a control.

## Blocker And Exact Next Action

Final Story 39.1 acceptance is blocked by human/qualified review and external protected evidence. The minimum exact Story 39.1 action is: management must preserve the opaque sponsor appointment and decision in an approved protected workspace, appoint a qualified independent CPA reviewer, obtain authorized current criteria/Description Criteria access, and return a reviewed scope/category/matrix disposition without copying proprietary criteria or protected evidence into Git.

Under the authoritative exception, **Story 39.4: Investigate Repository Exposure And Govern Containment** is the next story with urgency precedence because plausible exposure remains unresolved. Story 39.2 provisional setup is also dependency-eligible from this candidate scope, but it does not override the 39.4 immediate-triage gate. No Story 39.4 action is taken by this record.
