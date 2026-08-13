import { revalidatePath } from "next/cache";
import { NextRequest, NextResponse } from "next/server";
import type { ComicChapter, ComicChapterReq, ComicSeriesReq, ComicSeriesRes, ComicVolumeReq, ComicVolumeRes } from "lyo-comic-api-client";
import { getApi, getComicApi } from "@/lib/api/serverClient";
import { isUnauthorized } from "@/lib/auth/unauthorized";
import { chapterUpdateData } from "@/lib/comic/chapterUpdate";

export const dynamic = "force-dynamic";

type SplitBody = {
  sourceSeriesId?: string;
  series?: ComicSeriesReq;
  copyCover?: boolean;
  tags?: string[];
  chapterIds?: string[];
};

/**
 * Clone series metadata, then move selected chapters (and their pages) onto the new series.
 * Recreates volumes as needed. Does not duplicate page files — image refs stay shared.
 */
export async function POST(request: NextRequest) {
  let body: SplitBody;
  try {
    body = (await request.json()) as SplitBody;
  } catch {
    return NextResponse.json({ error: "Invalid JSON body." }, { status: 400 });
  }

  const sourceId = body.sourceSeriesId?.trim();
  const chapterIds = [...new Set((body.chapterIds ?? []).filter(Boolean))];
  if (!sourceId || !body.series?.title?.trim() || !body.series.slug?.trim()) {
    return NextResponse.json({ error: "sourceSeriesId, series.title, and series.slug are required." }, { status: 400 });
  }
  if (chapterIds.length === 0) {
    return NextResponse.json({ error: "Select at least one chapter to move." }, { status: 400 });
  }

  try {
    const comic = await getComicApi();
    const api = await getApi();
    const source = (await comic.getSeries(sourceId)).data;
    if (!source) return NextResponse.json({ error: "Source series not found." }, { status: 404 });

    const [volumesRes, chaptersRes] = await Promise.all([comic.getSeriesVolumes(sourceId), comic.getSeriesChapters(sourceId)]);
    const volumes = volumesRes.data ?? [];
    const chapters = chaptersRes.data ?? [];
    const chapterById = new Map(chapters.map((c) => [c.id, c]));
    const moving: ComicChapter[] = [];
    for (const id of chapterIds) {
      const ch = chapterById.get(id);
      if (!ch) return NextResponse.json({ error: `Chapter ${id} is not on this series.` }, { status: 400 });
      moving.push(ch);
    }

    const payload: ComicSeriesReq = {
      ...body.series,
      title: body.series.title.trim(),
      slug: body.series.slug.trim(),
      coverImageRef: body.copyCover ? source.coverImageRef ?? null : body.series.coverImageRef ?? null,
      tags: undefined,
    };

    const created = await api.create<ComicSeriesReq, ComicSeriesRes>("/api/comic/series", payload);
    const createdSeries = created.data?.data;
    if (!created.ok || !created.data?.isSuccess || !createdSeries?.id) {
      return NextResponse.json({ error: created.data?.error ?? "Could not create the new series (slug may already exist)." }, { status: 400 });
    }
    const newId = createdSeries.id;

    for (const name of body.tags ?? []) {
      if (!name?.trim()) continue;
      await comic.addSeriesTag(newId, { name: name.trim() }).catch(() => undefined);
    }

    const volumeById = new Map(volumes.map((v) => [v.id, v]));
    const newVolumeBySource = new Map<string, string>();

    const neededVolumeIds = [...new Set(moving.map((c) => c.volumeId).filter((id): id is string => Boolean(id)))];
    for (const sourceVolumeId of neededVolumeIds) {
      const vol = volumeById.get(sourceVolumeId);
      if (!vol) continue;
      const volCreated = await api.create<ComicVolumeReq, ComicVolumeRes>("/api/comic/volumes", {
        seriesId: newId,
        title: vol.title ?? null,
        volumeNumber: vol.volumeNumber ?? null,
        coverImageRef: vol.coverImageRef ?? null,
        publishedDate: vol.publishedDate ?? null,
      });
      const newVol = volCreated.data?.data;
      if (newVol?.id) newVolumeBySource.set(sourceVolumeId, newVol.id);
    }

    for (const ch of moving) {
      const nextVolumeId = ch.volumeId ? (newVolumeBySource.get(ch.volumeId) ?? null) : null;
      const updated = await api.update<ComicChapterReq, unknown>("/api/comic/chapters", [ch.id], chapterUpdateData(ch, { seriesId: newId, volumeId: nextVolumeId }));
      if (!updated.ok) {
        return NextResponse.json({ error: `Created series but failed to move chapter ${ch.chapterNumber}.` }, { status: 502, headers: { "x-new-series-id": newId } });
      }
    }

    const remaining = chapters.filter((c) => !chapterIds.includes(c.id));
    const remainingVolumeIds = new Set(remaining.map((c) => c.volumeId).filter(Boolean));
    for (const vol of volumes) {
      if (!remainingVolumeIds.has(vol.id) && neededVolumeIds.includes(vol.id)) {
        await api.deleteById("/api/comic/volumes", vol.id).catch(() => undefined);
      }
    }

    revalidatePath("/");
    revalidatePath("/search");
    revalidatePath("/manage");
    revalidatePath(`/manga/${source.slug}`);
    revalidatePath(`/manga/${createdSeries.slug}`);
    revalidatePath(`/manage/series/${sourceId}`);
    revalidatePath(`/manage/series/${newId}`);

    return NextResponse.json({
      isSuccess: true,
      series: createdSeries,
      movedChapterCount: moving.length,
      createdVolumeCount: newVolumeBySource.size,
    });
  } catch (err) {
    if (isUnauthorized(err)) {
      return NextResponse.json({ error: "Sign in required." }, { status: 401 });
    }
    const message = err instanceof Error ? err.message : "Split failed";
    return NextResponse.json({ error: message }, { status: 502 });
  }
}
