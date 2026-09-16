/** Lowercases and replaces runs of non-alphanumeric characters with one hyphen. */
export function slugTab(text: string | null | undefined): string {
  if (!text) return "";
  return text
    .trim()
    .toLowerCase()
    .split(/[^a-z0-9]+/)
    .filter(Boolean)
    .join("-");
}

/** Active tab: query string, then stored preference, then `defaultTab`, then the first tab. Unknown ids are skipped. */
export function resolveActiveTab(
  ids: readonly string[],
  sources: { query?: string | null; stored?: string | null; defaultTab?: string | null }
): string | null {
  for (const candidate of [sources.query, sources.stored, sources.defaultTab]) {
    if (candidate && ids.includes(candidate)) return candidate;
  }
  return ids[0] ?? null;
}
