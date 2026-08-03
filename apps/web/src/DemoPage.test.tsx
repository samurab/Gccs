import { cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { DemoPage } from "./DemoPage";

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

describe("DemoPage", () => {
  const preferredTime = "2026-08-04T14:00";
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

  it("opens an accessible, No-CUI demo request form and returns focus when closed", async () => {
    const user = userEvent.setup();
    render(<DemoPage />);

    const openButton = screen.getByRole("button", { name: /schedule a live demo/i });
    await user.click(openButton);

    const dialog = screen.getByRole("dialog", { name: /schedule a live demo/i });
    expect(within(dialog).getByLabelText(/first name/i)).toHaveFocus();
    expect(within(dialog).getByText(/do not include CUI, FCI, classified information/i)).toBeInTheDocument();
    expect(within(dialog).getByText(/not a confirmed reservation/i)).toBeInTheDocument();

    fireEvent.keyDown(document, { key: "Escape" });
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(openButton).toHaveFocus();
  });

  it("submits structured business-contact data and confirms receipt", async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(
      JSON.stringify({ status: "Received", receivedAt: "2026-08-02T18:00:00Z" }),
      { status: 202, headers: { "Content-Type": "application/json" } },
    ));
    vi.stubGlobal("fetch", fetchMock);
    const user = userEvent.setup();
    render(<DemoPage />);
    await user.click(screen.getByRole("button", { name: /schedule a live demo/i }));
    await user.type(screen.getByLabelText(/first name/i), "Avery");
    await user.type(screen.getByLabelText(/last name/i), "Ng");
    await user.type(screen.getByLabelText(/work email/i), "avery@example.com");
    await user.type(screen.getByLabelText(/company name/i), "Northstar Systems");
    await user.selectOptions(screen.getByLabelText(/number of employees/i), "11-50");
    await user.type(screen.getByLabelText(/how can we help/i), "Evidence readiness workflow");
    fireEvent.change(screen.getByLabelText(/date and time/i), { target: { value: preferredTime } });
    await user.click(screen.getByLabelText(/I agree that FeDril may use/i));
    await user.click(screen.getByRole("button", { name: /submit demo request/i }));

    expect(await screen.findByRole("status")).toHaveTextContent(/demo request received/i);
    expect(fetchMock).toHaveBeenCalledOnce();
    const [, request] = fetchMock.mock.calls[0];
    expect(JSON.parse(request.body)).toMatchObject({
      firstName: "Avery",
      lastName: "Ng",
      email: "avery@example.com",
      company: "Northstar Systems",
      employeeCount: "11-50",
      message: "Evidence readiness workflow",
      privacyConsent: true,
      preferredTimeZone: expect.any(String),
    });
  });

  it("keeps entered data available and reports an API failure for retry", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(
      JSON.stringify({ detail: "Online demo requests are temporarily unavailable." }),
      { status: 503, headers: { "Content-Type": "application/problem+json" } },
    )));
    const user = userEvent.setup();
    render(<DemoPage />);
    await user.click(screen.getByRole("button", { name: /schedule a live demo/i }));
    await user.type(screen.getByLabelText(/first name/i), "Avery");
    await user.type(screen.getByLabelText(/last name/i), "Ng");
    await user.type(screen.getByLabelText(/work email/i), "avery@example.com");
    await user.type(screen.getByLabelText(/company name/i), "Northstar Systems");
    fireEvent.change(screen.getByLabelText(/date and time/i), { target: { value: preferredTime } });
    await user.click(screen.getByLabelText(/I agree that FeDril may use/i));
    await user.click(screen.getByRole("button", { name: /submit demo request/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent(/temporarily unavailable/i);
    expect(screen.getByLabelText(/first name/i)).toHaveValue("Avery");
    expect(screen.getByRole("button", { name: /submit demo request/i })).toBeEnabled();
  });
});
