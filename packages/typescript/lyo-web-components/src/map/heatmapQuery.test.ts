import { describe, expect, it } from "vitest";
import { heatmapCountQuery, heatmapRowsFromCounts } from "./heatmapQuery.js";

describe("heatmapQuery", () => {
  it("heatmapCountQuery_Nation_GroupsByStateAndCounts", () => {
    const query = heatmapCountQuery();
    expect(query.Select).toEqual(["State"]);
    expect(query.Aggregates).toEqual([{ Function: "Count", Name: "Count", Field: "Id" }]);
    expect(query.SortBy).toEqual([{ PropertyName: "Count", Direction: "Desc", Priority: 0 }]);
    expect(query.Options).toMatchObject({ TotalCountMode: "None", IncludeFilterMode: "Full", ZipSiblingCollectionSelections: true });
    expect(query.whereClause).toBeUndefined();
    expect(query.Amount).toBe(200);
  });

  it("heatmapCountQuery_State_FiltersStateAndGroupsByCounty", () => {
    const query = heatmapCountQuery("PA");
    expect(query.Select).toEqual(["State", "County"]);
    expect(query.whereClause).toEqual({ $type: "condition", Field: "State", Comparison: "Equals", Value: "PA" });
    expect(query.Aggregates).toEqual([{ Function: "Count", Name: "Count", Field: "Id" }]);
    expect(query.Amount).toBeGreaterThanOrEqual(254);
  });

  it("heatmapRowsFromCounts_StateTotals_UseCount", () => {
    const rows = heatmapRowsFromCounts([
      { State: "NY", Count: 73428 },
      { State: "NJ", Count: 69735 },
    ]);
    expect(rows).toEqual([
      { state: "NY", value: 73428 },
      { state: "NJ", value: 69735 },
    ]);
  });

  it("heatmapRowsFromCounts_CountyTotals_KeepCounty", () => {
    const rows = heatmapRowsFromCounts([
      { State: "PA", County: "Philadelphia", Count: 72228 },
      { State: "PA", County: "Montgomery", Count: 55955 },
    ]);
    expect(rows).toEqual([
      { state: "PA", county: "Philadelphia", value: 72228 },
      { state: "PA", county: "Montgomery", value: 55955 },
    ]);
  });
});
