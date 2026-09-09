import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { SspSectionsPanel } from "./SspSectionsPanel";
import * as api from "@/lib/api";

vi.mock("@/lib/api", () => ({ getSspSections: vi.fn(), createSspSection: vi.fn(), updateSspSection: vi.fn(), changeSspSectionStatus: vi.fn() }));
afterEach(cleanup);
beforeEach(() => { vi.clearAllMocks(); vi.mocked(api.getSspSections).mockResolvedValue([]); });

it("renders empty and permission-denied states and fails closed", async () => {
  render(<SspSectionsPanel canManage={false} />);
  await screen.findByText("No SSP sections exist for this tenant.");
  expect(screen.getByRole("note")).toHaveTextContent("ManageTenant permission is required");
  expect(screen.getByRole("button", { name: "Create section" })).toBeDisabled();
});

it("creates a source-backed section without narrative content", async () => {
  vi.mocked(api.createSspSection).mockResolvedValue({ data: { id: "section" } as api.SspSection, error: null });
  render(<SspSectionsPanel canManage />); await screen.findByText("No SSP sections exist for this tenant.");
  await userEvent.type(screen.getByLabelText("Title"), "Authorization boundary");
  await userEvent.type(screen.getByLabelText("Owner"), "Security owner");
  await userEvent.type(screen.getByLabelText("Source name"), "NIST SP 800-171");
  await userEvent.type(screen.getByLabelText("Source URL"), "https://csrc.nist.gov/");
  await userEvent.type(screen.getByLabelText("Source reviewed"), "2026-09-01");
  await userEvent.click(screen.getByRole("button", { name: "Create section" }));
  await waitFor(() => expect(api.createSspSection).toHaveBeenCalledWith(expect.objectContaining({ title: "Authorization boundary", linkedRecords: [], sourceReferences: [expect.objectContaining({ source: "NIST SP 800-171" })] })));
});

it("shows load failures separately from an empty tenant", async () => {
  vi.mocked(api.getSspSections).mockRejectedValueOnce(new Error("SSP API unavailable"));
  render(<SspSectionsPanel canManage />);
  expect(await screen.findByRole("alert")).toHaveTextContent("SSP API unavailable");
  expect(screen.queryByText("No SSP sections exist for this tenant.")).not.toBeInTheDocument();
});
