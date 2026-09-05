import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { getFreshAccessToken } from "../authSession";
import {
  archiveReport,
  correctPlatformOwnerInvitation,
  getCurrentUserAccess,
  getEvidencePackage,
  getPlatformAccess,
  getRecentReports,
  getReportArtifact,
  revokeTenantInvitation,
  saveCompanyProfile,
  restoreReport,
  selectDevelopmentInvitationIdentity,
  selectDevelopmentTestingContext
} from "./api";

vi.mock("../authSession", () => ({
  getFreshAccessToken: vi.fn(),
  isMsalConfigured: false
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

  it("sends the selected local tenant, persona, and role through development authentication headers", async () => {
    vi.stubEnv("DEV", true);
    selectDevelopmentTestingContext(
      "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2",
      "Auditor",
      "cccccccc-cccc-cccc-cccc-ccccccccccc1",
      "auditor@example.com"
    );
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
          "X-Gccs-Dev-Email": "auditor@example.com",
          "X-Gccs-Dev-Role": "Auditor",
          "X-Gccs-Dev-User": "cccccccc-cccc-cccc-cccc-ccccccccccc1",
          "X-Gccs-Dev-Tenant": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2",
          "X-Gccs-Tenant": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"
        })
      }
    );
  });

  it("adds platform permissions only when explicitly configured for local development", async () => {
    vi.stubEnv("DEV", true);
    vi.stubEnv("VITE_GCCS_DEV_PLATFORM_PERMISSIONS", "ManageDemoRequests");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({
        userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
        userEmail: "operator@example.com",
        canProvisionTenants: false,
        canManageDemoRequests: true,
        permissions: ["ManageDemoRequests"]
      })
    });
    vi.stubGlobal("fetch", fetchMock);

    await getPlatformAccess();

    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:5062/api/platform/me/access",
      { headers: expect.objectContaining({ "X-Gccs-Dev-Platform-Permissions": "ManageDemoRequests" }) }
    );
  });

  it("does not grant platform permissions to every local development persona by default", async () => {
    vi.stubEnv("DEV", true);
    vi.stubEnv("VITE_GCCS_DEV_PLATFORM_PERMISSIONS", "");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({
        userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
        userEmail: "operator@example.com",
        canProvisionTenants: false,
        canManageDemoRequests: false,
        permissions: []
      })
    });
    vi.stubGlobal("fetch", fetchMock);

    await getPlatformAccess();

    const headers = fetchMock.mock.calls[0]?.[1]?.headers as Record<string, string>;
    expect(headers).not.toHaveProperty("X-Gccs-Dev-Platform-Permissions");
  });

  it("sends the expected invitation id when correcting a pending Owner invitation", async () => {
    vi.stubEnv("DEV", true);
    const fetchMock = vi.fn().mockResolvedValue({ ok: true, json: async () => ({ invitationId: "replacement" }) });
    vi.stubGlobal("fetch", fetchMock);

    await correctPlatformOwnerInvitation(
      "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      "samurab@sierralabsllc.com",
      "Correct the designated Owner before activation."
    );

    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:5062/api/platform/tenant-onboardings/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/owner-invitation/correct",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({
          expectedInvitationId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          newOwnerEmail: "samurab@sierralabsllc.com",
          reason: "Correct the designated Owner before activation."
        })
      })
    );
  });

  it("uses an invited development email without reusing the selected persona user id", async () => {
    vi.stubEnv("DEV", true);
    selectDevelopmentTestingContext(
      "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2",
      "Owner",
      "cccccccc-cccc-cccc-cccc-ccccccccccc1",
      "owner@example.com"
    );
    selectDevelopmentInvitationIdentity("  Invitee@Example.com ");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({
        tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2",
        userId: "dddddddd-dddd-dddd-dddd-ddddddddddd1",
        userEmail: "invitee@example.com",
        roles: ["Owner"],
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
          "X-Gccs-Dev-Email": "invitee@example.com"
        })
      }
    );
    const headers = fetchMock.mock.calls[0]?.[1]?.headers as Record<string, string>;
    expect(headers).not.toHaveProperty("X-Gccs-Dev-User");
  });

  it("uses only explicit server permissions for authorization gating", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
          userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
          userEmail: "admin@example.com",
          roles: ["Admin"],
          permissions: ["ViewReports"],
          rolePermissionMatrix: {
            Admin: ["ManageContracts", "ViewContracts", "ViewReports"]
          }
        })
      })
    );

    const access = await getCurrentUserAccess();

    expect(access.permissions).toEqual(["ViewReports"]);
  });

  it("fails closed when the API omits explicit permissions and the role matrix", async () => {
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

    expect(access.permissions).toEqual([]);
  });

  it("does not derive permissions from role names", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
          userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
          userEmail: "admin@example.com",
          roles: ["admin"],
          permissions: ["ViewReports"],
          rolePermissionMatrix: {
            Admin: ["ManageContracts", "ViewContracts", "ViewReports"]
          }
        })
      })
    );

    const access = await getCurrentUserAccess();

    expect(access.permissions).toEqual(["ViewReports"]);
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

  it("preserves field-keyed company profile validation errors", async () => {
    const errors = {
      uei: ["UEI is required before profile completion."],
      cageCode: ["CAGE code is required before profile completion."],
      samRegistrationExpiresAt: ["SAM expiration date is required before profile completion."]
    };
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: false,
        json: async () => ({
          title: "Company profile incomplete",
          detail: "Company profile is missing required completion fields.",
          status: 400,
          correlationId: "profile-validation-correlation",
          errors
        })
      })
    );

    const result = await saveCompanyProfile({} as Parameters<typeof saveCompanyProfile>[0]);

    expect(result.data).toBeNull();
    expect(result.errors).toEqual(errors);
    expect(result.errorSummary).toBe(
      "Company profile is missing required completion fields. Correlation ID: profile-validation-correlation."
    );
    expect(result.error).toContain("uei: UEI is required before profile completion.");
    expect(result.error).toContain("cageCode: CAGE code is required before profile completion.");
    expect(result.error).toContain(
      "samRegistrationExpiresAt: SAM expiration date is required before profile completion."
    );
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

  it("posts pending invitation revocation to the tenant-scoped endpoint", async () => {
    vi.stubEnv("DEV", true);
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ invitationId: "dddddddd-dddd-dddd-dddd-ddddddddddd1", status: "Revoked" })
    });
    vi.stubGlobal("fetch", fetchMock);

    const result = await revokeTenantInvitation(
      "dddddddd-dddd-dddd-dddd-ddddddddddd1",
      { reason: "Duplicate invitation." }
    );

    expect(result.data).toEqual(expect.objectContaining({ status: "Revoked" }));
    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:5062/api/tenant-invitations/dddddddd-dddd-dddd-dddd-ddddddddddd1/revoke",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ reason: "Duplicate invitation." })
      })
    );
  });

  it("loads a persisted evidence package through the tenant-scoped report detail endpoint", async () => {
    vi.stubEnv("DEV", true);
    selectDevelopmentTestingContext(
      "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2",
      "Auditor",
      "cccccccc-cccc-cccc-cccc-ccccccccccc1",
      "auditor@example.com"
    );
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({
        id: "33333333-3333-3333-3333-333333333332",
        tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2",
        type: "PrimeEvidencePackage",
        status: "Complete",
        title: "Prime review evidence package",
        disclaimer: "Workflow guidance only.",
        manifest: { scope: {}, items: [] }
      })
    });
    vi.stubGlobal("fetch", fetchMock);

    const report = await getEvidencePackage("33333333-3333-3333-3333-333333333332");

    expect(report.title).toBe("Prime review evidence package");
    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:5062/api/reports/evidence-packages/33333333-3333-3333-3333-333333333332",
      {
        headers: expect.objectContaining({
          "X-Gccs-Dev-Tenant": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2",
          "X-Gccs-Tenant": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"
        })
      }
    );
  });

  it("loads bounded report history and structured report detail with the selected tenant headers", async () => {
    vi.stubEnv("DEV", true);
    selectDevelopmentTestingContext(
      "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2",
      "Compliance Manager",
      "cccccccc-cccc-cccc-cccc-ccccccccccc1",
      "manager@example.com"
    );
    const reportId = "33333333-3333-3333-3333-333333333339";
    const report = {
      id: reportId,
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2",
      type: "ComplianceStatus",
      status: "Complete",
      title: "Compliance status report",
      generatedAt: "2026-07-27T21:00:00Z",
      generatedByUserId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
      disclaimer: "Workflow guidance only."
    };
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce({ ok: true, json: async () => [report] })
      .mockResolvedValueOnce({ ok: true, json: async () => ({ ...report, snapshot: { totalObligations: 1 } }) });
    vi.stubGlobal("fetch", fetchMock);

    const history = await getRecentReports();
    const detail = await getReportArtifact(reportId);

    expect(history).toEqual([report]);
    expect(detail.snapshot.totalObligations).toBe(1);
    expect(fetchMock).toHaveBeenNthCalledWith(
      1,
      "http://localhost:5062/api/reports/recent?limit=25",
      {
        headers: expect.objectContaining({
          "X-Gccs-Dev-Tenant": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2",
          "X-Gccs-Tenant": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"
        })
      }
    );
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      `http://localhost:5062/api/reports/${reportId}`,
      {
        headers: expect.objectContaining({
          "X-Gccs-Dev-Tenant": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2",
          "X-Gccs-Tenant": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"
        })
      }
    );
  });

  it("posts explicit reasons to report archive and restore lifecycle endpoints", async () => {
    vi.stubEnv("DEV", true);
    const reportId = "33333333-3333-3333-3333-333333333339";
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce({ ok: true, json: async () => ({ id: reportId, status: "Archived" }) })
      .mockResolvedValueOnce({ ok: true, json: async () => ({ id: reportId, status: "Complete" }) });
    vi.stubGlobal("fetch", fetchMock);

    await archiveReport(reportId, "Superseded snapshot.");
    await restoreReport(reportId, "Original remains authoritative.");

    expect(fetchMock).toHaveBeenNthCalledWith(
      1,
      `http://localhost:5062/api/reports/${reportId}/archive`,
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ reason: "Superseded snapshot." })
      })
    );
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      `http://localhost:5062/api/reports/${reportId}/restore`,
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ reason: "Original remains authoritative." })
      })
    );
  });
});
