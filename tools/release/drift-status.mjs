// Pure observation classification only: callers supply trusted, complete metadata;
// this module neither queries providers nor delivers alerts or authorizes changes.
const workflowPath = ".github/workflows/production-infrastructure-drift.yml";
export const driftJobNames = Object.freeze([
  "Check staging public runtime settings",
  "Check Production public runtime settings",
  "plan"
]);
const pending = new Set(["queued", "in_progress", "waiting", "requested", "pending"]);
const conclusions = new Set(["success", "failure", "cancelled", "skipped", "timed_out", "neutral", "action_required", "stale", "startup_failure"]);
const integer = value => Number.isSafeInteger(value) && value > 0;
const result = (status, reason) => ({ status, reason });
const unknown = reason => result("unverified", reason);

function validBinding(binding, requireSha = true) {
  return /^[A-Za-z0-9][A-Za-z0-9_-]*\/[A-Za-z0-9][A-Za-z0-9_.-]*$/.test(binding?.repository ?? "") &&
    integer(binding?.repositoryId) && (!requireSha || /^[a-f0-9]{40}$/.test(binding?.headSha ?? "")) &&
    typeof binding?.headBranch === "string" && binding.headBranch.length > 0 &&
    ["schedule", "workflow_dispatch"].includes(binding?.event);
}

function matchesRun(run, binding, requireSha = true) {
  return integer(run?.id) && integer(run?.run_attempt) && run?.path === workflowPath &&
    /^[a-f0-9]{40}$/.test(run?.head_sha ?? "") && (!requireSha || run.head_sha === binding.headSha) &&
    run?.head_branch === binding.headBranch && run?.event === binding.event &&
    [run?.repository, run?.head_repository].every(repo =>
      repo?.id === binding.repositoryId && repo?.full_name === binding.repository);
}

export function evaluateDriftRun({ binding, run, jobs, jobsComplete, jobsAttempt, planOutcome } = {}) {
  if (!validBinding(binding) || !integer(binding.runId) || !integer(binding.runAttempt)) return unknown("invalid_binding");
  if (!matchesRun(run, binding) || run.id !== binding.runId || run.run_attempt !== binding.runAttempt) return unknown("run_mismatch");
  if (pending.has(run.status) && run.conclusion === null) return result("pending", "run_not_terminal");
  if (run.status !== "completed" || !conclusions.has(run.conclusion)) return unknown("invalid_run_state");
  if (jobsComplete !== true || jobsAttempt !== binding.runAttempt || !Array.isArray(jobs?.jobs) || jobs.total_count !== jobs.jobs.length) return unknown("incomplete_jobs");
  const rows = jobs.jobs;
  if (rows.length !== driftJobNames.length || new Set(rows.map(job => job?.id)).size !== rows.length ||
      new Set(rows.map(job => job?.name)).size !== rows.length || rows.some(job =>
        !integer(job?.id) || !driftJobNames.includes(job?.name) || job?.run_id !== run.id ||
        // The Jobs API run_attempt field is optional; the collector must bind
        // the exact /attempts/N/jobs request even when that field is omitted.
        job?.run_attempt !== undefined && job.run_attempt !== run.run_attempt || job?.head_sha !== run.head_sha)) return unknown("job_inventory_mismatch");
  if (rows.some(job => job.status !== "completed" || !conclusions.has(job.conclusion))) return unknown("invalid_job_state");
  const plan = rows.find(job => job.name === "plan");
  // Jobs API failure alone does not distinguish drift from init/auth/plan errors.
  // A caller may supply a separately collected, exact-bound plan-output receipt.
  if (planOutcome !== undefined && (planOutcome?.runId !== run.id || planOutcome?.runAttempt !== run.run_attempt ||
      planOutcome?.headSha !== run.head_sha || planOutcome?.jobId !== plan.id ||
      !["no_drift", "drift", "execution_error"].includes(planOutcome?.outcome) ||
      (planOutcome.outcome === "no_drift" ? plan.conclusion !== "success" : plan.conclusion !== "failure"))) return unknown("plan_outcome_mismatch");
  if (run.conclusion === "success" && rows.some(job => job.conclusion !== "success")) return unknown("run_job_conflict");
  if (rows.every(job => job.conclusion === "success")) {
    return run.conclusion === "success" ? result("healthy", "all_expected_jobs_succeeded") : unknown("run_job_conflict");
  }
  if (run.conclusion === "cancelled" || rows.some(job => !["success", "failure"].includes(job.conclusion))) return result("incomplete", "non_success_job_coverage");
  if (run.conclusion !== "failure") return unknown("run_job_conflict");
  if (plan.conclusion === "failure" && rows.filter(job => job.conclusion === "failure").length === 1) {
    if (planOutcome?.outcome === "drift") return result("drift", "bound_plan_detected_drift");
    if (planOutcome?.outcome === "execution_error") return result("failed", "bound_plan_execution_error");
  }
  return result("failed", "failure_requires_triage");
}

function utcTime(value) {
  if (typeof value !== "string" || !/^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d(?:\.\d{3})?Z$/.test(value)) return NaN;
  const parsed = Date.parse(value);
  return Number.isFinite(parsed) && new Date(parsed).toISOString().replace(".000Z", "Z") === value.replace(".000Z", "Z") ? parsed : NaN;
}

// dueAt and graceMs are an explicit adopted cycle, not an inferred cron/SLA.
// "observed" proves a matching run started, never successful drift or alert delivery.
export function evaluateScheduledDriftCycle({ binding, now, dueAt, graceMs, runs, runsComplete } = {}) {
  const current = utcTime(now), due = utcTime(dueAt);
  if (!validBinding(binding, false) || binding.event !== "schedule" || !Number.isFinite(current) || !Number.isFinite(due) ||
      !Number.isSafeInteger(graceMs) || graceMs < 0 || !Number.isSafeInteger(due + graceMs)) return unknown("invalid_cycle_binding");
  if (runsComplete !== true || !Array.isArray(runs?.workflow_runs) || runs.total_count !== runs.workflow_runs.length) return unknown("incomplete_runs");
  const rows = runs.workflow_runs;
  if (new Set(rows.map(run => run?.id)).size !== rows.length || rows.some(run =>
      // Repository/default-branch cycles may span source advances. Each selected
      // run's own SHA is bound separately by evaluateDriftRun, never executed.
      !matchesRun(run, binding, false) || !Number.isFinite(utcTime(run.created_at)) || utcTime(run.created_at) > current)) return unknown("cycle_run_mismatch");
  if (rows.some(run => utcTime(run.created_at) >= due)) return result("observed", "matching_scheduled_run_started");
  return current >= due + graceMs ? result("overdue", "scheduled_run_not_observed") : result("pending", "within_schedule_grace");
}
