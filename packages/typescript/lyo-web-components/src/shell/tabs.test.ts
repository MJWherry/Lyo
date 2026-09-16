import { describe, expect, it } from "vitest";
import { resolveActiveTab, slugTab } from "./tabs.js";

describe("slugTab", () => {
  it("lowercases and collapses non-alphanumerics to one hyphen", () => {
    expect(slugTab("Run History")).toBe("run-history");
    expect(slugTab("  A / B -- C ")).toBe("a-b-c");
    expect(slugTab(null)).toBe("");
  });
});

describe("resolveActiveTab", () => {
  const ids = ["overview", "runs", "logs"];
  it("prefers the query string, then storage, then default, then first", () => {
    expect(resolveActiveTab(ids, { query: "logs", stored: "runs", defaultTab: "overview" })).toBe("logs");
    expect(resolveActiveTab(ids, { query: "nope", stored: "runs", defaultTab: "overview" })).toBe("runs");
    expect(resolveActiveTab(ids, { stored: "nope", defaultTab: "logs" })).toBe("logs");
    expect(resolveActiveTab(ids, {})).toBe("overview");
    expect(resolveActiveTab([], {})).toBeNull();
  });
});
