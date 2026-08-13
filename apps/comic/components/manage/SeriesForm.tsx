"use client";

import { FormEvent, useMemo, useState, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import type { ComicChapter, ComicSeriesReq, ComicSeriesRes, ComicStatusValue, ComicTypeValue } from "lyo-comic-api-client";
import { comicFileUrl } from "lyo-comic-api-client";
import { ChipInput } from "lyo-query-components";
import { bffFetch } from "@/lib/api/bffFetch";
import { collectPageImageRefs, deleteCoverFileIfOrphan } from "@/lib/comic/pageRefs";
import { revalidateComicViews } from "@/lib/comic/revalidateViews";
import { patchProgressCover } from "@/lib/reading/progress";
import { ChapterPagePicker } from "./ChapterPagePicker";
import { CoverTile } from "./CoverTile";

const TYPES: ComicTypeValue[] = ["manga", "manhwa", "manhua", "webtoon", "western"];
const STATUSES: ComicStatusValue[] = ["ongoing", "completed", "hiatus", "cancelled"];
const FORM_ID = "series-form";

type Draft = {
  title: string;
  slug: string;
  comicType: string;
  status: string;
  language: string;
  publishedYear: string;
  author: string;
  artist: string;
  demographic: string;
  description: string;
  tags: string[];
  coverRef: string;
};

function draftFrom(series?: ComicSeriesRes): Draft {
  return {
    title: series?.title ?? "",
    slug: series?.slug ?? "",
    comicType: String(series?.comicType ?? "manga"),
    status: String(series?.status ?? "ongoing"),
    language: series?.language ?? "",
    publishedYear: series?.publishedYear != null ? String(series.publishedYear) : "",
    author: series?.author ?? "",
    artist: series?.artist ?? "",
    demographic: series?.demographic ?? "",
    description: series?.description ?? "",
    tags: [...(series?.tags ?? [])],
    coverRef: series?.coverImageRef ?? "",
  };
}

async function syncSeriesTags(seriesId: string, next: string[], previous: string[]) {
  const prev = new Set(previous.map((t) => t.trim()).filter(Boolean));
  const want = new Set(next.map((t) => t.trim()).filter(Boolean));
  for (const name of want) {
    if (prev.has(name)) continue;
    const res = await bffFetch(`/api/comic/series/${encodeURIComponent(seriesId)}/tags`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name }),
    });
    if (!res.ok) throw new Error("Could not add tag");
  }
  for (const name of prev) {
    if (want.has(name)) continue;
    const res = await bffFetch(`/api/comic/series/${encodeURIComponent(seriesId)}/tags/${encodeURIComponent(name)}`, {
      method: "DELETE",
    });
    if (!res.ok) throw new Error("Could not remove tag");
  }
}

export function SeriesForm({
  series,
  chapters,
  heading,
  subtitle,
  actions,
  below,
  children,
}: {
  series?: ComicSeriesRes;
  chapters?: ComicChapter[];
  heading: string;
  subtitle?: ReactNode;
  actions?: ReactNode;
  below?: ReactNode;
  children?: ReactNode;
}) {
  const router = useRouter();
  const initial = useMemo(() => draftFrom(series), [series]);
  const [draft, setDraft] = useState(initial);
  const [coverFile, setCoverFile] = useState<File | null>(null);
  const [coverPreview, setCoverPreview] = useState<string | null>(null);
  const [pagePickerOpen, setPagePickerOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  const dirty =
    JSON.stringify(draft) !== JSON.stringify(initial) || coverFile != null;

  function patch<K extends keyof Draft>(key: K, value: Draft[K]) {
    setDraft((cur) => ({ ...cur, [key]: value }));
  }

  function clearChanges() {
    setDraft(draftFrom(series));
    if (coverPreview) URL.revokeObjectURL(coverPreview);
    setCoverPreview(null);
    setCoverFile(null);
    setError(null);
  }

  function onCoverFile(file: File | null) {
    if (coverPreview) URL.revokeObjectURL(coverPreview);
    setCoverFile(file);
    setCoverPreview(file ? URL.createObjectURL(file) : null);
  }

  async function uploadCover(file: File, seriesId?: string) {
    const form = new FormData();
    form.set("file", file, file.name);
    const qs = seriesId ? `?seriesId=${encodeURIComponent(seriesId)}` : "";
    const res = await bffFetch(`/api/files/upload${qs}`, { method: "POST", body: form });
    const data = (await res.json()) as { id?: string; error?: string };
    if (!res.ok || !data.id) throw new Error(data.error ?? "Cover upload failed");
    return data.id;
  }

  async function onSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setPending(true);
    setError(null);
    const payload: ComicSeriesReq = {
      title: draft.title.trim(),
      slug: draft.slug.trim(),
      comicType: draft.comicType as ComicTypeValue,
      status: draft.status as ComicStatusValue,
      description: draft.description || null,
      language: draft.language || null,
      publishedYear: draft.publishedYear ? Number(draft.publishedYear) : null,
      author: draft.author || null,
      artist: draft.artist || null,
      demographic: draft.demographic || null,
      coverImageRef: draft.coverRef || null,
      publisher: series?.publisher ?? null,
      source: series?.source ?? null,
      alternateTitles: (series?.alternateTitles ?? []).map((a) => ({
        title: a.title,
        language: a.language ?? null,
      })),
      tags: draft.tags.map((name) => ({ name })),
    };

    try {
      if (series) {
        const previousCover = series.coverImageRef ?? "";
        if (coverFile) payload.coverImageRef = await uploadCover(coverFile, series.id);
        const res = await bffFetch("/api/comic/series/Update", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ keys: [series.id], data: payload }),
        });
        if (!res.ok) throw new Error(await res.text());
        await syncSeriesTags(series.id, draft.tags, series.tags ?? []);
        const nextCover = payload.coverImageRef ?? null;
        const pageRefs = await collectPageImageRefs((chapters ?? []).map((c) => c.id));
        await deleteCoverFileIfOrphan(previousCover, nextCover, pageRefs);
        patchProgressCover(series.id, nextCover);
        await revalidateComicViews(series.id, series.slug);
        if (nextCover) patch("coverRef", nextCover);
        if (coverPreview) URL.revokeObjectURL(coverPreview);
        setCoverPreview(null);
        setCoverFile(null);
        router.refresh();
      } else {
        const res = await bffFetch("/api/comic/series", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload),
        });
        const created = (await res.json()) as { isSuccess?: boolean; data?: ComicSeriesRes; error?: unknown };
        if (!res.ok || !created.isSuccess || !created.data?.id) throw new Error("Create failed");
        if (coverFile) {
          const id = await uploadCover(coverFile, created.data.id);
          await bffFetch("/api/comic/series/Update", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              keys: [created.data.id],
              data: { ...payload, coverImageRef: id, tags: undefined },
            }),
          });
        }
        router.push(`/manage/series/${created.data.id}`);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Save failed");
    } finally {
      setPending(false);
    }
  }

  const coverSrc = coverPreview || comicFileUrl(draft.coverRef, series?.updatedTimestamp);

  return (
    <>
      <div className="manage-header">
        <div>
          <h1>{heading}</h1>
          {subtitle ? <p className="muted" style={{ margin: "0.35rem 0 0" }}>{subtitle}</p> : null}
        </div>
        <div className="manage-header__actions">
          {actions}
          <button className="btn btn--ghost" type="button" disabled={!dirty || pending} onClick={clearChanges}>
            Clear changes
          </button>
          <button className="btn" type="submit" form={FORM_ID} disabled={pending}>
            {pending ? "Saving…" : "Save"}
          </button>
        </div>
      </div>
      {error ? <p className="error">{error}</p> : null}
      <div className={children || below ? "series-layout" : undefined}>
        <div className={children || below ? "series-layout__main" : undefined}>
        <form id={FORM_ID} onSubmit={onSubmit} className="form-grid">
        <div className="form-head">
          <div className="form-head__cover field">
            <label>Cover</label>
            <CoverTile
              src={coverSrc}
              onUpload={(file) => onCoverFile(file)}
              onClear={() => {
                onCoverFile(null);
                patch("coverRef", "");
              }}
              onPickPage={chapters?.length ? () => setPagePickerOpen(true) : undefined}
            />
          </div>
          <div className="field form-head__title">
            <label htmlFor="title">Title</label>
            <input id="title" required value={draft.title} onChange={(e) => patch("title", e.target.value)} />
          </div>
          <div className="field form-head__slug">
            <label htmlFor="slug">Slug</label>
            <input id="slug" required value={draft.slug} onChange={(e) => patch("slug", e.target.value)} />
          </div>
          <div className="field form-head__language">
            <label htmlFor="language">Language</label>
            <input id="language" value={draft.language} onChange={(e) => patch("language", e.target.value)} />
          </div>
          <div className="field form-head__year">
            <label htmlFor="publishedYear">Year</label>
            <input id="publishedYear" value={draft.publishedYear} onChange={(e) => patch("publishedYear", e.target.value)} />
          </div>
        </div>
        <div className="field">
          <label htmlFor="comicType">Type</label>
          <select id="comicType" value={draft.comicType} onChange={(e) => patch("comicType", e.target.value)}>
            {TYPES.map((t) => (
              <option key={String(t)} value={t}>
                {t}
              </option>
            ))}
          </select>
        </div>
        <div className="field">
          <label htmlFor="status">Status</label>
          <select id="status" value={draft.status} onChange={(e) => patch("status", e.target.value)}>
            {STATUSES.map((s) => (
              <option key={String(s)} value={s}>
                {s}
              </option>
            ))}
          </select>
        </div>
        <div className="field">
          <label htmlFor="author">Author</label>
          <input id="author" value={draft.author} onChange={(e) => patch("author", e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="artist">Artist</label>
          <input id="artist" value={draft.artist} onChange={(e) => patch("artist", e.target.value)} />
        </div>
        <div className="form-demo-tags">
          <div className="field">
            <label htmlFor="demographic">Demographic</label>
            <input id="demographic" value={draft.demographic} onChange={(e) => patch("demographic", e.target.value)} />
          </div>
          <div className="field form-demo-tags__tags">
            <label>Tags</label>
            <ChipInput values={draft.tags} onChange={(tags) => patch("tags", tags)} placeholder="Type a tag and press Enter" />
          </div>
        </div>
        <div className="field" style={{ gridColumn: "1 / -1" }}>
          <label htmlFor="description">Synopsis</label>
          <textarea id="description" rows={5} value={draft.description} onChange={(e) => patch("description", e.target.value)} />
        </div>
      </form>
        {below}
        </div>
        {children ? <aside className="series-layout__side">{children}</aside> : null}
      </div>
      <ChapterPagePicker
        open={pagePickerOpen}
        chapters={chapters ?? []}
        onClose={() => setPagePickerOpen(false)}
        onSelect={(imageRef) => {
          onCoverFile(null);
          patch("coverRef", imageRef);
          setPagePickerOpen(false);
        }}
      />
    </>
  );
}
