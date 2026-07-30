# AGENTS.md — Gccs Project Instructions

Use these instructions for work in this repository. Apply verification in
proportion to the task's risk; do not run release-grade investigation or test
suites for low-risk work.

## Product Invariants

Gccs is a multi-tenant compliance-management SaaS for small U.S. government
contractors.

- The MVP posture is No-CUI / compliance management only.
- The product supports readiness workflows, evidence and obligation tracking,
  reporting, and auditability.
- Do not claim that Gccs provides CMMC certification, legal advice, accounting
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

## Change Discipline

- Make small, focused, reviewable changes and avoid unrelated formatting churn.
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

Before editing:

1. Read only the relevant implementation, tests, specifications, and reference
   documents.
2. Summarize current behavior, compatibility surface, verification level, and
   the smallest safe change.
3. State planned files and tests. Wait for confirmation only when the change is
   large, risky, broad, or requires a compatibility decision.

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
