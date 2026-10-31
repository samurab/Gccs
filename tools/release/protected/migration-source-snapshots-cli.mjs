import { createHash } from "node:crypto";
import { constants } from "node:fs";
import { open } from "node:fs/promises";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";
import {
  maximumSnapshotBytes, validateMigrationSnapshotAuthority, validateMigrationSourceSnapshots,
  validateUntaggedStagingSnapshotAuthority, validateUntaggedStagingSourceSnapshots
} from "./migration-source-snapshots.mjs";

const maximumAuthorityBytes = 1024 * 1024;
const hash = bytes => createHash("sha256").update(bytes).digest("hex");

export async function readBoundedFile(path, limit) {
  const file = await open(path, constants.O_RDONLY | constants.O_NOFOLLOW | constants.O_NONBLOCK);
  try {
    const stat = await file.stat();
    if (!stat.isFile() || !Number.isSafeInteger(stat.size) || stat.size > limit) {
      throw new Error("Input must be a bounded regular file");
    }
    const bytes = Buffer.alloc(stat.size + 1);
    let length = 0;
    while (length < bytes.length) {
      const { bytesRead } = await file.read(bytes, length, bytes.length - length, null);
      if (!bytesRead) break;
      length += bytesRead;
    }
    if (length !== stat.size) throw new Error("Input size changed while reading");
    return bytes.subarray(0, length);
  } finally {
    await file.close();
  }
}

export function parseComparisonAuthority(bytes, expectedSha256) {
  if (!Buffer.isBuffer(bytes) || bytes.length > maximumAuthorityBytes ||
      typeof expectedSha256 !== "string" || !/^[0-9a-f]{64}$/.test(expectedSha256) ||
      hash(bytes) !== expectedSha256) throw new Error("Comparison authority digest mismatch");
  const authority = JSON.parse(new TextDecoder("utf-8", { fatal: true }).decode(bytes));
  if (authority?.schemaVersion !== 1 || Object.keys(authority).sort().join("|") !==
      ["schemaVersion", "baselinePin", "candidatePin", "observedRefs", "approvedManifest", "version"].sort().join("|")) {
    throw new Error("Invalid comparison authority schema");
  }
  const { schemaVersion, ...options } = authority;
  return validateMigrationSnapshotAuthority(options);
}

export function parseUntaggedStagingAuthority(bytes, expectedSha256) {
  if (!Buffer.isBuffer(bytes) || bytes.length > maximumAuthorityBytes ||
      typeof expectedSha256 !== "string" || !/^[0-9a-f]{64}$/.test(expectedSha256) ||
      hash(bytes) !== expectedSha256) throw new Error("Comparison authority digest mismatch");
  const authority = JSON.parse(new TextDecoder("utf-8", { fatal: true }).decode(bytes));
  if (authority?.schemaVersion !== 2) throw new Error("Invalid untagged staging authority schema");
  const { schemaVersion, ...options } = authority;
  return validateUntaggedStagingSnapshotAuthority(options);
}

export async function main(args, readInput = readBoundedFile, output = console) {
  const sourceOnly = args.length === 5 && args[0] === "source-only-staging";
  if ((!sourceOnly && args.length !== 4) || !args.every(value => typeof value === "string" && value.length)) {
    throw new Error("Usage: migration-source-snapshots-cli.mjs [source-only-staging] <authority.json> <authority-sha256> <baseline.json> <candidate.json>");
  }
  const [authorityPath, authoritySha256, baselinePath, candidatePath] = sourceOnly ? args.slice(1) : args;
  const authorityBytes = await readInput(authorityPath, maximumAuthorityBytes);
  const options = sourceOnly ? parseUntaggedStagingAuthority(authorityBytes, authoritySha256)
    : parseComparisonAuthority(authorityBytes, authoritySha256);
  const baseline = await readInput(baselinePath, maximumSnapshotBytes);
  const candidate = await readInput(candidatePath, maximumSnapshotBytes);
  const result = sourceOnly ? validateUntaggedStagingSourceSnapshots(baseline, candidate, options)
    : validateMigrationSourceSnapshots(baseline, candidate, options);
  output.log(JSON.stringify({ ...result, authoritySha256,
    baselineSnapshotSha256: options.baselinePin.sha256, candidateSnapshotSha256: options.candidatePin.sha256 }));
  return result;
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main(process.argv.slice(2)).catch(() => {
    console.error("Migration snapshot comparison failed. Source content and diagnostics withheld.");
    process.exitCode = 1;
  });
}
