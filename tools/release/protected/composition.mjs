import { bindWorkflowBootstrap } from './workflow-bootstrap-binding.mjs';
import { planCanonicalTransport } from './finite-canonical-transport.mjs';
import { executeCanonicalTransport, NativeTransportError } from './native-canonical-transport.mjs';

// The pre-checkout inline consumer verifies the entire downloaded closure before importing this module.
const helpers = Object.freeze({ bindWorkflowBootstrap, planCanonicalTransport, executeCanonicalTransport, NativeTransportError });

export class WorkflowPreparationError extends Error {
  constructor(code, checkoutMayHaveOccurred = false, nativeMutationMayHaveOccurred = false) {
    super(code);
    this.name = 'WorkflowPreparationError';
    this.checkoutMayHaveOccurred = checkoutMayHaveOccurred;
    this.nativeMutationMayHaveOccurred = nativeMutationMayHaveOccurred;
  }
}

function record(value, keys) {
  if (!value || typeof value !== 'object' || Array.isArray(value) ||
      ![Object.prototype, null].includes(Object.getPrototypeOf(value)) || Object.getOwnPropertySymbols(value).length) throw new Error();
  const descriptors = Object.getOwnPropertyDescriptors(value);
  if (Object.keys(descriptors).sort().join('\0') !== [...keys].sort().join('\0') ||
      keys.some(key => !Object.hasOwn(descriptors[key], 'value'))) throw new Error();
  return Object.freeze(Object.fromEntries(keys.map(key => [key, descriptors[key].value])));
}

export async function prepareTrustedWorkflow(inputs, io) {
  let source, callbacks, bootstrap, context, plan;
  try {
    source = record(inputs, ['bootstrapPolicyBytes', 'pin', 'providerContext', 'mode', 'canonicalAuthorityBytes',
      'trustedCandidateSha', 'trustedStagingWorkflowSha']);
    callbacks = record(io, ['checkoutExact', 'runGit']);
    if (typeof callbacks.checkoutExact !== 'function' || typeof callbacks.runGit !== 'function' ||
        !Buffer.isBuffer(source.bootstrapPolicyBytes) || !Buffer.isBuffer(source.canonicalAuthorityBytes)) throw new Error();
    const provider = record(source.providerContext, ['repository', 'event', 'ref', 'workflowRef', 'workflowSha', 'sourceSha']);
    if (Object.values(provider).some(value => typeof value !== 'string')) throw new Error();
    source = Object.freeze({ ...source, providerContext: provider,
      bootstrapPolicyBytes: Buffer.from(source.bootstrapPolicyBytes), canonicalAuthorityBytes: Buffer.from(source.canonicalAuthorityBytes) });
    bootstrap = helpers.bindWorkflowBootstrap(source.bootstrapPolicyBytes, source.pin, provider, source.mode);
    if (source.mode === 'production' && source.trustedStagingWorkflowSha === null) throw new Error();
    context = Object.freeze({ repository: bootstrap.checkout.repository, workflowSha: bootstrap.workflowSha,
      candidateSha: source.trustedCandidateSha, stagingWorkflowSha: source.trustedStagingWorkflowSha, shallow: true });
    plan = helpers.planCanonicalTransport(source.canonicalAuthorityBytes, bootstrap.canonicalAuthoritySha256, context);
  } catch {
    throw new WorkflowPreparationError('trusted_workflow_preflight_rejected');
  }

  let checkout;
  try {
    checkout = await callbacks.checkoutExact(Object.freeze({ ...bootstrap.checkout }));
  } catch {
    throw new WorkflowPreparationError('trusted_checkout_uncertain_reconcile_no_retry', true);
  }
  try {
    checkout = record(checkout, ['repository', 'headSha', 'shallow']);
    if (checkout.repository !== context.repository || checkout.headSha !== bootstrap.workflowSha ||
        typeof checkout.shallow !== 'boolean') throw new Error();
  } catch {
    throw new WorkflowPreparationError('trusted_checkout_observation_rejected_reconcile_no_retry', true);
  }

  let transported;
  try {
    transported = await helpers.executeCanonicalTransport(source.canonicalAuthorityBytes, bootstrap.canonicalAuthoritySha256,
      Object.freeze({ ...context, shallow: checkout.shallow }), async argv => {
        const result = record(await callbacks.runGit(Object.freeze([...argv])), ['exitCode', 'stdout']);
        if (!Number.isSafeInteger(result.exitCode) || !Buffer.isBuffer(result.stdout)) throw new Error();
        return { exitCode: result.exitCode, stdout: Buffer.from(result.stdout) };
      });
  } catch (error) {
    throw new WorkflowPreparationError('trusted_transport_failed_reconcile_no_retry', true,
      error instanceof helpers.NativeTransportError && error.mutationMayHaveOccurred === true);
  }
  return Object.freeze({ preparationOnly: true, mode: source.mode, workflowSha: bootstrap.workflowSha,
    candidateSha: context.candidateSha, stagingWorkflowSha: context.stagingWorkflowSha,
    bootstrapPolicySha256: source.pin, canonicalAuthoritySha256: bootstrap.canonicalAuthoritySha256,
    canonicalRootSha: plan.authority.rootSha, canonicalTipSha: transported.tipSha,
    commitsVerified: transported.commitsVerified, nativeAncestryChecks: transported.nativeAncestryChecks,
    destinationRef: transported.destinationRef, checkoutHeadVerified: true, observedGraphMatchesAuthority: true,
    workflowCheckoutPerformed: true, separateCandidateCheckoutPerformed: false, protectedResultProduced: false, deploymentPerformed: false,
    releaseAuthorized: false });
}
