import assert from "node:assert/strict";
import { execFileSync, spawnSync } from "node:child_process";
import { copyFileSync, mkdirSync, mkdtempSync, rmSync, unlinkSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

import {
  hasProhibitedDatabaseSignature,
  isProhibitedArtifactPath,
} from "./check-sensitive-artifacts.mjs";

const sourceScript = fileURLToPath(new URL("./check-sensitive-artifacts.mjs", import.meta.url));

function git(repositoryRoot, args) {
  return execFileSync("git", args, { cwd: repositoryRoot, encoding: "utf8" }).trim();
}

function createRepository(t) {
  const repositoryRoot = mkdtempSync(path.join(tmpdir(), "fedril-artifact-policy-"));
  t.after(() => rmSync(repositoryRoot, { recursive: true, force: true }));
  const scriptDirectory = path.join(repositoryRoot, "tools/security");
  mkdirSync(scriptDirectory, { recursive: true });
  copyFileSync(sourceScript, path.join(scriptDirectory, "check-sensitive-artifacts.mjs"));
  git(repositoryRoot, ["init", "-q", "-b", "main"]);
  git(repositoryRoot, ["config", "user.name", "Security Test"]);
  git(repositoryRoot, ["config", "user.email", "security-test@example.invalid"]);
  git(repositoryRoot, ["config", "commit.gpgsign", "false"]);
  git(repositoryRoot, ["add", "tools"]);
  git(repositoryRoot, ["commit", "-qm", "baseline"]);
  return repositoryRoot;
}

test("rejects database backup directories and native backup suffixes", () => {
  const prohibited = [
    ".codex/db-backups/sample.bin",
    ".CODEX/DB-BACKUPS/sample.bin",
    "nested/.codex/db-backups/sample.bin",
    "sample.dump",
    "sample.BACKUP",
    "sample.bak",
    "sample.sqlite",
    "sample.sqlite3",
  ];

  for (const candidate of prohibited) {
    assert.equal(isProhibitedArtifactPath(candidate), true, candidate);
  }
});

test("preserves legitimate SQL and sanitized evidence formats", () => {
  const allowed = [
    "infra/database/development-schema.sql",
    "infra/database/predeploy-compliance-task-search-index.sql",
    "output/production-readiness/summary.json",
    "docs/soc2/register.csv",
  ];

  for (const candidate of allowed) {
    assert.equal(isProhibitedArtifactPath(candidate), false, candidate);
  }
});

test("detects PostgreSQL custom and SQLite files even when renamed", () => {
  assert.equal(hasProhibitedDatabaseSignature(Buffer.from("PGDMP\u0001\u000e\u0000archive")), true);
  assert.equal(hasProhibitedDatabaseSignature(Buffer.from("SQLite format 3\u0000data")), true);
  assert.equal(hasProhibitedDatabaseSignature(Buffer.from("sanitized evidence")), false);
});

test("CLI rejects an artifact added and deleted in intermediate commits", (t) => {
  const repositoryRoot = createRepository(t);
  const baseSha = git(repositoryRoot, ["rev-parse", "HEAD"]);
  const prohibitedPath = path.join(repositoryRoot, "transient.dump");
  writeFileSync(prohibitedPath, "synthetic test content");
  git(repositoryRoot, ["add", "transient.dump"]);
  git(repositoryRoot, ["commit", "-qm", "add prohibited artifact"]);
  unlinkSync(prohibitedPath);
  git(repositoryRoot, ["add", "-u"]);
  git(repositoryRoot, ["commit", "-qm", "delete prohibited artifact"]);

  const result = spawnSync(
    process.execPath,
    [path.join(repositoryRoot, "tools/security/check-sensitive-artifacts.mjs"), "--changed-since", baseSha],
    { cwd: repositoryRoot, encoding: "utf8" },
  );
  assert.equal(result.status, 1);
  assert.match(result.stderr, /transient\.dump/);
  assert.doesNotMatch(result.stderr, /synthetic test content/);
});

test("CLI rejects an extensionless PostgreSQL custom-format signature", (t) => {
  const repositoryRoot = createRepository(t);
  const baseSha = git(repositoryRoot, ["rev-parse", "HEAD"]);
  writeFileSync(path.join(repositoryRoot, "archive.bin"), Buffer.from("PGDMP synthetic test content"));
  git(repositoryRoot, ["add", "archive.bin"]);
  git(repositoryRoot, ["commit", "-qm", "add disguised database artifact"]);

  const result = spawnSync(
    process.execPath,
    [path.join(repositoryRoot, "tools/security/check-sensitive-artifacts.mjs"), "--changed-since", baseSha],
    { cwd: repositoryRoot, encoding: "utf8" },
  );
  assert.equal(result.status, 1);
  assert.match(result.stderr, /archive\.bin/);
  assert.doesNotMatch(result.stderr, /synthetic test content/);
});

test("CLI scans reachable legacy history for an initial all-zero ref base", (t) => {
  const repositoryRoot = createRepository(t);
  writeFileSync(path.join(repositoryRoot, "initial.dump"), "synthetic test content");
  git(repositoryRoot, ["add", "initial.dump"]);
  git(repositoryRoot, ["commit", "-qm", "add prohibited artifact"]);
  writeFileSync(path.join(repositoryRoot, "clean-followup.txt"), "ordinary content\n");
  git(repositoryRoot, ["add", "clean-followup.txt"]);
  git(repositoryRoot, ["commit", "-qm", "add clean followup"]);

  const result = spawnSync(
    process.execPath,
    [
      path.join(repositoryRoot, "tools/security/check-sensitive-artifacts.mjs"),
      "--changed-since",
      "0".repeat(40),
    ],
    { cwd: repositoryRoot, encoding: "utf8" },
  );
  assert.equal(result.status, 1);
  assert.match(result.stderr, /initial\.dump/);
  assert.doesNotMatch(result.stderr, /synthetic test content/);
});

test("CLI fails closed when the comparison base is invalid", (t) => {
  const repositoryRoot = createRepository(t);
  const result = spawnSync(
    process.execPath,
    [
      path.join(repositoryRoot, "tools/security/check-sensitive-artifacts.mjs"),
      "--changed-since",
      "f".repeat(40),
    ],
    { cwd: repositoryRoot, encoding: "utf8" },
  );
  assert.equal(result.status, 2);
  assert.match(result.stderr, /could not complete/i);
});

test("CLI preserves ordinary SQL and sanitized evidence commits", (t) => {
  const repositoryRoot = createRepository(t);
  const baseSha = git(repositoryRoot, ["rev-parse", "HEAD"]);
  mkdirSync(path.join(repositoryRoot, "infra/database"), { recursive: true });
  mkdirSync(path.join(repositoryRoot, "docs/soc2"), { recursive: true });
  writeFileSync(path.join(repositoryRoot, "infra/database/schema.sql"), "select 1;\n");
  writeFileSync(path.join(repositoryRoot, "docs/soc2/register.json"), "{}\n");
  writeFileSync(path.join(repositoryRoot, "docs/soc2/register.csv"), "control,status\nCC1.1,draft\n");
  git(repositoryRoot, [
    "add",
    "infra/database/schema.sql",
    "docs/soc2/register.json",
    "docs/soc2/register.csv",
  ]);
  git(repositoryRoot, ["commit", "-qm", "add legitimate artifacts"]);

  const result = spawnSync(
    process.execPath,
    [path.join(repositoryRoot, "tools/security/check-sensitive-artifacts.mjs"), "--changed-since", baseSha],
    { cwd: repositoryRoot, encoding: "utf8" },
  );
  assert.equal(result.status, 0, result.stderr);
});

test("CLI rejects an uppercase prohibited suffix introduced by rename", (t) => {
  const repositoryRoot = createRepository(t);
  writeFileSync(path.join(repositoryRoot, "ordinary.txt"), "synthetic test content");
  git(repositoryRoot, ["add", "ordinary.txt"]);
  git(repositoryRoot, ["commit", "-qm", "add ordinary file"]);
  const baseSha = git(repositoryRoot, ["rev-parse", "HEAD"]);
  git(repositoryRoot, ["mv", "ordinary.txt", "renamed.DUMP"]);
  git(repositoryRoot, ["commit", "-qm", "rename prohibited artifact"]);

  const result = spawnSync(
    process.execPath,
    [path.join(repositoryRoot, "tools/security/check-sensitive-artifacts.mjs"), "--changed-since", baseSha],
    { cwd: repositoryRoot, encoding: "utf8" },
  );
  assert.equal(result.status, 1);
  assert.match(result.stderr, /renamed\.DUMP/);
});

test("CLI rejects a safe file modified into a native database artifact", (t) => {
  const repositoryRoot = createRepository(t);
  writeFileSync(path.join(repositoryRoot, "archive.bin"), "ordinary content");
  git(repositoryRoot, ["add", "archive.bin"]);
  git(repositoryRoot, ["commit", "-qm", "add ordinary archive"]);
  const baseSha = git(repositoryRoot, ["rev-parse", "HEAD"]);
  writeFileSync(path.join(repositoryRoot, "archive.bin"), Buffer.from("PGDMP synthetic test content"));
  git(repositoryRoot, ["add", "archive.bin"]);
  git(repositoryRoot, ["commit", "-qm", "modify archive into database artifact"]);

  const result = spawnSync(
    process.execPath,
    [path.join(repositoryRoot, "tools/security/check-sensitive-artifacts.mjs"), "--changed-since", baseSha],
    { cwd: repositoryRoot, encoding: "utf8" },
  );
  assert.equal(result.status, 1);
  assert.match(result.stderr, /archive\.bin/);
  assert.doesNotMatch(result.stderr, /synthetic test content/);
});

test("CLI treats Git pathspec-magic filenames literally", (t) => {
  const repositoryRoot = createRepository(t);
  const baseSha = git(repositoryRoot, ["rev-parse", "HEAD"]);
  const adversarialName = ":(exclude)archive.bin";
  writeFileSync(path.join(repositoryRoot, adversarialName), Buffer.from("PGDMP synthetic test content"));
  git(repositoryRoot, ["add", "--", `:(literal)${adversarialName}`]);
  git(repositoryRoot, ["commit", "-qm", "add adversarial filename"]);

  const result = spawnSync(
    process.execPath,
    [path.join(repositoryRoot, "tools/security/check-sensitive-artifacts.mjs"), "--changed-since", baseSha],
    { cwd: repositoryRoot, encoding: "utf8" },
  );
  assert.equal(result.status, 1);
  assert.match(result.stderr, /:\(exclude\)archive\.bin/);
  assert.doesNotMatch(result.stderr, /synthetic test content/);
});

test("CLI current-tree mode rejects a tracked prohibited artifact", (t) => {
  const repositoryRoot = createRepository(t);
  writeFileSync(path.join(repositoryRoot, "current.sqlite3"), "synthetic test content");
  git(repositoryRoot, ["add", "current.sqlite3"]);
  git(repositoryRoot, ["commit", "-qm", "add current prohibited artifact"]);

  const result = spawnSync(
    process.execPath,
    [path.join(repositoryRoot, "tools/security/check-sensitive-artifacts.mjs"), "--all-tracked"],
    { cwd: repositoryRoot, encoding: "utf8" },
  );
  assert.equal(result.status, 1);
  assert.match(result.stderr, /current\.sqlite3/);
  assert.doesNotMatch(result.stderr, /synthetic test content/);
});

test("CLI staged mode rejects a prohibited artifact without reading unstaged content", (t) => {
  const repositoryRoot = createRepository(t);
  const stagedPath = path.join(repositoryRoot, "staged.dump");
  writeFileSync(stagedPath, "staged synthetic content");
  git(repositoryRoot, ["add", "staged.dump"]);
  writeFileSync(stagedPath, "different unstaged content");

  const result = spawnSync(
    process.execPath,
    [path.join(repositoryRoot, "tools/security/check-sensitive-artifacts.mjs"), "--staged"],
    { cwd: repositoryRoot, encoding: "utf8" },
  );
  assert.equal(result.status, 1);
  assert.match(result.stderr, /staged\.dump/);
  assert.doesNotMatch(result.stderr, /synthetic content|unstaged content/);
});

test("CLI staged mode permits legitimate source and sanitized evidence", (t) => {
  const repositoryRoot = createRepository(t);
  mkdirSync(path.join(repositoryRoot, "infra/database"), { recursive: true });
  mkdirSync(path.join(repositoryRoot, "docs/soc2"), { recursive: true });
  writeFileSync(path.join(repositoryRoot, "infra/database/schema.sql"), "select 1;\n");
  writeFileSync(path.join(repositoryRoot, "docs/soc2/register.json"), "{}\n");
  git(repositoryRoot, ["add", "infra/database/schema.sql", "docs/soc2/register.json"]);

  const result = spawnSync(
    process.execPath,
    [path.join(repositoryRoot, "tools/security/check-sensitive-artifacts.mjs"), "--staged"],
    { cwd: repositoryRoot, encoding: "utf8" },
  );
  assert.equal(result.status, 0, result.stderr);
});

test("CLI reports usage errors distinctly", (t) => {
  const repositoryRoot = createRepository(t);
  const result = spawnSync(
    process.execPath,
    [path.join(repositoryRoot, "tools/security/check-sensitive-artifacts.mjs")],
    { cwd: repositoryRoot, encoding: "utf8" },
  );
  assert.equal(result.status, 64);
  assert.match(result.stderr, /usage:/i);
});
