# AGENTS.md — FeDril Project Instructions

Use these instructions for work in this repository. Apply verification in
proportion to the task's risk; do not run release-grade investigation or test
suites for low-risk work.

## File Preservation and Explicit Permission

- Obtain fresh, explicit user approval that names the exact targets, action,
  and destination before deleting, trashing, archiving, moving, renaming,
  hiding, or reducing the accessibility of any file or directory.
- This rule applies to tracked, untracked, ignored, generated, temporary,
  cache, evidence, configuration, hidden, and symlink items.
- Broad instructions such as "clean up," "organize," "deploy," "finish,"
  "make clean," or general recovery authorization do not authorize source
  removal.
- Do not use ignore or exclude rules, sparse checkout, Finder flags, dot
  prefixes, or access or ownership changes to conceal items or make them
  inaccessible without the same exact approval.
- `git clean`, destructive reset or path restoration, shared-branch switches,
  stash removal, worktree removal or pruning, ref deletion, reflog expiry,
  garbage collection, and scripts with equivalent effects require fresh,
  exact permission before they discard or conceal work.
- A backup, commit, stash, archive, or manifest never authorizes removal of the
  source.
- Before requesting approval, report the inventory and current state, active
  task ownership, destination collisions, sensitivity, reversibility, and
  retention. Stop when scope or ownership is uncertain or recovery
  destinations conflict.
- Delegated agents and tools must follow this rule. Inspect commands and
  scripts for destructive or concealing side effects before running them.
- Ordinary requested in-place content edits remain allowed. Do not truncate or
  replace out-of-scope content, and do not overwrite a conflicting recovery
  destination.
- Previously approved backup jobs may continue only within their exact
  approved scope; they gain no new deletion or pruning authority.
- This is agent guidance, not operating-system enforcement or an absolute
  technical prevention mechanism.

## Local Deployment And External Snapshot Coordination

- When explicitly executing a local foreground deployment or release-build
  command, run
  its exact argument vector through the FeDril deployment guard:
  `/opt/homebrew/bin/python3 -I -B '/Users/devups/Library/Application Support/FeDrilProjectRecovery/external-project-snapshots/fedril_deployment_guard.py' -- <command> <args...>`.
- Resolve the exact command and arguments from the approved workflow or runbook
  first. Ask the user only when those sources cannot determine them. Do not
  guess, reconstruct, or substitute a command.
- The guard covers only the foreground local process tree that preserves the
  inherited lock descriptor. It does not cover unwrapped commands, detached
  processes that close descriptors, or the lifetime of remote GitHub Actions
  workflows.
- Do not terminate deployments to permit a snapshot. The snapshot runner
  defers cooperatively and records prolonged deferral separately.

## Product Invariants

FeDril is a multi-tenant compliance-management SaaS for small U.S. government
contractors.

- The external product and customer-facing brand name is FeDril.
- Preserve internal `Gccs.*` code namespaces, project paths, headers, storage
  keys, migration names, and compatibility identifiers unless an explicit
  technical rename/migration is requested.
- The MVP posture is No-CUI / compliance management only.
- The product supports readiness workflows, evidence and obligation tracking,
  reporting, and auditability.
- Do not claim that FeDril provides CMMC certification, legal advice, accounting
  advice, labor determinations, government approval, or government endorsement.
- Production compliance content must be source-backed, reviewable, and governed
  by qualified subject-matter review.
- Treat AI-generated compliance content as draft-only unless an implemented,
  reviewed workflow explicitly establishes otherwise.

## Architecture Invariants

Preserve the existing Clean Architecture boundaries:

- `apps/api`: ASP.NET Core endpoints, authentication, tenant context, RBAC,
  request validation, response shaping, and API composition.
- `src/Gccs.Application`: use cases, DTOs, ports, workflow orchestration, and
  application services.
- `src/Gccs.Domain`: framework-independent entities, value objects, enums, and
  domain rules.
- `src/Gccs.Infrastructure`: EF Core persistence, migrations, repositories,
  seed adapters, and external integrations.
- `apps/web`: React + Vite presentation, route shell, client API calls,
  accessibility, and user-visible states.
- `packages/compliance-content`: source-backed compliance content and metadata.
- `docs`: product, architecture, API, database, governance, and delivery
  documentation.

Keep endpoints thin. Do not query databases directly from API handlers.
Business workflows belong in application services, persistence in
infrastructure, and framework-independent rules in the domain.

## Security and Compliance Invariants

These requirements apply whenever the affected behavior is relevant:

- Enforce tenant isolation and server-side RBAC on every tenant-scoped read,
  write, search, report, export, and background job.
- Never trust tenant ids, user ids, roles, permissions, or ownership supplied
  only by the client.
- Do not disclose cross-tenant identifiers, metadata, names, evidence, report
  data, audit records, or errors. Prefer `404` for missing or cross-tenant
  resources unless the established API contract uses `403`.
- Do not log secrets, passwords, tokens, credentials, raw customer documents,
  sensitive file contents, or file contents in audit events.
- Use the standard API error contract and never expose stack traces.
- Preserve append-only audit history. Compliance-relevant business writes and
  audit events must be atomic, or use a proven transactional-outbox design for
  external side effects.
- Audit compliance-relevant mutations, approvals, rejections, status changes,
  upload decisions, exports, policy acknowledgements, and data-handling posture
  changes where the implemented product supports them.
- Preserve the No-CUI upload policy for CUI, classified information,
  export-controlled or ITAR data, and sensitive government-furnished
  information.
- Preserve evidence and source traceability, review metadata, content
  provenance, and tenant boundaries.
- Compliance report artifacts are immutable snapshots. Administrative
  lifecycle changes use explicit archive/restore transitions, a reason,
  tenant-safe authorization, and append-only audit events; do not add in-place
  editing or hard deletion.
- Keep permissions server-authoritative. The UI must consume server-provided
  permissions and fail closed when permission data is absent or malformed.
- For SOC 2 Stories 39.1 through 39.23, `Founder authority FR-01` may perform
  authorized implementation, operation, evidence collection and custody,
  management decision-making, and explicitly labeled management self-review.
  FR-01's appointment does not make that self-review independent or establish
  professional qualification. Keep independent Security/evidence review,
  qualified CPA review or examination, qualified Legal/Privacy advice,
  forensic validation, and other separation-of-duties gates unassigned and
  deferred while eligible external resources are unavailable; do not mark
  those dependencies fulfilled or `Not Applicable`. When a resource becomes
  available, verify appointment, competence, independence and conflicts, then
  record the scoped review as pending; mark the dependency fulfilled only after
  its review evidence and decision are preserved. Continue other safe,
  authorized work whose non-review prerequisites are satisfied, and revisit
  the external dependencies at each story checkpoint, when resources become
  available, after a material incident, and before acceptance or claims.

## Change Discipline

- Make small, focused, reviewable changes and avoid unrelated formatting churn.
- Treat untracked and pending files as user-owned work. Do not relocate or
  remove them merely to produce a clean checkout.
- Do not switch a shared primary branch or retire worktrees without fresh
  approval for the exact targets, confirmed task ownership, and a verified
  recovery path. Preserve recovery archive sources unless their removal is
  separately approved.
- These checkout and recovery safeguards are procedural guidance; they are not
  technical enforcement by Git or the operating system.
- Preserve existing public and internal contracts unless a breaking change is
  explicitly requested and approved with a migration and rollback plan.
- Before changing behavior, identify affected callers, routes, controls,
  persisted fields, configuration keys, workers, tests, and documented flows.
- Use existing middleware, policies, services, repositories, DTOs, validators,
  error handling, and naming patterns before introducing new abstractions.
- Prefer explicit validation and simple, maintainable implementations.
- Preserve unrelated user changes; never revert them without authorization.
- Development-only behavior must be explicitly gated and must not weaken
  staging or production authentication or authorization.
- Frontend work must cover the user-visible states relevant to the change:
  loading, empty, success, error, and authorization denied.
- EF Core model changes require a scoped migration when the project uses
  migrations. Inspect existing data before adding constraints; do not silently
  rewrite ambiguous customer values.
- If a breaking change is unavoidable, stop before implementation and document
  compatibility impact, migration steps, rollback, and required approval.

## Verification Level

Classify the task before deciding how much repository inspection and
verification to perform. If uncertain between two levels, use the higher level.
The size of a diff does not lower the level when it changes a shared security
boundary.

### Read-only

Use for explanations, investigations, reviews, and status reports.

- Inspect only the files and behavior needed for an evidence-backed answer.
- Do not edit, build, run broad tests, or perform external mutations unless the
  user expands the task.

### Low

Use for non-behavioral internal documentation, comments, formatting, isolated
copy, and presentation-only styling.

- Inspect affected files and nearby contracts.
- Validate changed links, commands, formatting, or claims as applicable.
- Do not run application builds or broad test suites unless executable
  behavior, configuration, generated artifacts, or type checking is affected.
- Customer-facing compliance, marketing, demo, and workflow documents are not
  automatically low risk; apply the documentation rules below.

### Standard

Use for localized application behavior that does not touch a high-risk boundary.

- Record the existing behavior that must remain compatible.
- Run the narrowest relevant existing test before editing when practical.
- Add or update focused tests for the changed behavior.
- Run focused tests and build each touched application or project.
- Inspect the final diff and smoke-test the original and changed flow when the
  behavior is user-facing.

### High

Automatically use High verification for authentication, tenant scope, RBAC,
audit behavior, No-CUI controls, uploads, document classification, reports,
exports, compliance lifecycle mutations, shared authorization or error
middleware, persistence constraints, migrations, background jobs, external
side effects, or cross-layer critical workflows.

- Test allowed and denied behavior for every affected action and role.
- Add tenant-isolation tests for every affected tenant-scoped path, including
  empty and cross-tenant cases.
- Prove rejected, invalid, repeated, and cross-tenant mutations leave resources,
  audit history, jobs, notifications, tokens, and external systems unchanged.
- Test relevant lifecycle boundaries, retries, duplicate submissions,
  concurrency, cancellation, and partial infrastructure failure.
- Verify audit atomicity or transactional-outbox behavior when applicable.
- Test the complete affected endpoint inventory when a shared security boundary
  changes; a representative endpoint is insufficient.
- Trace critical workflows through every changed layer. Use real-stack tests
  when mocks cannot prove the boundary, persistence, browser interaction, or
  external-provider behavior.
- Run focused tests, adjacent regression suites, migrations, and builds for all
  touched projects. Disclose any unavailable environment or skipped coverage.

Trigger only the tests relevant to the changed boundary:

- Tenant-isolation tests for tenant-scoped behavior.
- RBAC matrices for protected actions or permission changes.
- Audit and rollback tests for compliance-relevant mutations.
- No-CUI tests for upload, document, classification, export, or data-handling
  changes.
- Browser tests for changed interaction, focus, scrolling, discoverability, or
  route behavior.
- Database inspection and migration tests for persistence-model changes.
- Real-provider fault injection for changed transactional external side effects.

### Release

Use only for deployment, production readiness, launch evidence, full UAT, or an
explicit full-regression request.

- Follow `docs/regression-test-execution-prompts.md` and the applicable
  development-story test cases.
- Run required focused and adjacent suites, builds, migrations, dependency
  scans, and real-stack critical-path tests before staging.
- Deploy to production only from the approved launch-candidate tag and manifest
  after required approvals and staging evidence.
- Verification evidence must identify commit SHA, environment, database
  provider, commands, result counts, execution time, skipped scope, and
  environmental differences.

## Customer-Facing Documentation

Before treating marketing, sales, demo, compliance, or workflow content as
usable, verify claims against the current UI, API behavior, authorization
rules, tests, and No-CUI product posture.

- Label material claims as `Implemented`, `Partially implemented`, `Planned`, or
  `Do not claim`.
- Use enforcement language such as "required," "blocked," "prevented,"
  "enforced," and "must" only when the API or domain service enforces it.
- Do not use claims such as certified, compliant, approved, guaranteed,
  government approved, audit ready, secure CUI storage, legal advice, CMMC
  certification, or required before work begins without direct implementation
  evidence and appropriate qualified review.
- Derive customer-facing documents from product behavior. If a document says
  the product blocks an action, identify the endpoint, service, and test that
  prove it.
- Check that the UI exposes the described flow, the API enforces the rule, a
  test proves it, the wording avoids legal/compliance overclaiming, and the
  content preserves the No-CUI posture.

## Working Procedure

### SOC 2 autonomy and bounded execution

For Stories 39.1–39.23, use section 39's shared execution contract and dependency
gates. On every execution request, briefly identify material prompt inefficiency
and supply a better bounded instruction if needed; then act without waiting for
approval of the wording. Do not manufacture flaws or repeat the critique after
each routine continuation. A checkpoint is an execution boundary, not a request
for the founder to say "continue".
When architecture is flawed, analyze and state the corrected pattern before
coding. Proceed with an already-authorized bounded compatible correction; hold
unresolved breaking decisions or otherwise gated actions, not the entire queue.

- Verify technical answers through relevant code, tests, docs and scoped live
  GitHub/Azure reads before asking. Discover available tools first; do not ask
  the founder to design a technical solution or paste obtainable configuration.
  Inspect only sources relevant to the question; do not make local, Azure,
  GitHub and web checks a mandatory repeated sequence. Repair workflow rules only
  for demonstrated defects; adequate prompts need no rewrite or new configuration.
  Record inaccessible facts as unverified. Never expose secrets, restricted
  evidence or provider identifiers in chat or public Git.
- Use numeric story order as default priority, with immediate 39.4 exposure
  triage. After setup, continue independent 39.5–39.17 work whose actual
  prerequisites are met. A blocked approval or deferred reviewer stops the
  affected operation, not every other authorized task. Preserve live-risk,
  protected-evidence, release, independent-review and final-acceptance gates.
- Select the smallest remaining unit with task ID, finite affected-path and
  requirement inventory, allowed actions, evidence and verification outcome.
  Continue selecting eligible work until the authorized scope is exhausted or
  no safe eligible task remains. Do not automatically end with a handoff.
- Delegate independent read-heavy discovery to `grunt`, dependency/coverage
  mapping to `planner`, and consequential technical review to `soc2_reviewer`
  when useful. Read `.codex/agents/<role>.toml` if the client does not discover
  those roles. Inherit the current model and permissions. These role instructions
  do not create a separate sandbox or constitute an independent compliance review.
  Use only roles needed; root owns canonical records and shared-file edits.
  Assign disjoint files or isolated checkouts for delegated edits. Do useful
  independent work while agents run; join their results before dependent actions.
- Keep a compact current checkpoint in the existing ledger/story record:
  task, source-document hash, candidate SHA plus uncommitted-file fingerprints,
  environment, verified results and limitations, pending session/run IDs,
  authorization scope, blockers, and exact next action. Store restricted details
  only in approved protected evidence. Reconcile live state after interruption
  before replay; completion of a chat turn is not proof of task completion.
- Reuse verification only for unchanged relevant source/configuration,
  environment, population and freshness. Run the High/Release checks required
  by the affected boundary; do not run whole-repository checks for record-only
  edits or repeat passed CI merely because a chat disconnected. Diagnose a failed
  check before retry. Treat uncertain external outcomes as reconciliation work.
- Treat missing evidence as an open requirement, not success or N/A. Separate
  technical verification, operating evidence, management review, independent
  review and release. End only with a truthful closure or precise unavailable
  dependency/decision after other eligible authorized work is exhausted.
  An active external run may be checkpointed as pending; do not consume repeated
  turns with unchanged polling or claim unattended continuation without a runner.

Before editing:

1. Read only the relevant implementation, tests, specifications, and reference
   documents.
2. Summarize current behavior, compatibility surface, verification level, and
   the smallest safe change.
3. State planned files and tests. Use risk to choose verification. Proceed with
   already-authorized bounded work; size or risk alone does not require another
   confirmation. Obtain a concrete decision for an unresolved breaking change,
   scope expansion or action requiring fresh approval under the applicable rules.

After editing:

1. Inspect the final diff for unrelated changes, removed behavior, stale copy,
   configuration drift, and weakened security.
2. Report files changed, tests and builds run, relevant controls preserved,
   original flows re-tested, untested scope, hidden risks, dependencies, and
   remaining compatibility limitations.
3. Never claim that all bugs were detected or that unexecuted verification
   passed.

## Project References

Read only references relevant to the current task:

- `README.md`
- `docs/architecture.md`
- `docs/database-models.md`
- `docs/compliance-content-governance.md`
- `docs/security-control-implications.md`
- `docs/product-strategy.md`
- `docs/mvp-execution-plan.md`
- `docs/mvp-roadmap.md`
- `docs/design-flow-diagrams.md`
- `docs/workflow-diagram.md`
- `docs/development-story-prompts.md`
- `docs/development-story-test-cases.md`
- `docs/regression-test-execution-prompts.md`
