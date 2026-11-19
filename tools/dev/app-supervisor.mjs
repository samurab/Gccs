import { setTimeout as delay } from "node:timers/promises";

// Only pass a child spawned and tracked by this supervisor, never a port owner.
export async function stopOwnedProcess(child, graceMs = 5_000) {
  if (!child?.pid) return;
  const posix = process.platform !== "win32";
  const signal = (name) => {
    try {
      if (posix) process.kill(-child.pid, name);
      else child.kill(name);
    } catch (error) {
      if (error.code !== "ESRCH") throw error;
    }
  };
  const alive = () => {
    if (!posix) return child.exitCode === null && child.signalCode === null;
    try { process.kill(-child.pid, 0); return true; }
    catch (error) {
      if (error.code === "ESRCH") return false;
      throw error;
    }
  };
  const wait = async (timeoutMs) => {
    const deadline = Date.now() + timeoutMs;
    while (alive() && Date.now() < deadline) await delay(50);
    return !alive();
  };
  signal("SIGTERM");
  if (posix) signal("SIGCONT"); // Let suspended processes handle termination.
  if (await wait(graceMs)) return;
  signal("SIGKILL");
  if (!(await wait(5_000))) throw new Error("Owned process group did not exit; replacement deferred.");
}

// Serialize recovery for one app; its failures never stop another app.
export function createAppSupervisor({ ensureReady, onError, retryMs = 5_000 }) {
  let active;
  let retry;
  let stopped = false;

  const supervisor = {
    get recovering() { return Boolean(active || retry); },
    ensure() {
      if (stopped) return Promise.resolve();
      if (active) return active;
      clearTimeout(retry);
      retry = undefined;
      active = Promise.resolve().then(ensureReady).catch((error) => {
        if (!stopped) {
          onError(error);
          retry = setTimeout(() => {
            retry = undefined;
            void supervisor.ensure().catch(() => {});
          }, retryMs);
        }
        throw error;
      }).finally(() => { active = undefined; });
      return active;
    },
    stop() {
      stopped = true;
      clearTimeout(retry);
      retry = undefined;
    }
  };
  return supervisor;
}
