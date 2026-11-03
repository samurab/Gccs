import test from 'node:test';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import childProcess from 'node:child_process';
import { syncBuiltinESMExports } from 'node:module';
import { prepareClassifiedCiSource, runCiFromFile, ciFailureDiagnostic } from './ci-runner.mjs';
import { EXPECTED_HELPERS } from './admission.mjs';

const sha = character => character.repeat(40);
const pin = bytes => createHash('sha256').update(bytes).digest('hex');
const canary = 'SYNTHETIC_PRIVATE_TOKEN_URL_BODY_ERROR_DO_NOT_EMIT';
const blocked = () => { throw new Error('UNEXPECTED_ACTUAL_PROCESS_OR_PROVIDER_PORT'); };
childProcess.execFile = blocked; childProcess.execFileSync = blocked;
syncBuiltinESMExports(); globalThis.fetch = blocked;

function fixture(failure = null) {
  const context = { repository: 'owner/repository', ref: 'refs/pull/7/merge', workflowSha: sha('2'),
    candidateSha: sha('4'), candidateTreeSha: sha('d'), comparisonBaseSha: sha('2'), prHeadSha: sha('3'), mode: 'ci' };
  const authority = { schemaVersion: 2, purpose: 'classified-ci-history', repository: context.repository,
    sourceRef: context.ref, workflowSha: context.workflowSha, rootSha: sha('1'), tipSha: sha('4'),
    comparisonBaseSha: context.comparisonBaseSha, prHeadSha: context.prHeadSha,
    commits: [{ sha: sha('1'), treeSha: sha('a'), parents: [] },
      { sha: sha('2'), treeSha: sha('b'), parents: [sha('1')] },
      { sha: sha('3'), treeSha: sha('c'), parents: [sha('1')] },
      { sha: sha('4'), treeSha: sha('d'), parents: [sha('2'), sha('3')] }],
    classifiedTrees: ['a','b','c','d'].map(c => ({ treeSha: sha(c), inventorySha256: c.repeat(64) })),
    classificationReceiptSha256: 'f'.repeat(64) };
  if (failure === 'authority') authority.commits[0].parents = [sha('e')];
  const authorityBytes = Buffer.from(JSON.stringify(authority));
  const helpers = Object.fromEntries([...EXPECTED_HELPERS].map(path => [path, Buffer.from(`SYNTHETIC_HELPER:${path}`)]));
  const carrier = { canonicalAuthorityBase64: authorityBytes.toString('base64'),
    helperSha256: Object.fromEntries(Object.entries(helpers).map(([path, bytes]) => [path, pin(bytes)])) };
  const document = { context, checkRunId: 7, carrier };
  const calls = { document:0, reconcile:0, helpers:0, native:0, checkout:0, git:[], materialize:0, write:0, close:0 };
  let shallow = 0;
  const privateFailure = () => {
    const error = new Error(canary);
    Object.assign(error, { killed:true, signal:'SIGTERM', code:'ETIMEDOUT', stdout:canary, stderr:canary });
    throw error;
  };
  const native = {
    async checkoutExact(config) {
      calls.checkout++;
      assert.deepEqual(config, { repository:context.repository, ref:context.workflowSha, 'fetch-depth':1,
        'fetch-tags':false, submodules:false, clean:false, 'persist-credentials':false });
      if (failure === 'checkout') privateFailure();
      return { repository:context.repository, headSha:context.workflowSha, shallow:true };
    },
    async runGit(argv) {
      calls.git.push([...argv]);
      const result = (text = '', exitCode = 0) => ({ exitCode, stdout:Buffer.from(text) });
      if (argv.includes('show-ref')) return result('',1);
      if (argv.includes('--is-shallow-repository')) return result(String(shallow++ === 0));
      if (argv.includes('fetch')) {
        if (failure === 'fetch-killed') privateFailure();
        if (failure === 'fetch-nonzero') return result(canary,128);
        return result();
      }
      if (argv.includes('--verify')) return result(context.candidateSha);
      if (argv.includes('log')) {
        if (failure === 'graph') return result('SYNTHETIC_INVALID_GRAPH');
        return result(authority.commits.map(c => `${c.sha}\0${c.treeSha}\0${c.parents.join(' ')}`).join('\n'));
      }
      if (argv.includes('merge-base')) return result();
      throw new Error('unexpected fake command');
    },
    async materializeCandidate(candidate, tree) {
      calls.materialize++;
      assert.equal(candidate,context.candidateSha); assert.equal(tree,context.candidateTreeSha);
      if (failure === 'materialize') privateFailure();
    },
    async close() { calls.close++; if (failure === 'close') privateFailure(); },
  };
  const io = {
    async readBoundedFile(path, bound) {
      assert.equal(bound,1024*1024);
      if (path === '/synthetic/admission-context.json') {
        calls.document++;
        return Buffer.from(failure === 'document' ? '{INVALID' : JSON.stringify(document));
      }
      calls.helpers++;
      const relative = path.slice('/synthetic/'.length);
      assert.ok(Object.hasOwn(helpers,relative));
      return failure === 'helper' ? Buffer.from(canary) : helpers[relative];
    },
    async reconcileExactAdmission(checked, id, provider, token) {
      calls.reconcile++; assert.equal(checked.mode,'ci'); assert.equal(id,7); assert.equal(token,canary);
      if (failure === 'admission') privateFailure();
      return { carrier:structuredClone(carrier) };
    },
    createNativeCheckout(config) {
      calls.native++;
      assert.equal(config.candidateAtWorkspace,true); assert.equal(config.workspace,'/synthetic/empty-workspace');
      if (failure === 'native-init') privateFailure();
      return native;
    },
    async writeFile(path, bytes, options) {
      calls.write++; assert.equal(path,'/synthetic/ci-preparation-result.json');
      assert.deepEqual(options,{ flag:'wx', mode:0o600 });
      assert.ok(!bytes.includes(canary));
      if (failure === 'write') privateFailure();
    },
  };
  return { context, authority, authorityBytes, calls, native, io };
}

async function execute(f, validateOnly = false) {
  return runCiFromFile('/synthetic/admission-context.json',
    { GH_TOKEN:canary, GITHUB_WORKSPACE:'/synthetic/empty-workspace' }, validateOnly, f.io);
}

for (const [failure,phase,lastOperation] of [
  ['document','document-validation',null], ['admission','renewed-admission',null],
  ['helper','helper-integrity',null], ['authority','authority-validation',null],
  ['native-init','native-initialization',null], ['checkout','workflow-checkout',null],
  ['fetch-killed','canonical-transport','classified-fetch'],
  ['fetch-nonzero','canonical-transport','classified-fetch'],
  ['graph','canonical-transport','canonical-graph'], ['materialize','candidate-materialization',null],
  ['write','receipt-write',null], ['close','close',null],
]) {
  test(`fixed safe diagnostics preserve ${failure} boundary without retry or private content`, async () => {
    const f = fixture(failure);
    await assert.rejects(execute(f), error => {
      assert.equal(error.message,'classified_ci_source_failed_reconcile_no_retry');
      assert.deepEqual(ciFailureDiagnostic(error), { phase, lastGitOperation:lastOperation,
        retryMaximum:0, releaseAuthorized:false });
      assert.ok(!JSON.stringify(ciFailureDiagnostic(error)).includes(canary));
      return true;
    });
    for (const key of ['document','reconcile','native','checkout','materialize','write','close']) assert.ok(f.calls[key] <= 1);
    assert.ok(f.calls.git.filter(argv => argv.includes('fetch')).length <= 1);
    if (['document','admission','helper','authority'].includes(failure)) assert.equal(f.calls.native,0);
    if (!['write','close'].includes(failure)) assert.equal(f.calls.write,0);
    if (['native-init','document','admission','helper','authority'].includes(failure)) assert.equal(f.calls.close,0);
    else assert.equal(f.calls.close,1);
  });
}

test('successful receipt retains exact classified graph/source boundaries and no release authority', async () => {
  const f = fixture(), result = await execute(f);
  const source = { repository:f.context.repository, sourceRef:f.context.ref, workflowSha:f.context.workflowSha,
    candidateSha:f.context.candidateSha, comparisonBaseSha:f.context.comparisonBaseSha, prHeadSha:f.context.prHeadSha };
  const separate = fixture();
  const transport = await prepareClassifiedCiSource(f.authorityBytes,pin(f.authorityBytes),source,
    { checkoutExact:separate.native.checkoutExact, runGit:separate.native.runGit });
  assert.deepEqual(result,{ transport, sourceRef:f.context.ref, workflowSha:f.context.workflowSha,
    testedSha:f.context.candidateSha, comparisonBaseSha:f.context.comparisonBaseSha, prHeadSha:f.context.prHeadSha,
    fullClassifiedTipHistoryAvailable:true, legacySecretScannerEventRangeUnchanged:true, releaseAuthorized:false });
  assert.equal(f.calls.reconcile,1); assert.equal(f.calls.checkout,1); assert.equal(f.calls.materialize,1);
  assert.equal(f.calls.write,1); assert.equal(f.calls.close,1);
  const fetch = f.calls.git.filter(argv => argv.includes('fetch'));
  assert.equal(fetch.length,1); assert.ok(fetch[0].includes('--unshallow'));
  assert.ok(!fetch[0].includes('--all'));
});

test('validate-only still rechecks admission/helpers/authority and performs no native or receipt action', async () => {
  const f = fixture();
  assert.deepEqual(await execute(f,true),{ classifiedCiAuthorityValidated:true, nativeGitPerformed:false, releaseAuthorized:false });
  assert.equal(f.calls.reconcile,1); assert.equal(f.calls.helpers,EXPECTED_HELPERS.length);
  assert.equal(f.calls.native,0); assert.equal(f.calls.write,0);
});

test('unknown and forged errors cannot inject diagnostic content or invoke diagnostic accessors', async () => {
  assert.equal(ciFailureDiagnostic(new Error(canary)),null);
  assert.equal(ciFailureDiagnostic(new Proxy({}, { getPrototypeOf() { throw new Error(canary); } })),null);
  let genuine;
  try { await execute(fixture('admission')); } catch (error) { genuine=error; }
  let reads=0;
  const forged=Object.create(Object.getPrototypeOf(genuine));
  Object.defineProperty(forged,'diagnostic',{ value:{ get phase() { reads++; return canary; },
    lastGitOperation:null, arbitrary:canary } });
  assert.equal(ciFailureDiagnostic(forged),null); assert.equal(reads,0);
  assert.ok(Object.isFrozen(ciFailureDiagnostic(genuine)));
});
