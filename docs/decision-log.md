# Decision Log

This log records major GCCS product, architecture, security, compliance content, and release decisions. It is a governance artifact, not legal advice or production approval by itself.

## Entry Template

```text
Decision:
Date:
Context:
Options considered:
Chosen approach:
Risks accepted:
Mitigations:
Approver:
Review date:
```

## Decision: No-CUI MVP Launch Posture

Date: 2026-06-24

Context:

GCCS is preparing for the Production Readiness phase of the MVP. The product includes synthetic CUI-ready demonstration workflows for sandbox validation, but the MVP launch must not accept real customer CUI. Production launch depends on clear customer-facing posture, server-side tenant mode enforcement, staging evidence, content governance, support readiness, and formal approvals.

Options considered:

- Launch as No-CUI / compliance management only with synthetic CUI-ready demonstration workflows.
- Launch as production `CuiReady` for selected tenants.
- Remove synthetic CUI-ready demonstration workflows from the MVP.

Chosen approach:

Launch the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**. `DemoSandbox` may use synthetic or redacted demonstration data. `NoCui` production tenants must block real CUI. Future `CuiReady` operation requires separate approval before any real CUI can be accepted.

Explicit exclusion:

Real customer CUI remains prohibited until a future `CuiReady` posture is approved with architecture, customer terms, shared responsibility matrix, operating controls, support model, evidence handling controls, and required product, engineering, security, compliance content, and legal or contracting advisor signoff.

Risks accepted:

- Synthetic demo workflows may be confused with authorization to store real CUI.
- Future `CuiReady` implementation may leak into production claims.
- Production malware scanning remains a launch decision dependency.
- Staging restore evidence remains a production launch dependency.

Mitigations:

- Enforce tenant mode boundaries server-side for `DemoSandbox`, `NoCui`, and future `CuiReady`.
- Keep customer-facing copy aligned to No-CUI / compliance management only.
- Review product claims before launch.
- Build and attach the launch evidence package.
- Finalize support runbooks for suspected CUI and prohibited uploads.
- Track production blockers in `docs/production-readiness-checklist.md`.
- Execute the Production Readiness roadmap in `docs/production-readiness-roadmap.md`.

Approver:

Pending product owner, engineering lead, security owner, compliance content owner, customer success/support owner, and legal or contracting advisor approval.

Approval status:

| Required approver | Current status | Launch blocker while pending |
| --- | --- | --- |
| Product owner | Pending | Yes |
| Engineering lead | Pending | Yes |
| Security owner | Pending | Yes |
| Compliance content owner | Pending | Yes |
| Customer success/support owner | Pending | Yes |
| Legal or contracting advisor | Pending | Yes |

Review date:

Before MVP launch candidate tagging.
