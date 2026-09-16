import { describe, expect, it } from "vitest";
import {
  ROOT_PATH_KEY,
  addChild,
  ancestorKeys,
  collapseAll,
  containerSummary,
  copyText,
  displayValue,
  expandAll,
  flattenVisible,
  getAt,
  parseEditValue,
  pathKey,
  removeAt,
  renameKey,
  searchMatches,
  setAt,
  stepMatch,
  valueKind,
} from "./jsonTree.js";

const doc = { name: "Ada", tags: ["x", "y"], meta: { active: true, score: 3.5, note: null } };

describe("valueKind", () => {
  it("distinguishes every JSON kind", () => {
    expect(valueKind("a")).toBe("string");
    expect(valueKind(1)).toBe("number");
    expect(valueKind(false)).toBe("boolean");
    expect(valueKind(null)).toBe("null");
    expect(valueKind(undefined)).toBe("null");
    expect(valueKind({})).toBe("object");
    expect(valueKind([])).toBe("array");
  });
});

describe("pathKey", () => {
  it("renders a readable path with dot, index, and quoted segments", () => {
    expect(pathKey([])).toBe(ROOT_PATH_KEY);
    expect(pathKey(["meta", "active"])).toBe("$.meta.active");
    expect(pathKey(["tags", 1])).toBe("$.tags[1]");
    expect(pathKey(["a b", "c.d"])).toBe('$["a b"]["c.d"]');
  });

  it("lists ancestor keys from the root down", () => {
    expect(ancestorKeys(["meta", "active"])).toEqual(["$", "$.meta"]);
  });
});

describe("flattenVisible", () => {
  it("shows the root and only expanded containers", () => {
    const rows = flattenVisible(doc, new Set([ROOT_PATH_KEY]));
    expect(rows.map((r) => [r.key, r.depth, r.kind, r.expandable])).toEqual([
      ["", 0, "object", true],
      ["name", 1, "string", false],
      ["tags", 1, "array", true],
      ["meta", 1, "object", true],
    ]);
    expect(rows[2]!.path).toEqual(["tags"]);
    expect(rows[2]!.expanded).toBe(false);
  });

  it("uses [i] keys for array items when expanded", () => {
    const rows = flattenVisible(doc, new Set([ROOT_PATH_KEY, "$.tags"]));
    expect(rows.filter((r) => r.depth === 2).map((r) => [r.key, r.id])).toEqual([
      ["[0]", "$.tags[0]"],
      ["[1]", "$.tags[1]"],
    ]);
  });

  it("marks empty containers as not expandable", () => {
    expect(flattenVisible({ a: {} }, new Set([ROOT_PATH_KEY]))[1]!.expandable).toBe(false);
  });
});

describe("expandAll / collapseAll", () => {
  it("expandAll collects every non-empty container", () => {
    expect([...expandAll(doc)].sort()).toEqual(["$", "$.meta", "$.tags"]);
  });

  it("collapseAll keeps only the root open", () => {
    expect([...collapseAll()]).toEqual([ROOT_PATH_KEY]);
  });
});

describe("search", () => {
  it("matches keys and primitive values case-insensitively in document order", () => {
    expect(searchMatches(doc, "A")).toEqual(["$.name", "$.tags", "$.meta", "$.meta.active"]);
    expect(searchMatches(doc, "3.5")).toEqual(["$.meta.score"]);
    expect(searchMatches(doc, "  ")).toEqual([]);
  });

  it("stepMatch wraps in both directions and starts at the ends", () => {
    expect(stepMatch(3, -1, 1)).toBe(0);
    expect(stepMatch(3, -1, -1)).toBe(2);
    expect(stepMatch(3, 2, 1)).toBe(0);
    expect(stepMatch(3, 0, -1)).toBe(2);
    expect(stepMatch(0, 0, 1)).toBe(-1);
  });
});

describe("immutable edits", () => {
  it("getAt reads nested values and returns undefined for missing paths", () => {
    expect(getAt(doc, ["tags", 1])).toBe("y");
    expect(getAt(doc, ["nope", 0])).toBeUndefined();
    expect(getAt(doc, [])).toBe(doc);
  });

  it("setAt replaces without mutating the input", () => {
    const next = setAt(doc, ["meta", "score"], 4) as typeof doc;
    expect(next.meta.score).toBe(4);
    expect(doc.meta.score).toBe(3.5);
    expect(next.tags).toBe(doc.tags);
    expect(setAt(doc, [], 1)).toBe(1);
  });

  it("removeAt drops an object key or array item", () => {
    expect(removeAt(doc, ["tags", 0])).toMatchObject({ tags: ["y"] });
    expect(Object.keys(removeAt(doc, ["name"]) as object)).toEqual(["tags", "meta"]);
    expect(doc.tags).toEqual(["x", "y"]);
  });

  it("addChild adds a unique newField key to objects", () => {
    const once = addChild({ newField: 1 }, []) as Record<string, unknown>;
    expect(once).toEqual({ newField: 1, newField_1: null });
  });

  it("addChild appends an exemplar-shaped default to arrays", () => {
    expect(addChild({ a: ["s"] }, ["a"])).toEqual({ a: ["s", ""] });
    expect(addChild({ a: [{ n: 1, b: true }] }, ["a"])).toEqual({ a: [{ n: 1, b: true }, { n: 0, b: false }] });
    expect(addChild({ a: [] }, ["a"])).toEqual({ a: [null] });
  });

  it("renameKey keeps key order and de-duplicates collisions", () => {
    expect(Object.keys(renameKey(doc, ["tags"], "labels") as object)).toEqual(["name", "labels", "meta"]);
    expect(Object.keys(renameKey(doc, ["tags"], "name") as object)).toEqual(["name", "name_1", "meta"]);
  });
});

describe("display and copy", () => {
  it("containerSummary shows a count badge", () => {
    expect(containerSummary({ a: 1, b: 2 })).toBe("{…} 2");
    expect(containerSummary([1])).toBe("[…] 1");
    expect(containerSummary("x")).toBeNull();
  });

  it("displayValue quotes strings and prints null", () => {
    expect(displayValue("hi")).toBe('"hi"');
    expect(displayValue(null)).toBe("null");
    expect(displayValue(2)).toBe("2");
  });

  it("copyText gives raw strings and pretty JSON for containers", () => {
    expect(copyText(doc, ["name"])).toBe("Ada");
    expect(copyText(doc, ["tags"])).toBe('[\n  "x",\n  "y"\n]');
    expect(copyText(doc, ["meta", "note"])).toBe("null");
  });

  it("parseEditValue reads JSON literals and falls back to the raw string", () => {
    expect(parseEditValue("42")).toBe(42);
    expect(parseEditValue("true")).toBe(true);
    expect(parseEditValue("NULL")).toBeNull();
    expect(parseEditValue('{"a":1}')).toEqual({ a: 1 });
    expect(parseEditValue("hello")).toBe("hello");
  });
});
