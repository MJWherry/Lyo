import { diffArrays } from "diff";

export type DiffOptions = { ignoreWhitespace?: boolean; ignoreCase?: boolean };

/** A run of lines (`value` keeps their newlines) that is unchanged, added, or removed; `count` is the line count. */
export type DiffPart = { value: string; count: number; added: boolean; removed: boolean };

const splitLines = (text: string) => text.split(/(?<=\n)/).filter((line) => line.length > 0);

/**
 * Line diff of two texts. `ignoreWhitespace` compares lines with every whitespace character removed and `ignoreCase` compares them upper-cased;
 * the parts keep the original text.
 */
export function diffText(original: string, modified: string, options: DiffOptions = {}): DiffPart[] {
  const normalize = (line: string) => {
    const value = options.ignoreWhitespace ? line.replace(/\s/g, "") : line.replace(/\r?\n$/, "");
    return options.ignoreCase ? value.toUpperCase() : value;
  };
  const parts = diffArrays(splitLines(original), splitLines(modified), { comparator: (left, right) => normalize(left) === normalize(right) });
  return parts.map((p) => ({ value: p.value.join(""), count: p.value.length, added: Boolean(p.added), removed: Boolean(p.removed) }));
}

export type DiffLineKind = "add" | "del" | null;

/** One displayed row: a 1-based line number, the text without its newline, and whether that row was added or removed. */
export type NumberedDiffLine = { number: number; text: string; kind: DiffLineKind };

/** Lines of a diff part. A trailing newline does not add an empty row; a blank line in the middle does. */
export function partLines(value: string): string[] {
  if (value.length === 0) return [];
  const body = value.endsWith("\n") ? value.slice(0, -1) : value;
  return body.split("\n");
}

/**
 * Rows for one pane. `left` drops additions, `right` drops removals, `unified` keeps both in diff order.
 * Numbers start at 1 for the rows that pane shows.
 */
export function paneLines(parts: readonly DiffPart[], side: "left" | "right" | "unified"): NumberedDiffLine[] {
  const lines: NumberedDiffLine[] = [];
  for (const part of parts) {
    if (side === "left" && part.added) continue;
    if (side === "right" && part.removed) continue;
    let kind: DiffLineKind = null;
    if (side !== "right" && part.removed) kind = "del";
    else if (side !== "left" && part.added) kind = "add";
    for (const text of partLines(part.value)) lines.push({ number: lines.length + 1, text, kind });
  }
  return lines;
}

export function diffStats(parts: readonly DiffPart[]): { added: number; removed: number } {
  let added = 0;
  let removed = 0;
  for (const p of parts) {
    if (p.added) added += p.count;
    else if (p.removed) removed += p.count;
  }
  return { added, removed };
}

export type SearchHit = { start: number; end: number };

/** Non-overlapping occurrences of `query`; case-insensitive unless `matchCase`. A blank query has no hits. */
export function searchHits(text: string, query: string, options: { matchCase?: boolean } = {}): SearchHit[] {
  if (!query.trim()) return [];
  const haystack = options.matchCase ? text : text.toLowerCase();
  const needle = options.matchCase ? query : query.toLowerCase();
  const hits: SearchHit[] = [];
  for (let i = haystack.indexOf(needle); i >= 0; i = haystack.indexOf(needle, i + needle.length)) hits.push({ start: i, end: i + needle.length });
  return hits;
}

export type NumberedHighlightSegment = { text: string; hit: number | null };

/** Splits `text` around search hits. `hit` numbers each match from `hitOffset`, so several panes can share one running index. */
export function highlightSegments(text: string, query: string, options: { matchCase?: boolean; hitOffset?: number } = {}): NumberedHighlightSegment[] {
  const hits = searchHits(text, query, options);
  if (hits.length === 0) return [{ text, hit: null }];
  const segments: NumberedHighlightSegment[] = [];
  let cursor = 0;
  hits.forEach((h, i) => {
    if (h.start > cursor) segments.push({ text: text.slice(cursor, h.start), hit: null });
    segments.push({ text: text.slice(h.start, h.end), hit: (options.hitOffset ?? 0) + i });
    cursor = h.end;
  });
  if (cursor < text.length) segments.push({ text: text.slice(cursor), hit: null });
  return segments;
}

/** Next or previous hit index with wrap-around; -1 when there are no hits. */
export function stepHit(current: number, total: number, direction: 1 | -1): number {
  if (total <= 0) return -1;
  if (current < 0) return direction === 1 ? 0 : total - 1;
  return (current + direction + total) % total;
}
