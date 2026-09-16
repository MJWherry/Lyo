"use client";

import { useEffect, useState } from "react";
import Chip from "@mui/material/Chip";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import { durationColor, durationMsBetween, formatDuration, formatDurationFromDates } from "./duration.js";
import {
  DEFAULT_RELATIVE_WINDOW_MS,
  browserTimeZone,
  formatAbsolute,
  formatKind,
  latestUtcMs,
  toUtcMs,
  type LyoTimestampKind,
} from "./timestamp.js";

/** Browser IANA zone, resolved after mount so server and client render the same markup first (UTC). */
export function useBrowserTimeZone(): string {
  const [zone, setZone] = useState("UTC");
  useEffect(() => setZone(browserTimeZone()), []);
  return zone;
}

/** Current epoch ms, refreshed every `intervalMs` while mounted. Pass null to stop ticking. */
export function useNow(intervalMs: number | null = 30_000): number {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    if (intervalMs == null) return;
    const id = setInterval(() => setNow(Date.now()), intervalMs);
    return () => clearInterval(id);
  }, [intervalMs]);
  return now;
}

export type LyoTimestampProps = {
  /** ISO string (naive values are UTC), Date, epoch ms, or an array whose newest value is shown when `latest` is set. */
  value: unknown;
  kind?: LyoTimestampKind;
  /** Pick the newest instant from a collection value. */
  latest?: boolean;
  relativeWindowMs?: number;
  locale?: string;
  /** Overrides the browser zone. */
  timeZone?: string;
  empty?: string;
};

/** UTC instant in the browser zone with a zone label. Relative kinds fall back to absolute outside the window; the tooltip always shows absolute. */
export function LyoTimestamp({
  value,
  kind = "Absolute",
  latest,
  relativeWindowMs = DEFAULT_RELATIVE_WINDOW_MS,
  locale,
  timeZone,
  empty = "—",
}: LyoTimestampProps) {
  const browserZone = useBrowserTimeZone();
  const now = useNow(kind === "Absolute" ? null : 30_000);
  const utcMs = latest ? latestUtcMs(value) : toUtcMs(value);
  if (utcMs == null) return <span className="lyo-timestamp lyo-timestamp-empty">{empty}</span>;
  const absolute = formatAbsolute(utcMs, timeZone ?? browserZone, locale);
  const text = formatKind(kind, utcMs, absolute, now, relativeWindowMs);
  return (
    <Tooltip title={absolute}>
      <span className="lyo-timestamp">{text}</span>
    </Tooltip>
  );
}

export type LyoDurationProps = {
  /** Explicit duration in ms. */
  ms?: number | null;
  /** Or start / finish instants; unfinished durations tick and read `(running)`. */
  started?: unknown;
  finished?: unknown;
  /** Draw as a coloured chip banded by duration. */
  chip?: boolean;
};

export function LyoDuration({ ms, started, finished, chip }: LyoDurationProps) {
  const running = ms == null && started != null && toUtcMs(finished) == null;
  const now = useNow(running ? 1_000 : null);
  const value = ms ?? durationMsBetween(started, finished, now);
  const text = ms != null ? formatDuration(ms) : formatDurationFromDates(started, finished, now);
  if (!chip || value == null) return <Typography component="span" variant="body2">{text}</Typography>;
  return <Chip size="small" label={text} color={running ? "info" : durationColor(value)} variant={running ? "outlined" : "filled"} />;
}
