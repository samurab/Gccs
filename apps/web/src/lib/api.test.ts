import { afterEach, describe, expect, it, vi } from "vitest";
import { getCurrentUserAccess } from "./api";

describe("GCCS API client", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
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
});
