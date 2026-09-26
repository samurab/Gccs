import { resolve } from "node:path";
import { fileURLToPath } from "node:url";

// Validate deployment metadata only. This does not establish that an identity
// exists, has consent, or can authenticate against the configured API.
export function validateAuthConfig(env = process.env) {
  const value = name => env[name]?.trim() || undefined;
  const required = name => {
    const result = value(name);
    if (!result) throw new Error(`${name} is required`);
    return result;
  };
  const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
  const identifier = name => {
    const result = required(name);
    if (!guid.test(result)) throw new Error(`${name} must be a GUID`);
    return result;
  };
  const scope = name => {
    const result = required(name);
    const match = /^api:\/\/([^/]+)\/([A-Za-z0-9_.-]+)$/.exec(result);
    if (!match || !guid.test(match[1])) throw new Error(`${name} must be an api://<GUID>/<scope> delegated scope`);
    return `api://${match[1]}`;
  };
  identifier("FEDRIL_WORKFORCE_CLIENT_ID");
  identifier("FEDRIL_WORKFORCE_TENANT_ID");
  scope("FEDRIL_WORKFORCE_API_SCOPE");

  const requireCustomer = value("FEDRIL_REQUIRE_CUSTOMER_AUTH");
  if (requireCustomer && !["true", "false"].includes(requireCustomer)) {
    throw new Error("FEDRIL_REQUIRE_CUSTOMER_AUTH must be true or false");
  }
  const customerFields = ["CLIENT_ID", "TENANT_ID", "TENANT_SUBDOMAIN", "API_SCOPE"];
  const hasCustomer = customerFields.some(field => value(`FEDRIL_CUSTOMER_${field}`));
  const authority = value("FEDRIL_CUSTOMER_AUTHORITY");
  const audience = value("FEDRIL_CUSTOMER_AUDIENCE");
  if (!hasCustomer && !authority && !audience && requireCustomer !== "true") return;

  identifier("FEDRIL_CUSTOMER_CLIENT_ID");
  const tenant = identifier("FEDRIL_CUSTOMER_TENANT_ID");
  const subdomain = required("FEDRIL_CUSTOMER_TENANT_SUBDOMAIN");
  if (!/^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$/i.test(subdomain)) {
    throw new Error("FEDRIL_CUSTOMER_TENANT_SUBDOMAIN must be a DNS label");
  }
  const scopeAudience = scope("FEDRIL_CUSTOMER_API_SCOPE");
  if (requireCustomer === "true" || authority || audience) {
    const expectedAuthority = `https://${subdomain}.ciamlogin.com/${tenant}/v2.0`;
    if (required("FEDRIL_CUSTOMER_AUTHORITY").replace(/\/$/, "").toLowerCase() !== expectedAuthority.toLowerCase()) {
      throw new Error("FEDRIL_CUSTOMER_AUTHORITY must match the customer tenant and subdomain");
    }
    if (required("FEDRIL_CUSTOMER_AUDIENCE").toLowerCase() !== scopeAudience.toLowerCase()) {
      throw new Error("FEDRIL_CUSTOMER_AUDIENCE must match the customer API scope resource");
    }
  }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    validateAuthConfig();
    console.log("Deployment authentication configuration is consistent.");
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}
