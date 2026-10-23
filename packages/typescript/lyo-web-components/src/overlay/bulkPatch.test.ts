import { describe, expect, it } from "vitest";
import { bulkPatchData, bulkPatchRoute, bulkPatchSummary, lyoTypeForProperty } from "./bulkPatch.js";

describe("bulkPatchRoute", () => {
  it("defaults to the grid route and strips a trailing /Bulk (the client appends it)", () => {
    expect(bulkPatchRoute("/Person")).toBe("/Person");
    expect(bulkPatchRoute("/Person", "/Person/Admin/")).toBe("/Person/Admin");
    expect(bulkPatchRoute("/Person", "/Person/Bulk")).toBe("/Person");
    expect(bulkPatchRoute("/Person", " ")).toBe("/Person");
  });
});

describe("bulkPatchData", () => {
  it("maps the property to the parsed typed value", () => {
    expect(bulkPatchData("Age", "42")).toEqual({ Age: 42 });
    expect(bulkPatchData("Tags", '["a"]')).toEqual({ Tags: ["a"] });
    expect(bulkPatchData("Name", '"Ann"')).toEqual({ Name: "Ann" });
  });

  it("blank json clears the property", () => {
    expect(bulkPatchData("Name", null)).toEqual({ Name: null });
    expect(bulkPatchData("Name", " ")).toEqual({ Name: null });
  });
});

describe("lyoTypeForProperty", () => {
  it("maps filter property types onto LyoType full names", () => {
    expect(lyoTypeForProperty({ propertyName: "A" })).toBe("System.String");
    expect(lyoTypeForProperty({ propertyName: "A", type: "Number" })).toBe("System.Decimal");
    expect(lyoTypeForProperty({ propertyName: "A", type: "Bool" })).toBe("System.Boolean");
    expect(lyoTypeForProperty({ propertyName: "A", type: "DateTime" })).toBe("System.DateTime");
    expect(lyoTypeForProperty({ propertyName: "A", type: "DateOnly" })).toBe("System.DateOnly");
    expect(lyoTypeForProperty({ propertyName: "A", type: "TimeOnly" })).toBe("System.TimeOnly");
    expect(lyoTypeForProperty({ propertyName: "A", type: "Enum" })).toBe("System.Enum");
  });
});

describe("bulkPatchSummary", () => {
  it("reports updated / failed counts when the result carries them, otherwise the key count", () => {
    expect(bulkPatchSummary({ updatedCount: 3, failedCount: 0 }, 3)).toEqual({ message: "Patched 3 rows", severity: "success" });
    expect(bulkPatchSummary({ updatedCount: 2, failedCount: 1 }, 3)).toEqual({ message: "Patched 2 rows, 1 failed", severity: "warning" });
    expect(bulkPatchSummary({ ok: true }, 1)).toEqual({ message: "Patched 1 row", severity: "success" });
  });
});
