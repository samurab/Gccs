# No-CUI Policy Statement

Document type: customer-facing policy statement draft.

Review requirement: legal, contracting, and security owner review required before publication or contract attachment.

## Policy Summary

FeDril currently operates under a No-CUI / compliance management only MVP posture. Customers may use FeDril to organize compliance workflows, obligations, task ownership, evidence metadata, reports, and audit history, but must not upload, paste, import, attach, store, or process real Controlled Unclassified Information in FeDril.

## Scope

This policy applies to:

- Product onboarding.
- Contract metadata entry.
- Evidence upload and evidence metadata workflows.
- Notes, comments, task descriptions, report fields, and support messages.
- Demo tenants, pilot tenants, and production MVP tenants.
- Screenshots, exports, reports, and customer support communications.

## Prohibited Data

Customers must not enter or upload:

- Real CUI.
- Classified information.
- ITAR or export-controlled technical data.
- Sensitive government-furnished information.
- Credentials, passwords, secrets, private keys, or unrestricted security logs.
- Payroll records, SSNs, bank data, tax data, health data, disability data, or sensitive incident details.
- Any production customer data unless separately approved as non-sensitive and in scope.

## Allowed Data Examples

Customers may use:

- Synthetic data.
- Redacted records with prohibited data removed.
- Non-sensitive contract metadata.
- Non-sensitive company profile metadata.
- Obligation ownership, due dates, and workflow status.
- Evidence metadata that does not reveal prohibited information.
- Non-sensitive checklist, policy, or training metadata.

## Customer Acknowledgement

Before evidence upload or contract document upload is enabled, customers must acknowledge the current No-CUI notice in the application. The acknowledgement is also displayed in contract and evidence workflows so customers see the data boundary before discussing or organizing evidence.

> I understand that FeDril is currently No-CUI / compliance management only. I will not upload, paste, import, attach, store, or process real CUI, classified information, ITAR/export-controlled data, sensitive government-furnished information, credentials, payroll records, SSNs, health data, disability data, sensitive incident details, or other prohibited sensitive information in FeDril.

## Product Handling

FeDril should maintain user-facing guardrails that:

- State the No-CUI posture during onboarding.
- Display the No-CUI acknowledgement in contract and evidence workflows.
- Require acknowledgement before upload-related workflows where supported by the current application.
- Require per-file or workflow attestation where supported.
- Preserve audit history for data-handling acknowledgements and upload decisions.
- Route suspected prohibited-data incidents through support escalation.

## Support Handling

Customers must not send prohibited data to support. If a customer suspects prohibited data was entered:

1. Stop using the affected workflow.
2. Do not include the prohibited content in support messages.
3. Notify support using a non-sensitive description.
4. Follow FeDril support instructions for containment and disposition.

## Future Posture

Any future CUI-ready operation must be separately approved and must include appropriate architecture, customer terms, operational controls, support handling, shared responsibility documentation, and launch authorization. This policy does not authorize real CUI handling.

## Required Disclaimer

This policy statement describes the current FeDril MVP data posture. It is not legal advice, a government determination, or a certification of compliance. Customers remain responsible for determining their data-handling obligations with qualified advisors.
