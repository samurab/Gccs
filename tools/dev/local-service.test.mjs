import assert from "node:assert/strict";
import { createServer } from "node:net";
import { createServer as createHttpServer } from "node:http";
import { test } from "node:test";
import { ensureLocalService, probeHttp } from "./local-service.mjs";

async function withListener(host, run) {
  const server = createServer((socket) => socket.end());
  await new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(0, host, resolve);
  });
  try {
    await run(server.address().port);
  } finally {
    await new Promise((resolve) => server.close(resolve));
  }
}

async function unusedPort() {
  let port;
  await withListener("127.0.0.1", async (value) => { port = value; });
  return port;
}

test("reuses an already healthy service without starting a process", async () => {
  const result = await ensureLocalService({
    label: "test", port: 0, isReady: async () => true,
    start: () => assert.fail("must not start"), timeoutMs: 50
  });
  assert.equal(result, "reused");
});

for (const host of ["127.0.0.1", "::1"]) {
  test(`waits for an occupied ${host} port to become healthy without duplication`, async () => {
    await withListener(host, async (port) => {
      let checks = 0;
      const result = await ensureLocalService({
        label: "test", port, isReady: async () => ++checks >= 3,
        start: () => assert.fail("must not start"), timeoutMs: 1_000
      });
      assert.equal(result, "reused");
    });
  });
}

test("refuses to start when another or unhealthy service occupies the port", async () => {
  await withListener("127.0.0.1", async (port) => {
    await assert.rejects(ensureLocalService({
      label: "test", port, isReady: async () => false,
      start: () => assert.fail("must not start"), timeoutMs: 20
    }), /already occupied; no duplicate process/);
  });
});

test("starts a missing service once and waits for readiness", async () => {
  let starts = 0;
  let checks = 0;
  const result = await ensureLocalService({
    label: "test", port: await unusedPort(), isReady: async () => starts === 1 && ++checks >= 2,
    start: () => { starts++; }, timeoutMs: 1_000
  });
  assert.equal(result, "started");
  assert.equal(starts, 1);
});

test("reports a service that never becomes ready after starting", async () => {
  let starts = 0;
  await assert.rejects(ensureLocalService({
    label: "test", port: await unusedPort(), isReady: async () => false,
    start: () => { starts++; }, timeoutMs: 20
  }), /did not become ready/);
  assert.equal(starts, 1);
});

test("propagates startup failure without retrying the operation", async () => {
  await assert.rejects(ensureLocalService({
    label: "test", port: await unusedPort(), isReady: async () => false,
    start: () => { throw new Error("startup failed"); }, timeoutMs: 20
  }), /startup failed/);
});

async function withHttpServer(handler, run) {
  const server = createHttpServer(handler);
  await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
  try { await run(`http://127.0.0.1:${server.address().port}/`); }
  finally {
    server.closeAllConnections();
    await new Promise((resolve) => server.close(resolve));
  }
}

test("HTTP readiness distinguishes a responsive dependency error from a hang", async () => {
  await withHttpServer((request, response) => response.writeHead(503).end(), async (url) => {
    assert.deepEqual(await probeHttp(url, () => assert.fail("503 body must not be validated")),
      { ready: false, responsive: true });
  });
});

test("a nonresponding HTTP listener is detected without spawning another server", async () => {
  await withHttpServer(() => {}, async (url) => {
    assert.deepEqual(await probeHttp(url, () => true, 30), { ready: false, responsive: false });
    await assert.rejects(ensureLocalService({
      label: "foreign hung server", port: Number(new URL(url).port),
      isReady: async () => (await probeHttp(url, () => true, 20)).ready,
      start: () => assert.fail("occupied foreign listener must not be replaced"), timeoutMs: 30
    }), /already occupied/);
  });
});

test("healthy and malformed HTTP responses remain distinguishable from no response", async () => {
  await withHttpServer((request, response) => response.end('{"status":"ok"}'), async (url) => {
    assert.deepEqual(await probeHttp(url, async (response) => (await response.json()).status === "ok"),
      { ready: true, responsive: true });
  });
  await withHttpServer((request, response) => response.end('invalid JSON'), async (url) => {
    assert.deepEqual(await probeHttp(url, (response) => response.json()), { ready: false, responsive: true });
  });
});
