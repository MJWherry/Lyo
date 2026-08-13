import type { ProjectionQueryReq } from "lyo-query";

export type { ProjectionQueryReq };

export const SERIES_CARD_SELECT = [
  "Id",
  "Title",
  "Slug",
  "ComicType",
  "Status",
  "Author",
  "CoverImageRef",
  "UpdatedTimestamp",
  "Language",
  "Chapters.Count",
  "Volumes.Count",
] as const;

export const VOLUME_CARD_SELECT = [
  "Id",
  "Title",
  "VolumeNumber",
  "CoverImageRef",
  "SeriesId",
  "Series.Title",
  "Series.Slug",
  "UpdatedTimestamp",
  "Chapters.Count",
] as const;

export const CHAPTER_CARD_SELECT = [
  "Id",
  "Title",
  "ChapterNumber",
  "Language",
  "CoverImageRef",
  "SeriesId",
  "Series.Title",
  "Series.Slug",
  "PublishedDate",
  "UpdatedTimestamp",
] as const;

export const SERIES_WHERE_PRESETS = [
  "Title",
  "Slug",
  "Author",
  "Artist",
  "Language",
  "ComicType",
  "Status",
  "PublishedYear",
  "Demographic",
  "AlternateTitles.Title",
  "Chapters.Title",
  "Chapters.ChapterNumber",
  "Volumes.VolumeNumber",
] as const;

export const VOLUME_WHERE_PRESETS = [
  "Title",
  "VolumeNumber",
  "Series.Title",
  "Chapters.ChapterNumber",
] as const;

export const CHAPTER_WHERE_PRESETS = [
  "Title",
  "ChapterNumber",
  "Language",
  "Series.Title",
  "Volume.VolumeNumber",
  "PublishedDate",
] as const;

export type ComicQueryScope = "series" | "volumes" | "chapters";

export function comicQueryProjectRoute(scope: ComicQueryScope): string {
  if (scope === "volumes") return "/api/comic/volumes";
  if (scope === "chapters") return "/api/comic/chapters";
  return "/api/comic/series";
}

function toCamelKey(key: string): string {
  return key
    .split(".")
    .map((part) => (part.length ? part.charAt(0).toLowerCase() + part.slice(1) : part))
    .join(".");
}

/**
 * QueryProject dictionaries keep CLR property names (`Id`, `CoverImageRef`, `Series.Slug`).
 * Map them to camelCase (+ nested `series.slug`) for the Next UI.
 */
export function normalizeComicCardRow(raw: unknown): ComicCardRow {
  if (!raw || typeof raw !== "object") return {};
  const src = raw as Record<string, unknown>;
  const out: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(src)) {
    const camel = toCamelKey(key);
    out[camel] = value;
    if (!camel.includes(".")) continue;
    const parts = camel.split(".");
    let cur = out;
    for (let i = 0; i < parts.length - 1; i++) {
      const part = parts[i];
      const next = cur[part];
      if (!next || typeof next !== "object" || Array.isArray(next)) cur[part] = {};
      cur = cur[part] as Record<string, unknown>;
    }
    cur[parts[parts.length - 1]] = value;
  }
  const chapterCount = readCount(out, "chapterCount", "chapters.count", "Chapters.Count");
  const volumeCount = readCount(out, "volumeCount", "volumes.count", "Volumes.Count");
  if (chapterCount != null) out.chapterCount = chapterCount;
  if (volumeCount != null) out.volumeCount = volumeCount;
  return out as ComicCardRow;
}

function readCount(row: Record<string, unknown>, ...keys: string[]): number | null {
  for (const key of keys) {
    const direct = asCount(row[key]);
    if (direct != null) return direct;
    if (!key.includes(".")) continue;
    const [parent, child] = key.split(".");
    const nested = row[parent];
    if (nested && typeof nested === "object") {
      const n = asCount((nested as Record<string, unknown>)[child]);
      if (n != null) return n;
    }
  }
  return null;
}

function asCount(value: unknown): number | null {
  if (typeof value === "number" && Number.isFinite(value)) return value;
  if (typeof value === "string" && value.trim() !== "") {
    const n = Number(value);
    if (Number.isFinite(n)) return n;
  }
  return null;
}

export function normalizeComicCardRows(items: unknown[] | null | undefined): ComicCardRow[] {
  return (items ?? []).map(normalizeComicCardRow);
}

export function comicCardRowKey(row: ComicCardRow, index: number): string {
  return String(row.id ?? row.slug ?? `row-${index}`);
}

/** Projected card row from QueryProject (camelCase after {@link normalizeComicCardRow}). Nested Series.* flatten as `series.title` / `series.slug`. */
export interface ComicCardRow {
  id?: string;
  title?: string | null;
  slug?: string | null;
  comicType?: string | number | null;
  status?: string | number | null;
  author?: string | null;
  language?: string | null;
  coverImageRef?: string | null;
  updatedTimestamp?: string | null;
  volumeNumber?: number | null;
  chapterNumber?: number | null;
  seriesId?: string | null;
  publishedDate?: string | null;
  chapterCount?: number | null;
  volumeCount?: number | null;
  series?: {
    title?: string | null;
    slug?: string | null;
  } | null;
  [key: string]: unknown;
}
