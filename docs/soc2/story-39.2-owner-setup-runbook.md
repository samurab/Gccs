

Publication classification: **Sanitized public reference**. Complete originals and original SHA-256 hashes remain in protected `release-scope-lookups-20261018-v1/primary-source-snapshot-v1`. This counterpart records scope and traceability; it does not establish workforce control effectiveness, review completion or acceptance.

# Story 39.2 owner setup runbook

Status: **Instruction guide only — Story 39.2 setup and final gates remain blocked.**
Checked: 2026-10-14 EDT; provider cutoffs remain in the canonical workspace checkpoint. Management owner, operator, collector and custodian: `FR-01` under `SOC2-ROLE-FR-01`; management self-review pending until actually performed.

This guide explains the next actions for a first-time owner. It is **not** a second task register, evidence index, approval, or claim of operating effectiveness. Record status and decisions in [the Story 39.2 setup record](story-39.2-program-setup-record.md), [the execution ledger](soc2-execution-ledger.md), [the capacity decision](founder-capacity-budget-decision.md), and [the workspace decision](protected-evidence-workspace-decision.md). Keep names, account IDs, resource names, URLs, screenshots, invoices, authentication details, and raw evidence out of public Git and chat. Use only aliases and restricted evidence references here. Do not upload CUI or customer content for setup tests.

## Why the sequence matters

1. A personally owned Azure subscription does not prove business custody, independent recovery, or succession. Technical storage settings cannot repair that ownership gap.
2. The drafted 36-item checklist, 15-gap baseline, seven tasks, and 33-control calendar are not an operated program. Without actual owners, reviewers, deadlines, and reviewed cycles, a template can produce false readiness conclusions.
3. The public-repository exposure tracked in Story 39.4 is not contained by passing PR-diff checks. PR #129 closed unmerged on 2026-10-10; its failed push and passing PR-diff results remain in history. This guide does not authorize restricted inspection, history changes or branch-protection changes.

The correct pattern is **decide and authorize -> establish an organization-controlled boundary -> configure and independently test a protected store -> operate and review the register/calendar -> make a dated readiness decision**. A draft, a cost ceiling, and a successful synthetic test each prove different things; none alone completes Story 39.2.

## 1. Confirm the checkpoint before doing anything

**FR-01:** Open the four canonical records linked above and the current [Story 39.2 source](../development-story-prompts.md). Read the execution-order and shared-contract sections before changing a status. In the ledger, compare the recorded story-document hash, branch, revision, PR and deployment state with current Git/GitHub/Azure read-only state. In the Story 39.2 record, find tasks `SOC2-39.2-T01` through `T07`, the historical 36 checklist rows and 15 gaps, and the 33-criterion calendar. Record any disagreement in the canonical record before acting; do not duplicate an existing task, branch, resource, or deployment.

**Pass:** The checkpoint is reconciled and discrepancies have an owner and next action. **Current result:** provisional Story 39.1 scope permits setup; Story 39.2 setup is blocked; Story 39.4 immediate triage remains on hold for authorized containment. Do not advance Story 39.3 on the basis of this guide.

## 2. Use the corrected capacity decision and replan internal work

**FR-01:** The [capacity decision](founder-capacity-budget-decision.md) records 40 hours/week dedicated to internal SOC 2 Type I readiness from 2026-09-28 through 2026-10-30, with no blackout dates. Review the replacement estimates in the Story 39.2 register, record actual commitments separately, and escalate slippage at the next daily coordination check. T01, T02 and T04–T06 retain their expired estimates as history. Actual hours and capacity after 2026-10-30 remain unverified.

The [workspace decision](protected-evidence-workspace-decision.md) records the **2026-10-06 bounded implementation authorization** for a separate new FeDril LLC corporate Workforce target: $55/month inclusive recurring and the subsequently amended $25 aggregate upfront inclusive of taxes and mandatory fees. This supersedes the September no-provisioning/no-spend hold only within that scope. The authorized target excludes existing Production subscriptions/resources, protected evidence migration and restricted incident inspection. Reuse the prior decision rather than requesting it again. Before creation, verify the actual agreement terms, management-group placement, inherited policies and automatic deployment costs. The historical home root read/removal is recorded in the workspace decision; it does not establish the new target hierarchy or future inherited effects. Reverify the applicable target metadata without assuming new elevation authority.

**Pass:** The current authorization and proposed schedule are accurately recorded. Preparation can continue while hierarchy/pricing verification is pending. CPA, Legal/Privacy, forensic and independent review remain deferred at their respective gates; their absence does not stop unrelated management preparation or approved empty-target implementation. The historical October 15 package target provides no additional authority.

## 3. Establish organizational ownership before storage provisioning

**FR-01, only after specific authorization:** Verify legal entity/domain authority and a business-controlled billing arrangement in a restricted location. Decide who owns the workforce identity tenant, billing account, subscription, recovery method, and successor access. Use a new business-controlled boundary if approved; leave the existing personally owned and production boundaries unchanged. In the Azure portal, search **Microsoft Entra ID**, select **Overview > Manage tenants > Create**, and choose a workforce tenant if eligible. Tenant creation may be unavailable for the account/offer; do not work around role or paid-customer restrictions. Under **Cost Management + Billing**, verify the business billing account and invoice responsibility; under **Subscriptions**, create/select a subscription tied to that business arrangement only after the billing decision. Verify each relationship in the portal and preserve restricted proof. Never paste tenant, subscription, billing, domain-verification or user identifiers into the public records.

**Pass:** Business authority, billing responsibility, administrative and recovery ownership are evidenced and approved. A tenant or subscription merely bearing a business-like name does not pass.

## 4. Appoint people and resolve review independence

**FR-01:** In the Story 39.2 task register, assign a real accountable owner, control operator/collector, evidence custodian, reviewer, due date and escalation contact for each task/control family. The owner can fill multiple interim roles, but record the concentration risk and do not call self-review independent. Check actual availability, competence and conflicts before appointing a Security reviewer; seek qualified Legal/Privacy or CPA input where the source record requires it. Keep personal identities, resumes, conflict disclosures and signed appointments in the approved restricted location; save only aliases and evidence references in Git. If no independent reviewer exists, keep that review gate blocked.

**Pass:** Each required role is either actually appointed with dated evidence or explicitly marked unavailable with an interim safeguard and next review date. A role label alone does not pass.

## 5. Approve evidence scope, retention, access and cost design

**FR-01 with qualified reviewers where available:** List permitted evidence classes and excluded classes. Exclude CUI, customer documents, credentials, database dumps and incident content from the initial setup test. For real evidence later, decide classification, need-to-know groups, external sharing approval/expiry, hosting/residency, retention, legal holds, deletion and recovery. The proposed seven-year period is a management proposal, not an approved universal requirement; obtain applicable contract, privacy, Legal and CPA review before configuring irreversible retention. Compare the [Azure architecture/cost estimate](protected-evidence-azure-architecture-cost-estimate.md) with current official pricing and the approved ceiling, including logging and recovery costs. Choose the network design deliberately; do not infer that private networking is in the $15 ceiling.

If provisioned, set a scoped Azure budget from **Cost Management > Budgets > Add**, select the correct business subscription/resource-group scope, monthly amount, expiration, and alert recipients/thresholds. Test delivery. Azure budgets alert; they do **not** stop consumption, so review actual charges and have a manual stop/escalation rule.

**Pass:** An approved, costed design with allowed evidence, retention, access, network and recovery decisions. Missing qualified input is recorded as a blocker, not “not applicable.”

## 6. Implement the authorized empty corporate workspace — prerequisites pending

**Authorized business-subscription administrator, after action-time placement and terms verification:** Use the 2026-10-06 authority for the separate corporate target, within $55/month total recurring and $25 aggregate upfront inclusive of taxes and mandatory fees. Reuse the already created and authenticated selected Workforce tenant and the approved two-subscription design; its one-tenant Create authority is consumed. Complete the dedicated target-evidence subscription route and the existing workspace decision's exact action manifest. Confirm the selected directory/subscription before every creation action. Create a dedicated general-purpose v2 storage account in the approved region, after cost and redundancy checks. Do not reuse Production storage or attach its subscription. In the new account, disable anonymous Blob access and use a private container. Configure the approved networking path and secure transfer/TLS. Enable versioning and the selected reversible recovery settings; record retention approval as pending until management acts. Soft delete does not establish long-term retention. Irreversible retention, access grants and removal/denial tests need the exact existing or fresh action-specific authority. Empty implementation and synthetic testing may proceed without a CPA, Legal/Privacy or forensic appointment; protected migration and acceptance retain their own prerequisites.

Protected evidence reference; individual observations and source details are retained in the protected original.

Protected evidence reference; individual observations and source details are retained in the protected original. Recorded status: implemented, pending.

## 7. Run the synthetic acceptance test and independent review

**Custodian and appointed reviewer:** Make a plain-text test file containing only a harmless marker and date. In the protected container, perform and timestamp five checks: (1) authorized **save**, (2) authorized reviewer **read** of the intended version, (3) unauthorized account **denied** access, including a check that an anonymous link cannot read, (4) **recover** a deliberately deleted test version through the approved method, and (5) find the corresponding **audit** events in the configured destination after allowing for ingestion delay. Also test that a reviewer cannot write and that revoked access no longer works. Use a separate test account without privileged inherited permissions for the deny check. Do not use live customer or exposure evidence as a sample.

Protected evidence reference; individual observations and source details are retained in the protected original.

**Pass:** All required save/read/deny/recover/audit results were observed and independently reviewed. If the reviewer remains unavailable, note technical test results separately and keep the governance gate blocked.

## 7A. Run the synthetic-only reboot, unlock and local denial exercise

This procedure is **Planned** and human-gated. It may validate a narrow local recovery path for the exact two approved blank templates; it cannot establish offsite recovery, independent custody, sustained operating effectiveness or protected-workspace acceptance. Do not add real evidence, customer content, CUI or more source files for this exercise.

Current-state classification:

| Claim | Status | Limit |
| --- | --- | --- |
| Existing alternate local principal | **Implemented prerequisite only.** A 2026-10-10 read-only inventory found exactly one other local non-admin account | The identity is deliberately omitted. The inventory does not prove its human credential is available, that its holder is independent, or that the account is suitable for continuing custody |
| Reboot and post-login schedule recovery | **Planned** | No reboot-path observation exists |
| Protected evidence reference; individual observations and source details are retained in the protected original. | **Planned** | Protected evidence reference; individual observations and source details are retained in the protected original. |
| Another-user denial | **Planned** | No local cross-user denial result exists |
| Independent review or `SOC2-39.2-T03` acceptance | **Do not claim** | FR-01 remains owner/operator, sole recovery custodian and management self-reviewer |

### A. Schedule the human gate and establish the baseline
Protected evidence reference; individual observations and source details are retained in the protected original. IDs: FR-01. Recorded status: pending.

### B. Reboot and observe the password boundary
Protected evidence reference; individual observations and source details are retained in the protected original. IDs: FR-01. Recorded status: do not claim.

### C. Revalidate recovery without forcing an execution

11. **Coordinator, read-only:** Check the current-user job registration, interval, latest state and sanitized status history. Do not load, unload, kickstart or invoke the runner merely to produce a favorable result.
12. A fail-closed `blocked` result while the approved volume was locked can be recorded as expected only if it is the exact approved missing/locked-destination condition and no timeout, surviving worker or cleanup uncertainty occurred. It is not a successful backup.
13. After the human unlock, leave the job loaded and wait for the next normal interval plus a small observation allowance. Verify that a terminal allowed status appears without a kickstart or direct invocation. If the sources remain unchanged, the expected result is `unchanged`; do not edit a source to force a new version.
14. **Coordinator, read-only:** Re-run the complete baseline from step 3 and confirm the current runner count is zero. Record the post-reboot boot/session time, locked-state observation, human-unlock attestation, expected blocked status if one occurred, later interval result, latest-exit limitation, inventory result and protection result in the restricted evidence record. Public Git receives only the sanitized outcome and evidence reference.
15. Mark reboot-path recovery **Failed** if the current-user job is absent after login, the post-unlock normal interval does not produce an allowed terminal status, the latest result is timeout/failed/cleanup-incomplete, a runner remains, or any source/state/retained-copy invariant changes unexpectedly. Do not kickstart to hide the failure.

### D. Test another-user denial without changing privileges
Protected evidence reference; individual observations and source details are retained in the protected original. IDs: FR-01.

### E. Decide and record the result
Protected evidence reference; individual observations and source details are retained in the protected original. IDs: FR-01.

**Governance result:** Even after a technical pass, record **management-tested / independent review unavailable**. Keep independent review, distinct recovery custody, offsite recovery, sustained cadence, `SOC2-GAP-015`, `SOC2-39.2-T03` and protected-workspace acceptance open. A failed step remains in retained dated history with corrective action and retest status.

## 8. Operate the register and calendar, not just the template

**FR-01 plus appointed owners/reviewers:** Use the existing Story 39.2 record as the single register. For each of 36 checklist rows, 15 historical gaps, seven Story 39.2 tasks and relevant downstream story obligations, confirm the control/criterion mapping, risk/priority, owner, independent reviewer, due date, dependency, interim safeguard, evidence source, Type I/II impact, closure test and residual risk. Never overwrite a failed historical finding; add a dated disposition. For each of the 33 draft calendar controls, approve the event/period cadence, full population or sampling rule, source, collector, reviewer, collection/review deadlines, absence/failure alert and escalation. A no-event period needs a dated reconciliation of the source population, not an invented event.

At each due date the collector places actual restricted evidence, the reviewer checks authenticity, period, completeness/accuracy, population/sample basis and freshness, and records accept/reject with reason. Open exceptions for failures and significant changes, assign corrective action and independent retest, and retain the failure history. Record next due dates. Automation **configured** is not automation **tested and operating**.

**Pass:** Populated and owner-approved register/calendar are usable, with at least one real reviewed cycle where due. Do not call an unreviewed draft “operating evidence.”

## 9. Respect stage gates, preserve PR #129 history, and resume safely

**FR-01 and coordinator:** The Story 39.2 **setup gate** passes only when protected storage, the populated task/gap register and an owned usable calendar are evidenced. Its **final period gate** requires disposition of every scoped gap/control, current traceable evidence and an actually signed readiness decision. At Stories 39.18 and 39.22, freeze dated protected evidence-index snapshots and record approvers, open exceptions, proceed/defer decision and scope/period; do not claim a Type I/II report or SOC 2 assurance from setup work. Check the authoritative dependency rules before starting any later story.

Separately preserve Story 39.4's exposure checkpoint: PR #129 closed without merge on 2026-10-10. The public exposure and inherited-history failure remain unresolved. Do not recreate the PR, inspect restricted contents, weaken checks or rewrite history as part of this story. If this session ends, update the ledger with current story, branch/revision/PR, evidence references, tests, deployment state, specific blocker and exact next action. Reconcile live state on resume before retrying an external action.

**Pass:** The ledger accurately separates design, implementation, operating evidence, approval, staging and production. An unmet gate remains blocked, even if a document or configuration was created.

## Remaining non-sensitive owner actions

Reuse the existing capacity/role authority and current $55/month/$25 aggregate upfront ceilings, inclusive of taxes and mandatory fees; earlier $0 ceilings remain dated history. The new Workforce target and dedicated target evidence subscription now exist. Review the [actual corrected-window outcome and restoration](protected-evidence-workspace-decision.md#actual-corrected-window-subscription-outcome-and-restoration--2026-10-14): provider created an Enabled subscription, the agent clicked no ownership Accept/new terms, and full policy baseline/root-role absence/GA preservation were verified in86.228seconds. No request-expiry row or email delivery is established. The inherited policy inspected denies WestEurope; new groups/resources are absent, and FR-01 has direct subscription Owner. No further root elevation is proposed for ordinary empty setup.

Protected evidence reference; individual observations and source details are retained in the protected original. Recorded status: pending, deferred. References: [bundled remaining T03 manifest](protected-evidence-workspace-decision.md#bundled-remaining-t03-implementation-and-synthetic-test-manifest--2026-10-14); [seven-task checklist](story-39.2-program-setup-record.md#remaining-task-plan-and-acceptance-checklist--2026-10-14).

## Actual synthetic setup checkpoint and remaining actions — 2026-10-14

This dated checkpoint supersedes the preceding current-action summary's
pending network, workload-identity, synthetic retention, grant/removal,
probe-recovery and same-record capture decisions. FR-01 approved that exact
scope and the [actual bounded setup](protected-evidence-workspace-decision.md#actual-bounded-storage-and-synthetic-verification--2026-10-14)
completed. Do not repeat it to repair an interrupted chat transcript.

Protected evidence reference; individual observations and source details are retained in the protected original. IDs: FR-01. Recorded status: deferred. References: [dated v0.23 packet](story-39.2-program-setup-record.md#management-self-review-packet--soc2-register-draft-023--2026-10-14).

Protected evidence reference; individual observations and source details are retained in the protected original. Recorded status: Implemented, Partially implemented, Planned, Do not claim.

Protected evidence reference; individual observations and source details are retained in the protected original. References: [final bounded facts](protected-evidence-workspace-decision.md#final-read-only-mfa-cost-and-recovery-reconciliation--2026-10-14).

### Historical platform references checked 2026-09-28

## Actual limited management approval and next due action — 2026-10-14

FR-01 actually reviewed v0.23 and approved its limited management dispositions
under `SOC2-39.2-MGMT-20261014-01`. Do not request the same review again. The
seven-task register, 33-row internal calendar/quality plan, six open exceptions,
v140 synthetic results with limits, proposed timing/interim safeguards and
readiness defer are adopted for management use. The later v141 provider
supplement is outside the approved review scope and remains unreviewed.

On October 15, FR-01 performs the adopted initial follow-up using the existing
register/calendar: reconcile available items and failures, record actual
accept/revise/reject/defer decisions with reasons, preserve missing populations
and assign the next due actions. Continue the adopted daily coordination through
October 30. No future cycle, no-event period or capacity beyond October 30 is
assumed. The already reviewed synthetic sample does not represent all 33 scoped
control populations.

Protected evidence reference; individual observations and source details are retained in the protected original.

Protected evidence reference; individual observations and source details are retained in the protected original.

Protected evidence reference; individual observations and source details are retained in the protected original. IDs: FR-01, S39.2-TENANT-CREATOR-CHANGE-2026-10-14-01.

Protected evidence reference; individual observations and source details are retained in the protected original.

**Smallest human-only next recovery preparation:** Section 7A step 1 asks FR-01
to choose a maintenance window and confirm authorized local access to the
existing non-admin account without sharing credentials. Step 5 requires fresh
explicit authorization for one reboot after the unchanged-source/protection/job
baseline passes; Section 7A says the coordinator cannot infer reboot authority
from the runbook. No reboot or expanded/offsite copy was started here. This
exercise can address the local reboot/denial gap only; complete reviewer/access
coverage, appropriate retention/custody acceptance, distinct recovery custody,
offsite resilience, sustained operation and independent/qualified gates remain
open where applicable. Keep the adopted October 15 follow-up and readiness defer.

## Finite Reviewer Intake Preservation And Delivery Procedure — 2026-10-14
Protected evidence reference; individual observations and source details are retained in the protected original. References: [finite closure handoff](story-39.2-program-setup-record.md#finite-story-392-closure-and-reviewer-ready-handoff--2026-10-14).

### Exact Internal Snapshot Manifest

Package alias `S39.2-REVIEW-PACKAGE-20261014-01` binds only these existing
sanitized source files and one generated hash/provenance manifest:

| File | Package purpose |
| --- | --- |
| `story-39.2-program-setup-record.md` | Single register, calendar, quality queue, exceptions, actual v0.23 management disposition and finite T01–T07 handoff |
| `story-39.2-owner-setup-runbook.md` | Operational access, preservation, recovery and controlled delivery procedure |
| `protected-evidence-workspace-decision.md` | Protected evidence reference; individual observations and source details are retained in the protected original. |
| `soc2-evidence-inventory.csv` | Sanitized source/evidence provenance and quality/acceptance references |
| `manifest.json` | Capture UTC, source-relative names, byte sizes, SHA-256 hashes, record aliases, approved scope/exclusions and retention/claim limits |

1. Freeze the four files' byte hashes after the finite handoff and provider
   outcome notes are saved. Scan their full contents for the privately known
   tenant/resource/network/principal identifiers and names, email recipients,
   credentials, JWTs, private keys and SAS signatures. Check the CSV schema,
   unique IDs and all local references. A text scan supplements the source
   inspection; it does not classify arbitrary new customer or incident material.
2. Bind a new unique UTC/hash snapshot prefix within the existing approved
   private container. Record exact cloud coordinates only in the existing
   private administrative record. Recheck source hashes before upload and
   verify destination absence; stop on collision or changed source. Copy with
   no overwrite and preserve all existing blobs/versions and local files.
3. Use only the existing operator and, if necessary, a bounded new container
   Blob Data Contributor grant for this copy/read verification. Record exact
   assignment identity and deadline privately; remove only this newly created
   assignment in finally and verify exact GET404. Preserve baseline roles,
   subscription Owner and GA. No root elevation, new app, consent or reviewer
   grant is part of package staging.
4. Download each of the five newly created blobs using Entra authorization and
   verify byte size and SHA-256 against the frozen manifest. Record request,
   version, hash and cleanup references privately. A later canonical edit is a
   new source revision and does not alter the captured snapshot; use a new
   prefix for a future package instead of modifying this one.

Protected evidence reference; individual observations and source details are retained in the protected original. IDs: FR-01.

Only these sanitized governance snapshots are eligible under the current
expanded internal-staging request. The private administrative JSON, raw provider
exports, credentials, customer/CUI/incident/personnel contents and examination
reports are excluded. This is a bounded internal copy, not permission for general
restricted intake or external distribution. Immutability is procedural:
no immutable-retention lock or legal-hold configuration is asserted.

### Reversible Preservation And Recovery

Apply a minimum 90-day hold to the allowed administrative/synthetic metadata and
these bounded sanitized snapshots. Preserve every current/prior source and
version, then review retention monthly after that minimum; there is no automatic
expiry or deletion authority. This minimum does not shorten the earlier
preserve-until-explicit-disposition requirement and is not an enterprise legal
retention period. Keep the existing seven-day synthetic soft-delete/change-feed
settings and versioning, with no lifecycle purge or irreversible lock. The live
diagnostic workspace remains30 days; the actual 33-row synthetic result and
correlations remain retained in the approved administrative captures. No longer
log retention or extra ingestion is implied.

For an unavailable or altered snapshot, first compare the manifest and provider
version metadata read-only. Reuse the already demonstrated retained-version
retrieval pattern. Any recovery copy uses a separately unique destination and
preserves the original and every version; exact recovery-copy authority and
collision checks precede its creation. Do not delete/re-upload a governance
snapshot merely to repeat the synthetic acceptance test. A missing hash/version,
denied authorized read or inconsistent capture is an exception, not a passed
recovery. Existing local retained/recovered copies remain same-volume management
custody; no independent or offsite recovery claim follows.

### Least-Privilege Human Reviewer Handoff
Protected evidence reference; individual observations and source details are retained in the protected original. IDs: FR-01. Recorded status: pending.

### Completed Snapshot Receipt And Next Dependency

The reviewed internal package was captured 2026-10-15T01:19:25UTC and completed
01:20:48UTC, both October 14 EDT. Five new blobs contain the four exact canonical
files 492128 bytes plus manifest 1999 bytes. Collision count 0; no overwrite;
all five authenticated size/SHA-256 readbacks pass before and after temporary
Contributor removal. Contributor exact GET404 follows a 4.86-second window;
the intended current-operator container Reader remains. Final independent
readback confirms root 0/GA 1/full policy baseline and preservation of all three
synthetic versions plus five package versions. Current private v176/all 176
retained-recovered pairs match with owner-only files. See the [actual receipt](protected-evidence-workspace-decision.md#actual-protected-snapshot-receipt-and-finite-handoff).

This receipt is a later canonical revision than the frozen five-blob package;
do not overwrite the package to add it. The protected manifest and administrative
receipt provide the provenance for a future new-prefix update. No duplicate
local tracker or new source/recovery destination was created.

Agent-executable preparation and bounded internal staging are complete. The
next actual independent-review dependency is an eligible named human's
appointment/competence/conflict verification and scoped access under the
prepared procedure, not another engineering-choice or per-version management
approval. Continue adopted calendar coordination with real populations as they
exist. The optional Section 7A maintenance/reboot exercise is a separate design
follow-up and is not required to finish every safe action in this package.
Qualified enterprise-retention/review, actual future periods/T07 signatures and
unavailable provider cost/notification evidence remain open; no approval can
create those observations retroactively.

### Historical platform references checked 2026-09-28

- Microsoft Learn: [create a Microsoft Entra tenant](https://learn.microsoft.com/en-us/entra/fundamentals/create-new-tenant), [create a subscription](https://learn.microsoft.com/en-us/azure/cost-management-billing/manage/create-subscription), and [create Azure budgets](https://learn.microsoft.com/en-us/azure/cost-management-billing/costs/tutorial-acm-create-budgets).
- Microsoft Learn: [assign Blob data roles](https://learn.microsoft.com/en-us/azure/storage/blobs/assign-azure-role-data-access), [prevent Shared Key authorization](https://learn.microsoft.com/en-us/azure/storage/common/shared-key-authorization-prevent), [enable versioning](https://learn.microsoft.com/en-us/azure/storage/blobs/versioning-enable), [enable soft delete](https://learn.microsoft.com/en-us/azure/storage/blobs/soft-delete-blob-enable), and [monitor Blob Storage](https://learn.microsoft.com/en-us/azure/storage/blobs/monitor-blob-storage).
