import { describe, expect, it } from "vitest";
import type { ComparisonOperator, WhereClause } from "../models/whereClause.js";
import { printWhere } from "./print.js";

const condition = (Field: string, Comparison: ComparisonOperator, Value: unknown): WhereClause => ({ $type: "condition", Field, Comparison, Value });
const and = (...Children: WhereClause[]): WhereClause => ({ $type: "group", Operator: "And", Children });
const or = (...Children: WhereClause[]): WhereClause => ({ $type: "group", Operator: "Or", Children });

describe("printWhere", () => {
  it("prints a scalar condition with quoted value", () => {
    expect(printWhere(condition("Name", "Equals", "Ada"))).toBe("Name Equals 'Ada'");
  });

  it("prints NULL for a null value", () => {
    expect(printWhere(condition("Name", "Equals", null))).toBe("Name Equals NULL");
  });

  it("prints single-item lists inline and longer lists one item per line", () => {
    expect(printWhere(condition("Id", "In", [1]))).toBe("Id In '1'");
    expect(printWhere(condition("Id", "In", []))).toBe("Id In []");
    expect(printWhere(condition("Id", "In", [1, null]))).toBe("Id In [\n  - 1\n  - NULL\n]");
  });

  it("puts the group operator in front of each child after the first", () => {
    const where = and(condition("A", "Equals", 1), condition("B", "Equals", 2));
    expect(printWhere(where)).toBe("(\n  A Equals '1'\n  AND B Equals '2'\n)");
  });

  it("indents nested groups", () => {
    const where = or(condition("A", "Equals", 1), and(condition("B", "Equals", 2), condition("C", "Equals", 3)));
    expect(printWhere(where)).toBe("(\n  A Equals '1'\n  OR (\n    B Equals '2'\n    AND C Equals '3'\n  )\n)");
  });

  it("unwraps single-child groups and prints empty groups as (OP)", () => {
    expect(printWhere(and(condition("A", "Equals", 1)))).toBe("A Equals '1'");
    expect(printWhere(or())).toBe("(OR)");
  });

  it("returns an empty string for no clause", () => {
    expect(printWhere(null)).toBe("");
  });
});
