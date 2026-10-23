import { describe, expect, it } from "vitest";
import { LyoChipHues } from "../display/chips.js";
import { LyoTypes } from "./lyoTypeCatalog.js";
import {
  CUSTOM_TYPE_TOKEN,
  canWrapInCollection,
  chipValidation,
  collectionShape,
  composeFullName,
  concreteFullNameDisplay,
  concreteFullNameFromInput,
  defaultJsonFor,
  displayName,
  elementFullName,
  hueForCategory,
  isWideEditor,
  needsConcreteFullName,
  pickerGroups,
  shouldResetValue,
  toListKind,
  typeChip,
  validateTypedJson,
  valuePreview,
} from "./lyoTypeUi.js";

describe("chips", () => {
  it("maps categories to identity hues; Unknown has none", () => {
    expect(hueForCategory("Text")).toBe(LyoChipHues.Blue);
    expect(hueForCategory("Integer")).toBe(LyoChipHues.Teal);
    expect(hueForCategory("Collection")).toBe(LyoChipHues.Coral);
    expect(hueForCategory("Unknown")).toBeNull();
  });

  it("typeChip uses the display name, warning for regex/xml, raw text for unknown names", () => {
    expect(typeChip("System.Int32")).toMatchObject({ label: "Int", hue: LyoChipHues.Teal });
    expect(typeChip("regex")).toMatchObject({ label: "Regex", color: "warning" });
    expect(typeChip("My.Custom")).toEqual({ label: "My.Custom", color: "default", variant: "filled" });
  });
});

describe("displayName", () => {
  it("capitalizes and draws collections by element", () => {
    expect(displayName(LyoTypes.Int)).toBe("Int");
    expect(displayName(LyoTypes.IntList)).toBe("List<Int>");
    expect(displayName("System.String[]")).toBe("String[]");
    expect(displayName("JSON object")).toBe("JSON object");
    expect(displayName("")).toBe("—");
    expect(displayName("My.Custom")).toBe("My.Custom");
  });
});

describe("pickerGroups", () => {
  it("groups non-collection rows by category in category order, sorted by display name", () => {
    const groups = pickerGroups();
    expect(groups[0]).toMatchObject({ category: "Text" });
    expect(groups[0]!.types.map((t) => displayName(t))).toEqual(["Regex", "String", "Uri"]);
    expect(groups.map((g) => g.category)).not.toContain("Collection");
    expect(groups[0]!.types.map((t) => displayName(t))).not.toContain("Text");
    expect(groups.find((g) => g.category === "Json")!.types.map((t) => t.shortName)).toEqual(["JSON array", "JSON node", "JSON object", "xml"]);
  });
});

describe("collections", () => {
  it("canWrapInCollection allows scalar CLR picker types only", () => {
    expect(canWrapInCollection(LyoTypes.Int)).toBe(true);
    expect(canWrapInCollection(LyoTypes.Guid)).toBe(true);
    expect(canWrapInCollection(LyoTypes.Regex)).toBe(false);
    expect(canWrapInCollection(LyoTypes.Enum)).toBe(false);
    expect(canWrapInCollection(LyoTypes.JsonObject)).toBe(false);
    expect(canWrapInCollection(LyoTypes.IntList)).toBe(false);
  });

  it("collectionShape reads list / array from the stored name; byte[] stays a value", () => {
    expect(collectionShape(LyoTypes.IntList.fullName)).toBe("List");
    expect(collectionShape("System.String[]")).toBe("Array");
    expect(collectionShape("System.Byte[]")).toBe("Value");
    expect(collectionShape("int")).toBe("Value");
  });

  it("composeFullName wraps element types and leaves unwrappable ones alone", () => {
    expect(composeFullName("System.Int32", "List")).toBe(LyoTypes.IntList.fullName);
    expect(composeFullName("System.Boolean", "Array")).toBe("System.Boolean[]");
    expect(composeFullName("System.Boolean", "List")).toBe(
      "System.Collections.Generic.List`1[[System.Boolean, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]"
    );
    expect(composeFullName("System.Uri", "List")).toContain("[[System.Uri, System.Private.Uri,");
    expect(composeFullName("regex", "List")).toBe("System.Text.RegularExpressions.Regex");
    expect(composeFullName("int", "Value")).toBe("System.Int32");
    expect(composeFullName(CUSTOM_TYPE_TOKEN, "List")).toBe(CUSTOM_TYPE_TOKEN);
    expect(composeFullName(" My.Custom ", "List")).toBe("My.Custom");
  });

  it("elementFullName unwraps collections and normalizes scalars", () => {
    expect(elementFullName(LyoTypes.IntList.fullName)).toBe("System.Int32");
    expect(elementFullName("int")).toBe("System.Int32");
    expect(elementFullName("My.Custom")).toBe("My.Custom");
  });
});

describe("concrete names (Custom / Enum)", () => {
  it("needsConcreteFullName for custom, enum, and unknown", () => {
    expect(needsConcreteFullName(CUSTOM_TYPE_TOKEN)).toBe(true);
    expect(needsConcreteFullName("enum")).toBe(true);
    expect(needsConcreteFullName("My.Custom")).toBe(true);
    expect(needsConcreteFullName("int")).toBe(false);
  });

  it("display hides the enum sentinel and clearing an enum restores it", () => {
    expect(concreteFullNameDisplay("System.Enum")).toBe("");
    expect(concreteFullNameDisplay("My.Status")).toBe("My.Status");
    expect(concreteFullNameFromInput("enum", "")).toBe("System.Enum");
    expect(concreteFullNameFromInput("My.Custom", " ")).toBe("");
    expect(concreteFullNameFromInput("enum", " My.Status ")).toBe("My.Status");
  });
});

describe("values", () => {
  it("chipValidation returns a pattern for numeric, bool, and guid elements", () => {
    const ints = chipValidation("List<int>")!;
    expect(ints.pattern.test("12")).toBe(true);
    expect(ints.pattern.test("1.2")).toBe(false);
    expect(chipValidation("decimal")!.pattern.test(".5")).toBe(true);
    expect(chipValidation("bool")!.pattern.test("TRUE")).toBe(true);
    expect(chipValidation("List<Guid>")!.pattern.test("0190F4A2-7C1B-7E3D-9A0B-1A2B3C4D5E6F")).toBe(true);
    expect(chipValidation("string")).toBeNull();
  });

  it("shouldResetValue when identity or list shape changes", () => {
    expect(shouldResetValue("int", "int")).toBe(false);
    expect(shouldResetValue("System.Int32", "long")).toBe(true);
    expect(shouldResetValue("int", LyoTypes.IntList.fullName)).toBe(true);
    expect(shouldResetValue("A.Custom", "B.Custom")).toBe(false);
    expect(shouldResetValue("enum", "My.Status")).toBe(false);
    expect(shouldResetValue("int", "Int32")).toBe(false);
  });

  it("toListKind uses the element category", () => {
    expect(toListKind("List<int>")).toBe("Number");
    expect(toListKind("decimal")).toBe("Number");
    expect(toListKind("bool")).toBe("Bool");
    expect(toListKind("string[]")).toBe("String");
  });

  it("defaultJsonFor and isWideEditor", () => {
    expect(defaultJsonFor("int")).toBe("0");
    expect(defaultJsonFor("Guid")).toBe('"00000000-0000-0000-0000-000000000000"');
    expect(defaultJsonFor("My.Custom")).toBe("null");
    expect(isWideEditor("JSON object")).toBe(true);
    expect(isWideEditor("regex")).toBe(true);
    expect(isWideEditor("My.Custom")).toBe(true);
    expect(isWideEditor("int")).toBe(false);
  });

  it("valuePreview trims and truncates", () => {
    expect(valuePreview(null)).toBe("—");
    expect(valuePreview("null")).toBe("—");
    expect(valuePreview(" 42 ")).toBe("42");
    expect(valuePreview('"abcdefghij"', 5)).toBe('"abcd…');
  });

  it("validateTypedJson checks the payload against the editor kind", () => {
    expect(validateTypedJson("int", "")).toBe(true);
    expect(validateTypedJson("int", "3")).toBe(true);
    expect(validateTypedJson("int", "1.5")).toBe(false);
    expect(validateTypedJson("decimal", "1.5")).toBe(true);
    expect(validateTypedJson("bool", '"yes"')).toBe(false);
    expect(validateTypedJson("Guid", '"nope"')).toBe(false);
    expect(validateTypedJson("Guid", '"0190f4a2-7c1b-7e3d-9a0b-1a2b3c4d5e6f"')).toBe(true);
    expect(validateTypedJson("Uri", '"87gg"')).toBe(false);
    expect(validateTypedJson("Uri", '"https://lyo.dev/a"')).toBe(true);
    expect(validateTypedJson("Uri", '"/relative/path"')).toBe(true);
    expect(validateTypedJson("regex", '"("')).toBe(false);
    expect(validateTypedJson("List<int>", "[1,2]")).toBe(true);
    expect(validateTypedJson("List<int>", "{}")).toBe(false);
    expect(validateTypedJson("JSON object", "{bad")).toBe(false);
    expect(validateTypedJson("My.Custom", '{"a":1}')).toBe(true);
  });
});
