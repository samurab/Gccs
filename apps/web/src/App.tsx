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
  createTenantInvitation,
  createEvidenceUploadIntent,
  fallbackAuditLogs,
  fallbackAccess,
  fallbackNoCuiAcknowledgementStatus,
  fallbackOverview,
  getCompanyProfile,
  getAuditLogs,
  getComplianceOverview,
  getCurrentUserAccess,
  getNoCuiAcknowledgementStatus,
  getTenantInvitations,
  getTenantMembers,
  saveCompanyProfile,
  type AuditLogEntry,
  type CompanyProfile,
  type ComplianceOverview,
  type CurrentUserAccess,
  type NoCuiAcknowledgementStatus,
  type PagedResult,
  type TenantInvitation,
  type UpsertCompanyProfileRequest,
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

type ProfileFormState = {
  legalEntityName: string;
  doingBusinessAs: string;
  uei: string;
  cageCode: string;
  samRegistrationExpiresAt: string;
  naicsCode: string;
  naicsTitle: string;
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

const defaultProfileForm: ProfileFormState = {
  legalEntityName: "",
  doingBusinessAs: "",
  uei: "",
  cageCode: "",
  samRegistrationExpiresAt: "",
  naicsCode: "",
  naicsTitle: "",
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

export function App() {
  const [overview, setOverview] = useState(fallbackOverview);
  const [access, setAccess] = useState<CurrentUserAccess>(fallbackAccess);
  const [members, setMembers] = useState<TenantMember[]>([]);
  const [invitations, setInvitations] = useState<TenantInvitation[]>([]);
  const [companyProfile, setCompanyProfile] = useState<CompanyProfile | null>(null);
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
  const [selectedEvidenceFile, setSelectedEvidenceFile] = useState<File | null>(null);
  const [acknowledgementStatus, setAcknowledgementStatus] = useState<"idle" | "saving" | "saved" | "failed">("idle");
  const [uploadStatus, setUploadStatus] = useState<"idle" | "creating" | "created" | "blocked">("idle");
  const [uploadMessage, setUploadMessage] = useState("");

  const visibleNavigation = useMemo(
    () => navigationItems.filter((item) => hasAnyPermission(access, item.permissions)),
    [access]
  );
  const canManageUsers = access.permissions.includes("ManageUsers");
  const canManageEvidence = access.permissions.includes("ManageEvidence");
  const canManageCompanyProfile = access.permissions.includes("ManageCompanyProfile");
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
        const [nextMembers, nextInvitations] = canLoadUserManagement
          ? await Promise.all([getTenantMembers(), getTenantInvitations()])
          : [[], []];
        const nextAuditLogs = canLoadAuditLogs ? await getAuditLogs({ page: 1, pageSize: 5 }) : fallbackAuditLogs;
        const nextNoCuiAcknowledgement = canLoadNoCuiStatus
          ? await getNoCuiAcknowledgementStatus()
          : fallbackNoCuiAcknowledgementStatus;
        const nextCompanyProfile = canLoadCompanyProfile ? await getCompanyProfile() : null;

        if (isMounted) {
          setOverview(nextOverview);
          setAccess(nextAccess);
          setMembers(nextMembers);
          setInvitations(nextInvitations);
          setCompanyProfile(nextCompanyProfile);
          setAuditLogs(nextAuditLogs);
          setAuditLogStatus(canLoadAuditLogs ? "ready" : "idle");
          setNoCuiAcknowledgement(nextNoCuiAcknowledgement);
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
          setAuditLogs(fallbackAuditLogs);
          setAuditLogStatus("idle");
          setNoCuiAcknowledgement(fallbackNoCuiAcknowledgementStatus);
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

  async function handleAuditLogFilterSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await loadAuditLogs(1, auditLogFilters);
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
              key={companyProfile?.id ?? "new-profile"}
              canManageCompanyProfile={canManageCompanyProfile}
              profile={companyProfile}
              profileMessage={profileMessage}
              profileStatus={profileStatus}
              onSave={handleCompanyProfileSave}
            />
          ) : activeRoute === "evidence" ? (
            <EvidenceView
              acknowledgement={noCuiAcknowledgement}
              acknowledgementStatus={acknowledgementStatus}
              canManageEvidence={canManageEvidence}
              selectedFile={selectedEvidenceFile}
              uploadMessage={uploadMessage}
              uploadStatus={uploadStatus}
              onAcknowledge={handleNoCuiAcknowledgement}
              onFileSelected={setSelectedEvidenceFile}
              onUploadIntentSubmit={handleEvidenceUploadIntentSubmit}
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
  const primaryNaics = profile?.naicsCodes[0];
  const primaryLocation = profile?.locations[0];

  return {
    ...defaultProfileForm,
    legalEntityName: profile?.legalEntityName ?? "",
    doingBusinessAs: profile?.doingBusinessAs ?? "",
    uei: profile?.uei ?? "",
    cageCode: profile?.cageCode ?? "",
    samRegistrationExpiresAt: profile?.samRegistrationExpiresAt ?? "",
    naicsCode: primaryNaics?.code ?? "",
    naicsTitle: primaryNaics?.title ?? "",
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
  const naicsCodes =
    form.naicsCode.trim() && form.naicsTitle.trim()
      ? [
          {
            code: form.naicsCode.trim(),
            title: form.naicsTitle.trim(),
            isPrimary: true,
            sizeStandard: null,
            qualifiesAsSmall: null,
            lastCheckedAt: null
          }
        ]
      : [];
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
    certifications: [],
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
            <label>
              <span>NAICS</span>
              <input value={form.naicsCode} onChange={(event) => updateField("naicsCode", event.target.value)} />
            </label>
            <label>
              <span>NAICS title</span>
              <input value={form.naicsTitle} onChange={(event) => updateField("naicsTitle", event.target.value)} />
            </label>
            <label className="span-2">
              <span>Agency customers</span>
              <input value={form.agencyCustomers} onChange={(event) => updateField("agencyCustomers", event.target.value)} />
            </label>
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

function PlaceholderRoute({ route }: { route: Exclude<WorkspaceRoute, "dashboard" | "settings" | "evidence" | "profile"> }) {
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
  onAcknowledge,
  onFileSelected,
  onUploadIntentSubmit,
  selectedFile,
  uploadMessage,
  uploadStatus
}: {
  acknowledgement: NoCuiAcknowledgementStatus;
  acknowledgementStatus: "idle" | "saving" | "saved" | "failed";
  canManageEvidence: boolean;
  onAcknowledge: () => void;
  onFileSelected: (file: File | null) => void;
  onUploadIntentSubmit: (event: FormEvent<HTMLFormElement>) => void;
  selectedFile: File | null;
  uploadMessage: string;
  uploadStatus: "idle" | "creating" | "created" | "blocked";
}) {
  const uploadDisabled = !canManageEvidence || !acknowledgement.isAcknowledged;

  return (
    <section className="route-panel" aria-label="Evidence upload workflow">
      <div className="route-panel__intro">
        <p className="eyebrow">Evidence vault</p>
        <h2>No-CUI evidence management</h2>
        <p>Organize evidence by obligation, contract, control, vendor, employee, expiration, approval status, and audit history.</p>
      </div>

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
