import { createHash } from 'node:crypto';
import { readFile, writeFile, appendFile } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { EXPECTED_HELPERS, hash, decodeAuthority, requireAdmission } from './admission.mjs';
import { providerJson, reconcileExactAdmission } from './inline-bootstrap.mjs';
import { prepareTrustedWorkflow } from './composition.mjs';
import { createNativeCheckout } from './native-checkout.mjs';
import { main as compareSnapshots, readBoundedFile } from './migration-source-snapshots-cli.mjs';
import { validateApprovedRelease } from './release-control.mjs';
import { validateStagingRun } from '../release-evidence.mjs';

const sha = value => typeof value === 'string' && /^[a-f0-9]{40}$/.test(value);
const fail = () => { throw new Error('trusted_release_preparation_failed_reconcile_no_retry'); };

export async function buildCandidateSnapshot(runGit, candidateSha, sourceLabel) {
  if (!sha(candidateSha) || typeof sourceLabel !== 'string' || !sourceLabel) fail();
  const listing = await runGit(['git', 'ls-tree', '-r', '-z', '--full-tree', candidateSha, '--',
    'src/Gccs.Infrastructure/Persistence/Migrations/', 'infra/database/']);
  if (listing.exitCode !== 0 || !Buffer.isBuffer(listing.stdout)) fail();
  const entries = [], seen = new Set();
  for (const line of listing.stdout.toString('utf8').split('\0').filter(Boolean)) {
    const match = /^([0-7]{6}) ([a-z]+) ([a-f0-9]{40})\t(.+)$/.exec(line);
    if (!match) fail();
    const path = match[4];
    if (!(/^src\/Gccs.Infrastructure\/Persistence\/Migrations\/[^/]+\.cs$/.test(path) || /^infra\/database\/[^/]+\.sql$/.test(path))) continue;
    if (!['100644', '100755'].includes(match[1]) || match[2] !== 'blob') fail();
    if (seen.has(path)) fail(); seen.add(path);
    const response = await runGit(['git', 'cat-file', 'blob', match[3]]);
    if (response.exitCode !== 0 || !Buffer.isBuffer(response.stdout)) fail();
    const content = response.stdout;
    if (createHash('sha1').update(Buffer.concat([Buffer.from(`blob ${content.length}\0`), content])).digest('hex') !== match[3]) fail();
    entries.push({ path, type: 'blob', mode: match[1], sizeBytes: content.length, sha256: hash(content),
      gitBlobOid: match[3], contentBase64: content.toString('base64') });
  }
  if (!entries.length) fail();
  const bytes = Buffer.from(JSON.stringify({ schemaVersion: 1, sourceCommitSha: candidateSha, sourceTag: sourceLabel, entries }));
  if (bytes.length > 64 * 1024 * 1024) fail();
  return bytes;
}

export function comparisonAuthority(carrier, baseline, candidateSnapshotSha256, approvedManifest, observedRefs, version) {
  const baselinePin = { sha256: carrier.baselineSnapshot.sha256, commitSha: baseline.sourceCommitSha, tag: baseline.sourceTag };
  const candidatePin = { sha256: candidateSnapshotSha256, commitSha: carrier.candidate.sha,
    tag: carrier.candidate.tag ?? 'untagged-workflow-source' };
  if (carrier.mode === 'staging' && carrier.candidate.tag === null) {
    return { schemaVersion: 2, purpose: 'untagged-staging-source-comparison', workflowSha: carrier.workflowSha,
      candidateTreeSha: carrier.candidate.treeSha, bootstrapPolicySha256: hash(decodeAuthority(carrier.bootstrapPolicyBase64, 16384)),
      canonicalAuthoritySha256: hash(decodeAuthority(carrier.canonicalAuthorityBase64, 1024 * 1024)),
      admissionBindingSha256: carrier.bindingSha256, baselinePin, candidatePin,
      observedRefs: { baseline: observedRefs.baseline, candidate: { commitSha: carrier.candidate.sha, treeSha: carrier.candidate.treeSha },
        workflow: { commitSha: carrier.workflowSha } }, approvedManifest };
  }
  return { schemaVersion: 1, baselinePin, candidatePin, observedRefs, approvedManifest, version };
}

async function resolveBaselineTag(repository, tag, token) {
  if (typeof tag !== 'string' || !/^[A-Za-z0-9][A-Za-z0-9._-]*$/.test(tag)) fail();
  let object = (await providerJson(repository, `/git/ref/tags/${tag}`, token)).object;
  for (let depth = 0; depth < 16; depth++) {
    if (object?.type === 'commit' && sha(object.sha)) return { tag, commitSha: object.sha };
    if (object?.type !== 'tag' || !sha(object.sha)) fail();
    object = (await providerJson(repository, `/git/tags/${object.sha}`, token)).object;
  }
  fail();
}

export async function runFromFile(documentPath, env = process.env) {
  let native;
  try {
    const root = dirname(resolve(documentPath));
    const document = JSON.parse((await readBoundedFile(documentPath, 1024 * 1024)).toString('utf8'));
    const { context } = document;
    if (!['staging', 'production'].includes(context?.mode) || !Number.isSafeInteger(document.checkRunId) || document.checkRunId <= 0) fail();
    const selected = await reconcileExactAdmission(context, document.checkRunId, providerJson, env.GH_TOKEN);
    if (JSON.stringify(selected.carrier) !== JSON.stringify(document.carrier)) fail();
    const carrier = selected.carrier;
    for (const path of EXPECTED_HELPERS) if (hash(await readBoundedFile(join(root, path), 1024 * 1024)) !== carrier.helperSha256[path]) fail();
    const approvedBytes = await readBoundedFile(join(root, 'approved-release.json'), 1024 * 1024);
    const approvedManifest = JSON.parse(approvedBytes.toString('utf8'));
    if (JSON.stringify(approvedManifest) !== JSON.stringify(document.approvedManifest)) fail();
    validateApprovedRelease(approvedManifest, { production: context.mode === 'production' });
    if (context.mode === 'production' && validateStagingRun(document.stagingRun, approvedManifest,
      { repository: context.repository, repositoryId: document.repositoryId }) !== context.stagingWorkflowSha) fail();
    const bootstrap = decodeAuthority(carrier.bootstrapPolicyBase64, 16384);
    const authority = decodeAuthority(carrier.canonicalAuthorityBase64, 1024 * 1024);
    native = createNativeCheckout({ trustedRoot: root, workspace: env.GITHUB_WORKSPACE,
      repository: context.repository, workflowSha: context.workflowSha });
    const transport = await prepareTrustedWorkflow({ bootstrapPolicyBytes: bootstrap, pin: hash(bootstrap),
      providerContext: { repository: context.repository, event: context.event, ref: context.ref,
        workflowRef: context.workflowRef, workflowSha: context.workflowSha, sourceSha: document.sourceSha },
      mode: context.mode, canonicalAuthorityBytes: authority, trustedCandidateSha: context.candidateSha,
      trustedStagingWorkflowSha: context.stagingWorkflowSha }, { checkoutExact: native.checkoutExact, runGit: native.runGit });
    await native.materializeCandidate(context.candidateSha, context.candidateTreeSha);
    const baselineBytes = await readBoundedFile(join(root, 'baseline.snapshot.json'), 64 * 1024 * 1024);
    if (hash(baselineBytes) !== carrier.baselineSnapshot.sha256) fail();
    const baseline = JSON.parse(baselineBytes.toString('utf8'));
    if (context.mode === 'staging') {
      const mode = env.MIGRATION_COMPARISON || 'merge-base', requestedBaseline = env.MIGRATION_BASELINE_SHA || '';
      if (!((mode === 'merge-base' && requestedBaseline === '') ||
          (mode === 'approved-endpoints' && requestedBaseline === baseline.sourceCommitSha))) fail();
    }
    const baselineRef = await resolveBaselineTag(context.repository, baseline.sourceTag, env.GH_TOKEN);
    if (baselineRef.commitSha !== baseline.sourceCommitSha || baselineRef.tag !== approvedManifest.rollback.sourceTag) fail();
    const version = JSON.parse(await readFile(join(native.candidatePath, 'release/version.json'), 'utf8')).version;
    const candidateSnapshot = await buildCandidateSnapshot(native.runGit, context.candidateSha, context.candidateTag ?? 'untagged-workflow-source');
    const observedRefs = { baseline: baselineRef, candidate: { tag: context.candidateTag, commitSha: context.candidateSha } };
    if (context.candidateTag !== null) {
      const tag = (await providerJson(context.repository, `/git/ref/tags/${context.candidateTag}`, env.GH_TOKEN)).object;
      if (tag?.type !== 'commit' || tag.sha !== context.candidateSha) fail();
    }
    const comparison = comparisonAuthority(carrier, baseline, hash(candidateSnapshot), approvedManifest, observedRefs, version);
    const comparisonBytes = Buffer.from(JSON.stringify(comparison)), comparisonPin = hash(comparisonBytes);
    const comparisonPath = join(root, 'migration-authority.json'), candidatePath = join(root, 'candidate.snapshot.json');
    await writeFile(comparisonPath, comparisonBytes, { flag: 'wx', mode: 0o600 });
    await writeFile(candidatePath, candidateSnapshot, { flag: 'wx', mode: 0o600 });
    const args = [comparisonPath, comparisonPin, join(root, 'baseline.snapshot.json'), candidatePath];
    const migration = await compareSnapshots(context.candidateTag === null ? ['source-only-staging', ...args] : args);
    const receipt = { transport, migration, workflowSha: context.workflowSha, candidateSha: context.candidateSha,
      separateCandidateCheckoutPerformed: true, sourceDirectory: native.candidatePath, releaseAuthorized: false };
    await writeFile(join(root, 'preparation-result.json'), JSON.stringify(receipt), { flag: 'wx', mode: 0o600 });
    await appendFile(env.GITHUB_ENV, `COMMIT_SHA=${context.candidateSha}\nFEDRIL_SOURCE_DIR=${native.candidatePath}\n` +
      `APPROVED_RELEASE_MANIFEST=${join(root, 'approved-release.json')}\n` +
      `API_PACKAGE_GUARD=${join(root, 'tools/release/check-api-package.py')}\n` +
      `API_PACKAGE_GUARD_SHA256=${carrier.helperSha256['tools/release/check-api-package.py']}\n`);
    return receipt;
  } catch { fail(); }
  finally { if (native) { try { await native.close(); } catch { fail(); } } }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  runFromFile(process.argv[2]).catch(() => { console.error('Trusted release preparation held; preserve outputs and reconcile without retry.'); process.exitCode = 1; });
}
