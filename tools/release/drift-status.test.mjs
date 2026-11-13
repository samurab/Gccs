import test from "node:test";
import assert from "node:assert/strict";
import { mkdtemp, readFile, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { spawnSync } from "node:child_process";
import { driftJobNames, evaluateDriftRun, evaluateScheduledDriftCycle } from "./drift-status.mjs";

const binding = { repository: "example/service", repositoryId: 1, headSha: "a".repeat(40), headBranch: "release-main", event: "schedule", runId: 10, runAttempt: 2 };
const run = { id: 10, run_attempt: 2, path: ".github/workflows/production-infrastructure-drift.yml", head_sha: binding.headSha,
  head_branch: binding.headBranch, event: "schedule", repository: { full_name: binding.repository, id: 1 },
  head_repository: { full_name: binding.repository, id: 1 }, status: "completed", conclusion: "success", created_at: "2026-11-09T13:18:00Z" };
const fixture = () => ({ binding: structuredClone(binding), run: structuredClone(run), jobsComplete: true, jobsAttempt: 2,
  jobs: { total_count: 3, jobs: driftJobNames.map((name, i) => ({ id: i + 1, name, run_id: 10, run_attempt: 2, head_sha: binding.headSha, status: "completed", conclusion: "success" })) } });
const failedPlan = () => {
  const input = fixture();
  input.run.conclusion = "failure";
  input.jobs.jobs[2].conclusion = "failure";
  return input;
};
const receipt = outcome => ({ runId: 10, runAttempt: 2, headSha: binding.headSha, jobId: 3, outcome });

test("all three exact jobs must succeed for a healthy run", () => {
  assert.deepEqual(evaluateDriftRun(fixture()), { status: "healthy", reason: "all_expected_jobs_succeeded" });
});
test("failed plan without a separate outcome is generic failed, not detected drift", () => {
  assert.deepEqual(evaluateDriftRun(failedPlan()), { status: "failed", reason: "failure_requires_triage" });
});
for (const [outcome, status] of [["drift", "drift"], ["execution_error", "failed"]]) {
  test(`exact-bound ${outcome} receipt classifies the sole failed plan`, () => {
    assert.equal(evaluateDriftRun({ ...failedPlan(), planOutcome: receipt(outcome) }).status, status);
  });
}
test("extra settings failure stays triage-required even with a drift receipt", () => {
  const input = failedPlan(); input.jobs.jobs[0].conclusion = "failure";
  assert.deepEqual(evaluateDriftRun({ ...input, planOutcome: receipt("drift") }), { status: "failed", reason: "failure_requires_triage" });
});
test("an exact no-drift receipt is compatible with a successful plan", () => {
  assert.equal(evaluateDriftRun({ ...fixture(), planOutcome: receipt("no_drift") }).status, "healthy");
});
test("optional job run_attempt can be absent only with the bound attempt-specific request", () => {
  const input = fixture();
  input.jobs.jobs = input.jobs.jobs.map(({ run_attempt, ...job }) => job);
  assert.equal(evaluateDriftRun(input).status, "healthy");
  input.jobsAttempt = undefined;
  assert.equal(evaluateDriftRun(input).status, "unverified");
});
for (const status of ["queued", "in_progress", "waiting", "requested", "pending"]) {
  test(`${status} run is pending without a terminal jobs claim`, () => {
    const input = fixture(); input.run.status = status; input.run.conclusion = null; input.jobsComplete = false;
    assert.equal(evaluateDriftRun(input).status, "pending");
  });
}
for (const conclusion of ["skipped", "cancelled", "timed_out", "neutral", "action_required", "stale", "startup_failure"]) {
  for (const jobIndex of [0, 1, 2]) {
    test(`${driftJobNames[jobIndex]} ${conclusion} cannot pass coverage`, () => {
      const input = failedPlan(); input.jobs.jobs[jobIndex].conclusion = conclusion;
      assert.equal(evaluateDriftRun(input).status, "incomplete");
    });
  }
}
test("whole-run cancellation never passes even with terminal job metadata", () => {
  const input = failedPlan(); input.run.conclusion = "cancelled";
  assert.equal(evaluateDriftRun(input).status, "incomplete");
});
for (const [field, value] of [["id", 11], ["run_attempt", 1], ["path", ".github/workflows/ci.yml"], ["head_sha", "b".repeat(40)], ["head_branch", "other"], ["event", "push"], ["repository", { full_name: "other/service", id: 1 }], ["head_repository", { full_name: binding.repository, id: 2 }]]) {
  test(`run ${field} mismatch is unverified`, () => {
    const input = fixture(); input.run[field] = value;
    assert.equal(evaluateDriftRun(input).status, "unverified");
  });
}
for (const [field, value] of [["run_id", 9], ["run_attempt", 1], ["head_sha", "b".repeat(40)], ["name", "unexpected"], ["id", 0], ["status", "in_progress"], ["conclusion", "unknown"]]) {
  test(`job ${field} mismatch is unverified`, () => {
    const input = fixture(); input.jobs.jobs[0][field] = value;
    assert.equal(evaluateDriftRun(input).status, "unverified");
  });
}
for (const [field, value] of [["runId", 11], ["runAttempt", 1], ["headSha", "b".repeat(40)], ["jobId", 2], ["outcome", "unknown"]]) {
  test(`plan receipt ${field} mismatch is unverified`, () => {
    const input = failedPlan(); input.planOutcome = { ...receipt("drift"), [field]: value };
    assert.equal(evaluateDriftRun(input).status, "unverified");
  });
}
for (const mutate of [
  input => { input.jobsComplete = false; },
  input => { input.jobs.total_count = 4; },
  input => { input.jobs.jobs = []; input.jobs.total_count = 0; },
  input => { input.jobs.jobs[1] = structuredClone(input.jobs.jobs[0]); },
  input => { input.jobs.jobs[1].name = input.jobs.jobs[0].name; },
  input => { input.run.conclusion = "unknown"; },
  input => { input.run.status = "unknown"; },
  input => { input.jobs.jobs[0].conclusion = "failure"; },
  input => { input.run.conclusion = "failure"; },
  input => { input.planOutcome = receipt("drift"); },
  input => { input.binding.repositoryId = "1"; },
  input => { input.binding.headSha = "invalid"; }
]) {
  test(`partial/conflicting metadata fails closed (${mutate.toString()})`, () => {
    const input = fixture(); mutate(input);
    assert.equal(evaluateDriftRun(input).status, "unverified");
  });
}
test("missing input is unverified and no supplied content is echoed", () => {
  assert.equal(evaluateDriftRun().status, "unverified");
  assert.equal(evaluateScheduledDriftCycle().status, "unverified");
  const input = fixture(); input.run.path = "PRIVATE_SENTINEL";
  assert.doesNotMatch(JSON.stringify(evaluateDriftRun(input)), /PRIVATE_SENTINEL/);
});

const cycle = () => ({ binding: structuredClone(binding), now: "2026-11-09T13:30:00Z", dueAt: "2026-11-09T13:17:00Z", graceMs: 10 * 60_000,
  runsComplete: true, runs: { total_count: 0, workflow_runs: [] } });
test("missing scheduled cycle after grace is overdue", () => {
  assert.equal(evaluateScheduledDriftCycle(cycle()).status, "overdue");
});
test("grace equality is overdue; a millisecond before it is pending", () => {
  const input = cycle(); input.now = "2026-11-09T13:27:00Z";
  assert.equal(evaluateScheduledDriftCycle(input).status, "overdue");
  input.now = "2026-11-09T13:26:59.999Z";
  assert.equal(evaluateScheduledDriftCycle(input).status, "pending");
});
test("future cycle is pending, not missed", () => {
  const input = cycle(); input.dueAt = "2026-11-10T13:17:00Z";
  assert.equal(evaluateScheduledDriftCycle(input).status, "pending");
});
test("zero grace is explicit and supported", () => {
  const input = cycle(); input.graceMs = 0; input.now = input.dueAt;
  assert.equal(evaluateScheduledDriftCycle(input).status, "overdue");
});
test("matching cycle run observed is never success/delivery evidence", () => {
  const input = cycle(); input.runs = { total_count: 1, workflow_runs: [{ ...run, conclusion: "failure" }] };
  assert.deepEqual(evaluateScheduledDriftCycle(input), { status: "observed", reason: "matching_scheduled_run_started" });
});
test("old run does not satisfy this cycle", () => {
  const input = cycle(); input.runs = { total_count: 1, workflow_runs: [{ ...run, created_at: "2026-11-02T13:18:00Z" }] };
  assert.equal(evaluateScheduledDriftCycle(input).status, "overdue");
});
for (const mutate of [
  input => { input.runsComplete = false; },
  input => { input.runs.total_count = 1; },
  input => { input.graceMs = -1; },
  input => { input.graceMs = 0.5; },
  input => { input.graceMs = Number.MAX_SAFE_INTEGER; },
  input => { input.now = "2026-02-30T13:17:00Z"; },
  input => { input.dueAt = "2026-11-09T13:17:00-05:00"; },
  input => { input.dueAt = "2026-11-09"; },
  input => { input.binding.event = "workflow_dispatch"; },
  input => { input.runs = { total_count: 1, workflow_runs: [{ ...run, created_at: "2026-11-09T14:00:00Z" }] }; },
  input => { input.runs = { total_count: 1, workflow_runs: [{ ...run, head_sha: "malformed" }] }; },
  input => { input.runs = { total_count: 1, workflow_runs: [{ ...run, event: "workflow_dispatch" }] }; },
  input => { input.runs = { total_count: 2, workflow_runs: [run, run] }; }
]) {
  test(`missing-cycle uncertainty fails closed (${mutate.toString()})`, () => {
    const input = cycle(); mutate(input);
    assert.equal(evaluateScheduledDriftCycle(input).status, "unverified");
  });
}

const workflow = await readFile(new URL("../../.github/workflows/production-infrastructure-drift.yml", import.meta.url), "utf8");
const planShell = workflow.match(/      - name: Fail on configuration drift\n        id: drift-plan\n        run: \|\n([\s\S]*)$/)?.[1]?.replace(/^          /gm, "");
test("plan outcome is a job output, without new identities, notification or dispatch", () => {
  assert.ok(workflow.includes("      plan_outcome: ${{ steps.drift-plan.outputs.outcome }}"));
  assert.ok(planShell);
  assert.match(workflow, /PRODUCTION_TERRAFORM_STATE_READY" != "true"/);
  assert.doesNotMatch(planShell, /terraform.*apply|curl|gh |az /);
});
for (const [planExit, expectedExit, outcome] of [[0, 0, "no_drift"], [2, 1, "drift"], [1, 1, "execution_error"], [7, 7, "execution_error"]]) {
  test(`actual plan shell preserves Terraform ${planExit} exit semantics and outputs only ${outcome}`, async () => {
    assert.ok(planShell);
    // Match GitHub's regular output-file contract; retain generated fixtures for operator disposition.
    const outputDirectory = await mkdtemp(join(tmpdir(), "fedril-drift-plan-"));
    const outputFile = join(outputDirectory, "github-output");
    await writeFile(outputFile, "", { flag: "wx", mode: 0o600 });
    // A shell function replaces Terraform; no provider, state or plan file is touched.
    const child = spawnSync("/bin/bash", ["-c", `terraform() { return ${planExit}; }\n${planShell}`], {
      env: { PATH: "/usr/bin:/bin", GITHUB_OUTPUT: outputFile, RUNNER_TEMP: "/synthetic-not-created" }, encoding: "utf8", timeout: 5_000
    });
    assert.ifError(child.error);
    assert.equal(child.status, expectedExit, child.stderr);
    assert.deepEqual((await readFile(outputFile, "utf8")).match(/^outcome=.+$/gm), [`outcome=${outcome}`]);
  });
}
