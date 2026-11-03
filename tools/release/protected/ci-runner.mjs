import { writeFile } from 'node:fs/promises';
import { readBoundedFile } from './migration-source-snapshots-cli.mjs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { EXPECTED_HELPERS, decodeAuthority, hash, requireAdmission } from './admission.mjs';
import { providerJson, reconcileExactAdmission } from './inline-bootstrap.mjs';
import { planCanonicalTransport } from './finite-canonical-transport.mjs';
import { executeCanonicalTransport } from './native-canonical-transport.mjs';
import { createNativeCheckout } from './native-checkout.mjs';

const phases = new Set(['validation', 'authority-validation', 'workflow-checkout', 'canonical-transport',
  'document-validation', 'renewed-admission', 'helper-integrity', 'native-initialization',
  'candidate-materialization', 'receipt-write', 'close']);
const gitOperations = new Set(['destination-ref', 'shallow-state', 'classified-fetch', 'tip-readback',
  'canonical-graph', 'ancestry-check', 'other']);
class CiSourceFailure extends Error {
  constructor(phase, lastGitOperation = null) {
    super('classified_ci_source_failed_reconcile_no_retry');
    Object.defineProperty(this, 'diagnostic', { value: Object.freeze({
      phase: phases.has(phase) ? phase : 'validation',
      lastGitOperation: gitOperations.has(lastGitOperation) ? lastGitOperation : null,
      retryMaximum: 0, releaseAuthorized: false,
    }) });
  }
}
export const ciFailureDiagnostic = error => {
  try {
    if (!(error instanceof CiSourceFailure)) return null;
    const diagnostic = Object.getOwnPropertyDescriptor(error, 'diagnostic')?.value;
    if (!diagnostic || typeof diagnostic !== 'object') return null;
    const fields = Object.getOwnPropertyDescriptors(diagnostic);
    if (!Object.hasOwn(fields.phase ?? {}, 'value') || !phases.has(fields.phase.value) ||
        !Object.hasOwn(fields.lastGitOperation ?? {}, 'value') ||
        (fields.lastGitOperation.value !== null && !gitOperations.has(fields.lastGitOperation.value))) return null;
    return Object.freeze({ phase: fields.phase.value, lastGitOperation: fields.lastGitOperation.value,
      retryMaximum: 0, releaseAuthorized: false });
  } catch { return null; }
};
const fail = (phase = 'validation', error = null, lastGitOperation = null) => {
  const prior = ciFailureDiagnostic(error);
  throw new CiSourceFailure(prior && prior.phase !== 'validation' ? prior.phase : phase, prior?.lastGitOperation ?? lastGitOperation);
};
const gitOperation = argv => argv.includes('fetch') ? 'classified-fetch' : argv.includes('show-ref') ? 'destination-ref' :
  argv.includes('--is-shallow-repository') ? 'shallow-state' : argv.includes('log') ? 'canonical-graph' :
  argv.includes('merge-base') ? 'ancestry-check' : argv.includes('--verify') ? 'tip-readback' : 'other';
const snapshot = (value, keys) => {
  if (!value || typeof value !== 'object' || Array.isArray(value) || Object.getOwnPropertySymbols(value).length ||
      ![Object.prototype, null].includes(Object.getPrototypeOf(value))) fail();
  const descriptors = Object.getOwnPropertyDescriptors(value);
  if (Object.keys(descriptors).sort().join('\0') !== [...keys].sort().join('\0') ||
      keys.some(key => !Object.hasOwn(descriptors[key], 'value'))) fail();
  return Object.freeze(Object.fromEntries(keys.map(key => [key, descriptors[key].value])));
};

export async function prepareClassifiedCiSource(bytes, pin, context, io) {
  let authority, checked, callbacks;
  try {
    authority = Buffer.from(bytes); checked = snapshot(context, ['repository', 'sourceRef', 'workflowSha', 'candidateSha', 'comparisonBaseSha', 'prHeadSha']);
    callbacks = snapshot(io, ['checkoutExact', 'runGit']);
    if (!Buffer.isBuffer(bytes) || typeof callbacks.checkoutExact !== 'function' || typeof callbacks.runGit !== 'function') fail();
    planCanonicalTransport(authority, pin, { ...checked, shallow: true });
  } catch (error) { fail('authority-validation', error); }
  let checkout;
  try {
    checkout = await callbacks.checkoutExact(Object.freeze({ repository: checked.repository, ref: checked.workflowSha,
      'fetch-depth': 1, 'fetch-tags': false, submodules: false, clean: false, 'persist-credentials': false }));
    checkout = snapshot(checkout, ['repository', 'headSha', 'shallow']);
    if (checkout.repository !== checked.repository || checkout.headSha !== checked.workflowSha || typeof checkout.shallow !== 'boolean') fail();
  } catch (error) { fail('workflow-checkout', error); }
  let lastGitOperation = null;
  try {
    return await executeCanonicalTransport(authority, pin, Object.freeze({ ...checked, shallow: checkout.shallow }), async argv => {
      lastGitOperation = gitOperation(argv);
      const result = snapshot(await callbacks.runGit(Object.freeze([...argv])), ['exitCode', 'stdout']);
      if (!result || !Number.isSafeInteger(result.exitCode) || !Buffer.isBuffer(result.stdout)) fail();
      return { exitCode: result.exitCode, stdout: Buffer.from(result.stdout) };
    });
  } catch (error) { fail('canonical-transport', error, lastGitOperation); }
}

export async function runCiFromFile(documentPath, env = process.env, validateOnly = false,
  io = { readBoundedFile, reconcileExactAdmission, createNativeCheckout, writeFile }) {
  let native, phase = 'document-validation';
  try {
    const ports = snapshot(io, ['readBoundedFile', 'reconcileExactAdmission', 'createNativeCheckout', 'writeFile']);
    if (Object.values(ports).some(callback => typeof callback !== 'function')) fail();
    const root = dirname(resolve(documentPath));
    const document = JSON.parse((await ports.readBoundedFile(documentPath, 1024 * 1024)).toString('utf8')), { context } = document;
    if (context?.mode !== 'ci' || !Number.isSafeInteger(document.checkRunId) || document.checkRunId <= 0) fail();
    phase = 'renewed-admission';
    const selected = await ports.reconcileExactAdmission(context, document.checkRunId, providerJson, env.GH_TOKEN);
    if (JSON.stringify(selected.carrier) !== JSON.stringify(document.carrier)) fail();
    phase = 'helper-integrity';
    for (const path of EXPECTED_HELPERS) if (hash(await ports.readBoundedFile(join(root, path), 1024 * 1024)) !== selected.carrier.helperSha256[path]) fail();
    phase = 'authority-validation';
    const authority = decodeAuthority(selected.carrier.canonicalAuthorityBase64, 1024 * 1024), pin = hash(authority);
    const source = { repository: context.repository, sourceRef: context.ref, workflowSha: context.workflowSha,
      candidateSha: context.candidateSha, comparisonBaseSha: context.comparisonBaseSha, prHeadSha: context.prHeadSha };
    planCanonicalTransport(authority, pin, { ...source, shallow: true });
    if (validateOnly) return { classifiedCiAuthorityValidated: true, nativeGitPerformed: false, releaseAuthorized: false };
    phase = 'native-initialization';
    native = ports.createNativeCheckout({ trustedRoot: root, workspace: env.GITHUB_WORKSPACE, candidateAtWorkspace: true,
      repository: context.repository, workflowSha: context.workflowSha });
    phase = 'canonical-transport';
    const transport = await prepareClassifiedCiSource(authority, pin, source, { checkoutExact: native.checkoutExact, runGit: native.runGit });
    phase = 'candidate-materialization';
    await native.materializeCandidate(context.candidateSha, context.candidateTreeSha);
    const receipt = { transport, sourceRef: context.ref, workflowSha: context.workflowSha, testedSha: context.candidateSha,
      comparisonBaseSha: context.comparisonBaseSha, prHeadSha: context.prHeadSha, fullClassifiedTipHistoryAvailable: true,
      legacySecretScannerEventRangeUnchanged: true, releaseAuthorized: false };
    phase = 'receipt-write';
    await ports.writeFile(join(root, 'ci-preparation-result.json'), JSON.stringify(receipt), { flag: 'wx', mode: 0o600 });
    return receipt;
  } catch (error) { fail(phase, error); }
  finally { if (native) { try { await native.close(); } catch (error) { fail('close', error); } } }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const [mode, path] = process.argv.slice(2);
  if (!['validate', 'prepare'].includes(mode) || !path) { console.error('CI source preparation held.'); process.exitCode = 1; }
  else runCiFromFile(path, process.env, mode === 'validate').catch(error => { console.error('CI source preparation held; preserve outputs and reconcile without retry.');
    console.error(JSON.stringify(ciFailureDiagnostic(error) ?? { phase: 'validation', lastGitOperation: null, retryMaximum: 0, releaseAuthorized: false })); process.exitCode = 1; });
}
