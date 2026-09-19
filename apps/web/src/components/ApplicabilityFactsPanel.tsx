import { RefreshCw } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { getApplicabilityFacts, type ApplicabilityFact } from "@/lib/api";
import { formatUsDateTime } from "@/lib/dateFormat";

type ApplicabilityFactsPanelProps = {
  contractId: string;
  canView: boolean;
};

export function ApplicabilityFactsPanel({ contractId, canView }: ApplicabilityFactsPanelProps) {
  const [facts, setFacts] = useState<ApplicabilityFact[]>([]);
  const [status, setStatus] = useState<"idle" | "loading" | "ready" | "failed">("idle");
  const [error, setError] = useState("");

  const loadFacts = useCallback(async () => {
    try {
      setFacts(await getApplicabilityFacts(contractId));
      setStatus("ready");
    } catch (requestError) {
      setStatus("failed");
      setError(requestError instanceof Error ? requestError.message : "Applicability facts could not be loaded.");
    }
  }, [contractId]);

  function refreshFacts() {
    setStatus("loading");
    setError("");
    void loadFacts();
  }

  useEffect(() => {
    if (!canView) {
      return;
    }

    let active = true;
    void getApplicabilityFacts(contractId).then(
      nextFacts => {
        if (!active) {
          return;
        }
        setFacts(nextFacts);
        setStatus("ready");
      },
      requestError => {
        if (!active) {
          return;
        }
        setStatus("failed");
        setError(requestError instanceof Error ? requestError.message : "Applicability facts could not be loaded.");
      }
    );

    return () => {
      active = false;
    };
  }, [canView, contractId]);

  if (!canView) {
    return null;
  }

  return (
    <section className="applicability-facts-panel" aria-labelledby="applicability-facts-heading">
      <div className="applicability-facts-panel__header">
        <div>
          <p className="eyebrow">Derived context</p>
          <h3 id="applicability-facts-heading">Applicability facts</h3>
          <p>Read-only facts derived from the company profile and this contract.</p>
        </div>
        <button
          className="secondary-action"
          type="button"
          onClick={refreshFacts}
          disabled={status === "loading"}
          title="Refresh applicability facts"
        >
          <RefreshCw size={16} aria-hidden="true" />
          Refresh facts
        </button>
      </div>

      {status === "idle" || status === "loading" ? <p className="form-status">Loading applicability facts...</p> : null}
      {status === "failed" ? <p className="form-status form-status--error" role="alert">{error}</p> : null}
      {status === "ready" && facts.length === 0 ? (
        <p className="muted-inline">No derived applicability facts are available for this contract.</p>
      ) : null}

      {facts.length > 0 ? (
        <div className="applicability-facts-table-wrap">
          <table className="applicability-facts-table">
            <caption className="sr-only">Applicability facts derived for the selected contract</caption>
            <thead>
              <tr>
                <th scope="col">Fact</th>
                <th scope="col">Value</th>
                <th scope="col">Source</th>
                <th scope="col">Last updated</th>
              </tr>
            </thead>
            <tbody>
              {facts.map((fact) => (
                <tr key={`${fact.key}:${fact.sourceType}:${fact.sourceId}`}>
                  <th scope="row">{fact.key}</th>
                  <td>
                    <span className={fact.isUnknown ? "applicability-fact-value applicability-fact-value--unknown" : "applicability-fact-value"}>
                      {fact.isUnknown ? "Unknown" : fact.value}
                    </span>
                  </td>
                  <td>
                    <span>{fact.sourceType}</span>
                    <small>{fact.sourceId}</small>
                  </td>
                  <td>{formatUsDateTime(fact.lastUpdatedAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </section>
  );
}
