import { createServer } from "node:net";
import { setTimeout as delay } from "node:timers/promises";
import { startStartupWatchdog } from "./startup-watchdog.mjs";
import { readStartupOwnerIdentity } from "./startup-owner-identity.mjs";

// The kernel releases this loopback lease when the owning process exits, even on a crash.
export async function acquireStartupLease({ port, findHealthyUrl, timeoutMs = 90_000, watchdogSeconds = 45,
  readOwnerIdentity = readStartupOwnerIdentity }) {
  const coordinationPort = (port - 1 + 32_768) % 65_535 + 1;
  let deadline = Date.now() + timeoutMs;
  let previousOwner;
  let nextOwnerProbe = 0;
  let watchdog;
  try {
    while (true) {
      watchdog?.waiting();
      const existingUrl = await findHealthyUrl();
      if (existingUrl) { watchdog?.stop(); return { existingUrl, release() {} }; }
      // Arm before publishing the lease, so a hang immediately after bind is covered.
      watchdog ??= await startStartupWatchdog(watchdogSeconds);
      const server = createServer((socket) => socket.destroy());
      try {
        await new Promise((resolve, reject) => {
          server.once("error", reject);
          server.listen({ host: "127.0.0.1", port: coordinationPort, exclusive: true }, resolve);
        });
      } catch (error) {
        server.close();
        if (error.code !== "EADDRINUSE") throw error;
        if (Date.now() >= nextOwnerProbe) {
          nextOwnerProbe = Date.now() + Math.min(1_000, timeoutMs / 4);
          const owner = await readOwnerIdentity(coordinationPort);
          if (owner) {
            if (previousOwner && owner !== previousOwner) deadline = Date.now() + timeoutMs;
            previousOwner = owner;
          }
        }
        if (Date.now() >= deadline) throw new Error("Another local web startup or coordination-port owner did not become healthy within the startup wait; no new web app was started.");
        await delay(200);
        continue;
      }
      let released = false;
      let healthCheck;
      const release = () => {
        if (!released) { released = true; clearInterval(healthCheck); watchdog.stop(); server.close(); }
      };
      try {
        const existingUrl = await findHealthyUrl();
        if (existingUrl) release();
        else {
          let probing = false;
          healthCheck = setInterval(async () => {
            if (probing) return;
            probing = true;
            try {
              if (await findHealthyUrl()) { clearInterval(healthCheck); watchdog.stop(); }
            } catch { /* A failed health probe leaves the startup deadline armed. */ }
            finally { probing = false; }
          }, 200);
          healthCheck.unref();
        }
        server.unref();
        return { existingUrl, release };
      } catch (error) {
        release();
        throw error;
      }
    }
  } catch (error) {
    watchdog?.stop();
    throw error;
  }
}
