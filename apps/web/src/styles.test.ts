import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

describe("shared form action contrast", () => {
  it("uses the same editor columns and gutters for evidence, contracts, and notes", () => {
    const stylesheet = readFileSync(resolve(process.cwd(), "styles/globals.css"), "utf8");
    const classified = readFileSync(resolve(process.cwd(), "src/components/ClassifiedContent.css"), "utf8");
    for (const selector of ["contract-workspace", "evidence-metadata__workspace"]) {
      const rule = stylesheet.match(new RegExp(`\\.${selector}\\s*\\{([^}]*)\\}`))?.[1];
      expect(rule).toContain("grid-template-columns: var(--workspace-editor-columns)");
      expect(rule).toContain("gap: var(--workspace-card-gap)");
    }
    expect(classified.match(/\.classified-notes-workbench\s*\{([^}]*)\}/)?.[1]).toContain("grid-template-columns: var(--workspace-editor-columns)");
  });
  it("aligns new workflow cards to the existing content rail and bounds upload columns", () => {
    const stylesheet = readFileSync(resolve(process.cwd(), "styles/globals.css"), "utf8");
    const rail = stylesheet.match(/\.workspace-main > \.classification-review-panel,[\s\S]*?\{([^}]*)\}/)?.[1];
    expect(rail).toMatch(/max-width:\s*1180px/);
    expect(rail).toMatch(/margin-inline:\s*auto/);
    expect(stylesheet.match(/\.evidence-upload-panels\s*\{([^}]*)\}/)?.[1]).toMatch(/repeat\(2, minmax\(0, 1fr\)\)/);
  });

  it("keeps visible card boundaries around the obligation queue and calendar", () => {
    const stylesheet = readFileSync(resolve(process.cwd(), "styles/globals.css"), "utf8");
    const rule = stylesheet.match(/\.route-panel\.obligations-route,\s*\.route-panel\.calendar-route\s*\{([^}]*)\}/)?.[1];
    expect(rule).toMatch(/border:\s*1px solid var\(--line-strong\)/);
    expect(rule).toMatch(/background:\s*var\(--surface\)/);
    expect(stylesheet.match(/\.workspace-main\s*\{([^}]*)\}/)?.[1]).toMatch(/min-width:\s*0/);
  });

  it("keeps classification controls compact instead of reserving an empty detail column", () => {
    const stylesheet = readFileSync(resolve(process.cwd(), "src/components/ClassifiedContent.css"), "utf8");
    expect(stylesheet).toMatch(/\.classification-workbench\s*\{\s*display:\s*block/);
    expect(stylesheet).toMatch(/grid-template-columns:\s*max-content minmax\(220px, 360px\) max-content/);
  });

  it("uses a white foreground for the primary dark action without changing secondary actions", () => {
    const stylesheet = readFileSync(resolve(process.cwd(), "styles/globals.css"), "utf8");
    const baseRule = stylesheet.match(/\.form-actions button\s*\{(?<declarations>[^}]*)\}/)?.groups?.declarations;
    const primaryRule = stylesheet.match(
      /\.form-actions button:first-child\s*\{(?<declarations>[^}]*)\}/
    )?.groups?.declarations;

    expect(baseRule).toMatch(/color:\s*var\(--teal\)/);
    expect(primaryRule).toMatch(/background:\s*var\(--ink\)/);
    expect(primaryRule).toMatch(/color:\s*#ffffff/);
  });
});
