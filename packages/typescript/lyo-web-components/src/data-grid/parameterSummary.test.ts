import { describe, expect, it } from "vitest";
import { parameterHoverSummary } from "./parameterSummary.js";

describe("parameterHoverSummary", () => {
  it("lists keys when the row has parameter objects", () => {
    expect(
      parameterHoverSummary({
        JobParameters: [{ Key: "StartDate" }, { key: "EndDate" }],
        "JobParameters.Count": 2,
      })
    ).toEqual({ keys: ["StartDate", "EndDate"], text: "StartDate, EndDate" });
  });

  it("falls back to the count when keys are absent", () => {
    expect(parameterHoverSummary({ "JobParameters.Count": 4 })).toEqual({ keys: [], text: "4" });
    expect(parameterHoverSummary({})).toEqual({ keys: [], text: "0" });
  });
});
