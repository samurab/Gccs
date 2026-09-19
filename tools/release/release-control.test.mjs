import assert from "node:assert/strict";
import { mkdtemp, readFile, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import test from "node:test";
import {
  createArtifactMetadata,
  validateApprovedRelease,
  validateCandidate,
  validateMigrationDiff,
  verifyReleaseBundle
} from "./release-control.mjs";

test("validates SemVer release candidates against the application version", () => {
  validateCandidate("v1.2.3-rc.4", "a".repeat(40), "1.2.3");
  assert.throws(() => validateCandidate("launch-candidate-2026-09-19-1", "a".repeat(40), "1.2.3"));
  assert.throws(() => validateCandidate("v1.2.4-rc.1", "a".repeat(40), "1.2.3"));
});

test("allows expand-only migrations and blocks destructive candidate operations", () => {
  assert.equal(validateMigrationDiff("+migrationBuilder.AddColumn<string>(name: \"Reference\");"), true);
  assert.throws(() => validateMigrationDiff("+migrationBuilder.DropColumn(name: \"Legacy\");"), /expand-only/);
  assert.throws(() => validateMigrationDiff("+migrationBuilder.Sql(\"DELETE FROM Evidence\");"), /expand-only/);
  assert.throws(() => validateMigrationDiff("+DROP TABLE gccs.legacy_evidence;"), /expand-only/);
  assert.equal(validateMigrationDiff("-migrationBuilder.DropTable(name: \"AlreadyRemoved\");"), true);
});

test("requires immutable digests and approvals for production", () => {
  const artifact = name => ({ name, sha256: "b".repeat(64) });
  const manifest = {
    schemaVersion: 2,
    status: "approved",
    version: "1.2.3",
    candidateTag: "v1.2.3-rc.2",
    releaseTag: "v1.2.3",
    commitSha: "a".repeat(40),
    approvedAt: "2026-09-19",
    approvalScope: "No-CUI pilot",
    dataPosture: "no-cui-only",
    staging: { environment: "staging", runId: 42, evidenceUrl: "https://github.com/samurab/Gccs/actions/runs/42" },
    artifacts: {
      mode: "immutable-promotion",
      releaseAssetTag: "v1.2.3-rc.2",
      api: artifact("fedril-api.zip"),
      web: artifact("fedril-web.zip"),
      migration: artifact("fedril-migrations.sql"),
      sbom: artifact("fedril-sbom.spdx.json"),
      metadata: artifact("release-metadata.json")
    },
    approvals: { engineering: true, security: true, product: true },
    rollback: { sourceTag: "v1.2.2" }
  };

  assert.equal(validateApprovedRelease(manifest, { production: true }), manifest);
  assert.throws(() => validateApprovedRelease({ ...manifest, approvals: { ...manifest.approvals, security: false } }, { production: true }));
});

test("generates deterministic artifact digests", async () => {
  const directory = await mkdtemp(join(tmpdir(), "fedril-release-"));
  const files = {};
  for (const key of ["api", "web", "migration", "sbom"]) {
    files[key] = join(directory, `${key}.bin`);
    await writeFile(files[key], key, "utf8");
  }
  const output = join(directory, "metadata.json");
  const metadata = await createArtifactMetadata({
    version: "1.2.3",
    candidateTag: "v1.2.3-rc.1",
    commitSha: "c".repeat(40),
    stagingRunId: "99",
    files,
    output
  });

  assert.equal(metadata.stagingRunId, 99);
  assert.match(metadata.artifacts.api.sha256, /^[0-9a-f]{64}$/);
  assert.deepEqual(JSON.parse(await readFile(output, "utf8")), metadata);
});

test("rejects a production bundle whose artifact bytes changed after staging", async () => {
  const directory = await mkdtemp(join(tmpdir(), "fedril-bundle-"));
  const files = {};
  for (const key of ["api", "web", "migration", "sbom"]) {
    files[key] = join(directory, `${key}.bin`);
    await writeFile(files[key], key, "utf8");
  }
  const metadataPath = join(directory, "metadata.json");
  const metadata = await createArtifactMetadata({
    version: "1.2.3",
    candidateTag: "v1.2.3-rc.1",
    commitSha: "c".repeat(40),
    stagingRunId: "99",
    files,
    output: metadataPath
  });
  const digest = async path => {
    const { createHash } = await import("node:crypto");
    return createHash("sha256").update(await readFile(path)).digest("hex");
  };
  const manifest = {
    schemaVersion: 2,
    status: "approved",
    version: "1.2.3",
    candidateTag: "v1.2.3-rc.1",
    releaseTag: "v1.2.3",
    commitSha: "c".repeat(40),
    approvedAt: "2026-09-19",
    approvalScope: "No-CUI pilot",
    dataPosture: "no-cui-only",
    staging: { environment: "staging", runId: 99, evidenceUrl: "https://github.com/samurab/Gccs/actions/runs/99" },
    artifacts: {
      mode: "immutable-promotion",
      releaseAssetTag: "v1.2.3-rc.1",
      api: metadata.artifacts.api,
      web: metadata.artifacts.web,
      migration: metadata.artifacts.migration,
      sbom: metadata.artifacts.sbom,
      metadata: { name: "metadata.json", sha256: await digest(metadataPath) }
    },
    approvals: { engineering: true, security: true, product: true },
    rollback: { sourceTag: "v1.2.2" }
  };

  await verifyReleaseBundle(manifest, directory);
  await writeFile(files.web, "changed", "utf8");
  await assert.rejects(verifyReleaseBundle(manifest, directory));
});
