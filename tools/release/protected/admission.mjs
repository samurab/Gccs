import { createHash } from 'node:crypto';

export const CHECK_NAME = 'FeDril protected historical assertions (129)';
export const EXPECTED_HELPERS = Object.freeze([
  'tools/release/protected/admission.mjs', 'tools/release/protected/runner.mjs',
  'tools/release/protected/ci-runner.mjs',
  'tools/release/protected/inline-bootstrap.mjs',
  'tools/release/protected/native-checkout.mjs', 'tools/release/protected/composition.mjs',
  'tools/release/protected/workflow-bootstrap-binding.mjs', 'tools/release/protected/finite-canonical-transport.mjs',
  'tools/release/protected/native-canonical-transport.mjs', 'tools/release/protected/migration-source-snapshots.mjs',
  'tools/release/protected/migration-source-snapshots-cli.mjs', 'tools/release/protected/release-control.mjs',
  'tools/release/check-api-package.py', 'tools/release/release-control.mjs', 'tools/release/release-evidence.mjs',
  'tools/release/validate-auth-config.mjs', 'tools/release/check-azure-settings.mjs', 'tools/release/check-cleanup-quiescence.mjs',
]);
export const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const sha = value => typeof value === 'string' && /^[a-f0-9]{40}$/.test(value);
const digest = value => typeof value === 'string' && /^[a-f0-9]{64}$/.test(value);
const positive = value => typeof value === 'string' && /^[1-9][0-9]{0,19}$/.test(value);
const reject = () => { throw new Error('protected_admission_rejected'); };
const require = condition => { if (!condition) reject(); };
function keys(value, expected) {
  require(value && typeof value === 'object' && !Array.isArray(value) &&
    Object.keys(value).sort().join('\0') === [...expected].sort().join('\0'));
}
const freeze = value => {
  if (value && typeof value === 'object') { for (const nested of Object.values(value)) freeze(nested); Object.freeze(value); }
  return value;
};

export function decodeAuthority(value, cap) {
  require(typeof value === 'string' && value.length <= Math.ceil(cap / 3) * 4);
  const bytes = Buffer.from(value, 'base64');
  require(bytes.length > 0 && bytes.length <= cap && bytes.toString('base64') === value);
  return bytes;
}

export function validateContext(context) {
  keys(context, ['repository', 'appId', 'mode', 'workflowSha', 'candidateSha', 'candidateTreeSha', 'candidateTag',
    'stagingWorkflowSha', 'runId', 'runAttempt', 'event', 'ref', 'workflowRef', 'comparisonBaseSha', 'prHeadSha']);
  require(typeof context.repository === 'string' && /^[A-Za-z0-9][A-Za-z0-9-]*\/[A-Za-z0-9][A-Za-z0-9._-]*$/.test(context.repository));
  require(positive(context.appId) && positive(context.runId) && positive(context.runAttempt) &&
    sha(context.workflowSha) && sha(context.candidateSha) && sha(context.candidateTreeSha) && context.candidateSha !== '0'.repeat(40));
  require(['ci', 'staging', 'production'].includes(context.mode));
  require(context.candidateTag === null || (typeof context.candidateTag === 'string' &&
    /^v(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)-rc\.[1-9][0-9]*$/.test(context.candidateTag)));
  require(context.stagingWorkflowSha === null || sha(context.stagingWorkflowSha));
  const path = context.mode === 'ci' ? '.github/workflows/ci.yml' : context.mode === 'staging' ?
    '.github/workflows/staging.yml' : '.github/workflows/production-release.yml';
  require(context.workflowRef === `${context.repository}/${path}@${context.ref}`);
  if (context.mode === 'ci') {
    require(['push', 'pull_request', 'workflow_dispatch'].includes(context.event) && context.candidateTag === null &&
      context.stagingWorkflowSha === null && typeof context.ref === 'string');
    require(context.event === 'pull_request' ? /^refs\/pull\/[1-9][0-9]*\/merge$/.test(context.ref) :
      /^refs\/heads\/[A-Za-z0-9_./-]+$/.test(context.ref));
    require(sha(context.comparisonBaseSha) && (context.event === 'pull_request' ?
      sha(context.prHeadSha) && context.prHeadSha !== '0'.repeat(40) && context.comparisonBaseSha !== '0'.repeat(40) : context.prHeadSha === null));
  } else {
    require(context.comparisonBaseSha === null && context.prHeadSha === null);
    require(context.event === 'workflow_dispatch' && context.ref === 'refs/heads/release-main');
    if (context.mode === 'production') require(sha(context.stagingWorkflowSha) && context.candidateTag !== null);
  }
  return true;
}

export function requireAdmission(checks, context) {
  try {
    validateContext(context);
    require(Array.isArray(checks) && checks.length <= 1000);
    const details = `https://github.com/${context.repository}/actions/runs/${context.runId}/attempts/${context.runAttempt}`;
    const matches = checks.filter(check => check && check.name === CHECK_NAME && check.head_sha === context.candidateSha &&
      Number.isSafeInteger(check.app?.id) && String(check.app.id) === context.appId && check.details_url === details);
    require(matches.length === 1);
    const check = matches[0];
    require(Number.isSafeInteger(check.id) && check.id > 0 && check.status === 'completed' && check.conclusion === 'success' &&
      typeof check.external_id === 'string' && /^fedril-historical129:[a-f0-9]{64}$/.test(check.external_id));
    const text = check.output?.text;
    require(typeof text === 'string' && Buffer.byteLength(text, 'utf8') <= 60000);
    const carrier = JSON.parse(text);
    keys(carrier, ['schemaVersion', 'purpose', 'repository', 'runId', 'runAttempt', 'mode', 'workflowSha', 'candidate',
      'bindingSha256', 'bootstrapPolicyBase64', 'canonicalAuthorityBase64', 'helperSha256', 'baselineSnapshot']);
    require(carrier.schemaVersion === 1 && carrier.purpose === 'fedril-workflow-admission-v1' &&
      ['repository', 'runId', 'runAttempt', 'mode', 'workflowSha'].every(key => carrier[key] === context[key]) &&
      digest(carrier.bindingSha256) && check.external_id === 'fedril-historical129:' + carrier.bindingSha256);
    keys(carrier.candidate, ['sha', 'treeSha', 'tag', 'stagingWorkflowSha']);
    require(carrier.candidate.sha === context.candidateSha && carrier.candidate.treeSha === context.candidateTreeSha &&
      carrier.candidate.tag === context.candidateTag && carrier.candidate.stagingWorkflowSha === context.stagingWorkflowSha);
    if (context.mode === 'ci') {
      keys(carrier.helperSha256, EXPECTED_HELPERS);
      require(Object.values(carrier.helperSha256).every(digest));
      require(carrier.bootstrapPolicyBase64 === null && carrier.baselineSnapshot === null);
      const authority = JSON.parse(decodeAuthority(carrier.canonicalAuthorityBase64, 1024 * 1024).toString('utf8'));
      require(authority.schemaVersion === 2 && authority.purpose === 'classified-ci-history' &&
        authority.repository === context.repository && authority.workflowSha === context.workflowSha && authority.tipSha === context.candidateSha &&
        authority.sourceRef === context.ref && authority.comparisonBaseSha === context.comparisonBaseSha && authority.prHeadSha === context.prHeadSha);
    } else {
      keys(carrier.helperSha256, EXPECTED_HELPERS);
      require(Object.values(carrier.helperSha256).every(digest));
      keys(carrier.baselineSnapshot, ['path', 'sha256']);
      require(carrier.baselineSnapshot.path === 'release/migration-baseline.snapshot.json' && digest(carrier.baselineSnapshot.sha256));
      decodeAuthority(carrier.bootstrapPolicyBase64, 16384);
      decodeAuthority(carrier.canonicalAuthorityBase64, 1024 * 1024);
    }
    return freeze({ carrier, checkRunId: check.id, detailsUrl: details, releaseAuthorized: false });
  } catch { reject(); }
}

export function currentAdmissionTargets(checks, context) {
  const details = `https://github.com/${context.repository}/actions/runs/${context.runId}/attempts/${context.runAttempt}`;
  return checks.filter(check => check && check.name === CHECK_NAME && check.head_sha === context.candidateSha &&
    Number.isSafeInteger(check.app?.id) && String(check.app.id) === context.appId && check.details_url === details);
}
