import assert from "node:assert/strict";
import { createServer } from "node:http";
import { test } from "node:test";
import { mkdtemp, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { spawn } from "node:child_process";
import { once } from "node:events";
import { findExistingWebUrl } from "./start-web.mjs";

const title = "<title>FeDril | GovCon Compliance Readiness Software</title>";

async function withServer(handler, check) {
  const server = createServer(handler);
  await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
  try { await check({ server: { host: "127.0.0.1", port: server.address().port }, base: "/" }); }
  finally {
    server.closeAllConnections();
    await new Promise((resolve) => server.close(resolve));
  }
}

test("recognizes the existing healthy FeDril web app at its configured base path", async () => {
  await withServer((request, response) => {
    assert.equal(request.url, "/app/");
    response.end(title);
  }, async (config) => {
    config.base = "/app/";
    assert.equal(await findExistingWebUrl(config), `http://127.0.0.1:${config.server.port}/app/`);
  });
});

for (const [label, status, body] of [["unrelated page", 200, "Another app"], ["unhealthy response", 503, title]]) {
  test(`does not reuse an ${label}`, async () => {
    await withServer((request, response) => response.writeHead(status).end(body), async (config) => {
      assert.equal(await findExistingWebUrl(config), undefined);
    });
  });
}

test("does not follow redirects to another healthy listener", async () => {
  let requests = 0;
  await withServer((request, response) => {
    requests++;
    response.writeHead(302, { Location: "/other" }).end(title);
  }, async (config) => assert.equal(await findExistingWebUrl(config), undefined));
  assert.equal(requests, 1);
});

test("a hung listener has a bounded probe without any process signals", async () => {
  await withServer(() => {}, async (config) => {
    const start = Date.now();
    assert.equal(await findExistingWebUrl(config), undefined);
    assert.ok(Date.now() - start < 8_000);
  });
});

test("a missing listener retains normal startup", async () => {
  let config;
  await withServer((request, response) => response.end(title), async (value) => { config = value; });
  assert.equal(await findExistingWebUrl(config), undefined);
});

test("unsupported bindings and external base URLs are not probed", async () => {
  for (const config of [
    { server: { host: true, port: 5173 }, base: "/" },
    { server: { host: "example.com", port: 5173 }, base: "/" },
    { server: { host: "127.0.0.1", port: 0 }, base: "/" },
    { server: { host: "127.0.0.1", port: 5173, https: {} }, base: "/" },
    { server: { host: "127.0.0.1", port: 5173 }, base: "https://example.com/" }
  ]) assert.equal(await findExistingWebUrl(config), undefined);
});

test("custom native config reuses its isolated port instead of the shared web app", { timeout: 10_000 }, async (context) => {
  await withServer((request, response) => response.end(title), async ({ server }) => {
    // Retain the generated config under the project's file-preservation rule.
    const folder = await mkdtemp(join(tmpdir(), "fedril-web-config-"));
    const config = join(folder, "vite.config.mjs");
    await writeFile(config, `export default ${JSON.stringify({ server })};\n`, { flag: "wx" });
    const child = spawn(process.execPath, [new URL("./start-web.mjs", import.meta.url).pathname,
      "--config", config, "--configLoader", "native"], { cwd: new URL("../../apps/web/", import.meta.url), stdio: ["ignore", "pipe", "pipe"] });
    context.after(() => { if (child.exitCode === null && child.signalCode === null) child.kill("SIGKILL"); });
    let output = "";
    let errors = "";
    child.stdout.on("data", (chunk) => { output += chunk; });
    child.stderr.on("data", (chunk) => { errors += chunk; });
    assert.deepEqual(await once(child, "exit"), [0, null], errors);
    assert.ok(output.includes(`http://127.0.0.1:${server.port}/`), output);
    assert.ok(output.includes("no new web app was started"), output);
  });
});
