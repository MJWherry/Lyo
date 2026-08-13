import { describe, expect, it } from "vitest";
import { nextSorts } from "./sorts.js";

describe("nextSorts", () => {
  it("appends a new field as Asc with next priority", () => {
    const one = nextSorts([], "Id");
    expect(one).toEqual([{ PropertyName: "Id", Direction: "Asc", Priority: 0 }]);
    const two = nextSorts(one, "LastName");
    expect(two).toEqual([
      { PropertyName: "Id", Direction: "Asc", Priority: 0 },
      { PropertyName: "LastName", Direction: "Asc", Priority: 1 },
    ]);
  });

  it("cycles the same field Asc → Desc → remove and reindexes", () => {
    let sorts = nextSorts([], "Id");
    sorts = nextSorts(sorts, "LastName");
    sorts = nextSorts(sorts, "Id");
    expect(sorts).toEqual([
      { PropertyName: "Id", Direction: "Desc", Priority: 0 },
      { PropertyName: "LastName", Direction: "Asc", Priority: 1 },
    ]);
    sorts = nextSorts(sorts, "Id");
    expect(sorts).toEqual([{ PropertyName: "LastName", Direction: "Asc", Priority: 0 }]);
  });
});
