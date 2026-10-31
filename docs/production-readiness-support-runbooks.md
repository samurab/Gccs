# Production Readiness Support Runbooks

Story: PR-5.3 - Finalize Support Runbooks.

Review date: 2026-07-02.

Review status: support runbooks finalized; customer success/support owner approval remains required before launch approval.

Launch posture: No-CUI / compliance management only. Support actions must not authorize real customer CUI, classified information, ITAR/export-controlled technical data, credentials, payroll, SSNs, health or disability data, unrestricted security logs, sensitive incident details, or other prohibited sensitive content.

Severity targets follow `docs/software-delivery-plan.md`: Severity 1 within 30 minutes, Severity 2 within 4 business hours, Severity 3 within 1 business day, and Severity 4 within 3 business days.

## Support Routing

Launch support intake routes suspected CUI, prohibited upload, tenant exposure, access issue, evidence failure, report failure, content correction, security incident, backup restore, and rollback cases to the owners below. Every ticket must record tenant ID, actor or reporter, timestamp, affected workflow, impact, data-handling posture, evidence links, containment status, escalation owner, and closure decision.

## Runbook: Prohibited Upload

Owner: security owner.

Triage steps:
- Confirm tenant mode, upload workflow, file metadata, validation status, malware scan status, and audit event ID.
- Verify whether the blocked content is prohibited by the No-CUI policy.
- Confirm the file is not usable, downloadable, reportable, or available for extraction.

Escalation path: security owner first; customer success/support owner for customer communication; legal or contracting advisor if customer disputes classification or data-handling scope.

Severity guidance: Severity 1 if prohibited content became usable or downloadable; Severity 2 if blocked but customer workflow is impacted; Severity 3 for advisory or duplicate reports.

Evidence to capture: support ticket, tenant ID, upload intent ID, file metadata, validation response, audit event IDs, screenshots, timestamps, and containment decision.

No-CUI containment: preserve No-CUI posture, keep the upload blocked, disable download/export/extraction/report use for affected content, preserve audit logs, and do not request or store file contents in the ticket.

## Runbook: Suspected CUI

Owner: security owner.

Triage steps:
- Confirm the affected tenant posture and whether the item is classified as CUI, SyntheticCui, Prohibited, Unknown, or Unclassified.
- Review only metadata and customer-provided non-sensitive context.
- Create or update the CUI support escalation record and block downstream use while triage is open.

Escalation path: security owner first; compliance content owner for source/context questions; legal or contracting advisor for disputed CUI handling; engineering lead if containment control fails.

Severity guidance: Severity 1 if suspected real CUI was stored, downloaded, exported, or included in reports; Severity 2 if blocked but present in metadata; Severity 3 for classification questions without stored content.

Evidence to capture: escalation ID, tenant ID, affected item references, classification metadata, audit event IDs, reporter notes, containment timestamp, and resolution record.

No-CUI containment: preserve No-CUI posture, block download/export/extraction/report/evidence approval, do not copy content into logs or tickets, and keep the case escalated until owner resolution.

## Runbook: Tenant Exposure

Owner: engineering lead.

Triage steps:
- Capture reported endpoint, record ID, tenant ID, actor, role, timestamp, and expected tenant boundary.
- Verify RBAC and tenant predicates in logs or reproduced synthetic-only tests.
- Check whether cross-tenant IDs, metadata, names, evidence, reports, or audit logs were exposed.

Escalation path: engineering lead first; security owner for incident classification; customer success/support owner for communication; product owner for launch-impact decision.

Severity guidance: Severity 1 for confirmed cross-tenant data exposure; Severity 2 for plausible exposure blocked by RBAC; Severity 3 for false positive or customer confusion.

Evidence to capture: request IDs, audit IDs, sanitized logs, affected endpoint, role, tenant IDs, reproduction steps, screenshots, and containment actions.

No-CUI containment: assume exposed tenant-scoped data is sensitive, stop affected workflow if necessary, do not disclose cross-tenant details to unrelated tenants, and preserve audit evidence.

## Runbook: Access Issue

Owner: customer success/support owner.

Triage steps:
- Confirm user identity, tenant membership, role, permission, SSO/MFA state, and affected workflow.
- Check whether the denial is expected RBAC behavior or an authentication/configuration problem.
- Confirm no one bypasses RBAC or tenant mode to resolve the issue.

Escalation path: customer success/support owner first; engineering lead for authentication or authorization defects; security owner for suspicious account activity.

Severity guidance: Severity 1 for suspected account takeover or broad outage; Severity 2 for multiple users blocked from launch-critical workflows; Severity 3 for single-user access issues; Severity 4 for advisory role questions.

Evidence to capture: user ID, tenant ID, role, permission, endpoint, error contract, request ID, audit event, screenshots, and resolution note.

No-CUI containment: do not grant elevated access without approval, do not share tenant data through support channels, and preserve least-privilege access.

## Runbook: Evidence Failure

Owner: engineering lead.

Triage steps:
- Identify whether the failure is upload validation, malware scanning, storage, metadata persistence, evidence approval, export, or download.
- Confirm No-CUI acknowledgement and per-file attestation status.
- Check audit events and storage references without copying file contents into tickets.

Escalation path: engineering lead first; security owner for malware/prohibited/CUI signals; customer success/support owner for customer communication.

Severity guidance: Severity 1 if evidence from multiple tenants is inaccessible or integrity is at risk; Severity 2 for launch-critical upload/storage failure; Severity 3 for isolated evidence workflow failure.

Evidence to capture: evidence ID, file version ID, upload intent ID, validation response, scan status, storage key reference, audit IDs, error contract, timestamps, and reproduction steps.

No-CUI containment: keep failed or unscanned files unusable, block download/export/report use until clean and accepted, and never attach raw customer documents to support tickets.

## Runbook: Report Failure

Owner: engineering lead.

Triage steps:
- Identify report type, tenant ID, actor role, source records, export format, and failure mode.
- Confirm report RBAC, tenant scope, source links, last-reviewed dates, and claim-control language.
- Verify the report did not include prohibited, unknown, unapproved, or cross-tenant records.

Escalation path: engineering lead first; compliance content owner for source/content defects; legal or contracting advisor for overclaim concerns; security owner for data exposure.

Severity guidance: Severity 1 for cross-tenant or prohibited data in a report; Severity 2 for incorrect customer-facing compliance status; Severity 3 for isolated report generation failure.

Evidence to capture: report ID, export ID, tenant ID, request ID, source record IDs, sanitized output excerpt, error contract, audit event IDs, and fix decision.

No-CUI containment: revoke or mark affected exports invalid when data-handling or tenant-scope issues are suspected, and avoid sending report contents through support channels.

## Runbook: Content Correction

Owner: compliance content owner.

Triage steps:
- Identify obligation, clause, source URL, review state, last-reviewed date, customer report, and affected workflows.
- Determine whether the correction is source update, stale review, customer dispute, ambiguity, or typo.
- Keep draft, needs-review, rejected, retired, or disputed content hidden unless the workflow explicitly allows a dispute flag.

Escalation path: compliance content owner first; legal or contracting advisor for high-risk interpretation; product owner for customer-facing release note; engineering lead if content import or publication controls fail.

Severity guidance: Severity 1 for content that could cause unsafe customer action or regulatory misstatement at scale; Severity 2 for high-risk published content requiring correction; Severity 3 for non-critical metadata correction.

Evidence to capture: content ID, source URL, source snapshot/hash when available, reviewer, review state, customer report, proposed correction, approval decision, and release note impact.

No-CUI containment: do not request customer documents as proof unless non-sensitive and allowed; preserve source traceability and audit history.

## Runbook: Security Incident

Owner: security owner.

Triage steps:
- Classify incident type, affected tenants, systems, severity, initial detection time, and active containment status.
- Preserve logs, audit events, deployment/version context, and suspected indicators.
- Determine whether tenant isolation, data handling, authentication, uploads, reports, or support access are affected.

Escalation path: security owner first; engineering lead for technical containment; product owner and customer success/support owner for communications; legal or contracting advisor for notification obligations.

Severity guidance: Severity 1 for confirmed data exposure, active compromise, or cross-tenant impact; Severity 2 for credible security defect with limited impact; Severity 3 for suspicious activity requiring investigation.

Evidence to capture: incident ID, detection source, request IDs, audit IDs, log excerpts, affected tenants, containment actions, timeline, communications, and closure decision.

No-CUI containment: preserve No-CUI boundaries, prevent additional upload/report/export exposure, restrict support access to need-to-know, and do not copy sensitive content into incident notes.

## Runbook: Backup Restore

Owner: engineering lead.

Triage steps:
- Confirm restore reason, source database/server, restore point, target environment, data posture, and approver.
- Use synthetic-only staging restore rehearsal before production launch approval.
- Verify health, migrations, tenant boundaries, and teardown for temporary restored resources.

Escalation path: engineering lead first; security owner for data-handling and access; product owner for launch-blocking restore decisions.

Severity guidance: Severity 1 for production data loss or failed restore during incident; Severity 2 for missing launch restore evidence; Severity 3 for rehearsal defect with workaround.

Evidence to capture: restore command, source server, target server, restore time, health check output, migration state, reviewer, teardown confirmation, and cost/resource notes.

No-CUI containment: restored staging data must remain synthetic-only; restrict restored server access; delete temporary restored resources after evidence capture.

## Runbook: Rollback

Owner: engineering lead.

Triage steps:
- Identify failed deployment, version, migration state, health status, customer impact, and rollback target.
- Determine whether rollback is application-only or involves database migration risk.
- Confirm destructive migrations are not automatically reversed without explicit approval.

Escalation path: engineering lead first; product owner for launch/rollback decision; security owner for data-handling impact; customer success/support owner for customer communication.

Severity guidance: Severity 1 for production outage or data integrity risk; Severity 2 for launch-blocking staging rollback failure; Severity 3 for non-customer-impacting rollback rehearsal issue.

Evidence to capture: deployment run ID, commit/version, health checks, migration script, rollback command, timing, decision owner, result, and post-rollback smoke evidence.

No-CUI containment: rollback must preserve tenant isolation and No-CUI posture; if upload/report controls regress, disable affected workflows before restoring service.
