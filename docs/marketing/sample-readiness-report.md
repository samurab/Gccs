# Sample Readiness Report

Document type: sales sample and pilot demo artifact.

Generated PDF: `docs/marketing/sample-readiness-report.pdf`.

Data status: synthetic demonstration data only.

Required boundary: this report is workflow guidance only. It is not legal advice, a CMMC certification decision, an assessor determination, a contracting-officer determination, or a government endorsement.

## Report Summary

| Field | Sample value |
| --- | --- |
| Customer | Acme Federal Services LLC |
| Tenant posture | No-CUI / compliance management only |
| Report date | 2026-07-12 |
| Workflow reviewed | DoD subcontract readiness workflow |
| Contract record | Synthetic Subcontract 2026-001 |
| Report owner | Operations Lead |
| Review period | 2026-06-12 to 2026-07-12 |

## Executive Snapshot

Acme Federal Services LLC used FeDril to organize one synthetic subcontract workflow, identify source-backed obligations, assign readiness ownership, track allowed evidence metadata, and generate a readiness-oriented sample report.

This sample shows how FeDril can replace scattered spreadsheet tracking with a controlled compliance workspace. It does not evaluate legal compliance, certify CMMC compliance, determine official readiness, or authorize handling of real CUI.

## Readiness Overview

| Area | Status | Owner | Notes |
| --- | --- | --- | --- |
| Company profile | In progress | Operations Lead | Basic company metadata entered. |
| No-CUI acknowledgement | Acknowledged | Security Lead | No-CUI posture acknowledged for demo workflow. |
| Contract metadata | Complete | Contracts Lead | Synthetic subcontract record added. |
| Clause handling | In progress | Contracts Lead | Attached clauses, extraction candidates, or generated obligations require human review before pilot expansion. |
| Obligation ownership | In progress | Operations Lead | Owners assigned to 6 of 8 sample obligations. |
| Evidence metadata | Partial | Security Lead | Allowed sample evidence metadata attached to 4 obligations. |
| CMMC readiness tracking | Started | Security Lead | Workflow tracking only; no assessment determination. |
| Subcontractor flow-down tracking | Not started | Contracts Lead | Not applicable until subcontractor records are added. |
| Report generation | Complete | Operations Lead | Sample report generated from synthetic records. |

## Sample Obligation Matrix

| Obligation | Source family | Trigger | Required action | Owner | Evidence status | Review state |
| --- | --- | --- | --- | --- | --- | --- |
| Basic safeguarding practices | FAR safeguarding | Covered contractor information present in contract workflow | Maintain documented basic safeguarding workflow | Security Lead | Metadata attached | Draft sample |
| Cyber incident handling record | DFARS cyber | Contract includes cyber incident reporting requirement | Maintain internal reporting and escalation workflow | Security Lead | Gap identified | Draft sample |
| CMMC readiness task ownership | CMMC program workflow | DoD supplier readiness review | Assign control-family owners and track evidence metadata | Security Lead | Partial | Draft sample |
| Subcontractor flow-down review | Flow-down workflow | Subcontractor performs contract work | Determine whether flow-down tracking is needed | Contracts Lead | Not started | Draft sample |
| Evidence retention checkpoint | Internal readiness workflow | Evidence used to support obligation tracking | Record evidence location, owner, date, and review status | Operations Lead | Metadata attached | Draft sample |

## Evidence Metadata Snapshot

FeDril tracks evidence metadata and allowed non-sensitive files for the No-CUI MVP. The sample does not include real customer CUI, classified information, ITAR/export-controlled technical data, credentials, payroll records, SSNs, health data, or sensitive incident details.

| Evidence item | Linked obligation | Type | Sensitivity | Status |
| --- | --- | --- | --- | --- |
| Synthetic access-control policy excerpt | CMMC readiness task ownership | Policy metadata | Synthetic | Attached |
| Redacted training completion register | Basic safeguarding practices | Training metadata | Redacted | Attached |
| Evidence request checklist | Evidence retention checkpoint | Checklist | Non-sensitive | Attached |
| Incident response contact list placeholder | Cyber incident handling record | Contact metadata | Synthetic | Gap requires review |

## Open Gaps

- Clause tagging requires final human review before being used for a real pilot workflow.
- Two sample obligations do not yet have named owners.
- Cyber incident handling evidence is represented by placeholder metadata only.
- Subcontractor flow-down tracking has not started because no subcontractor record was added.
- Readiness labels must not be interpreted as pass/fail or certification status.

## Recommended Next Actions

1. Confirm the pilot workflow uses synthetic, redacted, or non-sensitive records only.
2. Assign owners for all obligations in the sample workflow.
3. Attach allowed evidence metadata for each high-priority obligation.
4. Review report language with the accountable compliance owner before sharing externally.
5. Schedule an end-of-pilot findings session to decide whether to convert to a paid subscription.

## Required Disclaimer

This sample report is for demonstration and workflow-planning purposes only. FeDril does not provide legal advice, accounting advice, labor determinations, CMMC certification, assessor determinations, contracting-officer determinations, or government endorsement. The MVP is No-CUI / compliance management only. Real CUI and other prohibited sensitive data must not be uploaded, pasted, imported, attached, or processed in FeDril unless a future separately approved posture exists.
