import { useEffect, useRef, useState } from "react";
import { acknowledgeDataHandlingNotice, getDataHandlingNoticeAcknowledgements, getPublishedDataHandlingNotice, type DataHandlingNotice } from "@/lib/api";

export function DataHandlingNoticePanel({ tenantId, mode }: { tenantId: string; mode: string }) {
  const [workflow, setWorkflow] = useState("EvidenceUpload");
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
      noticeRevision.current++;
      const context = (event as CustomEvent<{ workflowContext?: string }>).detail?.workflowContext;
      if (context && ["EvidenceUpload", "ContractIntake", "ReportGeneration", "Onboarding", "Support"].includes(context)) setWorkflow(context);
      setNotice(null); setAccepted(false); setChecked(false); setOpen(true);
      setStatus("A current notice acknowledgement is needed. Review the notice, acknowledge it, then retry your action.");
      setRevision(value => value + 1);
    }
    window.addEventListener("fedril:notice-required", renew);
    return () => window.removeEventListener("fedril:notice-required", renew);
  }, []);

  useEffect(() => {
    let active = true;
    void Promise.all([
      getPublishedDataHandlingNotice(mode, workflow),
      getDataHandlingNoticeAcknowledgements(tenantId, mode, workflow)
    ]).then(([current, history]) => {
      if (!active) return;
      setNotice(current);
      setAccepted(Boolean(current && history.some(item => item.tenantId === tenantId && item.mode === current.mode &&
        item.workflowContext === workflow && item.noticeId === current.noticeId && item.noticeVersion === current.version && item.status === "Current")));
      setStatus(current ? "" : "The current notice could not be loaded. Refresh before acknowledging.");
    }).catch(() => { if (active) setStatus("The current notice could not be loaded."); });
    return () => { active = false; };
  }, [tenantId, mode, workflow, revision]);

  async function acknowledge() {
    if (!notice || !checked || saving) return;
    const submittedRevision = noticeRevision.current;
    setSaving(true);
    const result = await acknowledgeDataHandlingNotice(tenantId, {
      mode: notice.mode, workflowContext: workflow, noticeId: notice.noticeId, noticeVersion: notice.version, acknowledged: true
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

  return <details className="posture-notice" open={open} onToggle={event => setOpen(event.currentTarget.open)}>
    <summary>Current data handling notices</summary>
    <label>Notice workflow <select value={workflow} disabled={saving} onChange={event => {
      noticeRevision.current++;
      setNotice(null); setAccepted(false); setChecked(false); setStatus("Loading current notice…"); setWorkflow(event.target.value);
    }}>
      <option value="EvidenceUpload">Evidence</option>
      <option value="ContractIntake">Contracts and extraction</option>
      <option value="ReportGeneration">Reports</option>
      <option value="Onboarding">General data handling</option>
      <option value="Support">Support</option>
    </select></label>
    {notice && <>
      <h3>{notice.title}</h3><p>Version {notice.version} · {notice.mode}</p><p>{notice.body}</p>
      {accepted ? <p>Current notice acknowledged for this workflow.</p> : <>
        <label><input type="checkbox" checked={checked} disabled={saving} onChange={event => setChecked(event.target.checked)} /> I have read and acknowledge this notice.</label>
        <button type="button" disabled={!checked || saving} onClick={() => void acknowledge()}>{saving ? "Recording…" : "Acknowledge current notice"}</button>
      </>}
    </>}
    {status && <p role="status">{status}</p>}
  </details>;
}
