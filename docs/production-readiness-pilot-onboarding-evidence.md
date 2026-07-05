# Production Readiness Pilot Onboarding Evidence

Story: PR-7.3 - Onboard Controlled Pilot Customers.

Evidence date: 2026-07-05.

Evidence owner: Customer success/support owner.

Review status: controlled pilot onboarding authorized for approved pilot cohort using non-sensitive identifiers only.

Authorization source: user-provided approved pilot customer identifiers/cohort and onboarding authorization on 2026-07-05.

Sanitized evidence artifact: `output/playwright/production-readiness/pr-7.3/pilot-onboarding-evidence.json`.

## Architectural Assessment

Pilot onboarding cannot be treated as a general production customer launch. That pattern fails structurally because the launch package still carries residual restore rehearsal and alert-route dependencies, and because uncontrolled onboarding would expand support, tenant, and data-handling risk before first-use monitoring is active.

Three failure modes addressed:

- Onboarding before a reviewed production smoke pass can admit pilot users into a production tenant with broken login, RBAC denial, upload controls, report generation, audit logging, scanner-backed upload, logs, alerts, or health checks.
- Recording real customer names, emails, domains, contract names, files, or CUI in readiness artifacts can leak customer metadata and violate the No-CUI MVP evidence posture.
- Creating pilot tenants without explicit tenant mode, role matrix, support routing, acknowledgement status, and first-use monitoring can produce cross-tenant support ambiguity, privilege drift, and unowned launch regressions.

The corrected pattern is a limited pilot-onboarding gate: only approved pilot cohort members are admitted, only pseudonymous identifiers are recorded in the repository, every pilot tenant remains `NoCui`, every pilot receives No-CUI/prohibited-data/support/limitations materials, and first workflow monitoring is active before use begins.

## Entry Gate

| Gate | Evidence | Result |
| --- | --- | --- |
| Production smoke pass | `docs/production-readiness-production-smoke-evidence.md` records `Smoke status: passed for PR-7.2 scanner-backed production smoke` and `Current gate result: passed for PR-7.2`. | Passed |
| Synthetic/non-sensitive evidence posture | PR-7.2 authenticated smoke artifact records `containsCustomerData=false`, `containsCui=false`, and `tokenCapturedInArtifact=false`. | Passed |
| Approved pilot cohort | Product/customer-success authorization was provided on 2026-07-05. Repository artifacts use only `PILOT-001` and `PILOT-002`. | Passed |
| Residual launch dependencies acknowledged | `PR41-RESTORE-001` and `PR72-ALERT-ROUTE-001` remain open dependencies for broader production customer launch or related claims. | Passed with limitations |

## Pilot Cohort Checklist

| Pilot ID | Tenant mode | Required roles verified | Onboarding materials delivered | No-CUI acknowledgement workflow | Support route active | First workflow monitoring | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| PILOT-001 | NoCui | Owner, Admin, Compliance Manager, Contributor, Auditor, Advisor | No-CUI guidance, prohibited data examples, support paths, known limitations, synthetic demo scope | Required before evidence/document upload and per-file attestation | `docs/production-readiness-support-runbooks.md` | Monitor first company profile, contract metadata, obligation/task, allowed evidence, report, and audit-log events | Authorized for controlled pilot |
| PILOT-002 | NoCui | Owner, Admin, Compliance Manager, Contributor, Auditor, Advisor | No-CUI guidance, prohibited data examples, support paths, known limitations, synthetic demo scope | Required before evidence/document upload and per-file attestation | `docs/production-readiness-support-runbooks.md` | Monitor first company profile, contract metadata, obligation/task, allowed evidence, report, and audit-log events | Authorized for controlled pilot |

## Smoke Verification

| Test case | Result | Evidence |
| --- | --- | --- |
| TC-PR-7.3.1 | Passed | Pilot onboarding begins only after the reviewed PR-7.2 production smoke pass and scanner-backed byte upload evidence. |
| TC-PR-7.3.2 | Passed | `docs/production-readiness-pilot-onboarding.md` provides No-CUI guidance, prohibited data examples, support paths, known limitations, and synthetic demo limits. |
| TC-PR-7.3.3 | Passed | Pilot checklist records `NoCui` tenant mode, required role set, support route, and acknowledgement workflow for each pseudonymous pilot ID. |
| TC-PR-7.3.4 | Passed | First workflow monitoring is required for each pilot tenant using non-sensitive identifiers only. |

## First-Use Monitoring

For each pilot tenant, monitor and record only non-sensitive identifiers:

- Tenant ID or pilot ID, not customer legal name or domain.
- Actor role, not personal email unless stored in the production system of record and omitted from readiness artifacts.
- Workflow step: company profile, contract metadata, obligation/task, allowed evidence, report, audit log.
- Result, timestamp, support ticket ID if applicable, severity, owner, mitigation, and target date.
- No raw file contents, real CUI, contract documents, unrestricted logs, secrets, credentials, or sensitive personal data.

## Limitations

- This evidence authorizes controlled pilot onboarding only; it does not close `PR41-RESTORE-001`.
- This evidence does not prove external alert owner notification receipt; `PR72-ALERT-ROUTE-001` remains open.
- This evidence does not authorize real CUI, classified information, ITAR/export-controlled technical data, sensitive government-furnished information, credentials, payroll, SSNs, health data, or unrestricted security logs.
- This evidence does not authorize broader production customer launch, marketing claims, CMMC certification claims, legal advice, government endorsement, or recoverability claims.

## Hidden Risks And Edge Cases

- A pilot tenant can drift from `NoCui` if tenant setup bypasses the approved provisioning path; verify tenant mode before first workflow use.
- Pilot users may paste sensitive data into free-text fields despite upload guardrails; support and monitoring must watch for suspected CUI or prohibited-data signals without copying content into tickets.
- The role matrix can degrade after onboarding through manual membership edits; first-use monitoring must include permission-denial and audit-log review.
- Support routes can fail operationally even when documented; Severity 1 and suspected CUI paths must be exercised through real support ownership during pilot.
