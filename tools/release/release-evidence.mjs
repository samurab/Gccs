import { execFileSync } from "node:child_process";
import { readFile } from "node:fs/promises";
import { setTimeout } from "node:timers/promises";
import { pathToFileURL } from "node:url";

const shaPattern = /^[a-f0-9]{40}$/;
const workflowPath = ".github/workflows/repository-artifact-policy.yml";
const stagingPath = ".github/workflows/staging.yml";
const positiveInteger = value => Number.isSafeInteger(value) && value > 0;
const reject = message => { throw new Error(message); };

function repositoryIdentity(repository, repositoryId) {
  if (!/^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/.test(repository ?? "") ||
      !/^[1-9][0-9]*$/.test(String(repositoryId ?? ""))) reject("Invalid release repository identity");
}

function sameRepository(run, repository, repositoryId) {
  return [run?.repository, run?.head_repository].every(repo =>
    repo?.full_name === repository && String(repo.id) === String(repositoryId));
}

export function artifactPolicyState(payload, { repository, repositoryId, commitSha, ref }) {
  repositoryIdentity(repository, repositoryId);
  if (!shaPattern.test(commitSha ?? "") || !/^refs\/(heads|tags)\/[A-Za-z0-9_.\/-]+$/.test(ref ?? "")) {
    reject("Invalid artifact-policy commit or ref");
  }
  if (!Array.isArray(payload?.workflow_runs)) reject("Invalid artifact-policy response");
  const runs = payload.workflow_runs.filter(run => run.path === workflowPath && run.event === "push" &&
    run.head_sha === commitSha && run.head_branch === ref.replace(/^refs\/(heads|tags)\//, ""));
  if (runs.length === 0) return "pending";
  if (runs.some(run => !positiveInteger(run.id) || !sameRepository(run, repository, repositoryId))) {
    reject("Artifact-policy run repository identity does not match");
  }
  const latest = runs.reduce((a, b) => a.id > b.id ? a : b);
  // GitHub head_branch omits the namespace. The event-derived workflow title
  // binds the full ref and initial-tag history population without blob access.
  const prefix = `artifact-policy ${ref} ${commitSha} `;
  const before = latest.display_title?.startsWith(prefix) ? latest.display_title.slice(prefix.length) : "";
  if (!shaPattern.test(before) || (ref.startsWith("refs/tags/") && before !== "0".repeat(40))) {
    reject("Artifact-policy full ref and history scope do not match");
  }
  if (["queued", "in_progress", "waiting", "requested", "pending"].includes(latest.status)) return "pending";
  if (latest.status !== "completed" || latest.conclusion !== "success") reject("Exact-ref artifact policy did not succeed");
  return "passed";
}

export async function requireArtifactPolicy(identity, readRuns, delay = setTimeout) {
  // Separate push-policy jobs may still be starting when automatic staging begins.
  for (let attempt = 0; attempt < 13; attempt += 1) {
    if (artifactPolicyState(await readRuns(), identity) === "passed") return;
    if (attempt < 12) await delay(10_000);
  }
  reject("Exact-ref artifact policy is unavailable or still pending; deployment held");
}

export function validateStagingRun(run, manifest, { repository, repositoryId }) {
  repositoryIdentity(repository, repositoryId);
  const id = manifest?.staging?.runId;
  if (!positiveInteger(id) || manifest.staging.environment !== "staging" ||
      manifest.staging.evidenceUrl !== `https://github.com/${repository}/actions/runs/${id}`) {
    reject("Staging evidence URL and run identity do not match");
  }
  if (run?.id !== id || !sameRepository(run, repository, repositoryId) || run.path !== stagingPath ||
      run.event !== "workflow_dispatch" || run.head_branch !== "main" || !shaPattern.test(run.head_sha ?? "") ||
      !positiveInteger(run.run_attempt) || run.status !== "completed" || run.conclusion !== "success") {
    reject("Approved staging run is not a successful expected main workflow run");
  }
  // This is the workflow's dispatch SHA, not the detached candidate checkout SHA.
  return run.head_sha;
}

export function validateAttestationRun(results, run, manifest, identity) {
  const sourceSha = validateStagingRun(run, manifest, identity);
  const { repository, repositoryId } = identity;
  const source = `https://github.com/${repository}`;
  const invocation = `${source}/actions/runs/${run.id}/attempts/${run.run_attempt}`;
  const workflow = `${source}/${stagingPath}@refs/heads/main`;
  if (!Array.isArray(results) || results.length === 0) reject("No verified staging attestations supplied");
  // Only consume gh's cryptographically verified certificate summary. Predicate
  // fields alone are workflow-controlled and cannot prove a run's identity.
  const matched = results.some(result => {
    const verified = result?.verificationResult;
    const cert = verified?.signature?.certificate;
    return verified?.statement?.predicateType === "https://slsa.dev/provenance/v1" && cert &&
      cert.certificateIssuer === "https://token.actions.githubusercontent.com" &&
      cert.subjectAlternativeName === workflow && cert.runnerEnvironment === "github-hosted" &&
      cert.sourceRepositoryURI === source && cert.sourceRepositoryIdentifier === String(repositoryId) &&
      cert.sourceRepositoryDigest === sourceSha && cert.sourceRepositoryRef === "refs/heads/main" &&
      cert.buildConfigURI === workflow && cert.buildConfigDigest === sourceSha &&
      cert.buildTrigger === "workflow_dispatch" && cert.runInvocationURI === invocation;
  });
  if (!matched) reject("Verified artifact provenance does not match the approved staging run and attempt");
}

const readJson = async path => {
  try { return JSON.parse(await readFile(path, "utf8")); }
  catch { reject("Unable to read release evidence JSON"); }
};

async function readVerification() {
  const chunks = [];
  let size = 0;
  for await (const chunk of process.stdin) {
    size += chunk.length;
    if (size > 8 * 1024 * 1024) reject("Verified attestation output exceeds the size limit");
    chunks.push(chunk);
  }
  try { return JSON.parse(Buffer.concat(chunks).toString("utf8")); }
  catch { reject("Invalid verified attestation JSON"); }
}

async function main([command, ...args]) {
  const identity = { repository: process.env.GITHUB_REPOSITORY, repositoryId: process.env.GITHUB_REPOSITORY_ID };
  if (command === "artifact-policy" && args.length === 4) {
    const [repository, repositoryId, commitSha, ref] = args;
    const expected = { repository, repositoryId, commitSha, ref };
    artifactPolicyState({ workflow_runs: [] }, expected);
    await requireArtifactPolicy(expected, async () => {
      try {
        return JSON.parse(execFileSync("gh", ["api", "--method", "GET",
          `repos/${repository}/actions/workflows/repository-artifact-policy.yml/runs`,
          "-f", `head_sha=${commitSha}`, "-f", `branch=${ref.replace(/^refs\/(heads|tags)\//, "")}`, "-f", "event=push", "-f", "per_page=100"],
        { encoding: "utf8", stdio: ["ignore", "pipe", "pipe"], timeout: 20_000, maxBuffer: 8 * 1024 * 1024 }));
      } catch {
        reject("Unable to read exact-ref artifact policy; deployment held");
      }
    });
  } else if (command === "staging-run" && args.length === 2) {
    console.log(validateStagingRun(await readJson(args[1]), await readJson(args[0]), identity));
  } else if (command === "attestation" && args.length === 2) {
    validateAttestationRun(await readVerification(), await readJson(args[1]), await readJson(args[0]), identity);
  } else reject("Invalid release-evidence command");
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  main(process.argv.slice(2)).catch(error => {
    console.error(error.message);
    process.exitCode = 1;
  });
}
