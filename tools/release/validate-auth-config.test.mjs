import test from "node:test";
import assert from "node:assert/strict";
import { mkdtemp, readFile, rm, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { validateAuthConfig } from "./validate-auth-config.mjs";

const id = "11111111-2222-3333-4444-555555555555";
const workforce = {
  FEDRIL_WORKFORCE_CLIENT_ID: id,
  FEDRIL_WORKFORCE_TENANT_ID: id,
  FEDRIL_WORKFORCE_API_SCOPE: `api://${id}/access_as_user`
};
const complete = {
  ...workforce,
  FEDRIL_REQUIRE_CUSTOMER_AUTH: "true",
  FEDRIL_CUSTOMER_CLIENT_ID: id,
  FEDRIL_CUSTOMER_TENANT_ID: id,
  FEDRIL_CUSTOMER_TENANT_SUBDOMAIN: "example-customers",
  FEDRIL_CUSTOMER_API_SCOPE: `api://${id}/access_as_customer`,
  FEDRIL_CUSTOMER_AUTHORITY: `https://example-customers.ciamlogin.com/${id}/v2.0`,
  FEDRIL_CUSTOMER_AUDIENCE: `api://${id}`
};

test("accepts complete matching release configuration and legacy workforce-only configuration", () => {
  assert.doesNotThrow(() => validateAuthConfig(complete));
  assert.doesNotThrow(() => validateAuthConfig(workforce));
  assert.doesNotThrow(() => validateAuthConfig({ ...complete, FEDRIL_CUSTOMER_AUTHORITY: `${complete.FEDRIL_CUSTOMER_AUTHORITY}/` }));
});

for (const name of Object.keys(complete).filter(name => name !== "FEDRIL_REQUIRE_CUSTOMER_AUTH")) {
  test(`rejects a release missing ${name}`, () => {
    assert.throws(() => validateAuthConfig({ ...complete, [name]: " " }), new RegExp(name));
  });
}

test("rejects partial optional customer config and half-configured API pairing", () => {
  assert.throws(() => validateAuthConfig({ ...workforce, FEDRIL_CUSTOMER_CLIENT_ID: id }), /TENANT_ID/);
  assert.throws(() => validateAuthConfig({ ...complete, FEDRIL_REQUIRE_CUSTOMER_AUTH: "false", FEDRIL_CUSTOMER_AUDIENCE: "" }), /AUDIENCE/);
  assert.throws(() => validateAuthConfig({ ...complete, FEDRIL_REQUIRE_CUSTOMER_AUTH: "false", FEDRIL_CUSTOMER_AUTHORITY: "" }), /AUTHORITY/);
});

for (const [name, value] of [
  ["FEDRIL_WORKFORCE_CLIENT_ID", "not-a-guid"],
  ["FEDRIL_WORKFORCE_TENANT_ID", "common"],
  ["FEDRIL_WORKFORCE_API_SCOPE", "https://graph.microsoft.com/.default"],
  ["FEDRIL_CUSTOMER_CLIENT_ID", "invalid"],
  ["FEDRIL_CUSTOMER_TENANT_ID", "organizations"],
  ["FEDRIL_CUSTOMER_TENANT_SUBDOMAIN", "host.example"],
  ["FEDRIL_CUSTOMER_API_SCOPE", `api://${id}/scope other`],
  ["FEDRIL_CUSTOMER_AUTHORITY", `http://example-customers.ciamlogin.com/${id}/v2.0`],
  ["FEDRIL_CUSTOMER_AUTHORITY", `https://wrong.ciamlogin.com/${id}/v2.0`],
  ["FEDRIL_CUSTOMER_AUTHORITY", "https://example-customers.ciamlogin.com/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/v2.0"],
  ["FEDRIL_CUSTOMER_AUTHORITY", `${complete.FEDRIL_CUSTOMER_AUTHORITY}?unexpected=true`],
  ["FEDRIL_CUSTOMER_AUDIENCE", "api://aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"],
  ["FEDRIL_REQUIRE_CUSTOMER_AUTH", "TRUE"]
]) {
  test(`rejects malformed or mismatched ${name}: ${value}`, () => {
    assert.throws(() => validateAuthConfig({ ...complete, [name]: value }), new RegExp(name));
  });
}

test("writer leaves existing output unchanged on rejection; valid output contains no API validation metadata", async () => {
  const directory = await mkdtemp(join(tmpdir(), "fedril-auth-config-"));
  try {
    const output = join(directory, "runtime-config.js");
    await writeFile(output, "existing configuration");
    const env = {
      ...complete,
      FEDRIL_API_BASE_URL: "https://api.example.test",
      FEDRIL_APP_VERSION: "1.0.0",
      FEDRIL_COMMIT_SHA: "a".repeat(40),
      FEDRIL_BUILD_ID: "123"
    };
    const writer = fileURLToPath(new URL("./write-runtime-config.mjs", import.meta.url));
    const rejected = spawnSync(process.execPath, [writer, output], { env: { ...env, FEDRIL_CUSTOMER_AUDIENCE: "wrong" }, encoding: "utf8" });
    assert.notEqual(rejected.status, 0);
    assert.equal(await readFile(output, "utf8"), "existing configuration");
    const accepted = spawnSync(process.execPath, [writer, output], { env, encoding: "utf8" });
    assert.equal(accepted.status, 0, accepted.stderr);
    const written = await readFile(output, "utf8");
    assert.match(written, /"customerClientId"/);
    assert.doesNotMatch(written, /AUTHORITY|AUDIENCE|requireCustomerAuth/);
  } finally {
    await rm(directory, { recursive: true, force: true });
  }
});

test("CLI returns failure without echoing supplied metadata", () => {
  const cli = fileURLToPath(new URL("./validate-auth-config.mjs", import.meta.url));
  const result = spawnSync(process.execPath, [cli], { env: { ...complete, FEDRIL_CUSTOMER_AUDIENCE: "never-print-this-value" }, encoding: "utf8" });
  assert.equal(result.status, 1);
  assert.match(result.stderr, /FEDRIL_CUSTOMER_AUDIENCE/);
  assert.doesNotMatch(result.stderr, /never-print-this-value/);
});
