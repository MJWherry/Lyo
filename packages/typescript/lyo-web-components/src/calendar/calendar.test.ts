import { describe, expect, it } from "vitest";
import { forMonth, formatCalendarClock, fromTimeFrames, monthRows, placeDayEvents, withTimeZone } from "./calendar.js";

const newYorkInstant = new Date("2026-09-30T02:00:00Z");

describe("calendar", () => {
  it("pads September 2026 from August 30 through October 3", () => {
    const calendar = forMonth(2026, 9, "UTC");
    expect(calendar.start).toBe("2026-08-30");
    expect(calendar.end).toBe("2026-10-03");
    expect(calendar.days).toHaveLength(35);
    expect(calendar.days[0]?.date).toBe("2026-08-30");
    expect(calendar.days.at(-1)?.date).toBe("2026-10-03");
  });

  it("lists a span that crosses local midnight on both days", () => {
    const calendar = fromTimeFrames("2026-09-15", "2026-09-16", "UTC", [
      { key: "night", title: "Night", start: "2026-09-15T23:00:00Z", end: "2026-09-16T01:00:00Z" },
    ]);
    expect(calendar.entries).toHaveLength(1);
    expect(calendar.days.find((day) => day.date === "2026-09-15")?.entries.map((entry) => entry.key)).toContain("night");
    expect(calendar.days.find((day) => day.date === "2026-09-16")?.entries.map((entry) => entry.key)).toContain("night");
  });

  it("rebuckets 02:00 UTC onto the previous evening in New York", () => {
    const utc = fromTimeFrames("2026-09-01", "2026-09-30", "UTC", [
      { key: "call", title: "Call", start: "2026-09-30T02:00:00Z", end: "2026-09-30T02:00:00Z" },
    ]);
    const local = withTimeZone(utc, "America/New_York");
    const days = local.days.filter((day) => day.entries.length > 0);
    expect(days).toHaveLength(1);
    expect(days[0]?.date).toBe("2026-09-29");
  });

  it("switches the same instant between 24-hour and 12-hour labels", () => {
    expect(formatCalendarClock(newYorkInstant, "America/New_York", true)).toBe("22:00");
    expect(formatCalendarClock(newYorkInstant, "America/New_York", false)).toBe("10:00 PM");
  });

  it("collapses the same hour into one month row and keeps three hour rows", () => {
    const at = (hour: number) => `2026-09-15T${String(hour).padStart(2, "0")}:00:00.000Z`;
    const same = monthRows([at(7), at(7), at(7)].map((start) => ({ start, end: start })), "2026-09-15", "UTC");
    expect(same.rows).toHaveLength(1);
    expect(same.moreRows).toBe(0);
    expect(same.rows[0]).toMatchObject({ allDay: false, hour: 7, indexes: [0, 1, 2] });

    const crowded = monthRows(
      [7, 7, 12, 13, 16].map((hour) => {
        const start = at(hour);
        return { start, end: start };
      }),
      "2026-09-15",
      "UTC"
    );
    expect(crowded.rows.map((row) => row.hour)).toEqual([7, 12, 13]);
    expect(crowded.moreRows).toBe(1);
    expect(crowded.rows[0]?.indexes).toEqual([0, 1]);
  });

  it("places an all-day span ahead of a timed fire and lanes two events at one instant", () => {
    const summary = monthRows(
      [
        { start: "2026-09-15T00:00:00.000Z", end: "2026-09-16T00:00:00.000Z" },
        { start: "2026-09-15T09:00:00.000Z", end: "2026-09-15T09:00:00.000Z" },
      ],
      "2026-09-15",
      "UTC"
    );
    expect(summary.rows[0]?.allDay).toBe(true);
    expect(summary.rows[1]?.hour).toBe(9);

    const blocks = placeDayEvents(
      [
        { start: "2026-09-15T09:00:00.000Z", end: "2026-09-15T09:00:00.000Z" },
        { start: "2026-09-15T09:00:00.000Z", end: "2026-09-15T09:00:00.000Z" },
      ],
      "2026-09-15",
      "UTC"
    );
    expect(blocks.map((block) => block.lane)).toEqual([0, 1]);
    expect(blocks[0]?.laneCount).toBe(2);
    expect(blocks[0]?.allDay).toBe(false);
    expect(blocks[0]?.topPercent).toBe(blocks[1]?.topPercent);
  });
});
