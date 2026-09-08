import { useEffect, useState } from "react";
import { getClassifiedNote, getClassifiedNotes, saveClassifiedNote, type ClassifiedNote } from "@/lib/api";

export function ClassifiedNotesPanel({ canManage }: { canManage: boolean }) {
  const [notes, setNotes] = useState<ClassifiedNote[]>([]);
  const [selected, setSelected] = useState<ClassifiedNote | null>(null);
  const [title, setTitle] = useState("");
  const [body, setBody] = useState("");
  const [classification, setClassification] = useState("");
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("Loading notes…");
  useEffect(() => {
    let active = true;
    getClassifiedNotes().then(items => { if (active) { setNotes(items); setMessage(items.length ? "" : "No classified notes yet."); } })
      .catch(() => { if (active) setMessage("Notes could not be loaded. Check your access and retry."); });
    return () => { active = false; };
  }, []);
  async function open(id: string) {
    setBusy(true);
    try { const note = await getClassifiedNote(id); setSelected(note); setTitle(note.title); setBody(note.body); setClassification(note.classification.classification); setMessage(""); }
    catch { setMessage("This note could not be opened. It may be restricted or unavailable."); }
    finally { setBusy(false); }
  }
  return <section aria-label="Classified notes" className="upload-panel">
    <h3>Classified notes</h3>
    <p>No-CUI restrictions apply to note text. Unknown notes need review before downstream use.</p>
    <ul>{notes.map(note => <li key={note.id}><button type="button" disabled={busy} onClick={() => void open(note.id)}>{note.title}</button> <span>{note.classification.classification}</span></li>)}</ul>
    {canManage ? <form onSubmit={async event => {
      event.preventDefault(); if (busy || !classification) return; setBusy(true);
      try {
        const result = await saveClassifiedNote(selected?.id ?? null, title, body, classification, selected?.revision ?? 0);
        if (result.data) { setSelected(result.data); setNotes(await getClassifiedNotes()); setMessage("Note saved."); }
        else setMessage(result.error ?? "Note save failed.");
      } catch { setMessage("Note save failed. Reload before retrying."); } finally { setBusy(false); }
    }}>
      <label>Note title<input required maxLength={240} value={title} onChange={e => setTitle(e.target.value)} disabled={busy} /></label>
      <label>Note text<textarea required maxLength={20000} value={body} onChange={e => setBody(e.target.value)} disabled={busy} /></label>
      <label>Note classification<select required value={classification} disabled={busy || selected !== null} onChange={e => setClassification(e.target.value)}>
        <option value="">Select classification</option>{["Unclassified", "Fci", "Cui", "Unknown", "Prohibited"].map(value => <option key={value}>{value}</option>)}
      </select></label>
      {selected && <p>Reclassification requires an authorized classification reviewer.</p>}
      <button disabled={busy || !classification} type="submit">{busy ? "Saving…" : "Save note"}</button>
      <button type="button" disabled={busy} onClick={() => { setSelected(null); setTitle(""); setBody(""); setClassification(""); }}>New note</button>
    </form> : <p>Your role can view notes but cannot save changes.</p>}
    {!canManage && selected && <p>{selected.body}</p>}
    <p role="status">{message}</p>
  </section>;
}
