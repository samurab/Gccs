import { afterEach, describe, expect, it, vi } from "vitest";
import { getFreshAccessToken } from "../auth";
import { getCurrentUserAccess } from "./api";

vi.mock("../auth", () => ({
  getFreshAccessToken: vi.fn()
}));

describe("GCCS API client", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.mocked(getFreshAccessToken).mockReset();
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
