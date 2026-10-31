import { spawn } from "node:child_process";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";

const repositoryRoot = resolve(fileURLToPath(new URL("../../", import.meta.url)));
const apiHealthUrl = "http://127.0.0.1:5062/health";
const webUrl = "http://localhost:5173/";
const managedProcesses = [];
let shuttingDown = false;
let healthMonitor;

process.on("SIGINT", () => void shutdown(0));
process.on("SIGTERM", () => void shutdown(0));

try {
  await startLocalStack();
} catch (error) {
  console.error(error instanceof Error ? error.message : error);
  await shutdown(1);
}

async function startLocalStack() {
  await runOnce("docker", ["compose", "-f", "infra/docker/docker-compose.yml", "up", "-d", "--wait"]);

  if (await isGccsApiHealthy()) {
    console.log(`Reusing healthy GCCS API at ${apiHealthUrl}.`);
  } else {
    await runOnce("dotnet", ["tool", "restore"]);
    await runOnce("dotnet", [
      "tool",
      "run",
      "dotnet-ef",
      "database",
      "update",
      "--project",
      "src/Gccs.Infrastructure/Gccs.Infrastructure.csproj",
      "--startup-project",
      "apps/api/Gccs.Api.csproj",
      "--context",
      "GccsDbContext"
    ]);
    startManaged("API", "dotnet", ["run", "--project", "apps/api"]);
    await waitFor("GCCS API", isGccsApiHealthy, 60_000);
  }

  if (await isFeDrilWebAvailable()) {
    console.log(`Reusing FeDril web app at ${webUrl}.`);
  } else {
    startManaged("Web", "npm", ["--workspace", "apps/web", "run", "dev"]);
    await waitFor("FeDril web app", isFeDrilWebAvailable, 30_000);
  }

  console.log(`\nFeDril local stack is ready:\n- Web: ${webUrl}\n- API: ${apiHealthUrl}\n`);
  console.log("Press Ctrl+C to stop processes started by this command.");

  healthMonitor = setInterval(async () => {
    if (shuttingDown) {
      return;
    }

    if (!(await isGccsApiHealthy()) || !(await isFeDrilWebAvailable())) {
      console.error("A local GCCS service became unavailable. Stopping the supervised stack.");
      await shutdown(1);
    }
  }, 15_000);
}

function startManaged(label, command, args) {
  const child = spawn(command, args, {
    cwd: repositoryRoot,
    detached: process.platform !== "win32",
    env: process.env,
    stdio: "inherit"
  });

  managedProcesses.push({ child, label });
  child.once("error", (error) => {
    console.error(`${label} failed to start: ${error.message}`);
    void shutdown(1);
  });
  child.once("exit", (code, signal) => {
    if (!shuttingDown) {
      console.error(`${label} stopped unexpectedly (${signal ?? `exit ${code ?? 1}`}).`);
      void shutdown(code ?? 1);
    }
  });
}

async function runOnce(command, args) {
  const exitCode = await new Promise((resolveExitCode, reject) => {
    const child = spawn(command, args, {
      cwd: repositoryRoot,
      env: process.env,
      stdio: "inherit"
    });

    child.once("error", reject);
    child.once("exit", (code) => resolveExitCode(code ?? 1));
  });

  if (exitCode !== 0) {
    throw new Error(`${command} ${args.join(" ")} failed with exit code ${exitCode}.`);
  }
}

async function waitFor(label, check, timeoutMs) {
  const deadline = Date.now() + timeoutMs;

  while (Date.now() < deadline) {
    if (await check()) {
      return;
    }

    await new Promise((resolveDelay) => setTimeout(resolveDelay, 500));
  }

  throw new Error(`${label} did not become ready within ${timeoutMs / 1000} seconds.`);
}

async function isGccsApiHealthy() {
  try {
    const response = await fetch(apiHealthUrl, { signal: AbortSignal.timeout(2_000) });
    if (!response.ok) {
      return false;
    }

    const health = await response.json();
    return health.service === "gccs-api" && health.status === "ok";
  } catch {
    return false;
  }
}

async function isFeDrilWebAvailable() {
  try {
    const response = await fetch(webUrl, { signal: AbortSignal.timeout(2_000) });
    if (!response.ok) {
      return false;
    }

    const html = await response.text();
    return html.includes("<title>FeDril | GovCon Compliance Readiness Software</title>");
  } catch {
    return false;
  }
}

async function shutdown(exitCode) {
  if (shuttingDown) {
    return;
  }

  shuttingDown = true;
  if (healthMonitor) {
    clearInterval(healthMonitor);
  }

  for (const { child } of managedProcesses) {
    if (child.exitCode !== null || child.pid === undefined) {
      continue;
    }

    try {
      if (process.platform === "win32") {
        child.kill("SIGTERM");
      } else {
        process.kill(-child.pid, "SIGTERM");
      }
    } catch {
      // The process may have exited between the status check and signal.
    }
  }

  setTimeout(() => process.exit(exitCode), 100);
}
