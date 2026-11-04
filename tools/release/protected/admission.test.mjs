import test from 'node:test';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import { EXPECTED_HELPERS, CHECK_NAME, hash, requireAdmission, validateContext, checkPlacementSha, currentAdmissionTargets } from './admission.mjs';
import { readAllChecks, waitForAdmission, reconcileExactAdmission, bootstrapFromEnvironment, providerJson, rawBytes } from './inline-bootstrap.mjs';
import { buildCandidateSnapshot, comparisonAuthority } from './runner.mjs';
import { controlledGitArgv, gitEnvironment } from './native-checkout.mjs';

const sha = character => character.repeat(40), digest = character => character.repeat(64);
const clone = value => JSON.parse(JSON.stringify(value));
function fixture(mode = 'ci') {
  const context = { repository: 'owner/repository', appId: '101', mode, workflowSha: sha('a'), candidateSha: sha('b'),
    candidateTreeSha: sha('c'), candidateTag: mode === 'ci' ? null : 'v1.2.3-rc.1',
    stagingWorkflowSha: mode === 'production' ? sha('d') : null, runId: '202', runAttempt: '1',
    comparisonBaseSha: mode === 'ci' ? sha('a') : null, prHeadSha: mode === 'ci' ? sha('d') : null,
    event: mode === 'ci' ? 'pull_request' : 'workflow_dispatch', ref: mode === 'ci' ? 'refs/pull/7/merge' : 'refs/heads/release-main' };
  const path = mode === 'ci' ? 'ci.yml' : mode === 'staging' ? 'staging.yml' : 'production-release.yml';
  context.workflowRef = `${context.repository}/.github/workflows/${path}@${context.ref}`;
  const carrier = { schemaVersion: 1, purpose: 'fedril-workflow-admission-v1', repository: context.repository,
    runId: context.runId, runAttempt: context.runAttempt, mode, workflowSha: context.workflowSha,
    candidate: { sha: context.candidateSha, treeSha: context.candidateTreeSha, tag: context.candidateTag,
      stagingWorkflowSha: context.stagingWorkflowSha }, bindingSha256: digest('e'),
    bootstrapPolicyBase64: mode === 'ci' ? null : Buffer.from('{}').toString('base64'),
    canonicalAuthorityBase64: mode === 'ci' ? Buffer.from(JSON.stringify({schemaVersion:2,purpose:'classified-ci-history',repository:context.repository,workflowSha:context.workflowSha,tipSha:context.candidateSha,sourceRef:context.ref,comparisonBaseSha:context.comparisonBaseSha,prHeadSha:context.prHeadSha})).toString('base64') : Buffer.from('{}').toString('base64'),
    helperSha256: Object.fromEntries(EXPECTED_HELPERS.map(path => [path, digest('f')])),
    baselineSnapshot: mode === 'ci' ? null : { path: 'release/migration-baseline.snapshot.json', sha256: digest('f') } };
  const check = { id: 303, app: { id: 101 }, head_sha: mode === 'ci' ? context.prHeadSha : context.candidateSha, name: CHECK_NAME,
    status: 'completed', conclusion: 'success', external_id: 'fedril-historical129:' + carrier.bindingSha256,
    details_url: `https://github.com/${context.repository}/actions/runs/${context.runId}/attempts/${context.runAttempt}`,
    output: { text: JSON.stringify(carrier) } };
  return { context, carrier, check };
}
function withCarrier(f, edit) { const value = clone(f.carrier); edit(value); return { ...f.check, output: { text: JSON.stringify(value) } }; }

test('CI admission binds merge test SHA separately from workflow SHA', () => {
  const f = fixture();
  const result = requireAdmission([f.check], f.context);
  assert.notEqual(result.carrier.candidate.sha, result.carrier.workflowSha);
  assert.equal(result.carrier.candidate.sha, f.context.candidateSha);
  assert.equal(result.releaseAuthorized, false);
  assert.ok(Object.isFrozen(result.carrier.candidate));
});

test('release admission requires exact finite helper map and baseline identity', () => {
  for (const mode of ['staging', 'production']) {
    const f = fixture(mode);
    assert.equal(requireAdmission([f.check], f.context).carrier.mode, mode);
    assert.throws(() => requireAdmission([withCarrier(f, c => delete c.helperSha256[EXPECTED_HELPERS[0]])], f.context));
    assert.throws(() => requireAdmission([withCarrier(f, c => c.baselineSnapshot.path = '../other')], f.context));
  }
});

test('wrong App head name state conclusion or attempt URL cannot admit', () => {
  const f = fixture();
  for (const change of [{ app: { id: 102 } }, { head_sha: sha('b') }, { name: 'other' },
    { status: 'queued' }, { conclusion: 'failure' }, { details_url: f.check.details_url + '/extra' },
    { details_url: f.check.details_url.replace('/attempts/1', '/attempts/2') }]) {
    assert.throws(() => requireAdmission([{ ...f.check, ...change }], f.context));
  }
});

test('duplicate current-attempt targets reject without latest-success selection', () => {
  const f = fixture();
  assert.throws(() => requireAdmission([f.check, { ...f.check, id: 304 }], f.context));
  assert.throws(() => requireAdmission([f.check, { ...f.check, id: 304, conclusion: 'failure' }], f.context));
  assert.throws(() => requireAdmission([f.check, { ...f.check, id: 304, status: 'queued', conclusion: null }], f.context));
  const old = { ...f.check, id: 304, details_url: f.check.details_url.replace('/attempts/1', '/attempts/2') };
  assert.equal(requireAdmission([old, f.check], f.context).checkRunId, 303);
});

test('carrier workflow run candidate tree and external binding must match', () => {
  const f = fixture();
  for (const edit of [c => c.workflowSha = sha('d'), c => c.runId = '999', c => c.runAttempt = '2',
    c => c.candidate.sha = sha('d'), c => c.candidate.treeSha = sha('d'), c => c.bindingSha256 = digest('f'),
    c => c.extra = 'unapproved', c => c.candidate.extra = true]) assert.throws(() => requireAdmission([withCarrier(f, edit)], f.context));
});

test('malformed overlarge and unexpected CI authority are rejected', () => {
  const f = fixture();
  for (const text of ['bad', '[]', 'x'.repeat(60001)]) assert.throws(() => requireAdmission([{ ...f.check, output: { text } }], f.context));
  assert.throws(() => requireAdmission([withCarrier(f, c => c.bootstrapPolicyBase64 = 'e30=')], f.context));
});

test('production requires real staging SHA and release context', () => {
  const f = fixture('production');
  for (const change of [{ stagingWorkflowSha: null }, { event: 'push' }, { ref: 'refs/heads/main' }, { workflowSha: 'invalid' }])
    assert.throws(() => validateContext({ ...f.context, ...change }));
});

test('CI preserves push dispatch and fork PR tested-merge contexts', () => {
  const f = fixture();
  for (const event of ['push', 'workflow_dispatch']) {
    const context = { ...f.context, event, prHeadSha: null, ref: 'refs/heads/release-main',
      workflowRef: 'owner/repository/.github/workflows/ci.yml@refs/heads/release-main' };
    assert.equal(validateContext(context), true);
  }
  assert.throws(() => validateContext({ ...f.context, ref: 'refs/pull/7/head' }));
  assert.throws(() => validateContext({ ...f.context, event: 'pull_request_target' }));
});

test('full bounded pagination is collected with filter all and no latest selector', async () => {
  const f = fixture(), calls = [];
  const checks = Array.from({ length: 101 }, (_, index) => ({ id: index + 1 }));
  const result = await readAllChecks(f.context, async (repository, path) => {
    calls.push(path);
    return { total_count: 101, check_runs: path.endsWith('page=1') ? checks.slice(0, 100) : checks.slice(100) };
  });
  assert.equal(result.length, 101); assert.equal(calls.length, 2);
  assert.ok(calls.every(path => path.includes('filter=all') && path.includes(`/commits/${f.context.prHeadSha}/`)));
});

test('truncated duplicate changing and excessive check populations fail closed', async () => {
  const f = fixture();
  for (const response of [{ total_count: 2, check_runs: [{ id: 1 }] }, { total_count: 2, check_runs: [{ id: 1 }, { id: 1 }] },
    { total_count: 1001, check_runs: [] }, { total_count: 0, check_runs: [{ id: 1 }] }]) {
    await assert.rejects(readAllChecks(f.context, async () => response));
  }
});

test('only absence waits; matching failure or duplicate stops immediately', async () => {
  const f = fixture(); let reads = 0, pauses = 0;
  const result = await waitForAdmission(f.context, async () => ({ total_count: reads++ ? 1 : 0, check_runs: reads === 1 ? [] : [f.check] }),
    'synthetic', async milliseconds => { pauses++; assert.equal(milliseconds, 15000); });
  assert.equal(result.checkRunId, 303); assert.equal(pauses, 1);
  for (const checks of [[{ ...f.check, conclusion: 'failure' }], [{ ...f.check, status: 'queued', conclusion: null }], [f.check, { ...f.check, id: 304 }]]) {
    let delays = 0;
    await assert.rejects(waitForAdmission(f.context, async () => ({ total_count: checks.length, check_runs: checks }),
      'synthetic', async () => { delays++; }));
    assert.equal(delays, 0);
  }
});

test('admission wait has finite observations and does not retry provider errors', async () => {
  const f = fixture(); let reads = 0, pauses = 0;
  await assert.rejects(waitForAdmission(f.context, async () => { reads++; return { total_count: 0, check_runs: [] }; },
    'synthetic', async () => { pauses++; }));
  assert.equal(reads, 20); assert.equal(pauses, 19);
  reads = 0;
  await assert.rejects(waitForAdmission(f.context, async () => { reads++; throw new Error('synthetic'); }, 'synthetic', async () => {}));
  assert.equal(reads, 1);
});

test('context snapshot cannot be changed while waiting', async () => {
  const f = fixture(), original = clone(f.context); let reads = 0;
  const result = await waitForAdmission(f.context, async (_, path) => {
    assert.ok(path.startsWith(`/commits/${original.prHeadSha}/check-runs?`));
    if (!reads++) { f.context.runAttempt = '2'; f.context.candidateSha = sha('f'); f.context.prHeadSha = sha('e'); return { total_count: 0, check_runs: [] }; }
    return { total_count: 1, check_runs: [f.check] };
  }, 'synthetic', async () => {});
  assert.equal(result.carrier.runAttempt, original.runAttempt);
});

test('valid ID handoff cannot hide duplicate or failed current-attempt target', async () => {
  const f = fixture();
  for (const duplicate of [{ ...f.check, id: 304 }, { ...f.check, id: 304, conclusion: 'failure' }]) {
    let exactGets = 0;
    await assert.rejects(reconcileExactAdmission(f.context, '303', async (repository, path) => {
      if (path.startsWith('/commits/')) return { total_count: 2, check_runs: [f.check, duplicate] };
      exactGets++; return f.check;
    }, 'synthetic'));
    assert.equal(exactGets, 0);
  }
  const paths = [];
  const result = await reconcileExactAdmission(f.context, '303', async (repository, path) => {
    paths.push(path); return path.startsWith('/commits/') ? { total_count: 1, check_runs: [f.check] } : f.check;
  }, 'synthetic');
  assert.equal(result.checkRunId, 303); assert.equal(paths.length, 2); assert.equal(paths[1], '/check-runs/303');
});

test('duplicate matching target during bootstrap handoff stops before helper and filesystem IO', async () => {
  const f = fixture(); let rawReads = 0, writes = 0;
  const env = { GITHUB_REPOSITORY: f.context.repository, GITHUB_REPOSITORY_ID: '404', GITHUB_SHA: f.context.candidateSha,
    GITHUB_EVENT_NAME: f.context.event, GITHUB_REF: f.context.ref, GITHUB_RUN_ID: '202', GITHUB_RUN_ATTEMPT: '1',
    FEDRIL_WORKFLOW_SHA: f.context.workflowSha, FEDRIL_WORKFLOW_REF: f.context.workflowRef, FEDRIL_MODE: 'ci',
    PROTECTED_ASSERTIONS_APP_ID: '101', FEDRIL_COMPARISON_BASE_SHA:f.context.comparisonBaseSha, FEDRIL_PR_HEAD_SHA:f.context.prHeadSha,
    FEDRIL_ADMISSION_CHECK_ID:'303', GH_TOKEN:'synthetic' };
  await assert.rejects(bootstrapFromEnvironment(env, {
    read: async (repo, path) => path.startsWith('/commits/') && path.includes('/check-runs?') ?
      { total_count: 2, check_runs: [f.check, { ...f.check, id: 304, conclusion: 'failure' }] } :
      { sha: f.context.candidateSha, commit: { tree: { sha: f.context.candidateTreeSha } } },
    raw: async () => { rawReads++; }, makeDirectory: async () => { writes++; }, put: async () => { writes++; }, append: async () => { writes++; },
  }));
  assert.equal(rawReads, 0); assert.equal(writes, 0);
});

test('raw helper fetch uses fixed HTTPS host with no token or redirects', async () => {
  let captured;
  const response = await rawBytes('owner/repository', sha('a'), EXPECTED_HELPERS[0], 10, async (url, options) => {
    captured = { url, options }; return { status: 200, body: [Buffer.from('abc')] };
  });
  assert.equal(response.toString(), 'abc');
  assert.equal(captured.url, `https://raw.githubusercontent.com/owner/repository/${sha('a')}/${EXPECTED_HELPERS[0]}`);
  assert.equal(captured.options.redirect, 'error');
  assert.equal(Object.hasOwn(captured.options.headers, 'Authorization'), false);
  await assert.rejects(rawBytes('owner/repository', sha('a'), '../escape', 10, async () => { throw new Error('must not fetch'); }));
});

test('provider errors redirect status and oversized bodies are fixed failures', async () => {
  for (const fake of [async () => { throw new Error('synthetic-token-never-echo'); },
    async () => ({ status: 302, body: [] }), async () => ({ status: 200, body: [Buffer.alloc(8 * 1024 * 1024 + 1)] })]) {
    await assert.rejects(providerJson('owner/repository', '/commits/' + sha('a'), 'synthetic', fake),
      { message: 'protected_bootstrap_held_reconcile_no_retry' });
  }
});

test('CI bootstrap selects exact merge source and writes no token or baseline payload', async () => {
  const f = fixture(), writes = [], directories = [], appends = [];
  const helperBytes = Buffer.from('synthetic helper');
  for (const path of EXPECTED_HELPERS) f.carrier.helperSha256[path] = hash(helperBytes);
  f.check.output.text = JSON.stringify(f.carrier);
  const env = { GITHUB_REPOSITORY: f.context.repository, GITHUB_REPOSITORY_ID: '404', GITHUB_SHA: f.context.candidateSha,
    GITHUB_EVENT_NAME: f.context.event, GITHUB_REF: f.context.ref, GITHUB_RUN_ID: '202', GITHUB_RUN_ATTEMPT: '1',
    FEDRIL_WORKFLOW_SHA: f.context.workflowSha, FEDRIL_WORKFLOW_REF: f.context.workflowRef, FEDRIL_MODE: 'ci',
    PROTECTED_ASSERTIONS_APP_ID: '101', FEDRIL_COMPARISON_BASE_SHA:f.context.comparisonBaseSha, FEDRIL_PR_HEAD_SHA:f.context.prHeadSha, GH_TOKEN: 'synthetic-token', RUNNER_TEMP: '/synthetic-temp', GITHUB_ENV: '/synthetic-env' };
  const result = await bootstrapFromEnvironment(env, {
    read: async (repo, path) => { assert.equal(path, '/commits/' + f.context.candidateSha); return { sha: f.context.candidateSha, commit: { tree: { sha: f.context.candidateTreeSha } } }; },
    raw: async () => helperBytes,
    wait: async context => requireAdmission([f.check], context),
    makeDirectory: async (...args) => directories.push(args), put: async (...args) => writes.push(args), append: async (...args) => appends.push(args),
  });
  assert.equal(result.candidateSha, f.context.candidateSha); assert.equal(writes.length, 1 + EXPECTED_HELPERS.length); assert.equal(directories.length, 1 + EXPECTED_HELPERS.length);
  assert.ok(!JSON.stringify(writes).includes('synthetic-token')); assert.equal(appends.length, 1);
});

test('controlled Git environment excludes credentials and proxy/config inheritance', () => {
  for (const key of ['HOME', 'GH_TOKEN', 'GITHUB_TOKEN', 'HTTPS_PROXY', 'GIT_CONFIG_COUNT', 'GIT_SSH_COMMAND']) assert.equal(Object.hasOwn(gitEnvironment, key), false);
  const args = controlledGitArgv('/synthetic-root', ['git', 'fetch', '--no-tags', 'https://github.com/owner/repository.git', sha('a')]);
  assert.ok(args.includes('credential.helper=')); assert.ok(args.includes('core.hooksPath=/synthetic-root/empty-hooks'));
  assert.equal(gitEnvironment.GIT_CONFIG_GLOBAL, '/dev/null');
  assert.throws(() => controlledGitArgv('/x', ['sh', '-c', 'bad']));
});

test('deleted pushes and invalid PR merge contexts stop before provider or filesystem IO', async () => {
  const f = fixture();
  for (const change of [{ GITHUB_SHA: '0'.repeat(40) }, { FEDRIL_EVENT_DELETED: 'true' },
    { GITHUB_REF: 'refs/pull/7/head' }, { FEDRIL_COMPARISON_BASE_SHA: '' }, { FEDRIL_PR_HEAD_SHA: '' }]) {
    let calls = 0;
    const env = { GITHUB_REPOSITORY: f.context.repository, FEDRIL_WORKFLOW_SHA: f.context.workflowSha, FEDRIL_MODE: 'ci',
      GITHUB_SHA: f.context.candidateSha, GITHUB_EVENT_NAME: 'pull_request', GITHUB_REF: f.context.ref,
      FEDRIL_COMPARISON_BASE_SHA: f.context.comparisonBaseSha, FEDRIL_PR_HEAD_SHA: f.context.prHeadSha, ...change };
    const called = async () => { calls++; throw new Error('must not run'); };
    await assert.rejects(bootstrapFromEnvironment(env, { read: called, raw: called, wait: called,
      makeDirectory: called, put: called, append: called }));
    assert.equal(calls, 0);
  }
});

test('candidate snapshot reads only positive current blobs and binds content identities', async () => {
  const content = Buffer.from('synthetic migration source');
  const oid = createHash('sha1').update(Buffer.concat([Buffer.from(`blob ${content.length}\0`), content])).digest('hex');
  const path = 'src/Gccs.Infrastructure/Persistence/Migrations/20261031000000_Synthetic.cs';
  const calls = [];
  const snapshot = JSON.parse(await buildCandidateSnapshot(async argv => {
    calls.push(argv);
    return { exitCode: 0, stdout: argv.includes('ls-tree') ? Buffer.from(`100644 blob ${oid}\t${path}\0` + `100644 blob ${oid}\tinfra/database/README.md\0`) : content };
  }, sha('b'), 'untagged-workflow-source'));
  assert.equal(snapshot.sourceCommitSha, sha('b')); assert.equal(snapshot.sourceTag, 'untagged-workflow-source');
  assert.equal(snapshot.entries.length, 1); assert.equal(snapshot.entries[0].gitBlobOid, oid);
  assert.equal(calls.length, 2); assert.ok(!calls.some(argv => argv.includes('fetch')));
});

test('candidate snapshot rejects symlink sources duplicate paths and changed object bytes', async () => {
  const path = 'infra/database/synthetic.sql', oid = sha('a');
  for (const listing of [`120000 blob ${oid}\t${path}\0`, `100644 blob ${oid}\t${path}\0`]) {
    await assert.rejects(buildCandidateSnapshot(async argv => ({ exitCode: 0,
      stdout: argv.includes('ls-tree') ? Buffer.from(listing) : Buffer.from('wrong object bytes') }), sha('b'), 'untagged-workflow-source'));
  }
});

test('source-only comparison authority uses genuine workflow label without RC fabrication', () => {
  const f = fixture('staging'); f.carrier.candidate.tag = null; f.carrier.candidate.sha = f.carrier.workflowSha;
  const result = comparisonAuthority(f.carrier, { sourceCommitSha: sha('1'), sourceTag: 'v1.0.0' }, digest('2'),
    { rollback: { sourceTag: 'v1.0.0' } }, { baseline: { tag: 'v1.0.0', commitSha: sha('1') } }, 'unused');
  assert.equal(result.schemaVersion, 2); assert.equal(result.purpose, 'untagged-staging-source-comparison');
  assert.equal(result.candidatePin.tag, 'untagged-workflow-source'); assert.equal(Object.hasOwn(result, 'version'), false);
  assert.equal(result.workflowSha, result.candidatePin.commitSha);
});

test('workflow drafts contain the current exact inline source and mode bindings', async () => {
  const base = new URL('../../../', import.meta.url);
  const a = await readFile(new URL('tools/release/protected/admission.mjs', base), 'utf8');
  const b = (await readFile(new URL('tools/release/protected/inline-bootstrap.mjs', base), 'utf8')).replace("import * as admission from './admission.mjs';\n", '');
  const prefix = a + '\nconst admission = { CHECK_NAME, EXPECTED_HELPERS, hash, decodeAuthority, validateContext, checkPlacementSha, requireAdmission, currentAdmissionTargets };\n' + b;
  for (const [name, mode] of [['ci.yml', 'ci'], ['staging.yml', 'staging'], ['production-release.yml', 'production']]) {
    const yaml = await readFile(new URL('.github/workflows/' + name, base), 'utf8');
    const blocks = yaml.split("node --input-type=module <<'FEDRIL_BOOTSTRAP'\n").slice(1);
    assert.equal(blocks.length, name === 'ci.yml' ? 2 : 1);
    for (const block of blocks) {
      const source = block.split('          FEDRIL_BOOTSTRAP')[0].split('\n').map(line => line.startsWith('          ') ? line.slice(10) : line).join('\n');
      assert.ok(source.startsWith(prefix));
    }
    assert.ok(yaml.includes('FEDRIL_MODE: ' + mode));
  }
});

test('release normal paths remove broad fetch and Git migration diff while preserving guards', async () => {
  for (const name of ['staging.yml', 'production-release.yml']) {
    const source = await readFile(new URL('../../../.github/workflows/' + name, import.meta.url), 'utf8');
    assert.ok(!source.includes('fetch-depth: 0')); assert.ok(!source.includes('git fetch ')); assert.ok(!source.includes('migration-diff'));
    for (const guard of ['API_PACKAGE_GUARD_SHA256', 'No-CUI', 'check-cleanup-quiescence.mjs', 'azure/login@']) assert.ok(source.includes(guard));
    assert.ok(source.indexOf('Authenticate exact current-attempt') < source.indexOf('azure/login@'));
  }
});

test('production final-publication job retains its baseline byte hash', async () => {
  const next = await readFile(new URL('../../../.github/workflows/production-release.yml', import.meta.url), 'utf8');
  assert.equal(hash(Buffer.from(next.split('  publish-release-record:\n')[1])), '57fea7974fef83430301e8aac8c8a0833a888946c9dca45b583c398a323966c7');
  const staging = await readFile(new URL('../../../.github/workflows/staging.yml', import.meta.url), 'utf8');
  assert.ok(!staging.includes('$RUNNER_TEMP/check-azure-settings.mjs'));
  assert.ok(!staging.includes('$RUNNER_TEMP/check-cleanup-quiescence.mjs'));
});

test('one CI admission gates backend exclusions without changing merge checkout identity', async () => {
  const ci = await readFile(new URL('../../../.github/workflows/ci.yml', import.meta.url), 'utf8');
  assert.equal((ci.match(/^  protected-admission:/gm) ?? []).length, 1);
  const backend = ci.split('  backend-tests:\n')[1].split('\n  backend:\n')[0];
  assert.ok(backend.includes('needs: protected-admission'));
  assert.ok(backend.includes('ref: $' + '{{ needs.protected-admission.outputs.tested_sha }}'));
  assert.ok(backend.includes('test "$FEDRIL_PROTECTED_ASSERTIONS_ADMITTED_SHA" = "$GITHUB_SHA"'));
  assert.ok(!ci.includes('fetch-depth: 0')); // Finite classified history preserves and extends secret coverage.
  const prerequisites = ci.split('  backend-prerequisites:\n')[1].split('\n  backend-tests:\n')[0];
  assert.ok(prerequisites.includes('needs: protected-admission'));
  assert.ok(prerequisites.includes('run: node --test tools/release/protected/*.test.mjs'));
  assert.ok(prerequisites.indexOf('Test trusted source and admission controls') < prerequisites.indexOf('Setup .NET SDK'));
  assert.ok(!prerequisites.includes('continue-on-error'));

});


test('PR check placement uses head while carrier still binds tested merge and tree', () => {
  const f = fixture();
  const headCheck = { ...f.check, head_sha: f.context.prHeadSha };
  const admitted = requireAdmission([headCheck], f.context);
  assert.equal(admitted.carrier.candidate.sha, f.context.candidateSha);
  assert.equal(admitted.carrier.candidate.treeSha, f.context.candidateTreeSha);
  assert.notEqual(headCheck.head_sha, admitted.carrier.candidate.sha);
  assert.throws(() => requireAdmission([{ ...headCheck, head_sha: f.context.candidateSha }], f.context));
});

test('PR listing queries only validated PR head placement', async () => {
  const f = fixture(), calls = [];
  const result = await readAllChecks(f.context, async (repository, route) => {
    calls.push(route);
    assert.ok(route.startsWith(`/commits/${f.context.prHeadSha}/check-runs?`));
    return { total_count: 0, check_runs: [] };
  });
  assert.deepEqual(result, []); assert.equal(calls.length, 1);
});


test('one shared placement rule is PR-only and preserves non-PR CI and release subjects', async () => {
  const pr = fixture();
  assert.equal(checkPlacementSha(pr.context), pr.context.prHeadSha);
  assert.equal(currentAdmissionTargets([pr.check], pr.context).length, 1);
  assert.equal(currentAdmissionTargets([{ ...pr.check, head_sha: pr.context.candidateSha }], pr.context).length, 0);
  for (const mode of ['staging', 'production']) {
    const f = fixture(mode);
    assert.equal(checkPlacementSha(f.context), f.context.candidateSha);
    assert.equal(requireAdmission([f.check], f.context).carrier.candidate.sha, f.context.candidateSha);
    assert.throws(() => requireAdmission([{ ...f.check, head_sha: sha('e') }], f.context));
    await readAllChecks(f.context, async (_, path) => {
      assert.ok(path.startsWith(`/commits/${f.context.candidateSha}/check-runs?`));
      return { total_count: 0, check_runs: [] };
    });
  }
  for (const event of ['push', 'workflow_dispatch']) {
    const f = fixture();
    Object.assign(f.context, { event, prHeadSha: null, ref: 'refs/heads/release-main',
      workflowRef: 'owner/repository/.github/workflows/ci.yml@refs/heads/release-main' });
    const authority = JSON.parse(Buffer.from(f.carrier.canonicalAuthorityBase64, 'base64'));
    Object.assign(authority, { sourceRef: f.context.ref, prHeadSha: null });
    f.carrier.canonicalAuthorityBase64 = Buffer.from(JSON.stringify(authority)).toString('base64');
    f.check.head_sha = f.context.candidateSha; f.check.output.text = JSON.stringify(f.carrier);
    assert.equal(checkPlacementSha(f.context), f.context.candidateSha);
    assert.equal(requireAdmission([f.check], f.context).carrier.candidate.sha, f.context.candidateSha);
    assert.throws(() => requireAdmission([{ ...f.check, head_sha: sha('d') }], f.context));
    await readAllChecks(f.context, async (_, path) => {
      assert.ok(path.startsWith(`/commits/${f.context.candidateSha}/check-runs?`));
      return { total_count: 0, check_runs: [] };
    });
  }
});

test('invalid PR context rejects before listing or current-target selection', async () => {
  const f = fixture();
  for (const change of [{ prHeadSha: null }, { prHeadSha: 'invalid' }, { prHeadSha: sha('0') },
    { comparisonBaseSha: sha('0') }, { ref: 'refs/pull/7/head' }, { workflowRef: 'unbound' }]) {
    const context = { ...f.context, ...change }; let reads = 0;
    await assert.rejects(readAllChecks(context, async () => { reads++; return { total_count: 0, check_runs: [] }; }));
    assert.equal(reads, 0);
    assert.throws(() => currentAdmissionTargets([], context));
    assert.throws(() => checkPlacementSha(context));
  }
});

test('PR placement cannot replace candidate tree base or head authority binding', () => {
  const f = fixture();
  for (const change of [{ candidateSha: sha('e') }, { candidateTreeSha: sha('e') }, { comparisonBaseSha: sha('e') },
    { workflowSha: sha('e') }, { runId: '999' }, { runAttempt: '2' }]) {
    assert.throws(() => requireAdmission([f.check], { ...f.context, ...change }));
  }
  const changedHead = { ...f.context, prHeadSha: sha('e') };
  assert.throws(() => requireAdmission([{ ...f.check, head_sha: changedHead.prHeadSha }], changedHead));
  for (const field of ['comparisonBaseSha', 'prHeadSha', 'tipSha']) {
    const carrier = clone(f.carrier), authority = JSON.parse(Buffer.from(carrier.canonicalAuthorityBase64, 'base64'));
    authority[field] = sha('e'); carrier.canonicalAuthorityBase64 = Buffer.from(JSON.stringify(authority)).toString('base64');
    assert.throws(() => requireAdmission([{ ...f.check, output: { text: JSON.stringify(carrier) } }], f.context));
  }
});

test('exact-ID reconciliation rejects merge placement after valid PR-head listing', async () => {
  const f = fixture(), paths = [];
  await assert.rejects(reconcileExactAdmission(f.context, String(f.check.id), async (_, path) => {
    paths.push(path);
    return path.startsWith('/commits/') ? { total_count: 1, check_runs: [f.check] } : { ...f.check, head_sha: f.context.candidateSha };
  }, 'synthetic'));
  assert.equal(paths.length, 2);
  assert.ok(paths[0].startsWith(`/commits/${f.context.prHeadSha}/check-runs?`));
  assert.equal(paths[1], '/check-runs/' + f.check.id);
});
