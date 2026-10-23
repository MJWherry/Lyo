import { comparisonDescription, conditionEquals, thresholdSuffix, type ConditionClause, type FilterPropertyDefinition } from "lyo-query";
import type { FilterState } from "../data-grid/types.js";

const MAX_COMPACT_LIST_ITEMS = 4;

export const FUZZY_THRESHOLD_HELP = "0–1, default 0.3";

/** Chip value text. Compact lists show four items then `... (+N more)`; `compact = false` lists everything (detail dialogs). */
export function formatFilterValue(value: unknown, compact = true): string {
  if (value == null) return "null";
  if (!Array.isArray(value)) return typeof value === "object" ? JSON.stringify(value) : String(value);
  const items = value.map((v) => (v == null ? "" : String(v))).filter((s) => s !== "");
  if (items.length === 0) return "[]";
  if (!compact || items.length <= MAX_COMPACT_LIST_ITEMS) return `[${items.join(", ")}]`;
  return `[${items.slice(0, MAX_COMPACT_LIST_ITEMS).join(", ")}, ... (+${items.length - MAX_COMPACT_LIST_ITEMS} more)]`;
}

/** `{display name} {operator} {value}` plus ` threshold=n` for fuzzy filters. */
export function filterChipText(condition: ConditionClause, definitions: readonly FilterPropertyDefinition[], compact = true): string {
  const displayName = definitions.find((d) => d.propertyName === condition.Field)?.displayName || condition.Field;
  return `${displayName} ${comparisonDescription(condition.Comparison)} ${formatFilterValue(condition.Value, compact)}${thresholdSuffix(condition)}`;
}

/** Adds an enabled filter unless an equal condition (threshold included) is already present; returns the same array when skipped. */
export function appendFilterUnique(states: FilterState[], condition: ConditionClause): FilterState[] {
  if (states.some((s) => conditionEquals(s.condition, condition))) return states;
  return [...states, { condition, isEnabled: true }];
}
