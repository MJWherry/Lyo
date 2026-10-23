import { describe, expect, it } from "vitest";
import { filterLogEntries, formatLogEntries, logLevelColor, type LyoLogEntry } from "./log.js";

const entries: LyoLogEntry[] = [
  { timestamp: "2026-01-01T00:00:00.000Z", level: "Debug", message: "booting", source: "Host" },
  { timestamp: "2026-01-01T00:00:01.000Z", level: "Information", message: "ready", source: "Api" },
  { timestamp: "2026-01-01T00:00:02.000Z", level: "Error", message: "db down", source: "Postgres" },
];

describe("filterLogEntries", () => {
  it("filters by minimum level", () => {
    expect(filterLogEntries(entries, "Information").map((e) => e.message)).toEqual(["ready", "db down"]);
  });

  it("searches message and source case-insensitively", () => {
    expect(filterLogEntries(entries, "Trace", "POSTGRES").map((e) => e.message)).toEqual(["db down"]);
    expect(filterLogEntries(entries, "Trace", "boot").map((e) => e.message)).toEqual(["booting"]);
    expect(filterLogEntries(entries, "Trace", "  ")).toHaveLength(3);
  });
});

describe("formatLogEntries", () => {
  it("writes one line per entry", () => {
    expect(formatLogEntries(entries.slice(0, 1))).toBe("2026-01-01T00:00:00.000Z Debug Host booting\n");
  });
});

describe("logLevelColor", () => {
  it("maps levels to chip colours", () => {
    expect(logLevelColor("Warning")).toBe("warning");
    expect(logLevelColor("Critical")).toBe("error");
    expect(logLevelColor("Information")).toBe("info");
    expect(logLevelColor("Trace")).toBe("default");
  });
});
