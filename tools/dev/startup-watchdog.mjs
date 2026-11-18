import { spawn } from "node:child_process";
import { once } from "node:events";
import { fileURLToPath } from "node:url";

export async function startStartupWatchdog(timeoutSeconds = 45) {
  // The development Mac and Linux use Bash; other platforms retain lease-only coordination.
  if (process.platform === "win32") return { waiting() {}, stop() {} };
  const child = spawn("/bin/bash", [fileURLToPath(new URL("./startup-owner-watchdog.sh", import.meta.url)), String(process.pid), String(timeoutSeconds)],
    { stdio: ["pipe", "ignore", "inherit"] });
  await once(child, "spawn");
  let stopped = false;
  child.stdin.on("error", (error) => { if (!stopped) console.error("Startup watchdog pipe failed:", error.message); });
  child.unref();
  child.stdin.unref();
  return {
    waiting() { child.stdin.write("waiting\n"); },
    stop() { if (!stopped) { stopped = true; child.stdin.end("ready\n"); } }
  };
}
