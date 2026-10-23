# Story 39.9: Production Network Baseline

## Public projection checkpoint — October 23, 2026

**Implemented and published:** non-Development host/CORS startup validation and
synthetic configuration tests are included in published `b4cc43ee`. Its CI run
`39983652302` passed all nine jobs, including real-stack report authorization.
Published `d15b8b84` extends the release checker to discover effective public-key
aliases, reject ambiguous spellings, recheck the relevant key population and
suppress every CLI failure diagnostic. Each lookup has a 30-second timeout request.
The retained local checker suite passed 55 tests with no skips.

**Partially implemented:** this checker detects observed name-population drift;
separate provider reads are not an atomic configuration snapshot. CLI output
projection does not limit the provider response retrieved internally by Azure CLI.
Additional HTTPS origins receive syntax validation and a baseline-review reminder,
not approval. The earlier origin-index-zero limitation is historical.

**Planned:** exact approved live origin/host comparison and rollout validation.
CI `40008612483` for `d15b8b84` failed at Playwright OS dependency installation;
its real-stack application checks did not execute. A proposed installation-timeout
correction has no successful execution evidence at this checkpoint.
**Do not claim:** deployment, complete network controls, approved extra origins,
atomic live verification, production effectiveness or whole-story acceptance.

Earlier sections below retain their original observation and approval cutoffs.
Current publication facts supersede older pending-publication statements only.

## Historical source projections

## Release integration checkpoint — October 22, 2026

**Implemented locally:** commit `b75d05b368125b3152b2f2fd903c211c9a9eb0ae`
adds non-Development startup validation for explicit HTTPS CORS origins and
non-universal host entries. Host validation uses the middleware's IDN
normalization. Development behavior and supported explicit origins, ports,
subdomain patterns and hosts remain covered by synthetic tests.

**Partially implemented:** the primary source owner's completed verification
batch reported 90 passed, zero failed and zero skipped, including 75 new
configuration cases and 15 adjacent cases. Exact source hashes were checked at
handoff. This is historical primary-checkout evidence, not fresh execution of the
integrated release candidate. The bounded technical review found no must-fix
issue; it is not independent SOC 2 assurance.

**Planned:** fresh candidate CI and a nonsecret comparison of every configured
live origin and host entry before rollout. The existing release checker covers
only origin index zero. Previously tolerated settings may now prevent startup.
**Do not claim:** deployed validation, a complete network baseline, provider
routing effectiveness, story acceptance or production promotion. Live network,
health-probe and applicable review dependencies remain open. All primary source,
index and original evidence remain preserved.

## Historical source inventory — October 20, 2026

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
