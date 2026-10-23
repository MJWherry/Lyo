import type { ProjectionQueryReq } from "lyo-query";
import type { HeatmapRow } from "./heatmap.js";

/** Enough for every state and territory. */
export const HEATMAP_STATE_QUERY_AMOUNT = 200;

/** Texas has 254 counties, so the state query asks for more than the national page. */
export const HEATMAP_COUNTY_QUERY_AMOUNT = 500;

const STATE_FIELD = "State";
const COUNTY_FIELD = "County";
const COUNT_NAME = "Count";
const COUNT_FIELD = "Id";

/**
 * Projection that groups rows by state, or by county inside one state.
 * `statePostal` adds `State = that code` and includes `County` in the group key.
 * Property names match the query client. The API binds them case-insensitively.
 */
export function heatmapCountQuery(statePostal?: string | null): ProjectionQueryReq {
  const state = statePostal?.trim();
  const query: ProjectionQueryReq = {
    Options: {
      ZipSiblingCollectionSelections: true,
      TotalCountMode: "None",
      IncludeFilterMode: "Full",
    },
    Select: state ? [STATE_FIELD, COUNTY_FIELD] : [STATE_FIELD],
    Aggregates: [{ Function: "Count", Name: COUNT_NAME, Field: COUNT_FIELD }],
    Start: 0,
    Amount: state ? HEATMAP_COUNTY_QUERY_AMOUNT : HEATMAP_STATE_QUERY_AMOUNT,
    SortBy: [{ PropertyName: COUNT_NAME, Direction: "Desc", Priority: 0 }],
  };
  if (state) query.whereClause = { $type: "condition", Field: STATE_FIELD, Comparison: "Equals", Value: state };
  return query;
}

/** Maps `{ State, Count }` and `{ State, County, Count }` rows onto heatmap rows. `Count` is the value, not a missing 1. */
export function heatmapRowsFromCounts(items: readonly Record<string, unknown>[]): HeatmapRow[] {
  const rows: HeatmapRow[] = [];
  for (const item of items) {
    const state = textField(item, STATE_FIELD);
    const count = numberField(item, COUNT_NAME);
    if (!state || count == null) continue;
    const county = textField(item, COUNTY_FIELD);
    const row: HeatmapRow = { state, value: count };
    if (county) row.county = county;
    rows.push(row);
  }
  return rows;
}

function textField(item: Record<string, unknown>, name: string): string | undefined {
  const value = readField(item, name);
  if (typeof value !== "string") return undefined;
  const text = value.trim();
  return text || undefined;
}

function numberField(item: Record<string, unknown>, name: string): number | undefined {
  const value = readField(item, name);
  if (typeof value === "number" && Number.isFinite(value)) return value;
  if (typeof value === "string" && value.trim() !== "" && Number.isFinite(Number(value))) return Number(value);
  return undefined;
}

function readField(item: Record<string, unknown>, name: string): unknown {
  if (Object.prototype.hasOwnProperty.call(item, name)) return item[name];
  const lower = name.toLowerCase();
  const key = Object.keys(item).find((candidate) => candidate.toLowerCase() === lower);
  return key === undefined ? undefined : item[key];
}
