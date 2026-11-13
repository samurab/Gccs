import { readFile } from "node:fs/promises";
import { pathToFileURL } from "node:url";
import { evaluateDriftRun, evaluateScheduledDriftCycle } from "./drift-status.mjs";

const path = ".github/workflows/production-infrastructure-drift.yml";
const integer = value => Number.isSafeInteger(value) && value > 0;
const sha = value => /^[a-f0-9]{40}$/.test(value ?? "");
const fail = () => { throw new Error("Drift observation unavailable"); };

// Proposed Monday cycle and grace; activation/adoption and actual notification
// delivery remain separate decisions. No raw provider fields enter issue text.
export function scheduledCycleAt(now) {
  const current = new Date(now);
  if (!Number.isFinite(current.valueOf()) || current.toISOString() !== now) fail();
  const due = new Date(current);
  due.setUTCDate(due.getUTCDate() - (due.getUTCDay() + 6) % 7);
  due.setUTCHours(13, 17, 0, 0);
  if (due > current) due.setUTCDate(due.getUTCDate() - 7);
  return due.toISOString();
}

export async function observeInfrastructureDrift({ repository, repositoryId, defaultBranch, sourceSha, assignee, now, eventName, eventRun }, request) {
  if (!/^[A-Za-z0-9][A-Za-z0-9_-]*\/[A-Za-z0-9][A-Za-z0-9_.-]*$/.test(repository ?? "") || !integer(repositoryId) || !sha(sourceSha) ||
      typeof defaultBranch !== "string" || !/^[A-Za-z0-9_.\/-]+$/.test(defaultBranch) ||
      !/^[A-Za-z0-9](?:[A-Za-z0-9-]{0,37}[A-Za-z0-9])?$/.test(assignee ?? "") ||
      !["workflow_run", "schedule", "workflow_dispatch"].includes(eventName)) fail();
  const dueAt = scheduledCycleAt(now);
  const root = `/repos/${repository}`;
  let calls = 0;
  const call = async (route, options) => {
    if (++calls > 24 || !route.startsWith(`${root}/`) && route !== root) fail();
    return request(route, options);
  };
  const get = async route => {
    const response = await call(route);
    if (response?.status !== 200) fail();
    return response;
  };
  const repo = (await get(root)).body;
  if (repo?.id !== repositoryId || repo?.full_name !== repository || repo?.default_branch !== defaultBranch) fail();
  if ((await call(`${root}/assignees/${encodeURIComponent(assignee)}`))?.status !== 204) fail();
  const pages = async (route, key) => {
    const rows = []; let total;
    for (let page = 1; page <= 5; page += 1) {
      const response = await get(`${route}${route.includes("?") ? "&" : "?"}per_page=100&page=${page}`);
      const list = key ? response.body?.[key] : response.body;
      if (!Array.isArray(list) || list.length > 100 || typeof response.hasNext !== "boolean") fail();
      if (key) {
        if (!Number.isSafeInteger(response.body.total_count) || response.body.total_count < 0 ||
            total !== undefined && total !== response.body.total_count) fail();
        total = response.body.total_count;
      }
      rows.push(...list);
      if (!response.hasNext) {
        if (key && rows.length !== total) fail();
        return rows;
      }
    }
    fail();
  };
  let outcome, run, identity;
  const binding = { repository, repositoryId, headBranch: defaultBranch, headSha: sourceSha };
  try {
    const ref = (await get(`${root}/git/ref/heads/${encodeURIComponent(defaultBranch)}`)).body;
    if (ref?.ref !== `refs/heads/${defaultBranch}` || ref?.object?.type !== "commit" || ref?.object?.sha !== sourceSha) fail();
    if (eventName === "workflow_run") {
      if (!integer(eventRun?.id) || !integer(eventRun?.run_attempt) || eventRun?.path !== path || !sha(eventRun?.head_sha) ||
          eventRun?.head_branch !== defaultBranch || !["schedule", "workflow_dispatch"].includes(eventRun?.event) ||
          eventRun?.repository?.id !== repositoryId || eventRun?.head_repository?.id !== repositoryId) fail();
      identity = `run ${eventRun.id} attempt ${eventRun.run_attempt}`;
      run = (await get(`${root}/actions/runs/${eventRun.id}/attempts/${eventRun.run_attempt}`)).body;
      if (run?.id !== eventRun.id || run?.run_attempt !== eventRun.run_attempt || run?.status !== "completed" ||
          run?.head_sha !== eventRun.head_sha || run?.event !== eventRun.event) fail();
    } else {
      identity = `cycle ${dueAt}`;
      const rows = await pages(`${root}/actions/workflows/production-infrastructure-drift.yml/runs?event=schedule&branch=${encodeURIComponent(defaultBranch)}&created=${encodeURIComponent(`>=${dueAt}`)}`, "workflow_runs");
      outcome = evaluateScheduledDriftCycle({ binding: { ...binding, event: "schedule" }, now, dueAt, graceMs: 30 * 60_000,
        runs: { total_count: rows.length, workflow_runs: rows }, runsComplete: true });
      if (outcome.status === "observed") run = rows.reduce((a, b) => a.id > b.id ? a : b);
    }
    if (run) {
      const rows = await pages(`${root}/actions/runs/${run.id}/attempts/${run.run_attempt}/jobs`, "jobs");
      outcome = evaluateDriftRun({ binding: { ...binding, headSha: run.head_sha, event: run.event, runId: run.id, runAttempt: run.run_attempt }, run,
        jobs: { total_count: rows.length, jobs: rows }, jobsComplete: true, jobsAttempt: run.run_attempt });
      if (outcome.status === "pending" && Date.parse(now) >= Date.parse(dueAt) + 30 * 60_000) {
        outcome = { status: "overdue", reason: "scheduled_cycle_not_terminal" };
      }
    }
  } catch {
    outcome = { status: "unverified", reason: "observation_incomplete" };
  }
  if (!outcome) fail();
  if (["healthy", "pending"].includes(outcome.status)) return { ...outcome, delivery: "not_required", calls };
  // A failed trust check never supplies a URL/identity from the rejected event.
  identity ??= `cycle ${dueAt}`;
  // Issue category/reason/run links inherit repository visibility. Activation
  // must approve that disclosure; never attach logs, configuration or evidence.
  const title = `[FeDril drift] ${identity}: ${outcome.status}`;
  const link = run && integer(run.id) && run.repository?.id === repositoryId && run.head_repository?.id === repositoryId &&
    run.path === path && sha(run.head_sha) && run.head_branch === defaultBranch ?
    `\nRun: https://github.com/${repository}/actions/runs/${run.id}` : "";
  const body = `Infrastructure drift observation: ${outcome.status}.\nReason: ${outcome.reason}.${link}\n\nAssigned triage is required. This issue is not proof of alert delivery, remediation, or control acceptance.`;
  const issues = await pages(`${root}/issues?state=all`, null);
  const assignedBotIssue = issue => !issue?.pull_request && integer(issue?.number) && issue?.title === title && issue?.body === body &&
    issue?.html_url === `https://github.com/${repository}/issues/${issue.number}` &&
    issue?.user?.login === "github-actions[bot]" && issue?.user?.type === "Bot" &&
    issue?.assignees?.some(user => user?.login?.toLowerCase() === assignee.toLowerCase());
  if (issues.some(assignedBotIssue)) return { ...outcome, delivery: "existing_issue", calls };
  const created = await call(`${root}/issues`, { method: "POST", body: { title, body, assignees: [assignee] } });
  if (created?.status !== 201 || !assignedBotIssue(created.body)) fail();
  return { ...outcome, delivery: "issue_recorded_not_delivery_verified", calls };
}

async function main() {
  const env = process.env;
  if (!env.GH_TOKEN || !env.GITHUB_EVENT_PATH) fail();
  const event = JSON.parse(await readFile(env.GITHUB_EVENT_PATH, "utf8"));
  const repository = env.GITHUB_REPOSITORY;
  const response = await observeInfrastructureDrift({ repository, repositoryId: Number(env.GITHUB_REPOSITORY_ID),
    defaultBranch: event.repository?.default_branch, sourceSha: env.GITHUB_SHA, assignee: env.DRIFT_RESPONSE_ASSIGNEE,
    now: new Date().toISOString(), eventName: env.GITHUB_EVENT_NAME, eventRun: event.workflow_run }, async (route, options = {}) => {
    const received = await fetch(`https://api.github.com${route}`, { method: options.method ?? "GET", redirect: "error",
      signal: AbortSignal.timeout(10_000), headers: { Authorization: `Bearer ${env.GH_TOKEN}`, Accept: "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28", "Content-Type": "application/json" }, body: options.body ? JSON.stringify(options.body) : undefined });
    return { status: received.status, hasNext: /rel="next"/.test(received.headers.get("link") ?? ""),
      body: received.status === 200 || received.status === 201 ? await received.json() : null };
  });
  console.log(JSON.stringify(response));
  if (!["healthy", "pending"].includes(response.status)) process.exitCode = 1;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  main().catch(() => { console.error("Infrastructure drift observation failed; no verified delivery."); process.exitCode = 1; });
}
