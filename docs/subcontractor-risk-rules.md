# Subcontractor Risk Rules

Story 24.2 calculates subcontractor risk from documented operational signals already tracked in the subcontractor profile, flow-down tracker, evidence request workflow, and SAM lookup data. The calculation is workflow guidance only; it is not a legal, CMMC assessment, or responsibility determination.

| Input | Elevated signal | Result |
| --- | --- | --- |
| Insurance expiration | Missing expiration date | Needs review |
| Insurance expiration | Expired | High |
| Insurance expiration | Expires within 60 days | Medium |
| NDA status | Missing or unknown | Needs review |
| NDA status | Not on file or not signed | Medium |
| CUI access and CMMC status | CUI access is enabled and CMMC status is missing or unknown | Needs review |
| SAM registration status | Present and not active | High |
| Flow-down clauses | Any flow-down is required, sent, acknowledged, or expired instead of signed, waived, not applicable, or not required | Medium |
| Evidence requests | Any open evidence request is past due | High |

Status precedence is High, then NeedsReview, then Medium, then Low. When no elevated signals are detected, the risk driver explains that no elevated subcontractor risk signals were found.
