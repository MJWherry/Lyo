import type { ConditionClause, ProjectionQueryReq, QueryConcreteReq, QueryReq, SortBy } from "lyo-query";
import { buildGridWhere, defaultQueryOptions } from "lyo-query";
import type { LyoColumn } from "./types.js";

function selectFields<T>(
  columns: readonly LyoColumn<T>[],
  hiddenFields: ReadonlySet<string>,
  keyFields?: readonly string[]
): string[] {
  const select = columns.filter((c) => !hiddenFields.has(c.field)).map((c) => c.field);
  for (const key of keyFields ?? []) {
    if (!select.includes(key)) select.unshift(key);
  }
  return select;
}

function prefixSelect(fields: readonly string[], alias: string): string[] {
  return fields
    .filter((f) => isRootSafeSelect(f, alias))
    .map((f) => (f.includes(".") ? f : `${alias}.${f}`));
}

/** Root Query Select is alias.scalar — nested paths like ContactAddresses.Count are QueryProject-only. */
function isRootSafeSelect(field: string, alias: string): boolean {
  const rest = field.startsWith(`${alias}.`) ? field.slice(alias.length + 1) : field;
  return Boolean(rest) && !rest.includes(".");
}

export function buildConcreteQuery(options: {
  start: number;
  amount: number;
  filters: readonly ConditionClause[];
  searchText?: string | null;
  quickSearchProperties: readonly string[];
  sorts: readonly SortBy[];
  include?: readonly string[];
}): QueryConcreteReq {
  return {
    Options: defaultQueryOptions({ TotalCountMode: "Exact" }),
    Start: options.start,
    Amount: options.amount,
    whereClause: buildGridWhere({
      filters: options.filters,
      searchText: options.searchText,
      quickSearchProperties: options.quickSearchProperties,
    }),
    SortBy: [...options.sorts],
    Include: options.include ? [...options.include] : [],
  };
}

export function buildProjectedQuery<T>(options: {
  start: number;
  amount: number;
  filters: readonly ConditionClause[];
  searchText?: string | null;
  quickSearchProperties: readonly string[];
  sorts: readonly SortBy[];
  columns: readonly LyoColumn<T>[];
  hiddenFields: ReadonlySet<string>;
  keyFields?: readonly string[];
  zipSiblingCollectionSelections?: boolean;
}): ProjectionQueryReq {
  const select = selectFields(options.columns, options.hiddenFields, options.keyFields);
  return {
    Options: defaultQueryOptions({
      TotalCountMode: "Exact",
      ZipSiblingCollectionSelections: options.zipSiblingCollectionSelections ?? true,
    }),
    Start: options.start,
    Amount: options.amount,
    Select: select,
    whereClause: buildGridWhere({
      filters: options.filters,
      searchText: options.searchText,
      quickSearchProperties: options.quickSearchProperties,
    }),
    SortBy: [...options.sorts],
  };
}

/** Root POST /Query — From + alias.property Select. */
export function buildRootQuery<T>(options: {
  start: number;
  amount: number;
  filters: readonly ConditionClause[];
  searchText?: string | null;
  quickSearchProperties: readonly string[];
  sorts: readonly SortBy[];
  columns: readonly LyoColumn<T>[];
  hiddenFields: ReadonlySet<string>;
  keyFields?: readonly string[];
  entityType?: string;
  fromAlias?: string;
}): QueryReq {
  const alias = options.fromAlias ?? "p";
  const select = prefixSelect(
    selectFields(options.columns, options.hiddenFields, options.keyFields),
    alias
  );
  return {
    Options: defaultQueryOptions({ TotalCountMode: "Exact" }),
    Start: options.start,
    Amount: options.amount,
    From: {
      Alias: alias,
      EntityType: options.entityType ?? "Person",
    },
    Select: select,
    whereClause: buildGridWhere({
      filters: options.filters,
      searchText: options.searchText,
      quickSearchProperties: options.quickSearchProperties,
    }),
    SortBy: options.sorts.filter((s) => isRootSafeSelect(s.PropertyName, alias)),
  };
}
