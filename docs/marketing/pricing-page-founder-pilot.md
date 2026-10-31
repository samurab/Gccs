# Pricing Page With Founder-Friendly Pilot Option

Document type: customer-facing pricing page draft.

Review requirement: founder, finance, and counsel review required before publication.

Publication status: draft pricing hypothesis. Do not publish or invoice from this page until finance and counsel approve the pilot terms, refund policy, payment mechanics, tax handling, and conversion credit language.

## Current-State Verification

| Pricing or offer claim | Status | Evidence |
| --- | --- | --- |
| 30-day guided pilot is the recommended first offer. | Planned business process | Documented in Phase 1 and Phase 4 guides; not a product feature. |
| `$750` flat pilot fee and conversion credit. | Planned pricing hypothesis | Requires finance/counsel approval and buyer validation. |
| Core workflow can show contract metadata, clauses, obligations, evidence metadata, reports, and audit history. | Implemented product capability | Current app exposes these tabs and flows with permission boundaries. |
| Evidence upload is No-CUI-gated. | Implemented for upload workflows | No-CUI acknowledgement and upload tests verify blocking before acknowledgement. |
| Payment processing, order form, refund policy, or annual subscription conversion. | Planned | No payment/order-form implementation or counsel-approved terms were verified for this page. |
| Tier usage quantities, metering, overages, and automated billing. | Planned | The application does not currently meter or enforce the compute-aware packaging targets below. |
| Metered LLM usage. | Do not claim as current | Current clause extraction is local deterministic processing; AI token cost is modeled only as a future scenario. |

## Page Hero

### Headline

Start with one guided compliance readiness pilot.

### Supporting copy

FeDril helps small government contractors replace scattered compliance spreadsheets with a No-CUI workspace for obligations, evidence metadata, ownership, current report artifacts, and audit history.

## Recommended First Offer

### 30-Day Guided Readiness Pilot

**Price:** $750 flat pilot fee.

**Conversion credit:** the $750 pilot fee is credited toward the first annual subscription if the customer converts within 30 days after pilot completion.

**Best for:** small government contractors, subcontractors, MSPs, and advisors who want to validate one real readiness workflow using synthetic, redacted, or non-sensitive data.

### Pilot Includes

- 30-day guided pilot workspace.
- Company compliance profile setup.
- One contract or synthetic readiness workflow.
- Manual clause attachment and generated obligation review.
- Evidence metadata setup using allowed data.
- Readiness dashboard walkthrough.
- Compliance Status, CMMC Readiness, or Evidence Package generation where applicable.
- End-of-pilot findings session.

### Pilot Excludes

- Real CUI upload, processing, storage, or support handling.
- Legal advice, accounting advice, labor determinations, or contracting-officer determinations.
- CMMC certification, assessor determinations, or assessment-success guarantees.
- Custom integrations.
- GovCloud, FedRAMP, SSO, SCIM, or enterprise procurement support.
- Automated clause extraction or unreviewed AI-generated determinations.

## Post-Pilot Pricing Hypothesis

These prices are initial market-test prices and should be validated through buyer conversations. The quantities are packaging targets, not current product-enforced limits.

| Plan | Price hypothesis | Best fit | Packaging target | Status |
| --- | --- | --- | --- | --- |
| Starter | $149/month or $1,490/year | Very small contractors starting readiness tracking | 3 users, 5 active contracts, 5 GB storage, 25 uploads/month, 20 reports/month | Planned commercial packaging; quantities are not enforced |
| Team | $399/month or $3,990/year | Contractors with multiple users, contracts, and reports | 15 users, 30 active contracts, 50 GB storage, 250 uploads/month, 100 reports/month | Planned commercial packaging; quantities are not enforced |
| Advisor | Custom, starting at $999/month after product validation | MSPs, consultants, and advisors supporting multiple customers | Target equivalent of three heavy workspaces | Planned; automated multi-client packaging is not implemented |

## Compute Economics Review

Internal model as of 2026-08-28:

- Observed Azure production footprint modeled at approximately `$284/month` of fixed platform cost before customer-success and support labor.
- At a 25-tenant reference mix of 80% lightweight and 20% heavy tenants, modeled current compute is approximately `$5.88` per lightweight tenant and `$36.95` per heavy tenant.
- At the proposed Starter and Team prices, modeled compute-only gross margin is approximately `96%` and `91%`, respectively.
- A separately labeled future AI scenario adds approximately `$0.21` per lightweight tenant and `$4.05` per heavy tenant under the model's explicit token assumptions.
- Compute-only gross margin is not total gross margin. Founder delivery, onboarding, support, expert review, payment processing, taxes, and sales costs are excluded.

The capacity curve is not load-tested, and Azure retail rates have not yet been reconciled to invoices. Do not publish compute-margin claims. Use them for internal pricing and investor preparation only.

## Qualification Requirements

The guided pilot is available only when:

- The customer identifies a named internal owner.
- The workflow can be run with synthetic, redacted, or non-sensitive data.
- The customer accepts the No-CUI pilot boundary.
- The customer agrees to a kickoff call and end-of-pilot findings session.
- The customer understands reports are workflow guidance, not certification or legal advice.

## Suggested CTA

### Primary CTA

Request a guided pilot demo.

### Secondary CTA

Review the No-CUI policy.

## FAQ

### Do we pay the customer for the pilot?

No. The customer pays FeDril for the pilot. The pilot fee validates urgency and filters out non-buyers.

### Why charge for a pilot?

A paid pilot creates commitment, identifies real purchase intent, and gives both sides a defined success window.

### Can the pilot be free?

Only for strategic design partners with clear referral, advisor, testimonial, or distribution value. Free pilots should not be the default.

### Can we upload real CUI?

No. The MVP is No-CUI / compliance management only. Use synthetic, redacted, or non-sensitive data.

### Does FeDril certify CMMC compliance?

No. FeDril supports readiness workflow tracking and evidence organization. It does not certify compliance or replace assessors, attorneys, advisors, or contracting authorities.

## Required Disclaimer

Pricing, pilot scope, and product availability are subject to written agreement. FeDril is a No-CUI compliance management and readiness workflow tool. FeDril does not provide legal advice, accounting advice, labor determinations, CMMC certification, assessment determinations, contracting-officer determinations, or government endorsement.

## Pre-Publication Checklist

| Check | Pass condition |
| --- | --- |
| Founder approval | Confirms offer, scope, and qualification requirements. |
| Finance approval | Confirms price, invoice handling, refund position, taxes, and conversion-credit treatment. |
| Counsel approval | Confirms terms, disclaimers, refund/conversion wording, and subscription transition language. |
| Product verification | Pilot includes only workflows the product currently supports or explicitly labels as guided/manual. |
| No-CUI boundary | Page excludes real CUI handling and prohibits sensitive data. |
| Buyer validation | Pricing remains a hypothesis until at least one qualified buyer pays. |
