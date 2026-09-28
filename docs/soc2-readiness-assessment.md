# FeDril SOC 2 readiness assessment — coordination baseline

Assessment date: 2026-09-28. Internal, sanitized, preliminary. This record is a checkpoint for Story 39.1, not an approved scope, management assertion, auditor conclusion, SOC 2 report, or customer-facing claim. The current working copy of `docs/development-story-prompts.md` has SHA-256 `72129c1acd3825ae71d85c6879cf2b57761bcb9802bc8a140980cb9926559248` and uncommitted SOC 2 edits; reconcile it with the committed source before accepting this assessment. Evidence classifications below are bounded by the listed inspection, not by what may exist in restricted systems.

## 1. Executive readiness assessment

**Decision: Defer a Type I readiness declaration.** Story 39.1's local scope record is explicitly draft/defer and its final approval path has not been verified in this session. The local Story 39.2 setup record and evidence inventory are preliminary and untracked. No protected operating-evidence workspace, complete control ownership, qualified review, independent examination engagement, or sufficient operating history was verified. No SOC 2 assurance claim follows from current implementation or deployment. The original checkout contains 31 dirty paths, including uncommitted authoritative story changes; those must be reconciled before publication or release.

Immediate sequence: reconcile 39.1 candidate boundary and management decision; address 39.4 exposure triage under its immediate exception; finish 39.2 provisional register/evidence setup; then progress only through the document's stage gates. The [execution ledger](soc2/soc2-execution-ledger.md) is the current sequence checkpoint.

## 2. Current FeDril control inventory

| Control area | Observed source and collection date | Classification | Remaining proof |
| --- | --- | --- | --- |
| Product boundary and No-CUI posture | `AGENTS.md`; `apps/api/Program.cs` No-CUI response and acknowledgement paths; `.github/workflows/staging.yml` and `production-release.yml` environment posture, inspected 2026-09-28 | Partially verified | End-to-end rejection, authorization, and production configuration evidence; do not infer comprehensive CUI prevention from strings/configuration |
| Tenant isolation, RBAC, evidence and audit | Repository architecture and preliminary local `docs/soc2/soc2-readiness-reference.md`, inspected 2026-09-28 | Partially verified | Affected endpoint inventory, allowed/denied and cross-tenant tests, audit atomicity and actual operating evidence by story |
| Change and release control | `.github/workflows/ci.yml`, `staging.yml`, `production-release.yml`; GitHub environment metadata and recent workflow outcomes, inspected 2026-09-28 | Partially verified | Live protection settings, approvals, successful run revisions, artifact relationship, and separation of duties |
| Azure infrastructure, monitoring, backup and recovery | Azure CLI enabled session and preliminary local SOC 2 reference, inspected 2026-09-28 | Unverified for control operation | Sanitized configuration, run history, access review, alert response and actual restoration evidence in approved restricted workspace |
| Governance, workforce, vendor, policy, incident and recurring controls | Local provisional SOC 2 records and authoritative stories, inspected 2026-09-28 | Unverified for approval and operation | Named accountable owners, approved policies, performed reviews/exercises, vendor evidence, exceptions, schedules and dated execution |
| Examination and report distribution | Local Story 39.3 setup draft, inspected 2026-09-28 | Unverified | Qualified firm engagement, accepted report, authorized recipient/disclosure process, and report custody; do not claim an examination |

Existing local `docs/soc2/story-39.1-common-criteria-matrix.csv` and `docs/soc2/soc2-evidence-inventory.csv` are more detailed candidate inventories. They are untracked in the original checkout and are not incorporated as accepted evidence here. The tracked `docs/soc2/system-boundary-index.md` is the current version-controlled boundary index. Preserve source dates and access limitations when reviewing each record.

## 3. Prioritized SOC 2 readiness checklist

| Priority | Required decision or work | Current classification | Acceptance evidence |
| --- | --- | --- | --- |
| Immediate | Reconcile Story 39.1 scope, sponsor, criteria, matrix and unknowns; obtain qualified review and authorized approval | Partially verified | Versioned approved candidate/decision record and criterion map |
| Immediate | Triage Story 39.4 potential repository exposure; contain only with authorization | Partially verified | Sanitized investigation, authorized action and preserved incident trail |
| High | Confirm protected evidence workspace, register, retention, ownership and recurring calendar in Story 39.2 | Partially verified | Approved workspace/configuration and operating-quality sample with reviewer |
| High | Finish governance and technical Stories 39.5–39.17 in documented order | Unverified | Criterion-level implementation, negative/security tests, owners and actual operating evidence |
| Gate | Conduct integrated Story 39.18 assessment with qualified reviewers | Blocked—dependency | Accepted 39.4–39.17 or approved N/A, plus current 39.1–39.3 |
| Later | Decide optional Type I disposition, then Type II entry and observation | Blocked—dependency | Recorded 39.19 disposition, approved 39.20 entry, defined period/populations, control operation |
| Later | Examination, report acceptance, disclosure and renewal | Blocked—dependency | Independent action and accepted restricted evidence, never inferred from code |

## 4. Type I readiness requirements

The candidate system boundary, services, subservice/inherited controls, Trust Services Criteria selection, complementary user entity controls, control owner, control design, and evidence mapping need a versioned approved decision. Each in-scope criterion requires a reviewable control description, implementation proof, gap/exception disposition, and qualified review. Technical controls require relevant production configuration and negative-path verification; organizational controls require actual approval and execution records. Story 39.18 is the integrated decision gate. An optional Type I examination under Story 39.19 requires an independent qualified firm and report acceptance; neither is established by this assessment.

## Evidence inventory and recurring-control calendar

The local provisional `docs/soc2/soc2-evidence-inventory.csv` and `docs/soc2/story-39.1-common-criteria-matrix.csv` identify candidate mappings; neither is an approved restricted evidence store. Source for this checkpoint: repository `AGENTS.md`, the story document, `apps/api/Program.cs`, three deployment/CI workflows, GitHub environment names and recent workflow outcomes, and the local provisional SOC 2 records, collected 2026-09-28. GitHub CLI authentication and Azure CLI session availability were checked as capability metadata only. Customer content, secret values, resource identifiers, and production control histories were not collected. Classification: source code and YAML **verified as present**; application behavior and operational effectiveness **partially verified or unverified** as specified above.

Use the authoritative recurring-control calendar starter and the relevant story records as the schedule of record. Control frequency, accountable owner, evidence location, reviewer, last performed date, and next due date are **unassigned or unverified** until approved; do not compute invented due dates or call automation operating on configuration alone.

## Outstanding decisions and blockers

1. Authorized management approval of scope, accountable sponsor, criteria choices, and qualified review is unverified. The local Story 39.1 record is draft/defer.
2. Protected evidence workspace, retention and access model, and accepted evidence inventory are unverified. The local Story 39.2 record is preliminary.
3. The current uncommitted story changes and untracked SOC 2 records need source/release reconciliation and sanitization before use as controlled artifacts.
4. External examination, report acceptance, and observation periods have no verified completion evidence. Later-stage stories remain gated.

## Pre-publication check

Before adapting any part of this internal assessment for sales, demo, workflow, or compliance content, verify the current UI flow, the enforcing API/domain service, an applicable test, authorization and tenant scope, and No-CUI posture. Label each material claim `Implemented`, `Partially implemented`, `Planned`, or `Do not claim`. Avoid enforcement language or certification, approval, legal, audit-readiness, or CUI-storage claims without direct implementation evidence and qualified review.
