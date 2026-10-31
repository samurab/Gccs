#!/usr/bin/env node

import { execFileSync, spawn } from "node:child_process";
import { realpathSync } from "node:fs";
import { lstat, open } from "node:fs/promises";
import path from "node:path";
import process from "node:process";
import { fileURLToPath } from "node:url";

const forbiddenSuffixes = [".dump", ".backup", ".bak", ".sqlite", ".sqlite3"];
const databaseSignatures = [Buffer.from("PGDMP"), Buffer.from("SQLite format 3\0")];
const gitOutputBufferBytes = 64 * 1024 * 1024;

export function isProhibitedArtifactPath(repositoryPath) {
  const normalized = repositoryPath.replaceAll("\\", "/").replace(/^\.\//, "").toLowerCase();
  const segments = normalized.split("/");
  const containsBackupDirectory = segments.some(
    (segment, index) => segment === ".codex" && segments[index + 1] === "db-backups",
  );
  return containsBackupDirectory || forbiddenSuffixes.some((suffix) => normalized.endsWith(suffix));
}

export function hasProhibitedDatabaseSignature(prefix) {
  return databaseSignatures.some(
    (signature) => prefix.length >= signature.length && prefix.subarray(0, signature.length).equals(signature),
  );
}

function gitOutput(repositoryRoot, args) {
  return execFileSync("git", args, {
    cwd: repositoryRoot,
    encoding: "utf8",
    maxBuffer: gitOutputBufferBytes,
    stdio: ["ignore", "pipe", "pipe"],
  });
}

function nullSeparatedGitLines(repositoryRoot, args) {
  return gitOutput(repositoryRoot, args).split("\0").filter(Boolean);
}

function introducedCommits(repositoryRoot, baseSha) {
  if (!/^[0-9a-f]{40}$/i.test(baseSha)) {
    throw new Error("--changed-since requires a full 40-character Git commit SHA");
  }

  if (/^0+$/.test(baseSha)) {
    return gitOutput(repositoryRoot, ["rev-list", "--reverse", "HEAD"]).trim().split("\n").filter(Boolean);
  }

  execFileSync("git", ["rev-parse", "--verify", `${baseSha}^{commit}`], {
    cwd: repositoryRoot,
    stdio: "ignore",
  });
  return gitOutput(repositoryRoot, ["rev-list", "--reverse", `${baseSha}..HEAD`])
    .trim()
    .split("\n")
    .filter(Boolean);
}

function introducedArtifacts(repositoryRoot, baseSha) {
  const artifacts = new Map();
  for (const commit of introducedCommits(repositoryRoot, baseSha)) {
    const changedPaths = nullSeparatedGitLines(repositoryRoot, [
      "diff-tree",
      "--root",
      "-m",
      "--no-commit-id",
      "--name-only",
      "--no-renames",
      "-z",
      "-r",
      "--diff-filter=ACM",
      commit,
    ]);

    for (const repositoryPath of changedPaths) {
      const literalPathspec = `:(literal)${repositoryPath}`;
      const treeEntry = gitOutput(repositoryRoot, ["ls-tree", "-z", commit, "--", literalPathspec]);
      const match = /^(\d+) (\w+) ([0-9a-f]+)\t([\s\S]*)\0$/.exec(treeEntry);
      if (!match || match[2] !== "blob" || match[4] !== repositoryPath) {
        continue;
      }
      artifacts.set(`${match[3]}\0${repositoryPath}`, {
        blobOid: match[3],
        commit,
        repositoryPath,
      });
    }
  }
  return [...artifacts.values()];
}

function allTrackedArtifacts(repositoryRoot) {
  return nullSeparatedGitLines(repositoryRoot, ["ls-tree", "-r", "-z", "HEAD"]).flatMap((entry) => {
    const match = /^(\d+) (\w+) ([0-9a-f]+)\t([\s\S]+)$/.exec(entry);
    if (!match || match[2] !== "blob") {
      return [];
    }
    return [{ blobOid: match[3], commit: "HEAD", repositoryPath: match[4], useWorktree: true }];
  });
}

function stagedArtifacts(repositoryRoot) {
  const paths = nullSeparatedGitLines(repositoryRoot, [
    "diff",
    "--cached",
    "--name-only",
    "--diff-filter=ACM",
    "-z",
  ]);

  return paths.flatMap((repositoryPath) => {
    const entry = gitOutput(repositoryRoot, [
      "ls-files",
      "--stage",
      "-z",
      "--",
      `:(literal)${repositoryPath}`,
    ]);
    const match = /^(\d+) ([0-9a-f]+) (\d+)\t([\s\S]*)\0$/.exec(entry);
    if (!match || match[3] !== "0" || match[4] !== repositoryPath) {
      return [];
    }
    return [{ blobOid: match[2], commit: "INDEX", repositoryPath }];
  });
}

function readBlobPrefix(repositoryRoot, blobOid, limit = 32) {
  return new Promise((resolve, reject) => {
    const child = spawn("git", ["cat-file", "blob", blobOid], {
      cwd: repositoryRoot,
      stdio: ["ignore", "pipe", "pipe"],
    });
    const chunks = [];
    let length = 0;
    let settled = false;
    let errorText = "";

    child.stdout.on("data", (chunk) => {
      if (settled) return;
      const remaining = limit - length;
      if (remaining > 0) {
        const slice = chunk.subarray(0, remaining);
        chunks.push(slice);
        length += slice.length;
      }
      if (length >= limit) {
        settled = true;
        resolve(Buffer.concat(chunks, length));
        child.kill();
      }
    });
    child.stderr.on("data", (chunk) => {
      errorText += chunk.toString("utf8");
    });
    child.on("error", reject);
    child.on("close", (code) => {
      if (settled) return;
      if (code === 0) {
        settled = true;
        resolve(Buffer.concat(chunks, length));
      } else {
        reject(new Error(`unable to inspect Git blob ${blobOid}: ${errorText.trim()}`));
      }
    });
  });
}

async function readWorktreePrefix(repositoryRoot, repositoryPath, blobOid, limit = 32) {
  const resolvedRoot = path.resolve(repositoryRoot);
  const resolvedPath = path.resolve(resolvedRoot, repositoryPath);
  if (!resolvedPath.startsWith(`${resolvedRoot}${path.sep}`)) {
    throw new Error(`tracked path resolves outside the repository: ${repositoryPath}`);
  }

  try {
    const stats = await lstat(resolvedPath);
    if (!stats.isFile()) {
      return readBlobPrefix(repositoryRoot, blobOid, limit);
    }

    const handle = await open(resolvedPath, "r");
    try {
      const prefix = Buffer.alloc(limit);
      const { bytesRead } = await handle.read(prefix, 0, limit, 0);
      return prefix.subarray(0, bytesRead);
    } finally {
      await handle.close();
    }
  } catch (error) {
    if (error && typeof error === "object" && "code" in error && error.code === "ENOENT") {
      return readBlobPrefix(repositoryRoot, blobOid, limit);
    }
    throw error;
  }
}

async function findProhibitedArtifacts(repositoryRoot, artifacts) {
  const failures = [];
  for (const artifact of artifacts) {
    if (isProhibitedArtifactPath(artifact.repositoryPath)) {
      failures.push({ ...artifact, reason: "prohibited path or database-backup suffix" });
      continue;
    }

    const prefix = artifact.useWorktree
      ? await readWorktreePrefix(repositoryRoot, artifact.repositoryPath, artifact.blobOid)
      : await readBlobPrefix(repositoryRoot, artifact.blobOid);
    if (hasProhibitedDatabaseSignature(prefix)) {
      failures.push({ ...artifact, reason: "native database-backup signature" });
    }
  }
  return failures;
}

async function main() {
  const scriptPath = fileURLToPath(import.meta.url);
  const repositoryRoot = gitOutput(path.dirname(scriptPath), ["rev-parse", "--show-toplevel"]).trim();

  let artifacts;
  if (process.argv.length === 3 && process.argv[2] === "--staged") {
    artifacts = stagedArtifacts(repositoryRoot);
  } else if (process.argv.length === 3 && process.argv[2] === "--all-tracked") {
    artifacts = allTrackedArtifacts(repositoryRoot);
  } else if (process.argv.length === 4 && process.argv[2] === "--changed-since") {
    artifacts = introducedArtifacts(repositoryRoot, process.argv[3]);
  } else {
    console.error(
      "usage: check-sensitive-artifacts.mjs (--staged | --all-tracked | --changed-since <full-base-commit-sha>)",
    );
    process.exitCode = 64;
    return;
  }

  const failures = await findProhibitedArtifacts(repositoryRoot, artifacts);
  if (failures.length > 0) {
    console.error("Prohibited database artifacts detected:");
    for (const failure of failures) {
      console.error(`- ${failure.repositoryPath} at ${failure.commit}: ${failure.reason}`);
    }
    console.error("Store restricted evidence outside Git. Renaming or deleting it in a later commit is insufficient.");
    process.exitCode = 1;
    return;
  }

  console.log("No prohibited database artifacts detected.");
}

if (
  process.argv[1] &&
  realpathSync(path.resolve(process.argv[1])) === realpathSync(fileURLToPath(import.meta.url))
) {
  try {
    await main();
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    console.error(`Sensitive artifact policy could not complete: ${message}`);
    process.exitCode = 2;
  }
}
