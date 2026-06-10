# Compliance Content Governance

Compliance content must remain source-backed, reviewed, and distinguishable from legal advice.

## Obligation Record Requirements

Each obligation should include:

- Source name and URL.
- Clause text version.
- Effective date when known.
- Source hash when a source snapshot is captured.
- Last reviewed date.
- Trigger conditions.
- Applicability dimensions.
- Required actions.
- Evidence examples.
- Flow-down requirements.
- Risk level.
- Confidence level.
- Expert review flag.
- Review owner or accountable content role.
- Review state: draft, needs review, approved, rejected, customer disputed, published, or retired.
- Superseded or replaced status when a clause or rule changes.

## First-Class MVP Source Families

The MVP obligation library should treat these as first-class source families:

- FAR safeguarding and technology restrictions, including FAR 52.204-21, FAR 52.204-25, and FAR 52.204-27.
- DFARS cyber clauses, including DFARS 252.204-7012, 252.204-7019, 252.204-7020, and 252.204-7021.
- 32 CFR Part 170 for CMMC program requirements.
- NIST SP 800-171 Rev. 2 for the current CMMC control baseline, while tracking Rev. 3 customer questions and future migration pressure.
- SBA sources for SAM, size status, affiliation, and socioeconomic program workflows.

## Review Cadence

- High-risk content: quarterly expert review.
- CMMC, FAR, DFARS, SBA, and labor changes: monthly monitoring.
- Customer-facing content changes: release note and audit trail.
- AI-generated drafts: human approval before becoming published content.
- Customer-disputed content: triage within one business day and route to the compliance content owner.

## Human Review Workflow

Automated extraction, AI-suggested obligations, manually created obligations, and source updates must move through explicit states:

```text
draft -> needs review -> approved -> published
draft -> rejected
published -> customer disputed -> needs review
published -> retired
published -> draft replacement
```

Rules:

- Draft, rejected, and retired content is hidden from customer-facing obligation views by default.
- Content requiring expert review cannot be published without reviewer identity, review date, and source metadata.
- Customer-disputed content remains visible only with a dispute flag until the content owner resolves it.
- Replacements must preserve the old record for audit history and link to the superseding record.

## Guardrails

- Cite source clauses or internal evidence for generated answers.
- Mark AI output as draft until reviewed.
- Log prompts, retrieved sources, outputs, and approvals.
- Do not train on customer documents unless explicitly contracted and isolated.
- Do not publish pass/fail, certification, legal, accounting, labor, or CMMC assessment determinations without approved expert review.
- Keep marketing and in-product claims aligned to source-backed workflow guidance.

## Content Test Set

Before enabling automated clause extraction or AI-assisted obligation generation, maintain a representative test set of solicitations, contracts, subcontracts, flow-down attachments, purchase orders, wage determinations, DD Form 254 metadata, and CUI marking guide metadata.

Track:

- Clause extraction precision and recall.
- Missed flow-down requirements.
- Missed reporting deadlines.
- False positives.
- Data handling classification errors.
- Reviewer edits and rejection reasons.
