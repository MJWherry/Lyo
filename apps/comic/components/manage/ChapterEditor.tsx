"use client";

import { FormEvent, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import type { ComicChapterRes, ComicPage, ComicVolumeRes } from "lyo-comic-api-client";
import { comicFileUrl } from "lyo-comic-api-client";
import { bffFetch } from "@/lib/api/bffFetch";
import { TrashIcon } from "./TrashIcon";
import { ArchiveDownloadLink } from "@/components/ArchiveDownloadLink";

const FORM_ID = "chapter-form";

export function ChapterEditor({
  seriesId,
  chapter,
  pages,
  volumes,
}: {
  seriesId: string;
  chapter: ComicChapterRes;
  pages: ComicPage[];
  volumes: ComicVolumeRes[];
}) {
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const [ordered, setOrdered] = useState(pages);

  useEffect(() => {
    setOrdered(pages);
  }, [pages]);

  async function saveMeta(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const fd = new FormData(e.currentTarget);
    setPending(true);
    const res = await bffFetch("/api/comic/chapters/Update", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        keys: [chapter.id],
        data: {
          seriesId,
          volumeId: String(fd.get("volumeId") || "") || null,
          chapterNumber: Number(fd.get("chapterNumber")),
          title: String(fd.get("title") || "") || null,
          language: String(fd.get("language") || "en"),
          pageCount: ordered.length,
        },
      }),
    });
    setPending(false);
    if (!res.ok) setError("Update failed");
    else router.refresh();
  }

  async function deletePage(page: ComicPage) {
    if (!confirm("Delete this page?")) return;
    await bffFetch(`/api/comic/pages/${page.id}`, { method: "DELETE" });
    if (page.imageRef) await bffFetch(`/api/files/${page.imageRef}`, { method: "DELETE" });
    setOrdered((cur) => cur.filter((p) => p.id !== page.id));
    router.refresh();
  }

  async function replacePage(page: ComicPage, file: File) {
    const form = new FormData();
    form.set("file", file, file.name);
    const qs = new URLSearchParams({ chapterId: chapter.id, seriesId });
    const up = await bffFetch(`/api/files/upload?${qs}`, { method: "POST", body: form });
    const data = (await up.json()) as { id?: string };
    if (!data.id) return;
    if (page.imageRef) await bffFetch(`/api/files/${page.imageRef}`, { method: "DELETE" });
    await bffFetch("/api/comic/pages/Update", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        keys: [page.id],
        data: { chapterId: chapter.id, pageNumber: page.pageNumber, imageRef: data.id },
      }),
    });
    router.refresh();
  }

  async function addPages(files: FileList | File[]) {
    const list = Array.from(files);
    let n = ordered.length;
    for (const file of list) {
      n += 1;
      const form = new FormData();
      form.set("file", file, file.name);
      const qs = new URLSearchParams({ chapterId: chapter.id, seriesId });
      const up = await bffFetch(`/api/files/upload?${qs}`, { method: "POST", body: form });
      const data = (await up.json()) as { id?: string };
      if (!data.id) continue;
      await bffFetch("/api/comic/pages", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ chapterId: chapter.id, pageNumber: n, imageRef: data.id }),
      });
    }
    router.refresh();
  }

  return (
    <div>
      <div className="manage-header">
        <div>
          <p className="muted" style={{ margin: "0 0 0.35rem" }}>
            <Link href={`/manage/series/${seriesId}`}>← Series</Link>
          </p>
          <h1>
            Chapter {chapter.chapterNumber}
            {chapter.title ? ` · ${chapter.title}` : ""}
          </h1>
        </div>
        <div className="manage-header__actions">
          <ArchiveDownloadLink href={`/api/comic/chapters/${encodeURIComponent(chapter.id)}/archive`} className="btn btn--ghost" label="Download chapter">
            Download
          </ArchiveDownloadLink>
          <button className="btn btn--ghost" type="reset" form={FORM_ID} disabled={pending}>
            Clear changes
          </button>
          <button className="btn" type="submit" form={FORM_ID} disabled={pending}>
            {pending ? "Saving…" : "Save"}
          </button>
        </div>
      </div>
      <form id={FORM_ID} onSubmit={saveMeta} className="form-grid">
        <div className="field">
          <label>Number</label>
          <input name="chapterNumber" defaultValue={chapter.chapterNumber} />
        </div>
        <div className="field">
          <label>Title</label>
          <input name="title" defaultValue={chapter.title ?? ""} />
        </div>
        <div className="field">
          <label>Language</label>
          <input name="language" defaultValue={chapter.language} />
        </div>
        <div className="field">
          <label>Volume</label>
          <select name="volumeId" defaultValue={chapter.volumeId ?? ""}>
            <option value="">None</option>
            {volumes.map((v) => (
              <option key={v.id} value={v.id}>
                {v.title || `Vol. ${v.volumeNumber}`}
              </option>
            ))}
          </select>
        </div>
      </form>
      {error ? <p className="error">{error}</p> : null}

      <h2 style={{ marginTop: "1.5rem" }}>Pages</h2>
      <div className="dropzone" style={{ marginBottom: "1rem" }}>
        <input type="file" accept="image/*" multiple onChange={(e) => e.target.files && addPages(e.target.files)} />
      </div>
      <div className="page-thumbs">
        {ordered.map((p) => {
          const src = comicFileUrl(p.imageRef);
          return (
            <div key={p.id} className="page-thumb">
              <div className="page-thumb__frame">
                {src ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img src={src} alt={`Page ${p.pageNumber}`} />
                ) : (
                  <div className="page-thumb__placeholder" />
                )}
                <label
                  className={src ? "page-thumb__hit" : "page-thumb__add"}
                  title={src ? "Replace image" : "Upload image"}
                  aria-label={src ? `Replace page ${p.pageNumber}` : `Upload page ${p.pageNumber}`}
                >
                  <input
                    type="file"
                    accept="image/*"
                    onChange={(e) => {
                      const f = e.target.files?.[0];
                      if (f) void replacePage(p, f);
                      e.target.value = "";
                    }}
                  />
                  {!src ? (
                    <span className="page-thumb__plus" aria-hidden>
                      +
                    </span>
                  ) : null}
                </label>
                <button
                  type="button"
                  className="page-thumb__delete"
                  aria-label={`Delete page ${p.pageNumber}`}
                  onClick={() => deletePage(p)}
                >
                  <TrashIcon />
                </button>
              </div>
              <p className="page-thumb__num">#{p.pageNumber}</p>
            </div>
          );
        })}
      </div>
    </div>
  );
}
