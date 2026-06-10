Below is a practical build guide for a **Government Contractor Compliance SaaS for small businesses** in the U.S. This is product and engineering guidance, not legal advice. For production compliance content, partner with a government contracts attorney, CMMC assessor/RP, labor compliance expert, or CPA depending on module scope.

**1. Product Definition**
Build the SaaS around one core promise:

> “Help small government contractors know what applies, prove what they did, and stay ready for audits, renewals, bids, and certifications.”

Small businesses usually struggle with scattered obligations: SAM registration, SBA size status, FAR/DFARS clauses, CMMC, CUI handling, labor standards, subcontractor flow-downs, reporting deadlines, and evidence collection.

Your app should not begin as a generic GRC platform. It should be a **govcon-specific compliance operating system**.

**Target Users**
- Small federal prime contractors
- Small subcontractors to large primes
- DoD suppliers preparing for CMMC
- 8(a), WOSB, HUBZone, SDVOSB, SDB firms
- Back-office staff: owners, proposal managers, contracts admins, IT/MSPs, HR/payroll, compliance consultants

**Core Jobs To Be Done**
- “Tell me what rules apply to this opportunity or contract.”
- “Show me what documents and evidence I need.”
- “Warn me before I miss a renewal, report, wage update, or certification requirement.”
- “Help me prepare for CMMC, SAM, SBA, eSRS, DOL, or prime contractor reviews.”
- “Keep my policies, evidence, training, vendors, and subcontractors organized.”

**2. Regulatory Research Foundation**
Start with a regulatory map. The SaaS should model compliance obligations by **entity**, **contract**, **clause**, **agency**, **NAICS**, **set-aside status**, **data type**, **place of performance**, **labor category**, and **subcontractor role**.

Important source domains:

- **SAM / UEI / entity registration**: SBA says small businesses need a UEI and SAM registration before bidding on federal work. Source: [SBA basic requirements](https://www.sba.gov/federal-contracting/contracting-guide/basic-requirements)
- **Small business size and affiliation**: SBA size standards determine whether a firm qualifies as small for a NAICS code. SBA warns of serious penalties for misrepresentation. Source: [SBA size standards](https://www.sba.gov/federal-contracting/contracting-guide/size-standards)
- **Small business contracting rules**: FAR, agency supplements, labor laws, integrity rules, limitations on subcontracting, and program-specific requirements. Source: [SBA governing rules](https://www.sba.gov/federal-contracting/contracting-guide/governing-rules-responsibilities)
- **Subcontracting plans and reporting**: Large primes and certain lower-tier subcontractors use eSRS for subcontracting reports. Source: [eSRS](https://www.esrs.gov/)
- **FCI safeguarding**: FAR 52.204-21 defines baseline safeguarding for systems that process, store, or transmit Federal Contract Information. Source: [FAR 52.204-21](https://www.acquisition.gov/far/52.204-21)
- **Telecom/video restrictions**: FAR 52.204-25 covers prohibited covered telecommunications and video surveillance services or equipment. Source: [FAR 52.204-25](https://www.acquisition.gov/far/52.204-25)
- **ByteDance/TikTok restriction**: FAR 52.204-27 prohibits covered applications on certain government or contractor IT. Source: [FAR 52.204-27](https://www.acquisition.gov/far/52.204-27)
- **Labor standards**: FAR Part 22 and clauses such as FAR 52.222-41 govern service contract labor standards. Sources: [FAR Part 22](https://www.acquisition.gov/far/part-22), [FAR 52.222-41](https://www.acquisition.gov/far/52.222-41)
- **CMMC / DoD cybersecurity**: 32 CFR Part 170 establishes CMMC for DoD contractors and subcontractors handling FCI/CUI. Current CMMC Phase 1 runs from November 10, 2025 to November 9, 2026 and focuses primarily on Level 1 and Level 2 self-assessments. Sources: [32 CFR Part 170](https://www.ecfr.gov/current/title-32/subtitle-A/chapter-I/subchapter-G/part-170), [DoD CMMC resources](https://dodcio.defense.gov/CMMC/Resources-Documentation/)
- **NIST 800-171**: NIST SP 800-171 Rev. 3 was published in May 2024 for protecting CUI in nonfederal systems, while CMMC currently references NIST SP 800-171 Rev. 2 in 32 CFR Part 170. Sources: [NIST SP 800-171 Rev. 3](https://csrc.nist.gov/pubs/sp/800/171/r3/final), [32 CFR Part 170](https://www.ecfr.gov/current/title-32/subtitle-A/chapter-I/subchapter-G/part-170)
- **CUI categories**: NARA maintains the government-wide CUI Registry. Source: [NARA CUI Registry](https://www.archives.gov/cui/registry/category-list)
- **SAM.gov APIs**: Use official APIs for entity data where appropriate. Source: [GSA SAM Entity Management API](https://open.gsa.gov/api/entity-api/)

**3. Research Phase**
Before writing code, run discovery in four tracks.

**Customer Discovery**
Interview at least:
- 10 small DoD subcontractors
- 5 civilian-agency contractors
- 5 proposal/contract managers
- 5 MSPs or CMMC consultants
- 3 govcon attorneys or compliance advisors
- 3 large-prime supplier compliance teams

Ask:
- What contract clauses cause confusion?
- What evidence do primes ask for?
- What renewals and reports get missed?
- What spreadsheets are they using today?
- Where does CUI/FCI live?
- How do they prepare for CMMC or NIST 800-171?
- What would they pay monthly to reduce audit panic?

**Competitor Research**
Map:
- GRC tools: Drata, Vanta, Hyperproof, Tugboat, Secureframe
- CMMC tools: CyberStrong, FutureFeed, Kieri, ComplyUp, PreVeil, Totem
- Govcon admin tools: GovWin, HigherGov, Unanet, Deltek, GovSpend
- Legal/compliance templates: PilieroMazza, Govology, APEX Accelerators, SBA resources

Look for gaps:
- Most GRC tools are not govcon-native.
- Most CMMC tools focus only on cybersecurity.
- Small businesses need **contract obligation + cybersecurity + reporting + evidence** in one workflow.

**Regulatory Ontology**
Create a structured obligation library:
```text
Regulation / Clause
→ Trigger condition
→ Applies to prime/sub
→ Applies by contract type
→ Data type: FCI, CUI, CDI, classified, export-controlled
→ Required actions
→ Evidence examples
→ Reporting deadline
→ Flow-down requirement
→ Penalty/risk
→ Source URL
→ Last reviewed date
```

**MVP Validation**
Do clickable prototypes before building:
- Contract intake wizard
- Clause-to-obligation dashboard
- CMMC evidence tracker
- SAM/SBA profile checker
- Compliance calendar
- Subcontractor flow-down tracker

**4. MVP Feature Set**
Build the first version around high-frequency pain, not every regulation.

**A. Company Profile**
Store:
- Legal entity name
- UEI
- CAGE code
- SAM expiration date
- NAICS codes
- SBA size status by NAICS
- Socioeconomic certifications: 8(a), WOSB, EDWOSB, HUBZone, SDVOSB, SDB
- Agency customers
- Prime/subcontractor role
- Products/services
- Employees and revenue ranges
- Locations
- IT environment summary
- Handles FCI/CUI? yes/no/unknown

**B. Contract Intake**
Allow users to upload or enter:
- Solicitation
- Contract
- Subcontract
- Purchase order
- Statement of work
- Flow-down attachment
- Wage determination
- DD Form 254, if classified work is involved
- CUI marking guide, if provided

The app should extract:
- Contract number
- Agency / prime
- Period of performance
- Contract type
- Place of performance
- Clauses
- Deliverables
- Reporting deadlines
- Data handling requirements
- Labor requirements
- Subcontractor flow-downs

**C. Clause Obligation Engine**
For each clause, show:
- Plain-English summary
- Who owns it: contracts, HR, IT, security, finance
- Required action
- Due date
- Evidence needed
- Flow-down requirement
- Risk level
- Source link
- “Ask expert” escalation option

Example:
```text
FAR 52.204-21
Trigger: Contract involves Federal Contract Information.
Action: Apply 15 basic safeguarding controls.
Evidence: access control policy, MFA configuration, media disposal record, antivirus logs, boundary protection records.
Owner: IT/security.
Flow-down: Include substance of clause in relevant subcontracts.
```

**D. Compliance Calendar**
Track:
- SAM renewal
- SBA certification renewal
- CMMC affirmation
- SPRS score review
- Insurance certificates
- Training
- Policy review
- Subcontractor certification expiration
- eSRS reporting dates
- Wage determination updates
- Contract deliverables
- Option period notices

**E. CMMC / NIST Workspace**
For DoD-focused users, this is a major value area.

Include:
- CMMC Level 1 self-assessment
- CMMC Level 2 readiness workspace
- Control-by-control evidence mapping
- SSP builder
- POA&M tracker
- SPRS score calculator
- Asset inventory
- External Service Provider / MSP responsibility matrix
- CUI data-flow diagrams
- System boundary definition
- Annual affirmation tracker

Be precise here: CMMC currently relies on FAR 52.204-21, NIST SP 800-171 Rev. 2, and selected NIST SP 800-172 requirements depending on level under 32 CFR Part 170. Also prepare for customers asking about NIST SP 800-171 Rev. 3 because NIST has finalized it.

**F. Evidence Vault**
This should be the heart of the platform.

Features:
- Folderless tagging by obligation, contract, control, vendor, employee
- Version history
- Expiration dates
- Evidence request workflows
- Approval status
- Read-only auditor view
- Encryption at rest and in transit
- No-CUI mode for MVP, or CUI-ready enclave if you choose a higher-security path

Evidence types:
- Policies
- Training records
- Screenshots
- System configs
- Vendor attestations
- Subcontractor certifications
- Signed flow-downs
- Payroll/wage records
- Incident records
- Access reviews
- Risk assessments
- Meeting notes
- Corrective action plans

**G. Subcontractor Management**
Track:
- Subcontractor profile
- Small business status
- Required flow-down clauses
- CMMC level or self-assessment status
- Insurance
- NDAs
- CUI access
- Export-control status
- Workshare percentage
- Limitations on subcontracting support
- Evidence requests

**H. Labor Compliance**
If serving service/construction contractors:
- Wage determination lookup and attachment
- Labor category mapping
- SCA/DBA applicability checklist
- Fringe benefit tracking
- Certified payroll support, if construction-focused
- Employee classification records
- Place-of-performance rules
- DOL audit evidence pack

**I. Reporting**
Generate:
- Compliance status report
- Contract obligation matrix
- CMMC readiness report
- Prime contractor evidence package
- SAM/SBA profile report
- Subcontractor compliance report
- Audit trail
- Executive risk dashboard

**5. Data Model**
Core entities:

```text
Tenant
User
Role
CompanyProfile
Certification
NAICSCode
Contract
Solicitation
Clause
Obligation
Task
Evidence
Control
Assessment
POAMItem
Asset
SystemBoundary
Vendor
Subcontractor
Employee
TrainingRecord
WageDetermination
Report
AuditLog
SourceReference
```

Key relationships:
```text
Contract has many Clauses
Clause maps to many Obligations
Obligation requires many EvidenceItems
EvidenceItem can satisfy many Obligations
Control maps to many EvidenceItems
Vendor/Subcontractor maps to FlowDownClauses
Task belongs to Obligation, Contract, or Control
```

Every compliance item should include:
```json
{
  "source": "FAR 52.204-21",
  "source_url": "https://www.acquisition.gov/far/52.204-21",
  "last_reviewed_at": "2026-06-03",
  "applicability_logic": "...",
  "confidence": "high",
  "requires_expert_review": false
}
```

**6. AI Features**
Use AI carefully. Do not let it “invent” compliance advice.

Good AI use cases:
- Clause extraction
- Contract summarization
- Evidence suggestion
- Policy draft assistance
- Gap analysis draft
- Plain-English explanations
- Search across obligations and evidence
- “What changed since last contract?” comparison

Guardrails:
- Every answer must cite source clauses or internal documents.
- Mark AI output as draft unless reviewed.
- Never provide final legal determinations automatically.
- Use retrieval-augmented generation from your curated obligation library.
- Log prompts, retrieved sources, generated output, and user approvals.
- Do not train on customer documents unless explicitly contracted and isolated.

**7. Architecture**
A sensible SaaS architecture:

```text
Frontend:
React + Vite for the authenticated SaaS app
Optional separate Next.js site for SEO-heavy public content

Backend:
ASP.NET Core on .NET 10 LTS
PostgreSQL primary database
Object storage for evidence files
Redis for jobs/cache
Queue for extraction and notifications

Search:
OpenSearch, Elasticsearch, or Postgres full-text initially

AI:
Document parser + OCR
RAG service over curated compliance library and tenant docs
Human-review workflow

Auth:
SSO/SAML/OIDC for higher tiers
MFA
RBAC
Tenant isolation

Hosting:
AWS GovCloud, Azure Government, or commercial cloud depending on data scope
```

If you will store **CUI**, design the platform around that from day one. If you are not ready, explicitly launch as **No-CUI / compliance management only**, and prevent users from uploading CUI.

**8. Security Requirements For The SaaS**
Minimum baseline:
- MFA
- RBAC
- Tenant isolation
- Encryption at rest
- TLS everywhere
- Immutable audit logs
- Secure file storage
- Malware scanning for uploads
- SSO on business tiers
- Least-privilege admin access
- Secrets manager
- Vulnerability scanning
- Backup and disaster recovery
- Incident response process
- Data retention controls
- Export and deletion workflows

For serious govcon customers:
- SOC 2 Type II
- ISO 27001 alignment
- NIST 800-171 control mapping
- CMMC-aware shared responsibility matrix
- FedRAMP-ready architecture if selling directly to federal agencies
- ITAR/export-control review if storing export-controlled technical data
- U.S.-person support option for sensitive tiers
- GovCloud deployment option

**Important product decision:**  
If the SaaS stores customer CUI, your service may become part of the customer’s CMMC assessment scope as an External Service Provider. Build a shared responsibility matrix and make this very explicit.

**9. Development Roadmap**
A realistic phased roadmap:

**Phase 0: Research and Validation, 4-6 weeks**
Deliverables:
- Persona map
- Regulatory obligation map
- Competitive matrix
- Clickable prototype
- 20-30 interviews
- Pricing hypothesis
- MVP requirements
- Legal/compliance advisor review

**Phase 1: MVP, 10-14 weeks**
Build:
- Tenant/user/RBAC
- Company profile
- Contract upload
- Manual clause tagging
- Obligation dashboard
- Task calendar
- Evidence vault
- Basic CMMC Level 1 checklist
- Reports
- Notifications
- Audit log

Avoid overbuilding AI here. Start with structured workflows.

**Phase 2: Govcon Intelligence, 8-12 weeks**
Add:
- Automated clause extraction
- Clause library
- Applicability engine
- SAM.gov entity lookup
- SBA size helper
- Subcontractor tracker
- Policy templates
- Evidence request workflows
- CMMC Level 2 readiness

**Phase 3: Advanced Compliance, 12-20 weeks**
Add:
- SSP builder
- POA&M manager
- SPRS score calculator
- CUI data-flow mapping
- Labor compliance module
- eSRS support
- Prime contractor portal
- Auditor read-only portal
- AI assistant with citations
- Integration APIs

**Phase 4: Enterprise / Regulated Deployment**
Add:
- SSO/SAML
- SCIM
- GovCloud deployment
- SOC 2 audit
- Data residency controls
- Advanced encryption/key management
- FedRAMP readiness package
- Marketplace/private cloud options

**10. Deployment Strategy**
Use three environments:
```text
dev → staging → production
```

Production requirements:
- Infrastructure as code: Terraform, Pulumi, or Bicep
- CI/CD with security checks
- Containerized services
- Separate tenant data boundaries
- Automated database migrations
- Blue/green or rolling deploys
- WAF
- DDoS protection
- Centralized logging
- Alerting and incident response
- Daily backups
- Point-in-time database recovery
- Regular restore tests

Recommended production stack:
- AWS: ECS/EKS, RDS Postgres, S3, KMS, CloudWatch, GuardDuty
- Azure: App Service/AKS, Azure SQL/Postgres, Blob Storage, Key Vault, Defender
- GovCloud/Government cloud tier for customers needing higher assurance

**11. Compliance Content Governance**
This is critical. Your app is only as good as its regulatory content.

Create a compliance content process:
- Maintain source-controlled obligation library
- Track source URL and effective date
- Review high-risk content quarterly
- Review CMMC/FAR/DFARS changes monthly
- Add “last reviewed” labels in the UI
- Separate legal interpretation from product workflow
- Have an expert review workflow before publishing new rules
- Notify customers when rules materially change

**12. Pricing Model**
Start simple:

```text
Starter: $99-$199/month
For very small subcontractors. Company profile, calendar, basic evidence vault.

Growth: $399-$799/month
Contract intake, clause obligations, subcontractor tracking, CMMC Level 1/2 readiness.

Pro: $1,000-$2,500/month
Advanced CMMC, SSP/POA&M, auditor portal, AI assistant, integrations.

Advisor/Consultant Plan:
Multi-client dashboard for MSPs, CMMC consultants, attorneys, CPAs.
```

Add implementation packages:
- CMMC readiness setup
- Contract obligation import
- Policy template package
- Subcontractor onboarding
- GovCloud/CUI-ready deployment

**13. Biggest Risks**
- Giving legal advice without proper controls
- Storing CUI before your platform is ready
- Poor tenant isolation
- AI hallucinating clause requirements
- Not tracking source/effective dates
- Treating all contractors alike
- Ignoring subcontractor flow-downs
- Underestimating labor compliance complexity
- Building a generic checklist instead of contract-specific obligations

**14. Project Governance and Risk Addendum**
Treat this product as a regulated-market SaaS from the start. Governance should help the team make disciplined decisions about scope, compliance claims, data handling, expert review, release readiness, and customer promises before those decisions become expensive to unwind.

**Governance Principles**
- Product claims must be source-backed, reviewed, and phrased as workflow guidance unless an approved expert has validated a stronger interpretation.
- No feature should silently expand the product's regulatory, security, or customer data obligations.
- CUI, export-controlled data, classified data, payroll data, and sensitive employee data require explicit intake controls, storage rules, retention rules, and customer-facing warnings.
- Every roadmap item should identify its compliance owner, technical owner, support owner, and release approver.
- Customer trust artifacts should be versioned: security overview, shared responsibility matrix, data handling policy, AI usage policy, incident response summary, and compliance content review process.

**Decision Rights**
- Product owner: prioritizes roadmap, validates customer value, and owns scope tradeoffs.
- Engineering lead: owns architecture, delivery estimates, technical risk, and release quality.
- Security lead: owns tenant isolation, evidence storage controls, audit logging, access control, vulnerability management, and incident response readiness.
- Compliance content owner: owns obligation library structure, source traceability, review cadence, and expert review workflow.
- Legal/compliance advisors: review high-risk regulatory interpretations, disclaimers, customer-facing claims, and content that could be treated as legal advice.
- Customer success lead: owns onboarding risks, support playbooks, implementation packages, and feedback loops from users and advisors.

**Governance Forums**
- Weekly delivery review: track roadmap progress, blocked work, quality signals, migration risks, and release readiness.
- Biweekly risk review: update the risk register, decide mitigations, assign owners, and escalate unresolved high-risk items.
- Monthly compliance content review: review source changes, CMMC/FAR/DFARS updates, customer-reported ambiguities, and expert review status.
- Quarterly security and privacy review: review access logs, vulnerability trends, backup restore evidence, incident drills, data retention, vendor risk, and roadmap changes that affect security posture.
- Pre-release go/no-go review: confirm tests, migrations, rollback plan, security checks, content approvals, customer communications, and support readiness.

**Risk Register**
Maintain a living risk register with at least these fields:
```text
Risk ID
Risk statement
Category: product, legal, security, privacy, compliance content, AI, delivery, financial, customer success
Affected module
Likelihood
Impact
Risk rating
Owner
Mitigation plan
Contingency plan
Target date
Current status
Decision log link
Last reviewed date
```

Example high-priority risks:
- Customers upload CUI into a No-CUI environment.
- Obligation library content becomes stale after FAR, DFARS, SBA, CMMC, or labor rule changes.
- AI-generated explanations are mistaken for final legal or compliance determinations.
- Contract clause extraction misses a flow-down, reporting deadline, or data handling requirement.
- Tenant isolation defect exposes one customer's evidence or contract data to another tenant.
- Product marketing implies certification, legal approval, CMMC readiness, or government endorsement beyond what the product can substantiate.
- Evidence retention and deletion behavior conflicts with customer contract, audit, litigation hold, or privacy needs.
- Consultant/advisor multi-client access creates cross-client data exposure risk.

**Stage Gates**
Before Phase 1 MVP release:
- Confirm No-CUI or CUI-ready positioning in product copy, onboarding, upload warnings, terms, and support scripts.
- Complete threat model for tenant isolation, file upload, evidence vault, audit logs, and admin access.
- Approve the initial obligation library through the content review workflow.
- Define support escalation paths for legal, CMMC, labor, security, and technical questions.
- Ship backup, restore, audit log, and access review procedures even if they are lightweight.

Before Phase 2 Govcon Intelligence:
- Validate clause extraction accuracy against representative solicitations, contracts, subcontracts, and flow-down attachments.
- Add human review states for extracted obligations before customers rely on them.
- Add source citations and confidence labels to extracted clauses and generated summaries.
- Document SAM.gov, SBA, and third-party data source limitations.
- Establish customer-facing correction workflow for disputed or ambiguous obligations.

Before Phase 3 Advanced Compliance:
- Reassess whether the platform remains No-CUI or moves to a CUI-ready architecture.
- Review External Service Provider implications for CMMC customers.
- Add formal model, prompt, retrieval, and output evaluation for AI assistant features.
- Validate SSP, POA&M, SPRS, and CUI data-flow outputs with qualified advisors.
- Complete vendor risk review for OCR, AI, storage, email, logging, analytics, and support tooling.

Before Phase 4 Enterprise / Regulated Deployment:
- Confirm SSO/SAML, SCIM, key management, audit export, data residency, and incident response requirements with target enterprise customers.
- Decide whether FedRAMP readiness, GovCloud, U.S.-person support, ITAR controls, or private deployment are product commitments or paid implementation paths.
- Run tabletop exercises for data breach, mis-scoped CUI upload, AI error, failed backup restore, and regulatory content correction.
- Prepare procurement artifacts: security overview, architecture diagram, shared responsibility matrix, subprocessors list, data retention policy, and support SLAs.

**Escalation Rules**
Escalate before release when:
- A feature changes what customer data the platform can store, process, or transmit.
- A workflow could be interpreted as legal, accounting, labor, certification, or CMMC assessment advice.
- A customer-facing report includes readiness scores, certification language, pass/fail labels, or official-sounding determinations.
- AI output will be used in reports, policies, SSPs, POA&Ms, or customer deliverables.
- A change affects tenant isolation, evidence access, audit logs, authentication, authorization, encryption, or retention.
- A source regulation, clause, or agency page materially changes or becomes unavailable.

**Decision Log**
Keep a source-controlled decision log for major product and architecture decisions. Each entry should include:
```text
Decision
Date
Context
Options considered
Chosen approach
Risks accepted
Mitigations
Approver
Review date
```

Suggested early decisions:
- No-CUI MVP vs CUI-ready MVP.
- Commercial cloud vs GovCloud starting point.
- Manual clause tagging first vs automated extraction first.
- Internal obligation library format and review workflow.
- Whether AI features are disabled, draft-only, or available behind expert-reviewed workflows.
- Evidence retention defaults and customer-controlled deletion/export behavior.
- Advisor/consultant multi-tenant access model.

**Project Health Metrics**
Track governance health alongside delivery velocity:
- Percentage of obligations with source URL, last reviewed date, owner, and confidence label.
- Number of high-risk obligations awaiting expert review.
- Clause extraction precision and recall on test contracts.
- Evidence vault access review completion rate.
- Open critical and high vulnerabilities by age.
- Backup restore test recency.
- AI outputs rejected or edited by reviewers.
- Customer-reported content corrections by module.
- Open risks by severity and days since last review.
- Release rollbacks, incidents, and support escalations by cause.

**Recommended MVP Scope**
For the first release, build:

1. Company compliance profile  
2. Contract and clause intake  
3. Obligation dashboard  
4. Compliance calendar  
5. Evidence vault  
6. CMMC Level 1 and Level 2 readiness tracker  
7. Subcontractor flow-down tracker  
8. Basic reports  
9. Source-backed obligation library  

That gives small businesses something immediately useful while leaving room to grow into labor compliance, eSRS, advanced CMMC, AI, and GovCloud/CUI support.

**15. MVP Operating Plan**
Use this section as the working execution brief for the first product release. It turns the product strategy into ownership, acceptance criteria, launch controls, and measurable operating expectations.

**Executive Summary**

**Product vision**
Build a govcon-specific compliance operating system that helps small contractors understand what applies to each company, opportunity, contract, clause, data type, certification, vendor, and subcontractor relationship, then prove action through tasks, evidence, reports, and audit trails.

**Target customer**
- Primary: small U.S. federal contractors and subcontractors preparing for or maintaining SAM, SBA, FAR/DFARS, CMMC, subcontractor flow-down, and evidence-management obligations.
- Early adopter: small DoD subcontractors, MSPs/CMMC consultants serving multiple small contractors, and back-office teams that need a practical obligation matrix without buying a broad enterprise GRC platform.
- Exclusions for MVP: direct federal agency buyers, classified programs, and customers requiring production CUI storage unless a separate CUI-ready deployment has been approved.

**MVP scope**
- Company compliance profile.
- Contract and clause intake with manual clause tagging.
- Source-backed obligation dashboard.
- Compliance calendar and reminders.
- Evidence vault with tagging, status, expiration dates, and audit history.
- CMMC Level 1 and Level 2 readiness tracker with draft-only guidance.
- Subcontractor flow-down tracker.
- Basic reports: obligation matrix, compliance status, CMMC readiness, evidence package, subcontractor status, and audit log.
- Source-backed obligation library with owner, confidence, source URL, last-reviewed date, and SME approval status.

**Success definition**
- Pilot users can onboard a company, enter at least one contract, tag clauses, generate an obligation matrix, assign tasks, upload evidence, and produce a report without engineering assistance.
- Every MVP obligation shown to customers has a source URL, trigger condition, confidence label, last-reviewed date, and review owner.
- No-CUI positioning is visible in onboarding, upload flows, support scripts, and customer-facing documentation.
- AI, if enabled, is clearly marked draft-only and cites retrieved sources.
- Pilot customers report that the product reduces spreadsheet tracking and improves readiness for prime, CMMC, SAM/SBA, or internal reviews.

**Launch timeline**
- Alpha: internal and advisor testing after core profile, intake, obligations, calendar, evidence, and reporting flows are usable end to end.
- Beta: 3-5 design partners after security review, SME-reviewed content baseline, support playbook, and pilot onboarding materials are ready.
- MVP launch: 8-12 pilot customers after beta feedback, migration readiness, backup restore verification, release signoff, and customer acceptance criteria are complete.

**Stakeholders and RACI**
Use RACI definitions consistently: Responsible performs the work, Accountable owns the outcome, Consulted gives required input, and Informed receives status.

| Area | Product Owner | Engineering Lead | Compliance SME | Security Owner | QA Owner | Customer Success | Legal/Contracting Advisor |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Product requirements | A/R | C | C | C | C | C | C |
| Technical architecture | C | A/R | C | C | C | I | I |
| Compliance content | C | I | A/R | C | C | I | C |
| Security controls | C | R | C | A/R | C | I | C |
| Test strategy | C | C | C | C | A/R | C | I |
| Pilot onboarding | C | C | I | I | C | A/R | I |
| Customer-facing legal/compliance claims | C | I | C | C | I | C | A/R |
| Release readiness | A | R | C | C | C | C | C |
| Support escalation process | C | C | C | C | C | A/R | C |

Approval rules:
- Requirements approval: Product Owner is accountable, with Engineering Lead, Security Owner, QA Owner, Compliance SME, and Customer Success consulted.
- Content approval: Compliance SME is accountable for obligation content; Legal/Contracting Advisor approves high-risk interpretations, disclaimers, and claims.
- Release approval: Product Owner is accountable; Engineering Lead, Security Owner, QA Owner, Compliance SME, and Customer Success must sign off for MVP.
- Compliance assumptions approval: Compliance SME and Legal/Contracting Advisor approve; Product Owner owns how assumptions are represented in product copy and UX.

**Assumptions and Constraints**
- MVP is No-CUI unless explicitly deployed as CUI-ready with approved architecture, customer terms, upload controls, support process, and shared responsibility matrix.
- CMMC guidance requires SME review before being published as customer-facing product content.
- AI output is draft-only, source-cited, logged, and subject to human review before being used in reports, policies, SSPs, POA&Ms, or customer deliverables.
- FedRAMP is future-state unless the business chooses to sell directly to federal agencies or makes FedRAMP readiness a paid enterprise commitment.
- Labor compliance may be deferred unless target pilot customers need service-contract or construction labor workflows early.
- Classified data is prohibited unless a separate cleared environment, cleared staff model, and legal/security operating plan exist.
- The MVP should optimize for high-confidence workflows and traceability instead of broad but shallow regulatory coverage.

**Risk Register**
Maintain these risks as initial entries in the living risk register:

| Risk | Category | Mitigation |
| --- | --- | --- |
| Regulatory interpretation risk | Legal/compliance content | Use source-backed workflow guidance, confidence labels, SME review, legal review for high-risk claims, and visible disclaimers. |
| CUI storage risk | Security/privacy/product | Default to No-CUI, add upload warnings and data classification controls, and require formal approval before any CUI-ready deployment. |
| AI hallucination risk | AI/compliance content | Require retrieval citations, draft labels, prompt/output logging, human review states, and reviewer rejection metrics. |
| Tenant isolation risk | Security | Threat-model tenant boundaries, enforce tenant-scoped authorization checks, add audit logs, and test cross-tenant access paths. |
| Slow content governance risk | Delivery/compliance content | Source-control obligation content, assign owners, set monthly review cadence, and track approval aging. |
| CMMC rule-change risk | Compliance content | Monitor DoD, CFR, NIST, and CMMC source updates monthly; version content and notify affected customers. |
| File upload/security risk | Security | Enforce file type and size limits, malware scanning, storage isolation, encryption, access review, and retention controls. |
| Customer adoption risk | Customer success/product | Validate with pilots, provide onboarding packages, reduce setup friction, and measure activation, evidence completion, and report generation. |

**Non-Functional Requirements**
- Performance: core dashboard pages should load in under 2 seconds at p95 for typical pilot tenants; report generation should complete in under 60 seconds for MVP-size datasets.
- Uptime: target 99.5% for MVP pilots, moving toward 99.9% for paid production tiers after operational maturity improves.
- Accessibility: meet WCAG 2.2 AA for key authenticated workflows, including keyboard navigation, form labels, color contrast, focus states, and error messaging.
- Security: require MFA-capable auth, RBAC, tenant isolation, TLS, encryption at rest, immutable audit logging for critical actions, secure secrets handling, malware scanning for uploads, and least-privilege admin access.
- Data retention: define default retention by data class and allow customer-controlled export and deletion where contractually permitted.
- Backup and recovery: perform daily backups, support point-in-time recovery for production data stores where available, and test restore procedures before MVP launch.
- Audit log retention: retain critical audit logs for at least 1 year for MVP unless customer contracts require longer retention.
- File size limits: set conservative MVP limits, such as 50 MB per file and 5 GB per tenant, with configurable higher limits for paid tiers after storage, scanning, and support costs are validated.
- Browser/device support: support current Chrome, Edge, Safari, and Firefox on desktop; support responsive tablet workflows; mobile should support review and status workflows, not heavy contract intake.

**Compliance Traceability Matrix**
Maintain a matrix for every obligation, feature, report, and source-backed compliance workflow.

| Field | Purpose |
| --- | --- |
| FAR/DFARS/SBA/DOL/CMMC/NIST source | Identifies the authoritative source, source URL, effective date, and last-reviewed date. |
| Trigger condition | Explains when the obligation applies by clause, contract type, data type, agency, NAICS, labor category, role, or certification. |
| Related feature | Links the source obligation to the product surface where the customer sees or acts on it. |
| Related entity model | Maps the obligation to entities such as CompanyProfile, Contract, Clause, Obligation, Task, Evidence, Control, Vendor, Subcontractor, or Report. |
| Related API | Identifies internal or external APIs used for intake, lookup, reporting, evidence, tasking, reminders, or integrations. |
| Related report | Identifies generated reports that include the obligation or its status. |
| SME approval status | Tracks draft, in review, approved, rejected, expired, or requires legal review. |

**Data Classification and Privacy Plan**
- Public: marketing content, public help docs, public source links, and non-sensitive educational material.
- Internal: product roadmaps, support procedures, internal notes, implementation plans, operational metrics, and non-customer system metadata.
- Contractor confidential: contracts, subcontracts, proposals, pricing, evidence, vendor records, policies, employee records, reports, and customer configuration data.
- FCI: allowed only when the environment, terms, onboarding, and access controls explicitly permit it.
- CUI: prohibited in the default No-CUI MVP; allowed only in a separately approved CUI-ready deployment with appropriate controls and customer commitments.
- Export-controlled: prohibited by default unless an approved export-control handling plan, storage environment, staffing model, and contractual terms exist.
- Classified: prohibited unless a separate cleared environment exists; do not design the MVP to store, process, or transmit classified information.
- Data deletion/export policy: customers must be able to export core records and evidence metadata; deletion must respect contractual retention, legal hold, audit, security, and backup constraints; document timelines clearly.

**Metrics and KPIs**
- Time to onboard a company.
- Time to generate an obligation matrix.
- Evidence completion rate.
- Overdue task rate.
- CMMC readiness score trend.
- Report generation success rate.
- User activation and retention.
- Pilot customer satisfaction.
- Percentage of obligations with source URL, last-reviewed date, owner, and SME approval status.
- Number of content corrections, support escalations, and high-risk assumptions awaiting review.

**Go-To-Market and Pricing**
- Pilot customer profile: small DoD subcontractors, federal services firms, MSPs/CMMC consultants, and small primes with repeatable compliance pain and a willingness to provide structured feedback.
- Pricing tiers: Starter for company profile, calendar, and basic evidence vault; Growth for contract intake, obligations, subcontractors, and CMMC readiness; Pro for advanced reporting, advisor workflows, integrations, and AI assistant features after review controls mature.
- Consultant/MSP plan: multi-client dashboard, role separation, evidence request workflows, client status reporting, and strict cross-client isolation controls.
- Onboarding package: company profile setup, SAM/SBA baseline review workflow, first contract intake, initial obligation matrix, evidence taxonomy, and first report.
- Support package: office hours, guided setup, content escalation, technical support, and release-readiness check-ins for pilot customers.
- Sales demo workflow: show company setup, contract intake, clause tagging, obligation dashboard, evidence request, CMMC tracker, subcontractor flow-down, and report export using sanitized demo data.

**Training and Documentation**
- Admin guide: tenant setup, users, roles, MFA/SSO where available, data classification, retention, exports, and audit logs.
- User guide: company profile, contract intake, obligations, tasks, calendar, evidence vault, subcontractors, reports, and notifications.
- Compliance content disclaimer: explain that the product provides workflow guidance and source-backed tracking, not legal, accounting, labor, certification, or CMMC assessment advice.
- CMMC readiness guide: explain Level 1 and Level 2 readiness workflows, evidence expectations, SME-review status, draft labels, and External Service Provider implications.
- Subcontractor portal guide: explain flow-down requests, evidence uploads, status responses, access limits, and expiration tracking.
- API developer guide: document authentication, tenant scoping, rate limits, entities, errors, audit behavior, and integration boundaries.
- Release notes process: publish customer-facing changes, new obligations, source updates, known limitations, security-relevant changes, and required customer actions.

**Support and Operations**
- Support channels: in-app support request, email support, pilot office hours, and designated escalation channel for urgent security or data-handling issues.
- SLA targets: acknowledge standard pilot support within 1 business day; acknowledge urgent security or availability issues within 4 business hours during pilot operating hours.
- Incident response process: triage severity, preserve evidence, contain impact, notify accountable owners, communicate to affected customers as required, and complete post-incident review.
- Customer escalation path: Customer Success owns first response, Engineering Lead owns technical defects, Security Owner owns security/data issues, Compliance SME owns content questions, and Legal/Contracting Advisor reviews high-risk claims or disputes.
- Bug triage process: classify severity, affected tenants, workaround, regression risk, test coverage, release target, and customer communication needs.
- Maintenance windows: publish scheduled maintenance at least 3 business days in advance for pilots unless urgent security work requires faster action.

**Release Governance**
- Alpha gate: core workflows are usable with seeded data; no known critical security issues; initial content library exists; internal users can complete onboarding, obligations, evidence, and reports.
- Beta gate: pilot onboarding guide, No-CUI controls, SME-reviewed content baseline, backup restore evidence, audit logging, support playbook, and beta feedback process are ready.
- MVP launch gate: launch scope is stable, high-priority pilot feedback is addressed or accepted, known limitations are documented, support coverage is staffed, and customer-facing materials are approved.
- Security signoff: Security Owner confirms threat model coverage, tenant isolation review, file upload controls, audit logging, vulnerability status, backup/restore evidence, and incident response readiness.
- Compliance SME signoff: Compliance SME confirms source-backed obligations, trigger logic, confidence labels, content review status, disclaimers, and escalation rules.
- Product Owner signoff: Product Owner confirms MVP scope, success criteria, pricing/package assumptions, release notes, pilot communications, and launch readiness.
- Pilot customer acceptance: design partners confirm that the product can support company onboarding, obligation tracking, evidence organization, reporting, and readiness review with representative data.
