# FeDril flagship demo storyboard

Status: **Source script and deterministic capture implemented; real narration and final editorial approval remain required.**

Audience: small U.S. government contractors organizing readiness work.

Fictional organization: **Northstar Precision Systems**.

Current flagship timing: **3:34 (214 seconds)** at 30 fps.

Editorial posture: No-CUI compliance management; workflow support rather than certification or legal judgment.

The timing and wording below are derived from [`narration/script.json`](./narration/script.json) and [`narration/timings.json`](./narration/timings.json). Adjacent scenes use a 0.5-second visual crossfade, which is why timestamp ranges overlap by half a second. Speech starts after each lead-in and finishes before the transition tail.

## Timestamped flagship storyboard and narration

### 00:00.0–00:20.0 — One operating view

- Section: The readiness problem
- Visual: Branded opening card, fictional-data label, and transition into the approved dashboard treatment.
- Narration: “Compliance work often lives across spreadsheets, inboxes, and disconnected folders. That makes it difficult to see who owns the work, what evidence exists, and which gaps need attention. FeDril brings those readiness activities into one organized, No-CUI workspace.”
- On-screen caption: **Centralize readiness work without storing CUI**
- Callout: **Fictional demonstration · Northstar Precision Systems**
- Capture asset: none; editable Remotion card.

### 00:19.5–00:45.5 — Readiness at a glance

- Section: Readiness dashboard
- Visual: Northstar dashboard with readiness signals, overdue work, high-priority work, evidence posture, and No-CUI context.
- Narration: “Northstar Precision Systems is a fictional company used only for this demonstration. Its dashboard brings together the current readiness signal, overdue work, high-priority obligations, and evidence status. Teams can start with the items that need attention instead of reconciling several trackers before every review.”
- On-screen caption: **Visibility into readiness, gaps, and overdue work**
- Callout: **Implemented: tenant-scoped readiness overview**
- Capture asset: `scene-02-dashboard.webm`

### 00:45.0–01:11.0 — Review the source-backed gap

- Section: Requirement requiring attention
- Visual: Obligation queue filtered to the high-priority item, then the detail view with source, plain-language action, expected evidence, and related work.
- Narration: “The obligation queue highlights a high-priority item tied to FAR 52.204-21. Opening the detail view keeps the plain-language action, source reference, expected evidence, current status, and related work together. FeDril supports the review process; it does not replace qualified compliance or legal judgment.”
- On-screen caption: **Trace readiness work to its source**
- Callout: **High priority · FAR 52.204-21**
- Capture asset: `scene-03-gap-review.webm`

### 01:10.5–01:39.5 — Make ownership visible

- Section: Ownership and remediation
- Visual: Assign Priya Shah as the tenant-member owner. Keep the linked high-priority task and overdue date visible. Do not simulate a due-date edit or send an email.
- Narration: “This readiness item is assigned to Northstar's compliance manager, Priya Shah. A linked remediation task identifies the expected action and the due date. The overdue state is visible in the work queue, so the team can discuss the gap, confirm the owner, and move the work forward through the implemented workflow.”
- On-screen caption: **Connect an owner, task, priority, and due date**
- Callout: **Owner: Priya Shah · Overdue remediation**
- Capture asset: `scene-04-remediation.webm`

### 01:39.0–02:04.0 — Associate evidence metadata

- Section: Evidence traceability
- Visual: Open the fictional Northstar quarterly access-review metadata record and show its obligation association. Do not open a file picker or imply file storage.
- Narration: “Northstar's fictional quarterly access-review summary is represented as non-sensitive evidence metadata. It is associated with the selected obligation and can be reviewed alongside the requirement. No customer document or file content is used in this demonstration, and the association does not imply that an assessor has accepted the evidence.”
- On-screen caption: **Link non-sensitive evidence metadata to readiness work**
- Callout: **Metadata only · No file content stored**
- Capture asset: `scene-05-evidence.webm`

### 02:03.5–02:29.5 — Keep the No-CUI boundary explicit

- Section: No-CUI posture
- Visual: Evidence workspace, No-CUI acknowledgement, prohibited-content guidance, and the disabled upload state. Do not upload a file.
- Narration: “FeDril's current product posture is compliance management only. The evidence workspace presents a No-CUI notice and identifies prohibited sensitive content before the upload area. This demonstration does not upload a file. Teams should use synthetic, redacted, or non-sensitive information and follow their own approved handling procedures.”
- On-screen caption: **Current posture: No-CUI compliance management**
- Callout: **Do not upload CUI or prohibited sensitive content**
- Capture asset: `scene-06-no-cui-boundary.webm`

### 02:29.0–02:54.0 — Review accountable activity

- Section: Auditability and access
- Visual: Sanitized tenant audit rows. Keep emails, raw IDs, local addresses, and development controls out of frame.
- Narration: “Server-authoritative permissions determine which actions each tenant member can perform. Compliance-relevant changes also create tenant-scoped audit events. The audit view gives reviewers a chronological record of activity without exposing another tenant's information. These controls support accountability; they do not certify an organization's compliance.”
- On-screen caption: **Tenant-scoped permissions and audit history**
- Callout: **What changed, and when**
- Capture asset: `scene-07-auditability.webm`

### 02:53.5–03:17.5 — Give leadership a readiness summary

- Section: Leadership visibility
- Visual: Compliance-status report card followed by its generated detail and artifact limitations. Present it as a current snapshot rather than a distinct executive-report feature.
- Narration: “A generated compliance-status snapshot brings the current obligation, task, evidence, and risk signals into a reviewable summary. Leadership can use that snapshot to focus the next readiness conversation and follow up on outstanding work. The report is workflow guidance, not legal advice, certification, or a guarantee of assessment results.”
- On-screen caption: **A reviewable leadership readiness summary**
- Callout: **Snapshot current readiness signals**
- Capture asset: `scene-08-reporting.webm`

### 03:17.0–03:34.0 — Build a repeatable process

- Section: Next step
- Visual: Branded close with schedule-a-demonstration call to action, No-CUI posture, product limitation, and AI narration disclosure.
- Narration: “FeDril helps teams organize CMMC readiness work, connect ownership and evidence metadata, and keep remediation visible. If your team is replacing fragmented trackers with a repeatable compliance-management process, schedule a FeDril demonstration.”
- On-screen caption: **Schedule a FeDril demonstration**
- Callout: **Compliance management · No-CUI posture**
- Capture asset: none; editable Remotion card.
- Required disclosure: **Narration generated using AI voice technology.**

## Flagship shot list

| Shot | Required framing and action | Acceptance evidence |
| --- | --- | --- |
| F-01 | 1920x1080 branded opening; FeDril wordmark, Northstar fictional-data label, and simple problem statement. | No customer or government marks. |
| F-02 | Dashboard at natural zoom; readiness and No-CUI signals visible without unnecessary scrolling. | Northstar visible; no raw IDs, credentials, local addresses, or developer controls. |
| F-03 | Obligation queue and FAR 52.204-21 detail. | Source reference, high priority, expected evidence, and related work remain readable. |
| F-04 | Owner assignment to Priya Shah; email option off; linked due date remains visible. | Successful API response and confirmation; no simulated date edit. |
| F-05 | Evidence metadata detail and obligation association. | Exactly one fictional record; no file content or file picker interaction. |
| F-06 | No-CUI acknowledgement and disabled upload state. | Warning is legible; no file is selected. |
| F-07 | Tenant audit table. | Sanitized actor label and chronological activity; no private identifiers. |
| F-08 | Compliance-status report card and detail. | Artifact limitations visible; use “readiness summary” wording. |
| F-09 | Branded close and call to action. | No-CUI limitation and AI narration disclosure visible. |

## Caption and callout treatment

- Captions are generated from the narration source into [`captions/fedril-demo.vtt`](./captions/fedril-demo.vtt), [`captions/fedril-demo.srt`](./captions/fedril-demo.srt), and [`captions/fedril-demo.json`](./captions/fedril-demo.json).
- Captions use technically accurate display forms, even when speech input uses pronunciation expansions.
- Use high-contrast text inside title-safe margins. Keep captions away from form controls, alerts, requirement details, report values, and the No-CUI notice.
- Show one concise scene callout at a time. Callouts support the spoken outcome and must not cover UI evidence.
- Use only subtle fades or short crossfades. Do not imply a product state change that did not occur in the capture.
- Use the capture-only pointer to guide attention through real semantic targets. Click feedback is allowed only where the deterministic walkthrough performs the corresponding implemented action; informational and No-CUI controls receive hover/point treatment only.
- Use restrained focus drift and progressive capture framing to keep long narration holds visually active without obscuring fields or simulating application state changes.

## 60-second homepage version

Current timing: exactly **01:00.0**, with 0.5-second transitions.

| Timestamp | Visual | Narration | Caption | Callout |
| --- | --- | --- | --- | --- |
| 00:00.0–00:12.4 | Brand and dashboard | “Compliance readiness can become scattered across spreadsheets and inboxes. FeDril brings the work into one No-CUI operating view.” | One view for readiness work | Fictional Northstar demonstration |
| 00:11.9–00:24.3 | Obligation queue and detail | “Teams can see high-priority obligations, ownership, due dates, and overdue remediation without rebuilding the status picture by hand.” | See gaps, owners, and due dates | Source-backed obligation detail |
| 00:23.8–00:36.2 | Evidence metadata and No-CUI context | “Non-sensitive evidence metadata can be associated with readiness work while the product keeps its current No-CUI boundary visible.” | Trace evidence metadata without storing CUI | Metadata only in this demonstration |
| 00:35.7–00:48.1 | Tenant audit history | “Tenant-scoped permissions and audit history help reviewers understand relevant activity and keep follow-up work accountable.” | Permissions and tenant-scoped audit history | Review what changed and when |
| 00:47.6–01:00.0 | Report snapshot and branded close | “Give leadership a reviewable readiness summary and keep the next action visible. Schedule a FeDril demonstration.” | Schedule a FeDril demonstration | Workflow guidance, not certification |

Caption files: [`captions/fedril-homepage-60.vtt`](./captions/fedril-homepage-60.vtt), [`captions/fedril-homepage-60.srt`](./captions/fedril-homepage-60.srt), and [`captions/fedril-homepage-60.json`](./captions/fedril-homepage-60.json).

## 30-second social version

Current timing: exactly **00:30.0**, 1080x1920, with 0.5-second transitions.

| Timestamp | Visual | Narration | Caption | Callout |
| --- | --- | --- | --- | --- |
| 00:00.0–00:07.875 | Brand and dashboard crop | “Bring fragmented compliance readiness work into one organized, No-CUI view.” | Organize readiness work | FeDril |
| 00:07.375–00:15.250 | Obligation/remediation detail crop | “See high-priority gaps, owners, due dates, and evidence metadata together.” | Gaps · Owners · Due dates · Evidence | Fictional Northstar demonstration |
| 00:14.750–00:22.625 | Compliance-status report treatment | “Review a current leadership readiness summary and keep follow-up work visible.” | Readiness visibility for leadership | Workflow guidance, not certification |
| 00:22.125–00:30.000 | Branded close | “Build a more repeatable compliance-management process with FeDril.” | Schedule a FeDril demonstration | No-CUI compliance management |

Caption files: [`captions/fedril-social-30.vtt`](./captions/fedril-social-30.vtt), [`captions/fedril-social-30.srt`](./captions/fedril-social-30.srt), and [`captions/fedril-social-30.json`](./captions/fedril-social-30.json).

## Editorial limitations

- Implemented: dashboard, obligation/detail, owner assignment, linked task date, evidence metadata association, No-CUI notice, audit history, and compliance-status snapshot.
- Partially implemented for this story: leadership reporting is an existing compliance-status snapshot, not a separate executive-report product; RBAC is described and test-backed but not shown through a role-switch montage.
- Completed draft execution: seven product captures and three placeholder-audio renders passed automated checks.
- Planned release execution: real narration, measured-timing rerender, complete playback review, and publication sign-off.
- Do not claim: certification, legal determination, government approval or endorsement, guaranteed outcomes, secure CUI handling, assessor acceptance, or production-data operation.
