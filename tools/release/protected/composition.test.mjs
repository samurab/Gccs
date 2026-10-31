import test from 'node:test';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { prepareTrustedWorkflow, WorkflowPreparationError } from './composition.mjs';

const sha = digit => digit.repeat(40);
const hash = value => createHash('sha256').update(value).digest('hex');
const bytes = value => Buffer.from(JSON.stringify(value));
const output = (text = '', exitCode = 0) => ({ exitCode, stdout: Buffer.from(text) });

function fixture(mode = 'staging') {
  const commits = ['1', '2', '3', '4'].map((value, index) => ({ sha: sha(value), treeSha: sha(['a', 'b', 'c', 'd'][index]),
    parents: index ? [sha(String(index))] : [] }));
  const authority = { schemaVersion: 1, repository: 'owner/repository', canonicalRef: 'refs/heads/release-main',
    workflowSha: sha('2'), rootSha: sha('1'), tipSha: sha('4'), commits,
    classifiedTrees: commits.map((commit, index) => ({ treeSha: commit.treeSha, inventorySha256: String(index + 5).repeat(64) })),
    classificationReceiptSha256: 'f'.repeat(64) };
  const policy = { schemaVersion: 1, repository: authority.repository, event: 'workflow_dispatch', ref: authority.canonicalRef,
    workflowPath: mode === 'production' ? '.github/workflows/production-release.yml' : '.github/workflows/staging.yml',
    workflowSha: sha('2'), canonicalAuthoritySha256: hash(bytes(authority)) };
  return { bootstrapPolicyBytes: bytes(policy), pin: hash(bytes(policy)), mode, canonicalAuthorityBytes: bytes(authority),
    trustedCandidateSha: sha('3'), trustedStagingWorkflowSha: mode === 'production' ? sha('1') : null,
    providerContext: { repository: policy.repository, event: policy.event, ref: policy.ref,
      workflowRef: `${policy.repository}/${policy.workflowPath}@${policy.ref}`, workflowSha: policy.workflowSha, sourceSha: policy.workflowSha } };
}

function changeAuthority(input, edit) {
  const authority = JSON.parse(input.canonicalAuthorityBytes);
  edit(authority);
  input.canonicalAuthorityBytes = bytes(authority);
  const policy = JSON.parse(input.bootstrapPolicyBytes);
  policy.canonicalAuthoritySha256 = hash(input.canonicalAuthorityBytes);
  input.bootstrapPolicyBytes = bytes(policy);
  input.pin = hash(input.bootstrapPolicyBytes);
}

function fakeIo(input, options = {}) {
  const policy = JSON.parse(input.bootstrapPolicyBytes), authority = JSON.parse(input.canonicalAuthorityBytes);
  const events = [];
  let shallowCalls = 0;
  const io = {
    async checkoutExact(config) {
      events.push({ kind: 'checkout', config });
      return { repository: policy.repository, headSha: policy.workflowSha, shallow: options.shallow ?? true };
    },
    async runGit(argv) {
      events.push({ kind: 'git', argv });
      assert.ok(Object.isFrozen(argv));
      if (argv.includes('show-ref')) return output('', options.collision ? 0 : 1);
      if (argv.includes('--is-shallow-repository')) return output(String(shallowCalls++ === 0 ? options.shallow ?? true : false));
      if (argv.includes('fetch')) {
        if (options.fetchThrow) throw new Error('synthetic-private-error');
        return output('', options.fetchExit ?? 0);
      }
      if (argv.includes('--verify')) return output(authority.tipSha);
      if (argv.includes('log')) return output(authority.commits.map(commit =>
        `${commit.sha}\0${options.wrongGraph ? sha('e') : commit.treeSha}\0${commit.parents.join(' ')}`).join('\n'));
      if (argv.includes('merge-base')) return output('', options.ancestryExit ?? 0);
      throw new Error('unexpected fake Git command');
    },
  };
  return { io, events };
}

async function noIo(input, ioOverride) {
  let calls = 0;
  const io = ioOverride ?? { checkoutExact() { calls++; }, runGit() { calls++; } };
  await assert.rejects(prepareTrustedWorkflow(input, io), error => error instanceof WorkflowPreparationError &&
    error.message === 'trusted_workflow_preflight_rejected' && !error.checkoutMayHaveOccurred);
  assert.equal(calls, 0);
}

test('staging validates before exact checkout, finite fetch, graph and ancestry', async () => {
  const input = fixture(), { io, events } = fakeIo(input);
  const result = await prepareTrustedWorkflow(input, io);
  assert.equal(events[0].kind, 'checkout');
  assert.deepEqual(events[0].config, { repository: 'owner/repository', ref: sha('2'), 'fetch-depth': 1,
    'fetch-tags': false, submodules: false, clean: false, 'persist-credentials': false });
  assert.ok(Object.isFrozen(events[0].config));
  assert.ok(events[1].argv.includes('show-ref'));
  assert.ok(events[2].argv.includes('--is-shallow-repository'));
  assert.ok(events[3].argv.includes('fetch'));
  assert.ok(events[3].argv.includes('--unshallow'));
  assert.equal(events.filter(event => event.argv?.includes('fetch')).length, 1);
  assert.equal(events.filter(event => event.argv?.includes('merge-base')).length, 2);
  assert.equal(result.canonicalTipSha, sha('4'));
  assert.equal(result.commitsVerified, 4);
  assert.equal(result.nativeAncestryChecks, 2);
  assert.equal(result.releaseAuthorized, false);
  assert.equal(result.separateCandidateCheckoutPerformed, false);
  assert.equal(result.protectedResultProduced, false);
  assert.equal(result.deploymentPerformed, false);
  assert.ok(Object.isFrozen(result));
});

test('production binds actual staging workflow and observed nonshallow checkout', async () => {
  const input = fixture('production'), { io, events } = fakeIo(input, { shallow: false });
  const result = await prepareTrustedWorkflow(input, io);
  assert.equal(result.stagingWorkflowSha, sha('1'));
  assert.equal(result.nativeAncestryChecks, 3);
  assert.equal(events.filter(event => event.argv?.includes('merge-base')).length, 3);
  assert.ok(!events.find(event => event.argv?.includes('fetch')).argv.includes('--unshallow'));
});

test('workflow and candidate may share identity without a separate checkout', async () => {
  const input = fixture();
  input.trustedCandidateSha = input.providerContext.workflowSha;
  const { io, events } = fakeIo(input);
  const result = await prepareTrustedWorkflow(input, io);
  assert.equal(result.candidateSha, result.workflowSha);
  assert.equal(result.workflowCheckoutPerformed, true);
  assert.equal(result.separateCandidateCheckoutPerformed, false);
  assert.equal(Object.hasOwn(result, 'candidateCheckoutPerformed'), false);
  assert.equal(events.filter(event => event.kind === 'checkout').length, 1);
  assert.equal(events.filter(event => event.argv?.includes('merge-base')).length, 1);
});

test('malformed policy and authority buffers cause zero IO', async () => {
  for (const change of [{ bootstrapPolicyBytes: 'text' }, { canonicalAuthorityBytes: 'text' },
    { bootstrapPolicyBytes: Buffer.alloc(0) }, { canonicalAuthorityBytes: Buffer.from('{bad') }]) {
    await noIo({ ...fixture(), ...change });
  }
});

test('tampered bootstrap policy causes zero IO', async () => {
  const input = fixture(); input.bootstrapPolicyBytes[0] ^= 1;
  await noIo(input);
});

test('tampered canonical authority causes zero IO', async () => {
  const input = fixture(); input.canonicalAuthorityBytes[0] ^= 1;
  await noIo(input);
});

test('provider context wrong workflow or source causes zero IO', async () => {
  for (const change of [{ workflowSha: sha('1') }, { sourceSha: sha('3') },
    { workflowRef: 'owner/repository/.github/workflows/other.yml@refs/heads/release-main' },
    { repository: 'owner/other' }, { event: 'push' }, { ref: 'refs/heads/main' }]) {
    const input = fixture(); Object.assign(input.providerContext, change);
    await noIo(input);
  }
});

test('production without actual staging workflow causes zero IO', async () => {
  await noIo({ ...fixture('production'), trustedStagingWorkflowSha: null });
});

test('candidate outside full canonical ancestry causes zero IO', async () => {
  await noIo({ ...fixture(), trustedCandidateSha: sha('e') });
});

test('staging workflow outside canonical ancestry causes zero IO', async () => {
  await noIo({ ...fixture('production'), trustedStagingWorkflowSha: sha('e') });
});

test('complete graph and every classification binding validate before IO', async () => {
  for (const edit of [a => a.commits.pop(), a => a.commits[1].parents = [sha('e')],
    a => a.commits[0].parents = [sha('4')], a => a.classifiedTrees.pop(),
    a => a.classifiedTrees[0].inventorySha256 = 'invalid']) {
    const input = fixture(); changeAuthority(input, edit); await noIo(input);
  }
});

test('top-level overrides and unsupported modes cause zero IO', async () => {
  for (const change of [{ checkout: {} }, { mode: 'staging-override' }, { mode: ['staging'] },
    { trustedCandidateSha: null }, { pin: 'f'.repeat(64) }]) await noIo({ ...fixture(), ...change });
});

test('wrong checkout repository or head stops before Git', async () => {
  for (const change of [{ repository: 'owner/other' }, { headSha: sha('3') }]) {
    const input = fixture(), { io, events } = fakeIo(input), original = io.checkoutExact;
    io.checkoutExact = async config => ({ ...await original(config), ...change });
    await assert.rejects(prepareTrustedWorkflow(input, io), { message: 'trusted_checkout_observation_rejected_reconcile_no_retry' });
    assert.equal(events.length, 1);
  }
});

test('checkout shallow observation must be a strict boolean', async () => {
  for (const shallow of [undefined, null, 0, 1, 'true', {}, new Boolean(true)]) {
    const input = fixture(), { io, events } = fakeIo(input), original = io.checkoutExact;
    io.checkoutExact = async config => ({ ...await original(config), shallow });
    await assert.rejects(prepareTrustedWorkflow(input, io), { message: 'trusted_checkout_observation_rejected_reconcile_no_retry' });
    assert.equal(events.length, 1);
  }
});

test('checkout throw stops with fixed reconciliation error and no Git', async () => {
  let calls = 0;
  await assert.rejects(prepareTrustedWorkflow(fixture(), {
    checkoutExact() { calls++; throw new Error('synthetic-private-error'); }, runGit() { calls++; },
  }), error => error.message === 'trusted_checkout_uncertain_reconcile_no_retry' &&
    error.checkoutMayHaveOccurred && !error.nativeMutationMayHaveOccurred && !String(error).includes('synthetic-private-error'));
  assert.equal(calls, 1);
});

test('native fetch throw is uncertain and never retried', async () => {
  const input = fixture(), { io, events } = fakeIo(input, { fetchThrow: true });
  await assert.rejects(prepareTrustedWorkflow(input, io), error => error.message === 'trusted_transport_failed_reconcile_no_retry' &&
    error.nativeMutationMayHaveOccurred && !String(error).includes('synthetic-private-error'));
  assert.equal(events.filter(event => event.argv?.includes('fetch')).length, 1);
});

test('native fetch nonzero exit stops without retry', async () => {
  const input = fixture(), { io, events } = fakeIo(input, { fetchExit: 128 });
  await assert.rejects(prepareTrustedWorkflow(input, io), { message: 'trusted_transport_failed_reconcile_no_retry' });
  assert.equal(events.length, 4);
});

test('native ancestry failure preserves post-fetch uncertainty', async () => {
  const input = fixture(), { io, events } = fakeIo(input, { ancestryExit: 1 });
  await assert.rejects(prepareTrustedWorkflow(input, io), error => error.nativeMutationMayHaveOccurred);
  assert.equal(events.filter(event => event.argv?.includes('fetch')).length, 1);
});

test('destination ref collision stops before native fetch', async () => {
  const input = fixture(), { io, events } = fakeIo(input, { collision: true });
  await assert.rejects(prepareTrustedWorkflow(input, io), error => !error.nativeMutationMayHaveOccurred);
  assert.equal(events.length, 2);
});

test('native graph mismatch stops before ancestry checks', async () => {
  const input = fixture(), { io, events } = fakeIo(input, { wrongGraph: true });
  await assert.rejects(prepareTrustedWorkflow(input, io), error => error.nativeMutationMayHaveOccurred);
  assert.equal(events.filter(event => event.argv?.includes('merge-base')).length, 0);
});

test('checkout options cannot be changed through callback', async () => {
  let gitCalls = 0;
  await assert.rejects(prepareTrustedWorkflow(fixture(), {
    checkoutExact(options) { options.ref = sha('e'); }, runGit() { gitCalls++; },
  }), { message: 'trusted_checkout_uncertain_reconcile_no_retry' });
  assert.equal(gitCalls, 0);
});

test('caller mutations while checkout awaits cannot change validated source or callbacks', async () => {
  const input = fixture(), { io, events } = fakeIo(input);
  let resolveCheckout;
  const originalCheckout = io.checkoutExact;
  io.checkoutExact = config => { events.push({ kind: 'pending-checkout', config }); return new Promise(resolve => { resolveCheckout = resolve; }); };
  const promise = prepareTrustedWorkflow(input, io);
  input.bootstrapPolicyBytes.fill(0); input.canonicalAuthorityBytes.fill(0);
  input.mode = 'production'; input.pin = 'e'.repeat(64); input.trustedCandidateSha = sha('e');
  input.trustedStagingWorkflowSha = sha('e'); input.providerContext.workflowSha = sha('e');
  io.runGit = () => { throw new Error('mutated unvalidated callback'); };
  io.checkoutExact = () => { throw new Error('mutated checkout callback'); };
  resolveCheckout({ repository: 'owner/repository', headSha: sha('2'), shallow: true });
  const result = await promise;
  assert.equal(result.workflowSha, sha('2'));
  assert.equal(result.candidateSha, sha('3'));
  assert.equal(result.stagingWorkflowSha, null);
  assert.equal(result.mode, 'staging');
  assert.equal(events.filter(event => event.argv?.includes('fetch')).length, 1);
  assert.equal(typeof originalCheckout, 'function');
});

test('Git command vectors cannot be mutated by callback', async () => {
  const input = fixture(); let calls = 0;
  await assert.rejects(prepareTrustedWorkflow(input, {
    checkoutExact: async () => ({ repository: 'owner/repository', headSha: sha('2'), shallow: true }),
    runGit(argv) { calls++; argv.push('--force'); },
  }), error => !error.nativeMutationMayHaveOccurred);
  assert.equal(calls, 1);
});

test('checkout result extra fields or accessors are rejected before Git', async () => {
  for (const response of [{ repository: 'owner/repository', headSha: sha('2'), shallow: true, override: true },
    { repository: 'owner/repository', headSha: sha('2'), get shallow() { throw new Error('accessor must not run'); } }]) {
    let gitCalls = 0;
    await assert.rejects(prepareTrustedWorkflow(fixture(), { checkoutExact: async () => response, runGit() { gitCalls++; } }),
      { message: 'trusted_checkout_observation_rejected_reconcile_no_retry' });
    assert.equal(gitCalls, 0);
  }
});

test('invalid native observation stops before fetch', async () => {
  for (const response of [{ exitCode: 1, stdout: 'text' }, { exitCode: true, stdout: Buffer.alloc(0) },
    { exitCode: 1, stdout: Buffer.alloc(0), stderr: 'unexpected' }]) {
    let calls = 0;
    await assert.rejects(prepareTrustedWorkflow(fixture(), {
      checkoutExact: async () => ({ repository: 'owner/repository', headSha: sha('2'), shallow: true }),
      runGit() { calls++; return response; },
    }), error => !error.nativeMutationMayHaveOccurred);
    assert.equal(calls, 1);
  }
});

test('input accessors and extra candidate callbacks are rejected before IO', async () => {
  const accessor = fixture();
  Object.defineProperty(accessor, 'mode', { enumerable: true, get() { throw new Error('accessor must not run'); } });
  await noIo(accessor);
  let calls = 0;
  await noIo(fixture(), { checkoutExact() { calls++; }, runGit() { calls++; }, candidateCheckout() { calls++; } });
  assert.equal(calls, 0);
});
