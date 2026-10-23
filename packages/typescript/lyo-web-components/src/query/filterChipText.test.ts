import { describe, expect, it } from "vitest";
import { filterProperty, fuzzy, type ConditionClause } from "lyo-query";
import { appendFilterUnique, filterChipText, formatFilterValue } from "./filterChipText.js";

const cond = (over: Partial<ConditionClause> = {}): ConditionClause => ({ $type: "condition", Field: "FirstName", Comparison: "Contains", Value: "ada", ...over });

describe("formatFilterValue", () => {
  it("shows at most four list values in compact mode", () => {
    expect(formatFilterValue(["a", "b", "c", "d", "e", "f"])).toBe("[a, b, c, d, ... (+2 more)]");
    expect(formatFilterValue(["a", "b", "c", "d"])).toBe("[a, b, c, d]");
  });

  it("lists everything when not compact", () => {
    expect(formatFilterValue(["a", "b", "c", "d", "e", "f"], false)).toBe("[a, b, c, d, e, f]");
  });

  it("formats null, empty lists, blanks, and scalars", () => {
    expect(formatFilterValue(null)).toBe("null");
    expect(formatFilterValue([])).toBe("[]");
    expect(formatFilterValue(["", "x"])).toBe("[x]");
    expect(formatFilterValue(3)).toBe("3");
    expect(formatFilterValue(true)).toBe("true");
  });
});

describe("filterChipText", () => {
  const definitions = [filterProperty("FirstName", { displayName: "First name" }), filterProperty("Age", { type: "Number" })];

  it("uses the display name and operator description", () => {
    expect(filterChipText(cond(), definitions)).toBe("First name Contains ada");
    expect(filterChipText(cond({ Field: "Age", Comparison: "GreaterThanOrEqual", Value: 30 }), definitions)).toBe("Age ≥ 30");
  });

  it("falls back to the field name without a definition", () => {
    expect(filterChipText(cond({ Field: "Other", Comparison: "Equals", Value: "x" }), [])).toBe("Other = x");
  });

  it("appends the threshold for fuzzy filters", () => {
    expect(filterChipText(fuzzy("FirstName", "ada", 0.4), definitions)).toBe("First name Fuzzy ada threshold=0.4");
    expect(filterChipText(fuzzy("FirstName", "ada"), definitions)).toBe("First name Fuzzy ada");
  });

  it("detail text lists every value", () => {
    const long = cond({ Comparison: "In", Value: ["a", "b", "c", "d", "e"] });
    expect(filterChipText(long, definitions, false)).toBe("First name In [a, b, c, d, e]");
  });
});

describe("appendFilterUnique", () => {
  it("skips a filter equal to an existing one, threshold included", () => {
    const states = [{ condition: fuzzy("FirstName", "ada", 0.4), isEnabled: true }];
    expect(appendFilterUnique(states, fuzzy("FirstName", "ada", 0.4))).toBe(states);
    expect(appendFilterUnique(states, fuzzy("FirstName", "ada", 0.5))).toHaveLength(2);
  });
});
