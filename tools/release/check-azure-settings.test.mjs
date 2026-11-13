import test from "node:test";
import assert from "node:assert/strict";
import { mkdtempSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const expected = {
  ASPNETCORE_ENVIRONMENT: "Staging", AllowedHosts: "api.example.test",
  Cors__AllowedOrigins__0: "https://web.example.test",
  Authentication__Authority: "https://login.microsoftonline.com/workforce/v2.0",
  Authentication__Audience: "api://workforce-api",
  Authentication__Customer__Authority: "https://customer.ciamlogin.com/customer/v2.0",
  Authentication__Customer__Audience: "api://customer-api"
};
const environment = {
  STAGING_API_BASE_URL: "https://api.example.test", STAGING_WEB_BASE_URL: "https://web.example.test",
  STAGING_MSAL_TENANT_ID: "workforce", STAGING_MSAL_API_SCOPE: "api://workforce-api/access_as_user",
  STAGING_CUSTOMER_AUTHORITY: expected.Authentication__Customer__Authority,
  STAGING_CUSTOMER_AUDIENCE: expected.Authentication__Customer__Audience,
  STAGING_RESOURCE_GROUP: "test-rg", STAGING_API_APP_NAME: "test-api"
};
function run({ values = expected, failure = false, missing = false, raw, namesRaw, namesAfterRaw,
  checkQuery = false, signalAtValues = false, hangAtValues = false } = {}) {
  // Retain synthetic fixtures; disposal requires separate exact authorization.
  const dir = mkdtempSync(join(process.env.FEDRIL_SETTINGS_TEST_ARTIFACT_ROOT ?? tmpdir(), "fedril-settings-test-"));
  const entries = Object.entries(values).map(([name, value]) => ({ name, value }));
  const quote = (value) => "'" + value.replaceAll("'", "'\\''") + "'";
  writeFileSync(join(dir, "az"), `#!/bin/sh\nexec ${quote(process.execPath)} ${quote(join(dir, "az.cjs"))} "$@"\n`, { mode: 0o700, flag: "wx" });
  writeFileSync(join(dir, "az.cjs"), `
if (${failure}) { console.error('secret-provider-detail'); process.exit(1); }
const entries = ${JSON.stringify(entries)};
const query = process.argv[process.argv.indexOf('--query') + 1];
const fs = require('node:fs');
const repeated = fs.existsSync(${JSON.stringify(dir)} + '/names-query.txt');
fs.writeFileSync(${JSON.stringify(dir)} + (query === '[].name' ? (repeated ? '/names-recheck-query.txt' : '/names-query.txt') : '/values-query.txt'), query, { flag: 'wx' });
if (query === '[].name') { console.log((repeated ? ${JSON.stringify(namesAfterRaw)} ?? ${JSON.stringify(namesRaw)} : ${JSON.stringify(namesRaw)}) ?? JSON.stringify(entries.map(({ name }) => name))); process.exit(0); }
if (${signalAtValues}) { fs.writeSync(2, 'secret-provider-detail'); process.kill(process.pid, 'SIGTERM'); }
if (${hangAtValues}) { fs.writeSync(2, 'secret-provider-detail'); setInterval(() => {}, 1000); return; }
const keys = [...query.matchAll(/name=='([^']+)'/g)].map((match) => match[1]);
if (query !== '[?' + keys.map((name) => "name=='" + name + "'").join('||') + '].{name:name,value:value}') process.exit(1);
if (${checkQuery} && entries.some(({ name }) => name.replaceAll('__', ':').toLowerCase().startsWith('cors:allowedorigins:') && !keys.includes(name))) process.exit(1);
console.log(${JSON.stringify(raw)} ?? JSON.stringify(entries.filter(({ name }) => keys.includes(name))));
`, { flag: "wx" });
  return spawnSync(process.execPath, [fileURLToPath(new URL("./check-azure-settings.mjs", import.meta.url)), "staging"], {
    encoding: "utf8", timeout: 45_000,
    env: { ...environment, PATH: `${dir}:/usr/bin:/bin`, ...(missing ? { STAGING_CUSTOMER_AUDIENCE: "" } : {}) }
  });
}
test("matches public explicit Azure configuration", () => {
  const result = run(); assert.equal(result.status, 0); assert.match(result.stdout, /7 keys/);
});
test("missing required identity variable fails before lookup", () => {
  const result = run({ missing: true }); assert.equal(result.status, 1); assert.match(result.stderr, /STAGING_CUSTOMER_AUDIENCE is missing/);
});
test("mismatch reports key only, never provider setting value", () => {
  const result = run({ values: { ...expected, Authentication__Audience: "unexpected-sensitive-value" } });
  assert.equal(result.status, 1); assert.match(result.stderr, /Authentication__Audience/);
  assert.doesNotMatch(result.stderr, /unexpected-sensitive-value/);
});
test("provider failure does not print provider diagnostics", () => {
  const result = run({ failure: true }); assert.equal(result.status, 1);
  assert.match(result.stderr, /Azure settings lookup failed/); assert.doesNotMatch(result.stderr, /secret-provider-detail/);
});
for (const [name, raw] of [
  ["malformed JSON", "secret-provider-detail is not JSON"],
  ["non-array JSON", JSON.stringify({ value: "secret-provider-detail" })],
  ["duplicate settings", JSON.stringify([{ name: "AllowedHosts", value: "secret-provider-detail" }, { name: "AllowedHosts", value: "api.example.test" }])],
  ["invalid entry shape", JSON.stringify([{ name: "AllowedHosts", value: { content: "secret-provider-detail" } }])]
]) test(`${name} fails without provider output`, () => {
  const result = run({ raw }); assert.equal(result.status, 1);
  assert.match(result.stderr, /response was malformed/);
  assert.doesNotMatch(result.stderr, /secret-provider-detail/);
});

test("query projects the discovered CORS population", () => {
  const result = run({ checkQuery: true }); assert.equal(result.status, 0);
});
test("valid additional origins pass syntax checks without approval claims", () => {
  const result = run({ values: { ...expected, Cors__AllowedOrigins__1: "https://other.example.test:8443" } });
  assert.equal(result.status, 0); assert.match(result.stdout, /2 CORS entries syntax-checked/);
  assert.match(result.stdout, /additional origins require approved baseline review/);
});
for (const origin of ["", "https://other.example.test/", "https://other.example.test/path", "https://other.example.test?x=y",
  "https://other.example.test#fragment", "https://user@other.example.test", "https://other.example.test\\path",
  "http://other.example.test", " https://other.example.test", "https://*.example.test", "https://localhost:8443", "https://[invalid]", "https://oth\ter.example.test", "https://oth\ner.example.test", "https://oth\rer.example.test"]) {
  test(`additional invalid CORS origin rejected: ${JSON.stringify(origin)}`, () => {
    const result = run({ values: { ...expected, Cors__AllowedOrigins__1: origin } });
    assert.equal(result.status, 1); assert.match(result.stderr, /Invalid public CORS configuration/);
    assert.doesNotMatch(result.stderr, /other\.example|user@|invalid\]/);
  });
}
test("non-numeric nested CORS setting fails without its value", () => {
  const result = run({ values: { ...expected, Cors__AllowedOrigins__name: "secret-provider-detail" } });
  assert.equal(result.status, 1); assert.match(result.stderr, /Invalid public CORS configuration/);
  assert.doesNotMatch(result.stderr, /secret-provider-detail/);
});
test("unrelated returned setting fails closed without its value", () => {
  const result = run({ raw: JSON.stringify([{ name: "Secret", value: "secret-provider-detail" }]) });
  assert.equal(result.status, 1); assert.match(result.stderr, /response was malformed/);
  assert.doesNotMatch(result.stderr, /secret-provider-detail/);
});
for (const key of ["cors__allowedorigins__1", "Cors:AllowedOrigins:1", "CORS__AllowedOrigins:1"]) {
  test(`discovers effective CORS alias ${key}`, () => {
    const result = run({ values: { ...expected, [key]: "https://other.example.test" }, checkQuery: true });
    assert.equal(result.status, 0); assert.match(result.stdout, /2 CORS entries syntax-checked/);
  });
  test(`rejects invalid value in CORS alias ${key}`, () => {
    const result = run({ values: { ...expected, [key]: "http://other.example.test" }, checkQuery: true });
    assert.equal(result.status, 1); assert.match(result.stderr, /Invalid public CORS configuration/);
  });
}
test("ambiguous aliases fail closed", () => {
  const result = run({ values: { ...expected, cors__allowedorigins__0: "https://other.example.test" } });
  assert.equal(result.status, 1); assert.match(result.stderr, /Invalid public CORS configuration/);
});
test("names-only projection omits unrelated setting values from CLI output", () => {
  const result = run({ values: { ...expected, Secret: "secret-provider-detail" }, checkQuery: true });
  assert.equal(result.status, 0); assert.doesNotMatch(result.stdout + result.stderr, /secret-provider-detail/);
});
for (const namesRaw of ["not JSON", "{}", '["AllowedHosts","AllowedHosts"]', '[{"name":"AllowedHosts"}]']) {
  test(`malformed names response is suppressed: ${namesRaw}`, () => {
    const result = run({ namesRaw }); assert.equal(result.status, 1); assert.match(result.stderr, /response was malformed/);
  });
}
test("disappearing CORS setting fails closed", () => {
  const result = run({ values: { ...expected, Cors__AllowedOrigins__1: "https://other.example.test" },
    raw: JSON.stringify(Object.entries(expected).map(([name, value]) => ({ name, value }))) });
  assert.equal(result.status, 1); assert.match(result.stderr, /changed or was incomplete/);
});

for (const key of ["Cors__AllowedOrigins__1", "cors:allowedorigins:1", "Authentication:Audience"]) {
  test(`new public setting appearing during lookup fails closed: ${key}`, () => {
    const result = run({ namesAfterRaw: JSON.stringify([...Object.keys(expected), key]) });
    assert.equal(result.status, 1);
    assert.match(result.stderr, /changed or was incomplete|Ambiguous public Azure setting aliases/);
  });
}
test("public setting disappearing after values lookup fails closed", () => {
  const result = run({ namesAfterRaw: JSON.stringify(Object.keys(expected).filter((name) => name !== "AllowedHosts")) });
  assert.equal(result.status, 1); assert.match(result.stderr, /changed or was incomplete/);
});
test("malformed final names response is suppressed", () => {
  const result = run({ namesAfterRaw: "secret-provider-detail is not JSON" });
  assert.equal(result.status, 1); assert.match(result.stderr, /response was malformed/);
  assert.doesNotMatch(result.stderr, /secret-provider-detail/);
});
for (const [canonical, alias] of [["AllowedHosts", "allowedhosts"],
  ["Authentication__Authority", "authentication:authority"],
  ["Authentication__Audience", "Authentication:Audience"],
  ["Authentication__Customer__Audience", "authentication:customer:audience"]]) {
  test(`ambiguous public alias fails without values: ${alias}`, () => {
    const result = run({ values: { ...expected, [alias]: "secret-provider-detail" } });
    assert.equal(result.status, 1); assert.match(result.stderr, /Ambiguous public Azure setting aliases/);
    assert.doesNotMatch(result.stderr, /secret-provider-detail/);
  });
  test(`single public alias resolves to canonical contract: ${alias}`, () => {
    const { [canonical]: value, ...others } = expected;
    const result = run({ values: { ...others, [alias]: value } });
    assert.equal(result.status, 0); assert.match(result.stdout, /7 keys/);
  });
}
test("signaled values lookup suppresses provider output", () => {
  const result = run({ signalAtValues: true });
  assert.equal(result.status, 1); assert.match(result.stderr, /Azure settings lookup failed/);
  assert.doesNotMatch(result.stdout + result.stderr, /secret-provider-detail/);
});
test("stalled values lookup terminates with suppressed diagnostics", () => {
  const result = run({ hangAtValues: true });
  assert.equal(result.signal, null); assert.equal(result.status, 1);
  assert.match(result.stderr, /Azure settings lookup failed/);
  assert.doesNotMatch(result.stdout + result.stderr, /secret-provider-detail/);
});
