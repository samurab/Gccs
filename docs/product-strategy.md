# Product Strategy

GCCS is a govcon-specific compliance operating system for small U.S. federal contractors and subcontractors. The core promise is to help customers know what applies, prove what they did, and stay ready for audits, renewals, bids, certifications, and prime contractor reviews.

This is product and engineering guidance, not legal advice. Production compliance content must be reviewed by qualified government contracts, cybersecurity, CMMC, labor, SBA, or finance experts as applicable.

## Target Customers

- Small federal prime contractors.
- Small subcontractors to large primes.
- DoD suppliers preparing for CMMC.
- 8(a), WOSB, HUBZone, SDVOSB, SDB, and similar small business program participants.
- MSPs, CMMC consultants, proposal managers, contracts admins, owners, HR/payroll users, and compliance advisors supporting small contractors.

## MVP Positioning

The MVP is **CUI-ready by design with gated CUI acceptance**.

GCCS should demonstrate full CUI-aware workflows from day one using synthetic, sample, or redacted data. Real customer CUI may be stored only when the tenant or deployment is explicitly approved for CUI-ready operation with the required architecture, customer terms, support process, shared responsibility matrix, and operating controls.

The product must not store classified data, ITAR/export-controlled technical data, SSNs, payroll records, protected medical or disability data, or other highly sensitive employee records unless a separate approved deployment posture exists.

CUI-readiness positioning must appear in:

- Onboarding.
- Contract upload.
- Evidence upload.
- Help and support scripts.
- Terms, privacy, and data handling documentation.
- Customer-facing sales and implementation materials.

The default demo posture should be **Demo/Sandbox CUI workflows with synthetic or redacted data**. Production tenants start with CUI upload disabled unless CUI-ready approval is granted.

## MVP Scope

Build the first release around high-frequency govcon compliance work:

- Company compliance profile.
- Contract and clause intake with manual clause tagging.
- Source-backed obligation dashboard.
- Compliance calendar.
- Evidence vault.
- CMMC Level 1 and Level 2 readiness tracker.
- Subcontractor flow-down tracker.
- Basic reports.
- Source-backed obligation library.

The MVP should optimize for traceability, task ownership, evidence readiness, and defensible workflow guidance instead of broad generic GRC coverage.

## Deferred Scope

The following should be deferred unless pilot customers make them launch blockers:

- Labor compliance module, except lightweight metadata capture.
- eSRS integration.
- SSP builder.
- SPRS score calculator.
- Full AI assistant.
- GovCloud and FedRAMP readiness.
- Public-sector direct sales features.

## Product Claims Policy

The product may say it helps users organize, track, prepare, and document compliance workflows.

The product must not claim that it:

- Provides legal advice.
- Certifies compliance.
- Guarantees CMMC readiness or assessment success.
- Replaces a CMMC assessor, attorney, CPA, labor expert, or contracting officer.
- Has government approval, endorsement, or authorization unless that is formally true and reviewed.

Customer-facing claims require source backing and review when they touch legal, labor, CMMC, SBA, certification, readiness scoring, security posture, or official-sounding determinations.

## Success Definition

MVP success means pilot users can onboard a company, enter a contract, tag clauses, classify data handling posture, generate an obligation matrix, assign tasks, upload allowed evidence, and produce a report without engineering support.

Every customer-visible MVP obligation must have:

- Source name and URL.
- Trigger condition.
- Applicability logic.
- Required action.
- Evidence examples.
- Risk level.
- Confidence label.
- Last reviewed date.
- Review owner or accountable content role.
- Review state.

## Strategic Decisions

| Decision | MVP approach |
| --- | --- |
| Data posture | CUI-ready by design; real CUI upload remains tenant-gated until approved. |
| Hosting | Commercial-cloud-friendly CUI-ready architecture first; GovCloud remains enterprise roadmap. |
| Clause handling | Manual tagging first; automated extraction later with human review. |
| AI | Deferred or draft-only, cited, logged, and review-gated. |
| Stack language | Current LTS .NET unless a runtime has already been chosen for the repo. |
| Content model | Source-controlled obligation library with review workflow and versioning. |
| Market entry | Small DoD subcontractors, MSPs/CMMC consultants, and back-office govcon teams. |
