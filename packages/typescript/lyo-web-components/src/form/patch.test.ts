import { describe, expect, it } from "vitest";
import { buildPatchRequest, flagsSelected, flagsToggle, formatFormValue, parseFormValue, propertyChange, valuesEqual } from "./patch.js";

describe("buildPatchRequest", () => {
  it("returns null when nothing changed", () => {
    expect(buildPatchRequest({}, "id-1")).toBeNull();
  });

  it("includes only changed properties and wraps a scalar key", () => {
    const patch = buildPatchRequest(
      {
        FirstName: propertyChange("FirstName", "Bo", "Ann"),
        LastName: propertyChange("LastName", "Li", "Li"),
      },
      "abc"
    );
    expect(patch).toEqual({ keys: ["abc"], data: { FirstName: "Ann" } });
  });
});

describe("propertyChange", () => {
  it("compares dates by instant, arrays and objects by value", () => {
    expect(propertyChange("At", new Date("2024-01-01T00:00:00Z"), new Date("2024-01-01T00:00:00Z")).hasChanged).toBe(false);
    expect(propertyChange("Tags", ["a", "b"], ["a", "b"]).hasChanged).toBe(false);
    expect(propertyChange("Tags", ["a"], ["a", "b"]).hasChanged).toBe(true);
    expect(propertyChange("N", null, 0)).toEqual({ propertyName: "N", originalValue: null, currentValue: 0, hasChanged: true });
  });

  it("valuesEqual treats NaN as equal to itself", () => {
    expect(valuesEqual(Number.NaN, Number.NaN)).toBe(true);
  });
});

describe("parseFormValue", () => {
  it("integers must be whole; blank is null only when nullable", () => {
    expect(parseFormValue("integer", "12")).toEqual({ value: 12 });
    expect(parseFormValue("integer", "1.5")).toEqual({ invalid: true });
    expect(parseFormValue("integer", " ", { nullable: true })).toEqual({ value: null });
    expect(parseFormValue("number", "", { nullable: false })).toEqual({ invalid: true });
    expect(parseFormValue("number", "2.5")).toEqual({ value: 2.5 });
  });

  it("GUIDs are validated and lower-cased", () => {
    expect(parseFormValue("guid", "nope")).toEqual({ invalid: true });
    expect(parseFormValue("guid", "0F8FAD5B-D9CB-469F-A165-70867728950E")).toEqual({ value: "0f8fad5b-d9cb-469f-a165-70867728950e" });
  });

  it("date-times become ISO UTC strings; dates and times stay as typed", () => {
    expect(parseFormValue("datetime", "2024-01-02T03:04")).toEqual({ value: "2024-01-02T03:04:00.000Z" });
    expect(parseFormValue("date", "2024-01-02")).toEqual({ value: "2024-01-02" });
    expect(parseFormValue("time", "13:45")).toEqual({ value: "13:45:00" });
  });

  it("strings keep their text; blank nullable strings become null", () => {
    expect(parseFormValue("string", " hi ")).toEqual({ value: " hi " });
    expect(parseFormValue("string", "", { nullable: true })).toEqual({ value: null });
    expect(parseFormValue("string", "")).toEqual({ value: "" });
  });
});

describe("formatFormValue", () => {
  it("prints values for inputs (datetime-local in UTC, empty for null)", () => {
    expect(formatFormValue("datetime", "2024-01-02T03:04:05Z")).toBe("2024-01-02T03:04");
    expect(formatFormValue("datetime", new Date("2024-01-02T03:04:05Z"))).toBe("2024-01-02T03:04");
    expect(formatFormValue("integer", 5)).toBe("5");
    expect(formatFormValue("string", null)).toBe("");
  });
});

describe("flags", () => {
  const options = [
    { value: 1, label: "Read" },
    { value: 2, label: "Write" },
    { value: 4, label: "Delete" },
  ];

  it("flagsSelected lists the options whose bits are set", () => {
    expect(flagsSelected(5, options)).toEqual([1, 4]);
    expect(flagsSelected(0, options)).toEqual([]);
  });

  it("flagsToggle sets and clears bits", () => {
    expect(flagsToggle(1, 2, true)).toBe(3);
    expect(flagsToggle(3, 1, false)).toBe(2);
  });
});
