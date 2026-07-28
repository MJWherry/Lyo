"use client";

import { formatTimestamp } from "@/lib/benchmarks/format";
import type { HistoryEntry } from "@/lib/benchmarks/types";

function label(entry: HistoryEntry): string {
  const parts: string[] = [];
  if (entry.runId) parts.push(entry.runId);
  const when = formatTimestamp(entry.runEnded || entry.runStarted || entry.generatedAt);
  if (when) parts.push(when);
  if (entry.isCurrent) parts.push("latest");
  return parts.join(" · ") || entry.file || "snapshot";
}

export function HistorySelect({
  suite,
  entries,
  currentFile,
  onSelect,
}: {
  suite: string;
  entries: HistoryEntry[];
  currentFile?: string | null;
  onSelect?: (file: string | null) => void;
}) {
  if (!entries.length) return null;

  const ordered = [...entries].reverse();

  return (
    <div className="panel" style={{ marginBottom: "1rem" }}>
      <div className="field-row" style={{ alignItems: "center" }}>
        <label>
          Snapshot
          <select
            value={currentFile ?? ""}
            onChange={(e) => {
              const file = e.target.value || null;
              onSelect?.(file);
            }}
          >
            <option value="">Latest (data/{suite}.json)</option>
            {ordered
              .filter((entry) => !entry.isCurrent)
              .map((entry) => (
                <option key={entry.file || entry.runId} value={entry.file}>
                  {label(entry)}
                </option>
              ))}
          </select>
        </label>
        <span className="faint" style={{ fontSize: "0.85rem" }}>
          {suite} · {entries.length} archived run{entries.length === 1 ? "" : "s"}
        </span>
      </div>
    </div>
  );
}
