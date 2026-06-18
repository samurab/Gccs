# Sprint Story Explanations

This document explains what each MVP sprint story does, how it fits into GCCS, and the value it adds to the project. It is written for product, engineering, QA, advisor, and stakeholder conversations.

GCCS is a CUI-ready govcon compliance management SaaS for small U.S. federal contractors and subcontractors. Its core promise is to help customers know what applies, prove what they did, and stay ready for audits, renewals, bids, certifications, and prime contractor reviews.

## Index

1. [Delivery Foundation](#1-delivery-foundation)
   - [Story 1.1: Repository And Project Structure](#story-11-repository-and-project-structure)
   - [Story 1.2: Local Development Services](#story-12-local-development-services)
   - [Story 1.3: Continuous Integration Baseline](#story-13-continuous-integration-baseline)
2. [Tenant, Identity, And RBAC](#2-tenant-identity-and-rbac)
   - [Story 2.1: Tenant Creation](#story-21-tenant-creation)
   - [Story 2.2: User Memberships](#story-22-user-memberships)
   - [Story 2.3: User Invitations](#story-23-user-invitations)
   - [Story 2.4: Role-Based Permissions](#story-24-role-based-permissions)
3. [Authenticated Application Shell](#3-authenticated-application-shell)
   - [Story 3.1: Protected API Access](#story-31-protected-api-access)
   - [Story 3.2: SaaS Navigation Shell](#story-32-saas-navigation-shell)
4. [CUI-Ready Gated Controls](#4-no-cui-controls)
   - [Story 4.1: Data Handling Acknowledgement](#story-41-no-cui-acknowledgement)
   - [Story 4.2: Upload Guardrails](#story-42-upload-guardrails)
5. [Audit Logging](#5-audit-logging)
   - [Story 5.1: Append-Only Audit Events](#story-51-append-only-audit-events)
   - [Story 5.2: Audit Log Viewer](#story-52-audit-log-viewer)
6. [Compliance Content Foundation](#6-compliance-content-foundation)
   - [Story 6.1: Obligation Schema](#story-61-obligation-schema)
   - [Story 6.2: Content Import](#story-62-content-import)
   - [Story 6.3: Content Review State](#story-63-content-review-state)
7. [Company Compliance Profile](#7-company-compliance-profile)
   - [Story 7.1: Create Company Profile](#story-71-create-company-profile)
   - [Story 7.2: NAICS And Size Status](#story-72-naics-and-size-status)
   - [Story 7.3: Certification Tracking](#story-73-certification-tracking)
8. [Contract Intake](#8-contract-intake)
   - [Story 8.1: Create Contract Record](#story-81-create-contract-record)
   - [Story 8.2: Contract Document Metadata And Upload](#story-82-contract-document-metadata-and-upload)
   - [Story 8.3: Contract Dates And Deliverables](#story-83-contract-dates-and-deliverables)
9. [Manual Clause Tagging](#9-manual-clause-tagging)
   - [Story 9.1: Clause Library Search](#story-91-clause-library-search)
   - [Story 9.2: Attach Clause To Contract](#story-92-attach-clause-to-contract)
   - [Story 9.3: Generate Obligations From Clause](#story-93-generate-obligations-from-clause)
10. [Obligation Dashboard](#10-obligation-dashboard)
    - [Story 10.1: Obligation List And Filters](#story-101-obligation-list-and-filters)
    - [Story 10.2: Obligation Detail](#story-102-obligation-detail)
    - [Story 10.3: Ownership Assignment](#story-103-ownership-assignment)
11. [Task And Compliance Calendar](#11-task-and-compliance-calendar)
    - [Story 11.1: Task Management](#story-111-task-management)
    - [Story 11.2: Calendar View](#story-112-calendar-view)
    - [Story 11.3: Renewal Generation](#story-113-renewal-generation)
12. [Evidence Vault](#12-evidence-vault)
    - [Story 12.1: Evidence Metadata](#story-121-evidence-metadata)
    - [Story 12.2: Evidence File Upload](#story-122-evidence-file-upload)
    - [Story 12.3: Evidence Approval](#story-123-evidence-approval)
13. [CMMC Readiness Tracker](#13-cmmc-readiness-tracker)
    - [Story 13.1: CMMC Level Selection](#story-131-cmmc-level-selection)
    - [Story 13.2: Control Readiness](#story-132-control-readiness)
    - [Story 13.3: POA&M Items](#story-133-poam-items)
    - [Story 13.4: Annual Affirmation Tracker](#story-134-annual-affirmation-tracker)
14. [Subcontractor Flow-Down Tracker](#14-subcontractor-flow-down-tracker)
    - [Story 14.1: Subcontractor Profile](#story-141-subcontractor-profile)
    - [Story 14.2: Flow-Down Clause Tracking](#story-142-flow-down-clause-tracking)
    - [Story 14.3: Subcontractor Evidence Requests](#story-143-subcontractor-evidence-requests)
15. [Reports](#15-reports)
    - [Story 15.1: Compliance Status Report](#story-151-compliance-status-report)
    - [Story 15.2: Contract Obligation Matrix](#story-152-contract-obligation-matrix)
    - [Story 15.3: CMMC Readiness Report](#story-153-cmmc-readiness-report)
    - [Story 15.4: Evidence Package](#story-154-evidence-package)
    - [Story 15.5: Subcontractor Compliance Report](#story-155-subcontractor-compliance-report)
16. [Notifications](#16-notifications)
    - [Story 16.1: Notification Preferences](#story-161-notification-preferences)
    - [Story 16.2: Due-Date Reminders](#story-162-due-date-reminders)
    - [Story 16.3: Assignment Notifications](#story-163-assignment-notifications)
17. [MVP Hardening And Release Readiness](#17-mvp-hardening-and-release-readiness)
    - [Story 17.1: End-To-End Pilot Workflow](#story-171-end-to-end-pilot-workflow)
    - [Story 17.2: Security And Tenant Isolation Verification](#story-172-security-and-tenant-isolation-verification)
    - [Story 17.3: Staging Environment](#story-173-staging-environment)
    - [Story 17.4: Production Readiness Checklist](#story-174-production-readiness-checklist)
17A. [Phase 1A: CUI Readiness Gate](#17a-phase-1a-cui-readiness-gate)
    - [Story 1A.1.1: Tenant Data Handling Mode Model](#story-1a11-tenant-data-handling-mode-model)
    - [Story 1A.1.2: Mode-Based Workflow Enforcement](#story-1a12-mode-based-workflow-enforcement)
    - [Story 1A.2.1: Classification Metadata Schema](#story-1a21-classification-metadata-schema)
    - [Story 1A.2.2: Classification UX And Review](#story-1a22-classification-ux-and-review)
    - [Story 1A.3.1: Synthetic Dataset Definition](#story-1a31-synthetic-dataset-definition)
    - [Story 1A.3.2: Demo Tenant Seeding](#story-1a32-demo-tenant-seeding)
    - [Story 1A.4.1: Approval Checklist Model](#story-1a41-approval-checklist-model)
    - [Story 1A.4.2: Approval Gate Enforcement](#story-1a42-approval-gate-enforcement)
    - [Story 1A.5.1: Baseline Responsibility Matrix](#story-1a51-baseline-responsibility-matrix)
    - [Story 1A.5.2: Tenant Matrix Acknowledgement](#story-1a52-tenant-matrix-acknowledgement)
    - [Story 1A.6.1: Versioned Notice Content](#story-1a61-versioned-notice-content)
    - [Story 1A.6.2: Notice Placement And Acknowledgement](#story-1a62-notice-placement-and-acknowledgement)
    - [Story 1A.7.1: Escalation Intake And Classification](#story-1a71-escalation-intake-and-classification)
    - [Story 1A.7.2: Escalation Workflow And Resolution](#story-1a72-escalation-workflow-and-resolution)
    - [Story 1A.8.1: Required CUI Audit Events](#story-1a81-required-cui-audit-events)
    - [Story 1A.8.2: CUI Audit Filters And Export](#story-1a82-cui-audit-filters-and-export)
    - [Story 1A.9.1: Security Review Checklist](#story-1a91-security-review-checklist)
    - [Story 1A.9.2: Technical Control Verification](#story-1a92-technical-control-verification)
    - [Story 1A.9.3: Incident Response Readiness](#story-1a93-incident-response-readiness)
18. [Automated Clause Extraction](#18-automated-clause-extraction)
    - [Story 18.1: Extraction Job Intake](#story-181-extraction-job-intake)
    - [Story 18.2: Text Extraction And Clause Candidate Detection](#story-182-text-extraction-and-clause-candidate-detection)
    - [Story 18.3: Extraction Results Review Screen](#story-183-extraction-results-review-screen)
19. [Human Review Workflow](#19-human-review-workflow)
    - [Story 19.1: Review States For Extracted Clauses](#story-191-review-states-for-extracted-clauses)
    - [Story 19.2: AI-Suggested Obligation Review](#story-192-ai-suggested-obligation-review)
    - [Story 19.3: Expert Escalation Queue](#story-193-expert-escalation-queue)
20. [Clause Library Expansion](#20-clause-library-expansion)
    - [Story 20.1: Versioned Clause Records](#story-201-versioned-clause-records)
    - [Story 20.2: Clause Search And Discovery](#story-202-clause-search-and-discovery)
    - [Story 20.3: Clause-To-Obligation Mapping](#story-203-clause-to-obligation-mapping)
21. [Applicability Engine](#21-applicability-engine)
    - [Story 21.1: Applicability Facts Model](#story-211-applicability-facts-model)
    - [Story 21.2: Rule Evaluation](#story-212-rule-evaluation)
    - [Story 21.3: Obligation Applicability Updates](#story-213-obligation-applicability-updates)
22. [SAM.gov Entity Lookup](#22-samgov-entity-lookup)
    - [Story 22.1: SAM.gov API Configuration](#story-221-samgov-api-configuration)
    - [Story 22.2: Company Entity Lookup](#story-222-company-entity-lookup)
    - [Story 22.3: Subcontractor Entity Lookup](#story-223-subcontractor-entity-lookup)
23. [SBA Size Helper](#23-sba-size-helper)
    - [Story 23.1: Size Standard Reference Data](#story-231-size-standard-reference-data)
    - [Story 23.2: Company Size Evaluation Helper](#story-232-company-size-evaluation-helper)
    - [Story 23.3: Opportunity NAICS Size Check](#story-233-opportunity-naics-size-check)
24. [Subcontractor Tracker Expansion](#24-subcontractor-tracker-expansion)
    - [Story 24.1: Expanded Subcontractor Compliance Profile](#story-241-expanded-subcontractor-compliance-profile)
    - [Story 24.2: Subcontractor Risk Status](#story-242-subcontractor-risk-status)
    - [Story 24.3: Contract-Specific Subcontractor Obligations](#story-243-contract-specific-subcontractor-obligations)
25. [Policy Templates](#25-policy-templates)
    - [Story 25.1: Approved Template Library](#story-251-approved-template-library)
    - [Story 25.2: Generate Draft Policy From Template](#story-252-generate-draft-policy-from-template)
    - [Story 25.3: Policy Approval And Evidence Linking](#story-253-policy-approval-and-evidence-linking)
26. [Evidence Request Workflows](#26-evidence-request-workflows)
    - [Story 26.1: Evidence Request Creation](#story-261-evidence-request-creation)
    - [Story 26.2: Evidence Submission And Review](#story-262-evidence-submission-and-review)
    - [Story 26.3: Evidence Request Dashboard](#story-263-evidence-request-dashboard)
27. [CMMC Level 2 Readiness Expansion](#27-cmmc-level-2-readiness-expansion)
    - [Story 27.1: Level 2 Control Assessment Detail](#story-271-level-2-control-assessment-detail)
    - [Story 27.2: Responsibility Matrix](#story-272-responsibility-matrix)
    - [Story 27.3: Readiness Gap Prioritization](#story-273-readiness-gap-prioritization)
    - [Story 27.4: Level 2 Readiness Report](#story-274-level-2-readiness-report)
28. [Extraction Content Test Set](#28-extraction-content-test-set)
    - [Story 28.1: Curated Test Document Set](#story-281-curated-test-document-set)
    - [Story 28.2: Precision And Recall Evaluation](#story-282-precision-and-recall-evaluation)
    - [Story 28.3: Extraction Regression Review](#story-283-extraction-regression-review)

## 1. Delivery Foundation

This sprint area creates the engineering base that lets the team build safely and consistently. It establishes the repository layout, local services, and CI checks that every later compliance feature depends on.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 1.1 | Developer documentation and project structure docs; no customer-facing SaaS page is expected. |
| 1.2 | Local setup and service health documentation; no customer-facing SaaS page is expected. |
| 1.3 | CI workflow and pull request validation documentation; no customer-facing SaaS page is expected. |

### Story 1.1: Repository And Project Structure

This story defines the project structure for the ASP.NET Core API, React/Vite web app, Clean Architecture source projects, compliance content package, docs, and infrastructure code. It makes clear where controllers, use cases, domain logic, infrastructure adapters, frontend code, and deployment assets belong.

It fits the project by preventing the MVP from becoming a collection of mixed concerns. GCCS needs compliance logic to live in backend/domain/application layers, not only in UI screens, because obligation decisions, tenant boundaries, RBAC checks, and CUI/data-handling rules must be enforceable server-side.

The value is delivery clarity. New engineers can find the right place for changes, reviewers can enforce architectural boundaries, and the team can build govcon workflows without rewriting the foundation later.

### Story 1.2: Local Development Services

This story provides local PostgreSQL, Redis, object storage, and malware-scanning placeholder services so developers can run realistic workflows on their machines. It also defines required configuration and health checks for service connectivity.

It fits the project because GCCS depends on persistent data, background jobs, file metadata, upload validation, and future scan workflows. Local services let developers test the same categories of dependencies that the MVP will use in staging and production.

The value is faster, safer development. Developers can reproduce issues, avoid hidden environment drift, and verify upload/evidence workflows before code reaches CI or staging.

### Story 1.3: Continuous Integration Baseline

This story creates the pull request validation pipeline. CI runs restore, builds, linting, tests, migration validation, and security-oriented scans, and it makes failures visible before code is merged.

It fits the project by giving every later sprint a quality gate. Tenant isolation, RBAC, audit logging, content validation, upload guardrails, and report correctness need automated checks because manual review alone is too fragile.

The value is release confidence. CI catches regressions early, makes logs actionable, and gives stakeholders evidence that the team is protecting the MVP baseline as the system grows.

## 2. Tenant, Identity, And RBAC

This sprint area creates the multi-tenant security model. It controls who belongs to which customer workspace and what each user is allowed to do.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 2.1 | Settings or tenant administration view for tenant metadata and status; backend tenant APIs. |
| 2.2 | Settings user/member management view for tenant memberships. |
| 2.3 | Settings invitation workflow for inviting, accepting, revoking, and expiring invitations. |
| 2.4 | Role-aware navigation and permission-controlled actions across all SaaS pages. |

### Story 2.1: Tenant Creation

This story lets the platform create tenant records with required metadata such as ID, display name, status, and timestamps. It also ensures tenant-owned records carry tenant IDs and that lifecycle changes are audited.

It fits the project because every customer workspace, contract, evidence item, obligation, subcontractor, and report must be isolated by tenant. GCCS cannot be trustworthy if one customer can see or affect another customer's data.

The value is the root of customer trust. Tenant creation gives the SaaS its basic customer boundary and supports future advisor and multi-client access models without mixing data.

### Story 2.2: User Memberships

This story manages how users are attached to tenants, including users who may belong to more than one tenant. It also rejects duplicate memberships and audits membership changes.

It fits the project because GCCS serves customers, advisors, MSPs, and consultants who may need access to one or more workspaces. Memberships define the relationship between a global user identity and a specific customer tenant.

The value is controlled collaboration. Customers can add the right people to the right tenant while preserving tenant-scoped views and avoiding accidental cross-client exposure.

### Story 2.3: User Invitations

This story lets tenant admins invite users by email and role, with token, expiration, pending, accepted, expired, and revoked states. It blocks non-admin invitation attempts and audits invitation lifecycle events.

It fits the project by making onboarding a governed workflow instead of an informal database operation. A compliance SaaS must know who invited whom, when, under what role, and whether the invitation was accepted.

The value is secure, auditable onboarding. Customers can bring in staff or advisors while the system keeps a clear record of access changes.

### Story 2.4: Role-Based Permissions

This story implements the permission matrix for roles such as owner, admin, compliance manager, contributor, auditor, and advisor. It enforces permissions server-side and reflects them in the UI.

It fits the project because compliance records include sensitive business, contract, security, vendor, and evidence data. UI hiding is helpful, but direct API calls must also be denied when a user lacks permission.

The value is least-privilege access. Users can do their jobs without receiving unnecessary powers, and read-only auditors can review approved information without changing customer records.

## 3. Authenticated Application Shell

This sprint area turns the product into an authenticated SaaS experience backed by protected APIs and role-aware navigation.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 3.1 | Protected API access behavior across authenticated pages; no standalone page is expected. |
| 3.2 | Main SaaS shell with Dashboard, Profile, Contracts, Obligations, Calendar, Evidence, CMMC, Subcontractors, Reports, and Settings navigation. |

### Story 3.1: Protected API Access

This story ensures protected endpoints reject unauthenticated requests, resolve the current user and tenant, handle missing tenant context clearly, and include correlation IDs in responses and logs.

It fits the project by connecting identity, tenant scope, and backend behavior. Every important GCCS workflow relies on knowing the active user, active tenant, and traceable request context.

The value is secure API behavior and easier troubleshooting. Customers get consistent errors, and the team can investigate issues using correlation IDs across logs and support cases.

### Story 3.2: SaaS Navigation Shell

This story creates the authenticated workspace shell, including primary routes, keyboard-accessible navigation, role-aware menu items, and loading, empty, and error states.

It fits the project by giving users a stable way to move between company profile, contracts, obligations, calendar, evidence, CMMC, subcontractors, reports, audit logs, and settings.

The value is usability. The first screen is the working dashboard, not marketing content, so pilot users can complete the end-to-end MVP workflow without needing engineering assistance.

## 4. CUI-Ready Gated Controls

This sprint area protects the MVP's declared CUI-ready gated posture. It makes users acknowledge prohibited data rules and blocks unsafe upload behavior.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 4.1 | Upload-related onboarding notice shown in Contract document upload and Evidence upload views. |
| 4.2 | Contract document upload and Evidence upload views; upload validation/error states. |

### Story 4.1: Data Handling Acknowledgement

This story displays a data handling notice before the first upload, disables upload until acknowledgement, stores the acknowledgement with tenant/user/timestamp/version, and audits the event.

It fits the project because the MVP supports CUI-ready workflows with gated CUI acceptance, while real CUI upload is allowed only for approved CUI-ready tenants. Classified data, export-controlled technical data, and other prohibited sensitive records still require a separately approved deployment posture. Upload workflows are where that risk is most likely to occur.

The value is risk reduction. The product creates a clear user-facing control and a record that the customer was warned before placing files into the platform.

### Story 4.2: Upload Guardrails

This story enforces upload validation, including file type limits, size limits, validation status, malware-scan placeholder status, and failed-upload audit events.

It fits the project because contract documents and evidence files are central to GCCS, but uploads also carry security and data-scope risk. Guardrails apply before files become usable evidence or contract records.

The value is safer evidence handling. The platform can reject unsupported or oversized files, track scan status, and avoid treating unvalidated files as compliance proof.

## 5. Audit Logging

This sprint area creates the audit trail needed for compliance workflows, customer trust, security review, and operational support.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 5.1 | Backend audit event model used by sensitive actions across all pages; no standalone customer page is expected. |
| 5.2 | Settings audit log viewer with pagination and filters. |

### Story 5.1: Append-Only Audit Events

This story records sensitive actions with tenant, actor, action, entity, timestamp, summary, and request metadata. It also prevents normal APIs from updating or deleting audit events.

It fits the project because GCCS must show who changed compliance records, evidence, access, statuses, uploads, reports, and content. Audit events support both customer accountability and internal investigation.

The value is traceability. Customers can prove process history, and the team can detect and investigate critical actions without relying on mutable records.

### Story 5.2: Audit Log Viewer

This story gives authorized users a tenant-scoped audit log view with pagination and filters by actor, action, date range, and entity type. Unauthorized roles are denied access.

It fits the project by turning the audit trail into an operational feature, not just a backend table. Admins, owners, and permitted advisors need a way to review activity inside the tenant.

The value is transparency. Customers can answer "who did what and when" during internal reviews, support investigations, and pilot readiness checks.

## 6. Compliance Content Foundation

This sprint area creates the source-backed obligation library that powers contract-specific obligations without making unsupported legal claims.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 6.1 | Compliance content schema/docs and obligation detail data shown in Obligation pages. |
| 6.2 | Compliance content import tooling/docs; no customer-facing SaaS page is expected for MVP import. |
| 6.3 | Internal content review/admin workflow and customer-facing Clause Library/Obligation views that hide draft or retired content. |

### Story 6.1: Obligation Schema

This story defines required obligation metadata: source URL, last reviewed date, risk, owner, confidence, trigger logic, required actions, flow-down flag, review state, and evidence examples.

It fits the project because GCCS depends on obligations that are traceable, reviewable, and explainable. The product should guide work from curated compliance content, not from unstructured notes or unsupported interpretations.

The value is defensible content. Every customer-visible obligation can show where it came from, when it was reviewed, why it applies, and what evidence may support it.

### Story 6.2: Content Import

This story imports compliance content packages, validates schema errors clearly, prevents duplicate imports, and logs import results for maintainers.

It fits the project by allowing the obligation library to grow in a controlled way. FAR, DFARS, CMMC, SBA, and other content can be managed as source-controlled packages instead of one-off manual database edits.

The value is maintainability. The team can update and test compliance content repeatedly while preserving source metadata and avoiding duplicate clauses or obligations.

### Story 6.3: Content Review State

This story manages content states such as draft, in review, approved, published, and retired. Draft content stays hidden from customers, expert-review-required content cannot publish without reviewer metadata, and retired content cannot be newly mapped.

It fits the project because content governance is central to avoiding stale or unsupported compliance guidance. GCCS needs a publishing workflow before customers rely on obligations.

The value is content safety. Customers see only approved/published guidance, and the team can retire or review content without breaking traceability.

## 7. Company Compliance Profile

This sprint area captures the customer's baseline compliance context: entity details, NAICS, size status, certifications, and renewal-related information.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 7.1 | Company Profile page for creating, editing, drafting, and completing profile data. |
| 7.2 | Company Profile page NAICS and size status section. |
| 7.3 | Company Profile page certification section and Calendar renewal items. |

### Story 7.1: Create Company Profile

This story lets users create and update a company profile, save drafts, validate required fields, calculate completion percentage, and audit profile changes.

It fits the project because the company profile is the starting context for determining obligations, gaps, renewals, and readiness. It anchors the user's tenant in real business information.

The value is onboarding clarity. Customers can see what profile information is missing and build a usable compliance baseline before adding contracts and obligations.

### Story 7.2: NAICS And Size Status

This story records multiple NAICS codes, one primary NAICS, and size status details per NAICS. It also warns when size status is missing.

It fits the project because small business qualification and opportunity fit often depend on NAICS and size status. These fields affect profile completeness and future SBA-related guidance.

The value is better bid and compliance context. Users can organize small business status by NAICS instead of keeping it in spreadsheets or disconnected notes.

### Story 7.3: Certification Tracking

This story tracks standard and custom certifications such as 8(a), WOSB, EDWOSB, HUBZone, SDVOSB, and SDB. It flags expired certifications, generates renewal tasks, and audits certification changes.

It fits the project because socioeconomic certifications are central to many small govcon businesses. They affect eligibility, renewals, reporting, and readiness for proposals or reviews.

The value is fewer missed renewals and clearer status visibility. Customers can see certification risks before they become bid, audit, or eligibility problems.

## 8. Contract Intake

This sprint area creates the contract record and captures the documents, dates, deliverables, and data posture needed to drive obligations.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 8.1 | Contracts page list/detail/create/edit workflow. |
| 8.2 | Contracts page document metadata and upload workflow. |
| 8.3 | Contracts page deliverables section and Calendar deadline items. |

### Story 8.1: Create Contract Record

This story lets users create draft and active contracts with key fields such as role, contract type, agency or prime, dates, and data handling posture. Contract lists and details are tenant scoped and audited.

It fits the project because GCCS is contract-specific, not just a generic checklist. Obligations, clauses, deliverables, evidence, and reports need a contract anchor.

The value is structured contract visibility. Customers can organize the basic facts that determine what work must be tracked and who is responsible.

### Story 8.2: Contract Document Metadata And Upload

This story attaches allowed contract documents to a contract with document type, storage reference, validation status, scan status, and audit events. Uploads require data handling acknowledgement.

It fits the project because contracts, solicitations, flow-down attachments, and related documents are the inputs for clause tagging and obligation generation. The story connects file handling to the CUI/data-handling controls.

The value is a safer contract intake path. Customers can store metadata and allowed documents while the platform blocks unsupported upload behavior.

### Story 8.3: Contract Dates And Deliverables

This story records deliverables with owner, due date, status, and description. Deliverable due dates appear on the calendar, overdue items are flagged, and status changes are audited.

It fits the project because compliance work often overlaps with contract performance deadlines. Deliverables need the same ownership, calendar, and audit discipline as obligations.

The value is operational readiness. Customers can see upcoming contract work and avoid missing deadlines that could affect performance, renewal, or prime relationship trust.

## 9. Manual Clause Tagging

This sprint area connects contracts to published clause content. It deliberately starts with manual tagging before automated extraction so the MVP can be useful while keeping human control.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 9.1 | Contracts page clause library search panel. |
| 9.2 | Contracts page clause attachment/removal workflow. |
| 9.3 | Contracts page clause processing and Obligation Dashboard generated obligation views. |

### Story 9.1: Clause Library Search

This story lets users search published clauses by number, title, and category while showing source URL and last reviewed date. Draft, retired, and other tenant custom content stay hidden.

It fits the project because users need a reliable way to find clauses from the curated library and connect them to contracts. Search is the entry point for obligation generation.

The value is source-backed contract analysis. Customers can identify relevant clauses without relying on unsupported free-text interpretation.

### Story 9.2: Attach Clause To Contract

This story attaches a published clause to a contract with a reason and source document reference, rejects duplicates, requires a reason for removal, and audits add/remove actions.

It fits the project because attaching clauses is the bridge between contract intake and obligation tracking. It records why a clause was associated with a contract and where it came from.

The value is traceability. Customers can explain why obligations exist and maintain a clean contract-clause record over time.

### Story 9.3: Generate Obligations From Clause

This story generates contract-specific obligations from mapped clause templates, including source URL, owner, action, evidence examples, risk, confidence, review metadata, and default tasks where needed.

It fits the project because the obligation dashboard should not require users to manually rewrite every requirement. Published content templates become actionable customer work.

The value is workflow acceleration. Customers move from "this clause exists" to "these are the tasks, evidence, owners, and risks we need to manage."

## 10. Obligation Dashboard

This sprint area gives users a central view of what applies, what is overdue, who owns it, and what evidence is needed.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 10.1 | Obligations page list, filters, empty state, high-risk, and overdue states. |
| 10.2 | Obligations page detail view with source, evidence, task, flow-down, and status information. |
| 10.3 | Obligations page assignment controls and related notification behavior. |

### Story 10.1: Obligation List And Filters

This story displays tenant-scoped obligations with filters by contract, risk, owner, status, module, due date, and source. High-risk and overdue obligations are visually distinct.

It fits the project because the obligation list is one of the core MVP screens. It turns contract and content data into a working compliance queue.

The value is focus. Users can quickly identify the obligations that need attention instead of hunting across contracts, spreadsheets, and notes.

### Story 10.2: Obligation Detail

This story shows source-backed obligation detail: summary, trigger, action, owner, evidence examples, flow-down requirement, source link, confidence, last reviewed date, expert review flag, linked tasks, and linked evidence.

It fits the project because users need enough context to act responsibly without treating the product as final legal advice. The detail page connects guidance, source traceability, and execution records.

The value is practical explainability. Users can understand what the obligation means, what to do next, and what proof to attach.

### Story 10.3: Ownership Assignment

This story assigns obligations to a tenant member or role, denies unauthorized assignment attempts, audits changes, and emits notifications when enabled.

It fits the project because compliance readiness depends on accountability. Obligations without owners become hidden risk.

The value is execution discipline. Teams can route work to contracts, IT, HR, finance, security, or advisors and track responsibility clearly.

## 11. Task And Compliance Calendar

This sprint area turns obligations, renewals, deliverables, evidence expirations, and CMMC actions into scheduled work.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 11.1 | Calendar page task list/detail workflow and task links from related module pages. |
| 11.2 | Calendar page aggregated calendar view and filters. |
| 11.3 | Calendar page generated renewal items plus source-module renewal fields. |

### Story 11.1: Task Management

This story creates tasks linked to obligations, contracts, controls, evidence, subcontractors, and certifications. It supports valid status transitions and audits task changes.

It fits the project because obligations become useful only when they can be assigned, tracked, blocked, completed, canceled, or reopened as work items.

The value is day-to-day execution. Customers can manage compliance as actionable tasks instead of static records.

### Story 11.2: Calendar View

This story aggregates tasks, renewals, reports, contract deadlines, deliverables, and policy reviews into a tenant-scoped calendar with filters and accessible overdue treatment.

It fits the project because a major customer pain is missing renewals, reports, wage updates, certifications, evidence expirations, or contract deadlines.

The value is deadline visibility. Users can see what is coming due and prioritize work before it turns into audit or performance risk.

### Story 11.3: Renewal Generation

This story generates renewal tasks from dated records such as SAM, certifications, evidence, insurance, policy reviews, and CMMC affirmation dates. It prevents duplicates and links tasks back to source records.

It fits the project because renewal tracking should be automatic once the system has dates. Manual calendar entry would weaken the MVP promise.

The value is proactive readiness. Customers get repeatable reminders tied to actual profile and compliance records.

## 12. Evidence Vault

This sprint area creates the core proof system for GCCS. It tracks metadata, files, versions, approval state, tags, links, and expiration.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 12.1 | Evidence page metadata form/list/detail and evidence links from Obligations and CMMC views. |
| 12.2 | Evidence page file upload, validation, version history, download, and delete states. |
| 12.3 | Evidence page approval/rejection workflow and Reports evidence package eligibility. |

### Story 12.1: Evidence Metadata

This story creates evidence records with title, type, owner, approval status, expiration date, tags, description, and source links. Evidence can link to multiple obligations or controls and can be filtered by tags.

It fits the project because evidence is the proof that work happened. Metadata lets the product organize evidence without relying on folder structures alone.

The value is reusable proof. One policy, screenshot, attestation, or training record can support multiple obligations while remaining searchable and expiration-aware.

### Story 12.2: Evidence File Upload

This story handles evidence file uploads with data handling acknowledgement, validation and scan gating, version history, permissions, download/delete controls, and audit events.

It fits the project because customers need to attach actual artifacts, but file handling must honor the MVP CUI-ready gated posture and security controls.

The value is controlled document proof. Users can upload allowed evidence, replace files without losing history, and ensure unvalidated files are not treated as usable evidence.

### Story 12.3: Evidence Approval

This story restricts evidence approval to authorized roles, requires reasons for rejection, makes approved evidence eligible for reports, and audits approval decisions.

It fits the project because not every uploaded file should count as accepted proof. Evidence needs a review state before it appears in packages or reports.

The value is quality control. Customers can distinguish draft, rejected, archived, expired, and approved evidence when preparing for prime, internal, or compliance reviews.

## 13. CMMC Readiness Tracker

This sprint area helps DoD-focused customers track CMMC Level 1 and Level 2 readiness in a draft-only, source-backed, non-certifying workflow.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 13.1 | CMMC page assessment creation, level selection, scope links, and progress summary. |
| 13.2 | CMMC page control readiness list/detail with evidence, task, asset, and POA&M links. |
| 13.3 | CMMC page POA&M item workflow and Calendar remediation tasks. |
| 13.4 | CMMC page annual affirmation tracker and Calendar affirmation reminders. |

### Story 13.1: CMMC Level Selection

This story creates readiness assessments for Level 1 or Level 2, links them to company profiles and contracts, tracks status/dates/owner, summarizes progress, and audits changes.

It fits the project because many target customers are DoD subcontractors preparing for CMMC. Level selection defines the assessment scope before controls, evidence, POA&M items, and affirmation reminders are tracked.

The value is structured readiness. Customers can organize CMMC work by level and scope without the product claiming certification or assessment success.

### Story 13.2: Control Readiness

This story loads controls by selected level, tracks control statuses, links evidence/tasks/assets/POA&M items, shows source baseline, and updates progress.

It fits the project because control-by-control tracking is the working layer of CMMC readiness. It connects CMMC requirements to evidence and remediation work.

The value is gap visibility. Customers can see what is implemented, partially implemented, not applicable, not started, or needs review.

### Story 13.3: POA&M Items

This story creates POA&M items linked to controls with gap, remediation plan, owner, due date, risk, status, task/calendar connection, open/overdue summary, and audit events.

It fits the project because readiness gaps need remediation plans, not just labels. POA&M items convert control gaps into owned, scheduled work.

The value is remediation management. Customers can track what must be fixed, who owns it, when it is due, and how it affects readiness.

### Story 13.4: Annual Affirmation Tracker

This story tracks CMMC affirmation due dates, generates reminder tasks, links evidence, and audits affirmation updates.

It fits the project because annual affirmation is an ongoing compliance management concern for CMMC customers. It belongs on the calendar and in evidence workflows.

The value is continuity. Customers can avoid losing track of recurring affirmation duties and preserve supporting evidence.

## 14. Subcontractor Flow-Down Tracker

This sprint area manages subcontractor profiles, required flow-down clauses, evidence requests, and contract-specific subcontractor compliance status.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 14.1 | Subcontractors page profile list/detail/create/edit workflow. |
| 14.2 | Subcontractors page flow-down clause assignment/status workflow and signed evidence links. |
| 14.3 | Subcontractors page evidence request workflow and Calendar request due dates. |

### Story 14.1: Subcontractor Profile

This story creates subcontractor profiles with legal name, point of contact, role, statuses, sensitive flags, dates, workshare percentage, and contract links. CUI and export-control flags are visible but do not imply CUI storage.

It fits the project because many small contractors act as primes or lower-tier subcontractors and must track supplier compliance obligations. Subcontractors need their own profile data and contract associations.

The value is supplier visibility. Customers can understand who supports each contract, what role they play, and where compliance risk may exist.

### Story 14.2: Flow-Down Clause Tracking

This story assigns required flow-down clauses from contract obligations to subcontractors, tracks statuses such as required, sent, acknowledged, signed, waived, and not applicable, links signed evidence, and audits status changes.

It fits the project because flow-downs are a central govcon risk. A clause that applies to a prime or upstream contract may need to be communicated and documented with a subcontractor.

The value is proof of flow-down management. Customers can show which clauses were sent, acknowledged, signed, waived, or marked not applicable, and they can attach approved signed evidence.

### Story 14.3: Subcontractor Evidence Requests

This story creates subcontractor evidence requests with requested item, due date, status, recipient, linked obligation, calendar visibility, received evidence linkage, completion updates, and overdue warnings.

It fits the project because flow-downs and supplier compliance often depend on documents from subcontractors, such as attestations, certifications, insurance, acknowledgements, or other approved evidence.

The value is follow-through. Customers can request, track, receive, and report on subcontractor evidence without losing requests in email threads.

## 15. Reports

This sprint area converts tracked compliance work into shareable outputs for internal reviews, prime contractor requests, CMMC readiness discussions, evidence packages, and subcontractor reviews.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 15.1 | Reports page compliance status report view/export. |
| 15.2 | Reports page contract obligation matrix view/export; Contract and Obligation data sources. |
| 15.3 | Reports page CMMC readiness report view/export; CMMC data sources. |
| 15.4 | Reports page evidence package generation/read-only package view; Evidence data sources. |
| 15.5 | Reports page subcontractor compliance report view/export; Subcontractor data sources. |

### Story 15.1: Compliance Status Report

This story generates a current status report showing obligation status, overdue tasks, evidence status, CMMC progress, subcontractor gaps, and high-risk items. It stores snapshot metadata and audits report generation.

It fits the project because leadership and compliance owners need a concise view of overall readiness. The report summarizes the operating state of the tenant.

The value is management visibility. Customers can communicate risk, progress, and gaps without manually compiling status from multiple screens.

### Story 15.2: Contract Obligation Matrix

This story generates a contract-specific matrix with clause, source, obligation, owner, status, risk, due date, evidence, flow-down indicators, source links, and last reviewed dates. Exports match screen data.

It fits the project because the obligation matrix is one of the clearest expressions of GCCS's promise: turn a contract into a source-backed list of required actions.

The value is audit and contract readiness. Customers can explain what applies to a specific contract and what has been done about it.

### Story 15.3: CMMC Readiness Report

This story reports CMMC progress by control family/category, includes POA&M items, gaps, evidence links, and affirmation dates, filters evidence links by permission, and retains role-protected snapshots.

It fits the project because CMMC work needs a reportable readiness view for internal teams, consultants, MSPs, and advisors.

The value is readiness communication. Customers can discuss status and gaps while preserving RBAC and avoiding unsupported certification claims.

### Story 15.4: Evidence Package

This story generates read-only evidence packages by selected obligations, contract, CMMC controls, or subcontractor scope. Approved evidence is included by default, and the manifest records title, type, links, approval state, and timestamp.

It fits the project because customers often need to respond to prime, auditor, internal, or advisor requests with a focused set of proof.

The value is faster evidence response. Customers can package the right approved records without exposing unrelated drafts or rejected files.

### Story 15.5: Subcontractor Compliance Report

This story reports subcontractor compliance by contract, highlights missing or overdue evidence requests, includes flow-down statuses, and preserves tenant-scoped exports.

It fits the project because supplier compliance is part of contract readiness. Primes and contractors need a clear view of subcontractor gaps.

The value is stronger subcontractor oversight. Customers can spot missing signed flow-downs, incomplete evidence, and overdue supplier tasks before they become contract risk.

## 16. Notifications

This sprint area keeps users informed about assignments, due dates, overdue items, evidence requests, renewals, and CMMC affirmation reminders.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 16.1 | Settings notification preferences view. |
| 16.2 | Notification center and Calendar due-date reminder behavior. |
| 16.3 | Notification center, mark-as-read behavior, and deep links to assigned records. |

### Story 16.1: Notification Preferences

This story assigns default preferences by role and lets users configure notifications for assignments, due-soon items, overdue items, evidence requests, renewals, and CMMC affirmation. Preference changes are tenant-aware and audited.

It fits the project because different roles need different notification levels. Owners, compliance managers, contributors, advisors, and auditors should not all receive the same noise.

The value is useful communication. Users can stay informed without being overwhelmed, while the platform keeps a record of preference changes.

### Story 16.2: Due-Date Reminders

This story identifies upcoming reminders, separates overdue reminders, prevents duplicate deliveries for the same event, and logs delivery failures without disrupting unrelated deliveries.

It fits the project because deadlines drive many compliance risks. Calendar visibility is important, but reminders help users act before due dates pass.

The value is proactive compliance operations. Customers are nudged about approaching and overdue work from tasks, renewals, evidence, reports, and affirmations.

### Story 16.3: Assignment Notifications

This story sends notifications when tasks, obligations, POA&M items, or evidence requests are assigned. Notifications link to the source record, support mark-as-read, and enforce authorization when opened.

It fits the project because ownership assignment only works if the owner knows about it and can navigate directly to the work.

The value is faster handoff. Teams can assign compliance work and trust the system to notify the right person while still enforcing access controls.

## 17. MVP Hardening And Release Readiness

This sprint area verifies that the MVP works end to end, protects tenant data, deploys to staging, and satisfies launch controls before pilot or production use.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 17.1 | End-to-end pilot workflow across Dashboard, Profile, Contracts, Obligations, Calendar, Evidence, CMMC, Subcontractors, Reports, Settings, and Notification Center. |
| 17.2 | Security verification docs/tests across all tenant-owned pages and protected APIs. |
| 17.3 | Staging environment documentation and deployment health/smoke test views or outputs. |
| 17.4 | Production readiness checklist documentation and launch limitation/support guidance. |

### Story 17.1: End-To-End Pilot Workflow

This story runs the full non-CUI pilot workflow: tenant/users, profile, contract, clauses, obligations, tasks, evidence, CMMC records, subcontractors, reports, and notifications. It also verifies role-specific happy paths and regression coverage.

It fits the project because the MVP is successful only if pilot users can complete the core workflow without engineering assistance.

The value is launch confidence. The team can prove that the separate modules operate as one usable product.

### Story 17.2: Security And Tenant Isolation Verification

This story attempts cross-tenant access across all tenant-owned modules, tests direct API RBAC bypass attempts, verifies tenant filters in repository/service tests, and documents security test results.

It fits the project because tenant isolation and server-side authorization are non-negotiable for a multi-tenant compliance SaaS.

The value is trust and risk reduction. The team has documented evidence that customer data boundaries and role rules are enforced across the product.

### Story 17.3: Staging Environment

This story provisions and deploys staging through CI/CD, including API, web, database, storage, cache, queue, secrets, jobs, dependency health checks, and post-deploy smoke tests. It also verifies staging has no production data or production secrets.

It fits the project because staging is the rehearsal environment for release readiness, support, backup checks, smoke tests, and rollback practice.

The value is operational maturity. The team can validate changes in a production-like environment before customers rely on them.

### Story 17.4: Production Readiness Checklist

This story ensures launch cannot proceed until readiness checklist items are complete or approved. It documents known limitations, CUI/data-handling boundaries, malware scanning limitations/path, support guidance, launch content review, and rollback testing.

It fits the project because GCCS operates in a regulated-market context where customer promises, data scope, content quality, security posture, support readiness, and rollback plans must be explicit.

The value is disciplined release control. The team avoids launching with unclear limitations, unreviewed content, incomplete support paths, or untested rollback procedures.

## 18. Automated Clause Extraction

This sprint area starts the Phase 2 move from manual clause tagging toward assisted contract analysis. It lets GCCS accept an uploaded contract document, process it asynchronously, identify likely clause references, and present those results for human review before anything becomes authoritative.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 18.1 | Contract document detail extraction action, extraction job API/status model, background worker stub, and audit events. |
| 18.2 | Extraction processing services, clause candidate data, job failure reasons, and candidate matching against the clause library. |
| 18.3 | Contract document extraction results review screen with filters, candidate detail, and accept/reject/edit/link actions. |

### Story 18.1: Extraction Job Intake

This story lets an authorized user start a clause extraction job from an existing contract document. The job records tenant, source document, requester, status, timestamps, and failure reason, then hands processing to a background worker or queue stub.

It fits the project by creating the operational doorway for automated clause extraction without blocking the user in the browser. GCCS already has contract documents and manual clause tagging; this story connects those pieces to an asynchronous processing workflow that later stories can expand.

The value is workflow acceleration with control. Compliance managers can start analysis from the document they are already reviewing, while tenant scoping, RBAC, validation, and audit logging keep the action secure and traceable.

### Story 18.2: Text Extraction And Clause Candidate Detection

This story extracts text from supported non-CUI contract files and detects likely FAR, DFARS, agency supplement, and local clause references. It stores candidates with raw text, normalized citation, title when available, location metadata, confidence, match method, and links to known clause library records when confidence is high.

It fits the project by turning uploaded contract material into reviewable structured data. Instead of asking users to find every clause manually, GCCS can surface likely matches while still respecting the CUI-ready gated upload posture and safe failure handling for unsupported or unreadable files.

The value is reduced manual effort and fewer missed clauses. Users get a starting point for clause review, and the system preserves enough metadata to support later review, audit, measurement, and improvement.

### Story 18.3: Extraction Results Review Screen

This story gives users a screen for reviewing extracted clause candidates beside the source contract context. Users can filter candidates, inspect confidence and source location, accept or reject candidates, edit citations, or link candidates to clause library records.

It fits the project by ensuring automated extraction remains human-supervised. Accepted candidates become reviewed contract clause links only after user action, while rejected candidates remain visible in extraction history for traceability.

The value is trustworthy automation. GCCS speeds up clause tagging but does not silently convert machine-detected text into obligations without a review decision.

## 19. Human Review Workflow

This sprint area adds explicit governance around extracted clauses and AI-suggested obligations. It prevents unreviewed machine output from becoming customer-facing compliance guidance.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 19.1 | Extraction candidate review states, transition rules, review filters, reviewer metadata, and audit events. |
| 19.2 | AI-suggested obligation review model and workflow, draft labels, dashboard/report exclusion rules, and source metadata. |
| 19.3 | Expert review queue with escalation reasons, assignments, due dates, notifications, and resolution workflow. |

### Story 19.1: Review States For Extracted Clauses

This story adds formal review states for extracted clause candidates, including pending review, accepted, rejected, needs clarification, and superseded. It records reviewer, reviewed date, decision notes, decision reason, and audit events for state transitions.

It fits the project by putting human judgment between extraction and obligation generation. GCCS can use automation to find likely clauses, but only accepted candidates should drive downstream contract clause links and obligations.

The value is governance and risk reduction. Customers and internal reviewers can see what was accepted, rejected, or left unresolved, which helps avoid treating raw extraction output as authoritative.

### Story 19.2: AI-Suggested Obligation Review

This story models AI-suggested obligations separately from approved obligations. It stores generated summaries, proposed owners, required actions, evidence suggestions, source citations, confidence, prompt version, model identifier, retrieved source references, and review state.

It fits the project by allowing GCCS to use AI as drafting support without weakening compliance content governance. Draft suggestions stay out of approved dashboards and reports until a qualified reviewer approves or revises them.

The value is safer AI-assisted content creation. The team can speed up obligation drafting while preserving source citations, reviewer accountability, and customer-facing clarity that unreviewed content is draft-only.

### Story 19.3: Expert Escalation Queue

This story creates a queue for uncertain or high-risk clause and obligation decisions. Reviewers can escalate items with required reasons such as low confidence, conflicting sources, customer dispute, high-risk clause, or possible legal interpretation, then assign experts and track resolution.

It fits the project by giving the compliance content process a place for hard decisions. Some extracted clauses and AI suggestions should not be approved by ordinary workflow alone, especially when the interpretation affects customer obligations or risk.

The value is controlled escalation. High-risk decisions get owner, priority, due date, notification, and resolution metadata before they can become approved product content.

## 20. Clause Library Expansion

This sprint area strengthens the source-backed clause library so it can support extraction matching, obligation mapping, search, and traceable customer explanations.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 20.1 | Clause version model, lifecycle states, version history UI/API, curated import/update workflow, and audit events. |
| 20.2 | Clause library search API and UI with citation, title, source family, obligation area, risk, and flow-down filters. |
| 20.3 | Clause-to-obligation mapping model, approval workflow, mapping history, validation, and mapping edit/view UI. |

### Story 20.1: Versioned Clause Records

This story adds versioned clause records with citation, title, source URL, effective date, last reviewed date, review owner, status, and supersedes relationships. It preserves version history when clause records are created, updated, approved, deprecated, or superseded.

It fits the project because compliance content changes over time. GCCS needs to know which clause version supported a match, mapping, obligation, report, or customer explanation.

The value is traceability. Approved versions can power matching and mappings, while deprecated or superseded versions remain available for history without being selected by default for new work.

### Story 20.2: Clause Search And Discovery

This story improves clause library search by citation, normalized citation, title, source, keyword, obligation area, risk level, and flow-down relevance. It hides draft or under-review clauses from standard users unless they have content review permission.

It fits the project by supporting both manual work and automated review. Users need to quickly find the right clause when tagging contracts, reviewing extracted candidates, or understanding why an obligation appears.

The value is speed and confidence. Better search reduces incorrect clause selection and makes source-backed content easier to discover during contract intake and review.

### Story 20.3: Clause-To-Obligation Mapping

This story maps approved clause versions to obligation templates with trigger conditions, required actions, owner roles, evidence examples, reporting deadlines, flow-down requirements, risk level, confidence, expert review flag, and review metadata.

It fits the project by connecting clause detection to the obligation dashboard. Once a clause is accepted for a contract, approved mappings define what practical work GCCS should generate for that customer.

The value is consistency. The same approved clause mapping can generate repeatable, source-backed obligations across contracts while keeping drafts and unreviewed mappings out of customer-facing workflows.

## 21. Applicability Engine

This sprint area helps GCCS decide whether an obligation likely applies based on structured facts about the company, contract, clause, data type, labor context, and subcontractor relationships.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 21.1 | Applicability facts model, provenance metadata, unknown-value handling, validation, and fact definitions documentation. |
| 21.2 | Deterministic rule format, evaluator service, result explanations, source rule metadata, and automated rule tests. |
| 21.3 | Applicability reevaluation triggers, dashboard indicators, explanation panel, result history, and audit events. |

### Story 21.1: Applicability Facts Model

This story defines tenant-scoped facts used for applicability decisions, including company profile, NAICS, certifications, agency, contract type, role, place of performance, data type, labor category, clause, subcontractor role, and FCI/CUI indicators. Facts include provenance, source record, last updated date, and explicit unknown values.

It fits the project by creating a consistent input layer for obligation logic. GCCS cannot reliably explain why something applies if it does not know which facts were used and where they came from.

The value is explainable decision support. Unknowns are not silently treated as false, and every applicability decision can point back to the tenant data that drove it.

### Story 21.2: Rule Evaluation

This story adds a deterministic rule evaluator that checks applicability rules against structured facts and returns applicable, not applicable, needs review, or insufficient information. Results include explanation, source rule ID, facts used, confidence, effective date, and review metadata.

It fits the project by turning curated content and tenant facts into repeatable workflow guidance. The evaluator supports FAR, DFARS, CMMC, SAM/SBA, and flow-down rule patterns without presenting unsupported legal conclusions.

The value is more accurate obligation dashboards. Customers see why an obligation appears or why more information is needed, and QA can test rule behavior consistently.

### Story 21.3: Obligation Applicability Updates

This story reevaluates obligations when relevant facts change, such as company profile values, contract facts, clause mappings, data type, subcontractor details, or rule versions. It stores current and prior results and displays applicability indicators with explanation panels.

It fits the project because compliance status is not static. A changed NAICS code, data-handling flag, subcontractor role, or clause mapping can change what the customer needs to do.

The value is current guidance. GCCS keeps dashboards aligned with the latest tenant context and audits material changes when obligations move between applicable, not applicable, and needs review states.

## 22. SAM.gov Entity Lookup

This sprint area connects GCCS company and subcontractor records to official SAM.gov entity data, using secure configuration and explicit user-controlled application of retrieved fields.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 22.1 | SAM.gov configuration, infrastructure adapter, service interface, diagnostics, safe errors, and secret-redaction behavior. |
| 22.2 | Company profile SAM lookup form, result view, conflict confirmation, source metadata, and audit events. |
| 22.3 | Subcontractor profile SAM lookup action, result handling, selected-field application, warnings, metadata, and audit events. |

### Story 22.1: SAM.gov API Configuration

This story configures SAM.gov lookup access with base URL, API key, timeout, retry policy, rate limit handling, service interface, infrastructure adapter, health diagnostics, and safe error behavior.

It fits the project by introducing an external authoritative data source without leaking secrets or coupling business logic directly to the API provider. The adapter can be mocked in tests and replaced if the integration changes.

The value is secure integration readiness. GCCS can use SAM.gov data while protecting API keys, avoiding sensitive log leakage, and giving users safe error messages when the external service is unavailable.

### Story 22.2: Company Entity Lookup

This story lets tenant admins search SAM.gov by UEI or legal business name, view matched legal name, UEI, CAGE, registration status, expiration date, address, and NAICS data, then explicitly apply selected fields to the company profile.

It fits the project by strengthening the company compliance profile with source-backed registration context. The user remains in control, and existing profile values are not overwritten without confirmation.

The value is cleaner onboarding and better profile accuracy. Customers can verify core registration information and preserve source, retrieved date, and applied-by metadata for auditability.

### Story 22.3: Subcontractor Entity Lookup

This story adds SAM.gov lookup to subcontractor profiles. Users can search by UEI or name, review entity status, UEI, CAGE, expiration, NAICS codes, and available status indicators, then apply selected fields to the current tenant's subcontractor record.

It fits the project because supplier compliance tracking is stronger when subcontractor records start from official entity data. No-match and multiple-match cases are handled without changing existing records.

The value is better subcontractor oversight. Teams can enrich supplier profiles with source metadata and reduce manual data entry while preserving tenant isolation and audit history.

## 23. SBA Size Helper

This sprint area adds source-backed SBA size standard reference data and guided size checks for company and opportunity workflows. It is decision support, not a final legal determination.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 23.1 | SBA size standard reference data model, import workflow, review states, validation, and audit events. |
| 23.2 | Company profile size evaluation helper with NAICS, receipts/employee inputs, result labels, disclaimers, and saved history. |
| 23.3 | Contract/opportunity NAICS size check action, result history, missing-data handling, expert-review task creation, and audit events. |

### Story 23.1: Size Standard Reference Data

This story defines and imports SBA size standard reference data with NAICS code, size metric, threshold, source URL, effective date, last reviewed date, status, owner, and review metadata. Only approved records feed customer-facing helper results.

It fits the project by giving the size helper a governed content foundation. Because SBA size standards affect small-business status workflows, the data must be source-backed, reviewed, and version-aware.

The value is defensible reference data. GCCS can support size-related workflows without relying on stale spreadsheets or unreviewed database edits.

### Story 23.2: Company Size Evaluation Helper

This story lets users select NAICS codes, enter annual receipts or employee range information, compare against approved size standards, and receive labels such as likely small, other than small, insufficient information, or expert review recommended.

It fits the project by connecting the company compliance profile to practical SBA readiness checks. Results include source context, disclaimers, tenant scope, and saved metadata when users choose to store them.

The value is clearer self-assessment support. Customers can identify likely gaps or uncertainties before bidding, renewing profile data, or discussing size status with advisors.

### Story 23.3: Opportunity NAICS Size Check

This story adds a contract or opportunity-level size check that compares the opportunity NAICS code against company inputs and approved size standards. It shows missing data, source-backed results, evaluation history, and can create expert review tasks.

It fits the project because size status is often opportunity-specific. A company may be small under one NAICS code and not under another, so the check belongs close to contract and opportunity intake.

The value is bid-readiness support. Customers can spot size-standard concerns early and create follow-up work before relying on a risky assumption.

## 24. Subcontractor Tracker Expansion

This sprint area broadens subcontractor tracking from basic profiles and flow-downs into a richer supplier compliance workspace.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 24.1 | Expanded subcontractor profile fields, validation, completeness indicator, filters, and sensitive-change audit events. |
| 24.2 | Subcontractor risk calculation rules, risk drivers, recalculation triggers, list/detail indicators, and tests. |
| 24.3 | Contract-specific subcontractor obligation links, accepted-flow-down bulk creation, supplier obligation status fields, and audit events. |

### Story 24.1: Expanded Subcontractor Compliance Profile

This story adds richer subcontractor fields: UEI, CAGE, NAICS, small-business status, certifications, insurance expiration, NDA status, CUI access, export-control status, CMMC level/status, workshare percentage, and responsible owner. It also adds validation, completeness indicators, filters, and audit events.

It fits the project by making subcontractor records useful for real compliance tracking, not just contact storage. Supplier obligations often depend on data access, certifications, flow-downs, insurance, CMMC posture, and workshare.

The value is better supplier visibility. Users can see which subcontractor profiles are complete, filter for expiring or risky data, and preserve history for sensitive changes.

### Story 24.2: Subcontractor Risk Status

This story calculates subcontractor risk from documented signals such as missing flow-downs, expired insurance, missing NDA, CUI access without CMMC status, overdue evidence, SAM status, and unresolved expert review. It shows low, medium, high, or needs review with visible risk drivers.

It fits the project by helping teams prioritize subcontractor follow-up. GCCS already tracks many supplier signals; this story turns them into an operational risk view.

The value is focused action. Compliance managers can quickly identify suppliers that need attention and understand why the system flagged them.

### Story 24.3: Contract-Specific Subcontractor Obligations

This story links subcontractors to specific contracts, flow-down clauses, obligations, and evidence requests. Supplier obligations show owner, due date, status, and required evidence, with bulk creation from accepted flow-down clauses only.

It fits the project because subcontractor compliance is contract-specific. A supplier may have different flow-downs and evidence needs depending on the contract, data involved, and accepted clauses.

The value is stronger flow-down management. Customers can track supplier requirements in the same contract context as prime obligations and avoid losing subcontractor work in separate spreadsheets.

## 25. Policy Templates

This sprint area gives customers approved policy templates that can be tailored from tenant context, reviewed, and linked as evidence without presenting draft text as final compliance advice.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 25.1 | Template library model, lifecycle states, placeholders, source references, preview/version history, and audit events. |
| 25.2 | Draft policy generation workflow, placeholder population, missing-value flags, draft storage, and edit/save screen. |
| 25.3 | Tenant policy approval states, revision history, obligation/control/evidence links, report inclusion, and audit events. |

### Story 25.1: Approved Template Library

This story creates a governed template library with title, category, body, placeholders, source references, version, lifecycle status, owner, last reviewed date, expert review flag, preview, and version history. Approval requires source and review metadata.

It fits the project by supporting repeatable policy drafting while keeping content governance intact. Standard users see approved templates, while draft and deprecated templates remain controlled by reviewer permissions.

The value is reusable, reviewed starting material. Customers can begin policies from source-backed templates instead of blank pages, and the team can manage template versions responsibly.

### Story 25.2: Generate Draft Policy From Template

This story lets users generate draft policies from approved templates. Placeholders are populated from company, contract, obligation, and CMMC context where available, missing values are flagged, and the generated policy stores the source template version and generation date.

It fits the project by connecting compliance content to tenant-specific work products. The generated output is useful immediately but remains draft until tenant approval.

The value is faster policy preparation. Customers can tailor policies with less manual copying while preserving source, version, and draft status.

### Story 25.3: Policy Approval And Evidence Linking

This story adds tenant-level approval, rejection, revision, approver metadata, review dates, expiration dates, and links between approved policies, obligations, CMMC controls, evidence packages, and reports.

It fits the project because approved policies often serve as evidence for obligations and controls. GCCS needs a way to distinguish draft policies from approved evidence-ready records.

The value is evidence reuse and lifecycle control. Customers can link approved policies to the compliance work they support while preserving revisions and audit history.

## 26. Evidence Request Workflows

This sprint area turns the evidence vault into an active collection workflow. Users can request, submit, review, return, accept, remind, and report on evidence tied to obligations, controls, contracts, and subcontractors.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 26.1 | Evidence request model, create workflows from supported records, validation, assignment notifications, and audit events. |
| 26.2 | Evidence submission and review workflow, request statuses, reviewer comments, accepted evidence linking, and notifications. |
| 26.3 | Evidence request dashboard with filters, overdue logic, bulk reminders, role-aware views, and export/report section. |

### Story 26.1: Evidence Request Creation

This story lets authorized users create evidence requests tied to obligations, controls, contracts, subcontractors, or evidence vault context. Requests include requester, assignee, related record, due date, status, instructions, required evidence type, priority, assignment notification, and audit event.

It fits the project by moving evidence collection from passive upload to managed workflow. GCCS can tell users not only what evidence is needed, but who needs to provide it and by when.

The value is accountability. Each evidence need has context, owner, due date, and tenant-safe assignment boundaries.

### Story 26.2: Evidence Submission And Review

This story lets assignees submit existing evidence or new allowed uploads, then lets reviewers accept or return submissions with comments. It manages statuses such as open, submitted, accepted, returned, overdue, and canceled, and links accepted evidence to the related requirement.

It fits the project by closing the loop between request and proof. Upload guardrails and tenant scope still apply, so the evidence workflow does not bypass data-handling controls.

The value is quality control. Requesters can review whether submitted evidence actually satisfies the requirement before it becomes accepted proof.

### Story 26.3: Evidence Request Dashboard

This story adds a dashboard for tracking evidence requests by status, due date, assignee, related record type, priority, subcontractor, and overdue state. It supports bulk reminders, export/report output, and role-aware views for requesters, assignees, auditors, and advisors.

It fits the project by giving compliance managers a working queue for evidence collection. Evidence requests across obligations, controls, contracts, and suppliers become visible in one place.

The value is operational control. Teams can see what is overdue, what is waiting for review, and where reminders are needed without manually reconciling records.

## 27. CMMC Level 2 Readiness Expansion

This sprint area deepens the CMMC workspace for DoD suppliers by adding richer Level 2 assessment detail, shared responsibility tracking, gap prioritization, and readiness reporting.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 27.1 | Level 2 control detail UI/API, assessment/evidence/responsibility fields, status history, validation, and audit events. |
| 27.2 | Responsibility matrix view/export with organization, MSP/ESP, cloud provider, subcontractor, and shared responsibility fields. |
| 27.3 | Gap priority rules, CMMC dashboard prioritized gaps, reason codes, POA&M/task creation, and recalculation tests. |
| 27.4 | Level 2 readiness report sections, source/control version metadata, draft-only language checks, export, and audit events. |

### Story 27.1: Level 2 Control Assessment Detail

This story expands Level 2 controls with assessment objective status, implementation status, evidence status, inherited responsibility, external service provider responsibility, notes, assessment date, assessor, status history, validation, and audit logging.

It fits the project by moving CMMC Level 2 tracking beyond a simple checklist. Readiness work needs detail about implementation, evidence, responsibility, and assessment history.

The value is assessment preparation. Security owners and advisors can see what is implemented, what evidence exists, who assessed it, and how the record changed over time.

### Story 27.2: Responsibility Matrix

This story creates a responsibility matrix for Level 2 controls, covering the organization, MSP/ESP, cloud provider, subcontractor, and shared responsibility. It links responsibilities to controls and evidence requests and supports grouped views and export.

It fits the project because CMMC customers often rely on external service providers, cloud providers, or subcontractors. GCCS needs to make responsibility explicit instead of leaving it buried in notes.

The value is shared-responsibility clarity. Customers can review who owns each control, identify missing provider details, and export the matrix for advisors or MSPs.

### Story 27.3: Readiness Gap Prioritization

This story calculates Level 2 gap priorities from control status, evidence status, due dates, risk level, CUI relevance, inherited responsibility, and assessment objective coverage. It shows reason codes and lets users create POA&M items or tasks from gaps.

It fits the project by helping teams focus limited resources on the most important CMMC work. Not every gap has equal operational or assessment impact.

The value is practical prioritization. Users can turn readiness gaps into assigned work and see why the system ranked an issue critical, high, medium, low, or needs review.

### Story 27.4: Level 2 Readiness Report

This story generates a Level 2 readiness report with control status, evidence status, gaps, POA&M items, responsibility matrix, source references, generated date, tenant, control version, and reviewer metadata. It uses draft-only readiness language and avoids pass/fail certification claims.

It fits the project by giving customers and advisors a governed way to discuss CMMC progress. The report reflects tenant-scoped data and source context without implying an official assessment result.

The value is executive and advisor communication. Customers can export a clear readiness package while preserving permissions, audit logging, and careful compliance language.

## 28. Extraction Content Test Set

This sprint area gives the extraction program a benchmark corpus and measurement process so automated clause detection can improve safely before customers rely on it.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 28.1 | Approved non-CUI test corpus structure, labels, metadata, label review workflow, and data handling documentation. |
| 28.2 | Extraction evaluation runner, precision/recall metrics, false positive/false negative output, thresholds, and CI/scheduled reporting. |
| 28.3 | Regression review workflow for missed clauses and false positives, classifications, follow-up tasks, resolution notes, and release summary. |

### Story 28.1: Curated Test Document Set

This story creates a test corpus using public, synthetic, redacted, or explicitly approved non-CUI documents. Each labeled document includes expected clause citations, locations when available, titles, flow-down indicators, metadata, known limitations, review workflow, and data handling rules.

It fits the project by giving extraction testing a controlled source of truth. Clause extraction cannot be measured responsibly using ad hoc customer documents or unlabeled examples.

The value is trustworthy evaluation. The team can test parser and matcher behavior against reviewed labels while preserving the product's data-handling posture.

### Story 28.2: Precision And Recall Evaluation

This story builds an evaluation runner that compares extracted candidates to expected labels and calculates precision, recall, false positives, false negatives, unmatched expected clauses, and threshold results. It produces machine-readable and human-readable output and can run in CI or on a schedule without customer data.

It fits the project by turning extraction quality into a measurable release signal. As matching logic and the clause library evolve, the team needs objective metrics rather than anecdotal confidence.

The value is regression visibility. Stakeholders can see whether extraction is improving, which clauses are missed, which extras are being detected, and whether release thresholds are met.

### Story 28.3: Extraction Regression Review

This story adds a workflow for reviewing missed clauses and false positives. Failures are classified as parser issue, matcher issue, library gap, label issue, source document quality, or expected limitation, with owner, status, resolution notes, follow-up tasks, traceability, and release summary output.

It fits the project by connecting extraction metrics to corrective action. Measurement alone is not enough; GCCS needs a way to decide what failed, who owns the fix, and whether open issues affect release readiness.

The value is continuous improvement. The extraction system becomes easier to govern, tune, and explain because every known failure can be tracked to a decision or follow-up task.

## 17A. Phase 1A: CUI Readiness Gate

This sprint area is a readiness gate inside Phase 1. It turns the product posture from general CUI-aware intent into explicit controls for tenant mode, classification, approval, customer responsibility, notices, escalation, auditability, and security review before any production tenant can upload real customer CUI.

| Story | Pages, views, or docs added/changed |
| --- | --- |
| 1A.1.1 | Tenant data handling mode model, mode history, validation, tenant administration display, and workflow access to current mode. |
| 1A.1.2 | Centralized mode enforcement across upload, evidence, notes, reports, extraction, UI restrictions, standard errors, and audit events. |
| 1A.2.1 | Shared classification metadata schema, review metadata, validation, downstream blocking, and change history. |
| 1A.2.2 | Classification selectors, warnings, badges, review queue, reviewer reclassification, and prohibited-data routing. |
| 1A.3.1 | Reviewed synthetic CUI demo dataset definition, metadata, classification, labels, and import precheck. |
| 1A.3.2 | Demo tenant seed/reset workflow with synthetic end-to-end examples, idempotency, mode restrictions, and audit logging. |
| 1A.4.1 | CUI-ready tenant approval checklist model, item metadata, states, tenant linkage, API/UI workflows, and audit logging. |
| 1A.4.2 | Server-side `CuiReady` approval gate, final approval permissions, stale-check detection, messaging, and failed-attempt audits. |
| 1A.5.1 | Baseline shared responsibility matrix, review/publish workflow, tenant settings visibility, checklist linkage, and traceability. |
| 1A.5.2 | Tenant matrix acknowledgement, version history, approval gate enforcement, change notifications, and audit logging. |
| 1A.6.1 | Versioned data handling notices for `DemoSandbox`, `NoCui`, and `CuiReady` modes with review metadata and retrieval. |
| 1A.6.2 | Notice placement and acknowledgement enforcement across onboarding, upload, notes, reports, extraction, and support. |
| 1A.7.1 | CUI support escalation intake, categories, affected item references, restricted views, content blocking, and audit logging. |
| 1A.7.2 | Escalation statuses, containment behavior, resolution history, reopen handling, notifications, reporting, and audit events. |
| 1A.8.1 | Required CUI audit event definitions and emission across success and blocked paths without sensitive content in summaries. |
| 1A.8.2 | CUI audit filters, readiness view, tenant-scoped export, export metadata, authorization, and export audit event. |
| 1A.9.1 | Security review checklist, finding tracking, accepted risk metadata, approval linkage, open finding reporting, and audit events. |
| 1A.9.2 | Technical control verification for tenant isolation, storage controls, backup/restore, admin/support access, and readiness summary. |
| 1A.9.3 | Incident response playbooks, escalation owners, tabletop evidence, approval linkage, reminders, and traceability. |

### Story 1A.1.1: Tenant Data Handling Mode Model

This story gives every tenant an explicit mode: `DemoSandbox`, `NoCui`, or `CuiReady`. It records mode history, approval references, reasons, and effective dates so every data-handling workflow can make decisions from one source of truth.

The value is enforceable posture. GCCS can distinguish demo-only, non-CUI, and approved CUI-ready tenants without relying on informal support notes or UI copy.

### Story 1A.1.2: Mode-Based Workflow Enforcement

This story applies tenant mode checks server-side and in the UI across uploads, evidence, notes, reports, and extraction jobs. It blocks direct API bypasses, returns standard errors, and records audit events for mode-restricted failures.

The value is operational safety. Users cannot accidentally or deliberately process real CUI in workflows that are not approved for it.

### Story 1A.2.1: Classification Metadata Schema

This story creates shared classification metadata for CUI-relevant objects, including `Unclassified`, `FCI`, `CUI`, `SyntheticCui`, `Prohibited`, and `Unknown`, plus source, confidence, reviewer, date, reason, and change history.

The value is consistent control. Uploads, notes, evidence, reports, documents, and extraction jobs can all enforce the same classification rules.

### Story 1A.2.2: Classification UX And Review

This story adds classification selection, warnings, badges, review queues, and reviewer reclassification workflows. Items marked `Unknown` or `Prohibited` are visibly controlled before they can be reused.

The value is human-in-the-loop data handling. Users see and resolve classification risk before content enters reports, extraction, or evidence approval.

### Story 1A.3.1: Synthetic Dataset Definition

This story defines reviewed synthetic CUI examples for demos, testing, and training. Every record is labeled `SyntheticCui`, versioned, reviewed, and visibly marked so nobody mistakes it for real customer CUI.

The value is safe demonstration. Sales, support, and QA can show realistic CUI workflows without exposing controlled or proprietary information.

### Story 1A.3.2: Demo Tenant Seeding

This story seeds `DemoSandbox` tenants with synthetic contracts, obligations, evidence, CMMC examples, subcontractor records, reports, and escalations. It prevents seed data from being loaded into production customer tenants.

The value is repeatable onboarding. Demo and training environments can be reset and reused without risking real customer data.

### Story 1A.4.1: Approval Checklist Model

This story creates the CUI-ready tenant approval checklist, including customer agreement, notices, responsibility matrix, security review, support escalation, backup/restore, admin access, retention, and incident response items.

The value is disciplined enablement. CUI workflows cannot be turned on without recorded evidence, review ownership, and approval state.

### Story 1A.4.2: Approval Gate Enforcement

This story enforces the CUI-ready approval checklist before tenant mode can change to `CuiReady`. It blocks incomplete, rejected, expired, or superseded approvals and audits failed attempts.

The value is configuration control. A mistaken tenant setting cannot authorize real CUI handling without the required approval trail.

### Story 1A.5.1: Baseline Responsibility Matrix

This story creates the shared responsibility matrix for tenant administration, access, MFA, classification, evidence storage, encryption, malware scanning, retention, backup, export, deletion, incident reporting, support, and customer content decisions.

The value is expectation clarity. Customers and internal teams can see who owns each CUI-relevant control before CUI workflows are enabled.

### Story 1A.5.2: Tenant Matrix Acknowledgement

This story lets tenant admins acknowledge the current responsibility matrix and requires that acknowledgement before CUI-ready approval. New matrix versions make prior acknowledgements outdated for future approvals.

The value is customer acceptance. GCCS has a record that the customer saw and accepted the current shared responsibility baseline.

### Story 1A.6.1: Versioned Notice Content

This story creates reviewed data handling notices for each tenant mode and workflow context. Notices carry version, effective date, owner, reviewer, and approval status.

The value is consistent guidance. Users see mode-appropriate CUI restrictions and responsibilities from governed content instead of ad hoc text.

### Story 1A.6.2: Notice Placement And Acknowledgement

This story places notices in onboarding, uploads, notes, reports, extraction, and support flows, and requires acknowledgement before CUI-relevant actions. Updated notice versions require renewed acknowledgement.

The value is timely warning. Users encounter data-handling guidance at the exact moments where a mistaken upload or processing action can happen.

### Story 1A.7.1: Escalation Intake And Classification

This story creates escalation intake for accidental CUI upload, suspected CUI, prohibited data, misclassification, and customer questions. It links affected content, severity, owner, status, and restricted support views.

The value is rapid triage. Potential data-handling problems become controlled records instead of scattered messages.

### Story 1A.7.2: Escalation Workflow And Resolution

This story adds escalation statuses, containment behavior, resolution types, reopen history, notifications, and reporting for CUI and prohibited-data cases.

The value is documented containment. Affected content stays blocked during triage, and resolutions preserve the decision history.

### Story 1A.8.1: Required CUI Audit Events

This story defines and emits audit events for mode changes, classification, upload blocks, checklist approvals, acknowledgements, downloads, exports, deletions, escalations, and extraction job actions.

The value is traceability. Readiness reviews and incident investigations can reconstruct what happened without exposing sensitive document content in audit summaries.

### Story 1A.8.2: CUI Audit Filters And Export

This story adds filters and tenant-scoped export for CUI-relevant audit events, including generated-by, generated-at, tenant, and filter metadata.

The value is review efficiency. Security reviewers and tenant admins can produce focused audit packages for readiness checks and investigations.

### Story 1A.9.1: Security Review Checklist

This story creates a formal security review checklist covering tenant isolation, storage, encryption, malware scanning, retention, backup, restore, admin/support access, logging, monitoring, and incident response.

The value is release discipline. High or critical open findings block CUI-ready approval unless properly accepted with scope, mitigation, and review date.

### Story 1A.9.2: Technical Control Verification

This story verifies the technical controls behind CUI readiness: tenant isolation for classified records and files, storage metadata, backup/restore evidence, admin/support permission checks, and readiness summary output.

The value is evidence-backed approval. CUI-ready status is based on tests and documented verification instead of assumptions.

### Story 1A.9.3: Incident Response Readiness

This story creates playbooks and readiness checks for accidental CUI upload, suspected CUI in non-CUI tenants, prohibited data, cross-tenant exposure suspicion, malware detection, and failed deletion/export requests.

The value is immediate response capability. The team has owners, triggers, containment steps, evidence collection, and closure criteria before CUI workflows are enabled.
