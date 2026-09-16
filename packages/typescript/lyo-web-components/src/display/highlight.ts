export type HighlightSegment = { text: string; match: boolean };

function escapeRegex(text: string): string {
  return text.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

/** Splits `text` into match / non-match segments for `query`, case-insensitively, keeping the original casing. */
export function splitHighlight(text: string | null | undefined, query: string | null | undefined): HighlightSegment[] {
  if (!text) return [];
  const q = query?.trim();
  if (!q) return [{ text, match: false }];
  return text
    .split(new RegExp(`(${escapeRegex(q)})`, "gi"))
    .filter((part) => part.length > 0)
    .map((part) => ({ text: part, match: part.toLowerCase() === q.toLowerCase() }));
}

/** Ellipsis past `maxLength`. */
export function truncateText(text: string | null | undefined, maxLength: number): { text: string; truncated: boolean } {
  if (!text) return { text: "", truncated: false };
  return text.length > maxLength ? { text: `${text.slice(0, maxLength)}…`, truncated: true } : { text, truncated: false };
}
