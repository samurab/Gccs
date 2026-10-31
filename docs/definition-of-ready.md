# Definition Of Ready

A development story is ready for implementation when it has enough product, compliance, design, data, security, dependency, and test context for an engineer to start without guessing about scope or release controls.

## Story-Level Checklist

Each ready story must have:

- A clear user story with actor, goal, and business value.
- A bounded scope with explicit included and excluded behavior.
- Testable acceptance criteria that identify actor/system, action or input, observable result, and applicable invariant.
- Matching `TC-*` cases in `docs/development-story-test-cases.md`.
- Known affected modules, files, APIs, data models, or UI surfaces.
- Required data fields and source systems identified or linked through `docs/data-requirements-and-source-systems.md`.
- Dependencies identified or linked through `docs/dependency-register.md`.
- Tenant isolation, RBAC, audit logging, and CUI/data-handling implications considered through `docs/security-control-implications.md`.
- Compliance content items marked for whether expert review is required when the story touches obligation, clause, regulatory, CMMC, SBA, FAR, DFARS, labor, reporting, or AI-generated guidance.
- CUI/data-handling impact stated for upload, import, paste, extraction, evidence, search, AI, report, or export workflows.
- Open questions, assumptions, and deferred items called out.

## Ready With Constraints

A story may be accepted as ready with constraints when:

- The missing item is explicitly deferred.
- The deferral does not affect tenant isolation, RBAC, audit logging, CUI/data-handling controls, or customer-facing compliance claims.
- The deferred work has a named follow-up story, risk note, or acceptance limitation.

## Not Ready

A story is not ready when:

- Acceptance criteria are subjective or cannot map to a focused `TC-*` case.
- Required fields, source systems, or dependencies are unknown.
- The story could change stored/processed customer data without CUI/data-handling and security impact review.
- The story introduces or changes customer-facing compliance content without source metadata, review state, and expert-review-required status.
- Cross-tenant, RBAC, audit, upload, report/export, AI, or search behavior is affected but not testable.

## Current Backlog Readiness Assessment

The Phase 1 development stories in `docs/development-phase-use-cases.md` are generally ready for implementation because they include user stories, tasks, acceptance criteria, and mapped `TC-*` regression cases.

Current readiness status: **Ready with constraints**.

Constraints:

- Some stories are already marked done and should be treated as implementation-complete unless regression gaps are discovered.
- External integrations such as SAM.gov/GSA lookup, SBA size automation, wage determination lookup, SPRS/CMMC status import, AI/RAG, search indexing, production object storage, production malware scanning, SSO/SAML, and GovCloud remain deferred unless a story explicitly activates them.
- Compliance content stories are ready only if each content record preserves source metadata, review state, review owner, confidence, and whether expert review is required.
- Any future story that expands data storage, upload, extraction, report export, search, or AI processing must be rechecked against the No-CUI production posture with synthetic CUI-ready demonstration workflows before implementation.

## Verification

Before starting a story:

1. Locate the story in `docs/development-phase-use-cases.md`.
2. Locate the matching tests in `docs/development-story-test-cases.md`.
3. Confirm source/data needs in `docs/data-requirements-and-source-systems.md`.
4. Confirm dependency impact in `docs/dependency-register.md`.
5. Confirm control implications in `docs/security-control-implications.md`.
6. If the story touches compliance content, confirm expert-review-required status is represented and tested.
