import { chipOf, type LyoChipColor, type LyoChipIconKey, type LyoChipSpec } from "./chips.js";

/** Lowercases and collapses `_`, `-`, and whitespace runs to one space. Blank input returns "". */
export function normalizeStatus(status: string | null | undefined): string {
  if (!status) return "";
  return status
    .split(/[\s_-]+/)
    .filter(Boolean)
    .join(" ")
    .toLowerCase();
}

/** Title-cases a normalized status (`partially_delivered` → `Partially Delivered`). */
export function humanizeStatus(status: string | null | undefined): string {
  return normalizeStatus(status)
    .split(" ")
    .filter(Boolean)
    .map((w) => w[0]!.toUpperCase() + w.slice(1))
    .join(" ");
}

/** Domain palette. Return null for statuses it has no opinion on so the default vocabulary applies. */
export type LyoStatusPalette = {
  name: string;
  resolve: (status: string | null | undefined) => LyoChipSpec | null;
};

const GROUPS: { keys: readonly string[]; color: LyoChipColor; icon: LyoChipIconKey }[] = [
  {
    keys: ["success", "succeeded", "ok", "complete", "completed", "done", "passed", "valid", "delivered", "sent", "read", "healthy", "active", "enabled", "online", "connected", "up"],
    color: "success",
    icon: "check",
  },
  {
    keys: ["warning", "warn", "degraded", "partial", "partial success", "partially delivered", "succeeded with warnings", "success with warnings", "stale"],
    color: "warning",
    icon: "warning",
  },
  {
    keys: ["failed", "failure", "error", "faulted", "invalid", "unhealthy", "undelivered", "rejected", "down", "disconnected"],
    color: "error",
    icon: "error",
  },
  { keys: ["timeout", "timed out", "expired"], color: "error", icon: "timer" },
  {
    keys: ["running", "in progress", "inprogress", "processing", "started", "sending", "receiving", "draining"],
    color: "info",
    icon: "play",
  },
  { keys: ["queued", "pending", "scheduled", "waiting", "accepted", "new", "created"], color: "info", icon: "schedule" },
  { keys: ["cancelled", "canceled", "aborted", "stopped"], color: "default", icon: "cancel" },
  { keys: ["skipped", "ignored", "noop"], color: "default", icon: "skip" },
  { keys: ["disabled", "inactive", "offline", "paused", "suspended"], color: "default", icon: "pause" },
  { keys: ["unknown", "none"], color: "default", icon: "help" },
];

/** Shared status vocabulary (outcomes, lifecycle, health). Null when the status is not recognized. */
export function resolveDefaultStatus(status: string | null | undefined): LyoChipSpec | null {
  const key = normalizeStatus(status);
  if (!key) return null;
  const group = GROUPS.find((g) => g.keys.includes(key));
  return group ? chipOf(humanizeStatus(key), { color: group.color, icon: group.icon }) : null;
}

export type LyoStatusPaletteResolver = {
  resolve: (status: string | null | undefined, paletteName?: string | null) => LyoChipSpec;
};

/** Tries palettes registered under `paletteName` in order, then the default vocabulary, then a neutral chip with the raw text. */
export function createStatusPaletteResolver(palettes: readonly LyoStatusPalette[] = []): LyoStatusPaletteResolver {
  return {
    resolve(status, paletteName) {
      const name = paletteName?.trim().toLowerCase();
      if (name) {
        for (const palette of palettes) {
          if (palette.name.toLowerCase() !== name) continue;
          const match = palette.resolve(status);
          if (match) return match;
        }
      }
      return resolveDefaultStatus(status) ?? chipOf(status);
    },
  };
}
