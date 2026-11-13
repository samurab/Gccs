# Decision Log

This log records major GCCS product, architecture, security, compliance content, and release decisions. It is a governance artifact, not legal advice or production approval by itself.

## Entry Template

```text
Decision:
Date:
Context:
Options considered:
Chosen approach:
Risks accepted:
Mitigations:
Approver:
Review date:
```

## Decision: No-CUI MVP Launch Posture

Date: 2026-06-24

Context:

GCCS is preparing for the Production Readiness phase of the MVP. The product includes synthetic CUI-ready demonstration workflows for sandbox validation, but the MVP launch must not accept real customer CUI. Production launch depends on clear customer-facing posture, server-side tenant mode enforcement, staging evidence, content governance, support readiness, and formal approvals.

Options considered:

- Launch as No-CUI / compliance management only with synthetic CUI-ready demonstration workflows.
- Launch as production `CuiReady` for selected tenants.
- Remove synthetic CUI-ready demonstration workflows from the MVP.

Chosen approach:

Launch the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**. `DemoSandbox` may use synthetic or redacted demonstration data. `NoCui` production tenants must block real CUI. Future `CuiReady` operation requires separate approval before any real CUI can be accepted.

Explicit exclusion:

Real customer CUI remains prohibited until a future `CuiReady` posture is approved with architecture, customer terms, shared responsibility matrix, operating controls, support model, evidence handling controls, and required product, engineering, security, compliance content, and legal or contracting advisor signoff.

Risks accepted:

- Synthetic demo workflows may be confused with authorization to store real CUI.
- Future `CuiReady` implementation may leak into production claims.
- Production malware scanning remains a launch decision dependency.
- Staging restore evidence remains a production launch dependency.

Mitigations:

- Enforce tenant mode boundaries server-side for `DemoSandbox`, `NoCui`, and future `CuiReady`.
- Keep customer-facing copy aligned to No-CUI / compliance management only.
- Review product claims before launch.
- Build and attach the launch evidence package.
- Finalize support runbooks for suspected CUI and prohibited uploads.
- Track production blockers in `docs/production-readiness-checklist.md`.
- Execute the Production Readiness roadmap in `docs/production-readiness-roadmap.md`.

Approver:

Pending product owner, engineering lead, security owner, compliance content owner, customer success/support owner, and legal or contracting advisor approval.

Approval status:

| Required approver | Current status | Launch blocker while pending |
| --- | --- | --- |
| Product owner | Pending | Yes |
| Engineering lead | Pending | Yes |
| Security owner | Pending | Yes |
| Compliance content owner | Pending | Yes |
| Customer success/support owner | Pending | Yes |
| Legal or contracting advisor | Pending | Yes |

Review date:

Before MVP launch candidate tagging.

## Decision: Evidence-Based Agent Instructions And Bounded Execution

Date: 2026-10-23, America/New_York

Status: **Implemented locally — instruction maintenance only.**

Context:

The user requested a review and direct correction of previous instructions, then
provided replacement AGENTS.md instructions. The available global/project
guidance and SOC 2 orchestration sources contained conflicting critique rules,
unnecessary discovery steps, ambiguous public-record write targets and a status
name inconsistent with section 39. The review covered these maintained sources;
historical chat messages and platform instructions were not rewritten.

Chosen approach:

| Previous instruction or ambiguity | Corrected operating rule | Reason |
| --- | --- | --- |
| Always find three flaws and rewrite the code. | Report evidence-backed findings without a quota; edit only within the requested task. | Avoid invented defects and unauthorized changes during read-only reviews. |
| Reject first and always use critique/solution/rationale headings. | Evaluate first; acknowledge sound approaches and use concise, task-appropriate explanations. | Evidence determines the conclusion; a fixed response ceremony does not improve correctness. |
| Routine stages can end with another plan or offer to continue. | Finish already-authorized design, compatible implementation, relevant verification and checkpoint before presenting an actual gated decision. | Reduce repeated plan/continue loops while respecting plan-only requests and holds. |
| Reconcile Git, GitHub and Azure and read release workflows for every story. | Inspect only relevant state and pending outcomes; perform provider/release discovery when needed and authorized. | Local instruction/offline units do not need unrelated live access. |
| The repository ledger is an automatic coordination write target. | Resolve operative private records through the accepted manifest; keep public projections separately approved. | Prevent private working context from being reconstructed or written into public Git. |
| Use a story Complete status. | Use section 39's existing Accepted status only after evidenced criteria and actual applicable decisions. | Keep status vocabulary and acceptance distinct from technical progress. |
| Missing thread tooling suggests an immediate handoff. | Continue eligible authorized work in the coordinating thread; hand off only when actually needed. | Tool availability should not create an artificial execution boundary. |
| Procedural controls or synthetic passes can imply absolute runtime guarantees. | State the exact tested boundary, residual limits and unverified scope; do not invent new absolute-proof requirements. | Avoid both assurance overclaims and requirements that the task never imposed. |

Maintained locations:

- Global user guidance: `~/.codex/AGENTS.md`.
- Project guidance: [AGENTS.md](../AGENTS.md).
- Repository and matching installed copies of the
  [SOC 2 orchestrator](../skills/fedril-soc2-story-orchestrator/SKILL.md) and its
  [recovery reference](../skills/fedril-soc2-story-orchestrator/references/release-and-resume.md)
  and [record template](../skills/fedril-soc2-story-orchestrator/references/story-record.md).
- This decision is the change note; it does not replace the SOC 2 evidence/task
  registers or the separately governed private continuation records.

Preserved boundaries:

File-preservation approval rules, deployment/snapshot coordination, No-CUI,
architecture, tenant/RBAC/audit invariants, exact publication/intake/runtime holds,
human acceptance and independent/qualified review gates remain applicable.
Section 39 task IDs, obligations and acceptance criteria are unchanged. Managed
Headroom blocks and RTK.md are unchanged. Existing adequate agent-role guidance
was reviewed without adding mandatory delegation ceremonies.

Authority and risks:

The user authorized instruction reevaluation and updates. That authorization is
not a new operational risk acceptance, provider action, evidence intake,
publication, release or acceptance decision. Instructions remain procedural;
they do not enforce an OS sandbox, prevent transport failure or guarantee work
after a turn ends. Global guidance applies beyond this repository where loaded;
project-specific operating gates remain scoped to the project.

Validation:

Both repository and installed skill copies passed structural validation. The
skill and its two reference files match their installed copies. Ten scoped
Markdown links, whitespace checks and preservation comparisons passed; the
original decision-log content remains intact. A separate technical evaluator
reviewed eight offline decision scenarios covering implementation continuation,
review-only scope, protected/public records, bounded claims, uncertain external
outcomes, file-action authority, unavailable thread tooling and pending acceptance.
It found no remaining substantive instruction conflict in that finite review.
These are simulated decisions, not executed provider actions or independent
compliance review. No application builds or broad regression tests ran for this
instruction-only change; no application behavior or SOC 2 readiness is established.

Review trigger:

Revisit on a demonstrated instruction conflict or workflow failure, or an
explicit user request. Do not rewrite adequate prompts or repeat this audit on
every continuation. New user instructions and actual scoped approvals govern
their applicable work; general requests to improve instructions do not lift holds.
