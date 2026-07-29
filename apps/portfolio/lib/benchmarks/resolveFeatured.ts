import { formatBytes, formatMs, formatParamValue } from "./format";
import { loadLatestReport } from "./loadReport";
import type { BenchMeasurement, BenchReport, LoadScenario } from "./types";
import type { CatalogBenchmarkItem } from "@/lib/catalog/types";

export type ResolvedFeaturedMetrics = {
  metric: string;
  detail: string;
  note: string;
};

/** Which figure is the big home-page number. Auto-picked when omitted. */
export type FeaturedPrimary = "mean" | "throughput" | "p95" | "checks";

/**
 * Resolve home-page metric/detail from live `docs/benchmarks/data/{suite}.json`
 * using selectors on the catalog item (`method`+`params` or `scenario`).
 */
export function resolveFeaturedMetrics(
  suite: string | undefined,
  item: CatalogBenchmarkItem
): ResolvedFeaturedMetrics | null {
  if (!suite) return null;
  const report = loadLatestReport(suite);
  if (!report) return null;

  if (item.scenario) {
    const scenario = findScenario(report, item.scenario);
    if (!scenario) return null;
    return formatLoad(scenario, item.primary);
  }

  if (item.method) {
    const measurement = findMeasurement(report, item.method, item.params);
    if (!measurement) return null;
    return formatMicro(measurement, item.primary);
  }

  return null;
}

function findScenario(report: BenchReport, name: string): LoadScenario | undefined {
  return (report.scenarios ?? []).find(
    (s) => s.name?.localeCompare(name, undefined, { sensitivity: "accent" }) === 0
  );
}

function findMeasurement(
  report: BenchReport,
  method: string,
  params?: Record<string, string>
): BenchMeasurement | undefined {
  for (const group of report.groups ?? []) {
    for (const m of group.measurements ?? []) {
      if (m.method !== method) continue;
      if (!paramsMatch(m.parameters, params)) continue;
      return m;
    }
  }
  return undefined;
}

function paramsMatch(
  actual: Record<string, string> | undefined,
  expected: Record<string, string> | undefined
): boolean {
  if (!expected || !Object.keys(expected).length) return true;
  if (!actual) return false;
  return Object.entries(expected).every(
    ([k, v]) => String(actual[k] ?? "") === String(v)
  );
}

function formatMicro(
  m: BenchMeasurement,
  primary?: FeaturedPrimary
): ResolvedFeaturedMetrics {
  const thr = m.throughputMbps;
  const pick: FeaturedPrimary =
    primary ?? (typeof thr === "number" && thr > 0 ? "throughput" : "mean");

  let metric = "—";
  if (pick === "throughput" && typeof thr === "number") metric = formatThroughput(thr);
  else if (typeof m.meanNs === "number") metric = `~${compactNs(m.meanNs)}`;

  const detailParts: string[] = [];
  const params = m.parameters ?? {};
  if (params.DataSize != null) detailParts.push(formatParamValue("DataSize", params.DataSize));
  else if (params.RowCount != null)
    detailParts.push(`${formatParamValue("RowCount", params.RowCount)} rows`);
  for (const [k, v] of Object.entries(params)) {
    if (k === "DataSize" || k === "RowCount") continue;
    detailParts.push(`${k}=${formatParamValue(k, v)}`);
  }
  if (pick === "throughput" && typeof m.meanNs === "number")
    detailParts.push(`~${compactNs(m.meanNs)}`);
  else if (pick === "mean" && typeof thr === "number" && thr > 0)
    detailParts.push(formatThroughput(thr));
  if (typeof m.allocatedBytes === "number")
    detailParts.push(`~${formatBytes(m.allocatedBytes)} allocated`);

  const paramNote = Object.entries(params)
    .map(([k, v]) => `${k}=${v}`)
    .join(", ");

  return {
    metric,
    detail: detailParts.join(" · "),
    note: paramNote ? `${m.method} @ ${paramNote}` : m.method,
  };
}

function formatLoad(
  s: LoadScenario,
  primary?: FeaturedPrimary
): ResolvedFeaturedMetrics {
  const pick: FeaturedPrimary = primary ?? "p95";
  const p95 = s.latency?.p95;
  const checks = s.checksPass;

  let metric = "—";
  if (pick === "p95" && typeof p95 === "number") metric = `~${compactMs(p95)} p95`;
  else if (pick === "checks" && typeof checks === "number")
    metric = `${formatChecks(checks)} checks`;
  else if (pick === "mean" && typeof s.latency?.avg === "number")
    metric = `~${compactMs(s.latency.avg)} avg`;
  else if (typeof p95 === "number") metric = `~${compactMs(p95)} p95`;

  const detailParts: string[] = [];
  if (s.profile) detailParts.push(`${s.profile} profile`);
  if (typeof s.requests === "number") detailParts.push(`${formatCount(s.requests)} requests`);
  if (typeof checks === "number") detailParts.push(`${formatChecks(checks)} checks`);
  if (typeof s.throughput === "number" && s.throughput > 0)
    detailParts.push(`~${s.throughput.toFixed(1)} req/s`);

  return {
    metric,
    detail: detailParts.join(" · "),
    note: s.name,
  };
}

function formatThroughput(mbps: number): string {
  if (mbps >= 1000) return `~${(mbps / 1000).toFixed(1)} GB/s`;
  if (mbps >= 10) return `~${mbps.toFixed(0)} MB/s`;
  return `~${mbps.toFixed(1)} MB/s`;
}

function compactNs(ns: number): string {
  // Tighter rounding for hero figures (still sourced from the measurement).
  if (ns < 1_000) return `${ns.toFixed(0)} ns`;
  if (ns < 1_000_000) return `${(ns / 1_000).toFixed(2)} µs`;
  if (ns < 1_000_000_000) {
    const ms = ns / 1_000_000;
    return ms >= 10 ? `${ms.toFixed(0)} ms` : `${ms.toFixed(1)} ms`;
  }
  const s = ns / 1_000_000_000;
  return s >= 10 ? `${s.toFixed(0)} s` : `${s.toFixed(2)} s`;
}

function compactMs(ms: number): string {
  if (ms < 1) return formatMs(ms);
  if (ms >= 1000) {
    const s = ms / 1000;
    return s >= 10 ? `${s.toFixed(0)} s` : `${s.toFixed(2)} s`;
  }
  return ms >= 10 ? `${ms.toFixed(0)} ms` : `${ms.toFixed(1)} ms`;
}

function formatChecks(pct: number): string {
  if (pct >= 99.95) return "100%";
  if (pct >= 99) return `${pct.toFixed(2)}%`;
  return `${pct.toFixed(1)}%`;
}

function formatCount(n: number): string {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(n % 1_000_000 === 0 ? 0 : 1)}M`;
  if (n >= 10_000) return `${Math.round(n / 1000)}k`;
  if (n >= 1000) return `${(n / 1000).toFixed(1)}k`;
  return n.toLocaleString("en-US");
}
