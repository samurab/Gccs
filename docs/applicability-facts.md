# Applicability Facts

Story 21.1 defines tenant-scoped facts used by the applicability engine. Unknown values are represented as `value: "unknown"` with `isUnknown: true`; they are not inferred as `false`.

| Fact key | Source record |
| --- | --- |
| `company.contractor_role` | Company profile contractor role |
| `company.naics` | Company NAICS code rows |
| `company.certification` | Company certification rows |
| `company.agency_customer` | Company profile agency customer list |
| `company.performance_location` | Company profile locations |
| `company.data_type` | Company profile data handling posture |
| `contract.agency` | Contract agency or prime name |
| `contract.type` | Contract kind |
| `contract.role` | Contract prime/sub relationship |
| `contract.performance_location` | Contract place of performance |
| `contract.data_type` | Contract data handling posture |
| `clause.citation` | Contract clause number |
| `subcontractor.role` | Subcontractor role description |
| `subcontractor.has_fci_access` | Subcontractor FCI access flag |
| `subcontractor.has_cui_access` | Subcontractor CUI access flag |

Each fact includes `tenantId`, `sourceType`, `sourceId`, and `lastUpdatedAt` when the source record has audit or reviewed metadata.
