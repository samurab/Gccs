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

export type ClauseLibraryItem = {
  id: string;
  source: string;
  number: string;
  title: string;
  category: string;
  plainEnglishSummary: string;
  sourceUrl: string;
  lastReviewedAt: string;
  isMappable: boolean;
};

export type ClauseSearchParams = {
  query?: string;
  category?: string;
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
  fileName: string;
  contentType: string;
  sizeBytes: number;
  status: string;
  validationStatus: string;
  malwareScanStatus: string;
  message: string;
  noticeVersion: string;
  expiresAt: string;
};

export type EvidenceMetadata = {
  id: string;
  tenantId: string;
  title: string;
  type: string;
  ownerFunction: string;
  status: string;
  effectiveAt: string | null;
  expiresAt: string | null;
  tags: string[];
  description: string;
  obligationIds: string[];
  controlIds: string[];
  contractIds: string[];
  vendorIds: string[];
  subcontractorIds: string[];
  employeeIds: string[];
  reportIds: string[];
  createdAt: string;
  updatedAt: string | null;
};

export type UpsertEvidenceMetadataRequest = Omit<EvidenceMetadata, "id" | "tenantId" | "createdAt" | "updatedAt">;

export type ControlSummary = {
  total: number;
  implemented: number;
  partiallyImplemented: number;
  notStarted: number;
  notApplicable: number;
  needsReview: number;
  completionPercentage: number;
};

export type CmmcAssessment = {
  id: string;
  tenantId: string;
  name: string;
  type: string;
  level: string;
  framework: string;
  status: string;
  startedAt: string;
  completedAt: string | null;
  affirmationDueAt: string | null;
  ownerFunction: string;
  companyProfileId: string | null;
  contractIds: string[];
  controlSummary: ControlSummary;
  openPoamItemCount: number;
  overduePoamItemCount: number;
  createdAt: string;
  updatedAt: string | null;
};

export type UpsertCmmcAssessmentRequest = Omit<
  CmmcAssessment,
  "id" | "tenantId" | "controlSummary" | "openPoamItemCount" | "overduePoamItemCount" | "createdAt" | "updatedAt"
>;

export type CmmcControlStatus = {
  assessmentId: string;
  controlId: string;
  title: string;
  family: string;
  requirement: string;
  assessmentObjective: string;
  sourceName: string;
  sourceUrl: string;
  sourceLastReviewedAt: string;
  sourceConfidence: string;
  status: string;
  result: string;
  evidenceItemIds: string[];
  taskIds: string[];
  assetIds: string[];
  poamItemIds: string[];
  assessedByUserId: string | null;
  assessedAt: string | null;
  notes: string;
};

export type CmmcPoamItem = {
  id: string;
  tenantId: string;
  assessmentId: string;
  controlId: string;
  weakness: string;
  plannedRemediation: string;
  riskLevel: string;
  status: string;
  ownerUserId: string | null;
  ownerFunction: string;
  targetCompletionAt: string;
  completedAt: string | null;
  remediationTaskId: string | null;
  evidenceItemIds: string[];
  isOverdue: boolean;
  createdAt: string;
  updatedAt: string | null;
};

export type UpsertCmmcPoamItemRequest = Omit<CmmcPoamItem, "id" | "tenantId" | "assessmentId" | "isOverdue" | "createdAt" | "updatedAt">;

export type Subcontractor = {
  id: string;
  tenantId: string;
  name: string;
  uei: string | null;
  cageCode: string | null;
  status: string;
  roleDescription: string;
  smallBusinessStatus: string;
  cmmcStatus: string;
  insuranceExpiresAt: string | null;
  ndaStatus: string;
  workshareDescription: string;
  worksharePercentage: number | null;
  hasFciAccess: boolean;
  hasCuiAccess: boolean;
  hasExportControlledAccess: boolean;
  requiredCmmcLevel: string | null;
  contactName: string | null;
  contactEmail: string | null;
  contactPhone: string | null;
  contactTitle: string | null;
  contractIds: string[];
  createdAt: string;
  updatedAt: string | null;
};

export type UpsertSubcontractorRequest = Omit<Subcontractor, "id" | "tenantId" | "createdAt" | "updatedAt">;

export type AuditLogEntry = {
  id: string;
  tenantId: string;
  actorUserId: string | null;
  action: string;
  entityType: string;
  entityId: string;
  occurredAt: string;
  ipAddress: string;
  userAgent: string;
  correlationId: string;
  summary: string;
  metadata: Record<string, string>;
};

export type CompanyNaicsCode = {
  code: string;
  title: string;
  isPrimary: boolean;
  sizeStandard: string | null;
  qualifiesAsSmall: boolean | null;
  lastCheckedAt: string | null;
};

export type CompanyCertification = {
  id: string | null;
  type: string;
  status: string;
  issuer: string;
  effectiveAt: string | null;
  expiresAt: string | null;
  referenceNumber: string | null;
};

export type CompanyLocation = {
  name: string;
  street1: string;
  street2: string | null;
  city: string;
  stateOrProvince: string;
  postalCode: string;
  country: string;
  isPlaceOfPerformance: boolean;
};

export type ItEnvironmentSummary = {
  description: string;
  usesExternalServiceProvider: boolean;
  externalServiceProviderName: string | null;
  keySystems: string[];
};

export type CompanyProfile = {
  id: string;
  tenantId: string;
  legalEntityName: string;
  doingBusinessAs: string | null;
  uei: string | null;
  cageCode: string | null;
  samRegistrationExpiresAt: string | null;
  naicsCodes: CompanyNaicsCode[];
  certifications: CompanyCertification[];
  agencyCustomers: string[];
  contractorRole: string;
  productsAndServices: string;
  employeeRange: string;
  revenueRange: string;
  locations: CompanyLocation[];
  itEnvironment: ItEnvironmentSummary;
  dataHandlingPosture: string;
  completionPercentage: number;
  isComplete: boolean;
  validationErrors: Record<string, string[]>;
  createdAt: string;
  updatedAt: string | null;
};

export type ContractRecord = {
  id: string;
  tenantId: string;
  contractNumber: string;
  title: string;
  agencyOrPrimeName: string;
  relationship: string;
  kind: string;
  status: string;
  awardedAt: string | null;
  periodOfPerformanceStart: string;
  periodOfPerformanceEnd: string;
  placeOfPerformance: string;
  description: string;
  dataHandlingPosture: string;
  createdAt: string;
  updatedAt: string | null;
};

export type UpsertContractRequest = Omit<ContractRecord, "id" | "tenantId" | "createdAt" | "updatedAt">;

export type ContractDocument = {
  id: string;
  contractId: string;
  type: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  storageUri: string | null;
  extractedTextHash: string | null;
  validationStatus: string;
  malwareScanStatus: string;
  noticeVersion: string;
  uploadedAt: string;
  uploadedByUserId: string;
  containsPotentialCui: boolean;
};

export type ContractDocumentUploadRequest = {
  type: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  containsPotentialCui: boolean;
};

export type ContractDeliverable = {
  id: string;
  contractId: string;
  name: string;
  description: string;
  dueAt: string | null;
  ownerFunction: string;
  status: string;
  isOverdue: boolean;
};

export type UpsertContractDeliverableRequest = Omit<ContractDeliverable, "id" | "contractId" | "isOverdue">;

export type CalendarEvent = {
  id: string;
  title: string;
  date: string;
  category: string;
  status: string;
  riskLevel: string;
  ownerFunction: string;
  module: string;
  relatedEntityType: string | null;
  relatedEntityId: string | null;
  contractId: string | null;
  isOverdue: boolean;
};

export type CalendarEventQueryParams = {
  from: string;
  to?: string;
  owner?: string;
  status?: string;
  risk?: string;
  contractId?: string;
  module?: string;
};

export type ContractClause = {
  id: string;
  contractId: string;
  clauseLibraryId: string;
  clauseNumber: string;
  title: string;
  source: string;
  sourceUrl: string;
  lastReviewedAt: string;
  attachmentReason: string;
  sourceDocumentReference: string | null;
  attachedAt: string;
  attachedByUserId: string;
};

export type ContractObligationDashboardItem = {
  id: string;
  contractId: string;
  contractNumber: string;
  contractTitle: string;
  contractClauseId: string;
  clauseNumber: string;
  obligationId: string;
  source: string;
  sourceUrl: string;
  title: string;
  plainEnglishSummary: string;
  requiredAction: string;
  ownerFunction: string;
  assignedUserId: string | null;
  assignedUserDisplayName: string | null;
  assignedRoleName: string | null;
  riskLevel: string;
  status: string;
  dueAt: string | null;
  module: string;
  isOverdue: boolean;
  isHighRisk: boolean;
  evidenceExamples: string[];
  confidence: string;
  lastReviewedAt: string;
  requiresExpertReview: boolean;
};

export type LinkedObligationTask = {
  id: string;
  title: string;
  status: string;
  dueAt: string | null;
  ownerFunction: string;
  riskLevel: string;
};

export type LinkedObligationEvidence = {
  id: string;
  name: string;
  status: string;
  type: string;
  expiresAt: string | null;
  originalFileName: string | null;
};

export type ContractObligationDetail = ContractObligationDashboardItem & {
  clauseTitle: string;
  triggerCondition: string;
  flowDownRequired: boolean;
  flowDownRequirement: string;
  linkedTasks: LinkedObligationTask[];
  linkedEvidence: LinkedObligationEvidence[];
};

export type ContractObligationQueryParams = {
  contractId?: string;
  riskLevel?: string;
  owner?: string;
  status?: string;
  module?: string;
  dueDate?: string;
  source?: string;
};

export type AttachContractClauseRequest = {
  clauseLibraryId: string;
  attachmentReason: string;
  sourceDocumentReference: string | null;
};

export type RemoveContractClauseRequest = {
  reason: string;
};

export type UpsertCompanyProfileRequest = Omit<
  CompanyProfile,
  "id" | "tenantId" | "completionPercentage" | "isComplete" | "validationErrors" | "createdAt" | "updatedAt"
> & {
  completeProfile: boolean;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
};

export type AuditLogQueryParams = {
  page?: number;
  pageSize?: number;
  actorUserId?: string;
  action?: string;
  entityType?: string;
  from?: string;
  to?: string;
};

export type ApiMutationResult<T> = {
  data: T | null;
  error: string | null;
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

export const fallbackAuditLogs: PagedResult<AuditLogEntry> = {
  items: [],
  page: 1,
  pageSize: 5,
  totalCount: 0,
  hasNextPage: false,
  hasPreviousPage: false
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

export async function getAuditLogs(params: AuditLogQueryParams = {}): Promise<PagedResult<AuditLogEntry>> {
  const searchParams = new URLSearchParams();
  searchParams.set("page", String(params.page ?? 1));
  searchParams.set("pageSize", String(params.pageSize ?? 5));

  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== "") {
      searchParams.set(key, String(value));
    }
  }

  return getJson<PagedResult<AuditLogEntry>>(`/api/audit-logs?${searchParams.toString()}`, fallbackAuditLogs);
}

export async function getNoCuiAcknowledgementStatus(): Promise<NoCuiAcknowledgementStatus> {
  return getJson<NoCuiAcknowledgementStatus>("/api/no-cui-acknowledgement", fallbackNoCuiAcknowledgementStatus);
}

export async function getEvidenceItems(tag?: string): Promise<EvidenceMetadata[]> {
  const query = tag ? `?tag=${encodeURIComponent(tag)}` : "";
  return getJson<EvidenceMetadata[]>(`/api/evidence-items${query}`, []);
}

export async function getCmmcAssessments(): Promise<CmmcAssessment[]> {
  return getJson<CmmcAssessment[]>("/api/cmmc/assessments", []);
}

export async function getCmmcControlStatuses(assessmentId: string): Promise<CmmcControlStatus[]> {
  return getJson<CmmcControlStatus[]>(`/api/cmmc/assessments/${assessmentId}/controls`, []);
}

export async function getCmmcPoamItems(assessmentId: string): Promise<CmmcPoamItem[]> {
  return getJson<CmmcPoamItem[]>(`/api/cmmc/assessments/${assessmentId}/poam-items`, []);
}

export async function getSubcontractors(): Promise<Subcontractor[]> {
  return getJson<Subcontractor[]>("/api/subcontractors", []);
}

export async function getCompanyProfile(): Promise<CompanyProfile | null> {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5062";

  try {
    const response = await fetch(`${apiBaseUrl}/api/company-profile`, {
      headers: getDevelopmentHeaders()
    });

    if (response.status === 204) {
      return null;
    }

    if (!response.ok) {
      return null;
    }

    return response.json();
  } catch {
    return null;
  }
}

export async function getContracts(): Promise<ContractRecord[]> {
  return getJson<ContractRecord[]>("/api/contracts", []);
}

export async function getContractDocuments(contractId: string): Promise<ContractDocument[]> {
  return getJson<ContractDocument[]>(`/api/contracts/${contractId}/documents`, []);
}

export async function getContractDeliverables(contractId: string): Promise<ContractDeliverable[]> {
  return getJson<ContractDeliverable[]>(`/api/contracts/${contractId}/deliverables`, []);
}

export async function getCalendarEvents(params: CalendarEventQueryParams): Promise<CalendarEvent[]> {
  const searchParams = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== "") {
      searchParams.set(key, value);
    }
  }

  return getJson<CalendarEvent[]>(`/api/calendar/events?${searchParams.toString()}`, []);
}

export async function getContractClauses(contractId: string): Promise<ContractClause[]> {
  return getJson<ContractClause[]>(`/api/contracts/${contractId}/clauses`, []);
}

export async function getContractObligations(
  params: ContractObligationQueryParams = {}
): Promise<ContractObligationDashboardItem[]> {
  const searchParams = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== "") {
      searchParams.set(key, value);
    }
  }

  const queryString = searchParams.toString();
  return getJson<ContractObligationDashboardItem[]>(`/api/contract-obligations${queryString ? `?${queryString}` : ""}`, []);
}

export async function getContractObligationDetail(
  contractClauseId: string,
  obligationId: string
): Promise<ContractObligationDetail | null> {
  return getJson<ContractObligationDetail | null>(
    `/api/contract-obligations/${contractClauseId}/${encodeURIComponent(obligationId)}`,
    null
  );
}

export async function updateContractObligationStatus(
  contractClauseId: string,
  obligationId: string,
  status: string
): Promise<ApiMutationResult<ContractObligationDetail>> {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5062";

  try {
    const response = await fetch(
      `${apiBaseUrl}/api/contract-obligations/${contractClauseId}/${encodeURIComponent(obligationId)}/status`,
      {
        method: "PATCH",
        headers: {
          ...(getDevelopmentHeaders() ?? {}),
          "Content-Type": "application/json"
        },
        body: JSON.stringify({ status })
      }
    );

    if (!response.ok) {
      return { data: null, error: await readErrorMessage(response) };
    }

    return { data: await response.json(), error: null };
  } catch {
    return { data: null, error: "The API request could not be completed." };
  }
}

export async function assignContractObligationOwner(
  contractClauseId: string,
  obligationId: string,
  request: { userId?: string | null; roleName?: string | null; notify?: boolean }
): Promise<ApiMutationResult<ContractObligationDetail>> {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5062";

  try {
    const response = await fetch(
      `${apiBaseUrl}/api/contract-obligations/${contractClauseId}/${encodeURIComponent(obligationId)}/owner`,
      {
        method: "PATCH",
        headers: {
          ...(getDevelopmentHeaders() ?? {}),
          "Content-Type": "application/json"
        },
        body: JSON.stringify(request)
      }
    );

    if (!response.ok) {
      return { data: null, error: await readErrorMessage(response) };
    }

    return { data: await response.json(), error: null };
  } catch {
    return { data: null, error: "The API request could not be completed." };
  }
}

export async function searchClauseLibrary(params: ClauseSearchParams = {}): Promise<ClauseLibraryItem[]> {
  const searchParams = new URLSearchParams();

  if (params.query) {
    searchParams.set("query", params.query);
  }

  if (params.category) {
    searchParams.set("category", params.category);
  }

  const queryString = searchParams.toString();
  return getJson<ClauseLibraryItem[]>(`/api/clauses${queryString ? `?${queryString}` : ""}`, []);
}

export async function getContract(contractId: string): Promise<ContractRecord | null> {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5062";

  try {
    const response = await fetch(`${apiBaseUrl}/api/contracts/${contractId}`, {
      headers: getDevelopmentHeaders()
    });

    if (!response.ok) {
      return null;
    }

    return response.json();
  } catch {
    return null;
  }
}

export async function saveCompanyProfile(
  request: UpsertCompanyProfileRequest
): Promise<ApiMutationResult<CompanyProfile>> {
  return putJsonResult<CompanyProfile>("/api/company-profile", request);
}

export async function createContract(request: UpsertContractRequest): Promise<ApiMutationResult<ContractRecord>> {
  return postJsonResult<ContractRecord>("/api/contracts", request);
}

export async function updateContract(
  contractId: string,
  request: UpsertContractRequest
): Promise<ApiMutationResult<ContractRecord>> {
  return putJsonResult<ContractRecord>(`/api/contracts/${contractId}`, request);
}

export async function createContractDocument(
  contractId: string,
  request: ContractDocumentUploadRequest
): Promise<ApiMutationResult<ContractDocument>> {
  return postJsonResult<ContractDocument>(`/api/contracts/${contractId}/documents`, request);
}

export async function deleteContractDocument(contractId: string, documentId: string): Promise<ApiMutationResult<null>> {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5062";

  try {
    const response = await fetch(`${apiBaseUrl}/api/contracts/${contractId}/documents/${documentId}`, {
      method: "DELETE",
      headers: getDevelopmentHeaders()
    });

    if (!response.ok) {
      return { data: null, error: await readErrorMessage(response) };
    }

    return { data: null, error: null };
  } catch {
    return { data: null, error: "The API request could not be completed." };
  }
}

export async function createContractDeliverable(
  contractId: string,
  request: UpsertContractDeliverableRequest
): Promise<ApiMutationResult<ContractDeliverable>> {
  return postJsonResult<ContractDeliverable>(`/api/contracts/${contractId}/deliverables`, request);
}

export async function updateContractDeliverable(
  contractId: string,
  deliverableId: string,
  request: UpsertContractDeliverableRequest
): Promise<ApiMutationResult<ContractDeliverable>> {
  return putJsonResult<ContractDeliverable>(`/api/contracts/${contractId}/deliverables/${deliverableId}`, request);
}

export async function attachContractClause(
  contractId: string,
  request: AttachContractClauseRequest
): Promise<ApiMutationResult<ContractClause>> {
  return postJsonResult<ContractClause>(`/api/contracts/${contractId}/clauses`, request);
}

export async function removeContractClause(
  contractId: string,
  contractClauseId: string,
  request: RemoveContractClauseRequest
): Promise<ApiMutationResult<ContractClause>> {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5062";

  try {
    const response = await fetch(`${apiBaseUrl}/api/contracts/${contractId}/clauses/${contractClauseId}`, {
      method: "DELETE",
      headers: {
        "Content-Type": "application/json",
        ...getDevelopmentHeaders()
      },
      body: JSON.stringify(request)
    });

    if (!response.ok) {
      return { data: null, error: await readErrorMessage(response) };
    }

    return { data: await response.json(), error: null };
  } catch {
    return { data: null, error: "The API request could not be completed." };
  }
}

export async function acknowledgeNoCuiNotice(noticeVersion: string): Promise<NoCuiAcknowledgementStatus | null> {
  const response = await postJson<NoCuiAcknowledgementStatus>("/api/no-cui-acknowledgement", {
    acknowledged: true,
    noticeVersion
  });

  return response;
}

export async function createEvidenceMetadata(
  request: UpsertEvidenceMetadataRequest
): Promise<ApiMutationResult<EvidenceMetadata>> {
  return postJsonResult<EvidenceMetadata>("/api/evidence-items", request);
}

export async function createCmmcAssessment(
  request: UpsertCmmcAssessmentRequest
): Promise<ApiMutationResult<CmmcAssessment>> {
  return postJsonResult<CmmcAssessment>("/api/cmmc/assessments", request);
}

export async function createCmmcPoamItem(
  assessmentId: string,
  request: UpsertCmmcPoamItemRequest
): Promise<ApiMutationResult<CmmcPoamItem>> {
  return postJsonResult<CmmcPoamItem>(`/api/cmmc/assessments/${assessmentId}/poam-items`, request);
}

export async function createSubcontractor(request: UpsertSubcontractorRequest): Promise<ApiMutationResult<Subcontractor>> {
  return postJsonResult<Subcontractor>("/api/subcontractors", request);
}

export async function updateEvidenceMetadata(
  evidenceItemId: string,
  request: UpsertEvidenceMetadataRequest
): Promise<ApiMutationResult<EvidenceMetadata>> {
  return putJsonResult<EvidenceMetadata>(`/api/evidence-items/${evidenceItemId}`, request);
}

export async function createEvidenceUploadIntent(file: File): Promise<ApiMutationResult<EvidenceUploadIntent>> {
  const placeholderEvidenceItemId = "00000000-0000-0000-0000-000000000041";
  return postJsonResult<EvidenceUploadIntent>(`/api/evidence-items/${placeholderEvidenceItemId}/upload-intents`, {
    fileName: file.name,
    contentType: file.type || "application/octet-stream",
    sizeBytes: file.size
  });
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

async function postJsonResult<T>(path: string, body: unknown): Promise<ApiMutationResult<T>> {
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
      return { data: null, error: await readErrorMessage(response) };
    }

    return { data: await response.json(), error: null };
  } catch {
    return { data: null, error: "The API could not be reached." };
  }
}

async function putJsonResult<T>(path: string, body: unknown): Promise<ApiMutationResult<T>> {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5062";

  try {
    const response = await fetch(`${apiBaseUrl}${path}`, {
      method: "PUT",
      headers: {
        ...(getDevelopmentHeaders() ?? {}),
        "Content-Type": "application/json"
      },
      body: JSON.stringify(body)
    });

    if (!response.ok) {
      return { data: null, error: await readErrorMessage(response) };
    }

    return { data: await response.json(), error: null };
  } catch {
    return { data: null, error: "The API request could not be completed." };
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

async function readErrorMessage(response: Response): Promise<string> {
  try {
    const problem = await response.json();
    const errors = problem.errors ? Object.values(problem.errors).flat().filter(Boolean).join(" ") : "";
    return [problem.detail, errors].filter(Boolean).join(" ") || problem.title || "The upload was rejected.";
  } catch {
    return "The upload was rejected.";
  }
}
