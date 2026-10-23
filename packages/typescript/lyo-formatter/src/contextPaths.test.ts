import { describe, expect, it } from "vitest";
import { lyoTypeDisplayName, pickerGroups } from "lyo-web-components";
import { contextPaths, previewTemplate, suggestionsAtCaret } from "./contextPaths.js";
import { registerFormatterType } from "./register.js";

describe("contextPaths", () => {
    it("lists dotted paths and skips empty input", () => {
        expect(contextPaths(null)).toEqual([]);
        expect(contextPaths("")).toEqual([]);
        expect(contextPaths(undefined)).toEqual([]);
        expect(contextPaths({ Definition: { Name: "Sync" }, Parameters: { Region: "east" } })).toEqual([
            "Definition.Name",
            "Parameters.Region",
        ]);
    });

    it("substitutes known paths and leaves unknown tokens", () => {
        expect(previewTemplate("Hi {Definition.Name} {Missing}", { Definition: { Name: "Sync" } })).toBe("Hi Sync {Missing}");
    });

    it("templateSuggest_OpenBrace_ListsContextKeys", () => {
        const template = "Hello {";
        expect(suggestionsAtCaret(template, template.length, { Name: "Ada", Address: { City: "Paris" } })).toEqual([
            "Name",
            "Address.City",
        ]);
    });
});

describe("formatter type", () => {
    it("registers Lyo.Formatter.Template under Text, not as a category choice", () => {
        registerFormatterType();
        const text = pickerGroups().find((group) => group.category === "Text");
        expect(text?.types.map((type) => type.fullName)).toContain("Lyo.Formatter.Template");
        expect(text?.types.map((type) => lyoTypeDisplayName(type))).toEqual(["Formatter", "Regex", "String", "Uri"]);
        expect(text?.types.map((type) => lyoTypeDisplayName(type))).not.toContain("Text");
    });
});
