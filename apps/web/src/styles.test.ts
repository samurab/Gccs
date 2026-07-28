import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

describe("shared form action contrast", () => {
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
