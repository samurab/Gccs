# Phase 4: Create A Pilot Offer

Document type: founder-led first-customer pilot close guide.

Status: internal working document. Counsel review is required before this becomes an order form, pilot agreement, invoice language, public pricing page, or customer-facing contract.

Required boundary: GCCS is a No-CUI / compliance management MVP. The pilot must not include real CUI, classified information, ITAR/export-controlled data, sensitive government-furnished information, credentials, payroll records, SSNs, health data, sensitive incident details, formal assessment work, legal advice, accounting advice, CMMC certification, assessor determinations, contracting-officer determinations, or government endorsement claims.

## Phase Goal

Convert one validated discovery prospect into a paid, managed, 30-day pilot with a narrow workflow, clear success criteria, weekly feedback, and a conversion decision.

The first customer should not buy a broad subscription. A broad subscription creates too many undefined expectations before the workflow, onboarding motion, support burden, price, and conversion path are proven.

The correct first sale is a managed pilot.

## Critique And Failure Risks

- Selling a broad subscription first will likely fail because the buyer has not yet proven which workflow is worth recurring payment.
- Accepting an "interesting" prospect wastes founder time. Interest without money, workflow access, feedback, and conversion intent is not customer validation.
- Running an unpaid pilot creates weak commitment and increases the risk that GCCS becomes free consulting instead of a validated SaaS workflow.
- Expanding scope to CUI, certification, legal advice, custom integrations, or formal assessment work would violate the MVP boundary and create security, legal, and delivery risk.
- Letting the customer define success after kickoff creates scope creep. Success criteria must be agreed before payment.

## Correct Pilot Offer

### Offer Name

30-Day Guided Readiness Pilot.

### Default Price

Use `$750` as the default first-customer pilot fee.

Allowed test range: `$500-$1,500` flat pilot fee.

Use the range this way:

| Price | Use when |
| ---: | --- |
| `$500` | Strategic design partner, strong referral value, clear testimonial potential, very small workflow, low support burden. |
| `$750` | Default first paid pilot. Balanced price for validation, commitment, and founder-led support. |
| `$1,000-$1,500` | More complex workflow, more users, advisor/MSP involvement, or high-value customer with clear conversion path. |

Do not discount below `$500` unless the prospect has unusually strong distribution value and signs up for specific discovery obligations.

### Conversion Credit

Credit the pilot fee toward the first annual subscription if the customer converts within the agreed conversion window.

Recommended conversion window: `30 days after pilot completion`.

Example language for counsel review:

`If Customer converts to an annual GCCS subscription within 30 days after pilot completion, the paid pilot fee will be credited toward the first annual subscription according to the applicable order form.`

### Duration

30 calendar days from kickoff.

Do not let the pilot drift into an open-ended trial. If the customer needs more time, require a written extension with a defined reason, new end date, and decision date.

## Required First-Customer Qualification

Your first paid customer target is not someone who says "interesting."

The first paid customer must be willing to provide all of the following:

| Requirement | Required proof before kickoff | Why it matters |
| --- | --- | --- |
| Money | Paid pilot fee or signed order form with payment date. | Validates urgency and separates buyers from curious prospects. |
| Real workflow data | Synthetic, redacted, or non-sensitive version of one real workflow. | Lets GCCS validate actual task, obligation, evidence, owner, and report behavior without handling prohibited data. |
| Weekly feedback | Named attendee for weekly check-ins. | Prevents silent failure and creates learning cadence. |
| Permission to use anonymized learnings | Written permission to use non-identifying workflow learnings. | Enables future marketing, onboarding, and product decisions without exposing customer details. |
| Testimonial if successful | Agreement to consider a testimonial, reference quote, or anonymized case note after success. | Creates evidence for the next customer. |

If any of these are missing, the prospect is not the correct first paid pilot unless there is a documented strategic reason.

## Pilot Includes

The managed pilot includes:

- One setup call.
- No-CUI boundary confirmation.
- One contract, program, subcontract, readiness process, or synthetic workflow.
- Obligation import or setup using allowed data and current app-supported workflow.
- Contract metadata setup in `Contracts`.
- Clause attachment or review using `Attached clauses` in `Contracts` and `Clause library search` in `Obligations` where applicable.
- Generated obligation review in `Obligations`.
- Owner assignment and workflow status updates.
- Evidence metadata setup in `Evidence` using synthetic, redacted, or non-sensitive data only.
- One report artifact from `Reports`: Compliance Status, CMMC Readiness, Subcontractor Compliance, or Evidence Package, as applicable.
- Weekly feedback calls during the pilot.
- End-of-pilot feedback and conversion session.

## Pilot Excludes

The pilot excludes:

- Real CUI upload, processing, storage, or support handling.
- Classified information.
- ITAR/export-controlled data.
- Sensitive government-furnished information.
- Credentials, passwords, secrets, private keys, unrestricted security logs, payroll records, SSNs, bank data, tax data, health data, disability data, or sensitive incident details.
- Legal advice.
- Accounting advice.
- Labor determinations.
- CMMC certification.
- C3PAO, assessor, or formal assessment determinations.
- Contracting-officer determinations.
- Government endorsement or government approval claims.
- Custom integrations.
- GovCloud, FedRAMP, SSO, SCIM, procurement portals, or enterprise procurement support.
- Custom report development.
- Bulk migration.
- Customer-specific compliance interpretations not reviewed by qualified advisors.

## Pilot Scope

Use one of these pilot scopes.

| Scope | Best fit | Primary artifact |
| --- | --- | --- |
| Contract readiness workflow | Contractor has one contract or subcontract they need to organize. | Compliance Status or Evidence Package. |
| CMMC readiness workflow | Contractor wants to organize readiness tasks without formal assessment claims. | CMMC Readiness report. |
| Evidence metadata workflow | Contractor loses track of proof, owners, and dates. | Evidence Package. |
| Subcontractor flow-down workflow | Prime/subcontractor context needs obligations and proof tracking. | Subcontractor Compliance report. |
| Synthetic modeled workflow | Customer cannot use real contract names or details. | Synthetic report artifact. |

Only one scope should be selected for the first pilot. Multiple workflows should require a separate phase, paid extension, or subscription conversion.

## Required Pre-Payment Checklist

Do not accept pilot payment until these items are confirmed.

| Item | Decision |
| --- | --- |
| Customer has a painful workflow validated during Phase 3. | Yes / No |
| Customer has a named internal pilot owner. | Yes / No |
| Customer can complete the pilot without real CUI or prohibited data. | Yes / No |
| Customer accepts the No-CUI posture in writing. | Yes / No |
| Customer agrees to weekly feedback. | Yes / No |
| Customer agrees to provide synthetic, redacted, or non-sensitive workflow data. | Yes / No |
| Customer agrees to an end-of-pilot conversion decision. | Yes / No |
| Customer understands reports are workflow guidance, not legal advice or certification decisions. | Yes / No |
| Customer is not requiring custom integrations or enterprise procurement. | Yes / No |
| Founder, security owner, and counsel have approved customer-facing pilot language. | Yes / No |

If any answer is `No`, do not close the pilot until the issue is resolved or the prospect is disqualified.

## Pilot Deliverables

| Deliverable | Completion standard |
| --- | --- |
| Kickoff completed | Customer owner, GCCS owner, workflow, data boundary, and calendar are confirmed. |
| No-CUI acknowledgement completed | Customer accepts the No-CUI posture before upload-related workflows. |
| Workflow configured | One contract, program, subcontract, readiness process, or synthetic workflow is represented in GCCS. |
| Obligations visible | Relevant source-backed obligations are visible and reviewed for workflow usefulness. |
| Owners assigned | At least one obligation or task has owner/status tracking configured. |
| Evidence metadata linked | Allowed evidence metadata is linked to obligations or controls where applicable. |
| Report generated | One report artifact is generated from `Reports` and reviewed as workflow guidance only. |
| Feedback captured | Weekly feedback notes are captured and summarized. |
| Conversion session completed | Customer decides to convert, extend under written scope, or stop. |

## Week-By-Week Execution Plan

| Week | Founder action | Customer action | Output |
| --- | --- | --- | --- |
| Pre-kickoff | Send pilot scope, No-CUI policy, payment link or order form, and kickoff agenda. | Pay fee, name owner, confirm No-CUI boundary, send allowed workflow summary. | Signed or paid pilot ready for kickoff. |
| Week 1 | Run setup call, confirm workflow, configure account/profile/contract metadata. | Attend kickoff, validate workflow, provide synthetic/redacted/non-sensitive inputs. | Pilot workspace and workflow scope locked. |
| Week 2 | Attach/review clauses, review generated obligations, assign owners/statuses. | Confirm whether obligations match the real workflow pain. | Source-backed workflow visible. |
| Week 3 | Configure evidence metadata and link it to obligations or controls. | Validate whether evidence tracking solves the broken spreadsheet problem. | Evidence metadata workflow working. |
| Week 4 | Generate report artifact, run feedback session, propose conversion path. | Decide whether to convert, extend under paid scope, or stop. | Conversion decision and lessons learned. |

## Setup Call Agenda

Use this agenda for the first customer setup call.

1. Confirm attendees and decision owner.
2. Restate the No-CUI boundary.
3. Confirm the selected pilot scope.
4. Confirm the workflow to model.
5. Confirm what synthetic, redacted, or non-sensitive data will be used.
6. Confirm the report artifact to generate.
7. Confirm weekly feedback meeting times.
8. Confirm end-of-pilot success criteria.
9. Confirm conversion decision date.
10. Confirm support process and prohibited-data handling.

## Weekly Feedback Questions

Ask these questions every week:

1. What part of this workflow replaced something you currently do manually?
2. What still forced you back into a spreadsheet, email, or shared folder?
3. Which field, label, report, or status was confusing?
4. Which evidence, obligation, or owner-tracking step felt useful?
5. What would prevent your team from using this weekly?
6. What would make this worth the post-pilot subscription price?
7. What should be removed, simplified, or renamed?

## End-Of-Pilot Conversion Session

The final session should produce a decision, not vague interest.

### Conversion Session Agenda

1. Review the original workflow pain.
2. Review what GCCS configured.
3. Review the report artifact.
4. Review what worked.
5. Review what failed or remained manual.
6. Review No-CUI fit.
7. Review subscription price and conversion credit.
8. Ask for a yes/no/extension decision.
9. If successful, ask for testimonial or anonymized case note.
10. Confirm next steps in writing.

### Decision Options

| Decision | Meaning | Next step |
| --- | --- | --- |
| Convert | Pilot solved enough pain to justify subscription. | Apply pilot fee credit to annual subscription. |
| Paid extension | Workflow is promising but needs a defined second scope. | Create paid extension with new scope, fee, and end date. |
| Stop | Workflow, timing, price, or No-CUI boundary is not a fit. | Close pilot and document lessons learned. |

Do not accept "let's keep talking" as a completed decision.

## Pilot Close Script

Use this after a qualified demo or Phase 3 discovery follow-up.

`Based on what you described, I would not recommend starting with a broad subscription. The correct next step is a 30-day guided readiness pilot around one workflow. The pilot is No-CUI only, costs $750, and is credited toward the first annual subscription if you convert within 30 days after completion. It includes setup, obligation workflow configuration, evidence metadata, one report artifact, weekly feedback, and an end-of-pilot decision session. It excludes real CUI, legal advice, certification claims, formal assessment work, and custom integrations.`

`For this to be a fit, I need a named pilot owner, weekly feedback, allowed workflow data, permission to use anonymized learnings, and willingness to provide a testimonial or reference quote if the pilot succeeds. Are you comfortable moving to a written pilot scope and payment?`

## Follow-Up Email After Verbal Yes

Subject: GCCS 30-day guided pilot - scope and next steps

Hi `<Name>`,

Based on our discussion, the proposed next step is a 30-day GCCS Guided Readiness Pilot.

Proposed pilot:

- Fee: `$750` flat pilot fee.
- Conversion credit: credited toward the first annual subscription if you convert within 30 days after pilot completion.
- Duration: 30 calendar days.
- Scope: one No-CUI readiness workflow.
- Includes: setup call, obligation workflow configuration, evidence metadata setup, one report artifact, weekly feedback, and end-of-pilot decision session.
- Excludes: real CUI, legal advice, accounting advice, certification claims, formal assessment work, government determinations, custom integrations, and enterprise procurement support.

Before kickoff, we need:

1. Named internal pilot owner.
2. Confirmation that the pilot will use only synthetic, redacted, or non-sensitive data.
3. Weekly feedback time.
4. Agreement that anonymized, non-identifying learnings may be used to improve GCCS.
5. Agreement to consider a testimonial, reference quote, or anonymized case note if the pilot is successful.

If this matches your understanding, the next step is the pilot scope confirmation and payment/order form.

Thank you,
`<Your Name>`

## Pilot Agreement Points For Counsel

Counsel should review these before external use:

- Pilot fee amount and refund policy.
- Conversion credit mechanics.
- Payment terms.
- Scope and deliverables.
- No-CUI data posture.
- Prohibited-data handling.
- Customer responsibility for data classification.
- Professional-advice disclaimers.
- Report and readiness-status disclaimers.
- Confidentiality.
- Anonymized-learning permission.
- Testimonial/reference permission.
- Support boundaries.
- Termination.
- Limitation of liability.
- Warranty disclaimer.
- Governing law and venue.

## First-Customer Scorecard

Use [phase-4-pilot-candidate-scorecard.csv](./phase-4-pilot-candidate-scorecard.csv) to compare candidates.

| Criterion | Score 0 | Score 1 | Score 2 |
| --- | --- | --- | --- |
| Payment | Will not pay. | Might pay later. | Pays or signs now. |
| Workflow data | No workflow access. | Vague workflow. | Specific allowed workflow data. |
| Weekly feedback | No commitment. | Informal availability. | Recurring weekly time agreed. |
| No-CUI fit | Needs prohibited data. | Unclear. | Accepts No-CUI clearly. |
| Testimonial potential | Refuses. | Maybe if successful. | Agrees to consider testimonial/reference/anonymized case note. |
| Conversion intent | Just exploring. | Possible future need. | Has a post-pilot buying decision. |

Minimum score to proceed: `9 / 12`.

Ideal first pilot score: `11 / 12` or higher.

## Phase 4 Exit Criteria

Phase 4 is complete when:

- One pilot customer is selected using the scorecard.
- Pilot scope is written.
- Pilot fee is paid or order form is signed.
- No-CUI boundary is acknowledged.
- Customer owner is named.
- Weekly feedback meetings are scheduled.
- Allowed workflow data is prepared.
- End-of-pilot conversion decision date is scheduled.
- Counsel-reviewed pilot language is ready before customer-facing contractual use.

## Hidden Risks

- A customer may verbally accept No-CUI but later paste sensitive details into descriptions, support messages, screenshots, or uploads. Reinforce prohibited-data handling in kickoff and every support channel.
- A broad pilot scope can become unpaid implementation consulting. Keep the pilot to one workflow.
- A testimonial cannot be assumed. Get permission language reviewed and documented before using customer names, logos, quotes, or case studies.
- The phrase "real workflow data" can be misread as permission to use sensitive data. Always say synthetic, redacted, or non-sensitive workflow data.
- Conversion credit can create accounting, tax, revenue-recognition, or contract issues. Have counsel and finance review before invoicing.
- Advisor-led pilots may produce useful product feedback but no direct buyer. Confirm who can authorize subscription conversion.
- A successful pilot does not prove scalable onboarding. Document founder time spent so pricing and implementation scope can be adjusted.

## Related Documents

- [Phase 1: Pilot Offer](./phase-1-pilot-offer.md)
- [Phase 1: Pilot Scope And Success Plan](./phase-1-pilot-scope-success-plan.md)
- [Pricing Page With Founder-Friendly Pilot Option](./pricing-page-founder-pilot.md)
- [Terms And Disclaimer Language For Counsel Review](./terms-disclaimer-counsel-review.md)
- [Phase 3: Validate With 20 Conversations](./phase-3-validate-with-20-conversations.md)
- [Sample Obligation/Evidence Workflow](./sample-obligation-evidence-workflow.md)
- [No-CUI Policy Statement](./no-cui-policy-statement.md)
