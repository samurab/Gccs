# Extraction Regression Review

This folder tracks reviewed extraction evaluation failures without using customer data. Review records classify missed clauses and false positives, assign owners, track status, link follow-up tasks, and record resolution notes or update references.

Allowed failure classifications:

- `parser`
- `matcher`
- `library`
- `label`
- `source_quality`
- `expected_limitation`

Allowed statuses:

- `open`
- `in_progress`
- `resolved`
- `accepted_risk`

Resolved records must link to at least one matcher, library, parser, or label update. Open records must have a follow-up task or an accepted risk explanation before release.
