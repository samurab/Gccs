import { createHash } from "node:crypto";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { readBoundedFile } from "./migration-source-snapshots-cli.mjs";
import {
  maximumSnapshotBytes, validateSourcePreflightAuthority, validateSourcePreflight
} from "./migration-source-snapshots.mjs";

const maximumAuthorityBytes = 1024 * 1024;
const hash = bytes => createHash("sha256").update(bytes).digest("hex");

export function parsePreflightAuthority(bytes, expectedSha256) {
  if (!Buffer.isBuffer(bytes) || bytes.length > maximumAuthorityBytes ||
      typeof expectedSha256 !== "string" || !/^[0-9a-f]{64}$/.test(expectedSha256) ||
      hash(bytes) !== expectedSha256) throw new Error("Source preflight authority digest mismatch");
  const authority = JSON.parse(new TextDecoder("utf-8", { fatal: true }).decode(bytes));
  if (authority?.schemaVersion !== 1 || authority.purpose !== "source-only-preflight" ||
      Object.keys(authority).sort().join("|") !== ["schemaVersion", "purpose", "baselinePin",
        "sourcePin", "observedBaselineRef", "approvedManifest"].sort().join("|")) {
    throw new Error("Invalid source preflight authority schema");
  }
  const { schemaVersion, purpose, ...options } = authority;
  return validateSourcePreflightAuthority(options);
}

export async function main(args, readInput = readBoundedFile, output = console) {
  if (args.length !== 4 || !args.every(value => typeof value === "string" && value.length)) {
    throw new Error("Usage: source-preflight-cli.mjs <authority.json> <authority-sha256> <baseline.json> <source.json>");
  }
  const [authorityPath, authoritySha256, baselinePath, sourcePath] = args;
  const options = parsePreflightAuthority(await readInput(authorityPath, maximumAuthorityBytes), authoritySha256);
  const baseline = await readInput(baselinePath, maximumSnapshotBytes);
  const source = await readInput(sourcePath, maximumSnapshotBytes);
  const result = validateSourcePreflight(baseline, source, options);
  output.log(JSON.stringify({ ...result, authoritySha256,
    baselineSnapshotSha256: options.baselinePin.sha256, sourceSnapshotSha256: options.sourcePin.sha256 }));
  return result;
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main(process.argv.slice(2)).catch(() => {
    console.error("Source-only preflight failed; no release authorized. Source content and diagnostics withheld.");
    process.exitCode = 1;
  });
}
