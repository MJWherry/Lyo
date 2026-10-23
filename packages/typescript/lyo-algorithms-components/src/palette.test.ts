import {describe, expect, it} from "vitest";
import {bareNames, suggestions} from "./palette.js";

describe("suggestions", () => {
  it("suggestions_PrefixAt_IncludesBagNameAndOmitsAtan", () => {
    const source = "Atk * 2";
    expect(bareNames(source)).toEqual(["Atk"]);
    const list = suggestions("At", bareNames(source), ["attack.physical"]);
    expect(list).toContain("Atk");
    expect(list).not.toContain("Math.Atan");
  });
});
