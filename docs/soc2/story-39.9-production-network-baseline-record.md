# Story 39.9: Production Network Baseline

Status: **In Progress — T01 source inventory prepared; live comparison requires verification.**
Recorded October 20, 2026, America/New_York. Evidence `S39.9-T01-SOURCE-20261020-01`.

## Scope and prerequisites

**Implemented:** a finite source inventory and twelve-row intended-flow matrix in
existing protected administrative custody. **Partially implemented:** configuration
baseline and T01 inventory. **Planned:** current live resource mapping, allowed/denied
connectivity, reviewed drift cycle and approved configuration changes. **Do not claim:**
live/deployed equivalence, private-only routing, completed hardening or acceptance.

Reuse39.1candidate asset families,39.2provisional sanitized administrative custody
and39.8prepared change-procedure/source-control baseline. Those inputs permit this
read-only source inventory; pending actual change-model/identity review still stops
dependent changes and reliance on that model. No final predecessor acceptance is
invented.39.10coordination remains open. Root owns canonical records; bounded grunt
source exploration is technical preparation, not independent Security/CPA review.

Owner/operator/custodian: FR-01 under SOC2-ROLE-FR-01; actual management review of
this new inventory pending. Independent/qualified resources remain deferred and
unassigned, not fulfilled or N/A. Task priority P1; due date unassigned; provisional
CC6/CC7/CC8 mapping subject to qualified criteria review; Type I and II relevant.

## Requirement disposition

Only `SOC2-39.9-T01` advances to **In Progress / source portion prepared**.
The matrix covers web/API, database, cache, storage, identity, malware scanning,
telemetry/alerts, email, CRM and build/deploy/state agents, with per-row source
references and explicit operation limits. A source-declared dependency is not proof
it is enabled or used. All twelve rows have live status Requires Verification.
The detailed topology and source settings remain protected; Git contains this
sanitized summary only. T02–T06 obligations/statuses remain unchanged.

Historical Azure observations are reused at their original September19/20cutoff.
They are not a current cloud export or a validated match to the present source.
No provider query, credential retry, application-settings export, plan/state read,
configuration change or network probe was performed. Source-level findings remain
separate from current exposure decisions and operating evidence.

The existing adoption README and drift workflow are reused as prospective inputs,
not copied into another execution workflow. The readiness check already fails on
missing flag/required variables and the source rejects detected drift/plan errors.
Actual backend, identity, imported baseline, network reachability and reviewed run
remain unverified. No readiness flag or schedule was changed. No completed drift
runbook, rollout plan, recurring cycle or story acceptance is asserted.

## Evidence and exact continuation

Protected packet: `soc2-39.9-network-preparation-2026-10-20-v1.0`. Logical deliverables are
`configuration-baseline.json`, `network-flow-matrix.csv`, `source-binding.json`
and integrity manifest. Source requirements SHA256 `9fe92fc2de22b54a4904d92c3ad82ac6dc2d372675ca02605a9ff3abb8df3c8d`;
base `548a6e80153de0d19d0630e5e35e614a4f071c8c` plus bound preserved dirty inputs. Source review and
structural/hash verification only; no application build or unchanged test rerun.

Next: after intended-context human authentication and appropriately scoped read
access are available, collect only approved nonsecret network metadata in protected
custody and bind it to the exact resource/environment population. Compare each
matrix row, including DNS/effective destination and migration-agent reachability.
Do not disable public access or change a runner/backend based on this inventory.
Any live read requiring new access/permission or restricted intake remains gated;
any provider change, infrastructure plan/import/apply, credential operation or
release needs its separate exact approval and applicable High/Release verification.

The39.4publication/release hold, No-CUI posture, source/evidence preservation and
immediate incident escalation triggers remain. Registered-business billing facts
still need an approved private source for the PathA quote; completed payer, rationale
and December15review questions are not reopened. Later control work whose actual
identity/topology/provider/change-review prerequisites remain unmet stays explicit.
