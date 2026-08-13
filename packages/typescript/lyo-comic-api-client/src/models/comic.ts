/** Wire JSON is camelCase (`LyoJsonSerializerOptions`). Enums are camelCase strings. */

export const ComicType = {
  unknown: 0,
  manga: 1,
  manhwa: 2,
  manhua: 3,
  webtoon: 4,
  western: 5,
} as const;

export type ComicTypeName = keyof typeof ComicType;
export type ComicTypeValue = ComicTypeName | number;

export const ComicStatus = {
  unknown: 0,
  ongoing: 1,
  completed: 2,
  hiatus: 3,
  cancelled: 4,
} as const;

export type ComicStatusName = keyof typeof ComicStatus;
export type ComicStatusValue = ComicStatusName | number;

export interface ComicAlternateTitleRes {
  id: string;
  title: string;
  language?: string | null;
}

export interface ComicAlternateTitleReq {
  title: string;
  language?: string | null;
}

export interface AddTagReq {
  name: string;
  tagType?: string;
  slug?: string | null;
}

export interface ComicSeriesRes {
  id: string;
  title: string;
  slug: string;
  comicType: ComicTypeValue;
  status: ComicStatusValue;
  description?: string | null;
  language?: string | null;
  publishedYear?: number | null;
  author?: string | null;
  artist?: string | null;
  publisher?: string | null;
  source?: string | null;
  coverImageRef?: string | null;
  coverImageUrl?: string | null;
  demographic?: string | null;
  createdTimestamp: string;
  updatedTimestamp?: string | null;
  alternateTitles?: ComicAlternateTitleRes[] | null;
  tags?: string[] | null;
  averageRating?: number | null;
  ratingCount?: number;
  commentCount?: number;
  favoriteCount?: number;
  isFavorited?: boolean | null;
}

export interface ComicSeriesReq {
  title: string;
  slug: string;
  comicType?: ComicTypeValue;
  status?: ComicStatusValue;
  description?: string | null;
  language?: string | null;
  publishedYear?: number | null;
  author?: string | null;
  artist?: string | null;
  publisher?: string | null;
  source?: string | null;
  coverImageRef?: string | null;
  demographic?: string | null;
  alternateTitles?: ComicAlternateTitleReq[];
  tags?: AddTagReq[] | null;
}

export interface ComicVolumeRes {
  id: string;
  seriesId: string;
  volumeNumber?: number | null;
  title?: string | null;
  coverImageRef?: string | null;
  coverImageUrl?: string | null;
  publishedDate?: string | null;
  createdTimestamp: string;
  updatedTimestamp?: string | null;
  tags?: string[] | null;
  averageRating?: number | null;
  ratingCount?: number;
  commentCount?: number;
  favoriteCount?: number;
  isFavorited?: boolean | null;
}

export interface ComicVolumeReq {
  seriesId: string;
  volumeNumber?: number | null;
  title?: string | null;
  coverImageRef?: string | null;
  publishedDate?: string | null;
}

/** Enriched chapter (`GET /volumes/{id}/chapters`, `GET /chapters/{id}`). */
export interface ComicChapterRes {
  id: string;
  seriesId: string;
  volumeId?: string | null;
  chapterNumber: number;
  title?: string | null;
  language: string;
  pageCount?: number | null;
  publishedDate?: string | null;
  source?: string | null;
  coverImageRef?: string | null;
  coverImageUrl?: string | null;
  createdTimestamp: string;
  updatedTimestamp?: string | null;
  tags?: string[] | null;
  averageRating?: number | null;
  ratingCount?: number;
  commentCount?: number;
  favoriteCount?: number;
  isFavorited?: boolean | null;
}

/**
 * Raw chapter from `GET /series/{id}/chapters` — not enriched, no coverImageUrl.
 */
export interface ComicChapter {
  id: string;
  seriesId: string;
  volumeId?: string | null;
  chapterNumber: number;
  title?: string | null;
  language: string;
  pageCount?: number | null;
  publishedDate?: string | null;
  source?: string | null;
  coverImageRef?: string | null;
  createdTimestamp: string;
  updatedTimestamp?: string | null;
}

export interface ComicChapterReq {
  seriesId: string;
  volumeId?: string | null;
  chapterNumber: number;
  title?: string | null;
  language: string;
  pageCount?: number | null;
  publishedDate?: string | null;
  source?: string | null;
  coverImageRef?: string | null;
}

/** Enriched page (`GET /pages/{id}`). */
export interface ComicPageRes {
  id: string;
  chapterId: string;
  pageNumber: number;
  imageRef?: string | null;
  imageUrl?: string | null;
  width?: number | null;
  height?: number | null;
  createdTimestamp: string;
  updatedTimestamp?: string | null;
}

/**
 * Raw page from `GET /chapters/{id}/pages` — `imageRef` only, no `imageUrl`.
 * Resolve images as `/api/files/{imageRef}` on the Next origin.
 */
export interface ComicPage {
  id: string;
  chapterId: string;
  pageNumber: number;
  imageRef?: string | null;
  width?: number | null;
  height?: number | null;
  createdTimestamp: string;
  updatedTimestamp?: string | null;
}

export interface ComicPageReq {
  chapterId: string;
  pageNumber: number;
  imageRef?: string | null;
  width?: number | null;
  height?: number | null;
}

export interface ComicSeriesQuery {
  titleContains?: string | null;
  comicType?: ComicTypeValue | null;
  status?: ComicStatusValue | null;
  language?: string | null;
  tags?: string[] | null;
  filterSeriesIds?: string[] | null;
  limit?: number | null;
  skip?: number;
}

export interface FileUploadResult {
  id: string;
}

/**
 * Display URL for a stored file id or an already-absolute cover/page ref.
 * GUIDs go through the Next BFF (`/api/files/{id}`); `http(s):` refs (seeded picsum, etc.) pass through.
 */
export function comicFileUrl(fileId: string | null | undefined, cacheBust?: string | number | null): string | null {
  if (!fileId) return null;
  const trimmed = fileId.trim();
  if (!trimmed) return null;
  if (/^https?:\/\//i.test(trimmed)) return trimmed;
  const path = `/api/files/${encodeURIComponent(trimmed)}`;
  if (cacheBust == null || String(cacheBust) === "") return path;
  return `${path}?v=${encodeURIComponent(String(cacheBust))}`;
}

export function comicTypeLabel(value: ComicTypeValue | null | undefined): string {
  if (value == null) return "";
  if (typeof value === "number") {
    const entry = Object.entries(ComicType).find(([, n]) => n === value);
    return entry ? capitalize(entry[0]) : "";
  }
  return capitalize(String(value));
}

export function comicStatusLabel(value: ComicStatusValue | null | undefined): string {
  if (value == null) return "";
  if (typeof value === "number") {
    const entry = Object.entries(ComicStatus).find(([, n]) => n === value);
    return entry ? capitalize(entry[0]) : "";
  }
  return capitalize(String(value));
}

function capitalize(s: string): string {
  if (!s) return s;
  return s.charAt(0).toUpperCase() + s.slice(1);
}

export function isVerticalDefault(comicType: ComicTypeValue | null | undefined): boolean {
  const name = typeof comicType === "number"
    ? Object.entries(ComicType).find(([, n]) => n === comicType)?.[0]
    : String(comicType).toLowerCase();
  return name === "webtoon" || name === "manhwa";
}
