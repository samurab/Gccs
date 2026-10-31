import { writeFile } from 'node:fs/promises';
import { readBoundedFile } from './migration-source-snapshots-cli.mjs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { EXPECTED_HELPERS, decodeAuthority, hash, requireAdmission } from './admission.mjs';
import { providerJson, reconcileExactAdmission } from './inline-bootstrap.mjs';
import { planCanonicalTransport } from './finite-canonical-transport.mjs';
import { executeCanonicalTransport } from './native-canonical-transport.mjs';
import { createNativeCheckout } from './native-checkout.mjs';

const fail = () => { throw new Error('classified_ci_source_failed_reconcile_no_retry'); };
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
  } catch { fail(); }
  let checkout;
  try {
    checkout = await callbacks.checkoutExact(Object.freeze({ repository: checked.repository, ref: checked.workflowSha,
      'fetch-depth': 1, 'fetch-tags': false, submodules: false, clean: false, 'persist-credentials': false }));
    checkout = snapshot(checkout, ['repository', 'headSha', 'shallow']);
    if (checkout.repository !== checked.repository || checkout.headSha !== checked.workflowSha || typeof checkout.shallow !== 'boolean') fail();
  } catch { fail(); }
  try {
    return await executeCanonicalTransport(authority, pin, Object.freeze({ ...checked, shallow: checkout.shallow }), async argv => {
      const result = snapshot(await callbacks.runGit(Object.freeze([...argv])), ['exitCode', 'stdout']);
      if (!result || !Number.isSafeInteger(result.exitCode) || !Buffer.isBuffer(result.stdout)) fail();
      return { exitCode: result.exitCode, stdout: Buffer.from(result.stdout) };
    });
  } catch { fail(); }
}

export async function runCiFromFile(documentPath, env = process.env, validateOnly = false) {
  let native;
  try {
    const root = dirname(resolve(documentPath));
    const document = JSON.parse((await readBoundedFile(documentPath, 1024 * 1024)).toString('utf8')), { context } = document;
    if (context?.mode !== 'ci' || !Number.isSafeInteger(document.checkRunId) || document.checkRunId <= 0) fail();
    const selected = await reconcileExactAdmission(context, document.checkRunId, providerJson, env.GH_TOKEN);
    if (JSON.stringify(selected.carrier) !== JSON.stringify(document.carrier)) fail();
    for (const path of EXPECTED_HELPERS) if (hash(await readBoundedFile(join(root, path), 1024 * 1024)) !== selected.carrier.helperSha256[path]) fail();
    const authority = decodeAuthority(selected.carrier.canonicalAuthorityBase64, 1024 * 1024), pin = hash(authority);
    const source = { repository: context.repository, sourceRef: context.ref, workflowSha: context.workflowSha,
      candidateSha: context.candidateSha, comparisonBaseSha: context.comparisonBaseSha, prHeadSha: context.prHeadSha };
    planCanonicalTransport(authority, pin, { ...source, shallow: true });
    if (validateOnly) return { classifiedCiAuthorityValidated: true, nativeGitPerformed: false, releaseAuthorized: false };
    native = createNativeCheckout({ trustedRoot: root, workspace: env.GITHUB_WORKSPACE, candidateAtWorkspace: true,
      repository: context.repository, workflowSha: context.workflowSha });
    const transport = await prepareClassifiedCiSource(authority, pin, source, { checkoutExact: native.checkoutExact, runGit: native.runGit });
    await native.materializeCandidate(context.candidateSha, context.candidateTreeSha);
    const receipt = { transport, sourceRef: context.ref, workflowSha: context.workflowSha, testedSha: context.candidateSha,
      comparisonBaseSha: context.comparisonBaseSha, prHeadSha: context.prHeadSha, fullClassifiedTipHistoryAvailable: true,
      legacySecretScannerEventRangeUnchanged: true, releaseAuthorized: false };
    await writeFile(join(root, 'ci-preparation-result.json'), JSON.stringify(receipt), { flag: 'wx', mode: 0o600 });
    return receipt;
  } catch { fail(); }
  finally { if (native) { try { await native.close(); } catch { fail(); } } }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const [mode, path] = process.argv.slice(2);
  if (!['validate', 'prepare'].includes(mode) || !path) { console.error('CI source preparation held.'); process.exitCode = 1; }
  else runCiFromFile(path, process.env, mode === 'validate').catch(() => { console.error('CI source preparation held; preserve outputs and reconcile without retry.'); process.exitCode = 1; });
}
