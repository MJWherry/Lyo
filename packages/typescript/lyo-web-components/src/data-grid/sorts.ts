import type { SortBy } from "lyo-query";

/**
 * Multi-column sort: a new field is appended; the same field cycles Asc → Desc → remove.
 * Priorities are 0-based and compacted after a remove.
 */
export function nextSorts(current: readonly SortBy[], field: string): SortBy[] {
  const index = current.findIndex((s) => s.PropertyName === field);
  if (index < 0) {
    return [
      ...current.map((s, i) => ({ ...s, Priority: i })),
      { PropertyName: field, Direction: "Asc", Priority: current.length },
    ];
  }
  const existing = current[index];
  if (existing.Direction === "Asc") {
    return current.map((s, i) => (i === index ? { ...s, Direction: "Desc", Priority: i } : { ...s, Priority: i }));
  }
  return current.filter((_, i) => i !== index).map((s, i) => ({ ...s, Priority: i }));
}
