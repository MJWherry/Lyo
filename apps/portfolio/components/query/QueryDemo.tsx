"use client";

import { useCallback, useMemo, useState } from "react";
import {
  WhereClauseBuilder,
  defaultGroup,
  type WhereClause,
} from "lyo-query-react";
import "lyo-query-react/styles.css";
import { CodeBlock } from "@/components/CodeBlock";

const PERSON_FIELDS = [
  "FirstName",
  "LastName",
  "SourceEntityType",
  "Id",
  "CreatedAt",
  "DateOfBirth",
] as const;

const DEMO_AMOUNT = 10;

type PersonRow = Record<string, unknown>;

type QueryPayload = {
  isSuccess?: boolean;
  total?: number | null;
  hasMore?: boolean | null;
  items?: PersonRow[];
  error?: string;
  start?: number;
  amount?: number;
  elapsedMs?: number;
};

export function QueryDemo() {
  const [whereClause, setWhereClause] = useState<WhereClause>(() =>
    defaultGroup("FirstName")
  );
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<QueryPayload | null>(null);
  const [error, setError] = useState<string | null>(null);

  const requestPreview = useMemo(
    () =>
      JSON.stringify(
        {
          Options: { TotalCountMode: "Exact", IncludeFilterMode: "Full" },
          Start: 0,
          Amount: DEMO_AMOUNT,
          whereClause,
          Include: [],
          SortBy: [],
        },
        null,
        2
      ),
    [whereClause]
  );

  const run = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await fetch("/api/person/query", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          start: 0,
          amount: DEMO_AMOUNT,
          totalCountMode: "Exact",
          whereClause,
        }),
      });
      const json = (await res.json()) as QueryPayload;
      if (!res.ok) {
        setError(json.error ?? `Request failed (${res.status})`);
        setData(null);
        return;
      }
      setData(json);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Request failed");
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [whereClause]);

  return (
    <div className="where-builder">
      <div className="panel">
        <h2 style={{ fontSize: "1.2rem" }}>Where clause</h2>
        <p className="muted" style={{ fontSize: "0.92rem" }}>
          Nested And/Or groups via <code>lyo-query-react</code> — same wire shape as the
          TypeScript Person client / .NET QueryNodeEditor. Date fields use a datetime picker.
        </p>
        <WhereClauseBuilder
          value={whereClause}
          onChange={setWhereClause}
          defaultField="FirstName"
          fieldPresets={PERSON_FIELDS}
        />
        <div className="field-row" style={{ marginTop: "1rem" }}>
          <button className="btn btn-primary" type="button" onClick={run} disabled={loading}>
            {loading ? "Querying…" : "Run QueryConcrete"}
          </button>
          <span className="faint" style={{ fontSize: "0.85rem" }}>
            Fixed page of {DEMO_AMOUNT} · TotalCountMode Exact
          </span>
        </div>
      </div>

      <div className="grid-2">
        <div className="panel">
          <h2 style={{ fontSize: "1.15rem" }}>Request JSON</h2>
          <CodeBlock code={requestPreview} language="json" />
        </div>
        <div className="panel">
          <h2 style={{ fontSize: "1.15rem" }}>Results</h2>
          {error ? <p className="badge badge-warn">{error}</p> : null}
          {!error && !data ? (
            <p className="muted">Run a query to see Person rows from TestApi.</p>
          ) : null}
          {data ? (
            <>
              <div style={{ display: "flex", gap: "0.5rem", marginBottom: "0.75rem", flexWrap: "wrap" }}>
                <span className={`badge ${data.isSuccess ? "badge-ok" : "badge-warn"}`}>
                  {data.isSuccess ? "Success" : "Failed"}
                </span>
                <span className="badge">
                  {data.items?.length ?? 0} rows
                  {data.total != null ? ` · total ${data.total}` : ""}
                  {data.hasMore ? " · has more" : ""}
                </span>
                {data.elapsedMs != null ? (
                  <span className="badge badge-accent">{data.elapsedMs.toFixed(0)} ms</span>
                ) : null}
              </div>
              <div className="table-wrap">
                <table className="data">
                  <thead>
                    <tr>
                      <th>First</th>
                      <th>Last</th>
                      <th>Source</th>
                      <th>Created</th>
                      <th>Id</th>
                    </tr>
                  </thead>
                  <tbody>
                    {(data.items ?? []).length === 0 ? (
                      <tr>
                        <td colSpan={5} className="muted">
                          No rows returned.
                        </td>
                      </tr>
                    ) : (
                      (data.items ?? []).map((row, i) => (
                        <tr key={String(row.id ?? row.Id ?? i)}>
                          <td>{String(row.firstName ?? row.FirstName ?? "—")}</td>
                          <td>{String(row.lastName ?? row.LastName ?? "—")}</td>
                          <td>{String(row.sourceEntityType ?? row.SourceEntityType ?? "—")}</td>
                          <td style={{ fontSize: "0.82rem" }}>
                            {formatDate(row.createdAt ?? row.CreatedAt)}
                          </td>
                          <td
                            className="faint"
                            style={{ fontFamily: "var(--font-mono)", fontSize: "0.78rem" }}
                          >
                            {String(row.id ?? row.Id ?? "—").slice(0, 13)}…
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </>
          ) : null}
        </div>
      </div>
    </div>
  );
}

function formatDate(value: unknown): string {
  if (value == null || value === "") return "—";
  const d = new Date(String(value));
  if (Number.isNaN(d.getTime())) return String(value);
  return d.toLocaleString();
}
