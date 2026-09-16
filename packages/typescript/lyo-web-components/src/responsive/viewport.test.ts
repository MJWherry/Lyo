import { describe, expect, it } from "vitest";
import { breakpointOf, isAtOrBelow } from "./viewport.js";

describe("breakpointOf", () => {
  it("uses the MUI breakpoints", () => {
    expect(breakpointOf(0)).toBe("xs");
    expect(breakpointOf(599)).toBe("xs");
    expect(breakpointOf(600)).toBe("sm");
    expect(breakpointOf(899)).toBe("sm");
    expect(breakpointOf(900)).toBe("md");
    expect(breakpointOf(1200)).toBe("lg");
    expect(breakpointOf(1536)).toBe("xl");
  });
});

describe("isAtOrBelow", () => {
  it("compares breakpoint ranks", () => {
    expect(isAtOrBelow(500, "sm")).toBe(true);
    expect(isAtOrBelow(899, "sm")).toBe(true);
    expect(isAtOrBelow(900, "sm")).toBe(false);
    expect(isAtOrBelow(1000, "md")).toBe(true);
  });

  it("is false before the width is known so SSR renders the wide layout", () => {
    expect(isAtOrBelow(null, "xl")).toBe(false);
  });
});
