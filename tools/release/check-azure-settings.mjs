import { execFileSync } from "node:child_process";

// Query only public configuration. Never print provider errors or setting values.
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
  const query = `[?${Object.keys(expected).map((name) => `name=='${name}'`).join("||")}].{name:name,value:value}`;
  const output = execFileSync("az", ["webapp", "config", "appsettings", "list", "--resource-group", required(`${prefix}_RESOURCE_GROUP`), "--name", required(`${prefix}_API_APP_NAME`), "--query", query, "--output", "json", "--only-show-errors"], { encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] });
  let entries;
  try {
    entries = JSON.parse(output);
    if (!Array.isArray(entries) || entries.some((entry) => !entry || typeof entry !== "object" ||
      typeof entry.name !== "string" || typeof entry.value !== "string" ||
      !Object.hasOwn(expected, entry.name)) || new Set(entries.map((entry) => entry.name)).size !== entries.length) {
      throw new Error();
    }
  } catch {
    throw new Error("Azure settings response was malformed; provider output suppressed.");
  }
  const actual = Object.fromEntries(entries.map(({ name, value }) => [name, value]));
  const mismatches = Object.keys(expected).filter((name) => actual[name] !== expected[name]);
  if (mismatches.length) throw new Error(`Public Azure settings mismatch or absent override: ${mismatches.join(", ")}`);
  console.log(`${environment}: public Azure settings contract matches (${Object.keys(expected).length} keys).`);
} catch (error) {
  console.error(error.code || error.status ? "Azure settings lookup failed; inspect authorized provider diagnostics separately." : error.message);
  process.exitCode = 1;
}
