import type { FilterPropertyDefinition, FilterPropertyType } from "lyo-query";
import { LyoTypes } from "../types/lyoTypeCatalog.js";

/** Base route handed to `client.bulkPatch` (which appends `/Bulk`). Falls back to the grid route. */
export function bulkPatchRoute(gridRoute: string, patchRoute?: string | null): string {
  const route = (patchRoute?.trim() || gridRoute).replace(/\/+$/, "");
  return route.replace(/\/Bulk$/i, "");
}

/** `{ [property]: value }` from the typed editor's JSON; blank clears the property. */
export function bulkPatchData(property: string, json: string | null | undefined): Record<string, unknown> {
  return { [property]: json?.trim() ? (JSON.parse(json) as unknown) : null };
}

const TYPE_NAMES: Record<FilterPropertyType, string> = {
  String: LyoTypes.String.fullName,
  Number: LyoTypes.Decimal.fullName,
  DateTime: LyoTypes.DateTime.fullName,
  DateOnly: LyoTypes.DateOnly.fullName,
  TimeOnly: LyoTypes.TimeOnly.fullName,
  Enum: LyoTypes.Enum.fullName,
  Bool: LyoTypes.Bool.fullName,
};

/** LyoType full name the value editor uses for a filter property. */
export function lyoTypeForProperty(definition: Pick<FilterPropertyDefinition, "type">): string {
  return TYPE_NAMES[definition.type ?? "String"];
}

/** Snackbar text for a bulk patch. Uses `updatedCount` / `failedCount` when the API returns them, otherwise the number of keys sent. */
export function bulkPatchSummary(result: unknown, keyCount: number): { message: string; severity: "success" | "warning" } {
  const counts = result as { updatedCount?: unknown; failedCount?: unknown } | null;
  const updated = typeof counts?.updatedCount === "number" ? counts.updatedCount : keyCount;
  const failed = typeof counts?.failedCount === "number" ? counts.failedCount : 0;
  const rows = `Patched ${updated} ${updated === 1 ? "row" : "rows"}`;
  return failed > 0 ? { message: `${rows}, ${failed} failed`, severity: "warning" } : { message: rows, severity: "success" };
}
