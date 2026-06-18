# Extraction Test Corpus

This corpus is for automated clause extraction evaluation only. It contains public, synthetic, or explicitly approved non-CUI content. Do not add customer contract documents, CUI, classified information, export-controlled technical data, payroll records, personal data, secrets, private keys, or unrestricted security logs.

## Data Handling Rules

- Allowed data classes: `public`, `synthetic`, and `approved_non_cui`.
- Every document must set `containsCui` to `false`.
- Customer data is not allowed in this corpus, even when a customer believes the document is non-CUI.
- `approved_non_cui` fixtures require a written approval basis in `corpus.json` before benchmark use.
- Every document must include document type, source family, contract type, known limitations, and benchmark approval status.
- Every label file must include expected clause citation, title, flow-down indicator, and source location when available.
- Source locations must use actual file line numbers and the `textAnchor` must appear inside the referenced line range.
- Label sets must be reviewed and approved before they are used as benchmark data.

## Review Workflow

1. Add or update the source text in `documents/`.
2. Add or update the expected labels in `labels/`.
3. Update `corpus.json` with source family, limitations, and data handling metadata.
4. Verify that each label source location points to the actual file line containing the expected clause anchor.
5. A QA owner or compliance content reviewer marks the document `approvedForBenchmark: true` only after reviewing labels and confirming the document is allowed non-CUI content.
6. Re-run the extraction corpus validation tests and evaluation runner before using changed labels as a benchmark.
