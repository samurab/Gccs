import { useEffect, useState, type FormEvent } from "react";
import {
  createLaborAssignment,
  createLaborCategory,
  deactivateLaborAssignment,
  deactivateLaborCategory,
  getContractLaborAssignments,
  getContractLaborCategories,
  getLaborClassificationEmployees,
  reclassifyLaborAssignment,
  reviewLaborAssignment,
  type LaborCategory,
  type LaborCategoryRequest,
  type LaborEmployeeAssignment,
  type LaborEmployeeAssignmentRequest,
  type LaborEmployeeOption
} from "@/lib/api";

const categoryDraft = (contractId: string): LaborCategoryRequest => ({
  contractId, title: "", wageDeterminationClassification: "", hourlyWage: 0, fringeRate: 0,
  fringeDescription: "", effectiveStart: "", effectiveEnd: null, sourceReference: ""
});
const assignmentDraft = (contractId: string): LaborEmployeeAssignmentRequest => ({
  employeeId: "", contractId, categoryId: "", workLocation: "", effectiveStart: "",
  effectiveEnd: null, sourceReference: "", evidenceItemIds: []
});

export function LaborClassificationPanel({ contractId, canManage, canViewSensitive }: {
  contractId: string; canManage: boolean; canViewSensitive: boolean;
}) {
  const [categories, setCategories] = useState<LaborCategory[]>([]);
  const [assignments, setAssignments] = useState<LaborEmployeeAssignment[]>([]);
  const [employees, setEmployees] = useState<LaborEmployeeOption[]>([]);
  const [category, setCategory] = useState(() => categoryDraft(contractId));
  const [assignment, setAssignment] = useState(() => assignmentDraft(contractId));
  const [state, setState] = useState<"loading" | "ready" | "saving" | "error">("loading");
  const [message, setMessage] = useState("");
  const [reclassify, setReclassify] = useState<Record<string, { categoryId: string; reason: string }>>({});
  const [reviewNotes, setReviewNotes] = useState<Record<string, string>>({});

  useEffect(() => {
    let active = true;
    Promise.all([
      getContractLaborCategories(contractId),
      getContractLaborAssignments(contractId),
      canViewSensitive ? getLaborClassificationEmployees() : Promise.resolve([])
    ]).then(([nextCategories, nextAssignments, nextEmployees]) => {
      if (!active) return;
      setCategories(Array.isArray(nextCategories) ? nextCategories : []);
      setAssignments(Array.isArray(nextAssignments) ? nextAssignments : []);
      setEmployees(Array.isArray(nextEmployees) ? nextEmployees : []);
      setCategory(categoryDraft(contractId)); setAssignment(assignmentDraft(contractId)); setState("ready");
    }).catch(() => {
      if (active) { setMessage("Labor categories and employee classifications could not be loaded."); setState("error"); }
    });
    return () => { active = false; };
  }, [contractId, canViewSensitive]);

  const setCategoryField = <K extends keyof LaborCategoryRequest>(key: K, value: LaborCategoryRequest[K]) =>
    setCategory(current => ({ ...current, [key]: value }));
  const setAssignmentField = <K extends keyof LaborEmployeeAssignmentRequest>(key: K, value: LaborEmployeeAssignmentRequest[K]) =>
    setAssignment(current => ({ ...current, [key]: value }));

  async function addCategory(event: FormEvent) {
    event.preventDefault(); setState("saving"); setMessage("");
    const result = await createLaborCategory(contractId, category);
    if (!result.data) { setState("error"); setMessage(result.error ?? "Labor category could not be created."); return; }
    setCategories(current => [...current, result.data!]); setCategory(categoryDraft(contractId));
    setState("ready"); setMessage("Labor category created and audit logged.");
  }

  async function addAssignment(event: FormEvent) {
    event.preventDefault(); setState("saving"); setMessage("");
    const result = await createLaborAssignment(contractId, assignment);
    if (!result.data) { setState("error"); setMessage(result.error ?? "Employee classification could not be created."); return; }
    setAssignments(current => [...current, result.data!]); setAssignment(assignmentDraft(contractId));
    setState("ready"); setMessage("Employee classification created and queued for review.");
  }

  async function deactivateCategory(categoryId: string) {
    setState("saving"); setMessage("");
    const result = await deactivateLaborCategory(contractId, categoryId);
    if (!result.data) { setState("error"); setMessage(result.error ?? "Labor category could not be deactivated."); return; }
    setCategories(current => current.map(item => item.id === categoryId ? result.data! : item)); setState("ready");
  }

  async function deactivateAssignment(assignmentId: string) {
    setState("saving"); setMessage("");
    const result = await deactivateLaborAssignment(contractId, assignmentId);
    if (!result.data) { setState("error"); setMessage(result.error ?? "Employee classification could not be deactivated."); return; }
    setAssignments(current => current.map(item => item.id === assignmentId ? result.data! : item)); setState("ready");
  }

  async function submitReclassification(item: LaborEmployeeAssignment) {
    const draft = reclassify[item.id] ?? { categoryId: "", reason: "" };
    if (!draft.categoryId || !draft.reason.trim()) { setState("error"); setMessage("Select a new category and enter a reclassification reason."); return; }
    setState("saving"); setMessage("");
    const result = await reclassifyLaborAssignment(contractId, item.id, draft.categoryId, draft.reason);
    if (!result.data) { setState("error"); setMessage(result.error ?? "Employee classification could not be changed."); return; }
    setAssignments(current => current.map(existing => existing.id === item.id ? result.data! : existing));
    setState("ready"); setMessage("Classification changed; prior and new categories were preserved in history.");
  }

  async function submitReview(item: LaborEmployeeAssignment, status: "Reviewed" | "Rejected") {
    const notes = reviewNotes[item.id] ?? "";
    if (!notes.trim()) { setState("error"); setMessage("Review notes are required."); return; }
    setState("saving"); setMessage("");
    const result = await reviewLaborAssignment(contractId, item.id, status, notes);
    if (!result.data) { setState("error"); setMessage(result.error ?? "Classification review could not be recorded."); return; }
    setAssignments(current => current.map(existing => existing.id === item.id ? result.data! : existing));
    setState("ready"); setMessage(`Classification ${status.toLowerCase()} with reviewer metadata.`);
  }

  return <section className="contract-esrs workflow-control-surface" aria-labelledby="labor-classification-heading">
    <div className="contract-documents__header"><div><span>Labor classifications</span><strong id="labor-classification-heading">{assignments.length}</strong></div></div>
    <p>Track source-backed wage, fringe, and worker-classification evidence. FeDril organizes review work and does not make legal labor determinations.</p>
    <p>Do not enter CUI, classified, export-controlled/ITAR, or sensitive government-furnished information.</p>
    {state === "loading" ? <p role="status">Loading labor classifications…</p> : null}
    {state === "error" ? <p role="alert">{message}</p> : message ? <p role="status">{message}</p> : null}

    <h3>Labor categories</h3>
    {categories.length === 0 && state !== "loading" ? <p>No labor categories recorded for this contract.</p> : null}
    {categories.map(item => <article key={item.id}>
      <strong>{item.title} · {item.isActive ? "Active" : "Inactive"}</strong>
      <span>{item.wageDeterminationClassification} · ${item.hourlyWage.toFixed(2)} wage · ${item.fringeRate.toFixed(2)} fringe</span>
      <small>{item.effectiveStart} to {item.effectiveEnd ?? "open ended"} · Source: {item.sourceReference}</small>
      {canManage && item.isActive ? <button type="button" onClick={() => void deactivateCategory(item.id)}>Deactivate category</button> : null}
    </article>)}
    {canManage ? <form onSubmit={addCategory}>
      <h3>Create labor category</h3>
      <label>Category title<input required maxLength={240} value={category.title} onChange={e => setCategoryField("title", e.target.value)} /></label>
      <label>Wage determination classification<input required maxLength={240} value={category.wageDeterminationClassification} onChange={e => setCategoryField("wageDeterminationClassification", e.target.value)} /></label>
      <label>Hourly wage<input required min="0" step="0.01" type="number" value={category.hourlyWage} onChange={e => setCategoryField("hourlyWage", Number(e.target.value))} /></label>
      <label>Fringe rate<input required min="0" step="0.01" type="number" value={category.fringeRate} onChange={e => setCategoryField("fringeRate", Number(e.target.value))} /></label>
      <label>Fringe information<textarea maxLength={1000} value={category.fringeDescription} onChange={e => setCategoryField("fringeDescription", e.target.value)} /></label>
      <label>Effective start<input required type="date" value={category.effectiveStart} onChange={e => setCategoryField("effectiveStart", e.target.value)} /></label>
      <label>Effective end<input type="date" value={category.effectiveEnd ?? ""} onChange={e => setCategoryField("effectiveEnd", e.target.value || null)} /></label>
      <label>Source reference<input required maxLength={500} value={category.sourceReference} onChange={e => setCategoryField("sourceReference", e.target.value)} /></label>
      <button disabled={state === "saving"} type="submit">Create labor category</button>
    </form> : <p>You have read-only access to labor categories.</p>}

    <h3>Employee assignments</h3>
    {assignments.length === 0 && state !== "loading" ? <p>No employee classifications recorded for this contract.</p> : null}
    {assignments.map(item => <article key={item.id}>
      <strong>{item.employeeName ?? `Restricted employee ${item.employeeId}`} · {item.laborCategoryTitle}</strong>
      {item.employeeEmail ? <span>{item.employeeEmail}</span> : <small>Sensitive employee fields are restricted.</small>}
      <small>{item.workLocation} · {item.effectiveStart} to {item.effectiveEnd ?? "open ended"} · {item.status}</small>
      <small>Source: {item.sourceReference} · Review: {item.reviewStatus}</small>
      {item.history.map(change => <small key={change.id}>History: {change.priorCategoryTitle ?? "Unclassified"} → {change.newCategoryTitle} · {change.reason} · {change.changedAt}</small>)}
      {canManage ? <div className="contract-esrs__actions">
        {item.status === "Active" ? <button type="button" onClick={() => void deactivateAssignment(item.id)}>Deactivate assignment</button> : null}
        <label>New category<select value={reclassify[item.id]?.categoryId ?? ""} onChange={e => setReclassify(current => ({ ...current, [item.id]: { categoryId: e.target.value, reason: current[item.id]?.reason ?? "" } }))}>
          <option value="">Select active category</option>{categories.filter(categoryItem => categoryItem.isActive && categoryItem.id !== item.categoryId).map(categoryItem => <option key={categoryItem.id} value={categoryItem.id}>{categoryItem.title}</option>)}
        </select></label>
        <label>Reclassification reason<input maxLength={1000} value={reclassify[item.id]?.reason ?? ""} onChange={e => setReclassify(current => ({ ...current, [item.id]: { categoryId: current[item.id]?.categoryId ?? "", reason: e.target.value } }))} /></label>
        <button type="button" onClick={() => void submitReclassification(item)}>Reclassify</button>
        <label>Review notes<textarea maxLength={1000} value={reviewNotes[item.id] ?? ""} onChange={e => setReviewNotes(current => ({ ...current, [item.id]: e.target.value }))} /></label>
        <button type="button" onClick={() => void submitReview(item, "Reviewed")}>Mark reviewed</button>
        <button type="button" onClick={() => void submitReview(item, "Rejected")}>Reject classification</button>
      </div> : null}
    </article>)}
    {canManage && canViewSensitive ? <form onSubmit={addAssignment}>
      <h3>Assign employee classification</h3>
      <label>Employee<select required value={assignment.employeeId} onChange={e => setAssignmentField("employeeId", e.target.value)}><option value="">Select employee</option>{employees.map(employee => <option key={employee.id} value={employee.id}>{employee.employeeNumber} — {employee.name}</option>)}</select></label>
      <label>Labor category<select required value={assignment.categoryId} onChange={e => setAssignmentField("categoryId", e.target.value)}><option value="">Select active category</option>{categories.filter(item => item.isActive).map(item => <option key={item.id} value={item.id}>{item.title}</option>)}</select></label>
      <label>Work location<input required maxLength={240} value={assignment.workLocation} onChange={e => setAssignmentField("workLocation", e.target.value)} /></label>
      <label>Effective start<input required type="date" value={assignment.effectiveStart} onChange={e => setAssignmentField("effectiveStart", e.target.value)} /></label>
      <label>Effective end<input type="date" value={assignment.effectiveEnd ?? ""} onChange={e => setAssignmentField("effectiveEnd", e.target.value || null)} /></label>
      <label>Source reference<input required maxLength={500} value={assignment.sourceReference} onChange={e => setAssignmentField("sourceReference", e.target.value)} /></label>
      <button disabled={state === "saving"} type="submit">Assign employee</button>
    </form> : canManage ? <p>Employee assignment requires sensitive-employee-data permission.</p> : <p>You have read-only access to employee classifications.</p>}
  </section>;
}
