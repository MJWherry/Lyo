import type { JobBlackoutWindowRes, JobDefinitionRes, JobScheduleRes } from "lyo-job-api-client";
import { civilToUtc, forMonth, type CalendarModel, type TimeFrame } from "lyo-web-components";

const DAY_BITS: Record<string, number> = {
  none: 0,
  sun: 1,
  sunday: 1,
  mon: 2,
  monday: 2,
  tue: 4,
  tuesday: 4,
  wed: 8,
  wednesday: 8,
  thu: 16,
  thursday: 16,
  fri: 32,
  friday: 32,
  sat: 64,
  saturday: 64,
  everyday: 127,
  weekdays: 62,
  weekends: 65,
};

const MONTH_BITS: Record<string, number> = {
  none: 0,
  jan: 1,
  january: 1,
  feb: 2,
  february: 2,
  mar: 4,
  march: 4,
  apr: 8,
  april: 8,
  may: 16,
  jun: 32,
  june: 32,
  jul: 64,
  july: 64,
  aug: 128,
  august: 128,
  sep: 256,
  september: 256,
  oct: 512,
  october: 512,
  nov: 1024,
  november: 1024,
  dec: 2048,
  december: 2048,
  everymonth: 4095,
};

const HUE: Record<string, number> = {
  SetTimes: 228,
  Interval: 174,
  IntervalWindow: 174,
  OneShot: 252,
  Cron: 292,
  blackout: 52,
};

/** Places each enabled schedule's own clocks on the month. Cron is not expanded. Holiday slugs stay names. */
export function schedulesCalendar(
  year: number,
  month: number,
  timeZone: string,
  definitions: JobDefinitionRes[]
): { calendar: CalendarModel; holidayNames: string[] } {
  const frames: TimeFrame[] = [];
  const holidayNames: string[] = [];
  for (const definition of definitions) {
    for (const schedule of definition.jobSchedules ?? []) {
      if (schedule.enabled === false) continue;
      frames.push(...applyBlackouts(scheduleFrames(schedule, year, month), schedule));
      for (const window of schedule.jobBlackoutCalendar?.blackoutWindows ?? []) {
        if (window.enabled === false) continue;
        if (window.holidaySlug) {
          holidayNames.push(window.name);
          continue;
        }
        frames.push(...windowFrames(window, year, month, timeZone));
      }
    }
  }
  return { calendar: forMonth(year, month, timeZone, frames), holidayNames };
}

function scheduleFrames(schedule: JobScheduleRes, year: number, month: number): TimeFrame[] {
  const kind = scheduleKind(schedule.type);
  const zone = schedule.timeZoneId || "UTC";
  const title = schedule.description || schedule.cronExpression || schedule.id;
  const grid = forMonth(year, month, zone);
  if (kind === "SetTimes") return clockPoints(schedule, grid.days.map((day) => day.date), zone, title, schedule.times ?? [], "SetTimes");
  if (kind === "Interval") return intervalFrames(schedule, grid.days.map((day) => day.date), zone, title);
  if (kind === "OneShot" && schedule.startDateUtc) {
    return [{ key: `once:${schedule.id}`, title, start: schedule.startDateUtc, end: schedule.startDateUtc, category: "OneShot", hue: HUE.OneShot }];
  }
  return [];
}

function applyBlackouts(frames: TimeFrame[], schedule: JobScheduleRes): TimeFrame[] {
  const calendar = schedule.jobBlackoutCalendar;
  if (!calendar || calendar.enabled === false) return frames;
  const windows = (calendar.blackoutWindows ?? []).filter((window) => window.enabled !== false);
  if (windows.length === 0) return frames;
  const zone = schedule.timeZoneId || "UTC";
  const kept: TimeFrame[] = [];
  for (const frame of frames) {
    const next = adjustForBlackout(frame, windows, zone);
    if (next) kept.push(next);
  }
  return kept;
}

function adjustForBlackout(frame: TimeFrame, windows: JobBlackoutWindowRes[], zone: string): TimeFrame | null {
  const local = localClock(frame.start, zone);
  for (const window of windows) {
    if (!coversClock(window, local.date, local.minutes)) continue;
    const policy = String(window.policy ?? "skip").toLowerCase();
    if (policy === "defer" && frame.start === frame.end) {
      const startClock = (window.startTime ?? "00:00").slice(0, 5);
      const endClock = (window.endTime ?? "23:59").slice(0, 5);
      const date = endClock <= startClock ? shiftDate(local.date, 1) : local.date;
      const utc = civilToUtc(date, endClock, zone);
      return { ...frame, start: utc, end: utc, key: `${frame.key}:deferred` };
    }
    if (policy === "skip") return { ...frame, muted: true };
    return frame;
  }
  return frame;
}

function coversClock(window: JobBlackoutWindowRes, date: string, minutes: number): boolean {
  if (!windowMatches(window, date)) return false;
  const start = clockMinutes(window.startTime) ?? 0;
  const end = clockMinutes(window.endTime) ?? 23 * 60 + 59;
  if (end <= start) return minutes >= start;
  return minutes >= start && minutes < end;
}

function localClock(utc: string, timeZone: string): { date: string; minutes: number } {
  const dtf = new Intl.DateTimeFormat("en-US", {
    timeZone,
    hourCycle: "h23",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
  const bag: Record<string, string> = {};
  for (const part of dtf.formatToParts(new Date(utc))) {
    if (part.type !== "literal") bag[part.type] = part.value;
  }
  let hour = Number(bag.hour);
  if (hour === 24) hour = 0;
  return { date: `${bag.year}-${bag.month}-${bag.day}`, minutes: hour * 60 + Number(bag.minute) };
}

function shiftDate(iso: string, days: number): string {
  const [year, month, day] = iso.split("-").map(Number);
  const next = new Date(Date.UTC(year, month - 1, day + days));
  const y = next.getUTCFullYear();
  const m = String(next.getUTCMonth() + 1).padStart(2, "0");
  const d = String(next.getUTCDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

function clockPoints(schedule: JobScheduleRes, dates: string[], zone: string, title: string, times: string[], category: string): TimeFrame[] {
  const frames: TimeFrame[] = [];
  for (const date of dates) {
    if (!matches(schedule, date)) continue;
    for (const time of times) {
      const clock = time.slice(0, 5);
      const utc = civilToUtc(date, clock, zone);
      frames.push({ key: `fire:${schedule.id}:${date}:${clock}`, title, start: utc, end: utc, category, hue: HUE[category] });
    }
  }
  return frames;
}

function intervalFrames(schedule: JobScheduleRes, dates: string[], zone: string, title: string): TimeFrame[] {
  const startMin = clockMinutes(schedule.startTime);
  const endMin = clockMinutes(schedule.endTime);
  if (startMin == null || endMin == null) return [];
  const frames: TimeFrame[] = [];
  const step = schedule.intervalMinutes && schedule.intervalMinutes > 0 ? schedule.intervalMinutes : null;
  for (const date of dates) {
    if (!matches(schedule, date)) continue;
    let endDate = date;
    let endClock = formatClock(endMin);
    if (endMin <= startMin) endDate = addDays(date, 1);
    frames.push({
      key: `interval:${schedule.id}:${date}`,
      title,
      start: civilToUtc(date, formatClock(startMin), zone),
      end: civilToUtc(endDate, endClock, zone),
      category: "IntervalWindow",
      hue: HUE.IntervalWindow,
    });
    if (step == null || endMin <= startMin) continue;
    for (let minute = startMin; minute <= endMin; minute += step) {
      const clock = formatClock(minute);
      const utc = civilToUtc(date, clock, zone);
      frames.push({ key: `fire:${schedule.id}:${date}:${clock}`, title, start: utc, end: utc, category: "Interval", hue: HUE.Interval });
    }
  }
  return frames;
}

function matches(schedule: JobScheduleRes, date: string): boolean {
  const days = flagMask(schedule.dayFlags, DAY_BITS, 127);
  const months = flagMask(schedule.monthFlags, MONTH_BITS, 4095);
  const month = Number(date.slice(5, 7));
  return (days & (1 << dayOfWeek(date))) !== 0 && (months & (1 << (month - 1))) !== 0;
}

function scheduleKind(value: unknown): string {
  const name = typeof value === "string" ? value.trim().toLowerCase() : "";
  if (name === "settimes" || value === 0) return "SetTimes";
  if (name === "interval" || value === 1) return "Interval";
  if (name === "oneshot" || value === 2) return "OneShot";
  if (name === "cron" || value === 3) return "Cron";
  return "SetTimes";
}

function flagMask(value: unknown, names: Record<string, number>, fallback: number): number {
  if (value == null) return fallback;
  if (typeof value === "number" && Number.isFinite(value)) return value;
  if (typeof value !== "string") return fallback;
  const text = value.trim();
  if (!text) return fallback;
  const named = names[text.toLowerCase()];
  if (named != null) return named;
  const parsed = Number(text);
  if (Number.isFinite(parsed)) return parsed;
  let mask = 0;
  let any = false;
  for (const part of text.split(/[,|\s]+/)) {
    const bit = names[part.toLowerCase()];
    if (bit == null) continue;
    mask |= bit;
    any = true;
  }
  return any ? mask : fallback;
}

function windowFrames(window: JobBlackoutWindowRes, year: number, month: number, timeZone: string): TimeFrame[] {
  const grid = forMonth(year, month, timeZone);
  const frames: TimeFrame[] = [];
  const startClock = (window.startTime ?? "00:00").slice(0, 5);
  const endClock = (window.endTime ?? "23:59").slice(0, 5);
  for (const day of grid.days) {
    if (!windowMatches(window, day.date)) continue;
    let endDate = day.date;
    if (endClock <= startClock) endDate = addDays(day.date, 1);
    frames.push({
      key: `${window.id}:${day.date}`,
      title: window.name,
      start: civilToUtc(day.date, startClock, timeZone),
      end: civilToUtc(endDate, endClock, timeZone),
      category: "blackout",
      hue: HUE.blackout,
    });
  }
  return frames;
}

function windowMatches(window: JobBlackoutWindowRes, date: string): boolean {
  const start = dateOnly(window.startDateUtc);
  if (start) {
    const end = dateOnly(window.endDateUtc) ?? start;
    return date >= start && date <= end;
  }
  const mask = flagMask(window.dayFlags, DAY_BITS, -1);
  if (mask < 0) return false;
  return (mask & (1 << dayOfWeek(date))) !== 0;
}

function dateOnly(value: string | null | undefined): string | null {
  if (!value) return null;
  return value.slice(0, 10);
}

function clockMinutes(value: string | null | undefined): number | null {
  if (!value) return null;
  const match = /^(\d{1,2}):(\d{2})/.exec(value);
  if (!match) return null;
  return Number(match[1]) * 60 + Number(match[2]);
}

function formatClock(minutes: number): string {
  const hour = Math.floor(minutes / 60);
  const minute = minutes % 60;
  return `${String(hour).padStart(2, "0")}:${String(minute).padStart(2, "0")}`;
}

function addDays(iso: string, days: number): string {
  const [year, month, day] = iso.split("-").map(Number);
  const dt = new Date(Date.UTC(year, month - 1, day + days));
  const y = dt.getUTCFullYear();
  const m = String(dt.getUTCMonth() + 1).padStart(2, "0");
  const d = String(dt.getUTCDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

function dayOfWeek(iso: string): number {
  const [year, month, day] = iso.split("-").map(Number);
  return new Date(Date.UTC(year, month - 1, day)).getUTCDay();
}
