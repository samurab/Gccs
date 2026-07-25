import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { getFreshAccessToken } from "../auth";
import { getCurrentUserAccess, selectDevelopmentTestingContext } from "./api";

vi.mock("../auth", () => ({
  getFreshAccessToken: vi.fn()
}));

describe("FeDril API client", () => {
  beforeEach(() => {
    const values = new Map<string, string>();
    Object.defineProperty(window, "localStorage", {
      configurable: true,
      value: {
        clear: () => values.clear(),
        getItem: (key: string) => values.get(key) ?? null,
        removeItem: (key: string) => values.delete(key),
        setItem: (key: string, value: string) => values.set(key, value)
      }
    });
  });

  afterEach(() => {
    window.localStorage.clear();
    vi.unstubAllGlobals();
    vi.unstubAllEnvs();
    vi.mocked(getFreshAccessToken).mockReset();
  });

  it("sends the selected local tenant and role through development authentication headers", async () => {
    vi.stubEnv("DEV", true);
    selectDevelopmentTestingContext("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2", "Auditor");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({
        tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2",
        userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
        userEmail: "auditor@example.com",
        roles: ["Auditor"],
        permissions: [],
        rolePermissionMatrix: {}
      })
    });
    vi.stubGlobal("fetch", fetchMock);

    await getCurrentUserAccess();

    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:5062/api/me/access",
      {
        headers: expect.objectContaining({
          "X-Gccs-Dev-Auth": "true",
          "X-Gccs-Dev-Role": "Auditor",
          "X-Gccs-Dev-Tenant": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2",
          "X-Gccs-Tenant": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"
        })
      }
    );
  });

  it("derives effective permissions from returned roles and role matrix", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
          userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
          userEmail: "admin@example.com",
          roles: ["Admin"],
          permissions: [],
          rolePermissionMatrix: {
            Admin: ["ManageContracts", "ViewContracts", "ViewReports"]
          }
        })
      })
    );

    const access = await getCurrentUserAccess();

    expect(access.permissions).toEqual(["ManageContracts", "ViewContracts", "ViewReports"]);
  });

  it("derives role permissions when the API omits the role matrix", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
          userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
          userEmail: "admin@example.com",
          roles: ["Admin"],
          permissions: [],
          rolePermissionMatrix: {}
        })
      })
    );

    const access = await getCurrentUserAccess();

    expect(access.permissions).toContain("ViewContracts");
    expect(access.permissions).toContain("ManageContracts");
    expect(access.permissions).toContain("ViewEvidence");
    expect(access.permissions).toContain("ViewReports");
  });

  it("matches returned role names case-insensitively", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
          userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
          userEmail: "admin@example.com",
          roles: ["admin"],
          permissions: [],
          rolePermissionMatrix: {
            Admin: ["ManageContracts", "ViewContracts", "ViewReports"]
          }
        })
      })
    );

    const access = await getCurrentUserAccess();

    expect(access.permissions).toEqual(["ManageContracts", "ViewContracts", "ViewReports"]);
  });

  it("does not derive permissions for unknown role names", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
          userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
          userEmail: "unknown@example.com",
          roles: ["Unknown"],
          permissions: [],
          rolePermissionMatrix: {
            Admin: ["ManageContracts", "ViewContracts", "ViewReports"]
          }
        })
      })
    );

    const access = await getCurrentUserAccess();

    expect(access.permissions).toEqual([]);
  });

  it("uses a freshly acquired bearer token for API requests", async () => {
    vi.stubEnv("DEV", false);
    vi.mocked(getFreshAccessToken).mockResolvedValue("fresh-access-token");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({
        tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
        userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
        userEmail: "admin@example.com",
        roles: [],
        permissions: [],
        rolePermissionMatrix: {}
      })
    });
    vi.stubGlobal("fetch", fetchMock);

    await getCurrentUserAccess();

    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:5062/api/me/access",
      { headers: { Authorization: "Bearer fresh-access-token" } }
    );
  });
});
