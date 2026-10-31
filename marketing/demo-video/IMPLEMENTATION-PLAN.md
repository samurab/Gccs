# FeDril demo-video implementation plan

## Critique and failure modes

The pipeline cannot safely be built as a recording-only task. Three architectural failures would make that approach unreliable:

1. **Uncontrolled application state:** recording an ordinary developer database would make content, dates, ownership, and report state drift between takes and could expose unrelated data.
2. **Presentation-layer simulation:** faking unsupported UI interactions or editing claims around screenshots would disconnect the video from API authorization, audit events, and current product behavior.
3. **Media timing guessed before narration:** fixed scene lengths would clip speech, overlap segments, or force unnatural delivery when real speech duration differs from the script estimate.

The correct pattern is a separately gated Development tenant, idempotent server-backed seed, deterministic browser capture through supported product flows, scene-level narration with measured timing, source-generated captions, and automated plus manual publication gates.

## Verification level and compatibility surface

This work is **High verification** because it touches Development authentication configuration, tenant membership, RBAC-visible workflows, audit events, No-CUI controls, evidence metadata, reporting, and a real-stack browser flow.

Compatibility surfaces that must remain unchanged:

- Production and non-Development startup behavior.
- Existing tenants and ordinary Development seed behavior.
- Tenant scope and server-side permission evaluation.
- Audit-event creation and append-only history.
- Evidence upload acknowledgement and content guardrails.
- Existing API request/response contracts.
- Normal UI rendering when capture mode is not enabled.
- Existing report and obligation workflows.

No database schema change, backend namespace rename, internal project rename, production configuration change, or destructive database operation is required.

## Implementation sequence

### 1. Audit architecture, behavior, tests, assets, and local tools

Status: **Completed**

- Locate the API, web app, application/domain/infrastructure projects, authentication gates, seed infrastructure, Playwright suites, and brand assets.
- Trace each proposed customer-visible feature through UI, API/service behavior, authorization, and tests.
- Classify claims as Implemented, Partially implemented, Planned, or Do not claim.
- Inventory Node, package managers, Playwright, media tools, plugins, and optional GUI applications.

Deliverable: [`AUDIT.md`](./AUDIT.md).

### 2. Add a separately gated Northstar Development seed

Status: **Implemented and focused verification passed for the draft revision**

Files:

- [`../../apps/api/LocalDevelopment/DevelopmentTenantBootstrapper.cs`](../../apps/api/LocalDevelopment/DevelopmentTenantBootstrapper.cs)
- [`../../tests/Gccs.Api.Tests/DevelopmentSeedDataTests.cs`](../../tests/Gccs.Api.Tests/DevelopmentSeedDataTests.cs)

Controls:

- Require `Development`, normal Development seed/auth gates, and `MarketingDemo:Enabled=true`.
- Create one No-CUI fictional tenant and four fictional role-bearing members.
- Seed metadata-only evidence with no storage URI or file content.
- Seed completed/incomplete readiness state, a high-risk overdue item, audit history, and deterministic dates.
- Preflight deterministic IDs and natural keys before mutation; fail closed on collisions.
- Prove flag-off, Production, idempotence, role separation, seed content, and collision cases.

### 3. Build the isolated local demo stack and idempotent scenario seed

Status: **Implemented; isolated live start, seed, verification, and stop passed**

Files:

- [`infra/docker-compose.yml`](./infra/docker-compose.yml)
- [`scripts/prepare-runtime.sh`](./scripts/prepare-runtime.sh)
- [`scripts/start-api.sh`](./scripts/start-api.sh)
- [`scripts/start-web.sh`](./scripts/start-web.sh)
- [`scripts/start-demo.sh`](./scripts/start-demo.sh)
- [`scripts/stop-demo.sh`](./scripts/stop-demo.sh)
- [`scripts/seed-demo.ts`](./scripts/seed-demo.ts)

Controls:

- Bind all services to loopback.
- Use dedicated database, cache, storage emulator, malware scanner, ports, and volumes.
- Generate untracked runtime credentials and avoid logging their values.
- Keep tenant-membership authorization and malware scanning enabled.
- Disable unrelated invitation delivery and extraction workers.
- Create only fictional contract/activity records.
- Import published source-backed clause content, generate obligations, update the fictional task, associate metadata-only evidence, create a compliance-status snapshot, and verify the required gap.
- Refuse any non-loopback API base URL.

### 4. Add capture-safe presentation and stable selectors

Status: **Implemented; focused web tests and capture-mode build passed**

Files:

- [`../../apps/web/src/App.tsx`](../../apps/web/src/App.tsx)
- [`../../apps/web/src/App.test.tsx`](../../apps/web/src/App.test.tsx)
- [`../../apps/web/styles/globals.css`](../../apps/web/styles/globals.css)

Controls:

- Gate capture presentation with `VITE_DEMO_CAPTURE=true`.
- Hide raw identifiers, user emails, development context controls, and unrelated synthetic-content controls only in capture mode.
- Preserve normal UI behavior when the flag is absent.
- Add semantic `data-testid` hooks only where accessible role/name selectors are insufficient.
- Disable animation and smooth scrolling for deterministic recording.
- Keep all API permissions and backend authorization unchanged.

### 5. Automate the product walkthrough

Status: **Implemented; all seven scenes recorded successfully**

Files:

- [`capture/playwright.config.ts`](./capture/playwright.config.ts)
- [`capture/walkthrough.spec.ts`](./capture/walkthrough.spec.ts)

The capture always starts the isolated stack and uses a 1920x1080 headless Chromium context, one worker, semantic selectors, explicit network/element waits, measured-timing-derived holds, scene-specific video, stills, and a sanitized execution log. It aborts non-loopback browser traffic before transmission, checks safety before and after every recording hold, verifies each WebM exceeds the longest consuming scene, and fails on missing elements, page/console errors, failed API calls, unexpected UI errors, visible credentials/local addresses/raw IDs, or internal branding.

Approved sequence:

1. Dashboard.
2. High-priority obligation detail.
3. Owner assignment and visible overdue task date.
4. Fictional evidence metadata association.
5. No-CUI notice without file upload.
6. Sanitized tenant audit history.
7. Compliance-status snapshot and limitations.

### 6. Create approved scripts, scene narration, and captions

Status: **Implemented; editorial approval required**

Files:

- [`narration/script.json`](./narration/script.json)
- [`narration/pronunciations.json`](./narration/pronunciations.json)
- [`narration/voice-config.json`](./narration/voice-config.json)
- [`scripts/generate-captions.ts`](./scripts/generate-captions.ts)
- [`captions/`](./captions/)
- [`STORYBOARD.md`](./STORYBOARD.md)

The flagship is 3:34 in the current timing manifest; shorter compositions are exactly 60 and 30 seconds. Claims remain limited to organization, centralization, visibility, traceability, accountability, and support for a repeatable process. Captions use approved source text and retain technical terms; pronunciation replacements apply only to speech input.

### 7. Generate and validate scene-level narration

Status: **Pipeline implemented; real generation and review incomplete**

Files:

- [`narration/generate-narration.ts`](./narration/generate-narration.ts)
- [`scripts/generate-narration.sh`](./scripts/generate-narration.sh)
- [`scripts/validate-narration.ts`](./scripts/validate-narration.ts)
- [`narration/manifest.json`](./narration/manifest.json)
- [`narration/timings.json`](./narration/timings.json)

The generator:

- Uses `gpt-4o-mini-tts`, WAV, configurable `TTS_VOICE`, and default `cedar`.
- Generates cedar, marin, and coral auditions from one excerpt.
- Sends one request per scene and never exposes the API credential.
- Stores source text, narration-friendly text, voice/model/direction, output, duration, source/config hashes, generation date, status, and normalization status.
- Reuses unchanged output when hashes and configuration match.
- Measures actual WAV duration and rebuilds scene/caption timing with lead-in and tail room. Short-form cuts receive closing hold time to remain exactly 60/30 seconds and fail rather than speeding up overlong narration.
- Verifies that the canonical WAV and the public copy consumed by Remotion are byte-identical.
- Produces silent placeholders instead of failing the project when no key is available.

Exit gate: one approved voice, real WAV files for every published scene, consistent normalization, strict validation passing, and manual pronunciation/audio review.

### 8. Build the editable Remotion project

Status: **Implemented; all three silent-audio drafts rendered and passed automated media validation**

Files:

- [`package.json`](./package.json)
- [`remotion.config.ts`](./remotion.config.ts)
- [`src/`](./src/)
- [`scripts/validate-media.ts`](./scripts/validate-media.ts)
- [`scripts/media-integrity.ts`](./scripts/media-integrity.ts)
- [`scripts/write-render-manifest.ts`](./scripts/write-render-manifest.ts)
- [`scripts/clean-generated.ts`](./scripts/clean-generated.ts)

Compositions:

- `Flagship`: 1920x1080, 30 fps, timing-manifest duration.
- `Homepage60`: 1920x1080, 30 fps, 60 seconds.
- `Social30`: 1080x1920, 30 fps, 30 seconds.

Each composition retains editable branded cards, section headings, callouts, source-timed captions (including opening and closing cards), safe margins, subtle transitions, narration audio, and the AI-voice disclosure. The render command records source and output hashes so stale drafts fail validation. No government seal, endorsement badge, certification mark, or invented customer asset is used.

### 9. Execute release-quality media QA

Status: **Partially completed; real narration and complete manual sign-off remain**

Required evidence:

- Completed: 84 focused high-risk API tests covering Development seeding, tenant membership, roles, obligations, evidence, No-CUI controls, audit behavior, and compliance-status reports.
- Completed after the final repeatability fixes: all 18 obligation-detail tests, including exact no-op owner-assignment side-effect checks; all 12 Development seed tests; and 2 real-PostgreSQL notification-preference concurrency tests. Without a PostgreSQL connection, those 2 integration tests report as skipped rather than false passes.
- Completed: 51 focused web tests and the capture-mode production build.
- Completed: marketing TypeScript checks and non-strict draft source/narration validators. The release validator intentionally remains failing until real narration replaces every placeholder.
- Completed: real-stack capture with all seven scene entries recorded as `completed`.
- Pending: strict narration validation with no placeholders.
- Partially completed: representative still and composition-frame inspection; complete playback review remains.
- Completed for the placeholder-audio drafts: duration, dimensions, frame rate, audio stream, blank-segment, disclosure, branding, and claim validation.
- Pending: completed pre-publication checklist and product/compliance approval.

Checklist: [`QA-CHECKLIST.md`](./QA-CHECKLIST.md).

## Rollback and cleanup

- Disable or remove `MarketingDemo__Enabled`; the Northstar bootstrap is excluded when false.
- Stop local services with `npm run demo:video:stop`.
- Remove generated media with `npm run demo:video:clean`.
- The clean command deliberately preserves the isolated database volume and runtime credential file. Remove those only through a separately reviewed, explicit maintenance action.
- No production database, schema, customer record, external identity, or backend namespace participates in rollback.

## Completion definition

The pipeline is complete only when source implementation and focused tests pass, real narration is approved, capture succeeds, all three MP4 files render and pass automated inspection, manual QA is signed, and no customer-facing claim exceeds the verified product behavior. A source-only or placeholder-audio render is a draft, not a publishable asset.
