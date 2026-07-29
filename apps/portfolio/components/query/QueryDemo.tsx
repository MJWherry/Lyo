"use client";

import { useCallback, useMemo, useState } from "react";
import {
  QueryBuilder,
  activeRequestPreview,
  createDefaultQueryBuilderValue,
  type QueryBuilderValue,
} from "lyo-query-react";
import "lyo-query-react/styles.css";
import {
  DEFAULT_PERSON_INCLUDES,
  DEFAULT_PERSON_SELECT_FIELDS,
} from "lyo-person-api-client";
import { CodeBlock } from "@/components/CodeBlock";

const PERSON_FIELDS = [
  "FirstName",
  "LastName",
  "SourceEntityType",
  "Id",
  "CreatedAt",
  "DateOfBirth",
] as const;

type PersonRow = Record<string, unknown>;

type RunPayload = {
  mode?: string;
  isSuccess?: boolean;
  total?: number | null;
  hasMore?: boolean | null;
  items?: PersonRow[];
  item?: PersonRow | null;
  error?: string;
  details?: unknown;
  start?: number;
  amount?: number;
  elapsedMs?: number;
};

export function QueryDemo() {
  const [builder, setBuilder] = useState<QueryBuilderValue>(() =>
    createDefaultQueryBuilderValue({
      defaultField: "FirstName",
      entityType: "PersonEntity",
      select: [...DEFAULT_PERSON_SELECT_FIELDS],
      include: [],
      amount: 10,
    })
  );
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<RunPayload | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [problemDetails, setProblemDetails] = useState<unknown>(null);
  const [statusCode, setStatusCode] = useState<number | null>(null);

  const requestPreview = useMemo(
    () => JSON.stringify(activeRequestPreview(builder), null, 2),
    [builder]
  );

  const run = useCallback(async () => {
    setLoading(true);
    setError(null);
    setProblemDetails(null);
    setStatusCode(null);
    try {
      const request =
        builder.mode === "concrete"
          ? builder.concrete
          : builder.mode === "project"
            ? builder.project
            : builder.mode === "query"
              ? builder.query
              : builder.get;

      const res = await fetch("/api/person/run", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ mode: builder.mode, request }),
      });
      const json = (await res.json()) as RunPayload;
      setStatusCode(res.status);
      if (!res.ok) {
        setError(json.error ?? `Request failed (${res.status})`);
        // Prefer upstream ProblemDetails; fall back to the whole BFF body.
        setProblemDetails(json.details ?? json);
        setData(null);
        return;
      }
      setData(json);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Request failed");
      setProblemDetails(null);
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [builder]);

  const runLabel =
    builder.mode === "concrete"
      ? "Run QueryConcrete"
      : builder.mode === "project"
        ? "Run QueryProject"
        : builder.mode === "query"
          ? "Run Query"
          : "Run Get";

  const problemJson =
    problemDetails == null
      ? null
      : typeof problemDetails === "string"
        ? problemDetails
        : JSON.stringify(problemDetails, null, 2);

  return (
    <div className="where-builder">
      <div className="panel">
        <h2 style={{ fontSize: "1.2rem" }}>Query builder</h2>
        <p className="muted" style={{ fontSize: "0.92rem" }}>
          Concrete, Projection, root Query, and Get — fixed Person routes via the BFF (no
          endpoint picker). In/NotIn values use chip input (Enter to add).
        </p>
        <QueryBuilder
          value={builder}
          onChange={setBuilder}
          defaultField="FirstName"
          fieldPresets={PERSON_FIELDS}
          includePresets={DEFAULT_PERSON_INCLUDES}
          selectPresets={DEFAULT_PERSON_SELECT_FIELDS}
        />
        <div className="field-row" style={{ marginTop: "1rem" }}>
          <button className="btn btn-primary" type="button" onClick={run} disabled={loading}>
            {loading ? "Running…" : runLabel}
          </button>
          <span className="faint" style={{ fontSize: "0.85rem" }}>
            Mode: {builder.mode}
            {builder.mode !== "get"
              ? ` · page ≤ ${builder.concrete.Amount ?? 10}`
              : ""}
          </span>
        </div>
      </div>

      <div className="grid-2 query-demo-split">
        <div className="panel query-demo-panel">
          <h2 style={{ fontSize: "1.15rem" }}>Request JSON</h2>
          <CodeBlock code={requestPreview} language="json" />
        </div>
        <div className="panel query-demo-panel">
          <h2 style={{ fontSize: "1.15rem" }}>Results</h2>
          {error ? (
            <p className="badge badge-warn" style={{ margin: "0 0 0.65rem" }}>
              {statusCode != null ? `${statusCode} · ${error}` : error}
            </p>
          ) : null}
          {problemJson ? (
            <>
              <h3 style={{ fontSize: "0.95rem", margin: "0 0 0.4rem" }}>Problem details</h3>
              <CodeBlock code={problemJson} language="json" />
            </>
          ) : null}
          {!error && !data ? (
            <p className="muted" style={{ margin: 0 }}>
              Run a request to see Person data from TestApi.
            </p>
          ) : null}
          {data ? (
            <>
              <div
                style={{
                  display: "flex",
                  gap: "0.5rem",
                  marginBottom: "0.65rem",
                  flexWrap: "wrap",
                }}
              >
                <span className={`badge ${data.isSuccess ? "badge-ok" : "badge-warn"}`}>
                  {data.isSuccess ? "Success" : "Failed"}
                </span>
                <span className="badge">{data.mode ?? builder.mode}</span>
                <span className="badge">
                  {data.items?.length ?? 0} rows
                  {data.total != null ? ` · total ${data.total}` : ""}
                  {data.hasMore ? " · has more" : ""}
                </span>
                {data.elapsedMs != null ? (
                  <span className="badge badge-accent">{data.elapsedMs.toFixed(0)} ms</span>
                ) : null}
              </div>
              {builder.mode === "get" && data.item ? (
                <CodeBlock code={JSON.stringify(data.item, null, 2)} language="json" />
              ) : (
                <ResultsTable items={data.items ?? []} mode={builder.mode} />
              )}
            </>
          ) : null}
        </div>
      </div>
    </div>
  );
}

function ResultsTable({
  items,
  mode,
}: {
  items: PersonRow[];
  mode: string;
}) {
  if (mode === "project" || mode === "query") {
    const keys = collectKeys(items);
    return (
      <div className="table-wrap">
        <table className="data">
          <thead>
            <tr>
              {keys.length === 0 ? <th>Row</th> : keys.map((k) => <th key={k}>{k}</th>)}
            </tr>
          </thead>
          <tbody>
            {items.length === 0 ? (
              <tr>
                <td colSpan={Math.max(keys.length, 1)} className="muted">
                  No rows returned.
                </td>
              </tr>
            ) : (
              items.map((row, i) => (
                <tr key={i}>
                  {keys.length === 0 ? (
                    <td>
                      <code>{JSON.stringify(row)}</code>
                    </td>
                  ) : (
                    keys.map((k) => (
                      <td key={k} style={{ fontSize: "0.82rem" }}>
                        {formatCell(row[k])}
                      </td>
                    ))
                  )}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    );
  }

  return (
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
          {items.length === 0 ? (
            <tr>
              <td colSpan={5} className="muted">
                No rows returned.
              </td>
            </tr>
          ) : (
            items.map((row, i) => (
              <tr key={String(row.id ?? row.Id ?? i)}>
                <td>{String(row.firstName ?? row.FirstName ?? "—")}</td>
                <td>{String(row.lastName ?? row.LastName ?? "—")}</td>
                <td>{String(row.sourceEntityType ?? row.SourceEntityType ?? "—")}</td>
                <td style={{ fontSize: "0.82rem" }}>
                  {formatDate(row.createdAt ?? row.CreatedAt ?? row.CreatedTimestamp)}
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
  );
}

function collectKeys(items: PersonRow[]): string[] {
  const keys = new Set<string>();
  for (const row of items.slice(0, 20)) {
    for (const k of Object.keys(row)) keys.add(k);
  }
  return [...keys].slice(0, 8);
}

function formatCell(value: unknown): string {
  if (value == null) return "—";
  if (typeof value === "object") return JSON.stringify(value);
  return String(value);
}

function formatDate(value: unknown): string {
  if (value == null || value === "") return "—";
  const d = new Date(String(value));
  if (Number.isNaN(d.getTime())) return String(value);
  return d.toLocaleString();
}
