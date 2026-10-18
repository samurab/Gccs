

Publication classification: **Sanitized public reference**. Complete originals and original SHA-256 hashes remain in protected `release-scope-lookups-20261018-v1/primary-source-snapshot-v1`. This counterpart records scope and traceability; it does not establish workforce control effectiveness, review completion or acceptance.

# Azure Read-Only SOC 2 Evidence Inventory

Status: **Sanitized point-in-time inventory / independent review required**

Evidence ID: `AZ-SOC2-INV-2026-09-19`

Collection time: 2026-09-20 02:00 UTC

Repository context: commit `87f02890ffd6ce5931696f7dad4d608fde26b655` on `codex/retry-staging-release-smoke`

Collector: automated read-only Azure CLI inspection under the currently authenticated session

CLI version: 2.87.0

This file is a sanitized Git index, not raw audit evidence. It intentionally omits tenant, subscription, resource, principal, network, address, endpoint and billing identifiers; secret values; configuration values that could disclose credentials; customer content; and raw log records. No Azure resource, identity, role, policy, setting, log, or data was modified.

## Scope And Reliability

The authenticated session could enumerate one enabled subscription and its visible control-plane resources. The signed-in identity had a subscription-scope Owner assignment, and a billing-account query was available. These facts demonstrate current technical administration and billing visibility; they do not prove legal business ownership, contract authority, budget approval, or exclusive control.

The collection supports a point-in-time implementation assessment only. It does not establish that controls operated throughout a Type II period, that returned populations were complete outside the accessible scope, or that FeDril is SOC 2 compliant or audit ready. Azure is only one part of the candidate boundary; workforce, GitHub, application, vendor, customer-commitment, legal, privacy, physical, CPA and management controls require separate evidence.

## Sanitized Resource Boundary

- Four resource groups and 41 resources were visible across five Azure location classifications.
- Observed service families included web hosting, static web hosting, container execution, PostgreSQL, two cache technologies, storage, virtual networking, private endpoints and DNS, monitoring, application telemetry, communications/email, identity-related resources and AI services.
- No Key Vault, Recovery Services vault, Backup vault, container registry, Azure Firewall, application gateway, bastion, NAT gateway, public-IP resource or network security group was returned.
- Absence from this inventory is not automatically a control failure. Architecture relevance, provider support, inherited controls and compensating controls require review.

## Control Findings

| Area | Design evidence | Live implementation evidence | Operating evidence | Conclusion and limitation |
| --- | --- | --- | --- | --- |
| Identity and privileged access | Azure RBAC is available | Protected evidence reference; individual observations and source details are retained in the protected original. | Unavailable | Privileged authority is concentrated. MFA enforcement, authentication-method policy, joiner/mover/leaver activity, access reviews, sign-in review and break-glass operation were not proven. |
| Directory inventory | Entra directory objects and audit querying are available | Two user objects, no group objects, 122 service-principal objects and one active directory role were returned. Directory audit-log querying was available. | Unavailable | Protected evidence reference; individual observations and source details are retained in the protected original. |
| Policy governance | Azure Policy and resource locks are available controls | No policy assignments, custom policy definitions or resource locks were returned in the accessible subscription scope. | Unavailable | No preventive-policy or deletion-lock baseline was proven. Management-group coverage was not reliably retrieved. |
| Web workloads | HTTPS, TLS, managed identity and health settings are configurable | Two web apps enforced HTTPS, used managed identity, required TLS 1.2 and had always-on enabled. Public network access remained enabled; FTPS was not disabled; HTTP/2 and platform health-check paths were not configured. Classic platform authentication was not enabled. | Unavailable | Application-layer JWT/Entra authorization may exist and must be assessed separately; platform-auth state does not prove anonymous application access. No period availability or access population was reviewed. |
| PostgreSQL | Encryption-in-transit, firewall, backup and high-availability controls are configurable | Protected evidence reference; individual observations and source details are retained in the protected original. | Unavailable | Public networking and broad Azure-services access require Story 39.9 risk review. Restore success, backup-job history, failover, access population and configuration history were not inspected. |
| Storage | HTTPS, TLS, private access, shared-key restriction, versioning, soft delete, change feed and recovery controls are configurable | Both accounts required HTTPS and TLS 1.2 and disallowed anonymous blob access. One explicitly disabled public network access; the other's public-network value was unset. Shared-key access remained enabled on both. Neither had blob versioning, blob/container soft delete, change feed, point-in-time restore or infrastructure-encryption flags enabled. | Unavailable | Existing application storage is not suitable as the protected SOC 2 workspace without a separately approved boundary and controls. Deletion recovery, integrity and account-deletion protection were not demonstrated. |
| Cache | TLS and private-access controls are configurable | The standard cache disabled public access and its non-TLS port and required TLS 1.2. A second enterprise cache existed but was not separately inspected in depth. | Unavailable | Detailed identity, persistence, backup, patch, diagnostic and enterprise-cache settings remain unverified. |
| Network boundary | Virtual networks, private endpoints and private DNS are implemented | One virtual network, three private endpoints, three private DNS zones and three network interfaces were returned. No public-IP resource was returned. | Unavailable | Database and web public-access settings remain material even though private endpoints exist. Traffic paths, DNS resolution, service firewalls and effective network rules were not fully validated. |
| Monitoring and logging | Azure Monitor, Log Analytics, application telemetry, alerts and diagnostic settings are available | One Log Analytics workspace with 30-day retention, one application telemetry component, one enabled metric alert and two action groups were returned. One resource had a diagnostic setting; 27 returned none and 13 probes were unavailable or unsupported. No subscription diagnostic setting was returned. | Partial point-in-time event population only | A 90-day Activity Log query returned 2,067 records, including 35 failed-status records and one Security-category record. Raw events were not read. Counts do not establish incidents or control failures. Alert delivery, investigation, completeness, retention sufficiency and recurring review remain unverified. |
| Recovery and resilience | Native database backup and storage recovery features are available | Database retention was seven days; geo-redundancy and HA were disabled. Storage recovery features were disabled. No recovery/backup vaults were returned. | Unavailable | No approved RPO/RTO, production-equivalent restore, regional recovery, continuity exercise or reviewed backup population was demonstrated. |
| Security posture | Defender and Advisor interfaces were queried | Defender pricing status was unavailable. Advisor returned 32 recommendations and no Security-category recommendation in that returned set. | Unavailable | Advisor is not a complete security assessment, and zero returned Security recommendations is not a clean bill of health. Defender coverage cannot be inferred. |

## Evidence Quality And Access Limitations
Protected evidence reference; individual observations and source details are retained in the protected original.

## Required Follow-Up

1. Preserve an approved raw configuration export and collection manifest in the future protected workspace, with hashes, collector, time, scope and independent reviewer.
2. Complete Story 39.6 for MFA, Conditional Access, authentication methods, complete privileged identities, service principals, access reviews and joiner/mover/leaver evidence.
3. Complete Story 39.9 for public database and web paths, firewall/service-access rules, private endpoints, DNS and effective traffic-flow validation.
4. Complete Story 39.10 for application secrets, key ownership, rotation, workload identity and shared-key elimination without reading secret values into Git.
5. Complete Story 39.12 for resource-specific diagnostic coverage, log retention, alert delivery, triage, review populations and failure handling.
6. Complete Story 39.14 for approved RPO/RTO, backup coverage, immutable/recoverable evidence storage, restore tests, high availability and regional continuity.
7. Have an independent security reviewer assess the configuration conclusions and a qualified CPA determine their relevance to the authorized criteria and examination route.

## Authoritative Capability References

- Microsoft, [Azure Activity Log](https://learn.microsoft.com/en-us/azure/azure-monitor/platform/activity-log)
- Microsoft, [Microsoft Entra report retention](https://learn.microsoft.com/en-us/entra/identity/monitoring-health/reference-reports-data-retention)
- Microsoft, [Microsoft Entra diagnostic settings](https://learn.microsoft.com/en-us/entra/identity/monitoring-health/howto-configure-diagnostic-settings)
- Microsoft, [Azure RBAC role assignments with Azure CLI](https://learn.microsoft.com/en-us/azure/role-based-access-control/role-assignments-cli)
- Microsoft, [Azure Blob immutable storage](https://learn.microsoft.com/en-us/azure/storage/blobs/immutable-storage-overview)
- Microsoft, [Azure Blob soft delete](https://learn.microsoft.com/en-us/azure/storage/blobs/soft-delete-blob-overview)
- Microsoft, [Azure Blob security and recovery](https://learn.microsoft.com/en-us/azure/storage/blobs/secure-blobs)

Provider documentation describes capabilities; it does not prove FeDril configured or operated them.
