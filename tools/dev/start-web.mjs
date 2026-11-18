import { isIP } from "node:net";
import { createRequire } from "node:module";
import { dirname, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { parseArgs } from "node:util";
import { acquireStartupLease } from "./startup-lease.mjs";

export async function findExistingWebUrl({ server, base }) {
  const host = server.host ?? "localhost";
  if (server.https || !["localhost", "127.0.0.1", "::1"].includes(host) ||
      !Number.isInteger(server.port) || server.port < 1 || server.port > 65535) return;
  const origin = `http://${isIP(host) === 6 ? `[${host}]` : host}:${server.port}`;
  const url = new URL(base, `${origin}/`);
  if (url.origin !== origin) return;
  try {
    const response = await fetch(url, { signal: AbortSignal.timeout(2_000), redirect: "error" });
    if (response.ok && (await response.text()).includes("<title>FeDril | GovCon Compliance Readiness Software</title>")) {
      return url.href;
    }
  } catch { /* An unavailable listener retains Vite's normal startup behavior. */ }
}

async function main() {
  let options;
  try {
    options = parseArgs({ options: {
      host: { type: "string" }, port: { type: "string" }, strictPort: { type: "boolean" },
      config: { type: "string" }, configLoader: { type: "string" }
    } }).values;
  } catch {
    // Other CLI forms go directly to Vite so their existing behavior is preserved.
  }
  if (options) {
    const { resolveConfig } = await import("vite");
    const config = await resolveConfig({ configFile: options.config, configLoader: options.configLoader, server: {
      host: options.host,
      port: options.port === undefined ? undefined : Number(options.port),
      strictPort: options.strictPort
    } }, "serve");
    const host = config.server.host ?? "localhost";
    if (!config.server.https && ["localhost", "127.0.0.1", "::1"].includes(host) &&
        Number.isInteger(config.server.port) && config.server.port > 0 && config.server.port <= 65535) {
      const lease = await acquireStartupLease({ port: config.server.port, findHealthyUrl: () => findExistingWebUrl(config) });
      if (lease.existingUrl) {
        console.log(`FeDril web app is already running and healthy at ${lease.existingUrl}. Using the existing instance; no new web app was started.`);
        return;
      }
    }
  }
  const require = createRequire(import.meta.url);
  const cli = resolve(dirname(require.resolve("vite/package.json")), "bin/vite.js");
  await import(pathToFileURL(cli).href);
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) await main();
