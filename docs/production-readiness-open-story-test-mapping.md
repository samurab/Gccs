# Production Readiness Open Story Test Mapping

Review status: Complete.

Review date: 2026-06-26.

Review owner: QA owner.

Source artifacts:

- `docs/production-readiness-open-story-readiness-review.md`
- `docs/production-readiness-story-test-cases.md`
- `docs/Smoke_Test_production-readiness-story-test-prompts.md`
- `docs/Automated_production-readiness-story-test-prompts.md`
- `docs/development-story-test-cases.md`

Mapping rule: every open launch story must reference applicable `TC-*` cases or document why no test case applies. Stories affecting tenant isolation, RBAC, uploads, reports, evidence, imports, exports, search, AI, extraction, or background jobs must include tenant-mode coverage or remain blocked/deferred.

No story in this mapping expands production data posture beyond No-CUI. Any future story that expands data posture beyond No-CUI is rejected unless a separate `CuiReady` approval gate exists and is approved.

| Story ID | Test case mapping | Primary coverage | Risky workflow coverage | Tenant-mode coverage | Coverage disposition |
| --- | --- | --- | --- | --- | --- |
| PR-1.1 | TC-PR-1.1.1; TC-PR-1.1.2; TC-PR-1.1.3; TC-PR-1.1.4 | Document validation, governance artifact, API test project | Readiness ambiguity gate | No-CUI ambiguity blocked | Covered by readiness review tests. |
| PR-1.2 | TC-PR-1.2.1; TC-PR-1.2.2; TC-PR-1.2.3; TC-PR-1.2.4 | Document validation, mapping artifact, API test project | Test coverage gate | Tenant-mode coverage required for risky workflows | Covered by this mapping and tests. |
| PR-1.3 | TC-PR-1.3.1; TC-PR-1.3.2; TC-PR-1.3.3; TC-PR-1.3.4 | Document validation, launch task/blocker checks | Upload, import, export, search, AI, evidence, report, extraction changes | Required before risky workflow acceptance | Launch task required for missing coverage. |
| PR-2.1 | TC-PR-2.1.1; TC-PR-2.1.2; TC-PR-2.1.3; TC-PR-2.1.4 | Document validation, scope freeze checks | Scope expansion gate | No-CUI posture cannot expand | Covered by scope-freeze artifact. |
| PR-2.2 | TC-PR-2.2.1; TC-PR-2.2.2; TC-PR-2.2.3; TC-PR-2.2.4 | Document validation, completion evidence checks | Completed workflow gap discovery | CUI/data-handling gaps become blockers | Launch task required for missing tests. |
| PR-2.3 | TC-PR-2.3.1; TC-PR-2.3.2; TC-PR-2.3.3; TC-PR-2.3.4 | Document validation, launch decision checks | Gap disposition gate | No-CUI gaps blocked or accepted by approver | Launch blocker/risk/defer record required. |
| PR-3.1 | TC-PR-3.1.1; TC-PR-3.1.2; TC-PR-3.1.3; TC-PR-3.1.4 | Staging smoke, health endpoint, deployment evidence | Staging deployment | Staging must report No-CUI posture | Manual staging evidence required. |
| PR-3.2 | TC-PR-3.2.1; TC-PR-3.2.2; TC-PR-3.2.3; TC-PR-3.2.4 | Staging end-to-end workflow | Upload, evidence, report, audit workflow | Blocked real CUI and synthetic-only data required | Manual staging evidence required. |
| PR-3.3 | TC-PR-3.3.1; TC-PR-3.3.2; TC-PR-3.3.3; TC-PR-3.3.4 | Staging security checks | Tenant isolation and RBAC direct API bypass | Tenant mode cannot be bypassed | Manual staging evidence required. |
| PR-3.4 | TC-PR-3.4.1; TC-PR-3.4.2; TC-PR-3.4.3; TC-PR-3.4.4 | Staging upload/report checks | Upload guardrails, report/export claims | Real CUI blocked; demo data synthetic/redacted | Manual staging evidence required. |
| PR-4.1 | TC-PR-4.1.1; TC-PR-4.1.2; TC-PR-4.1.3; TC-PR-4.1.4 | Backup/restore evidence checks | Restore path | Restored data must remain No-CUI | Manual restore evidence required. |
| PR-4.2 | TC-PR-4.2.1; TC-PR-4.2.2; TC-PR-4.2.3; TC-PR-4.2.4 | Deployment, migration, rollback evidence checks | Migration and rollback path | Migration must not expand data posture | Manual evidence required. |
| PR-4.3 | TC-PR-4.3.1; TC-PR-4.3.2; TC-PR-4.3.3; TC-PR-4.3.4 | Malware scanner decision checks | Upload scanning | Scanner path must preserve No-CUI limits | Launch decision required. |
| PR-5.1 | TC-PR-5.1.1; TC-PR-5.1.2; TC-PR-5.1.3; TC-PR-5.1.4 | Content governance checks | Obligation content publication | Content cannot imply CUI authorization | SME/legal review evidence required. |
| PR-5.2 | TC-PR-5.2.1; TC-PR-5.2.2; TC-PR-5.2.3; TC-PR-5.2.4 | Claims review checks | Customer-facing claims | Future `CuiReady` remains separately gated | Legal/contracting review evidence required. |
| PR-5.3 | TC-PR-5.3.1; TC-PR-5.3.2; TC-PR-5.3.3; TC-PR-5.3.4 | Support runbook checks | Support escalation | Suspected CUI runbook preserves No-CUI | Support/security signoff required. |
| PR-5.4 | TC-PR-5.4.1; TC-PR-5.4.2; TC-PR-5.4.3; TC-PR-5.4.4 | Onboarding, release notes, known-risk checks | Pilot onboarding and claims | Pilot onboarding prohibits real CUI | Known-risk log required. |
| PR-6.1 | TC-PR-6.1.1; TC-PR-6.1.2; TC-PR-6.1.3; TC-PR-6.1.4 | Approval evidence checks | Launch authorization | Approvers confirm No-CUI posture | Approval evidence required. |
| PR-6.2 | TC-PR-6.2.1; TC-PR-6.2.2; TC-PR-6.2.3; TC-PR-6.2.4 | Launch candidate tag checks | Release traceability | Tag evidence preserves No-CUI posture | Tag/evidence links required. |
| PR-7.1 | TC-PR-7.1.1; TC-PR-7.1.2; TC-PR-7.1.3; TC-PR-7.1.4 | Production deployment checks | Approved CI/CD deployment | Production remains No-CUI | Production deployment evidence required. |
| PR-7.2 | TC-PR-7.2.1; TC-PR-7.2.2; TC-PR-7.2.3; TC-PR-7.2.4 | Production smoke checks | Login, RBAC, upload, evidence, report, audit, logs, alerts, health | Real CUI upload remains blocked | Production smoke evidence required. |
| PR-7.3 | TC-PR-7.3.1; TC-PR-7.3.2; TC-PR-7.3.3; TC-PR-7.3.4 | Pilot onboarding checks | Pilot access and onboarding | Pilot customers acknowledge No-CUI limits | Pilot acceptance evidence required. |
| PR-8.1 | TC-PR-8.1.1; TC-PR-8.1.2; TC-PR-8.1.3; TC-PR-8.1.4 | Pilot monitoring checks | Health, logs, support, upload blocks, permission denials, reports, content disputes | Suspected CUI signals escalated | Daily monitoring evidence required. |
| PR-8.2 | TC-PR-8.2.1; TC-PR-8.2.2; TC-PR-8.2.3; TC-PR-8.2.4 | Post-launch review checks | Findings and disposition | No-CUI or tenant-mode issues block expansion | Review evidence required. |
| PR-8.3 | TC-PR-8.3.1; TC-PR-8.3.2; TC-PR-8.3.3; TC-PR-8.3.4 | Phase 2 gate checks | Expansion gate | No CUI posture expansion without approval | Phase 2 decision evidence required. |

## Coverage Gaps As Launch Tasks

| Coverage area | Gap status | Launch task or blocker |
| --- | --- | --- |
| Unit | Covered where code-level behavior changes; governance-only stories use document validation. | Add unit tests when a story changes domain or application logic. |
| Integration | Covered for API/report/upload paths where executable behavior changes. | Add integration tests when persistence, queues, external APIs, or background jobs change. |
| API | Required for server-side RBAC, upload, report, evidence, tenant-mode, and direct API bypass stories. | Block launch if high-risk API behavior lacks direct API tests. |
| Frontend | Not required for current governance-only PR-1.2 change. | Add frontend tests when a story changes `apps/web` user-visible behavior. |
| Staging | Required for PR-3.1 through PR-3.4. | Manual staging evidence is a launch task and cannot be skipped. |
| Tenant isolation | Required for PR-0.3, PR-3.3, risky workflow changes, reports, evidence, exports, and direct API bypass paths. | Block or defer if tenant isolation coverage is missing. |
| RBAC | Required for protected upload, report, evidence, audit, tenant, support, and deployment actions. | Block or defer if RBAC coverage is missing for a protected action. |
| Upload | Required for contract documents, evidence, wage determinations, malware scanning, and prohibited data paths. | Block or defer if No-CUI upload guardrails are not tested. |
| Report | Required for evidence packages, compliance reports, CMMC readiness reports, exports, and claim controls. | Block or defer if report/export paths can process CUI or unsupported claims. |
| Audit | Required for security-sensitive mutations, denied restricted workflows, uploads, exports, approvals, mode changes, and launch decisions. | Block or defer if required audit events are missing. |

## Risky Workflow Tenant-Mode Coverage

| Workflow | Required tenant-mode coverage | Current mapping |
| --- | --- | --- |
| Upload | Allowed non-CUI upload, blocked real CUI, blocked prohibited content, audit of allowed/blocked events. | PR-0.3, PR-3.4, PR-4.3, PR-7.2. |
| Evidence | Evidence metadata and package generation must reject CUI in `NoCui` and synthetic CUI outside `DemoSandbox`. | PR-0.3, PR-3.2, PR-7.2. |
| Report/export | Reports and exports must re-check tenant mode and avoid unsupported claims. | PR-0.3, PR-3.4, PR-7.2. |
| Import | Imports must classify data and block real CUI outside future approved `CuiReady`. | PR-1.3 and any future import story. |
| Extraction/background jobs | Queued processing must carry tenant ID and block CUI-classified records for `NoCui`. | PR-0.3, PR-3.2, PR-3.3. |
| Search/AI | Future search or AI processing must enforce tenant mode before indexing, retrieval, or generation. | PR-1.3 and future Phase 2+ stories. |
