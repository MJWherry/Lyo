export function formatNs(ns: number | undefined | null): string {
  if (ns === undefined || ns === null || Number.isNaN(ns)) return "—";
  if (ns < 1_000) return `${ns.toFixed(1)} ns`;
  if (ns < 1_000_000) return `${(ns / 1_000).toFixed(2)} µs`;
  if (ns < 1_000_000_000) return `${(ns / 1_000_000).toFixed(2)} ms`;
  return `${(ns / 1_000_000_000).toFixed(2)} s`;
}

export function formatBytes(bytes: number | undefined | null): string {
  if (bytes === undefined || bytes === null || Number.isNaN(bytes)) return "—";
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  return `${(bytes / (1024 * 1024 * 1024)).toFixed(2)} GB`;
}

export function formatMs(ms: number | undefined | null): string {
  if (ms === undefined || ms === null || Number.isNaN(ms)) return "—";
  if (ms < 1) return `${(ms * 1000).toFixed(1)} µs`;
  if (ms < 1000) return `${ms.toFixed(2)} ms`;
  return `${(ms / 1000).toFixed(2)} s`;
}

export function formatDuration(seconds: number | undefined | null): string | null {
  if (typeof seconds !== "number" || !Number.isFinite(seconds) || seconds < 0) return null;
  const s = Math.round(seconds);
  const h = Math.floor(s / 3600);
  const m = Math.floor((s % 3600) / 60);
  const sec = s % 60;
  if (h > 0) return `${h}h ${m}m`;
  if (m > 0) return `${m}m ${sec}s`;
  return `${sec}s`;
}

export function formatTimestamp(value: string | undefined | null): string | null {
  if (!value) return null;
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return null;
  return d.toLocaleString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function formatDeltaPct(pct: number | undefined | null, lowerIsBetter = true): {
  text: string;
  className: string;
} {
  if (pct === undefined || pct === null || Number.isNaN(pct)) {
    return { text: "—", className: "" };
  }
  const sign = pct > 0 ? "+" : "";
  const text = `${sign}${pct.toFixed(1)}%`;
  const worse = lowerIsBetter ? pct > 0 : pct < 0;
  return { text, className: worse ? "delta-up" : "delta-down" };
}

/** Humanize a single `[Params]` value (RowCount → 10,000; DataSize → 1 MB). */
export function formatParamValue(key: string, value: string | undefined | null): string {
  if (value == null || value === "") return "—";
  const n = Number(value);
  if (!Number.isFinite(n)) return value;

  const k = key.toLowerCase();
  if (
    k === "datasize" ||
    k.endsWith("size") ||
    k.endsWith("bytes") ||
    k.includes("payload")
  ) {
    return formatBytes(n);
  }

  if (
    Number.isInteger(n) &&
    (k === "rowcount" ||
      k === "rowsperfile" ||
      k.endsWith("count") ||
      k.endsWith("rows") ||
      k.includes("row"))
  ) {
    return n.toLocaleString("en-US");
  }

  return value;
}

export function paramLabel(parameters: Record<string, string> | undefined | null): string {
  if (!parameters) return "—";
  const keys = Object.keys(parameters);
  if (!keys.length) return "—";
  return keys.map((k) => `${k}=${formatParamValue(k, parameters[k])}`).join(", ");
}

/** Prefer formatting from `parameters`; fall back to a stored paramLabel string. */
export function displayParamLabel(
  parameters: Record<string, string> | undefined | null,
  fallback?: string | null
): string {
  if (parameters && Object.keys(parameters).length) return paramLabel(parameters);
  if (!fallback) return "—";
  // Bare numeric labels (legacy RowCount paramLabel) get thousands separators.
  if (/^\d+$/.test(fallback)) return Number(fallback).toLocaleString("en-US");
  return fallback;
}

export function slaBadgeClass(result: string | undefined | null): string {
  if (!result) return "badge";
  const r = result.toLowerCase();
  if (r.includes("exceed") || r.includes("meet") || r === "pass") return "badge badge-ok";
  if (r.includes("miss") || r.includes("fail") || r.includes("below")) return "badge badge-warn";
  return "badge";
}
