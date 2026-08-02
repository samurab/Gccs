import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LandingPage } from "./LandingPage";

describe("LandingPage", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("presents the FeDril pilot offer with No-CUI boundaries", () => {
    render(<LandingPage />);

    expect(screen.getByRole("heading", { name: /turn govcon compliance work into an operating system/i })).toBeInTheDocument();
    expect(
      screen.getByText(
        /FeDril tracks obligations, evidence, deadlines, and readiness gaps in one No-CUI workspace, so your team can see what is missing before reviews, renewals, and contract deliverables/i
      )
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /request a pilot demo/i })).toHaveAttribute("href", "/demo");
    expect(screen.getAllByRole("link", { name: /open workspace/i })).toHaveLength(2);
    for (const link of screen.getAllByRole("link", { name: /open workspace/i })) {
      expect(link).toHaveAttribute("href", "/app#/dashboard");
    }
    expect(screen.getByText(/No-CUI \/ compliance management only/i)).toBeInTheDocument();
    expect(screen.getByText(/does not certify compliance, provide legal advice, or provide government approval/i)).toBeInTheDocument();
    expect(screen.getByText(/30-day guided readiness pilot/i)).toBeInTheDocument();
    const primaryNavigation = screen.getByText("Platform").closest(".landing-nav__links");
    expect(primaryNavigation).not.toBeNull();
    expect(within(primaryNavigation as HTMLElement).getByRole("link", { name: /platform/i })).toHaveAttribute("href", "#platform");
    expect(screen.getByRole("heading", { name: /messy middle between contract requirements and evidence packages/i })).toBeInTheDocument();
    expect(screen.getByText(/Do not upload: real CUI, classified data/i)).toBeInTheDocument();
    const preview = screen.getByLabelText(/FeDril one-minute product walkthrough/i);
    expect(preview.querySelector('source[media="(max-width: 720px)"]')).toHaveAttribute(
      "src",
      "/videos/fedril-homepage-60-mobile.mp4",
    );
    expect(preview.querySelector('source[type="video/mp4"]:not([media])')).toHaveAttribute(
      "src",
      "/videos/fedril-homepage-60.mp4",
    );
    expect(preview.querySelector("track")).toHaveAttribute("src", "/captions/fedril-homepage-60.vtt");
    expect(screen.getByRole("link", { name: /watch the full product walkthrough/i })).toHaveAttribute("href", "/demo");
    expect(screen.getByRole("heading", { name: /turn govcon compliance work into an operating system/i })).toBeInTheDocument();
  });

  it("offers an explicit user gesture that enables narration", () => {
    const play = vi.spyOn(HTMLMediaElement.prototype, "play").mockResolvedValue();
    const { container } = render(<LandingPage />);
    const video = within(container).getByLabelText<HTMLVideoElement>(
      /FeDril one-minute product walkthrough/i,
    );
    video.muted = true;
    video.volume = 0;
    Object.defineProperty(video, "duration", { configurable: true, value: 60 });
    video.currentTime = 60;

    fireEvent.click(
      within(container).getByRole("button", { name: /play with narration/i }),
    );

    expect(video.muted).toBe(false);
    expect(video.volume).toBe(1);
    expect(video.currentTime).toBe(0);
    expect(play).toHaveBeenCalledOnce();
  });

  it("reports when narrated playback is blocked", async () => {
    vi.spyOn(HTMLMediaElement.prototype, "play").mockRejectedValue(
      new DOMException("Playback blocked", "NotAllowedError"),
    );
    const { container } = render(<LandingPage />);

    fireEvent.click(
      within(container).getByRole("button", { name: /play with narration/i }),
    );

    await waitFor(() => {
      expect(within(container).getByRole("status")).toHaveTextContent(
        /playback was blocked by the browser/i,
      );
    });
  });
});
