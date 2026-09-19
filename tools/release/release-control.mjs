import { createHash } from "node:crypto";
import { readFile, writeFile } from "node:fs/promises";
import { basename, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const repositoryRoot = resolve(fileURLToPath(new URL("../../", import.meta.url)));
const releaseVersionPath = resolve(repositoryRoot, "release/version.json");
const approvedReleasePath = resolve(repositoryRoot, "docs/release/approved-release.json");
const stableVersionPattern = /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$/;
const semVerPattern = /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)$/;
const shaPattern = /^[0-9a-f]{40}$/;
const digestPattern = /^[0-9a-f]{64}$/;
const destructiveMigrationPattern = /migrationBuilder\.(?:DropColumn|DropForeignKey|DropIndex|DropPrimaryKey|DropTable|AlterColumn|DeleteData|RenameColumn|RenameIndex|RenameTable|Sql)\s*\(/;
const destructiveSqlPattern = /^\+\s*(?:DROP\b|TRUNCATE\b|DELETE\s+FROM\b|UPDATE\b|ALTER\s+TABLE\b.*\bDROP\b)/i;

export function validateCandidate(candidateTag, commitSha, expectedVersion) {
  requiredString(commitSha, "commitSha");
  if (!shaPattern.test(commitSha)) {
    fail("commitSha must be a lowercase 40-character Git SHA");
  }
  if (!stableVersionPattern.test(expectedVersion)) {
    fail(`Invalid stable release version: ${expectedVersion}`);
  }
  const match = /^v((?:0|[1-9]\d*)\.(?:0|[1-9]\d*)\.(?:0|[1-9]\d*))-rc\.([1-9]\d*)$/.exec(candidateTag);
  if (!match || match[1] !== expectedVersion) {
    fail(`Candidate tag must match v${expectedVersion}-rc.N: ${candidateTag}`);
  }
}

export function validateMigrationDiff(diff) {
  const violations = diff
    .split("\n")
    .filter(line => line.startsWith("+") && !line.startsWith("+++"))
    .filter(line => destructiveMigrationPattern.test(line) || destructiveSqlPattern.test(line));

  if (violations.length > 0) {
    fail(
      "Candidate migrations must be expand-only. Move destructive schema/data changes to a separately reviewed " +
      `post-compatibility cleanup release. Blocked additions:\n${violations.join("\n")}`
    );
  }
  return true;
}

export function validateApprovedRelease(manifest, { production = false } = {}) {
  if (manifest.schemaVersion !== 2) fail("schemaVersion must be 2");
  if (manifest.status !== "legacy-import" && manifest.status !== "approved") {
    fail("status must be legacy-import or approved");
  }
  if (manifest.dataPosture !== "no-cui-only") fail("dataPosture must be no-cui-only");
  if (!shaPattern.test(requiredString(manifest.commitSha, "commitSha"))) {
    fail("commitSha must be a lowercase 40-character Git SHA");
  }
  requiredString(manifest.candidateTag, "candidateTag");
  const approvedAt = requiredString(manifest.approvedAt, "approvedAt");
  const approvedDate = new Date(`${approvedAt}T00:00:00Z`);
  if (!/^\d{4}-\d{2}-\d{2}$/.test(approvedAt) ||
      Number.isNaN(approvedDate.valueOf()) ||
      approvedDate.toISOString().slice(0, 10) !== approvedAt) {
    fail("approvedAt must be an ISO calendar date");
  }
  requiredString(manifest.approvalScope, "approvalScope");
  validateStaging(manifest.staging);
  validateApprovals(manifest.approvals);
  requiredString(manifest.rollback?.sourceTag, "rollback.sourceTag");

  if (manifest.status === "legacy-import") {
    if (!semVerPattern.test(requiredString(manifest.version, "version"))) {
      fail("A legacy-import version must be a SemVer prerelease");
    }
    if (manifest.releaseTag !== null) fail("A legacy import must not claim a final releaseTag");
    if (manifest.artifacts?.mode !== "legacy-source-rebuild") {
      fail("A legacy import must use legacy-source-rebuild artifact mode");
    }
    if (production) fail("Legacy imports are rollback evidence only and cannot use the immutable promotion workflow");
    return manifest;
  }

  const version = requiredString(manifest.version, "version");
  if (!stableVersionPattern.test(version)) fail(`Approved version must be stable SemVer: ${version}`);
  validateCandidate(manifest.candidateTag, manifest.commitSha, version);
  if (manifest.releaseTag !== `v${version}`) fail(`releaseTag must be v${version}`);
  if (manifest.artifacts?.mode !== "immutable-promotion") {
    fail("An approved release must use immutable-promotion artifact mode");
  }
  if (manifest.artifacts.releaseAssetTag !== manifest.candidateTag) {
    fail("artifacts.releaseAssetTag must equal candidateTag");
  }
  for (const key of ["api", "web", "migration", "sbom", "metadata"]) {
    validateArtifact(manifest.artifacts[key], `artifacts.${key}`);
  }
  if (!Object.values(manifest.approvals).every(value => value === true)) {
    fail("All required approvals must be true for an approved release");
  }
  return manifest;
}

export async function checkRepositoryVersions() {
  const releaseVersion = JSON.parse(await readFile(releaseVersionPath, "utf8")).version;
  if (!stableVersionPattern.test(releaseVersion)) fail(`Invalid release/version.json version: ${releaseVersion}`);

  const webPackage = JSON.parse(await readFile(resolve(repositoryRoot, "apps/web/package.json"), "utf8"));
  if (webPackage.version !== releaseVersion) {
    fail(`apps/web/package.json version ${webPackage.version} does not match ${releaseVersion}`);
  }

  const rootPackage = JSON.parse(await readFile(resolve(repositoryRoot, "package.json"), "utf8"));
  if (rootPackage.version !== releaseVersion) {
    fail(`package.json version ${rootPackage.version} does not match ${releaseVersion}`);
  }

  const buildProps = await readFile(resolve(repositoryRoot, "Directory.Build.props"), "utf8");
  const versionMatch = /<Version>([^<]+)<\/Version>/.exec(buildProps);
  if (versionMatch?.[1] !== releaseVersion) {
    fail(`Directory.Build.props version ${versionMatch?.[1] ?? "missing"} does not match ${releaseVersion}`);
  }

  validateApprovedRelease(JSON.parse(await readFile(approvedReleasePath, "utf8")));
  return releaseVersion;
}

export async function createArtifactMetadata({ version, candidateTag, commitSha, stagingRunId, files, output }) {
  validateCandidate(candidateTag, commitSha, version);
  const parsedRunId = Number.parseInt(stagingRunId, 10);
  if (!Number.isSafeInteger(parsedRunId) || parsedRunId < 1) fail("stagingRunId must be a positive integer");

  const artifacts = {};
  for (const [key, path] of Object.entries(files)) {
    const contents = await readFile(path);
    artifacts[key] = {
      name: basename(path),
      sha256: createHash("sha256").update(contents).digest("hex")
    };
  }

  const metadata = {
    schemaVersion: 1,
    version,
    candidateTag,
    commitSha,
    stagingRunId: parsedRunId,
    dataPosture: "no-cui-only",
    artifacts
  };
  await writeFile(output, `${JSON.stringify(metadata, null, 2)}\n`, "utf8");
  return metadata;
}

export async function verifyReleaseBundle(manifest, directory) {
  validateApprovedRelease(manifest, { production: true });
  const verified = {};
  for (const key of ["api", "web", "migration", "sbom", "metadata"]) {
    const artifact = manifest.artifacts[key];
    const contents = await readFile(resolve(directory, artifact.name));
    const actual = createHash("sha256").update(contents).digest("hex");
    if (actual !== artifact.sha256) fail(`${key} digest does not match the approved manifest`);
    verified[key] = actual;
  }

  const metadata = JSON.parse(await readFile(resolve(directory, manifest.artifacts.metadata.name), "utf8"));
  if (metadata.version !== manifest.version) fail("Artifact metadata version does not match the approved manifest");
  if (metadata.candidateTag !== manifest.candidateTag) fail("Artifact metadata candidateTag does not match the approved manifest");
  if (metadata.commitSha !== manifest.commitSha) fail("Artifact metadata commitSha does not match the approved manifest");
  if (metadata.stagingRunId !== manifest.staging.runId) fail("Artifact metadata stagingRunId does not match the approved manifest");
  if (metadata.dataPosture !== manifest.dataPosture) fail("Artifact metadata dataPosture does not match the approved manifest");
  for (const key of ["api", "web", "migration", "sbom"]) {
    if (metadata.artifacts?.[key]?.name !== manifest.artifacts[key].name ||
        metadata.artifacts?.[key]?.sha256 !== manifest.artifacts[key].sha256) {
      fail(`Artifact metadata ${key} identity does not match the approved manifest`);
    }
  }
  return verified;
}

function validateStaging(staging) {
  if (staging?.environment !== "staging") fail("staging.environment must be staging");
  if (!Number.isSafeInteger(staging?.runId) || staging.runId < 1) fail("staging.runId must be a positive integer");
  const evidenceUrl = requiredString(staging.evidenceUrl, "staging.evidenceUrl");
  if (!/^https:\/\/github\.com\/[^/]+\/[^/]+\/actions\/runs\/\d+$/.test(evidenceUrl)) {
    fail("staging.evidenceUrl must identify a GitHub Actions run");
  }
}

function validateApprovals(approvals) {
  for (const key of ["engineering", "security", "product"]) {
    if (typeof approvals?.[key] !== "boolean") fail(`approvals.${key} must be boolean`);
  }
}

function validateArtifact(artifact, label) {
  const name = requiredString(artifact?.name, `${label}.name`);
  if (!/^[0-9A-Za-z][0-9A-Za-z._-]*$/.test(name)) fail(`${label}.name must be a safe basename`);
  if (!digestPattern.test(artifact?.sha256 ?? "")) fail(`${label}.sha256 must be a lowercase SHA-256 digest`);
}

function requiredString(value, label) {
  if (typeof value !== "string" || value.trim().length === 0) fail(`${label} must be a non-empty string`);
  return value.trim();
}

function fail(message) {
  throw new Error(`Release control validation failed: ${message}`);
}

async function readStandardInput() {
  let input = "";
  process.stdin.setEncoding("utf8");
  for await (const chunk of process.stdin) input += chunk;
  return input;
}

async function main() {
  const [command, ...args] = process.argv.slice(2);
  if (command === "check") {
    const version = await checkRepositoryVersions();
    console.log(`Release controls are synchronized for ${version}.`);
    return;
  }
  if (command === "candidate") {
    const version = args[2] ?? JSON.parse(await readFile(releaseVersionPath, "utf8")).version;
    validateCandidate(args[0], args[1], version);
    console.log(`Candidate ${args[0]} is valid for ${version}.`);
    return;
  }
  if (command === "production") {
    const manifest = validateApprovedRelease(JSON.parse(await readFile(args[0] ?? approvedReleasePath, "utf8")), { production: true });
    if (args[1] !== manifest.releaseTag) fail(`Requested release tag ${args[1]} does not match ${manifest.releaseTag}`);
    console.log(`Approved immutable release ${manifest.releaseTag} is valid.`);
    return;
  }
  if (command === "metadata") {
    const metadata = await createArtifactMetadata({
      version: process.env.RELEASE_VERSION,
      candidateTag: process.env.CANDIDATE_TAG,
      commitSha: process.env.COMMIT_SHA,
      stagingRunId: process.env.STAGING_RUN_ID,
      files: {
        api: requiredString(process.env.API_ARTIFACT, "API_ARTIFACT"),
        web: requiredString(process.env.WEB_ARTIFACT, "WEB_ARTIFACT"),
        migration: requiredString(process.env.MIGRATION_ARTIFACT, "MIGRATION_ARTIFACT"),
        sbom: requiredString(process.env.SBOM_ARTIFACT, "SBOM_ARTIFACT")
      },
      output: requiredString(process.env.METADATA_OUTPUT, "METADATA_OUTPUT")
    });
    console.log(JSON.stringify(metadata));
    return;
  }
  if (command === "bundle") {
    const manifest = JSON.parse(await readFile(args[0] ?? approvedReleasePath, "utf8"));
    await verifyReleaseBundle(manifest, requiredString(args[1], "bundle directory"));
    console.log(`Release bundle matches ${manifest.releaseTag}.`);
    return;
  }
  if (command === "migration-diff") {
    validateMigrationDiff(await readStandardInput());
    console.log("Candidate migration diff is expand-only.");
    return;
  }
  console.error("Usage: release-control.mjs check|candidate <tag> <sha>|production <manifest> <release-tag>|metadata|bundle <manifest> <directory>|migration-diff");
  process.exitCode = 2;
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main().catch(error => {
    console.error(error.message);
    process.exitCode = 1;
  });
}
