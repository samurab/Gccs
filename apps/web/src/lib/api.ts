export type ModuleStatus = {
  key: string;
  name: string;
  purpose: string;
  status: string;
};

export type ObligationSummary = {
  id: string;
  source: string;
  title: string;
  ownerFunction: string;
  riskLevel: string;
  sourceUrl: string;
  lastReviewedAt: string;
};

export type ComplianceOverview = {
  productPromise: string;
  mvpDataPosture: string;
  modules: ModuleStatus[];
  priorityObligations: ObligationSummary[];
};

export type CurrentUserAccess = {
  tenantId: string | null;
  userId: string | null;
  userEmail: string | null;
  roles: string[];
  permissions: string[];
  rolePermissionMatrix: Record<string, string[]>;
};

export type TenantMember = {
  membershipId: string;
  tenantId: string;
  userId: string;
  email: string;
  displayName: string;
  userStatus: string;
  membershipStatus: string;
  roleName: string;
  mfaEnabled: boolean;
  lastSignedInAt: string | null;
  lastAccessedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type TenantInvitation = {
  invitationId: string;
  tenantId: string;
  email: string;
  roleName: string;
  invitationToken: string;
  status: "Pending" | "Accepted" | "Expired" | "Revoked" | string;
  expiresAt: string;
  acceptedAt: string | null;
  acceptedByUserId: string | null;
  revokedAt: string | null;
  revokedByUserId: string | null;
  notificationSentAt: string | null;
  notificationPlaceholder: string;
  createdAt: string;
  updatedAt: string | null;
};

export type CreateTenantInvitationRequest = {
  email: string;
  roleName: string;
  expiresInDays: number;
};

export const fallbackOverview: ComplianceOverview = {
  productPromise:
    "Connect to the GCCS API to load source-backed modules, obligations, review metadata, and tenant-scoped compliance workflow state.",
  mvpDataPosture: "No-CUI / compliance management only",
  modules: [],
  priorityObligations: []
};

export const fallbackAccess: CurrentUserAccess = {
  tenantId: null,
  userId: null,
  userEmail: null,
  roles: [],
  permissions: [],
  rolePermissionMatrix: {}
};

export async function getComplianceOverview(): Promise<ComplianceOverview> {
  return getJson<ComplianceOverview>("/api/compliance/overview", fallbackOverview);
}

export async function getCurrentUserAccess(): Promise<CurrentUserAccess> {
  return getJson<CurrentUserAccess>("/api/me/access", fallbackAccess);
}

export async function getTenantMembers(): Promise<TenantMember[]> {
  return getJson<TenantMember[]>("/api/tenant-members", []);
}

export async function getTenantInvitations(): Promise<TenantInvitation[]> {
  return getJson<TenantInvitation[]>("/api/tenant-invitations", []);
}

export async function createTenantInvitation(request: CreateTenantInvitationRequest): Promise<TenantInvitation | null> {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5062";

  try {
    const response = await fetch(`${apiBaseUrl}/api/tenant-invitations`, {
      method: "POST",
      headers: {
        ...(getDevelopmentHeaders() ?? {}),
        "Content-Type": "application/json"
      },
      body: JSON.stringify(request)
    });

    if (!response.ok) {
      return null;
    }

    return response.json();
  } catch {
    return null;
  }
}

async function getJson<T>(path: string, fallback: T): Promise<T> {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5062";

  try {
    const response = await fetch(`${apiBaseUrl}${path}`, { headers: getDevelopmentHeaders() });

    if (!response.ok) {
      return fallback;
    }

    return response.json();
  } catch {
    return fallback;
  }
}

function getDevelopmentHeaders(): HeadersInit | undefined {
  const role = import.meta.env.VITE_GCCS_DEV_ROLE;

  return import.meta.env.DEV
    ? {
        "X-Gccs-Dev-Auth": "true",
        ...(role ? { "X-Gccs-Dev-Role": role } : {})
      }
    : undefined;
}
