import { revalidatePath } from "next/cache";
import { NextRequest, NextResponse } from "next/server";
import type { ComicChapterReq, ComicChapterRes, ComicPage, ComicPageReq } from "lyo-comic-api-client";
import { getApi, getComicApi } from "@/lib/api/serverClient";
import { abortedUpstreamResponse } from "@/lib/api/abortedResponse";
import { isUnauthorized } from "@/lib/auth/unauthorized";
import { chapterUpdateData } from "@/lib/comic/chapterUpdate";
import { pageUpdateData } from "@/lib/comic/pageUpdate";

export const dynamic = "force-dynamic";

type SplitBody = {
  sourceChapterId?: string;
  chapter?: ComicChapterReq;
  copyCover?: boolean;
  pageIds?: string[];
};

/**
 * Clone chapter metadata, then move selected pages onto the new chapter.
 * Compacts page numbers on both sides. Does not duplicate page files — image refs stay shared.
 */
export async function POST(request: NextRequest) {
  let body: SplitBody;
  try {
    body = (await request.json()) as SplitBody;
  } catch {
    return NextResponse.json({ error: "Invalid JSON body." }, { status: 400 });
  }

  const sourceId = body.sourceChapterId?.trim();
  const pageIds = [...new Set((body.pageIds ?? []).filter(Boolean))];
  const chapterNumber = Number(body.chapter?.chapterNumber);
  const language = body.chapter?.language?.trim() || "";
  if (!sourceId || !body.chapter?.seriesId || !language || !Number.isFinite(chapterNumber)) {
    return NextResponse.json({ error: "sourceChapterId, chapter.seriesId, chapter.chapterNumber, and chapter.language are required." }, { status: 400 });
  }
  if (pageIds.length === 0) {
    return NextResponse.json({ error: "Select at least one page to move." }, { status: 400 });
  }

  try {
    const comic = await getComicApi(request.signal);
    const api = await getApi(request.signal);
    const source = (await comic.getChapter(sourceId)).data;
    if (!source) return NextResponse.json({ error: "Source chapter not found." }, { status: 404 });
    if (source.seriesId !== body.chapter.seriesId) {
      return NextResponse.json({ error: "Chapter does not belong to this series." }, { status: 400 });
    }

    const pages = (await comic.getChapterPages(sourceId)).data ?? [];
    const pageById = new Map(pages.map((p) => [p.id, p]));
    const moving: ComicPage[] = [];
    for (const id of pageIds) {
      const page = pageById.get(id);
      if (!page) return NextResponse.json({ error: `Page ${id} is not on this chapter.` }, { status: 400 });
      moving.push(page);
    }
    moving.sort((a, b) => a.pageNumber - b.pageNumber);

    const payload: ComicChapterReq = {
      seriesId: source.seriesId,
      volumeId: body.chapter.volumeId !== undefined ? body.chapter.volumeId : (source.volumeId ?? null),
      chapterNumber,
      title: body.chapter.title?.trim() || null,
      language,
      pageCount: moving.length,
      publishedDate: body.chapter.publishedDate ?? source.publishedDate ?? null,
      source: body.chapter.source ?? source.source ?? null,
      coverImageRef: body.copyCover ? source.coverImageRef ?? null : body.chapter.coverImageRef ?? null,
    };

    const created = await api.create<ComicChapterReq, ComicChapterRes>("/api/comic/chapters", payload);
    const createdChapter = created.data?.data;
    if (!created.ok || !created.data?.isSuccess || !createdChapter?.id) {
      return NextResponse.json({ error: created.data?.error ?? "Could not create the new chapter (number and language may already exist)." }, { status: 400 });
    }
    const newId = createdChapter.id;

    for (let i = 0; i < moving.length; i++) {
      const page = moving[i];
      const updated = await api.update<ComicPageReq, unknown>("/api/comic/pages", [page.id], pageUpdateData(page, { chapterId: newId, pageNumber: i + 1 }));
      if (!updated.ok) {
        return NextResponse.json({ error: `Created chapter but failed to move page ${page.pageNumber}.` }, { status: 502, headers: { "x-new-chapter-id": newId } });
      }
    }

    const movingIds = new Set(moving.map((p) => p.id));
    const remaining = pages.filter((p) => !movingIds.has(p.id)).sort((a, b) => a.pageNumber - b.pageNumber);
    for (let i = 0; i < remaining.length; i++) {
      const page = remaining[i];
      const nextNumber = i + 1;
      if (page.pageNumber === nextNumber) continue;
      const updated = await api.update<ComicPageReq, unknown>("/api/comic/pages", [page.id], pageUpdateData(page, { pageNumber: nextNumber }));
      if (!updated.ok) {
        return NextResponse.json({ error: `Moved pages but failed to renumber remaining page ${page.pageNumber}.` }, { status: 502, headers: { "x-new-chapter-id": newId } });
      }
    }

    const sourceCount = await api.update<ComicChapterReq, unknown>("/api/comic/chapters", [source.id], chapterUpdateData(source, { pageCount: remaining.length }));
    if (!sourceCount.ok) {
      return NextResponse.json({ error: "Moved pages but failed to update the original chapter page count." }, { status: 502, headers: { "x-new-chapter-id": newId } });
    }

    revalidatePath("/");
    revalidatePath("/search");
    revalidatePath("/manage");
    revalidatePath(`/manga/${source.seriesId}`);
    revalidatePath(`/manage/series/${source.seriesId}`);
    revalidatePath(`/manage/series/${source.seriesId}/chapters/${source.id}`);
    revalidatePath(`/manage/series/${source.seriesId}/chapters/${newId}`);

    return NextResponse.json({
      isSuccess: true,
      chapter: createdChapter,
      movedPageCount: moving.length,
      remainingPageCount: remaining.length,
    });
  } catch (err) {
    const aborted = abortedUpstreamResponse(err, request.signal);
    if (aborted) return aborted;
    if (isUnauthorized(err)) {
      return NextResponse.json({ error: "Sign in required." }, { status: 401 });
    }
    const message = err instanceof Error ? err.message : "Split failed";
    return NextResponse.json({ error: message }, { status: 502 });
  }
}
