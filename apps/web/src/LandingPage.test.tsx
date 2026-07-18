import { render, screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { LandingPage } from "./LandingPage";

describe("LandingPage", () => {
  it("presents the GCCS pilot offer with No-CUI boundaries", () => {
    render(<LandingPage />);

    expect(screen.getByRole("heading", { name: /compliance readiness tracking for small government contractors/i })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /request a pilot demo/i })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /open workspace/i })).toHaveAttribute("href", "/app#/dashboard");
    expect(screen.getByText(/No-CUI \/ compliance management only/i)).toBeInTheDocument();
    expect(screen.getByText(/does not certify compliance, provide legal advice, or accept real CUI/i)).toBeInTheDocument();
    expect(screen.getByText(/30-day guided readiness pilot/i)).toBeInTheDocument();
    const sectionTabs = screen.getByRole("navigation", { name: /landing page sections/i });
    expect(sectionTabs).toBeInTheDocument();
    expect(within(sectionTabs).getByRole("link", { name: /platform/i })).toHaveAttribute("href", "#platform");
    expect(screen.getByRole("heading", { name: /lightweight workspace/i })).toBeInTheDocument();
    expect(screen.getByText(/Blocked: real CUI, classified data/i)).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /built as a public page/i })).toBeInTheDocument();
  });
});
