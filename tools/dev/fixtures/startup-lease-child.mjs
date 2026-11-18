import { createServer } from "node:http";
import { acquireStartupLease } from "../startup-lease.mjs";

const port = Number(process.argv[2]);
const url = `http://127.0.0.1:${port}/`;
const marker = "startup lease test listener";
const report = (event) => console.log(JSON.stringify({ event, pid: process.pid, url }));
const lease = await acquireStartupLease({ port, watchdogSeconds: Number(process.argv[4] ?? 45), timeoutMs: Number(process.argv[5] ?? 90_000), findHealthyUrl: async () => {
  try {
    const response = await fetch(url, { signal: AbortSignal.timeout(200) });
    if (response.ok && await response.text() === marker) return url;
  } catch {}
} });
if (lease.existingUrl) {
  report("reused");
} else {
  report("acquired");
  // Keep the cold-start window open while other processes contend for the lease.
  await new Promise((resolve) => setTimeout(resolve, Number(process.argv[3] ?? 400)));
  const server = createServer((request, response) => response.end(marker));
  await new Promise((resolve) => server.listen(port, "127.0.0.1", resolve));
  report("ready");
  process.on("SIGTERM", () => {
    server.closeAllConnections();
    server.close();
    lease.release();
  });
}
