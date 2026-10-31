# MVP Completion Determination

Determination date: 2026-07-29.

Scope: Phase 1 No-CUI MVP only. This determination does not approve broader production launch, real CUI processing, CMMC certification claims, legal advice claims, government approval claims, or Phase 2/3/4 customer-facing use.

## Critique & Flaws

- Treating feature completion as launch completion will fail at scale because a GovCon SaaS launch also depends on evidence quality, support routing, approved claims, security posture, and separation-of-duties approval. The current record supports controlled No-CUI pilot/project completion, not unrestricted customer expansion.
- Treating a generic member list as an assignment picker would create unnecessary identity exposure. The implemented pattern now uses `/api/contract-obligations/assignment-candidates` for obligation assignment and keeps full `/api/tenant-members` administration behind `ManageUsers`.
- Treating future phases as MVP requirements would create scope drift. Phase 2 Govcon Intelligence, Phase 3 Advanced Compliance, and Phase 4 Regulated Deployment are documented future phases and are not required to complete the Phase 1 No-CUI MVP.

## Current-State Requirement Classification

| Requirement area | Status | Implementation / evidence | Completion decision |
| --- | --- | --- | --- |
| Tenant, user, and RBAC foundation | Implemented | `apps/api/Program.cs`; `src/Gccs.Application/Identity`; `tests/Gccs.Api.Tests/RoleBasedPermissionTests.cs`; `tests/Gccs.Api.Tests/TenantMembershipTests.cs` | Complete for No-CUI MVP controlled pilot. |
| React + Vite authenticated application shell | Implemented | `apps/web/src/App.tsx`; `apps/web/src/App.test.tsx`; `apps/web/src/lib/api.ts` | Complete for No-CUI MVP controlled pilot. |
| No-CUI posture and upload guardrails | Implemented with controlled-pilot limitations | `docs/product-strategy.md`; `docs/architecture.md`; upload guardrail tests; production readiness evidence | Complete for No-CUI MVP controlled pilot; do not claim CUI-ready production. |
| Company profile | Implemented | `src/Gccs.Application/Companies`; `apps/web/src/App.tsx`; company profile tests | Complete for MVP workflow. |
| Contract intake and manual clause tagging | Implemented | contract endpoints, contract services, contract tests | Complete for MVP workflow. |
| Obligation dashboard and detail workflow | Implemented | `apps/api/Program.cs`; `src/Gccs.Application/Compliance`; `src/Gccs.Infrastructure/Compliance`; obligation tests | Complete for MVP workflow. |
| Obligation assignment picker privacy | Implemented | `/api/contract-obligations/assignment-candidates` requires `ManageObligations`; `/api/tenant-members` requires `ManageUsers`; `tests/Gccs.Api.Tests/ObligationDetailTests.cs` | Complete after purpose-specific endpoint/documentation alignment. |
| Compliance calendar and notifications | Implemented with provider limitations | application/API/UI notification and task workflow coverage | Complete for MVP in-app workflow; external email/provider claims depend on configured provider evidence. |
| Evidence vault and reports | Implemented with No-CUI constraints | evidence services, report snapshot/archive behavior, tests, readiness evidence | Complete for No-CUI MVP controlled pilot. |
| Basic CMMC Level 1 / Level 2 readiness tracking | Implemented | `src/Gccs.Application/Cmmc`; CMMC tests | Complete for readiness tracking; do not claim certification, SSP generation, or SPRS scoring. |
| Subcontractor flow-down tracker | Implemented | subcontractor services/tests | Complete for MVP workflow. |
| Audit logging | Implemented | audit writer/service use across mutations; audit tests | Complete for covered MVP workflows. |
| Source-backed obligation library | Implemented with governed publication limits | `packages/compliance-content`; compliance content governance/readiness evidence | Complete for reviewed MVP content; do not publish unreviewed high-risk records as customer-facing determinations. |
| Broader production launch approval | Partially implemented | `docs/production-readiness-checklist.md`; `docs/production-readiness-approval-posture-addendum.md` | Not complete; production separation-of-duties approval remains required before broader launch. |
| UI validation, denial, empty, error, and accessibility evidence | Partially implemented | `docs/production-readiness-launch-gap-decisions.md` tracks `DOD-GAP-001` as open | Not complete for broader production evidence; acceptable only as an open controlled-pilot limitation. |
| Launch-candidate manifest alignment | Partially implemented | `docs/release/approved-launch-candidate.json`; `docs/production-readiness-pilot-monitoring.md` | Only `launch-candidate-2026-07-28-1` is approved by the manifest; newer tags need approval update before use as pilot expansion or customer-facing launch evidence. |
| Live monitoring/support reconciliation | Partially implemented | `docs/production-readiness-pilot-monitoring.md` | Artifact monitoring exists; live Azure/support/backlog reconciliation remains a manual dependency before broader launch claims. |
| Phase 2 Govcon Intelligence | Planned / gated | `docs/mvp-roadmap.md`; `docs/production-readiness-phase-2-gate.md` | Not required for Phase 1 MVP; approved only for solo-controlled pilot testing/project completion. |
| Phase 3 Advanced Compliance | Planned | `docs/mvp-roadmap.md` | Not required for MVP. |
| Phase 4 Regulated Deployment | Planned | `docs/mvp-roadmap.md` | Not required for MVP. |

## The Correct Solution

1. Treat the Phase 1 No-CUI MVP as complete only for controlled pilot/project-completion scope when the verified code, tests, and evidence remain aligned.
2. Preserve the purpose-specific obligation assignment endpoint:
   - `/api/tenant-members`: full tenant membership administration, `ManageUsers` only.
   - `/api/contract-obligations/assignment-candidates`: minimal `{ userId, displayName }` assignment candidates, `ManageObligations` only.
   - View-only obligation users must not receive either full members or assignment candidates.
3. Keep `DOD-GAP-001` open until staging/production screenshots or equivalent evidence prove validation failure, permission denial, empty state, error state, and basic accessibility using synthetic or non-sensitive data.
4. Use only the approved launch candidate recorded in `docs/release/approved-launch-candidate.json` for pilot expansion or customer-facing launch evidence.
5. Before broader production launch, require production separation-of-duties approval across product, engineering, security, compliance content, support, and legal/contracting advisor roles.
6. Before any CUI-ready implementation or claim, require a separate approved CUI architecture, terms, shared responsibility matrix, support process, operating controls, and verification evidence.

## Rationale

The MVP is a No-CUI compliance-management product, not a regulated CUI storage platform. That distinction is structural: tenant isolation, RBAC, audit logging, source-backed content, upload controls, and claim discipline are all part of the product boundary. A customer-facing completion decision is valid only when the implementation, API contract, UI behavior, tests, and readiness evidence agree.

The assignment workflow demonstrates the required pattern. Obligation managers need a minimal list of active assignees to complete work, but they do not need MFA state, login timestamps, invitation state, full role administration metadata, or suspended/disabled/cross-tenant identities. The separate assignment-candidates endpoint minimizes disclosure while preserving the admin member endpoint for `ManageUsers`.

The remaining items do not block Phase 1 MVP functionality for a controlled No-CUI pilot, but they do block stronger claims. Broader production launch, CUI processing, audit-ready claims, certification claims, government approval claims, and Phase 2/3/4 claims require separate approval and evidence.

## Pre-Publication Checklist

- Does the UI expose the described flow?
- Does the API enforce the described rule?
- Is there a focused test proving allowed and denied behavior?
- Does the wording avoid certification, legal, government-approval, CMMC-success, audit-ready, and CUI-storage overclaims?
- Does the wording preserve the No-CUI MVP posture?
