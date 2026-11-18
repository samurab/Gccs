import assert from "node:assert/strict";
import { createServer } from "node:net";
import { spawn } from "node:child_process";
import { once } from "node:events";
import { setTimeout as delay } from "node:timers/promises";
import { test } from "node:test";
import { acquireStartupLease } from "./startup-lease.mjs";

async function unusedPort() {
  const server = createServer();
  await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
  const port = server.address().port;
  await new Promise((resolve) => server.close(resolve));
  return port;
}

async function until(check) {
  const deadline = Date.now() + 10_000;
  while (!check()) {
    assert.ok(Date.now() < deadline, "Startup coordination timed out");
    await delay(10);
  }
}

function startChild(port, startupDelay = 400, watchdogSeconds = 45, timeoutMs = 90_000) {
  const child = spawn(process.execPath, [new URL("./fixtures/startup-lease-child.mjs", import.meta.url).pathname, String(port), String(startupDelay), String(watchdogSeconds), String(timeoutMs)],
    { stdio: ["ignore", "pipe", "pipe"] });
  const events = [];
  let pending = "";
  let errors = "";
  child.stdout.on("data", (chunk) => {
    pending += chunk;
    let newline;
    while ((newline = pending.indexOf("\n")) >= 0) {
      events.push(JSON.parse(pending.slice(0, newline)));
      pending = pending.slice(newline + 1);
    }
  });
  child.stderr.on("data", (chunk) => { errors += chunk; });
  const exited = once(child, "exit");
  return { child, events, exited, get errors() { return errors; } };
}

async function stop(children) {
  for (const { child } of children) if (child.exitCode === null && child.signalCode === null) child.kill("SIGKILL");
  await Promise.all(children.map(({ exited }) => exited));
}

test("eight simultaneous cold-start processes produce one owner and seven clean URL exits", async () => {
  const port = await unusedPort();
  const children = Array.from({ length: 8 }, () => startChild(port));
  try {
    await until(() => children.every(({ events }) => events.some(({ event }) => ["ready", "reused"].includes(event))));
    assert.equal(children.flatMap(({ events }) => events).filter(({ event }) => event === "acquired").length, 1);
    const reused = children.filter(({ events }) => events.some(({ event }) => event === "reused"));
    assert.equal(reused.length, 7);
    for (const entry of reused) {
      assert.deepEqual(await entry.exited, [0, null]);
      assert.equal(entry.errors, "");
      assert.equal(entry.events[0].url, `http://127.0.0.1:${port}/`);
    }
  } finally { await stop(children); }
});

test("a crash during startup releases the OS lease for exactly one waiting process", async () => {
  const port = await unusedPort();
  const owner = startChild(port, 30_000);
  const children = [owner];
  try {
    await until(() => owner.events.some(({ event }) => event === "acquired"));
    children.push(startChild(port), startChild(port));
    await delay(100);
    assert.equal(children.slice(1).flatMap(({ events }) => events).length, 0);
    owner.child.kill("SIGKILL");
    await owner.exited;
    await until(() => children.slice(1).every(({ events }) => events.some(({ event }) => ["ready", "reused"].includes(event))));
    assert.equal(children.slice(1).flatMap(({ events }) => events).filter(({ event }) => event === "acquired").length, 1);
    const reused = children.slice(1).find(({ events }) => events.some(({ event }) => event === "reused"));
    assert.deepEqual(await reused.exited, [0, null]);
  } finally { await stop(children); }
});

test("a fresh health check after acquisition releases the lease without starting a service", async () => {
  const port = await unusedPort();
  let probes = 0;
  const lease = await acquireStartupLease({ port, findHealthyUrl: async () => ++probes === 1 ? undefined : "http://localhost/" });
  assert.equal(lease.existingUrl, "http://localhost/");
  const next = await acquireStartupLease({ port, findHealthyUrl: async () => undefined, timeoutMs: 300 });
  try { assert.equal(next.existingUrl, undefined); }
  finally { lease.release(); next.release(); }
});

test("a suspended startup owner is killed by its watchdog and waiters elect one replacement", async () => {
  const port = await unusedPort();
  const owner = startChild(port, 30_000, 2);
  const children = [owner];
  try {
    await until(() => owner.events.some(({ event }) => event === "acquired"));
    owner.child.kill("SIGSTOP");
    children.push(startChild(port), startChild(port), startChild(port));
    assert.deepEqual(await Promise.race([owner.exited, delay(8_000, undefined, { ref: false }).then(() => { throw new Error("Watchdog did not terminate suspended owner"); })]), [null, "SIGKILL"]);
    assert.match(owner.errors, /startup watchdog: owner .*terminating it so a waiter can take over/);
    await until(() => children.slice(1).every(({ events }) => events.some(({ event }) => ["ready", "reused"].includes(event))));
    assert.equal(children.slice(1).flatMap(({ events }) => events).filter(({ event }) => event === "acquired").length, 1);
    const reused = children.slice(1).filter(({ events }) => events.some(({ event }) => event === "reused"));
    assert.equal(reused.length, 2);
    for (const entry of reused) assert.deepEqual(await entry.exited, [0, null]);
  } finally { await stop(children); }
});

test("healthy startup disarms the watchdog without releasing its lease or restarting the owner", async () => {
  const child = startChild(await unusedPort(), 0, 1);
  try {
    await until(() => child.events.some(({ event }) => event === "ready"));
    await delay(1_500);
    assert.equal(child.child.exitCode, null);
    assert.equal(child.child.signalCode, null);
    assert.equal(child.errors, "");
    const reused = startChild(Number(new URL(child.events[0].url).port));
    try {
      assert.deepEqual(await reused.exited, [0, null]);
      assert.equal(reused.events[0].event, "reused");
    } finally { await stop([reused]); }
  } finally { await stop([child]); }
});

test("watchdog rejects a PID that is not its own parent", async () => {
  const child = spawn("/bin/bash", [new URL("./startup-owner-watchdog.sh", import.meta.url).pathname, "1", "1"],
    { stdio: ["pipe", "ignore", "pipe"] });
  assert.deepEqual(await once(child, "exit"), [2, null]);
  child.stdin.destroy();
});

test("a busy coordination port fails closed within the bounded wait", async () => {
  const port = await unusedPort();
  const lease = await acquireStartupLease({ port, findHealthyUrl: async () => undefined });
  try {
    await assert.rejects(acquireStartupLease({ port, findHealthyUrl: async () => undefined, timeoutMs: 100 }), /no new web app was started/);
  } finally { lease.release(); }
});

test("different app ports acquire independently and failed probes release owned leases", async () => {
  const firstPort = await unusedPort();
  const secondPort = await unusedPort();
  const first = await acquireStartupLease({ port: firstPort, findHealthyUrl: async () => undefined });
  const second = await acquireStartupLease({ port: secondPort, findHealthyUrl: async () => undefined });
  first.release(); second.release();
  let probes = 0;
  await assert.rejects(acquireStartupLease({ port: firstPort, findHealthyUrl: async () => {
    if (++probes > 1) throw new Error("probe failed");
  } }), /probe failed/);
  const replacement = await acquireStartupLease({ port: firstPort, findHealthyUrl: async () => undefined });
  replacement.release();
});


test("owner changes renew the budget while an unchanged or unknown owner still times out", async () => {
  const port = await unusedPort();
  let healthy = false;
  const probe = async () => healthy ? "http://localhost/" : undefined;
  const owner = await acquireStartupLease({ port, findHealthyUrl: probe });
  try {
    let identities = 0;
    let finished = false;
    const waiting = acquireStartupLease({ port, findHealthyUrl: probe, timeoutMs: 600,
      readOwnerIdentity: async () => `owner-${Math.min(++identities, 4)}` });
    waiting.then(() => { finished = true; }, () => { finished = true; });
    await delay(1_000);
    assert.equal(finished, false);
    healthy = true;
    assert.equal((await waiting).existingUrl, "http://localhost/");
    assert.ok(identities >= 3);
    healthy = false;
    for (const identity of [undefined, "same-owner"]) {
      const started = Date.now();
      await assert.rejects(acquireStartupLease({ port, findHealthyUrl: probe, timeoutMs: 600,
        readOwnerIdentity: async () => identity }), /no new web app was started/);
      assert.ok(Date.now() - started >= 500 && Date.now() - started < 3_000);
    }
  } finally { owner.release(); }
});

test("the same waiters survive three suspended owners beyond the original wait budget", async () => {
  const port = await unusedPort();
  const children = [startChild(port, 30_000, 1, 1_600)];
  const victims = new Set();
  try {
    await until(() => children[0].events.some(({ event }) => event === "acquired"));
    children[0].child.kill("SIGSTOP");
    children.push(...Array.from({ length: 5 }, () => startChild(port, 400, 1, 1_600)));
    for (let generation = 0; generation < 3; generation++) {
      await until(() => children.some((entry) => !victims.has(entry) && entry.events.some(({ event }) => event === "acquired")));
      const owner = children.find((entry) => !victims.has(entry) && entry.events.some(({ event }) => event === "acquired"));
      owner.child.kill("SIGSTOP");
      victims.add(owner);
      assert.deepEqual(await Promise.race([owner.exited, delay(5_000, undefined, { ref: false }).then(() => { throw new Error("Watchdog did not terminate owner"); })]), [null, "SIGKILL"]);
      assert.match(owner.errors, /startup watchdog: owner/);
    }
    const survivors = children.filter((entry) => !victims.has(entry));
    await until(() => survivors.every(({ events }) => events.some(({ event }) => ["ready", "reused"].includes(event))));
    assert.equal(survivors.filter(({ events }) => events.some(({ event }) => event === "ready")).length, 1);
    const extras = survivors.filter(({ events }) => events.some(({ event }) => event === "reused"));
    assert.equal(extras.length, 2);
    for (const entry of extras) assert.deepEqual(await entry.exited, [0, null]);
  } finally { await stop(children); }
});
