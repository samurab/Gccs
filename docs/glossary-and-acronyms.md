# Glossary And Acronyms

This reference explains common government contracting, compliance, cybersecurity, product, and application terms used across GCCS documentation.

GCCS is product and engineering scaffolding, not legal advice. Definitions in this document are intended to help new team members, customers, advisors, and developers understand the application context. Production compliance content should be reviewed by qualified government contracting, cybersecurity, CMMC, labor, SBA, finance, or legal experts as applicable.

## How To Read The Product Domain

Small businesses that sell to the U.S. federal government often need to prove that they understand and follow contract-specific requirements. Those requirements may come from the company profile, the opportunity, the contract, incorporated clauses, subcontractor relationships, cybersecurity data handling, labor rules, or small business certification rules.

GCCS organizes that work into a traceable workflow:

```text
Company profile
-> Contract or solicitation intake
-> Clauses and source rules
-> Applicability decisions
-> Obligations
-> Tasks
-> Evidence
-> Reports and audit trail
```

The MVP posture is **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**. Demo tenants may use synthetic or redacted CUI examples, but users should not upload real customer CUI unless the tenant is explicitly approved for future `CuiReady` operation. Classified data, export-controlled technical data, payroll records, SSNs, secrets, private keys, and other prohibited sensitive information require a separately approved deployment posture.

## Acronym Quick Reference

| Acronym | Meaning | Plain-English context |
| --- | --- | --- |
| 8(a) | SBA 8(a) Business Development Program | SBA program for eligible socially and economically disadvantaged small businesses. |
| ACO | Administrative Contracting Officer | Government contracting official who may administer parts of a contract after award. |
| API | Application Programming Interface | A structured way for systems to exchange data. |
| BPA | Blanket Purchase Agreement | A purchasing arrangement used to simplify repeated buys. |
| CAGE | Commercial and Government Entity Code | Identifier often used for entities doing business with the government. |
| CDI | Covered Defense Information | DoD-related controlled information category commonly tied to DFARS cybersecurity clauses. |
| CFR | Code of Federal Regulations | The organized body of federal regulations. |
| CMMC | Cybersecurity Maturity Model Certification | DoD cybersecurity program for defense contractors and subcontractors. |
| CO | Contracting Officer | Government official with authority to enter, administer, or terminate contracts. |
| COR | Contracting Officer's Representative | Government representative who helps monitor contract performance but usually cannot change contract terms. |
| COTS | Commercial Off-The-Shelf | Commercial item sold in substantial quantities and used without major modification. |
| CPARS | Contractor Performance Assessment Reporting System | Government system for contractor past performance evaluations. |
| CUI | Controlled Unclassified Information | Sensitive but unclassified government information that requires safeguarding or dissemination controls. |
| DBA | Davis-Bacon Act | Labor standards law commonly relevant to federal construction work. |
| DFARS | Defense Federal Acquisition Regulation Supplement | DoD supplement to the FAR. |
| DIB | Defense Industrial Base | Companies and organizations supporting U.S. defense work. |
| DoD | Department of Defense | Federal department responsible for defense and military operations. |
| DOL | Department of Labor | Federal department responsible for labor standards and worker protections. |
| EDWOSB | Economically Disadvantaged Women-Owned Small Business | SBA women-owned small business program category. |
| ESP | External Service Provider | Third party that provides services affecting a contractor's systems or security responsibilities. |
| eSRS | Electronic Subcontracting Reporting System | System used for subcontracting plan reporting. |
| FAR | Federal Acquisition Regulation | Primary set of rules for federal government procurement. |
| FCI | Federal Contract Information | Non-public information provided by or generated for the government under a contract. |
| FIPS | Federal Information Processing Standards | Federal standards for information processing and security requirements. |
| FOCI | Foreign Ownership, Control, or Influence | Security concern for contractors with foreign ownership or influence. |
| FOUO | For Official Use Only | Legacy marking sometimes seen in older documents; not the same as modern CUI. |
| FTE | Full-Time Equivalent | Staffing measure used for workforce capacity. |
| GRC | Governance, Risk, and Compliance | Broad category of tools and processes for managing risk and compliance. |
| HUBZone | Historically Underutilized Business Zone | SBA program for eligible businesses in designated areas. |
| IDIQ | Indefinite Delivery, Indefinite Quantity | Contract vehicle for future orders when exact timing or quantity is not known. |
| ISR | Individual Subcontracting Report | eSRS report for subcontracting activity under a specific contract. |
| ITAR | International Traffic in Arms Regulations | Export-control rules for defense articles, services, and technical data. |
| KO | Contracting Officer | Alternate abbreviation often used by DoD for contracting officer. |
| LPTA | Lowest Price Technically Acceptable | Source selection method where technically acceptable proposals compete primarily on price. |
| MFA | Multi-Factor Authentication | Login control requiring more than one proof of identity. |
| MSP | Managed Service Provider | Third-party IT services provider. |
| NAICS | North American Industry Classification System | Industry code used for business classification and size standards. |
| NDA | Non-Disclosure Agreement | Agreement restricting disclosure of confidential information. |
| NIST | National Institute of Standards and Technology | Federal agency that publishes cybersecurity standards and guidance. |
| OIDC | OpenID Connect | Authentication protocol commonly used for single sign-on. |
| P-ATO | Provisional Authority to Operate | FedRAMP authorization milestone for cloud services. |
| POA&M | Plan of Action and Milestones | Remediation plan for open security or compliance gaps. |
| POC | Point of Contact | Person responsible for a topic, account, contract, or workflow. |
| PWS | Performance Work Statement | Defines required outcomes and performance standards. |
| RACI | Responsible, Accountable, Consulted, Informed | Role model for ownership and decision rights. |
| RAG | Retrieval-Augmented Generation | AI approach that grounds answers in retrieved sources. |
| RBAC | Role-Based Access Control | Permissions model based on assigned user roles. |
| RFI | Request for Information | Market research request before a formal procurement. |
| RFP | Request for Proposal | Solicitation asking vendors to submit proposals. |
| RFQ | Request for Quote | Solicitation asking vendors to submit quotes, often for simpler purchases. |
| SAM | System for Award Management | Federal registration system for entities doing business with the government. |
| SAML | Security Assertion Markup Language | Enterprise single sign-on protocol. |
| SBA | Small Business Administration | Federal agency supporting small businesses and small business contracting programs. |
| SCA | Service Contract Act | Labor standards law for certain federal service contracts, now reflected in service contract labor standards terminology. |
| SDB | Small Disadvantaged Business | Small business category tied to disadvantaged ownership or status. |
| SDVOSB | Service-Disabled Veteran-Owned Small Business | Small business program category for eligible service-disabled veteran-owned firms. |
| SIEM | Security Information and Event Management | Security logging and monitoring system. |
| SLA | Service Level Agreement | Commitment for service availability, response, or support performance. |
| SOC 2 | System and Organization Controls 2 | Security and privacy assurance report often requested by SaaS customers. |
| SOO | Statement of Objectives | High-level outcomes document that lets vendors propose an approach. |
| SOW | Statement of Work | Work description for a contract or subcontract. |
| SPRS | Supplier Performance Risk System | DoD system associated with supplier risk and NIST SP 800-171 assessment scores. |
| SSP | System Security Plan | Document describing a system boundary, environment, controls, and security implementation. |
| SSR | Summary Subcontract Report | eSRS report summarizing subcontracting activity. |
| T&M | Time and Materials | Contract type where payment is based on labor hours and materials. |
| UEI | Unique Entity ID | Entity identifier used in SAM.gov. |
| WOSB | Women-Owned Small Business | SBA women-owned small business program category. |

## Business And Contracting Terms

### Agency

A federal department or office buying goods or services, such as DoD, DHS, VA, GSA, NASA, or a civilian agency component.

### Award

The government's selection of a vendor and creation of a contract, order, or agreement.

### Bid, Quote, Offer, And Proposal

Common terms for a vendor's response to a government opportunity. The exact term depends on the solicitation method and procurement rules.

### Blanket Purchase Agreement

A BPA is a simplified arrangement for repeated purchases. It is not always the same as a contract by itself, but orders under it can create binding obligations.

### Clause

A contract provision that imposes rights, duties, restrictions, or procedures. Examples include FAR 52.204-21 for basic safeguarding and FAR 52.222-41 for service contract labor standards.

In GCCS, clauses are mapped to obligations, evidence, owners, risk levels, and source references.

### Contract

A legally binding agreement between the government and a contractor, or between a prime contractor and a subcontractor. In the app, a contract is a central organizing object for clauses, tasks, deadlines, evidence, reports, and subcontractor flow-downs.

### Contract Type

The pricing or performance structure for a contract. Examples include fixed-price, cost-reimbursement, time-and-materials, labor-hour, and IDIQ orders. Contract type can affect risk, reporting, invoicing, and compliance workflows.

### Contracting Officer

The government official with authority to bind the government. Only the contracting officer, sometimes abbreviated CO or KO, can usually change contract terms.

### Contracting Officer's Representative

A COR monitors technical performance and communicates with the contractor, but usually cannot modify contract terms, price, scope, or schedule.

### Deliverable

Something the contractor must provide, such as a report, product, service, data file, certification, plan, or evidence package.

### Flow-Down

A requirement that a prime contractor must include, or "flow down," to subcontractors. Flow-downs matter because a small subcontractor may need to follow government-derived requirements even without a direct contract with the government.

### Prime Contractor

The company that contracts directly with the federal government.

### Purchase Order

A purchasing document that may contain contract terms, delivery requirements, flow-down clauses, or compliance obligations.

### Solicitation

The government's request for vendors to respond. Examples include RFPs, RFQs, and IFBs. GCCS treats solicitations as important because obligations may be visible before award.

### Statement Of Work, Performance Work Statement, And Statement Of Objectives

These documents describe what work must be done.

- SOW: describes work activities and deliverables.
- PWS: emphasizes outcomes and performance standards.
- SOO: gives objectives and lets the contractor propose the method.

### Subcontractor

A company performing part of a prime contract or higher-tier subcontract. Subcontractors may inherit requirements through flow-down clauses, cybersecurity rules, data handling expectations, labor requirements, and prime-specific evidence requests.

### Workshare

The portion of contract work performed by a party. Workshare matters for subcontractor management, small business rules, limitations on subcontracting, and teaming arrangements.

## Small Business And Entity Terms

### Affiliation

SBA concept used to determine whether another company controls or has the power to control a business. Affiliation can affect whether a company qualifies as small.

### CAGE Code

A CAGE code is an identifier for entities doing business with the government. It is often part of a contractor's profile alongside UEI and SAM registration status.

### NAICS Code

NAICS codes classify industries. In government contracting, a solicitation usually has a NAICS code, and SBA size standards determine whether a company is small for that code.

### SAM Registration

SAM.gov is the federal system where entities register to do business with the government. SAM registration and renewal dates are important calendar items in GCCS.

### Set-Aside

A procurement reserved for a category of small business, such as small business, 8(a), WOSB, HUBZone, or SDVOSB participants.

### Size Standard

The SBA threshold used to decide whether a company is small for a NAICS code. The threshold may be based on revenue, number of employees, or other rules.

### Socioeconomic Certification

A certification or status tied to small business contracting programs. Examples include 8(a), WOSB, EDWOSB, HUBZone, SDVOSB, and SDB.

### UEI

The Unique Entity ID used in SAM.gov to identify an entity.

## Cybersecurity And Data Handling Terms

### Asset

A system, application, device, server, endpoint, cloud service, account, or data repository that may be part of a contractor's environment.

### CMMC

CMMC is the DoD cybersecurity program for defense contractors and subcontractors. GCCS tracks readiness work, evidence, controls, gaps, POA&Ms, and affirmations, but it does not certify a customer.

### CMMC Level 1

CMMC level focused on basic safeguarding of FCI. In the app, Level 1 readiness is usually represented as a checklist, task set, evidence map, and source-backed guidance.

### CMMC Level 2

CMMC level focused on protecting CUI using NIST SP 800-171 control requirements. In the app, Level 2 readiness may involve control-by-control evidence, an SSP, POA&Ms, asset inventory, data-flow mapping, and an MSP or ESP responsibility matrix.

### Controlled Unclassified Information

CUI is information that is not classified but still requires safeguarding or dissemination controls under law, regulation, or government-wide policy. The MVP prohibits CUI uploads unless a separately approved future `CuiReady` deployment exists.

### Covered Defense Information

CDI is a DoD-related category associated with DFARS cybersecurity requirements. CDI often overlaps with CUI concepts in defense contracting.

### Data Classification

The process of identifying what kind of data is being handled, such as public, internal, FCI, CUI, export-controlled, classified, payroll, or personal data. Classification affects upload rules, storage controls, access restrictions, and product scope.

### External Service Provider

An ESP is a third party that provides services affecting the contractor's system security or compliance responsibilities. MSPs, cloud providers, security monitoring vendors, and help desk providers may be ESPs depending on the environment and services.

### Federal Contract Information

FCI is non-public information provided by or generated for the government under a contract. FAR 52.204-21 establishes basic safeguarding requirements for covered contractor information systems that process, store, or transmit FCI.

### FedRAMP

FedRAMP is a federal authorization program for cloud products and services used by federal agencies. For GCCS, FedRAMP readiness is future-state unless the business commits to federal agency buyers or enterprise deployment requirements.

### GovCloud

Government-oriented cloud regions, such as AWS GovCloud or Azure Government, designed for higher-assurance workloads. GovCloud alone does not automatically make an application compliant.

### Incident

A security or privacy event that may require investigation, containment, notification, customer communication, or corrective action.

### ITAR And Export-Controlled Data

ITAR regulates defense articles, defense services, and related technical data. Export-controlled technical data is prohibited in the No-CUI MVP with synthetic CUI-ready demonstration workflows unless a separately approved environment and operating model exist.

### MFA

Multi-factor authentication requires users to provide more than one kind of identity proof, such as password plus authenticator app.

### NIST SP 800-171

NIST Special Publication 800-171 defines security requirements for protecting CUI in nonfederal systems. CMMC currently references NIST SP 800-171 Rev. 2 in 32 CFR Part 170, while customers may ask about Rev. 3 because NIST finalized it in 2024.

### POA&M

A Plan of Action and Milestones tracks security gaps, planned remediation, owners, dates, and status.

### Shared Responsibility Matrix

A document explaining which security and compliance responsibilities belong to the customer, GCCS, MSPs, cloud providers, or other third parties.

### SPRS Score

A DoD supplier risk or assessment score associated with NIST SP 800-171 self-assessment reporting. GCCS may help organize inputs and evidence but should not overstate official assessment outcomes.

### SSP

A System Security Plan describes the system boundary, environment, assets, controls, implementation details, and responsibilities.

### System Boundary

The defined scope of systems, networks, applications, users, data flows, and services included in a security assessment or compliance workspace.

## Labor And Workforce Terms

### Certified Payroll

Payroll reporting commonly associated with construction labor compliance. It may include sensitive payroll data and is deferred or restricted in the MVP unless approved controls exist.

### Davis-Bacon Act

The DBA is relevant to certain federal construction contracts and requires labor standards such as prevailing wage compliance.

### Fringe Benefits

Non-wage compensation such as health benefits, vacation, pension, or other benefits that may be part of labor standards compliance.

### Labor Category

A role or job category used to map employees or contractor staff to contract pricing, wage determinations, or labor standards requirements.

### Place Of Performance

The location where contract work is performed. It can affect labor standards, wage determinations, tax, security, and other compliance workflows.

### Service Contract Labor Standards

Rules formerly commonly referred to by the Service Contract Act or SCA. They can require wage determinations, fringe benefits, employee notice, and recordkeeping for covered service contracts.

### Wage Determination

A wage and fringe benefit schedule that may apply to service or construction work based on location, labor category, and contract coverage.

## GCCS Application Terms

### Applicability Logic

The reasoning used to decide whether a clause, rule, or obligation applies to a company, contract, subcontractor, data type, place of performance, or module.

In GCCS, applicability should be source-backed, reviewable, and traceable. The app should avoid final legal determinations unless reviewed by qualified experts.

### Audit Log

An immutable or tamper-resistant record of important actions, such as login, upload, deletion, export, evidence approval, obligation publication, role change, and data classification acknowledgement.

### Compliance Calendar

The app module that tracks renewal dates, report deadlines, evidence expirations, training due dates, policy reviews, contract deliverables, option notices, CMMC affirmations, and other compliance tasks.

### Compliance Content

The curated library of clauses, rules, obligations, evidence examples, source links, trigger conditions, risk levels, and review metadata used by the app.

### Confidence Label

A content metadata field that tells users how reliable or mature an obligation mapping is. Example values may include high, medium, low, or requires expert review.

### Evidence

Documentation or artifacts that support completion of a requirement. Examples include policies, screenshots, training records, signed flow-downs, vendor attestations, access reviews, risk assessments, and meeting notes.

In the No-CUI MVP with synthetic CUI-ready demonstration workflows, evidence uploads must not include prohibited sensitive data.

### Evidence Vault

The GCCS module for storing, tagging, linking, reviewing, approving, expiring, and exporting evidence. The vault should support tagging by obligation, contract, control, vendor, subcontractor, employee, and report.

### Expert Review

Review by a qualified subject matter expert, such as a government contracts attorney, CMMC assessor or registered practitioner, labor expert, CPA, security lead, or compliance advisor.

### Last Reviewed Date

The date content was last reviewed by the responsible content owner or expert. This is important because government rules, clauses, and program requirements change.

### NoCui Mode

A tenant data handling mode where GCCS may manage compliance workflows and non-sensitive evidence, but does not accept real customer CUI, classified data, export-controlled technical data, or other prohibited sensitive information. Demo and sandbox tenants may use synthetic or redacted CUI examples without enabling real CUI upload.

### Obligation

A specific action, control, deadline, evidence requirement, reporting duty, flow-down, or review activity derived from a source rule, clause, contract, or company profile.

Example:

```text
Source: FAR 52.204-21
Trigger: Contract involves Federal Contract Information.
Action: Apply basic safeguarding controls.
Evidence: access control policy, MFA configuration, media disposal record, antivirus logs.
Owner: IT/security.
```

### Obligation Dashboard

The GCCS view that helps users see what applies, who owns it, what evidence is needed, when it is due, and what source supports it.

### Obligation Library

The source-controlled package of reusable obligations. Each production obligation should include source name, source URL, last reviewed date, trigger condition, applicability logic, required action, evidence examples, risk level, confidence, and review state.

### Owner

The person or function responsible for completing or reviewing an item. Common owner categories include contracts, IT/security, HR, finance, legal, compliance, customer success, engineering, and subcontractor manager.

### Report

A generated output such as an obligation matrix, CMMC readiness report, evidence package, compliance status report, subcontractor status report, or audit log export.

### Review State

The governance status of content or AI output. Common states include draft, needs review, approved, rejected, customer disputed, published, retired, or superseded.

### Risk Level

A label that helps users prioritize. Risk may reflect potential impact, likelihood, customer sensitivity, legal exposure, security exposure, missed deadline consequences, or evidence difficulty.

### Source Reference

The authoritative citation behind a requirement. Source references may include FAR, DFARS, CFR, NIST, SBA, SAM.gov, eSRS, DOL, agency supplements, contract documents, or reviewed internal guidance.

### Task

An assigned unit of work with owner, due date, status, source context, and evidence requirements.

### Tenant

A customer account or isolated organization in the SaaS platform. Tenant isolation means one customer's data must not be visible to another customer.

### Trigger Condition

The fact pattern that causes an obligation to apply. Examples include "contract includes FAR 52.204-21," "company handles FCI," "subcontractor has CUI access," "SAM registration expires in 60 days," or "service work is performed in a location with a wage determination."

## AI And Automation Terms

### AI Draft

AI-generated content that has not been approved by a human reviewer. In GCCS, AI output should be clearly labeled draft-only unless reviewed.

### Clause Extraction

The process of identifying clauses from solicitations, contracts, subcontracts, purchase orders, or flow-down attachments.

### Gap Analysis

A comparison between current evidence or controls and expected obligations. GCCS may help draft gap findings, but high-risk conclusions require human review.

### Hallucination

An AI output that sounds plausible but is unsupported, incorrect, or invented. GCCS mitigates this through retrieval, citations, draft labels, logs, and review workflows.

### Human Review State

Workflow status indicating whether extracted clauses, generated obligations, AI summaries, or reports have been reviewed before users rely on them.

### Retrieval-Augmented Generation

An AI pattern where generated answers are grounded in retrieved source material, such as the obligation library, contract documents, or reviewed internal evidence.

## Security And SaaS Terms

### Encryption At Rest

Protecting stored data using encryption, such as database, object storage, or backup encryption.

### Encryption In Transit

Protecting data moving over networks, usually with TLS.

### Least Privilege

Giving users, admins, services, and integrations only the access they need to perform their work.

### Object Storage

Storage used for files such as uploaded evidence, generated reports, source snapshots, and exports.

### RBAC

Role-Based Access Control. GCCS should enforce permissions server-side, not only in the user interface.

### SSO

Single sign-on lets users authenticate through an identity provider such as Microsoft Entra ID, Okta, or Google Workspace.

### Tenant Isolation

Technical and operational controls that prevent one customer from accessing another customer's records, files, reports, or audit logs.

### Vulnerability Scanning

Automated review for known security weaknesses in application code, dependencies, containers, infrastructure, or cloud configuration.

## Common Source Families

These are common sources referenced in GCCS planning and content governance:

| Source family | Why it matters |
| --- | --- |
| FAR | Primary federal procurement rules and clauses. |
| DFARS | DoD-specific procurement and cybersecurity clauses. |
| 32 CFR Part 170 | CMMC program regulation. |
| NIST SP 800-171 | Security requirements for protecting CUI in nonfederal systems. |
| NARA CUI Registry | Government-wide CUI category reference. |
| SBA | Small business size, certification, and contracting program guidance. |
| SAM.gov | Entity registration and federal award ecosystem data. |
| eSRS | Subcontracting plan reporting. |
| DOL | Labor standards, wage determinations, and worker protections. |

Useful public references:

- SBA basic federal contracting requirements: <https://www.sba.gov/federal-contracting/contracting-guide/basic-requirements>
- SBA size standards: <https://www.sba.gov/federal-contracting/contracting-guide/size-standards>
- SBA governing rules and responsibilities: <https://www.sba.gov/federal-contracting/contracting-guide/governing-rules-responsibilities>
- FAR 52.204-21: <https://www.acquisition.gov/far/52.204-21>
- FAR Part 22: <https://www.acquisition.gov/far/part-22>
- FAR 52.222-41: <https://www.acquisition.gov/far/52.222-41>
- 32 CFR Part 170: <https://www.ecfr.gov/current/title-32/subtitle-A/chapter-I/subchapter-G/part-170>
- DoD CMMC resources: <https://dodcio.defense.gov/CMMC/Resources-Documentation/>
- NIST SP 800-171 Rev. 3: <https://csrc.nist.gov/pubs/sp/800/171/r3/final>
- NARA CUI Registry: <https://www.archives.gov/cui/registry/category-list>
- GSA SAM Entity Management API: <https://open.gsa.gov/api/entity-api/>
- eSRS: <https://www.esrs.gov/>

## Study Path For New Team Members

1. Read the product promise and No-CUI production posture with synthetic CUI-ready demonstration workflows in `docs/product-strategy.md`.
2. Learn the workflow terms in this glossary: company profile, contract, clause, obligation, evidence, report, audit log.
3. Review `docs/compliance-content-governance.md` to understand why every customer-visible obligation needs a source URL, trigger condition, confidence label, and review state.
4. Review `docs/mvp-execution-plan.md` to understand launch gates, data policy, support escalation, and acceptance criteria.
5. Review `packages/compliance-content/README.md` and `packages/compliance-content/obligations/mvp.json` to see how source-backed obligations are represented.
