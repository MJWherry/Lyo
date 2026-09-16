import { describe, expect, it } from "vitest";
import type { ConditionClause } from "../models/whereClause.js";
import { DEFAULT_FUZZY_THRESHOLD, comparisonDescription, conditionEquals, fuzzy, normalizeThreshold } from "./helpers.js";
import { printWhere } from "./print.js";

const cond = (over: Partial<ConditionClause> = {}): ConditionClause => ({ $type: "condition", Field: "Name", Comparison: "Equals", Value: "Ada", ...over });

describe("fuzzy threshold", () => {
  it("defaults to 0.3 like the server", () => {
    expect(DEFAULT_FUZZY_THRESHOLD).toBe(0.3);
  });

  it("normalizeThreshold clamps to 0..1 and treats blanks as unset", () => {
    expect(normalizeThreshold(1.5)).toBe(1);
    expect(normalizeThreshold(-0.2)).toBe(0);
    expect(normalizeThreshold(0.45)).toBe(0.45);
    expect(normalizeThreshold("0.4")).toBe(0.4);
    expect(normalizeThreshold("")).toBeUndefined();
    expect(normalizeThreshold(null)).toBeUndefined();
    expect(normalizeThreshold(Number.NaN)).toBeUndefined();
  });
});

describe("conditionEquals", () => {
  it("compares field, comparison, and value", () => {
    expect(conditionEquals(cond(), cond())).toBe(true);
    expect(conditionEquals(cond(), cond({ Field: "Other" }))).toBe(false);
    expect(conditionEquals(cond(), cond({ Comparison: "NotEquals" }))).toBe(false);
    expect(conditionEquals(cond(), cond({ Value: "Bob" }))).toBe(false);
  });

  it("compares list values item by item", () => {
    expect(conditionEquals(cond({ Comparison: "In", Value: [1, 2] }), cond({ Comparison: "In", Value: [1, 2] }))).toBe(true);
    expect(conditionEquals(cond({ Comparison: "In", Value: [1, 2] }), cond({ Comparison: "In", Value: [2, 1] }))).toBe(false);
  });

  it("treats Threshold as part of equality", () => {
    expect(conditionEquals(fuzzy("Name", "ada", 0.4), fuzzy("Name", "ada", 0.4))).toBe(true);
    expect(conditionEquals(fuzzy("Name", "ada", 0.4), fuzzy("Name", "ada", 0.5))).toBe(false);
    expect(conditionEquals(fuzzy("Name", "ada", 0.4), fuzzy("Name", "ada"))).toBe(false);
  });
});

describe("printWhere threshold", () => {
  it("appends threshold only for fuzzy comparisons", () => {
    expect(printWhere(fuzzy("Name", "ada", 0.4))).toBe("Name Fuzzy 'ada' threshold=0.4");
    expect(printWhere(fuzzy("Name", "ada"))).toBe("Name Fuzzy 'ada'");
    expect(printWhere(cond({ Comparison: "Contains", Threshold: 0.4 }))).toBe("Name Contains 'Ada'");
  });
});

describe("comparisonDescription", () => {
  it("matches the server enum descriptions", () => {
    expect(comparisonDescription("Equals")).toBe("=");
    expect(comparisonDescription("NotEquals")).toBe("≠");
    expect(comparisonDescription("GreaterThanOrEqual")).toBe("≥");
    expect(comparisonDescription("NotStartsWith")).toBe("Not Starts With");
    expect(comparisonDescription("NotFuzzy")).toBe("Not Fuzzy");
  });
});
