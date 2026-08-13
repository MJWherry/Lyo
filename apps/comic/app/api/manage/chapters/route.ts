import { NextRequest, NextResponse } from "next/server";
import type { ComicChapterReq, ComicChapterRes, ComicPageReq, ComicPageRes } from "lyo-comic-api-client";
import { getApi, apiFetch } from "@/lib/api/serverClient";
import { abortedUpstreamResponse } from "@/lib/api/abortedResponse";
import { isUnauthorized } from "@/lib/auth/unauthorized";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";

type IngestJson = {
  seriesId: string;
  volumeId?: string | null;
  chapterNumber: number;
  title?: string | null;
  language: string;
  publishedDate?: string | null;
};

/**
 * Create a chapter, upload page images, then create page rows.
 * Accepts multipart (`meta` JSON + `files`) or JSON without files.
 */
export async function POST(request: NextRequest) {
  try {
    const contentType = request.headers.get("content-type") ?? "";
    let meta: IngestJson;
    const files: File[] = [];

    if (contentType.includes("multipart/form-data")) {
      const form = await request.formData();
      const raw = form.get("meta");
      if (typeof raw !== "string") {
        return NextResponse.json({ error: "Missing meta JSON field." }, { status: 400 });
      }
      meta = JSON.parse(raw) as IngestJson;
      for (const [key, value] of form.entries()) {
        if (key === "files" && value instanceof File && value.size > 0) files.push(value);
      }
    } else {
      meta = (await request.json()) as IngestJson;
    }

    if (!meta?.seriesId || !meta.language || meta.chapterNumber == null) {
      return NextResponse.json({ error: "seriesId, language, and chapterNumber are required." }, { status: 400 });
    }

    const api = await getApi(request.signal);
    const created = await api.create<ComicChapterReq, ComicChapterRes>("/api/comic/chapters", {
      seriesId: meta.seriesId,
      volumeId: meta.volumeId || null,
      chapterNumber: Number(meta.chapterNumber),
      title: meta.title ?? null,
      language: meta.language,
      pageCount: files.length || null,
      publishedDate: meta.publishedDate ?? null,
    });
    const chapter = created.data?.data;
    if (!created.data?.isSuccess || !chapter?.id) {
      return NextResponse.json({ error: created.data?.error ?? "Chapter create failed." }, { status: 400 });
    }

    const pages = [];
    for (let i = 0; i < files.length; i++) {
      const file = files[i];
      const uploadForm = new FormData();
      uploadForm.set("file", file, file.name);
      const qs = new URLSearchParams({
        chapterId: chapter.id,
        seriesId: meta.seriesId,
      });
      if (meta.volumeId) qs.set("volumeId", meta.volumeId);
      const up = await apiFetch(`/files/upload?${qs.toString()}`, { method: "POST", body: uploadForm, signal: request.signal });
      if (!up.ok) {
        return NextResponse.json({ error: `Upload failed for ${file.name}` }, { status: 502 });
      }
      const uploaded = (await up.json()) as { id?: string };
      if (!uploaded.id) {
        return NextResponse.json({ error: `Upload returned no id for ${file.name}` }, { status: 502 });
      }
      const page = await api.create<ComicPageReq, ComicPageRes>("/api/comic/pages", {
        chapterId: chapter.id,
        pageNumber: i + 1,
        imageRef: uploaded.id,
      });
      pages.push(page.data?.data ?? null);
    }

    if (files.length > 0) {
      await api.update<ComicChapterReq, ComicChapterRes>("/api/comic/chapters", [chapter.id], {
        seriesId: meta.seriesId,
        volumeId: meta.volumeId || null,
        chapterNumber: Number(meta.chapterNumber),
        title: meta.title ?? null,
        language: meta.language,
        pageCount: files.length,
        publishedDate: meta.publishedDate ?? null,
      });
    }

    return NextResponse.json({ isSuccess: true, chapter, pages });
  } catch (err) {
    const aborted = abortedUpstreamResponse(err, request.signal);
    if (aborted) return aborted;
    if (isUnauthorized(err)) {
      return NextResponse.json({ error: "Sign in required." }, { status: 401 });
    }
    const message = err instanceof Error ? err.message : "Chapter ingest failed";
    return NextResponse.json({ error: message }, { status: 502 });
  }
}
