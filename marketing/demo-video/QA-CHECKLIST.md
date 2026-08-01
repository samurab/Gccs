# FeDril demo-video quality checklist

Use this checklist for the exact commit, environment, narration manifest, captures, and renders intended for publication. An unchecked item is not evidence of a pass.

## Automated verification

### Product behavior and security boundaries

- [ ] Record the commit SHA: `____________________________`.
- [ ] Record the operating system, database provider, browser version, and execution date.
- [ ] Focused Development-seed tests pass, including flag-off, Production, idempotence, content, roles, and collision behavior.
- [ ] Tenant-membership and role-permission allowed/denied tests pass for affected paths.
- [ ] Obligation dashboard/detail and owner-assignment tests pass.
- [ ] Evidence metadata, acknowledgement, and upload-guardrail tests pass.
- [ ] Audit viewer and append-only audit tests pass.
- [ ] Compliance-status report and report-RBAC tests pass.
- [ ] Web unit tests, lint, and a capture-mode production build pass.
- [ ] The marketing workspace passes `npm --workspace marketing/demo-video run typecheck`.

### Narration and captions

Run:

```bash
npm run narration:auditions
npm run narration:generate
npm run captions:generate
npm --workspace marketing/demo-video run narration:validate:strict
```

- [ ] Strict validation exits successfully.
- [ ] Every published scene is `generated`, not `placeholder-silent` or `failed`.
- [ ] Every published scene has the approved voice, model, direction, hash, duration, date, and normalization result.
- [ ] Captions were regenerated after the final narration-source edit.
- [ ] WebVTT, SRT, and Remotion JSON contain identical approved wording and non-overlapping cues.
- [ ] Canonical narration WAVs and the public WAVs consumed by Remotion are byte-identical for every scene.

### Walkthrough and rendered media

Run:

```bash
npm run demo:video:capture
npm run video:render
npm run video:validate
```

- [ ] All seven execution-log scenes report `completed`.
- [ ] Every captured WebM is at least 0.5 seconds longer than the longest measured composition scene that consumes it.
- [ ] Flagship is 1920x1080, 30 fps, and between 3 and 4 minutes.
- [ ] Homepage is 1920x1080, 30 fps, and 60 seconds (within the validator's ±0.05-second container tolerance).
- [ ] Social is 1080x1920, 30 fps, and 30 seconds (within the validator's ±0.05-second container tolerance).
- [ ] Every render contains an audio stream.
- [ ] Automated blank-segment detection passes.
- [ ] Customer-facing source validation passes for brand, disclosure, and prohibited marketing wording.
- [ ] `out/render-manifest.json` matches every output hash and the current capture, narration, caption, logo, timing, and composition-source digest.

## Manual narration review

- [ ] One approved voice is used throughout each video and the same voice is used across the campaign.
- [ ] Voice tone, volume, pace, accent, and microphone character sound consistent between scenes.
- [ ] FeDril is pronounced consistently and naturally.
- [ ] CMMC is spoken letter by letter.
- [ ] CUI is spoken letter by letter.
- [ ] DFARS is pronounced “Dee-Fars.”
- [ ] NIST is pronounced “Nist.”
- [ ] Other dictionary terms follow [`narration/pronunciations.json`](./narration/pronunciations.json).
- [ ] No word is missing.
- [ ] No sentence or scene is duplicated.
- [ ] No scene starts mid-word or ends before the speaker finishes.
- [ ] No narration segments overlap.
- [ ] Lead-in and tail pauses sound intentional.
- [ ] There are no clicks, pops, abrupt cuts, excessive silence, or large volume shifts at boundaries.
- [ ] Narration explains customer outcomes rather than cursor motion.
- [ ] Narration contains no unsupported compliance, certification, legal, government, or assessment-outcome claim.
- [ ] No background music is present in the narration-only review version.

## Manual caption and accessibility review

- [ ] Every caption matches the spoken source text exactly in meaning and sequence.
- [ ] Technical terms use their accurate displayed form rather than the pronunciation-expanded form.
- [ ] Each cue remains visible long enough to read at a comfortable pace.
- [ ] Captions remain within title-safe margins at 1920x1080 and 1080x1920.
- [ ] Caption contrast is sufficient over every captured screen and transition.
- [ ] Captions do not cover buttons, alerts, report values, requirement details, task dates, evidence metadata, or the No-CUI notice.
- [ ] Callouts do not compete with captions or obscure implementation evidence.
- [ ] Text remains legible at normal homepage/social playback size.
- [ ] The meaning remains clear with audio muted.

## Manual visual and product review

- [ ] Every visible organization, person, email-like label, contract, evidence name, date, and activity is fictional and approved for the demo.
- [ ] Northstar Precision Systems is identified as fictional in the opening or first relevant scene.
- [ ] No production data, customer data, CUI, FCI, credentials, tokens, keys, secrets, uploaded document content, or sensitive government-furnished information is visible.
- [ ] No raw UUID, local URL, browser address, terminal, developer toolbar, seed control, test email, stack trace, or debug notification is visible.
- [ ] No internal product branding is visible in UI, reports, captions, overlays, narration, stills, filenames displayed on screen, or credits.
- [ ] The application shows no broken page, blank state caused by a load failure, failed request, or unexpected error notification.
- [ ] The approved intentional disabled-upload notice is clearly contextualized and is not presented as a runtime error.
- [ ] No unnecessary scrolling, cursor wandering, accidental hover state, notification, or focus jump distracts from the workflow.
- [ ] Every presentation-cursor click ring corresponds to an actual implemented interaction executed by the Playwright walkthrough.
- [ ] Pointer-only treatment on No-CUI controls does not imply acknowledgement, upload enablement, or file submission.
- [ ] Progressive zoom and focus movement keep all cited UI evidence legible and do not crop required warnings, values, or limitations.
- [ ] Owner assignment succeeds through the product and the email option remains disabled.
- [ ] The task due date is shown but no unsupported due-date edit is simulated.
- [ ] Evidence remains metadata-only; no file picker or upload is performed.
- [ ] The No-CUI warning and current posture remain visible and accurately described.
- [ ] Audit rows are sanitized while retaining meaningful action/time context.
- [ ] The compliance-status artifact is called a readiness summary or snapshot and its limitations remain visible.
- [ ] The closing card contains the final call to action, No-CUI posture, and AI narration disclosure.
- [ ] No government seal, military styling, endorsement badge, certification mark, or unlicensed customer logo is shown.

## Claim-status review

| Claim/scene | Status allowed | Evidence to verify before release |
| --- | --- | --- |
| Tenant-scoped readiness dashboard | Implemented | Current UI, overview API/service, and affected tests |
| Source-backed obligation detail | Implemented | Current UI, obligation endpoints/services, and detail/dashboard tests |
| Owner assignment | Implemented | Current UI, owner endpoint, permission checks, and real-stack assignment test |
| Overdue remediation date | Implemented as linked task metadata | Current task API/state and seeded date; do not imply it was edited in the scene |
| Evidence association | Implemented as metadata-only in this demo | Current evidence metadata API/UI and evidence tests |
| No-CUI boundary | Implemented notice and server guardrails | Current UI, acknowledgement/upload API behavior, and tests |
| Tenant audit history | Implemented | Current audit UI/API and append-only/viewer tests |
| Server-authoritative permissions | Implemented | Access response, membership middleware, permission policies, and role tests |
| Leadership readiness summary | Partially implemented label | Current compliance-status snapshot only; no separate executive-report claim |
| Role-switch montage | Planned or omit | Do not simulate without a deterministic, tested capture |
| Customer-document upload | Do not claim | Omitted from the approved story; metadata only |
| Certification, legal conclusion, government approval/endorsement, guaranteed outcome, or secure CUI handling | Do not claim | Outside verified product posture and approved wording |

## Pre-publication checklist

For every material sentence in the narration, caption, callout, title, description, or landing page:

- [ ] **UI:** Does the current UI expose the exact flow or state shown?
- [ ] **API/domain:** Does the API or domain/application service actually enforce any wording such as “required,” “blocked,” “prevented,” “enforced,” or “must”?
- [ ] **Authorization:** Are tenant scope, membership, and permission behavior server-authoritative for the demonstrated action?
- [ ] **Tests:** Is there a focused test proving the allowed behavior and relevant denied/cross-tenant behavior?
- [ ] **No-CUI:** Does the content preserve the current No-CUI posture and avoid suggesting storage or handling of prohibited content?
- [ ] **Product truth:** Is the claim derived from current product behavior rather than an aspirational sales statement?
- [ ] **Status label:** Is the underlying capability accurately classified as Implemented, Partially implemented, Planned, or Do not claim?
- [ ] **Wording:** Does the language avoid certification, legal, accounting, labor-determination, government, security, and outcome overclaims?
- [ ] **Fictional data:** Can every visible data point be traced to the approved Northstar seed or source-backed public clause content?
- [ ] **Disclosure:** Does the final frame and accompanying description include “Narration generated using AI voice technology.”?

## Optional music gate

Music remains out of scope until the narration-only cut is approved.

- [ ] License provenance is documented and permits the intended commercial channels.
- [ ] The track is restrained and does not use dramatic military or cinematic cues.
- [ ] Music remains substantially quieter than narration.
- [ ] Music is reduced further during technical explanations.
- [ ] The final mix is revalidated for speech intelligibility, clipping, and consistent loudness.

## Sign-off record

| Review | Reviewer | Date | Result | Evidence/notes |
| --- | --- | --- | --- | --- |
| Product behavior |  |  |  |  |
| Security/tenant/RBAC |  |  |  |  |
| No-CUI/compliance wording |  |  |  |  |
| Narration/audio |  |  |  |  |
| Captions/accessibility |  |  |  |  |
| Brand/creative |  |  |  |  |
| Final publication |  |  |  |  |
