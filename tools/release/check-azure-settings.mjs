import { execFileSync } from "node:child_process";

// Project public configuration into CLI output. Never print provider errors or values.
// Azure CLI applies --query locally; this does not limit the provider's response.
const environment = process.argv[2];
if (!["staging", "production"].includes(environment)) {
  console.error("Usage: check-azure-settings.mjs staging|production");
  process.exit(2);
}
const prefix = environment.toUpperCase();
const required = (name) => {
  const value = process.env[name]?.trim();
  if (!value) throw new Error(`Required variable ${name} is missing`);
  return value;
};
try {
  const expected = {
    ASPNETCORE_ENVIRONMENT: environment === "production" ? "Production" : "Staging",
    AllowedHosts: new URL(required(`${prefix}_API_BASE_URL`)).hostname,
    Cors__AllowedOrigins__0: new URL(required(`${prefix}_WEB_BASE_URL`)).origin,
    Authentication__Authority: `https://login.microsoftonline.com/${required(`${prefix}_MSAL_TENANT_ID`)}/v2.0`,
    Authentication__Audience: required(`${prefix}_MSAL_API_SCOPE`).replace(/\/[^/]+$/, ""),
    Authentication__Customer__Authority: required(`${prefix}_CUSTOMER_AUTHORITY`),
    Authentication__Customer__Audience: required(`${prefix}_CUSTOMER_AUDIENCE`)
  };
  const corsPrefix = "Cors__AllowedOrigins__";
  const resourceGroup = required(`${prefix}_RESOURCE_GROUP`);
  const appName = required(`${prefix}_API_APP_NAME`);
  const lookup = (query) => {
    try {
      return execFileSync("az", ["webapp", "config", "appsettings", "list", "--resource-group", resourceGroup, "--name", appName, "--query", query, "--output", "json", "--only-show-errors"], { encoding: "utf8", stdio: ["ignore", "pipe", "pipe"], timeout: 30_000 });
    } catch {
      throw new Error("Azure settings lookup failed; inspect authorized provider diagnostics separately.");
    }
  };
  const normalize = (name) => name.replaceAll("__", ":").toLowerCase();
  const expectedKeys = new Map(Object.keys(expected).map((name) => [normalize(name), name]));
  const discover = () => {
    const output = lookup("[].name");
    let names;
    try {
      names = JSON.parse(output);
      if (!Array.isArray(names) || names.some((name) => typeof name !== "string") || new Set(names).size !== names.length)
        throw new Error();
    } catch {
      throw new Error("Azure settings response was malformed; provider output suppressed.");
    }
    // ASP.NET configuration keys are case-insensitive and accept both separators.
    const keys = new Map();
    const seen = new Set();
    for (const name of names) {
      const normalized = normalize(name);
      let canonical = expectedKeys.get(normalized);
      const isCors = normalized === "cors:allowedorigins" || normalized.startsWith("cors:allowedorigins:");
      if (isCors) {
        const match = /^cors:allowedorigins:(\d+)$/.exec(normalized);
        if (!match) throw new Error("Invalid public CORS configuration; values suppressed.");
        canonical = `${corsPrefix}${match[1]}`;
      }
      if (!canonical) continue;
      if (seen.has(canonical)) throw new Error(isCors
        ? "Invalid public CORS configuration; values suppressed."
        : "Ambiguous public Azure setting aliases; values suppressed.");
      seen.add(canonical);
      keys.set(name, canonical);
    }
    return keys;
  };
  const publicKeys = discover();
  const discoveredKeys = new Set(publicKeys.values());
  const missing = Object.keys(expected).filter((name) => !discoveredKeys.has(name));
  if (missing.length) throw new Error(`Public Azure settings mismatch or absent override: ${missing.join(", ")}`);
  const queriedKeys = [...publicKeys.keys()];
  const query = `[?${queriedKeys.map((name) => `name=='${name}'`).join("||")}].{name:name,value:value}`;
  const output = lookup(query);
  let entries;
  try {
    entries = JSON.parse(output);
    if (!Array.isArray(entries) || entries.some((entry) => !entry || typeof entry !== "object" ||
      typeof entry.name !== "string" || typeof entry.value !== "string" ||
      !publicKeys.has(entry.name)) || new Set(entries.map((entry) => publicKeys.get(entry.name))).size !== entries.length) {
      throw new Error();
    }
  } catch {
    throw new Error("Azure settings response was malformed; provider output suppressed.");
  }
  const returned = new Set(entries.map(({ name }) => name));
  const finalKeys = discover();
  if ([...publicKeys.keys()].some((name) => !returned.has(name)) || finalKeys.size !== publicKeys.size ||
    [...finalKeys].some(([name, canonical]) => publicKeys.get(name) !== canonical))
    throw new Error("Public configuration changed or was incomplete during lookup; retry after configuration is stable.");
  // Detect observed key-population drift; separate provider reads are not atomic.
  entries = entries.map(({ name, value }) => ({ name: publicKeys.get(name), value }));
  const origins = entries.filter(({ name }) => name.startsWith(corsPrefix));
  if (origins.some(({ name, value }) => {
    if (!/^Cors__AllowedOrigins__\d+$/.test(name) || !/^https:\/\//i.test(value) ||
      value !== value.trim() || /[\u0000-\u0020\u007f]/.test(value) ||
      /localhost|\*/i.test(value) || /[\/\\?#@]/.test(value.slice(8))) return true;
    try { return !new URL(value).hostname; } catch { return true; }
  })) throw new Error("Invalid public CORS configuration; values suppressed.");
  const actual = Object.fromEntries(entries.map(({ name, value }) => [name, value]));
  const mismatches = Object.keys(expected).filter((name) => actual[name] !== expected[name]);
  if (mismatches.length) throw new Error(`Public Azure settings mismatch or absent override: ${mismatches.join(", ")}`);
  console.log(`${environment}: public Azure settings contract matches (${Object.keys(expected).length} keys); ${origins.length} CORS entries syntax-checked${origins.length > 1 ? "; additional origins require approved baseline review" : ""}.`);
} catch (error) {
  console.error(error.message);
  process.exitCode = 1;
}
