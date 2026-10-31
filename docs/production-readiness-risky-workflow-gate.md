# Production Readiness Risky Workflow Gate

Review status: Complete.

Review date: 2026-06-26.

Review owner: Engineering lead.

Scope: production-readiness launch stories that can alter data ingress, data egress, automated processing, tenant boundaries, RBAC, audit logging, upload, import, export, search, AI, evidence, report, extraction, or background processing behavior.

Gate rule: a risky workflow story remains in launch scope only when tenant-mode enforcement, RBAC review, audit logging implications, tenant isolation coverage, and test evidence are explicit. If coverage is missing, the story is blocked, deferred, or narrowed before implementation.

No unreviewed data ingress, data egress, or automated processing story remains in launch scope.

| Story ID | Risky workflow category | Risk classification | Tenant-mode coverage | RBAC coverage | Audit logging coverage | Tenant isolation coverage | Disposition |
| --- | --- | --- | --- | --- | --- | --- | --- |
| PR-1.3 | Gate for upload, import, export, search, AI, evidence, report, extraction, and background processing changes | High | Required by this gate and PR-1.2 mapping | Required by this gate | Required by this gate | Required by this gate | Accepted as governance gate. |
| PR-2.2 | Completed-story review for upload, report, evidence, extraction, RBAC, audit, and tenant-mode gaps | High | Gaps become blockers or risks | Gaps become blockers or risks | Gaps become blockers or risks | Gaps become blockers or risks | Accepted; incomplete coverage must become PR-2.3 launch decision. |
| PR-2.3 | Gap disposition for risky workflows | High | No-CUI gaps block launch unless formally excepted | RBAC gaps require owner and mitigation | Audit gaps require owner and mitigation | Tenant isolation gaps require owner and mitigation | Accepted; each gap needs blocker/risk/defer disposition. |
| PR-3.2 | Staging end-to-end workflow: upload, evidence, report, extraction-adjacent obligation flow, audit | High | Blocked real CUI and synthetic-only data required | Role assignment and permission checks required | Audit log evidence required | Tenant workflow scoped to staging tenant | Accepted with staging dependency. |
| PR-3.3 | Staging tenant isolation and RBAC direct API bypass | Critical | Tenant mode cannot be bypassed | Owner/admin/contributor/auditor/advisor checks required | Denied access evidence reviewed where applicable | Cross-tenant read/update/delete/export/report checks required | Accepted with staging dependency. |
| PR-3.4 | Staging upload guardrails and report/export controls | Critical | Real CUI blocked; demo data synthetic/redacted | Upload/report permissions required | Upload block and report export audit events required | Report/export scoped to tenant | Accepted with staging dependency. |
| PR-4.2 | Deployment, migration, rollback | Medium | Migration cannot expand data posture | Operator permissions reviewed | Deployment and rollback events traceable | Migration rollback must preserve tenant data boundaries | Accepted with release-artifact dependency. |
| PR-4.3 | Malware scanning launch path for uploads | High | Scanner path must preserve No-CUI limits | Scanner administration access limited | Scanner decision and exception evidence retained | Upload scanning must not mix tenant data | Accepted with security/product decision dependency. |
| PR-5.1 | High-risk obligation content publication | Medium | Content cannot imply CUI authorization | Content publishing permissions reviewed | Review/publication decisions auditable | Content views remain tenant-scoped where applicable | Accepted with SME/legal dependency. |
| PR-5.2 | Customer-facing claims | High | Future `CuiReady` remains separately gated | Claim approval roles clear | Claim decisions retained | Claims must not imply tenant boundary exceptions | Accepted with legal/contracting dependency. |
| PR-5.3 | Support runbooks for prohibited upload, suspected CUI, tenant exposure, evidence/report failure | High | Suspected CUI path preserves No-CUI | Support access least privilege | Support actions preserve auditability | Tenant exposure runbook required | Accepted with support/security signoff dependency. |
| PR-5.4 | Pilot onboarding, release notes, known-risk log | Medium | Pilot onboarding prohibits real CUI | Pilot roles/access clear | Pilot acceptance and known-risk records retained | Pilot access scoped per tenant | Accepted with known-risk dependency. |
| PR-6.1 | Launch approvals | Medium | Approvers confirm No-CUI posture | Approver authority explicit | Approval evidence retained | Approval package covers tenant isolation evidence | Accepted with approver dependency. |
| PR-7.1 | Production deployment through CI/CD | High | Production remains No-CUI | Deployment permissions least privilege | Deployment events logged | Production tenant boundaries preserved | Accepted with production access dependency. |
| PR-7.2 | Production smoke: login, RBAC denial, upload warning/blocking, evidence upload, report generation, audit, logs, alerts, health | Critical | Real CUI upload remains blocked | Role checks required | Audit events reviewed | Tenant access checks required | Accepted with production deployment dependency. |
| PR-7.3 | Controlled pilot onboarding | High | Pilot customers acknowledge No-CUI limits | Pilot roles scoped | Onboarding acknowledgement retained | Pilot tenants isolated | Accepted with customer/support dependency. |
| PR-8.1 | Pilot monitoring: health, logs, support, upload blocks, permission denials, reports, content disputes, incidents | High | Suspected CUI signals escalated | Access denial trends reviewed | Monitoring review records retained | Tenant exposure signals escalated | Accepted with pilot activity dependency. |
| PR-8.2 | Post-launch readiness findings | High | No-CUI or tenant-mode findings block expansion | RBAC findings require disposition | Audit gaps require disposition | Tenant isolation findings block continuation | Accepted with pilot evidence dependency. |
| PR-8.3 | Phase 2 gate | Critical | No posture expansion without separate approval | RBAC instability blocks affected expansion | Audit instability blocks affected expansion | Tenant isolation instability blocks Phase 2 | Accepted; Phase 2 blocked until controls are stable. |

## Blocked Or Deferred Risky Workflow Stories

No risky workflow story is silently accepted without controls. Stories listed as accepted with dependency are narrowed to evidence collection, verification, or launch decision work until the named dependency is available. Any implementation change discovered during these stories that alters upload, import, export, search, AI, evidence, report, extraction, background processing, RBAC, audit logging, or tenant boundaries must return to this gate.

## Verification Requirements

- Every risky workflow story must have `TC-*` coverage in `docs/production-readiness-open-story-test-mapping.md`.
- Every risky workflow story must state tenant-mode, RBAC, audit logging, and tenant isolation coverage.
- Missing coverage creates a launch task, blocker, deferred follow-up, or narrowed scope record.
- Production data posture remains No-CUI unless a separate future `CuiReady` approval gate is approved.
