import type { ComicChapter, ComicChapterReq } from "lyo-comic-api-client";

export function chapterUpdateData(
  chapter: ComicChapter,
  patch: { seriesId?: string; volumeId?: string | null } = {}
): ComicChapterReq {
  return {
    seriesId: patch.seriesId ?? chapter.seriesId,
    volumeId: patch.volumeId !== undefined ? patch.volumeId : (chapter.volumeId ?? null),
    chapterNumber: Number(chapter.chapterNumber),
    title: chapter.title ?? null,
    language: chapter.language || "en",
    pageCount: chapter.pageCount ?? null,
    publishedDate: chapter.publishedDate ?? null,
    source: chapter.source ?? null,
    coverImageRef: chapter.coverImageRef ?? null,
  };
}
