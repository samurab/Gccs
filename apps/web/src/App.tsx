import {
  Archive,
  AlertTriangle,
  Ban,
  Bell,
  Building2,
  CalendarClock,
  CheckCircle2,
  ClipboardCheck,
  FileDown,
  FileSearch,
  FolderKanban,
  GitBranch,
  LayoutDashboard,
  RefreshCw,
  Printer,
  ScrollText,
  Send,
  Settings,
  ShieldCheck,
  SlidersHorizontal,
  UploadCloud,
  UserPlus,
  UsersRound,
  X
} from "lucide-react";
import { type FormEvent, type ReactNode, type RefObject, useCallback, useEffect, useMemo, useRef, useState } from "react";
import { ControlCoverageMeter } from "@/components/ControlCoverageMeter";
import { controlCoverageTone } from "@/components/controlCoverage";
import { DevelopmentTestingContextSelector } from "@/components/development/DevelopmentTestingContextSelector";
import { ModuleCard } from "@/components/ModuleCard";
import { TenantWorkspaceSelector } from "@/components/TenantWorkspaceSelector";
import { getNotificationOpenUrl } from "@/routing";
import {
  Alert,
  Button,
  DataHandlingBadge,
  LoadingState,
  MetricTile,
  PageHeader,
  RiskBadge,
  StatusPill,
  TaskCard,
  WorkflowColumn,
  WorkspaceMetricStrip
} from "@/components/ui";
import { formatUsDateOnly, formatUsDateTime } from "./lib/dateFormat";
import {
  acknowledgeNoCuiNotice,
  acknowledgeSharedResponsibilityMatrix,
  acceptClauseCandidate,
  archiveReport,
  applyCompanyEntityLookup,
  applySubcontractorEntityLookup,
  approveCuiReadyApprovalChecklist,
  assignContractObligationOwner,
  attachContractClause,
  createContract,
  createCuiReadyApprovalChecklist,
  createCmmcAssessment,
  createCmmcPoamItem,
  createSubcontractorEvidenceRequest,
  createSubcontractorFlowDown,
  createSubcontractor,
  createContractDeliverable,
  createTenantInvitation,
  createEvidenceUploadIntent,
  createEvidenceMetadata,
  getContentClassificationReviewItems,
  deleteContractDocument,
  fallbackAuditLogs,
  fallbackAccess,
  fallbackNoCuiAcknowledgementStatus,
  fallbackOverview,
  generateCmmcReadinessReport,
  generateComplianceStatusReport,
  generateContractClauseObligations,
  generateEvidencePackage,
  generateSubcontractorComplianceReport,
  getApprovedEvidencePackages,
  getEvidencePackage,
  getRecentReports,
  getReportArtifact,
  getReportExport,
  getCompanyProfile,
  getCmmcAssessments,
  getCmmcControlLibrary,
  getCmmcControlStatuses,
  getCmmcPoamItems,
  getSubcontractors,
  getSubcontractorEvidenceRequests,
  getSubcontractorFlowDowns,
  getCalendarEvents,
  getContractClauses,
  getContractDeliverables,
  getContractDocuments,
  getContractDocumentExtractionResults,
  getContractObligationDetail,
  getContractObligations,
  getContracts,
  getCuiReadyApprovalChecklists,
  getAuditLogEntityTypes,
  getAuditLogs,
  getComplianceOverview,
  getCurrentUserAccess,
  getEvidenceItems,
  getNoCuiAcknowledgementStatus,
  getNotificationPreferences,
  getNotifications,
  getObligationAssignmentCandidates,
  getPublishedSharedResponsibilityMatrix,
  getSharedResponsibilityMatrixAcknowledgements,
  getTenant,
  getTenantDataHandlingModeHistory,
  getTenantInvitations,
  getTenantMembers,
  reclassifyEvidenceItem,
  markClauseCandidateNeedsClarification,
  markNotificationRead,
  runDueDateReminders,
  revokeTenantInvitation,
  saveCompanyProfile,
  searchCompanyEntity,
  searchSubcontractorEntity,
  searchClauseLibrary,
  seedDemoTenant,
  removeContractClause,
  rejectClauseCandidate,
  restoreReport,
  requestReportPdfExport,
  downloadReportExport,
  startContractDocumentExtraction,
  supersedeClauseCandidate,
  supersedeCuiReadyApprovalChecklist,
  rejectCuiReadyApprovalChecklist,
  submitCuiReadyApprovalChecklist,
  updateNotificationPreferences,
  updateCuiReadyApprovalChecklistItem,
  updateTenantDataHandlingMode,
  updateCmmcAssessment,
  updateContractObligationStatus,
  updateContract,
  updateContractDeliverable,
  updateEvidenceMetadata,
  updateSubcontractorFlowDown,
  uploadContractDocumentFile,
  type ApprovedEvidencePackage,
  type AuditLogEntry,
  type ClauseLibraryItem,
  type ClauseSearchParams,
  type CalendarEvent,
  type CalendarEventQueryParams,
  type CmmcReadinessReport,
  type CompanyCertification,
  type CompanyProfile,
  type CompanyEntityLookupResult,
  type CmmcAssessment,
  type CmmcControlLibrary,
  type CmmcControlStatus,
  type CmmcPoamItem,
  type ComplianceOverview,
  type ComplianceStatusReport,
  type ContentClassificationReviewItem,
  type CuiReadyApprovalChecklist,
  type CuiReadyApprovalChecklistItem,
  type UpdateCuiReadyChecklistItemRequest,
  type SharedResponsibilityMatrix,
  type SharedResponsibilityMatrixAcknowledgement,
  type ContractClause,
  type ContractDeliverable,
  type ContractDocument,
  type ContractDocumentExtractionResults,
  type ExtractionJob,
  type ContractObligationDetail,
  type ContractObligationDashboardItem,
  type ContractObligationQueryParams,
  type ContractRecord,
  type CurrentUserAccess,
  type DueDateReminderRunResult,
  type EvidenceMetadata,
  type EvidencePackageGenerateRequest,
  type EvidencePackageReport,
  type NoCuiAcknowledgementStatus,
  type NotificationCenterItem,
  type NotificationPreference,
  type NotificationPreferenceUpdateRequest,
  type ObligationAssignmentCandidate,
  type PagedResult,
  type ReportHistoryItem,
  type Subcontractor,
  type SubcontractorEntityLookupResult,
  type SubcontractorComplianceReport,
  type SubcontractorEvidenceRequest,
  type SubcontractorFlowDown,
  type TenantInvitation,
  type RevokeTenantInvitationRequest,
  type Tenant,
  type TenantDataHandlingModeHistory,
  type AttachContractClauseRequest,
  type UpsertContractDeliverableRequest,
  type UpsertContractRequest,
  type UpsertCmmcAssessmentRequest,
  type UpsertCmmcPoamItemRequest,
  type UpsertCompanyProfileRequest,
  type UpsertEvidenceMetadataRequest,
  type UpsertSubcontractorEvidenceRequestRequest,
  type UpsertSubcontractorFlowDownRequest,
  type UpsertSubcontractorRequest,
  type UpdateTenantDataHandlingModeRequest,
  type ReclassifyContentRequest,
  type TenantMember,
} from "@/lib/api";

type WorkspaceRoute =
  | "dashboard"
  | "profile"
  | "contracts"
  | "obligations"
  | "calendar"
  | "evidence"
  | "cmmc"
  | "subcontractors"
  | "reports"
  | "settings";

type LoadState = "loading" | "ready" | "error";
type AccessLoadState = "loading" | "ready" | "error";
type ReportArtifact = ComplianceStatusReport | CmmcReadinessReport | SubcontractorComplianceReport | EvidencePackageReport;
type ReportDetailStatus = "idle" | "loading" | "ready" | "failed";

const tenantModeUpdateTimeoutMs = 15000;
const tenantModeHistoryTimeoutMs = 10000;

function withTimeout<T>(promise: Promise<T>, timeoutMs: number, message: string): Promise<T> {
  let timeoutId: ReturnType<typeof setTimeout> | undefined;
  const timeout = new Promise<T>((_, reject) => {
    timeoutId = setTimeout(() => reject(new Error(message)), timeoutMs);
  });

  return Promise.race([promise, timeout]).finally(() => {
    if (timeoutId) {
      clearTimeout(timeoutId);
    }
  });
}

type AuditLogFilters = {
  actorUserId: string;
  action: string;
  entityType: string;
  from: string;
  to: string;
};

type CalendarFilters = {
  from: string;
  to: string;
  owner: string;
  status: string;
  risk: string;
  contractId: string;
  module: string;
};

type NaicsFormRow = {
  code: string;
  title: string;
  isPrimary: boolean;
  sizeStandard: string;
  qualifiesAsSmall: string;
};

type CertificationFormRow = {
  id: string | null;
  type: string;
  status: string;
  issuer: string;
  effectiveAt: string;
  expiresAt: string;
  referenceNumber: string;
};

type ProfileFormState = {
  legalEntityName: string;
  doingBusinessAs: string;
  uei: string;
  cageCode: string;
  samRegistrationExpiresAt: string;
  naicsRows: NaicsFormRow[];
  certificationRows: CertificationFormRow[];
  agencyCustomers: string;
  contractorRole: string;
  productsAndServices: string;
  employeeRange: string;
  revenueRange: string;
  locationName: string;
  street1: string;
  city: string;
  stateOrProvince: string;
  postalCode: string;
  country: string;
  itDescription: string;
  usesExternalServiceProvider: boolean;
  externalServiceProviderName: string;
  keySystems: string;
  dataHandlingPosture: string;
};

type ContractFormState = {
  contractNumber: string;
  title: string;
  agencyOrPrimeName: string;
  relationship: string;
  kind: string;
  status: string;
  awardedAt: string;
  periodOfPerformanceStart: string;
  periodOfPerformanceEnd: string;
  placeOfPerformance: string;
  description: string;
  dataHandlingPosture: string;
};

type NavigationItem = {
  route: WorkspaceRoute;
  label: string;
  description: string;
  group: "Command" | "Operations" | "Assurance" | "Administration";
  icon: typeof LayoutDashboard;
  permissions?: string[];
};

const roleGuidance = [
  {
    role: "Owner",
    persona: "Founder, executive sponsor, or tenant owner",
    purpose: "Owns the workspace, account controls, users, audit visibility, and final business accountability."
  },
  {
    role: "Admin",
    persona: "Operations lead or delegated system administrator",
    purpose: "Manages users and day-to-day workspace configuration while supporting every compliance workflow."
  },
  {
    role: "Compliance Manager",
    persona: "Contracts admin, proposal manager, CMMC lead, or back-office compliance owner",
    purpose: "Runs the core FeDril workflow: profile, contracts, obligations, calendar, evidence, CMMC, subcontractors, and reports."
  },
  {
    role: "Contributor",
    persona: "IT, HR, finance, project, or vendor-support teammate",
    purpose: "Completes assigned tasks and uploads allowed non-CUI evidence without changing governed settings or approvals."
  },
  {
    role: "Auditor",
    persona: "Read-only reviewer, prime reviewer, assessor support, or internal executive reviewer",
    purpose: "Reviews source-backed status, evidence, CMMC readiness, subcontractors, reports, and tasks without editing records."
  },
  {
    role: "Advisor",
    persona: "MSP, CMMC consultant, govcon attorney, CPA, or compliance advisor",
    purpose: "Helps manage client compliance work inside explicit tenant boundaries with audit visibility."
  }
];

const navigationItems: NavigationItem[] = [
  {
    route: "dashboard",
    label: "Dashboard",
    description: "Workspace overview",
    group: "Command",
    icon: LayoutDashboard
  },
  {
    route: "profile",
    label: "Profile",
    description: "Company compliance profile",
    group: "Operations",
    icon: Building2,
    permissions: ["ViewCompanyProfile", "ManageCompanyProfile"]
  },
  {
    route: "contracts",
    label: "Contracts",
    description: "Contract and clause intake",
    group: "Operations",
    icon: FileSearch,
    permissions: ["ViewContracts", "ManageContracts"]
  },
  {
    route: "obligations",
    label: "Obligations",
    description: "Source-backed obligation matrix",
    group: "Command",
    icon: ClipboardCheck,
    permissions: ["ViewObligations", "ManageObligations"]
  },
  {
    route: "calendar",
    label: "Calendar",
    description: "Work queue, renewals, and reminders",
    group: "Command",
    icon: CalendarClock,
    permissions: ["ViewTasks", "ManageTasks"]
  },
  {
    route: "evidence",
    label: "Evidence",
    description: "No-CUI evidence vault",
    group: "Operations",
    icon: Archive,
    permissions: ["ViewEvidence", "ManageEvidence", "ApproveEvidence"]
  },
  {
    route: "cmmc",
    label: "CMMC",
    description: "Readiness and control tracking",
    group: "Assurance",
    icon: ShieldCheck,
    permissions: ["ViewCmmc", "ManageCmmc"]
  },
  {
    route: "subcontractors",
    label: "Subcontractors",
    description: "Flow-down and supplier status",
    group: "Operations",
    icon: GitBranch,
    permissions: ["ViewSubcontractors", "ManageSubcontractors"]
  },
  {
    route: "reports",
    label: "Reports",
    description: "Readiness reports and evidence packages",
    group: "Assurance",
    icon: ScrollText,
    permissions: ["ViewReports", "ManageReports"]
  },
  {
    route: "settings",
    label: "Settings",
    description: "Tenant access and workspace controls",
    group: "Administration",
    icon: Settings,
    permissions: ["ManageTenant", "ManageUsers", "ViewAuditLog"]
  }
];

const navigationGroups: Array<NavigationItem["group"]> = ["Command", "Operations", "Assurance", "Administration"];

const moduleIcons = [Building2, FileSearch, ClipboardCheck, CalendarClock, Archive, ShieldCheck, GitBranch, FolderKanban];
const ownerFunctionOptions = [
  ["Contracts", "Contracts"],
  ["Compliance", "Compliance"],
  ["ComplianceManager", "Compliance manager"],
  ["Security", "Security"],
  ["IT/security", "IT/security"],
  ["ProgramManagement", "Program management"],
  ["Finance", "Finance"],
  ["HR/payroll", "HR/payroll"],
  ["Legal", "Legal"],
  ["Reports", "Reports"],
  ["Subcontractors", "Subcontractors"]
] as const;
const defaultAuditLogFilters: AuditLogFilters = {
  actorUserId: "",
  action: "",
  entityType: "",
  from: "",
  to: ""
};

const defaultProfileForm: ProfileFormState = {
  legalEntityName: "",
  doingBusinessAs: "",
  uei: "",
  cageCode: "",
  samRegistrationExpiresAt: "",
  naicsRows: [{ code: "", title: "", isPrimary: true, sizeStandard: "", qualifiesAsSmall: "" }],
  certificationRows: [{ id: null, type: "Wosb", status: "Active", issuer: "", effectiveAt: "", expiresAt: "", referenceNumber: "" }],
  agencyCustomers: "",
  contractorRole: "Unknown",
  productsAndServices: "",
  employeeRange: "Unknown",
  revenueRange: "Unknown",
  locationName: "",
  street1: "",
  city: "",
  stateOrProvince: "",
  postalCode: "",
  country: "USA",
  itDescription: "",
  usesExternalServiceProvider: false,
  externalServiceProviderName: "",
  keySystems: "",
  dataHandlingPosture: "Unknown"
};

const defaultContractForm: ContractFormState = {
  contractNumber: "",
  title: "",
  agencyOrPrimeName: "",
  relationship: "Subcontractor",
  kind: "FixedPrice",
  status: "Draft",
  awardedAt: "",
  periodOfPerformanceStart: "",
  periodOfPerformanceEnd: "",
  placeOfPerformance: "",
  description: "",
  dataHandlingPosture: "FciOnly"
};

function getInitialRoute(): WorkspaceRoute {
  const route = window.location.hash.replace(/^#\/?/, "");
  return isWorkspaceRoute(route) ? route : "dashboard";
}

function isWorkspaceRoute(route: string): route is WorkspaceRoute {
  return navigationItems.some((item) => item.route === route);
}

function hasAnyPermission(access: CurrentUserAccess, permissions?: string[]) {
  if (!permissions || permissions.length === 0) {
    return true;
  }

  return permissions.some((permission) => access.permissions.includes(permission));
}

function defaultCalendarQuery(): CalendarEventQueryParams {
  const today = new Date();
  const from = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), 1));
  const to = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth() + 2, 0));

  return {
    from: from.toISOString().slice(0, 10),
    to: to.toISOString().slice(0, 10)
  };
}

function ownerOptionsWith(currentValue: string) {
  if (!currentValue || ownerFunctionOptions.some(([value]) => value === currentValue)) {
    return ownerFunctionOptions;
  }

  return [[currentValue, formatOwnerLabel(currentValue)], ...ownerFunctionOptions] as const;
}

function normalizeRoleKey(value: string | null | undefined) {
  return (value ?? "").replace(/[\s_-]/g, "").toLowerCase();
}

function canonicalAssignmentRoleName(value: string | null | undefined) {
  return normalizeRoleKey(value) === "compliancemanager" ? "Compliance Manager" : (value ?? "");
}

function defaultCalendarFilters(): CalendarFilters {
  const query = defaultCalendarQuery();

  return {
    from: query.from,
    to: query.to ?? "",
    owner: "",
    status: "",
    risk: "",
    contractId: "",
    module: ""
  };
}

function emptyStringsToUndefined(filters: CalendarFilters): Omit<CalendarEventQueryParams, "from" | "to"> {
  return {
    owner: filters.owner || undefined,
    status: filters.status || undefined,
    risk: filters.risk || undefined,
    contractId: filters.contractId || undefined,
    module: filters.module || undefined
  };
}

function isDemoCaptureMode() {
  return import.meta.env.VITE_DEMO_CAPTURE === "true";
}

export function App() {
  const demoCaptureMode = isDemoCaptureMode();
  const [overview, setOverview] = useState(fallbackOverview);
  const [access, setAccess] = useState<CurrentUserAccess>(fallbackAccess);
  const [members, setMembers] = useState<TenantMember[]>([]);
  const [obligationAssignmentCandidates, setObligationAssignmentCandidates] = useState<ObligationAssignmentCandidate[]>([]);
  const [invitations, setInvitations] = useState<TenantInvitation[]>([]);
  const [currentTenant, setCurrentTenant] = useState<Tenant | null>(null);
  const [tenantModeHistory, setTenantModeHistory] = useState<TenantDataHandlingModeHistory[]>([]);
  const [cuiReadyChecklists, setCuiReadyChecklists] = useState<CuiReadyApprovalChecklist[]>([]);
  const [sharedResponsibilityMatrix, setSharedResponsibilityMatrix] = useState<SharedResponsibilityMatrix | null>(null);
  const [sharedResponsibilityMatrixAcknowledgements, setSharedResponsibilityMatrixAcknowledgements] = useState<
    SharedResponsibilityMatrixAcknowledgement[]
  >([]);
  const [notifications, setNotifications] = useState<NotificationCenterItem[]>([]);
  const [notificationPreference, setNotificationPreference] = useState<NotificationPreference | null>(null);
  const [reminderRunResult, setReminderRunResult] = useState<DueDateReminderRunResult | null>(null);
  const [companyProfile, setCompanyProfile] = useState<CompanyProfile | null>(null);
  const [contracts, setContracts] = useState<ContractRecord[]>([]);
  const [clauseResults, setClauseResults] = useState<ClauseLibraryItem[]>([]);
  const [obligationDashboardItems, setObligationDashboardItems] = useState<ContractObligationDashboardItem[]>([]);
  const [selectedObligationDetail, setSelectedObligationDetail] = useState<ContractObligationDetail | null>(null);
  const [contractClauses, setContractClauses] = useState<ContractClause[]>([]);
  const [contractDeliverables, setContractDeliverables] = useState<ContractDeliverable[]>([]);
  const [contractDocuments, setContractDocuments] = useState<ContractDocument[]>([]);
  const [extractionJobsByDocumentId, setExtractionJobsByDocumentId] = useState<Record<string, ExtractionJob>>({});
  const [extractionResultsByDocumentId, setExtractionResultsByDocumentId] = useState<Record<string, ContractDocumentExtractionResults>>({});
  const [clauseCandidateReviewStatusFilter, setClauseCandidateReviewStatusFilter] = useState("all");
  const [calendarEvents, setCalendarEvents] = useState<CalendarEvent[]>([]);
  const [evidenceItems, setEvidenceItems] = useState<EvidenceMetadata[]>([]);
  const [classificationReviewItems, setClassificationReviewItems] = useState<ContentClassificationReviewItem[]>([]);
  const [cmmcAssessments, setCmmcAssessments] = useState<CmmcAssessment[]>([]);
  const [selectedCmmcAssessmentId, setSelectedCmmcAssessmentId] = useState<string | null>(null);
  const [cmmcControlLibrary, setCmmcControlLibrary] = useState<CmmcControlLibrary[]>([]);
  const [cmmcControls, setCmmcControls] = useState<CmmcControlStatus[]>([]);
  const [cmmcPoamItems, setCmmcPoamItems] = useState<CmmcPoamItem[]>([]);
  const [subcontractors, setSubcontractors] = useState<Subcontractor[]>([]);
  const [selectedSubcontractorId, setSelectedSubcontractorId] = useState<string | null>(null);
  const [subcontractorFlowDowns, setSubcontractorFlowDowns] = useState<SubcontractorFlowDown[]>([]);
  const [subcontractorEvidenceRequests, setSubcontractorEvidenceRequests] = useState<SubcontractorEvidenceRequest[]>([]);
  const [approvedEvidencePackages, setApprovedEvidencePackages] = useState<ApprovedEvidencePackage[]>([]);
  const [generatedReports, setGeneratedReports] = useState<ReportArtifact[]>([]);
  const [recentReports, setRecentReports] = useState<ReportHistoryItem[]>([]);
  const [selectedReport, setSelectedReport] = useState<ReportArtifact | null>(null);
  const [reportDetailStatus, setReportDetailStatus] = useState<ReportDetailStatus>("idle");
  const [reportDetailMessage, setReportDetailMessage] = useState("");
  const [selectedEvidenceItemId, setSelectedEvidenceItemId] = useState<string | null>(null);
  const [calendarFilters, setCalendarFilters] = useState<CalendarFilters>(() => defaultCalendarFilters());
  const [calendarStatus, setCalendarStatus] = useState<"idle" | "loading" | "ready" | "failed">("idle");
  const [calendarMessage, setCalendarMessage] = useState("");
  const [selectedContractId, setSelectedContractId] = useState<string | null>(null);
  const [auditLogs, setAuditLogs] = useState<PagedResult<AuditLogEntry>>(fallbackAuditLogs);
  const [auditLogEntityTypes, setAuditLogEntityTypes] = useState<string[]>([]);
  const [auditLogEntityTypeStatus, setAuditLogEntityTypeStatus] = useState<"idle" | "ready" | "failed">("idle");
  const [auditLogFilters, setAuditLogFilters] = useState<AuditLogFilters>(defaultAuditLogFilters);
  const [auditLogStatus, setAuditLogStatus] = useState<"idle" | "loading" | "ready" | "failed">("idle");
  const [noCuiAcknowledgement, setNoCuiAcknowledgement] = useState<NoCuiAcknowledgementStatus>(
    fallbackNoCuiAcknowledgementStatus
  );
  const [activeRoute, setActiveRoute] = useState<WorkspaceRoute>(getInitialRoute);
  const [loadState, setLoadState] = useState<LoadState>("loading");
  const [accessLoadState, setAccessLoadState] = useState<AccessLoadState>("loading");
  const [workspaceInitialized, setWorkspaceInitialized] = useState(import.meta.env.DEV);
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteRole, setInviteRole] = useState("Contributor");
  const [inviteStatus, setInviteStatus] = useState<"idle" | "sending" | "created" | "failed">("idle");
  const [inviteMessage, setInviteMessage] = useState("");
  const [invitationActionStatus, setInvitationActionStatus] = useState<"idle" | "revoking" | "succeeded" | "failed">("idle");
  const [invitationActionMessage, setInvitationActionMessage] = useState("");
  const [revokingInvitationId, setRevokingInvitationId] = useState<string | null>(null);
  const [profileStatus, setProfileStatus] = useState<"idle" | "saving" | "saved" | "failed">("idle");
  const [profileMessage, setProfileMessage] = useState("");
  const [profileValidationErrors, setProfileValidationErrors] = useState<Record<string, string[]>>({});
  const [contractStatus, setContractStatus] = useState<"idle" | "saving" | "saved" | "failed">("idle");
  const [contractMessage, setContractMessage] = useState("");
  const [contractClauseStatus, setContractClauseStatus] = useState<"idle" | "saving" | "saved" | "failed">("idle");
  const [contractClauseMessage, setContractClauseMessage] = useState("");
  const [clauseSearchStatus, setClauseSearchStatus] = useState<"idle" | "loading" | "ready" | "failed">("idle");
  const [clauseSearchMessage, setClauseSearchMessage] = useState("");
  const [obligationDashboardStatus, setObligationDashboardStatus] = useState<"idle" | "loading" | "ready" | "failed">("idle");
  const [obligationDashboardMessage, setObligationDashboardMessage] = useState("");
  const [obligationDetailStatus, setObligationDetailStatus] = useState<"idle" | "loading" | "ready" | "saving" | "failed">("idle");
  const [obligationDetailMessage, setObligationDetailMessage] = useState("");
  const [deliverableStatus, setDeliverableStatus] = useState<"idle" | "saving" | "saved" | "failed">("idle");
  const [deliverableMessage, setDeliverableMessage] = useState("");
  const [contractDocumentStatus, setContractDocumentStatus] = useState<"idle" | "saving" | "saved" | "failed">("idle");
  const [contractDocumentMessage, setContractDocumentMessage] = useState("");
  const [selectedEvidenceFile, setSelectedEvidenceFile] = useState<File | null>(null);
  const [acknowledgementStatus, setAcknowledgementStatus] = useState<"idle" | "saving" | "saved" | "failed">("idle");
  const [acknowledgementMessage, setAcknowledgementMessage] = useState("");
  const [uploadStatus, setUploadStatus] = useState<"idle" | "creating" | "created" | "blocked">("idle");
  const [uploadMessage, setUploadMessage] = useState("");
  const [evidenceMetadataStatus, setEvidenceMetadataStatus] = useState<"idle" | "saving" | "saved" | "failed">("idle");
  const [evidenceMetadataMessage, setEvidenceMetadataMessage] = useState("");
  const [cmmcStatus, setCmmcStatus] = useState<"idle" | "saving" | "saved" | "failed">("idle");
  const [cmmcMessage, setCmmcMessage] = useState("");
  const [cmmcPoamStatus, setCmmcPoamStatus] = useState<"idle" | "saving" | "saved" | "failed">("idle");
  const [cmmcPoamMessage, setCmmcPoamMessage] = useState("");
  const [subcontractorStatus, setSubcontractorStatus] = useState<"idle" | "saving" | "saved" | "failed">("idle");
  const [subcontractorMessage, setSubcontractorMessage] = useState("");
  const [subcontractorDetailStatus, setSubcontractorDetailStatus] = useState<"idle" | "loading" | "saving" | "ready" | "failed">("idle");
  const [subcontractorDetailMessage, setSubcontractorDetailMessage] = useState("");
  const [reportStatus, setReportStatus] = useState<"idle" | "loading" | "ready" | "failed">("idle");
  const [reportMessage, setReportMessage] = useState("");
  const [notificationPreferenceStatus, setNotificationPreferenceStatus] = useState<"idle" | "saving" | "saved" | "failed">("idle");
  const [notificationPreferenceMessage, setNotificationPreferenceMessage] = useState("");
  const [tenantModeStatus, setTenantModeStatus] = useState<"idle" | "saving" | "saved" | "failed">("idle");
  const [tenantModeMessage, setTenantModeMessage] = useState("");
  const [cuiReadyChecklistStatus, setCuiReadyChecklistStatus] = useState<"idle" | "saving" | "saved" | "failed">("idle");
  const [cuiReadyChecklistMessage, setCuiReadyChecklistMessage] = useState("");
  const [matrixAcknowledgementStatus, setMatrixAcknowledgementStatus] = useState<"idle" | "saving" | "saved" | "failed">("idle");
  const [matrixAcknowledgementMessage, setMatrixAcknowledgementMessage] = useState("");
  const [demoSeedStatus, setDemoSeedStatus] = useState<"idle" | "saving" | "saved" | "failed">("idle");
  const [demoSeedMessage, setDemoSeedMessage] = useState("");

  const visibleNavigation = useMemo(
    () =>
      accessLoadState === "ready"
        ? navigationItems.filter((item) => hasAnyPermission(access, item.permissions))
        : [],
    [access, accessLoadState]
  );
  const visibleNavigationByGroup = useMemo(
    () =>
      navigationGroups
        .map((group) => ({
          group,
          items: visibleNavigation.filter((item) => item.group === group)
        }))
        .filter((group) => group.items.length > 0),
    [visibleNavigation]
  );
  const activeNavigationItem = navigationItems.find((item) => item.route === activeRoute);
  const tenantMode = currentTenant?.dataHandlingMode ?? "NoCui";
  const activeTenantName = currentTenant?.displayName ?? "Tenant not loaded";
  const userDisplay = access.userEmail ?? "Development user";
  const workspacePriorityMetrics = useMemo(
    () => [
      {
        label: "Control coverage",
        value: <ControlCoverageMeter readinessScore={overview.readinessScore} />,
        tone: controlCoverageTone(overview.readinessScore.status),
        hint: `${overview.readinessScore.status} · implementation only`
      },
      {
        label: "Contract risk",
        value: overview.contractRiskIndicator.level,
        tone: riskIndicatorTone(overview.contractRiskIndicator.level),
        hint: formatHighRiskObligationHint(overview.contractRiskIndicator.highRiskObligations)
      },
      {
        label: "Overdue",
        value: obligationDashboardItems.filter((item) => item.isOverdue).length + calendarEvents.filter((event) => event.isOverdue).length,
        tone: "danger" as const,
        hint: "obligations and tasks"
      },
      {
        label: "High risk",
        value: obligationDashboardItems.filter((item) => item.isHighRisk).length,
        tone: "warning" as const,
        hint: "source-backed obligations"
      },
      {
        label: "Evidence review",
        value: classificationReviewItems.length,
        tone: classificationReviewItems.length > 0 ? ("warning" as const) : ("success" as const),
        hint: "classification queue"
      },
      {
        label: "No-CUI notice",
        value: noCuiAcknowledgement.isAcknowledged ? "Acknowledged" : "Required",
        tone: noCuiAcknowledgement.isAcknowledged ? ("success" as const) : ("danger" as const),
        hint: "upload guardrail"
      }
    ],
    [calendarEvents, classificationReviewItems, noCuiAcknowledgement.isAcknowledged, obligationDashboardItems, overview.contractRiskIndicator, overview.readinessScore]
  );
  const canManageUsers = access.permissions.includes("ManageUsers");
  const canManageEvidence = access.permissions.includes("ManageEvidence");
  const canManageCompanyProfile = access.permissions.includes("ManageCompanyProfile");
  const canManageContracts = access.permissions.includes("ManageContracts");
  const canReviewClauses = access.permissions.includes("ReviewClauses");
  const canManageObligations = access.permissions.includes("ManageObligations");
  const canManageCmmc = access.permissions.includes("ManageCmmc");
  const canManageReports = access.permissions.includes("ManageReports");
  const canArchiveReports = access.permissions.includes("ArchiveReports");
  const canExportReports = access.permissions.includes("ExportReports");
  const canViewAuditLog = access.permissions.includes("ViewAuditLog");
  const canManageTenant = access.permissions.includes("ManageTenant");

  useEffect(() => {
    function handleHashChange() {
      const nextRoute = getInitialRoute();
      const nextItem = navigationItems.find((item) => item.route === nextRoute);
      const hasLoadedAccess = access.userId !== null || access.permissions.length > 0 || access.roles.length > 0;

      if (!nextItem) {
        setActiveRoute("dashboard");
        window.history.replaceState(null, "", "#/dashboard");
        return;
      }

      if (!hasLoadedAccess) {
        setActiveRoute(nextRoute);
        return;
      }

      if (!hasAnyPermission(access, nextItem.permissions)) {
        setActiveRoute("dashboard");
        window.history.replaceState(null, "", "#/dashboard");
        return;
      }

      setActiveRoute(nextRoute);
    }

    window.addEventListener("hashchange", handleHashChange);
    handleHashChange();

    return () => {
      window.removeEventListener("hashchange", handleHashChange);
    };
  }, [access]);

  useEffect(() => {
    if (!workspaceInitialized) {
      return;
    }

    let isMounted = true;
    let resolvedAccess: CurrentUserAccess | null = null;

    getCurrentUserAccess()
      .then(async (nextAccess) => {
        resolvedAccess = nextAccess;
        if (isMounted) {
          setAccess(nextAccess);
          setAccessLoadState("ready");
        }

        const nextOverview = await getComplianceOverview();
        const canLoadUserManagement = nextAccess.permissions.includes("ManageUsers");
        const canLoadTenantContext =
          typeof getTenant === "function" &&
          nextAccess.permissions.includes("ViewCompanyProfile") &&
          nextAccess.tenantId !== null;
        const canUseTenantAdministrationApi = [
          getTenantDataHandlingModeHistory,
          getCuiReadyApprovalChecklists,
          getPublishedSharedResponsibilityMatrix,
          getSharedResponsibilityMatrixAcknowledgements
        ].every((loader) => typeof loader === "function");
        const canLoadTenantAdministration =
          canUseTenantAdministrationApi && nextAccess.permissions.includes("ManageTenant") && nextAccess.tenantId !== null;
        const canLoadAuditLogs = nextAccess.permissions.includes("ViewAuditLog");
        const canLoadNoCuiStatus = hasAnyPermission(nextAccess, ["ViewContracts", "ManageContracts", "ViewEvidence", "ManageEvidence"]);
        const canLoadEvidence = hasAnyPermission(nextAccess, ["ViewEvidence", "ManageEvidence", "ApproveEvidence"]);
        const canLoadCompanyProfile = hasAnyPermission(nextAccess, ["ViewCompanyProfile", "ManageCompanyProfile"]);
        const canLoadContracts = hasAnyPermission(nextAccess, ["ViewContracts", "ManageContracts"]);
        const canLoadObligations = hasAnyPermission(nextAccess, ["ViewObligations", "ManageObligations"]);
        const canLoadObligationAssignmentCandidates = nextAccess.permissions.includes("ManageObligations");
        const canLoadCalendar = hasAnyPermission(nextAccess, ["ViewTasks", "ManageTasks"]);
        const canLoadNotifications = hasAnyPermission(nextAccess, ["ViewTasks", "ManageTasks"]);
        const canLoadCmmc = hasAnyPermission(nextAccess, ["ViewCmmc", "ManageCmmc"]);
        const canLoadSubcontractors = hasAnyPermission(nextAccess, ["ViewSubcontractors", "ManageSubcontractors"]);
        const canLoadReports = hasAnyPermission(nextAccess, ["ViewReports", "ManageReports"]);
        const [nextMembers, nextInvitations, nextObligationAssignmentCandidates] = canLoadUserManagement
          ? await Promise.all([
              getTenantMembers(),
              getTenantInvitations(),
              canLoadObligationAssignmentCandidates ? getObligationAssignmentCandidates() : Promise.resolve([])
            ])
          : canLoadObligationAssignmentCandidates
            ? await Promise.all([
                Promise.resolve([]),
                Promise.resolve([]),
                getObligationAssignmentCandidates()
              ])
            : [[], [], []];
        const nextTenant = canLoadTenantContext ? await getTenant(nextAccess.tenantId!) : null;
        const [
          nextTenantModeHistory,
          nextCuiReadyChecklists,
          nextSharedResponsibilityMatrix,
          nextSharedResponsibilityMatrixAcknowledgements
        ] = canLoadTenantAdministration
          ? await Promise.all([
              getTenantDataHandlingModeHistory(nextAccess.tenantId!),
              getCuiReadyApprovalChecklists(nextAccess.tenantId!),
              getPublishedSharedResponsibilityMatrix(),
              getSharedResponsibilityMatrixAcknowledgements(nextAccess.tenantId!)
            ])
          : [[], [], null, []];
        const nextNotifications = canLoadNotifications ? await getNotifications() : [];
        const nextNotificationPreference = canLoadNotifications ? await getNotificationPreferences() : null;
        const [nextAuditLogs, nextAuditLogEntityTypeResult]: [
          PagedResult<AuditLogEntry>,
          { entityTypes: string[]; status: "idle" | "ready" | "failed" }
        ] = canLoadAuditLogs
          ? await Promise.all([
              getAuditLogs({ page: 1, pageSize: 5 }),
              getAuditLogEntityTypes()
                .then((entityTypes) => ({ entityTypes, status: "ready" as const }))
                .catch(() => ({ entityTypes: [], status: "failed" as const }))
            ])
          : [fallbackAuditLogs, { entityTypes: [], status: "idle" }];
        const nextNoCuiAcknowledgement = canLoadNoCuiStatus
          ? await getNoCuiAcknowledgementStatus()
          : fallbackNoCuiAcknowledgementStatus;
        const [nextEvidenceItems, nextClassificationReviewItems] = canLoadEvidence
          ? await Promise.all([getEvidenceItems(), getContentClassificationReviewItems()])
          : [[], []];
        const nextCompanyProfile = canLoadCompanyProfile ? await getCompanyProfile() : null;
        const nextContracts = canLoadContracts ? await getContracts() : [];
        const nextObligationDashboardItems = canLoadObligations ? await getContractObligations() : [];
        const nextCalendarEvents = canLoadCalendar ? await getCalendarEvents(defaultCalendarQuery()) : [];
        const [nextCmmcAssessments, nextCmmcControlLibrary] = canLoadCmmc
          ? await Promise.all([getCmmcAssessments(), getCmmcControlLibrary()])
          : [[], []];
        const nextCmmcControls = nextCmmcAssessments[0] ? await getCmmcControlStatuses(nextCmmcAssessments[0].id) : [];
        const nextCmmcPoamItems = nextCmmcAssessments[0] ? await getCmmcPoamItems(nextCmmcAssessments[0].id) : [];
        const nextSubcontractors = canLoadSubcontractors ? await getSubcontractors() : [];
        const nextSubcontractorFlowDowns = nextSubcontractors[0]
          ? await getSubcontractorFlowDowns(nextSubcontractors[0].id)
          : [];
        const nextSubcontractorEvidenceRequests = nextSubcontractors[0]
          ? await getSubcontractorEvidenceRequests(nextSubcontractors[0].id)
          : [];
        const [nextApprovedEvidencePackages, nextRecentReports] = canLoadReports
          ? await Promise.all([getApprovedEvidencePackages(), getRecentReports()])
          : [[], []];
        const nextContractClauses = nextContracts[0] ? await getContractClauses(nextContracts[0].id) : [];
        const nextContractDeliverables = nextContracts[0] ? await getContractDeliverables(nextContracts[0].id) : [];
        const nextContractDocuments = nextContracts[0] ? await getContractDocuments(nextContracts[0].id) : [];
        const nextExtractionResultEntries = nextContracts[0]
          ? await Promise.all(
              nextContractDocuments.map(async (document) => [
                document.id,
                await getContractDocumentExtractionResults(nextContracts[0].id, document.id, clauseCandidateReviewStatusFilter)
              ] as const)
            )
          : [];

        if (isMounted) {
          setOverview(nextOverview);
          setMembers(nextMembers);
          setInvitations(nextInvitations);
          setObligationAssignmentCandidates(nextObligationAssignmentCandidates);
          setCurrentTenant(nextTenant);
          setTenantModeHistory(nextTenantModeHistory);
          setCuiReadyChecklists(nextCuiReadyChecklists);
          setSharedResponsibilityMatrix(nextSharedResponsibilityMatrix);
          setSharedResponsibilityMatrixAcknowledgements(nextSharedResponsibilityMatrixAcknowledgements);
          setNotifications(nextNotifications);
          setNotificationPreference(nextNotificationPreference);
          setCompanyProfile(nextCompanyProfile);
          setContracts(nextContracts);
          setObligationDashboardItems(nextObligationDashboardItems);
          setSelectedObligationDetail(null);
          setObligationDashboardStatus(canLoadObligations ? "ready" : "idle");
          setContractClauses(nextContractClauses);
          setContractDeliverables(nextContractDeliverables);
          setContractDocuments(nextContractDocuments);
          setExtractionResultsByDocumentId(
            Object.fromEntries(
              nextExtractionResultEntries.filter(
                (entry): entry is readonly [string, ContractDocumentExtractionResults] => entry[1] !== null
              )
            )
          );
          setCalendarEvents(nextCalendarEvents);
          setCalendarStatus(canLoadCalendar ? "ready" : "idle");
          setSelectedContractId(nextContracts[0]?.id ?? null);
          setAuditLogs(nextAuditLogs);
          setAuditLogEntityTypes(nextAuditLogEntityTypeResult.entityTypes);
          setAuditLogEntityTypeStatus(nextAuditLogEntityTypeResult.status);
          setAuditLogStatus(canLoadAuditLogs ? "ready" : "idle");
          setNoCuiAcknowledgement(nextNoCuiAcknowledgement);
          setEvidenceItems(nextEvidenceItems);
          setClassificationReviewItems(nextClassificationReviewItems);
          setCmmcAssessments(nextCmmcAssessments);
          setSelectedCmmcAssessmentId(nextCmmcAssessments[0]?.id ?? null);
          setCmmcControlLibrary(nextCmmcControlLibrary);
          setCmmcControls(nextCmmcControls);
          setCmmcPoamItems(nextCmmcPoamItems);
          setSubcontractors(nextSubcontractors);
          setSelectedSubcontractorId(nextSubcontractors[0]?.id ?? null);
          setSubcontractorFlowDowns(nextSubcontractorFlowDowns);
          setSubcontractorEvidenceRequests(nextSubcontractorEvidenceRequests);
          setSubcontractorDetailStatus(canLoadSubcontractors ? "ready" : "idle");
          setApprovedEvidencePackages(nextApprovedEvidencePackages);
          setRecentReports(nextRecentReports);
          setSelectedReport(null);
          setReportDetailStatus("idle");
          setReportDetailMessage("");
          setReportStatus(canLoadReports ? "ready" : "idle");
          setCmmcStatus(canLoadCmmc ? "idle" : "idle");
          setSelectedEvidenceItemId(nextEvidenceItems[0]?.id ?? null);
          setLoadState("ready");
        }
      })
      .catch(() => {
        if (isMounted) {
          setOverview(fallbackOverview);
          setAccess(resolvedAccess ?? fallbackAccess);
          setAccessLoadState(resolvedAccess ? "ready" : "error");
          setMembers([]);
          setInvitations([]);
          setCurrentTenant(null);
          setTenantModeHistory([]);
          setCuiReadyChecklists([]);
          setSharedResponsibilityMatrix(null);
          setSharedResponsibilityMatrixAcknowledgements([]);
          setNotifications([]);
          setNotificationPreference(null);
          setReminderRunResult(null);
          setCompanyProfile(null);
          setContracts([]);
          setObligationDashboardItems([]);
          setSelectedObligationDetail(null);
          setObligationDashboardStatus("idle");
          setContractClauses([]);
          setContractDeliverables([]);
          setContractDocuments([]);
          setExtractionJobsByDocumentId({});
          setExtractionResultsByDocumentId({});
          setCalendarEvents([]);
          setCalendarStatus("idle");
          setSelectedContractId(null);
          setAuditLogs(fallbackAuditLogs);
          setAuditLogStatus("idle");
          setNoCuiAcknowledgement(fallbackNoCuiAcknowledgementStatus);
          setEvidenceItems([]);
          setClassificationReviewItems([]);
          setCmmcAssessments([]);
          setSelectedCmmcAssessmentId(null);
          setCmmcControls([]);
          setCmmcPoamItems([]);
          setSubcontractors([]);
          setSelectedSubcontractorId(null);
          setSubcontractorFlowDowns([]);
          setSubcontractorEvidenceRequests([]);
          setSubcontractorDetailStatus("idle");
          setApprovedEvidencePackages([]);
          setGeneratedReports([]);
          setRecentReports([]);
          setSelectedReport(null);
          setReportDetailStatus("idle");
          setReportDetailMessage("");
          setReportStatus("idle");
          setCmmcStatus("idle");
          setSelectedEvidenceItemId(null);
          setLoadState("error");
        }
      });

    return () => {
      isMounted = false;
    };
  }, [clauseCandidateReviewStatusFilter, workspaceInitialized]);

  const handleWorkspaceInitialized = useCallback(() => {
    setWorkspaceInitialized(true);
  }, []);

  async function handleInvitationSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setInviteStatus("sending");
    setInviteMessage("");

    const result = await createTenantInvitation({
      email: inviteEmail,
      roleName: inviteRole,
      expiresInDays: 7
    });

    if (result.data) {
      const createdInvitation = result.data;
      setInvitations((currentInvitations) => [createdInvitation, ...currentInvitations]);
      setInviteEmail("");
      setInviteRole("Contributor");
      setInviteStatus("created");
      setInviteMessage("Invitation created.");
      return;
    }

    setInviteStatus("failed");
    setInviteMessage(result.error ?? "Invitation was not created.");
  }

  async function handleInvitationRevoke(
    invitationId: string,
    request: RevokeTenantInvitationRequest
  ): Promise<boolean> {
    setInvitationActionStatus("revoking");
    setInvitationActionMessage("");
    setRevokingInvitationId(invitationId);

    const result = await revokeTenantInvitation(invitationId, request);
    setRevokingInvitationId(null);

    if (result.data) {
      setInvitations((currentInvitations) =>
        currentInvitations.map((invitation) =>
          invitation.invitationId === invitationId ? result.data! : invitation
        )
      );
      setInvitationActionStatus("succeeded");
      setInvitationActionMessage(`Invitation for ${result.data.email} was revoked.`);
      return true;
    }

    setInvitationActionStatus("failed");
    setInvitationActionMessage(result.error ?? "Invitation could not be revoked.");
    return false;
  }

  async function handleTenantModeUpdate(request: UpdateTenantDataHandlingModeRequest) {
    if (!currentTenant) {
      setTenantModeStatus("failed");
      setTenantModeMessage("Tenant context is still loading. Refresh the workspace or confirm your account has ManageTenant permission.");
      return;
    }

    setTenantModeStatus("saving");
    setTenantModeMessage("");

    try {
      const result = await withTimeout(
        updateTenantDataHandlingMode(currentTenant.id, request),
        tenantModeUpdateTimeoutMs,
        "Tenant data handling mode update timed out. Check the API connection and try again."
      );

      if (result.data) {
        setCurrentTenant(result.data);
        setTenantModeStatus("saved");
        setTenantModeMessage("Tenant data handling mode updated.");

        try {
          setTenantModeHistory(
            await withTimeout(
              getTenantDataHandlingModeHistory(result.data.id),
              tenantModeHistoryTimeoutMs,
              "Tenant mode updated, but history refresh timed out."
            )
          );
        } catch {
          setTenantModeMessage("Tenant data handling mode updated. History did not refresh; reload the page to confirm the audit trail.");
        }

        return;
      }

      setTenantModeStatus("failed");
      setTenantModeMessage(result.error ?? "Tenant data handling mode could not be updated.");
    } catch (error) {
      setTenantModeStatus("failed");
      setTenantModeMessage(error instanceof Error ? error.message : "Tenant data handling mode could not be updated.");
      return;
    }
  }

  async function handleDemoTenantSeed() {
    if (!currentTenant) {
      setDemoSeedStatus("failed");
      setDemoSeedMessage("Tenant context has not loaded yet. Demo seed cannot run until the active tenant is available.");
      return;
    }

    setDemoSeedStatus("saving");
    setDemoSeedMessage("");
    const result = await seedDemoTenant();

    if (!result.data) {
      setDemoSeedStatus("failed");
      setDemoSeedMessage(result.error ?? "Synthetic demo dataset could not be seeded.");
      return;
    }

    const [
      nextEvidenceItems,
      nextClassificationReviewItems,
      nextContracts,
      nextObligationDashboardItems,
      nextCmmcAssessments,
      nextCmmcControlLibrary,
      nextSubcontractors,
      nextApprovedEvidencePackages
    ] = await Promise.all([
      getEvidenceItems(),
      getContentClassificationReviewItems(),
      getContracts(),
      getContractObligations(),
      getCmmcAssessments(),
      getCmmcControlLibrary(),
      getSubcontractors(),
      getApprovedEvidencePackages()
    ]);

    const nextCmmcControls = nextCmmcAssessments[0] ? await getCmmcControlStatuses(nextCmmcAssessments[0].id) : [];
    const nextCmmcPoamItems = nextCmmcAssessments[0] ? await getCmmcPoamItems(nextCmmcAssessments[0].id) : [];

    setEvidenceItems(nextEvidenceItems);
    setClassificationReviewItems(nextClassificationReviewItems);
    setContracts(nextContracts);
    setObligationDashboardItems(nextObligationDashboardItems);
    setCmmcAssessments(nextCmmcAssessments);
    setSelectedCmmcAssessmentId(nextCmmcAssessments[0]?.id ?? null);
    setCmmcControlLibrary(nextCmmcControlLibrary);
    setCmmcControls(nextCmmcControls);
    setCmmcPoamItems(nextCmmcPoamItems);
    setSubcontractors(nextSubcontractors);
    setApprovedEvidencePackages(nextApprovedEvidencePackages);
    setDemoSeedStatus("saved");
    setDemoSeedMessage(
      `Synthetic demo dataset ${result.data.datasetVersion} seeded. Created ${result.data.createdCount} records.`
    );
  }

  async function handleCuiReadyChecklistCreate() {
    if (!currentTenant) {
      return;
    }

    setCuiReadyChecklistStatus("saving");
    setCuiReadyChecklistMessage("");
    const result = await createCuiReadyApprovalChecklist(currentTenant.id);
    if (result.data) {
      setCuiReadyChecklists((current) => [result.data!, ...current]);
      setCuiReadyChecklistStatus("saved");
      setCuiReadyChecklistMessage("CUI-ready checklist created.");
      return;
    }

    setCuiReadyChecklistStatus("failed");
    setCuiReadyChecklistMessage(result.error ?? "CUI-ready checklist could not be created.");
  }

  async function handleCuiReadyChecklistItemUpdate(
    checklistId: string,
    itemKey: string,
    request: UpdateCuiReadyChecklistItemRequest
  ) {
    if (!currentTenant) {
      return;
    }

    setCuiReadyChecklistStatus("saving");
    setCuiReadyChecklistMessage("");
    const result = await updateCuiReadyApprovalChecklistItem(currentTenant.id, checklistId, itemKey, request);
    handleCuiReadyChecklistResult(result, "Checklist item updated.");
  }

  async function handleCuiReadyChecklistReview(
    checklistId: string,
    action: "submit" | "approve" | "reject" | "supersede",
    reason: string | null
  ) {
    if (!currentTenant) {
      return;
    }

    setCuiReadyChecklistStatus("saving");
    setCuiReadyChecklistMessage("");
    const request = { reason };
    const result =
      action === "submit"
        ? await submitCuiReadyApprovalChecklist(currentTenant.id, checklistId)
        : action === "approve"
          ? await approveCuiReadyApprovalChecklist(currentTenant.id, checklistId, request)
          : action === "reject"
            ? await rejectCuiReadyApprovalChecklist(currentTenant.id, checklistId, request)
            : await supersedeCuiReadyApprovalChecklist(currentTenant.id, checklistId, request);
    handleCuiReadyChecklistResult(result, `Checklist ${action} completed.`);
  }

  function handleCuiReadyChecklistResult(
    result: { data: CuiReadyApprovalChecklist | null; error: string | null },
    successMessage: string
  ) {
    if (result.data) {
      setCuiReadyChecklists((current) =>
        current.map((checklist) => (checklist.id === result.data!.id ? result.data! : checklist))
      );
      setCuiReadyChecklistStatus("saved");
      setCuiReadyChecklistMessage(successMessage);
      return;
    }

    setCuiReadyChecklistStatus("failed");
    setCuiReadyChecklistMessage(result.error ?? "CUI-ready checklist action failed.");
  }

  async function handleSharedResponsibilityMatrixAcknowledge() {
    if (!currentTenant || !sharedResponsibilityMatrix) {
      return;
    }

    setMatrixAcknowledgementStatus("saving");
    setMatrixAcknowledgementMessage("");
    const result = await acknowledgeSharedResponsibilityMatrix(currentTenant.id, {
      matrixId: sharedResponsibilityMatrix.matrixId,
      matrixVersion: sharedResponsibilityMatrix.version,
      acknowledged: true
    });

    if (result.data) {
      setSharedResponsibilityMatrixAcknowledgements(
        await getSharedResponsibilityMatrixAcknowledgements(currentTenant.id)
      );
      setMatrixAcknowledgementStatus("saved");
      setMatrixAcknowledgementMessage("Shared responsibility matrix acknowledged.");
      return;
    }

    setMatrixAcknowledgementStatus("failed");
    setMatrixAcknowledgementMessage(result.error ?? "Shared responsibility matrix acknowledgement failed.");
  }

  async function handleNotificationRead(notificationId: string) {
    const result = await markNotificationRead(notificationId);
    if (result.data) {
      setNotifications((currentNotifications) =>
        currentNotifications.map((notification) => (notification.id === notificationId ? result.data! : notification))
      );
    }
  }

  async function handleCompanyProfileSave(request: UpsertCompanyProfileRequest) {
    setProfileStatus("saving");
    setProfileMessage("");
    setProfileValidationErrors({});

    const result = await saveCompanyProfile(request);
    if (result.data) {
      setCompanyProfile(result.data);
      setProfileStatus("saved");
      setProfileMessage(result.data.isComplete ? "Profile complete." : "Draft saved.");
      setProfileValidationErrors({});
      return;
    }

    setProfileStatus("failed");
    setProfileMessage(result.errorSummary ?? result.error ?? "Profile could not be saved.");
    setProfileValidationErrors(result.errors ?? {});
  }

  async function handleContractSave(contractId: string | null, request: UpsertContractRequest) {
    setContractStatus("saving");
    setContractMessage("");

    const result = contractId ? await updateContract(contractId, request) : await createContract(request);
    if (result.data) {
      const savedContract = result.data;
      setContracts((currentContracts) => {
        const exists = currentContracts.some((contract) => contract.id === savedContract.id);
        return exists
          ? currentContracts.map((contract) => (contract.id === savedContract.id ? savedContract : contract))
          : [savedContract, ...currentContracts];
      });
      setSelectedContractId(savedContract.id);
      setContractStatus("saved");
      setContractMessage(contractId ? "Contract updated." : "Contract created.");
      return;
    }

    setContractStatus("failed");
    setContractMessage(result.error ?? "Contract could not be saved.");
  }

  async function handleClauseSearch(params: ClauseSearchParams) {
    setClauseSearchStatus("loading");
    setClauseSearchMessage("");

    try {
      const results = await searchClauseLibrary(params);
      setClauseResults((currentResults) => mergeClauseSearchResults(currentResults, results));
      setClauseSearchStatus("ready");
      setClauseSearchMessage(
        results.length > 0
          ? `${results.length} published clause result${results.length === 1 ? "" : "s"} added or refreshed.`
          : "No new published clauses matched. Previous results were retained."
      );
    } catch {
      setClauseSearchStatus("failed");
      setClauseSearchMessage("Clause search could not be completed. Previous results were retained.");
    }
  }

  async function handleObligationFilter(params: ContractObligationQueryParams) {
    setObligationDashboardStatus("loading");
    setObligationDashboardMessage("");

    try {
      const results = await getContractObligations(params);
      setObligationDashboardItems(results);
      setObligationDashboardStatus("ready");
      setObligationDashboardMessage(
        results.length > 0 ? `${results.length} tenant-scoped obligations matched.` : "No obligations matched."
      );
    } catch {
      setObligationDashboardItems([]);
      setObligationDashboardStatus("failed");
      setObligationDashboardMessage("Obligations could not be loaded.");
    }
  }

  async function handleObligationDetailSelect(item: ContractObligationDashboardItem) {
    setObligationDetailStatus("loading");
    setObligationDetailMessage("");
    const detail = await getContractObligationDetail(item.contractClauseId, item.obligationId);

    if (detail) {
      setSelectedObligationDetail(detail);
      setObligationDetailStatus("ready");
      return;
    }

    setSelectedObligationDetail(null);
    setObligationDetailStatus("failed");
    setObligationDetailMessage("Obligation detail could not be loaded.");
  }

  async function handleObligationStatusUpdate(status: string) {
    if (!selectedObligationDetail) {
      return;
    }

    setObligationDetailStatus("saving");
    setObligationDetailMessage("");
    const result = await updateContractObligationStatus(
      selectedObligationDetail.contractClauseId,
      selectedObligationDetail.obligationId,
      status
    );

    if (result.data) {
      setSelectedObligationDetail(result.data);
      setObligationDashboardItems((currentItems) =>
        currentItems.map((item) =>
          item.contractClauseId === result.data?.contractClauseId && item.obligationId === result.data.obligationId
            ? {
                ...item,
                status: result.data.status,
                dueAt: result.data.dueAt
              }
            : item
        )
      );
      setObligationDetailStatus("ready");
      setObligationDetailMessage("Obligation status updated.");
      return;
    }

    setObligationDetailStatus("failed");
    setObligationDetailMessage(result.error ?? "Obligation status could not be updated.");
  }

  async function handleObligationOwnerAssign(kind: "user" | "role", value: string, notify: boolean) {
    if (!selectedObligationDetail) {
      return;
    }

    setObligationDetailStatus("saving");
    setObligationDetailMessage("");
    const result = await assignContractObligationOwner(
      selectedObligationDetail.contractClauseId,
      selectedObligationDetail.obligationId,
      {
        userId: kind === "user" ? value : null,
        roleName: kind === "role" ? value : null,
        notify
      }
    );

    if (result.data) {
      setSelectedObligationDetail(result.data);
      setObligationDashboardItems((currentItems) =>
        currentItems.map((item) =>
          item.contractClauseId === result.data?.contractClauseId && item.obligationId === result.data.obligationId
            ? {
                ...item,
                ownerFunction: result.data.ownerFunction,
                assignedUserId: result.data.assignedUserId,
                assignedUserDisplayName: result.data.assignedUserDisplayName,
                assignedRoleName: result.data.assignedRoleName
              }
            : item
        )
      );
      setObligationDetailStatus("ready");
      setObligationDetailMessage("Obligation owner assigned.");
      setNotifications(await getNotifications());
      return;
    }

    setObligationDetailStatus("failed");
    setObligationDetailMessage(result.error ?? "Obligation owner could not be assigned.");
  }

  async function handleContractSelect(contractId: string | null) {
    setSelectedContractId(contractId);
    setContractMessage("");
    setContractClauseMessage("");
    setDeliverableMessage("");
    setContractDocumentMessage("");
    setExtractionJobsByDocumentId({});
    setExtractionResultsByDocumentId({});
    const [nextClauses, nextDeliverables, nextDocuments] = contractId
      ? await Promise.all([getContractClauses(contractId), getContractDeliverables(contractId), getContractDocuments(contractId)])
      : [[], [], []];
    setContractClauses(nextClauses);
    setContractDeliverables(nextDeliverables);
    setContractDocuments(nextDocuments);
    if (contractId && nextDocuments.length > 0) {
      const resultEntries = await Promise.all(
        nextDocuments.map(async (document) => [document.id, await getContractDocumentExtractionResults(contractId, document.id)] as const)
      );
      setExtractionResultsByDocumentId(
        Object.fromEntries(resultEntries.filter((entry): entry is readonly [string, ContractDocumentExtractionResults] => entry[1] !== null))
      );
    }
  }

  async function handleContractClauseAttach(contractId: string, request: AttachContractClauseRequest) {
    setContractClauseStatus("saving");
    setContractMessage("");
    setContractClauseMessage("");

    const result = await attachContractClause(contractId, request);
    if (result.data) {
      setContractClauses((currentClauses) => [result.data!, ...currentClauses]);
      setContractClauseStatus("saved");
      setContractClauseMessage("Clause attached to contract.");
      return;
    }

    setContractClauseStatus("failed");
    setContractClauseMessage(result.error ?? "Clause could not be attached.");
  }

  async function handleContractClauseRemove(contractId: string, contractClauseId: string, reason: string) {
    setContractClauseStatus("saving");
    setContractClauseMessage("");

    const result = await removeContractClause(contractId, contractClauseId, { reason });
    if (result.data) {
      setContractClauses((currentClauses) => currentClauses.filter((clause) => clause.id !== contractClauseId));
      setContractClauseStatus("saved");
      setContractClauseMessage("Clause removed from contract.");
      return;
    }

    setContractClauseStatus("failed");
    setContractClauseMessage(result.error ?? "Clause could not be removed.");
  }

  async function handleContractClauseObligationGenerate(contractId: string, contractClauseId: string) {
    setContractClauseStatus("saving");
    setContractClauseMessage("");

    const result = await generateContractClauseObligations(contractId, contractClauseId);
    if (result.data) {
      const obligationCount = result.data.obligationIds.length;
      const taskCount = result.data.tasksCreated;
      setContractClauseStatus("saved");
      setContractClauseMessage(
        obligationCount === 0
          ? "No published obligation mappings are available for this clause."
          : `${obligationCount} obligation mapping${obligationCount === 1 ? "" : "s"} available; ` +
              `${taskCount} new task${taskCount === 1 ? "" : "s"} created.`
      );
      return;
    }

    setContractClauseStatus("failed");
    setContractClauseMessage(result.error ?? "Contract obligations could not be generated.");
  }

  async function handleDeliverableSave(
    contractId: string,
    deliverableId: string | null,
    request: UpsertContractDeliverableRequest
  ) {
    setDeliverableStatus("saving");
    setDeliverableMessage("");

    const result = deliverableId
      ? await updateContractDeliverable(contractId, deliverableId, request)
      : await createContractDeliverable(contractId, request);

    if (result.data) {
      const savedDeliverable = result.data;
      setContractDeliverables((currentDeliverables) => {
        const exists = currentDeliverables.some((deliverable) => deliverable.id === savedDeliverable.id);
        return exists
          ? currentDeliverables.map((deliverable) => (deliverable.id === savedDeliverable.id ? savedDeliverable : deliverable))
          : [savedDeliverable, ...currentDeliverables];
      });
      setDeliverableStatus("saved");
      setDeliverableMessage(deliverableId ? "Deliverable updated." : "Deliverable added to the contract calendar.");
      return;
    }

    setDeliverableStatus("failed");
    setDeliverableMessage(result.error ?? "Deliverable could not be saved.");
  }

  async function handleContractDocumentUpload(
    contractId: string,
    documentType: string,
    file: File | null,
    classification = "Unclassified",
    noCuiAttestation = false
  ): Promise<boolean> {
    if (!file) {
      setContractDocumentStatus("failed");
      setContractDocumentMessage("Select a contract document before upload.");
      return false;
    }

    setContractDocumentStatus("saving");
    setContractDocumentMessage("");
    const result = await uploadContractDocumentFile(
      contractId,
      documentType,
      file,
      classification,
      noCuiAttestation
    );

    if (result.data) {
      const savedDocument = result.data;
      setContractDocuments((currentDocuments) => [savedDocument, ...currentDocuments]);
      setContractDocumentStatus("saved");
      setContractDocumentMessage(
        `Document uploaded. Validation ${savedDocument.validationStatus}; malware scan ${savedDocument.malwareScanStatus}.`
      );
      return true;
    }

    setContractDocumentStatus("failed");
    setContractDocumentMessage(result.error ?? "Contract document upload was rejected.");
    return false;
  }

  async function handleContractDocumentDelete(contractId: string, documentId: string) {
    setContractDocumentStatus("saving");
    setContractDocumentMessage("");
    const result = await deleteContractDocument(contractId, documentId);

    if (!result.error) {
      setContractDocuments((currentDocuments) => currentDocuments.filter((document) => document.id !== documentId));
      setExtractionJobsByDocumentId((currentJobs) => {
        const nextJobs = { ...currentJobs };
        delete nextJobs[documentId];
        return nextJobs;
      });
      setContractDocumentStatus("saved");
      setContractDocumentMessage("Document metadata deleted.");
      return;
    }

    setContractDocumentStatus("failed");
    setContractDocumentMessage(result.error);
  }

  async function handleStartContractDocumentExtraction(contractId: string, documentId: string) {
    setContractDocumentStatus("saving");
    setContractDocumentMessage("");
    const result = await startContractDocumentExtraction(contractId, documentId);

    if (result.data) {
      const queuedJob = result.data;
      setExtractionJobsByDocumentId((currentJobs) => ({
        ...currentJobs,
        [documentId]: queuedJob
      }));
      setContractDocumentStatus("saved");
      setContractDocumentMessage(`Extraction job queued with status ${queuedJob.status}.`);
      setExtractionResultsByDocumentId((currentResults) => ({
        ...currentResults,
        [documentId]: {
          contractId,
          sourceDocumentId: documentId,
          latestJobStatus: queuedJob.status,
          failureReason: null,
          candidateCount: currentResults[documentId]?.candidateCount ?? 0,
          candidates: currentResults[documentId]?.candidates ?? []
        }
      }));
      void pollContractDocumentExtraction(contractId, documentId);
      return;
    }

    setContractDocumentStatus("failed");
    setContractDocumentMessage(result.error ?? "Extraction job could not be started.");
  }

  async function pollContractDocumentExtraction(contractId: string, documentId: string) {
    const maximumAttempts = 30;
    for (let attempt = 1; attempt <= maximumAttempts; attempt += 1) {
      await new Promise((resolve) => window.setTimeout(resolve, 1000));
      const results = await getContractDocumentExtractionResults(contractId, documentId);
      if (!results) {
        continue;
      }

      setExtractionResultsByDocumentId((currentResults) => ({
        ...currentResults,
        [documentId]: results
      }));
      setExtractionJobsByDocumentId((currentJobs) => {
        const currentJob = currentJobs[documentId];
        return currentJob && results.latestJobStatus
          ? {
              ...currentJobs,
              [documentId]: {
                ...currentJob,
                status: results.latestJobStatus,
                failureReason: results.failureReason
              }
            }
          : currentJobs;
      });

      if (results.latestJobStatus === "Completed") {
        setContractDocumentStatus("saved");
        setContractDocumentMessage(
          `Extraction completed with ${results.candidateCount} clause candidate${results.candidateCount === 1 ? "" : "s"}.`
        );
        return;
      }

      if (results.latestJobStatus === "Failed") {
        setContractDocumentStatus("failed");
        setContractDocumentMessage(results.failureReason ?? "Extraction failed.");
        return;
      }
    }

    setContractDocumentStatus("failed");
    setContractDocumentMessage(
      "Extraction is still processing. Its durable job will continue in the background; refresh this contract to check status."
    );
  }

  async function handleClauseCandidateReview(
    contractId: string,
    documentId: string,
    candidateId: string,
    action: "accept" | "reject" | "needs_clarification" | "supersede",
    clauseLibraryId: string | null
  ) {
    setContractDocumentStatus("saving");
    setContractDocumentMessage("");
    const result = await saveClauseCandidateReview(contractId, documentId, candidateId, action, clauseLibraryId);

    if (result.data) {
      setExtractionResultsByDocumentId((currentResults) => {
        const documentResults = currentResults[documentId];
        if (!documentResults) {
          return currentResults;
        }

        return {
          ...currentResults,
          [documentId]: {
            ...documentResults,
            candidates: documentResults.candidates.map((candidate) =>
              candidate.id === result.data?.id ? result.data : candidate
            )
          }
        };
      });
      if (action === "accept") {
        const nextClauses = await getContractClauses(contractId);
        setContractClauses(nextClauses);
      }

      setContractDocumentStatus("saved");
      setContractDocumentMessage(`Candidate ${result.data.reviewStatus}.`);
      return;
    }

    setContractDocumentStatus("failed");
    setContractDocumentMessage(result.error ?? "Candidate review could not be saved.");
  }

  function saveClauseCandidateReview(
    contractId: string,
    documentId: string,
    candidateId: string,
    action: "accept" | "reject" | "needs_clarification" | "supersede",
    clauseLibraryId: string | null
  ) {
    if (action === "accept") {
      return acceptClauseCandidate(contractId, documentId, candidateId, {
        clauseLibraryId,
        reason: "Reviewed from contract document extraction results."
      });
    }

    if (action === "needs_clarification") {
      return markClauseCandidateNeedsClarification(contractId, documentId, candidateId, {
        reason: "Clarification requested from contract document extraction results."
      });
    }

    if (action === "supersede") {
      return supersedeClauseCandidate(contractId, documentId, candidateId, {
        reason: "Superseded from contract document extraction results."
      });
    }

    return rejectClauseCandidate(contractId, documentId, candidateId, {
      clauseLibraryId: null,
      reason: "Rejected from contract document extraction results."
    });
  }

  async function handleNoCuiAcknowledgement() {
    setAcknowledgementStatus("saving");
    setAcknowledgementMessage("");
    const result = await acknowledgeNoCuiNotice(noCuiAcknowledgement.noticeVersion);

    if (result.data) {
      setNoCuiAcknowledgement(result.data);
      setAcknowledgementStatus("saved");
      setAcknowledgementMessage("Acknowledgement saved.");
      return;
    }

    setAcknowledgementStatus("failed");
    setAcknowledgementMessage(result.error ?? "Acknowledgement was not saved.");
  }

  async function handleEvidenceUploadIntentSubmit(
    event: FormEvent<HTMLFormElement>,
    classification: string,
    classificationReason: string,
    noCuiAttestation: boolean
  ) {
    event.preventDefault();

    if (!selectedEvidenceFile) {
      setUploadStatus("blocked");
      setUploadMessage("Select an allowed evidence file before upload.");
      return;
    }

    if (!noCuiAttestation) {
      setUploadStatus("blocked");
      setUploadMessage("Confirm the required No-CUI attestation before upload.");
      return;
    }

    setUploadStatus("creating");
    setUploadMessage("");
    const uploadIntent = await createEvidenceUploadIntent(selectedEvidenceFile, classification, classificationReason, noCuiAttestation);

    if (uploadIntent.data) {
      const classificationLabel = uploadIntent.data.classification?.classification ?? classification;
      setUploadStatus("created");
      setUploadMessage(
        `Uploaded ${uploadIntent.data.fileName}. Classification ${classificationLabel}; validation ${uploadIntent.data.validationStatus}; malware scan ${uploadIntent.data.malwareScanStatus}.`
      );
      return;
    }

    setUploadStatus("blocked");
    setUploadMessage(uploadIntent.error ?? "The API blocked the upload.");
  }

  async function handleEvidenceMetadataSave(evidenceItemId: string | null, request: UpsertEvidenceMetadataRequest) {
    setEvidenceMetadataStatus("saving");
    setEvidenceMetadataMessage("");

    const result = evidenceItemId
      ? await updateEvidenceMetadata(evidenceItemId, request)
      : await createEvidenceMetadata(request);

    if (result.data) {
      const saved = result.data;
      setEvidenceItems((currentItems) => {
        const exists = currentItems.some((item) => item.id === saved.id);
        return exists ? currentItems.map((item) => (item.id === saved.id ? saved : item)) : [saved, ...currentItems];
      });
      setClassificationReviewItems(await getContentClassificationReviewItems());
      setSelectedEvidenceItemId(saved.id);
      setEvidenceMetadataStatus("saved");
      setEvidenceMetadataMessage(evidenceItemId ? "Evidence metadata updated." : "Evidence metadata created.");
      return;
    }

    setEvidenceMetadataStatus("failed");
    setEvidenceMetadataMessage(result.error ?? "Evidence metadata could not be saved.");
  }

  async function handleEvidenceReclassify(evidenceItemId: string, request: ReclassifyContentRequest) {
    setEvidenceMetadataStatus("saving");
    setEvidenceMetadataMessage("");
    const result = await reclassifyEvidenceItem(evidenceItemId, request);
    if (result.data) {
      const saved = result.data;
      setEvidenceItems((currentItems) => currentItems.map((item) => (item.id === saved.id ? saved : item)));
      setClassificationReviewItems(await getContentClassificationReviewItems());
      setEvidenceMetadataStatus("saved");
      setEvidenceMetadataMessage("Classification updated.");
      return;
    }

    setEvidenceMetadataStatus("failed");
    setEvidenceMetadataMessage(result.error ?? "Classification could not be updated.");
  }

  async function handleCmmcAssessmentSave(assessmentId: string | null, request: UpsertCmmcAssessmentRequest) {
    setCmmcStatus("saving");
    setCmmcMessage("");
    const result = assessmentId
      ? await updateCmmcAssessment(assessmentId, request)
      : await createCmmcAssessment(request);

    if (result.data) {
      const savedAssessment = result.data;
      const [nextControls, nextPoamItems] = await Promise.all([
        getCmmcControlStatuses(savedAssessment.id),
        getCmmcPoamItems(savedAssessment.id)
      ]);
      setCmmcAssessments((currentAssessments) => {
        const exists = currentAssessments.some((assessment) => assessment.id === savedAssessment.id);
        return exists
          ? currentAssessments.map((assessment) => (assessment.id === savedAssessment.id ? savedAssessment : assessment))
          : [savedAssessment, ...currentAssessments];
      });
      setSelectedCmmcAssessmentId(savedAssessment.id);
      setCmmcControls(nextControls);
      setCmmcPoamItems(nextPoamItems);
      setCmmcStatus("saved");
      setCmmcMessage(assessmentId ? "CMMC readiness assessment updated." : "CMMC readiness assessment created.");
      return;
    }

    setCmmcStatus("failed");
    setCmmcMessage(result.error ?? "CMMC assessment could not be saved.");
  }

  async function handleCmmcAssessmentSelect(assessmentId: string) {
    setSelectedCmmcAssessmentId(assessmentId);
    setCmmcStatus("saving");
    setCmmcMessage("");

    const [nextControls, nextPoamItems] = await Promise.all([
      getCmmcControlStatuses(assessmentId),
      getCmmcPoamItems(assessmentId)
    ]);

    setCmmcControls(nextControls);
    setCmmcPoamItems(nextPoamItems);
    setCmmcStatus("idle");
  }

  function handleCmmcAssessmentNew() {
    setSelectedCmmcAssessmentId(null);
    setCmmcControls([]);
    setCmmcPoamItems([]);
    setCmmcStatus("idle");
    setCmmcMessage("");
    setCmmcPoamStatus("idle");
    setCmmcPoamMessage("");
  }

  async function handleCmmcPoamCreate(request: UpsertCmmcPoamItemRequest) {
    const assessment = cmmcAssessments.find((candidate) => candidate.id === selectedCmmcAssessmentId);
    if (!assessment) {
      setCmmcPoamStatus("failed");
      setCmmcPoamMessage("Select or create a CMMC assessment before adding POA&M items.");
      return;
    }

    setCmmcPoamStatus("saving");
    setCmmcPoamMessage("");
    const result = await createCmmcPoamItem(assessment.id, request);

    if (result.data) {
      setCmmcPoamItems((currentItems) => [result.data!, ...currentItems.filter((item) => item.id !== result.data!.id)]);
      const nextAssessments = await getCmmcAssessments();
      const nextControls = await getCmmcControlStatuses(assessment.id);
      setCmmcAssessments(nextAssessments);
      setSelectedCmmcAssessmentId(assessment.id);
      setCmmcControls(nextControls);
      setCmmcPoamStatus("saved");
      setCmmcPoamMessage("POA&M item created.");
      return;
    }

    setCmmcPoamStatus("failed");
    setCmmcPoamMessage(result.error ?? "POA&M item could not be created.");
  }

  async function handleSubcontractorCreate(request: UpsertSubcontractorRequest) {
    setSubcontractorStatus("saving");
    setSubcontractorMessage("");
    const result = await createSubcontractor(request);

    if (result.data) {
      setSubcontractors((currentItems) => [result.data!, ...currentItems.filter((item) => item.id !== result.data!.id)]);
      setSelectedSubcontractorId(result.data.id);
      setSubcontractorFlowDowns([]);
      setSubcontractorEvidenceRequests([]);
      setSubcontractorStatus("saved");
      setSubcontractorMessage("Subcontractor profile created.");
      return;
    }

    setSubcontractorStatus("failed");
    setSubcontractorMessage(result.error ?? "Subcontractor profile could not be created.");
  }

  async function handleSubcontractorSelect(subcontractorId: string) {
    setSelectedSubcontractorId(subcontractorId);
    setSubcontractorDetailStatus("loading");
    setSubcontractorDetailMessage("");
    const [nextFlowDowns, nextEvidenceRequests] = await Promise.all([
      getSubcontractorFlowDowns(subcontractorId),
      getSubcontractorEvidenceRequests(subcontractorId)
    ]);
    setSubcontractorFlowDowns(nextFlowDowns);
    setSubcontractorEvidenceRequests(nextEvidenceRequests);
    setSubcontractorDetailStatus("ready");
  }

  async function handleSubcontractorFlowDownSave(
    subcontractorId: string,
    flowDownId: string | null,
    request: UpsertSubcontractorFlowDownRequest
  ) {
    setSubcontractorDetailStatus("saving");
    setSubcontractorDetailMessage("");
    const result = flowDownId
      ? await updateSubcontractorFlowDown(subcontractorId, flowDownId, request)
      : await createSubcontractorFlowDown(subcontractorId, request);

    if (result.data) {
      const saved = result.data;
      setSubcontractorFlowDowns((currentItems) => {
        const exists = currentItems.some((item) => item.id === saved.id);
        return exists ? currentItems.map((item) => (item.id === saved.id ? saved : item)) : [saved, ...currentItems];
      });
      setSubcontractorDetailStatus("ready");
      setSubcontractorDetailMessage(flowDownId ? "Flow-down status updated." : "Flow-down assigned.");
      return;
    }

    setSubcontractorDetailStatus("failed");
    setSubcontractorDetailMessage(result.error ?? "Flow-down could not be saved.");
  }

  async function handleSubcontractorEvidenceRequestCreate(
    subcontractorId: string,
    request: UpsertSubcontractorEvidenceRequestRequest
  ) {
    setSubcontractorDetailStatus("saving");
    setSubcontractorDetailMessage("");
    const result = await createSubcontractorEvidenceRequest(subcontractorId, request);

    if (result.data) {
      setSubcontractorEvidenceRequests((currentItems) => [result.data!, ...currentItems]);
      setSubcontractorDetailStatus("ready");
      setSubcontractorDetailMessage("Subcontractor evidence request created.");
      return;
    }

    setSubcontractorDetailStatus("failed");
    setSubcontractorDetailMessage(result.error ?? "Evidence request could not be created.");
  }

  async function handleNotificationPreferenceSave(request: NotificationPreferenceUpdateRequest) {
    setNotificationPreferenceStatus("saving");
    setNotificationPreferenceMessage("");
    const result = await updateNotificationPreferences(request);

    if (result.data) {
      setNotificationPreference(result.data);
      setNotificationPreferenceStatus("saved");
      setNotificationPreferenceMessage("Notification preferences saved.");
      return;
    }

    setNotificationPreferenceStatus("failed");
    setNotificationPreferenceMessage(result.error ?? "Notification preferences could not be saved.");
  }

  async function handleDueDateReminderRun(leadTimeDays: number) {
    setNotificationPreferenceStatus("saving");
    setNotificationPreferenceMessage("");
    const result = await runDueDateReminders({ leadTimeDays, simulatedFailureTaskId: null });

    if (result.data) {
      setReminderRunResult(result.data);
      setNotificationPreferenceStatus("saved");
      setNotificationPreferenceMessage("Due-date reminder run completed.");
      const nextNotifications = await getNotifications();
      setNotifications(nextNotifications);
      return;
    }

    setNotificationPreferenceStatus("failed");
    setNotificationPreferenceMessage(result.error ?? "Due-date reminder run could not be completed.");
  }

  async function handleComplianceReportGenerate() {
    setReportStatus("loading");
    setReportMessage("");
    const result = await generateComplianceStatusReport();
    handleGeneratedReportResult(result.data, result.error, "Compliance status report generated.");
  }

  async function handleCmmcReportGenerate(assessmentId: string) {
    setReportStatus("loading");
    setReportMessage("");
    const result = await generateCmmcReadinessReport(assessmentId);
    handleGeneratedReportResult(result.data, result.error, "CMMC readiness report generated.");
  }

  async function handleSubcontractorReportGenerate(contractId?: string) {
    setReportStatus("loading");
    setReportMessage("");
    const result = await generateSubcontractorComplianceReport(contractId);
    handleGeneratedReportResult(result.data, result.error, "Subcontractor compliance report generated.");
  }

  async function handleEvidencePackageGenerate(request: EvidencePackageGenerateRequest) {
    setReportStatus("loading");
    setReportMessage("");
    const result = await generateEvidencePackage(request);

    if (result.data) {
      setGeneratedReports((currentReports) => [result.data!, ...currentReports]);
      setSelectedReport(result.data);
      setReportDetailStatus("ready");
      setReportDetailMessage("");
      setApprovedEvidencePackages(await getApprovedEvidencePackages());
      setReportStatus("ready");
      setReportMessage("Evidence package generated.");
      return;
    }

    setReportStatus("failed");
    setReportMessage(result.error ?? "Evidence package could not be generated.");
  }

  function handleGeneratedReportResult(
    report: ComplianceStatusReport | CmmcReadinessReport | SubcontractorComplianceReport | null,
    error: string | null,
    successMessage: string
  ) {
    if (report) {
      setGeneratedReports((currentReports) => [report, ...currentReports]);
      setRecentReports((currentReports) => [
        reportHistoryItem(report),
        ...currentReports.filter((candidate) => candidate.id !== report.id)
      ]);
      setSelectedReport(report);
      setReportDetailStatus("ready");
      setReportDetailMessage("");
      setReportStatus("ready");
      setReportMessage(successMessage);
      return;
    }

    setReportStatus("failed");
    setReportMessage(error ?? "Report could not be generated.");
  }

  async function handleGeneratedReportSelect(report: ReportArtifact | ReportHistoryItem) {
    if ("snapshot" in report || "manifest" in report) {
      setSelectedReport(report);
      setReportDetailStatus("ready");
      setReportDetailMessage("");
      return;
    }

    setSelectedReport(null);
    setReportDetailStatus("loading");
    setReportDetailMessage("");

    try {
      const detail = await getReportArtifact(report.id);
      setSelectedReport(detail);
      setReportDetailStatus("ready");
    } catch (error) {
      setReportDetailStatus("failed");
      setReportDetailMessage(error instanceof Error ? error.message : "Report detail could not be loaded.");
    }
  }

  async function handleApprovedEvidencePackageSelect(reportId: string) {
    setSelectedReport(null);
    setReportDetailStatus("loading");
    setReportDetailMessage("");

    try {
      const report = await getEvidencePackage(reportId);
      setSelectedReport(report);
      setReportDetailStatus("ready");
    } catch (error) {
      setReportDetailStatus("failed");
      setReportDetailMessage(error instanceof Error ? error.message : "Evidence package detail could not be loaded.");
    }
  }

  async function handleReportArchiveStateChange(reportId: string, archived: boolean, reason: string): Promise<boolean> {
    setReportStatus("loading");
    setReportMessage("");
    const result = archived
      ? await archiveReport(reportId, reason)
      : await restoreReport(reportId, reason);

    if (!result.data) {
      setReportStatus("failed");
      setReportMessage(result.error ?? `Report could not be ${archived ? "archived" : "restored"}.`);
      return false;
    }

    const updatedReport = result.data;
    setSelectedReport(updatedReport);
    setGeneratedReports((reports) =>
      reports.map((report) => (report.id === updatedReport.id ? { ...report, ...updatedReport } : report))
    );
    setRecentReports((reports) =>
      reports.map((report) =>
        report.id === updatedReport.id
          ? {
              ...report,
              status: updatedReport.status,
              archivedAt: updatedReport.archivedAt,
              archivedByUserId: updatedReport.archivedByUserId,
              archiveReason: updatedReport.archiveReason
            }
          : report
      )
    );
    setReportDetailStatus("ready");
    setReportStatus("ready");
    setReportMessage(`Report ${archived ? "archived" : "restored"} successfully.`);
    return true;
  }

  function handleReportDetailClose() {
    setSelectedReport(null);
    setReportDetailStatus("idle");
    setReportDetailMessage("");
  }

  async function handleAuditLogFilterSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await loadAuditLogs(1, auditLogFilters);
  }

  async function handleCalendarFilterSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setCalendarStatus("loading");
    setCalendarMessage("");

    try {
      const results = await getCalendarEvents({
        from: calendarFilters.from || defaultCalendarQuery().from,
        to: calendarFilters.to || defaultCalendarQuery().to,
        ...emptyStringsToUndefined(calendarFilters)
      });
      setCalendarEvents(results);
      setCalendarStatus("ready");
      setCalendarMessage(results.length > 0 ? `${results.length} calendar items matched.` : "No calendar items matched.");
    } catch {
      setCalendarEvents([]);
      setCalendarStatus("failed");
      setCalendarMessage("Calendar items could not be loaded.");
    }
  }

  async function handleAuditLogPageChange(page: number) {
    await loadAuditLogs(page, auditLogFilters);
  }

  async function loadAuditLogs(page: number, filters: AuditLogFilters) {
    setAuditLogStatus("loading");
    const nextAuditLogs = await getAuditLogs({
      page,
      pageSize: auditLogs.pageSize || 5,
      ...filters
    });

    setAuditLogs(nextAuditLogs);
    setAuditLogStatus("ready");
  }

  function handleRouteClick(route: WorkspaceRoute) {
    setActiveRoute(route);
  }

  return (
    <div className={`workspace-shell${demoCaptureMode ? " workspace-shell--demo-capture" : ""}`}>
      <a className="skip-link" href="#workspace-content">
        Skip to workspace content
      </a>
      <aside className="workspace-sidebar">
        <div className="workspace-brand">
          <img className="brand-logo" src="/F.svg" alt="" aria-hidden="true" />
          <div>
            <strong>FeDril</strong>
            <span>No-CUI workspace</span>
          </div>
        </div>
        <div className="sidebar-posture" aria-label="Workspace compliance posture">
          {import.meta.env.DEV && !demoCaptureMode ? (
            <DevelopmentTestingContextSelector currentTenantId={currentTenant?.id ?? access.tenantId} />
          ) : !import.meta.env.DEV ? (
            <TenantWorkspaceSelector
              currentTenantId={currentTenant?.id ?? access.tenantId}
              onInitialized={handleWorkspaceInitialized}
            />
          ) : null}
          <div>
            <span>Tenant</span>
            <strong>{activeTenantName}</strong>
          </div>
          <div>
            <span>Data handling</span>
            <DataHandlingBadge mode={tenantMode} />
          </div>
          <p>{overview.mvpDataPosture}</p>
        </div>
        <nav aria-label="Primary workspace navigation">
          {accessLoadState === "loading" ? (
            <p role="status">Loading authorized navigation...</p>
          ) : accessLoadState === "error" ? (
            <p role="status">Navigation unavailable until the API connection is restored.</p>
          ) : (
            visibleNavigationByGroup.map((group) => (
              <div className="workspace-nav-group" key={group.group}>
                <p>{group.group}</p>
                <ul className="workspace-nav">
                  {group.items.map((item) => {
                    const Icon = item.icon;
                    return (
                      <li key={item.route}>
                        <a
                          href={`#/${item.route}`}
                          aria-describedby={`workspace-nav-${item.route}-description`}
                          aria-label={item.label}
                          aria-current={activeRoute === item.route ? "page" : undefined}
                          onClick={() => handleRouteClick(item.route)}
                        >
                          <Icon size={18} aria-hidden="true" />
                          <span>
                            <strong>{item.label}</strong>
                            <small id={`workspace-nav-${item.route}-description`}>{item.description}</small>
                          </span>
                        </a>
                      </li>
                    );
                  })}
                </ul>
              </div>
            ))
          )}
        </nav>
        {!demoCaptureMode ? (
          <div className="workspace-sidebar__footer" aria-label="Signed-in workspace context">
            <span>Signed in</span>
            <strong>{userDisplay}</strong>
          </div>
        ) : null}
      </aside>

      <main id="workspace-content" className="workspace-main" tabIndex={-1}>
        <PageHeader
          eyebrow={`${activeNavigationItem?.group ?? "Command"} / FeDril Compliance Workspace`}
          title={activeRoute === "dashboard" ? "Dashboard" : activeNavigationItem?.label ?? "Dashboard"}
          description={
            activeRoute === "dashboard"
              ? "Operational view of tenant posture, obligations, evidence, and readiness risk."
              : activeNavigationItem?.description
          }
          actions={
            <div className="tenant-context" aria-label="Current tenant context">
              <NotificationCenter notifications={notifications} onMarkRead={handleNotificationRead} />
              <span>{activeTenantName}</span>
              <strong>{overview.mvpDataPosture}</strong>
            </div>
          }
        >
          <WorkspaceMetricStrip items={workspacePriorityMetrics} />
        </PageHeader>
        <PostureNotice currentTenant={currentTenant} />

        <WorkspaceState state={loadState} onRetry={() => window.location.reload()}>
          {activeRoute === "dashboard" ? (
            <DashboardView overview={overview} />
          ) : activeRoute === "profile" ? (
            <ProfileView
              key={`${companyProfile?.id ?? "new-profile"}-${companyProfile?.updatedAt ?? companyProfile?.createdAt ?? "draft"}`}
              canManageCompanyProfile={canManageCompanyProfile}
              profile={companyProfile}
              profileMessage={profileMessage}
              profileStatus={profileStatus}
              profileValidationErrors={profileValidationErrors}
              onProfileApplied={setCompanyProfile}
              onSave={handleCompanyProfileSave}
            />
          ) : activeRoute === "contracts" ? (
            <ContractsView
              canManageContracts={canManageContracts}
              canReviewClauses={canReviewClauses}
              clauseResults={clauseResults}
              contracts={contracts}
              contractClauses={contractClauses}
              contractClauseMessage={contractClauseMessage}
              contractClauseStatus={contractClauseStatus}
              contractDeliverables={contractDeliverables}
              contractDocuments={contractDocuments}
              extractionJobsByDocumentId={extractionJobsByDocumentId}
              extractionResultsByDocumentId={extractionResultsByDocumentId}
              clauseCandidateReviewStatusFilter={clauseCandidateReviewStatusFilter}
              deliverableMessage={deliverableMessage}
              deliverableStatus={deliverableStatus}
              contractDocumentMessage={contractDocumentMessage}
              contractDocumentStatus={contractDocumentStatus}
              contractMessage={contractMessage}
              contractStatus={contractStatus}
              noCuiAcknowledgement={noCuiAcknowledgement}
              acknowledgementMessage={acknowledgementMessage}
              acknowledgementStatus={acknowledgementStatus}
              selectedContractId={selectedContractId}
              tenantDataHandlingMode={currentTenant?.dataHandlingMode ?? "NoCui"}
              onAcknowledge={handleNoCuiAcknowledgement}
              onDeleteDocument={handleContractDocumentDelete}
              onChangeClauseCandidateReviewStatusFilter={setClauseCandidateReviewStatusFilter}
              onStartExtraction={handleStartContractDocumentExtraction}
              onReviewCandidate={handleClauseCandidateReview}
              onAttachClause={handleContractClauseAttach}
              onGenerateClauseObligations={handleContractClauseObligationGenerate}
              onRemoveClause={handleContractClauseRemove}
              onSaveDeliverable={handleDeliverableSave}
              onUploadDocument={handleContractDocumentUpload}
              onSave={handleContractSave}
              onSelectContract={handleContractSelect}
            />
          ) : activeRoute === "obligations" ? (
            <ObligationsView
              contracts={contracts}
              canManageObligations={canManageObligations}
              currentRoles={access.roles}
              currentUserId={access.userId}
              detail={selectedObligationDetail}
              detailMessage={obligationDetailMessage}
              detailStatus={obligationDetailStatus}
              items={obligationDashboardItems}
              assignmentCandidates={obligationAssignmentCandidates}
              message={obligationDashboardMessage}
              status={obligationDashboardStatus}
              onDetailSelect={handleObligationDetailSelect}
              onFilter={handleObligationFilter}
              onOwnerAssign={handleObligationOwnerAssign}
              onStatusUpdate={handleObligationStatusUpdate}
              clauseLibrary={
                <ClauseLibraryView
                  results={clauseResults}
                  searchMessage={clauseSearchMessage}
                  searchStatus={clauseSearchStatus}
                  onSearch={handleClauseSearch}
                />
              }
            />
          ) : activeRoute === "evidence" ? (
            <EvidenceView
              acknowledgement={noCuiAcknowledgement}
              acknowledgementMessage={acknowledgementMessage}
              acknowledgementStatus={acknowledgementStatus}
              canManageEvidence={canManageEvidence}
              controls={cmmcControlLibrary}
              evidenceItems={evidenceItems}
              evidenceMetadataMessage={evidenceMetadataMessage}
              evidenceMetadataStatus={evidenceMetadataStatus}
              obligationItems={obligationDashboardItems}
              selectedEvidenceItemId={selectedEvidenceItemId}
              selectedFile={selectedEvidenceFile}
              uploadMessage={uploadMessage}
              uploadStatus={uploadStatus}
              classificationReviewItems={classificationReviewItems}
              onAcknowledge={handleNoCuiAcknowledgement}
              onFileSelected={setSelectedEvidenceFile}
              onReclassifyEvidence={handleEvidenceReclassify}
              onMetadataSave={handleEvidenceMetadataSave}
              onSelectEvidence={setSelectedEvidenceItemId}
              onUploadIntentSubmit={handleEvidenceUploadIntentSubmit}
            />
          ) : activeRoute === "calendar" ? (
            <CalendarView
              contracts={contracts}
              events={calendarEvents}
              filters={calendarFilters}
              message={calendarMessage}
              status={calendarStatus}
              onFilterChange={setCalendarFilters}
              onFilterSubmit={handleCalendarFilterSubmit}
            />
          ) : activeRoute === "cmmc" ? (
            <CmmcView
              key={selectedCmmcAssessmentId ?? "new-assessment"}
              assessments={cmmcAssessments}
              canManageCmmc={canManageCmmc}
              controls={cmmcControls}
              contracts={contracts}
              message={cmmcMessage}
              poamItems={cmmcPoamItems}
              poamMessage={cmmcPoamMessage}
              poamStatus={cmmcPoamStatus}
              selectedAssessmentId={selectedCmmcAssessmentId}
              status={cmmcStatus}
              onSave={handleCmmcAssessmentSave}
              onCreatePoam={handleCmmcPoamCreate}
              onNewAssessment={handleCmmcAssessmentNew}
              onSelectAssessment={handleCmmcAssessmentSelect}
            />
          ) : activeRoute === "subcontractors" ? (
            <SubcontractorsView
              canManageSubcontractors={access.permissions.includes("ManageSubcontractors")}
              contracts={contracts}
              detailMessage={subcontractorDetailMessage}
              detailStatus={subcontractorDetailStatus}
              evidenceItems={evidenceItems}
              evidenceRequests={subcontractorEvidenceRequests}
              flowDowns={subcontractorFlowDowns}
              message={subcontractorMessage}
              obligationItems={obligationDashboardItems}
              onCreateEvidenceRequest={handleSubcontractorEvidenceRequestCreate}
              onSaveFlowDown={handleSubcontractorFlowDownSave}
              onSubcontractorApplied={(updated) =>
                setSubcontractors((current) => current.map((subcontractor) => (subcontractor.id === updated.id ? updated : subcontractor)))
              }
              status={subcontractorStatus}
              selectedSubcontractorId={selectedSubcontractorId}
              subcontractors={subcontractors}
              onCreate={handleSubcontractorCreate}
              onSelect={handleSubcontractorSelect}
            />
          ) : activeRoute === "reports" ? (
            <ReportsView
              approvedEvidencePackages={approvedEvidencePackages}
              assessments={cmmcAssessments}
              canArchiveReports={canArchiveReports}
              canExportReports={canExportReports}
              canManageReports={canManageReports}
              controls={cmmcControlLibrary}
              contracts={contracts}
              evidenceItems={evidenceItems}
              generatedReports={generatedReports}
              recentReports={recentReports}
              message={reportMessage}
              obligationItems={obligationDashboardItems}
              reportDetailMessage={reportDetailMessage}
              reportDetailStatus={reportDetailStatus}
              selectedReport={selectedReport}
              status={reportStatus}
              subcontractors={subcontractors}
              onApprovedEvidencePackageSelect={handleApprovedEvidencePackageSelect}
              onCmmcReportGenerate={handleCmmcReportGenerate}
              onComplianceReportGenerate={handleComplianceReportGenerate}
              onEvidencePackageGenerate={handleEvidencePackageGenerate}
              onGeneratedReportSelect={handleGeneratedReportSelect}
              onReportArchiveStateChange={handleReportArchiveStateChange}
              onReportDetailClose={handleReportDetailClose}
              onSubcontractorReportGenerate={handleSubcontractorReportGenerate}
            />
          ) : activeRoute === "settings" ? (
            <SettingsView
              canManageTenant={canManageTenant}
              canSeedDemoDataset={canManageObligations}
              canManageUsers={canManageUsers}
              canViewAuditLog={canViewAuditLog}
              auditLogEntityTypes={auditLogEntityTypes}
              auditLogEntityTypeStatus={auditLogEntityTypeStatus}
              auditLogFilters={auditLogFilters}
              auditLogStatus={auditLogStatus}
              auditLogs={auditLogs}
              cmmcAssessments={cmmcAssessments}
              cmmcPoamItems={cmmcPoamItems}
              evidenceItems={evidenceItems}
              currentTenant={currentTenant}
              currentUserId={access.userId}
              demoSeedMessage={demoSeedMessage}
              demoSeedStatus={demoSeedStatus}
              cuiReadyChecklists={cuiReadyChecklists}
              cuiReadyChecklistMessage={cuiReadyChecklistMessage}
              cuiReadyChecklistStatus={cuiReadyChecklistStatus}
              inviteEmail={inviteEmail}
              inviteMessage={inviteMessage}
              inviteRole={inviteRole}
              inviteStatus={inviteStatus}
              invitationActionMessage={invitationActionMessage}
              invitationActionStatus={invitationActionStatus}
              invitations={invitations}
              members={members}
              revokingInvitationId={revokingInvitationId}
              notificationPreference={notificationPreference}
              notificationPreferenceMessage={notificationPreferenceMessage}
              notificationPreferenceStatus={notificationPreferenceStatus}
              reminderRunResult={reminderRunResult}
              sharedResponsibilityMatrix={sharedResponsibilityMatrix}
              sharedResponsibilityMatrixAcknowledgements={sharedResponsibilityMatrixAcknowledgements}
              sharedResponsibilityMatrixAcknowledgementMessage={matrixAcknowledgementMessage}
              sharedResponsibilityMatrixAcknowledgementStatus={matrixAcknowledgementStatus}
              tenantModeHistory={tenantModeHistory}
              tenantModeMessage={tenantModeMessage}
              tenantModeStatus={tenantModeStatus}
              onAuditLogFilterChange={setAuditLogFilters}
              onAuditLogFilterSubmit={handleAuditLogFilterSubmit}
              onAuditLogPageChange={handleAuditLogPageChange}
              onDueDateReminderRun={handleDueDateReminderRun}
              onCuiReadyChecklistCreate={handleCuiReadyChecklistCreate}
              onCuiReadyChecklistItemUpdate={handleCuiReadyChecklistItemUpdate}
              onCuiReadyChecklistReview={handleCuiReadyChecklistReview}
              onDemoTenantSeed={handleDemoTenantSeed}
              onSharedResponsibilityMatrixAcknowledge={handleSharedResponsibilityMatrixAcknowledge}
              onInviteEmailChange={setInviteEmail}
              onInviteRoleChange={setInviteRole}
              onInvitationSubmit={handleInvitationSubmit}
              onInvitationRevoke={handleInvitationRevoke}
              onNotificationPreferenceSave={handleNotificationPreferenceSave}
              onTenantModeUpdate={handleTenantModeUpdate}
            />
          ) : (
            <DashboardView overview={overview} />
          )}
        </WorkspaceState>
      </main>
    </div>
  );
}

function NotificationCenter({
  notifications,
  onMarkRead
}: {
  notifications: NotificationCenterItem[];
  onMarkRead: (notificationId: string) => Promise<void>;
}) {
  const unreadCount = notifications.filter((notification) => !notification.readAt).length;
  const visibleNotifications = notifications.slice(0, 6);

  return (
    <details className="notification-center">
      <summary aria-label={`Notifications${unreadCount > 0 ? `, ${unreadCount} unread` : ""}`}>
        <Bell size={17} aria-hidden="true" />
        {unreadCount > 0 ? <span>{unreadCount}</span> : null}
      </summary>
      <div className="notification-center__panel" role="list" aria-label="Notifications">
        <div className="notification-center__header">
          <strong>Notifications</strong>
          <small>{unreadCount} unread</small>
        </div>
        {visibleNotifications.length > 0 ? (
          visibleNotifications.map((notification) => (
            <article
              className={`notification-center__item${notification.readAt ? "" : " notification-center__item--unread"}`}
              key={notification.id}
              role="listitem"
            >
              <div>
                <strong>{notification.placeholder}</strong>
                <small>
                  {notification.sourceType} · {formatUsDateTime(notification.attemptedAt)}
                </small>
              </div>
              <div className="notification-center__actions">
                <a href={getNotificationOpenUrl(notification.linkUrl)}>
                  Open
                </a>
                {!notification.readAt ? (
                  <button type="button" onClick={() => void onMarkRead(notification.id)} aria-label="Mark notification as read">
                    <CheckCircle2 size={15} aria-hidden="true" />
                  </button>
                ) : null}
              </div>
            </article>
          ))
        ) : (
          <p>No notifications.</p>
        )}
      </div>
    </details>
  );
}

function WorkspaceState({ children, onRetry, state }: { children: ReactNode; onRetry: () => void; state: LoadState }) {
  if (state === "loading") {
    return (
      <section className="route-state" aria-live="polite">
        <span className="state-dot" aria-hidden="true" />
        <h2>Loading workspace data</h2>
        <p>Retrieving tenant-scoped modules, permissions, and dashboard content.</p>
      </section>
    );
  }

  if (state === "error") {
    return (
      <section className="route-state route-state--error" role="alert">
        <h2>Workspace data could not be loaded</h2>
        <p>Check the API connection, authentication context, and active tenant before relying on this workspace.</p>
        <Button icon={<RefreshCw size={16} aria-hidden="true" />} onClick={onRetry} variant="primary">
          Retry connection
        </Button>
      </section>
    );
  }

  return children;
}

function CalendarView({
  contracts,
  events,
  filters,
  message,
  onFilterChange,
  onFilterSubmit,
  status
}: {
  contracts: ContractRecord[];
  events: CalendarEvent[];
  filters: CalendarFilters;
  message: string;
  status: "idle" | "loading" | "ready" | "failed";
  onFilterChange: (filters: CalendarFilters) => void;
  onFilterSubmit: (event: FormEvent<HTMLFormElement>) => void;
}) {
  const statusFilterRef = useRef<HTMLLabelElement>(null);
  const riskFilterRef = useRef<HTMLLabelElement>(null);
  const monthSummaryRef = useRef<HTMLDivElement>(null);
  const agendaListRef = useRef<HTMLDivElement>(null);
  const metrics = useMemo(
    () => ({
      total: events.length,
      overdue: events.filter((event) => event.isOverdue).length,
      highRisk: events.filter((event) => event.riskLevel === "High").length,
      months: new Set(events.map((event) => event.date.slice(0, 7))).size
    }),
    [events]
  );
  const ownerOptions = useMemo(() => {
    const values = new Set(["Contracts", "IT/security", "ComplianceManager", "Security", "reports", "Subcontractors"]);
    events.forEach((event) => {
      if (event.ownerFunction.trim()) {
        values.add(event.ownerFunction);
      }
    });

    return Array.from(values).sort((a, b) => a.localeCompare(b));
  }, [events]);
  const scrollToCalendarSection = (target: RefObject<HTMLElement | null>) => {
    target.current?.scrollIntoView?.({ behavior: isDemoCaptureMode() ? "auto" : "smooth", block: "start" });
    target.current?.focus?.();
  };

  return (
    <section className="route-panel" aria-label="Compliance calendar">
      <div className="route-panel__intro section-heading--split">
        <div>
          <p className="eyebrow">Compliance calendar</p>
          <h2>Calendar agenda</h2>
          <p>Tenant-scoped tasks, renewals, reports, contract deadlines, deliverables, and policy reviews.</p>
        </div>
        <div className="queue-metrics calendar-summary-metrics" aria-label="Calendar summary">
          <button type="button" onClick={() => scrollToCalendarSection(agendaListRef)} aria-label={`${metrics.total} total calendar agenda items`}>
            <strong>{metrics.total}</strong> agenda items
          </button>
          <button
            type="button"
            onClick={() => scrollToCalendarSection(statusFilterRef)}
            aria-label={`${metrics.overdue} overdue calendar items; jump to status filter`}
          >
            <strong>{metrics.overdue}</strong> overdue items
          </button>
          <button
            type="button"
            onClick={() => scrollToCalendarSection(riskFilterRef)}
            aria-label={`${metrics.highRisk} high-risk calendar items; jump to risk filter`}
          >
            <strong>{metrics.highRisk}</strong> high-risk items
          </button>
          <button
            type="button"
            onClick={() => scrollToCalendarSection(monthSummaryRef)}
            aria-label={`${metrics.months} calendar months represented; jump to month summary`}
          >
            <strong>{metrics.months}</strong> months represented
          </button>
        </div>
      </div>

      <form className="calendar-filter-form" onSubmit={onFilterSubmit}>
        <label>
          From
          <input
            type="date"
            value={filters.from}
            onChange={(event) => onFilterChange({ ...filters, from: event.target.value })}
          />
        </label>
        <label>
          To
          <input
            type="date"
            value={filters.to}
            onChange={(event) => onFilterChange({ ...filters, to: event.target.value })}
          />
        </label>
        <label>
          Owner
          <input
            aria-label="Owner"
            list="calendar-owner-options"
            value={filters.owner}
            onChange={(event) => onFilterChange({ ...filters, owner: event.target.value })}
            placeholder="Any owner"
          />
          <datalist id="calendar-owner-options">
            {ownerOptions.map((ownerOption) => (
              <option key={ownerOption} value={ownerOption}>
                {formatOwnerLabel(ownerOption)}
              </option>
            ))}
          </datalist>
        </label>
        <label ref={statusFilterRef} id="calendar-status-filter" tabIndex={-1}>
          Status
          <select
            aria-label="Status"
            value={filters.status}
            onChange={(event) => onFilterChange({ ...filters, status: event.target.value })}
          >
            <option value="">Any status</option>
            {[
              "open",
              "in_progress",
              "waiting_for_review",
              "completed",
              "canceled",
              "overdue",
              "satisfied",
              "NotStarted",
              "InProgress",
              "Submitted",
              "Accepted",
              "Late",
              "Waived",
              "Queued",
              "Generating",
              "Complete",
              "Failed",
              "Archived"
            ].map((statusOption) => (
              <option key={statusOption} value={statusOption}>
                {formatEnumLabel(statusOption)}
              </option>
            ))}
          </select>
        </label>
        <label ref={riskFilterRef} id="calendar-risk-filter" tabIndex={-1}>
          Risk
          <select aria-label="Risk" value={filters.risk} onChange={(event) => onFilterChange({ ...filters, risk: event.target.value })}>
            <option value="">Any</option>
            <option value="Low">Low</option>
            <option value="Medium">Medium</option>
            <option value="High">High</option>
            <option value="Critical">Critical</option>
          </select>
        </label>
        <label>
          Contract
          <select
            aria-label="Contract"
            value={filters.contractId}
            onChange={(event) => onFilterChange({ ...filters, contractId: event.target.value })}
          >
            <option value="">Any</option>
            {contracts.map((contract) => (
              <option key={contract.id} value={contract.id}>
                {contract.contractNumber}
              </option>
            ))}
          </select>
        </label>
        <label>
          Module
          <select aria-label="Module" value={filters.module} onChange={(event) => onFilterChange({ ...filters, module: event.target.value })}>
            <option value="">Any</option>
            <option value="Contract">Contract</option>
            <option value="CMMC">CMMC</option>
            <option value="Evidence">Evidence</option>
            <option value="Obligations">Obligations</option>
            <option value="Policy reviews">Policy reviews</option>
            <option value="Renewals">Renewals</option>
            <option value="Reports">Reports</option>
            <option value="Subcontractors">Subcontractors</option>
            <option value="Tasks">Tasks</option>
          </select>
        </label>
        <button type="submit" disabled={status === "loading"}>
          <SlidersHorizontal size={16} aria-hidden="true" />
          Apply filters
        </button>
      </form>

      {message ? (
        <p
          className={`form-message form-message--${status === "failed" ? "error" : "success"}`}
          role={status === "failed" ? "alert" : "status"}
        >
          {message}
        </p>
      ) : null}

      <div className="calendar-workspace">
        <div ref={monthSummaryRef} id="calendar-month-summary" className="calendar-month-strip" tabIndex={-1} aria-label="Month view summary">
          {events.length === 0 ? (
            <span>No calendar items yet</span>
          ) : (
            Object.entries(groupEventsByMonth(events)).map(([month, monthEvents]) => (
              <span key={month}>
                <strong>{formatMonthLabel(month)}</strong>
                {monthEvents.length} items
              </span>
            ))
          )}
        </div>

        <div ref={agendaListRef} id="calendar-agenda-list" className="calendar-agenda" tabIndex={-1} aria-label="Calendar list view">
          {events.length === 0 ? (
            <EmptyState
              title="No calendar items yet"
              body="Calendar items must have dates and fall inside the selected From and To range. Widen the date range or add due dates to obligation tasks, deliverables, renewals, reports, or policy reviews."
            />
          ) : (
            events.map((event) => (
              <article
                key={event.id}
                className={`calendar-event${event.isOverdue ? " calendar-event--overdue" : ""}`}
                aria-label={event.isOverdue ? "Overdue calendar item" : "Calendar item"}
              >
                <div className="calendar-event__date">
                  <strong>{formatDayLabel(event.date)}</strong>
                  <span>{formatMonthLabel(event.date.slice(0, 7))}</span>
                </div>
                <div className="calendar-event__body">
                  <div className="calendar-event__title">
                    <h3>{event.title}</h3>
                    {event.isOverdue ? (
                      <span className="risk-badge risk-badge--overdue">
                        <AlertTriangle size={14} aria-hidden="true" />
                        Overdue
                      </span>
                    ) : null}
                  </div>
                  <p>{formatCategory(event.category)}</p>
                  <dl>
                    <div>
                      <dt>Owner</dt>
                      <dd>{formatOwnerLabel(event.ownerFunction)}</dd>
                    </div>
                    <div>
                      <dt>Status</dt>
                      <dd>{formatStatus(event.status)}</dd>
                    </div>
                    <div>
                      <dt>Risk</dt>
                      <dd>{event.riskLevel}</dd>
                    </div>
                    <div>
                      <dt>Module</dt>
                      <dd>{event.module}</dd>
                    </div>
                  </dl>
                </div>
              </article>
            ))
          )}
        </div>
      </div>
    </section>
  );
}

function groupEventsByMonth(events: CalendarEvent[]): Record<string, CalendarEvent[]> {
  return events.reduce<Record<string, CalendarEvent[]>>((groups, event) => {
    const month = event.date.slice(0, 7);
    groups[month] = [...(groups[month] ?? []), event];
    return groups;
  }, {});
}

function formatMonthLabel(month: string) {
  const [year, monthIndex] = month.split("-").map(Number);
  return new Intl.DateTimeFormat("en-US", { month: "short", year: "numeric", timeZone: "UTC" }).format(
    new Date(Date.UTC(year, monthIndex - 1, 1))
  );
}

function formatDayLabel(date: string) {
  const [year, monthIndex, day] = date.split("-").map(Number);
  return new Intl.DateTimeFormat("en-US", { day: "2-digit", timeZone: "UTC" }).format(
    new Date(Date.UTC(year, monthIndex - 1, day))
  );
}

function formatCategory(category: string) {
  return category
    .split("_")
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

function formatStatus(status: string) {
  return formatCategory(status);
}

function formatEnumLabel(value: string) {
  return value
    .replace(/_/g, " ")
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/\b\w/g, (letter) => letter.toUpperCase());
}

function formatOwnerLabel(owner: string | null | undefined) {
  return (owner || "Unassigned")
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/^reports$/i, "Reports");
}

type UiTone = "neutral" | "success" | "warning" | "danger" | "info";

function statusTone(status: string): UiTone {
  const normalized = status.toLowerCase();
  if (normalized.includes("overdue") || normalized.includes("blocked") || normalized.includes("rejected") || normalized.includes("late")) {
    return "danger";
  }
  if (
    normalized.includes("review") ||
    normalized.includes("progress") ||
    normalized.includes("submitted") ||
    normalized.includes("sent") ||
    normalized.includes("draft") ||
    normalized.includes("requested")
  ) {
    return "warning";
  }
  if (
    normalized.includes("done") ||
    normalized.includes("accepted") ||
    normalized.includes("approved") ||
    normalized.includes("satisfied") ||
    normalized.includes("implemented") ||
    normalized.includes("complete")
  ) {
    return "success";
  }
  return "neutral";
}

function riskIndicatorTone(level: string): UiTone {
  const normalized = level.toLowerCase();
  if (normalized.includes("high") || normalized.includes("critical")) {
    return "danger";
  }
  if (normalized.includes("medium")) {
    return "warning";
  }
  if (normalized.includes("low")) {
    return "success";
  }
  return "neutral";
}

function formatHighRiskObligationHint(count: number): string {
  return `${count} high-risk ${count === 1 ? "obligation" : "obligations"}`;
}

function confidenceTone(confidence: string | null | undefined): UiTone {
  const normalized = (confidence ?? "").toLowerCase();
  if (normalized.includes("low") || normalized.includes("unknown")) {
    return "danger";
  }
  if (normalized.includes("medium") || normalized.includes("draft")) {
    return "warning";
  }
  if (normalized.includes("high") || normalized.includes("reviewed")) {
    return "success";
  }
  return "info";
}

function ScanMeta({
  items
}: {
  items: Array<{ label: string; value: ReactNode; tone?: UiTone }>;
}) {
  return (
    <dl className="scan-meta-grid">
      {items.map((item) => (
        <div className={item.tone ? `scan-meta-grid__item scan-meta-grid__item--${item.tone}` : "scan-meta-grid__item"} key={item.label}>
          <dt>{item.label}</dt>
          <dd>{item.value}</dd>
        </div>
      ))}
    </dl>
  );
}

function EvidenceRequirementChips({ items }: { items: string[] }) {
  if (!items.length) {
    return <span className="muted-inline">No required evidence mapped</span>;
  }

  return (
    <div className="evidence-requirement-chips" aria-label="Required evidence">
      {items.slice(0, 4).map((item) => (
        <span key={item}>{item}</span>
      ))}
      {items.length > 4 ? <strong>+{items.length - 4}</strong> : null}
    </div>
  );
}

function DataQualityWarnings({ warnings }: { warnings: string[] }) {
  if (!warnings.length) {
    return null;
  }

  return (
    <div className="data-quality-warnings" role="note" aria-label="Data quality warnings">
      <strong>Data quality</strong>
      <ul>
        {warnings.map((warning) => (
          <li key={warning}>{warning}</li>
        ))}
      </ul>
    </div>
  );
}

function obligationQualityWarnings(item: ContractObligationDashboardItem) {
  const warnings: string[] = [];
  if (!item.dueAt) warnings.push("No due date mapped.");
  if (!item.ownerFunction?.trim()) warnings.push("No owner function mapped.");
  if (item.evidenceExamples.length === 0) warnings.push("No required evidence examples mapped.");
  if (!item.confidence || confidenceTone(item.confidence) === "danger") warnings.push("Source confidence needs review.");
  if (!item.lastReviewedAt) warnings.push("Source review date is missing.");
  if (item.requiresExpertReview) warnings.push("Expert review is required before relying on this obligation.");
  return warnings;
}

function evidenceQualityWarnings(item: EvidenceMetadata) {
  const warnings: string[] = [];
  if (!item.ownerFunction?.trim()) warnings.push("No evidence owner mapped.");
  if (item.obligationIds.length === 0 && item.controlIds.length === 0) warnings.push("Evidence is not linked to an obligation or control.");
  if (item.expiresAt && item.expiresAt < new Date().toISOString().slice(0, 10)) warnings.push("Evidence is expired.");
  if (["Unknown", "Prohibited", "Cui"].includes(item.classification.classification)) warnings.push("Classification requires reviewer attention.");
  return warnings;
}

function cmmcControlQualityWarnings(control: CmmcControlStatus) {
  const warnings: string[] = [];
  if (control.evidenceItemIds.length === 0) warnings.push("No evidence linked to this control.");
  if (!control.sourceLastReviewedAt) warnings.push("Source review date is missing.");
  if (confidenceTone(control.sourceConfidence) === "danger") warnings.push("Source confidence needs review.");
  if (control.poamItemIds.length > 0 && control.taskIds.length === 0) warnings.push("POA&M exists without a linked task.");
  return warnings;
}

function subcontractorQualityWarnings(subcontractor: Subcontractor, flowDowns: SubcontractorFlowDown[], evidenceRequests: SubcontractorEvidenceRequest[]) {
  const warnings: string[] = [];
  if ((subcontractor.hasCuiAccess || subcontractor.hasExportControlledAccess) && !subcontractor.requiredCmmcLevel) {
    warnings.push("Sensitive access is enabled without a required CMMC level.");
  }
  if (subcontractor.insuranceExpiresAt && subcontractor.insuranceExpiresAt < new Date().toISOString().slice(0, 10)) {
    warnings.push("Insurance is expired.");
  }
  if (subcontractor.ndaStatus === "Unknown" || subcontractor.ndaStatus === "NotOnFile" || subcontractor.ndaStatus === "Expired") {
    warnings.push("NDA status needs review.");
  }
  if (flowDowns.length === 0) warnings.push("No flow-down clauses assigned.");
  if (evidenceRequests.some((request) => request.isOverdue)) warnings.push("One or more evidence requests are overdue.");
  return warnings;
}

function DashboardView({ overview }: { overview: ComplianceOverview }) {
  const demoCaptureMode = isDemoCaptureMode();
  const hasModules = overview.modules.length > 0;
  const hasPriorityObligations = overview.priorityObligations.length > 0;
  const hasAlerts = overview.alerts.length > 0;

  return (
    <>
      <section className="workspace-hero">
        <div>
          <p className="eyebrow">Workspace overview</p>
          <h2>GovCon obligations, evidence, and readiness in one operating view.</h2>
          <p className="hero-copy">
            {demoCaptureMode
              ? "Centralize readiness activities, ownership, evidence metadata, gaps, and remediation in one operating view."
              : overview.productPromise}
          </p>
        </div>
        <div className="hero-panel" aria-label="MVP platform posture">
          <span>Current data posture</span>
          <strong>{overview.mvpDataPosture}</strong>
        </div>
      </section>

      <section className="metrics-grid" aria-label="Workspace metrics">
        <MetricTile className="metric" label="Priority obligations" tone="success" value={overview.priorityObligations.length} />
        <MetricTile
          className="metric"
          label={demoCaptureMode ? "Readiness scope" : "MVP modules"}
          tone="success"
          value={demoCaptureMode ? "Current" : overview.modules.length}
        />
        <MetricTile className="metric" label="Evidence posture" tone="success" value="No-CUI" />
        <MetricTile className="metric" label="Source status" tone="success" value={demoCaptureMode ? "Source-backed" : "Seeded"} />
      </section>

      <section className="work-grid" aria-label="Dashboard alerts">
        <div>
          <div className="section-heading">
            <p className="eyebrow">Action queue</p>
            <h2>Dashboard alerts</h2>
          </div>
          <div className="obligation-list">
            {hasAlerts ? (
              overview.alerts.map((alert) => (
                <TaskCard
                  badges={
                    <span className={`risk risk--${alert.severity.toLowerCase()}`}>{alert.severity}</span>
                  }
                  className="obligation-item"
                  key={`${alert.alertType}-${alert.entityType}-${alert.entityId}`}
                  meta={[
                    { label: "Type", value: formatEnumLabel(alert.alertType) },
                    { label: "Detected", value: alert.detectedUtc }
                  ]}
                  summary={demoCaptureMode ? sanitizeDemoCaptureText(alert.message) : alert.message}
                  title={alert.title}
                />
              ))
            ) : (
              <EmptyState
                title="No dashboard alerts"
                body="Tenant-scoped checks did not find overdue, rejected, missing-evidence, or access-risk items."
              />
            )}
          </div>
        </div>
      </section>

      {!demoCaptureMode ? <section className="work-grid" aria-label="Compliance operations">
        <div>
          <div className="section-heading">
            <p className="eyebrow">MVP modules</p>
            <h2>Application structure</h2>
          </div>
          <div className="module-grid">
            {hasModules ? (
              overview.modules.map((module, index) => {
                const Icon = moduleIcons[index % moduleIcons.length];
                return <ModuleCard key={module.key} module={module} icon={Icon} />;
              })
            ) : (
              <EmptyState
                title="API overview unavailable"
                body="Backend source data must load before module status is shown."
              />
            )}
          </div>
        </div>

        <aside className="obligation-rail" aria-label="Priority source-backed obligations">
          <div className="section-heading">
            <p className="eyebrow">Source-backed</p>
            <h2>Priority obligations</h2>
          </div>
          <div className="obligation-list">
            {hasPriorityObligations ? (
              overview.priorityObligations.map((obligation) => (
                <TaskCard
                  badges={
                    <span className={`risk risk--${obligation.riskLevel.toLowerCase()}`}>{obligation.riskLevel}</span>
                  }
                  className="obligation-item"
                  key={obligation.id}
                  meta={[
                    { label: "Owner", value: formatOwnerLabel(obligation.ownerFunction) },
                    { label: "Reviewed", value: obligation.lastReviewedAt }
                  ]}
                  summary={obligation.title}
                  title={obligation.source}
                />
              ))
            ) : (
              <EmptyState
                title="Source data unavailable"
                body="Priority obligations are provided by the API, not by UI-only fallback content."
              />
            )}
          </div>
        </aside>
      </section> : null}
    </>
  );
}

function profileToForm(profile: CompanyProfile | null): ProfileFormState {
  const primaryLocation = profile?.locations[0];

  return {
    ...defaultProfileForm,
    legalEntityName: profile?.legalEntityName ?? "",
    doingBusinessAs: profile?.doingBusinessAs ?? "",
    uei: profile?.uei ?? "",
    cageCode: profile?.cageCode ?? "",
    samRegistrationExpiresAt: profile?.samRegistrationExpiresAt ?? "",
    naicsRows:
      profile && profile.naicsCodes.length > 0
        ? profile.naicsCodes.map((naics) => ({
            code: naics.code,
            title: naics.title,
            isPrimary: naics.isPrimary,
            sizeStandard: naics.sizeStandard ?? "",
            qualifiesAsSmall: naics.qualifiesAsSmall === null ? "" : String(naics.qualifiesAsSmall)
          }))
        : defaultProfileForm.naicsRows,
    certificationRows:
      profile && profile.certifications.length > 0
        ? profile.certifications.map((certification) => ({
            id: certification.id,
            type: certification.type,
            status: certification.status,
            issuer: certification.issuer,
            effectiveAt: certification.effectiveAt ?? "",
            expiresAt: certification.expiresAt ?? "",
            referenceNumber: certification.referenceNumber ?? ""
          }))
        : defaultProfileForm.certificationRows,
    agencyCustomers: profile?.agencyCustomers.join(", ") ?? "",
    contractorRole: profile?.contractorRole ?? "Unknown",
    productsAndServices: profile?.productsAndServices ?? "",
    employeeRange: profile?.employeeRange ?? "Unknown",
    revenueRange: profile?.revenueRange ?? "Unknown",
    locationName: primaryLocation?.name ?? "",
    street1: primaryLocation?.street1 ?? "",
    city: primaryLocation?.city ?? "",
    stateOrProvince: primaryLocation?.stateOrProvince ?? "",
    postalCode: primaryLocation?.postalCode ?? "",
    country: primaryLocation?.country ?? "USA",
    itDescription: profile?.itEnvironment.description ?? "",
    usesExternalServiceProvider: profile?.itEnvironment.usesExternalServiceProvider ?? false,
    externalServiceProviderName: profile?.itEnvironment.externalServiceProviderName ?? "",
    keySystems: profile?.itEnvironment.keySystems.join(", ") ?? "",
    dataHandlingPosture: profile?.dataHandlingPosture ?? "Unknown"
  };
}

function formToRequest(form: ProfileFormState, completeProfile: boolean): UpsertCompanyProfileRequest {
  const naicsCodes = form.naicsRows
    .filter((naics) => naics.code.trim() && naics.title.trim())
    .map((naics, index) => ({
      code: naics.code.trim(),
      title: naics.title.trim(),
      isPrimary: naics.isPrimary || (index === 0 && !form.naicsRows.some((candidate) => candidate.isPrimary)),
      sizeStandard: naics.sizeStandard.trim() || null,
      qualifiesAsSmall: naics.qualifiesAsSmall === "" ? null : naics.qualifiesAsSmall === "true",
      lastCheckedAt: null
    }));
  const certifications: CompanyCertification[] = form.certificationRows
    .filter((certification) => certification.issuer.trim())
    .map((certification) => ({
      id: certification.id,
      type: certification.type,
      status: certification.status,
      issuer: certification.issuer.trim(),
      effectiveAt: certification.effectiveAt || null,
      expiresAt: certification.expiresAt || null,
      referenceNumber: certification.referenceNumber.trim() || null
    }));
  const locations = form.locationName.trim()
    ? [
        {
          name: form.locationName.trim(),
          street1: form.street1.trim(),
          street2: null,
          city: form.city.trim(),
          stateOrProvince: form.stateOrProvince.trim(),
          postalCode: form.postalCode.trim(),
          country: form.country.trim() || "USA",
          isPlaceOfPerformance: true
        }
      ]
    : [];

  return {
    legalEntityName: form.legalEntityName.trim(),
    doingBusinessAs: form.doingBusinessAs.trim() || null,
    uei: form.uei.trim() || null,
    cageCode: form.cageCode.trim() || null,
    samRegistrationExpiresAt: form.samRegistrationExpiresAt || null,
    naicsCodes,
    certifications,
    agencyCustomers: splitList(form.agencyCustomers),
    contractorRole: form.contractorRole,
    productsAndServices: form.productsAndServices.trim(),
    employeeRange: form.employeeRange,
    revenueRange: form.revenueRange,
    locations,
    itEnvironment: {
      description: form.itDescription.trim(),
      usesExternalServiceProvider: form.usesExternalServiceProvider,
      externalServiceProviderName: form.externalServiceProviderName.trim() || null,
      keySystems: splitList(form.keySystems)
    },
    dataHandlingPosture: form.dataHandlingPosture,
    completeProfile
  };
}

function splitList(value: string): string[] {
  return value
    .split(",")
    .map((item) => item.trim())
    .filter(Boolean);
}

function contractToForm(contract: ContractRecord | null): ContractFormState {
  if (!contract) {
    return defaultContractForm;
  }

  return {
    contractNumber: contract.contractNumber,
    title: contract.title,
    agencyOrPrimeName: contract.agencyOrPrimeName,
    relationship: contract.relationship,
    kind: contract.kind,
    status: contract.status,
    awardedAt: contract.awardedAt ?? "",
    periodOfPerformanceStart: contract.periodOfPerformanceStart,
    periodOfPerformanceEnd: contract.periodOfPerformanceEnd,
    placeOfPerformance: contract.placeOfPerformance,
    description: contract.description,
    dataHandlingPosture: contract.dataHandlingPosture
  };
}

function contractFormToRequest(form: ContractFormState): UpsertContractRequest {
  return {
    contractNumber: form.contractNumber.trim(),
    title: form.title.trim(),
    agencyOrPrimeName: form.agencyOrPrimeName.trim(),
    relationship: form.relationship,
    kind: form.kind,
    status: form.status,
    awardedAt: form.awardedAt || null,
    periodOfPerformanceStart: form.periodOfPerformanceStart,
    periodOfPerformanceEnd: form.periodOfPerformanceEnd,
    placeOfPerformance: form.placeOfPerformance.trim(),
    description: form.description.trim(),
    dataHandlingPosture: form.dataHandlingPosture
  };
}

function ObligationsView({
  assignmentCandidates,
  canManageObligations,
  clauseLibrary,
  contracts,
  currentRoles,
  currentUserId,
  detail,
  detailMessage,
  detailStatus,
  items,
  message,
  onDetailSelect,
  onFilter,
  onOwnerAssign,
  onStatusUpdate,
  status
}: {
  canManageObligations: boolean;
  clauseLibrary: ReactNode;
  contracts: ContractRecord[];
  currentRoles: string[];
  currentUserId: string | null;
  detail: ContractObligationDetail | null;
  detailMessage: string;
  detailStatus: "idle" | "loading" | "ready" | "saving" | "failed";
  items: ContractObligationDashboardItem[];
  assignmentCandidates: ObligationAssignmentCandidate[];
  message: string;
  onDetailSelect: (item: ContractObligationDashboardItem) => Promise<void>;
  onFilter: (params: ContractObligationQueryParams) => Promise<void>;
  onOwnerAssign: (kind: "user" | "role", value: string, notify: boolean) => Promise<void>;
  onStatusUpdate: (status: string) => Promise<void>;
  status: "idle" | "loading" | "ready" | "failed";
}) {
  const [contractId, setContractId] = useState("");
  const [riskLevel, setRiskLevel] = useState("");
  const [owner, setOwner] = useState("");
  const [taskStatus, setTaskStatus] = useState("");
  const [module, setModule] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [source, setSource] = useState("");
  const [queueScope, setQueueScope] = useState<"all" | "mine" | "role">("all");
  const [requestedDetailId, setRequestedDetailId] = useState("");
  const detailPanelRef = useRef<HTMLElement | null>(null);
  const normalizedCurrentRoles = new Set(currentRoles.map(normalizeRoleKey));
  const myItems = currentUserId ? items.filter((item) => item.assignedUserId === currentUserId) : [];
  const roleItems = items.filter(
    (item) => item.assignedRoleName && normalizedCurrentRoles.has(normalizeRoleKey(item.assignedRoleName))
  );
  const visibleItems = queueScope === "mine" ? myItems : queueScope === "role" ? roleItems : items;
  const overdueCount = visibleItems.filter((item) => item.isOverdue).length;
  const highRiskCount = visibleItems.filter((item) => item.isHighRisk).length;
  const selectedDetailId = detail?.id ?? requestedDetailId;

  useEffect(() => {
    if (detailStatus !== "loading" && !detail) {
      return;
    }

    window.requestAnimationFrame(() => {
      if (typeof detailPanelRef.current?.scrollIntoView === "function") {
        detailPanelRef.current.scrollIntoView({ behavior: isDemoCaptureMode() ? "auto" : "smooth", block: "start" });
      }
      detailPanelRef.current?.focus({ preventScroll: true });
    });
  }, [detail?.id, detailStatus, detail]);

  return (
    <section className="route-panel obligations-route">
      <div className="section-heading section-heading--split">
        <div>
          <p className="eyebrow">Obligation dashboard</p>
          <h2>Obligation work queue</h2>
        </div>
        <div className="queue-metrics" aria-label="Obligation priority counts">
          <span>
            <strong>{overdueCount}</strong> overdue
          </span>
          <span>
            <strong>{highRiskCount}</strong> high risk
          </span>
        </div>
      </div>

      <div className="queue-scope-tabs" role="group" aria-label="Obligation queue view">
        <button type="button" aria-pressed={queueScope === "all"} onClick={() => setQueueScope("all")}>
          All assignments <span>{items.length}</span>
        </button>
        <button type="button" aria-pressed={queueScope === "mine"} onClick={() => setQueueScope("mine")}>
          My assignments <span>{myItems.length}</span>
        </button>
        <button type="button" aria-pressed={queueScope === "role"} onClick={() => setQueueScope("role")}>
          Role assignments <span>{roleItems.length}</span>
        </button>
      </div>
      <p className="queue-scope-description">
        {queueScope === "mine"
          ? "Obligations assigned directly to the signed-in tenant member."
          : queueScope === "role"
            ? "Obligations assigned to one of the signed-in member's active roles."
            : "All tenant-scoped obligation assignments."}
      </p>

      <form
        className="obligation-filter-form"
        onSubmit={(event) => {
          event.preventDefault();
          void onFilter({
            contractId: contractId || undefined,
            riskLevel: riskLevel || undefined,
            owner: owner.trim() || undefined,
            status: taskStatus || undefined,
            module: module || undefined,
            dueDate: dueDate || undefined,
            source: source.trim() || undefined
          });
        }}
      >
        <label>
          Contract
          <select value={contractId} onChange={(event) => setContractId(event.target.value)} disabled={status === "loading"}>
            <option value="">All contracts</option>
            {contracts.map((contract) => (
              <option key={contract.id} value={contract.id}>
                {contract.contractNumber}
              </option>
            ))}
          </select>
        </label>
        <label>
          Risk
          <select value={riskLevel} onChange={(event) => setRiskLevel(event.target.value)} disabled={status === "loading"}>
            <option value="">All risk</option>
            <option value="Critical">Critical</option>
            <option value="High">High</option>
            <option value="Medium">Medium</option>
            <option value="Low">Low</option>
          </select>
        </label>
        <label>
          Owner
          <input
            aria-label="Owner"
            list="obligation-owner-options"
            value={owner}
            onChange={(event) => setOwner(event.target.value)}
            placeholder="All owners"
            disabled={status === "loading"}
          />
          <datalist id="obligation-owner-options">
            {ownerFunctionOptions.map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </datalist>
        </label>
        <label>
          Status
          <select value={taskStatus} onChange={(event) => setTaskStatus(event.target.value)} disabled={status === "loading"}>
            <option value="">All status</option>
            <option value="Open">Open</option>
            <option value="InProgress">In progress</option>
            <option value="Blocked">Blocked</option>
            <option value="WaitingForReview">Waiting for review</option>
            <option value="Done">Done</option>
          </select>
        </label>
        <label>
          Module
          <select value={module} onChange={(event) => setModule(event.target.value)} disabled={status === "loading"}>
            <option value="">All modules</option>
            <option value="Cybersecurity">Cybersecurity</option>
            <option value="Labor">Labor</option>
            <option value="Supply chain">Supply chain</option>
            <option value="Contract">Contract</option>
          </select>
        </label>
        <label>
          Due date
          <select value={dueDate} onChange={(event) => setDueDate(event.target.value)} disabled={status === "loading"}>
            <option value="">Any due date</option>
            <option value="overdue">Overdue</option>
            <option value="next30">Next 30 days</option>
            <option value="none">No due date</option>
          </select>
        </label>
        <label>
          Source
          <input value={source} onChange={(event) => setSource(event.target.value)} placeholder="Clause or source" disabled={status === "loading"} />
        </label>
        <button type="submit" disabled={status === "loading"}>
          <SlidersHorizontal size={16} aria-hidden="true" />
          Apply filters
        </button>
      </form>

      {status === "failed" ? (
        <Alert title="Obligations did not load" tone="danger">
          {message || "Refresh the workspace or adjust filters before relying on the obligation queue."}
        </Alert>
      ) : message ? (
        <p className="form-status form-status--ok">{message}</p>
      ) : null}

      {status === "loading" && visibleItems.length === 0 ? (
        <LoadingState label="Loading obligation work queue" />
      ) : visibleItems.length > 0 ? (
        <div className="obligation-dashboard-list" aria-label="Tenant obligation work queue">
          {visibleItems.map((item) => {
            const itemClasses = `obligation-dashboard-item${item.id === selectedDetailId ? " obligation-dashboard-item--selected" : ""}${
              item.isOverdue ? " obligation-dashboard-item--overdue" : ""
            }${item.isHighRisk ? " obligation-dashboard-item--high-risk" : ""}`;

            return (
              <div className="obligation-capture-entity" data-testid="obligation-card" key={item.id}>
                <TaskCard
                  actions={
                    <button
                      className="secondary-action obligation-detail-button"
                      type="button"
                      aria-pressed={item.id === selectedDetailId}
                      onClick={() => {
                        setRequestedDetailId(item.id);
                        void onDetailSelect(item);
                      }}
                    >
                      {detailStatus === "loading" && item.id === requestedDetailId ? "Loading details" : "View details"}
                    </button>
                  }
                  badges={
                    <>
                      <span aria-label={`${item.riskLevel} risk obligation`}>
                        <RiskBadge level={item.riskLevel} />
                      </span>
                      {item.isOverdue ? (
                        <span className="status status--overdue" aria-label="Overdue obligation">
                          <AlertTriangle size={14} aria-hidden="true" />
                          Overdue
                        </span>
                      ) : null}
                      <StatusPill label={formatEnumLabel(item.status)} tone={statusTone(item.status)} />
                      <StatusPill label={`${formatEnumLabel(item.confidence)} confidence`} tone={confidenceTone(item.confidence)} />
                    </>
                  }
                  className={itemClasses}
                  meta={[
                    { label: "Contract", value: item.contractNumber },
                    { label: "Owner", value: formatOwnerLabel(item.ownerFunction) },
                    { label: "Due", value: item.dueAt ?? "No date", tone: item.isOverdue ? "danger" : "neutral" },
                    { label: "Module", value: item.module },
                    {
                      label: "Source",
                      value: (
                        <a href={item.sourceUrl} target="_blank" rel="noreferrer">
                          {item.source}
                        </a>
                      )
                    },
                    { label: "Reviewed", value: item.lastReviewedAt },
                    { label: "Evidence", value: `${item.evidenceExamples.length} required` },
                    { label: "Review gate", value: item.requiresExpertReview ? "Expert review" : "Workflow guidance" }
                  ]}
                  summary={item.plainEnglishSummary}
                  title={item.title}
                >
                  <p className="obligation-required-action">{item.requiredAction}</p>
                  <EvidenceRequirementChips items={item.evidenceExamples} />
                  <DataQualityWarnings warnings={obligationQualityWarnings(item)} />
                </TaskCard>
              </div>
            );
          })}
        </div>
      ) : (
        <EmptyState
          title={queueScope === "all" ? "Start with company profile or contract intake" : "No assignments in this queue"}
          body={
            queueScope === "all"
              ? "Complete the company profile, add a contract, and attach mapped clauses to generate tenant-scoped obligations."
              : queueScope === "mine"
                ? "No obligations are assigned directly to the signed-in tenant member."
                : "No obligations are assigned to the signed-in member's active roles."
          }
        />
      )}

      <ObligationDetailPanel
        key={`${detail?.id ?? "none"}:${detail?.assignedUserId ?? detail?.assignedRoleName ?? "default"}`}
        canManageObligations={canManageObligations}
        detail={detail}
        panelRef={detailPanelRef}
        assignmentCandidates={assignmentCandidates}
        message={detailMessage}
        status={detailStatus}
        onOwnerAssign={onOwnerAssign}
        onStatusUpdate={onStatusUpdate}
      />

      {clauseLibrary}
    </section>
  );
}

function ObligationDetailPanel({
  assignmentCandidates,
  canManageObligations,
  detail,
  panelRef,
  message,
  onOwnerAssign,
  onStatusUpdate,
  status
}: {
  canManageObligations: boolean;
  detail: ContractObligationDetail | null;
  panelRef: RefObject<HTMLElement | null>;
  assignmentCandidates: ObligationAssignmentCandidate[];
  message: string;
  onOwnerAssign: (kind: "user" | "role", value: string, notify: boolean) => Promise<void>;
  onStatusUpdate: (status: string) => Promise<void>;
  status: "idle" | "loading" | "ready" | "saving" | "failed";
}) {
  const [ownerKind, setOwnerKind] = useState<"user" | "role">(
    detail?.assignedRoleName ? "role" : "user"
  );
  const [selectedUserId, setSelectedUserId] = useState(detail?.assignedUserId ?? "");
  const [selectedRoleName, setSelectedRoleName] = useState(
    canonicalAssignmentRoleName(detail?.assignedRoleName)
  );

  if (status === "loading") {
    return (
      <section className="obligation-detail-panel" aria-live="polite" ref={panelRef} tabIndex={-1}>
        <LoadingState label="Loading source-backed obligation detail" />
      </section>
    );
  }

  if (!detail) {
    return message ? (
      <p className={`form-status ${status === "failed" ? "form-status--error" : "form-status--ok"}`}>{message}</p>
    ) : null;
  }

  return (
    <section className="obligation-detail-panel" aria-label="Obligation detail" ref={panelRef} tabIndex={-1}>
      <div className="section-heading section-heading--split">
        <div>
          <p className="eyebrow">Obligation detail</p>
          <h2>{detail.title}</h2>
        </div>
        <div className="obligation-dashboard-item__badges">
          <RiskBadge level={detail.riskLevel} />
          <StatusPill label={formatEnumLabel(detail.status)} tone={statusTone(detail.status)} />
          <StatusPill label={`${formatEnumLabel(detail.confidence)} confidence`} tone={confidenceTone(detail.confidence)} />
        </div>
      </div>

      {status === "failed" ? (
        <Alert title="Obligation update failed" tone="danger">
          {message || "The selected obligation could not be updated."}
        </Alert>
      ) : message ? (
        <p className="form-status form-status--ok">{message}</p>
      ) : null}

      <ScanMeta
        items={[
          { label: "Contract", value: detail.contractNumber },
          { label: "Owner", value: formatOwnerLabel(detail.ownerFunction) },
          { label: "Due", value: detail.dueAt ?? "No date", tone: detail.isOverdue ? "danger" : "neutral" },
          { label: "Source", value: detail.source },
          { label: "Reviewed", value: detail.lastReviewedAt },
          { label: "Flow-down", value: detail.flowDownRequired ? "Required" : "Not required" },
          { label: "Tasks", value: detail.linkedTasks.length },
          { label: "Evidence", value: detail.linkedEvidence.length }
        ]}
      />
      <DataQualityWarnings warnings={obligationQualityWarnings(detail)} />
      <p className="current-assignment-summary" role="status">
        <strong>Currently assigned to:</strong>{" "}
        {detail.assignedUserDisplayName
          ? `${detail.assignedUserDisplayName} (tenant member)`
          : detail.assignedRoleName
            ? `${formatOwnerLabel(detail.assignedRoleName)} (role queue)`
            : `${formatOwnerLabel(detail.ownerFunction)} (default functional owner)`}
      </p>

      <div className="obligation-detail-grid">
        <div>
          <h3>Why it applies</h3>
          <p>{detail.triggerCondition}</p>
        </div>
        <div>
          <h3>Required action</h3>
          <p>{detail.requiredAction}</p>
        </div>
        <div>
          <h3>Owner</h3>
          <p>{formatOwnerLabel(detail.ownerFunction)}</p>
        </div>
        <div>
          <h3>Assignment</h3>
          <p>{detail.assignedUserDisplayName ?? detail.assignedRoleName ?? "Default functional owner"}</p>
        </div>
        <div>
          <h3>Source</h3>
          <p>
            <a href={detail.sourceUrl} target="_blank" rel="noreferrer">
              {detail.source}
            </a>
          </p>
        </div>
        <div>
          <h3>Confidence</h3>
          <p>{detail.confidence}</p>
        </div>
        <div>
          <h3>Last reviewed</h3>
          <p>{detail.lastReviewedAt}</p>
        </div>
      </div>

      <div className="obligation-detail-section">
        <h3>Plain-English summary</h3>
        <p>{detail.plainEnglishSummary}</p>
      </div>

      <div className="obligation-detail-grid">
        <div>
          <h3>Evidence examples</h3>
          <ul>
            {detail.evidenceExamples.map((example) => (
              <li key={example}>{example}</li>
            ))}
          </ul>
        </div>
        <div>
          <h3>Flow-down</h3>
          <p>{detail.flowDownRequired ? detail.flowDownRequirement : "Not required for this obligation."}</p>
        </div>
        <div>
          <h3>Expert review</h3>
          <p>{detail.requiresExpertReview ? "Expert review required" : "No expert review flag"}</p>
        </div>
      </div>

      <div className="obligation-detail-grid">
        <div>
          <h3>Linked tasks</h3>
          {detail.linkedTasks.length > 0 ? (
            <ul>
              {detail.linkedTasks.map((task) => (
                <li key={task.id}>
                  {task.title} - {task.status} {task.dueAt ? `due ${task.dueAt}` : ""}
                </li>
              ))}
            </ul>
          ) : (
            <p>No linked tasks yet.</p>
          )}
        </div>
        <div>
          <h3>Linked evidence</h3>
          {detail.linkedEvidence.length > 0 ? (
            <ul>
              {detail.linkedEvidence.map((evidence) => (
                <li key={evidence.id}>
                  {evidence.name} - {evidence.status}
                  {evidence.originalFileName ? ` (${evidence.originalFileName})` : ""}
                </li>
              ))}
            </ul>
          ) : (
            <p>No linked evidence yet.</p>
          )}
        </div>
      </div>

      <form
        key={detail.id}
        className="status-update-form"
        onSubmit={(event) => {
          event.preventDefault();
          const formData = new FormData(event.currentTarget);
          const nextStatus = String(formData.get("status") ?? detail.status);
          void onStatusUpdate(nextStatus);
        }}
      >
        <label>
          Update status
          <select
            name="status"
            defaultValue={detail.status}
            disabled={!canManageObligations || status === "saving"}
          >
            <option value="Open">Open</option>
            <option value="InProgress">In progress</option>
            <option value="Blocked">Blocked</option>
            <option value="WaitingForReview">Waiting for review</option>
            <option value="Done">Done</option>
            <option value="Canceled">Canceled</option>
          </select>
        </label>
        <button type="submit" disabled={!canManageObligations || status === "saving"}>
          <CheckCircle2 size={16} aria-hidden="true" />
          Save status
        </button>
      </form>

      <form
        className="owner-assignment-form"
        onSubmit={(event) => {
          event.preventDefault();
          const formData = new FormData(event.currentTarget);
          const value = ownerKind === "user" ? selectedUserId : selectedRoleName;
          const notify = formData.get("notify") === "on";
          if (value) {
            void onOwnerAssign(ownerKind, value, notify);
          }
        }}
      >
        <label className="owner-assignment-form__kind">
          Assign by
          <select
            data-testid="obligation-owner-kind"
            value={ownerKind}
            onChange={(event) => {
              const nextKind = event.target.value as "user" | "role";
              setOwnerKind(nextKind);
              if (nextKind === "user" && !selectedUserId) {
                setSelectedUserId(detail.assignedUserId ?? "");
              }
              if (nextKind === "role" && !selectedRoleName) {
                setSelectedRoleName(canonicalAssignmentRoleName(detail.assignedRoleName));
              }
            }}
            disabled={!canManageObligations || status === "saving"}
          >
            <option value="user">Tenant member</option>
            <option value="role">Role</option>
          </select>
        </label>
        {ownerKind === "user" ? (
          <label className="owner-assignment-form__target">
            Tenant member
            <select
              data-testid="obligation-owner-member"
              name="userId"
              value={selectedUserId}
              onChange={(event) => setSelectedUserId(event.target.value)}
              disabled={!canManageObligations || status === "saving"}
            >
              <option value="">Select member</option>
              {assignmentCandidates.map((candidate) => (
                <option key={candidate.userId} value={candidate.userId}>
                  {candidate.displayName}
                </option>
              ))}
            </select>
          </label>
        ) : (
          <label className="owner-assignment-form__target">
            Role
            <select
              name="roleName"
              value={selectedRoleName}
              onChange={(event) => setSelectedRoleName(event.target.value)}
              disabled={!canManageObligations || status === "saving"}
            >
              <option value="">Select role</option>
              <option value="Compliance Manager">Compliance manager</option>
              <option value="Advisor">Advisor</option>
              <option value="Contributor">Contributor</option>
              <option value="Auditor">Auditor</option>
            </select>
          </label>
        )}
        <label className="inline-checkbox owner-assignment-form__notify">
          <input
            name="notify"
            type="checkbox"
            defaultChecked
            disabled={ownerKind === "role" || !canManageObligations || status === "saving"}
          />
          Also send assignment email
        </label>
        <small className="owner-assignment-form__help">
          {ownerKind === "user"
            ? "The member always receives an in-app notification. Email follows their assignment-email preference."
            : "Active members of this role receive an in-app notification. Role-assignment email is not sent."}
        </small>
        <button
          className="owner-assignment-form__submit"
          data-testid="obligation-owner-submit"
          type="submit"
          disabled={!canManageObligations || status === "saving"}
        >
          <UserPlus size={16} aria-hidden="true" />
          Assign owner
        </button>
      </form>
    </section>
  );
}

function ClauseLibraryView({
  results,
  searchMessage,
  searchStatus,
  onSearch
}: {
  results: ClauseLibraryItem[];
  searchMessage: string;
  searchStatus: "idle" | "loading" | "ready" | "failed";
  onSearch: (params: ClauseSearchParams) => Promise<void>;
}) {
  const [query, setQuery] = useState("");
  const [category, setCategory] = useState("");
  const [selectedClauseId, setSelectedClauseId] = useState<string | null>(null);
  const selectedClause = results.find((clause) => clause.id === selectedClauseId) ?? null;

  return (
    <section className="route-panel clause-library-route">
      <div className="section-heading section-heading--split">
        <div>
          <p className="eyebrow">Manual clause tagging</p>
          <h2>Clause library search</h2>
        </div>
        {selectedClause ? (
          <div className="selection-pill" aria-live="polite">
            Selected {selectedClause.number}
          </div>
        ) : null}
      </div>

      <form
        className="clause-search-form"
        onSubmit={(event) => {
          event.preventDefault();
          void onSearch({
            query: query.trim() || undefined,
            category: category || undefined
          });
        }}
      >
        <label>
          Clause search
          <input
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Clause number or title"
            disabled={searchStatus === "loading"}
          />
        </label>
        <label>
          Category
          <select value={category} onChange={(event) => setCategory(event.target.value)} disabled={searchStatus === "loading"}>
            <option value="">All published categories</option>
            <option value="FAR">FAR</option>
            <option value="DFARS">DFARS</option>
            <option value="CMMC">CMMC</option>
            <option value="Labor">Labor</option>
            <option value="Telecom">Telecom</option>
            <option value="ByteDance">ByteDance</option>
            <option value="Custom">Custom</option>
          </select>
        </label>
        <button type="submit" disabled={searchStatus === "loading"}>
          Search clauses
        </button>
      </form>

      {searchMessage ? (
        <p className={`form-status ${searchStatus === "failed" ? "form-status--error" : "form-status--ok"}`}>
          {searchMessage}
        </p>
      ) : null}

      <div className="clause-result-list" aria-label="Published clause search results">
        {results.length > 0 ? (
          results.map((clause) => (
            <article className="clause-result-item" key={clause.id}>
              <div>
                <div className="clause-result-item__header">
                  <strong>{clause.number}</strong>
                  <span>{clause.category}</span>
                </div>
                <h3>{clause.title}</h3>
                <p>{clause.plainEnglishSummary}</p>
                <dl>
                  {!isDemoCaptureMode() ? (
                    <div>
                      <dt>Published clause ID</dt>
                      <dd>{clause.id}</dd>
                    </div>
                  ) : null}
                  <div>
                    <dt>Source URL</dt>
                    <dd>
                      <a href={clause.sourceUrl} target="_blank" rel="noreferrer">
                        {clause.sourceUrl}
                      </a>
                    </dd>
                  </div>
                  <div>
                    <dt>Confidence</dt>
                    <dd>{formatEnumLabel(clause.confidence)}</dd>
                  </div>
                  <div>
                    <dt>Review state</dt>
                    <dd>{formatEnumLabel(clause.reviewState)}</dd>
                  </div>
                  <div>
                    <dt>Reviewed</dt>
                    <dd>{clause.lastReviewedAt}</dd>
                  </div>
                  <div>
                    <dt>Mapping</dt>
                    <dd>{clause.isMappable ? "Published and mappable" : "Unavailable"}</dd>
                  </div>
                </dl>
              </div>
              <button
                type="button"
                aria-pressed={selectedClauseId === clause.id}
                onClick={() => setSelectedClauseId(clause.id)}
                disabled={clause.isMappable === false}
              >
                {selectedClauseId === clause.id ? "Selected" : "Select clause"}
              </button>
            </article>
          ))
        ) : (
          <EmptyState
            title="No clause results yet"
            body="Search by clause number, title, or category to find published clauses available for mapping."
          />
        )}
      </div>
    </section>
  );
}

function mergeClauseSearchResults(
  currentResults: ClauseLibraryItem[],
  incomingResults: ClauseLibraryItem[]
): ClauseLibraryItem[] {
  const resultsById = new Map(currentResults.map((clause) => [clause.id, clause]));

  incomingResults.forEach((clause) => {
    resultsById.set(clause.id, clause);
  });

  return Array.from(resultsById.values());
}

function ContractsView({
  canManageContracts,
  canReviewClauses,
  clauseResults,
  contracts,
  contractClauses,
  contractClauseMessage,
  contractClauseStatus,
  contractDeliverables,
  contractDocuments,
  extractionJobsByDocumentId,
  extractionResultsByDocumentId,
  clauseCandidateReviewStatusFilter,
  deliverableMessage,
  deliverableStatus,
  contractDocumentMessage,
  contractDocumentStatus,
  contractMessage,
  contractStatus,
  noCuiAcknowledgement,
  acknowledgementMessage,
  acknowledgementStatus,
  selectedContractId,
  tenantDataHandlingMode,
  onAcknowledge,
  onDeleteDocument,
  onChangeClauseCandidateReviewStatusFilter,
  onStartExtraction,
  onReviewCandidate,
  onAttachClause,
  onGenerateClauseObligations,
  onRemoveClause,
  onSaveDeliverable,
  onUploadDocument,
  onSave,
  onSelectContract
}: {
  canManageContracts: boolean;
  canReviewClauses: boolean;
  clauseResults: ClauseLibraryItem[];
  contracts: ContractRecord[];
  contractClauses: ContractClause[];
  contractClauseMessage: string;
  contractClauseStatus: "idle" | "saving" | "saved" | "failed";
  contractDeliverables: ContractDeliverable[];
  contractDocuments: ContractDocument[];
  extractionJobsByDocumentId: Record<string, ExtractionJob>;
  extractionResultsByDocumentId: Record<string, ContractDocumentExtractionResults>;
  clauseCandidateReviewStatusFilter: string;
  deliverableMessage: string;
  deliverableStatus: "idle" | "saving" | "saved" | "failed";
  contractDocumentMessage: string;
  contractDocumentStatus: "idle" | "saving" | "saved" | "failed";
  contractMessage: string;
  contractStatus: "idle" | "saving" | "saved" | "failed";
  noCuiAcknowledgement: NoCuiAcknowledgementStatus;
  acknowledgementMessage: string;
  acknowledgementStatus: "idle" | "saving" | "saved" | "failed";
  selectedContractId: string | null;
  tenantDataHandlingMode: string;
  onAcknowledge: () => void;
  onDeleteDocument: (contractId: string, documentId: string) => Promise<void>;
  onChangeClauseCandidateReviewStatusFilter: (reviewStatus: string) => void;
  onStartExtraction: (contractId: string, documentId: string) => Promise<void>;
  onReviewCandidate: (
    contractId: string,
    documentId: string,
    candidateId: string,
    action: "accept" | "reject" | "needs_clarification" | "supersede",
    clauseLibraryId: string | null
  ) => Promise<void>;
  onAttachClause: (contractId: string, request: AttachContractClauseRequest) => Promise<void>;
  onGenerateClauseObligations: (contractId: string, contractClauseId: string) => Promise<void>;
  onRemoveClause: (contractId: string, contractClauseId: string, reason: string) => Promise<void>;
  onSaveDeliverable: (
    contractId: string,
    deliverableId: string | null,
    request: UpsertContractDeliverableRequest
  ) => Promise<void>;
  onUploadDocument: (
    contractId: string,
    documentType: string,
    file: File | null,
    classification?: string,
    noCuiAttestation?: boolean
  ) => Promise<boolean>;
  onSave: (contractId: string | null, request: UpsertContractRequest) => Promise<void>;
  onSelectContract: (contractId: string | null) => void;
}) {
  const selectedContract = contracts.find((contract) => contract.id === selectedContractId) ?? null;
  const [selectedDocumentFile, setSelectedDocumentFile] = useState<File | null>(null);
  const [documentType, setDocumentType] = useState("Contract");
  const [documentClassification, setDocumentClassification] = useState("Unclassified");
  const [documentNoCuiAttestation, setDocumentNoCuiAttestation] = useState(false);
  const [documentInputKey, setDocumentInputKey] = useState(0);
  const [clauseDraft, setClauseDraft] = useState<AttachContractClauseRequest>({
    clauseLibraryId: "",
    attachmentReason: "",
    sourceDocumentReference: ""
  });
  const [removalReasons, setRemovalReasons] = useState<Record<string, string>>({});
  const [deliverableDraft, setDeliverableDraft] = useState<UpsertContractDeliverableRequest>({
    name: "",
    description: "",
    dueAt: "",
    ownerFunction: "Contracts",
    status: "NotStarted"
  });
  const uploadDisabled =
    !canManageContracts || !selectedContract || !noCuiAcknowledgement.isAcknowledged || contractDocumentStatus === "saving";
  const clauseDisabled = !canManageContracts || !selectedContract || contractClauseStatus === "saving";
  const deliverableDisabled = !canManageContracts || !selectedContract || deliverableStatus === "saving";
  const overdueDeliverableCount = contractDeliverables.filter((deliverable) => deliverable.isOverdue).length;

  async function attachClause() {
    if (!selectedContract) {
      return;
    }

    await onAttachClause(selectedContract.id, {
      clauseLibraryId: clauseDraft.clauseLibraryId.trim(),
      attachmentReason: clauseDraft.attachmentReason.trim(),
      sourceDocumentReference: clauseDraft.sourceDocumentReference?.trim() || null
    });
    setClauseDraft({
      clauseLibraryId: "",
      attachmentReason: "",
      sourceDocumentReference: ""
    });
  }

  async function removeClause(contractClauseId: string) {
    if (!selectedContract) {
      return;
    }

    await onRemoveClause(selectedContract.id, contractClauseId, removalReasons[contractClauseId] ?? "");
    setRemovalReasons((currentReasons) => {
      const nextReasons = { ...currentReasons };
      delete nextReasons[contractClauseId];
      return nextReasons;
    });
  }

  async function saveNewDeliverable() {
    if (!selectedContract) {
      return;
    }

    await onSaveDeliverable(selectedContract.id, null, {
      ...deliverableDraft,
      name: deliverableDraft.name.trim(),
      description: deliverableDraft.description.trim(),
      ownerFunction: deliverableDraft.ownerFunction.trim(),
      dueAt: deliverableDraft.dueAt || null
    });
    setDeliverableDraft({
      name: "",
      description: "",
      dueAt: "",
      ownerFunction: "Contracts",
      status: "NotStarted"
    });
  }

  async function updateDeliverableStatus(deliverable: ContractDeliverable, status: string) {
    if (!selectedContract) {
      return;
    }

    await onSaveDeliverable(selectedContract.id, deliverable.id, {
      name: deliverable.name,
      description: deliverable.description,
      dueAt: deliverable.dueAt,
      ownerFunction: deliverable.ownerFunction,
      status
    });
  }

  return (
    <section className="route-panel contracts-route">
      <div className="section-heading section-heading--split">
        <div>
          <p className="eyebrow">Contract intake</p>
          <h2>{selectedContract ? selectedContract.contractNumber : "Create contract record"}</h2>
        </div>
        <button
          className="secondary-action"
          type="button"
          onClick={() => onSelectContract(null)}
          disabled={!canManageContracts || contractStatus === "saving"}
        >
          New contract
        </button>
      </div>

      {contractStatus === "failed" ? (
        <Alert title="Contract save failed" tone="danger">
          {contractMessage || "The contract record was not saved."}
        </Alert>
      ) : contractMessage ? (
        <p className="form-status form-status--ok">{contractMessage}</p>
      ) : null}

      <div className="contract-workspace">
        <aside className="contract-list" aria-label="Contract records">
          {contracts.length > 0 ? (
            contracts.map((contract) => (
              <button
                className={contract.id === selectedContract?.id ? "contract-list__item contract-list__item--active" : "contract-list__item"}
                key={contract.id}
                type="button"
                onClick={() => onSelectContract(contract.id)}
              >
                <strong>{contract.contractNumber}</strong>
                <span>{contract.title}</span>
                <small>{contract.relationship} · {contract.dataHandlingPosture}</small>
                <div className="scan-pill-row">
                  <StatusPill label={formatEnumLabel(contract.status)} tone={statusTone(contract.status)} />
                </div>
              </button>
            ))
          ) : (
            <EmptyState title="No contracts have been added yet" body="Create a draft or active contract record to start intake." />
          )}
        </aside>

        <ContractEditor
          key={selectedContract?.id ?? "new-contract"}
          canManageContracts={canManageContracts}
          contractStatus={contractStatus}
          selectedContract={selectedContract}
          tenantDataHandlingMode={tenantDataHandlingMode}
          onSave={onSave}
        />

        <NoCuiAcknowledgementPanel
          acknowledgement={noCuiAcknowledgement}
          acknowledgementMessage={acknowledgementMessage}
          acknowledgementStatus={acknowledgementStatus}
          canAcknowledge={canManageContracts}
          context="contract or evidence work"
          onAcknowledge={onAcknowledge}
        />

        {selectedContract ? (
          <section className="contract-detail" aria-label="Contract detail">
            <ScanMeta
              items={[
                { label: "Status", value: <StatusPill label={formatEnumLabel(selectedContract.status)} tone={statusTone(selectedContract.status)} /> },
                {
                  label: "Period",
                  value: `${selectedContract.periodOfPerformanceStart} to ${selectedContract.periodOfPerformanceEnd}`
                },
                { label: "Role", value: selectedContract.relationship },
                { label: "Agency or prime", value: selectedContract.agencyOrPrimeName },
                { label: "Data posture", value: selectedContract.dataHandlingPosture },
                { label: "Clauses", value: contractClauses.length },
                { label: "Deliverables", value: contractDeliverables.length },
                { label: "Overdue", value: overdueDeliverableCount, tone: overdueDeliverableCount > 0 ? "danger" : "success" },
                { label: "Documents", value: contractDocuments.length }
              ]}
            />
          </section>
        ) : null}

        <section className="contract-clauses" aria-label="Attached contract clauses">
          <div className="contract-documents__header">
            <div>
              <span>Attached clauses</span>
              <strong>{contractClauses.length}</strong>
            </div>
          </div>
          <form
            className="contract-clause-form"
            onSubmit={(event) => {
              event.preventDefault();
              void attachClause();
            }}
          >
            <label>
              Published clause ID
              <input
                list="published-clause-options"
                value={clauseDraft.clauseLibraryId}
                onChange={(event) => setClauseDraft((current) => ({ ...current, clauseLibraryId: event.target.value }))}
                disabled={clauseDisabled}
                placeholder="Example: far-52-204-21"
                required
              />
              <datalist id="published-clause-options">
                {clauseResults.map((clause) => (
                  <option key={clause.id} value={clause.id}>
                    {clause.number} - {clause.title}
                  </option>
                ))}
              </datalist>
            </label>
            <label>
              Attachment reason
              <input
                value={clauseDraft.attachmentReason}
                onChange={(event) => setClauseDraft((current) => ({ ...current, attachmentReason: event.target.value }))}
                disabled={clauseDisabled}
                required
              />
            </label>
            <label>
              Source document reference
              <input
                value={clauseDraft.sourceDocumentReference ?? ""}
                onChange={(event) => setClauseDraft((current) => ({ ...current, sourceDocumentReference: event.target.value }))}
                disabled={clauseDisabled}
              />
            </label>
            <button type="submit" disabled={clauseDisabled}>
              Attach clause
            </button>
          </form>
          {contractClauseStatus === "failed" ? (
            <Alert title="Clause action failed" tone="danger">
              {contractClauseMessage || "The clause register was not updated."}
            </Alert>
          ) : contractClauseMessage ? (
            <p className="form-status form-status--ok">{contractClauseMessage}</p>
          ) : null}
          <div className="contract-clause-list">
            {contractClauses.length > 0 ? (
              contractClauses.map((clause) => (
                <article className="contract-clause-item" key={clause.id}>
                  <div>
                    <strong>{clause.clauseNumber}</strong>
                    <span>{clause.title}</span>
                    <ScanMeta
                      items={[
                        { label: "Source", value: clause.source },
                        { label: "Review state", value: formatEnumLabel(clause.reviewState) },
                        { label: "Confidence", value: formatEnumLabel(clause.confidence) },
                        { label: "Last reviewed", value: clause.lastReviewedAt },
                        ...(!isDemoCaptureMode() && clause.reviewedByUserId
                          ? [{ label: "Reviewed by", value: clause.reviewedByUserId }]
                          : []),
                        ...(clause.nextReviewDueAt
                          ? [{ label: "Next review due", value: clause.nextReviewDueAt }]
                          : []),
                        {
                          label: "Review gate",
                          value: clause.requiresExpertReview ? "Expert review required" : "Published review complete"
                        },
                        { label: "Attached", value: clause.attachedAt },
                        { label: "Reason", value: clause.attachmentReason },
                        ...(clause.sourceDocumentReference
                          ? [{ label: "Source reference", value: clause.sourceDocumentReference }]
                          : [])
                      ]}
                    />
                    <a href={clause.sourceUrl} target="_blank" rel="noreferrer">
                      {clause.sourceUrl}
                    </a>
                  </div>
                  <div className="contract-clause-actions">
                    <button
                      type="button"
                      aria-label={`Generate obligations for ${clause.clauseNumber}`}
                      onClick={() => selectedContract && void onGenerateClauseObligations(selectedContract.id, clause.id)}
                      disabled={clauseDisabled}
                    >
                      Generate obligations
                    </button>
                    <form
                      className="contract-clause-remove"
                      onSubmit={(event) => {
                        event.preventDefault();
                        void removeClause(clause.id);
                      }}
                    >
                      <input
                        aria-label={`Removal reason for ${clause.clauseNumber}`}
                        value={removalReasons[clause.id] ?? ""}
                        onChange={(event) =>
                          setRemovalReasons((currentReasons) => ({
                            ...currentReasons,
                            [clause.id]: event.target.value
                          }))
                        }
                        placeholder="Removal reason"
                        disabled={clauseDisabled}
                      />
                      <button type="submit" disabled={clauseDisabled}>
                        Remove
                      </button>
                    </form>
                  </div>
                </article>
              ))
            ) : (
              <EmptyState title="No clauses attached yet" body="Attach published clauses from the library to begin contract-specific tracking." />
            )}
          </div>
        </section>

        <section className="contract-deliverables" aria-label="Contract deliverables">
          <div className="contract-documents__header">
            <div>
              <span>Deliverables</span>
              <strong>{contractDeliverables.length}</strong>
            </div>
          </div>
          <form
            className="deliverable-form"
            onSubmit={(event) => {
              event.preventDefault();
              void saveNewDeliverable();
            }}
          >
            <label>
              Name
              <input
                value={deliverableDraft.name}
                onChange={(event) => setDeliverableDraft((current) => ({ ...current, name: event.target.value }))}
                disabled={deliverableDisabled}
                required
              />
            </label>
            <label>
              Owner
              <select
                aria-label="Owner"
                value={deliverableDraft.ownerFunction}
                onChange={(event) => setDeliverableDraft((current) => ({ ...current, ownerFunction: event.target.value }))}
                disabled={deliverableDisabled}
                required
              >
                {ownerOptionsWith(deliverableDraft.ownerFunction).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Due date
              <input
                type="date"
                value={deliverableDraft.dueAt ?? ""}
                onChange={(event) => setDeliverableDraft((current) => ({ ...current, dueAt: event.target.value }))}
                disabled={deliverableDisabled}
              />
            </label>
            <label>
              Deliverable status
              <select
                value={deliverableDraft.status}
                onChange={(event) => setDeliverableDraft((current) => ({ ...current, status: event.target.value }))}
                disabled={deliverableDisabled}
              >
                <option value="NotStarted">Not started</option>
                <option value="InProgress">In progress</option>
                <option value="Submitted">Submitted</option>
                <option value="Accepted">Accepted</option>
                <option value="Late">Late</option>
                <option value="Waived">Waived</option>
              </select>
            </label>
            <label className="deliverable-form__description">
              Deliverable description
              <textarea
                value={deliverableDraft.description}
                onChange={(event) => setDeliverableDraft((current) => ({ ...current, description: event.target.value }))}
                disabled={deliverableDisabled}
              />
            </label>
            <button type="submit" disabled={deliverableDisabled}>
              Add deliverable
            </button>
          </form>
          {deliverableStatus === "failed" ? (
            <Alert title="Deliverable action failed" tone="danger">
              {deliverableMessage || "The deliverable register was not updated."}
            </Alert>
          ) : deliverableMessage ? (
            <p className="form-status form-status--ok">{deliverableMessage}</p>
          ) : null}
          <div className="deliverable-list">
            {contractDeliverables.length > 0 ? (
              contractDeliverables.map((deliverable) => (
                <article
                  className={deliverable.isOverdue ? "deliverable-item deliverable-item--overdue" : "deliverable-item"}
                  key={deliverable.id}
                >
                  <div>
                    <strong>{deliverable.name}</strong>
                    <div className="scan-pill-row">
                      <StatusPill label={formatEnumLabel(deliverable.status)} tone={statusTone(deliverable.status)} />
                      {deliverable.isOverdue ? <StatusPill label="Overdue" tone="danger" /> : null}
                    </div>
                    <ScanMeta
                      items={[
                        { label: "Owner", value: formatOwnerLabel(deliverable.ownerFunction) },
                        { label: "Due", value: deliverable.dueAt ?? "No due date", tone: deliverable.isOverdue ? "danger" : "neutral" }
                      ]}
                    />
                    <span className="legacy-summary">
                      {formatOwnerLabel(deliverable.ownerFunction)} · {deliverable.dueAt ?? "No due date"}
                      {deliverable.isOverdue ? " · Overdue" : ""}
                    </span>
                    {deliverable.description ? <p>{deliverable.description}</p> : null}
                  </div>
                  <select
                    aria-label={`Status for ${deliverable.name}`}
                    value={deliverable.status}
                    onChange={(event) => void updateDeliverableStatus(deliverable, event.target.value)}
                    disabled={deliverableDisabled}
                  >
                    <option value="NotStarted">Not started</option>
                    <option value="InProgress">In progress</option>
                    <option value="Submitted">Submitted</option>
                    <option value="Accepted">Accepted</option>
                    <option value="Late">Late</option>
                    <option value="Waived">Waived</option>
                  </select>
                </article>
              ))
            ) : (
              <EmptyState title="No deliverables yet" body="Add due dates and owners so contract obligations appear on the calendar." />
            )}
          </div>
        </section>

        <section className="contract-documents" aria-label="Contract documents">
          <div className="contract-documents__header">
            <div>
              <span>Documents</span>
              <strong>{contractDocuments.length}</strong>
            </div>
            <label className="contract-documents__field">
              <span>Document type</span>
              <select value={documentType} onChange={(event) => setDocumentType(event.target.value)} disabled={uploadDisabled}>
                <option value="Solicitation">Solicitation</option>
                <option value="Contract">Contract</option>
                <option value="Subcontract">Subcontract</option>
                <option value="PurchaseOrder">Purchase order</option>
                <option value="StatementOfWork">SOW</option>
                <option value="FlowDownAttachment">Flow-down</option>
                <option value="WageDetermination">Wage determination</option>
                <option value="Dd254">DD 254</option>
                <option value="CuiMarkingGuide">CUI marking guide</option>
                <option value="Other">Other</option>
              </select>
            </label>
            <label className="contract-documents__field">
              <span>Contract document classification</span>
              <select
                value={documentClassification}
                onChange={(event) => setDocumentClassification(event.target.value)}
                disabled={uploadDisabled}
              >
                <option value="Unclassified">Unclassified</option>
                <option value="Fci">FCI</option>
                <option value="Cui">CUI</option>
                <option value="SyntheticCui">Synthetic CUI</option>
                <option value="Unknown">Unknown</option>
                <option value="Prohibited">Prohibited</option>
              </select>
            </label>
            <label className="contract-documents__field">
              <span>Clause candidate review status</span>
              <select
                value={clauseCandidateReviewStatusFilter}
                onChange={(event) => onChangeClauseCandidateReviewStatusFilter(event.target.value)}
              >
                <option value="all">All review states</option>
                <option value="pending_review">Pending review</option>
                <option value="needs_clarification">Needs clarification</option>
                <option value="accepted">Accepted</option>
                <option value="rejected">Rejected</option>
                <option value="superseded">Superseded</option>
              </select>
            </label>
          </div>
          <form
            className="contract-document-upload"
            onSubmit={(event) => {
              event.preventDefault();
              if (selectedContract) {
                void onUploadDocument(
                    selectedContract.id,
                    documentType,
                    selectedDocumentFile,
                    documentClassification,
                    documentNoCuiAttestation
                  )
                  .then((uploaded) => {
                    if (uploaded) {
                      setSelectedDocumentFile(null);
                      setDocumentNoCuiAttestation(false);
                      setDocumentInputKey((currentKey) => currentKey + 1);
                    }
                  });
              }
            }}
          >
            <input
              key={documentInputKey}
              aria-label="Contract document"
              type="file"
              onChange={(event) => setSelectedDocumentFile(event.target.files?.[0] ?? null)}
              disabled={uploadDisabled}
            />
            <label>
              <input
                type="checkbox"
                checked={documentNoCuiAttestation}
                onChange={(event) => setDocumentNoCuiAttestation(event.target.checked)}
                disabled={uploadDisabled}
              />
              I confirm this file does not contain CUI, classified information, export-controlled data, ITAR data, or
              sensitive government-furnished information.
            </label>
            <button type="submit" disabled={uploadDisabled || !documentNoCuiAttestation}>
              Upload document
            </button>
          </form>
          {!noCuiAcknowledgement.isAcknowledged ? (
            <p className="form-status form-status--error">No-CUI acknowledgement is required before contract document upload.</p>
          ) : null}
          {contractDocumentStatus === "failed" ? (
            <Alert title="Document action failed" tone="danger">
              {contractDocumentMessage || "The document workflow was not updated."}
            </Alert>
          ) : contractDocumentMessage ? (
            <p className="form-status form-status--ok">{contractDocumentMessage}</p>
          ) : null}
          <div className="contract-document-list">
            {contractDocuments.length > 0 ? (
              contractDocuments.map((document) => (
                <article className="contract-document-item" key={document.id}>
                  <div>
                    <strong>{document.fileName}</strong>
                    <span>{document.type} · {document.validationStatus} · {document.malwareScanStatus}</span>
                    <ClassificationBadge classification={document.classification.classification} />
                    {extractionJobsByDocumentId[document.id] ? (
                      <small>Extraction {extractionJobsByDocumentId[document.id].status}</small>
                    ) : null}
                    {extractionResultsByDocumentId[document.id] ? (
                      <small>
                        Results {extractionResultsByDocumentId[document.id].latestJobStatus ?? "none"} ·{" "}
                        {extractionResultsByDocumentId[document.id].candidateCount} candidates
                        {extractionResultsByDocumentId[document.id].failureReason
                          ? ` · ${extractionResultsByDocumentId[document.id].failureReason}`
                          : ""}
                      </small>
                    ) : null}
                  </div>
                  {(() => {
                    const latestStatus =
                      extractionResultsByDocumentId[document.id]?.latestJobStatus ??
                      extractionJobsByDocumentId[document.id]?.status ??
                      null;
                    const isRunning = latestStatus === "Queued" || latestStatus === "Processing";
                    const isStoredText =
                      document.contentType.toLowerCase() === "text/plain" &&
                      document.malwareScanStatus.toLowerCase() === "clean" &&
                      Boolean(document.storageUri) &&
                      !document.storageUri?.startsWith("pending://");

                    return (
                      <>
                        <button
                          type="button"
                          onClick={() => selectedContract && void onStartExtraction(selectedContract.id, document.id)}
                          disabled={!canManageContracts || contractDocumentStatus === "saving" || isRunning || !isStoredText}
                          title={
                            isStoredText
                              ? "Queue tenant-scoped clause extraction."
                              : "Extraction requires a stored, malware-scanned plain-text document."
                          }
                        >
                          {isRunning ? "Extraction in progress" : isStoredText ? "Start extraction" : "Extraction unavailable"}
                        </button>
                        {!isStoredText ? (
                          <small>Extraction requires a stored, malware-scanned .txt document.</small>
                        ) : null}
                      </>
                    );
                  })()}
                  <button
                    type="button"
                    onClick={() => selectedContract && void onDeleteDocument(selectedContract.id, document.id)}
                    disabled={!canManageContracts || contractDocumentStatus === "saving"}
                  >
                    Delete
                  </button>
                  {extractionResultsByDocumentId[document.id]?.candidates.length ? (
                    <div className="contract-extraction-results">
                      {extractionResultsByDocumentId[document.id].candidates.map((candidate) => (
                        <div className="contract-extraction-result" key={candidate.id}>
                          <div>
                            <strong>{candidate.normalizedCitation}</strong>
                            <span>
                              {(candidate.confidence * 100).toFixed(0)}% · {candidate.matchMethod} · {candidate.reviewStatus} ·{" "}
                              {candidate.locationMetadata}
                            </span>
                            <small>{candidate.rawExtractedText}</small>
                          </div>
                          <button
                            type="button"
                            onClick={() =>
                              selectedContract &&
                              void onReviewCandidate(
                                selectedContract.id,
                                document.id,
                                candidate.id,
                                "accept",
                                candidate.clauseLibraryId
                              )
                            }
                            disabled={!canReviewClauses || !candidate.clauseLibraryId || contractDocumentStatus === "saving"}
                          >
                            Accept
                          </button>
                          <button
                            type="button"
                            onClick={() =>
                              selectedContract &&
                              void onReviewCandidate(selectedContract.id, document.id, candidate.id, "reject", null)
                            }
                            disabled={!canReviewClauses || contractDocumentStatus === "saving"}
                          >
                            Reject
                          </button>
                          <button
                            type="button"
                            onClick={() =>
                              selectedContract &&
                              void onReviewCandidate(
                                selectedContract.id,
                                document.id,
                                candidate.id,
                                "needs_clarification",
                                null
                              )
                            }
                            disabled={!canReviewClauses || contractDocumentStatus === "saving"}
                          >
                            Clarify
                          </button>
                          <button
                            type="button"
                            onClick={() =>
                              selectedContract &&
                              void onReviewCandidate(selectedContract.id, document.id, candidate.id, "supersede", null)
                            }
                            disabled={!canReviewClauses || contractDocumentStatus === "saving"}
                          >
                            Supersede
                          </button>
                        </div>
                      ))}
                    </div>
                  ) : null}
                </article>
              ))
            ) : (
              <EmptyState title="No contract documents yet" body="Upload non-CUI document metadata after acknowledgement." />
            )}
          </div>
        </section>
      </div>
    </section>
  );
}

function ContractEditor({
  canManageContracts,
  contractStatus,
  selectedContract,
  tenantDataHandlingMode,
  onSave
}: {
  canManageContracts: boolean;
  contractStatus: "idle" | "saving" | "saved" | "failed";
  selectedContract: ContractRecord | null;
  tenantDataHandlingMode: string;
  onSave: (contractId: string | null, request: UpsertContractRequest) => Promise<void>;
}) {
  const [form, setForm] = useState<ContractFormState>(() => contractToForm(selectedContract));
  const isCuiReady = tenantDataHandlingMode === "CuiReady";
  const realCuiModeMessage = isCuiReady
    ? ""
    : `${tenantDataHandlingMode} mode blocks real CUI, classified, and export-controlled contract records.`;
  const periodEndError =
    form.periodOfPerformanceStart &&
    form.periodOfPerformanceEnd &&
    form.periodOfPerformanceEnd < form.periodOfPerformanceStart
      ? "Period of performance end must be on or after the start date."
      : "";

  function updateField<TKey extends keyof ContractFormState>(field: TKey, value: ContractFormState[TKey]) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  async function save() {
    await onSave(selectedContract?.id ?? null, contractFormToRequest(form));
  }

  return (
    <form
      className="contract-form"
      onSubmit={(event) => {
        event.preventDefault();
        void save();
      }}
    >
      <fieldset disabled={!canManageContracts || contractStatus === "saving"}>
        <div className="form-grid">
          <label>
            <span>Contract number</span>
            <input value={form.contractNumber} onChange={(event) => updateField("contractNumber", event.target.value)} required />
          </label>
          <label>
            <span>Title</span>
            <input value={form.title} onChange={(event) => updateField("title", event.target.value)} required />
          </label>
          <label>
            <span>Agency or prime</span>
            <input value={form.agencyOrPrimeName} onChange={(event) => updateField("agencyOrPrimeName", event.target.value)} required />
          </label>
          <label>
            <span>Role</span>
            <select value={form.relationship} onChange={(event) => updateField("relationship", event.target.value)}>
              <option value="Prime">Prime</option>
              <option value="Subcontractor">Subcontractor</option>
              <option value="Supplier">Supplier</option>
              <option value="Consultant">Consultant</option>
            </select>
          </label>
          <label>
            <span>Contract type</span>
            <select value={form.kind} onChange={(event) => updateField("kind", event.target.value)}>
              <option value="Unknown">Unknown</option>
              <option value="FixedPrice">Fixed price</option>
              <option value="TimeAndMaterials">Time and materials</option>
              <option value="CostReimbursement">Cost reimbursement</option>
              <option value="IndefiniteDelivery">Indefinite delivery</option>
              <option value="PurchaseOrder">Purchase order</option>
              <option value="Other">Other</option>
            </select>
          </label>
          <label>
            <span>Status</span>
            <select value={form.status} onChange={(event) => updateField("status", event.target.value)}>
              <option value="Draft">Draft</option>
              <option value="Intake">Intake</option>
              <option value="Active">Active</option>
              <option value="OptionPending">Option pending</option>
              <option value="Closed">Closed</option>
              <option value="Archived">Archived</option>
            </select>
          </label>
          <label>
            <span>Awarded</span>
            <input type="date" value={form.awardedAt} onChange={(event) => updateField("awardedAt", event.target.value)} />
          </label>
          <label>
            <span>Start</span>
            <input
              type="date"
              value={form.periodOfPerformanceStart}
              onChange={(event) => updateField("periodOfPerformanceStart", event.target.value)}
              required
            />
          </label>
          <label>
            <span>End</span>
            <input
              type="date"
              value={form.periodOfPerformanceEnd}
              onChange={(event) => {
                event.currentTarget.setCustomValidity(
                  form.periodOfPerformanceStart && event.target.value < form.periodOfPerformanceStart
                    ? "Period of performance end must be on or after the start date."
                    : ""
                );
                updateField("periodOfPerformanceEnd", event.target.value);
              }}
              onBlur={(event) => {
                event.currentTarget.setCustomValidity(periodEndError);
                if (periodEndError) {
                  event.currentTarget.reportValidity();
                }
              }}
              required
            />
            {periodEndError ? <span className="field-error">Period of performance end must be on or after the start date.</span> : null}
          </label>
          <label>
            <span>FCI/CUI posture</span>
            <select value={form.dataHandlingPosture} onChange={(event) => updateField("dataHandlingPosture", event.target.value)}>
              <option value="NoFciOrCui">No FCI/CUI</option>
              <option value="FciOnly">FCI only</option>
              <option value="Cui" disabled={!isCuiReady}>CUI</option>
              <option value="Classified" disabled={!isCuiReady}>Classified</option>
              <option value="ExportControlled" disabled={!isCuiReady}>Export-controlled</option>
            </select>
          </label>
          {realCuiModeMessage ? <p className="form-status form-status--error span-2">{realCuiModeMessage}</p> : null}
          <label className="span-2">
            <span>Place of performance</span>
            <input value={form.placeOfPerformance} onChange={(event) => updateField("placeOfPerformance", event.target.value)} required />
          </label>
          <label className="span-2">
            <span>Description</span>
            <textarea value={form.description} onChange={(event) => updateField("description", event.target.value)} />
          </label>
        </div>
      </fieldset>
      <div className="form-actions">
        <button type="submit" disabled={!canManageContracts || contractStatus === "saving"}>
          {selectedContract ? "Update contract" : "Create contract"}
        </button>
      </div>
    </form>
  );
}

function ProfileView({
  canManageCompanyProfile,
  onProfileApplied,
  onSave,
  profile,
  profileMessage,
  profileStatus,
  profileValidationErrors
}: {
  canManageCompanyProfile: boolean;
  onProfileApplied: (profile: CompanyProfile) => void;
  onSave: (request: UpsertCompanyProfileRequest) => Promise<void>;
  profile: CompanyProfile | null;
  profileMessage: string;
  profileStatus: "idle" | "saving" | "saved" | "failed";
  profileValidationErrors: Record<string, string[]>;
}) {
  const [form, setForm] = useState<ProfileFormState>(() => profileToForm(profile));
  const [lookupQuery, setLookupQuery] = useState({ uei: "", legalBusinessName: "" });
  const [lookupResults, setLookupResults] = useState<CompanyEntityLookupResult[]>([]);
  const [lookupStatus, setLookupStatus] = useState<"idle" | "searching" | "applying" | "failed" | "applied">("idle");
  const [lookupMessage, setLookupMessage] = useState("");
  const visibleValidationErrors =
    Object.keys(profileValidationErrors).length > 0 ? profileValidationErrors : profile?.validationErrors ?? {};

  function validationId(field: string) {
    return `profile-validation-${field.replace(/[^a-z0-9]+/gi, "-").toLowerCase()}`;
  }

  function validationProps(field: string, label: string) {
    return visibleValidationErrors[field]?.length
      ? {
          "aria-describedby": validationId(field),
          "aria-invalid": true as const,
          "aria-label": label,
          "aria-required": true as const
        }
      : {};
  }

  function fieldValidation(field: string) {
    const messages = visibleValidationErrors[field];
    return messages?.length ? (
      <span className="field-error" id={validationId(field)}>
        {messages.join(" ")}
      </span>
    ) : null;
  }

  function updateField<TKey extends keyof ProfileFormState>(field: TKey, value: ProfileFormState[TKey]) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  function updateNaicsRow<TKey extends keyof NaicsFormRow>(index: number, field: TKey, value: NaicsFormRow[TKey]) {
    setForm((current) => ({
      ...current,
      naicsRows: current.naicsRows.map((naics, candidateIndex) =>
        candidateIndex === index ? { ...naics, [field]: value } : naics
      )
    }));
  }

  function updateCertificationRow<TKey extends keyof CertificationFormRow>(
    index: number,
    field: TKey,
    value: CertificationFormRow[TKey]
  ) {
    setForm((current) => ({
      ...current,
      certificationRows: current.certificationRows.map((certification, candidateIndex) =>
        candidateIndex === index ? { ...certification, [field]: value } : certification
      )
    }));
  }

  function setPrimaryNaics(index: number) {
    setForm((current) => ({
      ...current,
      naicsRows: current.naicsRows.map((naics, candidateIndex) => ({ ...naics, isPrimary: candidateIndex === index }))
    }));
  }

  function addNaicsRow() {
    setForm((current) => ({
      ...current,
      naicsRows: [
        ...current.naicsRows,
        { code: "", title: "", isPrimary: current.naicsRows.length === 0, sizeStandard: "", qualifiesAsSmall: "" }
      ]
    }));
  }

  function removeNaicsRow(index: number) {
    setForm((current) => {
      const nextRows = current.naicsRows.filter((_, candidateIndex) => candidateIndex !== index);
      const hasPrimary = nextRows.some((naics) => naics.isPrimary);
      return {
        ...current,
        naicsRows: nextRows.map((naics, candidateIndex) => ({
          ...naics,
          isPrimary: hasPrimary ? naics.isPrimary : candidateIndex === 0
        }))
      };
    });
  }

  function addCertificationRow() {
    setForm((current) => ({
      ...current,
      certificationRows: [
        ...current.certificationRows,
        { id: null, type: "Other", status: "Active", issuer: "", effectiveAt: "", expiresAt: "", referenceNumber: "" }
      ]
    }));
  }

  function removeCertificationRow(index: number) {
    setForm((current) => ({
      ...current,
      certificationRows:
        current.certificationRows.length === 1
          ? [{ id: null, type: "Wosb", status: "Active", issuer: "", effectiveAt: "", expiresAt: "", referenceNumber: "" }]
          : current.certificationRows.filter((_, candidateIndex) => candidateIndex !== index)
    }));
  }

  async function save(completeProfile: boolean) {
    await onSave(formToRequest(form, completeProfile));
  }

  async function searchSamGov() {
    setLookupStatus("searching");
    setLookupMessage("");
    const result = await searchCompanyEntity({
      uei: lookupQuery.uei.trim() || null,
      legalBusinessName: lookupQuery.legalBusinessName.trim() || null
    });

    if (result.data) {
      setLookupResults(result.data);
      setLookupStatus("idle");
      setLookupMessage(result.data.length === 0 ? "No SAM.gov matches found." : `${result.data.length} SAM.gov match found.`);
      return;
    }

    setLookupStatus("failed");
    setLookupMessage(result.error ?? "SAM.gov lookup failed.");
  }

  async function applySamGovResult(result: CompanyEntityLookupResult, confirmOverwrite: boolean) {
    setLookupStatus("applying");
    setLookupMessage("");
    const applied = await applyCompanyEntityLookup({
      result,
      selectedFields: ["legalEntityName", "uei", "cageCode", "samRegistrationExpiresAt", "address", "naics"],
      confirmOverwrite
    });

    if (applied.data) {
      onProfileApplied(applied.data);
      setForm(profileToForm(applied.data));
      setLookupStatus("applied");
      setLookupMessage("SAM.gov data applied.");
      return;
    }

    setLookupStatus("failed");
    setLookupMessage(applied.error ?? "SAM.gov data could not be applied.");
  }

  return (
    <section className="route-panel profile-route">
      <div className="section-heading section-heading--split">
        <div>
          <p className="eyebrow">Company profile</p>
          <h2>{profile?.legalEntityName || "Create company profile"}</h2>
        </div>
        <div className="completion-meter" aria-label="Profile completion">
          <span>{profile?.isComplete ? "Complete" : "Draft"}</span>
          <strong>{profile?.completionPercentage ?? 0}%</strong>
        </div>
      </div>

      {profileMessage ? (
        <p className={`form-status ${profileStatus === "failed" ? "form-status--error" : "form-status--ok"}`}>
          {profileMessage}
        </p>
      ) : null}

      {Object.keys(visibleValidationErrors).length > 0 ? (
        <div className="validation-summary validation-summary--error" role="alert">
          <p>Correct the following fields before completing the profile:</p>
          <ul>
            {Object.keys(visibleValidationErrors).map((field) => (
              <li key={field}>{field}</li>
            ))}
          </ul>
        </div>
      ) : null}

      <section className="profile-form" aria-label="SAM.gov entity lookup">
        <div className="form-grid">
          <label>
            <span>SAM UEI</span>
            <input value={lookupQuery.uei} onChange={(event) => setLookupQuery((current) => ({ ...current, uei: event.target.value }))} />
          </label>
          <label>
            <span>Legal business name</span>
            <input
              value={lookupQuery.legalBusinessName}
              onChange={(event) => setLookupQuery((current) => ({ ...current, legalBusinessName: event.target.value }))}
            />
          </label>
        </div>
        <div className="form-actions">
          <button type="button" onClick={searchSamGov} disabled={lookupStatus === "searching" || !canManageCompanyProfile}>
            Search SAM.gov
          </button>
        </div>
        {lookupMessage ? (
          <p className={`form-status ${lookupStatus === "failed" ? "form-status--error" : "form-status--ok"}`}>{lookupMessage}</p>
        ) : null}
        {lookupResults.length > 0 ? (
          <div className="validation-summary">
            {lookupResults.map((result) => (
              <div key={`${result.uei}-${result.retrievedAt}`}>
                <p>
                  <strong>{result.legalBusinessName}</strong> {result.uei} {result.cageCode ? `CAGE ${result.cageCode}` : ""}
                </p>
                <p>
                  {result.source} retrieved {formatUsDateTime(result.retrievedAt)} · {result.registrationStatus ?? "Status unknown"} · SAM expires{" "}
                  {result.samRegistrationExpiresAt ?? "unknown"}
                </p>
                <p>
                  {result.address
                    ? `${result.address.street1}, ${result.address.city}, ${result.address.stateOrProvince} ${result.address.postalCode}`
                    : "No address returned"}{" "}
                  · NAICS {result.naicsCodes.map((naics) => naics.code).join(", ") || "unknown"}
                </p>
                <div className="form-actions">
                  <button type="button" onClick={() => applySamGovResult(result, false)} disabled={!canManageCompanyProfile || lookupStatus === "applying"}>
                    Apply selected fields
                  </button>
                  <button type="button" onClick={() => applySamGovResult(result, true)} disabled={!canManageCompanyProfile || lookupStatus === "applying"}>
                    Apply and overwrite conflicts
                  </button>
                </div>
              </div>
            ))}
          </div>
        ) : null}
      </section>

      <form className="profile-form" onSubmit={(event) => event.preventDefault()}>
        <fieldset disabled={!canManageCompanyProfile || profileStatus === "saving"}>
          <div className="form-grid">
            <label>
              <span>Legal entity</span>
              <input
                {...validationProps("legalEntityName", "Legal entity")}
                value={form.legalEntityName}
                onChange={(event) => updateField("legalEntityName", event.target.value)}
              />
              {fieldValidation("legalEntityName")}
            </label>
            <label>
              <span>DBA</span>
              <input value={form.doingBusinessAs} onChange={(event) => updateField("doingBusinessAs", event.target.value)} />
            </label>
            <label>
              <span>UEI</span>
              <input {...validationProps("uei", "UEI")} value={form.uei} onChange={(event) => updateField("uei", event.target.value)} />
              {fieldValidation("uei")}
            </label>
            <label>
              <span>CAGE</span>
              <input
                {...validationProps("cageCode", "CAGE")}
                value={form.cageCode}
                onChange={(event) => updateField("cageCode", event.target.value)}
              />
              {fieldValidation("cageCode")}
            </label>
            <label>
              <span>SAM expires</span>
              <input
                {...validationProps("samRegistrationExpiresAt", "SAM expires")}
                type="date"
                value={form.samRegistrationExpiresAt}
                onChange={(event) => updateField("samRegistrationExpiresAt", event.target.value)}
              />
              {fieldValidation("samRegistrationExpiresAt")}
            </label>
            <label>
              <span>Role</span>
              <select
                {...validationProps("contractorRole", "Role")}
                value={form.contractorRole}
                onChange={(event) => updateField("contractorRole", event.target.value)}
              >
                <option value="Unknown">Unknown</option>
                <option value="Prime">Prime</option>
                <option value="Subcontractor">Subcontractor</option>
                <option value="Both">Both</option>
              </select>
              {fieldValidation("contractorRole")}
            </label>
            <div className="naics-editor span-2">
              <div className="naics-editor__header">
                <span>NAICS codes</span>
                <button type="button" onClick={() => addNaicsRow()} disabled={!canManageCompanyProfile || profileStatus === "saving"}>
                  Add NAICS
                </button>
              </div>
              {fieldValidation("naicsCodes")}
              {form.naicsRows.map((naics, index) => (
                <div className="naics-row" key={index}>
                  <label>
                    <span>Primary</span>
                    <input
                      checked={naics.isPrimary}
                      name="primary-naics"
                      type="radio"
                      onChange={() => setPrimaryNaics(index)}
                    />
                  </label>
                  <label>
                    <span>Code</span>
                    <input
                      {...(index === 0 ? validationProps("naicsCodes", "Code") : {})}
                      value={naics.code}
                      onChange={(event) => updateNaicsRow(index, "code", event.target.value)}
                    />
                  </label>
                  <label>
                    <span>Title</span>
                    <input value={naics.title} onChange={(event) => updateNaicsRow(index, "title", event.target.value)} />
                  </label>
                  <label>
                    <span>Size basis</span>
                    <input
                      {...validationProps(`naicsCodes[${index}].sizeStatus`, "Size basis")}
                      value={naics.sizeStandard}
                      onChange={(event) => updateNaicsRow(index, "sizeStandard", event.target.value)}
                    />
                  </label>
                  <label>
                    <span>Status</span>
                    <select
                      {...validationProps(`naicsCodes[${index}].sizeStatus`, "Status")}
                      value={naics.qualifiesAsSmall}
                      onChange={(event) => updateNaicsRow(index, "qualifiesAsSmall", event.target.value)}
                    >
                      <option value="">Unknown</option>
                      <option value="true">Small</option>
                      <option value="false">Other than small</option>
                    </select>
                    {fieldValidation(`naicsCodes[${index}].sizeStatus`)}
                  </label>
                  <button type="button" onClick={() => removeNaicsRow(index)} disabled={form.naicsRows.length === 1}>
                    Remove
                  </button>
                </div>
              ))}
            </div>
            <label className="span-2">
              <span>Agency customers</span>
              <input value={form.agencyCustomers} onChange={(event) => updateField("agencyCustomers", event.target.value)} />
            </label>
            <div className="certification-editor span-2">
              <div className="certification-editor__header">
                <span>Certifications</span>
                <button
                  type="button"
                  onClick={() => addCertificationRow()}
                  disabled={!canManageCompanyProfile || profileStatus === "saving"}
                >
                  Add certification
                </button>
              </div>
              {form.certificationRows.map((certification, index) => (
                <div className="certification-row" key={certification.id ?? index}>
                  <label>
                    <span>Type</span>
                    <select
                      value={certification.type}
                      onChange={(event) => updateCertificationRow(index, "type", event.target.value)}
                    >
                      <option value="EightA">8(a)</option>
                      <option value="Wosb">WOSB</option>
                      <option value="Edwosb">EDWOSB</option>
                      <option value="HubZone">HUBZone</option>
                      <option value="Sdvosb">SDVOSB</option>
                      <option value="Sdb">SDB</option>
                      <option value="Other">Custom</option>
                    </select>
                  </label>
                  <label>
                    <span>Certification status</span>
                    <select
                      value={certification.status}
                      onChange={(event) => updateCertificationRow(index, "status", event.target.value)}
                    >
                      <option value="Draft">Draft</option>
                      <option value="Active">Active</option>
                      <option value="ExpiringSoon">Expiring soon</option>
                      <option value="Expired">Expired</option>
                      <option value="Revoked">Revoked</option>
                      <option value="Unknown">Unknown</option>
                    </select>
                  </label>
                  <label>
                    <span>Issuer</span>
                    <input
                      value={certification.issuer}
                      onChange={(event) => updateCertificationRow(index, "issuer", event.target.value)}
                    />
                  </label>
                  <label>
                    <span>Effective</span>
                    <input
                      type="date"
                      value={certification.effectiveAt}
                      onChange={(event) => updateCertificationRow(index, "effectiveAt", event.target.value)}
                    />
                  </label>
                  <label>
                    <span>Expires</span>
                    <input
                      type="date"
                      value={certification.expiresAt}
                      onChange={(event) => updateCertificationRow(index, "expiresAt", event.target.value)}
                    />
                  </label>
                  <label>
                    <span>Reference</span>
                    <input
                      value={certification.referenceNumber}
                      onChange={(event) => updateCertificationRow(index, "referenceNumber", event.target.value)}
                    />
                  </label>
                  <button type="button" onClick={() => removeCertificationRow(index)}>
                    Remove
                  </button>
                </div>
              ))}
            </div>
            <label className="span-2">
              <span>Products and services</span>
              <textarea
                {...validationProps("productsAndServices", "Products and services")}
                value={form.productsAndServices}
                onChange={(event) => updateField("productsAndServices", event.target.value)}
              />
              {fieldValidation("productsAndServices")}
            </label>
            <label>
              <span>Employees</span>
              <select
                {...validationProps("employeeRange", "Employees")}
                value={form.employeeRange}
                onChange={(event) => updateField("employeeRange", event.target.value)}
              >
                <option value="Unknown">Unknown</option>
                <option value="Micro">Micro</option>
                <option value="Small">Small</option>
                <option value="MidSize">Mid-size</option>
                <option value="Large">Large</option>
              </select>
              {fieldValidation("employeeRange")}
            </label>
            <label>
              <span>Revenue</span>
              <select
                {...validationProps("revenueRange", "Revenue")}
                value={form.revenueRange}
                onChange={(event) => updateField("revenueRange", event.target.value)}
              >
                <option value="Unknown">Unknown</option>
                <option value="Micro">Micro</option>
                <option value="Small">Small</option>
                <option value="MidSize">Mid-size</option>
                <option value="Large">Large</option>
              </select>
              {fieldValidation("revenueRange")}
            </label>
            <label>
              <span>Location</span>
              <input
                {...validationProps("locations", "Location")}
                value={form.locationName}
                onChange={(event) => updateField("locationName", event.target.value)}
              />
              {fieldValidation("locations")}
            </label>
            <label>
              <span>Street</span>
              <input value={form.street1} onChange={(event) => updateField("street1", event.target.value)} />
            </label>
            <label>
              <span>City</span>
              <input value={form.city} onChange={(event) => updateField("city", event.target.value)} />
            </label>
            <label>
              <span>State</span>
              <input value={form.stateOrProvince} onChange={(event) => updateField("stateOrProvince", event.target.value)} />
            </label>
            <label>
              <span>Postal code</span>
              <input value={form.postalCode} onChange={(event) => updateField("postalCode", event.target.value)} />
            </label>
            <label>
              <span>Country</span>
              <input value={form.country} onChange={(event) => updateField("country", event.target.value)} />
            </label>
            <label className="span-2">
              <span>IT summary</span>
              <textarea
                {...validationProps("itEnvironment.description", "IT summary")}
                value={form.itDescription}
                onChange={(event) => updateField("itDescription", event.target.value)}
              />
              {fieldValidation("itEnvironment.description")}
            </label>
            <label>
              <span>FCI/CUI posture</span>
              <select
                {...validationProps("dataHandlingPosture", "FCI/CUI posture")}
                value={form.dataHandlingPosture}
                onChange={(event) => updateField("dataHandlingPosture", event.target.value)}
              >
                <option value="Unknown">Unknown</option>
                <option value="NoFciOrCui">No FCI/CUI</option>
                <option value="FciOnly">FCI only</option>
                <option value="Cui">CUI</option>
                <option value="Classified">Classified</option>
                <option value="ExportControlled">Export-controlled</option>
              </select>
              {fieldValidation("dataHandlingPosture")}
            </label>
            <label>
              <span>Key systems</span>
              <input value={form.keySystems} onChange={(event) => updateField("keySystems", event.target.value)} />
            </label>
            <label className="checkbox-label span-2">
              <input
                checked={form.usesExternalServiceProvider}
                type="checkbox"
                onChange={(event) => updateField("usesExternalServiceProvider", event.target.checked)}
              />
              <span>Uses external service provider</span>
            </label>
            <label className="span-2">
              <span>External service provider</span>
              <input
                value={form.externalServiceProviderName}
                onChange={(event) => updateField("externalServiceProviderName", event.target.value)}
              />
            </label>
          </div>
        </fieldset>
        <div className="form-actions">
          <button type="button" onClick={() => save(false)} disabled={!canManageCompanyProfile || profileStatus === "saving"}>
            Save draft
          </button>
          <button type="button" onClick={() => save(true)} disabled={!canManageCompanyProfile || profileStatus === "saving"}>
            Complete profile
          </button>
        </div>
      </form>
    </section>
  );
}

type CmmcAssessmentFormState = {
  name: string;
  level: "Level1" | "Level2";
  framework: string;
  status: string;
  startedAt: string;
  affirmationDueAt: string;
  ownerFunction: string;
  contractId: string;
};

const defaultCmmcAssessmentForm: CmmcAssessmentFormState = {
  name: "",
  level: "Level1",
  framework: "FarBasicSafeguarding",
  status: "Planned",
  startedAt: "",
  affirmationDueAt: "",
  ownerFunction: "",
  contractId: ""
};

function cmmcAssessmentToForm(assessment: CmmcAssessment): CmmcAssessmentFormState {
  return {
    name: assessment.name,
    level: assessment.level === "Level2" ? "Level2" : "Level1",
    framework: normalizeCmmcFrameworkForForm(assessment.framework, assessment.level),
    status: assessment.status,
    startedAt: assessment.startedAt,
    affirmationDueAt: assessment.affirmationDueAt ?? "",
    ownerFunction: assessment.ownerFunction,
    contractId: assessment.contractIds[0] ?? ""
  };
}

function normalizeCmmcFrameworkForForm(framework: string, level: string) {
  const normalized = framework.trim().toLowerCase();
  if (normalized === "far-52.204-21" || normalized === "farbasicsafeguarding" || normalized.includes("52.204-21")) {
    return "FarBasicSafeguarding";
  }

  if (normalized === "nistsp800171revision2" || normalized.includes("800-171 rev. 2") || normalized.includes("800-171 revision 2")) {
    return "NistSp800171Revision2";
  }

  if (normalized === "nistsp800171revision3" || normalized.includes("800-171 rev. 3") || normalized.includes("800-171 revision 3")) {
    return "NistSp800171Revision3";
  }

  if (normalized === "nistsp800172" || normalized.includes("800-172")) {
    return "NistSp800172";
  }

  if (normalized === "cmmc") {
    return "Cmmc";
  }

  return level === "Level2" ? "NistSp800171Revision2" : "FarBasicSafeguarding";
}

type CmmcPoamFormState = {
  controlId: string;
  weakness: string;
  plannedRemediation: string;
  ownerFunction: string;
  targetCompletionAt: string;
  riskLevel: string;
  status: string;
};

const defaultCmmcPoamForm: CmmcPoamFormState = {
  controlId: "",
  weakness: "",
  plannedRemediation: "",
  ownerFunction: "",
  targetCompletionAt: "",
  riskLevel: "High",
  status: "Open"
};

function CmmcView({
  assessments,
  canManageCmmc,
  controls,
  contracts,
  message,
  poamItems,
  poamMessage,
  poamStatus,
  selectedAssessmentId,
  onSave,
  onCreatePoam,
  onNewAssessment,
  onSelectAssessment,
  status
}: {
  assessments: CmmcAssessment[];
  canManageCmmc: boolean;
  controls: CmmcControlStatus[];
  contracts: ContractRecord[];
  message: string;
  poamItems: CmmcPoamItem[];
  poamMessage: string;
  poamStatus: "idle" | "saving" | "saved" | "failed";
  selectedAssessmentId: string | null;
  onSave: (assessmentId: string | null, request: UpsertCmmcAssessmentRequest) => Promise<void>;
  onCreatePoam: (request: UpsertCmmcPoamItemRequest) => Promise<void>;
  onNewAssessment: () => void;
  onSelectAssessment: (assessmentId: string) => Promise<void>;
  status: "idle" | "saving" | "saved" | "failed";
}) {
  const selectedAssessment = assessments.find((assessment) => assessment.id === selectedAssessmentId) ?? null;
  const [form, setForm] = useState<CmmcAssessmentFormState>(() =>
    selectedAssessment ? cmmcAssessmentToForm(selectedAssessment) : defaultCmmcAssessmentForm
  );
  const [poamForm, setPoamForm] = useState<CmmcPoamFormState>(defaultCmmcPoamForm);
  const activeControls = selectedAssessment ? controls : [];
  const activePoamItems = selectedAssessment ? poamItems : [];
  const controlsNeedingReview = activeControls.filter((control) => control.status === "NeedsReview" || control.result === "NotMet").length;
  const linkedEvidenceCount = activeControls.reduce((total, control) => total + control.evidenceItemIds.length, 0);
  const overduePoamCount = activePoamItems.filter((item) => item.isOverdue).length;

  function updateField<TKey extends keyof CmmcAssessmentFormState>(field: TKey, value: CmmcAssessmentFormState[TKey]) {
    setForm((current) => ({
      ...current,
      [field]: value,
      ...(field === "level"
        ? { framework: value === "Level1" ? "FarBasicSafeguarding" : "NistSp800171Revision2" }
        : {})
    }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    void onSave(selectedAssessment?.id ?? null, {
      name: form.name.trim(),
      type: "Readiness",
      level: form.level,
      framework: form.framework,
      status: form.status,
      startedAt: form.startedAt,
      completedAt: null,
      affirmationDueAt: form.affirmationDueAt || null,
      ownerFunction: form.ownerFunction.trim(),
      companyProfileId: null,
      contractIds: form.contractId ? [form.contractId] : []
    });
  }

  function startNewAssessment() {
    setForm(defaultCmmcAssessmentForm);
    setPoamForm(defaultCmmcPoamForm);
    onNewAssessment();
  }

  function updatePoamField<TKey extends keyof CmmcPoamFormState>(field: TKey, value: CmmcPoamFormState[TKey]) {
    setPoamForm((current) => ({
      ...current,
      [field]: value
    }));
  }

  function submitPoam(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!poamForm.controlId) {
      return;
    }

    void onCreatePoam({
      controlId: poamForm.controlId,
      weakness: poamForm.weakness.trim(),
      plannedRemediation: poamForm.plannedRemediation.trim(),
      riskLevel: poamForm.riskLevel,
      status: poamForm.status,
      ownerUserId: null,
      ownerFunction: poamForm.ownerFunction.trim(),
      targetCompletionAt: poamForm.targetCompletionAt,
      completedAt: poamForm.status === "Closed" ? poamForm.targetCompletionAt : null,
      remediationTaskId: null,
      evidenceItemIds: []
    });
  }

  return (
    <section className="route-panel" aria-label="CMMC readiness workspace">
      <div className="route-panel__intro">
        <p className="eyebrow">CMMC readiness</p>
        <h2>CMMC and NIST workspace</h2>
        <p>Create Level 1 or Level 2 readiness assessments, assign an owner, track dates, and watch completion progress.</p>
      </div>

      <WorkspaceMetricStrip
        items={[
          { label: "Assessments", value: assessments.length, tone: assessments.length > 0 ? "info" : "warning" },
          { label: "Controls needing review", value: controlsNeedingReview, tone: controlsNeedingReview > 0 ? "warning" : "success" },
          { label: "Linked evidence", value: linkedEvidenceCount, tone: linkedEvidenceCount > 0 ? "success" : "warning" },
          { label: "Overdue POA&M", value: overduePoamCount, tone: overduePoamCount > 0 ? "danger" : "success" }
        ]}
      />

      <form className="cmmc-create" onSubmit={submit}>
        <div className="section-heading section-heading--split">
          <div>
            <h3>{selectedAssessment ? "Edit readiness assessment" : "Create readiness assessment"}</h3>
            <p className="section-summary">
              {selectedAssessment
                ? "The selected readiness assessment is loaded into this form. Save changes to update the existing record."
                : "Create a Level 1 or Level 2 readiness workspace before tracking control status and POA&M work."}
            </p>
          </div>
          <Button
            disabled={!canManageCmmc || status === "saving"}
            onClick={startNewAssessment}
            size="sm"
            type="button"
            variant="secondary"
          >
            New assessment
          </Button>
        </div>
        <fieldset disabled={!canManageCmmc || status === "saving"}>
          <div className="form-grid">
            <label>
              <span>Assessment name</span>
              <input value={form.name} onChange={(event) => updateField("name", event.target.value)} required />
            </label>
            <label>
              <span>Target level</span>
              <select value={form.level} onChange={(event) => updateField("level", event.target.value as "Level1" | "Level2")}>
                <option value="Level1">Level 1</option>
                <option value="Level2">Level 2</option>
              </select>
            </label>
            <label>
              <span>Framework</span>
              <select value={form.framework} onChange={(event) => updateField("framework", event.target.value)}>
                <option value="FarBasicSafeguarding">FAR basic safeguarding</option>
                <option value="NistSp800171Revision2">NIST SP 800-171 Rev. 2</option>
                <option value="NistSp800171Revision3">NIST SP 800-171 Rev. 3</option>
                <option value="NistSp800172">NIST SP 800-172</option>
                <option value="Cmmc">CMMC</option>
              </select>
            </label>
            <label>
              <span>Status</span>
              <select value={form.status} onChange={(event) => updateField("status", event.target.value)}>
                <option value="Planned">Planned</option>
                <option value="InProgress">In progress</option>
                <option value="Complete">Complete</option>
                <option value="Expired">Expired</option>
                <option value="Superseded">Superseded</option>
              </select>
            </label>
            <label>
              <span>Started</span>
              <input type="date" value={form.startedAt} onChange={(event) => updateField("startedAt", event.target.value)} required />
            </label>
            <label>
              <span>Affirmation due</span>
              <input type="date" value={form.affirmationDueAt} onChange={(event) => updateField("affirmationDueAt", event.target.value)} />
            </label>
            <label>
              <span>Owner</span>
              <input
                list="cmmc-assessment-owner-options"
                value={form.ownerFunction}
                onChange={(event) => updateField("ownerFunction", event.target.value)}
                required
              />
              <datalist id="cmmc-assessment-owner-options">
                {ownerOptionsWith(form.ownerFunction).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </datalist>
            </label>
            <label>
              <span>Contract link</span>
              <select value={form.contractId} onChange={(event) => updateField("contractId", event.target.value)}>
                <option value="">No contract selected</option>
                {contracts.map((contract) => (
                  <option key={contract.id} value={contract.id}>
                    {contract.contractNumber} · {contract.title}
                  </option>
                ))}
              </select>
            </label>
          </div>
        </fieldset>
        <div className="form-actions">
          <button type="submit" disabled={!canManageCmmc || status === "saving"}>
            <ShieldCheck size={16} aria-hidden="true" />
            <span>{status === "saving" ? "Saving" : selectedAssessment ? "Save assessment" : "Create assessment"}</span>
          </button>
        </div>
        {!canManageCmmc ? <p className="form-status">ManageCmmc permission is required to create assessments.</p> : null}
        {status === "failed" ? (
          <Alert title="Assessment action failed" tone="danger">
            {message || "The CMMC assessment was not created."}
          </Alert>
        ) : message ? (
          <p className="form-status form-status--ok">{message}</p>
        ) : null}
      </form>

      <WorkflowColumn
        ariaLabel="CMMC readiness assessments"
        title="Readiness assessments"
        description="Completion progress is calculated from control statuses as the workspace fills in."
      >
        {assessments.length > 0 ? (
          <div className="evidence-list">
            {assessments.map((assessment) => (
              <TaskCard
                badges={
                  <>
                    <StatusPill label={formatCmmcLevel(assessment.level)} tone="info" />
                    <StatusPill label={formatEnumLabel(assessment.status)} tone={statusTone(assessment.status)} />
                    {assessment.id === selectedAssessmentId ? <StatusPill label="Selected" tone="success" /> : null}
                    {assessment.overduePoamItemCount > 0 ? <StatusPill label={`${assessment.overduePoamItemCount} overdue POA&M`} tone="danger" /> : null}
                  </>
                }
                className={assessment.id === selectedAssessmentId ? "ui-task-card--selected" : undefined}
                key={assessment.id}
                meta={[
                  { label: "Owner", value: formatOwnerLabel(assessment.ownerFunction) },
                  { label: "Complete", value: `${assessment.controlSummary.completionPercentage}%` },
                  { label: "Implemented", value: `${assessment.controlSummary.implemented}/${assessment.controlSummary.total}` },
                  { label: "Affirmation due", value: assessment.affirmationDueAt ?? "Not scheduled" },
                  { label: "Open POA&M", value: assessment.openPoamItemCount, tone: assessment.openPoamItemCount > 0 ? "warning" : "success" }
                ]}
                onClick={status === "saving" ? undefined : () => void onSelectAssessment(assessment.id)}
                title={assessment.name}
              >
                <span className="legacy-summary">
                  POA&M {assessment.openPoamItemCount} open · {assessment.overduePoamItemCount} overdue
                </span>
              </TaskCard>
            ))}
          </div>
        ) : (
          <EmptyState title="No CMMC assessment has started yet" body="Create a Level 1 or Level 2 workspace to begin tracking readiness." />
        )}
      </WorkflowColumn>

      <WorkflowColumn
        ariaLabel="CMMC control readiness"
        title="Control readiness"
        description="Source baseline, readiness status, and linked work items for the selected assessment."
      >
        {activeControls.length > 0 ? (
          <div className="evidence-list">
            {activeControls.map((control) => {
              const linkedEvidence = control.linkedEvidence ?? [];
              const openPoams = control.openPoamItems ?? [];

              return (
                <TaskCard
                  badges={
                    <>
                      <StatusPill label={formatEnumLabel(control.status)} tone={statusTone(control.status)} />
                      <StatusPill label={formatEnumLabel(control.result)} tone={statusTone(control.result)} />
                      <StatusPill label={`${formatEnumLabel(control.sourceConfidence)} confidence`} tone={confidenceTone(control.sourceConfidence)} />
                    </>
                  }
                  key={control.controlId}
                  meta={[
                    { label: "Family", value: control.family },
                    { label: "Source", value: control.sourceName },
                    { label: "Reviewed", value: control.sourceLastReviewedAt },
                    { label: "Evidence", value: linkedEvidence.length, tone: linkedEvidence.length > 0 ? "success" : "warning" },
                    { label: "Tasks", value: control.taskIds.length },
                    { label: "Assets", value: control.assetIds.length },
                    { label: "Open POA&M", value: openPoams.length, tone: openPoams.length > 0 ? "warning" : "neutral" }
                  ]}
                  title={`${control.controlId} · ${control.title}`}
                >
                  <span className="legacy-summary">
                    {control.status} · {control.result} · {control.sourceName} reviewed {control.sourceLastReviewedAt}
                  </span>
                  <span>{control.requirement}</span>
                  {linkedEvidence.length > 0 ? (
                    <span className="legacy-summary">
                      Evidence: {linkedEvidence.map((item) => `${item.title} (${formatEnumLabel(item.reviewStatus)})`).join("; ")}
                    </span>
                  ) : (
                    <span className="legacy-summary">Evidence: none linked</span>
                  )}
                  {openPoams.length > 0 ? (
                    <span className="legacy-summary">
                      Open POA&M: {openPoams.map((item) => `${item.title} (${formatEnumLabel(item.status)}, due ${item.dueDate})`).join("; ")}
                    </span>
                  ) : (
                    <span className="legacy-summary">Open POA&M: none</span>
                  )}
                  <span className="legacy-summary">
                    Tasks {control.taskIds.length} · Assets {control.assetIds.length}
                  </span>
                  <DataQualityWarnings warnings={cmmcControlQualityWarnings(control)} />
                </TaskCard>
              );
            })}
          </div>
        ) : (
          <EmptyState title="No controls loaded yet" body="Controls appear after a selected assessment has a Level 1 or Level 2 baseline." />
        )}
      </WorkflowColumn>

      <WorkflowColumn
        ariaLabel="CMMC POA&M remediation"
        title="POA&M remediation"
        description="Track control gaps, remediation owners, due dates, risk, and task-backed calendar work."
      >
        <form className="cmmc-create" onSubmit={submitPoam}>
          <fieldset disabled={!canManageCmmc || poamStatus === "saving" || !selectedAssessment || activeControls.length === 0}>
            <div className="form-grid">
              <label>
                <span>Control</span>
                <select value={poamForm.controlId} onChange={(event) => updatePoamField("controlId", event.target.value)} required>
                  <option value="">Select control</option>
                  {activeControls.map((control) => (
                    <option key={control.controlId} value={control.controlId}>
                      {control.controlId} · {control.title}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                <span>Risk</span>
                <select value={poamForm.riskLevel} onChange={(event) => updatePoamField("riskLevel", event.target.value)}>
                  <option value="Low">Low</option>
                  <option value="Medium">Medium</option>
                  <option value="High">High</option>
                  <option value="Critical">Critical</option>
                </select>
              </label>
              <label>
                <span>Status</span>
                <select value={poamForm.status} onChange={(event) => updatePoamField("status", event.target.value)}>
                  <option value="Open">Open</option>
                  <option value="InProgress">In progress</option>
                  <option value="WaitingForValidation">Waiting for validation</option>
                  <option value="Closed">Closed</option>
                  <option value="AcceptedRisk">Accepted risk</option>
                </select>
              </label>
              <label>
                <span>Owner</span>
                <input
                  list="cmmc-poam-owner-options"
                  value={poamForm.ownerFunction}
                  onChange={(event) => updatePoamField("ownerFunction", event.target.value)}
                  required
                />
                <datalist id="cmmc-poam-owner-options">
                  {ownerOptionsWith(poamForm.ownerFunction).map(([value, label]) => (
                    <option key={value} value={value}>
                      {label}
                    </option>
                  ))}
                </datalist>
              </label>
              <label>
                <span>Due date</span>
                <input
                  type="date"
                  value={poamForm.targetCompletionAt}
                  onChange={(event) => updatePoamField("targetCompletionAt", event.target.value)}
                  required
                />
              </label>
              <label className="span-2">
                <span>Gap</span>
                <input value={poamForm.weakness} onChange={(event) => updatePoamField("weakness", event.target.value)} required />
              </label>
              <label className="span-2">
                <span>Remediation plan</span>
                <textarea
                  value={poamForm.plannedRemediation}
                  onChange={(event) => updatePoamField("plannedRemediation", event.target.value)}
                  required
                />
              </label>
            </div>
          </fieldset>
          <div className="form-actions">
            <button
              type="submit"
              disabled={!canManageCmmc || poamStatus === "saving" || !selectedAssessment || activeControls.length === 0 || !poamForm.controlId}
            >
              <ClipboardCheck size={16} aria-hidden="true" />
              <span>{poamStatus === "saving" ? "Creating" : "Create POA&M"}</span>
            </button>
          </div>
          {selectedAssessment && activeControls.length === 0 ? (
            <p className="form-status form-status--error">Load a CMMC control baseline before creating POA&M items.</p>
          ) : null}
          {poamStatus === "failed" ? (
            <Alert title="POA&M action failed" tone="danger">
              {poamMessage || "The POA&M item was not created."}
            </Alert>
          ) : poamMessage ? (
            <p className="form-status form-status--ok">{poamMessage}</p>
          ) : null}
        </form>
        {activePoamItems.length > 0 ? (
          <div className="evidence-list">
            {activePoamItems.map((item) => (
              <TaskCard
                badges={
                  <>
                    <RiskBadge level={item.riskLevel} />
                    <StatusPill label={formatEnumLabel(item.status)} tone={statusTone(item.status)} />
                    {item.isOverdue ? <StatusPill label="Overdue" tone="danger" /> : null}
                  </>
                }
                key={item.id}
                meta={[
                  { label: "Owner", value: formatOwnerLabel(item.ownerFunction) },
                  { label: "Due", value: item.targetCompletionAt, tone: item.isOverdue ? "danger" : "neutral" },
                  { label: "Task", value: item.remediationTaskId ? "Linked" : "Not linked" },
                  { label: "Evidence", value: item.evidenceItemIds.length, tone: item.evidenceItemIds.length > 0 ? "success" : "warning" }
                ]}
                title={`${item.controlId} · ${item.weakness}`}
              >
                <span>{item.plannedRemediation}</span>
                <span className="legacy-summary">
                  Task {item.remediationTaskId ? "linked" : "not linked"} · Evidence {item.evidenceItemIds.length}
                  {item.isOverdue ? " · overdue" : ""}
                </span>
              </TaskCard>
            ))}
          </div>
        ) : (
          <EmptyState title="No POA&M items yet" body="Create remediation items for control gaps that need owner-tracked follow-up." />
        )}
      </WorkflowColumn>
    </section>
  );
}

function formatCmmcLevel(level: string) {
  return level === "Level1" ? "Level 1" : level === "Level2" ? "Level 2" : level;
}

type SubcontractorFormState = {
  name: string;
  contactName: string;
  contactEmail: string;
  status: string;
  roleDescription: string;
  smallBusinessStatus: string;
  cmmcStatus: string;
  requiredCmmcLevel: string;
  insuranceExpiresAt: string;
  ndaStatus: string;
  ownerFunction: string;
  worksharePercentage: string;
  contractId: string;
  hasCuiAccess: boolean;
  hasExportControlledAccess: boolean;
};

const defaultSubcontractorForm: SubcontractorFormState = {
  name: "Mission Supplier LLC",
  contactName: "Jane Contracts",
  contactEmail: "jane@example.com",
  status: "Prospective",
  roleDescription: "CUI helpdesk support",
  smallBusinessStatus: "Small",
  cmmcStatus: "Level 1 self-assessment draft",
  requiredCmmcLevel: "Level1",
  insuranceExpiresAt: "2027-01-31",
  ndaStatus: "Executed",
  ownerFunction: "Contracts",
  worksharePercentage: "35.5",
  contractId: "",
  hasCuiAccess: true,
  hasExportControlledAccess: true
};

const subcontractorStatusOptions = [
  ["Prospective", "Prospective"],
  ["Active", "Active"],
  ["Suspended", "Suspended"],
  ["Completed", "Completed"],
  ["Archived", "Archived"]
] as const;

const smallBusinessStatusOptions = [
  ["Unknown", "Unknown"],
  ["Small", "Small business"],
  ["Small, SDB", "Small, SDB"],
  ["OtherThanSmall", "Other than small"],
  ["SDB", "Small disadvantaged business"],
  ["WOSB", "WOSB"],
  ["EDWOSB", "EDWOSB"],
  ["HUBZone", "HUBZone"],
  ["SDVOSB", "SDVOSB"],
  ["8a", "8(a)"]
] as const;

const cmmcStatusOptions = [
  ["Unknown", "Unknown"],
  ["NotStarted", "Not started"],
  ["InProgress", "In progress"],
  ["Level 1 self-assessment draft", "Level 1 self-assessment draft"],
  ["Level 1 self-assessment complete", "Level 1 self-assessment complete"],
  ["Level 2 self-assessment draft", "Level 2 self-assessment draft"],
  ["Level 2 self-assessment complete", "Level 2 self-assessment complete"],
  ["Level 2 assessment scheduled", "Level 2 assessment scheduled"],
  ["Level 2 certified", "Level 2 certified"],
  ["Not required", "Not required"],
  ["NotApplicable", "Not applicable"]
] as const;

const requiredCmmcLevelOptions = [
  ["Unknown", "Unknown"],
  ["NotRequired", "Not required"],
  ["Level1", "Level 1"],
  ["Level2", "Level 2"],
  ["Level3", "Level 3"]
] as const;

const ndaStatusOptions = [
  ["Unknown", "Unknown"],
  ["NotOnFile", "Not on file"],
  ["Requested", "Requested"],
  ["Unsigned", "Unsigned"],
  ["Signed", "Signed"],
  ["Executed", "Executed"],
  ["OnFile", "On file"],
  ["NotRequired", "Not required"],
  ["Expired", "Expired"],
  ["Waived", "Waived"]
] as const;

function SubcontractorsView({
  canManageSubcontractors,
  contracts,
  detailMessage,
  detailStatus,
  evidenceItems,
  evidenceRequests,
  flowDowns,
  message,
  obligationItems,
  onCreateEvidenceRequest,
  onSaveFlowDown,
  onSubcontractorApplied,
  onCreate,
  onSelect,
  selectedSubcontractorId,
  status,
  subcontractors
}: {
  canManageSubcontractors: boolean;
  contracts: ContractRecord[];
  detailMessage: string;
  detailStatus: "idle" | "loading" | "saving" | "ready" | "failed";
  evidenceItems: EvidenceMetadata[];
  evidenceRequests: SubcontractorEvidenceRequest[];
  flowDowns: SubcontractorFlowDown[];
  message: string;
  obligationItems: ContractObligationDashboardItem[];
  onCreateEvidenceRequest: (subcontractorId: string, request: UpsertSubcontractorEvidenceRequestRequest) => Promise<void>;
  onSaveFlowDown: (
    subcontractorId: string,
    flowDownId: string | null,
    request: UpsertSubcontractorFlowDownRequest
  ) => Promise<void>;
  onSubcontractorApplied: (subcontractor: Subcontractor) => void;
  onCreate: (request: UpsertSubcontractorRequest) => Promise<void>;
  onSelect: (subcontractorId: string) => Promise<void>;
  selectedSubcontractorId: string | null;
  status: "idle" | "saving" | "saved" | "failed";
  subcontractors: Subcontractor[];
}) {
  const [form, setForm] = useState<SubcontractorFormState>(defaultSubcontractorForm);
  const [lookupQuery, setLookupQuery] = useState({ uei: "", legalBusinessName: "" });
  const [lookupResults, setLookupResults] = useState<SubcontractorEntityLookupResult[]>([]);
  const [lookupStatus, setLookupStatus] = useState<"idle" | "searching" | "applying" | "failed" | "applied">("idle");
  const [lookupMessage, setLookupMessage] = useState("");
  const selectedSubcontractor = subcontractors.find((subcontractor) => subcontractor.id === selectedSubcontractorId) ?? null;
  const suppliersWithSensitiveAccess = subcontractors.filter(
    (subcontractor) => subcontractor.hasCuiAccess || subcontractor.hasExportControlledAccess
  ).length;
  const activeSubcontractorCount = subcontractors.filter((subcontractor) => subcontractor.status === "Active").length;
  const expiredInsuranceCount = subcontractors.filter(
    (subcontractor) => subcontractor.insuranceExpiresAt && subcontractor.insuranceExpiresAt < new Date().toISOString().slice(0, 10)
  ).length;

  function updateField<TKey extends keyof SubcontractorFormState>(field: TKey, value: SubcontractorFormState[TKey]) {
    setForm((current) => ({
      ...current,
      [field]: value
    }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    void onCreate({
      name: form.name.trim(),
      uei: null,
      cageCode: null,
      status: form.status,
      roleDescription: form.roleDescription.trim(),
      smallBusinessStatus: form.smallBusinessStatus.trim(),
      cmmcStatus: form.cmmcStatus.trim(),
      insuranceExpiresAt: form.insuranceExpiresAt || null,
      ndaStatus: form.ndaStatus.trim(),
      workshareDescription: form.roleDescription.trim(),
      worksharePercentage: form.worksharePercentage ? Number(form.worksharePercentage) : null,
      hasFciAccess: true,
      hasCuiAccess: form.hasCuiAccess,
      hasExportControlledAccess: form.hasExportControlledAccess,
      requiredCmmcLevel: form.requiredCmmcLevel === "Unknown" ? null : form.requiredCmmcLevel,
      contactName: form.contactName.trim(),
      contactEmail: form.contactEmail.trim(),
      contactPhone: null,
      contactTitle: "Contracts Manager",
      contractIds: form.contractId ? [form.contractId] : [],
      ownerFunction: form.ownerFunction
    });
  }

  async function searchSelectedSubcontractor() {
    if (!selectedSubcontractor) {
      return;
    }

    setLookupStatus("searching");
    setLookupMessage("");
    const result = await searchSubcontractorEntity(selectedSubcontractor.id, {
      uei: lookupQuery.uei.trim() || null,
      legalBusinessName: lookupQuery.legalBusinessName.trim() || null
    });
    if (result.data) {
      setLookupResults(result.data);
      setLookupStatus("idle");
      setLookupMessage(result.data.length === 0 ? "No SAM.gov matches found." : `${result.data.length} SAM.gov match found.`);
      return;
    }

    setLookupStatus("failed");
    setLookupMessage(result.error ?? "SAM.gov subcontractor lookup failed.");
  }

  async function applySelectedSubcontractorResult(result: SubcontractorEntityLookupResult) {
    if (!selectedSubcontractor) {
      return;
    }

    setLookupStatus("applying");
    setLookupMessage("");
    const applied = await applySubcontractorEntityLookup(selectedSubcontractor.id, {
      result,
      selectedFields: ["name", "uei", "cageCode"]
    });
    if (applied.data) {
      onSubcontractorApplied(applied.data);
      setLookupStatus("applied");
      setLookupMessage("SAM.gov subcontractor data applied.");
      return;
    }

    setLookupStatus("failed");
    setLookupMessage(applied.error ?? "SAM.gov subcontractor data could not be applied.");
  }

  return (
    <section className="route-panel" aria-label="Subcontractor management">
      <div className="route-panel__intro">
        <p className="eyebrow">Flow-down tracking</p>
        <h2>Subcontractor management</h2>
        <p>Track supplier profile, contract links, CMMC posture, CUI access, export-control exposure, insurance, and NDA status.</p>
      </div>
      <WorkspaceMetricStrip
        items={[
          { label: "Suppliers", value: subcontractors.length, tone: subcontractors.length > 0 ? "info" : "warning" },
          { label: "Active", value: activeSubcontractorCount, tone: "info" },
          { label: "Sensitive access", value: suppliersWithSensitiveAccess, tone: suppliersWithSensitiveAccess > 0 ? "warning" : "success" },
          { label: "Expired insurance", value: expiredInsuranceCount, tone: expiredInsuranceCount > 0 ? "danger" : "success" }
        ]}
      />
      <form className="cmmc-create" onSubmit={submit}>
        <fieldset disabled={!canManageSubcontractors || status === "saving"}>
          <div className="form-grid">
            <label>
              <span>Legal name</span>
              <input value={form.name} onChange={(event) => updateField("name", event.target.value)} required />
            </label>
            <label>
              <span>Point of contact</span>
              <input value={form.contactName} onChange={(event) => updateField("contactName", event.target.value)} />
            </label>
            <label>
              <span>Contact email</span>
              <input value={form.contactEmail} onChange={(event) => updateField("contactEmail", event.target.value)} />
            </label>
            <label>
              <span>Status</span>
              <select value={form.status} onChange={(event) => updateField("status", event.target.value)}>
                {subcontractorStatusOptions.map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              <span>Small business</span>
              <select value={form.smallBusinessStatus} onChange={(event) => updateField("smallBusinessStatus", event.target.value)}>
                {smallBusinessStatusOptions.map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              <span>CMMC status</span>
              <select value={form.cmmcStatus} onChange={(event) => updateField("cmmcStatus", event.target.value)}>
                {cmmcStatusOptions.map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              <span>Required CMMC level from contract</span>
              <select value={form.requiredCmmcLevel} onChange={(event) => updateField("requiredCmmcLevel", event.target.value)}>
                {requiredCmmcLevelOptions.map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              <span>Insurance expires</span>
              <input type="date" value={form.insuranceExpiresAt} onChange={(event) => updateField("insuranceExpiresAt", event.target.value)} />
            </label>
            <label>
              <span>NDA status</span>
              <select value={form.ndaStatus} onChange={(event) => updateField("ndaStatus", event.target.value)}>
                {ndaStatusOptions.map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              <span>Owner</span>
              <input
                list="subcontractor-owner-options"
                value={form.ownerFunction}
                onChange={(event) => updateField("ownerFunction", event.target.value)}
                required
              />
              <datalist id="subcontractor-owner-options">
                {ownerOptionsWith(form.ownerFunction).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </datalist>
            </label>
            <label>
              <span>Workshare %</span>
              <input
                max="100"
                min="0"
                step="0.1"
                type="number"
                value={form.worksharePercentage}
                onChange={(event) => updateField("worksharePercentage", event.target.value)}
              />
            </label>
            <label>
              <span>Contract link</span>
              <select value={form.contractId} onChange={(event) => updateField("contractId", event.target.value)}>
                <option value="">No contract selected</option>
                {contracts.map((contract) => (
                  <option key={contract.id} value={contract.id}>
                    {contract.contractNumber} · {contract.title}
                  </option>
                ))}
              </select>
            </label>
            <label className="checkbox-label">
              <input
                checked={form.hasCuiAccess}
                type="checkbox"
                onChange={(event) => updateField("hasCuiAccess", event.target.checked)}
              />
              <span>CUI access allowed</span>
            </label>
            <label className="checkbox-label">
              <input
                checked={form.hasExportControlledAccess}
                type="checkbox"
                onChange={(event) => updateField("hasExportControlledAccess", event.target.checked)}
              />
              <span>Export-control exposure</span>
            </label>
            <label className="span-2">
              <span>Role</span>
              <textarea value={form.roleDescription} onChange={(event) => updateField("roleDescription", event.target.value)} />
            </label>
          </div>
        </fieldset>
        <div className="form-actions">
          <button type="submit" disabled={!canManageSubcontractors || status === "saving"}>
            <GitBranch size={16} aria-hidden="true" />
            <span>{status === "saving" ? "Creating" : "Create subcontractor"}</span>
          </button>
        </div>
        {status === "failed" ? (
          <Alert title="Subcontractor action failed" tone="danger">
            {message || "The subcontractor record was not created."}
          </Alert>
        ) : message ? (
          <p className="form-status form-status--ok">{message}</p>
        ) : null}
      </form>
      {selectedSubcontractor ? (
        <section className="profile-form" aria-label="Subcontractor SAM.gov lookup">
          <div className="form-grid">
            <label>
              <span>SAM UEI</span>
              <input value={lookupQuery.uei} onChange={(event) => setLookupQuery((current) => ({ ...current, uei: event.target.value }))} />
            </label>
            <label>
              <span>Legal business name</span>
              <input
                value={lookupQuery.legalBusinessName}
                onChange={(event) => setLookupQuery((current) => ({ ...current, legalBusinessName: event.target.value }))}
              />
            </label>
          </div>
          <div className="form-actions">
            <button type="button" onClick={searchSelectedSubcontractor} disabled={!canManageSubcontractors || lookupStatus === "searching"}>
              Search SAM.gov for selected subcontractor
            </button>
          </div>
          {lookupStatus === "failed" ? (
            <Alert title="SAM.gov lookup failed" tone="danger">
              {lookupMessage || "The supplier lookup did not complete."}
            </Alert>
          ) : lookupMessage ? (
            <p className="form-status form-status--ok">{lookupMessage}</p>
          ) : null}
          {lookupResults.length > 0 ? (
            <div className="validation-summary">
              {lookupResults.map((result) => (
                <div key={`${result.uei}-${result.retrievedAt}`}>
                  <p>
                    <strong>{result.legalBusinessName}</strong> {result.uei} {result.cageCode ? `CAGE ${result.cageCode}` : ""}
                  </p>
                  <p>
                  {result.source} retrieved {formatUsDateTime(result.retrievedAt)} · {result.registrationStatus ?? "Status unknown"} · SAM expires{" "}
                    {result.samRegistrationExpiresAt ?? "unknown"} · {result.exclusionStatus ?? "Exclusions unknown"}
                  </p>
                  <p>NAICS {result.naicsCodes.map((naics) => naics.code).join(", ") || "unknown"}</p>
                  <div className="form-actions">
                    <button type="button" onClick={() => applySelectedSubcontractorResult(result)} disabled={lookupStatus === "applying"}>
                      Apply selected fields
                    </button>
                  </div>
                </div>
              ))}
            </div>
          ) : null}
        </section>
      ) : null}
      {subcontractors.length > 0 ? (
        <div className="subcontractor-workspace">
          <div className="evidence-list" aria-label="Subcontractor list">
            {subcontractors.map((subcontractor) => (
              <button
                className={`evidence-list__item${subcontractor.id === selectedSubcontractorId ? " evidence-list__item--active" : ""}`}
                key={subcontractor.id}
                type="button"
                onClick={() => void onSelect(subcontractor.id)}
              >
                <strong>{subcontractor.name}</strong>
                <div className="scan-pill-row">
                  <StatusPill label={formatEnumLabel(subcontractor.status)} tone={statusTone(subcontractor.status)} />
                  {subcontractor.hasCuiAccess ? <StatusPill label="CUI access" tone="warning" /> : null}
                  {subcontractor.hasExportControlledAccess ? <StatusPill label="Export-control" tone="danger" /> : null}
                </div>
                <ScanMeta
                  items={[
                    { label: "Owner", value: formatOwnerLabel(subcontractor.ownerFunction) },
                    { label: "Small business", value: subcontractor.smallBusinessStatus },
                    { label: "CMMC", value: subcontractor.cmmcStatus },
                    { label: "Required CMMC", value: subcontractor.requiredCmmcLevel ?? "Unknown" },
                    {
                      label: "Insurance",
                      value: subcontractor.insuranceExpiresAt ?? "Not tracked",
                      tone:
                        subcontractor.insuranceExpiresAt && subcontractor.insuranceExpiresAt < new Date().toISOString().slice(0, 10)
                          ? "danger"
                          : "neutral"
                    },
                    { label: "NDA", value: subcontractor.ndaStatus },
                    { label: "Contracts", value: subcontractor.contractIds.length },
                    { label: "Workshare", value: subcontractor.worksharePercentage == null ? "Not set" : `${subcontractor.worksharePercentage}%` }
                  ]}
                />
                <span className="legacy-summary">
                  CUI access {subcontractor.hasCuiAccess ? "yes" : "no"} · export-control{" "}
                  {subcontractor.hasExportControlledAccess ? "yes" : "no"} · insurance {subcontractor.insuranceExpiresAt ?? "not tracked"}
                </span>
              </button>
            ))}
          </div>
          <SubcontractorDetailPanel
            canManageSubcontractors={canManageSubcontractors}
            contracts={contracts}
            detailMessage={detailMessage}
            detailStatus={detailStatus}
            evidenceItems={evidenceItems}
            evidenceRequests={evidenceRequests}
            flowDowns={flowDowns}
            obligationItems={obligationItems}
            onCreateEvidenceRequest={onCreateEvidenceRequest}
            onSaveFlowDown={onSaveFlowDown}
            subcontractor={selectedSubcontractor}
          />
        </div>
      ) : (
        <EmptyState title="No subcontractors have been added yet" body="Create a profile to track flow-downs, access flags, and supplier readiness." />
      )}
    </section>
  );
}

function SubcontractorDetailPanel({
  canManageSubcontractors,
  contracts,
  detailMessage,
  detailStatus,
  evidenceItems,
  evidenceRequests,
  flowDowns,
  obligationItems,
  onCreateEvidenceRequest,
  onSaveFlowDown,
  subcontractor
}: {
  canManageSubcontractors: boolean;
  contracts: ContractRecord[];
  detailMessage: string;
  detailStatus: "idle" | "loading" | "saving" | "ready" | "failed";
  evidenceItems: EvidenceMetadata[];
  evidenceRequests: SubcontractorEvidenceRequest[];
  flowDowns: SubcontractorFlowDown[];
  obligationItems: ContractObligationDashboardItem[];
  onCreateEvidenceRequest: (subcontractorId: string, request: UpsertSubcontractorEvidenceRequestRequest) => Promise<void>;
  onSaveFlowDown: (
    subcontractorId: string,
    flowDownId: string | null,
    request: UpsertSubcontractorFlowDownRequest
  ) => Promise<void>;
  subcontractor: Subcontractor | null;
}) {
  const [flowDownForm, setFlowDownForm] = useState({
    contractId: "",
    contractClauseId: "",
    obligationId: "",
    clauseNumber: "",
    title: "",
    status: "Required",
    signedEvidenceItemId: ""
  });
  const [evidenceRequestForm, setEvidenceRequestForm] = useState({
    requestedItem: "Signed flow-down acknowledgement",
    evidenceType: "SignedFlowDown",
    dueDate: "2026-07-31",
    status: "Sent",
    recipientName: "",
    recipientEmail: "",
    obligationId: "",
    relatedFlowDownClauseId: "",
    receivedEvidenceItemId: ""
  });

  if (detailStatus === "loading") {
    return (
      <section className="subcontractor-detail" aria-live="polite">
        <LoadingState label="Loading subcontractor flow-down detail" />
      </section>
    );
  }

  if (!subcontractor) {
    return <EmptyState title="Select a subcontractor" body="Choose a supplier to manage flow-downs and evidence requests." />;
  }

  function saveFlowDown(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!subcontractor) {
      return;
    }

    void onSaveFlowDown(subcontractor.id, null, {
      contractId: flowDownForm.contractId || null,
      contractClauseId: flowDownForm.contractClauseId || null,
      obligationId: flowDownForm.obligationId || null,
      clauseNumber: flowDownForm.clauseNumber.trim(),
      title: flowDownForm.title.trim(),
      status: flowDownForm.status,
      sentAt: flowDownForm.status === "Sent" ? new Date().toISOString().slice(0, 10) : null,
      acknowledgedAt: flowDownForm.status === "Acknowledged" ? new Date().toISOString().slice(0, 10) : null,
      signedAt: flowDownForm.status === "Signed" ? new Date().toISOString().slice(0, 10) : null,
      waivedAt: flowDownForm.status === "Waived" ? new Date().toISOString().slice(0, 10) : null,
      signedEvidenceItemId: flowDownForm.signedEvidenceItemId || null
    });
  }

  function createEvidenceRequest(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!subcontractor) {
      return;
    }

    if (!evidenceRequestForm.requestedItem.trim() || !evidenceRequestForm.dueDate) {
      return;
    }

    void onCreateEvidenceRequest(subcontractor.id, {
      requestedItem: evidenceRequestForm.requestedItem.trim(),
      requestedEvidenceTypes: [evidenceRequestForm.evidenceType],
      dueDate: evidenceRequestForm.dueDate,
      status: evidenceRequestForm.status,
      recipientName: evidenceRequestForm.recipientName.trim() || null,
      recipientEmail: evidenceRequestForm.recipientEmail.trim() || null,
      obligationId: evidenceRequestForm.obligationId || null,
      relatedFlowDownClauseId: evidenceRequestForm.relatedFlowDownClauseId || null,
      receivedEvidenceItemId: evidenceRequestForm.receivedEvidenceItemId || null,
      ownerFunction: subcontractor.ownerFunction || "Contracts"
    });
  }

  return (
    <section className="subcontractor-detail" aria-label="Subcontractor flow-down detail">
      <div className="section-heading section-heading--split">
        <div>
          <p className="eyebrow">Supplier detail</p>
          <h3>Selected subcontractor</h3>
          <p className="section-summary">
            {subcontractor.roleDescription} Contact {subcontractor.contactName ?? "not set"} · {subcontractor.contactEmail ?? "no email"}
          </p>
        </div>
        <div className="scan-pill-row">
          <StatusPill label={formatEnumLabel(subcontractor.status)} tone={statusTone(subcontractor.status)} />
          {subcontractor.hasCuiAccess ? <StatusPill label="CUI access" tone="warning" /> : null}
          {subcontractor.hasExportControlledAccess ? <StatusPill label="Export-control" tone="danger" /> : null}
        </div>
      </div>
      {detailStatus === "failed" ? (
        <Alert title="Subcontractor detail action failed" tone="danger">
          {detailMessage || "The selected supplier workflow could not be updated."}
        </Alert>
      ) : detailMessage ? (
        <p className="form-status form-status--ok">{detailMessage}</p>
      ) : null}
      <ScanMeta
        items={[
          { label: "Owner", value: formatOwnerLabel(subcontractor.ownerFunction) },
          { label: "CMMC status", value: subcontractor.cmmcStatus },
          { label: "Required CMMC", value: subcontractor.requiredCmmcLevel ?? "Unknown" },
          { label: "Insurance", value: subcontractor.insuranceExpiresAt ?? "Not tracked" },
          { label: "NDA", value: subcontractor.ndaStatus },
          { label: "Flow-downs", value: flowDowns.length, tone: flowDowns.length > 0 ? "info" : "warning" },
          {
            label: "Evidence requests",
            value: evidenceRequests.length,
            tone: evidenceRequests.some((request) => request.isOverdue) ? "danger" : evidenceRequests.length > 0 ? "info" : "warning"
          }
        ]}
      />
      <DataQualityWarnings warnings={subcontractorQualityWarnings(subcontractor, flowDowns, evidenceRequests)} />
      <div className="subcontractor-detail-grid">
        <form className="evidence-metadata" onSubmit={saveFlowDown}>
          <h3>Assign flow-down</h3>
          <fieldset disabled={!canManageSubcontractors || detailStatus === "saving"}>
            <div className="form-grid">
              <label>
                <span>Contract</span>
                <select
                  value={flowDownForm.contractId}
                  onChange={(event) => setFlowDownForm((current) => ({ ...current, contractId: event.target.value }))}
                >
                  <option value="">No contract</option>
                  {contracts.map((contract) => (
                    <option key={contract.id} value={contract.id}>
                      {contract.contractNumber}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                <span>Obligation</span>
                <select
                  value={flowDownForm.obligationId}
                  onChange={(event) => {
                    const obligation = obligationItems.find((item) => item.obligationId === event.target.value);
                    setFlowDownForm((current) => ({
                      ...current,
                      obligationId: event.target.value,
                      contractClauseId: obligation?.contractClauseId ?? current.contractClauseId,
                      clauseNumber: obligation?.clauseNumber ?? current.clauseNumber,
                      title: obligation?.title ?? current.title
                    }));
                  }}
                >
                  <option value="">Manual entry</option>
                  {obligationItems.map((item) => (
                    <option key={`${item.contractClauseId}-${item.obligationId}`} value={item.obligationId}>
                      {item.clauseNumber} · {item.title}
                    </option>
                  ))}
                </select>
              </label>
              <label>
              <span>Clause number</span>
              <input
                value={flowDownForm.clauseNumber}
                onChange={(event) => setFlowDownForm((current) => ({ ...current, clauseNumber: event.target.value }))}
                required
              />
              </label>
              <label>
                <span>Status</span>
                <select
                  value={flowDownForm.status}
                  onChange={(event) => setFlowDownForm((current) => ({ ...current, status: event.target.value }))}
                >
                  {["Required", "Sent", "Acknowledged", "Signed", "Waived", "NotApplicable"].map((statusOption) => (
                    <option key={statusOption} value={statusOption}>
                      {formatEnumLabel(statusOption)}
                    </option>
                  ))}
                </select>
              </label>
              <label className="span-2">
                <span>Title</span>
                <input
                  value={flowDownForm.title}
                  onChange={(event) => setFlowDownForm((current) => ({ ...current, title: event.target.value }))}
                  required
                />
              </label>
              <label className="span-2">
                <span>Signed evidence</span>
                <select
                  value={flowDownForm.signedEvidenceItemId}
                  onChange={(event) => setFlowDownForm((current) => ({ ...current, signedEvidenceItemId: event.target.value }))}
                >
                  <option value="">No evidence linked</option>
                  {evidenceItems.map((item) => (
                    <option key={item.id} value={item.id}>
                      {item.title} · {item.status}
                    </option>
                  ))}
                </select>
              </label>
            </div>
          </fieldset>
          <div className="form-actions">
            <button type="submit" disabled={!canManageSubcontractors || detailStatus === "saving"}>
              <GitBranch size={16} aria-hidden="true" />
              <span>Save flow-down</span>
            </button>
          </div>
        </form>
        <form className="evidence-metadata" onSubmit={createEvidenceRequest}>
          <h3>Request evidence</h3>
          <fieldset disabled={!canManageSubcontractors || detailStatus === "saving"}>
            <div className="form-grid">
              <label className="span-2">
                <span>Requested item</span>
                <input
                  value={evidenceRequestForm.requestedItem}
                  onChange={(event) => setEvidenceRequestForm((current) => ({ ...current, requestedItem: event.target.value }))}
                  required
                />
              </label>
              <label>
                <span>Evidence type</span>
                <select
                  value={evidenceRequestForm.evidenceType}
                  onChange={(event) => setEvidenceRequestForm((current) => ({ ...current, evidenceType: event.target.value }))}
                >
                  {["SignedFlowDown", "VendorAttestation", "SubcontractorCertification", "Policy", "Other"].map((type) => (
                    <option key={type} value={type}>
                      {formatEnumLabel(type)}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                <span>Due date</span>
                <input
                  type="date"
                  value={evidenceRequestForm.dueDate}
                  onChange={(event) => setEvidenceRequestForm((current) => ({ ...current, dueDate: event.target.value }))}
                  required
                />
              </label>
              <label>
                <span>Recipient</span>
                <input
                  value={evidenceRequestForm.recipientName}
                  onChange={(event) => setEvidenceRequestForm((current) => ({ ...current, recipientName: event.target.value }))}
                />
              </label>
              <label>
                <span>Email</span>
                <input
                  value={evidenceRequestForm.recipientEmail}
                  onChange={(event) => setEvidenceRequestForm((current) => ({ ...current, recipientEmail: event.target.value }))}
                />
              </label>
              <label>
                <span>Related flow-down</span>
                <select
                  value={evidenceRequestForm.relatedFlowDownClauseId}
                  onChange={(event) => setEvidenceRequestForm((current) => ({ ...current, relatedFlowDownClauseId: event.target.value }))}
                >
                  <option value="">None</option>
                  {flowDowns.map((flowDown) => (
                    <option key={flowDown.id} value={flowDown.id}>
                      {flowDown.clauseNumber} · {flowDown.status}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                <span>Status</span>
                <select
                  value={evidenceRequestForm.status}
                  onChange={(event) => setEvidenceRequestForm((current) => ({ ...current, status: event.target.value }))}
                >
                  {["Draft", "Sent", "Submitted", "Satisfied", "Cancelled"].map((statusOption) => (
                    <option key={statusOption} value={statusOption}>
                      {formatEnumLabel(statusOption)}
                    </option>
                  ))}
                </select>
              </label>
            </div>
          </fieldset>
          <div className="form-actions">
            <button
              type="submit"
              disabled={
                !canManageSubcontractors ||
                detailStatus === "saving" ||
                !evidenceRequestForm.requestedItem.trim() ||
                !evidenceRequestForm.dueDate
              }
            >
              <Send size={16} aria-hidden="true" />
              <span>Create request</span>
            </button>
          </div>
        </form>
      </div>
      <div className="subcontractor-detail-grid">
        <section className="evidence-metadata">
          <h3>Flow-down register</h3>
          {flowDowns.length > 0 ? (
            <div className="evidence-list">
              {flowDowns.map((flowDown) => (
                <article className="evidence-list__item" key={flowDown.id}>
                  <strong>{flowDown.clauseNumber} · {flowDown.title}</strong>
                  <div className="scan-pill-row">
                    <StatusPill label={formatEnumLabel(flowDown.status)} tone={statusTone(flowDown.status)} />
                    {flowDown.signedEvidenceItemId ? (
                      <StatusPill label="Evidence linked" tone="success" />
                    ) : (
                      <StatusPill label="Evidence missing" tone="warning" />
                    )}
                  </div>
                  <ScanMeta
                    items={[
                      { label: "Sent", value: flowDown.sentAt ?? "Not sent", tone: flowDown.sentAt ? "success" : "warning" },
                      { label: "Acknowledged", value: flowDown.acknowledgedAt ?? "Not acknowledged" },
                      { label: "Signed", value: flowDown.signedAt ?? "Not signed", tone: flowDown.signedAt ? "success" : "warning" },
                      { label: "Evidence", value: flowDown.signedEvidenceItemId ?? "Not linked" }
                    ]}
                  />
                </article>
              ))}
            </div>
          ) : (
            <EmptyState title="No flow-downs assigned" body="Assign required clauses from contract obligations or manual supplier terms." />
          )}
        </section>
        <section className="evidence-metadata">
          <h3>Evidence requests</h3>
          {evidenceRequests.length > 0 ? (
            <div className="evidence-list">
              {evidenceRequests.map((request) => (
                <article className={`evidence-list__item${request.isOverdue ? " evidence-list__item--overdue" : ""}`} key={request.id}>
                  <strong>{request.requestedItem}</strong>
                  <div className="scan-pill-row">
                    <StatusPill label={formatEnumLabel(request.status)} tone={statusTone(request.status)} />
                    {request.isOverdue ? <StatusPill label="Overdue" tone="danger" /> : null}
                    {request.receivedEvidenceItemId ? (
                      <StatusPill label="Received" tone="success" />
                    ) : (
                      <StatusPill label="Evidence needed" tone="warning" />
                    )}
                  </div>
                  <ScanMeta
                    items={[
                      { label: "Due", value: request.dueDate, tone: request.isOverdue ? "danger" : "neutral" },
                      { label: "Owner", value: formatOwnerLabel(request.ownerFunction ?? subcontractor.ownerFunction) },
                      { label: "Recipient", value: request.recipientName ?? "Not set" },
                      { label: "Evidence", value: request.receivedEvidenceItemId ?? "Not received" }
                    ]}
                  />
                  <EvidenceRequirementChips items={request.requestedEvidenceTypes} />
                </article>
              ))}
            </div>
          ) : (
            <EmptyState title="No evidence requests" body="Create supplier evidence requests for signed clauses, attestations, and certifications." />
          )}
        </section>
      </div>
    </section>
  );
}

function ReportsView({
  approvedEvidencePackages,
  assessments,
  canArchiveReports,
  canExportReports,
  canManageReports,
  controls,
  contracts,
  evidenceItems,
  generatedReports,
  recentReports,
  message,
  obligationItems,
  onApprovedEvidencePackageSelect,
  onCmmcReportGenerate,
  onComplianceReportGenerate,
  onEvidencePackageGenerate,
  onGeneratedReportSelect,
  onReportArchiveStateChange,
  onReportDetailClose,
  onSubcontractorReportGenerate,
  reportDetailMessage,
  reportDetailStatus,
  selectedReport,
  status,
  subcontractors
}: {
  approvedEvidencePackages: ApprovedEvidencePackage[];
  assessments: CmmcAssessment[];
  canArchiveReports: boolean;
  canExportReports: boolean;
  canManageReports: boolean;
  controls: CmmcControlLibrary[];
  contracts: ContractRecord[];
  evidenceItems: EvidenceMetadata[];
  generatedReports: ReportArtifact[];
  recentReports: ReportHistoryItem[];
  message: string;
  obligationItems: ContractObligationDashboardItem[];
  onApprovedEvidencePackageSelect: (reportId: string) => Promise<void>;
  onCmmcReportGenerate: (assessmentId: string) => Promise<void>;
  onComplianceReportGenerate: () => Promise<void>;
  onEvidencePackageGenerate: (request: EvidencePackageGenerateRequest) => Promise<void>;
  onGeneratedReportSelect: (report: ReportArtifact | ReportHistoryItem) => Promise<void>;
  onReportArchiveStateChange: (reportId: string, archived: boolean, reason: string) => Promise<boolean>;
  onReportDetailClose: () => void;
  onSubcontractorReportGenerate: (contractId?: string) => Promise<void>;
  reportDetailMessage: string;
  reportDetailStatus: ReportDetailStatus;
  selectedReport: ReportArtifact | null;
  status: "idle" | "loading" | "ready" | "failed";
  subcontractors: Subcontractor[];
}) {
  const generatedReportIds = new Set(generatedReports.map((report) => report.id));
  const visibleReports: Array<ReportArtifact | ReportHistoryItem> = [
    ...generatedReports,
    ...recentReports.filter((report) => !generatedReportIds.has(report.id))
  ];
  const [assessmentId, setAssessmentId] = useState(assessments[0]?.id ?? "");
  const [contractId, setContractId] = useState("");
  const [packageTitle, setPackageTitle] = useState("Prime review evidence package");
  const [packageScope, setPackageScope] = useState({
    obligationId: obligationItems[0]?.obligationId ?? "",
    contractId: contracts[0]?.id ?? "",
    controlId: "",
    subcontractorId: subcontractors[0]?.id ?? "",
    includeDraft: false
  });

  function generatePackage(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    void onEvidencePackageGenerate({
      title: packageTitle.trim(),
      obligationIds: packageScope.obligationId ? [packageScope.obligationId] : [],
      contractIds: packageScope.contractId ? [packageScope.contractId] : [],
      controlIds: packageScope.controlId ? [packageScope.controlId] : [],
      subcontractorIds: packageScope.subcontractorId ? [packageScope.subcontractorId] : [],
      includeDraftOrRejectedEvidence: packageScope.includeDraft
    });
  }

  return (
    <section className="route-panel" aria-label="Reports and audit packages">
      <div className="route-panel__intro">
        <p className="eyebrow">Reporting</p>
        <h2>Reports and audit packages</h2>
        <p>Generate tenant-scoped reports from source-backed obligations, CMMC readiness, subcontractor state, and approved evidence.</p>
      </div>
      <div className="form-status form-status--notice" role="note">
        Reports are workflow guidance only. They are not legal advice, certification decisions, assessor determinations,
        contracting-officer determinations, or government endorsements.
      </div>
      {!canManageReports ? (
        <div className="form-status form-status--notice" role="note">
          Your role can view existing reports but cannot generate new reports or evidence packages.
        </div>
      ) : null}
      {message ? <p className={`form-status ${status === "failed" ? "form-status--error" : "form-status--ok"}`}>{message}</p> : null}
      {canManageReports ? (
        <>
          <div className="report-action-grid">
            <section className="evidence-metadata">
              <h3>Compliance status</h3>
              <p>Snapshot obligation status, overdue tasks, evidence state, high-risk items, and readiness gaps.</p>
              <div className="form-actions">
                <button type="button" disabled={status === "loading"} onClick={() => void onComplianceReportGenerate()}>
                  <ScrollText size={16} aria-hidden="true" />
                  <span>Generate status</span>
                </button>
              </div>
            </section>
            <section className="evidence-metadata">
              <h3>CMMC readiness</h3>
              <label>
                <span>Assessment</span>
                <select value={assessmentId} onChange={(event) => setAssessmentId(event.target.value)}>
                  <option value="">Select assessment</option>
                  {assessments.map((assessment) => (
                    <option key={assessment.id} value={assessment.id}>
                      {assessment.name} · {assessment.level}
                    </option>
                  ))}
                </select>
              </label>
              <div className="form-actions">
                <button
                  type="button"
                  disabled={!assessmentId || status === "loading"}
                  onClick={() => void onCmmcReportGenerate(assessmentId)}
                >
                  <ShieldCheck size={16} aria-hidden="true" />
                  <span>Generate readiness</span>
                </button>
              </div>
            </section>
            <section className="evidence-metadata">
              <h3>Subcontractor compliance</h3>
              <label>
                <span>Contract filter</span>
                <select value={contractId} onChange={(event) => setContractId(event.target.value)}>
                  <option value="">All contracts</option>
                  {contracts.map((contract) => (
                    <option key={contract.id} value={contract.id}>
                      {contract.contractNumber}
                    </option>
                  ))}
                </select>
              </label>
              <div className="form-actions">
                <button type="button" disabled={status === "loading"} onClick={() => void onSubcontractorReportGenerate(contractId || undefined)}>
                  <UsersRound size={16} aria-hidden="true" />
                  <span>Generate supplier report</span>
                </button>
              </div>
            </section>
          </div>
          <form className="evidence-metadata" onSubmit={generatePackage}>
        <h3>Evidence package builder</h3>
        <div className="form-grid">
          <label className="span-2">
            <span>Package title</span>
            <input value={packageTitle} onChange={(event) => setPackageTitle(event.target.value)} />
          </label>
          <label>
            <span>Obligation</span>
            <select
              value={packageScope.obligationId}
              onChange={(event) => setPackageScope((current) => ({ ...current, obligationId: event.target.value }))}
            >
              <option value="">No obligation scope</option>
              {obligationItems.map((item) => (
                <option key={`${item.contractClauseId}-${item.obligationId}`} value={item.obligationId}>
                  {item.clauseNumber} · {item.title}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span>Contract</span>
            <select
              value={packageScope.contractId}
              onChange={(event) => setPackageScope((current) => ({ ...current, contractId: event.target.value }))}
            >
              <option value="">No contract scope</option>
              {contracts.map((contract) => (
                <option key={contract.id} value={contract.id}>
                  {contract.contractNumber}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span>Control ID</span>
            <select
              value={packageScope.controlId}
              onChange={(event) => setPackageScope((current) => ({ ...current, controlId: event.target.value }))}
            >
              <option value="">No control scope</option>
              {controls.map((control) => (
                <option key={control.controlId} value={control.controlId}>
                  {control.controlId} · {control.title}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span>Subcontractor</span>
            <select
              value={packageScope.subcontractorId}
              onChange={(event) => setPackageScope((current) => ({ ...current, subcontractorId: event.target.value }))}
            >
              <option value="">No subcontractor scope</option>
              {subcontractors.map((subcontractor) => (
                <option key={subcontractor.id} value={subcontractor.id}>
                  {subcontractor.name}
                </option>
              ))}
            </select>
          </label>
          <label className="checkbox-label">
            <input
              checked={packageScope.includeDraft}
              type="checkbox"
              onChange={(event) => setPackageScope((current) => ({ ...current, includeDraft: event.target.checked }))}
            />
            <span>Include draft/rejected evidence when authorized</span>
          </label>
        </div>
        <div className="form-actions">
          <button type="submit" disabled={!canManageReports || status === "loading"}>
            <FileDown size={16} aria-hidden="true" />
            <span>Generate package</span>
          </button>
        </div>
          </form>
        </>
      ) : null}
      <div className="report-action-grid">
        <section className="evidence-metadata">
          <h3>Recent generated reports</h3>
          {visibleReports.length > 0 ? (
            <div className="evidence-list">
              {visibleReports.map((report) => (
                <button
                  aria-pressed={selectedReport?.id === report.id}
                  className="evidence-list__item report-artifact-card"
                  data-testid="report-card"
                  key={report.id}
                  onClick={() => void onGeneratedReportSelect(report)}
                  type="button"
                >
                  <strong>{report.title}</strong>
                  <span>
                    {report.type} · {report.status} · {formatUsDateTime(report.generatedAt)}
                  </span>
                  <span>{reportCardSummary(report)}</span>
                  <span className="report-artifact-card__disclaimer">{report.disclaimer}</span>
                  <span className="report-artifact-card__action">View report details</span>
                </button>
              ))}
            </div>
          ) : (
            <EmptyState title="No reports have been generated yet" body="Use the report actions above to create persisted tenant-scoped snapshots." />
          )}
        </section>
        <section className="evidence-metadata">
          <h3>Approved evidence packages</h3>
          {approvedEvidencePackages.length > 0 ? (
            <div className="evidence-list">
              {approvedEvidencePackages.map((report) => (
                <button
                  aria-pressed={selectedReport?.id === report.reportId}
                  className="evidence-list__item report-artifact-card"
                  data-testid="report-card"
                  key={report.reportId}
                  onClick={() => void onApprovedEvidencePackageSelect(report.reportId)}
                  type="button"
                >
                  <strong>{report.title}</strong>
                  <span>
                    {report.status} · {report.evidenceItems.length} approved items · {formatUsDateOnly(report.generatedAt)}
                  </span>
                  <span className="report-artifact-card__disclaimer">{report.disclaimer}</span>
                  <span className="report-artifact-card__action">View package details</span>
                </button>
              ))}
            </div>
          ) : (
            <EmptyState title="No approved packages" body={`${evidenceItems.length} evidence records are available for future packages.`} />
          )}
        </section>
      </div>
      {reportDetailStatus === "loading" ? <LoadingState label="Loading report detail" /> : null}
      {reportDetailStatus === "failed" ? (
        <Alert title="Report detail did not load" tone="danger">
          {reportDetailMessage}
        </Alert>
      ) : null}
      {reportDetailStatus === "ready" && selectedReport ? (
        <ReportDetailPanel
          canArchive={canArchiveReports && "snapshot" in selectedReport}
          canExport={canExportReports && "snapshot" in selectedReport}
          report={selectedReport}
          onArchiveStateChange={onReportArchiveStateChange}
          onClose={onReportDetailClose}
        />
      ) : null}
    </section>
  );
}

function reportHistoryItem(
  report: ComplianceStatusReport | CmmcReadinessReport | SubcontractorComplianceReport
): ReportHistoryItem {
  return {
    id: report.id,
    tenantId: report.tenantId,
    type: report.type,
    status: report.status,
    title: report.title,
    generatedAt: report.generatedAt,
    generatedByUserId: report.generatedByUserId,
    disclaimer: report.disclaimer
  };
}

function reportCardSummary(report: ReportArtifact | ReportHistoryItem) {
  return "snapshot" in report || "manifest" in report ? renderReportSummary(report) : "Persisted report artifact";
}

function renderReportSummary(report: ReportArtifact) {
  if ("manifest" in report) {
    return `${report.manifest.items.length} evidence items · scope ${Object.values(report.manifest.scope)
      .filter((value) => Array.isArray(value) && value.length > 0)
      .length} dimensions`;
  }

  const snapshot = report.snapshot;
  const totalSubcontractors = typeof snapshot.totalSubcontractors === "number" ? `${snapshot.totalSubcontractors} subcontractors` : null;
  const openGaps = Array.isArray(snapshot.openGaps) ? `${snapshot.openGaps.length} CMMC gaps` : null;
  const highRisk = Array.isArray(snapshot.highRiskItems) ? `${snapshot.highRiskItems.length} high-risk items` : null;
  return [totalSubcontractors, openGaps, highRisk].filter(Boolean).join(" · ") || "Snapshot complete";
}

function ReportDetailPanel({
  canArchive,
  canExport,
  onArchiveStateChange,
  onClose,
  report
}: {
  canArchive: boolean;
  canExport: boolean;
  onArchiveStateChange: (reportId: string, archived: boolean, reason: string) => Promise<boolean>;
  onClose: () => void;
  report: ReportArtifact;
}) {
  const panelRef = useRef<HTMLElement>(null);
  const [lifecycleReason, setLifecycleReason] = useState("");
  const [lifecyclePending, setLifecyclePending] = useState(false);
  const [exportPending, setExportPending] = useState<"download" | "print" | null>(null);
  const [exportMessage, setExportMessage] = useState("");
  const exportOperationRef = useRef(0);
  const pendingPrintWindowRef = useRef<Window | null>(null);
  const metrics = reportDetailMetrics(report);
  const items = reportDetailItems(report);
  const isArchived = report.status === "Archived";

  useEffect(() => {
    const panel = panelRef.current;
    if (!panel) {
      return;
    }

    panel.scrollIntoView?.({ behavior: isDemoCaptureMode() ? "auto" : "smooth", block: "start" });
    panel.focus({ preventScroll: true });
  }, [report.id]);

  useEffect(() => () => {
    exportOperationRef.current += 1;
    pendingPrintWindowRef.current?.close();
    pendingPrintWindowRef.current = null;
  }, []);

  async function handleLifecycleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!lifecycleReason.trim()) {
      return;
    }

    setLifecyclePending(true);
    try {
      const succeeded = await onArchiveStateChange(report.id, !isArchived, lifecycleReason.trim());
      if (succeeded) {
        setLifecycleReason("");
      }
    } finally {
      setLifecyclePending(false);
    }
  }

  async function handlePdfExport(action: "download" | "print") {
    const operationId = ++exportOperationRef.current;
    const printWindow = action === "print" ? window.open("", "_blank") : null;
    if (action === "print" && !printWindow) {
      setExportMessage("The browser blocked the print window. Allow pop-ups for this site and try again.");
      return;
    }
    if (printWindow) {
      pendingPrintWindowRef.current = printWindow;
      printWindow.opener = null;
      printWindow.document.title = "Preparing report PDF";
      printWindow.document.body.textContent = "Preparing report PDF…";
    }

    setExportPending(action);
    setExportMessage("Preparing the tenant-scoped PDF…");
    try {
      const requested = await requestReportPdfExport(report.id);
      if (!requested.data) {
        throw new Error(requested.error ?? "The PDF export could not be requested.");
      }
      if (operationId !== exportOperationRef.current) {
        return;
      }

      let exportRecord = requested.data;
      for (let attempt = 0; exportRecord.status === "Queued" || exportRecord.status === "Processing"; attempt += 1) {
        if (attempt >= 60) {
          throw new Error("The PDF is still processing. Try again shortly.");
        }
        await new Promise<void>((resolve) => window.setTimeout(resolve, 1_000));
        if (operationId !== exportOperationRef.current) {
          return;
        }
        exportRecord = await getReportExport(exportRecord.id);
      }
      if (exportRecord.status !== "Ready") {
        throw new Error("The PDF could not be generated. Try again or contact an administrator.");
      }

      const downloaded = await downloadReportExport(exportRecord.id);
      if (operationId !== exportOperationRef.current) {
        return;
      }
      const objectUrl = URL.createObjectURL(downloaded.blob);
      if (action === "print" && printWindow) {
        printWindow.location.replace(objectUrl);
        printWindow.addEventListener("load", () => printWindow.print(), { once: true });
        window.setTimeout(() => URL.revokeObjectURL(objectUrl), 60_000);
        pendingPrintWindowRef.current = null;
        setExportMessage("The PDF opened in a print window.");
      } else {
        const link = document.createElement("a");
        link.href = objectUrl;
        link.download = downloaded.fileName;
        document.body.appendChild(link);
        link.click();
        link.remove();
        window.setTimeout(() => URL.revokeObjectURL(objectUrl), 0);
        setExportMessage("The PDF download started.");
      }
    } catch (error) {
      printWindow?.close();
      pendingPrintWindowRef.current = null;
      if (operationId === exportOperationRef.current) {
        setExportMessage(error instanceof Error ? error.message : "The PDF export could not be completed.");
      }
    } finally {
      if (operationId === exportOperationRef.current) {
        setExportPending(null);
      }
    }
  }

  return (
    <section
      className="report-detail"
      aria-label="Generated report detail"
      aria-live="polite"
      ref={panelRef}
      tabIndex={-1}
    >
      <div className="section-heading section-heading--split">
        <div>
          <p className="eyebrow">Report artifact</p>
          <h3>{report.title}</h3>
          <p>
            {formatEnumLabel(report.type)} · {formatEnumLabel(report.status)} · generated{" "}
            {formatUsDateTime(report.generatedAt)}
          </p>
        </div>
        <Button icon={<X size={16} aria-hidden="true" />} onClick={onClose} variant="secondary">
          Close detail
        </Button>
      </div>

      <Alert title="Artifact limitations" tone="warning">
        {report.disclaimer}
      </Alert>

      {canExport ? (
        <div className="form-actions" aria-label="Owner report export actions">
          <Button
            disabled={exportPending !== null}
            icon={<FileDown size={16} aria-hidden="true" />}
            onClick={() => void handlePdfExport("download")}
            variant="secondary"
          >
            {exportPending === "download" ? "Preparing PDF…" : "Download PDF"}
          </Button>
          <Button
            disabled={exportPending !== null}
            icon={<Printer size={16} aria-hidden="true" />}
            onClick={() => void handlePdfExport("print")}
            variant="secondary"
          >
            {exportPending === "print" ? "Preparing print…" : "Print PDF"}
          </Button>
        </div>
      ) : null}
      {exportMessage ? <p className="form-status" role="status">{exportMessage}</p> : null}

      {isArchived && "archiveReason" in report && report.archiveReason ? (
        <Alert title="Archived report" tone="warning">
          {String(report.archiveReason)}
        </Alert>
      ) : null}

      <dl className="report-detail__metrics">
        {metrics.map((metric) => (
          <div key={metric.label}>
            <dt>{metric.label}</dt>
            <dd>{metric.value}</dd>
          </div>
        ))}
      </dl>

      <div className="report-detail__content">
        <h4>Report content</h4>
        {items.length > 0 ? (
          <ul>
            {items.map((item, index) => (
              <li key={`${index}-${item}`}>{item}</li>
            ))}
          </ul>
        ) : (
          <p>No scoped detail rows were included in this report snapshot.</p>
        )}
      </div>

      {!isDemoCaptureMode() ? (
        <p className="report-detail__identifier">
          Report ID <code>{report.id}</code>
        </p>
      ) : null}

      {canArchive ? (
        <form className="evidence-form" onSubmit={(event) => void handleLifecycleSubmit(event)}>
          <label>
            <span>{isArchived ? "Restore reason" : "Archive reason"}</span>
            <textarea
              maxLength={500}
              required
              value={lifecycleReason}
              onChange={(event) => setLifecycleReason(event.target.value)}
            />
          </label>
          <div className="form-actions">
            <Button
              disabled={lifecyclePending || !lifecycleReason.trim()}
              icon={<Archive size={16} aria-hidden="true" />}
              type="submit"
              variant="secondary"
            >
              {lifecyclePending ? "Saving…" : isArchived ? "Restore report" : "Archive report"}
            </Button>
          </div>
        </form>
      ) : null}
    </section>
  );
}

function reportDetailMetrics(report: ReportArtifact): Array<{ label: string; value: string }> {
  if ("manifest" in report) {
    return [
      { label: "Evidence items", value: report.manifest.items.length.toString() },
      { label: "Obligation scope", value: report.manifest.scope.obligationIds.length.toString() },
      { label: "Contract scope", value: report.manifest.scope.contractIds.length.toString() },
      { label: "Control scope", value: report.manifest.scope.controlIds.length.toString() },
      { label: "Subcontractor scope", value: report.manifest.scope.subcontractorIds.length.toString() },
      {
        label: "Draft/rejected evidence",
        value: report.manifest.scope.includesDraftOrRejectedEvidence ? "Included by authorized override" : "Excluded"
      }
    ];
  }

  const snapshot = report.snapshot;
  if (report.type === "CmmcReadiness") {
    return [
      { label: "Assessment", value: snapshotText(snapshot, "assessmentName") },
      { label: "Target level", value: formatEnumLabel(snapshotText(snapshot, "targetLevel")) },
      { label: "Control rows", value: snapshotArrayLength(snapshot, "controlStatuses") },
      { label: "Open gaps", value: snapshotArrayLength(snapshot, "openGaps") },
      { label: "Open POA&M", value: snapshotArrayLength(snapshot, "openPoamItems") },
      { label: "Evidence links", value: snapshotArrayLength(snapshot, "evidenceLinks") }
    ];
  }

  if (report.type === "SubcontractorCompliance") {
    return [
      { label: "Subcontractors", value: snapshotText(snapshot, "totalSubcontractors") },
      { label: "Missing evidence", value: snapshotText(snapshot, "missingEvidenceRequests") },
      { label: "Overdue evidence", value: snapshotText(snapshot, "overdueEvidenceRequests") },
      { label: "Open flow-downs", value: snapshotText(snapshot, "openFlowDowns") },
      { label: "Contract scope", value: snapshotText(snapshot, "contractId", "All contracts") }
    ];
  }

  return [
    { label: "Total obligations", value: snapshotText(snapshot, "totalObligations") },
    { label: "High-risk obligations", value: snapshotText(snapshot, "highRiskObligations") },
    { label: "Overdue tasks", value: snapshotText(snapshot, "overdueTasks") },
    { label: "CMMC assessments", value: snapshotText(snapshot, "cmmcAssessments") },
    {
      label: "CMMC controls",
      value: `${snapshotText(snapshot, "cmmcControlsImplemented")} of ${snapshotText(snapshot, "cmmcControlsTotal")} implemented`
    },
    { label: "Subcontractor gaps", value: snapshotText(snapshot, "subcontractorGaps") }
  ];
}

function reportDetailItems(report: ReportArtifact): string[] {
  if ("manifest" in report) {
    return report.manifest.items.map(
      (item) => `${item.title} · ${formatEnumLabel(item.type)} · ${formatEnumLabel(item.status)}`
    );
  }

  const snapshot = report.snapshot;
  if (report.type === "CmmcReadiness") {
    return snapshotRecordItems(snapshot, "openGaps", (item) =>
      [recordText(item, "controlId"), recordText(item, "title"), formatEnumLabel(recordText(item, "status"))]
        .filter((value) => value && value !== "Not available")
        .join(" · ")
    );
  }

  if (report.type === "SubcontractorCompliance") {
    return snapshotRecordItems(snapshot, "rows", (item) =>
      [recordText(item, "name"), formatEnumLabel(recordText(item, "status")), recordText(item, "cmmcStatus")]
        .filter((value) => value && value !== "Not available")
        .join(" · ")
    );
  }

  const highRiskItems = snapshot.highRiskItems;
  return Array.isArray(highRiskItems) ? highRiskItems.filter((item): item is string => typeof item === "string") : [];
}

function snapshotText(snapshot: Record<string, unknown>, key: string, fallback = "Not available"): string {
  const value = snapshot[key];
  return typeof value === "string" || typeof value === "number" ? String(value) : fallback;
}

function snapshotArrayLength(snapshot: Record<string, unknown>, key: string): string {
  const value = snapshot[key];
  return Array.isArray(value) ? value.length.toString() : "0";
}

function snapshotRecordItems(
  snapshot: Record<string, unknown>,
  key: string,
  format: (item: Record<string, unknown>) => string
): string[] {
  const value = snapshot[key];
  return Array.isArray(value)
    ? value.filter((item): item is Record<string, unknown> => Boolean(item) && typeof item === "object").map(format)
    : [];
}

function recordText(record: Record<string, unknown>, key: string): string {
  const value = record[key];
  return typeof value === "string" || typeof value === "number" ? String(value) : "Not available";
}

function PostureNotice({ currentTenant }: { currentTenant: Tenant | null }) {
  const mode = currentTenant?.dataHandlingMode ?? "Unknown";
  const details = getPostureDetails(mode, isDemoCaptureMode());

  return (
    <section className={`posture-notice posture-notice--${details.tone}`} aria-label="MVP data handling posture">
      <div>
        <p className="eyebrow">MVP posture</p>
        <h2>{details.title}</h2>
        <p>{details.body}</p>
      </div>
      <div className="posture-notice__facts" aria-label="Current mode guardrails">
        <span>Active tenant: {currentTenant?.displayName ?? "Not loaded"}</span>
        <strong>Mode: {mode}</strong>
        <span>{details.limit}</span>
      </div>
    </section>
  );
}

function getPostureDetails(mode: string, demoCaptureMode = false) {
  if (mode === "CuiReady") {
    return {
      tone: "ready",
      title: "CUI-ready workflow gate is active",
      body:
        "This tenant can run CUI-ready workflows only after the shared responsibility matrix and approval checklist gates are satisfied. Continue to confirm classification before evidence, report, and extraction actions.",
      limit: "Reports remain workflow guidance, not certification or legal determinations."
    };
  }

  if (mode === "DemoSandbox") {
    return {
      tone: "sandbox",
      title: "DemoSandbox allows approved synthetic CUI examples only",
      body:
        "Use this tenant for UAT and training with seeded synthetic data. Do not upload real customer CUI, classified, export-controlled, payroll, tax, credential, or other prohibited sensitive data.",
      limit: "SyntheticCui is limited to approved seeded demo records."
    };
  }

  if (mode === "NoCui") {
    return {
      tone: "restricted",
      title: "No-CUI compliance management only",
      body: demoCaptureMode
        ? "Use fictional, redacted, or non-sensitive information for this demonstration. Do not enter or upload CUI, classified information, or prohibited sensitive content."
        : "This MVP posture blocks real CUI workflows. Use FCI, unclassified, and synthetic non-sensitive records only unless the tenant is formally moved through the CUI-ready approval gate.",
      limit: demoCaptureMode
        ? "Current posture: No-CUI readiness and compliance-management workflows."
        : "Real CUI, classified, and export-controlled data are blocked."
    };
  }

  return {
    tone: "restricted",
    title: "Tenant mode is not loaded",
    body:
      "Data handling controls depend on the active tenant. Avoid upload, evidence, report, and extraction actions until the tenant context is available.",
    limit: "Refresh the app or restart the API if this state persists."
  };
}

function sanitizeDemoCaptureText(value: string) {
  return value.replace(
    /\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b/gi,
    "a tenant user"
  );
}

function NotificationPreferencesPanel({
  message,
  onReminderRun,
  onSave,
  preference,
  reminderRunResult,
  status
}: {
  message: string;
  onReminderRun: (leadTimeDays: number) => Promise<void>;
  onSave: (request: NotificationPreferenceUpdateRequest) => Promise<void>;
  preference: NotificationPreference | null;
  reminderRunResult: DueDateReminderRunResult | null;
  status: "idle" | "saving" | "saved" | "failed";
}) {
  const [leadTimeDays, setLeadTimeDays] = useState(7);
  const [form, setForm] = useState<NotificationPreferenceUpdateRequest>({
    assignmentNotificationsEnabled: preference?.assignmentNotificationsEnabled ?? true,
    dueSoonNotificationsEnabled: preference?.dueSoonNotificationsEnabled ?? true,
    overdueNotificationsEnabled: preference?.overdueNotificationsEnabled ?? true,
    evidenceRequestNotificationsEnabled: preference?.evidenceRequestNotificationsEnabled ?? true,
    certificationRenewalNotificationsEnabled: preference?.certificationRenewalNotificationsEnabled ?? true,
    cmmcAffirmationNotificationsEnabled: preference?.cmmcAffirmationNotificationsEnabled ?? true
  });

  function updateFlag(field: keyof NotificationPreferenceUpdateRequest, value: boolean) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    void onSave(form);
  }

  return (
    <section className="invitation-section" aria-label="Notification preferences">
      <div className="section-heading">
        <p className="eyebrow">Notifications</p>
        <h2>Preferences and reminder runs</h2>
        <p className="section-summary">
          Control assignment, due-date, evidence request, certification renewal, and CMMC affirmation notifications for the active tenant role.
        </p>
      </div>
      <div className="settings-workspace">
        <form className="evidence-metadata" onSubmit={submit}>
          <h3>Preference toggles</h3>
          <div className="preference-toggle-grid">
            {(
              [
                ["assignmentNotificationsEnabled", "Assignment emails"],
                ["dueSoonNotificationsEnabled", "Due soon"],
                ["overdueNotificationsEnabled", "Overdue"],
                ["evidenceRequestNotificationsEnabled", "Evidence requests"],
                ["certificationRenewalNotificationsEnabled", "Certification renewals"],
                ["cmmcAffirmationNotificationsEnabled", "CMMC affirmations"]
              ] as Array<[keyof NotificationPreferenceUpdateRequest, string]>
            ).map(([field, label]) => (
              <label className="checkbox-label" key={field}>
                <input checked={form[field]} type="checkbox" onChange={(event) => updateFlag(field, event.target.checked)} />
                <span>{label}</span>
              </label>
            ))}
          </div>
          <div className="form-actions">
            <button type="submit" disabled={status === "saving"}>
              <Bell size={16} aria-hidden="true" />
              <span>Save preferences</span>
            </button>
          </div>
          {message ? <p className={`form-status ${status === "failed" ? "form-status--error" : "form-status--ok"}`}>{message}</p> : null}
        </form>
        <section className="evidence-metadata">
          <h3>Due-date reminder run</h3>
          <div className="form-grid">
            <label>
              <span>Lead time days</span>
              <input type="number" min="0" value={leadTimeDays} onChange={(event) => setLeadTimeDays(Number(event.target.value))} />
            </label>
          </div>
          <div className="form-actions">
            <button type="button" disabled={status === "saving"} onClick={() => void onReminderRun(leadTimeDays)}>
              <CalendarClock size={16} aria-hidden="true" />
              <span>Run reminders</span>
            </button>
          </div>
          {reminderRunResult ? (
            <div className="queue-metrics">
              <span>
                <strong>{reminderRunResult.upcomingSelected}</strong> upcoming
              </span>
              <span>
                <strong>{reminderRunResult.overdueSelected}</strong> overdue
              </span>
              <span>
                <strong>{reminderRunResult.created}</strong> created
              </span>
              <span>
                <strong>{reminderRunResult.failed}</strong> failed
              </span>
            </div>
          ) : (
            <p className="section-summary">Run the idempotent reminder job to create in-app notifications and email placeholders.</p>
          )}
        </section>
      </div>
    </section>
  );
}

function EvidenceView({
  acknowledgement,
  acknowledgementMessage,
  acknowledgementStatus,
  canManageEvidence,
  classificationReviewItems,
  controls,
  evidenceItems,
  evidenceMetadataMessage,
  evidenceMetadataStatus,
  obligationItems,
  onAcknowledge,
  onFileSelected,
  onMetadataSave,
  onReclassifyEvidence,
  onSelectEvidence,
  onUploadIntentSubmit,
  selectedEvidenceItemId,
  selectedFile,
  uploadMessage,
  uploadStatus
}: {
  acknowledgement: NoCuiAcknowledgementStatus;
  acknowledgementMessage: string;
  acknowledgementStatus: "idle" | "saving" | "saved" | "failed";
  canManageEvidence: boolean;
  classificationReviewItems: ContentClassificationReviewItem[];
  controls: CmmcControlLibrary[];
  evidenceItems: EvidenceMetadata[];
  evidenceMetadataMessage: string;
  evidenceMetadataStatus: "idle" | "saving" | "saved" | "failed";
  obligationItems: ContractObligationDashboardItem[];
  onAcknowledge: () => void;
  onFileSelected: (file: File | null) => void;
  onMetadataSave: (evidenceItemId: string | null, request: UpsertEvidenceMetadataRequest) => Promise<void>;
  onReclassifyEvidence: (evidenceItemId: string, request: ReclassifyContentRequest) => Promise<void>;
  onSelectEvidence: (evidenceItemId: string | null) => void;
  onUploadIntentSubmit: (
    event: FormEvent<HTMLFormElement>,
    classification: string,
    classificationReason: string,
    noCuiAttestation: boolean
  ) => void;
  selectedEvidenceItemId: string | null;
  selectedFile: File | null;
  uploadMessage: string;
  uploadStatus: "idle" | "creating" | "created" | "blocked";
}) {
  const uploadDisabled = !canManageEvidence || !acknowledgement.isAcknowledged;
  const selectedEvidence = evidenceItems.find((item) => item.id === selectedEvidenceItemId) ?? null;
  const [uploadClassification, setUploadClassification] = useState("Unclassified");
  const [uploadClassificationReason, setUploadClassificationReason] = useState("User confirmed upload classification.");
  const [noCuiAttestation, setNoCuiAttestation] = useState(false);
  const approvedEvidenceCount = evidenceItems.filter((item) => item.status === "Approved").length;
  const expiredEvidenceCount = evidenceItems.filter((item) => item.expiresAt && item.expiresAt < new Date().toISOString().slice(0, 10)).length;
  const linkedEvidenceCount = evidenceItems.filter((item) => item.obligationIds.length > 0 || item.controlIds.length > 0).length;

  return (
    <section className="route-panel" aria-label="Evidence upload workflow">
      <div className="route-panel__intro">
        <p className="eyebrow">Evidence vault</p>
        <h2>No-CUI evidence management</h2>
        <p>Organize evidence by obligation, contract, control, vendor, employee, expiration, approval status, and audit history.</p>
      </div>
      <WorkspaceMetricStrip
        items={[
          { label: "Evidence records", value: evidenceItems.length, tone: evidenceItems.length > 0 ? "info" : "warning" },
          { label: "Approved", value: approvedEvidenceCount, tone: approvedEvidenceCount > 0 ? "success" : "warning" },
          { label: "Linked", value: linkedEvidenceCount, tone: linkedEvidenceCount > 0 ? "success" : "warning" },
          { label: "Expired", value: expiredEvidenceCount, tone: expiredEvidenceCount > 0 ? "danger" : "success" },
          { label: "Review queue", value: classificationReviewItems.length, tone: classificationReviewItems.length > 0 ? "warning" : "success" }
        ]}
      />

      <EvidenceMetadataPanel
        key={selectedEvidence?.id ?? "new-evidence"}
        canManageEvidence={canManageEvidence}
        controls={controls}
        evidenceItems={evidenceItems}
        message={evidenceMetadataMessage}
        obligationItems={obligationItems}
        onSave={onMetadataSave}
        onReclassifyEvidence={onReclassifyEvidence}
        onSelectEvidence={onSelectEvidence}
        selectedEvidence={selectedEvidence}
        status={evidenceMetadataStatus}
      />

      <section className="evidence-metadata" aria-label="Classification review queue">
        <div className="section-heading--split">
          <div>
            <h3>Classification review</h3>
            <p>Unknown items are blocked from reports and extraction until reviewed; prohibited items route to escalation.</p>
          </div>
          <strong>{classificationReviewItems.length}</strong>
        </div>
        <div className="evidence-list">
          {classificationReviewItems.length > 0 ? (
            classificationReviewItems.map((item) => (
              <TaskCard
                badges={<ClassificationBadge classification={item.classification.classification} />}
                key={`${item.entityType}-${item.entityId}`}
                meta={[
                  { label: "Entity", value: item.entityType },
                  { label: "Route", value: item.reviewRoute }
                ]}
                title={item.title}
              />
            ))
          ) : (
            <EmptyState title="No classification reviews" body="Unknown and prohibited content will appear here for reviewer action." />
          )}
        </div>
      </section>

      <NoCuiAcknowledgementPanel
        acknowledgement={acknowledgement}
        acknowledgementMessage={acknowledgementMessage}
        acknowledgementStatus={acknowledgementStatus}
        canAcknowledge={canManageEvidence}
        context="evidence upload"
        onAcknowledge={onAcknowledge}
      />

      <form
        className="upload-panel"
        aria-label="Upload area"
        onSubmit={(event) => onUploadIntentSubmit(event, uploadClassification, uploadClassificationReason, noCuiAttestation)}
      >
        <div>
          <p className="eyebrow">Evidence files</p>
          <h3>Upload area</h3>
        </div>
        <label>
          <span>Evidence file</span>
          <input
            type="file"
            disabled={uploadDisabled}
            accept=".csv,.docx,.jpg,.jpeg,.pdf,.png,.txt,.xlsx"
            onChange={(event) => onFileSelected(event.target.files?.[0] ?? null)}
          />
        </label>
        <label>
          <span>Upload classification</span>
          <select
            value={uploadClassification}
            onChange={(event) => {
              setUploadClassification(event.target.value);
              setUploadClassificationReason(`User selected ${event.target.value} upload classification.`);
            }}
            disabled={uploadDisabled}
          >
            <option value="Unclassified">Unclassified</option>
            <option value="Fci">FCI</option>
            <option value="Cui">CUI</option>
            <option value="SyntheticCui">Synthetic CUI</option>
            <option value="Unknown">Unknown</option>
            <option value="Prohibited">Prohibited</option>
          </select>
        </label>
        <label>
          <span>Upload classification reason</span>
          <input
            value={uploadClassificationReason}
            onChange={(event) => setUploadClassificationReason(event.target.value)}
            disabled={uploadDisabled}
            required
          />
        </label>
        <p className="form-status">
          Allowed file types: PDF, PNG, JPG, TXT, CSV, DOCX, and XLSX. Maximum size: 25 MB.
        </p>
        <label className="checkbox-row">
          <input
            type="checkbox"
            checked={noCuiAttestation}
            onChange={(event) => setNoCuiAttestation(event.target.checked)}
            disabled={uploadDisabled}
            required
          />
          <span>
            I confirm this file does not contain CUI, classified information, export-controlled data, ITAR data, or sensitive government-furnished information.
          </span>
        </label>
        <button type="submit" disabled={uploadDisabled || !selectedFile || !noCuiAttestation || uploadStatus === "creating"}>
          <UploadCloud size={16} aria-hidden="true" />
          <span>{uploadStatus === "creating" ? "Uploading evidence" : "Upload evidence"}</span>
        </button>
        {!acknowledgement.isAcknowledged ? (
          <p className="form-status form-status--error">Upload is disabled until the No-CUI notice is acknowledged.</p>
        ) : null}
        {uploadStatus === "created" ? (
          <p className="form-status form-status--ok">{uploadMessage}</p>
        ) : null}
        {uploadStatus === "blocked" ? (
          <p className="form-status form-status--error">{uploadMessage || "The API blocked the upload. Confirm acknowledgement and permissions."}</p>
        ) : null}
      </form>

      {evidenceItems.length === 0 ? (
        <EmptyState
          title="No evidence has been uploaded yet"
          body="Accepted uploads remain pending until validation and malware scan workflow allow download."
        />
      ) : null}
    </section>
  );
}

function NoCuiAcknowledgementPanel({
  acknowledgement,
  acknowledgementMessage,
  acknowledgementStatus,
  canAcknowledge,
  context,
  onAcknowledge
}: {
  acknowledgement: NoCuiAcknowledgementStatus;
  acknowledgementMessage: string;
  acknowledgementStatus: "idle" | "saving" | "saved" | "failed";
  canAcknowledge: boolean;
  context: string;
  onAcknowledge: () => void;
}) {
  const [acknowledgedStatements, setAcknowledgedStatements] = useState({
    noRealCui: false,
    noProhibitedData: false,
    syntheticOnly: false,
    workflowGuidanceOnly: false
  });
  const allStatementsAcknowledged = Object.values(acknowledgedStatements).every(Boolean);

  function updateAcknowledgedStatement(key: keyof typeof acknowledgedStatements, checked: boolean) {
    setAcknowledgedStatements((currentStatements) => ({
      ...currentStatements,
      [key]: checked
    }));
  }

  return (
    <div className={`notice-panel${acknowledgement.isAcknowledged ? " notice-panel--acknowledged" : ""}`}>
      <span className="notice-panel__icon" aria-hidden="true">
        {acknowledgement.isAcknowledged ? <CheckCircle2 size={22} /> : <AlertTriangle size={22} />}
      </span>
      <div>
        <div className="notice-panel__header">
          <p className="eyebrow">Required before</p>
          <h3>{context}</h3>
          <p className="notice-panel__title">No-CUI acknowledgement</p>
        </div>
        {acknowledgement.isAcknowledged ? (
          <p className="notice-panel__summary">
            The current No-CUI notice is acknowledged for this tenant. Contract and evidence workflows remain limited to synthetic,
            redacted, or non-sensitive data.
          </p>
        ) : (
          <>
          <p>{acknowledgement.noticeCopy}</p>
          <fieldset className="acknowledgement-checklist">
            <legend>Required user acknowledgement</legend>
            <label className="checkbox-row">
              <input
                type="checkbox"
                checked={acknowledgedStatements.noRealCui}
                onChange={(event) => updateAcknowledgedStatement("noRealCui", event.target.checked)}
                disabled={!canAcknowledge || acknowledgementStatus === "saving"}
              />
              <span>I will not upload, paste, import, or attach real CUI.</span>
            </label>
            <label className="checkbox-row">
              <input
                type="checkbox"
                checked={acknowledgedStatements.noProhibitedData}
                onChange={(event) => updateAcknowledgedStatement("noProhibitedData", event.target.checked)}
                disabled={!canAcknowledge || acknowledgementStatus === "saving"}
              />
              <span>I will not upload classified information, ITAR/export-controlled data, credentials, payroll records, SSNs, health data, or sensitive incident details.</span>
            </label>
            <label className="checkbox-row">
              <input
                type="checkbox"
                checked={acknowledgedStatements.syntheticOnly}
                onChange={(event) => updateAcknowledgedStatement("syntheticOnly", event.target.checked)}
                disabled={!canAcknowledge || acknowledgementStatus === "saving"}
              />
              <span>I will use synthetic, redacted, or non-sensitive data during the pilot.</span>
            </label>
            <label className="checkbox-row">
              <input
                type="checkbox"
                checked={acknowledgedStatements.workflowGuidanceOnly}
                onChange={(event) => updateAcknowledgedStatement("workflowGuidanceOnly", event.target.checked)}
                disabled={!canAcknowledge || acknowledgementStatus === "saving"}
              />
              <span>I understand FeDril reports are workflow guidance, not legal advice or certification decisions.</span>
            </label>
          </fieldset>
          </>
        )}
        <dl className="notice-meta">
          <div>
            <dt>Notice version</dt>
            <dd>{acknowledgement.noticeVersion}</dd>
          </div>
          <div>
            <dt>Status</dt>
            <dd>{acknowledgement.isAcknowledged ? "Acknowledged" : "Not acknowledged"}</dd>
          </div>
          {acknowledgement.acknowledgedAt ? (
            <div>
              <dt>Acknowledged</dt>
              <dd>{formatUsDateTime(acknowledgement.acknowledgedAt)}</dd>
            </div>
          ) : null}
        </dl>
        {!acknowledgement.isAcknowledged ? (
          <button
            className="notice-action"
            type="button"
            disabled={!canAcknowledge || !allStatementsAcknowledged || acknowledgementStatus === "saving"}
            onClick={onAcknowledge}
          >
            <CheckCircle2 size={16} aria-hidden="true" />
            <span>{acknowledgementStatus === "saving" ? "Saving" : "I acknowledge the No-CUI upload limitation"}</span>
          </button>
        ) : (
          <p className="form-status form-status--ok">Acknowledgement already saved. No additional action is required.</p>
        )}
        {!canAcknowledge ? <p className="form-status">Required permission is missing for acknowledgement.</p> : null}
        {acknowledgementStatus === "saved" ? (
          <p className="form-status form-status--ok">{acknowledgementMessage || "Acknowledgement saved."}</p>
        ) : null}
        {acknowledgementStatus === "failed" ? (
          <p className="form-status form-status--error">{acknowledgementMessage || "Acknowledgement was not saved."}</p>
        ) : null}
      </div>
    </div>
  );
}

type EvidenceMetadataFormState = {
  title: string;
  type: string;
  ownerFunction: string;
  status: string;
  effectiveAt: string;
  expiresAt: string;
  tags: string;
  obligationIds: string;
  controlIds: string;
  classification: string;
  classificationReason: string;
  description: string;
};

const defaultEvidenceMetadataForm: EvidenceMetadataFormState = {
  title: "",
  type: "Policy",
  ownerFunction: "Security",
  status: "Requested",
  effectiveAt: "",
  expiresAt: "",
  tags: "",
  obligationIds: "",
  controlIds: "",
  classification: "Unclassified",
  classificationReason: "User confirmed evidence classification.",
  description: ""
};

function EvidenceMetadataPanel({
  canManageEvidence,
  controls,
  evidenceItems,
  message,
  obligationItems,
  onSave,
  onReclassifyEvidence,
  onSelectEvidence,
  selectedEvidence,
  status
}: {
  canManageEvidence: boolean;
  controls: CmmcControlLibrary[];
  evidenceItems: EvidenceMetadata[];
  message: string;
  obligationItems: ContractObligationDashboardItem[];
  onSave: (evidenceItemId: string | null, request: UpsertEvidenceMetadataRequest) => Promise<void>;
  onReclassifyEvidence: (evidenceItemId: string, request: ReclassifyContentRequest) => Promise<void>;
  onSelectEvidence: (evidenceItemId: string | null) => void;
  selectedEvidence: EvidenceMetadata | null;
  status: "idle" | "saving" | "saved" | "failed";
}) {
  const [form, setForm] = useState<EvidenceMetadataFormState>(() => evidenceToMetadataForm(selectedEvidence));
  const evidenceDateError =
    form.effectiveAt && form.expiresAt && form.expiresAt < form.effectiveAt
      ? "Expires date must be on or after Effective date."
      : "";

  function updateField<TKey extends keyof EvidenceMetadataFormState>(field: TKey, value: EvidenceMetadataFormState[TKey]) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  function save() {
    if (evidenceDateError) {
      return;
    }

    void onSave(selectedEvidence?.id ?? null, evidenceMetadataFormToRequest(form, selectedEvidence));
  }

  function reclassify() {
    if (!selectedEvidence) {
      return;
    }

    void onReclassifyEvidence(selectedEvidence.id, {
      classification: {
        classification: form.classification,
        source: "AdminReviewed",
        confidence: null,
        reviewedByUserId: null,
        reviewedAt: null,
        reason: form.classificationReason,
        isApprovedDemoContent: false
      }
    });
  }

  return (
    <section className="evidence-metadata" aria-label="Evidence metadata">
      <div className="section-heading--split">
        <div>
          <h3>Evidence metadata</h3>
          <p>Create reusable proof records with tags, expiration dates, status, and source links.</p>
        </div>
        <button type="button" onClick={() => onSelectEvidence(null)}>
          New evidence
        </button>
      </div>
      <div className="evidence-metadata__workspace">
        <div>
          <h4>Evidence list</h4>
          <div className="evidence-list" aria-label="Evidence list">
            {evidenceItems.length > 0 ? (
              evidenceItems.map((item) => (
                <button
                  className={`evidence-list__item${selectedEvidence?.id === item.id ? " evidence-list__item--active" : ""}`}
                  data-testid="evidence-item"
                  key={item.id}
                  type="button"
                  onClick={() => onSelectEvidence(item.id)}
                >
                  <strong>{item.title}</strong>
                  <div className="scan-pill-row">
                    <StatusPill label={formatEnumLabel(item.status)} tone={statusTone(item.status)} />
                    <ClassificationBadge classification={item.classification.classification} />
                    {item.expiresAt && item.expiresAt < new Date().toISOString().slice(0, 10) ? (
                      <StatusPill label="Expired" tone="danger" />
                    ) : null}
                  </div>
                  <ScanMeta
                    items={[
                      { label: "Owner", value: formatOwnerLabel(item.ownerFunction) },
                      {
                        label: "Expires",
                        value: item.expiresAt ?? "No expiration",
                        tone: item.expiresAt && item.expiresAt < new Date().toISOString().slice(0, 10) ? "danger" : "neutral"
                      },
                      { label: "Obligations", value: item.obligationIds.length, tone: item.obligationIds.length > 0 ? "success" : "warning" },
                      { label: "Controls", value: item.controlIds.length, tone: item.controlIds.length > 0 ? "success" : "neutral" },
                      { label: "Contracts", value: item.contractIds.length },
                      { label: "Tags", value: item.tags.length || "None" }
                    ]}
                  />
                  <DataQualityWarnings warnings={evidenceQualityWarnings(item)} />
                </button>
              ))
            ) : (
              <EmptyState title="No evidence in the evidence list yet" body="Create a reusable evidence record before uploading files." />
            )}
          </div>
        </div>
        <form
          className="evidence-metadata-form"
          onSubmit={(event) => {
            event.preventDefault();
            save();
          }}
        >
          <fieldset disabled={!canManageEvidence || status === "saving"}>
            <div className="form-grid">
              <label>
                <span>Title</span>
                <input value={form.title} onChange={(event) => updateField("title", event.target.value)} required />
              </label>
              <label>
                <span>Type</span>
                <select value={form.type} onChange={(event) => updateField("type", event.target.value)}>
                  <option value="Policy">Policy</option>
                  <option value="TrainingRecord">Training record</option>
                  <option value="Screenshot">Screenshot</option>
                  <option value="SystemConfiguration">System configuration</option>
                  <option value="VendorAttestation">Vendor attestation</option>
                  <option value="SubcontractorCertification">Subcontractor certification</option>
                  <option value="SignedFlowDown">Signed flow-down</option>
                  <option value="PayrollRecord">Payroll record</option>
                  <option value="IncidentRecord">Incident record</option>
                  <option value="AccessReview">Access review</option>
                  <option value="RiskAssessment">Risk assessment</option>
                  <option value="MeetingNote">Meeting note</option>
                  <option value="CorrectiveActionPlan">Corrective action plan</option>
                  <option value="Other">Other</option>
                </select>
              </label>
              <label>
                <span>Owner</span>
                <input
                  list="evidence-owner-options"
                  value={form.ownerFunction}
                  onChange={(event) => updateField("ownerFunction", event.target.value)}
                  required
                />
                <datalist id="evidence-owner-options">
                  {ownerOptionsWith(form.ownerFunction).map(([value, label]) => (
                    <option key={value} value={value}>
                      {label}
                    </option>
                  ))}
                </datalist>
              </label>
              <label>
                <span>Status</span>
                <select value={form.status} onChange={(event) => updateField("status", event.target.value)}>
                  <option value="Draft">Draft</option>
                  <option value="Requested">Requested</option>
                  <option value="Uploaded">Uploaded</option>
                  <option value="Submitted">Submitted</option>
                  <option value="InReview">In review</option>
                  <option value="Approved">Approved</option>
                  <option value="Rejected">Rejected</option>
                  <option value="Expired">Expired</option>
                  <option value="Archived">Archived</option>
                </select>
              </label>
              <label>
                <span>Effective</span>
                <input
                  aria-label="Effective"
                  aria-describedby={evidenceDateError ? "evidence-date-error" : undefined}
                  aria-invalid={Boolean(evidenceDateError)}
                  max={form.expiresAt || undefined}
                  type="date"
                  value={form.effectiveAt}
                  onChange={(event) => updateField("effectiveAt", event.target.value)}
                />
              </label>
              <label>
                <span>Expires</span>
                <input
                  aria-label="Expires"
                  aria-describedby={evidenceDateError ? "evidence-date-error" : undefined}
                  aria-invalid={Boolean(evidenceDateError)}
                  min={form.effectiveAt || undefined}
                  type="date"
                  value={form.expiresAt}
                  onChange={(event) => updateField("expiresAt", event.target.value)}
                />
                {evidenceDateError ? (
                  <span className="field-error" id="evidence-date-error" role="alert">
                    {evidenceDateError}
                  </span>
                ) : null}
              </label>
              <label>
                <span>Tags</span>
                <input value={form.tags} onChange={(event) => updateField("tags", event.target.value)} />
              </label>
              <label>
                <span>Obligations (optional)</span>
                <input
                  aria-label="Obligations"
                  list="evidence-obligation-options"
                  value={form.obligationIds}
                  onChange={(event) => updateField("obligationIds", event.target.value)}
                  placeholder="Leave blank or enter an obligation ID"
                />
                <datalist id="evidence-obligation-options">
                  {obligationItems.map((item) => (
                    <option key={`${item.contractClauseId}-${item.obligationId}`} value={item.obligationId}>
                      {item.clauseNumber} · {item.title}
                    </option>
                  ))}
                </datalist>
              </label>
              <label>
                <span>Controls</span>
                <input
                  list="evidence-control-options"
                  value={form.controlIds}
                  onChange={(event) => updateField("controlIds", event.target.value)}
                  placeholder="Select from suggestions or leave blank"
                />
                <datalist id="evidence-control-options">
                  {controls.map((control) => (
                    <option key={control.controlId} value={control.controlId}>
                      {control.controlId} · {control.title}
                    </option>
                  ))}
                </datalist>
              </label>
              <label>
                <span>Classification</span>
                <select value={form.classification} onChange={(event) => updateField("classification", event.target.value)}>
                  <option value="Unclassified">Unclassified</option>
                  <option value="Fci">FCI</option>
                  <option value="Cui">CUI</option>
                  <option value="SyntheticCui">Synthetic CUI</option>
                  <option value="Unknown">Unknown</option>
                  <option value="Prohibited">Prohibited</option>
                </select>
              </label>
              <label>
                <span>Classification reason</span>
                <input value={form.classificationReason} onChange={(event) => updateField("classificationReason", event.target.value)} />
              </label>
              <label className="span-2">
                <span>Description</span>
                <textarea value={form.description} onChange={(event) => updateField("description", event.target.value)} />
              </label>
            </div>
          </fieldset>
          <div className="form-actions">
            <button type="submit" disabled={!canManageEvidence || status === "saving" || Boolean(evidenceDateError)}>
              {selectedEvidence ? "Update metadata" : "Create metadata"}
            </button>
            <button type="button" onClick={reclassify} disabled={!selectedEvidence || !canManageEvidence || status === "saving"}>
              Review classification
            </button>
          </div>
          {status === "failed" ? (
            <Alert title="Evidence metadata action failed" tone="danger">
              {message || "The evidence metadata record was not saved."}
            </Alert>
          ) : message ? (
            <p className="form-status form-status--ok">{message}</p>
          ) : null}
        </form>
      </div>
    </section>
  );
}

function evidenceToMetadataForm(evidence: EvidenceMetadata | null): EvidenceMetadataFormState {
  if (!evidence) {
    return defaultEvidenceMetadataForm;
  }

  return {
    title: evidence.title,
    type: evidence.type,
    ownerFunction: evidence.ownerFunction,
    status: evidence.status,
    effectiveAt: evidence.effectiveAt ?? "",
    expiresAt: evidence.expiresAt ?? "",
    tags: evidence.tags.join(", "),
    obligationIds: evidence.obligationIds.join(", "),
    controlIds: evidence.controlIds.join(", "),
    classification: evidence.classification.classification,
    classificationReason: evidence.classification.reason ?? "Reviewer confirmed evidence classification.",
    description: evidence.description
  };
}

function evidenceMetadataFormToRequest(
  form: EvidenceMetadataFormState,
  evidence: EvidenceMetadata | null
): UpsertEvidenceMetadataRequest {
  return {
    title: form.title.trim(),
    type: form.type,
    ownerFunction: form.ownerFunction.trim(),
    status: form.status,
    effectiveAt: form.effectiveAt || null,
    expiresAt: form.expiresAt || null,
    tags: splitEvidenceList(form.tags),
    description: form.description.trim(),
    obligationIds: splitEvidenceList(form.obligationIds),
    controlIds: splitEvidenceList(form.controlIds),
    contractIds: evidence?.contractIds ?? [],
    vendorIds: evidence?.vendorIds ?? [],
    subcontractorIds: evidence?.subcontractorIds ?? [],
    employeeIds: evidence?.employeeIds ?? [],
    reportIds: evidence?.reportIds ?? [],
    classification: {
      classification: form.classification,
      source: "UserSelected",
      confidence: null,
      reviewedByUserId: null,
      reviewedAt: null,
      reason: form.classificationReason,
      isApprovedDemoContent: false
    }
  };
}

function splitEvidenceList(value: string): string[] {
  return value
    .split(",")
    .map((item) => item.trim())
    .filter(Boolean);
}

function CuiReadyChecklistPanel({
  checklists,
  currentTenant,
  currentUserId,
  matrix,
  message,
  onCreate,
  onItemUpdate,
  onReview,
  status
}: {
  checklists: CuiReadyApprovalChecklist[];
  currentTenant: Tenant | null;
  currentUserId: string | null;
  matrix: SharedResponsibilityMatrix | null;
  message: string;
  onCreate: () => Promise<void>;
  onItemUpdate: (checklistId: string, itemKey: string, request: UpdateCuiReadyChecklistItemRequest) => Promise<void>;
  onReview: (checklistId: string, action: "submit" | "approve" | "reject" | "supersede", reason: string | null) => Promise<void>;
  status: "idle" | "saving" | "saved" | "failed";
}) {
  const [reviewReason, setReviewReason] = useState("Approved for CUI-ready mode.");
  const latest = checklists[0] ?? null;
  const completedCount = latest?.items.filter((item) => item.status === "Complete").length ?? 0;

  function completeItem(checklistId: string, item: CuiReadyApprovalChecklistItem) {
    void onItemUpdate(checklistId, item.itemKey, {
      status: "Complete",
      owner: item.owner ?? "Security",
      evidenceLink: item.evidenceLink ?? "https://example.invalid/evidence/cui-ready",
      reviewerUserId: item.reviewerUserId ?? currentUserId,
      reviewedAt: item.reviewedAt ?? new Date().toISOString().slice(0, 10),
      notes: item.notes
    });
  }

  return (
    <section className="members-section" aria-label="CUI-ready approval checklist">
      <div className="section-heading section-heading--split">
        <div>
          <p className="eyebrow">CUI-ready approval</p>
          <h2>Approval checklist</h2>
          <p className="section-summary">Required readiness records must be complete and approved before enabling CUI-ready mode.</p>
        </div>
        <button type="button" onClick={() => void onCreate()} disabled={!currentTenant || status === "saving"}>
          <ClipboardCheck size={16} />
          <span>New checklist</span>
        </button>
      </div>
      {message ? (
        <p className={`form-status ${status === "failed" ? "form-status--error" : "form-status--ok"}`}>{message}</p>
      ) : null}
      {latest ? (
        <div className="approval-checklist">
          <div className="section-heading--split">
            <div>
              <h3>Version {latest.version}</h3>
              <p>{completedCount} of {latest.items.length} items complete</p>
            </div>
            <span className={`status status--${latest.state.toLowerCase()}`}>{latest.state}</span>
          </div>
          {matrix ? (
            <p className="section-summary">
              Shared responsibility matrix {matrix.version} · {matrix.state} · {matrix.reviewOwner}
            </p>
          ) : null}
          <div className="evidence-list">
            {latest.items.map((item) => (
              <article className="evidence-list__item" key={item.id}>
                <strong>{item.section}</strong>
                <span>{item.description}</span>
                <span>
                  Status: {item.status} · Owner: {item.owner ?? "No owner"} · Review date: {item.reviewedAt ?? "No review date"}
                </span>
                <button type="button" onClick={() => completeItem(latest.id, item)} disabled={status === "saving"}>
                  Mark complete
                </button>
              </article>
            ))}
          </div>
          <form className="invite-form" onSubmit={(event) => event.preventDefault()}>
            <label>
              <span>Review reason</span>
              <input value={reviewReason} onChange={(event) => setReviewReason(event.target.value)} />
            </label>
            <button type="button" onClick={() => void onReview(latest.id, "submit", null)} disabled={status === "saving"}>
              Submit
            </button>
            <button type="button" onClick={() => void onReview(latest.id, "approve", reviewReason)} disabled={status === "saving"}>
              Approve
            </button>
            <button type="button" onClick={() => void onReview(latest.id, "reject", reviewReason)} disabled={status === "saving"}>
              Reject
            </button>
            <button type="button" onClick={() => void onReview(latest.id, "supersede", reviewReason)} disabled={status === "saving"}>
              Supersede
            </button>
          </form>
          {latest.state === "Approved" && !isDemoCaptureMode() ? (
            <p className="form-status form-status--ok">Approved checklist ID: {latest.id}</p>
          ) : null}
          {latest.rejectionReason ? <p className="form-status form-status--error">{latest.rejectionReason}</p> : null}
        </div>
      ) : (
        <EmptyState title="No CUI-ready checklist" body="Create a checklist before requesting CUI-ready tenant mode." />
      )}
    </section>
  );
}

function SharedResponsibilityMatrixPanel({
  acknowledgements,
  matrix,
  message,
  onAcknowledge,
  status
}: {
  acknowledgements: SharedResponsibilityMatrixAcknowledgement[];
  matrix: SharedResponsibilityMatrix | null;
  message: string;
  onAcknowledge: () => Promise<void>;
  status: "idle" | "saving" | "saved" | "failed";
}) {
  const currentAcknowledgement = matrix
    ? acknowledgements.find(
        (acknowledgement) =>
          acknowledgement.matrixId === matrix.matrixId &&
          acknowledgement.matrixVersion === matrix.version &&
          acknowledgement.status === "Current"
      )
    : null;

  return (
    <section className="members-section" aria-label="Shared responsibility matrix">
      <div className="section-heading section-heading--split">
        <div>
          <p className="eyebrow">Shared responsibility baseline</p>
          <h2>Shared responsibility matrix</h2>
          <p className="section-summary">
            Published ownership baseline for platform controls, customer decisions, support obligations, and third-party dependencies.
          </p>
        </div>
        <div className="button-row">
          {matrix ? <span className={`status status--${matrix.state.toLowerCase()}`}>{matrix.state}</span> : null}
          <button
            type="button"
            onClick={() => void onAcknowledge()}
            disabled={!matrix || Boolean(currentAcknowledgement) || status === "saving"}
          >
            <CheckCircle2 size={16} />
            <span>{status === "saving" ? "Saving" : currentAcknowledgement ? "Acknowledged" : "Acknowledge"}</span>
          </button>
        </div>
      </div>
      {message ? (
        <p className={`form-status ${status === "failed" ? "form-status--error" : "form-status--ok"}`}>{message}</p>
      ) : null}
      {matrix ? (
        <>
          <div className="metric-grid">
            <div className="metric-card">
              <span>Version: </span>
              <strong>{matrix.version}</strong>
            </div>
            <div className="metric-card">
              <span>Effective: </span>
              <strong>{matrix.effectiveAt}</strong>
            </div>
            <div className="metric-card">
              <span>Review owner: </span>
              <strong>{matrix.reviewOwner}</strong>
            </div>
            <div className="metric-card">
              <span>Matrix acknowledgement status: </span>
              <strong>{currentAcknowledgement ? "Current" : "Required"}</strong>
            </div>
            <div className="metric-card">
              <span>Rows: </span>
              <strong>{matrix.rows.length}</strong>
            </div>
          </div>
          <div className="table-section">
            <div className="table-section__header">
              <h3>Responsibility rows</h3>
              <p>Control ownership and review accountability for the active shared responsibility baseline.</p>
            </div>
            <div className="member-table member-table--matrix" role="table" aria-label="Shared responsibility matrix rows">
              <div className="member-row member-row--header" role="row">
                <span role="columnheader">Category</span>
                <span role="columnheader">Owner</span>
                <span role="columnheader">Notes</span>
                <span role="columnheader">Review</span>
              </div>
              {matrix.rows.map((row) => (
                <article className="member-row" role="row" key={row.category}>
                  <span role="cell">{formatResponsibilityCategory(row.category)}</span>
                  <span role="cell">{row.responsibility}</span>
                  <span role="cell">{row.notes}</span>
                  <span role="cell">{row.reviewOwner} · {row.effectiveAt}</span>
                </article>
              ))}
            </div>
          </div>
          {acknowledgements.length > 0 ? (
            <div className="table-section">
              <div className="table-section__header">
                <h3>Acknowledgement history</h3>
                <p>Users who acknowledged the published matrix version for this tenant.</p>
              </div>
              <div className="member-table member-table--acknowledgements" role="table" aria-label="Shared responsibility matrix acknowledgement history">
                <div className="member-row member-row--header" role="row">
                  <span role="columnheader">Version</span>
                  <span role="columnheader">Status</span>
                  <span role="columnheader">Acknowledged</span>
                  <span role="columnheader">User</span>
                </div>
                {acknowledgements.map((acknowledgement) => (
                  <article className="member-row" role="row" key={acknowledgement.id}>
                    <span role="cell">{acknowledgement.matrixVersion}</span>
                    <span role="cell">{acknowledgement.status}</span>
                    <span role="cell">{formatUsDateTime(acknowledgement.acknowledgedAt)}</span>
                    <span role="cell">{acknowledgement.acknowledgedByUserId}</span>
                  </article>
                ))}
              </div>
            </div>
          ) : (
            <p className="form-status form-status--error">Current matrix acknowledgement is required before CUI-ready approval.</p>
          )}
          {currentAcknowledgement ? (
            <p className="form-status form-status--ok">Matrix acknowledgement status is Current.</p>
          ) : null}
        </>
      ) : (
        <EmptyState title="No published matrix" body="Publish a governed shared responsibility matrix before CUI-ready approval review." />
      )}
    </section>
  );
}

function formatResponsibilityCategory(value: string): string {
  return value
    .split("-")
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

function TenantModePanel({
  currentTenant,
  history,
  message,
  onUpdate,
  status
}: {
  currentTenant: Tenant | null;
  history: TenantDataHandlingModeHistory[];
  message: string;
  onUpdate: (request: UpdateTenantDataHandlingModeRequest) => Promise<void>;
  status: "idle" | "saving" | "saved" | "failed";
}) {
  const currentMode = currentTenant?.dataHandlingMode ?? "NoCui";
  const [selectedMode, setSelectedMode] = useState("");
  const [reason, setReason] = useState("");
  const [approvalRecordReference, setApprovalRecordReference] = useState("");
  const mode = selectedMode || currentMode;

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onUpdate({
      dataHandlingMode: mode,
      reason: reason.trim(),
      approvalRecordReference: approvalRecordReference.trim() || null
    });
  }

  return (
    <section className="members-section" aria-label="Tenant data handling mode">
      <div className="section-heading section-heading--split">
        <div>
          <p className="eyebrow">Active tenant mode</p>
          <h2>Data handling mode</h2>
          <p className="section-summary">
            The active tenant mode is the server-side source of truth for upload, evidence, report, note, and extraction controls.
          </p>
        </div>
        <span className={`status status--${(currentTenant?.dataHandlingMode ?? "unknown").toLowerCase()}`}>
          {currentTenant?.dataHandlingMode ?? "Unknown"}
        </span>
      </div>
      <div className="metric-grid">
        <div className="metric-card">
          <span>Active tenant: </span>
          <strong>{currentTenant?.displayName ?? "Not loaded"}</strong>
        </div>
        {!isDemoCaptureMode() ? (
          <div className="metric-card">
            <span>Tenant ID: </span>
            <strong>{currentTenant?.id ?? "Not loaded"}</strong>
          </div>
        ) : null}
        <div className="metric-card">
          <span>Current mode: </span>
          <strong>{currentTenant?.dataHandlingMode ?? "Unknown"}</strong>
        </div>
      </div>
      <form className="invite-form" onSubmit={submit}>
        <label>
          <span>Mode</span>
          <select value={mode} onChange={(event) => setSelectedMode(event.target.value)}>
            <option value="DemoSandbox">DemoSandbox</option>
            <option value="NoCui">NoCui</option>
            <option value="CuiReady">CuiReady</option>
          </select>
        </label>
        <label>
          <span>Reason for mode change</span>
          <input
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            required
            maxLength={600}
            placeholder={
              isDemoCaptureMode()
                ? "Document the approved reason for this mode change."
                : "Example: UAT CUI-ready gate validation after approved checklist."
            }
          />
        </label>
        <label>
          <span>Approval checklist ID</span>
          <input
            value={approvalRecordReference}
            onChange={(event) => setApprovalRecordReference(event.target.value)}
            maxLength={160}
            placeholder={mode === "CuiReady" ? "Paste the approved checklist ID" : "Not required unless switching to CuiReady"}
            required={mode === "CuiReady"}
          />
        </label>
        <button type="submit" disabled={!currentTenant || status === "saving"}>
          <ShieldCheck size={16} />
          <span>{status === "saving" ? "Saving" : "Update mode"}</span>
        </button>
      </form>
      {!currentTenant ? (
        <p className="form-status form-status--error">
          Tenant context has not loaded yet. The mode cannot be updated until the active tenant is available.
        </p>
      ) : null}
      {message ? (
        <p className={`form-status ${status === "failed" ? "form-status--error" : "form-status--ok"}`}>{message}</p>
      ) : null}
      {history.length > 0 ? (
        <div className="table-section">
          <div className="table-section__header">
            <h3>Mode change history</h3>
            <p>Server-recorded tenant mode changes used to verify upload and report posture changes.</p>
          </div>
          <div className="member-table member-table--mode-history" role="table" aria-label="Tenant data handling mode history">
            <div className="member-row member-row--header" role="row">
              <span role="columnheader">Changed</span>
              <span role="columnheader">Previous</span>
              <span role="columnheader">New</span>
              <span role="columnheader">Reason</span>
            </div>
            {history.map((entry) => (
              <article className="member-row" role="row" key={entry.id}>
                <span role="cell">{formatUsDateTime(entry.changedAt)}</span>
                <span role="cell">{entry.previousMode ?? "Initial"}</span>
                <span role="cell">{entry.newMode}</span>
                <span role="cell">{entry.reason}</span>
              </article>
            ))}
          </div>
        </div>
      ) : (
        <EmptyState title="No mode history yet" body="Tenant mode changes will appear here after the first recorded event." />
      )}
    </section>
  );
}

function DemoSandboxSeedPanel({
  canSeedDemoDataset,
  currentTenant,
  message,
  onSeed,
  status
}: {
  canSeedDemoDataset: boolean;
  currentTenant: Tenant | null;
  message: string;
  onSeed: () => Promise<void>;
  status: "idle" | "saving" | "saved" | "failed";
}) {
  const isDemoSandbox = currentTenant?.dataHandlingMode === "DemoSandbox";

  return (
    <section className="members-section" aria-label="Demo sandbox seed">
      <div className="section-heading section-heading--split">
        <div>
          <p className="eyebrow">Synthetic dataset</p>
          <h2>Demo sandbox seed</h2>
          <p className="section-summary">
            Load the approved synthetic CUI demo records for UAT. This action is available only when the active tenant mode is DemoSandbox.
          </p>
        </div>
        <button type="button" onClick={() => void onSeed()} disabled={!isDemoSandbox || !canSeedDemoDataset || status === "saving"}>
          <FolderKanban size={16} />
          <span>{status === "saving" ? "Seeding" : "Seed synthetic data"}</span>
        </button>
      </div>
      <div className="metric-grid">
        <div className="metric-card">
          <span>Required mode: </span>
          <strong>DemoSandbox</strong>
        </div>
        <div className="metric-card">
          <span>Dataset version: </span>
          <strong>2026.06.phase1a</strong>
        </div>
        <div className="metric-card">
          <span>Classification: </span>
          <strong>SyntheticCui</strong>
        </div>
      </div>
      {!isDemoSandbox ? (
        <p className="form-status form-status--error">Switch Data handling mode to DemoSandbox before seeding synthetic demo data.</p>
      ) : null}
      {isDemoSandbox && !canSeedDemoDataset ? (
        <p className="form-status form-status--error">Seed synthetic data requires obligation-management permission.</p>
      ) : null}
      {message ? (
        <p className={`form-status ${status === "failed" ? "form-status--error" : "form-status--ok"}`}>{message}</p>
      ) : null}
    </section>
  );
}

function InvitationListItem({
  invitation,
  isBusy,
  isRevoking,
  onRevoke
}: {
  invitation: TenantInvitation;
  isBusy: boolean;
  isRevoking: boolean;
  onRevoke: (invitationId: string, request: RevokeTenantInvitationRequest) => Promise<boolean>;
}) {
  const [isConfirming, setIsConfirming] = useState(false);
  const [reason, setReason] = useState("");
  const invitationLabel = isDemoCaptureMode() ? "Pending invitation" : invitation.email;

  async function handleRevoke(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const succeeded = await onRevoke(invitation.invitationId, { reason });
    if (succeeded) {
      setIsConfirming(false);
      setReason("");
    }
  }

  return (
    <article className="invitation-item">
      <div className="invitation-item__main">
        <span className="icon-box icon-box--small" aria-hidden="true">
          <UserPlus size={17} />
        </span>
        <span>
          <strong>{invitationLabel}</strong>
          <small>{invitation.roleName}</small>
        </span>
      </div>
      <div className="invitation-item__status">
        <span className={`status status--${invitation.status.toLowerCase()}`}>{invitation.status}</span>
        {invitation.status === "Pending" ? (
          <Button
            aria-label={`Revoke invitation for ${invitationLabel}`}
            disabled={isBusy}
            icon={<Ban size={15} />}
            onClick={() => setIsConfirming(true)}
            size="sm"
            variant="danger"
          >
            Revoke
          </Button>
        ) : null}
      </div>
      <span className="invitation-date">Expires {formatUsDateOnly(invitation.expiresAt)}</span>
      {!isDemoCaptureMode() ? <small className="notification-placeholder">{invitation.notificationPlaceholder}</small> : null}
      {isConfirming ? (
        <form className="invitation-revoke-form" onSubmit={handleRevoke}>
          <label>
            <span>Revocation reason</span>
            <textarea
              autoFocus
              maxLength={500}
              onChange={(event) => setReason(event.target.value)}
              required
              rows={2}
              value={reason}
            />
          </label>
          <div className="invitation-revoke-form__actions">
            <Button disabled={isBusy} type="submit" variant="danger">
              {isRevoking ? "Revoking" : "Confirm revoke"}
            </Button>
            <Button disabled={isBusy} onClick={() => setIsConfirming(false)} type="button" variant="ghost">
              Cancel
            </Button>
          </div>
        </form>
      ) : null}
    </article>
  );
}

function SettingsView({
  auditLogEntityTypes,
  auditLogEntityTypeStatus,
  auditLogFilters,
  auditLogStatus,
  auditLogs,
  cmmcAssessments,
  cmmcPoamItems,
  evidenceItems,
  canManageTenant,
  canSeedDemoDataset,
  canManageUsers,
  canViewAuditLog,
  currentTenant,
  currentUserId,
  demoSeedMessage,
  demoSeedStatus,
  cuiReadyChecklists,
  cuiReadyChecklistMessage,
  cuiReadyChecklistStatus,
  inviteEmail,
  inviteMessage,
  inviteRole,
  inviteStatus,
  invitationActionMessage,
  invitationActionStatus,
  invitations,
  members,
  revokingInvitationId,
  notificationPreference,
  notificationPreferenceMessage,
  notificationPreferenceStatus,
  reminderRunResult,
  sharedResponsibilityMatrix,
  sharedResponsibilityMatrixAcknowledgements,
  sharedResponsibilityMatrixAcknowledgementMessage,
  sharedResponsibilityMatrixAcknowledgementStatus,
  tenantModeHistory,
  tenantModeMessage,
  tenantModeStatus,
  onAuditLogFilterChange,
  onAuditLogFilterSubmit,
  onAuditLogPageChange,
  onCuiReadyChecklistCreate,
  onCuiReadyChecklistItemUpdate,
  onCuiReadyChecklistReview,
  onDemoTenantSeed,
  onDueDateReminderRun,
  onInviteEmailChange,
  onInviteRoleChange,
  onInvitationSubmit,
  onInvitationRevoke,
  onNotificationPreferenceSave,
  onSharedResponsibilityMatrixAcknowledge,
  onTenantModeUpdate
}: {
  auditLogEntityTypes: string[];
  auditLogEntityTypeStatus: "idle" | "ready" | "failed";
  auditLogFilters: AuditLogFilters;
  auditLogStatus: "idle" | "loading" | "ready" | "failed";
  auditLogs: PagedResult<AuditLogEntry>;
  cmmcAssessments: CmmcAssessment[];
  cmmcPoamItems: CmmcPoamItem[];
  evidenceItems: EvidenceMetadata[];
  canManageTenant: boolean;
  canSeedDemoDataset: boolean;
  canManageUsers: boolean;
  canViewAuditLog: boolean;
  currentTenant: Tenant | null;
  currentUserId: string | null;
  demoSeedMessage: string;
  demoSeedStatus: "idle" | "saving" | "saved" | "failed";
  cuiReadyChecklists: CuiReadyApprovalChecklist[];
  cuiReadyChecklistMessage: string;
  cuiReadyChecklistStatus: "idle" | "saving" | "saved" | "failed";
  inviteEmail: string;
  inviteMessage: string;
  inviteRole: string;
  inviteStatus: "idle" | "sending" | "created" | "failed";
  invitationActionMessage: string;
  invitationActionStatus: "idle" | "revoking" | "succeeded" | "failed";
  invitations: TenantInvitation[];
  members: TenantMember[];
  revokingInvitationId: string | null;
  notificationPreference: NotificationPreference | null;
  notificationPreferenceMessage: string;
  notificationPreferenceStatus: "idle" | "saving" | "saved" | "failed";
  reminderRunResult: DueDateReminderRunResult | null;
  sharedResponsibilityMatrix: SharedResponsibilityMatrix | null;
  sharedResponsibilityMatrixAcknowledgements: SharedResponsibilityMatrixAcknowledgement[];
  sharedResponsibilityMatrixAcknowledgementMessage: string;
  sharedResponsibilityMatrixAcknowledgementStatus: "idle" | "saving" | "saved" | "failed";
  tenantModeHistory: TenantDataHandlingModeHistory[];
  tenantModeMessage: string;
  tenantModeStatus: "idle" | "saving" | "saved" | "failed";
  onAuditLogFilterChange: (filters: AuditLogFilters) => void;
  onAuditLogFilterSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onAuditLogPageChange: (page: number) => void;
  onCuiReadyChecklistCreate: () => Promise<void>;
  onCuiReadyChecklistItemUpdate: (
    checklistId: string,
    itemKey: string,
    request: UpdateCuiReadyChecklistItemRequest
  ) => Promise<void>;
  onCuiReadyChecklistReview: (
    checklistId: string,
    action: "submit" | "approve" | "reject" | "supersede",
    reason: string | null
  ) => Promise<void>;
  onDemoTenantSeed: () => Promise<void>;
  onDueDateReminderRun: (leadTimeDays: number) => Promise<void>;
  onInviteEmailChange: (email: string) => void;
  onInviteRoleChange: (roleName: string) => void;
  onInvitationSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onInvitationRevoke: (
    invitationId: string,
    request: RevokeTenantInvitationRequest
  ) => Promise<boolean>;
  onNotificationPreferenceSave: (request: NotificationPreferenceUpdateRequest) => Promise<void>;
  onSharedResponsibilityMatrixAcknowledge: () => Promise<void>;
  onTenantModeUpdate: (request: UpdateTenantDataHandlingModeRequest) => Promise<void>;
}) {
  const currentAuditEntityLabels = useMemo(
    () =>
      new Map<string, string>([
        ...cmmcAssessments.map((assessment) => [`CmmcAssessment:${assessment.id}`, assessment.name] as const),
        ...cmmcPoamItems.map((item) => [`CmmcPoamItem:${item.id}`, item.weakness] as const),
        ...evidenceItems.map((evidence) => [`EvidenceItem:${evidence.id}`, evidence.title] as const)
      ]),
    [cmmcAssessments, cmmcPoamItems, evidenceItems]
  );

  if (!canManageTenant && !canManageUsers && !canViewAuditLog && !notificationPreference) {
    return (
      <section className="route-panel">
        <EmptyState
          title="Settings are restricted"
          body="Tenant settings require user-management or tenant-administration permissions."
        />
      </section>
    );
  }

  return (
    <>
      {canManageTenant ? (
        <>
          <TenantModePanel
            currentTenant={currentTenant}
            history={tenantModeHistory}
            message={tenantModeMessage}
            onUpdate={onTenantModeUpdate}
            status={tenantModeStatus}
          />
          {!isDemoCaptureMode() ? (
            <DemoSandboxSeedPanel
              canSeedDemoDataset={canSeedDemoDataset}
              currentTenant={currentTenant}
              message={demoSeedMessage}
              onSeed={onDemoTenantSeed}
              status={demoSeedStatus}
            />
          ) : null}
          <SharedResponsibilityMatrixPanel
            acknowledgements={sharedResponsibilityMatrixAcknowledgements}
            matrix={sharedResponsibilityMatrix}
            message={sharedResponsibilityMatrixAcknowledgementMessage}
            onAcknowledge={onSharedResponsibilityMatrixAcknowledge}
            status={sharedResponsibilityMatrixAcknowledgementStatus}
          />
          <CuiReadyChecklistPanel
            checklists={cuiReadyChecklists}
            currentTenant={currentTenant}
            currentUserId={currentUserId}
            matrix={sharedResponsibilityMatrix}
            message={cuiReadyChecklistMessage}
            onCreate={onCuiReadyChecklistCreate}
            onItemUpdate={onCuiReadyChecklistItemUpdate}
            onReview={onCuiReadyChecklistReview}
            status={cuiReadyChecklistStatus}
          />
        </>
      ) : null}
      <NotificationPreferencesPanel
        message={notificationPreferenceMessage}
        onReminderRun={onDueDateReminderRun}
        onSave={onNotificationPreferenceSave}
        preference={notificationPreference}
        reminderRunResult={reminderRunResult}
        status={notificationPreferenceStatus}
      />
      {canManageUsers ? (
        <>
          <section className="members-section" aria-label="Tenant team members">
            <div className="section-heading">
              <p className="eyebrow">Tenant access</p>
              <h2>Team members</h2>
              <p className="section-summary">
                Roles connect each person to the FeDril business goal: know what applies, assign the work, collect evidence,
                and keep the tenant ready for reviews without giving more access than needed.
              </p>
            </div>
            {members.length > 0 ? (
              <div className="table-section">
                <div className="table-section__header">
                  <h3>Current members</h3>
                  <p>People with active tenant access and their assigned role posture.</p>
                </div>
                <div className="member-table member-table--members" role="table" aria-label="Current tenant members">
                  <div className="member-row member-row--header" role="row">
                    <span role="columnheader">Member</span>
                    <span role="columnheader">Role</span>
                    <span role="columnheader">Status</span>
                    <span role="columnheader">MFA</span>
                  </div>
                  {members.map((member) => (
                    <article className="member-row" role="row" key={member.membershipId}>
                      <span className="member-person" role="cell">
                        <span className="icon-box icon-box--small" aria-hidden="true">
                          <UsersRound size={17} />
                        </span>
                        <span>
                          <strong>{member.displayName}</strong>
                          {!isDemoCaptureMode() ? <small>{member.email}</small> : null}
                        </span>
                      </span>
                      <span role="cell">{member.roleName}</span>
                      <span role="cell">
                        <span className={`status status--${member.membershipStatus.toLowerCase()}`}>
                          {member.membershipStatus}
                        </span>
                      </span>
                      <span role="cell">{member.mfaEnabled ? "Enabled" : "Not enabled"}</span>
                    </article>
                  ))}
                </div>
              </div>
            ) : (
              <EmptyState title="No tenant members available" body="Team membership is loaded from the active tenant context." />
            )}
          </section>

          <section className="invitation-section" aria-label="Tenant invitations">
            <div className="section-heading section-heading--split">
              <div>
                <p className="eyebrow">Controlled onboarding</p>
                <h2>User invitations</h2>
              </div>
              <form className="invite-form" onSubmit={onInvitationSubmit}>
                <label>
                  <span>Email</span>
                  <input
                    type="email"
                    value={inviteEmail}
                    onChange={(event) => onInviteEmailChange(event.target.value)}
                    required
                    maxLength={320}
                  />
                </label>
                <label>
                  <span>Role</span>
                  <select value={inviteRole} onChange={(event) => onInviteRoleChange(event.target.value)}>
                    <option>Admin</option>
                    <option>Compliance Manager</option>
                    <option>Contributor</option>
                    <option>Auditor</option>
                    <option>Advisor</option>
                  </select>
                </label>
                <button type="submit" disabled={inviteStatus === "sending"}>
                  {inviteStatus === "sending" ? <Send size={16} /> : <UserPlus size={16} />}
                  <span>{inviteStatus === "sending" ? "Sending" : "Invite"}</span>
                </button>
              </form>
            </div>
            <div className="role-guidance" aria-label="Role guidance">
              {roleGuidance.map((item) => (
                <article className="role-guidance__item" key={item.role}>
                  <div>
                    <h3>{item.role}</h3>
                    <p>{item.persona}</p>
                  </div>
                  <span>{item.purpose}</span>
                </article>
              ))}
            </div>
            {inviteMessage ? (
              <p className={`form-status ${inviteStatus === "failed" ? "form-status--error" : "form-status--ok"}`}>
                {inviteMessage}
              </p>
            ) : null}
            {invitationActionMessage ? (
              <p
                className={`form-status ${
                  invitationActionStatus === "failed" ? "form-status--error" : "form-status--ok"
                }`}
              >
                {invitationActionMessage}
              </p>
            ) : null}
            {invitations.length > 0 ? (
              <div className="invitation-list">
                {invitations.map((invitation) => (
                  <InvitationListItem
                    invitation={invitation}
                    isBusy={invitationActionStatus === "revoking"}
                    isRevoking={revokingInvitationId === invitation.invitationId}
                    key={invitation.invitationId}
                    onRevoke={onInvitationRevoke}
                  />
                ))}
              </div>
            ) : (
              <EmptyState title="No invitations available" body="Invitation state is loaded from the active tenant context." />
            )}
          </section>
        </>
      ) : null}

      {canViewAuditLog ? (
        <section className="members-section" aria-label="Audit log viewer">
          <div className="section-heading section-heading--split audit-log-heading">
            <div>
              <p className="eyebrow">Audit trail</p>
              <h2>Audit log</h2>
              <p className="section-summary">
                Filter by Action and Entity, then verify the expected text in the results table Summary column. From and To are optional date filters.
              </p>
            </div>
            <form className="audit-filter-form" onSubmit={onAuditLogFilterSubmit}>
              {!isDemoCaptureMode() ? (
                <label>
                  <span>Actor ID</span>
                  <input
                    value={auditLogFilters.actorUserId}
                    onChange={(event) => onAuditLogFilterChange({ ...auditLogFilters, actorUserId: event.target.value })}
                  />
                </label>
              ) : null}
              <label>
                <span>Action</span>
                <select
                  value={auditLogFilters.action}
                  onChange={(event) => onAuditLogFilterChange({ ...auditLogFilters, action: event.target.value })}
                >
                  <option value="">Any</option>
                  <option value="Created">Created</option>
                  <option value="Viewed">Viewed</option>
                  <option value="Updated">Updated</option>
                  <option value="Deleted">Deleted</option>
                  <option value="Uploaded">Uploaded</option>
                  <option value="Downloaded">Downloaded</option>
                  <option value="Approved">Approved</option>
                  <option value="Rejected">Rejected</option>
                  <option value="Archived">Archived</option>
                  <option value="Expired">Expired</option>
                  <option value="Exported">Exported</option>
                  <option value="SignedIn">Signed in</option>
                  <option value="SignedOut">Signed out</option>
                  <option value="PermissionChanged">Permission changed</option>
                </select>
              </label>
              <label>
                <span>Entity</span>
                <select
                  value={auditLogFilters.entityType}
                  onChange={(event) => onAuditLogFilterChange({ ...auditLogFilters, entityType: event.target.value })}
                >
                  <option value="">Any</option>
                  {auditLogEntityTypes.map((entityType) => (
                    <option key={entityType} value={entityType}>
                      {entityType}
                    </option>
                  ))}
                </select>
                {auditLogEntityTypeStatus === "failed" ? (
                  <small role="status">Entity choices could not be loaded. Reload the workspace and try again.</small>
                ) : auditLogEntityTypeStatus === "ready" && auditLogEntityTypes.length === 0 ? (
                  <small>No entity types exist in this tenant's audit history yet.</small>
                ) : null}
              </label>
              <label>
                <span>From</span>
                <input
                  type="datetime-local"
                  value={auditLogFilters.from}
                  title={
                    isDemoCaptureMode()
                      ? "Optional date filter."
                      : "Optional. Leave blank for UAT-22 unless you need to narrow the date range."
                  }
                  onChange={(event) => onAuditLogFilterChange({ ...auditLogFilters, from: event.target.value })}
                />
              </label>
              <label>
                <span>To</span>
                <input
                  type="datetime-local"
                  value={auditLogFilters.to}
                  title={
                    isDemoCaptureMode()
                      ? "Optional date filter."
                      : "Optional. Leave blank for UAT-22 unless you need to narrow the date range."
                  }
                  onChange={(event) => onAuditLogFilterChange({ ...auditLogFilters, to: event.target.value })}
                />
              </label>
              <button type="submit" disabled={auditLogStatus === "loading"}>
                <SlidersHorizontal size={16} />
                <span>{auditLogStatus === "loading" ? "Filtering" : "Filter"}</span>
              </button>
            </form>
          </div>
          {auditLogs.items.length > 0 ? (
            <div className="table-section">
              <div className="table-section__header">
                <h3>Tenant audit events</h3>
                <p>Filtered audit records for compliance-relevant tenant activity.</p>
              </div>
              <div className="member-table member-table--audit-log" role="table" aria-label="Tenant audit logs">
                <div className="member-row member-row--header" role="row">
                  <span role="columnheader">Date</span>
                  <span role="columnheader">Actor</span>
                  <span role="columnheader">Action</span>
                  <span role="columnheader">Entity</span>
                  <span role="columnheader">Summary</span>
                </div>
                {auditLogs.items.map((entry) => {
                  const currentEntityLabel = currentAuditEntityLabels.get(`${entry.entityType}:${entry.entityId}`);

                  return (
                    <article className="member-row" data-testid="audit-row" role="row" key={entry.id}>
                      <span role="cell">{formatUsDateTime(entry.occurredAt)}</span>
                      <span role="cell">{isDemoCaptureMode() && entry.actorUserId ? "Tenant user" : (entry.actorUserId ?? "System")}</span>
                      <span role="cell">{entry.action}</span>
                      <span role="cell">{entry.entityType}</span>
                      <span role="cell">
                        {entry.summary}
                        {currentEntityLabel ? <small className="audit-current-entity-label">Current record: {currentEntityLabel}</small> : null}
                      </span>
                    </article>
                  );
                })}
              </div>
            </div>
          ) : (
            <EmptyState title="No audit events match" body="Audit events are tenant-scoped and filtered by the controls above." />
          )}
          <div className="form-status">
            Page {auditLogs.page} of {Math.max(1, Math.ceil(auditLogs.totalCount / Math.max(1, auditLogs.pageSize)))} · {auditLogs.totalCount} events
          </div>
          <div className="form-status">
            <button type="button" disabled={!auditLogs.hasPreviousPage} onClick={() => onAuditLogPageChange(auditLogs.page - 1)}>
              Previous
            </button>
            <button type="button" disabled={!auditLogs.hasNextPage} onClick={() => onAuditLogPageChange(auditLogs.page + 1)}>
              Next
            </button>
          </div>
        </section>
      ) : null}
    </>
  );
}

function EmptyState({ body, title }: { body: string; title: string }) {
  return (
    <div className="empty-state">
      <h3>{title}</h3>
      <p>{body}</p>
    </div>
  );
}

function ClassificationBadge({ classification }: { classification: string }) {
  return (
    <span className={`status status--${classification.toLowerCase()}`}>
      {classification === "SyntheticCui" ? "Synthetic demo data" : classification}
    </span>
  );
}
