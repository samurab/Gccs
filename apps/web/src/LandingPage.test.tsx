import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { LandingPage } from "./LandingPage";

describe("LandingPage", () => {
  it("presents the GCCS pilot offer with No-CUI boundaries", () => {
    render(<LandingPage />);

    expect(screen.getByRole("heading", { name: /compliance readiness tracking for small government contractors/i })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /request a pilot demo/i })).toBeInTheDocument();
    expect(screen.getByText(/No-CUI \/ compliance management only/i)).toBeInTheDocument();
    expect(screen.getByText(/does not certify compliance, provide legal advice, or accept real CUI/i)).toBeInTheDocument();
    expect(screen.getByText(/30-day guided readiness pilot/i)).toBeInTheDocument();
  });
});
