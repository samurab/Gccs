# Production Readiness Staging Upload And Report Evidence

Story: PR-3.4 - Verify Upload Guardrails And Report Controls In Staging.

Evidence status: Complete. Authenticated staging upload guardrail and report-control smoke checks passed on 2026-07-02 using the signed-in in-app browser session for the `GCCS Staging` tenant.

Review date: 2026-07-02.

Staging API: `https://gccs-api-staging-19984.azurewebsites.net`.

Staging app: `https://mango-rock-016ff040f.7.azurestaticapps.net`.

Sanitized current evidence:

- `output/playwright/production-readiness/pr-3.4/staging-health.json`
- `output/playwright/production-readiness/pr-3.4/authenticated-upload-report-smoke.json`

Historical blocker evidence:

- `output/playwright/production-readiness/pr-3.4/authentication-blocker.json`

## Authenticated Staging Result

The authenticated smoke ran through the browser session without storing a token in the repository. The current artifact records `tokenCapturedInArtifact: false`, `containsCustomerData: false`, and `containsCui: false`.

Verified staging facts:

- Signed-in user had Owner role with `ManageEvidence`, `ViewReports`, and `ViewAuditLog`.
- Tenant No-CUI acknowledgement was present for `no-cui-mvp-v1`, and the warning text stated the tenant is not ready to store CUI.
- Unauthenticated report generation was denied with `401 authentication_required`.
- Synthetic evidence metadata was created with `Unclassified` classification.
- Allowed synthetic No-CUI upload returned `201`, `validationStatus: accepted`, `malwareScanStatus: scan-pending`, and `isUsable: false`.
- Missing per-file No-CUI attestation was rejected with validation key `noCuiAttestation`.
- Disallowed `.exe` file type was rejected with validation key `fileType`.
- Oversized upload was rejected with validation key `sizeBytes`.
- Potential or real CUI upload was blocked for the No-CUI tenant with `tenant_data_handling_mode_restricted`.
- Prohibited classification update was blocked with `content_classification_invalid`.
- Accepted upload and blocked upload attempts were visible in tenant audit logs.
- Compliance status report generation was tenant-scoped and contained no affirmative certification, legal-advice, government-endorsement, approval, or CUI-storage claim.
- Contract obligation matrix report and export included source metadata: clause source URL, clause last-reviewed date, obligation source URL, and obligation last-reviewed date.
- CMMC readiness report generated with draft/readiness language and no affirmative prohibited claims.

## Smoke Test Disposition

| Test case | Disposition | Evidence | Blocker |
| --- | --- | --- | --- |
| TC-PR-3.4.1 | Passed in authenticated staging. | `authenticated-upload-report-smoke.json` No-CUI acknowledgement and allowed upload checks | None |
| TC-PR-3.4.2 | Passed in authenticated staging. | `authenticated-upload-report-smoke.json` missing attestation, disallowed file type, oversized upload, real CUI, prohibited classification, and audit checks | None |
| TC-PR-3.4.3 | Passed in authenticated staging. | `authenticated-upload-report-smoke.json` unauthenticated report denial, tenant-scoped compliance report, obligation matrix report/export source metadata, and CMMC readiness draft language | None |
| TC-PR-3.4.4 | Passed in authenticated staging. | `authenticated-upload-report-smoke.json` prohibited-claim checks | None |

## Resolved Blocker

| Blocker ID | Summary | Owner | Severity | Required resolution | Current status |
| --- | --- | --- | --- | --- | --- |
| PR34-STAGE-001 | Authenticated staging PR-3.4 upload guardrail and report-control checks could not run without a signed-in staging session or approved non-interactive smoke credential. | QA owner | High | Provide a signed-in in-app browser staging session, staging-only smoke identity, or approved API token; run synthetic-only upload acknowledgement, blocked upload, report generation, report RBAC, source metadata, and prohibited-claim checks; attach sanitized outputs. | Closed on 2026-07-02. Signed-in in-app browser staging session was provided, synthetic-only authenticated smoke checks passed, and sanitized outputs are attached. |

## Hidden Risks And Edge Cases

- CMMC readiness source reference count is zero in current staging data because the assessment has no populated control statuses. PR-3.4 source metadata was verified through the contract obligation matrix report/export, which is the report path where obligation source links and last-reviewed dates appear.
- Accepted upload audit entries are keyed to `EvidenceFileVersion`, while rejected guardrail entries are keyed to `EvidenceUploadIntent` or `TenantDataHandlingModePolicy`. Future audit checks must include related entity types instead of filtering only on `EvidenceItem`.
- The smoke created synthetic staging records and object-storage versions. Cleanup was not performed because upload and guardrail audit history is append-only launch evidence.

## Launch Disposition

PR-3.4 is complete. The production readiness sequence can proceed to PR-4.1 next.

This evidence does not expand the No-CUI posture and does not authorize real CUI handling. Production launch remains blocked by unrelated open launch blockers, including restore rehearsal, malware scanning disposition, high-risk expert content review, and final launch approvals.
