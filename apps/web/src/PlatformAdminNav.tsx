import { Building2, Inbox, LayoutDashboard, LogOut, UsersRound } from "lucide-react";
import type { PlatformAccess } from "./lib/api";

type PlatformAdminNavProps = {
  access: PlatformAccess;
  active: "overview" | "demo-requests" | "customers" | "tenant-onboarding";
};

export function PlatformAdminNav({ access, active }: PlatformAdminNavProps) {
  const links = [
    { key: "overview", href: "/platform", label: "Overview", icon: LayoutDashboard, visible: true },
    { key: "demo-requests", href: "/platform/demo-requests", label: "Demo requests", icon: Inbox, visible: access.canManageDemoRequests },
    { key: "customers", href: "/platform/customers", label: "Customers", icon: UsersRound, visible: access.canViewPlatformCustomers === true },
    { key: "tenant-onboarding", href: "/platform/tenants/new", label: "Tenant onboarding", icon: Building2, visible: access.canManageTenantOnboarding === true || access.canProvisionTenants }
  ] as const;

  return (
    <nav aria-label="Platform operations" className="platform-console-nav">
      <div>
        {links.filter((link) => link.visible).map((link) => {
          const Icon = link.icon;
          return (
            <a aria-current={active === link.key ? "page" : undefined} href={link.href} key={link.key}>
              <Icon aria-hidden="true" size={17} />
              {link.label}
            </a>
          );
        })}
      </div>
      <a href="/app">
        <LogOut aria-hidden="true" size={17} />
        Workspace
      </a>
    </nav>
  );
}
