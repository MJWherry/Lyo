"use client";

import { formatTimestamp } from "@/lib/benchmarks/format";
import type { HistoryEntry } from "@/lib/benchmarks/types";

function label(entry: HistoryEntry): string {
  const when =
    formatTimestamp(entry.runEnded || entry.runStarted || entry.generatedAt) ?? "Unknown time";
  return entry.isCurrent ? `${when} (latest)` : when;
}

export function HistorySelect({
  suite,
  entries,
  currentFile,
  compareFile,
  onSelectSnapshot,
  onSelectCompare,
}: {
  suite: string;
  entries: HistoryEntry[];
  currentFile?: string | null;
  compareFile?: string | null;
  onSelectSnapshot?: (file: string | null) => void;
  onSelectCompare?: (file: string | null) => void;
}) {
  if (!entries.length) return null;

  const ordered = [...entries].reverse();
  const latestEntry = ordered.find((e) => e.isCurrent) ?? null;
  const displayedFile = currentFile || latestEntry?.file || null;
  const compareOptions = ordered.filter((entry) => entry.file && entry.file !== displayedFile);

  return (
    <div className="panel" style={{ marginBottom: "1rem" }}>
      <div className="field-row" style={{ alignItems: "center", flexWrap: "wrap", gap: "0.75rem 1.25rem" }}>
        <label>
          Snapshot
          <select
            value={currentFile ?? ""}
            onChange={(e) => {
              onSelectSnapshot?.(e.target.value || null);
            }}
          >
            <option value="">{latestEntry ? label(latestEntry) : "Latest"}</option>
            {ordered
              .filter((entry) => !entry.isCurrent)
              .map((entry) => (
                <option key={entry.file || entry.runId} value={entry.file}>
                  {label(entry)}
                </option>
              ))}
          </select>
        </label>

        {compareOptions.length ? (
          <label>
            Compare against
            <select
              value={compareFile ?? ""}
              onChange={(e) => {
                onSelectCompare?.(e.target.value || null);
              }}
            >
              <option value="">None (hide Δ)</option>
              {compareOptions.map((entry) => (
                <option key={entry.file || entry.runId} value={entry.file}>
                  {label(entry)}
                </option>
              ))}
            </select>
          </label>
        ) : null}

        <span className="faint" style={{ fontSize: "0.85rem" }}>
          {suite} · {entries.length} archived run{entries.length === 1 ? "" : "s"}
        </span>
      </div>
    </div>
  );
}
