import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { eventContext, captureHistory, verifyAndFetch, readProviderObject, gitEnv } from './finite-artifact-history.mjs';

const sha = value => value.repeat(40), zero = '0'.repeat(40);
const env = (event = 'push') => ({ GITHUB_REPOSITORY: 'owner/repository', GITHUB_EVENT_NAME: event, GITHUB_SHA: sha('3'),
  GITHUB_REF: event === 'push' ? 'refs/heads/release-main' : 'refs/pull/7/merge',
  ARTIFACT_COMPARISON_BASE_SHA: event === 'push' ? zero : sha('1'), ARTIFACT_PR_HEAD_SHA: sha('2') });
const commit = (id, parents = []) => ({ sha: sha(id), tree: { sha: sha(id) }, parents: parents.map(id => ({ sha: sha(id) })) });
const graph = { [sha('1')]: commit('1'), [sha('2')]: commit('2', ['1']), [sha('3')]: commit('3', ['1', '2']) };
const reader = async (type, id) => ({ status: 200, body: graph[id] });
const result = text => ({ exitCode: 0, stdout: Buffer.from(text) });
function fakeGit(plan, change = {}) {
  const calls = []; let shallow = 0;
  return { calls, run: async argv => {
    calls.push(argv); assert.ok(Object.isFrozen(argv));
    if (argv.includes('HEAD')) return result(plan.tipSha);
    if (argv.includes('--is-shallow-repository')) return result(String(shallow++ === 0));
    if (argv[0] === 'fetch') { if (change.failFetch) throw new Error('synthetic-private'); return result(''); }
    if (argv.includes('log')) return result(plan.commits.map(c => `${c.sha}\0${change.wrongTree ? sha('f') : c.treeSha}\0${c.parents.join(' ')}`).join('\n'));
    throw new Error('unexpected fake Git command');
  } };
}

test('initial parentless publication metadata needs only one source commit and zero boundary', async () => {
  let reads = 0;
  const plan = await captureHistory(eventContext(env()), async (type, id) => { reads++; assert.equal(type, 'commit'); return { status: 200, body: commit('3') }; });
  assert.equal(plan.commits.length, 1); assert.deepEqual(plan.sourceRoots, [sha('3')]); assert.equal(plan.baseSha, zero);
  assert.ok(reads <= 2);
  const io = fakeGit(plan); const verified = await verifyAndFetch(plan, io.run);
  assert.equal(verified.commitsVerified, 1);
  const fetch = io.calls.find(argv => argv[0] === 'fetch');
  assert.ok(fetch.includes('--unshallow')); assert.ok(fetch.includes('--no-tags')); assert.ok(!fetch.includes('--all'));
  assert.equal(fetch.at(-1), `${sha('3')}:refs/fedril/artifact-policy/${sha('3')}`);
});

test('PR complete merge/base/head ancestry is captured and verified without other refs', async () => {
  const plan = await captureHistory(eventContext(env('pull_request')), reader);
  assert.equal(plan.commits.length, 3); assert.equal(plan.baseSha, sha('1'));
  const io = fakeGit(plan); assert.equal((await verifyAndFetch(plan, io.run)).commitsVerified, 3);
  assert.equal(io.calls.filter(argv => argv[0] === 'fetch').length, 1);
});

test('tag object tip and before boundaries peel with metadata only', async () => {
  const context = { ...eventContext(env()), sourceRef: 'refs/tags/v1.0.0', tipObjectSha: sha('a'), baseObjectSha: sha('b') };
  const calls = [];
  const plan = await captureHistory(context, async (type, id) => {
    calls.push([type, id]);
    if ([sha('a'), sha('b')].includes(id)) return type === 'commit' ? { status: 404 } :
      { status: 200, body: { sha: id, object: { type: 'commit', sha: id === sha('a') ? sha('3') : sha('1') } } };
    return reader(type, id);
  });
  assert.equal(plan.tipSha, sha('3')); assert.equal(plan.baseSha, sha('1'));
  assert.ok(calls.every(([type]) => ['commit', 'tag'].includes(type)));
});

test('provider forbidden/missing/malformed graph or missing PR head fails before Git', async () => {
  for (const read of [async () => ({ status: 403 }), async () => ({ status: 404 }),
    async () => ({ status: 200, body: commit('1') }), async (type, id) => ({ status: 200, body: { ...graph[id], parents: [{ sha: sha('f') }] } })]) {
    await assert.rejects(captureHistory(eventContext(env()), read));
  }
  await assert.rejects(captureHistory({ ...eventContext(env('pull_request')), prHeadSha: sha('f') }, reader));
});

test('cyclic and excess-depth tag metadata fail closed', async () => {
  await assert.rejects(captureHistory(eventContext(env()), async () => ({ status: 200, body: commit('3', ['3']) })));
  await assert.rejects(captureHistory(eventContext(env()), async (type, id) => type === 'commit' ? { status: 404 } :
    { status: 200, body: { sha: id, object: { type: 'tag', sha: id } } }));
});

test('deletion event introduces no artifacts and performs no source fetch', async () => {
  const context = eventContext({ ...env(), ARTIFACT_EVENT_DELETED: 'true' }); let calls = 0;
  const plan = await captureHistory(context, async () => { calls++; });
  assert.equal((await verifyAndFetch(plan, async () => { calls++; })).deletionOnly, true); assert.equal(calls, 0);
});

test('invalid contexts or incomplete pinned graph stop before native IO', async () => {
  for (const change of [{ GITHUB_EVENT_NAME: 'workflow_dispatch' }, { GITHUB_SHA: zero }, { ARTIFACT_COMPARISON_BASE_SHA: 'bad' }])
    assert.throws(() => eventContext({ ...env(), ...change }));
  let calls = 0;
  await assert.rejects(verifyAndFetch({ schemaVersion: 1, deletionOnly: false, context: { repository: 'owner/repository' },
    tipSha: sha('3'), baseSha: zero, commits: [{ sha: sha('3'), treeSha: sha('3'), parents: [sha('f')] }] }, async () => { calls++; }));
  assert.equal(calls, 0);
});

test('native fetch failure and graph mismatch stop without retry', async () => {
  const plan = await captureHistory(eventContext(env()), reader);
  for (const change of [{ failFetch: true }, { wrongTree: true }]) {
    const io = fakeGit(plan, change); await assert.rejects(verifyAndFetch(plan, io.run));
    assert.equal(io.calls.filter(argv => argv[0] === 'fetch').length, 1);
  }
});

test('provider reader uses only fixed HTTPS metadata routes with no redirect or blob endpoint', async () => {
  const calls = [];
  const response = await readProviderObject('owner/repository', 'synthetic-token', 'commit', sha('3'), async (url, options) => {
    calls.push([url, options]); return { status: 200, body: [Buffer.from(JSON.stringify(commit('3')))] };
  });
  assert.equal(response.body.sha, sha('3')); assert.ok(calls[0][0].includes('/git/commits/'));
  assert.equal(calls[0][1].redirect, 'error'); assert.equal(calls.length, 1);
  assert.equal(Object.hasOwn(gitEnv, 'GH_TOKEN'), false); assert.equal(Object.hasOwn(gitEnv, 'HTTPS_PROXY'), false);
  await assert.rejects(readProviderObject('owner/repository', 'synthetic', 'blob', sha('3'), async () => {}));
});

test('source workflow retains existing triggers and scanner contract without broad history', async () => {
  const workflow = await readFile(new URL('../../.github/workflows/repository-artifact-policy.yml', import.meta.url), 'utf8');
  assert.ok(workflow.includes('branches: ["**"]')); assert.ok(workflow.includes('tags: ["**"]')); assert.ok(workflow.includes('  pull_request:'));
  assert.ok(workflow.includes('run-name: artifact-policy $' + '{{ github.ref }} $' + '{{ github.sha }} $' + '{{ github.event.before || \'none\' }}'));
  assert.ok(!workflow.includes('fetch-depth: 0')); assert.ok(!workflow.includes('git fetch --tags'));
  assert.ok(workflow.includes('node tools/security/check-sensitive-artifacts.mjs --changed-since "$ARTIFACT_BASE_COMMIT_SHA"'));
  assert.ok(workflow.indexOf('Verify exact finite event history') < workflow.indexOf('Reject database artifacts introduced'));
});
