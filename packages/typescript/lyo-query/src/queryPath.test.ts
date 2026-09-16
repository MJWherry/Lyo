import { describe, expect, it } from "vitest";
import { queryPathError } from "./queryPath.js";

describe("queryPathError", () => {
  it("accepts a property path and every window form", () => {
    expect(queryPathError("LastName")).toBeNull();
    expect(queryPathError("Addresses.City")).toBeNull();
    expect(queryPathError("LastName[0]")).toBeNull();
    expect(queryPathError("LastName[0:]")).toBeNull();
    expect(queryPathError("LastName[:3]")).toBeNull();
    expect(queryPathError("LastName[0:3]")).toBeNull();
    expect(queryPathError("LastName[:]")).toBeNull();
    expect(queryPathError("Addresses[0].City")).toBeNull();
    expect(queryPathError("Addresses[:3].City")).toBeNull();
    expect(queryPathError("Addresses[0].City[:2]")).toBeNull();
  });

  it("rejects a bad range, a blank, and anything that is not letters, dots, and a window", () => {
    expect(queryPathError("")).toBe("Field name is required.");
    expect(queryPathError("First_Name")).toMatch(/letters/);
    expect(queryPathError("LastName[]")).toBe("is not a valid range.");
    expect(queryPathError("LastName[-1]")).toBe("is not a valid range.");
    expect(queryPathError("LastName[2:2]")).toBe("is not a valid range.");
    expect(queryPathError("LastName[3:1]")).toBe("is not a valid range.");
    expect(queryPathError("LastName[0:3")).toMatch(/letters/);
  });
});
