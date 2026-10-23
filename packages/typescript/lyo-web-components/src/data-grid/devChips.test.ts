import { describe, expect, it } from "vitest";
import { responseDevChips } from "./devChips.js";

describe("responseDevChips", () => {
  it("builds elapsed, status, count/total, score, and has-more chips in order", () => {
    const chips = responseDevChips({ elapsedMs: 850, statusCode: 200, itemCount: 25, total: 1234, queryScore: 7, hasMore: true }, "en-US");
    expect(chips).toEqual([
      { label: "850 ms", color: "success", icon: "timer", variant: "filled" },
      { label: "200", color: "success", variant: "filled" },
      { label: "25 / 1,234", color: "default", variant: "filled" },
      { label: "Score 7", color: "default", variant: "filled" },
      { label: "Has more", color: "info", variant: "filled" },
    ]);
  });

  it("colours client errors warning and server errors error", () => {
    expect(responseDevChips({ statusCode: 404 })[0]!.color).toBe("warning");
    expect(responseDevChips({ statusCode: 503 })[0]!.color).toBe("error");
    expect(responseDevChips({ statusCode: 302 })[0]!.color).toBe("default");
  });

  it("dashes a missing side of count/total and skips absent metrics", () => {
    expect(responseDevChips({ itemCount: 3 }, "en-US")).toEqual([{ label: "3 / —", color: "default", variant: "filled" }]);
    expect(responseDevChips({ hasMore: false })).toEqual([]);
  });
});
