import assert from "node:assert/strict";
import { setTimeout as delay } from "node:timers/promises";
import { test } from "node:test";
import { spawn } from "node:child_process";
import { once } from "node:events";
import { createAppSupervisor, stopOwnedProcess } from "./app-supervisor.mjs";

async function until(check) {
  const deadline = Date.now() + 1_000;
  while (!check()) {
    assert.ok(Date.now() < deadline, "Recovery did not finish");
    await delay(5);
  }
}

test("concurrent recovery requests share one startup operation", async () => {
  let calls = 0;
  let release;
  const gate = new Promise((resolve) => { release = resolve; });
  const app = createAppSupervisor({
    ensureReady: async () => { calls++; await gate; }, onError: assert.fail
  });
  try {
    const first = app.ensure();
    assert.equal(first, app.ensure());
    await until(() => calls === 1);
    assert.equal(app.recovering, true);
    release();
    await first;
    assert.equal(calls, 1);
    assert.equal(app.recovering, false);
  } finally { app.stop(); }
});

test("one app retries its failure without restarting the healthy app", async () => {
  let apiStarts = 0;
  let webStarts = 0;
  const api = createAppSupervisor({
    ensureReady: async () => { apiStarts++; }, onError: assert.fail
  });
  const web = createAppSupervisor({
    ensureReady: async () => {
      webStarts++;
      if (webStarts === 1) throw new Error("web crashed");
    }, onError: () => {}, retryMs: 10
  });
  try {
    await api.ensure();
    await assert.rejects(web.ensure(), /web crashed/);
    await until(() => webStarts === 2 && !web.recovering);
    assert.equal(apiStarts, 1);
    assert.equal(webStarts, 2);
  } finally { api.stop(); web.stop(); }
});

test("repeated failures retry serially without overlapping startup operations", async () => {
  let attempts = 0;
  let active = 0;
  let maximum = 0;
  const app = createAppSupervisor({
    ensureReady: async () => {
      attempts++; active++; maximum = Math.max(maximum, active);
      await delay(5);
      active--;
      if (attempts < 3) throw new Error("not ready");
    }, onError: () => {}, retryMs: 5
  });
  try {
    await assert.rejects(app.ensure(), /not ready/);
    await until(() => attempts === 3 && !app.recovering);
    assert.equal(maximum, 1);
  } finally { app.stop(); }
});

test("shutdown cancels a scheduled retry and rejects further startup requests", async () => {
  let starts = 0;
  const app = createAppSupervisor({
    ensureReady: async () => { starts++; throw new Error("failed"); },
    onError: () => {}, retryMs: 20
  });
  await assert.rejects(app.ensure(), /failed/);
  app.stop();
  await app.ensure();
  await delay(40);
  assert.equal(starts, 1);
  assert.equal(app.recovering, false);
});

test("a startup failure after shutdown cannot schedule a new process", async () => {
  let release;
  let starts = 0;
  let errors = 0;
  const gate = new Promise((resolve) => { release = resolve; });
  const app = createAppSupervisor({
    ensureReady: async () => { starts++; await gate; throw new Error("late failure"); },
    onError: () => { errors++; }, retryMs: 5
  });
  const pending = app.ensure();
  await until(() => starts === 1);
  app.stop(); release();
  await assert.rejects(pending, /late failure/);
  await delay(20);
  assert.equal(starts, 1);
  assert.equal(errors, 0);
});

test("rapid request bursts and automatic retries never overlap recovery attempts", async () => {
  let attempts = 0;
  let active = 0;
  let maximum = 0;
  let requests = 0;
  const app = createAppSupervisor({
    ensureReady: async () => {
      attempts++; active++; maximum = Math.max(maximum, active);
      await delay(5);
      active--;
      if (attempts < 20) throw new Error("repeated crash");
    }, onError: () => {}, retryMs: 5
  });
  try {
    const deadline = Date.now() + 2_000;
    while (attempts < 20) {
      assert.ok(Date.now() < deadline, "Stress recovery timed out");
      for (let request = 0; request < 25; request++) {
        requests++;
        void app.ensure().catch(() => {});
      }
      await delay(2);
    }
    await until(() => !app.recovering);
    assert.equal(maximum, 1);
    assert.equal(active, 0);
    assert.equal(attempts, 20);
    assert.ok(requests > attempts);
  } finally { app.stop(); }
});

test("owned process termination waits for graceful exit", { skip: process.platform === "win32" }, async () => {
  const child = spawn(process.execPath, ["-e", "console.log('ready'); setInterval(() => {}, 1000)"],
    { detached: true, stdio: ["ignore", "pipe", "pipe"] });
  const exited = once(child, "exit");
  try {
    await once(child.stdout, "data");
    await stopOwnedProcess(child, 500);
    const [, signal] = await exited;
    assert.equal(signal, "SIGTERM");
  } finally {
    if (child.exitCode === null && child.signalCode === null) {
      await stopOwnedProcess(child, 50);
    }
  }
});

test("a suspended owned process ignoring termination is resumed then killed within the grace bound", { skip: process.platform === "win32" }, async () => {
  const child = spawn(process.execPath, ["-e", "process.on('SIGTERM', () => {}); console.log('ready'); setInterval(() => {}, 1000)"],
    { detached: true, stdio: ["ignore", "pipe", "pipe"] });
  const exited = once(child, "exit");
  try {
    await once(child.stdout, "data");
    process.kill(child.pid, "SIGSTOP");
    await stopOwnedProcess(child, 100);
    const [, signal] = await exited;
    assert.equal(signal, "SIGKILL");
  } finally {
    if (child.exitCode === null && child.signalCode === null) {
      await stopOwnedProcess(child, 50);
    }
  }
});

test("absence of an owned child never causes a process signal", async () => {
  await stopOwnedProcess(undefined);
  await stopOwnedProcess({});
});
