import { LoaderCircle, RefreshCw, SlidersHorizontal } from "lucide-react";
import { type FormEvent, useEffect, useState } from "react";
import { Button } from "@/components/ui";
import {
  getDevelopmentTestingContext,
  getSelectedDevelopmentRole,
  getSelectedTenantId,
  selectDevelopmentTestingContext,
  type DevelopmentTenantOption
} from "@/lib/api";

export function DevelopmentTestingContextSelector({
  currentTenantId
}: {
  currentTenantId: string | null;
}) {
  const [tenants, setTenants] = useState<DevelopmentTenantOption[]>([]);
  const [roles, setRoles] = useState<string[]>([]);
  const [tenantId, setTenantId] = useState(getSelectedTenantId() ?? currentTenantId ?? "");
  const [role, setRole] = useState(getSelectedDevelopmentRole());
  const [status, setStatus] = useState<"loading" | "ready" | "applying" | "error">("loading");
  const [message, setMessage] = useState("");

  useEffect(() => {
    let isMounted = true;

    getDevelopmentTestingContext()
      .then((context) => {
        if (!isMounted) return;

        const storedTenantId = getSelectedTenantId();
        const storedTenant = context.tenants.find((tenant) => tenant.tenantId === storedTenantId);
        const fallbackTenant = context.tenants.find((tenant) => tenant.isSelectable);
        const selectedRole = getSelectedDevelopmentRole();
        const normalizedRole = context.roles.includes(selectedRole) ? selectedRole : (context.roles[0] ?? "");
        if (storedTenantId && !storedTenant?.isSelectable && fallbackTenant && normalizedRole) {
          selectDevelopmentTestingContext(fallbackTenant.tenantId, normalizedRole);
          window.location.reload();
          return;
        }

        setTenants(context.tenants);
        setRoles(context.roles);
        setTenantId((selected) => {
          const selectedTenant = context.tenants.find((tenant) => tenant.tenantId === selected);
          return selectedTenant?.isSelectable
            ? selected
            : (context.tenants.find((tenant) => tenant.isSelectable)?.tenantId ?? "");
        });
        setRole((selected) => (context.roles.includes(selected) ? selected : (context.roles[0] ?? "")));
        setStatus("ready");
      })
      .catch(() => {
        if (!isMounted) return;
        setStatus("error");
        setMessage("Local testing contexts could not be loaded.");
      });

    return () => {
      isMounted = false;
    };
  }, []);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!tenantId || !role) return;

    setStatus("applying");
    selectDevelopmentTestingContext(tenantId, role);
    window.location.reload();
  }

  if (status === "loading") {
    return (
      <div className="tenant-workspace-selector" role="status">
        <LoaderCircle size={16} className="spin" aria-hidden="true" />
        <span>Loading test contexts</span>
      </div>
    );
  }

  return (
    <form className="tenant-workspace-selector development-testing-context" onSubmit={handleSubmit}>
      <div className="development-testing-context__heading">
        <SlidersHorizontal size={15} aria-hidden="true" />
        <span>Local test context</span>
      </div>
      <label htmlFor="development-test-tenant">Test tenant</label>
      <select
        id="development-test-tenant"
        value={tenantId}
        disabled={status === "applying" || tenants.length === 0}
        onChange={(event) => setTenantId(event.target.value)}
      >
        {tenants.every((tenant) => !tenant.isSelectable) ? <option value="">No operational tenants available</option> : null}
        {tenants.map((tenant) => (
          <option key={tenant.tenantId} value={tenant.tenantId} disabled={!tenant.isSelectable}>
            {tenant.displayName} ({tenant.tenantStatus}
            {tenant.isSelectable ? "" : " - unavailable"})
          </option>
        ))}
      </select>
      <label htmlFor="development-test-role">Test role</label>
      <select
        id="development-test-role"
        value={role}
        disabled={status === "applying" || roles.length === 0}
        onChange={(event) => setRole(event.target.value)}
      >
        {roles.map((roleName) => (
          <option key={roleName} value={roleName}>
            {roleName}
          </option>
        ))}
      </select>
      <Button type="submit" variant="secondary" disabled={status === "applying" || !tenantId || !role}>
        <RefreshCw size={14} className={status === "applying" ? "spin" : undefined} aria-hidden="true" />
        {status === "applying" ? "Applying" : "Apply"}
      </Button>
      {message ? (
        <span className="tenant-workspace-selector__error" role="alert">
          {message}
        </span>
      ) : null}
    </form>
  );
}
