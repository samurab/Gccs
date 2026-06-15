import {
  Archive,
  Building2,
  CalendarClock,
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
  UserPlus,
  UsersRound
} from "lucide-react";
import { type FormEvent, type ReactNode, useEffect, useMemo, useState } from "react";
import { ModuleCard } from "@/components/ModuleCard";
import {
  createTenantInvitation,
  fallbackAccess,
  fallbackOverview,
  getComplianceOverview,
  getCurrentUserAccess,
  getTenantInvitations,
  getTenantMembers,
  type ComplianceOverview,
  type CurrentUserAccess,
  type TenantInvitation,
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

const placeholderContent: Record<
  Exclude<WorkspaceRoute, "dashboard" | "settings">,
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
  evidence: {
    eyebrow: "Evidence vault",
    title: "No-CUI evidence management",
    description: "Organize evidence by obligation, contract, control, vendor, employee, expiration, approval status, and audit history.",
    emptyTitle: "No evidence has been uploaded yet",
    emptyBody: "The MVP is compliance management only; upload flows must block CUI until a CUI-ready enclave is approved."
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
  const [activeRoute, setActiveRoute] = useState<WorkspaceRoute>(getInitialRoute);
  const [loadState, setLoadState] = useState<LoadState>("loading");
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteRole, setInviteRole] = useState("Contributor");
  const [inviteStatus, setInviteStatus] = useState<"idle" | "sending" | "created" | "failed">("idle");

  const visibleNavigation = useMemo(
    () => navigationItems.filter((item) => hasAnyPermission(access, item.permissions)),
    [access]
  );
  const canManageUsers = access.permissions.includes("ManageUsers");

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
        const [nextMembers, nextInvitations] = canLoadUserManagement
          ? await Promise.all([getTenantMembers(), getTenantInvitations()])
          : [[], []];

        if (isMounted) {
          setOverview(nextOverview);
          setAccess(nextAccess);
          setMembers(nextMembers);
          setInvitations(nextInvitations);
          setLoadState("ready");
        }
      })
      .catch(() => {
        if (isMounted) {
          setOverview(fallbackOverview);
          setAccess(fallbackAccess);
          setMembers([]);
          setInvitations([]);
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
          ) : activeRoute === "settings" ? (
            <SettingsView
              canManageUsers={canManageUsers}
              inviteEmail={inviteEmail}
              inviteRole={inviteRole}
              inviteStatus={inviteStatus}
              invitations={invitations}
              members={members}
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

function PlaceholderRoute({ route }: { route: Exclude<WorkspaceRoute, "dashboard" | "settings"> }) {
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

function SettingsView({
  canManageUsers,
  inviteEmail,
  inviteRole,
  inviteStatus,
  invitations,
  members,
  onInviteEmailChange,
  onInviteRoleChange,
  onInvitationSubmit
}: {
  canManageUsers: boolean;
  inviteEmail: string;
  inviteRole: string;
  inviteStatus: "idle" | "sending" | "created" | "failed";
  invitations: TenantInvitation[];
  members: TenantMember[];
  onInviteEmailChange: (email: string) => void;
  onInviteRoleChange: (roleName: string) => void;
  onInvitationSubmit: (event: FormEvent<HTMLFormElement>) => void;
}) {
  if (!canManageUsers) {
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
