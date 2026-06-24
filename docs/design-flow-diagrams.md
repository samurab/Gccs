# Design Flow Diagrams

These diagrams translate the GCCS MVP product guidance into design flows for the authenticated SaaS workspace. The MVP is No-CUI / compliance management only with synthetic CUI-ready demonstration workflows: users can manage compliance work, metadata, obligations, tasks, and evidence, while real customer CUI is blocked unless the tenant is approved for future `CuiReady` operation.

## 1. User Operating Loop

```mermaid
flowchart TD
    entry["User opens GCCS workspace"]
    profile["Complete company compliance profile"]
    intake["Add contract, solicitation, subcontract, PO, SOW, wage determination, or flow-down"]
    guard{"Tenant mode allows this data?"}
    block["Block upload and show data handling guidance"]
    capture["Capture metadata and clauses"]
    obligations["Generate source-backed obligations"]
    dashboard["Review obligation dashboard"]
    plan["Assign owners, due dates, risk, and evidence needs"]
    execute["Complete tasks and collect evidence"]
    monitor["Monitor calendar and renewal deadlines"]
    report["Generate audit, bid, renewal, prime review, or certification package"]
    improve["Update profile, contract facts, controls, and evidence as work changes"]

    entry --> profile
    profile --> intake
    intake --> guard
    guard -->|"Blocked"| block
    guard -->|"Allowed"| capture
    capture --> obligations
    obligations --> dashboard
    dashboard --> plan
    plan --> execute
    execute --> monitor
    monitor --> report
    report --> improve
    improve --> dashboard
```

## 2. Workspace Navigation Flow

```mermaid
flowchart LR
    home["Workspace home"]
    company["Company profile"]
    contracts["Contracts"]
    obligations["Obligations"]
    calendar["Calendar"]
    evidence["Evidence vault"]
    cmmc["CMMC readiness"]
    subs["Subcontractors"]
    reports["Reports"]
    settings["Admin and settings"]

    home --> company
    home --> contracts
    home --> obligations
    home --> calendar
    home --> evidence
    home --> cmmc
    home --> subs
    home --> reports
    home --> settings

    company --> obligations
    contracts --> obligations
    obligations --> calendar
    obligations --> evidence
    obligations --> cmmc
    obligations --> subs
    calendar --> evidence
    evidence --> reports
    cmmc --> reports
    subs --> reports
```

## 3. Contract Intake Design Flow

```mermaid
flowchart TD
    start["Start contract intake"]
    choose["Choose intake type"]
    manual["Manual contract entry"]
    upload["Upload allowed document"]
    cuiCheck{"User flags CUI or classified content?"}
    rejected["Reject upload and record blocked attempt in audit log"]
    metadata["Capture contract metadata"]
    clauses["Add or tag clauses"]
    deliverables["Capture deliverables, reports, labor, data, and flow-down facts"]
    review["Review intake summary"]
    save["Save contract record"]
    map["Map facts to obligations"]

    start --> choose
    choose --> manual
    choose --> upload
    upload --> cuiCheck
    cuiCheck -->|"Yes"| rejected
    cuiCheck -->|"No"| metadata
    manual --> metadata
    metadata --> clauses
    clauses --> deliverables
    deliverables --> review
    review --> save
    save --> map
```

## 4. Clause To Obligation Flow

```mermaid
flowchart TD
    clause["Contract clause or profile trigger"]
    library["Curated obligation library"]
    applicability["Evaluate applicability dimensions"]
    dimensions["Entity, agency, NAICS, set-aside, contract type, role, data type, labor category, place of performance"]
    decision{"High-confidence match?"}
    expert["Route to expert review"]
    obligation["Create obligation instance"]
    task["Create task and calendar event"]
    evidenceNeed["Attach evidence requirements"]
    source["Show source, URL, confidence, and last reviewed date"]
    audit["Record mapping decision in audit log"]

    clause --> applicability
    library --> applicability
    applicability --> dimensions
    dimensions --> decision
    decision -->|"No"| expert
    expert --> obligation
    decision -->|"Yes"| obligation
    obligation --> task
    obligation --> evidenceNeed
    obligation --> source
    obligation --> audit
```

## 5. Evidence Vault Lifecycle

```mermaid
flowchart TD
    request["Evidence requested by obligation, control, contract, or subcontractor"]
    owner["Assign evidence owner"]
    submit["Submit file, link, note, or attestation"]
    scan["Run upload controls and malware scan"]
    classify{"Allowed by tenant data handling mode?"}
    reject["Reject or quarantine and notify owner"]
    tag["Tag by obligation, contract, control, vendor, employee, and report"]
    review["Review and approve evidence"]
    expiry{"Expiration or review date needed?"}
    schedule["Schedule renewal task"]
    reusable["Make evidence reusable across linked requirements"]
    package["Include in read-only evidence package"]

    request --> owner
    owner --> submit
    submit --> scan
    scan --> classify
    classify -->|"No"| reject
    classify -->|"Yes"| tag
    tag --> review
    review --> expiry
    expiry -->|"Yes"| schedule
    expiry -->|"No"| reusable
    schedule --> reusable
    reusable --> package
```

## 6. CMMC Readiness Flow

```mermaid
flowchart TD
    posture["Company FCI/CUI posture"]
    level{"Target CMMC level?"}
    level1["Level 1 self-assessment workspace"]
    level2["Level 2 readiness workspace"]
    controls["Control-by-control status"]
    evidence["Map evidence to controls"]
    assets["Define assets, system boundary, and data flows"]
    esp["Assign MSP or external service provider responsibilities"]
    gaps{"Control gaps remain?"}
    poam["Create POA&M items and remediation tasks"]
    sprs["Calculate or review SPRS score where applicable"]
    affirm["Track affirmation readiness"]
    report["Generate CMMC readiness report"]

    posture --> level
    level -->|"Level 1"| level1
    level -->|"Level 2"| level2
    level1 --> controls
    level2 --> controls
    controls --> evidence
    controls --> assets
    controls --> esp
    evidence --> gaps
    assets --> gaps
    esp --> gaps
    gaps -->|"Yes"| poam
    poam --> controls
    gaps -->|"No"| sprs
    sprs --> affirm
    affirm --> report
```

## 7. Subcontractor Flow-Down Flow

```mermaid
flowchart TD
    contract["Contract or prime flow-down attachment"]
    required["Identify required flow-down clauses"]
    sub["Create subcontractor profile"]
    role["Capture role, workshare, data access, export-control flag, and small business status"]
    requirements["Assign required clauses, insurance, NDAs, CMMC status, and evidence requests"]
    request["Send evidence request"]
    response{"Subcontractor complete?"}
    chase["Send reminder or escalate to contracts owner"]
    approve["Approve subcontractor compliance package"]
    monitor["Monitor expirations and status changes"]
    report["Include in subcontractor compliance report"]

    contract --> required
    required --> sub
    sub --> role
    role --> requirements
    requirements --> request
    request --> response
    response -->|"No"| chase
    chase --> request
    response -->|"Yes"| approve
    approve --> monitor
    monitor --> report
```

## 8. Compliance Content Governance Flow

```mermaid
stateDiagram-v2
    [*] --> Draft
    Draft --> SourceLinked: add source URL and trigger logic
    SourceLinked --> SMEReview: mark risk and confidence
    SMEReview --> Approved: expert approves
    SMEReview --> Draft: revision needed
    Approved --> Published: publish to obligation library
    Published --> Monitoring: monthly or quarterly review cadence
    Monitoring --> UpdatedDraft: source changes or product issue found
    UpdatedDraft --> SourceLinked
    Monitoring --> Published: no material change
    Published --> Deprecated: rule retired or replaced
    Deprecated --> [*]
```
