import { describe, expect, it } from "vitest";
import { countyPaths, nationPaths } from "./geo.js";

describe("geo", () => {
  it("nationPaths_DrawsTexasAlaskaAndHawaii", () => {
    const paths = nationPaths();
    for (const fips of ["48", "02", "15"]) {
      expect(paths.find((region) => region.fips === fips)?.d.startsWith("M")).toBe(true);
    }
  });

  it("countyPaths_TexasIncludesHarrisAndOmitsOrleans", () => {
    const paths = countyPaths("48");
    expect(paths.find((region) => region.fips === "48201")?.d.startsWith("M")).toBe(true);
    expect(paths.some((region) => region.fips.startsWith("22"))).toBe(false);
  });

  it("countyPaths_AlaskaAndHawaiiAreDrawnOnTheirOwnProjection", () => {
    expect(countyPaths("02").some((region) => region.d.startsWith("M"))).toBe(true);
    expect(countyPaths("15").some((region) => region.d.startsWith("M"))).toBe(true);
  });
});
