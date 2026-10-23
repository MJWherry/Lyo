import { describe, expect, it } from "vitest";
import { splitHighlight, truncateText } from "./highlight.js";

describe("splitHighlight", () => {
  it("splits case-insensitively and keeps original casing", () => {
    expect(splitHighlight("John Johnson", "john")).toEqual([
      { text: "John", match: true },
      { text: " ", match: false },
      { text: "John", match: true },
      { text: "son", match: false },
    ]);
  });

  it("returns one plain segment without a query", () => {
    expect(splitHighlight("abc", "")).toEqual([{ text: "abc", match: false }]);
    expect(splitHighlight("", "a")).toEqual([]);
  });

  it("escapes regex characters in the query", () => {
    expect(splitHighlight("a.b", ".")).toEqual([
      { text: "a", match: false },
      { text: ".", match: true },
      { text: "b", match: false },
    ]);
  });
});

describe("truncateText", () => {
  it("adds an ellipsis only past the limit", () => {
    expect(truncateText("abcdef", 4)).toEqual({ text: "abcd…", truncated: true });
    expect(truncateText("abc", 4)).toEqual({ text: "abc", truncated: false });
    expect(truncateText(null, 4)).toEqual({ text: "", truncated: false });
  });
});
