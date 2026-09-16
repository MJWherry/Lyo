import { toUtcMs } from "./timestamp.js";

export type LyoDurationColor = "success" | "info" | "warning" | "error";

/** Human-readable duration: `850 ms`, `1.5 s`, `1.5 min`. Null draws an em dash. */
export function formatDuration(ms: number | null | undefined): string {
  if (ms == null) return "—";
  if (ms < 1_000) return `${ms.toFixed(0)} ms`;
  if (ms < 60_000) return `${(ms / 1_000).toFixed(1)} s`;
  return `${(ms / 60_000).toFixed(1)} min`;
}

/** Clock-style duration (`mm:ss`, or `h:mm:ss` from an hour) for aligned stat columns. */
export function formatClock(ms: number | null | undefined): string {
  if (ms == null) return "—";
  const total = Math.floor(ms / 1_000);
  const h = Math.floor(total / 3_600);
  const m = Math.floor((total % 3_600) / 60);
  const s = total % 60;
  const pad = (n: number) => String(n).padStart(2, "0");
  return h >= 1 ? `${h}:${pad(m)}:${pad(s)}` : `${pad(m)}:${pad(s)}`;
}

/** Chip colour for a completed duration: fast is success, then info / warning / error. */
export function durationColor(ms: number): LyoDurationColor {
  if (ms < 2_000) return "success";
  if (ms < 10_000) return "info";
  if (ms < 30_000) return "warning";
  return "error";
}

/** Elapsed ms, measured to `nowMs` while unfinished. Null when not started. */
export function durationMsBetween(started: unknown, finished: unknown, nowMs: number = Date.now()): number | null {
  const start = toUtcMs(started);
  if (start == null) return null;
  return (toUtcMs(finished) ?? nowMs) - start;
}

/** Elapsed text; in-flight rows get a ` (running)` suffix. */
export function formatDurationFromDates(started: unknown, finished: unknown, nowMs: number = Date.now()): string {
  const ms = durationMsBetween(started, finished, nowMs);
  if (ms == null) return "—";
  const text = formatDuration(ms);
  return toUtcMs(finished) == null ? `${text} (running)` : text;
}
