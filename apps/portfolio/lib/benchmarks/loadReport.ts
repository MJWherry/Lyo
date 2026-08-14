import { readFileSync, existsSync } from "node:fs";
import path from "node:path";
import type { BenchReport } from "./types";

import queryApi from "../../../../docs/benchmarks/data/query-api.json";
import csv from "../../../../docs/benchmarks/data/csv.json";
import cache from "../../../../docs/benchmarks/data/cache.json";
import compression from "../../../../docs/benchmarks/data/compression.json";
import encryption from "../../../../docs/benchmarks/data/encryption.json";
import hashing from "../../../../docs/benchmarks/data/hashing.json";
import lock from "../../../../docs/benchmarks/data/lock.json";
import query from "../../../../docs/benchmarks/data/query.json";
import xlsx from "../../../../docs/benchmarks/data/xlsx.json";

const latestByName: Record<string, BenchReport> = {
  "query-api": queryApi as unknown as BenchReport,
  csv: csv as unknown as BenchReport,
  cache: cache as unknown as BenchReport,
  compression: compression as unknown as BenchReport,
  encryption: encryption as unknown as BenchReport,
  hashing: hashing as unknown as BenchReport,
  lock: lock as unknown as BenchReport,
  query: query as unknown as BenchReport,
  xlsx: xlsx as unknown as BenchReport,
};

function historyJsonPath(suite: string, file: string): string {
  const stem = file.replace(/\.json$/i, "");
  // Prefer synced public copy, then repo history (gitignored, local only).
  const candidates = [
    path.join(process.cwd(), "public", "benchmarks", "history", suite, `${stem}.json`),
    path.join(process.cwd(), "..", "..", "docs", "benchmarks", "history", suite, `${stem}.json`),
    path.join(process.cwd(), "docs", "benchmarks", "history", suite, `${stem}.json`),
  ];
  return candidates.find((p) => existsSync(/* turbopackIgnore: true */ p)) ?? candidates[0];
}

export function loadLatestReport(name: string): BenchReport | null {
  return latestByName[name] ?? null;
}

export function loadHistoryReport(name: string, file: string): BenchReport | null {
  const full = historyJsonPath(name, file);
  if (!existsSync(/* turbopackIgnore: true */ full)) return null;
  try {
    return JSON.parse(readFileSync(/* turbopackIgnore: true */ full, "utf8")) as BenchReport;
  } catch {
    return null;
  }
}

export function resolveReport(name: string, snapshotFile?: string | null): BenchReport | null {
  const latest = loadLatestReport(name);
  if (!snapshotFile) return latest;

  // The history index marks isCurrent, but that file is often only published as data/<name>.json.
  const isCurrent =
    latest?.history?.some((h) => h.isCurrent && h.file === snapshotFile) ||
    (latest?.runId != null && snapshotFile.includes(latest.runId));
  if (isCurrent) return latest;

  const hist = loadHistoryReport(name, snapshotFile);
  if (hist) {
    // History snapshots often omit the index; keep the latest suite's history for the picker.
    return {
      ...hist,
      history: hist.history?.length ? hist.history : latest?.history,
    };
  }

  return null;
}
