import type { AggregateSelect, ComputedField, JoinClause, JoinOn, QueryReq, QueryRequestBase, QueryRequestOptions } from "lyo-query";

/** One key set typed as `1` or `"abc", 2`: comma parts, quotes stripped, numeric parts as numbers. */
export function parseKeyInput(input: string): unknown[] {
  const trimmed = input.trim();
  if (!trimmed) return [];
  return trimmed.split(",").map((part) => {
    const text = part.trim();
    if (text.length >= 2 && text.startsWith('"') && text.endsWith('"')) return text.slice(1, -1).replace(/\\"/g, '"');
    return /^-?\d+(\.\d+)?$/.test(text) ? Number(text) : text;
  });
}

/** QueryRequest `Keys` rows (`[[...]]`) from the chips in the keys editor. */
export function keysFromLines(lines: readonly string[]): unknown[][] {
  return lines.map(parseKeyInput).filter((key) => key.length > 0);
}

export function formatKeySet(keySet: readonly unknown[]): string {
  return keySet.map((part) => (part == null ? "null" : typeof part === "string" ? `"${part}"` : String(part))).join(", ");
}

export function withKeys<T extends QueryRequestBase>(req: T, keys: unknown[][]): T {
  const { Keys: _old, ...rest } = req;
  return (keys.length > 0 ? { ...rest, Keys: keys } : rest) as T;
}

export function addComputedField(fields: readonly ComputedField[], name: string, template: string): ComputedField[] {
  if (!name.trim() || !template.trim()) return [...fields];
  return [...fields, { Name: name.trim(), Template: template.trim() }];
}

export function withComputedFields<T extends { ComputedFields?: ComputedField[] }>(req: T, fields: readonly ComputedField[]): T {
  const { ComputedFields: _old, ...rest } = req;
  return (fields.length > 0 ? { ...rest, ComputedFields: [...fields] } : rest) as T;
}

/** Empty aggregates are omitted. A blank field is omitted, including a Count of the rows in the group. */
export function withAggregates<T extends { Aggregates?: AggregateSelect[] }>(req: T, aggregates: readonly AggregateSelect[]): T {
  const { Aggregates: _old, ...rest } = req;
  if (aggregates.length === 0) return rest as T;
  return {
    ...rest,
    Aggregates: aggregates.map((item) => {
      const field = item.Field?.trim();
      return field ? { Function: item.Function, Name: item.Name, Field: field } : { Function: item.Function, Name: item.Name };
    }),
  } as T;
}

function mapJoin(query: QueryReq, index: number, update: (join: JoinClause) => JoinClause): QueryReq {
  const Joins = (query.Joins ?? []).map((join, i) => (i === index ? update(join) : join));
  return { ...query, Joins };
}

/** Patches one join. A blank `As` removes it (the related rows are then flattened into the parent rather than nested). */
export function patchJoin(query: QueryReq, index: number, patch: Partial<Omit<JoinClause, "On">>): QueryReq {
  return mapJoin(query, index, (join) => {
    const next = { ...join, ...patch };
    if (patch.As === undefined) return next;
    const { As: _as, ...rest } = next;
    return patch.As?.trim() ? { ...rest, As: patch.As.trim() } : rest;
  });
}

export function addJoinOn(query: QueryReq, index: number): QueryReq {
  const fromAlias = query.From.Alias || "p";
  return mapJoin(query, index, (join) => ({ ...join, On: [...join.On, { From: `${fromAlias}.`, To: `${join.Alias}.` }] }));
}

export function patchJoinOn(query: QueryReq, index: number, onIndex: number, patch: Partial<JoinOn>): QueryReq {
  return mapJoin(query, index, (join) => ({ ...join, On: join.On.map((on, i) => (i === onIndex ? { ...on, ...patch } : on)) }));
}

export function removeJoinOn(query: QueryReq, index: number, onIndex: number): QueryReq {
  return mapJoin(query, index, (join) => ({ ...join, On: join.On.filter((_, i) => i !== onIndex) }));
}

/** Sibling collection zipping is on unless the request turns it off. */
export function zipEnabled(req: { Options: QueryRequestOptions }): boolean {
  return req.Options.ZipSiblingCollectionSelections ?? true;
}

export function withZip<T extends { Options: QueryRequestOptions }>(req: T, zip: boolean): T {
  return { ...req, Options: { ...req.Options, ZipSiblingCollectionSelections: zip } };
}
