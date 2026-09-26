import { execFileSync } from "node:child_process";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { validateAuthConfig } from "./validate-auth-config.mjs";

export function validateRollbackAuth(env = process.env, execute = execFileSync) {
  validateAuthConfig({ ...env, FEDRIL_REQUIRE_CUSTOMER_AUTH: "false" });
  const resourceGroup = env.PRODUCTION_RESOURCE_GROUP?.trim();
  const app = env.PRODUCTION_API_APP_NAME?.trim();
  if (!resourceGroup || !app) throw new Error("Production resource group and API app are required for rollback validation");
  let settings;
  try {
    settings = JSON.parse(execute("az", ["webapp", "config", "appsettings", "list", "--resource-group", resourceGroup,
      "--name", app, "--query", "[?name=='Authentication__Customer__Authority' || name=='Authentication__Customer__Audience'].{name:name,value:value}", "--output", "json"],
    { encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] }));
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
  const requested = ["CLIENT_ID", "TENANT_ID", "TENANT_SUBDOMAIN", "API_SCOPE", "AUTHORITY", "AUDIENCE"]
    .some(key => env[`FEDRIL_CUSTOMER_${key}`]?.trim());
  if (!authority && !audience) {
    if (requested) throw new Error("Rollback cannot introduce a new customer authentication realm");
    return;
  }
  if (!authority || !audience) throw new Error("Live customer authentication is incomplete; rollback stopped");
  validateAuthConfig({ ...env, FEDRIL_REQUIRE_CUSTOMER_AUTH: "true" });
  const normalizeAuthority = value => value.trim().replace(/\/$/, "").toLowerCase();
  if (normalizeAuthority(authority) !== normalizeAuthority(env.FEDRIL_CUSTOMER_AUTHORITY) ||
      audience.toLowerCase() !== env.FEDRIL_CUSTOMER_AUDIENCE.trim().toLowerCase()) {
    throw new Error("Rollback customer authentication differs from the live realm; rollback stopped");
  }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    validateRollbackAuth();
    console.log("Rollback authentication configuration preserves the live realm.");
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}
