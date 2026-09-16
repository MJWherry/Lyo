import { describe, expect, it } from "vitest";
import { LyoTypes, allLyoTypes, lyoTypesByCategory, lyoTypeFromName, registerLyoType, isLyoCollection } from "./lyoTypeCatalog.js";

describe("catalog", () => {
  it("lists the 29 built-in rows (Unknown excluded) in declaration order", () => {
    const all = allLyoTypes();
    expect(all.length).toBeGreaterThanOrEqual(29);
    expect(all.slice(0, 3).map((t) => t.shortName)).toEqual(["string", "Uri", "bool"]);
    expect(all).not.toContain(LyoTypes.Unknown);
  });

  it("stores CLR full names", () => {
    expect(LyoTypes.Int.fullName).toBe("System.Int32");
    expect(LyoTypes.Regex.fullName).toBe("System.Text.RegularExpressions.Regex");
    expect(LyoTypes.StringArray.fullName).toBe("System.String[]");
    expect(LyoTypes.IntList.fullName).toMatch(/^System\.Collections\.Generic\.List`1\[\[System\.Int32, System\.Private\.CoreLib/);
  });
});

describe("lyoTypeFromName", () => {
  it("resolves full name, short name, CLR name, and aliases (case-insensitive, trimmed)", () => {
    expect(lyoTypeFromName("System.Int32")).toBe(LyoTypes.Int);
    expect(lyoTypeFromName("int")).toBe(LyoTypes.Int);
    expect(lyoTypeFromName("Int32")).toBe(LyoTypes.Int);
    expect(lyoTypeFromName(" INT ")).toBe(LyoTypes.Int);
    expect(lyoTypeFromName("uuid")).toBe(LyoTypes.Guid);
    expect(lyoTypeFromName("XDocument")).toBe(LyoTypes.Xml);
  });

  it("returns Unknown for blanks and unregistered names", () => {
    expect(lyoTypeFromName(null)).toBe(LyoTypes.Unknown);
    expect(lyoTypeFromName("  ")).toBe(LyoTypes.Unknown);
    expect(lyoTypeFromName("My.Custom.Type")).toBe(LyoTypes.Unknown);
  });

  it("matches registered lists regardless of the runtime version in the assembly-qualified name", () => {
    const net9 = "System.Collections.Generic.List`1[[System.Int32, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]";
    expect(lyoTypeFromName(net9)).toBe(LyoTypes.IntList);
  });

  it("synthesizes unlisted lists and arrays of known element types", () => {
    const boolArray = lyoTypeFromName("System.Boolean[]");
    expect(boolArray).toMatchObject({ shortName: "bool[]", category: "Collection", editorKind: "Collection", elementFullName: "System.Boolean", isArray: true, defaultJson: "[]" });
    const boolList = lyoTypeFromName("List<bool>");
    expect(boolList).toMatchObject({ shortName: "List<bool>", elementFullName: "System.Boolean", isArray: false });
    expect(lyoTypeFromName("System.Collections.Generic.List`1[[My.Thing, Other]]")).toBe(LyoTypes.Unknown);
  });
});

describe("categories and collections", () => {
  it("byCategory lists typed lists and arrays, byte[] is Binary", () => {
    const names = lyoTypesByCategory("Collection").map((t) => t.shortName);
    expect(names).toEqual(["List<string>", "List<int>", "List<long>", "List<Guid>", "string[]", "int[]"]);
    expect(LyoTypes.ByteArray.category).toBe("Binary");
  });

  it("isLyoCollection is true when an element type is present", () => {
    expect(isLyoCollection(LyoTypes.IntList)).toBe(true);
    expect(isLyoCollection(LyoTypes.ByteArray)).toBe(true);
    expect(isLyoCollection(LyoTypes.Int)).toBe(false);
  });
});

describe("registerLyoType", () => {
  it("adds a non-CLR row once and resolves it by alias", () => {
    const first = registerLyoType({ shortName: "template", fullName: "Test.Formatter.Template", description: "t", category: "Text", editorKind: "Formatter", defaultJson: '""', aliases: ["tpl"] });
    const again = registerLyoType({ shortName: "other", fullName: "Test.Formatter.Template", description: "x", category: "Text", editorKind: "Text", defaultJson: '""' });
    expect(again).toBe(first);
    expect(first.isClr).toBe(false);
    expect(lyoTypeFromName("TPL")).toBe(first);
    expect(allLyoTypes()).toContain(first);
  });
});
