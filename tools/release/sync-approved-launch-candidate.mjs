import { readFile, writeFile } from "node:fs/promises";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";

const repositoryRoot = resolve(fileURLToPath(new URL("../../", import.meta.url)));
const manifestPath = "docs/release/approved-launch-candidate.json";
const mode = process.argv[2];

if (mode !== "--write" && mode !== "--check") {
  console.error("Usage: node tools/release/sync-approved-launch-candidate.mjs --write|--check");
  process.exit(2);
}

const manifest = JSON.parse(await readFile(resolve(repositoryRoot, manifestPath), "utf8"));
const tag = requiredString(manifest.approvedLaunchCandidateTag, "approvedLaunchCandidateTag");
const sha = requiredString(manifest.approvedCommitSha, "approvedCommitSha");
const approvedDate = requiredString(manifest.approvedDate, "approvedDate");
const dataPosture = requiredString(manifest.dataPosture, "dataPosture");
const tagMatch = /^launch-candidate-(\d{4}-\d{2}-\d{2})-([1-9]\d*)$/.exec(tag);

if (!tagMatch) {
  fail(`Invalid approvedLaunchCandidateTag: ${tag}`);
}

if (!/^[0-9a-f]{40}$/.test(sha)) {
  fail(`Invalid approvedCommitSha: ${sha}`);
}

if (!isIsoDate(approvedDate) || tagMatch[1] !== approvedDate) {
  fail(`approvedDate must be a valid ISO date matching the tag date: ${approvedDate}`);
}

if (dataPosture !== "no-cui-only") {
  fail(`Unsupported dataPosture: ${dataPosture}`);
}

const files = [
  syncFile(".github/workflows/production.yml", [
    rule("workflow dispatch default", /^(\s*default:\s*)launch-candidate-\d{4}-\d{2}-\d{2}-\d+\s*$/m, (match) => `${match[1]}${tag}`)
  ]),
  syncFile("docs/production-deployment-runbook.md", [
    rule("approved tag table row", /^(\| Approved launch candidate tag \| `)[^`]+(` \|)$/m, (match) => `${match[1]}${tag}${match[2]}`),
    rule("approved commit table row", /^(\| Launch candidate commit \| `)[0-9a-f]+(` \|)$/m, (match) => `${match[1]}${sha}${match[2]}`),
    rule("website dispatch tag example", /^launch-candidate-\d{4}-\d{2}-\d{2}-\d+$/m, () => tag),
    rule("CLI dispatch tag example", /^(\s+-f launch_candidate_tag=)launch-candidate-\d{4}-\d{2}-\d{2}-\d+$/m, (match) => `${match[1]}${tag}`)
  ]),
  syncFile("docs/production-readiness-checklist.md", [
    rule("launch candidate checklist status", /(\| Launch candidate tag \|[^\n]*\| Created as `)[^`]+(` \|)/, (match) => `${match[1]}${tag}${match[2]}`),
    rule("production deployment checklist status", /(Candidate `)([^`]+)(` )(is awaiting protected production workflow execution|deployed successfully in protected production workflow run `\d+`; evidence artifact `\d+` is attached)/, (match) => `${match[1]}${tag}${match[3]}${match[2] === tag ? match[4] : "is awaiting protected production workflow execution"}`)
  ]),
  syncFile("docs/production-readiness-launch-candidate-tag.md", [
    rule("tag date", /^(Tag date: )[0-9]{4}-[0-9]{2}-[0-9]{2}(\.)$/m, (match) => `${match[1]}${approvedDate}${match[2]}`),
    rule("launch candidate tag", /^(Launch candidate tag: `)[^`]+(`\.)$/m, (match) => `${match[1]}${tag}${match[2]}`),
    rule("tagged commit", /^(Tagged commit: `)[0-9a-f]+(`\.)$/m, (match) => `${match[1]}${sha}${match[2]}`),
    rule("tag command", /^(git tag )launch-candidate-\d{4}-\d{2}-\d{2}-\d+( )[0-9a-f]+$/m, (match) => `${match[1]}${tag}${match[2]}${sha}`)
  ]),
  syncFile("docs/production-readiness-launch-closure-evidence.md", [
    rule("launch closure candidate row", /(Approved launch candidate manifest `docs\/release\/approved-launch-candidate\.json` records tag `)[^`]+(` at commit `)[0-9a-f]+(`)/, (match) => `${match[1]}${tag}${match[2]}${sha}${match[3]}`)
  ]),
  syncFile("docs/production-readiness-launch-gap-decisions.md", [
    rule("launch gap decision", /(PR-6\.2 created launch candidate tag `)[^`]+(`)/, (match) => `${match[1]}${tag}${match[2]}`)
  ]),
  syncFile("docs/production-readiness-production-deployment-evidence.md", [
    rule("current candidate execution status", /^(Current candidate execution status: `)([^`]+)(` )(is approved but not yet deployed\.|deployed successfully in production workflow run `\d+`\.)$/m, (match) => `${match[1]}${tag}${match[3]}${match[2] === tag ? match[4] : "is approved but not yet deployed."}`),
    rule("latest evidence date", /^(Latest evidence date: )[0-9]{4}-[0-9]{2}-[0-9]{2}(\. Historical evidence dates are retained below\.)$/m, (match) => `${match[1]}${approvedDate}${match[2]}`),
    rule("deployment evidence candidate", /^(Approved launch candidate tag: `)[^`]+(`\.)$/m, (match) => `${match[1]}${tag}${match[2]}`),
    rule("deployment precondition candidate", /(Manifest `docs\/release\/approved-launch-candidate\.json` approves tag `)[^`]+(` at `)[0-9a-f]+(`)/, (match) => `${match[1]}${tag}${match[2]}${sha}${match[3]}`),
    rule("CI/CD candidate status", /(Current candidate `)([^`]+)(` )(still requires protected production workflow execution after this launch-candidate gate merges\.|completed protected production workflow execution in run `\d+`\.)/, (match) => `${match[1]}${tag}${match[3]}${match[2] === tag ? match[4] : "still requires protected production workflow execution after this launch-candidate gate merges."}`),
    rule("production secrets candidate status", /(Current candidate `)([^`]+)(` )(still requires protected production workflow execution\.|resolved the required production environment secrets in run `\d+` without exposing their values\.)/, (match) => `${match[1]}${tag}${match[3]}${match[2] === tag ? match[4] : "still requires protected production workflow execution."}`)
  ]),
  syncFile("docs/production-readiness-release-notes.md", [
    rule("release note candidate", /^(Launch candidate tag: `)[^`]+(`\.)$/m, (match) => `${match[1]}${tag}${match[2]}`)
  ])
];

const results = await Promise.all(files);
const changedFiles = results.filter((result) => result.changed).map((result) => result.path);

if (mode === "--check" && changedFiles.length > 0) {
  console.error("Approved launch candidate artifacts are out of sync:");
  changedFiles.forEach((path) => console.error(`- ${path}`));
  console.error("Run: npm run sync:launch-candidate");
  process.exit(1);
}

if (changedFiles.length === 0) {
  console.log(`Approved launch candidate artifacts already match ${tag}.`);
} else {
  console.log(`Synchronized ${changedFiles.length} approved launch candidate artifact(s) to ${tag}.`);
}

function requiredString(value, propertyName) {
  if (typeof value !== "string" || value.trim().length === 0) {
    fail(`${propertyName} must be a non-empty string`);
  }

  return value.trim();
}

function isIsoDate(value) {
  const date = new Date(`${value}T00:00:00Z`);
  return !Number.isNaN(date.valueOf()) && date.toISOString().slice(0, 10) === value;
}

function rule(label, pattern, replacement) {
  return { label, pattern, replacement };
}

async function syncFile(path, rules) {
  const absolutePath = resolve(repositoryRoot, path);
  const original = await readFile(absolutePath, "utf8");
  let synchronized = original;

  for (const currentRule of rules) {
    const flags = currentRule.pattern.flags.includes("g")
      ? currentRule.pattern.flags
      : `${currentRule.pattern.flags}g`;
    const globalPattern = new RegExp(currentRule.pattern.source, flags);
    const matches = [...synchronized.matchAll(globalPattern)];

    if (matches.length !== 1) {
      fail(`${path}: expected one ${currentRule.label} field, found ${matches.length}`);
    }

    synchronized = synchronized.replace(globalPattern, (...args) => currentRule.replacement(args));
  }

  const changed = synchronized !== original;
  if (mode === "--write" && changed) {
    await writeFile(absolutePath, synchronized, "utf8");
  }

  return { path, changed };
}

function fail(message) {
  console.error(`Launch candidate synchronization failed: ${message}`);
  process.exit(1);
}
