import { beforeEach, describe, expect, it, vi } from "vitest";
import { getFreshAccessToken } from "../authSession";
import { getPlatformAccess } from "./api";

vi.mock("../authSession", () => ({
  getFreshAccessToken: vi.fn(),
  isMsalConfigured: true
}));

describe("FeDril API client with Microsoft Entra configured", () => {
  beforeEach(() => {
    vi.stubEnv("DEV", true);
    vi.stubEnv("VITE_GCCS_DEV_EMAIL", "spoofed@example.com");
    vi.stubEnv("VITE_GCCS_DEV_PLATFORM_PERMISSIONS", "ProvisionTenants");
    vi.mocked(getFreshAccessToken).mockResolvedValue("entra-access-token");
  });

  it("uses the Entra bearer token instead of development identity headers", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({
        userId: "user-id",
        userEmail: "operator@example.com",
        canProvisionTenants: true,
        canManageDemoRequests: true,
        permissions: []
      })
    });
    vi.stubGlobal("fetch", fetchMock);

    await getPlatformAccess();

    const headers = fetchMock.mock.calls[0]?.[1]?.headers as Record<string, string>;
    expect(headers.Authorization).toBe("Bearer entra-access-token");
    expect(headers).not.toHaveProperty("X-Gccs-Dev-Auth");
    expect(headers).not.toHaveProperty("X-Gccs-Dev-Email");
    expect(headers).not.toHaveProperty("X-Gccs-Dev-Platform-Permissions");
  });
});
