import { LoaderCircle, RefreshCw, SlidersHorizontal } from "lucide-react";
import { type FormEvent, useEffect, useState } from "react";
import { Button } from "@/components/ui";
import {
  getDevelopmentTestingContext,
  getSelectedDevelopmentRole,
  getSelectedDevelopmentUserId,
  getSelectedTenantId,
  selectDevelopmentTestingContext,
  type DevelopmentPersonaOption,
  type DevelopmentTenantOption
} from "@/lib/api";

export function DevelopmentTestingContextSelector({
  currentTenantId
}: {
  currentTenantId: string | null;
}) {
  const [tenants, setTenants] = useState<DevelopmentTenantOption[]>([]);
  const [personas, setPersonas] = useState<DevelopmentPersonaOption[]>([]);
  const [roles, setRoles] = useState<string[]>([]);
  const [tenantId, setTenantId] = useState(getSelectedTenantId() ?? currentTenantId ?? "");
  const [userId, setUserId] = useState(getSelectedDevelopmentUserId() ?? "");
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
        const currentTenant = context.tenants.find((tenant) => tenant.tenantId === currentTenantId);
        const fallbackTenant = context.tenants.find((tenant) => tenant.isSelectable);
        const selectedTenantId = storedTenant?.isSelectable
          ? storedTenant.tenantId
          : currentTenant?.isSelectable
            ? currentTenant.tenantId
            : (fallbackTenant?.tenantId ?? "");
        const availablePersonas = context.personas ?? [];
        const storedUserId = getSelectedDevelopmentUserId();
        const selectedPersona =
          availablePersonas.find(
            (persona) => persona.tenantId === selectedTenantId && persona.userId === storedUserId
          ) ?? availablePersonas.find((persona) => persona.tenantId === selectedTenantId);
        const selectedRole = getSelectedDevelopmentRole();
        const normalizedRole = selectedPersona?.roleName ??
          (context.roles.includes(selectedRole) ? selectedRole : (context.roles[0] ?? ""));

        if (storedTenantId && !storedTenant?.isSelectable && fallbackTenant && normalizedRole) {
          const fallbackPersona = availablePersonas.find((persona) => persona.tenantId === fallbackTenant.tenantId);
          selectDevelopmentTestingContext(
            fallbackTenant.tenantId,
            fallbackPersona?.roleName ?? normalizedRole,
            fallbackPersona?.userId,
            fallbackPersona?.email
          );
          window.location.reload();
          return;
        }

        setTenants(context.tenants);
        setPersonas(availablePersonas);
        setRoles(context.roles);
        setTenantId((selected) => {
          const selectedTenant = context.tenants.find((tenant) => tenant.tenantId === selected);
          return selectedTenant?.isSelectable ? selected : selectedTenantId;
        });
        setUserId(selectedPersona?.userId ?? "");
        setRole(normalizedRole);
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
  }, [currentTenantId]);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!tenantId || !role) return;

    setStatus("applying");
    const selectedPersona = personas.find(
      (persona) => persona.tenantId === tenantId && persona.userId === userId
    );
    selectDevelopmentTestingContext(
      tenantId,
      role,
      selectedPersona?.userId,
      selectedPersona?.email
    );
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
      <label htmlFor="development-test-tenant">Switch tenant</label>
      <select
        id="development-test-tenant"
        value={tenantId}
        disabled={status === "applying" || tenants.length === 0}
        onChange={(event) => {
          const nextTenantId = event.target.value;
          const firstPersona = personas.find((persona) => persona.tenantId === nextTenantId);
          setTenantId(nextTenantId);
          setUserId(firstPersona?.userId ?? "");
          if (firstPersona?.roleName) {
            setRole(firstPersona.roleName);
          }
        }}
      >
        {tenants.every((tenant) => !tenant.isSelectable) ? <option value="">No operational tenants available</option> : null}
        {tenants.map((tenant) => (
          <option key={tenant.tenantId} value={tenant.tenantId} disabled={!tenant.isSelectable}>
            {tenant.displayName} ({tenant.tenantStatus}
            {tenant.isSelectable ? "" : " - unavailable"})
          </option>
        ))}
      </select>
      <label htmlFor="development-test-user">Switch user</label>
      <select
        id="development-test-user"
        value={userId}
        disabled={status === "applying" || personas.every((persona) => persona.tenantId !== tenantId)}
        onChange={(event) => {
          const nextUserId = event.target.value;
          const nextPersona = personas.find(
            (persona) => persona.tenantId === tenantId && persona.userId === nextUserId
          );
          setUserId(nextUserId);
          if (nextPersona?.roleName) {
            setRole(nextPersona.roleName);
          }
        }}
      >
        {personas
          .filter((persona) => persona.tenantId === tenantId)
          .map((persona) => (
            <option key={persona.userId} value={persona.userId}>
              {persona.displayName} ({persona.roleName})
            </option>
          ))}
      </select>
      <label htmlFor="development-test-role">Switch role</label>
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
        {status === "applying" ? "Applying" : "Apply context"}
      </Button>
      {message ? (
        <span className="tenant-workspace-selector__error" role="alert">
          {message}
        </span>
      ) : null}
    </form>
  );
}
