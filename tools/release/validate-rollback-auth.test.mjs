import test from "node:test";
import assert from "node:assert/strict";
import { validateRollbackAuth } from "./validate-rollback-auth.mjs";

const id = "11111111-2222-3333-4444-555555555555";
const workforce = {
  PRODUCTION_RESOURCE_GROUP: "production-rg", PRODUCTION_API_APP_NAME: "production-api",
  FEDRIL_WORKFORCE_CLIENT_ID: id, FEDRIL_WORKFORCE_TENANT_ID: id,
  FEDRIL_WORKFORCE_API_SCOPE: `api://${id}/access_as_user`
};
const customer = {
  ...workforce, FEDRIL_CUSTOMER_CLIENT_ID: id, FEDRIL_CUSTOMER_TENANT_ID: id,
  FEDRIL_CUSTOMER_TENANT_SUBDOMAIN: "customers", FEDRIL_CUSTOMER_API_SCOPE: `api://${id}/access_as_customer`,
  FEDRIL_CUSTOMER_AUTHORITY: `https://customers.ciamlogin.com/${id}/v2.0`, FEDRIL_CUSTOMER_AUDIENCE: `api://${id}`
};
const settings = [
  { name: "Authentication__Customer__Authority", value: customer.FEDRIL_CUSTOMER_AUTHORITY },
  { name: "Authentication__Customer__Audience", value: customer.FEDRIL_CUSTOMER_AUDIENCE }
];
const azure = values => (command, args, options) => {
  assert.equal(command, "az");
  assert.deepEqual(args.slice(0, 4), ["webapp", "config", "appsettings", "list"]);
  assert.ok(args.includes("production-api"));
  assert.match(args[args.indexOf("--query") + 1], /Authentication__Customer__Authority/);
  assert.deepEqual(options.stdio, ["ignore", "pipe", "pipe"]);
  return JSON.stringify(values);
};

test("workforce-only rollback remains available when live customer realm is absent", () => {
  assert.doesNotThrow(() => validateRollbackAuth(workforce, azure([])));
});
test("rollback preserves a matching live customer realm", () => {
  assert.doesNotThrow(() => validateRollbackAuth(customer, azure(settings)));
});
test("rollback cannot remove a live customer realm", () => {
  assert.throws(() => validateRollbackAuth(workforce, azure(settings)), /required/);
});
test("rollback cannot introduce a customer realm", () => {
  assert.throws(() => validateRollbackAuth(customer, azure([])), /cannot introduce/);
});
test("rollback cannot replace a live customer realm", () => {
  assert.throws(() => validateRollbackAuth(customer, azure(settings.map(item => item.name.endsWith("Authority") ? { ...item, value: item.value.replace("customers.", "different.") } : item))), /differs/);
});
test("rollback rejects partial desired and live authentication", () => {
  assert.throws(() => validateRollbackAuth({ ...workforce, FEDRIL_CUSTOMER_CLIENT_ID: id }, azure([])), /required/);
  assert.throws(() => validateRollbackAuth(customer, azure(settings.slice(0, 1))), /incomplete/);
});
test("provider failures are sanitized and stop rollback", () => {
  assert.throws(() => validateRollbackAuth(workforce, () => { throw new Error("sensitive-provider-output"); }), error => /Unable to verify/.test(error.message) && !error.message.includes("sensitive"));
  assert.throws(() => validateRollbackAuth(workforce, () => "not-json"), /Unable to verify/);
  assert.throws(() => validateRollbackAuth(workforce, azure({ error: "provider failure" })), /Unable to verify/);
});
test("ambiguous live values stop rollback", () => {
  assert.throws(() => validateRollbackAuth(customer, azure([...settings, settings[0]])), /Ambiguous/);
});
