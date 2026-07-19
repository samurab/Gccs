import { render, screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { LandingPage } from "./LandingPage";

describe("LandingPage", () => {
  it("presents the FeDril pilot offer with No-CUI boundaries", () => {
    render(<LandingPage />);

    expect(screen.getByRole("heading", { name: /turn compliance work into an operating system/i })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /request a pilot demo/i })).toBeInTheDocument();
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
    expect(screen.getByRole("heading", { name: /turn compliance work into an operating system/i })).toBeInTheDocument();
  });
});
