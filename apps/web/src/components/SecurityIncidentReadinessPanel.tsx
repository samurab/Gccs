import { useEffect, useState } from "react";
import { approveIncidentReadiness, approveSecurityReviewReadiness, approveTechnicalReadiness, getCurrentUserAccess, getIncidentReadiness, getSecurityIncidentReadinessHistory,
  getSecurityReviewReadiness, getTechnicalReadiness, saveSecurityReviewReadiness, type IncidentReadinessRecord,
  saveIncidentReadiness, saveTechnicalReadiness, type ReadinessHistory, type SecurityReviewItem, type SecurityReviewRecord, type TechnicalReadinessRecord } from "@/lib/api";

const areas = ["tenant-isolation","evidence-storage","encryption","malware-scanning","retention","backup","restore","admin-access","support-access","antitrust-procurement-integrity","logging","monitoring","incident-response"];
const defaultReviewDueAt = new Date(Date.now()+365*86400000).toISOString().slice(0,10);

export function SecurityIncidentReadinessPanel({ userId }: { userId: string | null }) {
  const [security, setSecurity] = useState<SecurityReviewRecord | null>(null);
  const [technical, setTechnical] = useState<TechnicalReadinessRecord | null>(null);
  const [incident, setIncident] = useState<IncidentReadinessRecord | null>(null);
  const [history, setHistory] = useState<ReadinessHistory[]>([]);
  const [canApprove, setCanApprove] = useState(false);
  const [loading, setLoading] = useState(true); const [busy, setBusy] = useState(false); const [message, setMessage] = useState("");
  const [items, setItems] = useState<SecurityReviewItem[]>([]); const [approvalNotes, setApprovalNotes] = useState("");
  const [findings,setFindings]=useState<SecurityReviewRecord["findings"]>([]);const [risks,setRisks]=useState<SecurityReviewRecord["acceptedRisks"]>([]);
  async function load() {
    setLoading(true);
    try {
      const [s,t,i,h,a] = await Promise.all([getSecurityReviewReadiness(),getTechnicalReadiness(),getIncidentReadiness(),getSecurityIncidentReadinessHistory(),getCurrentUserAccess()]);
      setSecurity(s); setTechnical(t); setIncident(i); setHistory(h); setCanApprove(a.canApproveCuiReadiness === true);
      setItems(s?.items ?? areas.map(area => ({ area, status:"NotStarted", reviewerUserId:null, reviewedAt:null, evidenceLink:null, rationale:null })));
      setFindings(s?.findings??[]);setRisks(s?.acceptedRisks??[]);
      setMessage("");
    } catch (error) { setMessage(error instanceof Error ? error.message : "Readiness records could not be loaded."); }
    finally { setLoading(false); }
  }
  useEffect(() => { const timer=window.setTimeout(()=>void load(),0); return ()=>window.clearTimeout(timer); }, []);
  function update(area: string, patch: Partial<SecurityReviewItem>) { setItems(current => current.map(item => item.area === area ? {...item,...patch} : item)); }
  async function save() {
    setBusy(true); setMessage("");
    const normalized = items.map(item => ({...item, reviewerUserId: ["Passed","AcceptedRisk"].includes(item.status) ? userId : null,
      reviewedAt: ["Passed","AcceptedRisk"].includes(item.status) ? new Date().toISOString().slice(0,10) : null }));
    const result = await saveSecurityReviewReadiness({ expectedVersion: security?.version ?? 0, items: normalized,
      findings, acceptedRisks: risks });
    setBusy(false); if (!result.data) { setMessage(result.error ?? "Security review was not saved."); return; }
    setMessage("Security review version saved. Approval remains separate."); await load();
  }
  async function approve() {
    if (!security) return; setBusy(true); const result=await approveSecurityReviewReadiness(security.version,approvalNotes); setBusy(false);
    if (!result.data) { setMessage(result.error ?? "Security review was not approved."); return; } setApprovalNotes(""); await load();
  }
  return <section className="members-section" aria-label="Security and incident readiness">
    <div className="section-heading section-heading--split"><div><p className="eyebrow">CUI readiness controls</p><h2>Security and incident readiness</h2>
      <p className="section-summary">Versioned reviews, executed control evidence, incident exercises, and approvals. Only current approved records can satisfy the CUI-ready gate.</p></div>
      <button type="button" onClick={() => void load()} disabled={loading || busy}>Refresh</button></div>
    {message && <p role="status" className="form-status">{message}</p>}
    {loading ? <p role="status">Loading readiness records…</p> : <div className="readiness-record-grid">
      <article className="evidence-list__item"><div className="section-heading--split"><h3>Security review</h3><span className={`status status--${(security?.state ?? "draft").toLowerCase()}`}>{security?.state ?? "Not created"}</span></div>
        <p>{items.filter(i=>i.status==="Passed"||i.status==="AcceptedRisk").length} of {areas.length} checks ready · {security?.findings.filter(f=>f.status==="Open"&&(f.severity==="High"||f.severity==="Critical")).length ?? 0} blocking findings</p>
        <div className="readiness-check-grid"><div className="readiness-check-grid__header"><strong>Review area</strong><strong>Status</strong><strong>Evidence reference</strong><strong>Rationale</strong></div>{items.map(item => <div className="readiness-check-grid__row" key={item.area}><strong>{item.area.replaceAll("-"," ")}</strong>
          <label><span className="sr-only">{item.area} status</span><select value={item.status} disabled={busy} onChange={e=>update(item.area,{status:e.target.value as SecurityReviewItem["status"]})}>
            {["NotStarted","InReview","Passed","FindingOpen","AcceptedRisk"].map(x=><option key={x}>{x}</option>)}</select></label>
          <label><span className="sr-only">{item.area} evidence reference</span><input maxLength={600} disabled={busy} value={item.evidenceLink ?? ""} onChange={e=>update(item.area,{evidenceLink:e.target.value})}/></label>
          <label><span className="sr-only">{item.area} rationale</span><input maxLength={1200} disabled={busy} value={item.rationale ?? ""} onChange={e=>update(item.area,{rationale:e.target.value})}/></label>
        </div>)}</div>
        <div className="form-actions"><button type="button" disabled={busy || !userId} onClick={()=>void save()}>Save new review version</button></div>
        <h4>Findings</h4>{findings.map((finding,index)=><div className="evidence-list__item readiness-child-editor" key={finding.id}><label>Area<select value={finding.area} onChange={e=>setFindings(v=>v.map((x,n)=>n===index?{...x,area:e.target.value}:x))}>{areas.map(x=><option key={x}>{x.replaceAll("-"," ")}</option>)}</select></label><label>Summary<input value={finding.summary} onChange={e=>setFindings(v=>v.map((x,n)=>n===index?{...x,summary:e.target.value}:x))}/></label><label>Severity<select value={finding.severity} onChange={e=>setFindings(v=>v.map((x,n)=>n===index?{...x,severity:e.target.value}:x))}>{["Low","Medium","High","Critical"].map(x=><option key={x}>{x}</option>)}</select></label><label>Status<select value={finding.status} onChange={e=>setFindings(v=>v.map((x,n)=>n===index?{...x,status:e.target.value}:x))}>{["Open","Closed","AcceptedRisk"].map(x=><option key={x}>{x}</option>)}</select></label><label>Remediation owner<input value={finding.remediationOwner} onChange={e=>setFindings(v=>v.map((x,n)=>n===index?{...x,remediationOwner:e.target.value}:x))}/></label><label>Due date<input type="date" value={finding.dueAt??""} onChange={e=>setFindings(v=>v.map((x,n)=>n===index?{...x,dueAt:e.target.value||null}:x))}/></label><label>Closure notes<textarea value={finding.closureNotes??""} onChange={e=>setFindings(v=>v.map((x,n)=>n===index?{...x,closureNotes:e.target.value||null}:x))}/></label></div>)}
        <button type="button" disabled={busy} onClick={()=>setFindings(v=>[...v,{id:crypto.randomUUID(),area:"tenant-isolation",severity:"Medium",status:"Open",summary:"",remediationOwner:"",dueAt:null,closureNotes:null}])}>Add finding</button>
        <h4>Accepted risks</h4>{risks.map((risk,index)=><div className="evidence-list__item readiness-child-editor" key={risk.id}><label>Related finding<select value={risk.findingId??""} onChange={e=>setRisks(v=>v.map((x,n)=>n===index?{...x,findingId:e.target.value||null}:x))}><option value="">General review risk</option>{findings.map(f=><option key={f.id} value={f.id}>{f.summary||f.area}</option>)}</select></label><label>Scope<textarea value={risk.scope} onChange={e=>setRisks(v=>v.map((x,n)=>n===index?{...x,scope:e.target.value}:x))}/></label><label>Review date<input type="date" value={risk.reviewAt??""} onChange={e=>setRisks(v=>v.map((x,n)=>n===index?{...x,reviewAt:e.target.value||null}:x))}/></label><label>Expiration date<input type="date" value={risk.expiresAt??""} onChange={e=>setRisks(v=>v.map((x,n)=>n===index?{...x,expiresAt:e.target.value||null}:x))}/></label><label>Mitigation<textarea value={risk.mitigationNote} onChange={e=>setRisks(v=>v.map((x,n)=>n===index?{...x,mitigationNote:e.target.value}:x))}/></label></div>)}
        <button type="button" disabled={busy||!userId} onClick={()=>setRisks(v=>[...v,{id:crypto.randomUUID(),findingId:null,approverUserId:userId!,acceptedAt:new Date().toISOString().slice(0,10),scope:"Define accepted-risk scope",expiresAt:null,reviewAt:new Date(Date.now()+90*86400000).toISOString().slice(0,10),mitigationNote:"Define mitigation and monitoring."}])}>Add accepted risk</button>
        {security && <form onSubmit={e=>{e.preventDefault();void approve();}}><label>Approval notes<textarea required maxLength={1200} value={approvalNotes} onChange={e=>setApprovalNotes(e.target.value)}/></label>
          <button disabled={busy || !canApprove || security.state!=="Draft" || !approvalNotes.trim()}>Approve security review</button>{!canApprove&&<p>Platform readiness approval permission is required.</p>}</form>}
      </article>
      <TechnicalEditor record={technical} userId={userId} canApprove={canApprove} busy={busy} setBusy={setBusy} setMessage={setMessage} reload={load} />
      <IncidentEditor record={incident} userId={userId} canApprove={canApprove} busy={busy} setBusy={setBusy} setMessage={setMessage} reload={load} />
    </div>}
    <details><summary>Readiness history</summary>{history.length===0?<p>No readiness history.</p>:<ul>{history.map(h=><li key={h.id}>{new Date(h.occurredAt).toLocaleString()} · {h.recordType} v{h.version} · {h.action}</li>)}</ul>}</details>
  </section>;
}

const controlTypes=["tenant-isolation","evidence-storage","malware-scanner","backup-restore","administrator-access","support-access"];
function TechnicalEditor({record,userId,canApprove,busy,setBusy,setMessage,reload}:{record:TechnicalReadinessRecord|null;userId:string|null;canApprove:boolean;busy:boolean;setBusy:(v:boolean)=>void;setMessage:(v:string)=>void;reload:()=>Promise<void>}) {
  const [environment,setEnvironment]=useState("staging");const [executedAt,setExecutedAt]=useState(new Date().toISOString().slice(0,10));const [references,setReferences]=useState<Record<string,string>>({});const [notes,setNotes]=useState("");
  async function save(){setBusy(true);const result=await saveTechnicalReadiness({expectedVersion:record?.version??0,evidence:controlTypes.map(controlType=>({id:null,controlType,executedAt,environment,reviewerUserId:userId,result:"Passed",evidenceReference:references[controlType]??"",expiresAt:null,notes:"Executed and reviewed control verification."}))});setBusy(false);if(!result.data){setMessage(result.error??"Technical readiness was not saved.");return;}await reload();}
  async function approve(){if(!record)return;setBusy(true);const result=await approveTechnicalReadiness(record.version,notes);setBusy(false);if(!result.data){setMessage(result.error??"Technical readiness was not approved.");return;}await reload();}
  return <article className="evidence-list__item"><h3>Technical control verification</h3><p>{record?.state??"Not created"} · {record?.evidence.length??0} executed control records</p>
    <label>Environment<input value={environment} onChange={e=>setEnvironment(e.target.value)}/></label><label>Execution date<input type="date" value={executedAt} onChange={e=>setExecutedAt(e.target.value)}/></label>
    {controlTypes.map(type=><label key={type}>{type.replaceAll("-"," ")} evidence reference<input value={references[type]??record?.evidence.find(x=>x.controlType===type)?.evidenceReference??""} onChange={e=>setReferences(v=>({...v,[type]:e.target.value}))}/></label>)}
    <button type="button" disabled={busy||!userId} onClick={()=>void save()}>Save executed controls</button>
    {record&&<><label>Approval notes<textarea value={notes} onChange={e=>setNotes(e.target.value)}/></label><button type="button" disabled={busy||!canApprove||record.state!=="Draft"||!notes.trim()} onClick={()=>void approve()}>Approve technical readiness</button></>}
  </article>;
}

function IncidentEditor({record,userId,canApprove,busy,setBusy,setMessage,reload}:{record:IncidentReadinessRecord|null;userId:string|null;canApprove:boolean;busy:boolean;setBusy:(v:boolean)=>void;setMessage:(v:string)=>void;reload:()=>Promise<void>}) {
  const keys=["accidental-cui-upload","suspected-cui-in-no-cui-tenant","prohibited-data-upload","cross-tenant-exposure-suspicion","malware-detection","failed-deletion-export-request"];
  const functions=["security","support","legal-compliance","engineering","customer-success"];
  const [owner,setOwner]=useState("");const [contacts,setContacts]=useState<Record<string,string>>({});const [trigger,setTrigger]=useState("");const [tabletopRef,setTabletopRef]=useState("");const [notes,setNotes]=useState("");
  const [reviewDueAt,setReviewDueAt]=useState(defaultReviewDueAt);
  const [reviewBasis,setReviewBasis]=useState("Annual");
  const [followUps,setFollowUps]=useState<IncidentReadinessRecord["followUps"]>([]);
  useEffect(()=>{const timer=window.setTimeout(()=>{if(!record)return;setOwner(record.playbooks[0]?.owner??record.contacts[0]?.escalationRole??"");setContacts(Object.fromEntries(record.contacts.map(x=>[x.function,x.contact])));setTrigger(record.playbooks[0]?.trigger??"");setTabletopRef(record.tabletops[0]?.evidenceReference??"");setReviewDueAt(record.reviewDueAt);setReviewBasis(record.reviewBasis);setFollowUps(record.followUps);},0);return()=>window.clearTimeout(timer);},[record]);
  async function save(){
    const tabletopId=record?.tabletops[0]?.id??crypto.randomUUID();setBusy(true);
    const result=await saveIncidentReadiness({expectedVersion:record?.version??0,reviewDueAt,reviewBasis,
      contacts:functions.map(fn=>({id:record?.contacts.find(x=>x.function===fn)?.id??null,function:fn,contact:contacts[fn]??"",escalationRole:owner})),
      playbooks:keys.map(key=>{const existing=record?.playbooks.find(x=>x.key===key);return {id:existing?.id??null,key,trigger:trigger||existing?.trigger||"",containmentSteps:existing?.containmentSteps??["Contain affected content and preserve tenant boundaries"],notificationPath:existing?.notificationPath??"Security, support, legal/compliance, engineering, and customer success",evidenceToCollect:existing?.evidenceToCollect??["Audit events and affected record identifiers"],owner,closureCriteria:existing?.closureCriteria??"Containment verified and follow-up actions assigned"};}),
      tabletops:[{id:tabletopId,executedAt:record?.tabletops[0]?.executedAt??new Date().toISOString().slice(0,10),environment:record?.tabletops[0]?.environment??"staging",participants:record?.tabletops[0]?.participants??[owner],findings:record?.tabletops[0]?.findings??["Exercise completed; follow-ups recorded"],evidenceReference:tabletopRef,reviewerUserId:record?.tabletops[0]?.reviewerUserId??userId}],
      followUps:followUps.map(x=>({...x,tabletopId}))});
    setBusy(false);if(!result.data){setMessage(result.error??"Incident readiness was not saved.");return;}await reload();
  }
  async function approve(){if(!record)return;setBusy(true);const result=await approveIncidentReadiness(record.version,notes);setBusy(false);if(!result.data){setMessage(result.error??"Incident readiness was not approved.");return;}await reload();}
  return <article className="evidence-list__item"><h3>Incident readiness</h3><p>{record?.state??"Not created"} · {record?.playbooks.length??0} playbooks · {record?.tabletops.length??0} exercises · {followUps.filter(f=>f.status==="Open").length} open follow-ups{record?` · ${record.reviewBasis} review due ${record.reviewDueAt}`:""}</p>
    <label>Escalation owner<input value={owner} onChange={e=>setOwner(e.target.value)}/></label>{functions.map(fn=><label key={fn}>{fn.replaceAll("-"," ")} contact<input value={contacts[fn]??""} onChange={e=>setContacts(v=>({...v,[fn]:e.target.value}))}/></label>)}
    <label>Review basis<select value={reviewBasis} onChange={e=>setReviewBasis(e.target.value)}><option>Annual</option><option>Release</option></select></label><label>Next review date<input type="date" value={reviewDueAt} onChange={e=>setReviewDueAt(e.target.value)}/></label>
    <label>Reviewed trigger criteria<textarea value={trigger} onChange={e=>setTrigger(e.target.value)}/></label><label>Executed tabletop evidence reference<input value={tabletopRef} onChange={e=>setTabletopRef(e.target.value)}/></label>
    <h4>Exercise follow-ups</h4>{followUps.map((item,index)=><div className="readiness-child-editor" key={item.id}><label>Summary<input value={item.summary} onChange={e=>setFollowUps(v=>v.map((x,n)=>n===index?{...x,summary:e.target.value}:x))}/></label><label>Severity<select value={item.severity} onChange={e=>setFollowUps(v=>v.map((x,n)=>n===index?{...x,severity:e.target.value}:x))}>{["Low","Medium","High","Critical"].map(x=><option key={x}>{x}</option>)}</select></label><label>Status<select value={item.status} onChange={e=>setFollowUps(v=>v.map((x,n)=>n===index?{...x,status:e.target.value}:x))}><option>Open</option><option>Closed</option></select></label><label>Owner<input value={item.owner} onChange={e=>setFollowUps(v=>v.map((x,n)=>n===index?{...x,owner:e.target.value}:x))}/></label><label>Due date<input type="date" value={item.dueAt} onChange={e=>setFollowUps(v=>v.map((x,n)=>n===index?{...x,dueAt:e.target.value}:x))}/></label><label>Closure notes<textarea value={item.closureNotes??""} onChange={e=>setFollowUps(v=>v.map((x,n)=>n===index?{...x,closureNotes:e.target.value||null}:x))}/></label></div>)}
    <button type="button" disabled={busy||!owner.trim()} onClick={()=>setFollowUps(v=>[...v,{id:crypto.randomUUID(),tabletopId:record?.tabletops[0]?.id??"",severity:"Medium",status:"Open",summary:"",owner,dueAt:new Date(Date.now()+30*86400000).toISOString().slice(0,10),closureNotes:null}])}>Add follow-up</button>
    <button type="button" disabled={busy||!userId||!owner.trim()||functions.some(fn=>!(contacts[fn]??"").trim())||!trigger.trim()||!tabletopRef.trim()||!reviewDueAt} onClick={()=>void save()}>Save playbooks and exercise</button>
    {record&&<><label>Approval notes<textarea value={notes} onChange={e=>setNotes(e.target.value)}/></label><button type="button" disabled={busy||!canApprove||record.state!=="Draft"||!notes.trim()} onClick={()=>void approve()}>Approve incident readiness</button></>}
  </article>;
}
