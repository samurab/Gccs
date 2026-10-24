---
name: fedril-soc2-story-orchestrator
description: Execute, check status, and resume FeDril/GCCS SOC 2 Stories 39.1–39.23 using dependency-based continuation, bounded verification, existing evidence and safe recovery. Use for SOC 2 story/program execution; not the generic software story sequence.
---

# FeDril SOC 2 story orchestrator

Locate and verify the current repository root with Git and the expected FeDril
documents in the current workspace or known project roots. The historical path
`/Users/devups/Development/CodexProjects/Gccs` is a discovery hint, not an assumption.
Maintained source is repository-relative `skills/fedril-soc2-story-orchestrator`
and the discoverable installed copy in this environment is
`/Users/devups/.codex/skills/fedril-soc2-story-orchestrator`. Keep corresponding
files identical when updating this skill; compare first and preserve conflicting
edits. The current `docs/development-story-prompts.md` section 39, its companion
assessment prompt and applicable `AGENTS.md` govern requirements and authority.
This skill coordinates them; it does not replace them or broaden authorization.

## Shared rules

- Inspect each incoming execution prompt for material inefficiency. Briefly give
  a better bounded prompt if useful, then proceed under existing authorization.
  Do not manufacture flaws, repeat critique on routine continuation, or wait for
  approval of wording. Choose routine technical solutions with an evidence-backed
  rationale instead of asking the founder to design them.
  For flawed architecture, state the corrected pattern before coding and proceed
  with authorized compatible corrections. Hold unresolved breaking decisions or
  otherwise gated actions while continuing eligible independent work.
- Adequate prompts need no rewrite. Do not repeat workflow optimization or full
  tool inventories before every story. Change instructions only for demonstrated
  defects or useful missing behavior; inspect sources relevant to the question.
- Use numeric order as default priority and triage plausible 39.4 exposure
  immediately. After setup gates, continue dependency-satisfied independent work
  in 39.5–39.17 as section 39 permits. A deferred reviewer does not suspend all
  management-side work; preserve the exact affected acceptance/release gates.
- Keep `docs/soc2/soc2-execution-ledger.md` as the coordination checkpoint. Reuse existing canonical SOC 2 records; do not create a second tracker for the same fact. Use the [record template](references/story-record.md) for per-story handoffs and completion records.
- A story is `Complete` only with evidenced acceptance criteria and applicable gates. Track design, implementation, operating evidence, governance approval, staging, and production independently. A merged change or configured control does not prove operation, approval, an observation period, or assurance.
- Place only sanitized references and summaries in Git. Keep restricted evidence in its approved protected workspace. Never inspect or export secret values, customer content, or CUI, and do not put tenant, subscription, resource, user, network, or endpoint identifiers into chat or assessment artifacts.
- Preserve the No-CUI product posture and existing Clean Architecture, tenant isolation, server RBAC, audit, report immutability, and release boundaries. These are verification targets, not assumed facts.
- Continue eligible authorized work without waiting for "what's next". Stop only
  affected actions at unmet authority, access, evidence, live-risk or review gates;
  record their dependency and select other eligible work. End when authorized work
  is exhausted or no safe eligible work remains. Never infer human approval.
- Resolve technical questions from targeted repository inspection and authorized
  live GitHub/Azure metadata first. Check effective permissions, not only tool
  presence; preserve unverified scope when access fails. Ask only for an actual
  unavailable fact, access or human decision, with a concrete recommendation.
- Instructions do not prevent transport failure or run after a turn ends.
  Checkpoints limit lost work; an unattended supervisor is planned unless actually
  implemented, verified and active. Never promise background continuation otherwise.

## Start

1. Check applicable instructions, repo/branch/dirty state, current requirements
   hash and existing checkpoint. Preserve unrelated work. Read section 39's shared
   contract, gates and selected story, plus assessment/coverage references relevant
   to its remaining requirements. Use targeted reads thereafter; remap changed
   requirements without invalidating evidence whose relevant inputs still match.
2. Reconcile the current unit with actual Git, GitHub and Azure read-only state.
   Reuse its branch, PR, run and evidence; a stale ledger is not reason to recreate
   them. Read [release and recovery](references/release-and-resume.md) before
   external mutation or recovery from an uncertain outcome.
3. Select the smallest eligible unit: stable task ID, finite requirement/affected
   path inventory, permitted files/actions, dependencies, acceptance checks and
   evidence. Use judgment to fix verified in-scope defects. Map unrelated findings
   to the owning existing story with urgency/gate impact; triage critical risks.
4. Delegate useful independent discovery to project `grunt`, coverage/dependency
   mapping to `planner`, and technical review to `soc2_reviewer`. Their instructions
   live in `.codex/agents/`; read and pass them explicitly if role auto-discovery
   is unsupported. Use only necessary roles; inherit current model/permissions.
   Root owns canonical writes and decisions. Disjoint files or isolated checkouts
   are necessary for concurrent edits. Subagents are not independent assurance.
   Continue independent work while they run; reconcile outputs before dependent
   actions. Create a new user-owned chat only when explicitly requested.
5. Implement the bounded correction; apply the appropriate verification level.
   Reuse tests only when relevant source/configuration, environment, population and
   freshness match. Never weaken security tests to obtain a green check. Retain
   failures, skips and missing evidence; rerun affected checks when inputs change.
6. Update current checkpoint and requirement dispositions in existing records at
   material transitions, using [record fields](references/story-record.md). Check
   acceptance against evidence and actual review separately from technical progress
   and deployment. Continue the next eligible unit within authorized scope.

## Status

For a status-only request, reconcile and report without starting implementation.
Read the compact current checkpoint, current contract and relevant story; verify
pending GitHub/Azure state where accessible. Report verified technical outcomes,
acceptance, blockers and next eligible unit separately. Unknown remains unverified.

## Resume

Reconcile the saved task, requirements hash, branch, SHA/uncommitted fingerprints,
pending session/run, evidence and authorization first. Query actual external state
before any replay. Resume from the last verified transition; diagnose failed
checks rather than restarting discovery. Uncertain mutations need reconciliation,
not blind retry. Keep blocked scope explicit and continue independent authorized
work. No model instruction guarantees a successful reconnect.
For repeated failures, use the bounded diagnostic/read-retry procedure in
[release and recovery](references/release-and-resume.md). Do not retry an unchanged
authentication failure or use speculative settings to hide a transport problem.

## Dry run

After changing this skill, validate source and installed copies with
skill-creator's `quick_validate.py`, verify parity, and rehearse relevant
[failure scenarios](references/release-and-resume.md) without external writes.
Before first external mutation, verify applicable scenario handling and live
authority. Do not repeat the full rehearsal on routine resumes with no workflow
change. Use the existing isolated validator runtime:

```sh
'/Users/devups/Library/Application Support/FeDrilSOC2Tools/skill-validator-venv/bin/python' -I -B '/Users/devups/.codex/skills/.system/skill-creator/scripts/quick_validate.py' '/Users/devups/.codex/skills/fedril-soc2-story-orchestrator'
```

The local validator runtime is CPython 3.14 with pinned `PyYAML==6.0.3`; its retained wheel hash and provisioning record are in `/Users/devups/Library/Application Support/FeDrilSOC2Tools/skill-validator-provisioning.md`. This validator checks skill structure only. A dry run proves decision handling only; it does not satisfy a story gate.
