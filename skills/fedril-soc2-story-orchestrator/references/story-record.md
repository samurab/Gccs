# Per-story handoff and completion record

Create or update the existing canonical story record. Record the exact title from the current authoritative document and avoid duplicating an existing tracker.

| Field | Value |
| --- | --- |
| Story / exact title | |
| Record updated (UTC) | |
| Authoritative document path / SHA-256 | |
| Dependency and stage-gate disposition | |
| Task status | Planned / In Progress / Blocked / Ready For Review / Accepted / Deferred / Not Applicable; retain existing story status, with no new completion state |
| Design / implementation / operating evidence / governance | Separate states and source references |
| Existing behavior and smallest remaining gap | |
| Source and evidence references | Sanitized paths, collection date, verified / partially verified / unverified / N/A, access limitation |
| Branch / commit / PR | Verified identifiers or `none`; never guessed |
| Dirty-file binding / environment | Relevant uncommitted-file hashes, configuration/environment identity; protected details stay in approved custody |
| Pending run / session / process | Exact verified identifier and state; protected reference where sensitive; reconcile before retry |
| Authorized action scope | Existing approval reference, exact targets/actions and limits; fresh exact approval for destructive actions |
| Checks / affected roles / tenant cases / rollback | Command, revision, result, untested scope |
| Staging and production | Trigger, run, tested artifact/revision, health, approval, or N/A justification |
| Acceptance criteria | One row per criterion with evidence and disposition |
| Outstanding owner / due date / operational obligation | Named only when verified; otherwise `unassigned` |
| Blocker and exact next action | |
| Next eligible story | Based on authoritative gate, not numerical assumption |

Keep a compact current checkpoint above historical narrative. Use the existing
ledger for the eligible queue, program task index for task metadata, and evidence
inventory for evidence references; link instead of duplicating those populations.
Root owns shared writes. Record each requirement's evidence, tested revision and
environment, limitation and actual reviewer disposition. Technical verification
does not equal operating effectiveness, management approval or final acceptance.

At a blocked operation record the dependency and continue the next eligible unit
inside the user's scope. A checkpoint does not require the user to say continue.
Only end execution when the authorized work is exhausted or no safe eligible unit
remains; a real pending external run remains explicit. Never claim continuation
after turn completion without an active runner.

For a negative or failed check, record the candidate revision, failure, affected gate, investigation, and verified recovery. Never use `Complete` while a required row lacks evidence.
