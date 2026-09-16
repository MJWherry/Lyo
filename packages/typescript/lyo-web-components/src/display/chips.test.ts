import { describe, expect, it } from "vitest";
import { chipOf, chipFromBool, chipFromEnum, hueColor, LyoChipHues, normalizeHue } from "./chips.js";
import { createStatusPaletteResolver, humanizeStatus, normalizeStatus, resolveDefaultStatus } from "./status.js";

describe("chip hues", () => {
  it("wraps degrees onto 0-359", () => {
    expect(normalizeHue(-10)).toBe(350);
    expect(normalizeHue(370)).toBe(10);
    expect(LyoChipHues.Rose).toBe(332);
  });

  it("builds an hsl colour", () => {
    expect(hueColor(LyoChipHues.Blue)).toBe("hsl(228 72% 50%)");
  });
});

describe("chip specs", () => {
  it("chipOf turns blanks into an em dash", () => {
    expect(chipOf(" ").label).toBe("—");
    expect(chipOf("Ok", { color: "success" })).toEqual({ label: "Ok", color: "success", variant: "filled" });
  });

  it("chipFromEnum keeps unknown text with the default colour", () => {
    const color = (v: string) => (v === "Active" ? "success" : "default") as const;
    expect(chipFromEnum("active", ["Active", "Retired"], color)).toMatchObject({ label: "Active", color: "success" });
    expect(chipFromEnum("Weird", ["Active"], color)).toMatchObject({ label: "Weird", color: "default" });
  });

  it("chipFromBool maps true/false/null", () => {
    expect(chipFromBool(true, "Yes", "No", "success", "error").label).toBe("Yes");
    expect(chipFromBool(false, "Yes", "No", "success", "error").color).toBe("error");
    expect(chipFromBool(null, "Yes", "No", "success", "error").label).toBe("—");
  });
});

describe("status text", () => {
  it("normalizes separators and casing", () => {
    expect(normalizeStatus("Partially_Delivered")).toBe("partially delivered");
    expect(normalizeStatus("  partially--delivered ")).toBe("partially delivered");
    expect(normalizeStatus(null)).toBe("");
    expect(humanizeStatus("in_progress")).toBe("In Progress");
  });
});

describe("default status palette", () => {
  it("maps the shared vocabulary", () => {
    expect(resolveDefaultStatus("Succeeded")).toMatchObject({ label: "Succeeded", color: "success", icon: "check" });
    expect(resolveDefaultStatus("timed_out")).toMatchObject({ color: "error", icon: "timer" });
    expect(resolveDefaultStatus("RUNNING")).toMatchObject({ color: "info", icon: "play" });
    expect(resolveDefaultStatus("paused")).toMatchObject({ color: "default", icon: "pause" });
    expect(resolveDefaultStatus("mystery")).toBeNull();
  });

  it("resolver tries the domain palette first, then default, then raw text", () => {
    const resolver = createStatusPaletteResolver([
      { name: "job", resolve: (s) => (normalizeStatus(s) === "running" ? { label: "Running", color: "warning" } : null) },
    ]);
    expect(resolver.resolve("Running", "job").color).toBe("warning");
    expect(resolver.resolve("Running").color).toBe("info");
    expect(resolver.resolve("Mystery", "job")).toEqual({ label: "Mystery", color: "default", variant: "filled" });
    expect(resolver.resolve("", "job").label).toBe("—");
  });
});
