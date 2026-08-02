# FeDril demo capability and repository audit

Audit date: 2026-07-31
Scope: read-only architecture, feature, test, asset, and local-tool inspection performed before pipeline implementation.

## Architecture inventory

| Area | Finding | Evidence |
| --- | --- | --- |
| ASP.NET Core API | The API host is `apps/api`; endpoints, authentication, tenant context, membership authorization, permissions, response shaping, and Development bootstrap are composed in the API layer. | [`../../apps/api/Gccs.Api.csproj`](../../apps/api/Gccs.Api.csproj), [`../../apps/api/Program.cs`](../../apps/api/Program.cs) |
| React/Vite frontend | The presentation application is `apps/web`; the current route shell and feature views are composed in `App.tsx`. | [`../../apps/web/package.json`](../../apps/web/package.json), [`../../apps/web/src/App.tsx`](../../apps/web/src/App.tsx) |
| Application layer | Use cases, DTOs, ports, and workflow services are separated from the API. | [`../../src/Gccs.Application/Gccs.Application.csproj`](../../src/Gccs.Application/Gccs.Application.csproj) |
| Domain layer | Framework-independent compliance, evidence, identity, report, and tenancy models are separated from infrastructure. | [`../../src/Gccs.Domain/Gccs.Domain.csproj`](../../src/Gccs.Domain/Gccs.Domain.csproj) |
| Infrastructure layer | EF Core persistence, repositories, and migrations reside in infrastructure. | [`../../src/Gccs.Infrastructure/Gccs.Infrastructure.csproj`](../../src/Gccs.Infrastructure/Gccs.Infrastructure.csproj), [`../../src/Gccs.Infrastructure/Persistence/GccsDbContext.cs`](../../src/Gccs.Infrastructure/Persistence/GccsDbContext.cs) |
| Authentication and demo user | The API uses explicitly gated Development authentication locally and JWT bearer authentication with configured authority/audience outside that mode. The demo launcher selects fictional Priya Shah in the Northstar tenant and keeps membership authorization enabled. | [`../../apps/api/Security/ApiSecurityExtensions.cs`](../../apps/api/Security/ApiSecurityExtensions.cs), [`../../apps/api/Program.cs`](../../apps/api/Program.cs), [`scripts/start-api.sh`](./scripts/start-api.sh), [`scripts/start-web.sh`](./scripts/start-web.sh) |
| Existing database seeding | A hosted Development bootstrap exists. Northstar data is separately gated by `MarketingDemo:Enabled`, uses deterministic records, and performs collision preflight checks. | [`../../apps/api/LocalDevelopment/DevelopmentTenantBootstrapper.cs`](../../apps/api/LocalDevelopment/DevelopmentTenantBootstrapper.cs), [`../../tests/Gccs.Api.Tests/DevelopmentSeedDataTests.cs`](../../tests/Gccs.Api.Tests/DevelopmentSeedDataTests.cs) |
| End-to-end testing | Mocked-browser and real-stack Playwright suites already exist; the marketing capture adds a dedicated 1920x1080, single-worker real-stack configuration. | [`../../playwright.config.ts`](../../playwright.config.ts), [`../../playwright.real.config.ts`](../../playwright.real.config.ts), [`../../apps/web/e2e-real/`](../../apps/web/e2e-real/), [`capture/playwright.config.ts`](./capture/playwright.config.ts) |
| Branding assets | The existing company logo, favicon, and hero image are reusable; no government badge or certification mark is present or needed. | [`../../apps/web/public/company-logo.svg`](../../apps/web/public/company-logo.svg), [`../../apps/web/public/favicon.svg`](../../apps/web/public/favicon.svg), [`../../apps/web/public/landing/compliance-operations-hero.png`](../../apps/web/public/landing/compliance-operations-hero.png) |

The current Clean Architecture boundaries are suitable for this work. Demo orchestration belongs in `marketing/demo-video`; only the Development bootstrap and capture-safe presentation mode need product-layer awareness. Business writes continue through existing endpoints and services.

## Demonstrable feature inventory

### Implemented and evidence-backed

| Feature | Approved demonstration | Implementation/test evidence |
| --- | --- | --- |
| Readiness dashboard | Show tenant-scoped overview signals, overdue work, risk, and No-CUI posture. | [`../../apps/web/src/App.tsx`](../../apps/web/src/App.tsx), [`../../apps/api/Program.cs`](../../apps/api/Program.cs) |
| Requirements/obligations | Show the queue, source reference, detail, status, risk, expected evidence, and related work. | [`../../apps/web/src/App.tsx`](../../apps/web/src/App.tsx), [`../../tests/Gccs.Api.Tests/ObligationDashboardTests.cs`](../../tests/Gccs.Api.Tests/ObligationDashboardTests.cs), [`../../tests/Gccs.Api.Tests/ObligationDetailTests.cs`](../../tests/Gccs.Api.Tests/ObligationDetailTests.cs) |
| Requirement ownership | Assign the selected obligation to an eligible Northstar member through the existing owner endpoint. | [`../../apps/web/e2e-real/obligation-assignment.spec.ts`](../../apps/web/e2e-real/obligation-assignment.spec.ts), [`capture/walkthrough.spec.ts`](./capture/walkthrough.spec.ts) |
| Remediation task metadata | Show a linked high-priority task and its overdue date. | [`scripts/seed-demo.ts`](./scripts/seed-demo.ts), [`../../apps/web/src/App.tsx`](../../apps/web/src/App.tsx), [`../../tests/Gccs.Api.Tests/ComplianceTaskManagementTests.cs`](../../tests/Gccs.Api.Tests/ComplianceTaskManagementTests.cs) |
| Evidence metadata and association | Show a fictional, non-sensitive metadata record associated with the obligation. No file content is used. | [`../../tests/Gccs.Api.Tests/EvidenceMetadataTests.cs`](../../tests/Gccs.Api.Tests/EvidenceMetadataTests.cs), [`scripts/seed-demo.ts`](./scripts/seed-demo.ts) |
| No-CUI notice and upload boundary | Show the notice and disabled upload state. The backend has acknowledgement and upload guardrail tests. | [`../../apps/web/src/App.tsx`](../../apps/web/src/App.tsx), [`../../tests/Gccs.Api.Tests/NoCuiAcknowledgementTests.cs`](../../tests/Gccs.Api.Tests/NoCuiAcknowledgementTests.cs), [`../../tests/Gccs.Api.Tests/EvidenceFileUploadTests.cs`](../../tests/Gccs.Api.Tests/EvidenceFileUploadTests.cs) |
| Tenant-scoped audit history | Show sanitized audit rows created by compliance-relevant activity. | [`../../tests/Gccs.Api.Tests/AuditLogViewerTests.cs`](../../tests/Gccs.Api.Tests/AuditLogViewerTests.cs), [`../../tests/Gccs.Api.Tests/AuditAppendOnlyTests.cs`](../../tests/Gccs.Api.Tests/AuditAppendOnlyTests.cs) |
| Role-based permissions | Describe server-authoritative permissions and show one authorized audit view; permission behavior has API tests. | [`../../tests/Gccs.Api.Tests/RoleBasedPermissionTests.cs`](../../tests/Gccs.Api.Tests/RoleBasedPermissionTests.cs), [`../../tests/Gccs.Api.Tests/TenantMembershipTests.cs`](../../tests/Gccs.Api.Tests/TenantMembershipTests.cs) |
| Compliance-status report | Show a generated, persisted readiness snapshot and its limitations. | [`../../tests/Gccs.Api.Tests/ComplianceStatusReportTests.cs`](../../tests/Gccs.Api.Tests/ComplianceStatusReportTests.cs), [`../../apps/web/e2e-real/report-rbac.spec.ts`](../../apps/web/e2e-real/report-rbac.spec.ts) |
| Capture-safe UI | Hide raw IDs, email addresses, development controls, and unrelated seed controls while preserving API authorization and normal product behavior. | [`../../apps/web/src/App.tsx`](../../apps/web/src/App.tsx), [`../../apps/web/src/App.test.tsx`](../../apps/web/src/App.test.tsx) |

These features are implemented. Focused tests, the deterministic seven-scene walkthrough, capture guards, and representative visual inspection passed for this draft revision. Publication stability still depends on a complete manual playback review after real narration is generated.

### Partially implemented or intentionally constrained

| Feature | Current state | Approved wording/handling |
| --- | --- | --- |
| Executive reporting | There is no separate executive-report/export feature in the approved walkthrough. An implemented compliance-status artifact provides current readiness signals. | Call it a **leadership readiness summary** or **compliance-status snapshot**. Do not present it as a distinct executive reporting product. |
| Due-date assignment in the walkthrough | The linked task has an implemented due date and task update API, but the captured obligation interaction assigns only the owner. | Show the seed-prepared due date. Do not simulate a due-date edit. |
| Evidence workflow | Metadata creation, update, association, classification, and upload controls exist. The approved demo intentionally does not upload a file. | Say **fictional evidence metadata** and **metadata only**. Do not imply a document was uploaded or accepted by an assessor. |
| Role-based-access visualization | Server permissions and denied cases are implemented and tested. Workflow scenes use the Compliance Manager; the audit scene starts a separate Administrator session because that permission is server-authoritative. | Show the permissions/audit context and real authorized sessions. Do not stage a fake role switch or imply an in-product role-switch control. |
| AI narration | Scripts, pronunciation processing, hashing, placeholders, and validation are implemented. Real audio needs an approved credential and human voice review. | Do not publish until every selected scene reports generated, normalized audio and strict validation passes. |

### Not implemented or not approved for this demonstration

- A distinct executive report or executive export.
- An obligation-detail control that changes the linked remediation due date during the captured owner-assignment step.
- A customer-document upload or assessor acceptance flow.
- Storage or handling of CUI or FCI in this No-CUI demonstration.
- A production tenant, production identity provider, or production-data connection.
- Automated GUI editing in Screen Studio, Descript, or Keynote.
- Background music selection, licensing, mix, or ducking.
- Any product claim of certification, legal determination, government approval or endorsement, guaranteed outcomes, or permission to store CUI in the current product.

## Local capability audit

The following was observed on the audited macOS workstation:

| Capability | Status | Detail/action |
| --- | --- | --- |
| Node.js | Available | `v25.8.0` |
| npm | Available | `11.11.0`; this is the supported workspace package manager for the provided commands. |
| pnpm | Available with limitation | `11.9.0`; it warns that this repository's npm `workspaces` field is unsupported without a separate workspace file. Do not use it for this pipeline. |
| yarn | Needs installation if explicitly desired | Not found; not required. |
| Playwright | Available | CLI `1.61.0`; existing browser automation and repository suites are present. |
| System FFmpeg/FFprobe | Needs installation only for direct shell use | Not found on `PATH`. The implemented pipeline uses Remotion's bundled media commands after npm dependency installation. |
| Remotion npm runtime | Available | The project pins and installs Remotion `4.0.503` packages through [`package.json`](./package.json). The initial audit found them absent; the root workspace install resolved that dependency. |
| Browser plugin | Available | Installed in the local Codex environment; optional for visual inspection, not a pipeline dependency. |
| Computer Use plugin | Not available | No callable computer-control capability was exposed. The deterministic pipeline does not require it. |
| Remotion plugin | Available | Installed local Remotion workflow guidance was available. Rendering still depends on the npm runtime. |
| Screen Studio | Available as GUI app | Installed; no reliable command-line pipeline was identified. Not used. |
| Descript | Needs installation if explicitly desired | Application not found; not required. |
| Keynote | Available as GUI app | Installed; no reliable command-line pipeline was identified. Not used. |
| .NET SDK | Available | `10.0.203` |
| Docker and Compose | Available | Docker `29.5.3`; Compose `v5.1.4`. Required for the isolated demo dependencies. |

## Demo-environment audit

The implemented environment is intentionally separate from ordinary development resources:

- Loopback-only web/API and dependency ports.
- Capture-time browser routing aborts every non-loopback HTTP or HTTPS request before transmission.
- Dedicated PostgreSQL database and named volume.
- Dedicated Redis, Azurite, and ClamAV containers.
- Digest-pinned container images matching the verified local capture stack.
- Runtime-generated database password and storage key in an untracked, mode-`0600` file.
- Development-only API host and explicit marketing-demo flag.
- Membership authorization explicitly enabled.
- Invitation delivery and extraction workers disabled for capture.
- Northstar uses the `NoCui` posture and only fictional users/data.
- The stop command preserves the isolated database volume; it makes no destructive schema or production-data change.

Evidence: [`infra/docker-compose.yml`](./infra/docker-compose.yml), [`scripts/prepare-runtime.sh`](./scripts/prepare-runtime.sh), [`scripts/start-api.sh`](./scripts/start-api.sh), and [`scripts/stop-demo.sh`](./scripts/stop-demo.sh).

## Current generated-artifact status

- Narration manifest: 18 of 18 scene entries are `placeholder-silent`.
- Auditions: cedar, marin, and coral have placeholder files; real voice review remains.
- Current timing plan: Flagship `214000 ms` (3:34), Homepage `60000 ms`, Social `30000 ms`.
- Captions: WebVTT, SRT, and Remotion JSON are generated from approved source text.
- Product captures: seven approved scenes completed; stills, WebM assets, and a sanitized execution log were generated locally.
- Draft MP4 files: Flagship, Homepage60, and Social30 rendered locally and passed automated rendered-media validation. A render manifest binds their hashes to the capture, narration, caption, logo, timing, and composition inputs. Their audio streams contain intentional silent placeholders. The separate publication gate correctly fails while those placeholders remain.

Automated media validation does not make these drafts publishable. Complete [`QA-CHECKLIST.md`](./QA-CHECKLIST.md) after real narration generation and final rendering.
