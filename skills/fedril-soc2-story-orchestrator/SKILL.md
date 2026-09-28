---
name: fedril-soc2-story-orchestrator
description: Coordinate FeDril/GCCS SOC 2 Stories 39.1–39.23 from the authoritative story document, with verified stage gates, sanitized evidence, release checkpoints, and safe resume. Use when starting, checking status, or resuming this SOC 2 story sequence.
---

# FeDril SOC 2 story orchestrator

Use this skill for `start`, `status`, or `resume` of the FeDril/GCCS SOC 2 program. The repository is `/Users/devups/Development/CodexProjects/Gccs`; the version-controlled skill source is `skills/fedril-soc2-story-orchestrator`. The authoritative requirements are the **current** `docs/development-story-prompts.md`, section 39 and its SOC 2 readiness assessment companion prompt. Do not substitute this skill's summary for that document. Read applicable `AGENTS.md` and current relevant code, tests, records, and workflows before acting.

## Shared rules

- Process one story at a time in the document's execution order. Story 39.4 exposure triage is immediate, including when earlier setup remains pending. The next story is eligible only under the document's dependency and stage-gate rules.
- Keep `docs/soc2/soc2-execution-ledger.md` as the coordination checkpoint. Reuse existing canonical SOC 2 records; do not create a second tracker for the same fact. Use the [record template](references/story-record.md) for per-story handoffs and completion records.
- A story is `Complete` only with evidenced acceptance criteria and applicable gates. Track design, implementation, operating evidence, governance approval, staging, and production independently. A merged change or configured control does not prove operation, approval, an observation period, or assurance.
- Place only sanitized references and summaries in Git. Keep restricted evidence in its approved protected workspace. Never inspect or export secret values, customer content, or CUI, and do not put tenant, subscription, resource, user, network, or endpoint identifiers into chat or assessment artifacts.
- Preserve the No-CUI product posture and existing Clean Architecture, tenant isolation, server RBAC, audit, report immutability, and release boundaries. These are verification targets, not assumed facts.
- Do not claim background continuation without an active runner. Stop at unmet approvals, credentials, external dependencies, observation periods, or failed gates. Record the exact next action; ask only for the minimum decision or access that cannot be inferred.

## Start

1. Check repo root, remote, branch, dirty state, current story-document hash, applicable instructions, existing SOC 2 records, GitHub/Azure *capability metadata*, and workflow triggers/targets. Preserve unrelated changes; use an isolated checkout when needed. Compare the current document with the ledger's recorded hash and re-map changed requirements before proceeding.
2. Read section 39, the execution order/stage gates, shared execution contract, recommendation coverage, recurring calendar, companion prompt, and the complete eligible story. Check the [release and resume procedure](references/release-and-resume.md) before any external mutation.
3. Reconcile ledger claims with Git, GitHub, and Azure read-only state. Inspect existing branch, PR, workflow runs, artifact revision, deployments, approvals, and evidence references before creating or triggering anything. Record unknowns as unverified.
4. Check whether this environment actually supports named, separate task threads. If supported, create a thread with the exact `Story 39.N: <document title>` when that story becomes eligible and supply its contract, dependencies, evidence, and checkpoint. If unavailable, continue sequentially here with a per-story record; do not relabel subagents or files as UI threads.
5. Determine the smallest safe remaining work. For each story, use the repository's verification level and run its relevant tests and release gates. Update the ledger at every material transition and wait for verified outcome before the next story.

## Status

Read the ledger and source document; reconcile the current story with live read-only GitHub/Azure state where available. Report the current story, exact stage, verified evidence, blockers, next eligible story, and exact next action. Do not infer success from a missing or stale ledger entry.

## Resume

Re-run `status` reconciliation first. Verify the saved branch/revision/PR and deployment runs against live state, including whether a workflow completed after the last ledger update. Reuse existing work. A failed or interrupted run resumes from its last *verified* transition. If credentials or thread creation are unavailable, keep local verified work and a ready handoff; never fabricate an external result or rerun a deployment merely because recording was interrupted.

## Dry run

Before first mutation or a substantial workflow change, validate frontmatter with skill-creator's `quick_validate.py` and rehearse the [failure scenarios](references/release-and-resume.md) without writes to GitHub/Azure. A dry run proves decision handling only; it does not satisfy a story gate.
