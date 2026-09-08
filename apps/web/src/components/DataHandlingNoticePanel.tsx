import { useEffect, useRef, useState } from "react";
import { acknowledgeDataHandlingNotice, getDataHandlingNoticeAcknowledgements, getPublishedDataHandlingNotice, type DataHandlingNotice } from "@/lib/api";

const workflowLabels: Record<string, string> = {
  Onboarding: "Onboarding",
  EvidenceUpload: "Evidence upload",
  ContractUpload: "Contract upload",
  ClassifiedNote: "Classified notes",
  ReportGeneration: "Report generation",
  ExtractionJob: "Extraction",
  Support: "Support escalation"
};

export function DataHandlingNoticePanel({ tenantId, mode, workflowContext }: { tenantId: string; mode: string; workflowContext: string }) {
  const [notice, setNotice] = useState<DataHandlingNotice | null>(null);
  const [accepted, setAccepted] = useState(false);
  const [checked, setChecked] = useState(false);
  const [status, setStatus] = useState("Loading current notice…");
  const [saving, setSaving] = useState(false);
  const [open, setOpen] = useState(false);
  const [revision, setRevision] = useState(0);
  const noticeRevision = useRef(0);

  useEffect(() => {
    function renew(event: Event) {
      const context = (event as CustomEvent<{ workflowContext?: string }>).detail?.workflowContext;
      if (context !== workflowContext) return;
      noticeRevision.current++;
      setNotice(null); setAccepted(false); setChecked(false); setOpen(true);
      setStatus("A current notice acknowledgement is needed. Review the notice, acknowledge it, then retry your action.");
      setRevision(value => value + 1);
    }
    window.addEventListener("fedril:notice-required", renew);
    return () => window.removeEventListener("fedril:notice-required", renew);
  }, [workflowContext]);

  useEffect(() => {
    let active = true;
    void Promise.all([
      getPublishedDataHandlingNotice(mode, workflowContext),
      getDataHandlingNoticeAcknowledgements(tenantId, mode, workflowContext)
    ]).then(([current, history]) => {
      if (!active) return;
      const matching = current?.mode === mode ? current : null;
      setNotice(matching);
      setAccepted(Boolean(matching && history.some(item => item.tenantId === tenantId && item.mode === matching.mode &&
        item.workflowContext === workflowContext && item.noticeId === matching.noticeId && item.noticeVersion === matching.version && item.status === "Current")));
      setStatus(matching ? "" : "The current mode-specific notice could not be loaded. Refresh before continuing.");
    }).catch(() => { if (active) setStatus("The current notice could not be loaded."); });
    return () => { active = false; };
  }, [tenantId, mode, workflowContext, revision]);

  async function acknowledge() {
    if (!notice || !checked || saving) return;
    const submittedRevision = noticeRevision.current;
    setSaving(true);
    const result = await acknowledgeDataHandlingNotice(tenantId, {
      mode: notice.mode, workflowContext, noticeId: notice.noticeId, noticeVersion: notice.version, acknowledged: true
    });
    setSaving(false);
    if (submittedRevision !== noticeRevision.current) return;
    if (result.data) {
      setAccepted(true);
      setStatus("Acknowledgement recorded. Retry your original action if it was interrupted.");
    } else {
      setStatus(result.error ?? "Acknowledgement was not recorded. Reload the current notice and retry.");
    }
  }

  return <details className="posture-notice data-handling-notice" open={open} onToggle={event => setOpen(event.currentTarget.open)}>
    <summary>{workflowLabels[workflowContext] ?? workflowContext} data handling notice</summary>
    <div className="data-handling-notice-body">
    {notice && <>
      <h3>{notice.title}</h3><p>Version {notice.version} · {notice.mode}</p><p>{notice.body}</p>
      {accepted ? <p>Current notice acknowledged for this workflow.</p> : <>
        <label><input type="checkbox" checked={checked} disabled={saving} onChange={event => setChecked(event.target.checked)} /> I have read and acknowledge this notice.</label>
        <button type="button" disabled={!checked || saving} onClick={() => void acknowledge()}>{saving ? "Recording…" : "Acknowledge current notice"}</button>
      </>}
    </>}
    {status && <p role="status">{status}</p>}
    </div>
  </details>;
}
