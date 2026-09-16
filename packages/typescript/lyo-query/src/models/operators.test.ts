import { describe, expect, it } from "vitest";
import { operatorsFor, operatorsForCategory } from "./filterProperty.js";
import { COMPARISON_OPERATORS } from "./whereClause.js";

describe("operatorsForCategory", () => {
  it("gives Identifier the text operators, fuzzy included", () => {
    expect(operatorsForCategory("Identifier")).toEqual(operatorsFor("String"));
    expect(operatorsForCategory("Identifier")).toContain("Fuzzy");
  });

  it("maps the remaining categories to the filter types", () => {
    expect(operatorsForCategory("Text")).toEqual(operatorsFor("String"));
    expect(operatorsForCategory("Integer")).toEqual(operatorsFor("Number"));
    expect(operatorsForCategory("Number")).toEqual(operatorsFor("Number"));
    expect(operatorsForCategory("Temporal")).toEqual(operatorsFor("DateTime"));
    expect(operatorsForCategory("Enum")).toEqual(operatorsFor("Enum"));
    expect(operatorsForCategory("Boolean")).toEqual(operatorsFor("Bool"));
  });

  it("allows every operator for Unknown, Json, Collection, and Binary", () => {
    for (const c of ["Unknown", "Json", "Collection", "Binary"] as const) expect(operatorsForCategory(c)).toEqual([...COMPARISON_OPERATORS]);
  });
});
