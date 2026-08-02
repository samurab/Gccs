import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { DemoPage } from "./DemoPage";

describe("DemoPage", () => {
  it("presents the captioned flagship walkthrough with clear No-CUI boundaries", () => {
    render(<DemoPage />);

    expect(screen.getByRole("heading", { name: /see readiness work move from gap to follow-through/i })).toBeInTheDocument();
    const video = screen.getByLabelText(/FeDril flagship product walkthrough/i);
    expect(video.querySelector("source")).toHaveAttribute("src", "/videos/fedril-flagship.mp4");
    expect(video.querySelector("track")).toHaveAttribute("src", "/captions/fedril-demo.vtt");
    expect(screen.getByText(/Narration generated using AI voice technology/i)).toBeInTheDocument();
    expect(screen.getByText(/contains no production data, real customer information, CUI, FCI, credentials, or secrets/i)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /schedule a live demo/i })).toHaveAttribute(
      "href",
      "mailto:hello@fedril.example?subject=FeDril%20live%20demo"
    );
  });
});
