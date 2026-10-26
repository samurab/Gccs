

Publication classification: **Sanitized public reference**. Complete originals and original SHA-256 hashes remain in protected `release-scope-lookups-20261018-v1/primary-source-snapshot-v1`. This counterpart records scope and traceability; it does not establish workforce control effectiveness, review completion or acceptance.

# Dedicated Azure SOC 2 Evidence Boundary — Architecture And Cost Estimate

Status: **Draft design / $55 inclusive monthly authorization / implementation pending**

Estimate ID: `SOC2-AZURE-EVIDENCE-ESTIMATE-0.1`

Estimate date: 2026-09-20 EDT

Management owner: Founder authority FR-01

Required reviewers: Independent Security reviewer; qualified Legal/Privacy and CPA reviewers where triggered

Task mapping: `SOC2-39.2-T03`, `SOC2-39.1-T05`, `SOC2-39.1-T06`

This is a sanitized planning record. It contains no tenant, subscription, resource, user, network or endpoint identifiers. The approved ceiling is bounded recurring budget authority for the new corporate setup only. This record is not an Azure configuration export, protected evidence store, acceptance result or readiness conclusion.

## Management Inputs And Their Limits

| Field | Recorded input | Evidence status and consequence |
| --- | --- | --- |
| Historical subscription ownership | Personally owned by FR-01, self-attested | Preserve this historical record until contractual transition evidence is reviewed; current UI labels do not silently supersede it. |
| Current administrator and billing metadata | Separate company-domain Chrome session showed Global Administrator, Azure subscription Owner and Microsoft Customer Agreement Billing account owner access; the active billing sold-to company displayed FeDril LLC. FR-01 self-attested that FeDril LLC is the agreement's responsible party and payment administration is company-authorized | Management responsibility is resolved by self-attestation and corroborating UI metadata. Independent documentary acceptance and recovery evidence remain unavailable; no Microsoft transfer was executed by this work. |
| Business/domain relationship | Historical Sierra Labs LLC ownership of `fedril.com` remains an FR-01 self-attestation; on 2026-10-06, FR-01 self-attested that FeDril LLC is the registered legal entity and contracting name | Contract-name clarification is resolved. Documentary formation/authority evidence and FeDril LLC's relationship to the domain remain unverified. Domain ownership does not make the current personal Azure account business-managed. |
| Current Azure sign-in identity | Protected evidence reference; individual observations and source details are retained in the protected original. | Addresses are intentionally excluded from Git. The target business tenant must use governed organizational identities and recovery controls. |
| FR-01 may approve a dedicated storage resource | Yes, self-attested | Establishes proposed management authority, not an approved cost ceiling, purchase, or completed change approval. |
| FR-01 may administer the resource | Yes, self-attested | Proposed administrator only. Least privilege, MFA, recovery access and separation-of-duties review remain required. |
| Permitted Azure region | East US 2 / East US | Estimate uses East US 2 as preferred and East US as fallback. Contract, residency, service availability and qualified review remain required before selection. |
| Architecture and cost estimate | Approved for bounded corporate implementation | The original estimate-only authorization is complete. The 2026-10-06 decision authorizes only the new corporate setup within the inclusive ceiling; protected evidence movement and acceptance remain unauthorized. |
| Current budget ceilings | Approved at $55 per month total recurring; $25 aggregate upfront inclusive of taxes and mandatory fees under FR-01's subsequent 2026-10-13 amendment | Identity $15 plus Azure services $40: storage/recovery $10, logs $10, network $10 and contingency/tax $10. Azure budget alerts are monitoring only. Purchase, binding terms and provider creation retain separate gates. |
| Continuity or transfer arrangement | Available in principle by FR-01 self-attestation | A second administrator/recovery arrangement is available, but identities, custody, authentication, transfer method and emergency billing/administration operation are unconfigured and untested. |
| Independent Security reviewer | Unassigned / unverified | No independent approval or review may be inferred. |

## Proposed Architecture

The preferred candidate remains a dedicated standard general-purpose v2 Azure Storage account using a flat namespace and Hot ZRS in a new FeDril LLC-controlled Workforce SOC 2 tenant. The existing Workforce account/Microsoft Customer Agreement context is a verified bootstrap only, not the target tenant. The evidence boundary must remain separate from both External customer tenants and existing production resources; the current Production subscription must not be selected, transferred or attached, and its directory must not be altered. An untouched new-subscription form auto-populated the corporate Microsoft Customer Agreement billing scope, Azure Plan, current Workforce directory and company-admin owner, and Review validation passed. Advanced displayed the root management group while Review summarized `None`. Microsoft documents that new subscriptions default to the tenant root unless the hierarchy setting specifies another default management group, so this does not prove absent inherited governance. A separate read-only check then reported that the company administrator lacked permission to access the Tenant Root Group and that the visible subscription's parent access was `Not Authorized`. Effective default placement, inherited policies/initiatives and potential billable automatic deployment effects remain unavailable to this principal. Microsoft's generic pay-as-you-go offer states no upfront commitment, but the specific Microsoft Customer Agreement/Azure Plan terms, creation and target-evidence-tenant linkage remain unverified.

| Area | Proposed design | Approval or verification still required |
| --- | --- | --- |
| Boundary | Dedicated resource group and storage account used only for FeDril corporate SOC 2 evidence; no customer application containers or application identities | Naming, subscription placement, legal account owner and production-boundary review |
| Region | East US 2 preferred; East US fallback | Provider availability, residency/contract review and final management approval |
| Redundancy | Hot ZRS preferred for a small active evidence set; Hot LRS is the lower-cost comparison | Availability requirement, regional service support and budget decision |
| Identity | Microsoft Entra authentication; named individual accounts; no shared accounts; disable Shared Key authorization; disallow anonymous blob access | Protected evidence reference; individual observations and source details are retained in the protected original. |
| Data roles | FR-01 proposed as evidence custodian with the narrowest required data role; independent reviewer proposed as read-only; control-plane administration separated when another competent person is appointed | Actual role assignments, separation-of-duties safeguard, quarterly access-review approval and independent review |
| Network | Deny unrestricted public access. Prefer a private endpoint when FeDril has an approved private administration path; otherwise separately review a selected-network/firewall design | Access path, DNS/VPN dependencies, lockout recovery, cost and Security reviewer approval |
| Integrity and recovery | Enable Blob versioning, blob soft delete, container soft delete and change feed; evaluate point-in-time restore for active containers; protect the account with an Azure resource lock | Capability is available in the design but unconfigured; approved retention values, lifecycle rules, recovery objectives and a successful synthetic recovery test remain required |
| Immutability | Consider version-level time-based immutability only for approved final artifacts after Legal/CPA review; do not lock drafts by default | Legal hold, correction, privacy/deletion and retention conflicts; test in a synthetic container first |
| Encryption | Azure platform encryption at rest and TLS in transit are the baseline; customer-managed keys are not proposed without a verified contractual or risk trigger | Live configuration verification and qualified assessment of whether platform-managed keys are sufficient |
| Logging | Enable applicable storage resource/data-access diagnostic categories and route them to an approved protected log destination with alerting and retention; do not rely solely on the existing 30-day workspace | Category coverage, ingestion volume, retention, reviewer access, alert delivery and Story 39.12 operation |
| Retention | Seven years remains a proposal for finalized assurance records, not a platform-wide setting or universal requirement; separate short recovery windows from long evidence retention | Qualified CPA/Legal review, contract/legal-hold rules, data minimization, lifecycle implementation and disposal test |
| Git boundary | Store only opaque evidence IDs, versions, dates, status and sanitized conclusions in Git | Protected index and independent sample review |

## Proposed Configuration And Acceptance Gate

No item below is currently implemented for this new boundary. Before approval, the change record must define rollout and rollback and the administrator must verify:

Protected evidence reference; individual observations and source details are retained in the protected original.

## Current Authorized Monthly Allocation

| Category | Monthly ceiling | Scope limit |
| --- | ---: | --- |
| Identity | $15 | Entra Free is the initial runtime target. The reviewed P1 $168-plus-tax annual upfront commitment exceeds the current $25 aggregate upfront cap |
| Storage and recovery | $10 | Dedicated Hot ZRS evidence storage, versioning and recovery services |
| Logs | $10 | Required diagnostic ingestion and retention |
| Network | $10 | Approved restricted administration path and related networking |
| Contingency and tax | $10 | Variance and tax within the same recurring setup; not a general-purpose reserve |
| **Total** | **$55** | Inclusive recurring maximum; Azure alerts do not enforce it. Current aggregate upfront cap is $25 including taxes and mandatory fees |

The former $15/month core ceiling is superseded only for this new corporate setup. It remains historical planning evidence and does not become blanket authority for the customer/home tenants, personal subscription, production resources, external services, professional services or one-time purchases.

## Historical Retail Cost Model

This is a management-planning estimate, not an Azure quote. Prices were queried from the public Azure Retail Prices API on 2026-09-20 in USD for East US 2 consumption pricing. Actual charges depend on agreement, taxes, service availability, data volume, transactions, versions, change feed, log categories, retention, egress, restores, networking and support.

Observed unit rates for standard flat-namespace General Block Blob v2 were:

| Meter | Observed retail rate |
| --- | ---: |
| Hot LRS data stored, first volume tier | $0.0184 per GB-month |
| Hot ZRS data stored, first volume tier | $0.0230 per GB-month |
| Hot LRS write operations | $0.05 per 10,000 |
| Hot ZRS write operations | $0.0625 per 10,000 |
| Hot ZRS read operations | $0.004 per 10,000 |
| Analytics Logs ingestion | $2.76 per GB where the paid meter applies |
| Analytics Logs retention | $0.12 per GB-month where the paid meter applies |

The API can return multiple tier or special-price rows. The model uses the first-volume paid rates and does not assume free allowances. A configured Azure Pricing Calculator estimate and the actual FeDril agreement must supersede these observations before purchase.

### Planning scenarios

| Scenario | Assumptions | Approximate recurring planning range | Exclusions |
| --- | --- | ---: | --- |
| Small protected store | 5 GB logical evidence, up to 3x effective retained data after versions/change feed, Hot ZRS, no more than 1 GB monthly paid log ingestion, low transaction volume | **$5–$10/month** | Private endpoint/private connectivity, egress, restore operations, support, taxes and reviewer labor |
| Moderate protected store | 25 GB logical evidence, up to 3x effective retained data, Hot ZRS, no more than 1 GB monthly paid log ingestion, low transaction volume | **$5–$15/month** | Same exclusions; high version churn or diagnostic volume can exceed this range |
| Hardened private-access store | Either storage scenario plus private endpoint and required private administration path | **Quote separately before approval** | Private Link has endpoint-hour and data-processing charges; VPN/DNS/connectivity may dominate the storage cost |

The storage-only arithmetic is small: 15 GB effective Hot ZRS storage is approximately $0.35/month, and 75 GB is approximately $1.73/month, before operations and supporting services. The historical management envelope for the core store and low-volume logging was **$5–$15 per month ($60–$180 per year)**, excluding private networking and every professional-service category. It remains useful planning evidence but is not the current authorization. The 2026-10-06 decision supersedes it only with the inclusive $55/month allocation for the new corporate setup.

On 2026-10-06, a sanitized East US 2 public Retail Prices API recheck reconfirmed the Hot ZRS tier-zero primary storage rate of $0.023 per GB-month, writes at $0.0625 per 10,000 and reads at $0.004 per 10,000. It also reconfirmed paid Analytics Logs ingestion at $2.76 per GB and retention at $0.12 per GB-month. A returned free-tier row is not assumed available; the actual corporate agreement and configured services supersede public retail observations.

Proposed internal effort is 6–12 founder hours for configuration, documentation and synthetic testing plus 2–4 independent-review hours. These are planning assumptions, not committed capacity; the founder's earlier 50-hour weekly commitment expired and must be renewed.

## Decision And Evidence Conclusions

| Dimension | Conclusion | Basis | Limitation |
| --- | --- | --- | --- |
| Design | Partial / ready for qualified review | Dedicated corporate boundary, identity, network, integrity, recovery, logging, retention, budget and test requirements are defined; FR-01 self-attested FeDril LLC's registered legal entity and contracting name | Documentary entity/authority evidence, FeDril LLC's domain relationship, exact network path, retention and qualified review remain unresolved |
| Implementation | Authorized / pending | Company-admin/Microsoft Customer Agreement bootstrap and wizard access are verified, but ownership-transition evidence remains incomplete and no new Workforce tenant, dedicated resource group, resource or configuration was created | Wizard access does not prove independent tenant creation, final governance linkage, exact charges or configured controls |
| Operating evidence | Unavailable | No save/read/deny/recover/audit or access-review cycle has occurred | No control effectiveness, protected-custody or readiness conclusion is permitted |

Current disposition: **Authorized for bounded corporate setup / implementation pending.** Management responsibility is sufficient for empty preparation; use the existing company-admin account only to bootstrap a separate new Workforce SOC 2 tenant. The bounded root read/revocation is complete; it does not prove future effects or actual subscription placement. The new-subscription form was reachable and passed Review validation with corporate Microsoft Customer Agreement scope and current Workforce directory/owner defaults. Before each final `Create`, verify the specific agreement/offer terms, actual checkout/due-now total across the setup within the current $25 aggregate upfront cap including taxes and mandatory fees, complete recurring costs within $55/month total, governance linkage and action-time placement; obtain the runbook's exact confirmation. License purchase and binding terms require separate approval. After creation, verify actual management-group placement and inherited controls before relying on isolation. The staged tenant wizard still requires a subscription and resource group, and linkage from the proposed new subscription to the separate target evidence tenant remains unverified. The existing Production subscription must not be selected, transferred or attached, and its directory must not be altered. No protected migration, Production change or External customer route is authorized. `SOC2-39.2-T03` remains blocked for acceptance by incomplete independent documentary/recovery evidence, unconfigured continuity and controls, unapproved retention, absent synthetic verification and unavailable independent review.

## Sources

- Microsoft, [Secure your Azure Blob Storage](https://learn.microsoft.com/en-us/azure/storage/blobs/secure-blobs)
- Microsoft, [Authorize access to blobs using Microsoft Entra ID](https://learn.microsoft.com/en-us/azure/storage/blobs/authorize-access-azure-active-directory)
- Microsoft, [Configure anonymous read access for containers and blobs](https://learn.microsoft.com/en-us/azure/storage/blobs/anonymous-read-access-configure)
- Microsoft, [Soft delete for blobs](https://learn.microsoft.com/en-us/azure/storage/blobs/soft-delete-blob-overview)
- Microsoft, [Change feed support in Azure Blob Storage](https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-change-feed)
- Microsoft, [Azure Blob immutable storage overview](https://learn.microsoft.com/en-us/azure/storage/blobs/immutable-storage-overview)
- Microsoft, [Azure Retail Prices API](https://learn.microsoft.com/en-us/rest/api/cost-management/retail-prices/azure-retail-prices)
- Microsoft, [Azure Private Link pricing](https://azure.microsoft.com/en-us/pricing/details/private-link/)
- Microsoft, [Add a custom domain to Microsoft Entra ID](https://learn.microsoft.com/en-us/entra/fundamentals/add-custom-domain)
- Microsoft, [Transfer a subscription to another Microsoft Entra directory](https://learn.microsoft.com/en-us/azure/role-based-access-control/transfer-subscription)
- Microsoft, [Protect the resource hierarchy and set the default management group](https://learn.microsoft.com/en-us/azure/governance/management-groups/how-to/protect-resource-hierarchy)
- Microsoft, [Azure pay-as-you-go account](https://azure.microsoft.com/en-us/pricing/purchase-options/azure-account?icid=payg)

Provider documentation supports capability and public-price assumptions only. It does not prove FeDril ownership, configuration, operation, contract terms or control effectiveness.
