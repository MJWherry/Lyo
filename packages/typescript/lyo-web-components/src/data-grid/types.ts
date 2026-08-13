import type {
  ConditionClause,
  FilterPropertyDefinition,
  ProjectionQueryReq,
  QueryConcreteReq,
  SortBy,
} from "lyo-query";
import type { ReactNode } from "react";

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

export type LyoColumn<T> = {
  id: string;
  field: string;
  header: string;
  sortable?: boolean;
  filterable?: boolean;
  hideable?: boolean;
  hiddenByDefault?: boolean;
  quickSearch?: boolean;
  type?: FilterPropertyDefinition["type"];
  accessor?: (row: T) => unknown;
  cell?: (row: T) => ReactNode;
  size?: number;
  minSize?: number;
  maxSize?: number;
};

export function createLyoColumn<T>(col: LyoColumn<T>): LyoColumn<T> {
  return { sortable: true, filterable: true, hideable: true, ...col };
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
};

export type LyoDataGridMode = "concrete" | "project" | "query";

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
