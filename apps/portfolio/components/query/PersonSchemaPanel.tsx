"use client";

import { useCallback, useEffect, useState } from "react";
import { PERSON_ROOT_QUERY_ENTITY_TYPES } from "lyo-person-api-client";

type PropMeta = { name: string; type: string; nullable: boolean };
type TypeMeta = { typeName: string; properties: PropMeta[] };

type MetadataPayload = {
  entity?: TypeMeta | null;
  request?: TypeMeta | null;
  response?: TypeMeta | null;
  keyPropertyName?: string;
  keyType?: string;
  error?: string;
};

export function PersonSchemaPanel() {
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<MetadataPayload | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await fetch("/api/person/metadata");
      const json = (await res.json()) as Record<string, unknown>;
      if (!res.ok) {
        setError(
          typeof json.error === "string" ? json.error : `Metadata failed (${res.status})`
        );
        setData(null);
        return;
      }
      setData(normalizeMetadata(json));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load metadata");
      setData(null);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (open && !data && !loading && !error) void load();
  }, [open, data, loading, error, load]);

  return (
    <div className="panel" style={{ marginBottom: "1rem" }}>
      <button
        type="button"
        className="schema-toggle"
        aria-expanded={open}
        onClick={() => setOpen((v) => !v)}
      >
        <span>{open ? "▾" : "▸"} Person DB / API structure</span>
        <span className="faint" style={{ fontSize: "0.82rem", fontWeight: 400 }}>
          getMetadata(&quot;person&quot;)
        </span>
      </button>
      {open ? (
        <div className="schema-panel-body">
          <p className="muted" style={{ fontSize: "0.9rem", marginTop: "0.75rem" }}>
            Root <code>POST /Query</code> joins use PeopleDbContext entity type names. Common
            types:
          </p>
          <p className="schema-entity-chips">
            {PERSON_ROOT_QUERY_ENTITY_TYPES.map((t) => (
              <code key={t}>{t}</code>
            ))}
          </p>
          <p className="muted" style={{ fontSize: "0.88rem" }}>
            Contact* joins typically use <code>p.Id → ca.PersonId</code>.
          </p>
          {loading ? <p className="muted">Loading metadata…</p> : null}
          {error ? (
            <p className="badge badge-warn" style={{ margin: "0.5rem 0" }}>
              {error}{" "}
              <button type="button" className="btn btn-ghost" style={{ marginLeft: "0.5rem" }} onClick={load}>
                Retry
              </button>
            </p>
          ) : null}
          {data ? (
            <div className="schema-meta-grid">
              {data.keyPropertyName ? (
                <p className="faint" style={{ fontSize: "0.85rem", gridColumn: "1 / -1" }}>
                  Key: <code>{data.keyPropertyName}</code> ({data.keyType})
                </p>
              ) : null}
              <TypeBlock title="Entity" type={data.entity} />
              <TypeBlock title="Request" type={data.request} />
              <TypeBlock title="Response" type={data.response} />
            </div>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}

function TypeBlock({ title, type }: { title: string; type?: TypeMeta | null }) {
  if (!type?.properties?.length) {
    return (
      <div>
        <h3 style={{ fontSize: "0.95rem" }}>{title}</h3>
        <p className="faint" style={{ fontSize: "0.85rem", margin: 0 }}>
          Not included in metadata.
        </p>
      </div>
    );
  }

  return (
    <div>
      <h3 style={{ fontSize: "0.95rem" }}>
        {title}
        <span className="faint" style={{ fontWeight: 400, marginLeft: "0.4rem" }}>
          {type.typeName}
        </span>
      </h3>
      <div className="table-wrap">
        <table className="data">
          <thead>
            <tr>
              <th>Property</th>
              <th>Type</th>
              <th>Null</th>
            </tr>
          </thead>
          <tbody>
            {type.properties.map((p) => (
              <tr key={p.name}>
                <td>
                  <code style={{ fontSize: "0.8rem" }}>{p.name}</code>
                </td>
                <td style={{ fontSize: "0.8rem" }}>{shortType(p.type)}</td>
                <td>{p.nullable ? "yes" : "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function shortType(type: string): string {
  const bare = type.replace(/^System\./, "").replace(/^.*\./, "");
  return bare.length > 40 ? `${bare.slice(0, 37)}…` : bare;
}

function normalizeMetadata(raw: Record<string, unknown>): MetadataPayload {
  const pickType = (value: unknown): TypeMeta | null => {
    if (!value || typeof value !== "object") return null;
    const o = value as Record<string, unknown>;
    const typeName = String(o.typeName ?? o.TypeName ?? "");
    const propsRaw = o.properties ?? o.Properties;
    if (!Array.isArray(propsRaw)) return typeName ? { typeName, properties: [] } : null;
    const properties: PropMeta[] = propsRaw.map((p) => {
      const row = (p ?? {}) as Record<string, unknown>;
      return {
        name: String(row.name ?? row.Name ?? ""),
        type: String(row.type ?? row.Type ?? ""),
        nullable: Boolean(row.nullable ?? row.Nullable),
      };
    });
    return { typeName, properties };
  };

  return {
    entity: pickType(raw.entity ?? raw.Entity),
    request: pickType(raw.request ?? raw.Request),
    response: pickType(raw.response ?? raw.Response),
    keyPropertyName: String(raw.keyPropertyName ?? raw.KeyPropertyName ?? ""),
    keyType: String(raw.keyType ?? raw.KeyType ?? ""),
  };
}
