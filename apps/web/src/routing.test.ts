import { describe, expect, it } from "vitest";
import { getWorkspaceUrl, shouldRenderLandingPage } from "./routing";

function locationStub(pathname: string, search = "", hash = ""): Pick<Location, "pathname" | "search" | "hash"> {
  return { pathname, search, hash };
}

describe("routing", () => {
  it("renders the public landing page at root, /landing, and explicit landing query", () => {
    expect(shouldRenderLandingPage(locationStub("/"))).toBe(true);
    expect(shouldRenderLandingPage(locationStub("/landing"))).toBe(true);
    expect(shouldRenderLandingPage(locationStub("/app", "?view=landing"))).toBe(true);
  });

  it("renders root as public landing even when a legacy workspace hash is present", () => {
    expect(shouldRenderLandingPage(locationStub("/", "", "#/dashboard"))).toBe(true);
    expect(shouldRenderLandingPage(locationStub("/", "", "#/reports"))).toBe(true);
  });

  it("keeps /app authenticated workspace routes out of the public landing page", () => {
    expect(shouldRenderLandingPage(locationStub("/app"))).toBe(false);
    expect(shouldRenderLandingPage(locationStub("/app", "", "#/dashboard"))).toBe(false);
    expect(shouldRenderLandingPage(locationStub("/app", "", "#/reports"))).toBe(false);
  });

  it("uses /app as the shared workspace redirect URL", () => {
    expect(getWorkspaceUrl("https://gccs.example")).toBe("https://gccs.example/app");
  });
});
