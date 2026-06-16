# Compliance Content Package

This package stores source-backed obligation seed data. Production use requires expert review and a governed publishing workflow.

Records should align with the ontology in `AGENTS.md`:

```text
Regulation / Clause
-> Trigger condition
-> Applies to prime/sub
-> Applies by contract type
-> Data type
-> Required actions
-> Evidence examples
-> Reporting deadline
-> Flow-down requirement
-> Penalty/risk
-> Source URL
-> Clause text version
-> Effective date
-> Source hash
-> Last reviewed date
-> Review owner
-> Requires expert review
-> Review state
-> Superseded/replaced status
```

The MVP package must include DFARS cyber clauses as first-class sources for DoD workflows: DFARS 252.204-7012, 252.204-7019, 252.204-7020, and 252.204-7021.

Review states are `draft`, `needs_review`, `approved`, `rejected`, `customer_disputed`, `published`, and `retired`. Customer-facing publication must be blocked for draft, rejected, and retired content.
