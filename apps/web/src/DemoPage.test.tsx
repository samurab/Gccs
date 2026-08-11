import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { DemoPage } from "./DemoPage";

afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
});

function completeRequiredDemoFields(preferredStart: string) {
  fireEvent.change(screen.getByLabelText(/first name/i), { target: { value: "Avery" } });
  fireEvent.change(screen.getByLabelText(/last name/i), { target: { value: "Ng" } });
  fireEvent.change(screen.getByLabelText(/work email/i), { target: { value: "avery@example.com" } });
  fireEvent.change(screen.getByLabelText(/company name/i), { target: { value: "Northstar Systems" } });
  fireEvent.change(screen.getByLabelText(/date and time/i), { target: { value: preferredStart } });
  fireEvent.click(screen.getByRole("checkbox", { name: /I agree that FeDril may use/i }));
}

describe("DemoPage", () => {
  it("presents the captioned flagship walkthrough with clear No-CUI boundaries", () => {
    render(<DemoPage />);

    expect(screen.getByRole("heading", { name: /see readiness work move from gap to follow-through/i })).toBeInTheDocument();
    const video = screen.getByLabelText(/FeDril flagship product walkthrough/i);
    expect(video.querySelector("source")).toHaveAttribute("src", "/videos/fedril-flagship.mp4");
    expect(video.querySelector("track")).toHaveAttribute("src", "/captions/fedril-demo.vtt");
    expect(screen.getByText(/Narration generated using AI voice technology/i)).toBeInTheDocument();
    expect(screen.getByText(/contains no production data, real customer information, CUI, FCI, credentials, or secrets/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /schedule a live demo/i })).toBeInTheDocument();
  });

  it("opens the live-demo scheduler from the post-video call to action", async () => {
    const user = userEvent.setup();
    render(<DemoPage />);

    await user.click(screen.getByRole("button", { name: /schedule a live demo/i }));

    expect(screen.getByRole("dialog", { name: /schedule a live demo/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/first name/i)).toHaveFocus();
    expect(screen.getByLabelText(/date and time/i)).toBeRequired();
  });

  it("explains the two-hour minimum for a complete demo date and time", () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-08-10T18:00:00-04:00"));
    render(<DemoPage />);
    fireEvent.click(screen.getByRole("button", { name: /schedule a live demo/i }));
    const preferredStart = screen.getByLabelText<HTMLInputElement>(/date and time/i);

    fireEvent.change(preferredStart, { target: { value: "2026-08-10T19:00" } });
    fireEvent.invalid(preferredStart);

    expect(preferredStart.validity.rangeUnderflow).toBe(true);
    expect(preferredStart.validationMessage).toMatch(/^Value must be .+ or later\.$/);
    expect(preferredStart.closest("label")).toHaveClass("demo-request-form__explicit-validation");
    expect(screen.getByRole("alert")).toHaveTextContent(/^Value must be .+ or later\.$/);
  });

  it("refreshes the two-hour boundary before submitting a form that remained open", () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-08-11T12:00:00-04:00"));
    const fetchMock = vi.fn();
    vi.stubGlobal("fetch", fetchMock);
    render(<DemoPage />);
    fireEvent.click(screen.getByRole("button", { name: /schedule a live demo/i }));
    completeRequiredDemoFields("2026-08-11T14:00");

    vi.setSystemTime(new Date("2026-08-11T12:01:00-04:00"));
    fireEvent.submit(screen.getByLabelText(/date and time/i).closest("form")!);

    expect(fetchMock).not.toHaveBeenCalled();
    expect(screen.getByRole("alert")).toHaveTextContent(/^Value must be .+ or later\.$/);
    expect(screen.getByLabelText(/date and time/i)).toHaveFocus();
  });

  it("maps an API preferred-time validation error back to the scheduler field", async () => {
    vi.spyOn(Date, "now").mockReturnValue(new Date("2026-08-11T12:00:00-04:00").getTime());
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({
      ok: false,
      json: vi.fn().mockResolvedValue({
        title: "Demo request invalid",
        detail: "The demo request is invalid.",
        errors: { preferredStartAt: ["Select a demo time between two hours and 90 days from now."] },
      }),
    }));
    render(<DemoPage />);
    fireEvent.click(screen.getByRole("button", { name: /schedule a live demo/i }));
    completeRequiredDemoFields("2026-08-11T15:00");

    await act(async () => {
      fireEvent.submit(screen.getByLabelText(/date and time/i).closest("form")!);
      await Promise.resolve();
    });

    await waitFor(() => expect(screen.getByText("Please correct the preferred demo time.")).toBeInTheDocument());
    expect(screen.getByText(/^Value must be .+ or later\.$/)).toBeInTheDocument();
    expect(screen.queryByText("The demo request is invalid.")).not.toBeInTheDocument();
  });

  it("explains local capture and links developers to the operator calendar after submission", async () => {
    vi.spyOn(Date, "now").mockReturnValue(new Date("2026-08-11T12:00:00-04:00").getTime());
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({
      ok: true,
      json: vi.fn().mockResolvedValue({ status: "Received", receivedAt: "2026-08-11T16:00:00Z" }),
    }));
    render(<DemoPage />);
    fireEvent.click(screen.getByRole("button", { name: /schedule a live demo/i }));
    completeRequiredDemoFields("2026-08-11T15:00");

    await act(async () => {
      fireEvent.submit(screen.getByLabelText(/date and time/i).closest("form")!);
      await Promise.resolve();
    });

    await waitFor(() => expect(screen.getByText(/local development capture transport/i)).toBeInTheDocument());
    expect(screen.getByText(/no email was sent/i)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /open the local operator calendar/i })).toHaveAttribute("href", "/platform/demo-requests");
  });
});
