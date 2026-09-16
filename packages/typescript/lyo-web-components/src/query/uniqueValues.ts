import type { SpUniqueValueCount } from "lyo-query";

/** Selection key for a unique value: null values select as `""`. */
export const uniqueValueKey = (item: SpUniqueValueCount) => item.value ?? "";

const defaultLabel = (value: string | null) => value || "(null)";

/** Most frequent first, filtered case-insensitively on the displayed label. */
export function filterUniqueValues(
  items: readonly SpUniqueValueCount[],
  query: string,
  format: (value: string | null) => string = defaultLabel
): SpUniqueValueCount[] {
  const lower = query.trim().toLowerCase();
  const sorted = [...items].sort((a, b) => b.count - a.count);
  return lower ? sorted.filter((i) => format(i.value).toLowerCase().includes(lower)) : sorted;
}

export function selectAllVisible(selected: readonly string[], visible: readonly SpUniqueValueCount[]): string[] {
  const next = [...selected];
  for (const key of visible.map(uniqueValueKey)) if (!next.includes(key)) next.push(key);
  return next;
}

export function clearVisible(selected: readonly string[], visible: readonly SpUniqueValueCount[]): string[] {
  const hidden = new Set(visible.map(uniqueValueKey));
  return selected.filter((key) => !hidden.has(key));
}
