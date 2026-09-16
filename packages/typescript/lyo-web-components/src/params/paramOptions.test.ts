import { describe, expect, it } from "vitest";
import type { QueryReq } from "lyo-query";
import {
  allowedValuesFromOptions,
  bindQueryOptions,
  bindSprocParameters,
  createDefaultOptionsJson,
  isValidStoredProcName,
  optionsEditorError,
  optionsFromAllowedValues,
  optionsKindOf,
  parseParameterOptions,
  pruneSelection,
  queryBaseRoute,
  queryPlaceholderKeys,
  readKeyValue,
  serializeParameterOptions,
  sprocPlaceholderKeys,
} from "./paramOptions.js";

const query = (where: QueryReq["whereClause"]): QueryReq => ({
  From: { Alias: "c", EntityType: "Customer" },
  Select: ["c.Id", "c.Name"],
  Options: { TotalCountMode: "None", IncludeFilterMode: "Full" },
  whereClause: where,
});

describe("kind detection", () => {
  it("reads camelCase string kinds, PascalCase kinds, and numeric kinds", () => {
    expect(optionsKindOf('{"kind":"static","items":[{"key":"a","label":"A"}]}')).toBe("Static");
    expect(optionsKindOf('{"Kind":"Query","Query":{}}')).toBe("Query");
    expect(optionsKindOf('{"kind":2}')).toBe("Sproc");
  });

  it("returns null for blank, invalid, or unknown kinds", () => {
    expect(optionsKindOf(null)).toBeNull();
    expect(optionsKindOf("{bad")).toBeNull();
    expect(optionsKindOf('{"kind":"weird"}')).toBeNull();
  });

  it("parseParameterOptions normalizes PascalCase property names", () => {
    const parsed = parseParameterOptions('{"Kind":"Sproc","StoredProcName":"public.f","SprocParameters":{"a":"{{X}}"},"KeyField":"Id"}');
    expect(parsed).toEqual({
      kind: "Sproc",
      items: [],
      queryRoute: null,
      query: null,
      storedProcName: "public.f",
      sprocParameters: { a: "{{X}}" },
      keyField: "Id",
      labelField: null,
    });
  });

  it("serializeParameterOptions writes the server's camelCase shape and drops null fields", () => {
    expect(serializeParameterOptions({ kind: "Static", items: [{ key: "a", label: "A" }] })).toBe('{"kind":"static","items":[{"key":"a","label":"A"}],"sprocParameters":{}}');
  });

  it("createDefaultOptionsJson matches the server defaults per kind", () => {
    expect(parseParameterOptions(createDefaultOptionsJson("Static"))?.items).toEqual([{ key: "key", label: "label" }]);
    expect(parseParameterOptions(createDefaultOptionsJson("Sproc"))).toMatchObject({ storedProcName: "public.report_rows", keyField: "Key", labelField: "Value" });
    expect(parseParameterOptions(createDefaultOptionsJson("Query"))?.query).toMatchObject({ Select: ["c.Id", "c.Name"], Amount: 200 });
    expect(createDefaultOptionsJson(null)).toBeNull();
  });
});

describe("allowed values", () => {
  it("optionsFromAllowedValues builds static options with key = label", () => {
    expect(parseParameterOptions(optionsFromAllowedValues('["a", 2]'))?.items).toEqual([
      { key: "a", label: "a" },
      { key: "2", label: "2" },
    ]);
    expect(optionsFromAllowedValues("[]")).toBeNull();
  });

  it("allowedValuesFromOptions round-trips static keys in the list kind's wire shape and ignores query options", () => {
    const json = serializeParameterOptions({ kind: "Static", items: [{ key: "1", label: "One" }, { key: "2", label: "Two" }] });
    expect(allowedValuesFromOptions(json, "Number")).toBe("[1,2]");
    expect(allowedValuesFromOptions(json)).toBe('["1","2"]');
    expect(allowedValuesFromOptions(createDefaultOptionsJson("Query"))).toBeNull();
  });
});

describe("{{Key}} binding", () => {
  const where: QueryReq["whereClause"] = {
    $type: "group",
    Operator: "And",
    Children: [
      { $type: "condition", Field: "c.RegionId", Comparison: "Equals", Value: "{{ Region }}" },
      { $type: "condition", Field: "c.Tag", Comparison: "In", Value: ["{{tag}}", "fixed"] },
    ],
  };

  it("queryPlaceholderKeys collects distinct keys from where values, sorted case-insensitively", () => {
    expect(queryPlaceholderKeys(query(where))).toEqual(["Region", "tag"]);
  });

  it("bindQueryOptions substitutes sibling values case-insensitively without mutating the template", () => {
    const template = query(where);
    const result = bindQueryOptions(template, { region: "7", TAG: "vip" });
    expect(result.missing).toEqual([]);
    expect(result.bound?.whereClause).toEqual({
      $type: "group",
      Operator: "And",
      Children: [
        { $type: "condition", Field: "c.RegionId", Comparison: "Equals", Value: "7" },
        { $type: "condition", Field: "c.Tag", Comparison: "In", Value: ["vip", "fixed"] },
      ],
    });
    expect(template.whereClause).toEqual(where);
  });

  it("bindQueryOptions reports keys whose sibling is missing or blank", () => {
    expect(bindQueryOptions(query(where), { Region: " " })).toEqual({ bound: null, missing: ["Region", "tag"] });
  });

  it("sprocPlaceholderKeys / bindSprocParameters bind argument templates", () => {
    expect(sprocPlaceholderKeys({ a: "{{Start}}-{{End}}", b: "lit" })).toEqual(["End", "Start"]);
    expect(bindSprocParameters({ a: "{{Start}}-{{End}}", b: "lit" }, { start: "1", end: "2" })).toEqual({ bound: { a: "1-2", b: "lit" }, missing: [] });
    expect(bindSprocParameters({ a: "{{Start}}" }, {})).toEqual({ bound: null, missing: ["Start"] });
  });
});

describe("readKeyValue", () => {
  it("prefers Key / Value columns, then the first two select paths, then a single select path", () => {
    expect(readKeyValue({ key: 1, value: "One" }, ["c.Id"])).toEqual({ key: "1", label: "One" });
    expect(readKeyValue({ Id: "a", Name: "Alpha" }, ["c.Id", "c.Name"])).toEqual({ key: "a", label: "Alpha" });
    expect(readKeyValue({ Id: "a" }, ["c.Id"])).toEqual({ key: "a", label: "a" });
    expect(readKeyValue({ Other: 1 }, ["c.Id"])).toBeNull();
  });

  it("uses explicit key / label fields when given", () => {
    expect(readKeyValue({ code: "x", title: "X" }, null, { keyField: "Code", labelField: "Title" })).toEqual({ key: "x", label: "X" });
    expect(readKeyValue({ code: "x" }, null, { keyField: "code" })).toEqual({ key: "x", label: "x" });
  });
});

describe("pruneSelection", () => {
  const items = [
    { key: "a", label: "A" },
    { key: "b", label: "B" },
  ];

  it("clears a single value that is no longer an option (case-insensitive match keeps it)", () => {
    expect(pruneSelection("z", items)).toBeNull();
    expect(pruneSelection("A", items)).toBe("A");
  });

  it("filters a multi value to known keys and serializes with the list kind", () => {
    expect(pruneSelection('["a","z","b"]', items, { multiple: true })).toBe('["a","b"]');
    expect(pruneSelection('["z"]', items, { multiple: true })).toBeNull();
  });

  it("leaves the value alone when there are no items or the type is a formatter template", () => {
    expect(pruneSelection("z", [])).toBe("z");
    expect(pruneSelection("z", items, { keepCustom: true })).toBe("z");
  });
});

describe("editor validation", () => {
  it("isValidStoredProcName requires schema.func", () => {
    expect(isValidStoredProcName("public.report_rows")).toBe(true);
    expect(isValidStoredProcName("report_rows")).toBe(false);
    expect(isValidStoredProcName("public.1bad")).toBe(false);
  });

  it("optionsEditorError explains what is missing for sproc and query options", () => {
    expect(optionsEditorError({ kind: "Sproc", storedProcName: "nope" })).toBe("Stored proc name must be schema.func (letters, digits, underscore).");
    expect(optionsEditorError({ kind: "Query", query: { ...query(null), From: { Alias: "c", EntityType: "" } } })).toBe(
      "Set From.EntityType (table) so the Value picker can load options."
    );
    expect(optionsEditorError({ kind: "Query", query: { ...query(null), Select: [] } })).toBe("Add at least one Select path (e.g. c.Id, c.Name).");
    expect(optionsEditorError({ kind: "Static", items: [{ key: "a", label: "a" }] })).toBeNull();
  });

  it("queryBaseRoute maps the stored route onto the client's base route", () => {
    expect(queryBaseRoute(null)).toBe("/");
    expect(queryBaseRoute("Query")).toBe("/");
    expect(queryBaseRoute("Reporting/Query")).toBe("/Reporting");
  });
});
