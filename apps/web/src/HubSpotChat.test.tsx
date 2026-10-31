import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { HubSpotChat } from "./HubSpotChat";
import { shouldOfferHubSpotChat } from "./routing";

describe("HubSpotChat", () => {
  beforeEach(() => {
    const values = new Map<string, string>();
    Object.defineProperty(window, "localStorage", {
      configurable: true,
      value: {
        clear: () => values.clear(),
        getItem: (key: string) => values.get(key) ?? null,
        removeItem: (key: string) => values.delete(key),
        setItem: (key: string, value: string) => values.set(key, value),
      },
    });
    window.history.replaceState({}, "", "/");
    delete window.HubSpotConversations;
    delete window.hsConversationsOnReady;
    document.getElementById("hs-script-loader")?.remove();
  });

  afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
  });

  it.each(["/", "/landing", "/demo"])("is offered on the public marketing route %s", (pathname) => {
    expect(shouldOfferHubSpotChat({ pathname })).toBe(true);
  });

  it.each(["/app", "/platform", "/platform/demo-requests", "/invitations/accept", "/demo-request-details"])(
    "is not offered on the protected or customer-detail route %s",
    (pathname) => {
      expect(shouldOfferHubSpotChat({ pathname })).toBe(false);
    },
  );

  it("does not contact HubSpot before explicit consent", () => {
    render(<HubSpotChat />);

    expect(screen.getByRole("complementary", { name: /HubSpot chat privacy choices/i })).toBeInTheDocument();
    expect(screen.getByText(/Do not share CUI, FCI, classified information/i)).toBeInTheDocument();
    expect(document.getElementById("hs-script-loader")).toBeNull();
  });

  it("loads the exact HTTPS HubSpot script and refreshes the widget after consent", () => {
    const refresh = vi.fn();
    window.HubSpotConversations = { widget: { refresh, remove: vi.fn() } };
    render(<HubSpotChat />);

    fireEvent.click(screen.getByRole("button", { name: "Enable chat" }));

    const script = document.getElementById("hs-script-loader") as HTMLScriptElement | null;
    expect(script).not.toBeNull();
    expect(script?.src).toBe("https://js-na2.hs-scripts.com/247124975.js");
    expect(script?.async).toBe(true);
    expect(script?.defer).toBe(true);
    expect(window.localStorage.getItem("fedril.hubspot-chat-consent.v1")).toBe("accepted");

    script?.dispatchEvent(new Event("load"));
    window.hsConversationsOnReady?.[0]?.();
    expect(refresh).toHaveBeenCalledOnce();
  });

  it("persists a declined preference without loading HubSpot and lets the visitor reconsider", () => {
    render(<HubSpotChat />);

    fireEvent.click(screen.getByRole("button", { name: "Not now" }));

    expect(window.localStorage.getItem("fedril.hubspot-chat-consent.v1")).toBe("declined");
    expect(document.getElementById("hs-script-loader")).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Chat privacy settings" }));
    expect(screen.getByRole("button", { name: "Enable chat" })).toBeInTheDocument();
  });
});
