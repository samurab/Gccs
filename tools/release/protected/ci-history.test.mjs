import test from 'node:test';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import { planCanonicalTransport } from './finite-canonical-transport.mjs';
import { prepareClassifiedCiSource } from './ci-runner.mjs';

const sha = character => character.repeat(40);
const encoded = value => Buffer.from(JSON.stringify(value));
const pin = bytes => createHash('sha256').update(bytes).digest('hex');
function fixture() {
  const context = { repository: 'owner/repository', sourceRef: 'refs/pull/7/merge', workflowSha: sha('2'),
    candidateSha: sha('4'), comparisonBaseSha: sha('2'), prHeadSha: sha('3') };
  const authority = { schemaVersion: 2, purpose: 'classified-ci-history', repository: context.repository, sourceRef: context.sourceRef,
    workflowSha: context.workflowSha, rootSha: sha('1'), tipSha: sha('4'), comparisonBaseSha: context.comparisonBaseSha,
    prHeadSha: context.prHeadSha, commits: [
      { sha: sha('1'), treeSha: sha('a'), parents: [] }, { sha: sha('2'), treeSha: sha('b'), parents: [sha('1')] },
      { sha: sha('3'), treeSha: sha('c'), parents: [sha('1')] }, { sha: sha('4'), treeSha: sha('d'), parents: [sha('2'), sha('3')] },
    ], classifiedTrees: ['a', 'b', 'c', 'd'].map(character => ({ treeSha: sha(character), inventorySha256: character.repeat(64) })),
    classificationReceiptSha256: 'f'.repeat(64) };
  return { context, authority };
}
function fakeIo(f, options = {}) {
  const calls = []; let shallowCount = 0;
  return { calls, io: {
    async checkoutExact(config) { calls.push(['checkout', config]); return { repository: f.context.repository, headSha: f.context.workflowSha, shallow: true }; },
    async runGit(argv) {
      calls.push(['git', argv]); assert.ok(Object.isFrozen(argv));
      const result = (text = '', exitCode = 0) => ({ exitCode, stdout: Buffer.from(text) });
      if (argv.includes('show-ref')) return result('', 1);
      if (argv.includes('--is-shallow-repository')) return result(String(shallowCount++ === 0));
      if (argv.includes('fetch')) { if (options.fetchFail) throw new Error('synthetic-private'); return result(); }
      if (argv.includes('--verify')) return result(f.authority.tipSha);
      if (argv.includes('log')) return result(f.authority.commits.map(c => `${c.sha}\0${c.treeSha}\0${c.parents.join(' ')}`).join('\n'));
      if (argv.includes('merge-base')) return result();
      throw new Error('unexpected synthetic command');
    },
  } };
}

test('CI finite authority includes root, merge parents, workflow and exact comparison boundary', async () => {
  const f = fixture(), bytes = encoded(f.authority), { io, calls } = fakeIo(f);
  const result = await prepareClassifiedCiSource(bytes, pin(bytes), f.context, io);
  assert.equal(result.commitsVerified, 4); assert.equal(result.nativeAncestryChecks, 3);
  const fetch = calls.find(([kind, argv]) => kind === 'git' && argv.includes('fetch'))[1];
  assert.ok(fetch.includes('--unshallow')); assert.ok(fetch.includes('--no-tags'));
  assert.equal(fetch.at(-1), `${sha('4')}:refs/fedril/verified-ci/${sha('4')}`);
  assert.ok(!fetch.includes('--all')); assert.ok(!fetch.some(arg => arg.includes('refs/heads/main')));
  assert.equal(result.releaseAuthorized, false);
});

test('legacy parent boundary missing base missing head and incomplete classification reject before IO', async () => {
  for (const edit of [a => a.commits[0].parents = [sha('e')], a => a.commits.splice(1, 1),
    a => a.commits.splice(2, 1), a => a.classifiedTrees.pop(), a => a.commits.push({ sha: sha('e'), treeSha: sha('e'), parents: [] })]) {
    const f = fixture(); edit(f.authority); const bytes = encoded(f.authority); let calls = 0;
    await assert.rejects(prepareClassifiedCiSource(bytes, pin(bytes), f.context, { checkoutExact() { calls++; }, runGit() { calls++; } }));
    assert.equal(calls, 0);
  }
});

test('PR head success cannot substitute for actual merge test SHA or comparison base', () => {
  const f = fixture(), bytes = encoded(f.authority);
  for (const change of [{ candidateSha: sha('3') }, { comparisonBaseSha: sha('3') }, { prHeadSha: sha('2') },
    { workflowSha: sha('1') }, { sourceRef: 'refs/pull/7/head' }]) {
    assert.throws(() => planCanonicalTransport(bytes, pin(bytes), { ...f.context, ...change, shallow: true }));
  }
});

test('initial branch zero-before accepts only complete classified root ancestry', () => {
  const f = fixture(); f.context.sourceRef = 'refs/heads/release-main'; f.context.comparisonBaseSha = '0'.repeat(40); f.context.prHeadSha = null;
  Object.assign(f.authority, { sourceRef: f.context.sourceRef, comparisonBaseSha: f.context.comparisonBaseSha, prHeadSha: null });
  const bytes = encoded(f.authority);
  assert.equal(planCanonicalTransport(bytes, pin(bytes), { ...f.context, shallow: true }).authority.commits.length, 4);
  f.authority.commits[0].parents = [sha('e')];
  assert.throws(() => planCanonicalTransport(encoded(f.authority), pin(encoded(f.authority)), { ...f.context, shallow: true }));
});

test('CI fetch failure stops once without retry', async () => {
  const f = fixture(), bytes = encoded(f.authority), { io, calls } = fakeIo(f, { fetchFail: true });
  await assert.rejects(prepareClassifiedCiSource(bytes, pin(bytes), f.context, io), { message: 'classified_ci_source_failed_reconcile_no_retry' });
  assert.equal(calls.filter(([kind, argv]) => kind === 'git' && argv.includes('fetch')).length, 1);
});

test('CI authority and context remain bound while checkout awaits', async () => {
  const f = fixture(), bytes = encoded(f.authority), { io, calls } = fakeIo(f);
  let release;
  io.checkoutExact = () => new Promise(resolve => { release = resolve; });
  const running = prepareClassifiedCiSource(bytes, pin(bytes), f.context, io);
  bytes.fill(0); f.context.candidateSha = sha('e'); io.runGit = () => { throw new Error('mutated callback'); };
  release({ repository: 'owner/repository', headSha: sha('2'), shallow: true });
  const result = await running;
  assert.equal(result.tipSha, sha('4')); assert.equal(result.commitsVerified, 4);
  assert.equal(calls.filter(([kind, argv]) => kind === 'git' && argv.includes('fetch')).length, 1);
});

test('secret scan uses complete finite checkout and explicit full tip history without all refs', async () => {
  const source = await readFile(new URL('../../../.github/workflows/ci.yml', import.meta.url), 'utf8');
  const job = source.split('  secret-scan:\n')[1];
  assert.ok(job.includes('needs: protected-admission'));
  assert.ok(job.includes('ci-runner.mjs'));
  assert.ok(!job.includes('fetch-depth: 0'));
  assert.ok(job.includes('gitleaks/gitleaks-action@e0c47f4f8be36e29cdc102c57e68cb5cbf0e8d1e'));
  assert.ok(job.includes('gitleaks git --redact --exit-code=2'));
  assert.ok(job.includes('--log-opts="--full-history -m $CI_TESTED_SHA"'));
  assert.ok(!job.includes('--all'));
});
