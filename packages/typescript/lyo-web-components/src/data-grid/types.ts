import type {
  ConditionClause,
  FilterPropertyDefinition,
  ProjectionQueryReq,
  QueryConcreteReq,
  SortBy,
} from "lyo-query";
import type { ReactNode } from "react";
import type { LyoIdAbbreviation } from "../display/idField.js";

export const LyoDataGridFeatureFlags = {
  None: 0,
  BulkMenu: 1 << 0,
  BulkExport: 1 << 1,
  BulkPatch: 1 << 2,
  BulkDelete: 1 << 3,
  Filterable: 1 << 4,
  Searchable: 1 << 5,
  AutoRefresh: 1 << 6,
  All:
    (1 << 0) |
    (1 << 1) |
    (1 << 2) |
    (1 << 3) |
    (1 << 4) |
    (1 << 5) |
    (1 << 6),
} as const;

export type LyoDataGridFeatureFlags = number;

export function hasFeature(flags: number, feature: number): boolean {
  return (flags & feature) === feature;
}

export type LyoRelatedQueryProject = (
  route: string,
  body: unknown
) => Promise<{ ok?: boolean; data?: { isSuccess?: boolean; items?: unknown[] | null } }>;

/** API client a related lookup targets when the related entity lives behind a different API than the grid's. */
export type LyoRelatedClient = { queryProject: LyoRelatedQueryProject };

export type LyoRelatedLookup = {
  /** Related entity QueryProject base route (for example `PersonAddress`). */
  route: string;
  /** Related Select paths. `Id` is always unioned by the loader. */
  select: readonly string[];
  /** Client for the related QueryProject. Defaults to the grid's `apiClient`. */
  client?: LyoRelatedClient;
};

export type LyoColumnAlign = "left" | "center" | "right";

export type LyoColumn<T> = {
  id: string;
  field: string;
  header: string;
  sortable?: boolean;
  /** Server sort path when it differs from `field` (for example a display column sorted by `Owner.LastName`). */
  sortKey?: string;
  filterable?: boolean;
  hideable?: boolean;
  hiddenByDefault?: boolean;
  quickSearch?: boolean;
  /** Column shows an identifier: it is never picked as the card heading. */
  identifier?: boolean;
  /** How `LyoIdField` shortens an identifier column. */
  idAbbreviation?: LyoIdAbbreviation;
  /** Character count for `idAbbreviation`. */
  idAbbreviationLength?: number;
  /** Makes this column the card heading, overriding the automatic name-like pick. */
  cardTitle?: boolean;
  /** Renders the value with `LyoTimestamp` when no `cell` is supplied. */
  timestamp?: boolean;
  /** Timestamp shows the latest of several instants (see `LyoTimestamp` `latest`). */
  timestampLatest?: boolean;
  /** Age under which the timestamp reads as relative ("5 minutes ago"). */
  timestampRelativeWindowMs?: number;
  /** Truncates plain text cells with an ellipsis and a tooltip holding the full value. */
  maxDisplayLength?: number;
  /** Formats the value for plain text cells when no `cell` is supplied. */
  format?: (value: unknown, row: T) => string;
  align?: LyoColumnAlign;
  type?: FilterPropertyDefinition["type"];
  accessor?: (row: T) => unknown;
  /** Second argument is the related entity when `related` is set; otherwise undefined. */
  cell?: (row: T, related?: unknown) => ReactNode;
  related?: LyoRelatedLookup;
  size?: number;
  minSize?: number;
  maxSize?: number;
};

export function createLyoColumn<T>(col: LyoColumn<T>): LyoColumn<T> {
  return { sortable: true, filterable: true, hideable: true, ...col };
}

/**
 * Identifier column. With `related` it QueryProjects the related rows by `Keys` after the parent page loads and passes the match to `cell`.
 * Without a `cell` the grid renders the value with `LyoIdField` (copy button, navigation when `LyoIdNavigation` knows the route).
 */
export function createLyoIdColumn<T, TRes = unknown>(
  col: Omit<LyoColumn<T>, "cell"> & {
    cell?: (row: T, related: TRes | null) => ReactNode;
  }
): LyoColumn<T> {
  const { cell, ...rest } = col;
  return createLyoColumn({
    ...rest,
    identifier: true,
    cell: cell ? (row, related) => cell(row, (related as TRes | null | undefined) ?? null) : undefined,
  });
}

export type FilterState = {
  condition: ConditionClause;
  isEnabled: boolean;
};

export type SavedSort = {
  sortBy: string;
  descending: boolean;
  index: number;
};

export type LyoDataGridPersistedState = {
  searchText?: string | null;
  filterStates?: FilterState[];
  sorts?: SavedSort[];
  page?: number;
  pageSize?: number;
  hiddenColumnFields?: string[];
  selectedItemKeys?: unknown[][];
  columnSizing?: Record<string, number>;
  layout?: "Auto" | "Table" | "Cards";
};

export type LyoDataGridMode = "concrete" | "project" | "query";

export function projectedField(row: Record<string, unknown>, field: string): unknown {
  return projectedValue(row, field);
}

export function projectedId(row: Record<string, unknown>): string {
  return String(projectedField(row, "Id") ?? "");
}

export function projectedValue(row: unknown, field: string): unknown {
  if (row == null || typeof row !== "object") return undefined;
  const rec = row as Record<string, unknown>;
  if (field in rec) return rec[field];
  const camel = field.charAt(0).toLowerCase() + field.slice(1);
  if (camel in rec) return rec[camel];
  const aliased = `p.${field}`;
  if (aliased in rec) return rec[aliased];
  const parts = field.split(".");
  let cur: unknown = rec;
  for (const part of parts) {
    if (cur == null || typeof cur !== "object") return undefined;
    const obj = cur as Record<string, unknown>;
    cur = obj[part] ?? obj[part.charAt(0).toLowerCase() + part.slice(1)];
  }
  return cur;
}

export function keyEquals(a: unknown[], b: unknown[]): boolean {
  if (a.length !== b.length) return false;
  return a.every((v, i) => JSON.stringify(v) === JSON.stringify(b[i]));
}

export type { QueryConcreteReq, ProjectionQueryReq, SortBy };
