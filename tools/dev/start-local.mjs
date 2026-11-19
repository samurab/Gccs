import { spawn } from "node:child_process";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { ensureLocalService, probeHttp } from "./local-service.mjs";
import { createAppSupervisor, stopOwnedProcess } from "./app-supervisor.mjs";

const repositoryRoot = resolve(fileURLToPath(new URL("../../", import.meta.url)));
const apiHealthUrl = "http://127.0.0.1:5062/health";
const webUrl = "http://127.0.0.1:5173/";
let shuttingDown = false;
let healthMonitor;
let checkingHealth = false;
const services = [
  {
    label: "API", port: 5062, isReady: isGccsApiHealthy, probe: probeApi, timeoutMs: 60_000,
    command: "dotnet", args: ["run", "--project", "apps/api"],
    prepare: async () => {
      await runOnce("dotnet", ["tool", "restore"]);
      await runOnce("dotnet", [
        "tool", "run", "dotnet-ef", "database", "update",
        "--project", "src/Gccs.Infrastructure/Gccs.Infrastructure.csproj",
        "--startup-project", "apps/api/Gccs.Api.csproj", "--context", "GccsDbContext"
      ]);
    }
  },
  {
    label: "Web", port: 5173, isReady: isFeDrilWebAvailable, probe: probeWeb, timeoutMs: 30_000,
    command: "npm", args: ["--workspace", "apps/web", "run", "dev", "--", "--host", "127.0.0.1", "--port", "5173", "--strictPort"]
  }
];

for (const service of services) {
  service.supervisor = createAppSupervisor({
    ensureReady: async () => {
      const attempt = service.attempt = (service.attempt ?? 0) + 1;
      console.log(`AppRecovery ${JSON.stringify({ app: service.label, attempt, event: "started" })}`);
      try {
        const state = await ensureLocalService({
          ...service,
          start: async () => {
            // A live child may still be starting before it opens its port.
            if (shuttingDown || isRunning(service.child)) return;
            await service.prepare?.();
            if (!shuttingDown) startManaged(service);
          }
        });
        console.log(`${service.label} ${state} and healthy on port ${service.port}.`);
      } catch (error) {
        // A live process can hold its port while its event loop is hung.
        if (!shuttingDown && isRunning(service.child)) {
          const health = await service.probe();
          if (!health.responsive && await isDockerAvailable()) {
            console.error(`${service.label} is unresponsive after its readiness timeout; stopping only its owned process group.`);
            await stopOwnedProcess(service.child);
          }
        }
        throw error;
      } finally {
        console.log(`AppRecovery ${JSON.stringify({ app: service.label, attempt, event: "finished" })}`);
      }
    },
    onError: (error) => console.error(`${service.label} recovery failed; retrying: ${error.message}`)
  });
}

process.on("SIGINT", () => void shutdown(0));
process.on("SIGTERM", () => void shutdown(0));

try {
  await startLocalStack();
} catch (error) {
  console.error(error instanceof Error ? error.message : error);
  await shutdown(1);
}

async function startLocalStack() {
  await runOnce("docker", ["compose", "-f", "infra/docker/docker-compose.yml", "up", "-d", "--no-recreate", "--wait"]);

  await Promise.allSettled(services.map((service) => service.supervisor.ensure()));

  console.log(`\nSupervising FeDril apps independently:\n- Web: ${webUrl}\n- API: ${apiHealthUrl}\n`);
  console.log("Press Ctrl+C to stop processes started by this command.");

  healthMonitor = setInterval(() => void checkHealth(), 15_000);
}

async function checkHealth() {
  if (shuttingDown || checkingHealth) return;
  checkingHealth = true;
  try {
    const healthy = await Promise.all(services.map((service) => service.isReady()));
    if (healthy.every(Boolean)) return;
    // Docker loss is shared; retain the login launcher's Docker recovery path.
    if (!(await isDockerAvailable())) {
      console.error("Docker became unavailable; restarting the dependency startup workflow.");
      await shutdown(1);
      return;
    }
    services.forEach((service, index) => {
      if (!healthy[index] && !service.supervisor.recovering) {
        void service.supervisor.ensure().catch(() => {});
      }
    });
  } finally {
    checkingHealth = false;
  }
}

function isDockerAvailable() {
  return new Promise((resolve) => {
    const child = spawn("docker", ["info", "--format", "{{.ServerVersion}}"], {
      cwd: repositoryRoot, env: process.env, stdio: "ignore", timeout: 5_000
    });
    child.once("error", () => resolve(false));
    child.once("exit", (code) => resolve(code === 0));
  });
}

function isRunning(child) {
  return child?.pid !== undefined && child.exitCode === null && child.signalCode === null;
}

function startManaged(service) {
  const child = spawn(service.command, service.args, {
    cwd: repositoryRoot,
    detached: process.platform !== "win32",
    env: process.env,
    stdio: "inherit"
  });

  service.child = child;
  child.once("error", (error) => {
    console.error(`${service.label} failed to start: ${error.message}`);
    if (service.child === child) service.child = undefined;
    if (!shuttingDown) void service.supervisor.ensure().catch(() => {});
  });
  child.once("exit", (code, signal) => {
    if (service.child === child) service.child = undefined;
    if (!shuttingDown) {
      console.error(`${service.label} stopped unexpectedly (${signal ?? `exit ${code ?? 1}`}); recovering only ${service.label}.`);
      void service.supervisor.ensure().catch(() => {});
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

async function isGccsApiHealthy() {
  return (await probeApi()).ready;
}

async function isFeDrilWebAvailable() {
  return (await probeWeb()).ready;
}

function probeApi() {
  return probeHttp(apiHealthUrl, async (response) => {
    const health = await response.json();
    return health.service === "gccs-api" && health.status === "ok";
  });
}

function probeWeb() {
  return probeHttp(webUrl, async (response) =>
    (await response.text()).includes("<title>FeDril | GovCon Compliance Readiness Software</title>"));
}

async function shutdown(exitCode) {
  if (shuttingDown) {
    return;
  }

  shuttingDown = true;
  if (healthMonitor) {
    clearInterval(healthMonitor);
  }

  for (const service of services) {
    service.supervisor.stop();
    const child = service.child;
    if (!isRunning(child)) {
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
