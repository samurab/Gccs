import { createHash } from "node:crypto";
import {
  validateApprovedRelease, validateCandidate, validateMigrationComparison,
  validateMigrationDeletions, validateMigrationSourceEdits,
  validateMigrationSources, validateMigrationDiff
} from "./release-control.mjs";

export const maximumSnapshotBytes = 64 * 1024 * 1024;

const migrationRoot = "src/Gccs.Infrastructure/Persistence/Migrations/";
const modelSnapshot = `${migrationRoot}GccsDbContextModelSnapshot.cs`;
const sha = /^[0-9a-f]{40}$/;
const digest = /^[0-9a-f]{64}$/;
const hash = (algorithm, bytes) => createHash(algorithm).update(bytes).digest("hex");
const fail = message => { throw new Error(message); };
const migrationPath = path => path.startsWith(migrationRoot);

function exactKeys(value, keys, label) {
  if (!value || Array.isArray(value) || typeof value !== "object" ||
      Object.keys(value).length !== keys.length ||
      Object.keys(value).sort().join("|") !== [...keys].sort().join("|")) {
    fail(`Invalid ${label} fields`);
  }
}

// Pins and ref observations come from separately authorized sources; this parser
// validates their consistency, not their provenance or content classification.
export function parseMigrationSourceSnapshot(bytes, pin) {
  return parseSourceSnapshot(bytes, pin, false);
}

export function parseCommitSourceSnapshot(bytes, pin) {
  return parseSourceSnapshot(bytes, pin, true);
}

function parseSourceSnapshot(bytes, pin, untagged) {
  if (!Buffer.isBuffer(bytes) || bytes.length > maximumSnapshotBytes ||
      !digest.test(pin?.sha256 ?? "") || !sha.test(pin?.commitSha ?? "") ||
      (untagged ? pin?.tag !== null : typeof pin?.tag !== "string" || !pin.tag) || hash("sha256", bytes) !== pin.sha256) {
    fail("Snapshot bytes must match an exact governed digest and source pin");
  }
  const snapshot = JSON.parse(new TextDecoder("utf-8", { fatal: true }).decode(bytes));
  exactKeys(snapshot, ["schemaVersion", "sourceCommitSha", "sourceTag", "entries"], "snapshot");
  if (snapshot.schemaVersion !== (untagged ? 2 : 1) || snapshot.sourceCommitSha !== pin.commitSha ||
      snapshot.sourceTag !== pin.tag || !Array.isArray(snapshot.entries)) {
    fail("Snapshot source identity or schema does not match its pin");
  }
  const files = new Map();
  for (const entry of snapshot.entries) {
    exactKeys(entry, ["path", "type", "mode", "sizeBytes", "sha256", "gitBlobOid", "contentBase64"], "source entry");
    const path = entry.path;
    if (typeof path !== "string" || /[\\\u0000-\u001f]/.test(path) ||
        path.split("/").some(part => part === "." || part === ".." || !part) ||
        !((migrationPath(path) && /^[^/]+\.cs$/.test(path.slice(migrationRoot.length))) || /^infra\/database\/[^/]+\.sql$/.test(path)) ||
        files.has(path)) {
      fail("Snapshot contains a duplicate or out-of-scope source path");
    }
    if (entry.type !== "blob" || !["100644", "100755"].includes(entry.mode)) {
      fail("Snapshot source must be a regular Git blob with an explicit file mode");
    }
    if (typeof entry.contentBase64 !== "string") fail("Source content must be canonical base64");
    const content = Buffer.from(entry.contentBase64, "base64");
    if (content.toString("base64") !== entry.contentBase64 || entry.sizeBytes !== content.length ||
        !digest.test(entry.sha256 ?? "") || hash("sha256", content) !== entry.sha256 ||
        !sha.test(entry.gitBlobOid ?? "") ||
        hash("sha1", Buffer.concat([Buffer.from(`blob ${content.length}\0`), content])) !== entry.gitBlobOid) {
      fail("Source bytes do not match their size, SHA-256 and Git blob identity");
    }
    const source = new TextDecoder("utf-8", { fatal: true }).decode(content);
    if (source.includes("\0")) fail("Source must be UTF-8 text without NUL bytes");
    files.set(path, { ...entry, source });
  }
  return { sourceCommitSha: snapshot.sourceCommitSha, sourceTag: snapshot.sourceTag, files };
}

function addedSqlLines(previous, current) {
  if (previous === current) return [];
  let left = previous.split("\n");
  let right = current.split("\n");
  let prefix = 0;
  while (prefix < left.length && prefix < right.length && left[prefix] === right[prefix]) prefix += 1;
  let suffix = 0;
  while (suffix < left.length - prefix && suffix < right.length - prefix &&
      left[left.length - 1 - suffix] === right[right.length - 1 - suffix]) suffix += 1;
  left = left.slice(prefix, left.length - suffix);
  right = right.slice(prefix, right.length - suffix);
  // Bound the remaining LCS matrix to 64 MB after trimming unchanged ends.
  const width = right.length + 1;
  if ((left.length + 1) * width > 16_000_000) fail("SQL snapshot comparison exceeds the bounded line matrix");
  const lengths = new Uint32Array((left.length + 1) * width);
  for (let i = 1; i <= left.length; i += 1) {
    for (let j = 1; j <= right.length; j += 1) {
      lengths[i * width + j] = left[i - 1] === right[j - 1]
        ? lengths[(i - 1) * width + j - 1] + 1
        : Math.max(lengths[(i - 1) * width + j], lengths[i * width + j - 1]);
    }
  }
  const added = [];
  // Inspect both tie-break traces so one ambiguous alignment cannot hide a
  // destructive added line found by the other. This is not Git-diff equivalence.
  for (const insertOnTie of [true, false]) {
    let i = left.length;
    let j = right.length;
    while (i || j) {
      if (i && j && left[i - 1] === right[j - 1]) { i -= 1; j -= 1; }
      else if (j && (!i || lengths[i * width + j - 1] > lengths[(i - 1) * width + j] ||
          (insertOnTie && lengths[i * width + j - 1] === lengths[(i - 1) * width + j]))) {
        added.push(`+${right[--j]}`);
      } else i -= 1;
    }
  }
  return added;
}

export function validateMigrationSnapshotAuthority(options) {
  exactKeys(options, ["baselinePin", "candidatePin", "observedRefs", "approvedManifest", "version"], "comparison authority");
  const { baselinePin, candidatePin, observedRefs, approvedManifest, version } = options;
  for (const pin of [baselinePin, candidatePin]) {
    exactKeys(pin, ["sha256", "commitSha", "tag"], "source pin");
    if (typeof pin.sha256 !== "string" || !digest.test(pin.sha256) ||
        typeof pin.commitSha !== "string" || !sha.test(pin.commitSha) ||
        typeof pin.tag !== "string" || !pin.tag) fail("Invalid governed source pin");
  }
  const approved = validateApprovedRelease(approvedManifest);
  validateCandidate(candidatePin?.tag, candidatePin?.commitSha, version);
  if (observedRefs?.baseline?.tag !== baselinePin?.tag ||
      observedRefs?.baseline?.commitSha !== baselinePin?.commitSha ||
      observedRefs?.candidate?.tag !== candidatePin?.tag ||
      observedRefs?.candidate?.commitSha !== candidatePin?.commitSha) {
    fail("Both exact tag observations must match their governed source pins");
  }
  validateMigrationComparison(baselinePin.tag, candidatePin.commitSha, {
    comparison: "approved-endpoints", expectedBaseSha: baselinePin.commitSha,
    approvedBaselineTag: approved.rollback.sourceTag
  }, observedRefs.baseline.commitSha);
  return options;
}

export function validateMigrationSourceSnapshots(baselineBytes, candidateBytes, options) {
  const { baselinePin, candidatePin } = validateMigrationSnapshotAuthority(options);
  const baseline = parseMigrationSourceSnapshot(baselineBytes, baselinePin);
  const candidate = parseMigrationSourceSnapshot(candidateBytes, candidatePin);
  return compareMigrationInventories(baseline, candidate);
}

export function validateSourcePreflightAuthority(options) {
  exactKeys(options, ["baselinePin", "sourcePin", "observedBaselineRef", "approvedManifest"], "source preflight authority");
  const { baselinePin, sourcePin, observedBaselineRef, approvedManifest } = options;
  for (const pin of [baselinePin, sourcePin]) {
    exactKeys(pin, ["sha256", "commitSha", "tag"], "source pin");
    if (typeof pin.sha256 !== "string" || !digest.test(pin.sha256) ||
        typeof pin.commitSha !== "string" || !sha.test(pin.commitSha)) fail("Invalid governed source pin");
  }
  if (sourcePin.tag !== null || typeof baselinePin.tag !== "string" || !baselinePin.tag) {
    fail("Source preflight requires an explicitly untagged source and a tagged baseline");
  }
  exactKeys(observedBaselineRef, ["tag", "commitSha"], "baseline observation");
  if (observedBaselineRef.tag !== baselinePin.tag || observedBaselineRef.commitSha !== baselinePin.commitSha) {
    fail("Baseline observation must match the governed baseline pin");
  }
  const approved = validateApprovedRelease(approvedManifest);
  validateMigrationComparison(baselinePin.tag, sourcePin.commitSha, {
    comparison: "approved-endpoints", expectedBaseSha: baselinePin.commitSha,
    approvedBaselineTag: approved.rollback.sourceTag
  }, observedBaselineRef.commitSha);
  return options;
}

export function validateSourcePreflight(baselineBytes, sourceBytes, options) {
  const { baselinePin, sourcePin } = validateSourcePreflightAuthority(options);
  const baseline = parseMigrationSourceSnapshot(baselineBytes, baselinePin);
  const source = parseCommitSourceSnapshot(sourceBytes, sourcePin);
  return { purpose: "source-only-preflight", releaseAuthorized: false,
    ...compareMigrationInventories(baseline, source) };
}

function compareMigrationInventories(baseline, candidate) {
  const deleted = [...baseline.files.keys()].filter(path => migrationPath(path) && !candidate.files.has(path));
  validateMigrationDeletions(deleted);
  const edits = [];
  const typeChanges = [];
  for (const [path, old] of baseline.files) {
    const current = candidate.files.get(path);
    if (!current) continue;
    if (current.mode !== old.mode || current.type !== old.type) typeChanges.push(path);
    if (migrationPath(path) && current.sha256 !== old.sha256) edits.push(path);
  }
  validateMigrationSourceEdits(edits, typeChanges);
  const addedSources = [];
  let changedSqlFiles = 0;
  for (const [path, current] of candidate.files) {
    const old = baseline.files.get(path);
    if (migrationPath(path)) {
      if (old || path.endsWith(".Designer.cs") || path === modelSnapshot) continue;
      if (!/^\d{14}_[^/]+\.cs$/.test(path.slice(migrationRoot.length))) fail("Unrecognized added migration source");
      addedSources.push({ path, source: current.source });
    } else if (!old || old.sha256 !== current.sha256) {
      changedSqlFiles += 1;
      validateMigrationDiff(addedSqlLines(old?.source ?? "", current.source).join("\n"));
    }
  }
  validateMigrationSources(addedSources);
  return { baselineCommitSha: baseline.sourceCommitSha, candidateCommitSha: candidate.sourceCommitSha,
    sourceFilesCompared: baseline.files.size, addedMigrationSources: addedSources.length, changedSqlFiles };
}
