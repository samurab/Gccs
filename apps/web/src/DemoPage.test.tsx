import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it } from "vitest";
import { DemoPage } from "./DemoPage";

afterEach(cleanup);

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
});
