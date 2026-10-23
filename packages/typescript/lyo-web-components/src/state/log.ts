import type { LyoChipColor } from "../display/chips.js";

export const LYO_LOG_LEVELS = ["Trace", "Debug", "Information", "Warning", "Error", "Critical"] as const;

export type LyoLogLevel = (typeof LYO_LOG_LEVELS)[number];

export type LyoLogEntry = {
  /** ISO string, Date, or epoch ms. */
  timestamp: string | number | Date;
  level: LyoLogLevel;
  message: string;
  source?: string | null;
};

function rank(level: LyoLogLevel): number {
  return LYO_LOG_LEVELS.indexOf(level);
}

/** Entries at or above `minLevel` whose message or source contains `search` (case-insensitive). */
export function filterLogEntries(entries: readonly LyoLogEntry[], minLevel: LyoLogLevel, search?: string | null): LyoLogEntry[] {
  const q = search?.trim().toLowerCase();
  const min = rank(minLevel);
  return entries.filter(
    (e) =>
      rank(e.level) >= min &&
      (!q || e.message.toLowerCase().includes(q) || (e.source ?? "").toLowerCase().includes(q))
  );
}

function isoOf(value: LyoLogEntry["timestamp"]): string {
  if (typeof value === "string") return value;
  return new Date(value).toISOString();
}

/** `timestamp level source message` per line, for copy and download. */
export function formatLogEntries(entries: readonly LyoLogEntry[]): string {
  return entries.map((e) => `${isoOf(e.timestamp)} ${e.level} ${e.source ?? ""} ${e.message}\n`).join("");
}

export function logLevelColor(level: LyoLogLevel): LyoChipColor {
  switch (level) {
    case "Warning":
      return "warning";
    case "Error":
    case "Critical":
      return "error";
    case "Information":
      return "info";
    default:
      return "default";
  }
}
