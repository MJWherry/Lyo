import { describe, expect, it } from "vitest";
import { hueColor } from "../display/chips.js";
import { HEATMAP_EMPTY, HEATMAP_HUE, aggregateHeatmap, heatFill, heatmapView } from "./heatmap.js";

describe("heatmap", () => {
  it("aggregateHeatmap_PostalAndName_SumToTheSameState", () => {
    const model = aggregateHeatmap([
      { state: "tx", value: 2 },
      { state: "Texas", county: "Harris", value: 3 },
    ]);
    expect(model.states["48"]).toBe(5);
    expect(model.counties["48201"]).toBe(3);
  });

  it("aggregateHeatmap_ParishAndFips_ResolveToTheSameCounty", () => {
    const model = aggregateHeatmap([
      { state: "LA", county: "Orleans Parish", value: 2 },
      { state: "Louisiana", county: "22071", value: 3 },
    ]);
    expect(model.counties["22071"]).toBe(5);
    expect(model.states["22"]).toBe(5);
  });

  it("aggregateHeatmap_StateOnlyRow_CountsOnTheStateAndStaysUnassigned", () => {
    const model = aggregateHeatmap([{ state: "Texas", value: 4 }]);
    expect(model.states["48"]).toBe(4);
    expect(model.unassigned["48"]).toBe(4);
    const texasCounties = Object.entries(model.counties).filter(([fips, value]) => fips.startsWith("48") && value > 0);
    expect(texasCounties).toEqual([]);
  });

  it("aggregateHeatmap_AmbiguousOrUnknown_IsReportedUnmatched", () => {
    const model = aggregateHeatmap([
      { state: "MD", county: "Baltimore", value: 4 },
      { state: "Narnia", value: 7 },
    ]);
    expect(model.unmatched).toEqual([
      { state: "MD", county: "Baltimore", value: 4 },
      { state: "Narnia", value: 7 },
    ]);
    expect(model.states["24"] ?? 0).toBe(0);
    expect(model.counties["24005"] ?? 0).toBe(0);
    expect(model.counties["24510"] ?? 0).toBe(0);
  });

  it("aggregateHeatmap_MissingValue_CountsAsOne", () => {
    const model = aggregateHeatmap([{ state: "TX", county: "Harris" }]);
    expect(model.states["48"]).toBe(1);
    expect(model.counties["48201"]).toBe(1);
  });

  it("heatmapView_National_UsesStateTotals", () => {
    const model = aggregateHeatmap([
      { state: "TX", county: "Harris", value: 3 },
      { state: "TX", value: 2 },
      { state: "CA", value: 9 },
    ]);
    const view = heatmapView(model);
    expect(view.level).toBe("nation");
    expect(view.max).toBe(9);
    expect(view.regions.find((region) => region.postal === "TX")?.value).toBe(5);
    expect(view.regions.find((region) => region.postal === "CA")?.value).toBe(9);
  });

  it("heatmapView_State_UsesThatStatesCounties", () => {
    const model = aggregateHeatmap([
      { state: "TX", county: "Harris", value: 3 },
      { state: "LA", county: "Orleans", value: 8 },
    ]);
    const view = heatmapView(model, "48");
    expect(view.level).toBe("state");
    expect(view.regions.find((region) => region.fips === "48201")?.value).toBe(3);
    expect(view.regions.some((region) => region.fips.startsWith("22"))).toBe(false);
  });

  it("heatFill_ZeroIsNeutral_MaxIsFull", () => {
    expect(heatFill(0, 8)).toBe(HEATMAP_EMPTY);
    expect(heatFill(8, 8)).toBe(hueColor(HEATMAP_HUE));
  });
});
