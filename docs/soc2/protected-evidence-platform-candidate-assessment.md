

# Protected Evidence Platform Candidate Assessment

Status: **SharePoint rejected / Azure design retained / current subscription ineligible / selection blocked**
Assessment ID: `SOC2-EVIDENCE-PLATFORM-CANDIDATES-0.1`
Assessment date: 2026-09-19 EDT
Last updated: 2026-09-20 EDT
Decision owner: Founder authority FR-01
Task mapping: `SOC2-39.2-T03`

## Verified Starting Facts

- FeDril uses Azure and Microsoft Entra for production infrastructure and authentication-related boundaries.
- Production Azure Blob containers named for application evidence, exports and reports exist, but they serve the application boundary. They are not approved as the corporate SOC 2 evidence workspace.
- Repository references to Microsoft 365 and SharePoint are synthetic UAT/customer-profile data and do not prove that FeDril owns or licenses Microsoft 365 or SharePoint Online.
- No verified FeDril Google Workspace, Google Shared Drive or comparable business document-management account was found in the supplied repository evidence.
- FR-01 states that FeDril does not own a business-managed Microsoft 365 tenant with SharePoint Online. The separately supplied “FR-01 administrative authority: Yes” response is not treated as SharePoint evidence because the prerequisite tenant does not exist; SharePoint site-creation permission is therefore not applicable, not merely unverified.
- `AZ-SOC2-INV-2026-09-19` verifies that the current Azure session can administer one enabled subscription at Owner scope and can query an associated billing account. This proves technical administration and billing visibility, not legal business ownership, contract authority, budget approval or exclusive control.
- FR-01 states that the existing Azure subscription is legally and financially owned by FR-01 personally. Protected ownership/billing evidence remains unavailable. This self-attested personal ownership fails the documented business-managed-account gate and creates unmitigated continuity risk.
- FR-01 states that Sierra Labs LLC owns `fedril.com` and that the current Azure account uses FR-01's personal email. The email address is excluded from Git. Registrar, entity, authority and account records remain unavailable in a protected workspace.
- FR-01 approves a $15/month recurring ceiling for core protected Azure storage, excluding private networking. This is bounded budget authority, not authorization to provision in the personally owned subscription or to purchase excluded services.
- Continuity/transfer arrangements and the independent Security reviewer remain unverified or unassigned.
- FR-01 self-attests authority to approve and administer a new dedicated storage resource, permits East US 2 or East US for estimate purposes, and authorizes architecture/cost estimate collection. These inputs do not approve provisioning, recurring spend or role assignments.
- The two existing application storage accounts require HTTPS/TLS and reject anonymous blob access, but neither had blob versioning, blob/container soft delete, change feed or point-in-time restore enabled; shared-key access remained enabled. They are not acceptable substitutes for a separately designed evidence boundary.
- `SOC2-AZURE-EVIDENCE-ESTIMATE-0.1` defines a sanitized dedicated-boundary design, and FR-01 approves a $15/month core ceiling excluding private networking and professional services. The current subscription's personal ownership is self-attested, but no candidate has eligible business ownership, configured identity/network policy, approved audit retention/recovery, reviewer population or approved contract terms.

## Candidate Comparison

| Candidate | Potential fit | Required verification | Material concern | Current disposition |
| --- | --- | --- | --- | --- |
| Microsoft 365 SharePoint Online restricted site | Business-owned collaborative documents, named membership and reviewer access; Microsoft documents sharing audit events when applicable | Existing business tenant/license, FR-01 administrative authority, Entra MFA/conditional access, external-sharing policy, versioning, audit availability/retention, recovery, region, contract and cost | FeDril does not own the prerequisite business Microsoft 365 tenant | **Rejected for the current selection; do not purchase solely to preserve this candidate order** |
| Dedicated Azure Storage account/container | Aligns with verified Azure administration; can support RBAC, private access, versioning, soft delete, change feed and optional immutability when explicitly configured | Business-controlled subscription, separate resource/boundary, identity roles, private access, data-operation logs and retention, versioning, blob/container soft delete, backup/account-deletion protection, reviewer workflow and lifecycle policy | Current subscription is personally owned; continuity is unverified; high configuration risk; account deletion needs separate protection; immutability/retention choices can prevent correction or deletion | **Architecture reusable, but current subscription is ineligible; establish business ownership before provisioning** |
| Google Workspace Shared Drive | Organization-owned files can persist beyond an individual account; administrators/managers can restrict external and non-member sharing where supported | Existing business Workspace/license, organizational ownership, 2-step verification, admin roles, sharing restrictions, audit logs/retention, version/recovery behavior, region, contract and cost | No evidence FeDril owns Workspace; adding another identity/vendor boundary increases governance burden | **Consider only if an appropriate business Workspace already exists** |
| Existing FeDril production application Blob containers | No new provider | Would require proving isolation from tenants/application identities, reviewer usability, retention, access and audit controls | Mixes corporate assurance evidence with the customer-facing application boundary and existing service identities; increases disclosure and availability coupling | **Excluded from selection as currently designed** |
| Personal drive/account or public Git | Convenient | None can satisfy the approved business-account boundary | Personal ownership, weak continuity/governance or public disclosure | **Prohibited** |

## Conditional Selection Order

1. Record SharePoint as rejected because FeDril does not own the prerequisite business tenant.
2. Record the current subscription as personally owned and therefore ineligible under the business-managed gate. Apply `SOC2-AZURE-BUSINESS-TRANSITION-0.1` to establish a Sierra Labs LLC-controlled Entra/billing/subscription boundary without changing the production subscription.
3. Review the completed `SOC2-AZURE-EVIDENCE-ESTIMATE-0.1` architecture/cost estimate for a new, dedicated Azure Storage evidence boundary in the new business subscription. Do not create the tenant, change DNS or provision resources without separate bounded authorization.
4. Evaluate Google Shared Drive only if FeDril already owns an appropriate business Workspace account or a documented business reason justifies adding the provider.
5. Reject personal storage, public Git and the existing production application containers for this purpose.

This ordering is a cost- and boundary-minimization recommendation, not a platform approval or purchase authorization.

## Approval Evidence Required

For the selected candidate, preserve in the protected workspace or an approved procurement record:

- Legal account/subscription owner and administrator population.
- License/SKU, price, billing period and renewal terms.
- Contract, data-processing terms, subprocessors and hosting/residency facts.
- MFA and recovery/break-glass configuration.
- Sharing defaults, external-sharing restrictions and reviewer-access process.
- Version/integrity, audit, retention, legal-hold, backup and recovery capabilities.
- Configuration screenshots/exports or provider/API evidence with collection date and reviewer.
- Approved retention and deletion behavior.
- Save/read/deny/recover test results using synthetic content.
- Independent reviewer conclusion and unresolved limitations.

## Current Decision

No platform is selected. `SOC2-39.2-T03` remains **Blocked**. SharePoint is rejected for the current selection. The Azure architecture/cost estimate, permitted candidate regions, Sierra Labs LLC/`fedril.com` relationship, FR-01's personal Azure ownership and the approved $15/month core ceiling are recorded as management inputs. `SOC2-AZURE-BUSINESS-TRANSITION-0.1` recommends a new business-controlled Azure boundary and defers production transfer. No continuity arrangement, Azure/DNS change, reviewer workflow, retention or recovery/audit configuration is approved. Existing application storage is excluded as currently configured. Do not publish subscription IDs, tenant IDs, email addresses, URLs, credentials or recovery information.

## Authoritative Capability Sources

- Microsoft, [Azure Blob immutable storage overview](https://learn.microsoft.com/en-us/azure/storage/blobs/immutable-storage-overview)
- Microsoft, [Azure Blob soft-delete overview](https://learn.microsoft.com/en-us/azure/storage/blobs/soft-delete-blob-overview)
- Microsoft, [Azure Blob security and recovery guidance](https://learn.microsoft.com/en-us/azure/storage/blobs/secure-blobs)
- Microsoft, [Add a custom domain to Microsoft Entra ID](https://learn.microsoft.com/en-us/entra/fundamentals/add-custom-domain)
- Microsoft, [Transfer a subscription to another Microsoft Entra directory](https://learn.microsoft.com/en-us/azure/role-based-access-control/transfer-subscription)
- Microsoft, [Manage emergency access accounts](https://learn.microsoft.com/en-us/entra/identity/role-based-access-control/security-emergency-access)
- Microsoft, [Microsoft 365 sharing auditing](https://learn.microsoft.com/en-us/purview/audit-log-sharing)
- Google, [Shared Drive access controls](https://support.google.com/a/users/answer/12380484)
- Google, [Shared Drive organization ownership](https://support.google.com/a/users/answer/7212025)

Capability documentation does not prove that FeDril owns, licenses, configures or operates any candidate control.
