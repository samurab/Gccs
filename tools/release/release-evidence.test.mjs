import test from "node:test";
import assert from "node:assert/strict";
import { artifactPolicyState, requireArtifactPolicy, validateStagingRun, validateAttestationRun } from "./release-evidence.mjs";

const identity = { repository: "synthetic-owner/synthetic-repo", repositoryId: "42" };
const candidate = { ...identity, commitSha: "a".repeat(40), ref: "refs/tags/v0.2.1-rc.1" };
const repository = { full_name: identity.repository, id: 42 };
const run = {
  id: 73, run_attempt: 2, repository, head_repository: repository,
  path: ".github/workflows/staging.yml", event: "workflow_dispatch", head_branch: "main",
  head_sha: "b".repeat(40), status: "completed", conclusion: "success"
};
const manifest = {
  commitSha: candidate.commitSha,
  staging: { environment: "staging", runId: run.id, evidenceUrl: `https://github.com/${identity.repository}/actions/runs/${run.id}` }
};
const policyRun = {
  ...run, id: 100, path: ".github/workflows/repository-artifact-policy.yml", event: "push",
  head_branch: "v0.2.1-rc.1", head_sha: candidate.commitSha,
  display_title: `artifact-policy ${candidate.ref} ${candidate.commitSha} ${"0".repeat(40)}`
};
const source = `https://github.com/${identity.repository}`;
const workflow = `${source}/.github/workflows/staging.yml@refs/heads/main`;
const verified = () => [{ verificationResult: {
  statement: { predicateType: "https://slsa.dev/provenance/v1" },
  signature: { certificate: {
    certificateIssuer: "https://token.actions.githubusercontent.com",
    subjectAlternativeName: workflow, runnerEnvironment: "github-hosted",
    sourceRepositoryURI: source, sourceRepositoryIdentifier: identity.repositoryId,
    sourceRepositoryDigest: run.head_sha, sourceRepositoryRef: "refs/heads/main",
    buildConfigURI: workflow, buildConfigDigest: run.head_sha, buildTrigger: "workflow_dispatch",
    runInvocationURI: `${source}/actions/runs/${run.id}/attempts/${run.run_attempt}`
  } }
} }];

test("an unrelated green main/PR policy cannot authorize a candidate tag", () => {
  assert.equal(artifactPolicyState({ workflow_runs: [{ ...policyRun, head_branch: "main" }] }, candidate), "pending");
  assert.equal(artifactPolicyState({ workflow_runs: [{ ...policyRun, event: "pull_request" }] }, candidate), "pending");
  assert.equal(artifactPolicyState({ workflow_runs: [{ ...policyRun, head_sha: "c".repeat(40) }] }, candidate), "pending");
  assert.equal(artifactPolicyState({ workflow_runs: [policyRun] }, candidate), "passed");
});

test("a same-name branch policy cannot stand in for a tag's initial-history policy", () => {
  for (const display_title of [
    `artifact-policy refs/heads/v0.2.1-rc.1 ${candidate.commitSha} ${"0".repeat(40)}`,
    `artifact-policy ${candidate.ref} ${candidate.commitSha} ${"b".repeat(40)}`,
    `artifact-policy ${candidate.ref} ${"b".repeat(40)} ${"0".repeat(40)}`,
    "Repository artifact policy", undefined
  ]) {
    assert.throws(() => artifactPolicyState({ workflow_runs: [{ ...policyRun, display_title }] }, candidate), /ref and history scope/);
  }
  const branch = { ...candidate, ref: "refs/heads/main" };
  assert.equal(artifactPolicyState({ workflow_runs: [{ ...policyRun, head_branch: "main",
    display_title: `artifact-policy ${branch.ref} ${branch.commitSha} ${"b".repeat(40)}` }] }, branch), "passed");
});

test("the latest exact-tag failure or in-progress run wins over older success", () => {
  assert.throws(() => artifactPolicyState({ workflow_runs: [policyRun, { ...policyRun, id: 101, conclusion: "failure" }] }, candidate), /did not succeed/);
  assert.equal(artifactPolicyState({ workflow_runs: [policyRun, { ...policyRun, id: 101, status: "in_progress", conclusion: null }] }, candidate), "pending");
  for (const conclusion of ["cancelled", "skipped", "neutral", "timed_out", null]) {
    assert.throws(() => artifactPolicyState({ workflow_runs: [{ ...policyRun, conclusion }] }, candidate), /did not succeed/);
  }
});

test("policy metadata rejects foreign identities and malformed inputs", () => {
  for (const overrides of [{ repository: { ...repository, id: 99 } }, { head_repository: { ...repository, full_name: "other/repo" } }, { id: 0 }]) {
    assert.throws(() => artifactPolicyState({ workflow_runs: [{ ...policyRun, ...overrides }] }, candidate), /identity/);
  }
  for (const overrides of [{ commitSha: "invalid" }, { ref: "v0.2.1-rc.1" }, { ref: "" }, { repositoryId: "0" }, { repository: "owner/repo;command" }]) {
    assert.throws(() => artifactPolicyState({ workflow_runs: [] }, { ...candidate, ...overrides }), /Invalid/);
  }
  assert.throws(() => artifactPolicyState({}, candidate), /response/);
});

test("policy waits only on pending metadata and stops on a real failure or access error", async () => {
  let reads = 0, delays = 0;
  await requireArtifactPolicy(candidate, async () => ({ workflow_runs: ++reads === 1 ? [] : [policyRun] }), async ms => { assert.equal(ms, 10_000); delays += 1; });
  assert.equal(reads, 2); assert.equal(delays, 1);
  reads = 0;
  await assert.rejects(requireArtifactPolicy(candidate, async () => { reads += 1; throw new Error("access denied"); }, async () => assert.fail("No retry after access error")), /access denied/);
  assert.equal(reads, 1);
  await assert.rejects(requireArtifactPolicy(candidate, async () => ({ workflow_runs: [{ ...policyRun, conclusion: "failure" }] }), async () => assert.fail("No retry after policy failure")), /did not succeed/);
});

test("missing policy evidence has a finite wait budget", async () => {
  let reads = 0, delays = 0;
  await assert.rejects(requireArtifactPolicy(candidate, async () => { reads += 1; return { workflow_runs: [] }; }, async () => { delays += 1; }), /pending/);
  assert.equal(reads, 13); assert.equal(delays, 12);
});

test("staging evidence identifies the same repository, run ID and successful main workflow", () => {
  assert.equal(validateStagingRun(run, manifest, identity), run.head_sha);
  for (const overrides of [{ id: 74 }, { run_attempt: 0 }, { path: ".github/workflows/other.yml" }, { event: "push" }, { head_branch: "feature" }, { head_sha: "not-sha" }, { status: "in_progress" }, { conclusion: "failure" }, { repository: { ...repository, id: 99 } }]) {
    assert.throws(() => validateStagingRun({ ...run, ...overrides }, manifest, identity), /staging run/);
  }
  for (const evidenceUrl of [`${source}/actions/runs/74`, "https://github.com/other/repo/actions/runs/73", `${source}/actions/runs/073`, `${source}/actions/runs/73?unexpected=true`]) {
    assert.throws(() => validateStagingRun(run, { ...manifest, staging: { ...manifest.staging, evidenceUrl } }, identity), /URL/);
  }
});

test("a detached candidate SHA is not mistaken for its producer workflow SHA", () => {
  assert.notEqual(manifest.commitSha, run.head_sha);
  assert.doesNotThrow(() => validateAttestationRun(verified(), run, manifest, identity));
  const wrong = verified(); wrong[0].verificationResult.signature.certificate.sourceRepositoryDigest = manifest.commitSha;
  assert.throws(() => validateAttestationRun(wrong, run, manifest, identity), /provenance/);
});

test("verified certificate binds exact run/attempt, workflow, source, trigger and repository ID", () => {
  const overrides = {
    certificateIssuer: "https://other.example.test", subjectAlternativeName: `${source}/.github/workflows/other.yml@refs/heads/main`,
    runnerEnvironment: "self-hosted", sourceRepositoryURI: "https://github.com/other/repo",
    sourceRepositoryIdentifier: "99", sourceRepositoryDigest: candidate.commitSha, sourceRepositoryRef: "refs/heads/feature",
    buildConfigURI: `${source}/.github/workflows/other.yml@refs/heads/main`, buildConfigDigest: candidate.commitSha,
    buildTrigger: "pull_request", runInvocationURI: `${source}/actions/runs/${run.id}/attempts/1`
  };
  for (const [key, value] of Object.entries(overrides)) {
    const wrong = verified(); wrong[0].verificationResult.signature.certificate[key] = value;
    assert.throws(() => validateAttestationRun(wrong, run, manifest, identity), /provenance/, key);
    const missing = verified(); delete missing[0].verificationResult.signature.certificate[key];
    assert.throws(() => validateAttestationRun(missing, run, manifest, identity), /provenance/, key);
  }
});

test("predicate-only claims and unverified payloads cannot substitute for verified certificates", () => {
  const forged = verified(); delete forged[0].verificationResult.signature;
  forged[0].verificationResult.statement.predicate = { runDetails: { metadata: { invocationId: `${source}/actions/runs/${run.id}/attempts/2` } } };
  assert.throws(() => validateAttestationRun(forged, run, manifest, identity), /provenance/);
  for (const payload of [[], {}, [{ statement: verified()[0].verificationResult.statement }]]) {
    assert.throws(() => validateAttestationRun(payload, run, manifest, identity), /attestation|provenance/);
  }
  const wrong = verified(); wrong[0].verificationResult.statement.predicateType = "https://example.test/untrusted";
  assert.throws(() => validateAttestationRun(wrong, run, manifest, identity), /provenance/);
});
