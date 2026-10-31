import { createHash } from "node:crypto";

const sha = /^[0-9a-f]{40}$/;
const digest = /^[0-9a-f]{64}$/;
const validSha = value => typeof value === "string" && sha.test(value);
const validDigest = value => typeof value === "string" && digest.test(value);
const fail = message => { throw new Error(message); };
const hash = bytes => createHash("sha256").update(bytes).digest("hex");
const same = (left, right) => JSON.stringify(left) === JSON.stringify(right);

function keys(value, names, label) {
  if (!value || Array.isArray(value) || typeof value !== "object" ||
      !same(Object.keys(value).sort(), [...names].sort())) fail(`Invalid ${label} fields`);
}

function graphMap(commits, rootSha, tipSha) {
  if (!Array.isArray(commits) || !commits.length || commits.length > 4096) fail("Bounded complete commit graph required");
  const map = new Map();
  for (const commit of commits) {
    keys(commit, ["sha", "treeSha", "parents"], "commit");
    if (!validSha(commit.sha) || !validSha(commit.treeSha) || !Array.isArray(commit.parents) ||
        !commit.parents.every(parent => typeof parent === "string" && validSha(parent)) ||
        new Set(commit.parents).size !== commit.parents.length || map.has(commit.sha)) fail("Invalid or duplicate commit");
    map.set(commit.sha, commit);
  }
  if (!map.has(rootSha) || !map.has(tipSha)) fail("Governed root and tip must be present");
  for (const commit of map.values()) {
    if (!commit.parents.length && commit.sha !== rootSha) fail("Only the governed clean root may be parentless");
    if (commit.sha === rootSha && commit.parents.length) fail("Governed clean root must be parentless");
    if (commit.parents.some(parent => !map.has(parent))) fail("Commit graph has an unclassified parent boundary");
  }
  const done = new Set();
  const active = new Set();
  const stack = [[tipSha, false]];
  while (stack.length) {
    const [id, leaving] = stack.pop();
    if (leaving) { active.delete(id); done.add(id); continue; }
    if (active.has(id)) fail("Commit graph contains a cycle");
    if (done.has(id)) continue;
    active.add(id);
    stack.push([id, true]);
    for (const parent of map.get(id).parents) stack.push([parent, false]);
  }
  if (done.size !== map.size) fail("Graph contains commits outside the governed tip ancestry");
  return map;
}

function ancestor(map, older, newer) {
  const pending = [newer];
  const visited = new Set();
  while (pending.length) {
    const id = pending.pop();
    if (id === older) return true;
    if (visited.has(id)) continue;
    visited.add(id);
    pending.push(...map.get(id).parents);
  }
  return false;
}

function planCanonicalReleaseTransport(bytes, expectedDigest, context) {
  if (!Buffer.isBuffer(bytes) || bytes.length > 1024 * 1024 || typeof expectedDigest !== "string" ||
      !validDigest(expectedDigest) || hash(bytes) !== expectedDigest) fail("Exact bounded transport authority digest required");
  keys(context, ["repository", "workflowSha", "candidateSha", "stagingWorkflowSha", "shallow"], "provider context");
  if (typeof context.shallow !== "boolean" || !validSha(context.workflowSha) || !validSha(context.candidateSha) ||
      !(context.stagingWorkflowSha === null || validSha(context.stagingWorkflowSha))) fail("Exact provider source identities required");
  const authority = JSON.parse(new TextDecoder("utf-8", { fatal: true }).decode(bytes));
  keys(authority, ["schemaVersion", "repository", "canonicalRef", "workflowSha", "rootSha", "tipSha",
    "commits", "classifiedTrees", "classificationReceiptSha256"], "transport authority");
  if (authority.schemaVersion !== 1 || authority.canonicalRef !== "refs/heads/release-main" ||
      typeof authority.repository !== "string" || !/^[A-Za-z0-9][A-Za-z0-9_.-]*\/[A-Za-z0-9][A-Za-z0-9_.-]*$/.test(authority.repository) ||
      authority.repository !== context.repository || authority.workflowSha !== context.workflowSha ||
      !validSha(authority.rootSha) || !validSha(authority.tipSha) ||
      !validDigest(authority.classificationReceiptSha256)) fail("Transport authority does not match governed source context");
  const map = graphMap(authority.commits, authority.rootSha, authority.tipSha);
  const trees = new Set([...map.values()].map(commit => commit.treeSha));
  if (!Array.isArray(authority.classifiedTrees) || authority.classifiedTrees.length !== trees.size) fail("Every distinct source tree requires a classification binding");
  for (const tree of authority.classifiedTrees) {
    keys(tree, ["treeSha", "inventorySha256"], "tree classification");
    if (!trees.delete(tree.treeSha) || !validDigest(tree.inventorySha256)) fail("Invalid or duplicate classified tree");
  }
  for (const id of [context.workflowSha, context.candidateSha, context.stagingWorkflowSha].filter(id => id !== null)) {
    if (!map.has(id) || !ancestor(map, id, authority.tipSha)) fail("Required source is outside complete canonical ancestry");
  }
  const destinationRef = `refs/fedril/verified-canonical/${authority.tipSha}`;
  const fetchArgv = ["git", "--no-replace-objects", "-c", "fetch.fsckObjects=true", "fetch",
    "--no-tags", "--no-prune", "--no-prune-tags", "--no-recurse-submodules", "--no-auto-maintenance",
    "--no-write-fetch-head", "--no-write-commit-graph", "--refmap=", ...(context.shallow ? ["--unshallow"] : []),
    `https://github.com/${authority.repository}.git`, `${authority.tipSha}:${destinationRef}`];
  return { authoritySha256: expectedDigest, authority, fetchArgv, destinationRef, requireDestinationRefAbsent: true,
    bootstrap: { ref: context.workflowSha, fetchDepth: 1, fetchTags: false, submodules: false,
      clean: false, persistCredentials: false },
    releaseAuthorized: false };
}

export function verifyCanonicalGraph(bytes, expectedDigest, context, observed) {
  const plan = planCanonicalTransport(bytes, expectedDigest, context);
  keys(observed, ["tipSha", "shallow", "commits"], "observed graph");
  if (observed.shallow !== false || observed.tipSha !== plan.authority.tipSha) fail("Fetched graph is shallow or has the wrong tip");
  const expected = graphMap(plan.authority.commits, plan.authority.rootSha, plan.authority.tipSha);
  const actual = graphMap(observed.commits, plan.authority.rootSha, observed.tipSha);
  if (actual.size !== expected.size) fail("Fetched commit population differs from authority");
  for (const [id, commit] of actual) {
    const approved = expected.get(id);
    if (!approved || approved.treeSha !== commit.treeSha || !same(approved.parents, commit.parents)) {
      fail("Fetched parent or source tree differs from authority");
    }
  }
  return { tipSha: observed.tipSha, commitsVerified: actual.size, observedGraphMatchesAuthority: true,
    authoritySha256: plan.authoritySha256, releaseAuthorized: false };
}


export function planCanonicalTransport(bytes, expectedDigest, context) {
  if (!Buffer.isBuffer(bytes) || bytes.length > 1024 * 1024 || !validDigest(expectedDigest) || hash(bytes) !== expectedDigest) {
    fail("Exact bounded transport authority digest required");
  }
  const authority = JSON.parse(new TextDecoder("utf-8", { fatal: true }).decode(bytes));
  if (authority.schemaVersion !== 2) return planCanonicalReleaseTransport(bytes, expectedDigest, context);
  keys(context, ["repository", "sourceRef", "workflowSha", "candidateSha", "comparisonBaseSha", "prHeadSha", "shallow"], "CI context");
  keys(authority, ["schemaVersion", "purpose", "repository", "sourceRef", "workflowSha", "tipSha", "rootSha",
    "comparisonBaseSha", "prHeadSha", "commits", "classifiedTrees", "classificationReceiptSha256"], "CI authority");
  if (authority.purpose !== "classified-ci-history" || authority.repository !== context.repository ||
      typeof authority.repository !== "string" || !/^[A-Za-z0-9][A-Za-z0-9_.-]*\/[A-Za-z0-9][A-Za-z0-9_.-]*$/.test(authority.repository) ||
      authority.sourceRef !== context.sourceRef || !/^refs\/(heads\/[A-Za-z0-9_.\/-]+|pull\/[1-9][0-9]*\/merge)$/.test(context.sourceRef ?? "") ||
      authority.workflowSha !== context.workflowSha || authority.tipSha !== context.candidateSha ||
      authority.comparisonBaseSha !== context.comparisonBaseSha || authority.prHeadSha !== context.prHeadSha ||
      typeof context.shallow !== "boolean" || !validSha(context.workflowSha) || !validSha(context.candidateSha) ||
      context.candidateSha === "0".repeat(40) || !validSha(context.comparisonBaseSha) ||
      !(context.prHeadSha === null || validSha(context.prHeadSha)) || !validSha(authority.rootSha) ||
      !validDigest(authority.classificationReceiptSha256)) fail("CI authority differs from actual source context");
  if (context.sourceRef.startsWith("refs/pull/") ? (!validSha(context.prHeadSha) || context.comparisonBaseSha === "0".repeat(40)) :
      context.prHeadSha !== null) fail("CI event identities invalid");
  const map = graphMap(authority.commits, authority.rootSha, authority.tipSha);
  const required = [context.workflowSha, context.candidateSha, context.prHeadSha,
    context.comparisonBaseSha === "0".repeat(40) ? null : context.comparisonBaseSha].filter(value => value !== null);
  for (const id of required) if (!map.has(id) || !ancestor(map, id, authority.tipSha)) fail("CI source or comparison boundary outside classified ancestry");
  const trees = new Set([...map.values()].map(commit => commit.treeSha));
  if (!Array.isArray(authority.classifiedTrees) || authority.classifiedTrees.length !== trees.size) fail("Every CI tree needs classification");
  for (const tree of authority.classifiedTrees) {
    keys(tree, ["treeSha", "inventorySha256"], "CI tree classification");
    if (!trees.delete(tree.treeSha) || !validDigest(tree.inventorySha256)) fail("CI tree classification invalid");
  }
  const destinationRef = "refs/fedril/verified-ci/" + authority.tipSha;
  return { authoritySha256: expectedDigest, authority, destinationRef, requireDestinationRefAbsent: true,
    fetchArgv: ["git", "--no-replace-objects", "-c", "fetch.fsckObjects=true", "fetch", "--no-tags", "--no-prune",
      "--no-prune-tags", "--no-recurse-submodules", "--no-auto-maintenance", "--no-write-fetch-head", "--no-write-commit-graph",
      "--refmap=", ...(context.shallow ? ["--unshallow"] : []), "https://github.com/" + authority.repository + ".git",
      authority.tipSha + ":" + destinationRef], releaseAuthorized: false };
}
