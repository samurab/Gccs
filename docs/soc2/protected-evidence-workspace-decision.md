

Publication classification: **Sanitized public reference**. Complete originals and original SHA-256 hashes remain in protected `release-scope-lookups-20261018-v1/primary-source-snapshot-v1`. This counterpart records scope and traceability; it does not establish workforce control effectiveness, review completion or acceptance.

# Protected SOC 2 Evidence Workspace Decision

Status: **Partially implemented — selected Workforce tenant created; evidence workspace and acceptance pending**
Decision ID: `SOC2-EVIDENCE-WORKSPACE-0.1`
Decision date: 2026-09-19 EDT
Last updated: 2026-10-14 America/New_York — creation-gate preparation; provider cutoffs remain section-specific
Task mapping: `SOC2-39.2-T03`
Management owner and confirmed custodian: Founder authority FR-01 (`FR01-CUSTODY-ACK-2026-09-28`)
Required reviewer: Independent Security reviewer; qualified Legal/Privacy and CPA input where triggered

## Current Decision Record

| Field | Current record |
| --- | --- |
| Platform | Selected separate Workforce tenant created through the approved Governed Workforce route on 2026-10-14; exact display/domain and Workforce type verified. Protected evidence subscription/store, effective governance population and workspace acceptance remain unverified/unimplemented at the indicated boundaries. |
| Tenant selection | `S39.2-TENANT-SELECTION-20261013-01`: FR-01 accepted the proposed display name and immutable initial-domain prefix on 2026-10-13. Exact values are retained in the execution/provider context, not public Git; this resolves the missing management selection for form preparation only. |
| Tenant Country/Region selection | United States accepted as input and subsequently read in the target Entra Properties Tenant Country/Region field on 2026-10-14. East US 2 remains the Azure resource-location preference, not a tenant-country value or proof of all-service residency. Historical loading/blank observations remain dated limitations. |
| Disallowed creation paths | The Production External customer directory and IAB guest/customer context are not approved creation paths. The existing Production subscription must not be selected, transferred or attached, and its directory must not be altered. The existing Workforce account may be used only as the verified administrator/billing bootstrap for a separate new Workforce tenant |
| Account boundary | Target remains a new corporate Workforce SOC 2 tenant, separate from two observed External customer tenants and existing production resources. FR-01 confirmed that FeDril LLC is the responsible party for the existing Microsoft Customer Agreement and that payment administration is company-authorized while preserving the separate-new-tenant requirement. No Microsoft account, billing or subscription transfer was executed by this work |
| Billing owner | In a separate company-domain Chrome session, the authenticated principal was Global Administrator in the existing Workforce directory, Azure subscription Owner and Microsoft Customer Agreement Billing account owner. The active billing account displayed `FeDril`, its sold-to company displayed `FeDril LLC`, and registration was not provided. FR-01 additionally self-attested that FeDril LLC is the responsible party and payment administration is company-authorized. This resolves the immediate management-responsibility clarification, but independent documentary acceptance and recovery evidence remain unavailable |
| Business/domain relationship | Historical record: Sierra Labs LLC ownership of `fedril.com` remains an FR-01 self-attestation. On 2026-10-06, FR-01 self-attested that FeDril LLC is the registered legal entity and contracting name. Documentary formation/authority evidence and FeDril LLC's relationship to the domain remain unverified; do not infer domain ownership |
| Current Azure sign-in identity | Protected evidence reference; individual observations and source details are retained in the protected original. |
| Workspace owner | Target owner is FeDril LLC through the proposed corporate boundary. The registered-entity and contracting-name clarification is resolved by FR-01 self-attestation, but no accepted business-managed evidence workspace currently exists and documentary authority evidence remains unavailable |
| Hosting region | East US 2 preferred and East US fallback permitted by FR-01 for estimate purposes; contract, residency and final selection review remain pending |
| MFA enforcement | Unverified |
| Public or link sharing | Unverified; required target is disabled by default |
| Version history | Unverified |
| Audit logging | Unverified |
| Deletion recovery or backup | Recovery capability is included in the proposed architecture but is not configured or tested |
| Authorized members | FR-01 reports that a second administrator/recovery arrangement is available, but identities, custody, roles, authentication and recovery operation are not configured or tested. Independent Security, Legal/Privacy, forensic and validation reviewers remain unavailable |
| External-sharing authority | FR-01; default deny with case-specific approval, recipient verification, business purpose, expiry and qualified review when sensitive, contractual, privacy or incident information is involved |
| Save/read/deny/recover test | Not performed; execute only after platform, ownership, access, retention and recovery controls are approved |
| Current budget ceilings | Authorized up to $55/month total recurring for this new corporate setup only: identity $15 and Azure services $40, allocated as storage/recovery $10, logs $10, network $10 and contingency/tax $10. The subsequent 2026-10-13 FR-01 amendment raised the aggregate upfront ceiling to $25 inclusive of taxes and mandatory fees. Azure budget alerts are monitoring only, not a hard spending cap. Neither ceiling alone authorizes a purchase or provider write |
| Continuity or transfer arrangement | Available in principle by FR-01 self-attestation through a second administrator/recovery arrangement; identities, custody, authentication, transfer method and emergency operation remain unconfigured and untested |
| Independent Security reviewer | Unassigned / currently unavailable / deferred; not `Not Applicable` |
| Prior deferral | FR-01's 2026-09-28 no-provisioning/no-spend direction and the 2026-10-04 package inventory remain historical. They are superseded only for the bounded new corporate setup described in the 2026-10-06 authorization. Sensitive evidence collection, migration and Stories 39.18/39.22 remain gated. |
| Evidence reference | `SOC2-39.2-T03`; `SOC2-AZURE-EVIDENCE-ESTIMATE-0.1`; `SOC2-AZURE-BUSINESS-TRANSITION-0.1`; related governance references `SOC2-ROLE-FR-01` and `FR01-ACK-2026-09-19` |

The 2026-10-13 root-read/revocation checkpoint below supersedes the earlier current root-permission blocker, not the historical denied-read observations or the protected-workspace acceptance gate. The currently unresolved prerequisite is the specific agreement and dedicated non-Production target-subscription route/linkage; the existing Production-containing subscription remains excluded.

## 2026-10-13 Bounded Root-Read And Revocation Checkpoint

FR-01 approved one primary root-read window of no more than 30 minutes with conditional emergency removal. The verified primary actor elevated at 02:27:09 UTC, completed the approved read, selected No/Save to revoke, and an independent authorized CLI query at 02:33:25 UTC found the exact temporary root User Access Administrator assignment absent and zero primary/emergency elevation assignments. Matching Entra audit events report successful elevation and removal, the latter at 02:33:10 UTC. The emergency route was not used. Exact actors, assignment and audit references are retained only in the owner-only private `SOC2-ROOT-ACCESS-CHANGE-2026-10-12-01` record and byte-equal v10/recovered-v10 copies; no restricted identifiers or raw evidence were moved to Git. Earlier denied reads remain historical.

The root read established the current default new-subscription management group and one primary User Access Administrator assignment inherited at Tenant Root Group while elevated. The observed root deny, policy assignments (including initiative/policy assignments), exemptions and remediation views returned no entries; policy definitions were not inspected. Those point-in-time filters do not prove absence at child/deleted scopes, future conditions, or zero automatic cost. No root grant remains from this window; no tenant, subscription, resource, policy, billing, Production or backup-allowlist change occurred.

At the 02:45 UTC provider cutoff, the existing company session had reached the Governed Workforce wizard. It showed a paid-license notice and required Subscription and Resource Group fields, with only the existing Production-containing subscription offered. No values/options were selected and no submission occurred. Its tenant-creation billing asset would be hosted under the selected home-tenant subscription and resource group, not proof that the proposed evidence tenant would receive a dedicated subscription. A scoped query at 02:44:55 UTC returned no paid license SKU for the current bootstrap tenant; it does not describe other tenants. Active MCA metadata and read-only General Terms were observed, but the latter refer pricing/payment to applicable order/Product Terms and do not establish a specific new Azure Plan $0-upfront quote. No terms were accepted, document downloaded, purchase made or billing change performed. At that cutoff, a two-subscription design choice was pending; FR-01's later decision is recorded below. `SOC2-GAP-016`, `SOC2-39.2-T03` and `SOC2-GAP-015` remained open because eligibility, terms, protected workspace, approved retention and full save/read/deny/recover/access-review tests were absent. Independent acceptance and period evidence remained unavailable.

FR-01 subsequently approved the revised two-subscription *design* on 2026-10-13 EDT: one non-Production bootstrap subscription to host tenant creation, followed by a separate evidence subscription in the new tenant. This supersedes only the earlier undecided route choice. The 2026-10-06 bounded empty-creation authority and unchanged $55/month total/$0 upfront ceilings remain, subject to verified eligible licensing, specific terms, complete cost, target linkage and the runbook's exact final-Create confirmations. No license purchase, terms acceptance, subscription/tenant/resource creation, Production attachment or protected migration followed from the decision. Independent workspace acceptance and operating evidence remain unavailable.

FR-01 then amended the upfront boundary on 2026-10-13 EDT (`S39.2-E18`): the **current aggregate upfront ceiling is $25 including taxes and mandatory fees**; the **$55/month total recurring ceiling is unchanged**. This supersedes the earlier $0 cap prospectively, without rewriting that dated decision. The $6.48 one-seat monthly catalog subtotal before tax (`S39.2-E17`) is not an actual checkout total or eligibility result. Verify checkout/due-now, offer and MCA/Azure Plan terms, both-subscription costs and linkage before any separately approved purchase or final Create. No transaction, terms acceptance, provider change, Production change or workspace acceptance resulted from the budget amendment.

Candidate comparison: `SOC2-EVIDENCE-PLATFORM-CANDIDATES-0.1` rejects the unavailable SharePoint candidate and records that the historically personally owned Azure subscription is ineligible under the business-managed gate. `SOC2-AZURE-BUSINESS-TRANSITION-0.1` now targets a new FeDril LLC-controlled corporate Workforce tenant, billing boundary and subscription, with entity/domain verification required before any DNS or domain association. The existing Production subscription must not be selected, transferred or attached, and its directory must not be altered. `SOC2-AZURE-EVIDENCE-ESTIMATE-0.1` records the sanitized Azure design, historical retail model and current inclusive authorization. Existing customer/home tenants, production application Blob containers, personal storage and public Git are excluded.

## 2026-10-06 Bounded Authorization Event

FR-01 authorized implementation preparation and bounded creation of a new FeDril LLC-controlled corporate Workforce tenant and dedicated Azure protected-evidence boundary. The authorization is **implementation pending**, not workspace acceptance, control operation or authority to migrate protected evidence.

Protected evidence reference; individual observations and source details are retained in the protected original. IDs: FR-01. References: [pay-as-you-go offer](https://azure.microsoft.com/en-us/pricing/purchase-options/azure-account?icid=payg).

This authorization does not permit restricted evidence inspection or migration; selection, transfer or attachment of the existing Production subscription; alteration of its directory; GitHub action; Story 39.4 containment or closure; external communication; or DNS/domain association before FeDril LLC's authority over or relationship to the domain is verified.

## 2026-10-11 Execution Checkpoint

The user directed continued Story 39.2 management work while CPA, Legal/Privacy
and forensic dependencies remain deferred. The existing $55/month recurring and
$0 upfront corporate-target authorization is reused. Independent reviews remain
deferred at acceptance/migration gates and do not prevent otherwise permitted
empty preparation.

Protected evidence reference; individual observations and source details are retained in the protected original.

Protected evidence reference; individual observations and source details are retained in the protected original. IDs: S39.2-E10, SOC2-GAP-016, SOC2-EXC-006.

Management ownership, task scheduling, source screening and calendar preparation
continue through the shared register while that read is pending. Retention and
cadence approval are actual FR-01 management decisions still to be recorded;
the agent cannot perform that human self-review. Qualified retention review
remains deferred. Final creation awaits the unresolved placement/terms check,
then follows the existing bounded authority and any provider action-time terms
or access confirmations.

## 2026-10-11 Assisted Plan And Session Review

The coordinator reviewed and revised version 0.21 after the planner and grunt
finished. October 15 is a proposed management/source/cadence review checkpoint;
October 16 is a conditional estimate for bounded empty implementation. Before
creation, positively bind the corporate actor/directory, verify root/default
placement and inherited authorization/policy/automatic-cost effects, confirm the
specific agreement/action-time terms, and keep the $55/month inclusive/$0
upfront ceiling. An unmet prerequisite causes T03 replanning. The date alone
does not create authority or satisfy workspace acceptance.

Protected evidence reference; individual observations and source details are retained in the protected original. IDs: S39.2-E12, S39.2-E13.

Next step: complete existing-account authentication locally, without sharing
credentials, then verify the intended directory and existing scoped reads. If
the principal still lacks access, use a known authorized root reader or prepare
a separate reviewed access-recovery decision. No new role grant/elevation,
Production change, billing submission or resource creation occurred in this
review. Actual FR-01 management self-review/signatures and external reviews
remain separate pending/deferred gates.

## 2026-10-11 Sign-In Follow-Up (21:15 EDT / 2026-10-12 01:15 UTC)
Protected evidence reference; individual observations and source details are retained in the protected original. IDs: FR-01.

Protected evidence reference; individual observations and source details are retained in the protected original. IDs: S39.2-E13, SOC2-GAP-016, SOC2-EXC-006. Recorded status: Deferred.

## 2026-10-12 Authentication Restored (20:18 EDT / 2026-10-13 00:18 UTC)
Protected evidence reference; individual observations and source details are retained in the protected original. IDs: FR-01.

Protected evidence reference; individual observations and source details are retained in the protected original. IDs: S39.2-E13, SOC2-GAP-016, SOC2-EXC-006. Recorded status: deferred.

## 2026-10-12 Recovery Preparation Implemented

The user requested implementation of the corrected recovery solution. The
coordinator implemented the sanitized decision/approval worksheet and execution
checks in [the existing recovery runbook](azure-business-ownership-transition-plan.md),
reference `SOC2-ROOT-RECOVERY-PROPOSAL-0.1`, after planner/grunt review. This is
preparation, not an elevation grant or a signed FR-01 decision.

The current corporate administrator's direct built-in Global Administrator
assignment was read; Access management for Azure resources remained No and no
elevated-users discovery list was exposed. Existing root authorization remains
unverified and the settings denial is preserved. The two-blank-template custody
scope and 20-source project-snapshot scope do not cover a completed exact Azure
change record. Protected capture, usable revocation recovery and separate
action-time approval remain prerequisites to the conditional privilege change.
No existing source, backup scope, permission, security default, resource,
Production subscription or budget was changed. T03/GAP-016/EXC-006 remain open.

## 2026-10-12 Root Inventory Follow-Up
Protected evidence reference; individual observations and source details are retained in the protected original.

The coordinator verified three administrator account objects and an email
alias resolving to an existing object. Neither aliases nor account count prove
separate recovery custody, root authority or an independent reviewer. Exact
identifiers remain excluded. FR-01 must locally refresh the intended company
administrator session for the next scoped read; denied results remain unknown,
never zero. The protected-record and recovery preflight still make the
conditional privilege change a no-go. No permission, resource, billing or
backup-population change was performed. T03/GAP-016/EXC-006 remain open.

## 2026-09-28 Decision-Package Scheduling Event

The earlier 2026-09-28 decision named 2026-10-15 as the next workspace decision review. FR-01 clarified that **2026-10-15 is the planned completion target for the internal no-cost decision package**. Review the decision only after the package's contents and evidence references are verified; record any review and disposition separately. This scheduling clarification does not close `SOC2-GAP-015`, authorize a workspace, provision a resource, incur a cost, or establish control operation.

## 2026-10-04 Internal No-Cost Package Inventory

This is a sanitized source inventory prepared for FR-01, not a completed or verified decision package and not a management self-review. It uses existing records; no raw provider export, restricted incident artifact or generated Type I package was reviewed for this inventory.

| Decision area | Existing planning input | Unresolved verification or choice |
| --- | --- | --- |
| Business ownership and platform | [Candidate assessment](protected-evidence-platform-candidate-assessment.md) rejects the unavailable SharePoint candidate and the personally owned Azure subscription; the [business-transition plan](azure-business-ownership-transition-plan.md) proposes a separate company-controlled boundary | Protected proof of company/domain/billing authority, eligible account ownership and an explicit platform decision are unavailable |
| Continuity and access | The transition plan defines proposed administrator, recovery and reviewer stages | Protected evidence reference; individual observations and source details are retained in the protected original. |
| Retention and recovery | This record states a proposed retention rule and synthetic save/read/deny/recover/audit acceptance tests; the [Azure design](protected-evidence-azure-architecture-cost-estimate.md) describes versioning and recovery choices | Qualified retention review, selected recovery values, protected integrity evidence and synthetic test results are absent |
| Network and cost | The Azure design compares conditional access paths and estimates a $5–$15 monthly core planning envelope; the [capacity/budget record](founder-capacity-budget-decision.md) preserves FR-01's no-spend direction | Administration path, private-networking cost, current agreement price and any provisioning/spending authorization are absent; the historical $15 core ceiling is not current spend authority |
| Review and residual gaps | The Story 39.4 [investigation](story-39.4-exposure-investigation.md) and this workspace record identify the blocked classification and containment gates | Independent Security and other triggered reviewers are unavailable; public exposure, protected custody and organizational recovery remain unresolved |

Next internal step: verify that each source reference, assumption, exclusion and unresolved choice is current before treating the no-cost package as ready for FR-01 review. The planned 2026-10-15 target is not a decision date. FR-01 must record a separate dated approval or continued deferral; a continued deferral needs an owner, next review trigger and effect on Stories 39.2 and 39.4. No platform selection, data movement, account change, spend or restricted inspection follows from this inventory.

## 2026-09-28 Read-Only Capability Recheck

Authorized Azure CLI capability metadata was queried without reading storage contents, secret values, identifiers, user details, customer data or CUI. One enabled accessible subscription and three storage accounts were returned. Aggregate results were:

Protected evidence reference; individual observations and source details are retained in the protected original.

Protected evidence reference; individual observations and source details are retained in the protected original.

## Proposed Retention

Proposed retention is seven years from the end of the applicable audit or reporting period. This is a management proposal, not an AICPA, legal or universal contractual requirement. It remains subject to qualified CPA and Legal review, applicable contracts, legal holds, report terms, data-minimization requirements and verified deletion obligations. No retention schedule is approved or operating.

## Minimum Selection Gate

A candidate platform may be approved only after evidence confirms:

Protected evidence reference; individual observations and source details are retained in the protected original.

## Required Verification

After selection and configuration, create only a synthetic, non-sensitive sample and record:

| Test | Required result | Evidence to retain |
| --- | --- | --- |
| Save | Authorized custodian can store a versioned sample in the intended location | Timestamp, actor, object/version identifier and integrity reference |
| Read | Authorized reviewer can retrieve the approved version | Account, role, timestamp and successful access event |
| Deny | Unapproved account or link cannot retrieve or enumerate the sample | Denied-access result without exposing the sample |
| Recover | Authorized custodian can recover an earlier/deleted synthetic version using the approved process | Recovery event, restored version, timing and reviewer confirmation |
| Audit | Relevant save/read/share/delete/recover events are available to the reviewer | Export/reference, coverage limitations and retention |

Do not use customer documents, credentials, database contents, personnel records, CUI or live incident evidence for the initial test.

## Evidence Conclusions

| Dimension | Conclusion | Basis | Limitation |
| --- | --- | --- | --- |
| Design | Partial | Corporate target boundary, inclusive recurring ceiling, access boundary, external-sharing rule, proposed retention, Azure architecture/cost estimate and required verification are defined; FR-01 self-attested that FeDril LLC is the registered legal entity and contracting name | Documentary entity/authority evidence, FeDril LLC's domain relationship, continuity, exact network path, retention and qualified reviews remain unresolved |
| Implementation | Partial | Protected evidence reference; individual observations and source details are retained in the protected original. | The separate evidence tenant/subscription/store is not created or accepted. Effective creation authority, provider availability/validation of the accepted immutable tenant values, exact final provider confirmation, documentary/recovery evidence and workspace controls remain unresolved |
| Operating evidence | Unavailable | No save/read/deny/recover or access-review test has occurred | No operating conclusion is permitted |

## Next Action

**Current 2026-10-14 gate-resolution preparation supersedes the October 13 next-action state below.** Reuse accepted selections and completed bootstrap work. The [creation-gate packet](#creation-gate-resolution-packet--2026-10-14-americanew_york) prepares the necessary exact Tenant Creator/custody/revocation action and the effective default-template/governed-effects decision; current observations and actual approval states remain separate. No broader role, final Create or protected incident custody is inferred.

Protected evidence reference; individual observations and source details are retained in the protected original.

Reuse the actual accepted tenant display name and immutable initial-domain prefix under `S39.2-TENANT-SELECTION-20261013-01`; prepare/validate those exact values and United States in the tenant Country/Region field. Treat East2 as a candidate East US 2 resource-location preference pending technical validation, not a tenant-country value. Verify effective creation authority, provider availability and current provider review, cost, terms and governance linkage before exact final-Create confirmation; do not re-ask unchanged selections. Keep the $25 aggregate upfront ceiling inclusive of taxes and mandatory fees and $55/month aggregate recurring ceiling. Preserve the two-subscription design and Production exclusion. Use the runbook's exact final-Create confirmation only after these prerequisites pass; any necessary security-sensitive grant or new binding terms needs its own action-time approval. After tenant creation, verify the separate evidence-subscription route, recovery/access/retention controls and synthetic save/read/deny/recover/audit tests before qualified workspace acceptance. Domain association remains separately gated by documentary authority. `SOC2-39.2-T03` and `SOC2-GAP-015` remain open; restricted collection, movement and inspection remain blocked by workspace acceptance and separate inspection authorization.

## 2026-10-13 Accepted Tenant Selection — Form Preparation Only

`S39.2-TENANT-SELECTION-20261013-01` / `S39.4-E04`: FR-01 accepted the proposed
tenant display name and immutable initial-domain prefix and supplied United
States with East2 regional shorthand. The actual selected identity values remain
in the execution/provider context; only this sanitized selection reference is
recorded in Git. Use **United States** in the tenant Country/Region field. Treat
East2 as the candidate **East US 2** Azure resource-location preference, subject
to technical validation and later resource-specific approval; do not place it in
the tenant-country field or infer tenant/data residency from it.

The acceptance follows the explicit explanation that selection permits preparing
and validating the form while final creation remains gated. It resolves the prior
unanswered intended-selection prerequisite; do not request those unchanged values
again. It does not authorize final Create, role/permission grants, subscription
or resource changes, purchases, terms acceptance, protected evidence collection,
movement or restricted inspection. Effective creation authority, provider name/
domain availability, actual final-review target/terms/cost, exact final-Create
confirmation and protected-workspace acceptance remain separate requirements.
Prior pending-selection statements above are dated history and are superseded
for form preparation only. No tenant, evidence store or operating control is
established by accepting a name.

Next bounded step: prepare and validate the accepted values in the existing
corporate tenant-creation form against the approved existing non-Production
bootstrap selectors; verify the intended Country/Region, effective authority and
provider availability without granting a role or submitting Create. Preserve only
sanitized result/limitation references. Stop at the exact final-Create confirmation
boundary after a concrete review, and retain the independent acceptance, recovery,
access/retention, classification and incident gates.

### Nonbinding Form Review And Authority Boundary — 2026-10-13

Grunt prepared the selected values and **United States** in the existing form,
then reached **Review + Create** without a visible validation error. The final
**Create** control was not selected. Reaching Review does not reserve the initial
domain, prove availability or establish effective create permission. An Azure
region-metadata lookup recognizes **East US 2** (`eastus2`) in the US geography;
this confirms the candidate resource-location spelling, not service availability,
resource-specific approval, tenant country or data residency.

The current Microsoft Learn tenant-governance creation procedure lists the Tenant Creator role in the home tenant and one
of the selected-subscription ARM roles Tenant Contributor, Subscription Owner or
Subscription Creator. It states the Tenant Creator requirement applies regardless
of the restrict-tenant-creation toggle. The earlier actor-profile role retrieval error remains historical. The specific
Tenant Creator assignment view subsequently loaded with zero visible active
assignments/count zero at approximately 21:29 EDT. Eligible/indirect authority
and successful final Create permission are unproved. No role was granted by this
preparation; an exact scoped assignment, if approved, is a separate action. Source:
`https://learn.microsoft.com/en-us/entra/id-governance/tenant-governance/how-to-create-tenant`.

The procedure also describes a Free tenant-creation billing asset and a governed
relationship that provisions cross-tenant settings, delegated administrative
privileges and service principals according to the governance template. The
current review's Policy Template label is blank. A successful details read of
the existing version-1 default template displayed zero role assignments and zero
applications, with a P1/P2 full-feature warning. That displayed population does
not prove all future relationship effects absent; actual effective effects remain
unverified. A Free label is not a verified total cost or authorization for
security-sensitive inherited effects. Before final Create, verify the exact
selected template and resulting authority/application/access scope, ownership,
commercial/resource effects and rollback/retention limits, then obtain the
existing exact action-time creation approval and any separately necessary grant
or terms approval. Do not work around missing role evidence or approve an unknown
template by inference. No tenant, governance relationship, role assignment,
service principal, resource, purchase or incident-evidence movement occurred.

## Creation Gate Resolution Packet — 2026-10-14 America/New_York

Packet: `S39.2-CREATE-GATES-20261014-01`; status **Approved staged custody/grant/verify/revoke executed and verified; final Create excluded**. Scope is the two remaining creation gates, not
repeating accepted identity selections, root-read/revocation, license purchase
or empty bootstrap creation. FR-01 is the actual management owner, custodian
and proposed grant/revocation operator under the existing appointment; this
preparation is not FR-01's signature, self-review or independent approval.
Exact actor, tenant, assignment, template and protected-path bindings belong in
privately reviewed action metadata and a separately authorized administrative
record, with sanitized aliases only here.

### Gate 1 — Necessary Role, Bounded Action And Custody
Protected evidence reference; individual observations and source details are retained in the protected original.

| Approval component | Concrete proposed scope / readiness requirement |
| --- | --- |
| Necessity | Only the currently required built-in Tenant Creator role for the existing accepted corporate operator in the existing verified corporate home tenant; supported selected-subscription authority verified independently. No Global Administrator, Azure root, Production or additional ARM grant. |
| Exact source record | Proposed new administrative record alias `SOC2-TENANT-CREATION-AUTHORITY-CHANGE-2026-10-14-01`, in the existing owner-only local scaffold if its exact prospective path/ownership/access are verified and separately approved. Do not modify or expand the old root-change record's scope. |
| Copies and recovery destinations | Propose one non-overwriting manual retained copy and one recovery verification copy of that exact new record in the verified existing encrypted recovery boundary. Bind exact prospective paths and inventory collisions privately before approval; keep original and all versions. No automatic-backup population expansion or arbitrary destination. |
| Sensitivity and permitted contents | Restricted administrative identity/scope/assignment/audit metadata and management action evidence only; no credential values, dump contents, customer data, CUI or incident artifacts. Existing scaffold/recovery behavior is a bounded precedent, not an accepted enterprise evidence store. |
| Current state and ownership | FR-01 owns the current administrative task. Verify all prospective sources/destinations do not already conflict, are not symlinks/special files, and preserve current owner-only/encryption protections. Stop on collision, unclear custody/ownership or unavailable recovery; do not overwrite, relocate, change access or delete a source. |
| Retention | Proposed temporary administrative custody: retain source, each observed version, manual copy and recovery verification until a separately authorized management disposition/approved long-term migration. No source/copy deletion, pruning, automatic expiry or assumption that the seven-year proposal is approved. |
| Exact role action | If fresh specific approval exists, create only one direct Tenant Creator assignment for the privately bound existing operator/home tenant. Record the newly created assignment's exact identity; removal permission is limited to that new assignment, not other pre-existing assignments. |
| Timing and revocation | Proposed single attended window of at most 30 minutes; revoke immediately after the authorized verification/creation operation, earlier on failure, or at the window end. The timer is a procedural target; do not claim provider-enforced expiry or PIM licensing. No idle grant while waiting for another decision. |
| Recovery / exception | Positively verify a current authorized role-removal route and responsible operator before grant. If removal cannot be executed or proved, stop other changes, retain the failure and escalate through incident governance; no broader emergency grant or external communication from this packet. |
| Authorization state | Generic gate-resolution, earlier bounded tenant setup, accepted names and the prior root-record exception do not supply this new custody/grant/removal authorization. Exact actor/scope/action, source/copy/recovery destination, retention, revocation owner and window must receive actual approval. Final Create and any automatic governed effects retain their own concrete approval. |

**Execution sequence after readiness and the exact approval:**

1. Privately verify actor/home tenant, active administrative authority, required
   role state, selected-subscription rights and Gate 2's exact effect manifest.
   Match the already accepted tenant values, United States and bootstrap pair.
2. Reconcile the source/copy/recovery target inventory and prove the proposed
   one-record custody boundary through authorized metadata and bounded save/read/
   manual-copy/recovery checks. Retain all source/versions; no raw incident data.
   Custody success is operationally verified, not independently accepted.
3. Record actual approval, exact grant/removal scope, actual start/window end,
   operator/recovery ownership and before-state in that authorized record. If
   actual approval/custody/recovery is missing, do not grant.
4. Grant only the bound new Tenant Creator assignment if necessary and approved;
   verify role identity/scope and effective permitted metadata. Do not substitute
   a broader permission or treat membership as proof final Create will succeed.
5. Perform only the separately authorized bounded operation. If final Create
   lacks its exact approval or Gate 2 remains unresolved, do not leave the role
   active while waiting: remove only the created assignment under the same
   approved revocation scope and retain that result.
6. Verify assignment removal through exact assignment/role inventory and available
   audit events, refresh access where appropriate, and retain continued expected
   baseline roles. A denied request alone is not proof the assignment was removed.
7. Save observed successes/failures and retained administrative versions; complete
   the same authorized manual recovery verification without broadening backups.
   No future role, grant or source deletion is authorized by this operation.

### Gate 2 — Effective Governance Template And Automatic Effects

Reconcile the actual template selected by this exact creation route. The October
13 Review label was blank and the version-1 default display showed zero roles/
apps with a feature warning. These observations do not prove no relationship,
application or privilege effects. The provider procedure describes a billing
asset and governed cross-tenant/delegated-administration/application effects;
quantify actual selection and actual enforced defaults before final Create.

| Effect to bind in the approved private manifest | Required verified fact / stop condition |
| --- | --- |
| Selected template | Exact active template identity/version and how the blank Review label resolves; distinguish configured role/app lists from inherited mandatory behavior. Stop if the effective template cannot be established. |
| Governance relationship | Exact home/target directory relationship, who can govern it and applicable lifecycle transitions. No claim that selected empty lists disable default relationships. |
| Privileges and applications | Exact operator/default cross-tenant administrative access, delegated rights, service-principal/app consent and trust/authentication effects actually produced by this route; disclose incomplete readings and licensing limitations. |
| Billing and resources | Exact automatically created asset, selected bootstrap linkage, offer/terms, commercial effect and costs against existing ceilings. Free labels and zero estimates are not an unlimited spend authorization or target evidence-subscription proof. |
| Recovery / lifecycle | Identity recovery and governed-relationship removal/offboarding limits, dependency impacts and retained administrative evidence. Any future revoke/delete/directory transfer is separately scoped; it is not rollback authority supplied by copying a record. |
| Creation permission | Present this known effect set with the accepted exact target, creator/custodian, supported roles, provider review and costs for actual final-Create approval. Unknown privilege/effect scope remains a stop, not automatic acceptance. |

Current packet prepares a scoped decision. It does not grant a role, create a
protected record with restricted identifiers, configure the template, submit
Create or accept the protected workspace. New gate observations and exact
remaining decisions are recorded below after grunt's current reads. Existing
No-CUI/classification/professional review/incident closure boundaries are unchanged.

### Prepared Approval Targets And Narrow Readiness — 2026-10-14
Protected evidence reference; individual observations and source details are retained in the protected original. IDs: SOC2-TENANT-CREATION-AUTHORITY-CHANGE-2026-10-14-01.

Current selected-subscription View my access returned one inherited Owner with
no condition/group and zero displayed denies; no additional ARM grant is needed
for the observed selected target. PIM's unavailable/full-feature warnings do not
prove eligibility or expiry. The private provider member selector prepares exactly
one existing corporate operator; approval applies only to that positively rebound
operator/home directory and the exact newly created assignment/removal. Technical
Tenant Creator scope is the home directory, broader than use for one intended
tenant; the intended-target limitation and 30-minute attended timer are procedural.

An additional non-sensitive private approval draft was created during preparation
before the coordinator requested canonical-only presentation. It contains prospective
local paths and aliases only, no raw provider identifiers or actual grant/audit
record. It is preserved under the file-preservation rule; the current canonical
packet remains this workspace decision. No actual proposed grant record, manual
copy/recovery directory, role assignment or final Create was performed. New custody
and action approval remain pending. Root review is pending; no signature or
independent/protected-workspace acceptance is inferred.

### Gate 2 Latest Verified Outcome — 2026-10-14

Root verified current Microsoft primary guidance and found a material discrepancy:
the tenant-creation/deployment procedure says an existing default template creates
a governance relationship automatically, while the automatic-relationship guidance
says an empty default role/permission policy does not establish one. The setup
Verify procedure describes relationship objects in both tenants when a relationship
exists. Therefore the observed empty version-1 template does not establish that
this exact creation will produce a relationship or that it will produce none.
This discrepancy is retained; no source is silently preferred as runtime proof.

The same guidance distinguishes partner-specific cross-tenant settings and role
assignments conditional on delegated administration, and service principals/
permissions conditional on multitenant application management. Observed default
v1 has zero configured role assignments and zero applications. Additional
**template-configured** delegated roles, trust settings and managed-application
permissions are not expected; that is a configuration/source inference to verify
after any approved Create, not a claim that a new tenant has no built-in apps or
all possible privileges are absent.

The bounded proposed creation manifest is: the selected new tenant, creator Global
Administrator, Free tenant-creation billing asset, and **either no governance
relationship or matching governance relationship objects in both directories**.
No template edit is needed for the observed empty role/app configuration. Recheck
exact effective version-1 binding at final Review, particularly the earlier blank
label. Actual final-Create approval remains absent and must explicitly cover
these known possible relationship outcomes and permitted post-Create metadata
verification; final Create is not approved by documenting the alternatives.

After separately authorized Create, verify actual relationship population in both
directories, creator role, billing linkage and presence/absence of each conditional
template effect. Preserve any unexpected privilege/application/trust/billing effect
or inconsistent relationship population and stop further work. Do not silently
edit the template, delete objects, accept an unexpected effect or label conflicting
documentation as a verified deployment result.

**Gate 1 staged authorization is grant, verify and revoke only.** Create is not
part of that scope. Verify the necessary direct assignment and supported access,
then promptly revoke only the assignment created under the approved window and
prove removal. Do not leave it idle while awaiting final-Create approval. Any
later creation window and necessary role action require their exact approved
scope; the current staged verification does not pre-authorize them.

## Actual Staged Custody And Role Approval — 2026-10-14

`S39.2-MGMT-TENANTCREATOR-20261014-01` / `S39.4-E06`: FR-01 explicitly approved
the exact staged scope presented after root review. The root coordinator conveyed
the actual execution-chat approval; no exact response time or signature beyond
that response is invented. This supersedes pending-approval statements for this
specific stage only, not final creation or enterprise evidence acceptance.

Approved source is the proposed one administrative record identified by
`SOC2-TENANT-CREATION-AUTHORITY-CHANGE-2026-10-14-01`; approved manual custody is
its exact previously reviewed retained/recovered directories under the dedicated
manual parent. Exact paths remain in the approved execution/private-record context.
Preserve source and all versions/copies; no automatic-backup changes or deletion.
Permitted contents are administrative identity/scope/role/assignment/audit and
approval/revocation metadata, not incident content, dumps, secret values, customer
data or CUI. The new exception is limited to that record/population and does not
extend the old root record or authorize general protected incident evidence.

Approved external action is Tenant Creator for exactly the single prepared
corporate administrator/home directory: **grant, verify and revoke only the new
assignment**, within one attended maximum 30-minute window, preserving existing
Global Administrator. No selected-subscription or root grant; no extra account.
The role's technical directory scope remains broader than one tenant and the
window is manual/procedural, not provider-enforced expiry. Prompt revocation and
positive removal verification take priority over documentary work; no idle
privilege awaits final Create. Grunt executes the approved custody/preflight and
bounded role action and preserves private results; planner records only sanitized
approval/outcomes. Actual execution/verification results remain pending until
observed. On a failure, preserve the outcome and prioritize the approved removal
and incident-escalation path; no broader permission or deletion is inferred.

**Final Create is expressly excluded from this approval.** New tenant creation,
template edits, governed relationship/app effects, purchases, protected incident
migration/inspection and Story 39.4 acceptance remain separate. Template guidance
discrepancy and expected none/paired relationship possibilities remain disclosed;
no runtime effect is resolved before an authorized creation. Do not ask FR-01 to
approve this unchanged staged scope again.

## Actual Staged Execution And Remaining Creation Decision — 2026-10-14

`S39.4-E07` / protected record `S39.2-TENANT-CREATOR-CHANGE-2026-10-14-01`:
grunt completed the exact approved custody and role stage. **No temporary
Tenant Creator assignment remains active for the approved target.**

| Observed check | Actual outcome / limitation |
| --- | --- |
| New administrative custody | Source versions v01 through v05 preserved; five manual retained and five recovered versions match their corresponding captures, and latest source matches v05. Files0600, new directories0700, current owner and no observed symlinks. Same-mounted-volume recovery only; no independent/enterprise workspace acceptance. |
| Role before / positive / after | Exact approved target active count 0 → 1 → 0; newly created assignment identity captured privately. After removal, independent Graph exact-target count0 and exact created-assignment lookup404/not found; portal count0. Existing actor Global Administrator count1 remains. |
| Actual provider events | Add success 2026-10-14T17:09:16.6098505Z; Remove success 2026-10-14T17:11:37.86744Z. Two matching exact-target audit references retained privately. Logged add/remove interval approximately141.26seconds; independent absence verified17:12:16.125252Z. |
| Attended window | Start17:09:03.968923Z, approved maximum-window deadline17:39:03.968923Z; positive removal independently established approximately193seconds after start, before the deadline. Cross-execution monotonic delta disagreed with UTC and is invalid as elapsed proof; no reliable watchdog or provider-autoexpiry claim. Use observed UTC/provider event interval for this result. |
| Scope and source preservation | Only the approved new assignment granted/verified/removed; no other role change or Final Create performed. Source/all versions/copies preserved. No automatic-backup changes, template edit, purchase, source deletion, secret/customer/CUI/incident content or dump inspection. |

The directory-wide privilege risk was reduced for this operation by the exact
actor/action restriction, no other use and verified prompt removal; the role
itself did not become single-tenant scoped. The manual-window risk was controlled
by observed completion inside the approved UTC interval; the faulty monotonic
measurement is retained as a limitation, not a passed timer. Before any future
window, persist one UTC start/deadline, check fresh UTC at each operation and
prioritize immediate removal on failure/deadline. Do not reuse cross-execution
monotonic deltas or hold a role while waiting for approval. Template/documentation
uncertainty is bounded by the explicit none/paired-relationship proposal and
post-Create verification; it cannot be resolved as a runtime result before Create.

### Concrete Final-Create Decision Ready For Review

**Not approved or executed by this stage.** Reuse the accepted exact display name,
immutable prefix, United States and verified existing non-Production bootstrap
pair; no repeated identity/setup decisions. Actual final-Create permission must
cover only one empty selected governed Workforce tenant and the following scope:

- One fresh attended necessary-role window for the same corporate operator/home
  tenant: Tenant Creator grant only if needed, one approved Create, metadata
  verification, then revoke only that window's newly created assignment while
  preserving GA and other baseline roles. Current staged grant/removal permission
  does not authorize a second window. No idle privilege awaiting further approval.
- Recheck effective default v1 binding and zero configured roles/apps at final
  Review. Known possible effects are new tenant, creator Global Administrator,
  Free tenant-creation billing asset and either no governance relationship or
  matching relationship objects in both directories. No additional template-
  configured delegation/trust/managed apps is expected; actual state verified
  afterward. Do not interpret normal tenant built-in apps as template additions.
- Verify exact created tenant/country, creator rights, bootstrap/billing linkage,
  both relationship populations and conditional template role/app/trust effects.
  Preserve and stop on an unexpected permission, inconsistent relationship or
  charge; no automatic deletion, template change, directory transfer or broader
  corrective grant. Free label is not unlimited spending authorization; existing
  setup cost ceilings remain and no purchase/new paid license is included.
- Explicitly extend the same one administrative record/manual version population
  to the new window and resulting creation/effect/revocation metadata if this
  future action is approved; preserve all source/versions, no automatic backups
  or other evidence scope. Exact action-time target/terms/cost review still applies.

This is the remaining reviewable action, not a new approval. An empty tenant is
not an evidence store: access/retention/recovery configuration, synthetic acceptance,
independent review and later protected incident classification/containment remain
separate. Story 39.4 and SOC2-GAP-001 are not closed by the completed admin stage.

## Actual Bounded Tenant-Creation Approval — 2026-10-14

`S39.2-MGMT-CREATE-20261014-01` / `S39.4-E08`: FR-01 explicitly approved the
exact next scope presented after review: **one empty selected governed Workforce
tenant**, using the accepted display name/immutable domain/United States and
approved existing non-Production bootstrap pair. Root conveyed actual execution-
chat approval; no response timestamp/signature beyond that consent is inferred.
This new approval supersedes final-Create-pending statements for this exact
operation, while the earlier grant-only stage remains closed with its own evidence.

Authorized fresh window is the same bound corporate operator/home directory,
Tenant Creator grant only if necessary, one Create, scoped metadata verification
and revocation of only that window's new assignment, preserving existing roles,
within one attended maximum30-minute UTC window. Recheck target, current billing/
terms/cost and effective empty defaultv1 before action; no new paid purchase,
template edit, additional ARM/root/user grant or duplicate tenant. Known approved
possible governance outcome is none or matching relationship objects in both
directories; creatorGA and Free tenant-creation billing asset are included in the
reviewed manifest. Unexpected privilege/application/trust/charge or inconsistent
relationship population triggers preservation/stop and prompt role removal,
not self-authorized correction/deletion or another Create.

The SAME approved administrative record/manual retained/recovered population
is extended to this new window and resulting creation/effect/revocation metadata;
preserve all source versions and no automatic-backup changes. Existing aggregate
$25 upfront inclusive of taxes/mandatory fees and $55/month ceilings remain; this
approval supplies no additional paid purchase. Grunt executes the actual operation
with removal prioritized over docs and verifies UTC timing, positive role/creation
results and exact new-assignment absence. Actual operation/outcomes remain pending
until observed. Do not repeat this unchanged exact authorization question.

This approval does not establish enterprise evidence storage, accept retention/
access/recovery controls, permit incident content/dump inspection or close Story
39.4. Professional/independent acceptance, unknown artifact provenance and
restricted incident classification/containment remain separate boundaries.

## Observed One-Tenant Creation And Verification Handoff — 2026-10-14

`S39.4-E09` / protected record `S39.2-TENANT-CREATOR-CHANGE-2026-10-14-01` v10:
**one Create was submitted and the provider reported success**. Separate Graph
tenant-information lookup matches the accepted exact display name and initial
domain; the home Manage Tenants row identifies the target as Workforce with those
same values. Exact new identity is recorded privately. The one-tenant Create scope
is consumed; this outcome permits no duplicate/retry Create or another role grant.

| Verification | Actual observed result / unavailable scope |
| --- | --- |
| Fresh role and cleanup | Provider Add success17:28:36.963Z and Remove success17:31:10.668Z, logged active interval approximately153.71seconds. Separate Graph removal verification17:31:51.736Z: exact-target0, exact fresh-assignment404, homeGA1 retained; portal empty. No fresh home Tenant Creator assignment remains active. |
| UTC window | Start17:28:30.398Z/deadline17:58:30.398Z; removal verification approximately201.34seconds after start, before maximum30minutes. This is observed manual UTC-window compliance, not automatic expiry. Prior cross-execution monotonic limitation retained; not used to claim timer reliability. |
| Approved administrative custody | Source currentv10 and all ten retained/recovered version pairs match; files0600, newdirs0700, currentowner/no observed symlinks. Original versions preserved, same-volume recovery only, no automatic-backup population change. |
| Template | Current defaultv1 configured roles0/apps0, unchanged. No template edit. Relationship/effect runtime is not inferred from configuration alone. |
| Target authenticated metadata | Protected evidence reference; individual observations and source details are retained in the protected original. Recorded status: pending. |
| Home governance | Related Tenants view unavailable for lack of required license, not zero. Home Governed Tenants Active table loaded No governed tenants found; home point-in-time observation only, not complete counterpart/governance proof. |
| Billing asset / linkage | Approved bootstrap pair selected and Free asset documented as expected; actual resulting asset/linkage unverified. Resource-group display first showed0 then remained loading; separate ARM API read denied. These results cannot prove asset absence, permanent zero cost or successful linkage. No extra paid transaction/terms acceptance was encountered. |
| Scope preserved | No second tenant, other role grant, new paid purchase, template/Production/automation change, deletion, credential value, customer/CUI/incident content or dump inspection. Provider success and role removal are specific to this operation, not independent professional assurance. |

Protected evidence reference; individual observations and source details are retained in the protected original. IDs: FR-01.

Protected evidence reference; individual observations and source details are retained in the protected original. IDs: SOC2-GAP-001.

### Local Authentication Failure And Reopen Handoff — 2026-10-14

FR-01 reported **BadRequest** in the new tenant's local authentication/security
registration flow at `2026-10-14T19:01:23.6448592Z` (15:01 EDT). This is an actual
reported verification failure; its cause and completed target authentication are
unverified. Correlation/diagnostic identifiers are excluded from this sanitized
record. Existing one-tenant creation and successful temporary-role removal remain
verified; the failure does not authorize another Create, grant, reset or method
change. Grunt refreshes/reopens only the existing target's login/security flow;
no agent Next/enrollment or credential/QR/recovery-secret handling. Authenticated
target metadata and enterprise/incident acceptance remain pending. Fresh-flow
outcome is recorded only once observed; no success is inferred from reopening.

Protected evidence reference; individual observations and source details are retained in the protected original. Recorded status: pending.


Protected evidence reference; individual observations and source details are retained in the protected original. IDs: FR-01.

The existing approved private administrative record
`S39.2-TENANT-CREATOR-CHANGE-2026-10-14-01` v13 preserves both user-reported
attempt references and timestamps, known administrative references, query limits
and diagnostic questions for an authorized target administrator/Microsoft
account-and-tenant support handoff. All 13 retained/recovered pairs match the
source/version records; previous versions remain preserved. Only sanitized
references appear here. No external message was sent, no sensitive enrollment
material was captured, and no reset, grant, consent or security change was made.
The next step is scoped provider/target-administrator diagnosis using that
private handoff; repeated reopening alone has not established a fix. Created
tenant and temporary-role removal outcomes remain verified; authentication,
remaining target checks and Story39.4 acceptance remain pending.

Root diagnostic review completed 2026-10-14 after both delegated completions.
Private 13-version retained/recovered equality and restrictive file modes were
independently checked. The cause remains unknown; home-only empty results and
denied policy/API reads do not resolve target registration. Existing-target
administrator or Microsoft support diagnosis is the next scoped action; no
external submission, method reset, new role or creation is authorized by review.

### Local Comparative Authentication Checklist — 2026-10-14

FR-01 requests local troubleshooting before any Microsoft support submission.
No external submission is authorized by this checkpoint.

| Local step | Observed outcome and limitation |
| --- | --- |
| Prior same-session portal attempts | Two user-reported BadRequest failures remain recorded above. Reopening reached the security-registration heading without proving authentication; repeating that identical attempt has not established a fix. |
| Isolated target-specific sign-in | Protected evidence reference; individual observations and source details are retained in the protected original. |
| Sensitive local test | Protected evidence reference; individual observations and source details are retained in the protected original. IDs: FR-01. |
| Alternate registration entry/method | The delegated diagnostic stream disconnected before a verified alternate-route result was received. Direct Security info route/account context and available enabled methods remain unverified. This tooling interruption is separate from the user-reported BadRequest and proves no authentication cause; no browser operation was repeated to obtain another final report. |

Protected evidence reference; individual observations and source details are retained in the protected original.

### Read-Only Post-Authentication Checkpoint — 2026-10-14

After FR-01 reported local completion, the existing Incognito session displayed
the accepted target tenant overview and the current account as Global
Administrator. Target portal access, accepted identity/domain and Workforce
type are now verified; authentication-method enrollment and the cause of the
prior BadRequest failures remain unverified. The target overview displays
Microsoft Entra ID Free, Users1, Groups0, Applications0 and Devices0. The separate
all-Enterprise-Applications view contains 19 Microsoft/platform-labelled entries;
the default enterprise filter shows0. These are distinct view populations,
not proof that service principals or all applications are absent.

Home Governed Tenants and target Governing Tenants all-status views displayed0
rows with P1/P2 licensing warnings; each is a point-in-time view result. Target
Related Tenants governance remained unavailable for lack of license. Target
organization-specific cross-tenant settings displayed0 configured entries;
default trust was disabled, B2B direct connect blocked and B2B collaboration
allowed in the settings view. Delegated administrator partners displayed0.
These metadata observations do not prove complete historical relationship
absence, independently validated effective-policy enforcement or license-complete
governance coverage. Country remained loading/blank; generic US-datacenter
copy is not independently verified tenant-country evidence. The approved home
bootstrap resource-group overview loaded Showing0of0 with TypeAll/LocationAll
and No deployments; actual creation billing asset/linkage remains unverified,
and this view cannot prove asset absence or zero cost. A fresh home Tenant
Creator absence refresh was not performed. The earlier verified exact temporary-
role removal remains valid for its recorded observation; no new grant or Create
occurred. The approved private administrative record is now v14: source and
new retained/recovered pair match, all prior13 pairs remain preserved and match,
files0600 and retained/recovered directories0700 were verified.

The next setup gate remains the separately approved protected evidence
subscription/store and its scoped access, encryption, version/integrity,
retention, recovery and audit configuration, followed by synthetic save/read/
deny/recover verification. Neither tenant creation nor this portal access
accepts the store or authorizes incident-content migration/inspection. Missing
metadata, qualified/independent review, unknown dump provenance, containment
validation and Story39.4/SOC2-GAP-001 disposition remain open. No new
subscription/store, paid purchase, role, policy, template or security change
was made by this read-only checkpoint.

Protected evidence reference; individual observations and source details are retained in the protected original.

## Remaining Empty-Workspace Action Manifest — 2026-10-14

**Planned / incomplete; preparation authorized, no provider transaction by this
manifest.** Reuse `SOC2-39.2-T03`, the existing two-subscription design, selected
created/authenticated Workforce target and FR-01 management/custody appointment.
The accepted one-tenant Create scope is consumed. Current ceilings are $25
aggregate upfront inclusive of taxes and mandatory fees and $55 total monthly
inclusive recurring across both subscriptions and supporting services. The
historical $0 instructions do not supersede the later amendment. A cost ceiling
is not a quote, hard consumption cap, purchase or binding-terms approval.

1. **Target subscription preparation — grunt lookup, FR-01 accountable.**
   Verify the existing corporate MCA account/profile/invoice section and actual
   supported new-subscription/request route into the selected target directory;
   prepare one dedicated non-Production evidence subscription. Record the exact
   directory, owner, name, agreement/offer, effective management-group parent,
   inherited policy/initiative/exemption/remediation effects and any default
   deployments in the approved private provider/change context. Target country
   is now actually read as United States; tenant-creation billing-asset linkage
   remains unresolved and cannot be inferred from an empty bootstrap resource
   group.
   No Production transfer/attachment, new root elevation or duplicate tenant.
2. **Action-time transaction boundary — FR-01 decision.** Read-only preparation
   reuses existing authority. Before the exact final Create, reconcile actual
   due-now plus mandatory fees/tax and the complete recurring model against both
   aggregate ceilings, confirm target/placement and obtain the existing runbook's
   exact confirmation. Separately identify any new purchase, binding terms or
   security-sensitive grant. The currently inspected cross-directory route
   creates a subscription request and sends a provider-generated ownership email;
   its target owner acceptance has a seven-day window in current Microsoft
   documentation. Exact action review must cover request/email/acceptance,
   target owner identity and actual resulting permissions, not assume an immediate
   silent target subscription. No external submission/notification authority is
   inferred from the prior design decision. Independent reviewer unavailability does not stop
   otherwise authorized empty preparation; it remains an acceptance gate.
3. **Empty store manifest — selected design, unresolved values explicit.**
   Candidate is one dedicated resource group and Standard general-purpose v2
   flat-namespace Blob account, Hot ZRS, East US 2 preferred/East US fallback,
   private container, Entra authorization and platform encryption. Proposed
   reviewable initial values: Standard_ZRS/Hot, flat namespace, HTTPS required,
   minimum TLS1.2, anonymous Blob access disabled and public network disabled
   while the exact private administration path is costed. This seals the empty
   store and postpones data use; it does not establish usable access or recovery.
   Propose versioning plus seven-day Blob/container soft-delete and change-feed
   windows for synthetic setup only, no point-in-time restore, locked immutability
   or automatic lifecycle deletion. Propose a protected 30-day synthetic-log
   destination as the initial comparison, not approved long-term evidence
   retention. Validate actual provider compatibility/SKU availability and total
   quote before selection. Exact resource names, private DNS/access/recovery
   path, named data principals/scopes, diagnostic categories/destination and
   resource-lock treatment remain action-time selections. Shared Key
   disablement follows actual approved-client compatibility verification. These
   values are proposals, not saved configuration, retention approval or universal
   SOC 2 requirements.
4. **Custody boundary — FR-01 custodian.** The current administrative record's
   approved population covers creation/effect diagnostics only. It is not blanket
   custody for subscription/storage evidence or incident content. Before capturing
   a new exact protected configuration/test population, identify its approved
   destination, retention, access and recoverable capture scope. Git carries only
   sanitized aliases/results; no private provider identifiers or sensitive data.
5. **Synthetic technical tests — planned, none passed by this manifest.** Once
   exact controls and test authority are selected, verify authorized save and
   intended-version read; an unprivileged existing test principal and anonymous
   link denied; reviewer write denied; version integrity and approved recovery;
   corresponding read/write/delete audit events after ingestion delay; revoked
   access denied; and administrator/recovery continuity. Preserve timestamps,
   expected/actual results, complete role/scope basis, sampling/population and
   limitations. Test-object deletion, access revocation and any grant require
   exact existing or fresh target-specific authority under `AGENTS.md`; do not
   execute a destructive recovery test from a generic checklist.
6. **Governance acceptance — unresolved.** FR-01 management self-review is
   distinct from independent Security review and triggered qualified Legal/CPA
   input. Design remains partial, implementation is limited to the identity
   foundation, operating evidence for the protected store is unavailable.
   Technical success alone does not authorize restricted evidence migration,
   dump inspection, severity/notification judgment, containment or Story39.4
   acceptance. Unknown dump provenance remains a P0 blocker in T02/T03; reuse the
   existing answer rather than request it again.

Owners, criterion/control mappings, Type I/II impact, uncommitted conditional
2026-10-16 implementation estimate and independent-review vacancies remain in
`SOC2-39.2-T03` and the Story39.4 task register. Next unblocked action is the actual
read-only target subscription route/terms/placement capability check and exact
form preparation; any unavailable source remains named with an owner and next
step, never treated as absence or approval. Leave new empty resources unused if
verification fails; deletion is not an automatic rollback.


Actual read-only continuation: target Entra Properties now displays Tenant
Country/Region United States. The corporate MCA Add-subscription form permits
selection of the existing evidence target directory; the prepared, unsubmitted
Review+Create passes validation with that directory, existing corporate owner
and Microsoft Azure Plan. This is a new-subscription route, not a Production
subscription transfer. Cross-directory management-group selection is disabled
with an explanatory notice; Review displays None, which does not prove absent
inherited target governance. The form retains a generic default name/no budget;
no exact evidence-subscription name has been accepted. No checkout/due-now
quote or new terms were shown, and final Create was not selected. Final route
owner/effective placement, complete charges/terms, exact target naming and store
configuration remain action-time prerequisites; validation alone is not
creation authority or cost proof.

Current Microsoft primary procedure distinguishes this cross-directory route
as subscription-request creation followed by provider-generated ownership email
and target-owner acceptance within seven days. The final action review therefore
includes those effects and actual target-default hierarchy verification; the
selected form does not establish immediate provision or email authorization.
The source is Microsoft's create-subscription procedure, updated 2026-02-11,
as checked by the delegated lookup. No request/email/acceptance was executed.

Actual target-hierarchy check: target directory context was confirmed, but the
Resource Manager default management-group settings read denied
`Microsoft.Management/managementGroups/settings/read`; the default-management-
group field displayed Unauthorized. No elevation or grant occurred. This
specifically leaves target default placement unavailable before the request;
it does not prove absent inherited policies or automatic deployments. An
already authorized target hierarchy administrator can supply the narrow read,
or FR-01 must decide a concrete scoped-access change. Earlier home hierarchy
reads cannot substitute for this target result. Extension of the private
creation/effect record to a new subscription/store manifest population remains
a custody decision, not an implicit result of local document preparation.

The exact prospective evidence-subscription name is now prepared in the private
unsubmitted provider form for FR-01 review; its sanitized alias is the dedicated
target-evidence subscription. Review displays that proposal, the selected target
directory, corporate owner/MCA Azure Plan, management group None and no budget.
This resolves blank naming preparation only; acceptance of the name and exact
request/email/ownership effects remains the final action decision. No Create.
The existing private administrative record v15 adds only the actual country
observation within approved post-Create diagnostics: source/new retained/recovered
capture match, all fourteen prior pairs preserved/matching, files0600 and custody
directories0700. No new MCA/store private-identifier population was captured.

### Target-Hierarchy Access Proposal — Not Authorized — 2026-10-14

Bounded read-only preflight now verifies the target operator profile as enabled
Member and the current Global Administrator assignment through the target
all-type assignment inventory (one operator User/Directory row); this is current
assignment evidence, not PIM eligibility. Azure Access management displays No,
editable but untouched, with Save disabled. One profile-assigned-role route
failed; the alternate role inventory supplied the observation. Target global-root
User Access Administrator/elevated-user baseline and a sufficient existing
reader were not established; unavailable inventory does not prove no reader.
No record-scope extension, toggle, grant or other provider write occurred.

**Preferred next action:** an already authorized target hierarchy administrator
performs only the target default/settings, root/inherited assignment and
policy-effect reads required for placement/cost. Do not elevate if that authority
is available. The historical home read and removal remain separate.

**Conditional alternative:** the existing hierarchy runbook can prepare one
attended target-only Access management No-to-Yes-to-No operation, acquiring
User Access Administrator at Azure global root `/`, for no more than30 minutes.
That broad authorization capability is a material risk despite the narrow read
purpose; no added Reader/custom/other assignment is included. The operation is
not ready for approval/execution while the exact target root-assignment baseline,
protected target-root change-record scope, named revocation owner, reliable
assignment/audit removal proof and recovery route remain unverified. Proposed
reads are only global-root elevation/role metadata and target-root hierarchy,
default placement, RBAC/deny assignments, inherited policies/initiatives/
exemptions, definition effects and remediation/deployment metadata relevant to
a new subscription; no evaluation/remediation or resource/content inspection.

Before any conditional elevation, the exact target, actor, `/` scope, reads,
start/deadline, explicit removal and validation method, fallback/recovery owner
and approved same-record/manual-retention extension must be reviewable and
separately authorized. No new private file is implied. After reads, set No/Save,
prove exact temporary assignment removal through assignment and audit evidence,
then refresh independently; denied access alone is insufficient removal proof.
Thirty minutes is procedural, not automatic expiry. Failure to remove or verify
removal triggers the existing recovery/incident gate and stops other changes.
Current v15 custody and existing budget/empty-setup authorizations are preserved;
they neither authorize this new target privilege nor solve these access gates.

Root continuation review completed 2026-10-14 after both delegated reports.
Actual country is United States; fifteen record-copy pairs/source match were
independently checked. Hold final subscription request/Create until target
placement and inherited effects are inspected. Target enabled Member/current
Global Administrator and elevation setting No are verified. Microsoft documents
the same-operator No/removal and exact-assignment API routes; this is documentary
route verification, not a performed target-root removal test. Existing record
recovery passed, but its scope must be explicitly extended before target-root
privileged captures. A proposed new-target-only attended UAA elevation/read/
removal window requires fresh approval, preserved GA, exact new-assignment
identification, UTC deadline and prompt removal/absence proof. Pre-elevation root
inventory is unavailable; no zero-assignment claim is made. Preserve the limitation
and stop on unexpected pre-existing authority or unverifiable removal. No root
elevation, new assignment, subscription request, provider email, storage Create
or incident acceptance was executed by this review.

### Actual Target-Root Inspection Approval — 2026-10-14

FR-01 explicitly approves the exact selected new target only: the verified
Member/current active Global Administrator operator may enable Access management
for Azure resources, acquiring temporary User Access Administrator at Azure
global root `/`, for one attended window of no more than30 minutes. Authorized
reads are target hierarchy/default placement, root role and deny metadata and
policy-effect metadata only. No additional role assignment, custom role or
policy write is permitted. Prompt removal of only the new elevation and positive
assignment/audit proof take priority over documentation; preserve Global
Administrator and pre-existing authority. Pre-elevation root inventory was
unavailable, not baseline0. Stop on unexpected authority or unverifiable removal.

The exact existing administrative source and approved manual retained/recovered
population are explicitly extended to these target-root inspection captures.
Retain every prior/new version, verify capture integrity/protection and change
no automatic-backup population or automation. Sanitized logical reference stays
`S39.2-TENANT-CREATOR-CHANGE-2026-10-14-01`; exact source/destinations and provider
identifiers remain privately bound to this actual approval. No new file or
arbitrary setup/incident population is implied.

This is actual user authorization conveyed by root, not a fabricated signature
or independent review. It supersedes the approval/scope-pending portion of the
conditional target-access proposal only for this exact inspection operation.
Subscription Create/request/email/acceptance, storage, Production and all other
next actions remain excluded. Actual read/removal outcomes are pending until
observed; no execution or containment success follows from this approval alone.


Actual in-flight exception: window start20:20:11.104492Z and deadline
20:50:11.104492Z on 2026-10-14. Yes/Save displayed Successfully updated
properties; subsequent No/Save failed with Insufficient privileges and Save
remained enabled. Temporary root removal is unproven; No display is not proof
of saved disablement or assignment absence. No hierarchy reads or additional
grant were performed before this failure. Same-operator existing-portal removal
and positive verification are the immediate priority; stop other changes and
retain the failure even if subsequent removal succeeds. No automatic expiry
or completed operation is claimed.


Subsequent positive removal checkpoint: correct target CLI context and operator
Member identity were verified. Root filtered inventory returned one exact fresh
User Access Administrator assignment whose createdAt was20:20:57.4235457Z,
within the approved window; definition and identity were privately captured
before deletion. Exact-assignment DELETE returned200, its subsequent GET404,
and the root filtered GET200 returned exact-new-assignment count0 at
20:27:06.301109Z. This positive absence is approximately415.20 seconds after
window start and before20:50:11.104492Z deadline; the value is an observed manual
UTC interval, not automatic expiry. Two portal No/Save Insufficient privileges
failures remain preserved separately and are not the removal mechanism.
No other assignment was changed and no hierarchy reads occurred. Private v18
source/new retained-recovered pair match; previous versions remain retained.
Current portal No, preserved target GA and independent audit coverage are pending
final read-only checks; missing checks are not inferred from the delete response.
The specific new assignment is now positively absent, while intended inspection
is incomplete and no new elevation/next action is authorized.


Final read-only check: target portal reload displays Access management No with
Save disabled. Direct Graph Global Administrator inventory returned200/count1,
preserving the operator's target GA baseline. Exact Azure root Activity metadata
returned200 with Delete Started20:27:05.1250568Z and Succeeded
20:27:06.2812966Z events, privately preserved; no Add audit event is claimed.
The exact new assignment remains positively absent. The observed interval from
its createdAt to verified absence is approximately368.88 seconds; total attended
start-to-verification is approximately415.20 seconds, inside the30-minute limit.
These UTC observations prove manual removal for this operation, not expiry or
complete pre-existing root-authority absence.

One post-removal hierarchy read returned403. No intended elevated hierarchy/
policy inspection was executed, so target default placement and inherited
policy/cost prerequisites remain unresolved. Private v20 source and all twenty
retained/recovered pairs match; current-owner files0600/directories0700 verified,
all earlier versions preserved. No extra assignment, policy, Create/request/
email, storage, Production or automation change occurred. This window is consumed.
Next action is root review of the failed portal removal and tested exact-target
API removal/recovery evidence, then an existing authorized target reader or a
separately reviewed new inspection window; no automatic regrant/retry is implied.
Restricted evidence acceptance, incident investigation/containment and Story39.4
closure remain open. Failure history is retained despite successful removal.

Root failed-window review completed 2026-10-14 after both delegated completions.
Twenty retained/recovered captures/source equality and approved ownership/modes
were independently verified. Exact fresh root assignment DELETE success, GET404,
filtered GET200/absence, activity Succeeded, persisted No and preserved GA support
removal; they do not complete hierarchy inspection. API authentication should have
been ready before elevation. Any separately approved retry must preflight target
CLI identity/token, current own-root-assignment inventory, exact capture/deletion
route and recoverable record before grant; use the tested exact-assignment API
removal as primary, not the failed portal Save route. The consumed window supplies
no second grant; subscription/store/incident acceptance remain blocked.

### Actual One-Fresh-Retry Target Inspection Approval — 2026-10-14

FR-01 explicitly approves one fresh, single attended<=30minute target-only
inspection window by the same verified operator. Before grant, confirm correct-
target authenticated CLI, own root-assignment capture capability, the tested
exact-assignment API DELETE/removal verification route and approved protected
custody. If local CLI authentication is required, finish the local human step
before elevation; no new consent or credential handling is implied.

Acquire only the new temporary User Access Administrator at `/`; read target
hierarchy/default placement, RBAC/deny metadata and policy-assigned referenced
definition effects. Prompt API removal/proof of only that new assignment takes
priority, preserving target GA and all pre-existing authority. Do not rely on
the previously failed portal No/Save as the removal path. The exact existing
administrative record/manual copies already approved for target-root inspection
remain the capture destination; preserve all twenty prior versions and every
new version, create no new record and change no automatic-backup population.

This actual approval covers one fresh retry only and consumes that window once
operated. Previous portal failures, positive first-window removal and the missing
inspection remain historical evidence; they are not overwritten. No subscription
Create/request/email, store, policy write, Production or later action is approved.
Actual inspection/removal outcomes remain pending; approval alone proves neither.

### Actual Fresh-Retry Outcome — 2026-10-14

Before grant, correct target/operator Member and direct active GA count1 were
verified; own root User Access Administrator inventory returned200/count0 and
the previous exact assignment GET404. These are bounded own-assignment facts,
not proof that all root authority is absent. Private v21 preserved preflight.
The attended window began20:39:46.618197Z, deadline21:09:46.618197Z. Target
ElevateAccess POST returned200; inventory then identified one exact new own
User Access Administrator assignment createdAt20:41:08.9841709Z, privately
captured in v22 before any read/removal.

The first target management-group inventory returned403 and triggered the
approved stop. Default placement, root deny and assigned-policy/definition-
effect reads were not executed; no absent-policy, safe-placement or zero-cost
conclusion is supported. The helper did not retain this403 error body/code/
message, so its specific cause remains unknown; no additional query was made
to infer it. Prompt exact-assignment DELETE returned200 at20:41:11.534605Z,
subsequent exact GET404 and own root inventory GET200/count0. The observed
createdAt-to-delete-response interval is approximately2.55 seconds; attended
start-to-delete-response approximately84.92 seconds, before deadline. Positive
assignment absence, not elapsed time, establishes this specific removal;
no automatic expiry or broader authority absence is claimed.

Final full target portal reload displays Access management No/Save disabled and
current GA; independent direct Graph GA count1 remains. Root Activity query
returned200 but matched no exact retry-assignment events at that point in time,
so retry audit-success evidence is unverified, not absent forever. The first
window's two successful delete-event observations remain separate and cannot
prove this retry's audit outcome. Private v25 source and all twenty-five retained/
recovered pairs match/current-owner files0600/directories0700/no observed
symlinks; all previous versions retained. No extra role, policy, subscription,
request/email, storage, Production or automation change occurred.

This single retry window is consumed. Target hierarchy/placement/cost remains
blocked despite verified exact temporary-role removal; no repeated elevation,
new request or store provision is authorized by this outcome. Root reviews the
403 coverage limitation and pending retry audit trace before deciding the next
scoped diagnostic/authorized-reader path. Keep prior portal failures, first-
window non-inspection and this retry failure in history. Restricted workspace
acceptance, incident disposition and Story39.4/SOC2-GAP-001 closure remain open.

One subsequent current-unprivileged management-group read returned403/AuthorizationFailed for `Microsoft.Management/managementGroups/read` with target actor/directory matched and a credential-refresh hint; private v26/all26 retained-recovered captures match with prior versions preserved, but this baseline result establishes neither the earlier elevated denial cause nor an authorization-propagation explanation.

Root retry review completed 2026-10-14 after both delegated completions.
Twenty-six matching source/retained/recovered captures and restrictive modes were
independently checked; no bearer/JWT capture patterns were detected by the scoped
check. Exact fresh root assignment removal and preserved GA are verified; hierarchy
inspection and retry audit success remain incomplete. The current unelevated
AuthorizationFailed response confirms correct target/operator, not the original
denial cause. New assignment inventory is not effective-authorization readiness.
Primary Azure RBAC troubleshooting documentation describes caching of role changes;
propagation is a plausible explanation only, not a verified cause here.

A revised inspection proposal, NOT approved or executed, would preflight the
authenticated target API/custody and exact removal route; grant one fresh target-
only UAA assignment; retain structured denial details privately and poll only the
approved read at bounded intervals for at most ten minutes. No broader role,
policy change, logout/cache clearing or app consent is included. If access becomes
available, complete only the approved metadata inventory; otherwise stop. In all
cases promptly delete only the new assignment with positive absence proof inside
a thirty-minute attended UTC window. Sensitive reauthentication that cannot finish
promptly stops the operation and triggers removal, not idle privilege. The same
approved record/copy population is retained. Every prior window is consumed; a
new window requires fresh approval. Subscription/request-email/storage/Production
and incident acceptance remain excluded.

### Actual Revised Readiness-Window Approval — 2026-10-14

FR-01 explicitly approves ONE revised attended<=30minute inspection window for
the same selected target/operator, administrative record and manual retained/
recovered population. Correct-target API authentication, capture capability,
tested exact-new-assignment removal and protected custody are pre-grant gates.
Acquire only one fresh root User Access Administrator assignment; poll the
approved read readiness for up to10 minutes, preserving structured private
errors. If ready, read target hierarchy/default settings, root RBAC/deny and
assigned-policy referenced effects; otherwise stop. Prompt API DELETE of only
the new assignment and positive absence verification remain inside the30minute
UTC boundary. Preserve GA/pre-existing roles and all twenty-six prior versions.

No additional Reader/custom/other role, policy write, store, subscription Create/
request/email, Production, logout/cache clearing, consent or purchase is included.
Previous two windows remain consumed and their failures/removal evidence
preserved. This actual revised timing authorization does not prove that earlier
403 was caused by propagation; context-matched delayed readiness, if observed,
provides temporal evidence only. No fresh confirmation is required for this exact
approved window. Actual readiness, inspection, removal and custody outcomes are
pending; dump provenance and incident/CUI gates remain unchanged.

### Actual Revised-Window Outcome — 2026-10-14

Pre-grant API authentication/capture/removal/custody checks passed for the exact
target. Window start20:57:28.524139Z/deadline21:27:28.524139Z; fresh root UAA
createdAt20:59:28.502396Z, ElevateAccess POST200 at20:59:28.962384Z and exact
assignment captured privately before polling. Readiness cutoff21:09:29.169169Z.
First poll around20:59:29 returned403/AuthorizationFailed with matched actor/
target context and structured error retained; second poll21:00:00.384991Z
returned404/NotFound with provider text “Management Groups are not enabled in
this tenant.” No200 readiness occurred. Polling stopped; no management-group
initialization, default/settings/deny/policy-effect queries or extra grants.
This is an actual point-in-time provider state response, not future placement,
absent-policy, zero-cost or conclusive earlier403 propagation evidence.

Exact new-assignment DELETE returned200 at21:00:01.160941Z, exact GET404 and
own UAA inventory GET200/count0. Observed createdAt-to-delete-response interval
approximately32.66 seconds; start-to-delete-response approximately152.64 seconds,
inside30minutes. Full target portal reload shows Access management No/Save
disabled; direct post-removal Graph GA200/count1 preserved. One post-removal
root Activity query returned200/exact-assignment matches0 at that point in time;
audit success remains unverified, subject to ingestion/query coverage limits.
No provider Add or successful-delete audit event is invented. Private v33 source/
all thirty-three retained/recovered pairs match, current-owner files0600/
directories0700/no observed symlinks; all prior versions preserved.

This revised window is consumed. Root reviews the newly observed not-enabled
management-group state and remaining target bootstrap/placement/terms/cost
prerequisites before a separately authorized next action. No repeat elevation,
initialization, subscription Create/request/email, storage, policy, Production
or automation change was made or authorized by this result. Store acceptance,
qualified review, unknown dump provenance, incident disposition/containment and
Story39.4/SOC2-GAP-001 closure remain open; prior failures and audit gaps persist.

Root revised-window review completed 2026-10-14 after both delegated reports.
Thirty-three record-copy pairs/source and owner-only modes were independently
verified. Exact fresh assignment removal, saved No and preserved GA support
cleanup; retry audit events remain unverified. The provider explicitly reports
management groups not enabled; no absence of all policies or future-placement
claim follows. Microsoft describes hierarchy/root initialization on first use.
Do not add a management group or repeat elevation merely to make an optional
feature inspectable. Proposed next decision is the existing supported cross-
directory subscription REQUEST only, with its owner email and acceptance flow
disclosed. This is not approval to accept ownership, initialize hierarchy, assign
additional roles or create paid resources. Inspect actual target subscription
placement/RBAC/deny/policy/billing before resources or evidence use. Existing
request/name/owner/terms and exact administrative-capture extension still need
actual final action confirmation. Incident-store and Story39.4 acceptance remain
open; no downstream action was executed by this review.

## Actual Subscription-Request Failure And Bounded Remedy Proposal — 2026-10-14

FR-01 actually approved the exact prepared subscription REQUEST and bounded
subscription/billing metadata capture in the existing tenant-creator
administrative record and its same manual retained/recovered destinations.
This authorized the provider ownership-request email, not target-owner
acceptance, additional terms, security-policy changes, grants, paid resources,
hierarchy initialization, Production changes or deletion. Existing inclusive
$25 aggregate upfront/$55 monthly ceilings remain in force.

Current unsubmitted Review + create matched the exact privately prepared name,
new Workforce target, corporate MCA billing context and intended company-admin
owner; validation passed, management group displayed None and no budget was set.
No checkout/due-now quote or binding-agreement notice was displayed. This
observation is not a price guarantee or permission to accept ownership/terms.
Preflight metadata was retained as private v34 before the one approved action.

The coordinator selected the exact prepared Create once. The notification
reported **Failed**: target `blockSubscriptionsIntoTenant` policy is enabled
and the selected destination owner is not exempt. Refreshed View Requests then
explicitly reported no subscription creation requests to view. Therefore no
request creation, subscription creation, actual expiry or ownership acceptance
is verified. Email delivery was not inspected and is not claimed; do not assert
that no email could have been sent solely from the empty request view.
Failure/inventory observations are retained in private v35.

A subsequent read-only, context-matched ARM GET of
`/providers/Microsoft.Subscription/policies/default?api-version=2021-10-01`
succeeded: `blockSubscriptionsIntoTenant=true`,
`blockSubscriptionsLeavingTenant=true`, `exemptedPrincipals=[]`. This
corroborates the destination restriction and supplies no existing exempt-owner
route at that cutoff. Exact target response is retained in private v36.
All thirty-six retained/recovered pairs match; source matches v36 and all
previous versions are preserved. No policy/exemption/grant change, retry,
paid resource, acceptance, Production or automatic-backup population change
occurred. The record extension remains narrow administrative capture, not
accepted enterprise evidence custody or incident-content authority.

### Proposed Smallest Remedy — Not Approved Or Executed

1. Preserve both tenant-wide blocking flags. Microsoft documents that an exempt
   user bypasses **all** subscription-policy settings, including incoming and
   outgoing movement. A one-owner exemption narrows the principal population;
   it is not an incoming-only, one-request or one-subscription technical boundary.
2. Before any change, privately bind the exact selected owner object, target,
   current policy values and complete exemption population, accountable
   administrator, attended start/deadline, existing capture destinations and
   positive restoration route. Current empty exemptions are a baseline, not
   authority to overwrite a later change. Stop on concurrent drift.
3. Prefer an already authorized capable administrator. If an additional fresh
   root-UAA window is necessary, obtain exact fresh grant/removal authority;
   all prior windows are consumed. Microsoft's portal procedure requires a
   direct Global Administrator with elevated permissions for policy edits.
   A successful read does not prove write authority. No new role is authorized
   by the request approval.
4. Any proposed exception needs explicit approval of the full incoming/outgoing
   bypass, exact add/remove targets, bounded attended lifetime, retained failure
   evidence and one exact REQUEST retry. Preserve existing flags, exemptions,
   GA and unrelated authority; capture each new effect before restoration.
5. A staged add/one-REQUEST/restore exercise could test request creation only.
   Current primary guidance and the failed runtime do not prove whether later
   target-owner acceptance rechecks the policy or requires the exemption then.
   Do not guarantee acceptance after restoration, and do not leave the exception
   active while waiting for later acceptance authority. Acceptance, its real
   terms/charges/placement, and any separately needed exception lifetime remain
   their own concrete decision after supported verification.
6. Restore only the exact new exception and, if separately approved/created,
   exact new temporary root assignment promptly within the approved window.
   Verify the full policy matches the preflight baseline and positive exact-role
   absence while preserving GA. A failure triggers the approved recovery route
   and escalation; it is never silently recorded as successful cleanup.

Current primary Microsoft references checked October 14:

- https://learn.microsoft.com/en-us/azure/cost-management-billing/manage/manage-azure-subscription-policy
- https://learn.microsoft.com/en-us/azure/cost-management-billing/manage/create-subscription-request
- https://learn.microsoft.com/en-us/rest/api/subscription/2021-10-01/subscription-policy/get

These establish documented behavior and available metadata procedures, not
independent review, current effective write permission, future costs or
acceptance-stage policy behavior. FR-01 decision on this concrete proposal is
pending. The failed request remains retained history; protected-store/setup,
management review and final-period gates remain unmet. Professional and
independent dependencies remain deferred/unassigned, not fulfilled or N/A.

## Actual Bounded-Remedy Failure Recovery And Corrected Helper — 2026-10-14

FR-01's actual “please implement the correct solution” response authorized the
six-step proposal above: exact target/owner, one fresh root UAA if needed,
temporary owner exemption preserving both blocks, one REQUEST retry/email,
new-only restoration, attended <=30 minutes, same record/copy population and
existing inclusive cost ceilings. Ownership acceptance, extra terms, resources,
Production and source deletion remained excluded. This approval was not a
management register/calendar review or independent acceptance.

Preflight reverified the same exact retry Review form, no new agreement or
checkout, current full policy baseline, own root UAA0 and direct GA1. The
provider-denied request-owner fingerprint exactly matched the target operator
object; differing displayed corporate email and target UPN/mail did not justify
exempting another identity. All existing source/copy versions were preserved.

The attended window started **22:30:58.310896 UTC**, with deadline
**23:00:58.310936 UTC**. ElevateAccess POST200 created the exact new own root
assignment, captured before the policy attempt. The coordinator helper then
made an avoidable contract error: it sent a response-shaped `properties` wrapper
instead of Microsoft's documented top-level request fields. PUT returned200,
but GET did not verify the expected exemption. The request retry was therefore
**not performed**. Independent subsequent reads confirmed the full policy
properties, including policyId, remained exactly at baseline: incoming=true,
leaving=true, exemptions0. HTTP200 did not prove a policy change.

Protected evidence reference; individual observations and source details are retained in the protected original.

Normal silent MSAL `force_refresh=True` for the same existing account was
performed beginning **22:38:02.263268 UTC**; returned token target and actor were
checked in memory. Using that token, DELETE of only the exact new assignment
returned200, followed by independent current-CLI NotFound. Token values were
never stored or printed. This temporal result does not conclusively establish
why the earlier authorization failed.

Final context-matched proof at **22:38:40.517342 UTC** verified full policy
properties exactly equal baseline, both blocks true, exemptions0, own root UAA0,
exact new assignment GET404 and direct GA1. The attended interval to this final
proof was **462.21 seconds**, within30 minutes. No effective exemption, request
retry, ownership acceptance, paid resource or Production change was verified or
claimed. No independent human review or complete provider-audit-event population
is established by these API checks. The one fresh role window is consumed;
request retry remains unexecuted, not successfully completed.

### Corrected Runnable Helper And Local Verification

The failed helper source and corrected runnable source are retained **inside the
same existing private administrative record**, not a new file population. The
corrected helper uses only the three top-level request fields, preserves both
blocking flags, excludes response-only policyId, refreshes the exact same account
after grant and before removal, and treats an absent exemption as a baseline
verification case rather than an assertion that can skip recovery. New-only
role removal remains an independent finally action. The default invocation
stops before provider access; execution requires a fresh recorded unconsumed
approval reference and matching current record version. This is procedural
approval guidance, not a cryptographic or operating-system approval boundary.

Local `--validate` passed exact request shape, flag preservation, add/remove,
invalid response-envelope, changed-flag and duplicate-principal checks. A second
check proved default execution stops before provider access and leaves the
record unchanged. Corrected source SHA-256:
`003e0a9767f7e9e1e88db4aa6a0beb382e116ef6f23d1de7323b420c44f32798`.
These are meaningful local checks, not a corrected live policy-write test.
The corrected API contract is Microsoft's `subscription-policy/add-update-policy-for-tenant`
2021-10-01 request schema, checked directly on October14.

Private source **v56** and all **56 retained/recovered pairs** match; current
owner files0600/directories0700/no observed symlinks were checked, and every
prior version remains. No automatic-backup population or source accessibility
changed. Next action is root review of this actual failure/recovery and corrected
runnable helper, then a fresh exact window decision if another grant is needed.
Do not replay the consumed grant, treat the earlier failed implementation as
success, or leave a future exception active pending ownership acceptance.
Acceptance-stage policy requirements remain unverified and separately gated.

## Actual Corrected Window Subscription Outcome And Restoration — 2026-10-14

FR-01 actually approved “Authorize one corrected window and verified
restoration.” The exact current form, target/owner, full policy baseline,
corrected helper hash and same-account silent refresh were reverified. Existing
cost ceilings and all exclusions were preserved. Stale Save-disabled feedback
about the previously removed assignment did not authorize or require old-role
removal.

The fresh attended window started **22:53:37.403365 UTC**, deadline
**23:23:37.403390 UTC**. ElevateAccess POST200 was recorded22:53:38.492517;
corrected top-level policy PUT200 at22:53:39.966182; GET verified the exact one
selected-owner exemption and both blocking flags at22:53:40.506064. The exact
prepared Create action was selected **once**. Provider processing then displayed
**Successfully created the subscription**, with a new identifier retained
privately. The agent clicked no ownership Accept action and accepted no new
terms. This is a stronger/different provider outcome than a pending request;
its automatic ownership mechanism and email delivery are unverified.

Prompt restoration followed the actual provider result. Only the new exemption
was removed by PUT200 at**22:55:01.783310 UTC**; only the new root UAA was removed
by DELETE200 at**22:55:03.289915 UTC**. Final helper proof at
**22:55:03.632364 UTC** verified full policy properties exactly equal baseline,
incoming=true, leaving=true, exemptions0, own root UAA0, exact new role GET404
and direct GA1. Attended duration **86.228 seconds**, inside30 minutes. No exception
was retained for future acceptance. The one corrected window is consumed.

Read-only actual-state reconciliation confirms one exact-name subscription in
the target, state **Enabled**. Direct ARM reads using the same refreshed identity
show resource groups0/resources0 with no continuation, one direct own Owner
assignment at the new subscription, deny assignments0 and one inherited policy
assignment. Its referenced built-in definition was read: effect **Deny** for
West Europe location, no parameters, identity or overrides. That inspected rule
has no deployment effect; do not generalize it into a permanent absence of all
future policy effects or charges. Management-group placement is not independently
established by these reads. No agent-created paid resource or Production change
occurred. The new subscription Owner assignment is an observed provisioning
effect, not the temporary root role, and was not removed.

Initial CLI calls to new-subscription resource metadata failed because its local
account inventory did not recognize the new identifier; this is retained history,
not an empty-resource or authorization conclusion. Direct refreshed-token ARM
reads supplied the positive scoped counts above. Refreshed portal View Requests
has no creation requests; no actual request-expiry row is available. No mailbox
inspection or delivery claim was made. A later Storage name-availability metadata
request returned404/SubscriptionNotFound; the candidate name is therefore
**unverified**, not unavailable, and Storage-provider readiness remains a
pre-provisioning recheck. No resource creation was attempted to work around it.

Private source **v67**, all **67 retained/recovered pairs**, current-owner
files0600/directories0700 and no observed symlinks were verified. All earlier
failures, helper sources and versions remain. Draft future resource names are
prospective values only, not captured storage configuration or test results.
`S39.2-CORRECTED-20261014-01` indexes this actual result. Protected-store/setup
acceptance remains blocked. This operation does not perform management calendar
review, independent review, qualified advice, incident review or period evidence.

## Bundled Remaining T03 Implementation And Synthetic-Test Manifest — 2026-10-14

**Planned; no further external mutation by this manifest.** Reuse the October6
bounded empty corporate-setup authority, current two-subscription design,
existing $25 inclusive aggregate upfront/$55 inclusive aggregate monthly limits
and the now verified dedicated target subscription. Do not ask to recreate the
tenant/subscription or reapprove unchanged budget. New security-sensitive access,
exact test removals, synthetic retention/network selection and broader capture
need the concrete decisions below. This is the existing canonical T03 manifest,
not a second task register or a store-acceptance claim.

Exact prospective resource-group/account/container/log-workspace/probe values
are privately bound in `next_storage_test_manifest_draft` in the existing
administrative record. Public aliases below refer only to those proposed values.
Current subscription has no resources or groups. Global account-name availability
is unverified after the documented404; recheck before action, stop on collision
and do not overwrite or substitute a different name silently. No proposed value
is a reservation or a created endpoint.

### Proposed Component And Access Configuration

| Target alias | Concrete proposed state | Authority/dependency and validation |
| --- | --- | --- |
| Dedicated target subscription | Reuse observed Enabled asset and current direct FR-01 Owner; both tenant move blocks stay true, exemptions0, own root UAA0 | No new root grant is proposed. Reverify target, owner, placement and inherited assignments before resources. Do not alter Production or another subscription. |
| New dedicated resource group | One new group; East US2 preferred, East US only after compatible availability/cost validation | Existing empty-setup scope applies; exact proposed private name must remain collision-free. No group deletion or migration. |
| New evidence Blob account | StorageV2, Standard_ZRS, Hot, flat namespace, platform encryption, HTTPS-only, TLS1.2 minimum, anonymous Blob access disabled, Shared Key disabled for the selected Entra-only clients | Recheck Storage-provider recognition, regional SKU and selected-client compatibility before creation. No hierarchical namespace, SFTP/NFS, public container, account-key inspection or SAS credential generation. No immutable lock. |
| Own-operator access proposal | Protected evidence reference; individual observations and source details are retained in the protected original. | Requires actual management selection because the private administration path remains unresolved. Bind actual current IP privately at action time; no broad CIDR, all-networks fallback or trusted-service bypass. Dynamic/IPv6-only routing can block access. Private endpoint/DNS/VPN alternative needs its complete quote and access/recovery path first. Sealed public-network-disabled creation is possible preparation but cannot be called usable or tested. |
| Private evidence container | One new container, anonymous access None; synthetic-only until custody/acceptance gates pass | Container scope binds the grants below. No customer/CUI/incident/report intake. No automatic lifecycle deletion. |
| Reversible recovery proposal | Blob versioning; seven-day Blob and container soft-delete; seven-day change-feed window for synthetic setup only | Actual management retention decision required. These are proposed synthetic settings, not seven-year retention or qualified Legal approval. Preserve all created versions and failed samples; no locked immutability, point-in-time restore, purge or lifecycle deletion. |
| Synthetic audit destination | One new Log Analytics workspace, pay-as-you-go Analytics, proposed30-day synthetic-log retention; Blob-service diagnostic setting with StorageRead/StorageWrite/StorageDelete to that exact workspace | Validate supported categories, actual pricing and authenticated query access before action. No ingestion from Production/customer workloads or broad subscription-wide diagnostics. Retention expiration is disclosed; authoritative administrative copies remain retained pending separate disposition. Do not log tokens, keys, SAS URLs or content. |
| Cost monitoring | Reconcile existing recurring commitments; one scoped monthly budget/alerts to the privately bound FR-01 recipient, if selected and authorized; test notification delivery | Budgets alert rather than stop consumption. Include the test email in the bundled authorization. Do not assume an alert, free tier, hard cap or budget is operating from saved configuration. |

Protected evidence reference; individual observations and source details are retained in the protected original. IDs: FR-01.

### Exact Planned Grants And New-Only Revocation Scope
Protected evidence reference; individual observations and source details are retained in the protected original. IDs: FR-01.

### Synthetic Save Read Deny Recover Audit Sequence

All cloud object targets and destinations refer to the exact privately proposed
probe in the exact new container/account. Obtain fresh exact action approval
before its base-blob deletion and new-assignment revocations. No local source
file or unrelated cloud object may be removed.

1. Verify effective TLS, network rule, anonymous/shared-key denial, versioning,
   recovery values, diagnostics and actual principals/scopes. Use only the
   harmless dated synthetic marker; never customer content, CUI or exposure data.
   Keep credentials/token values out of results and transcripts.
2. As the authorized collector, save the probe and record timestamp, hash,
   metadata and returned version reference privately. Perform one authorized
   in-place overwrite to create a second version and prove prior-version
   retrieval/integrity; do not delete either version.
3. From the distinct target principal before its data grant, attempt authenticated
   read and record the actual authorization denial. From the permitted client
   network, perform an anonymous read and retain the specific result; a firewall
   denial alone cannot prove RBAC. Do not publish or send a usable anonymous link.
4. Grant the scoped test Reader, read the intended exact version/hash, and prove
   write and delete attempts are denied without changing the probe or versions.
   Re-list/hash as collector to prove failed attempts left resources unchanged.
5. Under the fresh exact probe-removal approval, delete only its current base
   blob, preserve every prior version, and recover the exact content by the
   documented version-to-base recovery method. Verify recovered hash and
   retrievability of preserved versions; no version purge or permanent deletion.
   If provider recovery semantics differ, stop before deletion and revise the
   concrete recoverable action rather than improvising.
6. Revoke only the exact new test grant(s), allow documented propagation and
   prove denial; rejected operations must leave all objects/versions unchanged.
   Token refresh must keep the same identities; no cache clearing or other
   principal may substitute for the approved test.
7. Query the protected synthetic log destination after bounded ingestion wait;
   match actual read/write/delete operations, actors, times and result to the
   test population. Preserve missing/rejected events and correlation limits.
   No observed audit row is invented; configuration alone is not audit success.
8. Verify final grants/new-only revocations, both tenant blocks/exemptions0/root0,
   stored object/version hashes, recovery, log access, actual costs and all
   source/retained/recovered copies. Leave resources unused for real evidence;
   no deletion rollback. Failure means preserve/escalate/retest, not silent
   cleanup. No independent store acceptance is implied by FR-01 testing.

### Cost Basis And Bounded Custody Decision

October14 public East US2 retail lookup reconfirmed General Block Blob v2 Hot ZRS
storage **$0.023/GB-month**, writes **$0.0625/10,000**, reads **$0.004/10,000**.
These are public meter rates, not a tax-inclusive MCA quote. The attempted current
Log Analytics product filter returned no applicable rows; do not silently treat
the historical ingestion/retention estimate as a current quote. The earlier core
$5–$15/month envelope remains planning context only. Private networking, existing
licenses/support, logs, recovery, taxes/fees and all setup subscriptions count
against the already approved $55/month/$25 upfront totals. Verify the actual
selected-service model before resource creation; stop on a new term or a ceiling
breach. Selected-network setup avoids inventing a VPN price, but does not itself
prove a complete invoice. Synthetic volume must stay minimal; alerts are not
spending enforcement.

The actual storage/configuration/test capture extension is still pending. Bind
it to the **same existing administrative record and exact same retained/recovered
destinations**: only the named new resource configuration, network/client metadata,
exact role events, harmless marker hashes/version references, provider errors,
synthetic diagnostic metadata and supported cost/budget observations. Preserve
all source/current/prior copies and failures, owner-only permissions and the
existing backup allowlist. No automatic population expansion, real evidence,
customer files, incident contents, personnel records, keys/tokens or reports.
This is temporary management-side synthetic custody; it does not approve
enterprise intake, independent recovery, offsite resilience or workspace acceptance.

Protected evidence reference; individual observations and source details are retained in the protected original. IDs: FR-01. Recorded status: deferred.

## Actual Bounded Storage And Synthetic Verification — 2026-10-14

**Implemented for synthetic setup only.** Subsequent exact FR-01 authority
selected the privately bound operator IPv4/32, a new nonprivileged workload
application/service principal with a short-lived certificate, the reversible
synthetic retention settings, exact container grants/new-only revocations,
exact probe base deletion/version recovery, and capture in the same approved
administrative record/manual retained and recovered population. This supersedes
the preceding manifest's pending-decision statements for those exact actions.
It does not approve enterprise evidence intake or independent acceptance.

`S39.2-STORAGE-SYNTHETIC-20261014-01` indexes the actual result. Restricted
coordinates, provider correlation metadata, helper source/history and retained
failures remain in protected record
`S39.2-TENANT-CREATOR-CHANGE-2026-10-14-01`, current v140. The replacement
collector found the original bounded helper still running, inspected its saved
state and allowed its existing log query to finish; it did not replay creation,
grants, writes, deletion or recovery after the interrupted agent stream.

| Boundary | Actual result and limit |
| --- | --- |
| Dedicated target | New target resource group, Hot ZRS storage, private container, Log Analytics workspace, diagnostics and monthly budget created in the new evidence subscription; no Production action or real evidence intake |
| Storage configuration | Independent ARM readback HTTP 200: TLS1_2, HTTPS-only, shared-key authorization disabled, anonymous Blob access disabled, public network Enabled with default Deny/bypass None and exactly the approved operator IPv4 rule; container public access None. Configuration is verified; no separate disallowed-network, shared-key or insecure-TLS request was executed |
| Recovery configuration | Versioning enabled; seven-day Blob/container soft delete and change feed; permanent delete disabled; point-in-time restore disabled. These are synthetic settings, not approved long-term retention or immutable custody |
| Save/prior read | Save v1 HTTP 201, overwrite v2 HTTP 201, prior-version read HTTP 206 and expected hash; original versions retained |
| Anonymous and no-role denial | Anonymous read from the permitted network HTTP 401/NoAuthenticationInformation. Distinct workload obtained a valid correct-tenant token; pre-grant read HTTP 403. Neither a wrong-issuer token nor a firewall denial substitutes for RBAC proof |
| Scoped Reader | Protected evidence reference; individual observations and source details are retained in the protected original. |
| Exact recovery | Approved exact current base deletion HTTP 202; retained v2 read HTTP 206 while base absent; re-upload of recovered bytes to the same base HTTP 201, expected hash and all three resulting versions present. No prior version was deleted |
| Effective revocation | Exact Reader assignment removed and GET 404. The same authenticated workload continued three HTTP 206 reads during propagation, then HTTP 403 about 60 seconds after removal. Immediate revocation is disproved by this observation; no fixed future propagation guarantee follows |
| Final authority | Independent readback at 23:43:51 UTC and following requests: all four newly recorded data-role entries GET 404, exact public certificate absent, new service principal disabled. Directory objects retained; no private key persisted by the helper. Own root UAA 0, direct GA 1; full subscription policy equals baseline with both blocks true and exemptions 0 |
| Logs and query | Blob StorageRead/StorageWrite/StorageDelete enabled to the exact new workspace; workspace 30-day retention and 0.1 GB/day ingestion cap. Existing bounded query completed 23:42:54 UTC after six new queries plus one earlier attempt; 33 actual scoped rows include read/write/delete and authorization denials. This proves the observed synthetic population only |
| Cost | Monthly 15 USD monitoring budget creation HTTP 201; alert delivery unverified. Existing 25 USD aggregate upfront/55 USD monthly ceilings remain; budget and ingestion cap are not a hard bill ceiling. Actual tax-inclusive aggregate invoice and sustained spend remain unverified |
| Administrative custody | Current v140 and all 140 retained/recovered pairs are byte equal to their matching captures; current equals latest; files 0600/current owner/no file symlinks. All prior sources/helpers/failures preserved. Same-volume recovery remains a management custody limitation; no automatic backup population expansion |

The 33-row actual query population is: DeleteBlob 202 OAuth 1 and 403 OAuth 1;
GetBlob 206 OAuth 12, 401 Anonymous 3 and 403 OAuth 5;
GetBlobProperties 404 OAuth 2; ListBlobs 200 OAuth 5;
PutBlob 201 OAuth 3 and 403 OAuth 1. Exact actors, timestamps and correlation
references remain private. Failed harness attempts remain in the record:
anonymous 401 was initially misclassified, and an application request was
rejected for certificate end-date formatting. Corrected classification and
certificate handling preceded the successful tests; these failures are not
erased or counted as passed operations. The record's generic `success` flag
describes request completion, so expected-denial verdicts use actual status and
provider code rather than treating every `success:false` as a failed test.

Three architectural limits remain: the single dynamic operator IP can lock out
administration; deleting RBAC assignments does not instantly revoke cached
authorization; and versioning/short recovery plus same-volume copies does not
provide independent, offsite or immutable evidence custody. The bounded pattern
used exact data scopes, valid-principal denial checks, observed propagation,
new-only revocation and protected retained metadata to test these boundaries.
Future access, retention and recovery design needs management disposition and
appropriate independent review before real intake.

Protected evidence reference; individual observations and source details are retained in the protected original. IDs: FR-01. Recorded status: Partially implemented, Planned, Do not claim.

### Final Read-Only MFA Cost And Recovery Reconciliation — 2026-10-14

`S39.2-FINAL-READONLY-20261014-01` records the final bounded lookups in the
same approved private record, now current v141 with all 141 retained/recovered
pairs byte-equal and current matching latest. The preceding v140 observations
remain dated cleanup/configuration evidence rather than a claim that v140 is
still the latest capture. No new consent, authentication flow, grant, test
notification, reboot, copy destination or external mutation was performed.

Protected evidence reference; individual observations and source details are retained in the protected original.

These unavailable facts remain open acceptance items. No additional permission,
provider consent or broadened collection was inferred to close them.

### Actual Limited Management Review — 2026-10-14
Protected evidence reference; individual observations and source details are retained in the protected original. IDs: SOC2-39.2-MGMT-20261014-01, FR-01. Recorded status: pending. References: [canonical
setup record](story-39.2-program-setup-record.md#management-self-review-packet--soc2-register-draft-023--2026-10-14).

Protected evidence reference; individual observations and source details are retained in the protected original.

### Supplemental Existing-Authority MFA Lookup — 2026-10-14 EDT
Protected evidence reference; individual observations and source details are retained in the protected original. IDs: S39.2-MFA-SUPPLEMENT-20261014-01.

Protected evidence reference; individual observations and source details are retained in the protected original.

### Existing Portal MFA Event Verified — 2026-10-14 EDT
Protected evidence reference; individual observations and source details are retained in the protected original. IDs: S39.2-PORTAL-MFA-20261014-01.

Protected evidence reference; individual observations and source details are retained in the protected original. IDs: FR-01. Recorded status: Implemented, Do not claim.

Protected evidence reference; individual observations and source details are retained in the protected original.

The smallest remaining human-only recovery preparation is Runbook 7A step 1:
FR-01 chooses a maintenance window and confirms local human access to the
existing non-admin account. Step 5 separately requires explicit authorization
for one normal human-initiated reboot after the read-only baseline passes.
The runbook itself supplies no reboot authority. No reboot, new account,
credential reset, offsite destination or recovery-population expansion was
started here. Even a later successful management exercise would not establish
independence, distinct recovery custody or offsite resilience.

## Finite Internal Governance Snapshot Intake — 2026-10-14 EDT

The user's expanded request to implement the remaining solution now authorizes
bounded internal staging of the four sanitized canonical records and one hash
manifest under `S39.2-REVIEW-PACKAGE-20261014-01`. Parent review bound the exact
[copy manifest and procedure](story-39.2-owner-setup-runbook.md#finite-reviewer-intake-preservation-and-delivery-procedure--2026-10-14)
before execution: same existing protected container, new unique UTC/hash prefix,
full-content privacy/source validation, no overwrite, preserved sources/versions,
Entra readback of all five hashes, same operator's intended container Data Reader
and at-most 10-minute copy Contributor followed by exact new-role removal. No
root elevation, other principal, new app or independent-reviewer assignment.
This supersedes the earlier synthetic-only scope for these exact sanitized
copies; all customer, CUI, incident, personnel, report, raw administrative JSON,
credential and general restricted intake exclusions remain.

Use a minimum 90-day hold for the allowed administrative/synthetic metadata and
bounded sanitized snapshots, preserving all versions until a separately exact
disposition. Review monthly after that minimum. No automatic expiry/deletion,
lifecycle purge, immutable lock, shortened existing hold or legal/enterprise
retention conclusion is introduced. Existing seven-day recovery/change-feed and
versioning remain; live logs stay 30 days, with the actual 33-row result retained
in the approved administrative captures. This adds negligible snapshot volume
rather than a paid log-retention service; tax-inclusive actual aggregate cost
remains unverified and the 25 USD upfront/55 USD monthly ceilings remain.

Protected evidence reference; individual observations and source details are retained in the protected original. Recorded status: pending.

### Finite Cost And Notification Verification Outcome

Fresh bounded reads confirm the 15 USD monthly budget and three unchanged
50/80/100% Actual email rules. ActualCost still returns 404/NotFound; reported
budget currentSpend 0 USD is not a zero-spend/invoice or aggregate-cost proof.
The original action-group inventory was empty. Official Consumption Budgets
operations expose no direct send-test action; supported Azure Monitor test
routes were checked against current Microsoft documentation and installed CLI.

The first resource-group sample request returned 500 with a missing groupName
adapter error and no status locator; its eventual delivery is not assumed.
One new empty action group was created 201/read 200, but its provided-receiver
test returned 400 because receivers must already exist. Under the user's existing
same-recipient budget/test-email authority, exactly one receiver for that same
private budget email was configured 200/read 200/status Enabled. A provider welcome
or verification email may have been sent. The matching-receiver test and one
bounded correction to the official sample's `budget` discriminator both
returned 400/BadRequest, “There are no valid receivers in the request”. All
failures are retained; no successful provider transport, threshold evaluation,
direct Budget-engine delivery or mailbox receipt is claimed. No further retry,
new recipient, threshold change, role expansion, spend-forcing event or email
subscriber was created. The action group/receiver remain retained for authorized
follow-up; no resource removal was attempted.

Public Monitor pricing lists 1000 monthly email notifications included; this
bounded email-only configuration does not add an alert rule, SMS, webhook or
automation workload. That public allowance is not an inclusive agreement quote
or proof of all account-wide usage. The provider/receiver problem is a finite
open supporting-control exception, not a reason to repeat passed storage tests
or stop sanitized package staging. The current private v160 checkpoint preserves
the actual configuration and rejected attempts.

Primary references checked October 14 EDT: [Budgets operations](https://learn.microsoft.com/en-us/rest/api/consumption/budgets?view=rest-consumption-2026-06-01),
[resource-group sample operation](https://learn.microsoft.com/en-us/rest/api/monitor/action-groups/create-notifications-at-resource-group-level?view=rest-monitor-2021-09-01),
[action-group sample operation](https://learn.microsoft.com/en-us/rest/api/monitor/action-groups/create-notifications-at-action-group-resource-level?view=rest-monitor-2021-09-01),
[Cost Management query](https://learn.microsoft.com/en-us/rest/api/cost-management/query/usage?view=rest-cost-management-2025-03-01)
and [Monitor pricing](https://azure.microsoft.com/en-us/pricing/details/monitor/).

### Actual Protected Snapshot Receipt And Finite Handoff

`S39.2-REVIEW-PACKAGE-20261014-01` is now **Implemented for the bounded
sanitized internal purpose**. Frozen capture cutoff was 2026-10-15T01:19:25UTC
(October 14 EDT). Four canonical source files total 492128 bytes; manifest 1999
bytes; five no-overwrite blobs were created in a new unique prefix. All five
byte sizes and SHA-256 hashes passed authenticated readback, then passed again
after temporary Contributor removal. Manifest SHA-256 is
`2f64c96e13b4791ff95f17e0315af71933a961f6b911af712213227628381d06`.
The manifest lists four source hashes and does not include its own hash, avoiding
a self-referential digest. No cloud path, resource/principal identifier or
restricted administrative record was exported into the snapshot.

The existing operator's intended container Data Reader was created and retained
for operational custody. Temporary copy Contributor was removed with exact
GET404 after 4.86 seconds, inside the 600-second bound. Post-copy scope inventory
contains the original Owner and intended Reader only. Independent readback
starting 2026-10-15T01:22:01UTC confirms Reader 200, temporary Contributor 404,
root UAA 0/GA 1, full subscription-policy baseline, unchanged Entra/network
storage posture, five package version objects and all three original synthetic
versions preserved. Authenticated reads are expected through intended Reader;
they do not prove instantaneous invalidation of any cached Contributor token.
No subsequent mutation of governance snapshots was used as a revocation test.

Package completion was 2026-10-15T01:20:48UTC. At the final source-cutoff check,
all four canonical files still matched the captured hashes. This receipt and
later canonical edits are deliberately outside that already frozen snapshot;
the original five blobs will not be overwritten to include them. The exact
version/request/hash/role coordinates and final receipt are in the same approved
administrative record, now current v176. Current equals latest; all 176 retained/
recovered pairs match, files 0600/current owner/no file symlinks. All earlier
source/helper/failure versions remain preserved, with no automatic backup
population expansion or new local recovery destination.

Protected evidence reference; individual observations and source details are retained in the protected original. Recorded status: pending, deferred.

No further agent-executable copy, privilege, cost poll or synthetic test remains
necessary for this finite package. Keep the actual notification-test/provider
exception, unavailable cost data, future calendar/control populations, eligible
reviewer/qualified gates and future T07 readiness decisions explicit. Neither
this bounded package nor management self-review is final Story 39.2 acceptance,
an independent assurance conclusion or permission to store customer/CUI/incident
material. No application build or deployment occurred.

The resulting store, existing populated register and adopted owned calendar are
usable for **provisional sanitized internal setup**. Full T03 enterprise custody,
broader intake, unavailable independent review and final Story39.2 period
acceptance remain distinct unfulfilled conclusions. No additional management
micro-approval or optional local reboot is a prerequisite to declaring this
finite authorized implementation/staging batch complete.
