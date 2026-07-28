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
  "query-api": queryApi as BenchReport,
  csv: csv as BenchReport,
  cache: cache as BenchReport,
  compression: compression as BenchReport,
  encryption: encryption as BenchReport,
  hashing: hashing as BenchReport,
  lock: lock as BenchReport,
  query: query as BenchReport,
  xlsx: xlsx as BenchReport,
};

function historyJsonPath(suite: string, file: string): string {
  const stem = file.replace(/\.json$/i, "");
  // Prefer synced public copy, then repo history (gitignored, local only).
  const candidates = [
    path.join(process.cwd(), "public", "benchmarks", "history", suite, `${stem}.json`),
    path.join(process.cwd(), "..", "..", "docs", "benchmarks", "history", suite, `${stem}.json`),
    path.join(process.cwd(), "docs", "benchmarks", "history", suite, `${stem}.json`),
  ];
  return candidates.find((p) => existsSync(p)) ?? candidates[0];
}

export function loadLatestReport(name: string): BenchReport | null {
  return latestByName[name] ?? null;
}

export function loadHistoryReport(name: string, file: string): BenchReport | null {
  const full = historyJsonPath(name, file);
  if (!existsSync(full)) return null;
  try {
    return JSON.parse(readFileSync(full, "utf8")) as BenchReport;
  } catch {
    return null;
  }
}

export function resolveReport(name: string, snapshotFile?: string | null): BenchReport | null {
  const latest = loadLatestReport(name);
  if (snapshotFile) {
    const hist = loadHistoryReport(name, snapshotFile);
    if (hist) {
      // History snapshots often omit the index; keep the latest suite's history for the picker.
      return {
        ...hist,
        history: hist.history?.length ? hist.history : latest?.history,
      };
    }
  }
  return latest;
}
