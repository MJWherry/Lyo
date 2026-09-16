import type { JobDefinitionRes, JobScheduleRes } from "lyo-job-api-client";
import { describe, expect, it } from "vitest";
import { schedulesCalendar } from "./scheduleCalendar.js";

function definition(schedule: JobScheduleRes): JobDefinitionRes {
  return { id: "def", name: "Job", type: "t", workerType: "w", enabled: true, jobSchedules: [schedule] };
}

describe("schedulesCalendar", () => {
  it("places a Tuesday 09:00 UTC set-times schedule on September 2026 Tuesdays", () => {
    const calendar = schedulesCalendar(2026, 9, "UTC", [
      definition({
        id: "standup",
        enabled: true,
        type: "SetTimes",
        times: ["09:00:00"],
        dayFlags: 4,
        timeZoneId: "UTC",
        description: "Standup",
      }),
    ]).calendar;
    const days = calendar.days.filter((day) => day.entries.some((entry) => entry.title === "Standup")).map((day) => day.date);
    expect(days).toEqual(["2026-09-01", "2026-09-08", "2026-09-15", "2026-09-22", "2026-09-29"]);
    expect(calendar.days.find((day) => day.date === "2026-09-01")?.entries[0]?.start).toBe("2026-09-01T09:00:00.000Z");
  });

  it("keeps a 13:00 America/New_York clock at 17:00 UTC in September", () => {
    const calendar = schedulesCalendar(2026, 9, "America/New_York", [
      definition({
        id: "klein",
        enabled: true,
        type: "SetTimes",
        times: ["13:00"],
        dayFlags: 127,
        timeZoneId: "America/New_York",
        description: "Klein",
      }),
    ]).calendar;
    const entry = calendar.days.find((day) => day.date === "2026-09-01")?.entries[0];
    expect(entry?.title).toBe("Klein");
    expect(entry?.start).toBe("2026-09-01T17:00:00.000Z");
  });

  it("grays a set-times fire that falls inside a dated skip blackout and keeps the blackout span", () => {
    const calendar = schedulesCalendar(2026, 9, "UTC", [
      definition({
        id: "klein",
        enabled: true,
        type: "setTimes",
        times: ["12:00:00"],
        dayFlags: "everyDay",
        monthFlags: "everyMonth",
        timeZoneId: "UTC",
        description: "Klein",
        jobBlackoutCalendar: {
          id: "cal",
          name: "Holidays",
          enabled: true,
          blackoutWindows: [
            {
              id: "labor",
              jobBlackoutCalendarId: "cal",
              name: "Labor Day (2026)",
              dayFlags: "everyDay",
              startTime: "00:00:00",
              endTime: "23:59:00",
              policy: "skip",
              enabled: true,
              startDateUtc: "2026-09-07T00:00:00Z",
              endDateUtc: "2026-09-07T00:00:00Z",
            },
          ],
        },
      }),
    ]).calendar;
    const labor = calendar.days.find((day) => day.date === "2026-09-07")?.entries ?? [];
    expect(labor.find((entry) => entry.title === "Klein")?.muted).toBe(true);
    expect(labor.find((entry) => entry.title === "Labor Day (2026)")?.muted).toBeFalsy();
    expect(calendar.days.find((day) => day.date === "2026-09-08")?.entries.some((entry) => entry.title === "Klein")).toBe(true);
  });

  it("places camelCase setTimes and everyDay flags from the API", () => {
    const calendar = schedulesCalendar(2026, 9, "UTC", [
      definition({
        id: "klein",
        enabled: true,
        type: "setTimes",
        times: ["07:00:00"],
        dayFlags: "everyDay",
        monthFlags: "everyMonth",
        timeZoneId: "America/New_York",
        description: "Klein",
      }),
    ]).calendar;
    const entry = calendar.days.find((day) => day.date === "2026-09-01")?.entries.find((item) => item.title === "Klein");
    expect(entry?.start).toBe("2026-09-01T11:00:00.000Z");
  });

  it("emits nothing for a disabled schedule", () => {
    const calendar = schedulesCalendar(2026, 9, "UTC", [
      definition({
        id: "off",
        enabled: false,
        type: "SetTimes",
        times: ["09:00"],
        dayFlags: 127,
        timeZoneId: "UTC",
        description: "Off",
      }),
    ]).calendar;
    expect(calendar.days.every((day) => day.entries.length === 0)).toBe(true);
  });
});
