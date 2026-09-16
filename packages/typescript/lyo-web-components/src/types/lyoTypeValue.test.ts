import { describe, expect, it } from "vitest";
import { LyoTypes } from "./lyoTypeCatalog.js";
import { editorModeFor, invalidChips, jsonToScalarText, parseListJson, scalarTextToJson, serializeListJson } from "./lyoTypeValue.js";

describe("list json", () => {
  it("parseListJson reads strings, numbers, and bools as canonical text and skips null / blank", () => {
    expect(parseListJson('[" a ", 1.50, true, false, null, ""]')).toEqual(["a", "1.50", "True", "False"]);
  });

  it("parseListJson returns empty for blank, invalid, or non-array json", () => {
    expect(parseListJson(null)).toEqual([]);
    expect(parseListJson("{bad")).toEqual([]);
    expect(parseListJson('{"a":1}')).toEqual([]);
  });

  it("serializeListJson writes the kind's wire shape and drops values that do not parse", () => {
    expect(serializeListJson([" a ", "", "b"], "String")).toBe('["a","b"]');
    expect(serializeListJson(["1", "x", "2.5"], "Number")).toBe("[1,2.5]");
    expect(serializeListJson(["TRUE", "no", "false"], "Bool")).toBe("[true,false]");
  });

  it("serializeListJson returns null when nothing survives", () => {
    expect(serializeListJson([], "String")).toBeNull();
    expect(serializeListJson(["x"], "Number")).toBeNull();
  });
});

describe("editorModeFor", () => {
  it("routes collections to chips, documents / binary / unknown to the tree, the rest to a scalar field", () => {
    expect(editorModeFor(LyoTypes.IntList.fullName)).toBe("Collection");
    expect(editorModeFor("JSON object")).toBe("JsonTree");
    expect(editorModeFor("byte[]")).toBe("JsonTree");
    expect(editorModeFor("My.Custom")).toBe("JsonTree");
    expect(editorModeFor("int")).toBe("Scalar");
    expect(editorModeFor("enum")).toBe("Scalar");
    expect(editorModeFor("xml")).toBe("Scalar");
  });
});

describe("jsonToScalarText", () => {
  it("unwraps strings and prints numbers / bools", () => {
    expect(jsonToScalarText("string", '"hi"')).toBe("hi");
    expect(jsonToScalarText("int", "42")).toBe("42");
    expect(jsonToScalarText("bool", "true")).toBe("true");
  });

  it("uses the type default for blank json and empty text for blank json when unset is allowed", () => {
    expect(jsonToScalarText("int", "")).toBe("0");
    expect(jsonToScalarText("int", "", true)).toBe("");
  });

  it("DateTime prints a UTC datetime-local value", () => {
    expect(jsonToScalarText("DateTime", '"2024-01-02T03:04:05Z"')).toBe("2024-01-02T03:04");
    expect(jsonToScalarText("DateTimeOffset", '"2024-01-02T05:04:05+02:00"')).toBe("2024-01-02T03:04");
  });

  it("falls back to the trimmed raw text when the json is invalid", () => {
    expect(jsonToScalarText("Guid", ' "abc ')).toBe("abc");
  });
});

describe("scalarTextToJson", () => {
  it("blank text commits null when unset is allowed and JSON null otherwise", () => {
    expect(scalarTextToJson("int", " ", true)).toEqual({ json: null });
    expect(scalarTextToJson("int", "")).toEqual({ json: "null" });
  });

  it("integers must be whole numbers", () => {
    expect(scalarTextToJson("int", " 12 ")).toEqual({ json: "12" });
    expect(scalarTextToJson("long", "1.5")).toEqual({ error: "Enter an integer." });
  });

  it("decimals accept any finite number", () => {
    expect(scalarTextToJson("decimal", ".5")).toEqual({ json: "0.5" });
    expect(scalarTextToJson("double", "x")).toEqual({ error: "Enter a number." });
  });

  it("bool writes json true / false", () => {
    expect(scalarTextToJson("bool", "TRUE")).toEqual({ json: "true" });
  });

  it("Guid and Uri validate before committing", () => {
    expect(scalarTextToJson("Guid", "nope")).toEqual({ error: "Enter a GUID." });
    expect(scalarTextToJson("Guid", "0F8FAD5B-D9CB-469F-A165-70867728950E")).toEqual({ json: '"0f8fad5b-d9cb-469f-a165-70867728950e"' });
    expect(scalarTextToJson("Uri", "87gg")).toEqual({ error: "Enter an absolute URI or a path with /." });
    expect(scalarTextToJson("Uri", "https://x.test/a")).toEqual({ json: '"https://x.test/a"' });
  });

  it("temporal values serialize like System.Text.Json", () => {
    expect(scalarTextToJson("DateTime", "2024-01-02T03:04")).toEqual({ json: '"2024-01-02T03:04:00Z"' });
    expect(scalarTextToJson("DateTimeOffset", "2024-01-02T03:04")).toEqual({ json: '"2024-01-02T03:04:00+00:00"' });
    expect(scalarTextToJson("DateOnly", "2024-01-02")).toEqual({ json: '"2024-01-02"' });
    expect(scalarTextToJson("TimeOnly", "13:45")).toEqual({ json: '"13:45:00"' });
    expect(scalarTextToJson("TimeSpan", "1.02:03:04")).toEqual({ json: '"1.02:03:04"' });
    expect(scalarTextToJson("TimeSpan", "soon")).toEqual({ error: "Enter a duration like 1.02:03:04." });
  });

  it("text-like editors write a json string", () => {
    expect(scalarTextToJson("string", 'say "hi"')).toEqual({ json: '"say \\"hi\\""' });
    expect(scalarTextToJson("regex", "^a+$")).toEqual({ json: '"^a+$"' });
    expect(scalarTextToJson("enum", "Active")).toEqual({ json: '"Active"' });
  });
});

describe("invalidChips", () => {
  it("returns the chips that fail the type's chip pattern", () => {
    expect(invalidChips(LyoTypes.IntList.fullName, ["1", "x", "2"])).toEqual(["x"]);
    expect(invalidChips(LyoTypes.StringList.fullName, ["anything"])).toEqual([]);
  });
});
