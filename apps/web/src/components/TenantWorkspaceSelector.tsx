import { LoaderCircle } from "lucide-react";
import { useEffect, useState } from "react";
import {
  getMyTenantWorkspaces,
  getSelectedTenantId,
  selectMyTenantWorkspace,
  selectTenant,
  type TenantWorkspace
} from "@/lib/api";

type TenantWorkspaceSelectorProps = {
  currentTenantId: string | null;
  onInitialized: () => void;
};

export function TenantWorkspaceSelector({
  currentTenantId,
  onInitialized
}: TenantWorkspaceSelectorProps) {
  const [workspaces, setWorkspaces] = useState<TenantWorkspace[]>([]);
  const [status, setStatus] = useState<"loading" | "ready" | "switching" | "error">("loading");
  const [message, setMessage] = useState("");
  const selectedTenantId = getSelectedTenantId() ?? currentTenantId ?? "";

  useEffect(() => {
    let isMounted = true;

    getMyTenantWorkspaces()
      .then(async (result) => {
        if (!isMounted) return;

        setWorkspaces(result.tenants);
        const selectable = result.tenants.filter((tenant) => tenant.isSelectable);
        const storedTenantId = getSelectedTenantId();
        const storedTenantIsValid = selectable.some((tenant) => tenant.tenantId === storedTenantId);
        const automaticTenantId = result.preferredTenantId ?? (selectable.length === 1 ? selectable[0].tenantId : null);

        if (storedTenantIsValid) {
          setStatus("ready");
          onInitialized();
          return;
        }

        if (automaticTenantId) {
          setStatus("switching");
          const selection = await selectMyTenantWorkspace(automaticTenantId);
          if (!isMounted) return;
          if (!selection.data) {
            setStatus("error");
            setMessage(selection.error ?? "Workspace selection was denied.");
            return;
          }

          selectTenant(selection.data.tenantId);
          setStatus("ready");
          onInitialized();
          return;
        }

        setStatus("ready");
      })
      .catch(() => {
        if (!isMounted) return;
        setStatus("error");
        setMessage("Workspace access could not be loaded.");
      });

    return () => {
      isMounted = false;
    };
  }, [onInitialized]);

  async function handleSelection(tenantId: string) {
    if (!tenantId || tenantId === selectedTenantId) return;

    setStatus("switching");
    setMessage("");
    const result = await selectMyTenantWorkspace(tenantId);
    if (!result.data) {
      setStatus("error");
      setMessage(result.error ?? "Workspace selection was denied.");
      return;
    }

    selectTenant(result.data.tenantId);
    window.location.reload();
  }

  if (status === "loading") {
    return (
      <div className="tenant-workspace-selector" role="status">
        <LoaderCircle size={16} className="spin" aria-hidden="true" />
        <span>Loading workspaces</span>
      </div>
    );
  }

  return (
    <div className="tenant-workspace-selector">
      <label htmlFor="tenant-workspace">Workspace</label>
      <select
        id="tenant-workspace"
        value={selectedTenantId}
        disabled={status === "switching" || workspaces.every((tenant) => !tenant.isSelectable)}
        onChange={(event) => void handleSelection(event.target.value)}
      >
        {!selectedTenantId ? <option value="">Select workspace</option> : null}
        {workspaces.map((tenant) => (
          <option key={tenant.tenantId} value={tenant.tenantId} disabled={!tenant.isSelectable}>
            {tenant.displayName}
            {tenant.isSelectable ? "" : ` (${tenant.unavailableReason ?? "Unavailable"})`}
          </option>
        ))}
      </select>
      {status === "switching" ? <span role="status">Switching workspace...</span> : null}
      {message ? (
        <span className="tenant-workspace-selector__error" role="alert">
          {message}
        </span>
      ) : null}
    </div>
  );
}
