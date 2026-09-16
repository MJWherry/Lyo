import { describe, expect, it } from "vitest";
import { formatResultErrors, resolveResultPhase } from "./resultPhase.js";

describe("resolveResultPhase", () => {
  it("loading wins, then error, then empty, then content", () => {
    expect(resolveResultPhase({ loading: true, result: { isSuccess: true, data: [1] } })).toBe("loading");
    expect(resolveResultPhase({ result: null })).toBe("loading");
    expect(resolveResultPhase({ result: { isSuccess: false, errors: [] } })).toBe("error");
    expect(resolveResultPhase({ result: { isSuccess: true, data: [] } })).toBe("empty");
    expect(resolveResultPhase({ result: { isSuccess: true, data: "" } })).toBe("empty");
    expect(resolveResultPhase({ result: { isSuccess: true, data: null } })).toBe("empty");
    expect(resolveResultPhase({ result: { isSuccess: true, data: [1] } })).toBe("content");
  });

  it("uses a custom emptiness predicate", () => {
    const isEmpty = (d: { items: unknown[] }) => d.items.length === 0;
    expect(resolveResultPhase({ result: { isSuccess: true, data: { items: [] } }, isEmpty })).toBe("empty");
  });
});

describe("formatResultErrors", () => {
  it("joins descriptions and falls back when there are none", () => {
    expect(formatResultErrors([{ code: "X", description: "Bad" }, { description: "Worse" }])).toBe("Bad\nWorse");
    expect(formatResultErrors([{ code: "X" }])).toBe("X");
    expect(formatResultErrors([])).toBe("The operation failed.");
  });
});
