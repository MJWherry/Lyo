import { describe, expect, it } from "vitest";
import {
  formatAbsolute,
  formatKind,
  formatRelative,
  latestUtcMs,
  timeZoneLabel,
  toUtcMs,
} from "./timestamp.js";

const HOUR = 3_600_000;
const MINUTE = 60_000;

describe("toUtcMs", () => {
  it("reads a naive ISO string as UTC", () => {
    expect(toUtcMs("2026-07-01T12:00:00")).toBe(Date.UTC(2026, 6, 1, 12));
  });

  it("honours an explicit offset", () => {
    expect(toUtcMs("2026-07-01T12:00:00-04:00")).toBe(Date.UTC(2026, 6, 1, 16));
  });

  it("accepts Date and epoch ms, rejects blanks and junk", () => {
    const d = new Date(Date.UTC(2026, 0, 2));
    expect(toUtcMs(d)).toBe(d.getTime());
    expect(toUtcMs(d.getTime())).toBe(d.getTime());
    expect(toUtcMs("")).toBeNull();
    expect(toUtcMs("   ")).toBeNull();
    expect(toUtcMs("not a date")).toBeNull();
    expect(toUtcMs(null)).toBeNull();
  });
});

describe("latestUtcMs", () => {
  it("picks the newest value in a collection and skips nulls", () => {
    expect(latestUtcMs(["2026-01-01T00:00:00Z", null, "2026-03-01T00:00:00Z", "2026-02-01T00:00:00Z"])).toBe(
      Date.UTC(2026, 2, 1)
    );
  });

  it("treats a scalar as a one-item collection", () => {
    expect(latestUtcMs("2026-01-01T00:00:00Z")).toBe(Date.UTC(2026, 0, 1));
    expect(latestUtcMs([])).toBeNull();
  });
});

describe("formatRelative", () => {
  it("formats inside the window", () => {
    expect(formatRelative(-(3 * HOUR + 15 * MINUTE))).toBe("3h 15m ago");
    expect(formatRelative(2 * HOUR)).toBe("in 2h");
    expect(formatRelative(-5 * MINUTE)).toBe("5m ago");
    expect(formatRelative(30_000)).toBe("now");
  });

  it("returns null outside the window", () => {
    expect(formatRelative(-25 * HOUR)).toBeNull();
    expect(formatRelative(2 * HOUR, HOUR)).toBeNull();
  });
});

describe("formatKind", () => {
  const now = Date.UTC(2026, 6, 1, 12);

  it("absolute always returns the absolute text", () => {
    expect(formatKind("Absolute", now - HOUR, "ABS", now)).toBe("ABS");
  });

  it("relative uses relative text inside the window", () => {
    expect(formatKind("Relative", now - HOUR, "ABS", now)).toBe("1h ago");
    expect(formatKind("Relative", now - 48 * HOUR, "ABS", now)).toBe("ABS");
  });

  it("TimeUntil falls back to absolute for a past instant, TimeSince for a future one", () => {
    expect(formatKind("TimeUntil", now - HOUR, "ABS", now)).toBe("ABS");
    expect(formatKind("TimeUntil", now + HOUR, "ABS", now)).toBe("in 1h");
    expect(formatKind("TimeSince", now + HOUR, "ABS", now)).toBe("ABS");
    expect(formatKind("TimeSince", now - HOUR, "ABS", now)).toBe("1h ago");
  });
});

describe("timeZoneLabel", () => {
  const july = new Date(Date.UTC(2026, 6, 1, 12));

  it("returns UTC for UTC zones", () => {
    expect(timeZoneLabel("UTC", july)).toBe("UTC");
    expect(timeZoneLabel("Etc/UTC", july)).toBe("UTC");
  });

  it("keeps a short abbreviation", () => {
    expect(timeZoneLabel("America/New_York", july)).toBe("EDT");
  });

  it("builds an acronym from the long name when the short name is an offset", () => {
    expect(timeZoneLabel("Europe/Berlin", july)).toBe("CEST");
  });

  it("falls back to a UTC offset", () => {
    expect(timeZoneLabel("Etc/GMT+4", july)).toBe("UTC-4");
    expect(timeZoneLabel("Asia/Kathmandu", july)).toBe("UTC+5:45");
  });
});

describe("formatAbsolute", () => {
  it("renders local text in the zone plus the label", () => {
    const text = formatAbsolute(Date.UTC(2026, 6, 1, 16, 5), "America/New_York", "en-US");
    expect(text).toBe("7/1/26, 12:05 PM EDT");
  });
});
