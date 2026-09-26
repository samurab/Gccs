import { mkdir, writeFile } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { validateAuthConfig } from "./validate-auth-config.mjs";

const output = process.argv[2];
if (!output) {
  console.error("Usage: write-runtime-config.mjs <output-path>");
  process.exit(2);
}

validateAuthConfig();

const config = {
  apiBaseUrl: requiredUrl("FEDRIL_API_BASE_URL"),
  workforceClientId: required("FEDRIL_WORKFORCE_CLIENT_ID"),
  workforceTenantId: required("FEDRIL_WORKFORCE_TENANT_ID"),
  workforceApiScope: required("FEDRIL_WORKFORCE_API_SCOPE"),
  customerClientId: optional("FEDRIL_CUSTOMER_CLIENT_ID"),
  customerTenantId: optional("FEDRIL_CUSTOMER_TENANT_ID"),
  customerTenantSubdomain: optional("FEDRIL_CUSTOMER_TENANT_SUBDOMAIN"),
  customerApiScope: optional("FEDRIL_CUSTOMER_API_SCOPE"),
  appVersion: required("FEDRIL_APP_VERSION"),
  candidateTag: optional("FEDRIL_CANDIDATE_TAG"),
  releaseTag: optional("FEDRIL_RELEASE_TAG"),
  commitSha: requiredSha("FEDRIL_COMMIT_SHA"),
  buildId: required("FEDRIL_BUILD_ID")
};

const target = resolve(output);
await mkdir(dirname(target), { recursive: true });
await writeFile(target, `window.__FEDRIL_CONFIG__ = Object.freeze(${JSON.stringify(config)});\n`, "utf8");

function required(name) {
  const value = optional(name);
  if (!value) throw new Error(`${name} is required`);
  return value;
}

function optional(name) {
  const value = process.env[name]?.trim();
  return value || undefined;
}

function requiredUrl(name) {
  const value = required(name);
  const url = new URL(value);
  if (url.protocol !== "https:" && url.hostname !== "localhost" && url.hostname !== "127.0.0.1") {
    throw new Error(`${name} must use HTTPS outside local development`);
  }
  return value.replace(/\/$/, "");
}

function requiredSha(name) {
  const value = required(name);
  if (!/^[0-9a-f]{40}$/.test(value)) throw new Error(`${name} must be a lowercase 40-character Git SHA`);
  return value;
}
