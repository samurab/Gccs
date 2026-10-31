import { createHash } from 'node:crypto';
import { execFile } from 'node:child_process';
import { readFile, writeFile, appendFile, readdir, lstat } from 'node:fs/promises';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const ZERO = '0'.repeat(40), LIMIT = 4096;
const sha = value => typeof value === 'string' && /^[a-f0-9]{40}$/.test(value);
const fail = () => { throw new Error('finite_artifact_history_held_reconcile_no_retry'); };
const hash = bytes => createHash('sha256').update(bytes).digest('hex');

export function eventContext(env) {
  const repository = env.GITHUB_REPOSITORY, event = env.GITHUB_EVENT_NAME, sourceRef = env.GITHUB_REF;
  if (!/^[A-Za-z0-9][A-Za-z0-9-]*\/[A-Za-z0-9][A-Za-z0-9._-]*$/.test(repository ?? '') ||
      !['push', 'pull_request'].includes(event) || !sha(env.GITHUB_SHA) ||
      !/^refs\/(heads\/.+|tags\/.+|pull\/[1-9][0-9]*\/merge)$/.test(sourceRef ?? '')) fail();
  const base = env.ARTIFACT_COMPARISON_BASE_SHA;
  if (!sha(base)) fail();
  if (event === 'pull_request' && (!sourceRef.startsWith('refs/pull/') || !sha(env.ARTIFACT_PR_HEAD_SHA) || base === ZERO)) fail();
  const deleted = event === 'push' && env.ARTIFACT_EVENT_DELETED === 'true';
  if (!deleted && env.GITHUB_SHA === ZERO) fail();
  return Object.freeze({ repository, event, sourceRef, tipObjectSha: env.GITHUB_SHA, baseObjectSha: base,
    prHeadSha: event === 'pull_request' ? env.ARTIFACT_PR_HEAD_SHA : null, deleted });
}

export async function captureHistory(context, readObject) {
  context = Object.freeze({ ...context });
  if (context.deleted) return { schemaVersion: 1, context, deletionOnly: true, commits: [] };
  const bySha = new Map();
  const resolveObject = async original => {
    let current = original;
    for (let depth = 0; depth < 16; depth++) {
      if (!sha(current) || current === ZERO) fail();
      const result = await readObject('commit', current);
      if (result.status === 200) {
        const commit = result.body;
        if (commit?.sha !== current || !sha(commit.tree?.sha) || !Array.isArray(commit.parents) || commit.parents.length > 64 ||
            commit.parents.some(parent => !sha(parent.sha) || parent.sha === ZERO) ||
            new Set(commit.parents.map(parent => parent.sha)).size !== commit.parents.length) fail();
        bySha.set(current, { sha: current, treeSha: commit.tree.sha, parents: commit.parents.map(parent => parent.sha) });
        return current;
      }
      if (result.status !== 404) fail();
      const tag = await readObject('tag', current);
      if (tag.status !== 200 || tag.body?.sha !== current || !['tag', 'commit'].includes(tag.body.object?.type) || !sha(tag.body.object?.sha)) fail();
      current = tag.body.object.sha;
    }
    fail();
  };
  const tipSha = await resolveObject(context.tipObjectSha);
  const baseSha = context.baseObjectSha === ZERO ? ZERO : await resolveObject(context.baseObjectSha);
  const pending = [tipSha, ...(baseSha === ZERO ? [] : [baseSha])], visited = new Set();
  while (pending.length) {
    const id = pending.pop(); if (visited.has(id)) continue;
    if (visited.size >= LIMIT) fail();
    await resolveObject(id); visited.add(id);
    pending.push(...bySha.get(id).parents);
  }
  if (bySha.size > LIMIT || (context.prHeadSha !== null && !visited.has(context.prHeadSha))) fail();
  const commits = [...visited].sort().map(id => bySha.get(id));
  // A separate traversal detects cycles even when all parents were returned.
  const done = new Set(), active = new Set();
  const visit = id => {
    if (active.has(id)) fail(); if (done.has(id)) return;
    active.add(id); for (const parent of bySha.get(id).parents) visit(parent); active.delete(id); done.add(id);
  };
  try { for (const id of [tipSha, ...(baseSha === ZERO ? [] : [baseSha])]) visit(id); } catch { fail(); }
  return { schemaVersion: 1, context, deletionOnly: false, tipSha, baseSha, commits,
    sourceRoots: commits.filter(commit => commit.parents.length === 0).map(commit => commit.sha),
    limits: 'Authenticated provider metadata consistency; not independent source classification or an object intake sandbox.' };
}

export async function readProviderObject(repository, token, type, identity, fetchImpl = fetch) {
  if (!['commit', 'tag'].includes(type) || !sha(identity) || !/^[A-Za-z0-9][A-Za-z0-9-]*\/[A-Za-z0-9][A-Za-z0-9._-]*$/.test(repository ?? '') || typeof token !== 'string' || !token || /[\r\n]/.test(token)) fail();
  try {
    const response = await fetchImpl(`https://api.github.com/repos/${repository}/git/${type === 'commit' ? 'commits' : 'tags'}/${identity}`,
      { redirect: 'error', signal: AbortSignal.timeout(30000), headers: { Authorization: `Bearer ${token}`,
        Accept: 'application/vnd.github+json', 'X-GitHub-Api-Version': '2026-03-10', 'User-Agent': 'FeDril-finite-artifact-policy' } });
    if (response.status === 404) return { status: 404 };
    if (response.status !== 200 || !response.body) fail();
    const chunks = []; let size = 0;
    for await (const chunk of response.body) { size += chunk.length; if (size > 1024 * 1024) fail(); chunks.push(Buffer.from(chunk)); }
    return { status: 200, body: JSON.parse(Buffer.concat(chunks).toString('utf8')) };
  } catch { fail(); }
}

export const gitEnv = Object.freeze({ PATH: '/usr/bin:/bin', LANG: 'C', LC_ALL: 'C', TZ: 'UTC', GIT_CONFIG_NOSYSTEM: '1',
  GIT_CONFIG_SYSTEM: '/dev/null', GIT_CONFIG_GLOBAL: '/dev/null', GIT_NO_REPLACE_OBJECTS: '1', GIT_TERMINAL_PROMPT: '0' });

export async function verifyAndFetch(plan, runGit) {
  const serialized = JSON.stringify(plan);
  if (Buffer.byteLength(serialized) > 2 * 1024 * 1024) fail();
  plan = JSON.parse(serialized);
  if (plan.schemaVersion !== 1 || typeof plan.deletionOnly !== 'boolean' || !Array.isArray(plan.commits) ||
      plan.commits.length > LIMIT || !/^[A-Za-z0-9][A-Za-z0-9-]*\/[A-Za-z0-9][A-Za-z0-9._-]*$/.test(plan.context?.repository ?? '')) fail();
  if (!plan.deletionOnly) {
    const map = new Map();
    for (const c of plan.commits) {
      if (!sha(c.sha) || !sha(c.treeSha) || !Array.isArray(c.parents) || c.parents.some(parent => !sha(parent)) || map.has(c.sha)) fail();
      map.set(c.sha, c);
    }
    if (!sha(plan.tipSha) || !sha(plan.baseSha) || !map.has(plan.tipSha) || (plan.baseSha !== ZERO && !map.has(plan.baseSha)) ||
        [...map.values()].some(c => c.parents.some(parent => !map.has(parent)))) fail();
  }
  if (plan.deletionOnly) return { deletionOnly: true, sourceBlobsInspected: false, releaseAuthorized: false };
  const call = async argv => {
    let result; try { result = await runGit(Object.freeze([...argv])); } catch { fail(); }
    if (!result || result.exitCode !== 0 || !Buffer.isBuffer(result.stdout) || result.stdout.length > 2 * 1024 * 1024) fail();
    return result.stdout.toString('utf8').trim();
  };
  if (await call(['rev-parse', '--verify', 'HEAD']) !== plan.tipSha) fail();
  const shallow = await call(['rev-parse', '--is-shallow-repository']);
  if (!['true', 'false'].includes(shallow)) fail();
  const starts = [...new Set([plan.tipSha, plan.baseSha].filter(id => id !== ZERO))];
  await call(['fetch', '--no-tags', '--no-prune', '--no-prune-tags', '--no-recurse-submodules', '--no-auto-maintenance',
    '--no-write-fetch-head', '--no-write-commit-graph', '--refmap=', ...(shallow === 'true' ? ['--unshallow'] : []),
    `https://github.com/${plan.context.repository}.git`, ...starts.map(id => `${id}:refs/fedril/artifact-policy/${id}`)]);
  if (await call(['rev-parse', '--is-shallow-repository']) !== 'false') fail();
  const graph = await call(['--no-pager', 'log', '--no-decorate', '--no-show-signature', '--format=%H%x00%T%x00%P', ...starts]);
  const observed = graph.split('\n').map(line => {
    const fields = line.split('\0');
    if (fields.length !== 3 || !sha(fields[0]) || !sha(fields[1]) ||
        (fields[2] && !/^[a-f0-9]{40}( [a-f0-9]{40})*$/.test(fields[2]))) fail();
    return { sha: fields[0], treeSha: fields[1], parents: fields[2] ? fields[2].split(' ') : [] };
  });
  const expected = [...plan.commits].sort((a, b) => a.sha.localeCompare(b.sha));
  observed.sort((a, b) => a.sha.localeCompare(b.sha));
  if (JSON.stringify(observed) !== JSON.stringify(expected)) fail();
  return { tipSha: plan.tipSha, comparisonBaseSha: plan.baseSha, commitsVerified: observed.length,
    completeEventHistoryAvailable: true, releaseAuthorized: false };
}

export function nativeGit(cwd) {
  return argv => new Promise((accept, reject) => {
    const prefix = ['--no-replace-objects', '-c', 'core.hooksPath=/dev/null', '-c', 'credential.helper=', '-c', 'gc.auto=0',
      '-c', 'maintenance.auto=false', '-c', 'fetch.fsckObjects=true'];
    execFile('/usr/bin/git', [...prefix, ...argv], { cwd, env: gitEnv, encoding: 'buffer', timeout: 30000, maxBuffer: 2 * 1024 * 1024 },
      (error, stdout) => { if (error) { reject(new Error('native_artifact_operation_uncertain')); return; } accept({ exitCode: 0, stdout }); });
  });
}

export async function main(mode, path, env = process.env) {
  try {
    if (mode === 'capture') {
      const context = eventContext(env), stat = await lstat(env.GITHUB_WORKSPACE);
      if (!stat.isDirectory() || stat.isSymbolicLink() || (await readdir(env.GITHUB_WORKSPACE)).length !== 0) fail();
      const plan = await captureHistory(context, (type, id) => readProviderObject(context.repository, env.GH_TOKEN, type, id));
      const bytes = Buffer.from(JSON.stringify(plan));
      await writeFile(path, bytes, { flag: 'wx', mode: 0o600 });
      await appendFile(env.GITHUB_ENV, `ARTIFACT_HISTORY_PIN=${hash(bytes)}\nARTIFACT_TIP_COMMIT_SHA=${plan.tipSha ?? ZERO}\nARTIFACT_BASE_COMMIT_SHA=${plan.baseSha ?? ZERO}\nARTIFACT_DELETE_ONLY=${plan.deletionOnly ? 'true' : 'false'}\n`);
    } else if (mode === 'verify') {
      const bytes = await readFile(path); if (bytes.length > 2 * 1024 * 1024 || hash(bytes) !== env.ARTIFACT_HISTORY_PIN) fail();
      await writeFile(path + '.verify-intent.json', JSON.stringify({planSha256:hash(bytes),state:'ONE_NATIVE_VERIFICATION_ATTEMPT'}), {flag:'wx',mode:0o600});
      const result = await verifyAndFetch(JSON.parse(bytes.toString('utf8')), nativeGit(env.GITHUB_WORKSPACE));
      await writeFile(path + '.result.json', JSON.stringify(result), { flag: 'wx', mode: 0o600 });
    } else fail();
  } catch { fail(); }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main(process.argv[2], process.argv[3]).catch(() => { console.error('Artifact policy held; preserve outputs and reconcile without retry.'); process.exitCode = 1; });
}
