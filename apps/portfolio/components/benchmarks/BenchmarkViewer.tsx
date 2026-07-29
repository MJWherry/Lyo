"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import {
  currentSnapshotFile,
  defaultCompareFile,
  isLatestHistoryFile,
  withDeltasAgainst,
} from "@/lib/benchmarks/deltas";
import type { BenchReport, HistoryEntry } from "@/lib/benchmarks/types";
import { HistorySelect } from "./HistorySelect";
import { LoadReport } from "./LoadReport";
import { MicroReport } from "./MicroReport";

const COMPARE_NONE = "none";

async function fetchReport(suite: string, snapshot: string | null): Promise<BenchReport> {
  const url = snapshot
    ? `/api/benchmarks/${suite}?snapshot=${encodeURIComponent(snapshot)}`
    : `/api/benchmarks/${suite}`;
  const res = await fetch(url);
  if (!res.ok) {
    const body = (await res.json().catch(() => ({}))) as { error?: string };
    throw new Error(body.error ?? `Failed to load report (${res.status})`);
  }
  return res.json() as Promise<BenchReport>;
}

function buildHref(suite: string, snapshot: string | null, compare: string | null | undefined): string {
  const params = new URLSearchParams();
  if (snapshot) params.set("snapshot", snapshot);
  if (compare === COMPARE_NONE) params.set("compare", COMPARE_NONE);
  else if (compare) params.set("compare", compare);
  const qs = params.toString();
  return qs ? `/benchmarks/${suite}?${qs}` : `/benchmarks/${suite}`;
}

export function BenchmarkViewer({
  suite,
  initialReport,
}: {
  suite: string;
  initialReport: BenchReport;
}) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const snapshot = searchParams.get("snapshot");
  const compareParam = searchParams.get("compare");

  const [baseReport, setBaseReport] = useState<BenchReport>(initialReport);
  const [compareReport, setCompareReport] = useState<BenchReport | null>(null);
  const [compareReady, setCompareReady] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const history: HistoryEntry[] = baseReport.history ?? initialReport.history ?? [];

  const bakedCompare = useMemo(() => defaultCompareFile(baseReport, history), [baseReport, history]);
  const displayedFile = useMemo(() => currentSnapshotFile(snapshot, history), [snapshot, history]);

  const hideDeltas = compareParam === COMPARE_NONE;

  /**
   * Baseline file to Δ against.
   * - compare=none → none
   * - compare=<file> → that file (unless it is the displayed snapshot)
   * - unset → publisher prior-run file from deltaBaseline
   */
  const effectiveCompare = useMemo(() => {
    if (hideDeltas) return null;
    const requested =
      compareParam && compareParam !== COMPARE_NONE ? compareParam : bakedCompare;
    if (!requested || requested === displayedFile) return null;
    return requested;
  }, [hideDeltas, compareParam, bakedCompare, displayedFile]);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    fetchReport(suite, snapshot)
      .then((data) => {
        if (!cancelled) setBaseReport(data);
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load snapshot");
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [suite, snapshot]);

  useEffect(() => {
    let cancelled = false;
    if (!effectiveCompare) {
      setCompareReport(null);
      setCompareReady(true);
      return;
    }

    setCompareReady(false);
    // Latest is indexed in history but often only lives in data/<suite>.json.
    const compareSnapshot = isLatestHistoryFile(effectiveCompare, history)
      ? null
      : effectiveCompare;

    fetchReport(suite, compareSnapshot)
      .then((data) => {
        if (cancelled) return;
        // Guard: never Δ a report against itself.
        if (data.runId && baseReport.runId && data.runId === baseReport.runId) {
          setCompareReport(null);
        } else {
          setCompareReport(data);
        }
        setCompareReady(true);
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load compare baseline");
          setCompareReport(null);
          setCompareReady(true);
        }
      });

    return () => {
      cancelled = true;
    };
    // baseReport.runId: re-check self-compare after snapshot loads
  }, [suite, effectiveCompare, history, baseReport.runId]);

  const report = useMemo(() => {
    if (hideDeltas || !effectiveCompare) return withDeltasAgainst(baseReport, null);
    if (!compareReady) {
      // Keep publisher-baked Δs only while the default prior-run baseline is loading.
      if (compareParam == null && effectiveCompare === bakedCompare) return baseReport;
      return withDeltasAgainst(baseReport, null);
    }
    if (!compareReport) return withDeltasAgainst(baseReport, null);

    const recomputed = withDeltasAgainst(baseReport, compareReport);
    // Preserve "prior run" wording when the selection is still the publisher default.
    if (
      (compareParam == null || compareParam === bakedCompare) &&
      recomputed.deltaBaseline &&
      baseReport.deltaBaseline?.kind === "previousRun"
    ) {
      recomputed.deltaBaseline.kind = "previousRun";
    }
    return recomputed;
  }, [
    baseReport,
    compareReport,
    compareReady,
    hideDeltas,
    effectiveCompare,
    compareParam,
    bakedCompare,
  ]);

  return (
    <>
      <HistorySelect
        suite={suite}
        entries={history}
        currentFile={snapshot}
        compareFile={hideDeltas ? "" : (effectiveCompare ?? "")}
        onSelectSnapshot={(file) => {
          router.push(buildHref(suite, file, undefined));
        }}
        onSelectCompare={(file) => {
          router.push(buildHref(suite, snapshot, file ?? COMPARE_NONE));
        }}
      />
      {loading ? <p className="faint">Loading snapshot…</p> : null}
      {error ? <p className="badge badge-warn">{error}</p> : null}
      {report.type === "load" ? <LoadReport report={report} /> : <MicroReport report={report} />}
    </>
  );
}
