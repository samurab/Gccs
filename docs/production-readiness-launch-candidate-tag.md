# Production Readiness Launch Candidate Tag

Story: PR-6.2 - Tag Launch Candidate With Evidence Links.

Tag status: created.

Tag date: 2026-08-18.

Tag owner: Engineering lead.

Launch candidate tag: `launch-candidate-2026-08-18-1`.

Tagged commit: `d49f594f8277aacb501c9ad4c8906960750eebf2`.

Approved launch candidate manifest: `docs/release/approved-launch-candidate.json`.

Tag command:

```bash
git tag launch-candidate-2026-08-18-1 d49f594f8277aacb501c9ad4c8906960750eebf2
```

This tag is a No-CUI MVP launch candidate marker for solo-controlled pilot testing and project completion. It is not a production deployment approval, production separation-of-duties approval, legal advice, certification evidence, government endorsement, broader customer launch approval, or authorization to accept real CUI.

## Preconditions

| Precondition | Status | Evidence |
| --- | --- | --- |
| Required launch approvals complete | Passed for solo-controlled pilot testing | `docs/production-readiness-launch-approval-record.md` records product owner, engineering lead, security owner, compliance content owner, customer success/support owner, and legal or contracting advisor approval scopes under `docs/production-readiness-approval-posture-addendum.md`. |
| Accepted exceptions recorded | Passed | `docs/production-readiness-launch-gap-decisions.md` records `PR41-RESTORE-001`, `PR43-MALWARE-001`, `PR51-HIGH-RISK-001`, `PR52-CLAIM-001`, and `PR53-SUPPORT-001`. |
| Evidence package gathered | Passed | This artifact links launch approval, closure evidence, staging smoke, staging workflow, staging security, upload/report controls, backup/restore disposition, rollback, content review, support runbooks, release notes, pilot onboarding, and known-risk log. |
| Approved build and deployment path passed | Passed | PR #57 CI run `31907508233`, exact-candidate main CI run `31910692965`, exact-candidate staging workflow run `31910692946`, and Static Web Apps run `31910692991` completed successfully for candidate commit `50b2dd279f216f816b92fdbaf2c4d4be025ce4ea`. |
| Missing-evidence tag block rule retained | Passed | If any required approval, evidence link, build artifact, deployment artifact, release note, known limitation, support path, staging evidence, rollback plan, or content-scope link is removed, tag creation must be blocked or the tag must be superseded. |

## Build And Deployment Artifacts

### 2026-08-15 audit-workspace candidate refresh

The candidate-specific approval is recorded in `docs/production-readiness-launch-approval-record.md`. PR #57 CI run `31907508233`, exact-candidate main CI run `31910692965`, staging run `31910692946`, and Static Web Apps run `31910692991` completed successfully. The candidate improves tenant-scoped audit discovery and display, authenticated workspace links, active tenant-member assignee resolution, responsive audit UI behavior, and focused regression coverage; it introduces no EF Core model or migration change.

| Refreshed artifact | Location |
| --- | --- |
| Candidate source | PR #57 merge commit `50b2dd279f216f816b92fdbaf2c4d4be025ce4ea` |
| Delta from prior production candidate | Audit entity-type endpoint/catalog, descriptive audit metadata, authenticated-workspace notification links, active tenant-membership assignee lookup, responsive audit filter UI, local development host binding, and focused backend/frontend tests. No EF Core migration file changed. |
| Pull-request CI | GitHub Actions run `31907508233` |
| Exact-candidate main CI | GitHub Actions run `31910692965` |
| Exact-candidate staging deployment | GitHub Actions run `31910692946` |
| Static Web Apps deployment | GitHub Actions run `31910692991` |
| Staging smoke artifact | Run `31910692946`, artifact `9253616864` (`staging-smoke-test-results`) |

| Artifact | Location |
| --- | --- |
| Build artifact source | Exact-candidate staging run `31910692946`, job `Deploy staging`, step `Build staging artifacts`; main CI run `31910692965` independently built and tested the same commit. |
| API deployment artifact | Exact-candidate staging run `31910692946`, job `Deploy staging`, step `Deploy staging API App Service`. |
| Web deployment artifact | Exact-candidate staging run `31910692946`, job `Deploy staging`, step `Deploy staging Static Web App`; Static Web Apps run `31910692991` independently processed the same web commit. |
| Migration artifact | Exact-candidate staging run `31910692946`, step `Generate idempotent migration script`; EF migration validation passed and the candidate contains no EF Core migration file change. |
| Smoke artifact | Exact-candidate staging run `31910692946`, artifact `9253616864`, file `staging-health.json`. |
| Deployment run URL | `https://github.com/samurab/Gccs/actions/runs/31910692946` |

### 2026-08-03 demo-scheduler and discovery-asset candidate refresh

The candidate-specific approval is recorded in `docs/production-readiness-launch-approval-record.md`. PR #36 CI run `30862127900`, PR #35 CI run `30863257848`, and exact-candidate staging run `30864030411` completed successfully. The candidate reuses the existing scheduled-demo workflow on the post-video `/demo` call to action and adds reviewed discovery/UAT assets; it introduces no EF Core migration file changes.

| Refreshed artifact | Location |
| --- | --- |
| Candidate source | PR #36 squash merge `17616fb139762812185e93c50ef6a59ea68e0294` plus PR #35 squash merge `fec0276b6d2cba3629a874f9cf76cd6e5f6a36da` |
| Delta from prior production candidate | Post-video scheduler CTA and focused interaction test; frontend transitive dependency audit updates; two reviewed marketing PDFs; UAT acceptance instructions; browser-artifact ignore rule. No EF Core migration file changed. |
| Pull-request CI | GitHub Actions runs `30862127900` and `30863257848` |
| Exact-candidate staging deployment | GitHub Actions run `30864030411` |
| Staging smoke artifact | Run `30864030411`, artifact `staging-smoke-test-results` |
| Hosted interaction review | Exact-candidate staging `/demo`: one scheduler dialog opened, first-name focus transferred, and one `datetime-local` field exposed; no request was submitted. |

### 2026-08-02 FeDril demo-video candidate refresh

The candidate-specific approval is recorded in `docs/production-readiness-launch-approval-record.md` and `marketing/demo-video/QA-CHECKLIST.md`. Main staging run `30757029225` and Static Web Apps run `30757029209` deployed merge commit `85fb7a7c2d9fcfbaf5aef5abbbaed019032bbd94` successfully. Main CI run `30757029213` completed successfully and satisfied the blocking pre-tag gate.

| Refreshed artifact | Location |
| --- | --- |
| Candidate source | PR #26 squash merge `85fb7a7c2d9fcfbaf5aef5abbbaed019032bbd94` |
| Delta from prior production candidate | 90 paths from `launch-candidate-2026-08-01-1`: editable Playwright/Remotion demo pipeline, fictional Northstar Development seed support, generated narration/captions, flagship/homepage/social media, public landing and `/demo` integration, mobile playback source, and focused frontend/backend fixes and tests. No EF Core migration file changed. |
| Main CI | GitHub Actions run `30757029213` |
| Staging deployment | GitHub Actions run `30757029225` |
| Static Web Apps deployment | GitHub Actions run `30757029209` |
| Staging smoke artifact | Run `30757029225`, artifact `staging-smoke-test-results` |
| Demo publication review | `marketing/demo-video/QA-CHECKLIST.md`, strict narration/media validation, and exact-candidate hosted desktop/mobile Playwright verification |

| Artifact | Location |
| --- | --- |
| Build artifact source | Exact-candidate staging run `30757029225`, job `Deploy staging`, step `Build staging artifacts`; main CI run `30757029213` independently built and tested the same commit. |
| API deployment artifact | Exact-candidate staging run `30757029225`, job `Deploy staging`, step `Deploy staging API App Service`. |
| Web deployment artifact | Exact-candidate staging run `30757029225`, job `Deploy staging`, step `Deploy staging Static Web App`; Static Web Apps run `30757029209` independently built and deployed the same web commit. |
| Migration artifact | Exact-candidate staging run `30757029225`, step `Generate idempotent migration script`; the delta from `launch-candidate-2026-08-01-1` contains no EF Core migration file change. |
| Smoke artifact | Exact-candidate staging run `30757029225`, artifact `staging-smoke-test-results`, file `staging-health.json`. |
| Deployment run URL | `https://github.com/samurab/Gccs/actions/runs/30757029225` |

## Evidence Package

| Evidence area | Link |
| --- | --- |
| Launch approval record | `docs/production-readiness-launch-approval-record.md` |
| Approval posture addendum | `docs/production-readiness-approval-posture-addendum.md` |
| Launch closure evidence | `docs/production-readiness-launch-closure-evidence.md` |
| Release notes | `docs/production-readiness-release-notes.md` |
| Known limitations and accepted risks | `docs/production-readiness-launch-gap-decisions.md` |
| Support paths | `docs/production-readiness-support-runbooks.md` |
| Pilot onboarding | `docs/production-readiness-pilot-onboarding.md` |
| Staging smoke evidence | `docs/production-readiness-staging-smoke-evidence.md` |
| Staging MVP workflow evidence | `docs/production-readiness-staging-workflow-evidence.md` |
| Staging tenant isolation and RBAC evidence | `docs/production-readiness-staging-security-evidence.md` |
| Staging upload guardrail and report-control evidence | `docs/production-readiness-staging-upload-report-evidence.md` |
| Backup and restore disposition | `docs/production-readiness-backup-restore-evidence.md` |
| Deployment, migration, and rollback evidence | `docs/production-readiness-deployment-migration-rollback-evidence.md` |
| Malware scanning decision | `docs/production-readiness-malware-scanning-decision.md` |
| Customer-facing claims review | `docs/production-readiness-customer-claims-review.md` and `output/production-readiness/customer-claims-review.json` |
| Demo-video publication review | `marketing/demo-video/QA-CHECKLIST.md` |
| Content review evidence | `output/production-readiness/expert-content/staging-content-review-summary.json` and `output/production-readiness/expert-content/high-risk-obligation-review.json` |
| Content scope | `packages/compliance-content/obligations/mvp.json` |

## Smoke Verification

| Test case | Result | Evidence |
| --- | --- | --- |
| TC-PR-6.2.1 | Passed | Evidence package and required approval scopes are complete under the solo-controlled pilot posture before launch candidate tagging. |
| TC-PR-6.2.2 | Passed | Tag record includes tag, commit, build artifact, deployment artifact, migration artifact, smoke artifact, and evidence package location. |
| TC-PR-6.2.3 | Passed | Release notes, known limitations, support paths, staging evidence, rollback plan, and content scope are linked. |
| TC-PR-6.2.4 | Passed | Missing approval or missing evidence remains a blocking condition; no tag may be created or retained if required links are removed. |

## Consequences And Limitations

- The same user approved all six PR-6.1 role scopes for solo-controlled pilot testing and project completion only. This is not production separation-of-duties approval.
- `PR41-RESTORE-001` was closed by the 2026-07-05 staging point-in-time restore rehearsal. That evidence supports only the tested staging path; it does not prove geo-disaster recovery or production customer-data restore.
- Production scanner evidence is attached for the private ClamAV-compatible path. A single scanner instance is accepted only for this controlled No-CUI pilot and remains a broader-launch hardening limitation.
- The tag points to PR #57 merge commit `50b2dd279f216f816b92fdbaf2c4d4be025ce4ea`; exact-candidate CI and staging passed, and the candidate introduces no EF Core migration file changes.
- The 86.41 MB flagship MP4 is below GitHub's hard file-size limit but above its recommended 50 MB threshold; durable object/media storage remains the correct scaling path for future revisions.
- If release notes, support runbooks, claim language, content scope, or accepted risks change after this tag, the tag must be superseded or re-approved under the same solo-controlled pilot posture or under a future production separation-of-duties approval model.
