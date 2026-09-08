# MVP Roadmap Story Traceability

This inventory maps each outcome in [mvp-roadmap.md](mvp-roadmap.md) to the canonical backlog in [development-phase-use-cases.md](development-phase-use-cases.md). Statuses are roadmap classifications, not proof of deployed behavior. `Implemented` and `Partially implemented` claims require current product and test evidence before use in customer-facing material.

## Phase 0 - Research And Validation

| Roadmap outcome | Canonical story | Status | Outcome type |
| --- | --- | --- | --- |
| Persona map | `0.1` Persona Map | Planned | Research deliverable |
| Regulatory obligation map | `0.2` Regulatory Obligation Map | Planned | Research/content-governance deliverable |
| Competitive matrix | `0.3` Competitive Matrix | Planned | Research deliverable |
| Clickable prototypes | `0.4` Clickable Prototype Validation | Planned | Research/design deliverable |
| 20-30 customer and expert interviews | `0.5` Customer And Expert Interviews | Planned | Research deliverable |
| Pricing hypothesis | `0.6` Pricing Hypothesis | Planned | Commercial hypothesis |
| MVP requirements | `0.7` MVP Requirements Baseline | Planned | Product decision artifact |
| Advisor review | `0.8` Advisor Review And Phase Gate | Planned | Human review/decision gate |

Stories `0.1` through `0.8` use governed human-evidence tests and are excluded from the software implementation sequence.

## Phase 1 - MVP

| Roadmap outcome | Canonical story coverage | Status | Notes |
| --- | --- | --- | --- |
| Tenant, user, and RBAC foundation | `2.1`-`2.4` | Planned | Server-side tenant and permission boundaries remain part of story verification. |
| React + Vite authenticated shell backed by ASP.NET Core API | `3.1`-`3.2` | Planned | UI and API behavior require separate evidence. |
| No-CUI data posture foundation | `4.1`-`4.2`, `1A.1.1`-`1A.9.3` | Planned | Real customer CUI remains outside the approved MVP posture. |
| Company profile | `7.1`-`7.3` | Planned | Certification tracking is recordkeeping, not certification. |
| Contract upload and manual clause tagging | `8.1`-`8.3`, `9.1`-`9.3` | Planned | Upload controls remain server-authoritative. |
| Obligation dashboard | `10.1`-`10.3` | Planned | Tenant isolation and source traceability apply. |
| Compliance calendar | `11.1`-`11.3` | Planned | Includes tasks, calendar, and renewals. |
| Evidence vault | `12.1`-`12.3` | Planned | Evidence approval does not establish regulatory approval. |
| Basic CMMC Level 1 and Level 2 readiness tracking | `13.1`-`13.4` | Planned | Excludes SSP generation and SPRS scoring from Phase 1. |
| Subcontractor flow-down tracker | `14.1`-`14.3` | Planned | Includes subcontractor records, clauses, and evidence requests. |
| Reports | `15.1`-`15.5` | Planned | Reports are immutable snapshots and readiness outputs, not certifications. |
| Notifications | `16.1`-`16.3` | Planned | Notification side effects require retry/idempotency controls when implemented. |
| Audit log | `5.1`-`5.2` | Planned | Append-only and tenant-scoped. |
| Source-backed obligation library | `6.1`-`6.3`, `9.1` | Planned | Requires provenance and qualified review before production publication. |
| MVP hardening and release readiness | `17.1`-`17.4`; `PR-0.1`-`PR-8.3` | Planned | Development hardening and production-readiness gates remain separate backlogs. |

## Phase 1A - CUI Readiness Gate

| Roadmap outcome | Canonical story coverage | Status |
| --- | --- | --- |
| Tenant data handling modes | `1A.1.1`-`1A.1.2` | Planned |
| Data classification controls | `1A.2.1`-`1A.2.2` | Planned |
| Synthetic CUI demo dataset | `1A.3.1`-`1A.3.2` | Planned |
| Future `CuiReady` approval checklist | `1A.4.1`-`1A.4.2` | Planned |
| Shared responsibility matrix baseline | `1A.5.1`-`1A.5.2` | Planned |
| Customer-facing data handling notice | `1A.6.1`-`1A.6.2` | Planned |
| Support escalation path | `1A.7.1`-`1A.7.2` | Planned |
| CUI audit events | `1A.8.1`-`1A.8.2` | Planned |
| Security review | `1A.9.1`-`1A.9.3` | Planned |

Phase 1A is a readiness track inside Phase 1. Its existence does not authorize real customer CUI processing.

## Phase 2 - Govcon Intelligence

| Roadmap outcome | Canonical story coverage | Status |
| --- | --- | --- |
| Automated clause extraction | `18.1`-`18.3` | Planned |
| Human review of extracted clauses and AI suggestions | `19.1`-`19.3` | Planned |
| Clause library | `20.1`-`20.3` | Planned |
| Applicability engine | `21.1`-`21.3` | Planned |
| SAM.gov entity lookup | `22.1`-`22.3` | Planned |
| SBA size helper | `23.1`-`23.3` | Planned |
| Subcontractor tracker | `24.1`-`24.3` | Planned |
| Policy templates | `25.1`-`25.3` | Planned |
| Evidence request workflows | `26.1`-`26.3` | Planned |
| CMMC Level 2 readiness | `27.1`-`27.4` | Planned |
| Extraction precision/recall test set | `28.1`-`28.3` | Planned |
| Extraction/AI data-handling boundary | `18.1`, `18.3`, `19.1`-`19.3`, `1A.1.2`, `1A.2.1`-`1A.2.2` | Planned |

## Phase 3 - Advanced Compliance

| Roadmap outcome | Canonical story coverage | Status | Guardrail |
| --- | --- | --- | --- |
| SSP builder | `29.1`-`29.3` | Planned | Draft/review workflow; not certification. |
| SPRS score calculator | `30.1`-`30.3` | Planned | Readiness calculation; not a submission or government record. |
| eSRS support | `31.1`-`31.3` | Planned | Support/package workflow; does not claim agency submission. |
| Labor compliance module | `32.1`-`32.3` | Planned | Demand-gated; no legal or labor determination. |
| AI assistant | `33.1`-`33.3` | Planned | Citations, logging, draft-only output, and human review required. |
| Prime contractor and auditor portals | `34.1`-`34.3` | Planned | Explicitly shared approved snapshots only. |

The executable Phase 3 range is `29.1` through `34.3`.

## Phase 4 - Enterprise / Regulated Deployment

| Roadmap outcome | Canonical story coverage | Status | Guardrail |
| --- | --- | --- | --- |
| SSO/SAML and SCIM | `35.1`-`35.3` | Planned | Enterprise identity lifecycle controls. |
| GovCloud or government cloud path | `36.1`-`36.3` | Planned | Hosting region/provider status does not establish FeDril authorization. |
| FedRAMP readiness package | `37.1`-`37.3` | Planned | Readiness artifacts only; no authorization claim. |
| Higher-assurance CUI enclave and customer-managed keys | `38.1`-`38.3` | Planned | Future gated deployment; not part of the No-CUI MVP. |
| SOC 2 assurance program | `39.1`-`39.3` | Planned | Governance and independent assurance, not software implementation. |

The executable Phase 4 software range is `35.1` through `38.3`. Stories `39.1` through `39.3` require governed human evidence and are excluded from `gccs-story-sequence`.

## FedRAMP Decision And Readiness Track

| Roadmap ID | Canonical story or disposition | Roadmap status | Classification |
| --- | --- | --- | --- |
| `FR-0` | External governance record; cross-reference `0.7`, `0.8`, `37.1` | Planned | Product/program decision gate |
| `FR-1` | `37.1`, `37.3`; positive authorization remains externally governed | Partially implemented | Governance and claims-control gap |
| `FR-2` | `29.1`-`29.3`, `37.1`-`37.3`; trust/SSP persistence gap remains governed by roadmap evidence | Partially implemented | Product foundation plus persistence evidence |
| `FR-3` | `17.2`, `1A.8.1`-`1A.9.3`, `35.1`-`35.3`, `36.3` | Planned | Cross-phase security/operations foundation |
| `FR-4` | `36.1`, `38.1`, `1A.5.1` | Planned | Prospective boundary and shared-responsibility definition |
| `FR-5` | External activation, advisor/counsel, and independent-assessor gate; cross-reference `0.8`, `37.1` | Planned | External program decision |
| `FR-6` | `36.1`-`36.3`; infrastructure adoption evidence remains outside story completion | Partially implemented foundation | Infrastructure/operations foundation |
| `FR-7` | External gap assessment and operating-evidence program; cross-reference `1A.9.1`-`1A.9.3`, `37.1` | Planned | Human/independent readiness gate |
| `FR-8` | `37.1`-`37.3`, `1A.5.1`, `36.1` | Planned | Governed package production |
| `FR-9` | External Marketplace, independent-assessment, Program, and/or agency process | Planned | Independent/external authorization gate |
| `FR-10` | External continuous-monitoring operating program; cross-reference `36.3`, `37.1`-`37.3` | Planned | Ongoing operations and assessment |

`FR-5`, `FR-7`, `FR-9`, and the independent portions of `FR-10` cannot be satisfied by application code alone. Their acceptance evidence must identify qualified reviewers, the applicable current program rules, the assessed boundary, dates or periods, official status evidence, and unresolved exceptions.

## Artifact Synchronization Rules

- Canonical story text, tasks, and acceptance criteria live in [development-phase-use-cases.md](development-phase-use-cases.md).
- Software implementation prompts live in [development-story-prompts.md](development-story-prompts.md).
- Test contracts live in [development-story-test-cases.md](development-story-test-cases.md).
- Human-evidence and test execution prompts live in [development-story-test-prompts.md](development-story-test-prompts.md).
- Software smoke and automated prompts intentionally exclude Phase 0 and Story 39.
- Production readiness retains independent `PR-*` identifiers in [production-readiness-phase-use-cases.md](production-readiness-phase-use-cases.md).
- The story sequence implements one software story at a time and does not execute external research or assurance outcomes.
