import * as admission from './admission.mjs';
import { mkdir, writeFile, appendFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { setTimeout as delay } from 'node:timers/promises';

const fixedFailure = () => { throw new Error('protected_bootstrap_held_reconcile_no_retry'); };
const sha40 = value => typeof value === 'string' && /^[a-f0-9]{40}$/.test(value);
const repoName = value => typeof value === 'string' && /^[A-Za-z0-9][A-Za-z0-9-]*\/[A-Za-z0-9][A-Za-z0-9._-]*$/.test(value);

export async function readResponse(response, cap) {
  if (response.status !== 200 || !response.body) fixedFailure();
  const chunks = []; let length = 0;
  for await (const chunk of response.body) {
    length += chunk.length; if (length > cap) fixedFailure(); chunks.push(Buffer.from(chunk));
  }
  return Buffer.concat(chunks);
}

export async function providerJson(repository, suffix, token, fetchImpl = fetch) {
  if (!repoName(repository) || typeof suffix !== 'string' || !suffix.startsWith('/') || /[\r\n#]/.test(suffix) ||
      typeof token !== 'string' || !token || /[\r\n]/.test(token)) fixedFailure();
  try {
    const response = await fetchImpl(`https://api.github.com/repos/${repository}${suffix}`, { redirect: 'error',
      signal: AbortSignal.timeout(30000), headers: { Accept: 'application/vnd.github+json', Authorization: `Bearer ${token}`,
        'X-GitHub-Api-Version': '2026-03-10', 'User-Agent': 'FeDril-protected-admission-reader' } });
    return JSON.parse((await readResponse(response, 8 * 1024 * 1024)).toString('utf8'));
  } catch { fixedFailure(); }
}

export async function rawBytes(repository, workflowSha, path, cap, fetchImpl = fetch) {
  if (!repoName(repository) || !sha40(workflowSha) || typeof path !== 'string' ||
      !/^[A-Za-z0-9_.\/-]+$/.test(path) || path.split('/').some(part => !part || ['.', '..'].includes(part))) fixedFailure();
  try {
    return await readResponse(await fetchImpl(`https://raw.githubusercontent.com/${repository}/${workflowSha}/${path}`,
      { redirect: 'error', signal: AbortSignal.timeout(30000), headers: { 'User-Agent': 'FeDril-protected-admission-reader' } }), cap);
  } catch { fixedFailure(); }
}

export async function readAllChecks(context, read = providerJson, token) {
  context = Object.freeze({ ...context });
  admission.validateContext(context);
  const all = [], ids = new Set(); let expected;
  for (let page = 1; page <= 10; page++) {
    const result = await read(context.repository, `/commits/${context.candidateSha}/check-runs?filter=all&per_page=100&app_id=${context.appId}&check_name=${encodeURIComponent(admission.CHECK_NAME)}&page=${page}`, token);
    if (!Number.isSafeInteger(result.total_count) || result.total_count < 0 || result.total_count > 1000 ||
        !Array.isArray(result.check_runs) || result.check_runs.length > 100 || (expected !== undefined && expected !== result.total_count)) fixedFailure();
    expected = result.total_count;
    for (const check of result.check_runs) {
      if (!Number.isSafeInteger(check?.id) || check.id <= 0 || ids.has(check.id)) fixedFailure();
      ids.add(check.id); all.push(check);
    }
    if (all.length === expected) return all;
    if (all.length > expected || result.check_runs.length !== 100) fixedFailure();
  }
  fixedFailure();
}

export async function waitForAdmission(context, read = providerJson, token, pause = delay) {
  context = Object.freeze({ ...context });
  admission.validateContext(context);
  for (let attempt = 0; attempt < 20; attempt++) {
    const checks = await readAllChecks(context, read, token);
    if (admission.currentAdmissionTargets(checks, context).length) return admission.requireAdmission(checks, context);
    if (attempt < 19) await pause(15000);
  }
  fixedFailure();
}

export async function reconcileExactAdmission(context, checkRunId, read = providerJson, token) {
  context = Object.freeze({ ...context });
  const identity = Number(checkRunId);
  if (!Number.isSafeInteger(identity) || identity <= 0 || String(identity) !== String(checkRunId)) fixedFailure();
  const selected = admission.requireAdmission(await readAllChecks(context, read, token), context);
  if (selected.checkRunId !== identity) fixedFailure();
  const exact = admission.requireAdmission([await read(context.repository, "/check-runs/" + identity, token)], context);
  if (exact.checkRunId !== identity || JSON.stringify(exact.carrier) !== JSON.stringify(selected.carrier)) fixedFailure();
  return selected;
}

export async function bootstrapFromEnvironment(env, { read = providerJson, raw = rawBytes, wait = waitForAdmission,
    makeDirectory = mkdir, put = writeFile, append = appendFile } = {}) {
  try {
    env = Object.freeze(Object.fromEntries(['GITHUB_REPOSITORY', 'GITHUB_REPOSITORY_ID', 'GITHUB_SHA', 'GITHUB_EVENT_NAME',
      'GITHUB_REF', 'GITHUB_RUN_ID', 'GITHUB_RUN_ATTEMPT', 'FEDRIL_WORKFLOW_SHA', 'FEDRIL_WORKFLOW_REF', 'FEDRIL_MODE',
      'PROTECTED_ASSERTIONS_APP_ID', 'CANDIDATE_TAG', 'GH_TOKEN', 'RUNNER_TEMP', 'GITHUB_ENV', 'FEDRIL_COMPARISON_BASE_SHA',
      'FEDRIL_PR_HEAD_SHA', 'FEDRIL_EVENT_DELETED', 'FEDRIL_ADMISSION_CHECK_ID'].map(key => [key, env[key]])));
    const repository = env.GITHUB_REPOSITORY, workflowSha = env.FEDRIL_WORKFLOW_SHA, mode = env.FEDRIL_MODE;
    if (!repoName(repository) || !sha40(workflowSha) || !['ci', 'staging', 'production'].includes(mode)) fixedFailure();
    const token = env.GH_TOKEN;
    if (env.FEDRIL_ADMISSION_CHECK_ID && !/^[1-9][0-9]{0,19}$/.test(env.FEDRIL_ADMISSION_CHECK_ID)) fixedFailure();
    let candidateSha, candidateTag = null, stagingWorkflowSha = null, approvedManifest = null, approvedManifestBytes = null, stagingRun = null;
    if (mode === 'ci') {
      candidateSha = env.GITHUB_SHA;
      if (candidateSha === '0'.repeat(40) || env.FEDRIL_EVENT_DELETED === 'true' ||
          !['push', 'pull_request', 'workflow_dispatch'].includes(env.GITHUB_EVENT_NAME) ||
          !sha40(env.FEDRIL_COMPARISON_BASE_SHA || '0'.repeat(40)) ||
          (env.GITHUB_EVENT_NAME === 'pull_request' && (!sha40(env.FEDRIL_PR_HEAD_SHA) || !env.FEDRIL_COMPARISON_BASE_SHA || env.FEDRIL_COMPARISON_BASE_SHA === '0'.repeat(40))) ||
          (env.GITHUB_EVENT_NAME === 'pull_request' && !/^refs\/pull\/[1-9][0-9]*\/merge$/.test(env.GITHUB_REF))) fixedFailure();
    } else if (mode === 'staging') {
      candidateTag = env.CANDIDATE_TAG || null;
      if (candidateTag) {
        if (!/^v(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)-rc\.[1-9][0-9]*$/.test(candidateTag)) fixedFailure();
        const ref = await read(repository, `/git/ref/tags/${candidateTag}`, token);
        if (ref.object?.type !== 'commit') fixedFailure(); candidateSha = ref.object.sha;
      } else candidateSha = workflowSha;
      approvedManifestBytes = await raw(repository, workflowSha, 'docs/release/approved-release.json', 1024 * 1024);
      approvedManifest = JSON.parse(approvedManifestBytes.toString('utf8'));
    } else {
      approvedManifestBytes = await raw(repository, workflowSha, 'docs/release/approved-release.json', 1024 * 1024);
      approvedManifest = JSON.parse(approvedManifestBytes.toString('utf8'));
      candidateSha = approvedManifest.commitSha; candidateTag = approvedManifest.candidateTag;
      const runId = approvedManifest.staging?.runId;
      if (!Number.isSafeInteger(runId) || runId <= 0) fixedFailure();
      stagingRun = await read(repository, `/actions/runs/${runId}`, token);
      if (stagingRun.id !== runId || stagingRun.repository?.full_name !== repository ||
          String(stagingRun.repository?.id) !== env.GITHUB_REPOSITORY_ID || stagingRun.path !== '.github/workflows/staging.yml' ||
          stagingRun.event !== 'workflow_dispatch' || stagingRun.head_branch !== 'release-main' ||
          stagingRun.status !== 'completed' || stagingRun.conclusion !== 'success' || !sha40(stagingRun.head_sha)) fixedFailure();
      stagingWorkflowSha = stagingRun.head_sha;
    }
    if (!sha40(candidateSha)) fixedFailure();
    const commit = await read(repository, `/commits/${candidateSha}`, token);
    if (commit.sha !== candidateSha || !sha40(commit.commit?.tree?.sha)) fixedFailure();
    const context = { repository, appId: env.PROTECTED_ASSERTIONS_APP_ID, mode, workflowSha,
      candidateSha, candidateTreeSha: commit.commit.tree.sha, candidateTag, stagingWorkflowSha,
      runId: env.GITHUB_RUN_ID, runAttempt: env.GITHUB_RUN_ATTEMPT, event: env.GITHUB_EVENT_NAME,
      ref: env.GITHUB_REF, workflowRef: env.FEDRIL_WORKFLOW_REF,
      comparisonBaseSha: mode === 'ci' ? env.FEDRIL_COMPARISON_BASE_SHA || '0'.repeat(40) : null,
      prHeadSha: mode === 'ci' ? env.FEDRIL_PR_HEAD_SHA || null : null };
    admission.validateContext(context);
    const selected = env.FEDRIL_ADMISSION_CHECK_ID ?
      await reconcileExactAdmission(context, env.FEDRIL_ADMISSION_CHECK_ID, read, token) : await wait(context, read, token);
    const root = join(env.RUNNER_TEMP, `fedril-trusted-${context.runId}-${context.runAttempt}`);
    await makeDirectory(root, { mode: 0o700 });
    const document = { context, carrier: selected.carrier, checkRunId: selected.checkRunId,
      sourceSha: env.GITHUB_SHA, repositoryId: env.GITHUB_REPOSITORY_ID, approvedManifest, stagingRun };
    await put(join(root, 'admission-context.json'), JSON.stringify(document), { flag: 'wx', mode: 0o600 });
    {
      for (const path of admission.EXPECTED_HELPERS) {
        const bytes = await raw(repository, workflowSha, path, 1024 * 1024);
        if (admission.hash(bytes) !== selected.carrier.helperSha256[path]) fixedFailure();
        const destination = join(root, path);
        await makeDirectory(dirname(destination), { recursive: true, mode: 0o700 });
        await put(destination, bytes, { flag: 'wx', mode: 0o600 });
      }
      if (mode !== 'ci') {
        const baseline = selected.carrier.baselineSnapshot;
        const bytes = await raw(repository, workflowSha, baseline.path, 64 * 1024 * 1024);
        if (admission.hash(bytes) !== baseline.sha256) fixedFailure();
        await put(join(root, 'baseline.snapshot.json'), bytes, { flag: 'wx', mode: 0o600 });
        await put(join(root, 'approved-release.json'), approvedManifestBytes, { flag: 'wx', mode: 0o600 });
        if (stagingRun) await put(join(root, 'approved-staging-run.json'), JSON.stringify(stagingRun), { flag: 'wx', mode: 0o600 });
      }
    }
    await append(env.GITHUB_ENV, `FEDRIL_TRUSTED_ROOT=${root}\nFEDRIL_ADMITTED_SHA=${candidateSha}\n`);
    return { trustedRoot: root, candidateSha, workflowSha, checkRunId: selected.checkRunId, releaseAuthorized: false };
  } catch { fixedFailure(); }
}
