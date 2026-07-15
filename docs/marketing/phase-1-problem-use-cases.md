# Phase 1: Problem And Use Cases

Document type: buyer problem framing and use-case inventory.

## Core Problem

Small government contractors are asked to prove readiness, but the work is scattered across contracts, spreadsheets, shared drives, email, and individual memory.

The buyer does not only need a list. They need an operating view of:

- What applies.
- Why it applies.
- Who owns it.
- What evidence metadata exists.
- What is overdue or blocked.
- What can be reported without overclaiming compliance.

## Priority Use Cases

| Use case | Current pain | App-aligned demo path |
| --- | --- | --- |
| Contract readiness intake | Contract metadata and clauses are tracked manually. | `Contracts` tab -> contract form -> `Attached clauses`. |
| Clause-to-obligation tracking | Requirements are copied into spreadsheets without source context. | `Contracts` tab -> attach mapped clauses -> `Obligations` tab. |
| Owner assignment | No one knows who owns each obligation. | `Obligations` tab -> `View details` -> `Assign by`. |
| Workflow status | Readiness status is stale or subjective. | `Obligations` tab -> `Update status` -> `Save status`. |
| Evidence organization | Evidence exists but is hard to connect to obligations. | `Evidence` tab -> `Evidence metadata` -> `Obligations (optional)` and `Controls`. |
| Reporting | Reports are rebuilt manually for each request. | `Reports` tab -> Compliance Status, CMMC Readiness, Subcontractor Compliance, or Evidence Package. |
| Auditability | Changes are not traceable. | `Settings` tab -> `Audit log`. |

## Pain Statements To Test

- "We track this in spreadsheets, but nobody trusts the spreadsheet."
- "We know evidence exists, but we cannot quickly show what it supports."
- "Readiness depends on one person knowing where everything is."
- "We need to prepare for reviews without pretending the tool certifies us."
- "We need a No-CUI workspace because we are not ready to put sensitive content into a SaaS product."

## Claims To Avoid

- "GCCS proves compliance."
- "GCCS certifies CMMC readiness."
- "GCCS stores CUI."
- "GCCS replaces an assessor or attorney."
- "GCCS automatically determines contract obligations without review."

