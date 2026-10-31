import { createHash } from "node:crypto";

const paths = Object.freeze({
  staging: ".github/workflows/staging.yml",
  production: ".github/workflows/production-release.yml",
});
const sha = value => typeof value === "string" && /^[a-f0-9]{40}$/.test(value);
const digest = value => typeof value === "string" && /^[a-f0-9]{64}$/.test(value);
const reject = () => { throw new Error("workflow_bootstrap_binding_rejected"); };
const keys = (value, expected) => value && typeof value === "object" && !Array.isArray(value) &&
  Object.keys(value).sort().join("\0") === [...expected].sort().join("\0");

// Caller supplies an external trusted pin and authenticated provider context.
// This pure comparison neither delivers that authority nor performs checkout.
export function bindWorkflowBootstrap(bytes, pin, context, mode) {
  if (!Buffer.isBuffer(bytes) || bytes.length === 0 || bytes.length > 16384 || !digest(pin) ||
      createHash("sha256").update(bytes).digest("hex") !== pin ||
      typeof mode !== "string" || !Object.hasOwn(paths, mode)) reject();
  let policy;
  try { policy = JSON.parse(new TextDecoder("utf-8", { fatal: true }).decode(bytes)); }
  catch { reject(); }
  if (!keys(policy, ["schemaVersion", "repository", "event", "ref", "workflowPath", "workflowSha", "canonicalAuthoritySha256"]) ||
      policy.schemaVersion !== 1 || typeof policy.repository !== "string" ||
      !/^[A-Za-z0-9][A-Za-z0-9-]*\/[A-Za-z0-9][A-Za-z0-9._-]*$/.test(policy.repository) ||
      policy.event !== "workflow_dispatch" || policy.ref !== "refs/heads/release-main" ||
      policy.workflowPath !== paths[mode] || !sha(policy.workflowSha) || !digest(policy.canonicalAuthoritySha256)) reject();
  if (!keys(context, ["repository", "event", "ref", "workflowRef", "workflowSha", "sourceSha"]) ||
      context.repository !== policy.repository || context.event !== policy.event || context.ref !== policy.ref ||
      context.workflowRef !== `${policy.repository}/${policy.workflowPath}@${policy.ref}` ||
      context.workflowSha !== policy.workflowSha || context.sourceSha !== policy.workflowSha) reject();
  return {
    mode,
    workflowSha: policy.workflowSha,
    canonicalAuthoritySha256: policy.canonicalAuthoritySha256,
    checkout: {
      repository: policy.repository,
      ref: policy.workflowSha,
      "fetch-depth": 1,
      "fetch-tags": false,
      submodules: false,
      clean: false,
      "persist-credentials": false,
    },
    releaseAuthorized: false,
  };
}
