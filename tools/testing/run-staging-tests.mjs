import { rm } from "node:fs/promises";
import { spawn } from "node:child_process";
import { createRequire } from "node:module";
import path from "node:path";

const require = createRequire(import.meta.url);
const playwrightCli = require.resolve("@playwright/test/cli");
const authDirectory = path.resolve(process.env.PLAYWRIGHT_STAGING_AUTH_DIR ?? ".auth");
const authFiles = ["staging-manager.json", "staging-auditor.json"]
  .map(fileName => path.join(authDirectory, fileName));

let exitCode = 1;
try {
  exitCode = await run(process.execPath, [playwrightCli, "test", "--config", "playwright.staging.config.ts", ...process.argv.slice(2)]);
} finally {
  await Promise.all(authFiles.map(file => rm(file, { force: true })));
  console.log("Removed temporary staging authentication state.");
}

process.exitCode = exitCode;

function run(command, args) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, { stdio: "inherit", env: process.env });
    child.once("error", reject);
    child.once("exit", code => resolve(code ?? 1));
  });
}
