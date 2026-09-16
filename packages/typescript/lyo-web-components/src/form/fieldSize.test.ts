import { describe, expect, it } from "vitest";
import { autoFieldSize, fieldSizeColumns, resolveFieldSize } from "./fieldSize.js";

describe("fieldSizeColumns", () => {
  it("maps each size to xs / sm / md grid columns", () => {
    expect(fieldSizeColumns("Small")).toEqual({ xs: 6, sm: 4, md: 3 });
    expect(fieldSizeColumns("Medium")).toEqual({ xs: 12, sm: 6, md: 4 });
    expect(fieldSizeColumns("Large")).toEqual({ xs: 12, sm: 12, md: 6 });
    expect(fieldSizeColumns("Full")).toEqual({ xs: 12, sm: 12, md: 12 });
  });
});

describe("autoFieldSize", () => {
  it("bools, numbers, dates, and times are small", () => {
    for (const kind of ["bool", "number", "integer", "date", "time"] as const) expect(autoFieldSize(kind)).toBe("Small");
  });

  it("enums, flags, date-times, and GUIDs are medium", () => {
    for (const kind of ["enum", "flags", "datetime", "guid"] as const) expect(autoFieldSize(kind)).toBe("Medium");
  });

  it("strings are large; multiline and rich text are full width", () => {
    expect(autoFieldSize("string")).toBe("Large");
    expect(autoFieldSize("string", { multiline: true })).toBe("Full");
    expect(autoFieldSize("richtext")).toBe("Full");
  });
});

describe("resolveFieldSize", () => {
  it("an explicit size wins over the derived one", () => {
    expect(resolveFieldSize("Full", "bool")).toEqual({ xs: 12, sm: 12, md: 12 });
    expect(resolveFieldSize("Auto", "bool")).toEqual({ xs: 6, sm: 4, md: 3 });
    expect(resolveFieldSize(undefined, "guid")).toEqual({ xs: 12, sm: 6, md: 4 });
  });
});
