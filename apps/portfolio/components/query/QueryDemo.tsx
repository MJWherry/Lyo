"use client";

import { useCallback, useMemo, useState } from "react";
import {
  QueryBuilder,
  activeRequestPreview,
  createDefaultQueryBuilderValue,
  type QueryBuilderValue,
} from "lyo-query-components";
import { LyoAlert, LyoButton, LyoChip, LyoStack } from "lyo-web-components";
import {
  DEFAULT_PERSON_INCLUDES,
  DEFAULT_PERSON_ROOT_QUERY_SELECT_FIELDS,
  DEFAULT_PERSON_SELECT_FIELDS,
  PERSON_ROOT_QUERY_ENTITY_TYPES,
} from "lyo-person-api-client";
import { ClientCodeBlock } from "@/components/ClientCodeBlock";
import { PersonSchemaPanel } from "./PersonSchemaPanel";

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

function createPersonBuilder(): QueryBuilderValue {
  const builder = createDefaultQueryBuilderValue({
    defaultField: "FirstName",
    entityType: "Person",
    select: [...DEFAULT_PERSON_SELECT_FIELDS],
    include: [],
    amount: 10,
  });
  // Root Query cannot use nested projection paths — keep a clean scalar Select.
  builder.query.Select = DEFAULT_PERSON_ROOT_QUERY_SELECT_FIELDS.map((f) => `p.${f}`);
  return builder;
}

export function QueryDemo() {
  const [builder, setBuilder] = useState<QueryBuilderValue>(createPersonBuilder);
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<RunPayload | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [problemDetails, setProblemDetails] = useState<unknown>(null);
  const [statusCode, setStatusCode] = useState<number | null>(null);

  const requestPreview = useMemo(
    () => JSON.stringify(activeRequestPreview(builder), null, 2),
    [builder]
  );

  const activeAmount =
    builder.mode === "concrete"
      ? builder.concrete.Amount
      : builder.mode === "project"
        ? builder.project.Amount
        : builder.mode === "query"
          ? builder.query.Amount
          : null;

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

  const selectPresets =
    builder.mode === "query"
      ? DEFAULT_PERSON_ROOT_QUERY_SELECT_FIELDS.map((f) => `p.${f}`)
      : DEFAULT_PERSON_SELECT_FIELDS;

  return (
    <div className="where-builder">
      <PersonSchemaPanel />

      <div className="panel">
        <h2 style={{ fontSize: "1.2rem" }}>Query builder</h2>
        <p className="muted" style={{ fontSize: "0.92rem" }}>
          Concrete, Projection, root Query, and Get — fixed Person routes via the BFF (no
          endpoint picker). In/NotIn values use chip input (Enter to add). Root Query Select
          must be <code>alias.property</code>; +Join defaults to ContactAddressEntity on{" "}
          <code>PersonId</code>.
        </p>
        <QueryBuilder
          value={builder}
          onChange={setBuilder}
          defaultField="FirstName"
          fieldPresets={PERSON_FIELDS}
          includePresets={DEFAULT_PERSON_INCLUDES}
          selectPresets={selectPresets}
          entityTypePresets={PERSON_ROOT_QUERY_ENTITY_TYPES}
        />
        <LyoStack direction="row" spacing={1} alignItems="center" sx={{ mt: 2 }}>
          <LyoButton variant="contained" onClick={() => void run()} disabled={loading}>
            {loading ? "Running…" : runLabel}
          </LyoButton>
          <span className="faint" style={{ fontSize: "0.85rem" }}>
            Mode: {builder.mode}
            {activeAmount != null ? ` · page ≤ ${activeAmount}` : ""}
          </span>
        </LyoStack>
      </div>

      <div className="grid-2 query-demo-split">
        <div className="panel query-demo-panel">
          <h2 style={{ fontSize: "1.15rem" }}>Request JSON</h2>
          <ClientCodeBlock code={requestPreview} language="json" />
        </div>
        <div className="panel query-demo-panel">
          <h2 style={{ fontSize: "1.15rem" }}>Results</h2>
          {error ? (
            <LyoAlert severity="warning" sx={{ mb: 1 }}>
              {statusCode != null ? `${statusCode} · ${error}` : error}
            </LyoAlert>
          ) : null}
          {problemJson ? (
            <>
              <h3 style={{ fontSize: "0.95rem", margin: "0 0 0.4rem" }}>Problem details</h3>
              <ClientCodeBlock code={problemJson} language="json" />
            </>
          ) : null}
          {!error && !data ? (
            <p className="muted" style={{ margin: 0 }}>
              Run a request to see Person data from TestApi.
            </p>
          ) : null}
          {data ? (
            <>
              <LyoStack direction="row" spacing={1} sx={{ mb: 1 }} flexWrap="wrap">
                <LyoChip
                  size="small"
                  color={data.isSuccess ? "success" : "warning"}
                  label={data.isSuccess ? "Success" : "Failed"}
                />
                <LyoChip size="small" label={data.mode ?? builder.mode} />
                <LyoChip
                  size="small"
                  label={`${data.items?.length ?? 0} rows${data.total != null ? ` · total ${data.total}` : ""}${data.hasMore ? " · has more" : ""}`}
                />
                {data.elapsedMs != null ? (
                  <LyoChip size="small" color="primary" label={`${data.elapsedMs.toFixed(0)} ms`} />
                ) : null}
              </LyoStack>
              {builder.mode === "get" && data.item ? (
                <ClientCodeBlock code={JSON.stringify(data.item, null, 2)} language="json" />
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
