/** Keys and hover text for a parameter-count cell. Keys win; a count is the fallback when the row has none. */
export function parameterHoverSummary(source: unknown): { keys: string[]; text: string } {
  const keys = collectKeys(source);
  if (keys.length > 0) return { keys, text: keys.join(", ") };
  const count = readCount(source);
  return { keys: [], text: count == null ? "0" : String(count) };
}

function collectKeys(source: unknown): string[] {
  if (Array.isArray(source)) {
    const keys: string[] = [];
    for (const item of source) {
      if (typeof item === "string" && item.trim()) keys.push(item.trim());
      else if (item && typeof item === "object") {
        const rec = item as Record<string, unknown>;
        const key = rec.Key ?? rec.key;
        if (typeof key === "string" && key.trim()) keys.push(key.trim());
      }
    }
    return keys;
  }
  if (!source || typeof source !== "object") return [];
  for (const value of Object.values(source as Record<string, unknown>)) {
    if (!Array.isArray(value)) continue;
    const keys = collectKeys(value);
    if (keys.length > 0) return keys;
  }
  return [];
}

function readCount(source: unknown): number | null {
  if (typeof source === "number" && Number.isFinite(source)) return source;
  if (!source || typeof source !== "object") return null;
  for (const [name, value] of Object.entries(source as Record<string, unknown>)) {
    if (!name.endsWith("Count") && name !== "count") continue;
    const count = typeof value === "number" ? value : Number(value);
    if (Number.isFinite(count)) return count;
  }
  return null;
}
