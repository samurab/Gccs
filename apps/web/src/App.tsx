import {
  Archive,
  AlertTriangle,
  Building2,
  CalendarClock,
  CheckCircle2,
  ClipboardCheck,
  FileSearch,
  FolderKanban,
  GitBranch,
  Home,
  LayoutDashboard,
  ScrollText,
  Send,
  Settings,
  ShieldCheck,
  SlidersHorizontal,
  UploadCloud,
  UserPlus,
  UsersRound
} from "lucide-react";
import { type FormEvent, type ReactNode, useEffect, useMemo, useState } from "react";
import { ModuleCard } from "@/components/ModuleCard";
import {
  acknowledgeNoCuiNotice,
  assignContractObligationOwner,
  attachContractClause,
  createContract,
  createContractDeliverable,
  createContractDocument,
  createTenantInvitation,
  createEvidenceUploadIntent,
  createEvidenceMetadata,
  deleteContractDocument,
  fallbackAuditLogs,
  fallbackAccess,
  fallbackNoCuiAcknowledgementStatus,
  fallbackOverview,
  getCompanyProfile,
  getCalendarEvents,
  getContractClauses,
  getContractDeliverables,
  getContractDocuments,
  getContractObligationDetail,
  getContractObligations,
  getContracts,
  getAuditLogs,
  getComplianceOverview,
  getCurrentUserAccess,
  getEvidenceItems,
  getNoCuiAcknowledgementStatus,
  getTenantInvitations,
  getTenantMembers,
  saveCompanyProfile,
  searchClauseLibrary,
  removeContractClause,
  updateContractObligationStatus,
  updateContract,
  updateContractDeliverable,
  updateEvidenceMetadata,
  type AuditLogEntry,
  type ClauseLibraryItem,
  type ClauseSearchParams,
  type CalendarEvent,
  type CalendarEventQueryParams,
  type CompanyCertification,
  type CompanyProfile,
  type ComplianceOverview,
  type ContractClause,
  type ContractDeliverable,
  type ContractDocument,
  type ContractObligationDetail,
  type ContractObligationDashboardItem,
  type ContractObligationQueryParams,
  type ContractRecord,
  type CurrentUserAccess,
  type EvidenceMetadata,
  type NoCuiAcknowledgementStatus,
  type PagedResult,
  type TenantInvitation,
  type AttachContractClauseRequest,
  type UpsertContractDeliverableRequest,
  type UpsertContractRequest,
  type UpsertCompanyProfileRequest,
  type UpsertEvidenceMetadataRequest,
  type TenantMember
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

type AuditLogFilters = {
  actorUserId: string;
  action: string;
  entityType: string;
  from: string;
  to: string;
};

type CalendarFilters = {
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
  icon: typeof LayoutDashboard;
  permissions?: string[];
};

const navigationItems: NavigationItem[] = [
  {
    route: "dashboard",
    label: "Dashboard",
    description: "Workspace overview",
    icon: LayoutDashboard
  },
  {
    route: "profile",
    label: "Profile",
    description: "Company compliance profile",
    icon: Building2,
    permissions: ["ViewCompanyProfile", "ManageCompanyProfile"]
  },
  {
    route: "contracts",
    label: "Contracts",
    description: "Contract and clause intake",
    icon: FileSearch,
    permissions: ["ViewContracts", "ManageContracts"]
  },
  {
    route: "obligations",
    label: "Obligations",
    description: "Source-backed obligation matrix",
    icon: ClipboardCheck,
    permissions: ["ViewObligations", "ManageObligations"]
  },
  {
    route: "calendar",
    label: "Calendar",
    description: "Tasks, renewals, and reminders",
    icon: CalendarClock,
    permissions: ["ViewTasks", "ManageTasks"]
  },
  {
    route: "evidence",
    label: "Evidence",
    description: "No-CUI evidence vault",
    icon: Archive,
    permissions: ["ViewEvidence", "ManageEvidence", "ApproveEvidence"]
  },
  {
    route: "cmmc",
    label: "CMMC",
    description: "Readiness and control tracking",
    icon: ShieldCheck,
    permissions: ["ViewCmmc", "ManageCmmc"]
  },
  {
    route: "subcontractors",
    label: "Subcontractors",
    description: "Flow-down and supplier status",
    icon: GitBranch,
    permissions: ["ViewSubcontractors", "ManageSubcontractors"]
  },
  {
    route: "reports",
    label: "Reports",
    description: "Audit-ready exports",
    icon: ScrollText,
    permissions: ["ViewReports", "ManageReports"]
  },
  {
    route: "settings",
    label: "Settings",
    description: "Tenant access and workspace controls",
    icon: Settings,
    permissions: ["ManageTenant", "ManageUsers", "ViewAuditLog"]
  }
];

const moduleIcons = [Building2, FileSearch, ClipboardCheck, CalendarClock, Archive, ShieldCheck, GitBranch, FolderKanban];
const defaultAuditLogFilters: AuditLogFilters = {
  actorUserId: "",
  action: "",
  entityType: "",
  from: "",
  to: ""
};

const defaultCalendarFilters: CalendarFilters = {
  owner: "",
  status: "",
  risk: "",
  contractId: "",
  module: ""
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

const placeholderContent: Record<
  Exclude<WorkspaceRoute, "dashboard" | "settings" | "evidence">,
  { eyebrow: string; title: string; description: string; emptyTitle: string; emptyBody: string }
> = {
  profile: {
    eyebrow: "Company profile",
    title: "Company compliance profile",
    description: "Capture SAM, UEI, NAICS, certification, role, location, and data-posture details for the active tenant.",
    emptyTitle: "No company profile has been created yet",
    emptyBody: "The first profile workflow will collect entity details without allowing CUI uploads."
  },
  contracts: {
    eyebrow: "Contract intake",
    title: "Contracts and clauses",
    description: "Track solicitations, contracts, subcontracts, flow-down attachments, deadlines, and manual clause tagging.",
    emptyTitle: "No contracts have been added yet",
    emptyBody: "Add contract intake in the next workflow slice to start building obligation matrices."
  },
  obligations: {
    eyebrow: "Obligation matrix",
    title: "Source-backed obligations",
    description: "Map clauses and applicability rules to actions, owners, evidence, deadlines, source URLs, and review metadata.",
    emptyTitle: "No tenant-specific obligation matrix yet",
    emptyBody: "Seeded library obligations appear on the dashboard; contract-specific obligations will show here after intake."
  },
  calendar: {
    eyebrow: "Compliance calendar",
    title: "Tasks, renewals, and reminders",
    description: "Track SAM renewals, certifications, CMMC affirmations, evidence reviews, reports, and contract deliverables.",
    emptyTitle: "No calendar items yet",
    emptyBody: "Tasks and reminders will be created from obligations, controls, evidence expiration, and contract deadlines."
  },
  cmmc: {
    eyebrow: "CMMC readiness",
    title: "CMMC and NIST workspace",
    description: "Prepare Level 1 and Level 2 readiness tracking with draft-only guidance, control evidence, SSP, and POA&M planning.",
    emptyTitle: "No CMMC assessment has started yet",
    emptyBody: "Readiness views will preserve source traceability and SME review metadata before customer-facing use."
  },
  subcontractors: {
    eyebrow: "Flow-down tracking",
    title: "Subcontractor management",
    description: "Track subcontractor status, required flow-down clauses, CMMC posture, insurance, NDAs, and evidence requests.",
    emptyTitle: "No subcontractors have been added yet",
    emptyBody: "Subcontractor workflows will stay tenant-scoped and role-controlled as records are added."
  },
  reports: {
    eyebrow: "Reporting",
    title: "Reports and audit packages",
    description: "Generate obligation matrices, compliance status reports, CMMC readiness summaries, evidence packages, and audit trails.",
    emptyTitle: "No reports have been generated yet",
    emptyBody: "Reports will cite source-backed content and tenant evidence once workflow data exists."
  }
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

function emptyStringsToUndefined(filters: CalendarFilters): Omit<CalendarEventQueryParams, "from" | "to"> {
  return {
    owner: filters.owner || undefined,
    status: filters.status || undefined,
    risk: filters.risk || undefined,
    contractId: filters.contractId || undefined,
    module: filters.module || undefined
  };
}

export function App() {
  const [overview, setOverview] = useState(fallbackOverview);
  const [access, setAccess] = useState<CurrentUserAccess>(fallbackAccess);
  const [members, setMembers] = useState<TenantMember[]>([]);
  const [invitations, setInvitations] = useState<TenantInvitation[]>([]);
  const [companyProfile, setCompanyProfile] = useState<CompanyProfile | null>(null);
  const [contracts, setContracts] = useState<ContractRecord[]>([]);
  const [clauseResults, setClauseResults] = useState<ClauseLibraryItem[]>([]);
  const [obligationDashboardItems, setObligationDashboardItems] = useState<ContractObligationDashboardItem[]>([]);
  const [selectedObligationDetail, setSelectedObligationDetail] = useState<ContractObligationDetail | null>(null);
  const [contractClauses, setContractClauses] = useState<ContractClause[]>([]);
  const [contractDeliverables, setContractDeliverables] = useState<ContractDeliverable[]>([]);
  const [contractDocuments, setContractDocuments] = useState<ContractDocument[]>([]);
  const [calendarEvents, setCalendarEvents] = useState<CalendarEvent[]>([]);
  const [evidenceItems, setEvidenceItems] = useState<EvidenceMetadata[]>([]);
  const [selectedEvidenceItemId, setSelectedEvidenceItemId] = useState<string | null>(null);
  const [calendarFilters, setCalendarFilters] = useState<CalendarFilters>(defaultCalendarFilters);
  const [calendarStatus, setCalendarStatus] = useState<"idle" | "loading" | "ready" | "failed">("idle");
  const [calendarMessage, setCalendarMessage] = useState("");
  const [selectedContractId, setSelectedContractId] = useState<string | null>(null);
  const [auditLogs, setAuditLogs] = useState<PagedResult<AuditLogEntry>>(fallbackAuditLogs);
  const [auditLogFilters, setAuditLogFilters] = useState<AuditLogFilters>(defaultAuditLogFilters);
  const [auditLogStatus, setAuditLogStatus] = useState<"idle" | "loading" | "ready" | "failed">("idle");
  const [noCuiAcknowledgement, setNoCuiAcknowledgement] = useState<NoCuiAcknowledgementStatus>(
    fallbackNoCuiAcknowledgementStatus
  );
  const [activeRoute, setActiveRoute] = useState<WorkspaceRoute>(getInitialRoute);
  const [loadState, setLoadState] = useState<LoadState>("loading");
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteRole, setInviteRole] = useState("Contributor");
  const [inviteStatus, setInviteStatus] = useState<"idle" | "sending" | "created" | "failed">("idle");
  const [profileStatus, setProfileStatus] = useState<"idle" | "saving" | "saved" | "failed">("idle");
  const [profileMessage, setProfileMessage] = useState("");
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
  const [uploadStatus, setUploadStatus] = useState<"idle" | "creating" | "created" | "blocked">("idle");
  const [uploadMessage, setUploadMessage] = useState("");
  const [evidenceMetadataStatus, setEvidenceMetadataStatus] = useState<"idle" | "saving" | "saved" | "failed">("idle");
  const [evidenceMetadataMessage, setEvidenceMetadataMessage] = useState("");

  const visibleNavigation = useMemo(
    () => navigationItems.filter((item) => hasAnyPermission(access, item.permissions)),
    [access]
  );
  const canManageUsers = access.permissions.includes("ManageUsers");
  const canManageEvidence = access.permissions.includes("ManageEvidence");
  const canManageCompanyProfile = access.permissions.includes("ManageCompanyProfile");
  const canManageContracts = access.permissions.includes("ManageContracts");
  const canManageObligations = access.permissions.includes("ManageObligations");
  const canViewAuditLog = access.permissions.includes("ViewAuditLog");

  useEffect(() => {
    function handleHashChange() {
      const nextRoute = getInitialRoute();
      const nextItem = navigationItems.find((item) => item.route === nextRoute);

      if (!nextItem || !hasAnyPermission(access, nextItem.permissions)) {
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
    let isMounted = true;

    Promise.all([getComplianceOverview(), getCurrentUserAccess()])
      .then(async ([nextOverview, nextAccess]) => {
        const canLoadUserManagement = nextAccess.permissions.includes("ManageUsers");
        const canLoadAuditLogs = nextAccess.permissions.includes("ViewAuditLog");
        const canLoadNoCuiStatus = hasAnyPermission(nextAccess, ["ViewEvidence", "ManageEvidence"]);
        const canLoadCompanyProfile = hasAnyPermission(nextAccess, ["ViewCompanyProfile", "ManageCompanyProfile"]);
        const canLoadContracts = hasAnyPermission(nextAccess, ["ViewContracts", "ManageContracts"]);
        const canLoadObligations = hasAnyPermission(nextAccess, ["ViewObligations", "ManageObligations"]);
        const canLoadCalendar = hasAnyPermission(nextAccess, ["ViewTasks", "ManageTasks"]);
        const [nextMembers, nextInvitations] = canLoadUserManagement
          ? await Promise.all([getTenantMembers(), getTenantInvitations()])
          : [[], []];
        const nextAuditLogs = canLoadAuditLogs ? await getAuditLogs({ page: 1, pageSize: 5 }) : fallbackAuditLogs;
        const nextNoCuiAcknowledgement = canLoadNoCuiStatus
          ? await getNoCuiAcknowledgementStatus()
          : fallbackNoCuiAcknowledgementStatus;
        const nextEvidenceItems = canLoadNoCuiStatus ? await getEvidenceItems() : [];
        const nextCompanyProfile = canLoadCompanyProfile ? await getCompanyProfile() : null;
        const nextContracts = canLoadContracts ? await getContracts() : [];
        const nextObligationDashboardItems = canLoadObligations ? await getContractObligations() : [];
        const nextCalendarEvents = canLoadCalendar ? await getCalendarEvents(defaultCalendarQuery()) : [];
        const nextContractClauses = nextContracts[0] ? await getContractClauses(nextContracts[0].id) : [];
        const nextContractDeliverables = nextContracts[0] ? await getContractDeliverables(nextContracts[0].id) : [];
        const nextContractDocuments = nextContracts[0] ? await getContractDocuments(nextContracts[0].id) : [];

        if (isMounted) {
          setOverview(nextOverview);
          setAccess(nextAccess);
          setMembers(nextMembers);
          setInvitations(nextInvitations);
          setCompanyProfile(nextCompanyProfile);
          setContracts(nextContracts);
          setObligationDashboardItems(nextObligationDashboardItems);
          setSelectedObligationDetail(null);
          setObligationDashboardStatus(canLoadObligations ? "ready" : "idle");
          setContractClauses(nextContractClauses);
          setContractDeliverables(nextContractDeliverables);
          setContractDocuments(nextContractDocuments);
          setCalendarEvents(nextCalendarEvents);
          setCalendarStatus(canLoadCalendar ? "ready" : "idle");
          setSelectedContractId(nextContracts[0]?.id ?? null);
          setAuditLogs(nextAuditLogs);
          setAuditLogStatus(canLoadAuditLogs ? "ready" : "idle");
          setNoCuiAcknowledgement(nextNoCuiAcknowledgement);
          setEvidenceItems(nextEvidenceItems);
          setSelectedEvidenceItemId(nextEvidenceItems[0]?.id ?? null);
          setLoadState("ready");
        }
      })
      .catch(() => {
        if (isMounted) {
          setOverview(fallbackOverview);
          setAccess(fallbackAccess);
          setMembers([]);
          setInvitations([]);
          setCompanyProfile(null);
          setContracts([]);
          setObligationDashboardItems([]);
          setSelectedObligationDetail(null);
          setObligationDashboardStatus("idle");
          setContractClauses([]);
          setContractDeliverables([]);
          setContractDocuments([]);
          setCalendarEvents([]);
          setCalendarStatus("idle");
          setSelectedContractId(null);
          setAuditLogs(fallbackAuditLogs);
          setAuditLogStatus("idle");
          setNoCuiAcknowledgement(fallbackNoCuiAcknowledgementStatus);
          setEvidenceItems([]);
          setSelectedEvidenceItemId(null);
          setLoadState("error");
        }
      });

    return () => {
      isMounted = false;
    };
  }, []);

  async function handleInvitationSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setInviteStatus("sending");

    const createdInvitation = await createTenantInvitation({
      email: inviteEmail,
      roleName: inviteRole,
      expiresInDays: 7
    });

    if (createdInvitation) {
      setInvitations((currentInvitations) => [createdInvitation, ...currentInvitations]);
      setInviteEmail("");
      setInviteRole("Contributor");
      setInviteStatus("created");
      return;
    }

    setInviteStatus("failed");
  }

  async function handleCompanyProfileSave(request: UpsertCompanyProfileRequest) {
    setProfileStatus("saving");
    setProfileMessage("");

    const result = await saveCompanyProfile(request);
    if (result.data) {
      setCompanyProfile(result.data);
      setProfileStatus("saved");
      setProfileMessage(result.data.isComplete ? "Profile complete." : "Draft saved.");
      return;
    }

    setProfileStatus("failed");
    setProfileMessage(result.error ?? "Profile could not be saved.");
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
      setClauseResults(results);
      setClauseSearchStatus("ready");
      setClauseSearchMessage(results.length > 0 ? `${results.length} published clause results.` : "No published clauses matched.");
    } catch {
      setClauseResults([]);
      setClauseSearchStatus("failed");
      setClauseSearchMessage("Clause search could not be completed.");
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
      return;
    }

    setObligationDetailStatus("failed");
    setObligationDetailMessage(result.error ?? "Obligation owner could not be assigned.");
  }

  async function handleContractSelect(contractId: string | null) {
    setSelectedContractId(contractId);
    setContractClauseMessage("");
    setDeliverableMessage("");
    setContractDocumentMessage("");
    const [nextClauses, nextDeliverables, nextDocuments] = contractId
      ? await Promise.all([getContractClauses(contractId), getContractDeliverables(contractId), getContractDocuments(contractId)])
      : [[], [], []];
    setContractClauses(nextClauses);
    setContractDeliverables(nextDeliverables);
    setContractDocuments(nextDocuments);
  }

  async function handleContractClauseAttach(contractId: string, request: AttachContractClauseRequest) {
    setContractClauseStatus("saving");
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

  async function handleContractDocumentUpload(contractId: string, documentType: string, file: File | null) {
    if (!file) {
      setContractDocumentStatus("failed");
      setContractDocumentMessage("Select a contract document before upload.");
      return;
    }

    setContractDocumentStatus("saving");
    setContractDocumentMessage("");
    const result = await createContractDocument(contractId, {
      type: documentType,
      fileName: file.name,
      contentType: file.type || "application/octet-stream",
      sizeBytes: file.size,
      containsPotentialCui: false
    });

    if (result.data) {
      const savedDocument = result.data;
      setContractDocuments((currentDocuments) => [savedDocument, ...currentDocuments]);
      setContractDocumentStatus("saved");
      setContractDocumentMessage(
        `Document metadata captured. Validation ${savedDocument.validationStatus}; malware scan ${savedDocument.malwareScanStatus}.`
      );
      return;
    }

    setContractDocumentStatus("failed");
    setContractDocumentMessage(result.error ?? "Contract document upload was rejected.");
  }

  async function handleContractDocumentDelete(contractId: string, documentId: string) {
    setContractDocumentStatus("saving");
    setContractDocumentMessage("");
    const result = await deleteContractDocument(contractId, documentId);

    if (!result.error) {
      setContractDocuments((currentDocuments) => currentDocuments.filter((document) => document.id !== documentId));
      setContractDocumentStatus("saved");
      setContractDocumentMessage("Document metadata deleted.");
      return;
    }

    setContractDocumentStatus("failed");
    setContractDocumentMessage(result.error);
  }

  async function handleNoCuiAcknowledgement() {
    setAcknowledgementStatus("saving");
    const acknowledgement = await acknowledgeNoCuiNotice(noCuiAcknowledgement.noticeVersion);

    if (acknowledgement) {
      setNoCuiAcknowledgement(acknowledgement);
      setAcknowledgementStatus("saved");
      return;
    }

    setAcknowledgementStatus("failed");
  }

  async function handleEvidenceUploadIntentSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!selectedEvidenceFile) {
      setUploadStatus("blocked");
      setUploadMessage("Select an allowed evidence file before upload.");
      return;
    }

    setUploadStatus("creating");
    setUploadMessage("");
    const uploadIntent = await createEvidenceUploadIntent(selectedEvidenceFile);

    if (uploadIntent.data) {
      setUploadStatus("created");
      setUploadMessage(
        `Upload intent created for ${uploadIntent.data.fileName}. Validation ${uploadIntent.data.validationStatus}; malware scan ${uploadIntent.data.malwareScanStatus}.`
      );
      return;
    }

    setUploadStatus("blocked");
    setUploadMessage(uploadIntent.error ?? "The API blocked the upload intent.");
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
      setSelectedEvidenceItemId(saved.id);
      setEvidenceMetadataStatus("saved");
      setEvidenceMetadataMessage(evidenceItemId ? "Evidence metadata updated." : "Evidence metadata created.");
      return;
    }

    setEvidenceMetadataStatus("failed");
    setEvidenceMetadataMessage(result.error ?? "Evidence metadata could not be saved.");
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
        ...defaultCalendarQuery(),
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
    <div className="workspace-shell">
      <a className="skip-link" href="#workspace-content">
        Skip to workspace content
      </a>
      <aside className="workspace-sidebar">
        <div className="workspace-brand">
          <span className="brand-mark" aria-hidden="true">
            <Home size={18} />
          </span>
          <div>
            <strong>GCCS</strong>
            <span>No-CUI workspace</span>
          </div>
        </div>
        <nav aria-label="Primary workspace navigation">
          <ul className="workspace-nav">
            {visibleNavigation.map((item) => {
              const Icon = item.icon;
              return (
                <li key={item.route}>
                  <a
                    href={`#/${item.route}`}
                    aria-current={activeRoute === item.route ? "page" : undefined}
                    onClick={() => handleRouteClick(item.route)}
                  >
                    <Icon size={18} aria-hidden="true" />
                    <span>
                      <strong>{item.label}</strong>
                      <small>{item.description}</small>
                    </span>
                  </a>
                </li>
              );
            })}
          </ul>
        </nav>
      </aside>

      <main id="workspace-content" className="workspace-main" tabIndex={-1}>
        <header className="workspace-topbar">
          <div>
            <p className="eyebrow">GCCS Compliance Workspace</p>
            <h1>{activeRoute === "dashboard" ? "Dashboard" : navigationItems.find((item) => item.route === activeRoute)?.label}</h1>
          </div>
          <div className="tenant-context" aria-label="Current tenant context">
            <span>{access.userEmail ?? "Development user"}</span>
            <strong>{overview.mvpDataPosture}</strong>
          </div>
        </header>

        <WorkspaceState state={loadState}>
          {activeRoute === "dashboard" ? (
            <DashboardView overview={overview} />
          ) : activeRoute === "profile" ? (
            <ProfileView
              key={`${companyProfile?.id ?? "new-profile"}-${companyProfile?.updatedAt ?? companyProfile?.createdAt ?? "draft"}`}
              canManageCompanyProfile={canManageCompanyProfile}
              profile={companyProfile}
              profileMessage={profileMessage}
              profileStatus={profileStatus}
              onSave={handleCompanyProfileSave}
            />
          ) : activeRoute === "contracts" ? (
            <ContractsView
              canManageContracts={canManageContracts}
              contracts={contracts}
              contractClauses={contractClauses}
              contractClauseMessage={contractClauseMessage}
              contractClauseStatus={contractClauseStatus}
              contractDeliverables={contractDeliverables}
              contractDocuments={contractDocuments}
              deliverableMessage={deliverableMessage}
              deliverableStatus={deliverableStatus}
              contractDocumentMessage={contractDocumentMessage}
              contractDocumentStatus={contractDocumentStatus}
              contractMessage={contractMessage}
              contractStatus={contractStatus}
              noCuiAcknowledgement={noCuiAcknowledgement}
              selectedContractId={selectedContractId}
              onDeleteDocument={handleContractDocumentDelete}
              onAttachClause={handleContractClauseAttach}
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
              detail={selectedObligationDetail}
              detailMessage={obligationDetailMessage}
              detailStatus={obligationDetailStatus}
              items={obligationDashboardItems}
              members={members}
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
              acknowledgementStatus={acknowledgementStatus}
              canManageEvidence={canManageEvidence}
              evidenceItems={evidenceItems}
              evidenceMetadataMessage={evidenceMetadataMessage}
              evidenceMetadataStatus={evidenceMetadataStatus}
              selectedEvidenceItemId={selectedEvidenceItemId}
              selectedFile={selectedEvidenceFile}
              uploadMessage={uploadMessage}
              uploadStatus={uploadStatus}
              onAcknowledge={handleNoCuiAcknowledgement}
              onFileSelected={setSelectedEvidenceFile}
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
          ) : activeRoute === "settings" ? (
            <SettingsView
              canManageUsers={canManageUsers}
              canViewAuditLog={canViewAuditLog}
              auditLogFilters={auditLogFilters}
              auditLogStatus={auditLogStatus}
              auditLogs={auditLogs}
              inviteEmail={inviteEmail}
              inviteRole={inviteRole}
              inviteStatus={inviteStatus}
              invitations={invitations}
              members={members}
              onAuditLogFilterChange={setAuditLogFilters}
              onAuditLogFilterSubmit={handleAuditLogFilterSubmit}
              onAuditLogPageChange={handleAuditLogPageChange}
              onInviteEmailChange={setInviteEmail}
              onInviteRoleChange={setInviteRole}
              onInvitationSubmit={handleInvitationSubmit}
            />
          ) : (
            <PlaceholderRoute route={activeRoute} />
          )}
        </WorkspaceState>
      </main>
    </div>
  );
}

function WorkspaceState({ children, state }: { children: ReactNode; state: LoadState }) {
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
  const metrics = useMemo(
    () => ({
      total: events.length,
      overdue: events.filter((event) => event.isOverdue).length,
      highRisk: events.filter((event) => event.riskLevel === "High").length,
      months: new Set(events.map((event) => event.date.slice(0, 7))).size
    }),
    [events]
  );

  return (
    <section className="route-panel" aria-label="Compliance calendar">
      <div className="route-panel__intro section-heading--split">
        <div>
          <p className="eyebrow">Compliance calendar</p>
          <h2>Calendar agenda</h2>
          <p>Tenant-scoped tasks, renewals, reports, contract deadlines, deliverables, and policy reviews.</p>
        </div>
        <div className="queue-metrics" aria-label="Calendar summary">
          <span>
            <strong>{metrics.total}</strong> items
          </span>
          <span>
            <strong>{metrics.overdue}</strong> overdue
          </span>
          <span>
            <strong>{metrics.highRisk}</strong> high risk
          </span>
          <span>
            <strong>{metrics.months}</strong> months
          </span>
        </div>
      </div>

      <form className="calendar-filter-form" onSubmit={onFilterSubmit}>
        <label>
          Owner
          <input
            value={filters.owner}
            onChange={(event) => onFilterChange({ ...filters, owner: event.target.value })}
          />
        </label>
        <label>
          Status
          <select value={filters.status} onChange={(event) => onFilterChange({ ...filters, status: event.target.value })}>
            <option value="">Any</option>
            <option value="open">Open</option>
            <option value="in_progress">In progress</option>
            <option value="waiting_for_review">Waiting for review</option>
            <option value="completed">Completed</option>
          </select>
        </label>
        <label>
          Risk
          <select value={filters.risk} onChange={(event) => onFilterChange({ ...filters, risk: event.target.value })}>
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
          <select value={filters.module} onChange={(event) => onFilterChange({ ...filters, module: event.target.value })}>
            <option value="">Any</option>
            <option value="Contract">Contract</option>
            <option value="Obligations">Obligations</option>
            <option value="Policy reviews">Policy reviews</option>
            <option value="Renewals">Renewals</option>
            <option value="Reports">Reports</option>
            <option value="Tasks">Tasks</option>
          </select>
        </label>
        <button type="submit" disabled={status === "loading"}>
          <SlidersHorizontal size={16} aria-hidden="true" />
          Apply filters
        </button>
      </form>

      {message ? (
        <p className={`form-message form-message--${status === "failed" ? "error" : "success"}`} role="status">
          {message}
        </p>
      ) : null}

      <div className="calendar-workspace">
        <div className="calendar-month-strip" aria-label="Month view summary">
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

        <div className="calendar-agenda" aria-label="Calendar list view">
          {events.length === 0 ? (
            <EmptyState
              title="No calendar items yet"
              body="Tasks, renewals, reports, contract deadlines, deliverables, and policy reviews will appear here."
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
                      <dd>{event.ownerFunction}</dd>
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

function DashboardView({ overview }: { overview: ComplianceOverview }) {
  const hasModules = overview.modules.length > 0;
  const hasPriorityObligations = overview.priorityObligations.length > 0;

  return (
    <>
      <section className="workspace-hero">
        <div>
          <p className="eyebrow">Workspace overview</p>
          <h2>Govcon obligations, evidence, and readiness in one operating view.</h2>
          <p className="hero-copy">{overview.productPromise}</p>
        </div>
        <div className="hero-panel" aria-label="MVP platform posture">
          <span>Current data posture</span>
          <strong>{overview.mvpDataPosture}</strong>
        </div>
      </section>

      <section className="metrics-grid" aria-label="Workspace metrics">
        <div className="metric">
          <span>Priority obligations</span>
          <strong>{overview.priorityObligations.length}</strong>
        </div>
        <div className="metric">
          <span>MVP modules</span>
          <strong>{overview.modules.length}</strong>
        </div>
        <div className="metric">
          <span>Evidence posture</span>
          <strong>No-CUI</strong>
        </div>
        <div className="metric">
          <span>Source status</span>
          <strong>Seeded</strong>
        </div>
      </section>

      <section className="work-grid" aria-label="Compliance operations">
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
                <article key={obligation.id} className="obligation-item">
                  <div>
                    <span className={`risk risk--${obligation.riskLevel.toLowerCase()}`}>{obligation.riskLevel}</span>
                    <h3>{obligation.source}</h3>
                  </div>
                  <p>{obligation.title}</p>
                  <dl>
                    <div>
                      <dt>Owner</dt>
                      <dd>{obligation.ownerFunction}</dd>
                    </div>
                    <div>
                      <dt>Reviewed</dt>
                      <dd>{obligation.lastReviewedAt}</dd>
                    </div>
                  </dl>
                </article>
              ))
            ) : (
              <EmptyState
                title="Source data unavailable"
                body="Priority obligations are provided by the API, not by UI-only fallback content."
              />
            )}
          </div>
        </aside>
      </section>
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
  canManageObligations,
  clauseLibrary,
  contracts,
  detail,
  detailMessage,
  detailStatus,
  items,
  members,
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
  detail: ContractObligationDetail | null;
  detailMessage: string;
  detailStatus: "idle" | "loading" | "ready" | "saving" | "failed";
  items: ContractObligationDashboardItem[];
  members: TenantMember[];
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
  const overdueCount = items.filter((item) => item.isOverdue).length;
  const highRiskCount = items.filter((item) => item.isHighRisk).length;

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
          <input value={owner} onChange={(event) => setOwner(event.target.value)} placeholder="IT/security" disabled={status === "loading"} />
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

      {message ? (
        <p className={`form-status ${status === "failed" ? "form-status--error" : "form-status--ok"}`}>{message}</p>
      ) : null}

      {items.length > 0 ? (
        <div className="obligation-dashboard-list" aria-label="Tenant obligation work queue">
          {items.map((item) => (
            <article
              key={item.id}
              className={`obligation-dashboard-item${item.isOverdue ? " obligation-dashboard-item--overdue" : ""}${
                item.isHighRisk ? " obligation-dashboard-item--high-risk" : ""
              }`}
            >
              <div className="obligation-dashboard-item__main">
                <div className="obligation-dashboard-item__badges">
                  <span className={`risk risk--${item.riskLevel.toLowerCase()}`} aria-label={`${item.riskLevel} risk obligation`}>
                    {item.riskLevel}
                  </span>
                  {item.isOverdue ? (
                    <span className="status status--overdue" aria-label="Overdue obligation">
                      <AlertTriangle size={14} aria-hidden="true" />
                      Overdue
                    </span>
                  ) : null}
                  <span className="status status--active">{item.status}</span>
                </div>
                <h3>{item.title}</h3>
                <p>{item.plainEnglishSummary}</p>
              </div>
              <dl>
                <div>
                  <dt>Contract</dt>
                  <dd>{item.contractNumber}</dd>
                </div>
                <div>
                  <dt>Owner</dt>
                  <dd>{item.ownerFunction}</dd>
                </div>
                <div>
                  <dt>Due</dt>
                  <dd>{item.dueAt ?? "No date"}</dd>
                </div>
                <div>
                  <dt>Module</dt>
                  <dd>{item.module}</dd>
                </div>
                <div>
                  <dt>Source</dt>
                  <dd>
                    <a href={item.sourceUrl} target="_blank" rel="noreferrer">
                      {item.source}
                    </a>
                  </dd>
                </div>
              </dl>
              <p className="obligation-required-action">{item.requiredAction}</p>
              <button className="secondary-action obligation-detail-button" type="button" onClick={() => void onDetailSelect(item)}>
                View details
              </button>
            </article>
          ))}
        </div>
      ) : (
        <EmptyState
          title="Start with company profile or contract intake"
          body="Complete the company profile, add a contract, and attach mapped clauses to generate tenant-scoped obligations."
        />
      )}

      <ObligationDetailPanel
        canManageObligations={canManageObligations}
        detail={detail}
        members={members}
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
  canManageObligations,
  detail,
  members,
  message,
  onOwnerAssign,
  onStatusUpdate,
  status
}: {
  canManageObligations: boolean;
  detail: ContractObligationDetail | null;
  members: TenantMember[];
  message: string;
  onOwnerAssign: (kind: "user" | "role", value: string, notify: boolean) => Promise<void>;
  onStatusUpdate: (status: string) => Promise<void>;
  status: "idle" | "loading" | "ready" | "saving" | "failed";
}) {
  const [ownerKind, setOwnerKind] = useState<"user" | "role">("user");

  if (status === "loading") {
    return (
      <section className="obligation-detail-panel" aria-live="polite">
        <h2>Loading obligation detail</h2>
        <p>Retrieving source-backed detail, linked tasks, and evidence.</p>
      </section>
    );
  }

  if (!detail) {
    return message ? (
      <p className={`form-status ${status === "failed" ? "form-status--error" : "form-status--ok"}`}>{message}</p>
    ) : null;
  }

  return (
    <section className="obligation-detail-panel" aria-label="Obligation detail">
      <div className="section-heading section-heading--split">
        <div>
          <p className="eyebrow">Obligation detail</p>
          <h2>{detail.title}</h2>
        </div>
        <span className="status status--active">{detail.status}</span>
      </div>

      {message ? (
        <p className={`form-status ${status === "failed" ? "form-status--error" : "form-status--ok"}`}>{message}</p>
      ) : null}

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
          <p>{detail.ownerFunction}</p>
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
          const value = String(formData.get(ownerKind === "user" ? "userId" : "roleName") ?? "");
          const notify = formData.get("notify") === "on";
          if (value) {
            void onOwnerAssign(ownerKind, value, notify);
          }
        }}
      >
        <label>
          Assign by
          <select
            value={ownerKind}
            onChange={(event) => setOwnerKind(event.target.value as "user" | "role")}
            disabled={!canManageObligations || status === "saving"}
          >
            <option value="user">Tenant member</option>
            <option value="role">Role</option>
          </select>
        </label>
        {ownerKind === "user" ? (
          <label>
            Tenant member
            <select name="userId" disabled={!canManageObligations || status === "saving"}>
              <option value="">Select member</option>
              {members.map((member) => (
                <option key={member.userId} value={member.userId}>
                  {member.displayName || member.email}
                </option>
              ))}
            </select>
          </label>
        ) : (
          <label>
            Role
            <select name="roleName" disabled={!canManageObligations || status === "saving"}>
              <option value="">Select role</option>
              <option value="ComplianceManager">Compliance manager</option>
              <option value="Advisor">Advisor</option>
              <option value="Contributor">Contributor</option>
              <option value="Auditor">Auditor</option>
            </select>
          </label>
        )}
        <label className="inline-checkbox">
          <input name="notify" type="checkbox" disabled={!canManageObligations || status === "saving"} />
          Notify owner
        </label>
        <button type="submit" disabled={!canManageObligations || status === "saving"}>
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
                  <div>
                    <dt>Source</dt>
                    <dd>
                      <a href={clause.sourceUrl} target="_blank" rel="noreferrer">
                        {clause.source}
                      </a>
                    </dd>
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
              <button type="button" onClick={() => setSelectedClauseId(clause.id)} disabled={!clause.isMappable}>
                Select clause
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

function ContractsView({
  canManageContracts,
  contracts,
  contractClauses,
  contractClauseMessage,
  contractClauseStatus,
  contractDeliverables,
  contractDocuments,
  deliverableMessage,
  deliverableStatus,
  contractDocumentMessage,
  contractDocumentStatus,
  contractMessage,
  contractStatus,
  noCuiAcknowledgement,
  selectedContractId,
  onDeleteDocument,
  onAttachClause,
  onRemoveClause,
  onSaveDeliverable,
  onUploadDocument,
  onSave,
  onSelectContract
}: {
  canManageContracts: boolean;
  contracts: ContractRecord[];
  contractClauses: ContractClause[];
  contractClauseMessage: string;
  contractClauseStatus: "idle" | "saving" | "saved" | "failed";
  contractDeliverables: ContractDeliverable[];
  contractDocuments: ContractDocument[];
  deliverableMessage: string;
  deliverableStatus: "idle" | "saving" | "saved" | "failed";
  contractDocumentMessage: string;
  contractDocumentStatus: "idle" | "saving" | "saved" | "failed";
  contractMessage: string;
  contractStatus: "idle" | "saving" | "saved" | "failed";
  noCuiAcknowledgement: NoCuiAcknowledgementStatus;
  selectedContractId: string | null;
  onDeleteDocument: (contractId: string, documentId: string) => Promise<void>;
  onAttachClause: (contractId: string, request: AttachContractClauseRequest) => Promise<void>;
  onRemoveClause: (contractId: string, contractClauseId: string, reason: string) => Promise<void>;
  onSaveDeliverable: (
    contractId: string,
    deliverableId: string | null,
    request: UpsertContractDeliverableRequest
  ) => Promise<void>;
  onUploadDocument: (contractId: string, documentType: string, file: File | null) => Promise<void>;
  onSave: (contractId: string | null, request: UpsertContractRequest) => Promise<void>;
  onSelectContract: (contractId: string | null) => void;
}) {
  const selectedContract = contracts.find((contract) => contract.id === selectedContractId) ?? null;
  const [selectedDocumentFile, setSelectedDocumentFile] = useState<File | null>(null);
  const [documentType, setDocumentType] = useState("Contract");
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

      {contractMessage ? (
        <p className={`form-status ${contractStatus === "failed" ? "form-status--error" : "form-status--ok"}`}>
          {contractMessage}
        </p>
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
                <small>{contract.status} · {contract.relationship}</small>
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
          onSave={onSave}
        />

        {selectedContract ? (
          <section className="contract-detail" aria-label="Contract detail">
            <div>
              <span>Period</span>
              <strong>{selectedContract.periodOfPerformanceStart} to {selectedContract.periodOfPerformanceEnd}</strong>
            </div>
            <div>
              <span>Role</span>
              <strong>{selectedContract.relationship}</strong>
            </div>
            <div>
              <span>Agency or prime</span>
              <strong>{selectedContract.agencyOrPrimeName}</strong>
            </div>
            <div>
              <span>Data posture</span>
              <strong>{selectedContract.dataHandlingPosture}</strong>
            </div>
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
                value={clauseDraft.clauseLibraryId}
                onChange={(event) => setClauseDraft((current) => ({ ...current, clauseLibraryId: event.target.value }))}
                disabled={clauseDisabled}
                required
              />
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
          {contractClauseMessage ? (
            <p className={`form-status ${contractClauseStatus === "failed" ? "form-status--error" : "form-status--ok"}`}>
              {contractClauseMessage}
            </p>
          ) : null}
          <div className="contract-clause-list">
            {contractClauses.length > 0 ? (
              contractClauses.map((clause) => (
                <article className="contract-clause-item" key={clause.id}>
                  <div>
                    <strong>{clause.clauseNumber}</strong>
                    <span>{clause.title}</span>
                    <small>
                      Reviewed {clause.lastReviewedAt} · {clause.attachmentReason}
                    </small>
                    <a href={clause.sourceUrl} target="_blank" rel="noreferrer">
                      {clause.sourceUrl}
                    </a>
                  </div>
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
              <input
                value={deliverableDraft.ownerFunction}
                onChange={(event) => setDeliverableDraft((current) => ({ ...current, ownerFunction: event.target.value }))}
                disabled={deliverableDisabled}
                required
              />
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
          {deliverableMessage ? (
            <p className={`form-status ${deliverableStatus === "failed" ? "form-status--error" : "form-status--ok"}`}>
              {deliverableMessage}
            </p>
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
                    <span>
                      {deliverable.ownerFunction} · {deliverable.dueAt ?? "No due date"}
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
          </div>
          <form
            className="contract-document-upload"
            onSubmit={(event) => {
              event.preventDefault();
              if (selectedContract) {
                void onUploadDocument(selectedContract.id, documentType, selectedDocumentFile);
              }
            }}
          >
            <input
              aria-label="Contract document"
              type="file"
              onChange={(event) => setSelectedDocumentFile(event.target.files?.[0] ?? null)}
              disabled={uploadDisabled}
            />
            <button type="submit" disabled={uploadDisabled}>
              Upload metadata
            </button>
          </form>
          {!noCuiAcknowledgement.isAcknowledged ? (
            <p className="form-status form-status--error">No-CUI acknowledgement is required before contract document upload.</p>
          ) : null}
          {contractDocumentMessage ? (
            <p className={`form-status ${contractDocumentStatus === "failed" ? "form-status--error" : "form-status--ok"}`}>
              {contractDocumentMessage}
            </p>
          ) : null}
          <div className="contract-document-list">
            {contractDocuments.length > 0 ? (
              contractDocuments.map((document) => (
                <article className="contract-document-item" key={document.id}>
                  <div>
                    <strong>{document.fileName}</strong>
                    <span>{document.type} · {document.validationStatus} · {document.malwareScanStatus}</span>
                  </div>
                  <button
                    type="button"
                    onClick={() => selectedContract && void onDeleteDocument(selectedContract.id, document.id)}
                    disabled={!canManageContracts || contractDocumentStatus === "saving"}
                  >
                    Delete
                  </button>
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
  onSave
}: {
  canManageContracts: boolean;
  contractStatus: "idle" | "saving" | "saved" | "failed";
  selectedContract: ContractRecord | null;
  onSave: (contractId: string | null, request: UpsertContractRequest) => Promise<void>;
}) {
  const [form, setForm] = useState<ContractFormState>(() => contractToForm(selectedContract));

  function updateField<TKey extends keyof ContractFormState>(field: TKey, value: ContractFormState[TKey]) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  async function save() {
    await onSave(selectedContract?.id ?? null, contractFormToRequest(form));
  }

  return (
    <form className="contract-form" onSubmit={(event) => event.preventDefault()}>
      <fieldset disabled={!canManageContracts || contractStatus === "saving"}>
        <div className="form-grid">
          <label>
            <span>Contract number</span>
            <input value={form.contractNumber} onChange={(event) => updateField("contractNumber", event.target.value)} />
          </label>
          <label>
            <span>Title</span>
            <input value={form.title} onChange={(event) => updateField("title", event.target.value)} />
          </label>
          <label>
            <span>Agency or prime</span>
            <input value={form.agencyOrPrimeName} onChange={(event) => updateField("agencyOrPrimeName", event.target.value)} />
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
              <option value="Active">Active</option>
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
            />
          </label>
          <label>
            <span>End</span>
            <input
              type="date"
              value={form.periodOfPerformanceEnd}
              onChange={(event) => updateField("periodOfPerformanceEnd", event.target.value)}
            />
          </label>
          <label>
            <span>FCI/CUI posture</span>
            <select value={form.dataHandlingPosture} onChange={(event) => updateField("dataHandlingPosture", event.target.value)}>
              <option value="NoFciOrCui">No FCI/CUI</option>
              <option value="FciOnly">FCI only</option>
              <option value="Cui">CUI</option>
              <option value="Classified">Classified</option>
              <option value="ExportControlled">Export-controlled</option>
            </select>
          </label>
          <label className="span-2">
            <span>Place of performance</span>
            <input value={form.placeOfPerformance} onChange={(event) => updateField("placeOfPerformance", event.target.value)} />
          </label>
          <label className="span-2">
            <span>Description</span>
            <textarea value={form.description} onChange={(event) => updateField("description", event.target.value)} />
          </label>
        </div>
      </fieldset>
      <div className="form-actions">
        <button type="button" onClick={() => save()} disabled={!canManageContracts || contractStatus === "saving"}>
          {selectedContract ? "Update contract" : "Create contract"}
        </button>
      </div>
    </form>
  );
}

function ProfileView({
  canManageCompanyProfile,
  onSave,
  profile,
  profileMessage,
  profileStatus
}: {
  canManageCompanyProfile: boolean;
  onSave: (request: UpsertCompanyProfileRequest) => Promise<void>;
  profile: CompanyProfile | null;
  profileMessage: string;
  profileStatus: "idle" | "saving" | "saved" | "failed";
}) {
  const [form, setForm] = useState<ProfileFormState>(() => profileToForm(profile));

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

      {profile && Object.keys(profile.validationErrors).length > 0 ? (
        <div className="validation-summary" role="status">
          {Object.entries(profile.validationErrors).map(([field, messages]) => (
            <p key={field}>
              <strong>{field}</strong> {messages.join(" ")}
            </p>
          ))}
        </div>
      ) : null}

      <form className="profile-form" onSubmit={(event) => event.preventDefault()}>
        <fieldset disabled={!canManageCompanyProfile || profileStatus === "saving"}>
          <div className="form-grid">
            <label>
              <span>Legal entity</span>
              <input value={form.legalEntityName} onChange={(event) => updateField("legalEntityName", event.target.value)} />
            </label>
            <label>
              <span>DBA</span>
              <input value={form.doingBusinessAs} onChange={(event) => updateField("doingBusinessAs", event.target.value)} />
            </label>
            <label>
              <span>UEI</span>
              <input value={form.uei} onChange={(event) => updateField("uei", event.target.value)} />
            </label>
            <label>
              <span>CAGE</span>
              <input value={form.cageCode} onChange={(event) => updateField("cageCode", event.target.value)} />
            </label>
            <label>
              <span>SAM expires</span>
              <input
                type="date"
                value={form.samRegistrationExpiresAt}
                onChange={(event) => updateField("samRegistrationExpiresAt", event.target.value)}
              />
            </label>
            <label>
              <span>Role</span>
              <select value={form.contractorRole} onChange={(event) => updateField("contractorRole", event.target.value)}>
                <option value="Unknown">Unknown</option>
                <option value="Prime">Prime</option>
                <option value="Subcontractor">Subcontractor</option>
                <option value="Both">Both</option>
              </select>
            </label>
            <div className="naics-editor span-2">
              <div className="naics-editor__header">
                <span>NAICS codes</span>
                <button type="button" onClick={() => addNaicsRow()} disabled={!canManageCompanyProfile || profileStatus === "saving"}>
                  Add NAICS
                </button>
              </div>
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
                    <input value={naics.code} onChange={(event) => updateNaicsRow(index, "code", event.target.value)} />
                  </label>
                  <label>
                    <span>Title</span>
                    <input value={naics.title} onChange={(event) => updateNaicsRow(index, "title", event.target.value)} />
                  </label>
                  <label>
                    <span>Size basis</span>
                    <input
                      value={naics.sizeStandard}
                      onChange={(event) => updateNaicsRow(index, "sizeStandard", event.target.value)}
                    />
                  </label>
                  <label>
                    <span>Status</span>
                    <select
                      value={naics.qualifiesAsSmall}
                      onChange={(event) => updateNaicsRow(index, "qualifiesAsSmall", event.target.value)}
                    >
                      <option value="">Unknown</option>
                      <option value="true">Small</option>
                      <option value="false">Other than small</option>
                    </select>
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
              <textarea value={form.productsAndServices} onChange={(event) => updateField("productsAndServices", event.target.value)} />
            </label>
            <label>
              <span>Employees</span>
              <select value={form.employeeRange} onChange={(event) => updateField("employeeRange", event.target.value)}>
                <option value="Unknown">Unknown</option>
                <option value="Micro">Micro</option>
                <option value="Small">Small</option>
                <option value="MidSize">Mid-size</option>
                <option value="Large">Large</option>
              </select>
            </label>
            <label>
              <span>Revenue</span>
              <select value={form.revenueRange} onChange={(event) => updateField("revenueRange", event.target.value)}>
                <option value="Unknown">Unknown</option>
                <option value="Micro">Micro</option>
                <option value="Small">Small</option>
                <option value="MidSize">Mid-size</option>
                <option value="Large">Large</option>
              </select>
            </label>
            <label>
              <span>Location</span>
              <input value={form.locationName} onChange={(event) => updateField("locationName", event.target.value)} />
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
              <textarea value={form.itDescription} onChange={(event) => updateField("itDescription", event.target.value)} />
            </label>
            <label>
              <span>FCI/CUI posture</span>
              <select
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

function PlaceholderRoute({ route }: { route: Exclude<WorkspaceRoute, "dashboard" | "settings" | "evidence" | "profile" | "contracts"> }) {
  const content = placeholderContent[route];

  return (
    <section className="route-panel">
      <div className="route-panel__intro">
        <p className="eyebrow">{content.eyebrow}</p>
        <h2>{content.title}</h2>
        <p>{content.description}</p>
      </div>
      <EmptyState title={content.emptyTitle} body={content.emptyBody} />
    </section>
  );
}

function EvidenceView({
  acknowledgement,
  acknowledgementStatus,
  canManageEvidence,
  evidenceItems,
  evidenceMetadataMessage,
  evidenceMetadataStatus,
  onAcknowledge,
  onFileSelected,
  onMetadataSave,
  onSelectEvidence,
  onUploadIntentSubmit,
  selectedEvidenceItemId,
  selectedFile,
  uploadMessage,
  uploadStatus
}: {
  acknowledgement: NoCuiAcknowledgementStatus;
  acknowledgementStatus: "idle" | "saving" | "saved" | "failed";
  canManageEvidence: boolean;
  evidenceItems: EvidenceMetadata[];
  evidenceMetadataMessage: string;
  evidenceMetadataStatus: "idle" | "saving" | "saved" | "failed";
  onAcknowledge: () => void;
  onFileSelected: (file: File | null) => void;
  onMetadataSave: (evidenceItemId: string | null, request: UpsertEvidenceMetadataRequest) => Promise<void>;
  onSelectEvidence: (evidenceItemId: string | null) => void;
  onUploadIntentSubmit: (event: FormEvent<HTMLFormElement>) => void;
  selectedEvidenceItemId: string | null;
  selectedFile: File | null;
  uploadMessage: string;
  uploadStatus: "idle" | "creating" | "created" | "blocked";
}) {
  const uploadDisabled = !canManageEvidence || !acknowledgement.isAcknowledged;
  const selectedEvidence = evidenceItems.find((item) => item.id === selectedEvidenceItemId) ?? null;

  return (
    <section className="route-panel" aria-label="Evidence upload workflow">
      <div className="route-panel__intro">
        <p className="eyebrow">Evidence vault</p>
        <h2>No-CUI evidence management</h2>
        <p>Organize evidence by obligation, contract, control, vendor, employee, expiration, approval status, and audit history.</p>
      </div>

      <EvidenceMetadataPanel
        key={selectedEvidence?.id ?? "new-evidence"}
        canManageEvidence={canManageEvidence}
        evidenceItems={evidenceItems}
        message={evidenceMetadataMessage}
        onSave={onMetadataSave}
        onSelectEvidence={onSelectEvidence}
        selectedEvidence={selectedEvidence}
        status={evidenceMetadataStatus}
      />

      <div className={`notice-panel${acknowledgement.isAcknowledged ? " notice-panel--acknowledged" : ""}`}>
        <span className="notice-panel__icon" aria-hidden="true">
          {acknowledgement.isAcknowledged ? <CheckCircle2 size={22} /> : <AlertTriangle size={22} />}
        </span>
        <div>
          <p className="eyebrow">Required before upload</p>
          <h3>No-CUI acknowledgement</h3>
          <p>{acknowledgement.noticeCopy}</p>
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
                <dd>{new Date(acknowledgement.acknowledgedAt).toLocaleString()}</dd>
              </div>
            ) : null}
          </dl>
          {!acknowledgement.isAcknowledged ? (
            <button
              className="notice-action"
              type="button"
              disabled={!canManageEvidence || acknowledgementStatus === "saving"}
              onClick={onAcknowledge}
            >
              <CheckCircle2 size={16} aria-hidden="true" />
              <span>{acknowledgementStatus === "saving" ? "Saving" : "I acknowledge the No-CUI upload limitation"}</span>
            </button>
          ) : null}
          {!canManageEvidence ? <p className="form-status">ManageEvidence permission is required to acknowledge or upload.</p> : null}
          {acknowledgementStatus === "saved" ? <p className="form-status form-status--ok">Acknowledgement saved.</p> : null}
          {acknowledgementStatus === "failed" ? (
            <p className="form-status form-status--error">Acknowledgement was not saved.</p>
          ) : null}
        </div>
      </div>

      <form className="upload-panel" onSubmit={onUploadIntentSubmit}>
        <label>
          <span>Evidence file</span>
          <input
            type="file"
            disabled={uploadDisabled}
            accept=".csv,.docx,.jpg,.jpeg,.pdf,.png,.txt,.xlsx"
            onChange={(event) => onFileSelected(event.target.files?.[0] ?? null)}
          />
        </label>
        <p className="form-status">
          Allowed file types: PDF, PNG, JPG, TXT, CSV, DOCX, and XLSX. Maximum size: 25 MB.
        </p>
        <button type="submit" disabled={uploadDisabled || !selectedFile || uploadStatus === "creating"}>
          <UploadCloud size={16} aria-hidden="true" />
          <span>{uploadStatus === "creating" ? "Creating upload intent" : "Upload evidence"}</span>
        </button>
        {!acknowledgement.isAcknowledged ? (
          <p className="form-status form-status--error">Upload is disabled until the No-CUI notice is acknowledged.</p>
        ) : null}
        {uploadStatus === "created" ? (
          <p className="form-status form-status--ok">{uploadMessage}</p>
        ) : null}
        {uploadStatus === "blocked" ? (
          <p className="form-status form-status--error">{uploadMessage || "The API blocked the upload intent. Confirm acknowledgement and permissions."}</p>
        ) : null}
      </form>

      <EmptyState
        title="No evidence has been uploaded yet"
        body="Accepted uploads remain pending until the malware scan placeholder and future storage workflow are complete."
      />
    </section>
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
  description: ""
};

function EvidenceMetadataPanel({
  canManageEvidence,
  evidenceItems,
  message,
  onSave,
  onSelectEvidence,
  selectedEvidence,
  status
}: {
  canManageEvidence: boolean;
  evidenceItems: EvidenceMetadata[];
  message: string;
  onSave: (evidenceItemId: string | null, request: UpsertEvidenceMetadataRequest) => Promise<void>;
  onSelectEvidence: (evidenceItemId: string | null) => void;
  selectedEvidence: EvidenceMetadata | null;
  status: "idle" | "saving" | "saved" | "failed";
}) {
  const [form, setForm] = useState<EvidenceMetadataFormState>(() => evidenceToMetadataForm(selectedEvidence));

  function updateField<TKey extends keyof EvidenceMetadataFormState>(field: TKey, value: EvidenceMetadataFormState[TKey]) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  function save() {
    void onSave(selectedEvidence?.id ?? null, evidenceMetadataFormToRequest(form, selectedEvidence));
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
        <div className="evidence-list" aria-label="Evidence list">
          {evidenceItems.length > 0 ? (
            evidenceItems.map((item) => (
              <button
                className={`evidence-list__item${selectedEvidence?.id === item.id ? " evidence-list__item--active" : ""}`}
                key={item.id}
                type="button"
                onClick={() => onSelectEvidence(item.id)}
              >
                <strong>{item.title}</strong>
                <span>{item.status} · {item.ownerFunction} · {item.expiresAt ?? "No expiration"}</span>
              </button>
            ))
          ) : (
            <EmptyState title="No evidence metadata yet" body="Create a reusable evidence record before uploading files." />
          )}
        </div>
        <form className="evidence-metadata-form" onSubmit={(event) => event.preventDefault()}>
          <fieldset disabled={!canManageEvidence || status === "saving"}>
            <div className="form-grid">
              <label>
                <span>Title</span>
                <input value={form.title} onChange={(event) => updateField("title", event.target.value)} />
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
                  <option value="AccessReview">Access review</option>
                  <option value="RiskAssessment">Risk assessment</option>
                  <option value="Other">Other</option>
                </select>
              </label>
              <label>
                <span>Owner</span>
                <input value={form.ownerFunction} onChange={(event) => updateField("ownerFunction", event.target.value)} />
              </label>
              <label>
                <span>Status</span>
                <select value={form.status} onChange={(event) => updateField("status", event.target.value)}>
                  <option value="Requested">Requested</option>
                  <option value="Uploaded">Uploaded</option>
                  <option value="InReview">In review</option>
                  <option value="Approved">Approved</option>
                  <option value="Rejected">Rejected</option>
                  <option value="Expired">Expired</option>
                  <option value="Archived">Archived</option>
                </select>
              </label>
              <label>
                <span>Effective</span>
                <input type="date" value={form.effectiveAt} onChange={(event) => updateField("effectiveAt", event.target.value)} />
              </label>
              <label>
                <span>Expires</span>
                <input type="date" value={form.expiresAt} onChange={(event) => updateField("expiresAt", event.target.value)} />
              </label>
              <label>
                <span>Tags</span>
                <input value={form.tags} onChange={(event) => updateField("tags", event.target.value)} />
              </label>
              <label>
                <span>Obligations</span>
                <input value={form.obligationIds} onChange={(event) => updateField("obligationIds", event.target.value)} />
              </label>
              <label>
                <span>Controls</span>
                <input value={form.controlIds} onChange={(event) => updateField("controlIds", event.target.value)} />
              </label>
              <label className="span-2">
                <span>Description</span>
                <textarea value={form.description} onChange={(event) => updateField("description", event.target.value)} />
              </label>
            </div>
          </fieldset>
          <div className="form-actions">
            <button type="button" onClick={save} disabled={!canManageEvidence || status === "saving"}>
              {selectedEvidence ? "Update metadata" : "Create metadata"}
            </button>
          </div>
          {message ? (
            <p className={`form-status ${status === "failed" ? "form-status--error" : "form-status--ok"}`}>{message}</p>
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
    reportIds: evidence?.reportIds ?? []
  };
}

function splitEvidenceList(value: string): string[] {
  return value
    .split(",")
    .map((item) => item.trim())
    .filter(Boolean);
}

function SettingsView({
  auditLogFilters,
  auditLogStatus,
  auditLogs,
  canManageUsers,
  canViewAuditLog,
  inviteEmail,
  inviteRole,
  inviteStatus,
  invitations,
  members,
  onAuditLogFilterChange,
  onAuditLogFilterSubmit,
  onAuditLogPageChange,
  onInviteEmailChange,
  onInviteRoleChange,
  onInvitationSubmit
}: {
  auditLogFilters: AuditLogFilters;
  auditLogStatus: "idle" | "loading" | "ready" | "failed";
  auditLogs: PagedResult<AuditLogEntry>;
  canManageUsers: boolean;
  canViewAuditLog: boolean;
  inviteEmail: string;
  inviteRole: string;
  inviteStatus: "idle" | "sending" | "created" | "failed";
  invitations: TenantInvitation[];
  members: TenantMember[];
  onAuditLogFilterChange: (filters: AuditLogFilters) => void;
  onAuditLogFilterSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onAuditLogPageChange: (page: number) => void;
  onInviteEmailChange: (email: string) => void;
  onInviteRoleChange: (roleName: string) => void;
  onInvitationSubmit: (event: FormEvent<HTMLFormElement>) => void;
}) {
  if (!canManageUsers && !canViewAuditLog) {
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
      {canManageUsers ? (
        <>
          <section className="members-section" aria-label="Tenant team members">
            <div className="section-heading">
              <p className="eyebrow">Tenant access</p>
              <h2>Team members</h2>
            </div>
            {members.length > 0 ? (
              <div className="member-table" role="table" aria-label="Current tenant members">
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
                        <small>{member.email}</small>
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
            {inviteStatus === "created" ? <p className="form-status form-status--ok">Invitation created.</p> : null}
            {inviteStatus === "failed" ? <p className="form-status form-status--error">Invitation was not created.</p> : null}
            {invitations.length > 0 ? (
              <div className="invitation-list">
                {invitations.map((invitation) => (
                  <article className="invitation-item" key={invitation.invitationId}>
                    <div className="invitation-item__main">
                      <span className="icon-box icon-box--small" aria-hidden="true">
                        <UserPlus size={17} />
                      </span>
                      <span>
                        <strong>{invitation.email}</strong>
                        <small>{invitation.roleName}</small>
                      </span>
                    </div>
                    <span className={`status status--${invitation.status.toLowerCase()}`}>{invitation.status}</span>
                    <span className="invitation-date">Expires {new Date(invitation.expiresAt).toLocaleDateString()}</span>
                    <small className="notification-placeholder">{invitation.notificationPlaceholder}</small>
                  </article>
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
          <div className="section-heading section-heading--split">
            <div>
              <p className="eyebrow">Audit trail</p>
              <h2>Audit log</h2>
            </div>
            <form className="invite-form" onSubmit={onAuditLogFilterSubmit}>
              <label>
                <span>Actor ID</span>
                <input
                  value={auditLogFilters.actorUserId}
                  onChange={(event) => onAuditLogFilterChange({ ...auditLogFilters, actorUserId: event.target.value })}
                />
              </label>
              <label>
                <span>Action</span>
                <select
                  value={auditLogFilters.action}
                  onChange={(event) => onAuditLogFilterChange({ ...auditLogFilters, action: event.target.value })}
                >
                  <option value="">Any</option>
                  <option value="Created">Created</option>
                  <option value="Updated">Updated</option>
                  <option value="Deleted">Deleted</option>
                  <option value="Rejected">Rejected</option>
                </select>
              </label>
              <label>
                <span>Entity</span>
                <input
                  value={auditLogFilters.entityType}
                  onChange={(event) => onAuditLogFilterChange({ ...auditLogFilters, entityType: event.target.value })}
                />
              </label>
              <label>
                <span>From</span>
                <input
                  type="datetime-local"
                  value={auditLogFilters.from}
                  onChange={(event) => onAuditLogFilterChange({ ...auditLogFilters, from: event.target.value })}
                />
              </label>
              <label>
                <span>To</span>
                <input
                  type="datetime-local"
                  value={auditLogFilters.to}
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
            <div className="member-table" role="table" aria-label="Tenant audit logs">
              <div className="member-row member-row--header" role="row">
                <span role="columnheader">Date</span>
                <span role="columnheader">Actor</span>
                <span role="columnheader">Action</span>
                <span role="columnheader">Entity</span>
                <span role="columnheader">Summary</span>
              </div>
              {auditLogs.items.map((entry) => (
                <article className="member-row" role="row" key={entry.id}>
                  <span role="cell">{new Date(entry.occurredAt).toLocaleString()}</span>
                  <span role="cell">{entry.actorUserId ?? "System"}</span>
                  <span role="cell">{entry.action}</span>
                  <span role="cell">{entry.entityType}</span>
                  <span role="cell">{entry.summary}</span>
                </article>
              ))}
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
