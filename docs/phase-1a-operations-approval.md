# Phase 1A — operational decisions and verification hold

Status: **Partially implemented. Not approved for real customer CUI.**

This is an internal decision worksheet, not a customer assurance statement. Repository tests and
historical staging restore evidence do not verify today's production recovery capability.
No deployment, cloud restore, backup deletion, or incident notification was performed for this work.

## Decisions awaiting an accountable owner

| Decision | Current value | What is needed |
| --- | --- | --- |
| Primary incident responder | Unassigned | A named person who accepts responsibility, has appropriate access, and can coordinate containment |
| Backup incident responder | Unassigned | A different person or contracted support provider who can act when the primary is unavailable |
| Escalation destination | Unassigned / untested | An actual monitored mailbox, paging service, or team channel; owner, coverage hours, and successful test receipt |
| RPO (maximum acceptable data loss) | Not approved | Business owner's approved duration and supporting backup/replication evidence |
| RTO (maximum acceptable downtime) | Not approved | Business owner's approved duration and a timed recovery rehearsal, including validation |
| Current deployment security review | Not verified by this work | Exact deployed SHA, resource IDs, configuration observations, reviewer, date, findings, and disposition |
| Current deployment recovery rehearsal | Not performed | Separate approval for exact isolated restore target, region, cost ceiling, access, retention, and teardown |

## How to fill this in

1. Identify who currently administers the application, Azure subscription, database, and identity
   provider. Ask that person explicitly to accept the primary responder role. Ownership of an
   account alone is not an on-call commitment. If that person is you, record your name only after
   accepting the role and confirming your access.
2. Identify a second authorized person or a contracted provider. If you are operating alone,
   leave backup coverage unassigned until an actual arrangement exists; do not list an AI assistant
   or an uncontacted vendor as the backup.
3. Choose a destination that both responders can access. Record who monitors it, during which
   hours, the expected response time, and what happens when nobody responds. With approval, send
   a synthetic incident-ID-only test and record receipt by both responders. Never attach suspected
   CUI or customer document contents to the alert.
4. Decide the RPO by asking: after losing the database, how much recent customer work could the
   business accept recreating? Record the answer as a duration. Backup retention is not RPO;
   keeping seven days of backups does not establish how much recent data can be recovered.
5. Decide the RTO by asking: from the declared incident, how long can customers tolerate being
   unable to use the service? Include investigation, restore, configuration, integrity checks,
   tenant/RBAC checks, and reopening access. Do not infer RTO from an infrastructure restore command's duration.
6. Have the responsible business owner approve both targets. Have the technical owner explain
   whether the current design can meet them. If not, keep the gap open or revise the targets with
   explicit business approval; do not relabel an aspiration as a tested result.
7. Approve an isolated restore rehearsal separately. Identify source and destination resource IDs,
   restore point, spending limit, authorized operators, validation checks, evidence location, and
   teardown decision. Do not restore over production. Measure observed data loss and end-to-end
   downtime, then compare them to the approved RPO/RTO.

## Review evidence inventory

For each area, record `Not verified`, `Failed`, or `Verified`, with the exact environment, SHA,
date, reviewer, evidence reference, and unresolved exceptions:

- Tenant isolation and RBAC, including denied access and background-job tenant identity.
- Private evidence/document/report storage, encryption, public access, and administrative access.
- Scanner health and fail-closed behavior; retention and durable cleanup backlog/retries.
- Database backups, object versions/backups, restore integrity, and reconciliation of deleted objects.
- Secrets, identity configuration, monitoring, incident contacts, and escalation receipt.
- Incident playbooks for accidental/suspected CUI, prohibited data, malware, suspected cross-tenant
  exposure, and failed deletion/export. Include who may contain, review, release, notify, and close.

An unresolved critical finding or unknown recovery capability remains a readiness gap. This worksheet
does not itself enforce a CI/CD gate, approve a release, or authorize CUI handling.

## Application changes in this increment

- **Implemented in code:** shared policy calls the current-notice guard; missing/outdated acknowledgement
  returns 428 with a workflow context, and the web notice panel can reopen for explicit renewal.
  This does not establish coverage of every metadata/note route.
- **Implemented in code:** evidence/document deletion queues private-object cleanup in the resource/audit
  transaction. The PostgreSQL worker uses row locking, bounded delete attempts, retry backoff, and
  completion audit. Logical deletion does not mean physical cleanup has already completed.
- **Partially implemented:** typed escalation references and reviewed-safe evidence release.
  Referral alone does not release content. Unsupported document/report release remains rejected.
- **Not complete:** metadata/result-read containment coverage, version-bound review/release for all
  resource types, upload crash-orphan reconciliation, and an externally monitored cleanup/incident channel.
- **Not performed:** deployment, production mutation, cloud restore, recovery-target approval, or
  notification delivery testing. Preserve No-CUI posture.

## Migration and rollout dependencies

`AddDurableObjectCleanup` adds the `gccs.object_cleanup` table. Apply it through the approved
deployment process before starting the updated API/worker. `ObjectCleanupProcessing:Enabled`
defaults to true when a database connection is configured. Setting it false pauses physical
cleanup but does not restore logical access; pending rows remain. Monitor incomplete rows,
attempt counts, last error codes, and age. No alert recipient has been configured by this work.

Do not roll the migration down while cleanup records remain: dropping the table loses pending
cleanup intent. Coordinate API and web rollout for the 428 renewal contract. Existing legacy
No-CUI acknowledgements are not silently converted into current-notice consent.
