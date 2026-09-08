import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { ClassifiedNotesPanel } from "./ClassifiedNotesPanel";
import { getClassifiedNotes, saveClassifiedNote } from "@/lib/api";
vi.mock("@/lib/api", () => ({ getClassifiedNotes: vi.fn(), getClassifiedNote: vi.fn(), saveClassifiedNote: vi.fn() }));
beforeEach(() => { vi.mocked(getClassifiedNotes).mockReset().mockResolvedValue([]); vi.mocked(saveClassifiedNote).mockReset(); });
afterEach(cleanup);
it("requires a selected classification before note save", async () => {
  render(<ClassifiedNotesPanel canManage />); const user = userEvent.setup();
  await screen.findByText("No classified notes yet.");
  await user.type(screen.getByLabelText("Note title"), "Synthetic");
  await user.type(screen.getByLabelText("Note text"), "Synthetic note");
  expect(screen.getByRole("button", { name: "Save note" })).toBeDisabled();
  await user.selectOptions(screen.getByLabelText("Note classification"), "Unclassified");
  vi.mocked(saveClassifiedNote).mockResolvedValue({ data: null, error: "Current notice required" });
  await user.click(screen.getByRole("button", { name: "Save note" }));
  expect(saveClassifiedNote).toHaveBeenCalledWith(null, "Synthetic", "Synthetic note", "Unclassified", 0);
  expect(await screen.findByText("Current notice required")).toBeVisible();
});
it("does not expose note writes to a read-only user", async () => {
  render(<ClassifiedNotesPanel canManage={false} />);
  await screen.findByText("No classified notes yet.");
  expect(screen.queryByRole("button", { name: "Save note" })).not.toBeInTheDocument();
});
it("reports loading failure rather than claiming an empty list", async () => {
  vi.mocked(getClassifiedNotes).mockRejectedValue(new Error("Unavailable"));
  render(<ClassifiedNotesPanel canManage={false} />);
  expect(await screen.findByText(/Notes could not be loaded/)).toBeVisible();
});
