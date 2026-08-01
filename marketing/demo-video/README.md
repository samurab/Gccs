# FeDril demo-video pipeline

This directory contains the editable source and automation for a fictional FeDril product demonstration. It creates a 3:34 flagship video, a 60-second homepage cut, and a 30-second vertical social cut from an isolated local environment.

The demonstration uses only the fictional organization **Northstar Precision Systems**. It does not use production systems, customer records, CUI, FCI, real customer contract information, credentials, or uploaded document content.

## Current state

| Item | Status | Current evidence |
| --- | --- | --- |
| Isolated demo services and Northstar seed | Implemented | [`infra/docker-compose.yml`](./infra/docker-compose.yml), [`scripts/start-demo.sh`](./scripts/start-demo.sh), and [`scripts/seed-demo.ts`](./scripts/seed-demo.ts) |
| Deterministic 1920x1080 walkthrough capture | Implemented and executed | Seven completed scenes are recorded under `public/captures/`; the source and execution log are [`capture/walkthrough.spec.ts`](./capture/walkthrough.spec.ts) and [`assets/capture/execution-log.json`](./assets/capture/execution-log.json). |
| Flagship, homepage, and social compositions | Implemented and rendered as drafts | Remotion source under [`src/`](./src/); all three draft files passed automated duration, resolution, frame-rate, audio-stream, blank-segment, branding, disclosure, and claim checks. |
| Narration scripts, captions, and timing model | Implemented | [`narration/script.json`](./narration/script.json), [`narration/timings.json`](./narration/timings.json), and [`captions/`](./captions/) |
| AI narration | Partially implemented | All 18 scene files currently have `placeholder-silent` status because no approved API credential was available. See [`narration/manifest.json`](./narration/manifest.json). |
| Rendered MP4 drafts | Implemented with intentional silent-placeholder audio | Generated under `out/`; technically validated, but not publishable until real narration and manual review pass. |
| Background music | Not implemented | Intentionally deferred until the narration-only version passes review. |

See [`AUDIT.md`](./AUDIT.md) for the repository and capability audit, [`STORYBOARD.md`](./STORYBOARD.md) for the approved content, and [`QA-CHECKLIST.md`](./QA-CHECKLIST.md) for the release gates.

## Architecture and safety boundaries

The pipeline keeps product behavior and media production separate:

1. The API starts in `Development` with an explicit `MarketingDemo__Enabled=true` gate.
2. Dedicated loopback ports and dedicated Docker volumes isolate the demo from normal development and production systems.
3. The development bootstrap creates the Northstar tenant, fictional members, metadata-only evidence, an overdue gap, and audit history.
4. The idempotent seed script adds the source-backed fictional contract workflow and verifies the expected high-priority item.
5. Playwright always starts the isolated stack, blocks non-loopback browser requests before transmission, and rejects visible credentials, raw identifiers, local addresses, internal branding, unexpected API errors, and unexpected error notices. A capture-only presentation cursor moves to semantic UI targets and displays click feedback only when the verified workflow performs that interaction.
6. Remotion combines the approved recordings, source-generated captions, narration, callouts, branded cards, progressive focus movement, and animated caption/callout entrances.

The seed does not weaken tenant membership enforcement, permission checks, audit behavior, or the No-CUI boundary. Runtime credentials are generated locally in `.runtime/demo.env`; the environment file and launcher logs use restrictive permissions, are ignored by Git, and must not be copied into tickets, recordings, or documentation.

## Prerequisites

- macOS with Node.js, npm, .NET SDK, Docker Desktop, and Docker Compose.
- Full FFmpeg with the `afade`, `areverse`, `loudnorm`, and `silenceremove` audio filters. On macOS, install it with `brew install ffmpeg`.
- Repository dependencies installed with `npm install` at the repository root.
- Playwright Chromium installed. If it is absent, run `npx playwright install chromium`.
- Optional: an approved `OPENAI_API_KEY` in the current shell for real narration.

Remotion's bundled FFmpeg is intentionally reduced and does not contain every narration-normalization filter. The generator verifies filter capability and prefers a full system FFmpeg; when those filters are unavailable, generated speech is retained without normalization and strict narration validation rejects it. Do not substitute GUI capture tools unless the structured capture has a verified blocker.

## End-to-end run

Run these commands from the repository root:

```bash
npm install
npm run narration:auditions
npm run narration:generate
npm run captions:generate
npm run narration:validate
npm run demo:video:capture
npm run video:preview
npm run video:render
npm run video:validate
```

`video:validate` is the publication gate: it requires real narration and all three current, manifest-bound renders. With no API key, the generation, capture, preview, and render steps still succeed, while this final release check intentionally reports the silent placeholders.

`npm run demo:video:capture` starts the isolated stack through the Playwright `webServer` configuration, verifies or refreshes the fictional seed, derives each recording hold from the current measured narration timing, records seven application scenes, validates that every WebM is long enough, saves stills and an execution log, and shuts down its child service processes when the capture finishes.

The presentation cursor is not a product feature. It is a deterministic recording aid. Its targets are resolved from the same semantic locators used to execute and verify the real browser workflow; the No-CUI scene uses pointing only and never simulates acknowledgement or upload.

Use these commands when you want to inspect the demo environment manually:

```bash
npm run demo:video:start
npm run demo:video:seed
npm run demo:video:stop
```

`demo:video:start` remains attached to its terminal. Run the seed or browser inspection in a second terminal. The API and web app listen only on `127.0.0.1`.

## Narration workflow

The speech pipeline uses the model, response format, direction, audition voices, and normalization targets in [`narration/voice-config.json`](./narration/voice-config.json). `TTS_VOICE` selects one voice for every scene; the default is `cedar`.

Set the approved credential without writing it to a file or command history:

```bash
read -s OPENAI_API_KEY
export OPENAI_API_KEY
export TTS_VOICE=cedar
npm run narration:auditions
npm run narration:generate
npm run captions:generate
npm --workspace marketing/demo-video run narration:validate:strict
```

The audition command generates the same excerpt in `cedar`, `marin`, and `coral` under `narration/auditions/`. Review all three, then set one `TTS_VOICE` for the complete video. Do not splice different voices into one composition.

The generator sends one request per scene, never logs the credential, applies pronunciation replacements only to speech input, writes WAV files to `assets/narration/`, and records source and configuration hashes in `narration/manifest.json`. Unchanged scenes are reused when their hashes and voice settings match. Captions remain technically accurate because they are generated from the approved source text, not the pronunciation-expanded text or generated audio. Homepage60 and Social30 receive closing-card hold time when measured speech is shorter than 60 or 30 seconds; generation fails with script-edit guidance if speech exceeds the target and never speeds up narration.

### No-key fallback

When `OPENAI_API_KEY` is unavailable, narration commands succeed with silent WAV placeholders and timing metadata. Non-strict validation reports those placeholders as warnings. The rest of the capture, preview, and source-validation workflow remains usable. `npm run video:validate:draft` validates the technical draft; `npm run video:validate` remains strict and fails until real narration exists.

After the credential is configured, the exact regeneration command is:

```bash
TTS_VOICE=cedar npm run narration:generate
```

Then regenerate captions and run strict narration validation. Do not publish a placeholder-audio render.

## Preview, render, and validation

Preview the editable project:

```bash
npm run video:preview
```

Render all deliverables:

```bash
npm run video:render
```

Expected outputs:

- `marketing/demo-video/out/fedril-flagship.mp4` — 1920x1080, 30 fps, current timing 3:34.
- `marketing/demo-video/out/fedril-homepage-60.mp4` — 1920x1080, 30 fps, 1:00.
- `marketing/demo-video/out/fedril-social-30.mp4` — 1080x1920, 30 fps, 0:30.

Run source and rendered-media checks:

```bash
npm run narration:validate
npm run video:validate:draft
npm run video:validate
```

The release validators check dimensions, exact short-form targets, the flagship duration range, 30 fps, an audio stream, blank segments, source-video duration against measured narration, canonical/public narration equality, all customer-facing caption/script/overlay sources, required disclosure and card-caption render contracts, and prohibited marketing wording. `video:render` also writes `out/render-manifest.json`; validation rejects an output whose hash or capture, narration, caption, logo, timing, or composition-source digest is stale. Automated checks do not replace the manual review in [`QA-CHECKLIST.md`](./QA-CHECKLIST.md).

## Caption regeneration

```bash
npm run captions:generate
```

This produces WebVTT, SRT, and Remotion JSON for all three compositions in [`captions/`](./captions/). Cue timing is derived from each approved scene's narration window. The final reviewer must still confirm that captions do not cover controls, alerts, report details, or other important UI content.

The `gccs-demo.vtt` and `gccs-demo.srt` filenames are compatibility aliases explicitly retained for the requested deliverable shape; their contents use only FeDril branding. Use the `fedril-*` files for every customer-facing publication or sidecar upload.

## Cleaning generated files

```bash
npm run demo:video:clean
```

The clean command removes generated captures, narration WAV files, render output, and transient test output. It preserves editable source, manifests, captions, the isolated database volume, and `.runtime/demo.env`. Stop the environment separately with `npm run demo:video:stop`.

## Manual steps that remain

1. Review the three real voice auditions and approve one voice.
2. Generate real narration with an approved credential and run strict narration validation.
3. Preview the complete duration of all three compositions at full size and verify every caption, capture transition, and visual beat; representative frames and capture stills have been inspected, but this editorial pass is not automated.
4. Regenerate and rerender after real narration so measured speech durations replace placeholder timing, then rerun strict narration and media validation.
5. Complete the manual narration, product-claim, accessibility, and pre-publication checks.
6. Obtain product/compliance review before publishing the videos or description.
7. Add only properly licensed, restrained music after narration-only approval, if music is still desired.

## Assumptions and dependencies

- Docker can download and run PostgreSQL, Redis, Azurite, and ClamAV images.
- Container images are digest-pinned to the versions used for the verified capture. Refresh a digest only through a reviewed dependency update followed by the focused API tests, two-run seed check, full capture, and rerender.
- Loopback ports `5064`, `5175`, `15434`, `16381`, `19002`, and `13312` are available.
- Published clause content required by the demo seed remains importable in Development.
- The browser has a compatible Playwright Chromium build.
- The visible UI labels and semantic selectors used by the capture remain compatible.
- The generated due date is intentionally historical relative to the current demo date so the remediation stays overdue.
- The compliance-status artifact is presented only as a readiness snapshot; it is not a distinct executive-report product.
- No live customer, production, external provider, or production identity system is part of this pipeline.

## Known incomplete items and hidden risks

- The seven product captures and three draft MP4s passed their automated checks. They remain non-publishable because the audio manifest contains silent placeholders and full-duration editorial review has not been signed off.
- A generated report can become stale after later changes. Re-seed or regenerate the snapshot immediately before capture.
- Browser recording timing depends on the local machine. A resource-constrained host can produce uneven video despite deterministic holds.
- Real speech duration can change the flagship duration from the current 3:34 timing. The generator updates scene timing, preserves exact 60/30-second short cuts by extending only the closing hold, and refuses overlong short-form narration; recapture and rerender after every narration change.
- The walkthrough assigns an owner through the UI, but the overdue task date is prepared by the demo seed because the captured obligation workflow does not expose a due-date editor.
- The workflow scenes use Priya Shah as Compliance Manager; the audit scene signs in as Alex Morgan as Administrator because that permission is server-authoritative. The capture does not stage a fake role switch or claim an unimplemented role-switch interface.
- Evidence is metadata-only. No file upload is performed or implied.
- Screen Studio and Keynote are optional manual tools, not repeatable pipeline dependencies. Computer-control automation and Descript are not available in the audited environment.
- Repeated non-notifying owner assignment is idempotent, but simultaneous competing owner changes remain last-write-wins because this aggregate has no concurrency token.
- Concurrent first-read notification-preference creation is handled and was verified with real PostgreSQL. A simultaneous first-time preference update still uses the existing read-then-create path and is outside this demo-pipeline fix.
- The notification-preference race recovery depends on PostgreSQL unique-violation metadata and the current tenant/user constraint name; a persistence migration that renames that constraint must update and rerun the concurrency tests.
- Earlier pipeline iterations left isolated `fedril-marketing-demo` and `fedril-marketing-demo-v2` Docker volumes on this workstation. They contain demo-only local state and were not deleted because volume removal is destructive; review them explicitly before any manual cleanup.

## Publication disclosure

Every published version and its accompanying page or description must include:

> Narration generated using AI voice technology.

The video also needs to retain its fictional-data and No-CUI limitations. Do not publish until the checklist records evidence for the UI flow, API behavior, authorization, tests, wording, and No-CUI posture.
