import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { observeInfrastructureDrift, scheduledCycleAt } from "./observe-infrastructure-drift.mjs";
import { driftJobNames } from "./drift-status.mjs";

const input = () => ({ repository: "example/service", repositoryId: 1, defaultBranch: "release-main", sourceSha: "a".repeat(40),
  assignee: "responder", now: "2026-11-09T14:00:00.000Z", eventName: "schedule" });
const makeRun = () => ({ id: 10, run_attempt: 2, path: ".github/workflows/production-infrastructure-drift.yml", head_sha: "a".repeat(40),
  head_branch: "release-main", event: "schedule", repository: { full_name: "example/service", id: 1 },
  head_repository: { full_name: "example/service", id: 1 }, status: "completed", conclusion: "success", created_at: "2026-11-09T13:18:00Z" });
const makeJobs = () => driftJobNames.map((name, i) => ({ id: i + 1, name, run_id: 10, run_attempt: 2, head_sha: "a".repeat(40), status: "completed", conclusion: "success" }));
const outcomeSteps = outcome => ["no_drift", "drift", "execution_error"].map(name => ({
  name: `Record plan outcome ${name}`, status: "completed", conclusion: name === outcome ? "success" : "skipped"
}));
function provider({ runs = [], jobs = makeJobs(), issues = [], override } = {}) {
  const calls = [];
  const request = async (route, options) => {
    calls.push({ route, options });
    const overridden = override?.(route, options);
    if (overridden) return overridden;
    if (options?.method === "POST") {
      assert.equal(route, "/repos/example/service/issues");
      return { status: 201, body: { ...options.body, number: 20, html_url: "https://github.com/example/service/issues/20",
        user: { login: "github-actions[bot]", type: "Bot" }, assignees: options.body.assignees.map(login => ({ login })) } };
    }
    if (route === "/repos/example/service") return { status: 200, body: { id: 1, full_name: "example/service", default_branch: "release-main" } };
    if (route.includes("/assignees/")) return { status: 204 };
    if (route.includes("/git/ref/")) return { status: 200, body: { ref: "refs/heads/release-main", object: { type: "commit", sha: "a".repeat(40) } } };
    if (route.includes("/actions/workflows/")) return { status: 200, hasNext: false, body: { total_count: runs.length, workflow_runs: runs } };
    if (route.includes("/jobs?")) return { status: 200, hasNext: false, body: { total_count: jobs.length, jobs } };
    if (route.includes("/actions/runs/10/attempts/2")) return { status: 200, body: runs[0] ?? makeRun() };
    if (route.includes("/issues?")) return { status: 200, hasNext: false, body: issues };
    throw new Error("Unexpected synthetic route");
  };
  return { request, calls };
}
const writes = calls => calls.filter(call => call.options?.method === "POST");

test("Monday cycle computation stays in UTC and handles pre-cycle/week boundaries", () => {
  assert.equal(scheduledCycleAt("2026-11-09T14:00:00.000Z"), "2026-11-09T13:17:00.000Z");
  assert.equal(scheduledCycleAt("2026-11-09T13:16:59.999Z"), "2026-11-02T13:17:00.000Z");
  assert.equal(scheduledCycleAt("2026-11-15T23:59:59.999Z"), "2026-11-09T13:17:00.000Z");
  assert.throws(() => scheduledCycleAt("2026-02-30T00:00:00.000Z"));
});
test("missing cycle creates one assigned fixed-body issue and never claims delivery", async () => {
  const mock = provider();
  const outcome = await observeInfrastructureDrift(input(), mock.request);
  assert.equal(outcome.status, "overdue");
  assert.equal(outcome.delivery, "issue_recorded_not_delivery_verified");
  assert.equal(writes(mock.calls).length, 1);
  assert.deepEqual(writes(mock.calls)[0].options.body.assignees, ["responder"]);
  assert.match(writes(mock.calls)[0].options.body.body, /not proof of alert delivery/);
  assert.ok(mock.calls.some(call => call.route.includes("event=schedule&branch=release-main&created=")));
});
test("healthy complete cycle performs no notification write", async () => {
  const mock = provider({ runs: [makeRun()] });
  const outcome = await observeInfrastructureDrift(input(), mock.request);
  assert.equal(outcome.status, "healthy");
  assert.equal(writes(mock.calls).length, 0);
});
test("missing cycle within grace is pending without an issue", async () => {
  const mock = provider();
  assert.equal((await observeInfrastructureDrift({ ...input(), now: "2026-11-09T13:30:00.000Z" }, mock.request)).status, "pending");
  assert.equal(writes(mock.calls).length, 0);
});
for (const status of ["queued", "waiting", "in_progress"]) {
  test(`overdue ${status} cycle produces an assigned overdue issue`, async () => {
    const mock = provider({ runs: [{ ...makeRun(), status, conclusion: null }] });
    assert.equal((await observeInfrastructureDrift(input(), mock.request)).status, "overdue");
    assert.equal(writes(mock.calls).length, 1);
  });
}
for (const conclusion of ["skipped", "cancelled", "timed_out"]) {
  test(`${conclusion} job cannot pass and records incomplete coverage`, async () => {
    const jobs = makeJobs(); jobs[1].conclusion = conclusion;
    const mock = provider({ runs: [{ ...makeRun(), conclusion: "failure" }], jobs });
    assert.equal((await observeInfrastructureDrift(input(), mock.request)).status, "incomplete");
    assert.equal(writes(mock.calls).length, 1);
  });
}
test("generic failed plan never becomes drift through Jobs API alone", async () => {
  const jobs = makeJobs(); jobs[2].conclusion = "failure";
  const mock = provider({ runs: [{ ...makeRun(), conclusion: "failure" }], jobs });
  const outcome = await observeInfrastructureDrift(input(), mock.request);
  assert.equal(outcome.status, "failed");
  assert.match(writes(mock.calls)[0].options.body.body, /failure_requires_triage/);
});
test("exact attempt plan marker records detected drift without reading logs or artifacts", async () => {
  const jobs = makeJobs(); jobs[2].conclusion = "failure"; jobs[2].steps = outcomeSteps("drift");
  const mock = provider({ runs: [{ ...makeRun(), conclusion: "failure" }], jobs });
  const outcome = await observeInfrastructureDrift(input(), mock.request);
  assert.equal(outcome.status, "drift");
  assert.match(writes(mock.calls)[0].options.body.body, /bound_plan_detected_drift/);
  assert.ok(mock.calls.every(call => !/logs|artifacts/.test(call.route)));
});
test("complete terminal event uses exact attempt endpoint and no logs/artifacts", async () => {
  const eventRun = makeRun();
  const mock = provider({ runs: [eventRun] });
  assert.equal((await observeInfrastructureDrift({ ...input(), eventName: "workflow_run", eventRun }, mock.request)).status, "healthy");
  assert.ok(mock.calls.some(call => call.route === "/repos/example/service/actions/runs/10/attempts/2"));
  assert.ok(mock.calls.some(call => call.route.includes("/attempts/2/jobs?")));
  assert.ok(mock.calls.every(call => !/logs|artifacts|dispatches|rerun|settings|secrets/.test(call.route)));
});
test("manual completed drift run is evaluated but cannot satisfy a scheduled query", async () => {
  const eventRun = { ...makeRun(), event: "workflow_dispatch" };
  const mock = provider({ runs: [eventRun] });
  assert.equal((await observeInfrastructureDrift({ ...input(), eventName: "workflow_run", eventRun }, mock.request)).status, "healthy");
  const cycleMock = provider({ runs: [eventRun] });
  assert.equal((await observeInfrastructureDrift(input(), cycleMock.request)).status, "unverified");
});
for (const [field, value] of [["head_sha", "malformed"], ["path", ".github/workflows/untrusted.yml"], ["head_branch", "fork"], ["head_repository", { id: 2 }], ["id", "SECRET_SENTINEL"]]) {
  test(`untrusted terminal ${field} cannot drive source checkout or leak raw metadata`, async () => {
    const eventRun = { ...makeRun(), [field]: value };
    const mock = provider();
    assert.equal((await observeInfrastructureDrift({ ...input(), eventName: "workflow_run", eventRun }, mock.request)).status, "unverified");
    assert.ok(!mock.calls.some(call => call.route.includes("/actions/runs/")));
    assert.doesNotMatch(JSON.stringify(writes(mock.calls)), /SECRET_SENTINEL|untrusted\.yml|b{40}/);
  });
}
for (const change of [{ assignee: "" }, { assignee: "bad/name" }, { assignee: "x\nSECRET_SENTINEL" }, { sourceSha: "bad" }, { repository: "../other" }]) {
  test(`invalid activation binding fails before provider calls: ${JSON.stringify(change)}`, async () => {
    const mock = provider();
    await assert.rejects(observeInfrastructureDrift({ ...input(), ...change }, mock.request));
    assert.equal(mock.calls.length, 0);
  });
}
test("repository and assignability failures create no issue", async () => {
  for (const routeFragment of ["/assignees/", "/repos/example/service"]) {
    const mock = provider({ override: route => (routeFragment === "/repos/example/service" ? route === routeFragment : route.includes(routeFragment)) ? { status: 403 } : undefined });
    await assert.rejects(observeInfrastructureDrift(input(), mock.request));
    assert.equal(writes(mock.calls).length, 0);
  }
});
for (const failed of [false, true]) {
  test(`monitored prior default-branch SHA is independent of trusted observer code (${failed ? "failure" : "success"})`, async () => {
    const eventRun = { ...makeRun(), head_sha: "b".repeat(40), conclusion: failed ? "failure" : "success" };
    const jobs = makeJobs().map(job => ({ ...job, head_sha: eventRun.head_sha }));
    if (failed) jobs[2].conclusion = "failure";
    const mock = provider({ runs: [eventRun], jobs });
    assert.equal((await observeInfrastructureDrift({ ...input(), eventName: "workflow_run", eventRun }, mock.request)).status, failed ? "failed" : "healthy");
    assert.ok(!mock.calls.some(call => call.route.includes(eventRun.head_sha)));
  });
}
test("daily cycle tolerates monitored source advancement without changing observer code", async () => {
  const monitored = { ...makeRun(), head_sha: "b".repeat(40) };
  const jobs = makeJobs().map(job => ({ ...job, head_sha: monitored.head_sha }));
  const mock = provider({ runs: [monitored], jobs });
  assert.equal((await observeInfrastructureDrift(input(), mock.request)).status, "healthy");
});
test("fetched attempt must match the triggering source exactly", async () => {
  const eventRun = makeRun();
  const mock = provider({ runs: [{ ...eventRun, head_sha: "b".repeat(40) }] });
  assert.equal((await observeInfrastructureDrift({ ...input(), eventName: "workflow_run", eventRun }, mock.request)).status, "unverified");
});
test("changed default-branch source produces unverified signal, without executing trigger source", async () => {
  const mock = provider({ override: route => route.includes("/git/ref/") ? { status: 200, body: { ref: "refs/heads/release-main", object: { type: "commit", sha: "b".repeat(40) } } } : undefined });
  assert.equal((await observeInfrastructureDrift(input(), mock.request)).status, "unverified");
  assert.equal(writes(mock.calls).length, 1);
});
test("provider metadata error is not retried or converted to healthy", async () => {
  const mock = provider({ override: route => route.includes("/actions/workflows/") ? { status: 503, body: { message: "PRIVATE_SENTINEL" } } : undefined });
  assert.equal((await observeInfrastructureDrift(input(), mock.request)).status, "unverified");
  assert.equal(mock.calls.filter(call => call.route.includes("/actions/workflows/")).length, 1);
  assert.doesNotMatch(JSON.stringify(writes(mock.calls)), /PRIVATE_SENTINEL/);
});
test("incomplete jobs pagination is unverified", async () => {
  const mock = provider({ runs: [makeRun()], override: route => route.includes("/jobs?") ? { status: 200, hasNext: false, body: { total_count: 3, jobs: makeJobs().slice(0, 2) } } : undefined });
  assert.equal((await observeInfrastructureDrift(input(), mock.request)).status, "unverified");
});
test("optional per-job attempt is accepted through exact attempt-specific collection", async () => {
  const jobs = makeJobs().map(({ run_attempt, ...job }) => job);
  const mock = provider({ runs: [makeRun()], jobs });
  assert.equal((await observeInfrastructureDrift(input(), mock.request)).status, "healthy");
});
test("runs pagination has a hard five-page cap and signals unverified", async () => {
  const mock = provider({ override: route => route.includes("/actions/workflows/") ? { status: 200, hasNext: true,
    body: { total_count: 600, workflow_runs: [] } } : undefined });
  assert.equal((await observeInfrastructureDrift(input(), mock.request)).status, "unverified");
  assert.equal(mock.calls.filter(call => call.route.includes("/actions/workflows/")).length, 5);
  assert.ok(mock.calls.length <= 24);
});
test("issue pagination is followed before creating one alert", async () => {
  const mock = provider({ override: route => route.includes("/issues?") ? { status: 200, hasNext: route.endsWith("page=1"), body: [] } : undefined });
  await observeInfrastructureDrift(input(), mock.request);
  assert.equal(mock.calls.filter(call => call.route.includes("/issues?")).length, 2);
  assert.equal(writes(mock.calls).length, 1);
});
async function priorIssue() {
  const mock = provider(); await observeInfrastructureDrift(input(), mock.request);
  return { ...writes(mock.calls)[0].options.body, number: 20, html_url: "https://github.com/example/service/issues/20",
    user: { login: "github-actions[bot]", type: "Bot" }, assignees: [{ login: "responder" }] };
}
test("an attributable exact assigned existing issue prevents duplicate creation without editing it", async () => {
  const issue = await priorIssue(); issue.state = "closed";
  const mock = provider({ issues: [issue] });
  assert.equal((await observeInfrastructureDrift(input(), mock.request)).delivery, "existing_issue");
  assert.equal(writes(mock.calls).length, 0);
  assert.ok(mock.calls.every(call => !call.options?.method || call.options.method === "GET"));
});
for (const change of [{ assignees: [] }, { body: "spoofed body" }, { user: { login: "other", type: "User" } }, { html_url: "https://example.invalid/20" }, { pull_request: {} }]) {
  test(`spoofed/unassigned existing title cannot suppress alert: ${JSON.stringify(change)}`, async () => {
    const issue = { ...await priorIssue(), ...change };
    const mock = provider({ issues: [issue] });
    assert.equal((await observeInfrastructureDrift(input(), mock.request)).delivery, "issue_recorded_not_delivery_verified");
    assert.equal(writes(mock.calls).length, 1);
  });
}
test("uncertain issue creation is not retried and never claimed successful", async () => {
  const mock = provider({ override: (route, options) => options?.method === "POST" ? { status: 502 } : undefined });
  await assert.rejects(observeInfrastructureDrift(input(), mock.request));
  assert.equal(writes(mock.calls).length, 1);
});
test("observer workflow uses trusted default-source SHA and no cloud or trigger-source privileges", async () => {
  const workflow = await readFile(new URL("../../.github/workflows/production-infrastructure-drift-observer.yml", import.meta.url), "utf8");
  assert.match(workflow, /workflow_run:\n    workflows: \[Production infrastructure drift\]\n    types: \[completed\]/);
  assert.match(workflow, /ref: \$\{\{ github.sha \}\}/);
  assert.match(workflow, /persist-credentials: false/);
  assert.match(workflow, /contents: read\n  actions: read\n  issues: write/);
  assert.match(workflow, /if: vars.DRIFT_OBSERVER_READY == 'true'/);
  assert.match(workflow, /cancel-in-progress: false/);
  assert.doesNotMatch(workflow, /id-token:|azure\/login|environment:|workflow_dispatch:|github.event.workflow_run.head_sha|download-artifact|npm |pull_request_target|secrets\./);
});
test("drift workflow emits one fixed allowlisted plan-outcome marker", async () => {
  const workflow = await readFile(new URL("../../.github/workflows/production-infrastructure-drift.yml", import.meta.url), "utf8");
  for (const outcome of ["no_drift", "drift", "execution_error"]) {
    assert.match(workflow, new RegExp(`- name: Record plan outcome ${outcome}\\n        if: always\\(\\) && steps\\.drift-plan\\.outputs\\.outcome == '${outcome}'\\n        run: true`));
  }
});
