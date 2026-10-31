import assert from "node:assert/strict";
import { createHash } from "node:crypto";
import test from "node:test";
import { planCanonicalTransport, verifyCanonicalGraph } from "./finite-canonical-transport.mjs";

const id = digit => digit.repeat(40);
const root = id("1");
const workflow = id("2");
const candidate = id("3");
const tip = id("4");
const tree = id("a");
const hash = bytes => createHash("sha256").update(bytes).digest("hex");

function fixture() {
  return { authority: { schemaVersion: 1, repository: "example-owner/example-repo",
    canonicalRef: "refs/heads/release-main", workflowSha: workflow, rootSha: root, tipSha: tip,
    commits: [{ sha: root, treeSha: tree, parents: [] },
      { sha: workflow, treeSha: tree, parents: [root] },
      { sha: candidate, treeSha: tree, parents: [workflow] },
      { sha: tip, treeSha: tree, parents: [candidate] }],
    classifiedTrees: [{ treeSha: tree, inventorySha256: "a".repeat(64) }],
    classificationReceiptSha256: "b".repeat(64) },
  context: { repository: "example-owner/example-repo", workflowSha: workflow,
    candidateSha: candidate, stagingWorkflowSha: workflow, shallow: true } };
}

function plan(f) {
  const bytes = Buffer.from(JSON.stringify(f.authority));
  return planCanonicalTransport(bytes, hash(bytes), f.context);
}

test("finite transport binds bootstrap and requests only exact canonical SHA", () => {
  const f = fixture();
  const result = plan(f);
  assert.equal(result.bootstrap.ref, workflow);
  assert.equal(result.bootstrap.clean, false);
  assert.equal(result.bootstrap.fetchTags, false);
  assert.equal(result.bootstrap.persistCredentials, false);
  assert.deepEqual(result.fetchArgv.slice(-2), ["https://github.com/example-owner/example-repo.git",
    `${tip}:refs/fedril/verified-canonical/${tip}`]);
  for (const flag of ["--no-tags", "--no-prune", "--no-prune-tags", "--no-recurse-submodules",
    "--no-auto-maintenance", "--no-write-fetch-head", "--no-write-commit-graph", "--refmap=", "--unshallow"]) {
    assert.ok(result.fetchArgv.includes(flag));
  }
  assert.equal(result.releaseAuthorized, false);
  f.context.shallow = false;
  assert.ok(!plan(f).fetchArgv.includes("--unshallow"));
});

test("finite transport rejects incomplete ancestry and old-history bridge", () => {
  for (const change of [
    f => { f.authority.commits[0].parents = [id("9")]; },
    f => { f.authority.commits[3].parents.push(id("9")); },
    f => { f.authority.commits.splice(1, 1); },
    f => { f.authority.commits.push({ sha: id("9"), treeSha: tree, parents: [] }); },
    f => { f.authority.commits[1].parents = [candidate]; }
  ]) {
    const f = fixture();
    change(f);
    assert.throws(() => plan(f));
  }
});

test("finite transport requires candidate and staging-workflow ancestry and exact workflow context", () => {
  for (const key of ["workflowSha", "candidateSha", "stagingWorkflowSha"]) {
    const f = fixture();
    f.context[key] = id("9");
    assert.throws(() => plan(f));
  }
  const f = fixture();
  f.context.stagingWorkflowSha = null;
  assert.equal(plan(f).releaseAuthorized, false);
});

test("finite transport validates all tree classification bindings", () => {
  for (const change of [
    f => { f.authority.classifiedTrees = []; },
    f => { f.authority.classifiedTrees.push(f.authority.classifiedTrees[0]); },
    f => { f.authority.classifiedTrees[0].inventorySha256 = ["a".repeat(64)]; },
    f => { f.authority.commits[1].treeSha = id("b"); },
    f => { f.authority.classificationReceiptSha256 = "bad"; }
  ]) {
    const f = fixture();
    change(f);
    assert.throws(() => plan(f));
  }
});

test("finite transport rejects wrong digest, repository, branch and unbounded input", () => {
  const f = fixture();
  const bytes = Buffer.from(JSON.stringify(f.authority));
  assert.throws(() => planCanonicalTransport(bytes, "0".repeat(64), f.context), /digest/);
  assert.throws(() => planCanonicalTransport(Buffer.alloc(1024 * 1024 + 1), hash(bytes), f.context), /digest/);
  for (const change of [
    g => { g.authority.repository = "../other"; },
    g => { g.context.repository = "another/repository"; },
    g => { g.authority.canonicalRef = "refs/heads/main"; },
    g => { g.context.shallow = "false"; },
    g => { g.authority.extra = "unclassified"; }
  ]) {
    const g = fixture();
    change(g);
    assert.throws(() => plan(g));
  }
});

test("finite transport supports a fully inventoried merge graph", () => {
  const f = fixture();
  f.authority.commits[2].parents = [root];
  f.authority.commits[3].parents = [candidate, workflow];
  assert.equal(plan(f).authority.commits.length, 4);
});

test("post-fetch comparison verifies complete observed parent and tree population", () => {
  const f = fixture();
  const bytes = Buffer.from(JSON.stringify(f.authority));
  const observed = { tipSha: tip, shallow: false, commits: [...f.authority.commits].reverse() };
  assert.equal(verifyCanonicalGraph(bytes, hash(bytes), f.context, observed).commitsVerified, 4);
  for (const mutate of [
    o => { o.shallow = true; },
    o => { o.tipSha = candidate; },
    o => { o.commits[0].parents = [workflow]; },
    o => { o.commits[0].treeSha = id("b"); },
    o => { o.commits.pop(); }
  ]) {
    const o = structuredClone(observed);
    mutate(o);
    assert.throws(() => verifyCanonicalGraph(bytes, hash(bytes), f.context, o));
  }
});
