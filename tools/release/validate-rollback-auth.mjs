import { execFileSync } from "node:child_process";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { appendFile } from "node:fs/promises";
import { validateAuthConfig } from "./validate-auth-config.mjs";

const customerFields = {
  customerClientId: "CLIENT_ID", customerTenantId: "TENANT_ID",
  customerTenantSubdomain: "TENANT_SUBDOMAIN", customerApiScope: "API_SCOPE"
};

async function readLiveFrontend(env, request) {
  try {
    const base = new URL(env.PRODUCTION_WEB_BASE_URL);
    if (base.protocol !== "https:" || base.username || base.password || base.search || base.hash || base.pathname !== "/") throw new Error();
    const response = await request(new URL("/runtime-config.js", base), { redirect: "error", signal: AbortSignal.timeout(10000) });
    if (!response.ok || response.redirected || !response.body) throw new Error();
    const reader = response.body.getReader();
    const chunks = [];
    let length = 0;
    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        length += value.byteLength;
        if (length > 65536) throw new Error();
        chunks.push(Buffer.from(value));
      }
    } finally {
      await reader.cancel();
    }
    const wrapper = /^\s*window\.__FEDRIL_CONFIG__\s*=\s*Object\.freeze\((\{[\s\S]*\})\);\s*$/.exec(Buffer.concat(chunks).toString("utf8"));
    if (!wrapper) throw new Error();
    const config = JSON.parse(wrapper[1]);
    for (const key of Object.keys(customerFields)) {
      if (config[key] !== undefined && typeof config[key] !== "string") throw new Error();
    }
    const apiUrl = value => {
      const url = new URL(value);
      if (url.protocol !== "https:" || url.username || url.password || url.search || url.hash) throw new Error();
      return url.href.replace(/\/$/, "");
    };
    if (typeof config.apiBaseUrl !== "string" || apiUrl(config.apiBaseUrl) !== apiUrl(env.PRODUCTION_API_BASE_URL)) throw new Error();
    for (const [key, suffix] of Object.entries({ workforceClientId: "CLIENT_ID", workforceTenantId: "TENANT_ID", workforceApiScope: "API_SCOPE" })) {
      if (typeof config[key] !== "string" || config[key].trim() !== env[`FEDRIL_WORKFORCE_${suffix}`]?.trim()) throw new Error();
    }
    return config;
  } catch {
    throw new Error("Unable to verify live production frontend authentication; rollback stopped");
  }
}

export async function validateRollbackAuth(env = process.env, execute = execFileSync, request = fetch) {
  validateAuthConfig({ ...env, FEDRIL_REQUIRE_CUSTOMER_AUTH: "false" });
  const resourceGroup = env.PRODUCTION_RESOURCE_GROUP?.trim();
  const app = env.PRODUCTION_API_APP_NAME?.trim();
  if (!resourceGroup || !app) throw new Error("Production resource group and API app are required for rollback validation");
  let settings;
  try {
    settings = JSON.parse(execute("az", ["webapp", "config", "appsettings", "list", "--resource-group", resourceGroup,
      "--name", app, "--query", "[?name=='Authentication__Customer__Authority' || name=='Authentication__Customer__Audience'].{name:name,value:value}", "--output", "json"],
    { encoding: "utf8", stdio: ["ignore", "pipe", "pipe"], timeout: 30000 }));
    if (!Array.isArray(settings) || settings.some(item => !item || typeof item.name !== "string" || typeof item.value !== "string")) throw new Error();
  } catch {
    throw new Error("Unable to verify live production customer authentication; rollback stopped");
  }
  const read = name => {
    const matches = settings.filter(item => item.name === name);
    if (matches.length > 1) throw new Error("Ambiguous live customer authentication settings; rollback stopped");
    return matches[0]?.value.trim() || "";
  };
  const authority = read("Authentication__Customer__Authority");
  const audience = read("Authentication__Customer__Audience");
  const frontend = await readLiveFrontend(env, request);
  const frontendCount = Object.keys(customerFields).filter(key => frontend[key]?.trim()).length;
  if (!authority && !audience && frontendCount === 0) {
    return { customerConfigured: false };
  }
  if (!authority || !audience || frontendCount !== 4) throw new Error("Live customer authentication is incomplete or inconsistent; rollback stopped");
  validateAuthConfig({ ...env, FEDRIL_REQUIRE_CUSTOMER_AUTH: "true" });
  const normalizeAuthority = value => value.trim().replace(/\/$/, "").toLowerCase();
  if (normalizeAuthority(authority) !== normalizeAuthority(env.FEDRIL_CUSTOMER_AUTHORITY) ||
      audience.toLowerCase() !== env.FEDRIL_CUSTOMER_AUDIENCE.trim().toLowerCase()) {
    throw new Error("Rollback customer authentication differs from the live realm; rollback stopped");
  }
  for (const [key, suffix] of Object.entries(customerFields)) {
    if (frontend[key].trim() !== env[`FEDRIL_CUSTOMER_${suffix}`].trim()) {
      throw new Error("Rollback frontend authentication differs from the live realm; rollback stopped");
    }
  }
  return { customerConfigured: true };
}

export async function prepareRollbackAuth(env = process.env, execute = execFileSync, request = fetch, append = appendFile) {
  const result = await validateRollbackAuth(env, execute, request);
  if (!result.customerConfigured) {
    if (!env.GITHUB_ENV) throw new Error("GITHUB_ENV is required to preserve workforce-only rollback");
    try {
      await append(env.GITHUB_ENV, ["CLIENT_ID", "TENANT_ID", "TENANT_SUBDOMAIN", "API_SCOPE", "AUTHORITY", "AUDIENCE"]
        .map(suffix => `FEDRIL_CUSTOMER_${suffix}=\n`).join(""), "utf8");
    } catch {
      throw new Error("Unable to set effective rollback authentication configuration; rollback stopped");
    }
  }
  return result;
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    await prepareRollbackAuth();
    console.log("Rollback authentication configuration preserves the live realm.");
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}
