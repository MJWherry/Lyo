import type { ParameterOptionsItem, QueryReq, WhereClause } from "lyo-query";
import { parseListJson, serializeListJson } from "../types/lyoTypeValue.js";
import type { LyoListJsonKind } from "../types/lyoTypeUi.js";

export type ParameterOptionsKind = "Static" | "Query" | "Sproc";

export const PARAMETER_OPTIONS_KINDS: readonly ParameterOptionsKind[] = ["Static", "Query", "Sproc"];

/** Port of the server `ParameterOptions`: the picker source stored on a definition parameter's `Options` column. */
export type ParameterOptions = {
  kind: ParameterOptionsKind;
  items: ParameterOptionsItem[];
  /** Route for the root query; blank means `Query`. */
  queryRoute: string | null;
  /** Root query template; where values may hold `{{SiblingKey}}` placeholders. */
  query: QueryReq | null;
  /** `schema.func` for Sproc options. */
  storedProcName: string | null;
  /** Sproc argument name to a literal or `{{SiblingKey}}`. */
  sprocParameters: Record<string, string>;
  keyField: string | null;
  labelField: string | null;
};

export type ParameterOptionsDraft = Pick<ParameterOptions, "kind"> & Partial<Omit<ParameterOptions, "kind">>;

export type SiblingValues = Readonly<Record<string, string | null | undefined>>;

const PLACEHOLDER = /\{\{([^{}]+)\}\}/g;
const SPROC_NAME = /^[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)+$/;

function normalizeKind(raw: unknown): ParameterOptionsKind | null {
  if (typeof raw === "number") return PARAMETER_OPTIONS_KINDS[raw] ?? null;
  if (typeof raw !== "string") return null;
  return PARAMETER_OPTIONS_KINDS.find((k) => k.toLowerCase() === raw.trim().toLowerCase()) ?? null;
}

function prop(record: Record<string, unknown>, name: string): unknown {
  const lower = name.toLowerCase();
  const key = Object.keys(record).find((k) => k.toLowerCase() === lower);
  return key === undefined ? undefined : record[key];
}

const asString = (v: unknown): string | null => (typeof v === "string" ? v : null);
const isRecord = (v: unknown): v is Record<string, unknown> => v !== null && typeof v === "object" && !Array.isArray(v);

const CAMEL_KEYS = new Set(["whereClause", "subClause", "$type"]);
const ENUM_KEYS = new Set(["Comparison", "TotalCountMode", "IncludeFilterMode", "Direction", "Type", "Operator"]);
const capitalize = (s: string) => (s ? s[0]!.toUpperCase() + s.slice(1) : s);

/** Server writes the query camelCase (`from`, `select`, `comparison: "equals"`); the TS models are PascalCase. */
function pascalizeQuery(value: unknown, parentKey = ""): unknown {
  if (Array.isArray(value)) return value.map((v) => pascalizeQuery(v, parentKey === "Value" ? "Value" : ""));
  if (!isRecord(value) || parentKey === "Value") return value;
  const out: Record<string, unknown> = {};
  for (const [k, v] of Object.entries(value)) {
    const lower = k.toLowerCase();
    const camel = [...CAMEL_KEYS].find((c) => c.toLowerCase() === lower);
    const key = camel ?? capitalize(k);
    out[key] = ENUM_KEYS.has(key) && typeof v === "string" ? capitalize(v) : key === "$type" && typeof v === "string" ? v.toLowerCase() : pascalizeQuery(v, key);
  }
  return out;
}

/** Parses Options JSON (camelCase or PascalCase, string or numeric kind). Null when blank, invalid, or the kind is unknown. */
export function parseParameterOptions(json: string | null | undefined): ParameterOptions | null {
  if (!json?.trim()) return null;
  let raw: unknown;
  try {
    raw = JSON.parse(json);
  } catch {
    return null;
  }
  if (!isRecord(raw)) return null;
  const kind = normalizeKind(prop(raw, "kind"));
  if (!kind) return null;
  const items = prop(raw, "items");
  const sproc = prop(raw, "sprocParameters");
  const query = prop(raw, "query");
  return {
    kind,
    items: Array.isArray(items)
      ? items.filter(isRecord).map((i) => ({ key: String(prop(i, "key") ?? ""), label: String(prop(i, "label") ?? prop(i, "key") ?? "") }))
      : [],
    queryRoute: asString(prop(raw, "queryRoute")),
    query: isRecord(query) ? (pascalizeQuery(query) as QueryReq) : null,
    storedProcName: asString(prop(raw, "storedProcName")),
    sprocParameters: isRecord(sproc) ? Object.fromEntries(Object.entries(sproc).map(([k, v]) => [k, String(v ?? "")])) : {},
    keyField: asString(prop(raw, "keyField")),
    labelField: asString(prop(raw, "labelField")),
  };
}

/** Kind of an Options document, or null when blank / invalid. */
export function optionsKindOf(json: string | null | undefined): ParameterOptionsKind | null {
  return parseParameterOptions(json)?.kind ?? null;
}

/** Writes Options JSON in the server shape (camelCase, lower-case kind, null fields omitted). */
export function serializeParameterOptions(options: ParameterOptionsDraft): string {
  const out: Record<string, unknown> = { kind: options.kind.toLowerCase(), items: options.items ?? [] };
  if (options.queryRoute != null) out.queryRoute = options.queryRoute;
  if (options.query != null) out.query = options.query;
  if (options.storedProcName != null) out.storedProcName = options.storedProcName;
  out.sprocParameters = options.sprocParameters ?? {};
  if (options.keyField != null) out.keyField = options.keyField;
  if (options.labelField != null) out.labelField = options.labelField;
  return JSON.stringify(out);
}

export function defaultOptionsQuery(): QueryReq {
  return {
    From: { Alias: "c", EntityType: "" },
    Select: ["c.Id", "c.Name"],
    ComputedFields: [
      { Name: "Key", Template: "{c.Id}" },
      { Name: "Value", Template: "{c.Name}" },
    ],
    Amount: 200,
    Options: { TotalCountMode: "None", IncludeFilterMode: "Full" },
  };
}

/** Starting Options JSON for a kind pick (placeholder item, root query template, or sproc stub). Null clears the options. */
export function createDefaultOptionsJson(kind: ParameterOptionsKind | null | undefined): string | null {
  if (!kind) return null;
  if (kind === "Static") return serializeParameterOptions({ kind, items: [{ key: "key", label: "label" }] });
  if (kind === "Sproc") return serializeParameterOptions({ kind, storedProcName: "public.report_rows", keyField: "Key", labelField: "Value" });
  return serializeParameterOptions({ kind, query: defaultOptionsQuery() });
}

/** Static Options whose items are the AllowedValues tokens (key = label). Null when the list is empty. */
export function optionsFromAllowedValues(allowedValues: string | null | undefined): string | null {
  const keys = parseListJson(allowedValues);
  return keys.length === 0 ? null : serializeParameterOptions({ kind: "Static", items: keys.map((k) => ({ key: k, label: k })) });
}

/** AllowedValues JSON array from static option keys; null for query / sproc options. */
export function allowedValuesFromOptions(optionsJson: string | null | undefined, kind: LyoListJsonKind = "String"): string | null {
  const options = parseParameterOptions(optionsJson);
  return options?.kind === "Static" ? serializeListJson(options.items.map((i) => i.key), kind) : null;
}

export function isValidStoredProcName(name: string | null | undefined): boolean {
  return Boolean(name?.trim()) && SPROC_NAME.test(name!.trim());
}

/** Message the options editor shows for an incomplete document, or null when it can load options. */
export function optionsEditorError(options: ParameterOptionsDraft): string | null {
  if (options.kind === "Sproc")
    return isValidStoredProcName(options.storedProcName) ? null : "Stored proc name must be schema.func (letters, digits, underscore).";
  if (options.kind !== "Query") return null;
  const query = options.query;
  if (!query?.From?.EntityType?.trim()) return "Set From.EntityType (table) so the Value picker can load options.";
  if (!query.Select?.length) return "Add at least one Select path (e.g. c.Id, c.Name).";
  return null;
}

/** `LyoQueryClient.query` takes the base route and appends `/Query`; the stored route names the whole endpoint. */
export function queryBaseRoute(route: string | null | undefined): string {
  const trimmed = route?.trim().replace(/^\/+|\/+$/g, "") || "Query";
  const base = trimmed.replace(/\/?Query$/i, "");
  return `/${base}`;
}

function collectFromValue(value: unknown, keys: Map<string, string>) {
  if (typeof value === "string") {
    for (const m of value.matchAll(PLACEHOLDER)) {
      const key = m[1]!.trim();
      if (!keys.has(key.toLowerCase())) keys.set(key.toLowerCase(), key);
    }
  } else if (Array.isArray(value)) for (const item of value) collectFromValue(item, keys);
}

function collectFromClause(clause: WhereClause | null | undefined, keys: Map<string, string>) {
  if (!clause) return;
  if (clause.$type === "condition") collectFromValue(clause.Value, keys);
  else for (const child of clause.Children) collectFromClause(child, keys);
  collectFromClause((clause as { subClause?: WhereClause }).subClause, keys);
}

const sortedKeys = (keys: Map<string, string>) => [...keys.values()].sort((a, b) => a.localeCompare(b, undefined, { sensitivity: "base" }));

function scopes(query: QueryReq): Array<WhereClause | null | undefined> {
  return [query.whereClause, query.From?.Query?.whereClause, ...(query.Joins ?? []).map((j) => j.Query?.whereClause)];
}

/** Distinct `{{Key}}` placeholders in the root, From, and Join where clauses. */
export function queryPlaceholderKeys(query: QueryReq): string[] {
  const keys = new Map<string, string>();
  for (const clause of scopes(query)) collectFromClause(clause, keys);
  return sortedKeys(keys);
}

export function sprocPlaceholderKeys(parameters: Readonly<Record<string, string>> | null | undefined): string[] {
  const keys = new Map<string, string>();
  for (const value of Object.values(parameters ?? {})) collectFromValue(value, keys);
  return sortedKeys(keys);
}

function lookup(siblings: SiblingValues, key: string): string | null | undefined {
  const lower = key.toLowerCase();
  const match = Object.keys(siblings).find((k) => k.toLowerCase() === lower);
  return match === undefined ? undefined : siblings[match];
}

function missingKeys(keys: string[], siblings: SiblingValues): string[] {
  return keys.filter((k) => !lookup(siblings, k)?.trim());
}

function replacePlaceholders(template: string, siblings: SiblingValues): string {
  return template.replace(PLACEHOLDER, (whole, key: string) => lookup(siblings, key.trim()) ?? whole);
}

function bindValue(value: unknown, siblings: SiblingValues): unknown {
  if (typeof value === "string") return replacePlaceholders(value, siblings);
  return Array.isArray(value) ? value.map((v) => bindValue(v, siblings)) : value;
}

function bindClause<T extends WhereClause | null | undefined>(clause: T, siblings: SiblingValues): T {
  if (!clause) return clause;
  const sub = (clause as { subClause?: WhereClause }).subClause;
  const bound: WhereClause =
    clause.$type === "condition"
      ? { ...clause, Value: bindValue(clause.Value, siblings) }
      : { ...clause, Children: clause.Children.map((c) => bindClause(c, siblings)) };
  if (sub) (bound as { subClause?: WhereClause }).subClause = bindClause(sub, siblings);
  return bound as T;
}

export type BindResult<T> = { bound: T; missing: [] } | { bound: null; missing: string[] };

/** Copies the template with `{{Key}}` placeholders filled from sibling values (case-insensitive). Fails listing keys whose sibling is missing or blank. */
export function bindQueryOptions(template: QueryReq, siblings: SiblingValues): BindResult<QueryReq> {
  const missing = missingKeys(queryPlaceholderKeys(template), siblings);
  if (missing.length > 0) return { bound: null, missing };
  const bound: QueryReq = { ...template, whereClause: bindClause(template.whereClause, siblings) };
  if (template.From?.Query) bound.From = { ...template.From, Query: { ...template.From.Query, whereClause: bindClause(template.From.Query.whereClause, siblings) } };
  if (template.Joins)
    bound.Joins = template.Joins.map((j) => (j.Query ? { ...j, Query: { ...j.Query, whereClause: bindClause(j.Query.whereClause, siblings) } } : j));
  return { bound, missing: [] };
}

export function bindSprocParameters(template: Readonly<Record<string, string>> | null | undefined, siblings: SiblingValues): BindResult<Record<string, string>> {
  const missing = missingKeys(sprocPlaceholderKeys(template), siblings);
  if (missing.length > 0) return { bound: null, missing };
  return { bound: Object.fromEntries(Object.entries(template ?? {}).map(([k, v]) => [k, replacePlaceholders(v, siblings)])), missing: [] };
}

function formatCell(value: unknown): string {
  if (value == null) return "";
  return typeof value === "object" ? JSON.stringify(value) : String(value);
}

function selectProperty(path: string): string {
  const trimmed = path.trim();
  return trimmed.slice(trimmed.lastIndexOf(".") + 1);
}

function pair(row: Record<string, unknown>, keyName: string, labelName: string | null): ParameterOptionsItem | null {
  const key = prop(row, keyName);
  if (key === undefined) return null;
  const keyText = formatCell(key);
  if (!keyText) return null;
  const label = labelName ? prop(row, labelName) : undefined;
  return { key: keyText, label: label === undefined ? keyText : formatCell(label) };
}

/**
 * Picker key / label from a projected row. With `fields` the named columns win; otherwise `Key` / `Value` columns, then the first two select paths, then a
 * single select path. Null when no key is found.
 */
export function readKeyValue(
  row: unknown,
  selectPaths: readonly string[] | null | undefined,
  fields?: { keyField?: string | null; labelField?: string | null }
): ParameterOptionsItem | null {
  if (!isRecord(row)) return null;
  if (fields) {
    const explicit = pair(row, fields.keyField?.trim() || "Key", fields.labelField?.trim() || "Value");
    if (explicit) return explicit;
  }
  if (prop(row, "Key") !== undefined && prop(row, "Value") !== undefined) return pair(row, "Key", "Value");
  if (selectPaths && selectPaths.length >= 2) return pair(row, selectProperty(selectPaths[0]!), selectProperty(selectPaths[1]!));
  if (selectPaths?.length === 1) return pair(row, selectProperty(selectPaths[0]!), null);
  return null;
}

/**
 * Drops selected keys that are no longer options (case-insensitive). Multi values filter the JSON array; single values clear to null. The value is left alone
 * when there are no items yet or `keepCustom` is set (formatter templates are free text).
 */
export function pruneSelection(
  value: string | null | undefined,
  items: readonly ParameterOptionsItem[],
  options: { multiple?: boolean; listKind?: LyoListJsonKind; keepCustom?: boolean } = {}
): string | null {
  if (!value || items.length === 0) return value ?? null;
  if (options.keepCustom && !options.multiple) return value;
  const keys = new Set(items.map((i) => i.key.toLowerCase()));
  if (options.multiple) return serializeListJson(parseListJson(value).filter((v) => keys.has(v.toLowerCase())), options.listKind ?? "String");
  return keys.has(value.toLowerCase()) ? value : null;
}
