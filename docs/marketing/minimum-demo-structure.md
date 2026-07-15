# Minimum Demo Structure

Document type: live demo script for first-customer discovery and guided pilot sales calls.

Data status: synthetic, redacted, or non-sensitive data only.

Required boundary: the demo must not use real CUI, classified information, ITAR/export-controlled data, sensitive government-furnished information, credentials, payroll, SSNs, health data, disability data, sensitive incident details, or unrestricted security logs.

## Demo Objective

Show that GCCS can replace one fragile compliance spreadsheet with a controlled readiness workspace where a contractor can see readiness status, open an obligation, assign an owner, update workflow status, link allowed evidence metadata, view source-backed content, generate current report artifacts, and review audit history.

## Pre-Demo Setup

Use a demo tenant with:

- Synthetic contractor profile.
- Synthetic or redacted contract metadata.
- No-CUI posture acknowledged.
- Source-backed sample obligations.
- Sample users for operations, contracts, and security roles.
- Allowed evidence metadata records.
- Sample audit events.
- A sample Compliance Status report, CMMC Readiness report, or Evidence Package generated from synthetic records.

Do not use production customer data.

## Demo Sequence

### 1. Contractor Logs In

**Action:** Log in as a demo contractor user.

**Talk track:** "This demo uses synthetic data in a No-CUI workspace. GCCS is a compliance management and readiness workflow tool. It does not provide legal advice, certify CMMC compliance, or authorize real CUI handling."

**Proof point:** Authenticated tenant workspace opens without showing cross-tenant data.

### 2. Dashboard Shows Readiness Status

**Tab:** `Dashboard`.

**Action:** Open the dashboard.

**Show:**

- Readiness summary.
- Open obligations.
- Evidence metadata gaps.
- Upcoming due dates.
- Recent activity.

**Talk track:** "The goal is to show operational readiness status: what exists, who owns it, what evidence metadata is attached, and what still needs review."

**Avoid saying:** "This proves compliance" or "This means you will pass an assessment."

### 3. User Opens An Obligation

**Tab:** `Obligations`.

**Action:** Open a sample source-backed obligation.

**Show:**

- Obligation name.
- Source family.
- Trigger condition.
- Required action.
- Risk or priority label.
- Review state.

**Talk track:** "GCCS turns requirements into reviewable obligation records. The MVP uses review-driven clause handling so the workflow remains accountable and does not pretend to make unsupported legal or assessment determinations."

### 4. User Assigns Tasks And Evidence

**Tabs:** `Obligations` for owner and status; `Evidence` for evidence metadata.

**Action:** Assign an owner, update the obligation workflow status, then create or select allowed evidence metadata from the Evidence tab and link it to the obligation where applicable.

**Show:**

- Owner assignment.
- Due date displayed from the obligation record.
- Status change.
- Evidence metadata fields: `Title`, `Type`, `Owner`, `Status`, `Effective`, `Expires`, `Tags`, `Obligations (optional)`, `Controls`, `Classification`, `Classification reason`, and `Description`.
- Evidence sensitivity reminder.

**Talk track:** "This is where spreadsheet tracking becomes owned work. The team can see who owns the obligation, what evidence metadata exists, and what still needs review."

**No-CUI handling:** If evidence upload or evidence metadata entry appears, state that only synthetic, redacted, or non-sensitive data may be used.

### 5. User Sees Source-Backed Content

**Tab:** `Obligations`.

**Action:** Open source metadata for the obligation.

**Show:**

- Source name.
- Source URL or citation field.
- Last reviewed date.
- Confidence or review state.
- Expert review flag or workflow guidance label, if present.

**Talk track:** "Customer-facing obligation content should remain source-backed, reviewable, and governed. GCCS is not asking the user to trust an unexplained spreadsheet row."

### 6. User Generates Reports Or Evidence Package

**Tab:** `Reports`.

**Action:** Open the Reports tab and generate one current report artifact: Compliance Status, CMMC Readiness, Subcontractor Compliance, or Evidence Package.

**Show:**

- Workflow summary.
- Obligation matrix.
- Evidence metadata snapshot.
- Open gaps.
- Generated report status and timestamp.
- Required disclaimer.

**Talk track:** "The report is a readiness artifact and internal workflow aid. It is not legal advice, CMMC certification, an assessor determination, or a government endorsement."

### 7. User Sees Audit Trail

**Tab:** `Settings`.

**Action:** Open the audit trail or recent activity view.

**Show:**

- Data-handling acknowledgement.
- Obligation update.
- Owner assignment.
- Evidence metadata attachment.
- Report generation or evidence package export.

**Talk track:** "Compliance-relevant workflow changes should be traceable. GCCS helps answer what changed, who changed it, and when."

### 8. User Sees No-CUI Warning On Upload Or Evidence Data Entry

**Tabs:** `Contracts` and `Evidence`.

**Action:** Open the Contracts or Evidence workflow where the No-CUI acknowledgement panel appears, then open an upload-related action to show that upload is blocked until acknowledgement and attestation requirements are satisfied.

**Show:**

- No-CUI warning.
- Prohibited data list.
- User acknowledgement or attestation.
- Support instruction for suspected prohibited data.

**Talk track:** "This is a hard MVP boundary. Real CUI and other prohibited sensitive data must not be entered into GCCS. If a customer requires real CUI handling, they are not a fit for the current MVP posture."

## Required Demo Close

End with a specific pilot ask:

> "The next step is a 30-day guided readiness pilot. We select one contract or synthetic workflow, confirm No-CUI boundaries, assign an internal owner, configure obligations and evidence metadata, and generate a Compliance Status report, CMMC Readiness report, or Evidence Package as appropriate. The pilot is $750, credited toward the first annual subscription if you convert within the agreed period."

## Demo Qualification Questions

- Which compliance workflow is most painful today?
- Where does the evidence live now?
- Who owns readiness status?
- What happens when a prime, customer, advisor, or reviewer asks for proof?
- Can the pilot use synthetic, redacted, or non-sensitive data only?
- Who would own the 30-day pilot internally?
- What would make the pilot worth converting to a paid subscription?

## Stop Conditions

Stop or redirect the demo if the prospect:

- Requires real CUI upload or processing in the MVP.
- Asks GCCS to certify CMMC compliance.
- Asks for legal, accounting, labor, or contracting determinations.
- Wants to paste sensitive customer data into the demo.
- Treats readiness status as an official pass/fail determination.

## Hidden Risks And Edge Cases

- Screenshots or demo recordings can accidentally capture sensitive prospect information; use synthetic data only.
- Evidence descriptions can leak prohibited details even when files are not uploaded.
- A readiness dashboard can be misread as an assessment result if labels are too strong.
- Clause handling requires human review; do not imply extraction candidates or generated obligations are final compliance determinations without review.
- The pilot close must match pricing and terms reviewed by counsel.
