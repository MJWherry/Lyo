import { describe, expect, it } from "vitest";
import { withLineNumbers } from "./codeBlock.js";

describe("withLineNumbers", () => {
  it("pads numbers to the widest line number", () => {
    const code = Array.from({ length: 10 }, (_, i) => `l${i + 1}`).join("\r\n");
    const lines = withLineNumbers(code).split("\n");
    expect(lines[0]).toBe(" 1  l1");
    expect(lines[9]).toBe("10  l10");
  });

  it("returns empty text for empty input", () => {
    expect(withLineNumbers("")).toBe("");
    expect(withLineNumbers(null)).toBe("");
  });
});
