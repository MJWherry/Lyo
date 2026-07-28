"use client";

import { useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import type { BenchReport, HistoryEntry } from "@/lib/benchmarks/types";
import { HistorySelect } from "./HistorySelect";
import { LoadReport } from "./LoadReport";
import { MicroReport } from "./MicroReport";

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
  const [report, setReport] = useState<BenchReport>(initialReport);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const history: HistoryEntry[] = initialReport.history ?? report.history ?? [];

  useEffect(() => {
    let cancelled = false;
    const url = snapshot
      ? `/api/benchmarks/${suite}?snapshot=${encodeURIComponent(snapshot)}`
      : `/api/benchmarks/${suite}`;

    setLoading(true);
    setError(null);
    fetch(url)
      .then(async (res) => {
        if (!res.ok) {
          const body = (await res.json().catch(() => ({}))) as { error?: string };
          throw new Error(body.error ?? `Failed to load report (${res.status})`);
        }
        return res.json() as Promise<BenchReport>;
      })
      .then((data) => {
        if (!cancelled) setReport(data);
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load snapshot");
          // Keep showing previous/latest rather than blanking the page.
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [suite, snapshot]);

  return (
    <>
      <HistorySelect
        suite={suite}
        entries={history}
        currentFile={snapshot}
        onSelect={(file) => {
          if (!file) router.push(`/benchmarks/${suite}`);
          else router.push(`/benchmarks/${suite}?snapshot=${encodeURIComponent(file)}`);
        }}
      />
      {loading ? <p className="faint">Loading snapshot…</p> : null}
      {error ? <p className="badge badge-warn">{error}</p> : null}
      {report.type === "load" ? <LoadReport report={report} /> : <MicroReport report={report} />}
    </>
  );
}
