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
