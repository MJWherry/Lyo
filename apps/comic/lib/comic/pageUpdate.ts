import type { ComicPage, ComicPageReq } from "lyo-comic-api-client";

export function pageUpdateData(
  page: ComicPage,
  patch: { chapterId?: string; pageNumber?: number } = {}
): ComicPageReq {
  return {
    chapterId: patch.chapterId ?? page.chapterId,
    pageNumber: patch.pageNumber ?? page.pageNumber,
    imageRef: page.imageRef ?? null,
    width: page.width ?? null,
    height: page.height ?? null,
  };
}
