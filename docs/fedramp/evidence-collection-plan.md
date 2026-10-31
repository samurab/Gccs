# Reusable Operational Evidence Collection Plan

- Plan date: 2026-09-03
- Current scope: commercial No-CUI controls only
- Future scope: candidate regulated boundary after activation and approval

## Collection Principles

1. Evidence proves operation in a named environment at a named release and time. Configuration intent is not operating evidence.
2. Every record must include evidence ID, control/outcome, environment, commit SHA, release tag, collection timestamp, collector identity, method/tool version, result, reviewer, limitations, retention class, expiry or next collection date, artifact hash when practical, and superseded-evidence link.
3. Evidence from local development, synthetic staging, commercial production, or a superseded release cannot be relabeled as evidence for the future regulated offering.
4. Repository artifacts must be sanitized. Credentials, tokens, connection strings, customer content, raw uploads, CUI, unrestricted logs, exploitable vulnerability details, and sensitive incident data belong only in an approved restricted evidence system.
5. Corrections append a superseding record. They do not rewrite historical results.
6. Collection failure, stale evidence, missing evidence, and verification-pipeline failure are findings; they are not silently converted to a passing state.

## Evidence Classes

| Class | Examples | Collection | Minimum metadata | Storage/retention decision |
| --- | --- | --- | --- | --- |
| Source and build | Commit, dependency lockfiles, build/test results, migration validation, SBOM, signed artifact/provenance | Automatically in CI | Commit, runner, workflow, tools, result, artifact digest | Source history plus immutable CI artifact; define retention before readiness activation |
| Security test | Tenant-isolation, RBAC, No-CUI rejection, audit rollback, upload/storage authorization, report/export tests | CI and release gates | Test inventory, counts, environment, release, failures/skips | Immutable results with release association; no sensitive test payloads |
| Deployed configuration | Resource inventory, region, network, identity, keys, storage, database, backup, logging, alert, policy state | Read-only cloud APIs and policy evaluation | Resource IDs or sanitized aliases, subscription/environment, policy result, collection identity | Restricted evidence store; sanitized summary may be committed |
| Operating control | Access review, alert delivery, vulnerability response, backup restore, incident/tabletop exercise, key rotation, recovery | Scheduled automation plus accountable review | Period, population, exceptions, reviewer, remediation links | Restricted append-only evidence store with profile-approved retention |
| Supplier/inherited | Provider authorization package references, service scope, shared-responsibility material, subprocessor review | Provider portal/API and contract review | Provider, service, region, status date, scope, owner, expiry | Restricted supplier register; refresh on provider/status change |
| Certification package | Boundary, package overview, Security Decision Record, Key Security Indicators, secure configuration guide, assessment results | Generated from governed source records | Schema version, profile, boundary version, evidence freshness, approvals | Approved package repository with strict access and release history |

## Initial Collection Schedule

This schedule is a reusable internal baseline, not a claim that it meets a selected FedRAMP profile. Replace it with the applicable official cadence after `FR-5` selects the certification profile.

| Evidence | Current cadence | Triggered refresh | Owner | Pass condition |
| --- | --- | --- | --- | --- |
| Source build, unit/integration tests, migration validation | Every pull request and protected release | Toolchain or workflow change | Engineering | Required jobs pass; failures/skips recorded |
| Dependency, secret, and future SBOM evidence | Every pull request; scheduled weekly candidate scan | Dependency, base image, action, or tool change | Engineering/Security | Inventory generated; findings enter response workflow |
| Tenant/RBAC endpoint inventory and denial matrix | Every authorization-boundary change; at least each release | Role, permission, endpoint, middleware, report/export, or background-job change | Security/QA | Every in-scope route has expected allowed/denied and cross-tenant evidence |
| No-CUI upload and prohibited-content controls | Every upload/data-flow change; each release | Scanner, storage, classification, report/export, support, or AI change | Product/Security/QA | Allowed and prohibited cases pass; rejected cases leave no prohibited object or downstream job |
| Deployment provenance and configuration conformance | Every deployment | Infrastructure, secret, identity, policy, or network change | Engineering/Security | Artifact, release, configuration policy, and health evidence match approved boundary |
| Cloud resource inventory and drift | Monthly during foundation; profile-defined after activation | Resource or provider change | Engineering/Security | All discovered resources are classified; unauthorized drift is tracked |
| Privileged access review | Quarterly during foundation | Personnel, role, support model, or emergency-access change | Security | Population complete; excess access removed and evidenced |
| Alert routing and delivery | Monthly and after receiver/rule change | Incident, monitoring, or provider change | Security/Operations | Test reaches accountable receiver; failure opens finding |
| Backup configuration | Monthly | Database, region, network, retention, key, or provider change | Engineering | Configuration matches approved policy and recoverable window |
| Restore and recovery exercise | Quarterly during foundation; profile/RTO-defined after activation | Material backup, network, database, migration, or key change | Engineering/Security | Restored system passes scoped checks and teardown/retention is documented |
| Incident-response contacts and playbooks | Quarterly | Staff, supplier, reporting rule, or boundary change | Security/Support | Contacts, authority, communications, and escalation paths verified |
| Incident/tabletop exercise | At least annually during foundation; profile-defined after activation | Major incident or boundary change | Security | Objectives, timeline, decisions, findings, owners, and closure evidence recorded |
| Supplier/inherited-control review | Quarterly and before new dependency use | Provider status, contract, region, service, or subprocessor change | Security/Legal/Procurement | Scope and inheritance remain valid; gaps are tracked |
| Evidence freshness and package completeness | Monthly during foundation | Rule/profile/boundary change | Security/Compliance Engineering | No required record is silently stale, missing, or environment-mismatched |

## Evidence Record Template

```yaml
evidenceId: FE-YYYYMMDD-0001
securityOutcome: ""
environment: commercial-production | staging | future-regulated
boundaryVersion: ""
commitSha: ""
releaseTag: ""
collectedAtUtc: ""
collector:
  type: automation | named-reviewer
  identity: ""
method:
  commandOrWorkflow: ""
  toolVersions: []
result: passed | failed | partial | not-run
reviewer: ""
reviewedAtUtc: ""
limitations: []
artifactLocation: ""
artifactSha256: ""
retentionClass: ""
nextCollectionDue: ""
supersedes: ""
findingIds: []
containsCustomerData: false
containsCui: false
sanitizationReviewed: false
```

## Storage Architecture Decision Required

Do not use the application process memory or the Git repository as the authoritative evidence store. Before persistent collection begins, approve an evidence architecture with:

- Immutable or write-once retention appropriate to the evidence type.
- Separate access roles for collectors, reviewers, package publishers, and auditors.
- Encryption and key ownership, backup/recovery, legal retention, deletion, export, and incident procedures.
- Artifact hashing and provenance links.
- Tenant and environment separation.
- A sanitized index safe for source control and a restricted store for sensitive evidence.
- Automated freshness, expiry, collection-failure, and missing-evidence alerts.

## Immediate Evidence Gaps

| Gap | Owner | Target | Dependency | Risk |
| --- | --- | --- | --- | --- |
| No authoritative evidence store or normalized schema | Security/Engineering | Before relying on FedRAMP readiness records | Architecture approval and persistence design | High |
| September 2 launch candidate lacks executed production evidence | Engineering/Product | Before treating it as deployed | Protected production workflow approval | High for current-release claims |
| No real infrastructure inventory or drift baseline | Engineering/Security | `FR-6` design slice | Cloud discovery and real IaC decision | High for future boundary |
| No SBOM or complete software/service inventory | Engineering/Security | `FR-3` foundation | Tool selection and CI storage | Medium |
| No whole-boundary vulnerability response register and recurring report | Security | Before `FR-7` persistent measurement | Asset inventory, severity policy, owners | High for future certification |
| No approved target profile, security category, or federal use case | Product/Security | Activation gate | Customer validation and advisor/assessor input | Blocks certification planning |
| No current secure configuration guide | Security/Product/Engineering | After candidate boundary and configuration exist | `FR-5` and `FR-6` | High for future package |
