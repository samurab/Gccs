import { cleanup, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const {
  acknowledgeNoCuiNoticeMock,
  allWorkflowAccess,
  assignContractObligationOwnerMock,
  attachContractClauseMock,
  clauseLibraryItem,
  contract,
  contractClause,
  contractDeliverable,
  contractDocument,
  createCmmcAssessmentMock,
  createContractDeliverableMock,
  createContractMock,
  createContractDocumentMock,
  createEvidenceMetadataMock,
  createEvidenceUploadIntentMock,
  createTenantInvitationMock,
  deleteContractDocumentMock,
  fallbackOverview,
  getCompanyProfileMock,
  getCmmcAssessmentsMock,
  getCmmcControlStatusesMock,
  getCalendarEventsMock,
  getContractClausesMock,
  getContractDeliverablesMock,
  getContractDocumentsMock,
  getContractObligationDetailMock,
  getContractObligationsMock,
  getContractsMock,
  getEvidenceItemsMock,
  getAuditLogsMock,
  getNoCuiAcknowledgementStatusMock,
  getComplianceOverviewMock,
  getCurrentUserAccessMock,
  getTenantInvitationsMock,
  getTenantMembersMock,
  invitations,
  calendarEvents,
  cmmcAssessment,
  cmmcControl,
  evidenceMetadata,
  members,
  obligationDashboardItem,
  obligationDetail,
  overview,
  profile,
  restrictedAccess,
  removeContractClauseMock,
  saveCompanyProfileMock,
  searchClauseLibraryMock,
  updateContractDeliverableMock,
  updateContractObligationStatusMock,
  updateContractMock,
  updateEvidenceMetadataMock
} = vi.hoisted(() => ({
  acknowledgeNoCuiNoticeMock: vi.fn(),
  assignContractObligationOwnerMock: vi.fn(),
  attachContractClauseMock: vi.fn(),
  createContractDeliverableMock: vi.fn(),
  createCmmcAssessmentMock: vi.fn(),
  createContractMock: vi.fn(),
  createContractDocumentMock: vi.fn(),
  createEvidenceMetadataMock: vi.fn(),
  createEvidenceUploadIntentMock: vi.fn(),
  createTenantInvitationMock: vi.fn(),
  deleteContractDocumentMock: vi.fn(),
  getAuditLogsMock: vi.fn(),
  getCalendarEventsMock: vi.fn(),
  getCmmcAssessmentsMock: vi.fn(),
  getCmmcControlStatusesMock: vi.fn(),
  getCompanyProfileMock: vi.fn(),
  getContractClausesMock: vi.fn(),
  getContractDeliverablesMock: vi.fn(),
  getContractDocumentsMock: vi.fn(),
  getContractObligationDetailMock: vi.fn(),
  getContractObligationsMock: vi.fn(),
  getContractsMock: vi.fn(),
  getEvidenceItemsMock: vi.fn(),
  getComplianceOverviewMock: vi.fn(),
  getCurrentUserAccessMock: vi.fn(),
  getNoCuiAcknowledgementStatusMock: vi.fn(),
  getTenantInvitationsMock: vi.fn(),
  getTenantMembersMock: vi.fn(),
  removeContractClauseMock: vi.fn(),
  saveCompanyProfileMock: vi.fn(),
  searchClauseLibraryMock: vi.fn(),
  updateContractDeliverableMock: vi.fn(),
  updateContractObligationStatusMock: vi.fn(),
  updateContractMock: vi.fn(),
  updateEvidenceMetadataMock: vi.fn(),
  allWorkflowAccess: {
    tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
    userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
    userEmail: "admin@example.com",
    roles: ["Admin"],
    permissions: [
      "ManageUsers",
      "ManageCompanyProfile",
      "ManageContracts",
      "ManageObligations",
      "ViewCompanyProfile",
      "ViewContracts",
      "ViewObligations",
      "ViewTasks",
      "ViewEvidence",
      "ManageEvidence",
      "ViewCmmc",
      "ManageCmmc",
      "ViewSubcontractors",
      "ViewReports",
      "ViewAuditLog"
    ],
    rolePermissionMatrix: {}
  },
  restrictedAccess: {
    tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
    userId: "cccccccc-cccc-cccc-cccc-ccccccccccc2",
    userEmail: "auditor@example.com",
    roles: ["Auditor"],
    permissions: ["ViewObligations", "ViewReports"],
    rolePermissionMatrix: {}
  },
  fallbackOverview: {
    productPromise:
      "Connect to the GCCS API to load source-backed modules, obligations, review metadata, and tenant-scoped compliance workflow state.",
    mvpDataPosture: "No-CUI / compliance management only",
    modules: [],
    priorityObligations: []
  },
  invitations: [
    {
      invitationId: "dddddddd-dddd-dddd-dddd-ddddddddddd1",
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      email: "pending@example.com",
      roleName: "Contributor",
      invitationToken: "pending-token",
      status: "Pending",
      expiresAt: "2026-06-20T12:00:00Z",
      acceptedAt: null,
      acceptedByUserId: null,
      revokedAt: null,
      revokedByUserId: null,
      notificationSentAt: "2026-06-13T12:00:00Z",
      notificationPlaceholder: "Local invitation notification queued for pending@example.com with token pending-token.",
      createdAt: "2026-06-13T12:00:00Z",
      updatedAt: null
    }
  ],
  members: [
    {
      membershipId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1",
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
      email: "admin@example.com",
      displayName: "Avery Admin",
      userStatus: "Active",
      membershipStatus: "Active",
      roleName: "Admin",
      mfaEnabled: true,
      lastSignedInAt: null,
      lastAccessedAt: null,
      createdAt: "2026-06-13T12:00:00Z",
      updatedAt: null
    }
  ],
  overview: {
    productPromise: "Keep every govcon obligation tied to evidence and review status.",
    mvpDataPosture: "No-CUI / compliance management only",
    modules: [
      {
        key: "company-profile",
        name: "Company compliance profile",
        purpose: "Capture entity, SAM, NAICS, certification, and data posture details.",
        status: "planned"
      },
      {
        key: "obligations",
        name: "Obligation dashboard",
        purpose: "Map clauses to actions, owners, evidence, deadlines, and source links.",
        status: "seeded"
      }
    ],
    priorityObligations: [
      {
        id: "far-52-204-21",
        source: "FAR 52.204-21",
        title: "Basic Safeguarding of Covered Contractor Information Systems",
        ownerFunction: "IT/security",
        riskLevel: "High",
        sourceUrl: "https://www.acquisition.gov/far/52.204-21",
        lastReviewedAt: "2026-06-03"
      }
    ]
  },
  profile: {
    id: "99999999-9999-9999-9999-999999999991",
    tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
    legalEntityName: "Acme Federal Services",
    doingBusinessAs: "Acme Gov",
    uei: "ABCDEF123456",
    cageCode: "1A2B3",
    samRegistrationExpiresAt: "2027-06-15",
    naicsCodes: [
      {
        code: "541330",
        title: "Engineering Services",
        isPrimary: true,
        sizeStandard: "$25.5M",
        qualifiesAsSmall: true,
        lastCheckedAt: "2026-06-15"
      }
    ],
    certifications: [],
    agencyCustomers: ["Department of Defense"],
    contractorRole: "Subcontractor",
    productsAndServices: "Engineering and cybersecurity support services",
    employeeRange: "Small",
    revenueRange: "Small",
    locations: [
      {
        name: "HQ",
        street1: "100 Main St",
        street2: null,
        city: "Arlington",
        stateOrProvince: "VA",
        postalCode: "22201",
        country: "USA",
        isPlaceOfPerformance: true
      }
    ],
    itEnvironment: {
      description: "Microsoft 365 GCC High with managed endpoints.",
      usesExternalServiceProvider: true,
      externalServiceProviderName: "Trusted MSP",
      keySystems: ["Microsoft 365", "Intune"]
    },
    dataHandlingPosture: "FciOnly",
    completionPercentage: 100,
    isComplete: true,
    validationErrors: {},
    createdAt: "2026-06-15T12:00:00Z",
    updatedAt: null
  },
  contract: {
    id: "88888888-8888-8888-8888-888888888881",
    tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
    contractNumber: "W15QKN-26-C-0001",
    title: "Base operations support services",
    agencyOrPrimeName: "Department of Defense",
    relationship: "Subcontractor",
    kind: "FixedPrice",
    status: "Active",
    awardedAt: "2026-06-15",
    periodOfPerformanceStart: "2026-07-01",
    periodOfPerformanceEnd: "2027-06-30",
    placeOfPerformance: "Arlington, VA",
    description: "No-CUI contract intake record.",
    dataHandlingPosture: "FciOnly",
    createdAt: "2026-06-15T12:00:00Z",
    updatedAt: null
  },
  clauseLibraryItem: {
    id: "far-52-204-27",
    source: "FAR 52.204-27",
    number: "52.204-27",
    title: "Prohibition on a ByteDance Covered Application",
    category: "ByteDance",
    plainEnglishSummary: "Prevent covered ByteDance applications on certain government or contractor information technology.",
    sourceUrl: "https://www.acquisition.gov/far/52.204-27",
    lastReviewedAt: "2026-06-03",
    isMappable: true
  },
  contractClause: {
    id: "55555555-5555-5555-5555-555555555551",
    contractId: "88888888-8888-8888-8888-888888888881",
    clauseLibraryId: "far-52-204-27",
    clauseNumber: "52.204-27",
    title: "Prohibition on a ByteDance Covered Application",
    source: "Far",
    sourceUrl: "https://www.acquisition.gov/far/52.204-27",
    lastReviewedAt: "2026-06-03",
    attachmentReason: "Required by prime flow-down.",
    sourceDocumentReference: "flowdown.pdf section 4",
    attachedAt: "2026-06-15T12:00:00Z",
    attachedByUserId: "cccccccc-cccc-cccc-cccc-ccccccccccc1"
  },
  contractDocument: {
    id: "77777777-7777-7777-7777-777777777771",
    contractId: "88888888-8888-8888-8888-888888888881",
    type: "StatementOfWork",
    fileName: "sow.pdf",
    contentType: "application/pdf",
    sizeBytes: 2048,
    storageUri: "pending://contracts/88888888-8888-8888-8888-888888888881/documents/77777777-7777-7777-7777-777777777771/sow.pdf",
    extractedTextHash: null,
    validationStatus: "accepted",
    malwareScanStatus: "scan-pending",
    noticeVersion: "no-cui-mvp-v1",
    uploadedAt: "2026-06-15T12:00:00Z",
    uploadedByUserId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
    containsPotentialCui: false
  },
  contractDeliverable: {
    id: "66666666-6666-6666-6666-666666666661",
    contractId: "88888888-8888-8888-8888-888888888881",
    name: "Monthly status report",
    description: "Submit the monthly performance package to the prime.",
    dueAt: "2026-06-01",
    ownerFunction: "Contracts",
    status: "InProgress",
    isOverdue: true
  },
  calendarEvents: [
    {
      id: "task:11111111-1111-1111-1111-111111111111",
      title: "Obligation follow-up",
      date: "2026-06-01",
      category: "task",
      status: "open",
      riskLevel: "High",
      ownerFunction: "contracts",
      module: "Obligations",
      relatedEntityType: "obligation",
      relatedEntityId: "obligation-fci-safeguards",
      contractId: "88888888-8888-8888-8888-888888888881",
      isOverdue: true
    },
    {
      id: "deliverable:22222222-2222-2222-2222-222222222222",
      title: "Monthly status report",
      date: "2026-07-15",
      category: "deliverable",
      status: "InProgress",
      riskLevel: "Medium",
      ownerFunction: "contracts",
      module: "Contract",
      relatedEntityType: "contract",
      relatedEntityId: "88888888-8888-8888-8888-888888888881",
      contractId: "88888888-8888-8888-8888-888888888881",
      isOverdue: false
    },
    {
      id: "report:33333333-3333-3333-3333-333333333333",
      title: "Compliance status report",
      date: "2026-07-20",
      category: "report",
      status: "Complete",
      riskLevel: "Low",
      ownerFunction: "reports",
      module: "Reports",
      relatedEntityType: "report",
      relatedEntityId: "33333333-3333-3333-3333-333333333333",
      contractId: null,
      isOverdue: false
    }
  ],
  cmmcAssessment: {
    id: "c131c131-c131-c131-c131-c131c131c131",
    tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
    name: "Level 1 workspace",
    type: "Readiness",
    level: "Level1",
    framework: "FAR-52.204-21",
    status: "Planned",
    startedAt: "2026-06-15",
    completedAt: null,
    affirmationDueAt: "2027-06-15",
    ownerFunction: "Security",
    companyProfileId: null,
    contractIds: [],
    controlSummary: {
      total: 0,
      implemented: 0,
      partiallyImplemented: 0,
      notStarted: 0,
      notApplicable: 0,
      needsReview: 0,
      completionPercentage: 0
    },
    createdAt: "2026-06-15T12:00:00Z",
    updatedAt: null
  },
  cmmcControl: {
    assessmentId: "c131c131-c131-c131-c131-c131c131c131",
    controlId: "AC.L1-3.1.1",
    title: "Authorized access control",
    family: "Access Control",
    requirement: "Limit information system access to authorized users, processes, and devices.",
    assessmentObjective: "Determine whether authorized access is identified and enforced.",
    sourceName: "CMMC Level 1 baseline",
    sourceUrl: "https://dodcio.defense.gov/CMMC/Resources-Documentation/",
    sourceLastReviewedAt: "2026-06-15",
    sourceConfidence: "high",
    status: "Implemented",
    result: "Met",
    evidenceItemIds: ["edededed-eded-eded-eded-edededededed"],
    taskIds: ["11111111-1111-1111-1111-111111111111"],
    assetIds: ["22222222-2222-2222-2222-222222222222"],
    poamItemIds: ["33333333-3333-3333-3333-333333333333"],
    assessedByUserId: null,
    assessedAt: "2026-06-15",
    notes: "Evidence reviewed."
  },
  evidenceMetadata: {
    id: "edededed-eded-eded-eded-edededededed",
    tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
    title: "Access control policy",
    type: "Policy",
    ownerFunction: "Security",
    status: "Requested",
    effectiveAt: "2026-01-15",
    expiresAt: "2026-08-15",
    tags: ["policy", "access-control"],
    description: "Policy evidence for access control obligations.",
    obligationIds: ["obligation-fci-safeguards"],
    controlIds: ["AC.L1-3.1.1"],
    contractIds: [],
    vendorIds: [],
    subcontractorIds: [],
    employeeIds: [],
    reportIds: [],
    createdAt: "2026-06-15T12:00:00Z",
    updatedAt: null
  },
  obligationDashboardItem: {
    id: "55555555555555555555555555555551:obligation-fci-safeguards",
    contractId: "88888888-8888-8888-8888-888888888881",
    contractNumber: "W15QKN-26-C-0001",
    contractTitle: "Base operations support services",
    contractClauseId: "55555555-5555-5555-5555-555555555551",
    clauseNumber: "52.204-21",
    obligationId: "obligation-fci-safeguards",
    source: "FAR 52.204-21",
    sourceUrl: "https://www.acquisition.gov/far/52.204-21",
    title: "Apply FCI safeguards",
    plainEnglishSummary: "Apply basic safeguarding controls to systems that handle FCI.",
    requiredAction: "Apply basic safeguarding controls.",
    ownerFunction: "IT/security",
    assignedUserId: null,
    assignedUserDisplayName: null,
    assignedRoleName: null,
    riskLevel: "High",
    status: "Open",
    dueAt: "2026-06-01",
    module: "Cybersecurity",
    isOverdue: true,
    isHighRisk: true,
    evidenceExamples: ["Access control policy"],
    confidence: "high",
    lastReviewedAt: "2026-06-03",
    requiresExpertReview: false
  },
  obligationDetail: {
    id: "55555555555555555555555555555551:obligation-fci-safeguards",
    contractId: "88888888-8888-8888-8888-888888888881",
    contractNumber: "W15QKN-26-C-0001",
    contractTitle: "Base operations support services",
    contractClauseId: "55555555-5555-5555-5555-555555555551",
    clauseNumber: "52.204-21",
    clauseTitle: "Basic Safeguarding",
    obligationId: "obligation-fci-safeguards",
    source: "FAR 52.204-21",
    sourceUrl: "https://www.acquisition.gov/far/52.204-21",
    title: "Apply FCI safeguards",
    plainEnglishSummary: "Apply basic safeguarding controls to systems that handle FCI.",
    triggerCondition: "Contract involves FCI.",
    requiredAction: "Apply basic safeguarding controls.",
    ownerFunction: "IT/security",
    assignedUserId: null,
    assignedUserDisplayName: null,
    assignedRoleName: null,
    riskLevel: "High",
    status: "Open",
    dueAt: "2026-06-01",
    module: "Cybersecurity",
    isOverdue: true,
    isHighRisk: true,
    flowDownRequired: true,
    flowDownRequirement: "Flow down to subcontractors handling FCI.",
    evidenceExamples: ["Access control policy"],
    confidence: "high",
    lastReviewedAt: "2026-06-03",
    requiresExpertReview: true,
    linkedTasks: [
      {
        id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa9",
        title: "Collect MFA configuration",
        status: "Open",
        dueAt: "2026-07-15",
        ownerFunction: "IT/security",
        riskLevel: "High"
      }
    ],
    linkedEvidence: [
      {
        id: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee9",
        name: "Access control policy",
        status: "Approved",
        type: "Policy",
        expiresAt: "2027-06-30",
        originalFileName: "access-control-policy.pdf"
      }
    ]
  }
}));

vi.mock("@/lib/api", () => ({
  assignContractObligationOwner: assignContractObligationOwnerMock,
  attachContractClause: attachContractClauseMock,
  createTenantInvitation: createTenantInvitationMock,
  createCmmcAssessment: createCmmcAssessmentMock,
  createContractDeliverable: createContractDeliverableMock,
  createContract: createContractMock,
  createContractDocument: createContractDocumentMock,
  createEvidenceMetadata: createEvidenceMetadataMock,
  deleteContractDocument: deleteContractDocumentMock,
  getCalendarEvents: getCalendarEventsMock,
  getCmmcAssessments: getCmmcAssessmentsMock,
  getCmmcControlStatuses: getCmmcControlStatusesMock,
  getCompanyProfile: getCompanyProfileMock,
  getContractClauses: getContractClausesMock,
  getContractDeliverables: getContractDeliverablesMock,
  getContractDocuments: getContractDocumentsMock,
  getContractObligationDetail: getContractObligationDetailMock,
  getContractObligations: getContractObligationsMock,
  getContracts: getContractsMock,
  getEvidenceItems: getEvidenceItemsMock,
  removeContractClause: removeContractClauseMock,
  saveCompanyProfile: saveCompanyProfileMock,
  searchClauseLibrary: searchClauseLibraryMock,
  updateContractDeliverable: updateContractDeliverableMock,
  updateContractObligationStatus: updateContractObligationStatusMock,
  updateContract: updateContractMock,
  updateEvidenceMetadata: updateEvidenceMetadataMock,
  acknowledgeNoCuiNotice: acknowledgeNoCuiNoticeMock,
  createEvidenceUploadIntent: createEvidenceUploadIntentMock,
  fallbackAccess: {
    tenantId: null,
    userId: null,
    userEmail: null,
    roles: [],
    permissions: [],
    rolePermissionMatrix: {}
  },
  fallbackNoCuiAcknowledgementStatus: {
    isAcknowledged: false,
    noticeVersion: "no-cui-mvp-v1",
    noticeCopy:
      "The GCCS MVP is compliance management only and is not ready to store CUI. Do not upload CUI, classified information, ITAR/export-controlled technical data, SSNs, payroll, bank or tax details, protected medical or disability data, passwords, secrets, private keys, unrestricted security logs, or other prohibited sensitive content.",
    tenantId: null,
    acknowledgedByUserId: null,
    acknowledgedAt: null
  },
  fallbackOverview,
  getComplianceOverview: getComplianceOverviewMock,
  getCurrentUserAccess: getCurrentUserAccessMock,
  getAuditLogs: getAuditLogsMock,
  fallbackAuditLogs: {
    items: [],
    page: 1,
    pageSize: 5,
    totalCount: 0,
    hasNextPage: false,
    hasPreviousPage: false
  },
  getNoCuiAcknowledgementStatus: getNoCuiAcknowledgementStatusMock,
  getTenantInvitations: getTenantInvitationsMock,
  getTenantMembers: getTenantMembersMock
}));

import { App } from "@/App";

describe("App", () => {
  beforeEach(() => {
    window.location.hash = "";
    acknowledgeNoCuiNoticeMock.mockReset();
    assignContractObligationOwnerMock.mockReset();
    attachContractClauseMock.mockReset();
    createEvidenceUploadIntentMock.mockReset();
    createEvidenceMetadataMock.mockReset();
    createCmmcAssessmentMock.mockReset();
    createContractDeliverableMock.mockReset();
    createContractMock.mockReset();
    createContractDocumentMock.mockReset();
    createTenantInvitationMock.mockReset();
    deleteContractDocumentMock.mockReset();
    updateContractDeliverableMock.mockReset();
    updateContractObligationStatusMock.mockReset();
    updateContractMock.mockReset();
    updateEvidenceMetadataMock.mockReset();
    saveCompanyProfileMock.mockReset();
    removeContractClauseMock.mockReset();
    getComplianceOverviewMock.mockReset();
    getCurrentUserAccessMock.mockReset();
    getAuditLogsMock.mockReset();
    getCalendarEventsMock.mockReset();
    getCmmcAssessmentsMock.mockReset();
    getCmmcControlStatusesMock.mockReset();
    getNoCuiAcknowledgementStatusMock.mockReset();
    getTenantInvitationsMock.mockReset();
    getTenantMembersMock.mockReset();
    getContractsMock.mockReset();
    getEvidenceItemsMock.mockReset();
    getContractClausesMock.mockReset();
    getContractDeliverablesMock.mockReset();
    getContractDocumentsMock.mockReset();
    getContractObligationDetailMock.mockReset();
    getContractObligationsMock.mockReset();
    searchClauseLibraryMock.mockReset();
    getAuditLogsMock.mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 5,
      totalCount: 0,
      hasNextPage: false,
      hasPreviousPage: false
    });
    getNoCuiAcknowledgementStatusMock.mockResolvedValue({
      isAcknowledged: false,
      noticeVersion: "no-cui-mvp-v1",
      noticeCopy:
        "The GCCS MVP is compliance management only and is not ready to store CUI. Do not upload CUI, classified information, ITAR/export-controlled technical data, SSNs, payroll, bank or tax details, protected medical or disability data, passwords, secrets, private keys, unrestricted security logs, or other prohibited sensitive content.",
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      acknowledgedByUserId: null,
      acknowledgedAt: null
    });
    getCompanyProfileMock.mockResolvedValue(profile);
    getContractsMock.mockResolvedValue([]);
    getEvidenceItemsMock.mockResolvedValue([]);
    getCmmcAssessmentsMock.mockResolvedValue([]);
    getCmmcControlStatusesMock.mockResolvedValue([]);
    getCalendarEventsMock.mockResolvedValue([]);
    getContractClausesMock.mockResolvedValue([]);
    getContractDeliverablesMock.mockResolvedValue([]);
    getContractDocumentsMock.mockResolvedValue([]);
    getContractObligationDetailMock.mockResolvedValue(null);
    getContractObligationsMock.mockResolvedValue([]);
    searchClauseLibraryMock.mockResolvedValue([]);
    saveCompanyProfileMock.mockImplementation((request) =>
      Promise.resolve({
        data: {
          ...profile,
          ...request,
          id: profile.id,
          tenantId: profile.tenantId,
          completionPercentage: request.completeProfile ? 100 : 62,
          isComplete: request.completeProfile,
          validationErrors: request.completeProfile ? {} : { uei: ["UEI is required before profile completion."] },
          createdAt: profile.createdAt,
          updatedAt: "2026-06-15T13:00:00Z"
        },
        error: null
      })
    );
    createContractMock.mockImplementation((request) =>
      Promise.resolve({
        data: {
          ...contract,
          ...request,
          id: "88888888-8888-8888-8888-888888888882",
          tenantId: contract.tenantId,
          createdAt: contract.createdAt,
          updatedAt: null
        },
        error: null
      })
    );
    createEvidenceMetadataMock.mockImplementation((request) =>
      Promise.resolve({
        data: {
          ...evidenceMetadata,
          ...request,
          id: "edededed-eded-eded-eded-ededededede2",
          createdAt: evidenceMetadata.createdAt,
          updatedAt: null
        },
        error: null
      })
    );
    createCmmcAssessmentMock.mockImplementation((request) =>
      Promise.resolve({
        data: {
          ...cmmcAssessment,
          ...request,
          id: cmmcAssessment.id,
          tenantId: cmmcAssessment.tenantId,
          controlSummary: cmmcAssessment.controlSummary,
          createdAt: cmmcAssessment.createdAt,
          updatedAt: "2026-06-15T13:30:00Z"
        },
        error: null
      })
    );
    updateEvidenceMetadataMock.mockImplementation((_evidenceItemId, request) =>
      Promise.resolve({
        data: {
          ...evidenceMetadata,
          ...request,
          updatedAt: "2026-06-15T13:00:00Z"
        },
        error: null
      })
    );
    updateContractMock.mockImplementation((contractId, request) =>
      Promise.resolve({
        data: {
          ...contract,
          ...request,
          id: contractId,
          updatedAt: "2026-06-15T13:00:00Z"
        },
        error: null
      })
    );
    createContractDocumentMock.mockResolvedValue({ data: contractDocument, error: null });
    attachContractClauseMock.mockResolvedValue({ data: contractClause, error: null });
    removeContractClauseMock.mockResolvedValue({ data: contractClause, error: null });
    createContractDeliverableMock.mockResolvedValue({
      data: {
        ...contractDeliverable,
        id: "66666666-6666-6666-6666-666666666662",
        name: "Final acceptance package",
        description: "Closeout evidence and deliverable package.",
        dueAt: "2026-08-15",
        status: "NotStarted",
        isOverdue: false
      },
      error: null
    });
    updateContractDeliverableMock.mockImplementation((_contractId, _deliverableId, request) =>
      Promise.resolve({
        data: {
          ...contractDeliverable,
          ...request,
          isOverdue: false
        },
        error: null
      })
    );
    updateContractObligationStatusMock.mockImplementation((_contractClauseId, _obligationId, status) =>
      Promise.resolve({
        data: {
          ...obligationDetail,
          status
        },
        error: null
      })
    );
    assignContractObligationOwnerMock.mockImplementation((_contractClauseId, _obligationId, request) =>
      Promise.resolve({
        data: {
          ...obligationDetail,
          ownerFunction: request.userId ? "Avery Admin" : request.roleName,
          assignedUserId: request.userId ?? null,
          assignedUserDisplayName: request.userId ? "Avery Admin" : null,
          assignedRoleName: request.roleName ?? null
        },
        error: null
      })
    );
    deleteContractDocumentMock.mockResolvedValue({ data: null, error: null });
  });

  afterEach(() => {
    cleanup();
  });

  it("TC-3.2.1 lands authenticated users in the workspace dashboard", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);

    render(<App />);

    expect(await screen.findByRole("heading", { name: "Dashboard" })).toBeInTheDocument();
    expect(screen.getByText(overview.productPromise)).toBeInTheDocument();
    expect(screen.queryByText(/marketing/i)).not.toBeInTheDocument();
    expect(screen.getByRole("navigation", { name: /primary workspace navigation/i })).toBeInTheDocument();
  });

  it("TC-3.2.2 supports keyboard navigation across each visible primary route", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    const user = userEvent.setup();

    render(<App />);

    await screen.findByRole("heading", { name: "Dashboard" });
    await user.tab();
    expect(screen.getByRole("link", { name: /skip to workspace content/i })).toHaveFocus();

    const routeChecks = [
      ["Profile", "Acme Federal Services"],
      ["Contracts", "No contracts have been added yet"],
      ["Obligations", "Clause library search"],
      ["Calendar", "Calendar agenda"],
      ["Evidence", "No-CUI acknowledgement"],
      ["CMMC", "No CMMC assessment has started yet"],
      ["Subcontractors", "No subcontractors have been added yet"],
      ["Reports", "No reports have been generated yet"],
      ["Settings", "Team members"]
    ];

    for (const [linkName, expectedText] of routeChecks) {
      const link = screen.getByRole("link", { name: new RegExp(linkName, "i") });
      link.focus();
      expect(link).toHaveFocus();
      await user.keyboard("{Enter}");
      expect(await screen.findByText(expectedText)).toBeInTheDocument();
      expect(link).toHaveAttribute("aria-current", "page");
    }
  });

  it("TC-7.1.2 and TC-7.1.3 renders and saves the company profile form", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /profile/i }));
    expect(await screen.findByDisplayValue("Acme Federal Services")).toBeInTheDocument();
    expect(screen.getByText("100%")).toBeInTheDocument();

    const legalEntity = screen.getByLabelText("Legal entity");
    await user.clear(legalEntity);
    await user.type(legalEntity, "Acme Federal Services Updated");
    await user.click(screen.getByRole("button", { name: /add naics/i }));
    const codeInputs = screen.getAllByLabelText("Code");
    const titleInputs = screen.getAllByLabelText("Title");
    const basisInputs = screen.getAllByLabelText("Size basis");
    const statusInputs = screen.getAllByLabelText("Status");
    await user.type(codeInputs[1], "541511");
    await user.type(titleInputs[1], "Custom Computer Programming Services");
    await user.type(basisInputs[1], "$34M");
    await user.selectOptions(statusInputs[1], "true");
    await user.click(screen.getByRole("button", { name: /save draft/i }));

    expect(saveCompanyProfileMock).toHaveBeenCalledWith(
      expect.objectContaining({
        legalEntityName: "Acme Federal Services Updated",
        completeProfile: false,
        naicsCodes: expect.arrayContaining([
          expect.objectContaining({ code: "541330", qualifiesAsSmall: true, sizeStandard: "$25.5M" }),
          expect.objectContaining({ code: "541511", qualifiesAsSmall: true, sizeStandard: "$34M" })
        ])
      })
    );
    expect(await screen.findByText("Draft saved.")).toBeInTheDocument();
    expect(screen.getByText("62%")).toBeInTheDocument();
  });

  it("TC-7.3.1 submits company certification rows from the profile form", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /profile/i }));
    await user.selectOptions(screen.getByLabelText("Type"), "Wosb");
    await user.selectOptions(screen.getByLabelText("Certification status"), "Active");
    await user.type(screen.getByLabelText("Issuer"), "SBA");
    await user.type(screen.getByLabelText("Effective"), "2026-01-01");
    await user.type(screen.getByLabelText("Expires"), "2026-08-15");
    await user.type(screen.getByLabelText("Reference"), "WOSB-2026");
    await user.click(screen.getByRole("button", { name: /save draft/i }));

    expect(saveCompanyProfileMock).toHaveBeenCalledWith(
      expect.objectContaining({
        completeProfile: false,
        certifications: [
          expect.objectContaining({
            type: "Wosb",
            status: "Active",
            issuer: "SBA",
            effectiveAt: "2026-01-01",
            expiresAt: "2026-08-15",
            referenceNumber: "WOSB-2026"
          })
        ]
      })
    );
  });

  it("TC-8.1.1 and TC-8.1.3 renders contract detail and submits a new contract", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    getContractsMock.mockResolvedValueOnce([contract]);
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /contracts/i }));
    expect(await screen.findByRole("heading", { name: "W15QKN-26-C-0001" })).toBeInTheDocument();
    expect(screen.getByText("2026-07-01 to 2027-06-30")).toBeInTheDocument();
    expect(screen.getByText("FciOnly")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /new contract/i }));
    await user.type(screen.getByLabelText("Contract number"), "FA8750-26-F-0002");
    await user.type(screen.getByLabelText("Title"), "Cybersecurity support services");
    await user.type(screen.getByLabelText("Agency or prime"), "Department of the Air Force");
    await user.selectOptions(screen.getByLabelText("Contract type"), "TimeAndMaterials");
    await user.selectOptions(screen.getByLabelText("Status"), "Draft");
    await user.type(screen.getByLabelText("Awarded"), "2026-06-15");
    await user.type(screen.getByLabelText("Start"), "2026-07-01");
    await user.type(screen.getByLabelText("End"), "2027-06-30");
    await user.type(screen.getByLabelText("Place of performance"), "Dayton, OH");
    await user.type(screen.getByLabelText("Description"), "No-CUI contract intake record.");
    await user.click(screen.getByRole("button", { name: /create contract/i }));

    expect(createContractMock).toHaveBeenCalledWith(
      expect.objectContaining({
        contractNumber: "FA8750-26-F-0002",
        title: "Cybersecurity support services",
        agencyOrPrimeName: "Department of the Air Force",
        kind: "TimeAndMaterials",
        status: "Draft",
        dataHandlingPosture: "FciOnly"
      })
    );
    expect(await screen.findByText("Contract created.")).toBeInTheDocument();
  });

  it("TC-8.2.1 disables contract document upload before No-CUI acknowledgement", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    getContractsMock.mockResolvedValueOnce([contract]);
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /contracts/i }));

    expect(await screen.findByLabelText("Contract document")).toBeDisabled();
    expect(screen.getByText(/No-CUI acknowledgement is required before contract document upload/i)).toBeInTheDocument();
  });

  it("TC-8.2.1 and TC-8.2.2 gates contract document upload on No-CUI acknowledgement", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    getNoCuiAcknowledgementStatusMock.mockResolvedValueOnce({
      isAcknowledged: true,
      noticeVersion: "no-cui-mvp-v1",
      noticeCopy: "No-CUI only.",
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      acknowledgedByUserId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
      acknowledgedAt: "2026-06-15T12:00:00Z"
    });
    getContractsMock.mockResolvedValueOnce([contract]);
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /contracts/i }));
    const fileInput = await screen.findByLabelText("Contract document");
    expect(fileInput).toBeEnabled();
    await user.upload(fileInput, new File(["source"], "sow.pdf", { type: "application/pdf" }));
    await user.click(screen.getByRole("button", { name: /upload metadata/i }));

    expect(createContractDocumentMock).toHaveBeenCalledWith(
      contract.id,
      expect.objectContaining({
        type: "Contract",
        fileName: "sow.pdf",
        contentType: "application/pdf",
        containsPotentialCui: false
      })
    );
    expect(await screen.findByText(/Document metadata captured/i)).toBeInTheDocument();
  });

  it("TC-8.3.1, TC-8.3.3, and TC-8.3.4 manages contract deliverables", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    getContractsMock.mockResolvedValueOnce([contract]);
    getContractDeliverablesMock.mockResolvedValueOnce([contractDeliverable]);
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /contracts/i }));

    expect(await screen.findByText("Monthly status report")).toBeInTheDocument();
    expect(screen.getByText(/Contracts . 2026-06-01 . Overdue/i)).toBeInTheDocument();

    await user.type(screen.getByLabelText("Name"), "Final acceptance package");
    await user.clear(screen.getByLabelText("Owner"));
    await user.type(screen.getByLabelText("Owner"), "Program manager");
    await user.type(screen.getByLabelText("Due date"), "2026-08-15");
    await user.type(screen.getByLabelText("Deliverable description"), "Closeout evidence and deliverable package.");
    await user.click(screen.getByRole("button", { name: /add deliverable/i }));

    expect(createContractDeliverableMock).toHaveBeenCalledWith(
      contract.id,
      expect.objectContaining({
        name: "Final acceptance package",
        ownerFunction: "Program manager",
        dueAt: "2026-08-15",
        status: "NotStarted"
      })
    );
    expect(await screen.findByText(/Deliverable added to the contract calendar/i)).toBeInTheDocument();

    await user.selectOptions(screen.getByLabelText("Status for Monthly status report"), "Submitted");

    expect(updateContractDeliverableMock).toHaveBeenCalledWith(
      contract.id,
      contractDeliverable.id,
      expect.objectContaining({
        name: contractDeliverable.name,
        status: "Submitted"
      })
    );
  });

  it("TC-9.2.1 and TC-9.2.3 attaches and removes contract clauses with reasons", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    getContractsMock.mockResolvedValueOnce([contract]);
    getContractClausesMock.mockResolvedValueOnce([contractClause]);
    attachContractClauseMock.mockResolvedValueOnce({
      data: {
        ...contractClause,
        id: "55555555-5555-5555-5555-555555555552",
        clauseLibraryId: "far-52-204-21",
        clauseNumber: "52.204-21",
        title: "Basic Safeguarding"
      },
      error: null
    });
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /contracts/i }));
    expect(await screen.findByText("Prohibition on a ByteDance Covered Application")).toBeInTheDocument();

    await user.type(screen.getByLabelText("Published clause ID"), "far-52-204-21");
    await user.type(screen.getByLabelText("Attachment reason"), "Required by award package.");
    await user.type(screen.getByLabelText("Source document reference"), "contract.pdf section 12");
    await user.click(screen.getByRole("button", { name: /attach clause/i }));

    expect(attachContractClauseMock).toHaveBeenCalledWith(
      contract.id,
      expect.objectContaining({
        clauseLibraryId: "far-52-204-21",
        attachmentReason: "Required by award package.",
        sourceDocumentReference: "contract.pdf section 12"
      })
    );
    expect(await screen.findByText("Clause attached to contract.")).toBeInTheDocument();

    const removalReason = screen.getByLabelText("Removal reason for 52.204-27");
    await user.type(removalReason, "Removed from revised flow-down.");
    const removalForm = removalReason.closest("form");
    expect(removalForm).not.toBeNull();
    await user.click(within(removalForm as HTMLElement).getByRole("button", { name: /remove/i }));

    expect(removeContractClauseMock).toHaveBeenCalledWith(contract.id, contractClause.id, {
      reason: "Removed from revised flow-down."
    });
    expect(await screen.findByText("Clause removed from contract.")).toBeInTheDocument();
  });

  it("TC-9.1.1 and TC-9.1.3 searches published clauses and shows source metadata", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    searchClauseLibraryMock.mockResolvedValueOnce([clauseLibraryItem]);
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /obligations/i }));
    await user.type(screen.getByLabelText("Clause search"), "52.204-27");
    await user.selectOptions(screen.getByLabelText("Category"), "ByteDance");
    await user.click(screen.getByRole("button", { name: /search clauses/i }));

    expect(searchClauseLibraryMock).toHaveBeenCalledWith({
      query: "52.204-27",
      category: "ByteDance"
    });
    expect(await screen.findByText("Prohibition on a ByteDance Covered Application")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "FAR 52.204-27" })).toHaveAttribute(
      "href",
      "https://www.acquisition.gov/far/52.204-27"
    );
    expect(screen.getByText("2026-06-03")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /select clause/i }));

    expect(screen.getByText("Selected 52.204-27")).toBeInTheDocument();
  });

  it("TC-10.1.2 and TC-10.1.3 filters the obligation work queue and highlights priority items", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    getContractsMock.mockResolvedValueOnce([contract]);
    getContractObligationsMock.mockResolvedValueOnce([obligationDashboardItem]);
    getContractObligationsMock.mockResolvedValueOnce([obligationDashboardItem]);
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /obligations/i }));
    expect(await screen.findByText("Obligation work queue")).toBeInTheDocument();
    expect(screen.getByText("Apply FCI safeguards")).toBeInTheDocument();
    expect(screen.getByLabelText("High risk obligation")).toBeInTheDocument();
    expect(screen.getByLabelText("Overdue obligation")).toBeInTheDocument();

    await user.selectOptions(screen.getByLabelText("Contract"), contract.id);
    await user.selectOptions(screen.getByLabelText("Risk"), "High");
    await user.type(screen.getByLabelText("Owner"), "IT/security");
    await user.selectOptions(screen.getByLabelText("Status"), "Open");
    await user.selectOptions(screen.getByLabelText("Module"), "Cybersecurity");
    await user.selectOptions(screen.getByLabelText("Due date"), "overdue");
    await user.type(screen.getByLabelText("Source"), "52.204-21");
    await user.click(screen.getByRole("button", { name: /apply filters/i }));

    expect(getContractObligationsMock).toHaveBeenLastCalledWith({
      contractId: contract.id,
      riskLevel: "High",
      owner: "IT/security",
      status: "Open",
      module: "Cybersecurity",
      dueDate: "overdue",
      source: "52.204-21"
    });
    expect(await screen.findByText("1 tenant-scoped obligations matched.")).toBeInTheDocument();
  });

  it("TC-10.1.4 guides setup when the obligation work queue is empty", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    getContractObligationsMock.mockResolvedValueOnce([]);
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /obligations/i }));

    expect(await screen.findByText("Start with company profile or contract intake")).toBeInTheDocument();
    expect(screen.getByText(/add a contract, and attach mapped clauses/i)).toBeInTheDocument();
    expect(screen.getByText("Clause library search")).toBeInTheDocument();
  });

  it("TC-10.2.1 and TC-10.2.2 opens obligation detail with source content, tasks, and evidence", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    getContractsMock.mockResolvedValueOnce([contract]);
    getContractObligationsMock.mockResolvedValueOnce([obligationDashboardItem]);
    getContractObligationDetailMock.mockResolvedValueOnce(obligationDetail);
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /obligations/i }));
    await user.click(await screen.findByRole("button", { name: /view details/i }));

    expect(getContractObligationDetailMock).toHaveBeenCalledWith(
      obligationDashboardItem.contractClauseId,
      obligationDashboardItem.obligationId
    );
    expect(await screen.findByRole("region", { name: /obligation detail/i })).toBeInTheDocument();
    expect(screen.getByText("Contract involves FCI.")).toBeInTheDocument();
    expect(screen.getAllByText("Apply basic safeguarding controls.").length).toBeGreaterThan(0);
    expect(screen.getAllByRole("link", { name: "FAR 52.204-21" })[0]).toHaveAttribute(
      "href",
      "https://www.acquisition.gov/far/52.204-21"
    );
    expect(screen.getByText("Expert review required")).toBeInTheDocument();
    expect(screen.getByText(/Collect MFA configuration - Open due 2026-07-15/i)).toBeInTheDocument();
    expect(screen.getByText(/Access control policy - Approved/i)).toBeInTheDocument();
  });

  it("TC-10.2.3 updates obligation status and refreshes the dashboard row", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    getContractsMock.mockResolvedValueOnce([contract]);
    getContractObligationsMock.mockResolvedValueOnce([obligationDashboardItem]);
    getContractObligationDetailMock.mockResolvedValueOnce(obligationDetail);
    updateContractObligationStatusMock.mockResolvedValueOnce({
      data: {
        ...obligationDetail,
        status: "Blocked"
      },
      error: null
    });
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /obligations/i }));
    await user.click(await screen.findByRole("button", { name: /view details/i }));
    await user.selectOptions(await screen.findByLabelText("Update status"), "Blocked");
    await user.click(screen.getByRole("button", { name: /save status/i }));

    expect(updateContractObligationStatusMock).toHaveBeenCalledWith(
      obligationDetail.contractClauseId,
      obligationDetail.obligationId,
      "Blocked"
    );
    expect(await screen.findByText("Obligation status updated.")).toBeInTheDocument();
    expect(screen.getAllByText("Blocked").length).toBeGreaterThan(0);
  });

  it("TC-10.3.1 assigns an obligation to a tenant member", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    getContractsMock.mockResolvedValueOnce([contract]);
    getContractObligationsMock.mockResolvedValueOnce([obligationDashboardItem]);
    getContractObligationDetailMock.mockResolvedValueOnce(obligationDetail);
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /obligations/i }));
    await user.click(await screen.findByRole("button", { name: /view details/i }));
    await user.selectOptions(await screen.findByLabelText("Tenant member"), members[0].userId);
    await user.click(screen.getByLabelText("Notify owner"));
    await user.click(screen.getByRole("button", { name: /assign owner/i }));

    expect(assignContractObligationOwnerMock).toHaveBeenCalledWith(
      obligationDetail.contractClauseId,
      obligationDetail.obligationId,
      {
        userId: members[0].userId,
        roleName: null,
        notify: true
      }
    );
    expect(await screen.findByText("Obligation owner assigned.")).toBeInTheDocument();
    expect(screen.getAllByText("Avery Admin").length).toBeGreaterThan(0);
  });

  it("TC-10.3.2 assigns an obligation to a role", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    getContractsMock.mockResolvedValueOnce([contract]);
    getContractObligationsMock.mockResolvedValueOnce([obligationDashboardItem]);
    getContractObligationDetailMock.mockResolvedValueOnce(obligationDetail);
    assignContractObligationOwnerMock.mockResolvedValueOnce({
      data: {
        ...obligationDetail,
        ownerFunction: "ComplianceManager",
        assignedRoleName: "ComplianceManager"
      },
      error: null
    });
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /obligations/i }));
    await user.click(await screen.findByRole("button", { name: /view details/i }));
    await user.selectOptions(await screen.findByLabelText("Assign by"), "role");
    await user.selectOptions(screen.getByLabelText("Role"), "ComplianceManager");
    await user.click(screen.getByRole("button", { name: /assign owner/i }));

    expect(assignContractObligationOwnerMock).toHaveBeenCalledWith(
      obligationDetail.contractClauseId,
      obligationDetail.obligationId,
      {
        userId: null,
        roleName: "ComplianceManager",
        notify: false
      }
    );
    expect(await screen.findByText("Obligation owner assigned.")).toBeInTheDocument();
    expect(screen.getAllByText("ComplianceManager").length).toBeGreaterThan(0);
  });

  it("TC-2.4.2 renders workspace actions and TC-3.2.3 hides restricted navigation", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(restrictedAccess);

    render(<App />);

    await screen.findByRole("heading", { name: "Dashboard" });
    const navigation = screen.getByRole("navigation", { name: /primary workspace navigation/i });

    expect(within(navigation).getByRole("link", { name: /dashboard/i })).toBeInTheDocument();
    expect(within(navigation).getByRole("link", { name: /obligations/i })).toBeInTheDocument();
    expect(within(navigation).getByRole("link", { name: /reports/i })).toBeInTheDocument();
    expect(within(navigation).queryByRole("link", { name: /profile/i })).not.toBeInTheDocument();
    expect(within(navigation).queryByRole("link", { name: /contracts/i })).not.toBeInTheDocument();
    expect(within(navigation).queryByRole("link", { name: /settings/i })).not.toBeInTheDocument();
    expect(getTenantMembersMock).not.toHaveBeenCalled();
    expect(getTenantInvitationsMock).not.toHaveBeenCalled();
    expect(getNoCuiAcknowledgementStatusMock).not.toHaveBeenCalled();
  });

  it("TC-3.2.4 shows loading, empty, and error states", async () => {
    let resolveOverview: (value: typeof fallbackOverview) => void = () => undefined;
    getComplianceOverviewMock.mockReturnValueOnce(
      new Promise((resolve) => {
        resolveOverview = resolve;
      })
    );
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce([]);
    getTenantMembersMock.mockResolvedValueOnce([]);

    const { unmount } = render(<App />);
    expect(screen.getByText("Loading workspace data")).toBeInTheDocument();
    resolveOverview(fallbackOverview);
    expect(await screen.findByText("API overview unavailable")).toBeInTheDocument();
    expect(screen.getByText("Source data unavailable")).toBeInTheDocument();

    unmount();
    cleanup();
    getComplianceOverviewMock.mockReset();
    getCurrentUserAccessMock.mockReset();
    getComplianceOverviewMock.mockRejectedValueOnce(new Error("API unavailable"));
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);

    render(<App />);
    expect(await screen.findByRole("alert")).toHaveTextContent("Workspace data could not be loaded");
  });

  it("keeps user invitation actions in the role-aware settings route", async () => {
    const createdInvitation = {
      ...invitations[0],
      invitationId: "dddddddd-dddd-dddd-dddd-ddddddddddd5",
      email: "new.invite@example.com",
      invitationToken: "new-token",
      notificationPlaceholder: "Local invitation notification queued for new.invite@example.com with token new-token."
    };
    getComplianceOverviewMock.mockResolvedValueOnce(fallbackOverview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce([]);
    getTenantMembersMock.mockResolvedValueOnce([]);
    createTenantInvitationMock.mockResolvedValueOnce(createdInvitation);
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /settings/i }));
    await user.type(screen.getByLabelText("Email"), "new.invite@example.com");
    await user.selectOptions(screen.getByLabelText("Role"), "Auditor");
    await user.click(screen.getByRole("button", { name: /invite/i }));

    expect(createTenantInvitationMock).toHaveBeenCalledWith({
      email: "new.invite@example.com",
      roleName: "Auditor",
      expiresInDays: 7
    });
    expect(await screen.findByText("Invitation created.")).toBeInTheDocument();
    expect(screen.getByText("new.invite@example.com")).toBeInTheDocument();
  });

  it("TC-5.2.1, TC-5.2.3, and TC-5.2.4 renders tenant audit logs with filters and pagination", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(fallbackOverview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce([]);
    getTenantMembersMock.mockResolvedValueOnce([]);
    getAuditLogsMock.mockResolvedValueOnce({
      items: [
        {
          id: "51515151-5151-5151-5151-515151515101",
          tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
          actorUserId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
          action: "Created",
          entityType: "TenantInvitation",
          entityId: "dddddddd-dddd-dddd-dddd-ddddddddddd1",
          occurredAt: "2026-06-15T12:00:00Z",
          ipAddress: "203.0.113.10",
          userAgent: "test",
          correlationId: "audit-table",
          summary: "Invitation was created.",
          metadata: {}
        }
      ],
      page: 1,
      pageSize: 5,
      totalCount: 6,
      hasNextPage: true,
      hasPreviousPage: false
    });
    getAuditLogsMock.mockResolvedValueOnce({
      items: [],
      page: 2,
      pageSize: 5,
      totalCount: 6,
      hasNextPage: false,
      hasPreviousPage: true
    });
    getAuditLogsMock.mockResolvedValueOnce({
      items: [],
      page: 1,
      pageSize: 5,
      totalCount: 0,
      hasNextPage: false,
      hasPreviousPage: false
    });
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /settings/i }));
    expect(await screen.findByRole("table", { name: /tenant audit logs/i })).toBeInTheDocument();
    expect(screen.getByText("Invitation was created.")).toBeInTheDocument();
    expect(screen.getByText(/Page 1 of 2/)).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /next/i }));
    expect(getAuditLogsMock).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2, pageSize: 5 }));

    await user.type(screen.getByLabelText("Actor ID"), "cccccccc-cccc-cccc-cccc-ccccccccccc1");
    await user.selectOptions(screen.getByLabelText("Action"), "Created");
    await user.type(screen.getByLabelText("Entity"), "TenantInvitation");
    await user.click(screen.getByRole("button", { name: /filter/i }));

    expect(getAuditLogsMock).toHaveBeenLastCalledWith(
      expect.objectContaining({
        page: 1,
        pageSize: 5,
        actorUserId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
        action: "Created",
        entityType: "TenantInvitation"
      })
    );
    expect(await screen.findByText("No audit events match")).toBeInTheDocument();
  });

  it("TC-4.1.1 and TC-4.1.2 shows the No-CUI notice before upload and disables upload controls", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /evidence/i }));

    expect(screen.getByText("No-CUI acknowledgement")).toBeInTheDocument();
    expect(screen.getByText(/compliance management only and is not ready to store CUI/i)).toBeInTheDocument();
    expect(screen.getByLabelText("Evidence file")).toBeDisabled();
    expect(screen.getByRole("button", { name: /upload evidence/i })).toBeDisabled();
    expect(screen.getByText(/upload is disabled until the No-CUI notice is acknowledged/i)).toBeInTheDocument();
  });

  it("TC-12.1 renders and creates reusable evidence metadata", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    getEvidenceItemsMock.mockResolvedValueOnce([evidenceMetadata]);
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /evidence/i }));
    expect(await screen.findByText("Evidence metadata")).toBeInTheDocument();
    expect(screen.getByText("Access control policy")).toBeInTheDocument();
    expect(screen.getByDisplayValue("obligation-fci-safeguards")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /new evidence/i }));
    await user.clear(screen.getByLabelText("Title"));
    await user.type(screen.getByLabelText("Title"), "Quarterly access review");
    await user.selectOptions(screen.getByLabelText("Type"), "AccessReview");
    await user.type(screen.getByLabelText("Tags"), "access-review, quarterly");
    await user.type(screen.getByLabelText("Obligations"), "obligation-access-review");
    await user.click(screen.getByRole("button", { name: /create metadata/i }));

    expect(createEvidenceMetadataMock).toHaveBeenCalledWith(
      expect.objectContaining({
        title: "Quarterly access review",
        type: "AccessReview",
        tags: ["access-review", "quarterly"],
        obligationIds: ["obligation-access-review"]
      })
    );
    expect(await screen.findByText("Evidence metadata created.")).toBeInTheDocument();
  });

  it("TC-4.1.3 and TC-4.1.4 saves acknowledgement before enabling upload intent creation", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    acknowledgeNoCuiNoticeMock.mockResolvedValueOnce({
      isAcknowledged: true,
      noticeVersion: "no-cui-mvp-v1",
      noticeCopy:
        "The GCCS MVP is compliance management only and is not ready to store CUI. Do not upload CUI, classified information, ITAR/export-controlled technical data, SSNs, payroll, bank or tax details, protected medical or disability data, passwords, secrets, private keys, unrestricted security logs, or other prohibited sensitive content.",
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      acknowledgedByUserId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
      acknowledgedAt: "2026-06-14T12:00:00Z"
    });
    createEvidenceUploadIntentMock.mockResolvedValueOnce({
      data: {
        id: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee1",
        evidenceItemId: "00000000-0000-0000-0000-000000000041",
        tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
        createdByUserId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
        fileName: "policy.pdf",
        contentType: "application/pdf",
        sizeBytes: 6,
        status: "upload-pending",
        validationStatus: "accepted",
        malwareScanStatus: "scan-pending",
        message: "No-CUI acknowledgement is on record.",
        noticeVersion: "no-cui-mvp-v1",
        expiresAt: "2026-06-14T12:15:00Z"
      },
      error: null
    });
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /evidence/i }));
    await user.click(screen.getByRole("button", { name: /i acknowledge the no-cui upload limitation/i }));

    expect(acknowledgeNoCuiNoticeMock).toHaveBeenCalledWith("no-cui-mvp-v1");
    expect(await screen.findByText("Acknowledgement saved.")).toBeInTheDocument();
    expect(screen.getAllByText("Acknowledged").length).toBeGreaterThan(0);
    const fileInput = screen.getByLabelText("Evidence file");
    expect(fileInput).toBeEnabled();

    await user.upload(fileInput, new File(["policy"], "policy.pdf", { type: "application/pdf" }));
    await user.click(screen.getByRole("button", { name: /upload evidence/i }));

    expect(createEvidenceUploadIntentMock).toHaveBeenCalledWith(expect.objectContaining({ name: "policy.pdf", type: "application/pdf" }));
    expect(await screen.findByText(/Upload intent created for policy.pdf/i)).toBeInTheDocument();
    expect(screen.getByText(/malware scan scan-pending/i)).toBeInTheDocument();
  });

  it("TC-4.2.1 and TC-4.2.2 shows server-side upload guardrail rejection messages", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    getNoCuiAcknowledgementStatusMock.mockResolvedValueOnce({
      isAcknowledged: true,
      noticeVersion: "no-cui-mvp-v1",
      noticeCopy:
        "The GCCS MVP is compliance management only and is not ready to store CUI. Do not upload CUI, classified information, ITAR/export-controlled technical data, SSNs, payroll, bank or tax details, protected medical or disability data, passwords, secrets, private keys, unrestricted security logs, or other prohibited sensitive content.",
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      acknowledgedByUserId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
      acknowledgedAt: "2026-06-14T12:00:00Z"
    });
    createEvidenceUploadIntentMock.mockResolvedValueOnce({
      data: null,
      error: "File size exceeds the No-CUI MVP upload limit."
    });
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /evidence/i }));
    expect(screen.getByText(/Allowed file types: PDF, PNG, JPG, TXT, CSV, DOCX, and XLSX/i)).toBeInTheDocument();

    const fileInput = screen.getByLabelText("Evidence file");
    await user.upload(fileInput, new File(["policy"], "policy.pdf", { type: "application/pdf" }));
    await user.click(screen.getByRole("button", { name: /upload evidence/i }));

    expect(createEvidenceUploadIntentMock).toHaveBeenCalledWith(
      expect.objectContaining({ name: "policy.pdf", type: "application/pdf" })
    );
    expect(await screen.findByText("File size exceeds the No-CUI MVP upload limit.")).toBeInTheDocument();
  });

  it("TC-13.1 renders and creates a CMMC Level 2 readiness assessment", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    getContractsMock.mockResolvedValueOnce([contract]);
    getCmmcAssessmentsMock.mockResolvedValueOnce([cmmcAssessment]);
    getCmmcControlStatusesMock.mockResolvedValueOnce([cmmcControl]);
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /cmmc/i }));
    expect(await screen.findByText("Level 1 workspace")).toBeInTheDocument();
    expect(screen.getByText("AC.L1-3.1.1 · Authorized access control")).toBeInTheDocument();
    expect(screen.getByText("Implemented · Met · CMMC Level 1 baseline reviewed 2026-06-15")).toBeInTheDocument();
    expect(screen.getByText("Evidence 1 · Tasks 1 · Assets 1 · POA&M 1")).toBeInTheDocument();
    await user.clear(screen.getByLabelText("Assessment name"));
    await user.type(screen.getByLabelText("Assessment name"), "Level 2 workspace");
    await user.selectOptions(screen.getByLabelText("Target level"), "Level2");
    await user.selectOptions(screen.getByLabelText("Contract link"), contract.id);
    await user.click(screen.getByRole("button", { name: /create assessment/i }));

    expect(createCmmcAssessmentMock).toHaveBeenCalledWith(
      expect.objectContaining({
        name: "Level 2 workspace",
        level: "Level2",
        framework: "NIST-SP-800-171-Rev2",
        ownerFunction: "Security",
        contractIds: [contract.id]
      })
    );
    expect(await screen.findByText("CMMC readiness assessment created.")).toBeInTheDocument();
  });

  it("TC-11.2.1, TC-11.2.2, and TC-11.2.3 renders calendar events, filters them, and marks overdue items", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    getContractsMock.mockResolvedValueOnce([contract]);
    getCalendarEventsMock.mockResolvedValueOnce(calendarEvents);
    getCalendarEventsMock.mockResolvedValueOnce([calendarEvents[0]]);
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /calendar/i }));
    expect(await screen.findByRole("heading", { name: "Calendar agenda" })).toBeInTheDocument();
    expect(screen.getByText("Obligation follow-up")).toBeInTheDocument();
    expect(screen.getByText("Monthly status report")).toBeInTheDocument();
    expect(screen.getByText("Compliance status report")).toBeInTheDocument();
    expect(screen.getByLabelText("Overdue calendar item")).toBeInTheDocument();

    await user.type(screen.getByLabelText("Owner"), "contracts");
    await user.selectOptions(screen.getByLabelText("Status"), "open");
    await user.selectOptions(screen.getByLabelText("Risk"), "High");
    await user.selectOptions(screen.getByLabelText("Contract"), contract.id);
    await user.selectOptions(screen.getByLabelText("Module"), "Obligations");
    await user.click(screen.getByRole("button", { name: /apply filters/i }));

    expect(getCalendarEventsMock).toHaveBeenLastCalledWith(
      expect.objectContaining({
        owner: "contracts",
        status: "open",
        risk: "High",
        contractId: contract.id,
        module: "Obligations"
      })
    );
    expect(await screen.findByText("1 calendar items matched.")).toBeInTheDocument();
  });
});
