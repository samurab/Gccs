export type RuntimeConfig = {
  apiBaseUrl?: string;
  workforceClientId?: string;
  workforceTenantId?: string;
  workforceApiScope?: string;
  customerClientId?: string;
  customerTenantId?: string;
  customerTenantSubdomain?: string;
  customerApiScope?: string;
  accessTokenStorageKey?: string;
  appVersion?: string;
  candidateTag?: string;
  releaseTag?: string;
  commitSha?: string;
  buildId?: string;
};

declare global {
  interface Window {
    __FEDRIL_CONFIG__?: RuntimeConfig;
  }
}

function runtimeConfig(): RuntimeConfig {
  return typeof window === "undefined" ? {} : window.__FEDRIL_CONFIG__ ?? {};
}

function nonEmpty(value: string | undefined): string | undefined {
  const normalized = value?.trim();
  return normalized ? normalized : undefined;
}

export function getApiBaseUrl(): string {
  const configured = nonEmpty(runtimeConfig().apiBaseUrl) ?? nonEmpty(import.meta.env.VITE_API_BASE_URL);
  if (configured) {
    return configured.replace(/\/$/, "");
  }
  if (import.meta.env.DEV || import.meta.env.MODE === "test") {
    return "http://localhost:5062";
  }
  throw new Error("FeDril runtime configuration is missing apiBaseUrl.");
}

export function getAuthenticationRuntimeConfig() {
  const config = runtimeConfig();
  return {
    workforceClientId: nonEmpty(config.workforceClientId) ?? nonEmpty(import.meta.env.VITE_MSAL_CLIENT_ID),
    workforceTenantId: nonEmpty(config.workforceTenantId) ?? nonEmpty(import.meta.env.VITE_MSAL_TENANT_ID),
    workforceApiScope: nonEmpty(config.workforceApiScope) ?? nonEmpty(import.meta.env.VITE_MSAL_API_SCOPE),
    customerClientId: nonEmpty(config.customerClientId) ?? nonEmpty(import.meta.env.VITE_CUSTOMER_MSAL_CLIENT_ID),
    customerTenantId: nonEmpty(config.customerTenantId) ?? nonEmpty(import.meta.env.VITE_CUSTOMER_MSAL_TENANT_ID),
    customerTenantSubdomain: nonEmpty(config.customerTenantSubdomain)
      ?? nonEmpty(import.meta.env.VITE_CUSTOMER_MSAL_TENANT_SUBDOMAIN),
    customerApiScope: nonEmpty(config.customerApiScope) ?? nonEmpty(import.meta.env.VITE_CUSTOMER_MSAL_API_SCOPE),
    accessTokenStorageKey: nonEmpty(config.accessTokenStorageKey)
      ?? nonEmpty(import.meta.env.VITE_GCCS_ACCESS_TOKEN_STORAGE_KEY)
      ?? "gccs.accessToken"
  };
}

export function getRuntimeReleaseInfo() {
  const config = runtimeConfig();
  return {
    version: nonEmpty(config.appVersion) ?? "development",
    candidateTag: nonEmpty(config.candidateTag),
    releaseTag: nonEmpty(config.releaseTag),
    commitSha: nonEmpty(config.commitSha),
    buildId: nonEmpty(config.buildId)
  };
}
