import { createConnection } from "node:net";
import { setTimeout } from "node:timers/promises";

export async function probeHttp(url, validate, timeoutMs = 2_000) {
  let response;
  try {
    response = await fetch(url, { signal: AbortSignal.timeout(timeoutMs) });
    if (!response.ok) return { ready: false, responsive: true };
    return { ready: Boolean(await validate(response)), responsive: true };
  } catch (error) {
    return {
      ready: false,
      responsive: Boolean(response) && !["AbortError", "TimeoutError"].includes(error.name)
    };
  }
}

async function isListening(port, host) {
  return new Promise((resolve, reject) => {
    const socket = createConnection({ host, port });
    socket.once("connect", () => { socket.destroy(); resolve(true); });
    socket.once("error", (error) => {
      socket.destroy();
      if (["ECONNREFUSED", "EAFNOSUPPORT", "ENETUNREACH"].includes(error.code)) resolve(false);
      else reject(error);
    });
    socket.setTimeout(2_000, () => {
      socket.destroy();
      reject(new Error(`Timed out checking ${host}:${port}; refusing to start another process.`));
    });
  });
}

export async function ensureLocalService({ label, port, isReady, start, timeoutMs }) {
  if (await isReady()) return "reused";

  const occupied = await isListening(port, "127.0.0.1") || await isListening(port, "::1");
  if (!occupied) await start();

  const deadline = Date.now() + timeoutMs;
  do {
    if (await isReady()) return occupied ? "reused" : "started";
    await setTimeout(Math.min(500, Math.max(0, deadline - Date.now())));
  } while (Date.now() < deadline);

  throw new Error(`${label} did not become ready on port ${port} within ${timeoutMs / 1000} seconds.${occupied ? " The port is already occupied; no duplicate process was started." : ""}`);
}
