import test from "node:test";
import assert from "node:assert/strict";
import { main, verifyCleanupQuiescence } from "./check-cleanup-quiescence.mjs";

const env = { DEPLOYMENT_AZURE_SUBSCRIPTION_ID: "synthetic-subscription",
  STAGING_RESOURCE_GROUP: "synthetic-group", STAGING_API_APP_NAME: "synthetic-api" };
function fixture(overrides = {}) {
  const calls = [];
  const lookup = args => {
    calls.push(args);
    const command = args.slice(1, args.indexOf("--subscription")).join(" ");
    const query = args.at(-1);
    if (overrides.read) return overrides.read(command, query, calls.length);
    if (command === "show") return overrides.state ?? "Stopped";
    if (command === "list") return overrides.apps ?? [env.STAGING_API_APP_NAME];
    if (command === "deployment slot list") return overrides.slots ?? [];
    if (query === "[].name") return overrides.names ?? ["ObjectCleanupProcessing__Enabled", "UnrelatedSecretKey"];
    return overrides.values ?? ["false"];
  };
  return { calls, lookup };
}

test("only reads explicit subscription/app metadata and projects the cleanup flag", () => {
  const { calls, lookup } = fixture();
  assert.deepEqual(verifyCleanupQuiescence("staging", "Stopped", env, lookup),
    { environment: "staging", state: "Stopped", cleanupDisabled: true });
  for (const args of calls) {
    assert.equal(args[args.indexOf("--subscription") + 1], env.DEPLOYMENT_AZURE_SUBSCRIPTION_ID);
    assert.equal(args[args.indexOf("--resource-group") + 1], env.STAGING_RESOURCE_GROUP);
    assert.doesNotMatch(args.join(" "), /\b(?:set|start|stop|restart|delete|remove)\b/);
    assert.ok(!args.at(-1).includes("UnrelatedSecretKey"));
  }
  assert.equal(calls.filter(args => args.at(-1) === "state").length, 2);
  assert.equal(calls.filter(args => args.at(-1) === "[?name=='ObjectCleanupProcessing__Enabled'].value").length, 2);
});

test("accepts one case-insensitive colon alias and false conversion", () => {
  const { lookup } = fixture({ names: ["objectCleanupProcessing:Enabled"], values: [" False "] });
  assert.doesNotThrow(() => verifyCleanupQuiescence("staging", "Stopped", env, lookup));
});

for (const [name, overrides] of [
  ["running legacy API", { state: "Running" }],
  ["another API app", { apps: ["synthetic-api", "other-api"] }],
  ["an API slot", { slots: ["synthetic-api/staging"] }],
  ["missing override", { names: [] }],
  ["ambiguous separators", { names: ["ObjectCleanupProcessing__Enabled", "ObjectCleanupProcessing:Enabled"] }],
  ["duplicate provider names", { names: ["ObjectCleanupProcessing__Enabled", "ObjectCleanupProcessing__Enabled"] }],
  ["true override", { values: ["true"] }],
  ["absent returned value", { values: [] }],
  ["malformed flag type", { values: [false] }],
  ["multiple values", { values: ["false", "false"] }],
  ["malformed name response", { names: [null] }]
]) test(`fails closed for ${name}`, () => {
  const { lookup } = fixture(overrides);
  assert.throws(() => verifyCleanupQuiescence("staging", "Stopped", env, lookup));
});

test("post-start verification still requires cleanup disabled", () => {
  const { lookup } = fixture({ state: "Running" });
  assert.doesNotThrow(() => verifyCleanupQuiescence("staging", "Running", env, lookup));
  const unsafe = fixture({ state: "Running", values: ["true"] });
  assert.throws(() => verifyCleanupQuiescence("staging", "Running", env, unsafe.lookup));
});

test("observed setting and API-state drift stops the rollout", () => {
  for (const kind of ["key", "value", "state", "topology"]) {
    let keys = 0, values = 0, states = 0, apps = 0;
    const lookup = (command, query) => {
      if (command === "list") return ++apps === 2 && kind === "topology" ? [] : [env.STAGING_API_APP_NAME];
      if (command === "deployment slot list") return [];
      if (query === "state") return ++states === 2 && kind === "state" ? "Running" : "Stopped";
      if (query === "[].name") return ++keys === 2 && kind === "key" ? ["ObjectCleanupProcessing:Enabled"] : ["ObjectCleanupProcessing__Enabled"];
      return ++values === 2 && kind === "value" ? ["true"] : ["false"];
    };
    assert.throws(() => verifyCleanupQuiescence("staging", "Stopped", env, fixture({ read: lookup }).lookup), kind);
  }
});

test("invalid routing and malformed CLI/provider output never print values", () => {
  for (const args of [["invalid", "Stopped"], ["staging", "invalid"], ["staging"]]) {
    const messages = [];
    assert.equal(main(args, env, () => { throw new Error("private diagnostic"); },
      { log: x => messages.push(x), error: x => messages.push(x) }), 1);
    assert.doesNotMatch(messages.join(" "), /private diagnostic|synthetic-api|synthetic-group/);
  }
  const messages = [];
  assert.equal(main(["staging", "Stopped"], env, () => { throw new Error("private diagnostic"); },
    { log: x => messages.push(x), error: x => messages.push(x) }), 1);
  assert.doesNotMatch(messages.join(" "), /private diagnostic/);
  assert.equal(messages.length, 1);
});

test("production uses only production routing and rejects missing subscription", () => {
  const prod = { DEPLOYMENT_AZURE_SUBSCRIPTION_ID: "synthetic-subscription",
    PRODUCTION_RESOURCE_GROUP: "synthetic-group", PRODUCTION_API_APP_NAME: "synthetic-api" };
  assert.doesNotThrow(() => verifyCleanupQuiescence("production", "Stopped", prod, fixture().lookup));
  assert.throws(() => verifyCleanupQuiescence("production", "Stopped", { ...prod, DEPLOYMENT_AZURE_SUBSCRIPTION_ID: "" }, fixture().lookup));
});
