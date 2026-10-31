# Phase 1: Lock The Offer

Document type: founder-led offer definition and customer-discovery guide.

Status: internal working document. Use for controlled discovery only until founder, product, security, and counsel review are complete.

Required boundary: FeDril is a No-CUI / compliance management MVP. Do not claim legal advice, CMMC certification, assessor determinations, government endorsement, or permission to upload real CUI.

## Phase Goal

Define a narrow, credible first offer that a small government contractor can understand, evaluate, and buy without requiring custom enterprise procurement.

The offer should answer:

1. Who is this for?
2. What painful workflow does it replace?
3. What outcome does the buyer get in 30 days?
4. What data is allowed?
5. What is excluded?
6. What does the pilot cost?
7. What decision happens at the end?

## Locked Offer Answers

| Offer question | Locked answer |
| --- | --- |
| Who is this for? | Small U.S. government contractors, subcontractors, and advisors supporting small contractors who need readiness workflow discipline without enterprise GRC overhead. |
| What painful workflow does it replace? | Spreadsheet-driven tracking of contract readiness, source-backed obligations, evidence metadata, ownership, status, and report artifacts. |
| What outcome does the buyer get in 30 days? | One configured No-CUI readiness workflow with contract metadata, attached or reviewed clauses, generated obligations, owner/status tracking, evidence metadata, a report artifact, and audit history. |
| What data is allowed? | Synthetic, redacted, or non-sensitive data only. |
| What is excluded? | Real CUI, legal advice, CMMC certification, assessor determinations, government endorsement, custom integrations, GovCloud/FedRAMP, SSO, SCIM, and enterprise procurement support. |
| What does the pilot cost? | $750 flat pilot fee by default, credited toward the first annual subscription if the customer converts within the agreed period. |
| What decision happens at the end? | Convert to paid subscription, continue limited discovery by mutual agreement, or stop. |

## Recommended First Offer

### 30-Day Guided Readiness Pilot

FeDril helps a small government contractor organize one No-CUI readiness workflow by setting up contract metadata, attaching or reviewing relevant clauses, generating source-backed obligations, assigning ownership, linking allowed evidence metadata, and generating a current report artifact.

### Pilot Price

Use a paid pilot by default.

Recommended starting price: `$750 flat pilot fee`.

Conversion credit: credit the pilot fee toward the first annual subscription if the customer converts within the agreed period.

### Best-Fit Customer

- Small U.S. government contractor or subcontractor.
- Has contract, CMMC, evidence, or flow-down readiness work currently tracked in spreadsheets, email, or shared folders.
- Can run the pilot with synthetic, redacted, or non-sensitive data only.
- Has a named internal owner.
- Wants operational readiness discipline, not certification guarantees.

### Poor-Fit Customer

- Requires real CUI handling immediately.
- Wants FeDril to certify compliance.
- Needs legal, accounting, labor, or contracting determinations.
- Requires GovCloud, FedRAMP, SSO, SCIM, custom integrations, or enterprise procurement before any pilot.
- Cannot assign an internal owner for the pilot.

## Offer Categories

| Category | Decision |
| --- | --- |
| Target buyer | Small GovCon owner, operations lead, contracts lead, security lead, or advisor supporting small contractors. |
| Pain point | Spreadsheet-driven readiness tracking is scattered, stale, and hard to prove. |
| Primary promise | Replace one fragile readiness spreadsheet with owned obligations, allowed evidence metadata, report artifacts, and audit history. |
| Pilot scope | One contract or synthetic workflow, No-CUI only, guided setup, end-of-pilot findings session. |
| Deliverables | Configured pilot workspace, mapped workflow, ownership/status tracking, evidence metadata, report artifact, findings summary. |
| Exclusions | Real CUI, legal advice, CMMC certification, custom integrations, GovCloud/FedRAMP, enterprise procurement support. |
| Price | $750 paid pilot, credited toward first annual subscription if converted. |
| Close | Decide whether to convert, continue discovery, or stop. |

## Supporting Documents

- [Ideal Customer Profile](./phase-1-ideal-customer-profile.md)
- [Problem And Use Cases](./phase-1-problem-use-cases.md)
- [Positioning And Messaging](./phase-1-positioning-messaging.md)
- [Pilot Offer](./phase-1-pilot-offer.md)
- [Customer Discovery Questions](./phase-1-discovery-questions.md)
- [Offer Validation Checklist](./phase-1-offer-validation-checklist.md)
- [One-Page Offer](./phase-1-one-page-offer.md)
- [Discovery Call Script](./phase-1-discovery-call-script.md)
- [Qualification Scorecard](./phase-1-qualification-scorecard.md)
- [Pilot Scope And Success Plan](./phase-1-pilot-scope-success-plan.md)
- [Founder Action Checklist](./phase-1-founder-action-checklist.md)

## Implementation Sequence

1. Use the [Qualification Scorecard](./phase-1-qualification-scorecard.md) to reject bad-fit prospects before a full demo.
2. Use the [Discovery Call Script](./phase-1-discovery-call-script.md) for the first 30-minute call.
3. Send the [One-Page Offer](./phase-1-one-page-offer.md) after a qualified discovery call.
4. Use the [Pilot Scope And Success Plan](./phase-1-pilot-scope-success-plan.md) before accepting payment.
5. Track founder execution in the [Founder Action Checklist](./phase-1-founder-action-checklist.md).

## App Reality Check

Do not describe the offer in ways the app does not currently support.

| Claim area | Current app-aligned wording |
| --- | --- |
| Contract setup | Use the `Contracts` tab to create contract metadata and attach or review clauses. |
| Clause workflow | Use `Attached clauses` in `Contracts`; use `Clause library search` in `Obligations` as a search helper. |
| Obligations | Use the `Obligations` tab to review generated source-backed obligations, assign ownership, and update workflow status. |
| Evidence | Use the `Evidence` tab to create metadata and link it to obligations or controls. |
| Reports | Use the `Reports` tab for Compliance Status, CMMC Readiness, Subcontractor Compliance, or Evidence Package artifacts. |
| Audit history | Use the `Settings` tab audit log when the user has audit-log permission. |
| No-CUI | Acknowledgement is displayed in `Contracts` and `Evidence`; upload-related workflows are gated by acknowledgement and attestation. |

## Exit Criteria

Phase 1 is complete when:

- The ICP is narrow enough to reject bad-fit prospects.
- The pain statement is specific and buyer-recognizable.
- The pilot scope can be delivered without custom engineering.
- The No-CUI boundary is explicit.
- The pilot price and conversion credit are clear.
- The discovery call has a concrete close.
- Public-facing language has been reviewed before real sales use.
- The one-page offer, call script, scorecard, pilot success plan, and founder checklist are ready for controlled discovery.
