# MVP Roadmap Story Authoring And Synchronization Prompt

Use this prompt when `docs/mvp-roadmap.md` changes and the Agile/Scrum backlog and its derived execution artifacts must be reconciled.

```text
Act as a senior systems architect, Scrum backlog editor, test architect, and evidence-driven compliance reviewer for FeDril.

Objective

Review `/Users/devups/Development/CodexProjects/Gccs/docs/mvp-roadmap.md` phase by phase and synchronize the repository's Agile/Scrum documentation. For every roadmap outcome, map it to an existing story, refine an incomplete story, create the smallest missing story, or classify it as a non-software research, operational, decision, or independent-assurance gate. Update all affected story, implementation-prompt, test, traceability, and sequence documents without implementing application code.

Scope boundary

- This is a documentation-authoring and consistency task. Do not implement stories, change application code, run broad application builds, mark stories `Done`, commit, push, deploy, or alter external systems unless separately authorized.
- Read application code, UI routes, API behavior, authorization policies, migrations, and focused tests only as needed to classify current implementation status and substantiate present-tense claims.
- Preserve unrelated working-tree changes. Do not overwrite, normalize, or reformat unrelated content.
- Treat `/Users/devups/Development/CodexProjects/Gccs/docs/development-phase-use-cases.md` as the canonical development backlog. Derived prompt and test files must mirror its story IDs, titles, intent, and acceptance criteria; they must not become competing sources of truth.

Required references

Read these before editing:

- `/Users/devups/Development/CodexProjects/Gccs/AGENTS.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/mvp-roadmap.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/development-phase-use-cases.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/development-story-prompts.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/development-story-test-cases.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/development-story-test-prompts.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/Smoke_Test_development-story-test-prompts.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/Automated development-story-test-prompts.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/story_level_regression-test-execution-prompts.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/production-readiness-phase-use-cases.md`
- `/Users/devups/.codex/skills/gccs-story-sequence/SKILL.md`

Discover other affected documentation with `rg` before editing. Inspect only files that reference the roadmap phase, capability, story ID, story title, delivery range, or execution order being changed. Likely candidates include README, architecture, product strategy, execution plans, indexes, production-readiness prompts, and traceability documents, but do not change a file merely because it is listed as a possibility.

Authority and conflict rules

Apply this precedence when sources disagree:

1. `AGENTS.md` product, architecture, security, compliance, and verification invariants.
2. Proven current UI, API/domain enforcement, authorization behavior, persistence, and tests for implementation-status claims.
3. `mvp-roadmap.md` for approved roadmap outcomes and phase placement.
4. `development-phase-use-cases.md` for canonical story structure and identifiers.
5. Derived implementation, smoke, automated-test, regression, and sequence artifacts.

Do not silently resolve a genuine product-scope, compatibility, or numbering conflict. Document the conflict and use the smallest non-breaking interpretation. Stop before editing if resolution would require renumbering established stories, changing a public/internal contract, or changing the approved No-CUI posture.

Required workflow

1. Inspect repository state.
   - Run `git status --short` and identify pre-existing changes.
   - Record which files are safe to edit for this task.
   - Do not revert or absorb unrelated changes.

2. Inventory the complete roadmap before editing.
   - Cover Phase 0, Phase 1, the Phase 1A readiness track, Phase 2, Phase 3, Phase 4, the FedRAMP decision/readiness track, and any newly added phase or cross-phase item.
   - Build a working matrix with: roadmap phase/item, outcome type, existing story ID(s), canonical title(s), current roadmap status, evidence inspected, gap, and proposed action.
   - Classify each outcome as one of: product feature; research/discovery deliverable; operational/readiness control; product or architecture decision; independent assessment/certification; or external customer/agency/marketplace gate.
   - Detect duplicate coverage, orphan stories, missing roadmap items, title drift, ID collisions, incorrect phase placement, and inconsistent execution ranges.

3. Prove status before changing wording.
   - `Implemented`: use only when the material behavior exists in the current product and focused tests or equivalent direct evidence prove it.
   - `Partially implemented`: identify the implemented evidence and the specific remaining gap.
   - `Planned`: use when the outcome is approved backlog work but implementation is not proven.
   - `Do not claim`: use for prohibited, unsupported, expired, or independently controlled assertions.
   - A documentation entry, mock, configuration flag, infrastructure provider capability, or readiness workflow is not proof that the deployed service operates a control or holds an authorization.
   - Never infer status from an existing story, prompt, heading, `Done` marker, UI warning, or roadmap statement alone.

4. Reuse and allocate story IDs safely.
   - Reuse the existing canonical story whenever it materially covers the roadmap outcome; refine it instead of duplicating it.
   - Preserve every established story ID and title unless an explicit migration is approved.
   - Derive the next available ID from the current canonical backlog only after checking every downstream artifact. Do not assume the skill's documented range is current.
   - Do not force non-software external outcomes into the executable development sequence. Use a clearly identified research, governance, readiness, or external-gate story family when that matches the existing repository convention.
   - Keep production-readiness `PR-*` stories separate from development stories unless the roadmap outcome truly requires both, in which case cross-reference rather than duplicate acceptance criteria.

5. Author or refine canonical stories in the established format.
   Use this exact shape unless the surrounding section has a stricter compatible convention:

   #### Story <ID>: <Title>

   **Roadmap status: Implemented | Partially implemented | Planned | Do not claim.**

   As a <specific actor>, I want <bounded capability or deliverable>, so that <observable outcome>.

   Tasks:

   - <concrete task>

   Acceptance criteria:

   - <testable criterion>

   Story-writing rules:
   - Create vertical, independently reviewable slices. Split a roadmap item when one story would combine unrelated actors, workflows, data boundaries, or external approvals.
   - Every story must state actor, capability, and business outcome; tasks must describe work, not repeat the story sentence.
   - Every acceptance criterion must identify the actor or system, action/state/input, observable result or durable artifact, and applicable invariant.
   - Avoid subjective terms such as `easy`, `robust`, `appropriate`, `complete`, or `ready` unless paired with a measurable check, evidence artifact, named reviewer, or gate decision.
   - Include loading, empty, success, validation/error, and authorization-denied states for affected UI workflows.
   - Keep architecture explicit: thin API endpoints; application-layer orchestration; framework-independent domain rules; infrastructure persistence/integrations; server-authoritative permissions; UI presentation and fail-closed states.

6. Add risk-proportionate acceptance criteria and tests.
   For each affected story, include only relevant controls, but never omit a control that the workflow crosses:
   - tenant isolation for all tenant-scoped reads, writes, searches, reports, exports, portals, and jobs;
   - server-side RBAC, including direct API denial and absence of side effects;
   - atomic compliance mutation plus append-only audit event, or a proven transactional-outbox boundary;
   - No-CUI/data-handling mode and classification controls for uploads, notes, evidence, extraction, AI, reports, exports, and external access;
   - source provenance, qualified review metadata, lifecycle/versioning, and retirement behavior for compliance content;
   - immutable report/evidence snapshots and explicit archive/restore transitions where applicable;
   - standard errors without stack traces or cross-tenant disclosure;
   - retries, idempotency, duplicate submission, concurrency, cancellation, partial failure, and rollback/no-side-effect behavior where applicable;
   - accessibility and keyboard behavior for changed user-visible workflows;
   - draft-only status, citations, logging, and human review for AI-generated compliance content.

7. Separate software evidence from external outcomes.
   - Research stories may prove completion, traceability, sampling, review, and recorded decisions; they cannot prove product-market fit or regulatory correctness by themselves.
   - Readiness stories may produce governed artifacts and evidence; they cannot claim sustained operating effectiveness without operating evidence.
   - Independent assessments, certifications, authorizations, marketplace statuses, customer sponsorship, and agency decisions must remain external gates with human-owned evidence.
   - Do not create acceptance criteria asserting that application code can grant CMMC certification, SOC 2 status, FedRAMP authorization/equivalency, legal conclusions, government approval, or permission to process real CUI.

8. Synchronize affected artifacts.
   - `docs/mvp-roadmap.md`: retain roadmap-level outcomes and concise story traceability; do not duplicate full story bodies.
   - `docs/development-phase-use-cases.md`: canonical use cases, story text, status, tasks, and acceptance criteria.
   - `docs/development-story-prompts.md`: one implementation prompt per executable software story, matching canonical ID/title/scope and the repository's existing prompt format.
   - `docs/development-story-test-cases.md`: uniquely numbered `TC-<story-id>.<n>` cases covering every acceptance criterion and negative/security path that is testable in the product.
   - `docs/development-story-test-prompts.md`: matching focused automated-test prompts for each executable story.
   - `docs/Smoke_Test_development-story-test-prompts.md`: operator-observable smoke/manual checks for each executable story; do not pretend non-automatable external gates are smoke tests.
   - `docs/Automated development-story-test-prompts.md`: backend, frontend, integration, persistence, and browser strategies only where the behavior is automatable.
   - `docs/story_level_regression-test-execution-prompts.md`: ensure the generic or story-specific regression guidance includes the changed boundary.
   - `docs/production-readiness-phase-use-cases.md` and related readiness artifacts: update only when a roadmap change affects launch/readiness work; preserve `PR-*` identity.
   - `/Users/devups/.codex/skills/gccs-story-sequence/SKILL.md`: update constants, start points, ranges, ordering rules, scope notes, and fallback verification only when the executable development-story sequence actually changes. Keep the one-story-at-a-time implementation/test/commit/push workflow intact.
   - Other relevant docs discovered by reference search: update only stale links, ranges, phase mappings, or claims caused by this roadmap synchronization.

9. Maintain exact cross-file consistency.
   - Story IDs and titles must match byte-for-byte across canonical and derived inventories.
   - Every executable story must have canonical acceptance criteria, implementation guidance, `TC-*` coverage, an automated-test prompt, a smoke/manual prompt, and applicable regression guidance.
   - Each acceptance criterion must map to at least one test case or be explicitly labeled non-automatable with the required human evidence, owner, and review method.
   - Keep story order deterministic and phase-correct. Update tables of contents, delivery tables, links, start points, and terminal ranges when affected.
   - Do not copy a `Done` marker to a new or unverified story.

10. Validate before handing off.
   - Re-run reference searches for every new or changed story ID and title.
   - Check duplicate and missing IDs, duplicate `TC-*` IDs, orphan prompts, missing prompt/test counterparts, broken relative links, malformed headings, stale range/start-point claims, and roadmap items with no disposition.
   - Search changed customer-facing or compliance text for overclaims, including `certified`, `compliant`, `approved`, `guaranteed`, `audit ready`, `secure CUI`, `government approved`, `government endorsed`, and `required before work begins`; retain such language only when accurately negated, scoped, quoted for review, or directly evidenced and approved.
   - Inspect `git diff --check`, the scoped final diff, and `git status --short`.
   - Do not run builds or broad test suites for this documentation-only task unless an executable artifact, generated file, configuration, or code contract was changed.

Required final report

Report, in order:

1. Current-state roadmap coverage by phase.
2. Files changed and why.
3. Stories reused, refined, added, split, deferred, or classified as external/non-software gates.
4. Story IDs/titles and sequence ranges changed, including any skill update.
5. Consistency and link-check commands run with results.
6. Evidence supporting every `Implemented` or `Partially implemented` classification.
7. Non-automatable criteria and required human evidence.
8. Unresolved conflicts, hidden risks, edge cases, dependencies, and skipped verification.

Definition of done

- Every Phase 0, Phase 1, Phase 1A, Phase 2, Phase 3, Phase 4, FedRAMP-track, and newly added roadmap item maps to canonical story IDs or has an explicit non-software/external-gate disposition.
- No duplicate story represents an outcome already covered by an existing story.
- Every new or materially changed executable story is synchronized across the canonical backlog and applicable implementation, test-case, smoke, automated-test, and regression artifacts.
- Story IDs, titles, phase order, start points, ranges, and `TC-*` identifiers are internally consistent.
- Current-state claims are evidence-backed; future work is labeled `Planned` or `Partially implemented`; unsupported claims are labeled `Do not claim`.
- FeDril external branding, internal `Gccs.*` compatibility identifiers, Clean Architecture boundaries, server-authoritative security controls, append-only audit posture, and the No-CUI production boundary remain intact.
- The final diff contains no unrelated changes and the report discloses all unverified scope.
```

## Critique And Failure Modes Addressed

- A direct “create stories for every roadmap phase” instruction duplicates existing stories, produces ID collisions, and causes the canonical backlog, implementation prompts, test files, and sequence skill to drift. The prompt therefore requires a gap-first inventory and exact cross-file reconciliation.
- Treating every roadmap bullet as an application feature converts research, operating controls, independent examinations, and agency decisions into false software acceptance criteria. The prompt classifies outcome types and keeps external gates distinct from executable stories.
- Updating status and enforcement wording from documentation alone can create security and compliance overclaims. The prompt requires UI/API/domain/test evidence, negative-path coverage, and explicit `Implemented`, `Partially implemented`, `Planned`, or `Do not claim` classifications.

## Hidden Risks And Dependencies

- The repository may already contain uncommitted roadmap-story edits. The executor must preserve them and distinguish pre-existing changes from its own work.
- Current story ranges or start points may disagree across the canonical backlog, derived prompts, and `gccs-story-sequence`; reconciliation must follow the canonical inventory and must not silently renumber established IDs.
- External program rules, evidence windows, assessor scope, and marketplace processes change. Revalidate them against authoritative current sources when an activation gate is reached; a backlog refresh is not authorization evidence.
- Automated tests cannot prove interview quality, expert competence, sustained control operation, auditor independence, customer sponsorship, or agency decisions. Those require governed human evidence.
- A documentation-only pass can verify consistency and claims but cannot prove unexecuted application behavior or release readiness.
