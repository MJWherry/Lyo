import type { ComicCardRow } from "lyo-comic-api-client";
import { comicFileUrl } from "lyo-comic-api-client";

function nested(row: ComicCardRow, path: string): unknown {
  const dotted = row[path];
  if (dotted != null) return dotted;
  const parts = path.split(".");
  let cur: unknown = row;
  for (const p of parts) {
    if (!cur || typeof cur !== "object") return undefined;
    cur = (cur as Record<string, unknown>)[p] ?? (cur as Record<string, unknown>)[p.charAt(0).toLowerCase() + p.slice(1)];
  }
  return cur;
}

export function cardSlug(row: ComicCardRow): string | null {
  const slug = row.slug ?? nested(row, "series.slug") ?? nested(row, "Series.Slug");
  return typeof slug === "string" && slug ? slug : null;
}

export function cardTitle(row: ComicCardRow): string {
  const seriesTitle = nested(row, "series.title") ?? nested(row, "Series.Title");
  if (row.chapterNumber != null) {
    const ch = `Ch. ${row.chapterNumber}`;
    const t = row.title ? `${ch} · ${row.title}` : ch;
    return typeof seriesTitle === "string" && seriesTitle ? `${seriesTitle} — ${t}` : t;
  }
  if (row.volumeNumber != null) {
    const vol = `Vol. ${row.volumeNumber}`;
    const t = row.title ? `${vol} · ${row.title}` : vol;
    return typeof seriesTitle === "string" && seriesTitle ? `${seriesTitle} — ${t}` : t;
  }
  return row.title || (typeof seriesTitle === "string" ? seriesTitle : "Untitled");
}

export function cardCoverSrc(row: ComicCardRow): string | null {
  const ref = row.coverImageRef ?? (typeof row.CoverImageRef === "string" ? row.CoverImageRef : null);
  const bust = row.updatedTimestamp ?? (typeof row.UpdatedTimestamp === "string" ? row.UpdatedTimestamp : null);
  return comicFileUrl(ref ?? null, bust);
}

export function readHref(seriesId: string, chapterId?: string | null, page?: number): string {
  const q = new URLSearchParams();
  if (chapterId) q.set("chapter", chapterId);
  const pageNum = page != null ? Math.floor(Number(page)) : 1;
  if (Number.isFinite(pageNum) && pageNum >= 1 && pageNum < Number.MAX_SAFE_INTEGER / 2) {
    q.set("page", String(pageNum));
  }
  const qs = q.toString();
  return `/read/${encodeURIComponent(seriesId)}${qs ? `?${qs}` : ""}`;
}

export function cardHref(row: ComicCardRow): string {
  const slug = cardSlug(row);
  if (row.chapterNumber != null && row.id) {
    const seriesId = typeof row.seriesId === "string" && row.seriesId ? row.seriesId : null;
    if (seriesId) return readHref(seriesId, row.id);
  }
  if (!slug) return "/search";
  if (row.volumeNumber != null && row.id) return `/manga/${encodeURIComponent(slug)}/volume/${encodeURIComponent(row.id)}`;
  return `/manga/${encodeURIComponent(slug)}`;
}
