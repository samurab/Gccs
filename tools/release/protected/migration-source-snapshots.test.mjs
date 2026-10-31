import assert from "node:assert/strict";
import { createHash } from "node:crypto";
import test from "node:test";
import { maximumSnapshotBytes, parseMigrationSourceSnapshot, validateMigrationSourceSnapshots,
  validateUntaggedStagingSnapshotAuthority, validateUntaggedStagingSourceSnapshots } from "./migration-source-snapshots.mjs";

import { main, parseComparisonAuthority, parseUntaggedStagingAuthority } from "./migration-source-snapshots-cli.mjs";

const migrationRoot = "src/Gccs.Infrastructure/Persistence/Migrations/";
const originalPath = `${migrationRoot}20260927000000_Original.cs`;
const addedPath = `${migrationRoot}20261026000000_Added.cs`;
const snapshotPath = `${migrationRoot}GccsDbContextModelSnapshot.cs`;
const sqlPath = "infra/database/bootstrap.sql";
const baselineSha = "b".repeat(40);
const candidateSha = "c".repeat(40);
const baselineTag = "v1.2.2";
const candidateTag = "v1.2.4-rc.1";
const hash = (algorithm, bytes) => createHash(algorithm).update(bytes).digest("hex");
const forward = operation => `protected override void Up(MigrationBuilder migrationBuilder) { ${operation} }
  protected override void Down(MigrationBuilder migrationBuilder) { migrationBuilder.DropTable("rollback"); }`;

function entry(path, source, overrides = {}) {
  const bytes = Buffer.isBuffer(source) ? source : Buffer.from(source);
  return { path, type: "blob", mode: "100644", sizeBytes: bytes.length,
    sha256: hash("sha256", bytes),
    gitBlobOid: hash("sha1", Buffer.concat([Buffer.from(`blob ${bytes.length}\0`), bytes])),
    contentBase64: bytes.toString("base64"), ...overrides };
}

function encoded(entries, sourceCommitSha, sourceTag) {
  const bytes = Buffer.from(JSON.stringify({ schemaVersion: 1, sourceCommitSha, sourceTag, entries }));
  return { bytes, pin: { sha256: hash("sha256", bytes), commitSha: sourceCommitSha, tag: sourceTag } };
}

function inputs(previous, current) {
  const baseline = encoded(previous, baselineSha, baselineTag);
  const candidate = encoded(current, candidateSha, candidateTag);
  return { baseline, candidate, options: {
    baselinePin: baseline.pin, candidatePin: candidate.pin, version: "1.2.4",
    observedRefs: { baseline: { tag: baselineTag, commitSha: baselineSha },
      candidate: { tag: candidateTag, commitSha: candidateSha } },
    approvedManifest: {
      schemaVersion: 2, status: "legacy-import", version: "1.2.3-legacy.1",
      candidateTag: "historical-source", releaseTag: null, commitSha: "a".repeat(40),
      approvedAt: "2026-09-19", approvalScope: "Synthetic test fixture", dataPosture: "no-cui-only",
      staging: { environment: "staging", runId: 42, evidenceUrl: "https://github.com/example-owner/example-repo/actions/runs/42" },
      artifacts: { mode: "legacy-source-rebuild" },
      approvals: { engineering: false, security: false, product: false }, rollback: { sourceTag: baselineTag }
    }
  } };
}

function check(previous, current, change = () => {}) {
  const fixture = inputs(previous, current);
  change(fixture);
  return validateMigrationSourceSnapshots(fixture.baseline.bytes, fixture.candidate.bytes, fixture.options);
}

const original = entry(originalPath, forward('migrationBuilder.AddColumn<string>(name: "Reference");'));

test("snapshot adapter accepts a new expand-only migration, designer and model advancement", () => {
  const oldModel = entry(snapshotPath, "old model");
  const added = entry(addedPath, forward('migrationBuilder.CreateTable(name: "NewTable");'));
  const result = check([original, oldModel], [original, entry(snapshotPath, "new model"), added,
    entry(addedPath.replace(/\.cs$/, ".Designer.cs"), "designer")]);
  assert.deepEqual(result, { baselineCommitSha: baselineSha, candidateCommitSha: candidateSha,
    sourceFilesCompared: 2, addedMigrationSources: 1, changedSqlFiles: 0 });
});

test("snapshot adapter rejects deletion and rename of a prior migration or designer", () => {
  for (const path of [originalPath, originalPath.replace(/\.cs$/, ".Designer.cs"), snapshotPath]) {
    assert.throws(() => check([entry(path, "old")], []), /must not delete migration/);
    assert.throws(() => check([entry(path, "old")], [entry(path.replace(/\.cs$/, "_Renamed.cs"), "new")]),
      /must not delete migration|must not modify prior migration/);
  }
});

test("snapshot adapter rejects edits to old migrations and designers", () => {
  for (const path of [originalPath, originalPath.replace(/\.cs$/, ".Designer.cs")]) {
    assert.throws(() => check([entry(path, "old")], [entry(path, "changed")]), /must not modify prior migration/);
  }
});

test("snapshot adapter rejects mode changes even for the advancing model snapshot and SQL", () => {
  for (const path of [originalPath, snapshotPath, sqlPath]) {
    assert.throws(() => check([entry(path, "same")], [entry(path, "same", { mode: "100755" })]), /must not change migration file types/);
  }
});

test("snapshot adapter retains destructive Up and malformed source rejection", () => {
  for (const operation of ['migrationBuilder.DropTable("old");', 'migrationBuilder.Sql("UPDATE Records SET Id=1");']) {
    assert.throws(() => check([], [entry(addedPath, forward(operation))]), /expand-only/);
  }
  assert.throws(() => check([], [entry(addedPath, "class MissingUp {}")]), /exactly one standard Up/);
  assert.throws(() => check([], [entry(`${migrationRoot}unrecognized.cs`, forward(""))]), /Unrecognized added migration/);
});

test("snapshot adapter allows unchanged historical SQL and benign added lines", () => {
  const old = entry(sqlPath, "DROP TABLE legacy;\nCREATE TABLE current(id integer);\n");
  const current = entry(sqlPath, "DROP TABLE legacy;\nCREATE TABLE current(id integer);\nCREATE INDEX ix ON current(id);\n");
  assert.equal(check([old], [old]).changedSqlFiles, 0);
  assert.equal(check([old], [current]).changedSqlFiles, 1);
});

test("snapshot adapter rejects destructive new SQL lines and moved destructive lines", () => {
  for (const sql of ["DROP TABLE old;", "TRUNCATE old;", "DELETE FROM old;", "UPDATE old SET id=1;", "ALTER TABLE old DROP COLUMN id;"]) {
    assert.throws(() => check([], [entry(sqlPath, sql)]), /expand-only/);
    assert.throws(() => check([entry(sqlPath, "SELECT 1;\n")], [entry(sqlPath, `SELECT 1;\n${sql}\n`)]), /expand-only/);
  }
  assert.throws(() => check([entry(sqlPath, "DROP TABLE old;\nSELECT 1;")],
    [entry(sqlPath, "SELECT 1;\nDROP TABLE old;")]), /expand-only/);
  assert.throws(() => check([entry(sqlPath, "SELECT 1;\nDROP TABLE old;")],
    [entry(sqlPath, "DROP TABLE old;\nSELECT 1;")]), /expand-only/);
});

test("snapshot adapter requires both observed exact tags and the approved rollback baseline", () => {
  for (const change of [
    f => { f.options.observedRefs = undefined; },
    f => { f.options.observedRefs.baseline.commitSha = "d".repeat(40); },
    f => { f.options.observedRefs.candidate.commitSha = "d".repeat(40); },
    f => { f.options.observedRefs.candidate.tag = "v1.2.4-rc.2"; },
    f => { f.options.approvedManifest.rollback.sourceTag = "v1.2.1"; }
  ]) assert.throws(() => check([], [], change), /observations|baseline/);
  assert.throws(() => check([], [], f => { f.options.version = "1.2.5"; }), /Candidate tag/);
});

test("snapshot parser rejects a digest or source pin mismatch", () => {
  const fixture = encoded([original], baselineSha, baselineTag);
  assert.throws(() => parseMigrationSourceSnapshot(fixture.bytes, { ...fixture.pin, sha256: "0".repeat(64) }), /digest/);
  assert.throws(() => parseMigrationSourceSnapshot(fixture.bytes, { ...fixture.pin, commitSha: candidateSha }), /identity/);
  assert.throws(() => parseMigrationSourceSnapshot(fixture.bytes, { ...fixture.pin, tag: candidateTag }), /identity/);
});

test("snapshot parser rejects duplicate, traversal, excluded and ambiguous source paths", () => {
  const malformed = ["../outside.cs", `${migrationRoot}../outside.cs`, "infra/database/../outside.sql",
    "output/evidence.json", `${migrationRoot}nested/file.cs`, `${migrationRoot}bad\\name.cs`,
    `${migrationRoot}bad\nname.cs`, "/infra/database/file.sql",
    "src/GccsXInfrastructure/Persistence/Migrations/20261026000000_Outside.cs"];
  for (const path of malformed) {
    const fixture = encoded([entry(path, "source")], baselineSha, baselineTag);
    assert.throws(() => parseMigrationSourceSnapshot(fixture.bytes, fixture.pin), /out-of-scope/);
  }
  const duplicate = encoded([original, original], baselineSha, baselineTag);
  assert.throws(() => parseMigrationSourceSnapshot(duplicate.bytes, duplicate.pin), /duplicate/);
});

test("snapshot parser rejects symlinks, non-blob types and unsupported modes", () => {
  for (const overrides of [{ mode: "120000" }, { mode: "040000" }, { type: "tree" }]) {
    const fixture = encoded([entry(originalPath, "source", overrides)], baselineSha, baselineTag);
    assert.throws(() => parseMigrationSourceSnapshot(fixture.bytes, fixture.pin), /regular Git blob/);
  }
});

test("snapshot parser verifies exact size, raw byte hashes and canonical encoding", () => {
  for (const overrides of [{ sizeBytes: 999 }, { sha256: "0".repeat(64) },
    { gitBlobOid: "0".repeat(40) }, { contentBase64: "c291cmNl\n" }]) {
    const fixture = encoded([entry(originalPath, "source", overrides)], baselineSha, baselineTag);
    assert.throws(() => parseMigrationSourceSnapshot(fixture.bytes, fixture.pin), /Source bytes/);
  }
  const invalidUtf8 = encoded([entry(originalPath, Buffer.from([255]))], baselineSha, baselineTag);
  assert.throws(() => parseMigrationSourceSnapshot(invalidUtf8.bytes, invalidUtf8.pin), /encoded data/);
  const nul = encoded([entry(originalPath, "source\0")], baselineSha, baselineTag);
  assert.throws(() => parseMigrationSourceSnapshot(nul.bytes, nul.pin), /NUL bytes/);
});

test("snapshot parser rejects extra metadata fields and bounds untrusted input", () => {
  const extra = encoded([{ ...original, payload: "unclassified" }], baselineSha, baselineTag);
  assert.throws(() => parseMigrationSourceSnapshot(extra.bytes, extra.pin), /entry fields/);
  assert.throws(() => parseMigrationSourceSnapshot(Buffer.alloc(maximumSnapshotBytes + 1), { sha256: "0".repeat(64), commitSha: baselineSha, tag: baselineTag }), /digest/);
});

test("snapshot SQL comparison accepts long unchanged ends without a full matrix", () => {
  const previous = "SELECT 1;\n".repeat(5_000);
  assert.equal(check([entry(sqlPath, previous)], [entry(sqlPath, `${previous}CREATE INDEX ix ON data(id);`)]).changedSqlFiles, 1);
  assert.throws(() => check([entry(sqlPath, previous)], [entry(sqlPath, `${previous}DROP TABLE data;`)]), /expand-only/);
});

test("snapshot SQL comparison accepts the measured population dimensions", () => {
  const prefix = "SELECT unchanged;\n".repeat(923);
  const suffix = "\nSELECT tail;\nSELECT end;";
  const previous = prefix + "SELECT prior;\n".repeat(3_795) + suffix;
  const current = prefix + "CREATE INDEX next ON data(id);\n".repeat(3_818) + suffix;
  assert.equal(check([entry(sqlPath, previous)], [entry(sqlPath, current)]).changedSqlFiles, 1);
});

test("snapshot SQL comparison fails closed before allocating an oversized changed matrix", () => {
  const previous = "SELECT old;\n".repeat(4_000);
  const current = "SELECT new;\n".repeat(4_000);
  assert.throws(() => check([entry(sqlPath, previous)], [entry(sqlPath, current)]), /bounded line matrix/);
});

test("snapshot parser accepts the measured serialized size using synthetic source", () => {
  const source = "x".repeat(38_802_304);
  const fixture = encoded([entry(snapshotPath, source)], baselineSha, baselineTag);
  assert.ok(fixture.bytes.length > 51_736_700 && fixture.bytes.length < maximumSnapshotBytes);
  assert.equal(parseMigrationSourceSnapshot(fixture.bytes, fixture.pin).files.size, 1);
});


test("snapshot comparison preserves historical destructive lines around trimmed duplicate boundaries", () => {
  const previous = "SELECT repeated;\nDROP TABLE historical;\nSELECT repeated;\nSELECT end;";
  assert.equal(check([entry(sqlPath, previous)], [entry(sqlPath,
    "SELECT repeated;\nDROP TABLE historical;\nCREATE INDEX next ON data(id);\nSELECT repeated;\nSELECT end;")]).changedSqlFiles, 1);
  assert.throws(() => check([entry(sqlPath, previous)], [entry(sqlPath,
    "SELECT repeated;\nSELECT repeated;\nDROP TABLE historical;\nSELECT end;")]), /expand-only/);
});

function authorityFixture() {
  const fixture = inputs([original], [original, entry(addedPath, forward('migrationBuilder.CreateTable(name: "NewTable");'))]);
  const bytes = Buffer.from(JSON.stringify({ schemaVersion: 1, ...fixture.options }));
  return { ...fixture, authorityBytes: bytes, authoritySha256: hash("sha256", bytes) };
}

test("snapshot CLI validates authority before source reads and emits only hashes/counts", async () => {
  const f = authorityFixture();
  const reads = [];
  const messages = [];
  const data = { authority: f.authorityBytes, baseline: f.baseline.bytes, candidate: f.candidate.bytes };
  const result = await main(["authority", f.authoritySha256, "baseline", "candidate"], async (path, limit) => {
    reads.push([path, limit]);
    return data[path];
  }, { log: value => messages.push(JSON.parse(value)) });
  assert.equal(result.addedMigrationSources, 1);
  assert.deepEqual(reads, [["authority", 1024 * 1024], ["baseline", maximumSnapshotBytes], ["candidate", maximumSnapshotBytes]]);
  assert.equal(messages.length, 1);
  assert.equal(messages[0].authoritySha256, f.authoritySha256);
  assert.equal(messages[0].baselineSnapshotSha256, f.baseline.pin.sha256);
  assert.ok(!JSON.stringify(messages).includes("NewTable"));
});

test("snapshot CLI rejects bad authority without reading source", async () => {
  const f = authorityFixture();
  for (const change of [
    authority => { authority.schemaVersion = 2; },
    authority => { authority.observedRefs.baseline.commitSha = "d".repeat(40); },
    authority => { authority.baselinePin.sha256 = "bad"; },
    authority => { authority.approvedManifest.rollback.sourceTag = "v1.2.1"; },
    authority => { authority.extra = "unclassified"; }
  ]) {
    const authority = JSON.parse(f.authorityBytes);
    change(authority);
    const bytes = Buffer.from(JSON.stringify(authority));
    let reads = 0;
    await assert.rejects(() => main(["authority", hash("sha256", bytes), "baseline", "candidate"], async path => {
      reads += 1;
      assert.equal(path, "authority");
      return bytes;
    }));
    assert.equal(reads, 1);
  }
  assert.throws(() => parseComparisonAuthority(f.authorityBytes, "0".repeat(64)), /digest mismatch/);
});

test("snapshot CLI emits no success result when payload bytes differ from authority", async () => {
  const f = authorityFixture();
  let emitted = false;
  await assert.rejects(() => main(["authority", f.authoritySha256, "baseline", "candidate"], async path => {
    if (path === "authority") return f.authorityBytes;
    return path === "baseline" ? f.baseline.bytes : Buffer.from("changed source");
  }, { log: () => { emitted = true; } }), /digest/);
  assert.equal(emitted, false);
  await assert.rejects(() => main([]), /Usage/);
});

function untaggedInputs(previous = [original], current = [original]) {
  const tagged = inputs(previous, current);
  const candidate = encoded(current, candidateSha, "untagged-workflow-source");
  const options = {
    purpose: "untagged-staging-source-comparison", workflowSha: candidateSha, candidateTreeSha: "d".repeat(40),
    bootstrapPolicySha256: "1".repeat(64), canonicalAuthoritySha256: "2".repeat(64), admissionBindingSha256: "3".repeat(64),
    baselinePin: tagged.baseline.pin, candidatePin: candidate.pin,
    observedRefs: { baseline: { tag: baselineTag, commitSha: baselineSha },
      candidate: { commitSha: candidateSha, treeSha: "d".repeat(40) }, workflow: { commitSha: candidateSha } },
    approvedManifest: tagged.options.approvedManifest
  };
  const authorityBytes = Buffer.from(JSON.stringify({ schemaVersion: 2, ...options }));
  return { baseline: tagged.baseline, candidate, options, authorityBytes, authoritySha256: hash("sha256", authorityBytes) };
}

test("untagged staging compares actual workflow source without a fictitious release tag", () => {
  const f = untaggedInputs([original], [original, entry(addedPath, forward('migrationBuilder.CreateTable(name: "NewTable");'))]);
  assert.equal(validateUntaggedStagingSnapshotAuthority(f.options), f.options);
  const result = validateUntaggedStagingSourceSnapshots(f.baseline.bytes, f.candidate.bytes, f.options);
  assert.deepEqual(result, { baselineCommitSha: baselineSha, candidateCommitSha: candidateSha,
    sourceFilesCompared: 1, addedMigrationSources: 1, changedSqlFiles: 0,
    comparisonMode: "source-only-staging", releaseAuthorized: false });
});

test("untagged staging CLI binds authority before source reads and emits source-only metadata", async () => {
  const f = untaggedInputs();
  const reads = [], messages = [];
  const data = { authority: f.authorityBytes, baseline: f.baseline.bytes, candidate: f.candidate.bytes };
  const result = await main(["source-only-staging", "authority", f.authoritySha256, "baseline", "candidate"], async (path, limit) => {
    reads.push([path, limit]); return data[path];
  }, { log: value => messages.push(JSON.parse(value)) });
  assert.equal(result.releaseAuthorized, false);
  assert.deepEqual(reads, [["authority", 1024 * 1024], ["baseline", maximumSnapshotBytes], ["candidate", maximumSnapshotBytes]]);
  assert.equal(messages[0].comparisonMode, "source-only-staging");
  assert.equal(messages[0].authoritySha256, f.authoritySha256);
  assert.equal(messages[0].candidateSnapshotSha256, f.candidate.pin.sha256);
  assert.ok(!JSON.stringify(messages).includes("AddColumn"));
});

test("untagged staging rejects purpose, schema, type, source and policy digest overrides", () => {
  const f = untaggedInputs();
  for (const change of [
    o => { o.purpose = "candidate-release"; },
    o => { o.version = "1.2.4"; },
    o => { o.workflowSha = "a".repeat(40); },
    o => { o.workflowSha = 1; },
    o => { o.candidateTreeSha = "bad"; },
    o => { o.candidatePin.commitSha = "a".repeat(40); },
    o => { o.candidatePin.tag = candidateTag; },
    ...["bootstrapPolicySha256", "canonicalAuthoritySha256", "admissionBindingSha256"].map(key => o => { o[key] = "bad"; })
  ]) {
    const options = structuredClone(f.options); change(options);
    assert.throws(() => validateUntaggedStagingSnapshotAuthority(options));
  }
  assert.throws(() => parseUntaggedStagingAuthority(f.authorityBytes, "0".repeat(64)), /digest mismatch/);
  const wrongVersion = Buffer.from(JSON.stringify({ schemaVersion: 1, ...f.options }));
  assert.throws(() => parseUntaggedStagingAuthority(wrongVersion, hash("sha256", wrongVersion)), /schema/);
});

test("untagged staging requires exact authenticated observation shape and approved rollback baseline", () => {
  const f = untaggedInputs();
  for (const change of [
    o => { o.observedRefs = null; },
    o => { delete o.observedRefs.workflow; },
    o => { o.observedRefs.workflow.commitSha = "a".repeat(40); },
    o => { o.observedRefs.candidate.commitSha = "a".repeat(40); },
    o => { o.observedRefs.candidate.treeSha = "a".repeat(40); },
    o => { o.observedRefs.candidate.tag = "pretend-tag"; },
    o => { o.observedRefs.baseline.commitSha = "a".repeat(40); },
    o => { o.observedRefs.baseline.tag = "v1.2.1"; },
    o => { o.approvedManifest.rollback.sourceTag = "v1.2.1"; },
    o => { o.baselinePin.sha256 = "bad"; }
  ]) {
    const options = structuredClone(f.options); change(options);
    assert.throws(() => validateUntaggedStagingSnapshotAuthority(options));
  }
});

test("untagged staging retains prior migration deletion, edit and mode rejection", () => {
  for (const current of [[], [entry(originalPath, "changed")], [entry(originalPath, originalSource(), { mode: "100755" })]]) {
    const f = untaggedInputs([original], current);
    assert.throws(() => validateUntaggedStagingSourceSnapshots(f.baseline.bytes, f.candidate.bytes, f.options), /must not/);
  }
});

function originalSource() {
  return Buffer.from(original.contentBase64, "base64");
}

test("untagged staging uses the same expand-only Up and added SQL guards", () => {
  for (const current of [[original, entry(addedPath, forward('migrationBuilder.DropTable("old");'))],
    [original, entry(sqlPath, "DROP TABLE old;")]]) {
    const f = untaggedInputs([original], current);
    assert.throws(() => validateUntaggedStagingSourceSnapshots(f.baseline.bytes, f.candidate.bytes, f.options), /expand-only/);
  }
  const sql = entry(sqlPath, "DROP TABLE historical;\nCREATE TABLE current(id integer);\n");
  const f = untaggedInputs([original, sql], [original, sql]);
  assert.equal(validateUntaggedStagingSourceSnapshots(f.baseline.bytes, f.candidate.bytes, f.options).changedSqlFiles, 0);
});

test("tagged and untagged authority contracts cannot substitute for each other", async () => {
  const f = untaggedInputs();
  const tagged = authorityFixture();
  assert.throws(() => parseComparisonAuthority(f.authorityBytes, f.authoritySha256), /schema/);
  assert.throws(() => parseUntaggedStagingAuthority(tagged.authorityBytes, tagged.authoritySha256), /schema/);
  await assert.rejects(() => main(["source-only-unknown", "authority", f.authoritySha256, "baseline", "candidate"]), /Usage/);
});

test("untagged CLI rejects bad authority before payload reads and mismatched payload before output", async () => {
  const f = untaggedInputs();
  let reads = 0, emitted = false;
  await assert.rejects(() => main(["source-only-staging", "authority", "0".repeat(64), "baseline", "candidate"], async path => {
    reads += 1; assert.equal(path, "authority"); return f.authorityBytes;
  }, { log: () => { emitted = true; } }), /digest/);
  assert.equal(reads, 1); assert.equal(emitted, false);
  for (const malformedPath of ["baseline", "candidate"]) {
    await assert.rejects(() => main(["source-only-staging", "authority", f.authoritySha256, "baseline", "candidate"], async path => {
      if (path === "authority") return f.authorityBytes;
      if (path === malformedPath) return Buffer.from("different bytes");
      return path === "baseline" ? f.baseline.bytes : f.candidate.bytes;
    }, { log: () => { emitted = true; } }), /digest/);
    assert.equal(emitted, false);
  }
});
