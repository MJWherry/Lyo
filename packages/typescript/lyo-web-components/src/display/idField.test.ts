import { describe, expect, it } from "vitest";
import { abbreviateId, idFieldCanToggle } from "./idField.js";

const guid = "0190f4a2-7c1e-7b3a-9d2f-1a2b3c4d5e6f";

describe("abbreviateId", () => {
  it("keeps the last 9 characters by default", () => {
    expect(abbreviateId(guid)).toBe("b3c4d5e6f");
  });

  it("supports prefix and none", () => {
    expect(abbreviateId(guid, "prefix")).toBe("0190f4a2-");
    expect(abbreviateId(guid, "none")).toBe(guid);
    expect(abbreviateId(guid, "suffix", 4)).toBe("5e6f");
  });

  it("never abbreviates numeric ids or short values", () => {
    expect(abbreviateId("12345678901234")).toBe("12345678901234");
    expect(abbreviateId("abc")).toBe("abc");
  });

  it("returns null text for blanks", () => {
    expect(abbreviateId(null)).toBe("null");
    expect(abbreviateId("  ")).toBe("null");
  });
});

describe("idFieldCanToggle", () => {
  it("only toggles when abbreviation hides characters", () => {
    expect(idFieldCanToggle(guid)).toBe(true);
    expect(idFieldCanToggle("12345678901234")).toBe(false);
    expect(idFieldCanToggle("abc")).toBe(false);
    expect(idFieldCanToggle(guid, "none")).toBe(false);
    expect(idFieldCanToggle(null)).toBe(false);
  });
});
