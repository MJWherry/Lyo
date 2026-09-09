import { projectedValue, type LyoColumn } from "./types.js";

/** Dynamic CRUD QueryProject MaxKeySetCount; typed endpoints allow more. */
export const RELATED_KEY_CHUNK_SIZE = 100;

export type RelatedLoadGroup = {
  route: string;
  select: string[];
  fields: string[];
};

export function relatedIdKey(value: unknown): string | null {
  if (value == null) return null;
  if (typeof value === "string") {
    const t = value.trim();
    if (!t || t === "00000000-0000-0000-0000-000000000000") return null;
    return t;
  }
  if (typeof value === "number" && Number.isFinite(value)) return String(value);
  if (typeof value === "object" && "toString" in value) {
    const t = String(value).trim();
    if (!t || t === "[object Object]") return null;
    if (t === "00000000-0000-0000-0000-000000000000") return null;
    return t;
  }
  return null;
}

/** Visible id-columns grouped by related QueryProject route; Select paths are unioned and always include Id. */
export function relatedGroups<T>(
  columns: readonly LyoColumn<T>[],
  hiddenFields: ReadonlySet<string>
): RelatedLoadGroup[] {
  const byRoute = new Map<string, { select: Set<string>; fields: Set<string> }>();
  for (const col of columns) {
    if (!col.related || hiddenFields.has(col.field)) continue;
    const route = col.related.route.trim().replace(/\/+$/, "");
    let group = byRoute.get(route);
    if (!group) {
      group = { select: new Set(["Id"]), fields: new Set() };
      byRoute.set(route, group);
    }
    group.fields.add(col.field);
    for (const path of col.related.select) {
      const trimmed = path.trim();
      if (trimmed) group.select.add(trimmed);
    }
    group.select.add("Id");
  }
  return [...byRoute.entries()].map(([route, g]) => ({
    route,
    select: [...g.select],
    fields: [...g.fields],
  }));
}

export function collectRelatedIds(rows: readonly unknown[], fields: readonly string[]): unknown[] {
  const seen = new Set<string>();
  const ids: unknown[] = [];
  for (const row of rows) {
    for (const field of fields) {
      const raw = projectedValue(row, field);
      const key = relatedIdKey(raw);
      if (!key || seen.has(key.toLowerCase())) continue;
      seen.add(key.toLowerCase());
      ids.push(raw);
    }
  }
  return ids;
}

/** QueryProject Keys rows `[[id], …]`, chunked. */
export function chunkKeys(ids: readonly unknown[], chunkSize = RELATED_KEY_CHUNK_SIZE): unknown[][][] {
  const size = Math.max(1, chunkSize);
  const rows = ids.map((id) => [id]);
  const chunks: unknown[][][] = [];
  for (let i = 0; i < rows.length; i += size) chunks.push(rows.slice(i, i + size));
  return chunks;
}

export function relatedQueryBody(select: readonly string[], keys: unknown[][]): Record<string, unknown> {
  return {
    Select: [...select],
    Keys: keys,
    Start: 0,
    Amount: keys.length,
  };
}

export function indexRelatedItems(items: readonly unknown[]): Map<string, unknown> {
  const map = new Map<string, unknown>();
  for (const item of items) {
    const key = relatedIdKey(projectedValue(item, "Id"));
    if (key) map.set(key.toLowerCase(), item);
  }
  return map;
}

export type RelatedLookup = Map<string, Map<string, unknown>>;

export function lookupRelated(lookup: RelatedLookup, route: string, id: unknown): unknown | undefined {
  const key = relatedIdKey(id);
  if (!key) return undefined;
  return lookup.get(route.trim().replace(/\/+$/, ""))?.get(key.toLowerCase());
}

export async function loadRelatedEntities<T>(
  queryProject: (route: string, body: unknown) => Promise<{ ok?: boolean; data?: { isSuccess?: boolean; items?: unknown[] | null } }>,
  columns: readonly LyoColumn<T>[],
  hiddenFields: ReadonlySet<string>,
  rows: readonly unknown[]
): Promise<RelatedLookup> {
  const lookup: RelatedLookup = new Map();
  for (const group of relatedGroups(columns, hiddenFields)) {
    const ids = collectRelatedIds(rows, group.fields);
    if (ids.length === 0) continue;
    const byId = lookup.get(group.route) ?? new Map<string, unknown>();
    for (const keys of chunkKeys(ids)) {
      try {
        const res = await queryProject(group.route, relatedQueryBody(group.select, keys));
        if (!res?.ok || res.data?.isSuccess === false) continue;
        for (const [k, v] of indexRelatedItems(res.data?.items ?? [])) byId.set(k, v);
      } catch {
        /* parent grid still renders; cell gets null related */
      }
    }
    lookup.set(group.route, byId);
  }
  return lookup;
}
