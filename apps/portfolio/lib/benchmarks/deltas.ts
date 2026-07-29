import type { BenchReport, HistoryEntry } from "./types";

function pctDelta(current: number | undefined | null, previous: number | undefined | null): number | undefined {
  if (current == null || previous == null || previous === 0 || Number.isNaN(current) || Number.isNaN(previous)) {
    return undefined;
  }
  return ((current - previous) / previous) * 100;
}

/** Match publisher `_param_key`: sorted (k, str(v)) pairs. */
function paramKey(parameters: Record<string, string> | undefined): string {
  return Object.entries(parameters ?? {})
    .map(([k, v]) => [k, String(v)] as const)
    .sort(([a], [b]) => (a < b ? -1 : a > b ? 1 : 0))
    .map(([k, v]) => `${k}=${v}`)
    .join("\0");
}

function measurementKey(group: string, method: string | undefined, parameters: Record<string, string> | undefined): string {
  return `${group}\0${method ?? ""}\0${paramKey(parameters)}`;
}

function comparisonKey(axis: string, algorithm: string | undefined, parameters: Record<string, string> | undefined): string {
  return `${axis}\0${algorithm ?? ""}\0${paramKey(parameters)}`;
}

function clearDeltas(report: BenchReport): BenchReport {
  const next: BenchReport = structuredClone(report);
  delete next.deltaBaseline;

  for (const group of next.groups ?? []) {
    for (const m of group.measurements ?? []) {
      delete m.deltaMeanPct;
      delete m.deltaAllocPct;
    }
  }

  for (const group of next.comparison?.groups ?? []) {
    for (const row of group.rows ?? []) {
      delete row.deltaMeanPct;
      delete row.deltaAllocPct;
    }
  }

  for (const scenario of next.scenarios ?? []) {
    delete scenario.deltaP95Pct;
  }

  return next;
}

function applyMicroDeltas(report: BenchReport, baseline: BenchReport): void {
  const meanByKey = new Map<string, number>();
  const allocByKey = new Map<string, number>();

  for (const group of baseline.groups ?? []) {
    const gname = group.name ?? "";
    for (const m of group.measurements ?? []) {
      const key = measurementKey(gname, m.method, m.parameters);
      if (m.meanNs != null) meanByKey.set(key, m.meanNs);
      if (m.allocatedBytes != null) allocByKey.set(key, m.allocatedBytes);
    }
  }

  for (const group of report.groups ?? []) {
    const gname = group.name ?? "";
    for (const m of group.measurements ?? []) {
      const key = measurementKey(gname, m.method, m.parameters);
      const deltaMean = pctDelta(m.meanNs, meanByKey.get(key));
      const deltaAlloc = pctDelta(m.allocatedBytes, allocByKey.get(key));
      if (deltaMean != null) m.deltaMeanPct = deltaMean;
      else delete m.deltaMeanPct;
      if (deltaAlloc != null) m.deltaAllocPct = deltaAlloc;
      else delete m.deltaAllocPct;
    }
  }

  const cmpMean = new Map<string, number>();
  const cmpAlloc = new Map<string, number>();
  for (const group of baseline.comparison?.groups ?? []) {
    const axis = group.axis ?? "";
    for (const row of group.rows ?? []) {
      const key = comparisonKey(axis, row.algorithm, row.parameters);
      if (row.meanNs != null) cmpMean.set(key, row.meanNs);
      if (row.allocatedBytes != null) cmpAlloc.set(key, row.allocatedBytes);
    }
  }

  for (const group of report.comparison?.groups ?? []) {
    const axis = group.axis ?? "";
    for (const row of group.rows ?? []) {
      const key = comparisonKey(axis, row.algorithm, row.parameters);
      const deltaMean = pctDelta(row.meanNs, cmpMean.get(key));
      const deltaAlloc = pctDelta(row.allocatedBytes, cmpAlloc.get(key));
      if (deltaMean != null) row.deltaMeanPct = deltaMean;
      else delete row.deltaMeanPct;
      if (deltaAlloc != null) row.deltaAllocPct = deltaAlloc;
      else delete row.deltaAllocPct;
    }
  }
}

function applyLoadDeltas(report: BenchReport, baseline: BenchReport): void {
  const p95ByName = new Map<string, number>();
  for (const scenario of baseline.scenarios ?? []) {
    if (scenario.name && scenario.latency?.p95 != null) {
      p95ByName.set(scenario.name, scenario.latency.p95);
    }
  }

  for (const scenario of report.scenarios ?? []) {
    if (!scenario.name) continue;
    const delta = pctDelta(scenario.latency?.p95, p95ByName.get(scenario.name));
    if (delta != null) scenario.deltaP95Pct = delta;
    else delete scenario.deltaP95Pct;
  }
}

/**
 * Recompute Δ fields on `report` against a baseline snapshot (or clear them).
 * Δ% = (displayed − baseline) / baseline × 100, keyed by group/method/params
 * (micro) or scenario name (load) — same as scripts/benchmarks/build_manifests.py.
 */
export function withDeltasAgainst(report: BenchReport, baseline: BenchReport | null): BenchReport {
  const next = clearDeltas(report);
  if (!baseline) return next;

  if (next.type === "load") applyLoadDeltas(next, baseline);
  else applyMicroDeltas(next, baseline);

  next.deltaBaseline = {
    kind: "selectedRun",
    runId: baseline.runId,
    runStarted: baseline.runStarted,
    runEnded: baseline.runEnded,
  };
  return next;
}

/** History file for the report's baked-in delta baseline, if present. */
export function defaultCompareFile(report: BenchReport, history: HistoryEntry[]): string | null {
  const runId = report.deltaBaseline?.runId;
  if (!runId) return null;
  return history.find((h) => h.runId === runId)?.file ?? null;
}

/** Snapshot file currently displayed (null = latest / isCurrent). */
export function currentSnapshotFile(snapshot: string | null, history: HistoryEntry[]): string | null {
  if (snapshot) return snapshot;
  return history.find((h) => h.isCurrent)?.file ?? null;
}

/** True when `file` is the suite's latest (isCurrent) entry — may not exist on disk. */
export function isLatestHistoryFile(file: string | null | undefined, history: HistoryEntry[]): boolean {
  if (!file) return true;
  return history.some((h) => h.isCurrent && h.file === file);
}
