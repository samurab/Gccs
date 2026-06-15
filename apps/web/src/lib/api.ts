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

export type NoCuiAcknowledgementStatus = {
  isAcknowledged: boolean;
  noticeVersion: string;
  noticeCopy: string;
  tenantId: string | null;
  acknowledgedByUserId: string | null;
  acknowledgedAt: string | null;
};

export type EvidenceUploadIntent = {
  id: string;
  evidenceItemId: string;
  tenantId: string;
  createdByUserId: string;
  status: string;
  message: string;
  noticeVersion: string;
  expiresAt: string;
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

export const fallbackNoCuiAcknowledgementStatus: NoCuiAcknowledgementStatus = {
  isAcknowledged: false,
  noticeVersion: "no-cui-mvp-v1",
  noticeCopy:
    "The GCCS MVP is compliance management only and is not ready to store CUI. Do not upload CUI, classified information, ITAR/export-controlled technical data, SSNs, payroll, bank or tax details, protected medical or disability data, passwords, secrets, private keys, unrestricted security logs, or other prohibited sensitive content.",
  tenantId: null,
  acknowledgedByUserId: null,
  acknowledgedAt: null
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

export async function getNoCuiAcknowledgementStatus(): Promise<NoCuiAcknowledgementStatus> {
  return getJson<NoCuiAcknowledgementStatus>("/api/no-cui-acknowledgement", fallbackNoCuiAcknowledgementStatus);
}

export async function acknowledgeNoCuiNotice(noticeVersion: string): Promise<NoCuiAcknowledgementStatus | null> {
  const response = await postJson<NoCuiAcknowledgementStatus>("/api/no-cui-acknowledgement", {
    acknowledged: true,
    noticeVersion
  });

  return response;
}

export async function createEvidenceUploadIntent(fileName: string): Promise<EvidenceUploadIntent | null> {
  const placeholderEvidenceItemId = "00000000-0000-0000-0000-000000000041";
  return postJson<EvidenceUploadIntent>(`/api/evidence-items/${placeholderEvidenceItemId}/upload-intents`, { fileName });
}

export async function createTenantInvitation(request: CreateTenantInvitationRequest): Promise<TenantInvitation | null> {
  return postJson<TenantInvitation>("/api/tenant-invitations", request);
}

async function postJson<T>(path: string, body: unknown): Promise<T | null> {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5062";

  try {
    const response = await fetch(`${apiBaseUrl}${path}`, {
      method: "POST",
      headers: {
        ...(getDevelopmentHeaders() ?? {}),
        "Content-Type": "application/json"
      },
      body: JSON.stringify(body)
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
