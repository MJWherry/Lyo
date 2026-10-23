import type { LyoColumn } from "./types.js";

/** Largest selection or page run the grid exports or patches in one request. */
export const DEFAULT_MAX_BULK_SIZE = 2000;

export type LyoDataGridLayout = "Auto" | "Table" | "Cards";

/** Column labels that make a good card heading, most specific first. Matched against the header, then the field leaf. */
const PREFERRED_TITLE_LABELS = ["name", "key", "title", "subject", "label", "displayname", "fullname", "fulladdress", "filename", "email"];

const leaf = (field: string) => field.slice(field.lastIndexOf(".") + 1);
const normalize = (value: string) => value.replace(/[^\p{L}\p{N}]/gu, "").toLowerCase();

/** True when the last dotted segment is `Id` (for example `Id`, `JobDefinition.Id`). */
export function isIdField(field: string | null | undefined): boolean {
  const name = field?.trim();
  return Boolean(name) && leaf(name!).toLowerCase() === "id";
}

function distinctIgnoreCase(values: Iterable<string>): string[] {
  const seen = new Set<string>();
  const out: string[] = [];
  for (const value of values) {
    const trimmed = value.trim();
    if (!trimmed || seen.has(trimmed.toLowerCase())) continue;
    seen.add(trimmed.toLowerCase());
    out.push(trimmed);
  }
  return out;
}

/**
 * Property paths OR-ed into quick search: `explicit` when non-empty, otherwise `quickSearch` columns.
 * Leaf `Id` fields are always unioned so pasting an identifier finds its row without per-grid wiring.
 */
export function quickSearchProperties<T>(columns: readonly LyoColumn<T>[], explicit?: readonly string[]): string[] {
  const named = explicit?.length ? explicit : columns.filter((c) => c.quickSearch).map((c) => c.field);
  return distinctIgnoreCase([...named, ...columns.filter((c) => isIdField(c.field)).map((c) => c.field)]);
}

/** For example `Search Name, Type, Id`; `Search...` when no properties are known. */
export function quickSearchPlaceholder<T>(columns: readonly LyoColumn<T>[], explicit?: readonly string[]): string {
  const labels = quickSearchProperties(columns, explicit).map((property) => {
    const header = columns.find((c) => c.field.toLowerCase() === property.toLowerCase())?.header?.trim();
    return header || leaf(property);
  });
  const distinct = distinctIgnoreCase(labels);
  return distinct.length === 0 ? "Search..." : `Search ${distinct.join(", ")}`;
}

export type LyoCardColumn<T> = { column: LyoColumn<T>; isTitle: boolean };

/** Visible columns in declaration order. At most one is the heading: an explicit `cardTitle`, else the first name-like non-identifier column. */
export function cardColumns<T>(columns: readonly LyoColumn<T>[], hiddenFields: ReadonlySet<string>): LyoCardColumn<T>[] {
  const visible = columns.filter((c) => c.field.trim() && !hiddenFields.has(c.field));
  const title =
    visible.find((c) => c.cardTitle) ??
    PREFERRED_TITLE_LABELS.map((label) => visible.find((c) => !c.identifier && normalize(c.header || leaf(c.field)) === label)).find(Boolean);
  return visible.map((column) => ({ column, isTitle: column === title }));
}

/** Whether rows draw as cards. `Auto` follows the viewport, so it flips as the window is resized. */
export function resolveCardMode(layout: LyoDataGridLayout, belowCardBreakpoint: boolean): boolean {
  if (layout === "Cards") return true;
  if (layout === "Table") return false;
  return belowCardBreakpoint;
}

export type LyoBulkState = { loading: boolean; selectedCount: number; total: number; maxBulkSize: number };

const n0 = (value: number) => value.toLocaleString("en-US");

/** Toolbar suffix describing what a bulk action would touch. */
export function bulkLabel({ loading, selectedCount, total, maxBulkSize }: LyoBulkState): string {
  if (loading) return "";
  if (selectedCount > 0) return selectedCount <= maxBulkSize ? `(${n0(selectedCount)} items)` : "(too many items)";
  if (total <= 0) return "(no items)";
  return total <= maxBulkSize ? `(${n0(total)} items)` : `(first ${n0(maxBulkSize)} of ${n0(total)})`;
}

/** A selection over the cap is the only hard block; unselected exports page at the cap. */
export function canExport({ loading, selectedCount, total, maxBulkSize }: LyoBulkState): boolean {
  if (loading) return false;
  if (selectedCount > 0) return selectedCount <= maxBulkSize;
  return total > 0;
}

/** True when `next` asks for a field the last request did not load. Hiding a column never needs a reload. */
export function selectGrew(previous: readonly string[] | null | undefined, next: readonly string[]): boolean {
  const old = new Set((previous ?? []).map((f) => f.trim().toLowerCase()).filter(Boolean));
  return next.some((f) => {
    const key = f.trim().toLowerCase();
    return Boolean(key) && !old.has(key);
  });
}
