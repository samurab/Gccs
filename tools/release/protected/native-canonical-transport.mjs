import { planCanonicalTransport, verifyCanonicalGraph } from "./finite-canonical-transport.mjs";

export class NativeTransportError extends Error {
  constructor(code, mutationMayHaveOccurred) {
    super(code);
    this.name = "NativeTransportError";
    this.mutationMayHaveOccurred = mutationMayHaveOccurred;
  }
}

// The approved caller owns isolated checkout/configuration, exclusive writers,
// authority authentication, private command capture and exact native lifecycle.
export async function executeCanonicalTransport(bytes, pin, context, runGit) {
  let attempted = false;
  const reject = code => { throw new NativeTransportError(code, attempted); };
  const call = async (argv, allowed = [0]) => {
    let result;
    try { result = await runGit(argv); }
    catch { reject(attempted ? "native_operation_uncertain_reconcile" : "native_preflight_failed"); }
    if (!result || !Number.isSafeInteger(result.exitCode) || !allowed.includes(result.exitCode) ||
        !Buffer.isBuffer(result.stdout) || result.stdout.length > 2 * 1024 * 1024) {
      reject(attempted ? "native_operation_failed_reconcile" : "native_preflight_failed");
    }
    return result;
  };
  const text = result => {
    try { return new TextDecoder("utf-8", { fatal: true }).decode(result.stdout).trim(); }
    catch { reject(attempted ? "native_output_invalid_reconcile" : "native_output_invalid"); }
  };
  let plan;
  try { plan = planCanonicalTransport(bytes, pin, context); }
  catch { reject("transport_authority_rejected"); }
  const absent = await call(["git", "--no-replace-objects", "show-ref", "--verify", "--quiet", plan.destinationRef], [0, 1]);
  if (absent.exitCode !== 1) reject("destination_ref_collision");
  const before = text(await call(["git", "--no-replace-objects", "rev-parse", "--is-shallow-repository"]));
  if (before !== String(context.shallow)) reject("native_shallow_context_mismatch");
  attempted = true;
  await call(plan.fetchArgv);
  const tip = text(await call(["git", "--no-replace-objects", "rev-parse", "--verify", `${plan.destinationRef}^{commit}`]));
  if (tip !== plan.authority.tipSha) reject("native_tip_mismatch_reconcile");
  const shallow = text(await call(["git", "--no-replace-objects", "rev-parse", "--is-shallow-repository"]));
  if (shallow !== "false") reject("native_graph_still_shallow_reconcile");
  const output = text(await call(["git", "--no-replace-objects", "--no-pager", "log",
    "--no-decorate", "--no-show-signature", "--format=%H%x00%T%x00%P", plan.authority.tipSha]));
  const lines = output.split("\n");
  if (lines.length > 4096) reject("native_graph_population_exceeded_reconcile");
  const commits = lines.map(line => {
    const fields = line.split("\0");
    if (fields.length !== 3 || !/^[a-f0-9]{40}$/.test(fields[0]) || !/^[a-f0-9]{40}$/.test(fields[1]) ||
        (fields[2] !== "" && !/^[a-f0-9]{40}( [a-f0-9]{40})*$/.test(fields[2]))) {
      reject("native_graph_output_invalid_reconcile");
    }
    return { sha: fields[0], treeSha: fields[1], parents: fields[2] ? fields[2].split(" ") : [] };
  });
  let verified;
  try { verified = verifyCanonicalGraph(bytes, pin, context, { tipSha: tip, shallow: false, commits }); }
  catch { reject("native_graph_mismatch_reconcile"); }
  const sourceIdentities = context.sourceRef ?
    [context.workflowSha, context.candidateSha, context.prHeadSha, context.comparisonBaseSha === "0".repeat(40) ? null : context.comparisonBaseSha] :
    [context.workflowSha, context.candidateSha, context.stagingWorkflowSha];
  const sources = [...new Set(sourceIdentities.filter(value => value !== null))];
  for (const source of sources) {
    await call(["git", "--no-replace-objects", "merge-base", "--is-ancestor", source, tip]);
  }
  return { ...verified, nativeAncestryChecks: sources.length, destinationRef: plan.destinationRef,
    fetchAttempted: true, releaseAuthorized: false };
}
