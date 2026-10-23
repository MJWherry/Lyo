import { describe, expect, it } from "vitest";
import { durationColor, durationMsBetween, formatClock, formatDuration, formatDurationFromDates } from "./duration.js";

describe("formatDuration", () => {
  it("buckets ms, seconds and minutes", () => {
    expect(formatDuration(null)).toBe("—");
    expect(formatDuration(850)).toBe("850 ms");
    expect(formatDuration(1_500)).toBe("1.5 s");
    expect(formatDuration(90_000)).toBe("1.5 min");
  });
});

describe("formatClock", () => {
  it("uses mm:ss under an hour and h:mm:ss after", () => {
    expect(formatClock(null)).toBe("—");
    expect(formatClock(65_000)).toBe("01:05");
    expect(formatClock(3_725_000)).toBe("1:02:05");
  });
});

describe("durationColor", () => {
  it("maps speed buckets", () => {
    expect(durationColor(1_999)).toBe("success");
    expect(durationColor(9_999)).toBe("info");
    expect(durationColor(29_999)).toBe("warning");
    expect(durationColor(30_000)).toBe("error");
  });
});

describe("durationMsBetween / formatDurationFromDates", () => {
  const start = "2026-01-01T00:00:00Z";
  it("returns null before start and measures to now while running", () => {
    expect(durationMsBetween(null, null)).toBeNull();
    expect(durationMsBetween(start, "2026-01-01T00:00:02Z")).toBe(2_000);
    expect(durationMsBetween(start, null, Date.UTC(2026, 0, 1, 0, 0, 5))).toBe(5_000);
  });

  it("suffixes running rows", () => {
    expect(formatDurationFromDates(start, null, Date.UTC(2026, 0, 1, 0, 0, 5))).toBe("5.0 s (running)");
    expect(formatDurationFromDates(start, "2026-01-01T00:00:02Z")).toBe("2.0 s");
  });
});
