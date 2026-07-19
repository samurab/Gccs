# Production Readiness Launch Candidate Tag

Story: PR-6.2 - Tag Launch Candidate With Evidence Links.

Tag status: created.

Tag date: 2026-07-19.

Tag owner: Engineering lead.

Launch candidate tag: `launch-candidate-2026-07-19-2`.

Tagged commit: `15101d201a7747ce77b2fdcb47cd91cb8c1c77ef`.

Approved launch candidate manifest: `docs/release/approved-launch-candidate.json`.

Tag command:

```bash
git tag launch-candidate-2026-07-19-2 15101d201a7747ce77b2fdcb47cd91cb8c1c77ef
```

This tag is a No-CUI MVP launch candidate marker for solo-controlled pilot testing and project completion. It is not a production deployment approval, production separation-of-duties approval, legal advice, certification evidence, government endorsement, broader customer launch approval, or authorization to accept real CUI.

## Preconditions

| Precondition | Status | Evidence |
| --- | --- | --- |
| Required launch approvals complete | Passed for solo-controlled pilot testing | `docs/production-readiness-launch-approval-record.md` records product owner, engineering lead, security owner, compliance content owner, customer success/support owner, and legal or contracting advisor approval scopes under `docs/production-readiness-approval-posture-addendum.md`. |
| Accepted exceptions recorded | Passed | `docs/production-readiness-launch-gap-decisions.md` records `PR41-RESTORE-001`, `PR43-MALWARE-001`, `PR51-HIGH-RISK-001`, `PR52-CLAIM-001`, and `PR53-SUPPORT-001`. |
| Evidence package gathered | Passed | This artifact links launch approval, closure evidence, staging smoke, staging workflow, staging security, upload/report controls, backup/restore disposition, rollback, content review, support runbooks, release notes, pilot onboarding, and known-risk log. |
| Approved build and deployment path passed | Passed | GitHub Actions staging workflow run `28635229630` completed successfully on branch `codex/production-readiness-pr-6-2-launch-tag` at commit `6c8927ec9cf79de977d76cb2594b87dd48f973bd`. |
| Missing-evidence tag block rule retained | Passed | If any required approval, evidence link, build artifact, deployment artifact, release note, known limitation, support path, staging evidence, rollback plan, or content-scope link is removed, tag creation must be blocked or the tag must be superseded. |

## Build And Deployment Artifacts

| Artifact | Location |
| --- | --- |
| Build artifact source | GitHub Actions run `28635229630`, step `Build staging artifacts`, command `dotnet publish apps/api/Gccs.Api.csproj --configuration Release --output "$RUNNER_TEMP/gccs-api-staging"` and `npm run build:web`. |
| API deployment artifact | GitHub Actions run `28635229630`, job `Deploy staging`, step `Deploy staging API App Service`, package `$RUNNER_TEMP/gccs-api-staging`, app `gccs-api-staging-19984`. |
| Web deployment artifact | GitHub Actions run `28635229630`, job `Deploy staging`, step `Deploy staging Static Web App`, app location `apps/web/dist`, Static Web App `gccs-web-staging-19984`. |
| Migration artifact | GitHub Actions run `28635229630`, step `Generate idempotent migration script`, output `$RUNNER_TEMP/gccs-staging-migrations.sql`; persisted reference `output/production-readiness/deployment-migration-rollback/gccs-staging-migrations.sql`. |
| Smoke artifact | GitHub Actions run `28635229630`, artifact `staging-smoke-test-results`, file `staging-health.json`. |
| Deployment run URL | `https://github.com/samurab/Gccs/actions/runs/28635229630` |

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
- `PR41-RESTORE-001` means restore rehearsal was not executed. Launch-candidate tagging can proceed, but production customer launch and restore-capability claims remain blocked until actual restore evidence is attached or separately dispositioned.
- `PR43-MALWARE-001` remains time-boxed; external scanner endpoint evidence remains due before exception expiration.
- The tag points to the deployed PR-6.1 approval commit. This PR-6.2 record is a follow-on governance record and does not change runtime application behavior.
- If release notes, support runbooks, claim language, content scope, or accepted risks change after this tag, the tag must be superseded or re-approved under the same solo-controlled pilot posture or under a future production separation-of-duties approval model.
