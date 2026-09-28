# Per-story handoff and completion record

Create or update the existing canonical story record. Record the exact title from the current authoritative document and avoid duplicating an existing tracker.

| Field | Value |
| --- | --- |
| Story / exact title | |
| Record updated (UTC) | |
| Authoritative document path / SHA-256 | |
| Dependency and stage-gate disposition | |
| Status | Not started / In progress / Implemented—verification pending / Blocked—dependency / Blocked—human decision / Awaiting operational evidence / Complete |
| Design / implementation / operating evidence / governance | Separate states and source references |
| Existing behavior and smallest remaining gap | |
| Source and evidence references | Sanitized paths, collection date, verified / partially verified / unverified / N/A, access limitation |
| Branch / commit / PR | Verified identifiers or `none`; never guessed |
| Checks / affected roles / tenant cases / rollback | Command, revision, result, untested scope |
| Staging and production | Trigger, run, tested artifact/revision, health, approval, or N/A justification |
| Acceptance criteria | One row per criterion with evidence and disposition |
| Outstanding owner / due date / operational obligation | Named only when verified; otherwise `unassigned` |
| Blocker and exact next action | |
| Next eligible story | Based on authoritative gate, not numerical assumption |

For a negative or failed check, record the candidate revision, failure, affected gate, investigation, and verified recovery. Never use `Complete` while a required row lacks evidence.
