import { afterEach, describe, expect, it, vi } from "vitest";
import { getApiBaseUrl, getAuthenticationRuntimeConfig, getRuntimeReleaseInfo } from "./runtimeConfig";

describe("runtime configuration", () => {
  afterEach(() => {
    delete window.__FEDRIL_CONFIG__;
    vi.unstubAllEnvs();
  });

  it("prefers the deployment-time configuration over build-time values", () => {
    vi.stubEnv("VITE_API_BASE_URL", "https://build.example.test");
    window.__FEDRIL_CONFIG__ = {
      apiBaseUrl: "https://runtime.example.test",
      workforceClientId: "runtime-workforce",
      appVersion: "1.2.3",
      commitSha: "0123456789012345678901234567890123456789"
    };

    expect(getApiBaseUrl()).toBe("https://runtime.example.test");
    expect(getAuthenticationRuntimeConfig().workforceClientId).toBe("runtime-workforce");
    expect(getRuntimeReleaseInfo()).toMatchObject({
      version: "1.2.3",
      commitSha: "0123456789012345678901234567890123456789"
    });
  });

  it("preserves local build-time defaults when no runtime overlay exists", () => {
    vi.stubEnv("VITE_API_BASE_URL", "http://localhost:5062");
    vi.stubEnv("VITE_MSAL_CLIENT_ID", "local-workforce");

    expect(getApiBaseUrl()).toBe("http://localhost:5062");
    expect(getAuthenticationRuntimeConfig().workforceClientId).toBe("local-workforce");
    expect(getRuntimeReleaseInfo().version).toBe("development");
  });
});
