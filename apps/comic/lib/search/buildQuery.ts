import type { ConditionClause, WhereClause } from "lyo-query";
import {
  CHAPTER_CARD_SELECT,
  SERIES_CARD_SELECT,
  VOLUME_CARD_SELECT,
  type ComicQueryScope,
  type ProjectionQueryReq,
} from "lyo-comic-api-client";

export type SimpleSearch = {
  title?: string;
  type?: string;
  status?: string;
  language?: string;
  author?: string;
  year?: string | number;
  tags?: string[];
};

export type SearchBody = {
  scope?: ComicQueryScope;
  start?: number;
  amount?: number;
  simple?: SimpleSearch;
  whereClause?: WhereClause | null;
};

function condition(field: string, comparison: ConditionClause["Comparison"], value: unknown): ConditionClause {
  return { $type: "condition", Field: field, Comparison: comparison, Value: value };
}

export function simpleToWhere(simple: SimpleSearch | undefined, scope: ComicQueryScope): WhereClause[] {
  if (!simple) return [];
  const clauses: WhereClause[] = [];
  const title = simple.title?.trim();
  if (title) {
    const field = scope === "volumes" ? "Series.Title" : "Title";
    clauses.push(condition(field, "Contains", title));
  }
  if (simple.type && scope === "series") clauses.push(condition("ComicType", "Equals", simple.type));
  if (simple.status && scope === "series") clauses.push(condition("Status", "Equals", simple.status));
  if (simple.language) {
    const field = scope === "volumes" ? "Series.Language" : "Language";
    clauses.push(condition(field, "Equals", simple.language));
  }
  if (simple.author?.trim() && scope === "series") {
    clauses.push(condition("Author", "Contains", simple.author.trim()));
  }
  const year = typeof simple.year === "number" ? simple.year : Number(simple.year);
  if (Number.isFinite(year) && year > 0 && scope === "series") {
    clauses.push(condition("PublishedYear", "Equals", year));
  }
  return clauses;
}

export function mergeWhere(parts: WhereClause[]): WhereClause | null {
  const filtered = parts.filter(Boolean);
  if (filtered.length === 0) return null;
  if (filtered.length === 1) return filtered[0];
  return { $type: "group", Operator: "And", Children: filtered };
}

export function selectForScope(scope: ComicQueryScope): string[] {
  if (scope === "volumes") return [...VOLUME_CARD_SELECT];
  if (scope === "chapters") return [...CHAPTER_CARD_SELECT];
  return [...SERIES_CARD_SELECT];
}

export function buildProjectionQuery(
  scope: ComicQueryScope,
  simple: SimpleSearch | undefined,
  advanced: WhereClause | null | undefined,
  start: number,
  amount: number,
  keys?: unknown[][]
): ProjectionQueryReq {
  const whereClause = mergeWhere([...simpleToWhere(simple, scope), ...(advanced ? [advanced] : [])]);
  return {
    Options: { TotalCountMode: "Exact", IncludeFilterMode: "Full" },
    Start: start,
    Amount: amount,
    Select: selectForScope(scope),
    SortBy: [{ PropertyName: "UpdatedTimestamp", Direction: "Desc", Priority: 0 }],
    whereClause,
    Keys: keys,
  };
}

export function normalizeScope(raw: unknown): ComicQueryScope {
  if (raw === "volumes" || raw === "volume") return "volumes";
  if (raw === "chapters" || raw === "chapter") return "chapters";
  return "series";
}
