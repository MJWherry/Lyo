import { describe, expect, it } from "vitest";
import { diffStats, diffText, highlightSegments, paneLines, searchHits, stepHit } from "./diffOptions.js";

describe("diffText", () => {
  it("reports whitespace-only changes unless ignoreWhitespace is set", () => {
    expect(diffStats(diffText("a  b\nc\n", "a b\nc\n"))).toEqual({ added: 1, removed: 1 });
    expect(diffStats(diffText("a  b\nc\n", "a b\nc\n", { ignoreWhitespace: true }))).toEqual({ added: 0, removed: 0 });
  });

  it("reports case-only changes unless ignoreCase is set", () => {
    expect(diffStats(diffText("Hello\n", "hello\n"))).toEqual({ added: 1, removed: 1 });
    expect(diffStats(diffText("Hello\n", "hello\n", { ignoreCase: true }))).toEqual({ added: 0, removed: 0 });
  });

  it("counts lines, not parts", () => {
    expect(diffStats(diffText("a\n", "a\nb\nc\n"))).toEqual({ added: 2, removed: 0 });
  });
});

describe("paneLines", () => {
  const parts = diffText("line a\nline b\n", "line a\nline c\n");

  it("numbers each side from 1 and marks only that side's changes", () => {
    expect(paneLines(parts, "left")).toEqual([
      { number: 1, text: "line a", kind: null },
      { number: 2, text: "line b", kind: "del" },
    ]);
    expect(paneLines(parts, "right")).toEqual([
      { number: 1, text: "line a", kind: null },
      { number: 2, text: "line c", kind: "add" },
    ]);
  });

  it("keeps added and removed lines together in unified order", () => {
    expect(paneLines(parts, "unified")).toEqual([
      { number: 1, text: "line a", kind: null },
      { number: 2, text: "line b", kind: "del" },
      { number: 3, text: "line c", kind: "add" },
    ]);
  });

  it("keeps a blank line and drops a trailing newline", () => {
    expect(paneLines(diffText("a\n\nb\n", "a\n\nb\n"), "unified")).toEqual([
      { number: 1, text: "a", kind: null },
      { number: 2, text: "", kind: null },
      { number: 3, text: "b", kind: null },
    ]);
    expect(paneLines(diffText("", ""), "left")).toEqual([]);
  });
});

describe("search", () => {
  it("finds case-insensitive hits by default and none for a blank query", () => {
    expect(searchHits("Foo foo FOO", "foo")).toEqual([
      { start: 0, end: 3 },
      { start: 4, end: 7 },
      { start: 8, end: 11 },
    ]);
    expect(searchHits("Foo foo", "foo", { matchCase: true })).toEqual([{ start: 4, end: 7 }]);
    expect(searchHits("abc", "  ")).toEqual([]);
  });

  it("splits text into highlight segments, numbering hits from an offset", () => {
    expect(highlightSegments("a foo b foo", "foo", { hitOffset: 3 })).toEqual([
      { text: "a ", hit: null },
      { text: "foo", hit: 3 },
      { text: " b ", hit: null },
      { text: "foo", hit: 4 },
    ]);
    expect(highlightSegments("abc", "")).toEqual([{ text: "abc", hit: null }]);
  });

  it("steps through hits with wrap-around", () => {
    expect(stepHit(-1, 3, 1)).toBe(0);
    expect(stepHit(2, 3, 1)).toBe(0);
    expect(stepHit(0, 3, -1)).toBe(2);
    expect(stepHit(0, 0, 1)).toBe(-1);
  });
});
