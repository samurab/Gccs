import { execFile } from "node:child_process";
import { promisify } from "node:util";
import { fileURLToPath } from "node:url";

const execute = promisify(execFile);
export async function readStartupOwnerIdentity(port) {
  if (process.platform === "win32") return;
  try {
    const { stdout } = await execute("/bin/bash", [fileURLToPath(new URL("./startup-owner-identity.sh", import.meta.url)), String(port)],
      { timeout: 1_000, maxBuffer: 1_024 });
    const identity = stdout.trim();
    if (/^[1-9]\d*\|[^\r\n]+$/.test(identity)) return identity;
  } catch { /* Unknown owners keep the original bounded wait. */ }
}
