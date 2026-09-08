# MVP Roadmap Status Workbook Authoring Prompt

Use this prompt to generate an evidence-backed Excel workbook showing completed work, remaining work, and the dependency-aware execution order for the full FeDril MVP roadmap.

```text
Act as a senior delivery architect, Scrum program analyst, evidence-based status reviewer, and spreadsheet author for FeDril.

Objective

Create a polished `.xlsx` workbook that shows every MVP roadmap phase, track, canonical story, and story task; identifies what is completed, partially completed, blocked, externally gated, not started, or not verified; and presents the sequential execution plan for all remaining work.

The workbook must help the owner move through the remaining roadmap without confusing documentation existence, a `Done` label, planned acceptance criteria, UI presentation, infrastructure-provider capability, or an internal readiness artifact with proven implementation or external assurance.

Output

- Create one workbook at `/Users/devups/Development/CodexProjects/Gccs/outputs/<unique-thread-id>/fedril-mvp-roadmap-status-and-execution-plan.xlsx`.
- Do not create CSV substitutes or extra workbook variants.
- Do not edit source documentation, application code, tests, skills, git state, or external systems while producing the workbook.
- Use the bundled spreadsheet runtime and `@oai/artifact-tool` according to the available Spreadsheets skill. Before the first workbook-authoring command, successfully run the required artifact-operation marker once for one XLSX output.

Authoritative references

Read these before classifying or ordering work:

- `/Users/devups/Development/CodexProjects/Gccs/AGENTS.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/mvp-roadmap.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/mvp-roadmap-story-traceability.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/development-phase-use-cases.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/development-story-prompts.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/development-story-test-cases.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/development-story-test-prompts.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/Smoke_Test_development-story-test-prompts.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/Automated development-story-test-prompts.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/story_level_regression-test-execution-prompts.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/regression-test-execution-prompts.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/production-readiness-phase-use-cases.md`
- `/Users/devups/Development/CodexProjects/Gccs/docs/production-readiness-story-prompts.md`
- `/Users/devups/.codex/skills/gccs-story-sequence/SKILL.md`
- `/Users/devups/.codex/skills/gccs-production-readiness-sequence/SKILL.md`

Discover directly relevant status evidence with `rg`, including story IDs, exact titles, `Done` markers, implementation files, focused tests, smoke evidence, production-readiness evidence, commits, and explicit gate decisions. Inspect only the evidence required to classify the roadmap. Do not run broad application builds, full regression suites, deployments, or external mutations for this reporting task.

Authority and conflict rules

Apply this precedence:

1. `AGENTS.md` product, security, compliance, architecture, and verification invariants.
2. Current implementation plus focused passing tests and governed operating evidence.
3. Approved gate or review records for research, production-readiness, and external-assurance work.
4. `mvp-roadmap.md` for roadmap outcomes and phase placement.
5. `mvp-roadmap-story-traceability.md` and `development-phase-use-cases.md` for canonical story IDs, titles, tasks, and mappings.
6. Derived implementation, test, regression, and sequence documents.

When sources disagree, preserve the canonical ID/title and record the conflict in the workbook. Do not silently renumber, merge, or infer completion.

Status classification rules

Use exactly these task and story statuses:

- `Completed`: Direct evidence proves the required deliverable or behavior and its applicable acceptance/test obligations. For software, require implementation plus relevant focused test evidence. For research/governance, require the governed artifact, reviewer/approval evidence, and required disposition. For an external examination or authorization, require the actual authoritative external evidence.
- `Partially Completed`: Evidence proves a meaningful subset, but one or more material acceptance criteria, tasks, tests, approvals, migrations, operating periods, or failure paths remain open. State both the proven portion and the gap.
- `Blocked`: A named dependency, failed verification, missing approval, missing environment, unresolved security/compliance issue, or external prerequisite currently prevents execution. Record blocker owner and unblock condition when documented.
- `Not Started`: Direct evidence shows the approved work has not begun.
- `Not Verified`: The source set does not prove either completion or non-start. Use this instead of guessing.
- `External/Human Gate`: The outcome depends on research participants, qualified reviewers, customers, auditors, assessors, agencies, Marketplace processes, an operating-evidence period, or another external decision. This is not equivalent to complete or blocked.
- `Not Applicable`: An approved scope decision explicitly excludes the item. Record the decision source and date.

Additional status rules:

- A `Done` heading, story prompt, acceptance criterion, test definition, UI warning, configuration field, migration, source-controlled policy, or roadmap status is evidence to inspect, not sufficient proof by itself.
- Do not upgrade a story to `Completed` because some tasks are complete. Calculate story completion from all child task statuses and required acceptance/test evidence.
- Do not treat a missing artifact as `Not Started` unless evidence proves non-start; otherwise use `Not Verified`.
- Preserve the roadmap's `Implemented`, `Partially implemented`, `Planned`, and `Do not claim` labels in a separate `Roadmap classification` column. Do not replace evidence-based execution status with those labels.
- Never claim FeDril is CMMC certified, SOC 2 certified/compliant, FedRAMP authorized/equivalent, government approved, audit ready, or approved for real customer CUI based on this workbook.

Inventory requirements

Before authoring the workbook, build and validate a normalized inventory containing:

- Every roadmap phase and cross-phase track: Phase 0, Phase 1, Phase 1A, Phase 2, Phase 3, Phase 4, Production Readiness `PR-*`, and FedRAMP `FR-0` through `FR-10`.
- Every canonical story and exact title from `development-phase-use-cases.md`, including non-software Stories `0.1`-`0.8`, software Stories `1.1`-`38.3`, and SOC 2 Stories `39.1`-`39.3`.
- Every task bullet beneath each canonical story.
- Every production-readiness story and task from `production-readiness-phase-use-cases.md`.
- Each story's roadmap mapping, outcome type, acceptance-criteria count, `TC-*` count, implementation-prompt availability, smoke-prompt availability, automated-test-prompt availability, and regression guidance.
- Status evidence: source file, section or line locator, evidence type, observed result, verification date when present, and limitation.
- Dependencies, predecessor story IDs, gate type, and whether the item belongs to the executable software story sequence, production-readiness sequence, or governed human/external workflow.

Do not duplicate a task merely because it appears in a derived prompt. The canonical backlog owns story tasks. Derived documents supply execution and verification metadata.

Workbook design

Create four worksheets in this order:

1. `Summary`
2. `Execution Plan`
3. `Task Tracker`
4. `Sources & Method`

### 1. Summary

Provide the main answer first:

- Workbook title and `Status reviewed as of` date.
- Overall story counts and task counts by status.
- Phase summary table with phase/track, total stories, completed, partially completed, blocked, not started, not verified, external/human gate, remaining tasks, and next executable story or gate.
- Separate visible callouts for:
  - earliest remaining software story;
  - earliest remaining production-readiness story;
  - earliest remaining human/external gate;
  - No-CUI posture;
  - unresolved status-evidence gaps.
- Add one compact, source-linked chart only if it materially improves the phase-status comparison. Do not add decorative charts or oversized KPI cards.
- Summary counts must use bounded formulas linked to the tracker/plan. Do not hardcode totals that can be calculated.

### 2. Execution Plan

Use one row per canonical story, production-readiness story, or `FR-*` gate in dependency-aware execution order.

Required columns:

- Overall sequence
- Phase/track
- Workstream
- Story or gate ID
- Exact title
- Outcome type
- Roadmap classification
- Evidence-based status
- Completed task count
- Total task count
- Remaining task count
- Predecessor IDs
- Gate/unblock condition
- Verification method
- Execution mechanism
- Next action
- Evidence summary
- Evidence source
- Risk/limitation
- Owner
- Target date

Use formulas to calculate completed, total, and remaining task counts from `Task Tracker`. Use bounded `COUNTIFS`/`SUMIFS` or simple equivalent formulas. Keep `Owner` and `Target date` as clearly formatted editable planning inputs when source documents do not provide them; leave them blank rather than inventing values.

The plan must retain all rows, including completed work, so updates remain traceable. Add filtering and a calculated `Remaining?` field so the user can filter to active work without relying on unsupported dynamic-array formulas.

### 3. Task Tracker

Use one row per unique canonical task.

Required columns:

- Overall sequence
- Phase/track
- Story/gate ID
- Exact story title
- Task sequence within story
- Task description
- Outcome type
- Roadmap classification
- Task status
- Remaining?
- Dependency/predecessor
- Completion evidence
- Evidence type
- Evidence source and locator
- Verification date
- Acceptance/test references
- Blocker or external dependency
- Next action
- Owner
- Target date
- Notes/limitations

Use a validated status list containing the exact execution statuses. Keep identifiers as text. Store dates as real spreadsheet dates. Add filters, freeze the header row and identifying columns, wrap task/evidence/notes columns, and use bounded conditional formatting for status and missing-evidence warnings.

### 4. Sources & Method

Record:

- Each source document, its purpose, and the date inspected.
- Status definitions and required evidence.
- Story-level rollup rules.
- Refresh instructions for moving a task from one status to another.
- Sequence rules and exceptions.
- Known conflicts or stale references.
- The distinction between software verification, governed human evidence, operating evidence, and independent/external evidence.
- The No-CUI and claims guardrails.

Do not place long methodology prose on the Summary sheet.

Story rollup logic

- `Completed` only when every required task is completed and all applicable acceptance/test evidence is present.
- `Blocked` when incomplete work has a current blocker that prevents the next required action.
- `Partially Completed` when at least one required task is completed but the story is not complete and not currently blocked.
- `External/Human Gate` when the next controlling action is external or human-owned and no software step can satisfy it.
- `Not Started` only when evidence proves non-start for every required task.
- Otherwise use `Not Verified`.

If workbook formulas would make the rollup opaque, calculate the evidence-based initial story status in the inventory and use simple formulas only for counts and `Remaining?`. Explain the refresh rule beside the editable status columns.

Sequential execution rules

Build the execution order from dependencies and verified completion, not merely numeric sorting:

1. Resolve the earliest incomplete or unverified Phase 0 research/decision gate needed to validate the MVP baseline. Stories `0.1`-`0.8` use governed human evidence and must not enter the software commit/push loop.
2. Execute remaining Phase 1 software stories in canonical delivery order, beginning with the earliest story whose prerequisites are satisfied. Verify the configured Phase 1 continuation point rather than assuming every earlier `Done` marker is valid.
3. Execute Phase 1A Stories `1A.1.1`-`1A.9.3` in canonical order when completing the future CUI-readiness track. Preserve the current No-CUI production boundary; completion of readiness stories alone does not authorize real CUI.
4. Execute production-readiness Stories `PR-0.1`-`PR-8.3` through the production-readiness sequence when preparing an approved launch. Do not mix their IDs with software stories.
5. Execute Phase 2 Stories `18.1`-`28.3` in canonical order after required MVP and gate dependencies are satisfied.
6. Execute Phase 3 Stories `29.1`-`34.3` in canonical order. Phase 3 begins with SSP Story `29.1`, not `30.1`.
7. Execute Phase 4 software Stories `35.1`-`38.3` in canonical order only after their identity, environment, security, operating, and data-handling prerequisites are satisfied.
8. Treat SOC 2 Stories `39.1`-`39.3` as a governed assurance workflow, not software implementation. Schedule examination work only after scope, commercial justification, control operation, evidence-period, and independent-auditor prerequisites are met.
9. Include `FR-0`-`FR-10` in the plan, but mark activation-dependent steps accordingly. Do not automatically schedule a full FedRAMP program. `FR-5`, `FR-7`, `FR-9`, and independent portions of `FR-10` remain human/external gates unless the roadmap activation conditions are met and current program requirements are revalidated.

For software stories, identify `/Users/devups/.codex/skills/gccs-story-sequence/SKILL.md` as the execution mechanism and preserve its one-story-at-a-time implementation, smoke, automated-test, regression, commit, and push workflow. For `PR-*` stories, identify the production-readiness sequence skill. For Phase 0, Story 39, and external `FR-*` gates, identify the governed human-evidence prompt or external process instead of a software skill.

Formatting requirements

- Use a restrained professional palette, one consistent font, dark table headers with white text, light structural borders, and no decorative iconography.
- Hide gridlines on all sheets.
- Use concise titles, readable widths, wrapped long descriptions, and fitted row heights without clipping.
- Place outputs before source/method content.
- Use tables with unique names and filters.
- Use status colors consistently: completed green, partial blue, blocked red, not started gray, not verified amber, external/human gate purple, and not applicable muted gray. Color is supplemental; status text must remain explicit.
- Use amber input formatting for editable owner, target date, and task-status cells.
- Keep valid records neutral; reserve warning formatting for blocked, missing evidence, conflicting status, or overdue editable target dates.
- Do not use merged cells in working tables.

Verification before export

1. Confirm every roadmap phase/item maps to a story, `PR-*` item, `FR-*` item, or explicit external/non-software disposition.
2. Confirm every canonical story appears exactly once in `Execution Plan` and every canonical task appears exactly once in `Task Tracker`.
3. Confirm story IDs and titles match the canonical source byte-for-byte.
4. Confirm Phase 3 starts at `29.1`; Phase 4 software ends at `38.3`; Story 39 is excluded from the software sequence.
5. Confirm no duplicate task keys, story IDs, or execution sequence values.
6. Confirm formulas use bounded ranges and produce no `#REF!`, `#DIV/0!`, `#VALUE!`, `#NAME?`, `#N/A`, `#NUM!`, `#NULL!`, `#SPILL!`, or `#CALC!` errors.
7. Recalculate once, inspect key Summary, Execution Plan, and Task Tracker ranges including values and formulas, and independently reconcile headline counts to source inventory counts.
8. Test representative status changes in a disposable copy or restore them before export. Verify remaining counts and next-action indicators update while evidence records remain unchanged.
9. Render and visually inspect every worksheet. Correct clipped text, unreadable wrapping, incorrect widths/heights, `####`, broken charts, misplaced conditional formatting, and unusable frozen panes.
10. Export the single final `.xlsx` file and verify it can be reopened or inspected successfully.

Required final report

Report:

1. Workbook output path.
2. As-of date and source files inspected.
3. Counts of phases/tracks, stories/gates, and tasks inventoried.
4. Counts by evidence-based status.
5. The first remaining software story, production-readiness story, and external/human gate.
6. The dependency-aware sequence for the next ten actionable items.
7. Every conflict, `Not Verified` classification, external gate, skipped verification, and unavailable environment.
8. Confirmation that no source documents, code, tests, skills, commits, pushes, deployments, or external systems were changed.

Definition of done

- The workbook contains all Phase 0, Phase 1, Phase 1A, Phase 2, Phase 3, Phase 4, `PR-*`, and `FR-*` work in a traceable hierarchy.
- Completed versus remaining work is evidence-backed and does not rely solely on labels or document presence.
- Every task has a status, source locator, verification method, remaining flag, dependency, and next action or an explicit `Not Verified` explanation.
- The execution plan is sequential, dependency-aware, activation-gated, and consistent with the canonical software and production-readiness sequences.
- The workbook preserves FeDril branding, internal `Gccs.*` identifiers, Clean Architecture expectations, server-authoritative security controls, append-only audit posture, source/review traceability, immutable report posture, and No-CUI production boundary.
- Formula, structure, visual, count-reconciliation, and export verification pass, with all limitations disclosed.
```

## Critique And Failure Modes Addressed

- Treating `Done`, a test prompt, or a documented acceptance criterion as completion would materially overstate progress. The prompt requires direct implementation, test, artifact, approval, operating, or external evidence according to the work type.
- Collapsing unknown work into `Not Started` produces a false remaining-work baseline and an unreliable schedule. The workbook therefore distinguishes `Not Verified`, `Not Started`, `Blocked`, and `External/Human Gate`.
- Sorting every ID numerically would omit dependency gates, misorder Phase 1A and SSP Story `29.1`, and incorrectly schedule SOC 2 or FedRAMP assurance as ordinary software. The prompt uses dependency-aware sequence rules and explicit activation gates.

## Hidden Risks And Dependencies

- A complete status audit may require repository history, CI results, deployed-environment evidence, reviewer records, or external documents that are not available locally. Missing evidence must remain `Not Verified`.
- Task-level completion cannot always be derived from story-level tests; partial implementation and untested failure paths must remain visible.
- Production readiness, operating effectiveness, independent examination, customer sponsorship, and agency decisions require governed human or external evidence.
- The workbook is a status snapshot. Owners must refresh evidence, dates, blockers, and statuses as work progresses.
- External assurance rules and required evidence periods can change. Revalidate current authoritative requirements when an activation gate is reached.
