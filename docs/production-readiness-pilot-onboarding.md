# Production Readiness Pilot Onboarding

Story: PR-5.4 - Prepare Pilot Onboarding, Release Notes, And Known-Risk Log.

Review date: 2026-07-02.

Review status: launch-ready draft; product owner and customer success/support owner approval remain required before pilot use.

## Pilot Scope

GCCS pilot tenants are limited to the MVP No-CUI / compliance management only posture. Pilot users may manage company profile metadata, contract metadata, source-backed obligations, tasks, evidence metadata, allowed non-sensitive evidence, CMMC readiness workflow records, subcontractor records, and reports using synthetic, redacted, or non-sensitive data only.

Synthetic demo workflows may show CUI-aware concepts for training, but they do not authorize production storage, upload, processing, reporting, extraction, export, or support handling of real customer CUI.

Pilot onboarding may begin only after `docs/production-readiness-production-smoke-evidence.md` records a reviewed PR-7.2 production smoke pass and PR-7.3 prerequisites are satisfied. The 2026-07-05 authenticated production smoke passed login, tenant access, RBAC denial, No-CUI acknowledgement, upload guardrails, scanner-backed byte-level evidence upload, report generation, audit visibility, logging, API `Http5xx` alert resource, and health checks using synthetic-only data. Any future blocked, failed, missing, or unreviewed critical smoke row for upload controls, evidence upload, report generation, audit logging, logs, alerts, or health checks prevents pilot onboarding.

## Prohibited Data

Pilot users must not upload, paste, import, or attach:

- Real customer CUI.
- Classified information.
- ITAR or export-controlled technical data.
- Sensitive government-furnished information.
- Credentials, passwords, secrets, private keys, or unrestricted security logs.
- Payroll, SSNs, bank, tax, protected health, disability, or sensitive incident details.
- Production customer data unless separately approved as non-sensitive and in scope.

## Required Pilot Acknowledgement

Before evidence or document upload workflows are used, pilot users must acknowledge the No-CUI notice and per-file attestation. Any unknown, suspected CUI, or prohibited-data case must be reported through support and kept out of tickets, screenshots, logs, and shared documents.

## Support Paths

Use `docs/production-readiness-support-runbooks.md` for support routing. Required launch support paths include prohibited upload, suspected CUI, tenant exposure, access issue, evidence failure, report failure, content correction, security incident, backup restore, and rollback.

## Known Limitations

- Restore rehearsal evidence remains a launch approval blocker until completed or formally dispositioned.
- Production scanner evidence is attached for PR-7.2 using private ClamAV container `gccs-clamav-production`; a single ACI scanner is acceptable only for controlled No-CUI pilot scope and must be hardened before broader production use.
- Alert owner notification receipt remains pending because `gccs-api-production-http5xx` has no attached action-group receiver; do not claim alert notification routing is complete until receiver evidence is attached.
- High-risk obligations that are not `published` remain withheld from customer-facing production views.
- Customer-facing claims and support materials require final accountable owner approvals before PR-6.1 launch approval.
- Reports are workflow guidance only, not legal advice, certification decisions, assessor determinations, contracting-officer determinations, or government endorsements.

## Pilot Exit Criteria

Pilot use remains in scope only while No-CUI boundaries, tenant isolation, RBAC, audit logging, upload guardrails, report claim controls, support routing, and known-risk mitigations remain active. Any suspected tenant exposure, real CUI handling, unsupported product claim, or unresolved Severity 1 incident blocks pilot expansion.
