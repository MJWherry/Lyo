import { describe, expect, it } from "vitest";
import { clearVisible, filterUniqueValues, selectAllVisible } from "./uniqueValues.js";

const items = [
  { value: "b", count: 1 },
  { value: "a", count: 5 },
  { value: null, count: 2 },
];

describe("unique values", () => {
  it("sorts by count and filters on the formatted label", () => {
    const format = (v: string | null) => (v === "a" ? "Alpha" : (v ?? "(null)"));
    expect(filterUniqueValues(items, "", format).map((i) => i.value)).toEqual(["a", null, "b"]);
    expect(filterUniqueValues(items, "alp", format).map((i) => i.value)).toEqual(["a"]);
    expect(filterUniqueValues(items, "null", format).map((i) => i.value)).toEqual([null]);
  });

  it("select all adds every visible value to the current selection", () => {
    expect(selectAllVisible(["x"], [items[1], items[2]])).toEqual(["x", "a", ""]);
  });

  it("clear all removes only the visible values", () => {
    expect(clearVisible(["x", "a", "b"], [items[0], items[1]])).toEqual(["x"]);
  });
});
