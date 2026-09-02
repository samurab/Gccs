# FeDril Compute Cost And Pricing Model

Document type: internal investor-readiness and pricing analysis.

Status: management model, not audited financial guidance. Use for investor discussion only after reconciling Azure retail-rate estimates to actual invoices and replacing capacity assumptions with load-test evidence.

As of: 2026-08-28.

## Critique & Flaws

- A single low-priced unlimited tier fails to absorb FeDril's always-on Azure baseline at very low tenant counts and exposes margin to heavy users because the application does not meter or enforce storage, scans, exports, or other usage quantities.
- Seat count is a weak compute proxy. Stored bytes, uploaded files, malware-scan duration, report and PDF generation, egress, database load, and any future AI actions are more directly related to platform cost.
- Treating current clause extraction as a token cost would misstate the architecture. The implemented extractor reads supported text content and performs deterministic local matching; the repository does not configure a metered LLM provider for that workflow.

## The Correct Solution

Price on customer value and use activity limits as anti-abuse and capacity-planning guardrails. Keep current and future compute separated:

| Model layer | Status | Treatment |
| --- | --- | --- |
| Shared Azure platform | Implemented / observed | Model as fixed capacity allocated across tenants. |
| Storage, egress, email, and transactions | Implemented services; profile volumes assumed | Model as variable cost using explicit light and heavy profiles. |
| AI token cost | Planned scenario only | Show separately; do not describe it as current FeDril compute. |
| Tier quantities and overages | Planned | Do not claim they are enforced until server-side metering and billing controls exist. |
| Advisor multi-client packaging | Planned | Do not sell it as an automated product capability until the workflow and tenant-boundary tests are proven. |

The auditable workbook is the calculation source:

- `outputs/compute_cost_pricing_20260828/fedril-compute-cost-and-pricing-model.xlsx`

## Current Production Footprint

The live Azure subscription was inspected read-only on 2026-08-28.

| Component | Observed configuration | Modeled monthly cost | Evidence status |
| --- | --- | ---: | --- |
| API hosting | Linux App Service P0v4; production app shares the staging App Service plan | $53.29 | Observed SKU; Azure retail rate |
| PostgreSQL compute | Flexible Server B2s | $49.64 | Observed SKU; Azure retail rate |
| PostgreSQL storage | 32 GB provisioned | $3.68 | Observed quantity; Azure retail rate |
| Redis | Standard C1 | $100.74 | Observed SKU; Azure retail rate |
| ClamAV | Azure Container Instance, 1 vCPU and 2 GB, continuously requested | $36.06 | Observed resources; Azure retail rates |
| Static web app | Standard | $9.00 | Observed SKU; Azure retail rate |
| Private endpoints | PostgreSQL, Redis, and Blob | $21.90 | Observed count; $0.01/hour planning assumption |
| Monitoring/logging reserve | Model reserve | $10.00 | Assumption; replace with invoice actual |
| **Total fixed platform** |  | **$284.31/month** | Retail-rate estimate |

Azure Cost Management returned HTTP 429 during preparation, so this is not an invoice reconciliation. Taxes, discounts, credits, support plans, and negotiated rates are excluded.

## Lightweight And Heavy Profiles

The profiles are planning assumptions, not measured customer averages or enforced limits.

| Driver per tenant / month | Lightweight | Heavy |
| --- | ---: | ---: |
| Users | 3 | 15 |
| Active contracts | 5 | 30 |
| Stored data | 5 GB | 50 GB |
| Uploaded files | 25 | 250 |
| Report generations | 20 | 100 |
| PDF exports | 10 | 100 |
| Internet egress | 1 GB | 20 GB |
| Relative compute weight | 1x | 6x |
| Current modeled compute cost at 25 tenants / 20% heavy mix | **$5.88** | **$36.95** |
| Future AI scenario | $0.21 | $4.05 |
| Current plus future AI scenario | **$6.09** | **$41.00** |

The future AI scenario assumes GPT-5.4 mini at $0.75 per million input tokens and $4.50 per million output tokens, 20 actions for a lightweight tenant, and 200 actions for a heavy tenant. It is a scenario, not an implemented product claim.

## Revised Pricing Tiers

| Tier | Recommended price | Annual prepay | Modeled profile | Current compute GM | Future-AI compute GM | Product status |
| --- | ---: | ---: | --- | ---: | ---: | --- |
| Starter | **$149/month** | **$1,490/year** | Lightweight | 96.1% | 95.9% | Planned commercial packaging |
| Team | **$399/month** | **$3,990/year** | Heavy | 90.7% | 89.7% | Planned commercial packaging |
| Advisor | **Starting at $999/month** | **Starting at $9,990/year** | Three heavy workspaces | 88.9% | 87.7% | Planned; automated multi-client packaging is not implemented |

Recommended packaging targets:

| Tier | Users | Active contracts | Storage | Uploads / month | Reports / month |
| --- | ---: | ---: | ---: | ---: | ---: |
| Starter | 3 | 5 | 5 GB | 25 | 20 |
| Team | 15 | 30 | 50 GB | 250 | 100 |
| Advisor | 45 | 90 | 150 GB | 750 | 300 |

These quantities are not currently enforced. Until metering exists, use them in order forms as fair-use and custom-quote triggers only after finance and counsel review. Sustained usage above three times a target, or any request for dedicated capacity, should trigger a custom plan review rather than an automatic overage claim.

The $750 guided pilot remains a buyer-validation hypothesis. Its economics are dominated by founder delivery and onboarding labor, not Azure compute. Do not use compute margin to justify the pilot price without also modeling delivery hours.

## Scale Sensitivity

Assumptions: 80% Starter/lightweight, 20% Team/heavy, $199 blended monthly revenue per tenant, 50 weighted units in the baseline capacity block, and $175 for each modeled incremental capacity block.

| Tenants | MRR | Current compute cost / tenant | Current compute GM | Future-AI compute GM |
| ---: | ---: | ---: | ---: | ---: |
| 5 | $995 | $57.59 | 71.1% | 70.6% |
| 10 | $1,990 | $29.15 | 85.3% | 84.9% |
| 25 | $4,975 | $12.10 | 93.9% | 93.4% |
| 50 | $9,950 | $9.91 | 95.0% | 94.5% |
| 100 | $19,900 | $8.82 | 95.6% | 95.1% |
| 250 | $49,750 | $8.16 | 95.9% | 95.4% |

The 50-weight-unit capacity assumption is not load-test evidence. The scale table is a sensitivity model, not a capacity promise.

## Investor Answer

> FeDril is currently a fixed-capacity SaaS, not an AI-token business. The observed Azure footprint models to about $284 per month before support and customer-success labor. At 25 tenants with an 80% light and 20% heavy mix, current compute is about $5.88 for a light tenant and $36.95 for a heavy tenant. At $149 and $399 pricing, that produces about 94% blended compute gross margin. Under a separately labeled future AI scenario, token cost adds about $0.21 per light tenant and $4.05 per heavy tenant. The near-term risk is not token expense; it is unmetered usage and an unproven capacity curve. We cross an 85% compute-margin target at roughly 10 tenants in the model, but we still need invoice reconciliation and load testing.

If asked what is excluded:

- Founder and customer-success delivery.
- Onboarding and support labor.
- Qualified expert review of compliance content.
- Sales, payment processing, taxes, credits, and Azure support plans.
- Dedicated customer environments, GovCloud, FedRAMP, or real-CUI architecture.

## Rationale

The model separates fixed capacity from marginal activity. At the 25-tenant reference mix, total weighted usage is:

`25 × ((80% × 1) + (20% × 6)) = 50 weighted units`

Fixed allocation is therefore:

- Lightweight: `$284.31 ÷ 50 × 1 = $5.69` before variable activity.
- Heavy: `$284.31 ÷ 50 × 6 = $34.12` before variable activity.

Storage, egress, email, and transaction estimates produce total current compute of $5.88 and $36.95. The proposed prices preserve compute margin while leaving room for non-compute COGS, which must be measured separately.

## Hidden Risks, Edge Cases, And Dependencies

- Actual Azure invoice data was unavailable because the Cost Management API returned HTTP 429. Retail prices can differ from billed rates.
- Production and staging share an App Service plan. That complicates cost allocation and creates a shared-capacity and shared-failure dependency.
- No production load test proves that 50 weighted units fit one capacity block or that an incremental block costs $175.
- The application does not currently meter tier usage or enforce the proposed commercial quantities. Heavy usage could go undetected.
- Redis, database, scanning, and report-export bottlenecks may scale at different rates; a single blended capacity block is a simplification.
- Compute gross margin is not total gross margin. Human delivery may dominate the $750 guided pilot and early subscriptions.
- Any future AI workflow must preserve the No-CUI posture, draft-only/review-gated AI policy, tenant isolation, source traceability, and audit requirements.
- Advisor packaging depends on proven multi-client workflows and server-authoritative tenant isolation. Do not claim availability based on a pricing row alone.

## Evidence And Next Actions

1. Reconcile the workbook to Azure invoices for two complete billing months.
2. Add per-tenant telemetry for stored bytes, upload count and size, malware-scan duration, report and PDF generation, egress, database time, queue time, and failures.
3. Run representative light/heavy load tests and identify independent scale triggers for App Service, PostgreSQL, Redis, Blob Storage, and ClamAV.
4. Implement server-authoritative usage meters before publishing hard quantities or overage terms.
5. Add support, onboarding, expert-review, and founder-delivery labor to a full COGS model.
6. Review the revised pricing with founder, finance, and counsel before publication or invoicing.

## Sources

- [Azure Retail Prices API](https://prices.azure.com/api/retail/prices)
- [Azure App Service for Linux pricing](https://azure.microsoft.com/en-us/pricing/details/app-service/linux/)
- [Azure Database for PostgreSQL pricing](https://azure.microsoft.com/en-us/pricing/details/postgresql/flexible-server/)
- [Azure Cache for Redis pricing](https://azure.microsoft.com/en-us/pricing/details/cache/)
- [Azure Container Instances pricing](https://azure.microsoft.com/en-us/pricing/details/container-instances/)
- [Azure Static Web Apps pricing](https://azure.microsoft.com/en-us/pricing/details/app-service/static/)
- [Azure Blob Storage pricing](https://azure.microsoft.com/en-us/pricing/details/storage/blobs/)
- [Azure Private Link pricing](https://azure.microsoft.com/en-us/pricing/details/private-link/)
- [OpenAI GPT-5.4 mini pricing](https://openai.com/index/introducing-gpt-5-4-mini-and-nano/)

