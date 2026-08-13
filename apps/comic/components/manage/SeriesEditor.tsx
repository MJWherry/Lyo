"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState, type DragEvent, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import type { ComicChapter, ComicSeriesRes, ComicVolumeRes } from "lyo-comic-api-client";
import { comicFileUrl } from "lyo-comic-api-client";
import { SeriesForm } from "./SeriesForm";
import { CoverTile } from "./CoverTile";
import { ChapterPagePicker } from "./ChapterPagePicker";
import { TrashIcon } from "./TrashIcon";
import { bffFetch } from "@/lib/api/bffFetch";
import { chapterUpdateData } from "@/lib/comic/chapterUpdate";
import { collectPageImageRefs, deleteCoverFileIfOrphan } from "@/lib/comic/pageRefs";
import { volumeUpdateData } from "@/lib/comic/volumeUpdate";

const CHAPTER_DRAG = "text/plain";

export function SeriesEditor({
  series,
  volumes,
  chapters,
}: {
  series: ComicSeriesRes;
  volumes: ComicVolumeRes[];
  chapters: ComicChapter[];
}) {
  const router = useRouter();
  const [volTitle, setVolTitle] = useState("");
  const [volNumber, setVolNumber] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [volumesOpen, setVolumesOpen] = useState(true);
  const [addVolumeOpen, setAddVolumeOpen] = useState(false);
  const [addChapterOpen, setAddChapterOpen] = useState(false);
  const [collapsed, setCollapsed] = useState<Record<string, boolean>>({});
  const [localChapters, setLocalChapters] = useState(chapters);
  const [localVolumes, setLocalVolumes] = useState(volumes);
  const [draggingId, setDraggingId] = useState<string | null>(null);
  const [pickerVolumeId, setPickerVolumeId] = useState<string | null>(null);

  useEffect(() => {
    setLocalChapters(chapters);
  }, [chapters]);

  useEffect(() => {
    setLocalVolumes(volumes);
  }, [volumes]);

  const sortedVolumes = useMemo(
    () => [...localVolumes].sort((a, b) => Number(a.volumeNumber ?? 0) - Number(b.volumeNumber ?? 0)),
    [localVolumes]
  );
  const sortedChapters = useMemo(
    () => [...localChapters].sort((a, b) => Number(a.chapterNumber) - Number(b.chapterNumber)),
    [localChapters]
  );
  const unassigned = sortedChapters.filter((ch) => !ch.volumeId);

  async function addVolume(e: FormEvent) {
    e.preventDefault();
    setError(null);
    const res = await bffFetch("/api/comic/volumes", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        seriesId: series.id,
        title: volTitle || null,
        volumeNumber: volNumber ? Number(volNumber) : null,
      }),
    });
    if (!res.ok) {
      setError("Could not add volume");
      return;
    }
    setVolTitle("");
    setVolNumber("");
    setAddVolumeOpen(false);
    router.refresh();
  }

  async function deleteVolume(id: string) {
    if (!confirm("Delete volume?")) return;
    await bffFetch(`/api/comic/volumes/${id}`, { method: "DELETE" });
    router.refresh();
  }

  async function deleteChapter(id: string) {
    if (!confirm("Delete chapter?")) return;
    await bffFetch(`/api/comic/chapters/${id}`, { method: "DELETE" });
    setLocalChapters((cur) => cur.filter((c) => c.id !== id));
    router.refresh();
  }

  async function setVolumeCover(volume: ComicVolumeRes, coverImageRef: string | null, uploadedFile?: File) {
    let nextRef = coverImageRef;
    if (uploadedFile) {
      const form = new FormData();
      form.set("file", uploadedFile, uploadedFile.name);
      const up = await bffFetch(`/api/files/upload?seriesId=${encodeURIComponent(series.id)}`, { method: "POST", body: form });
      const data = (await up.json()) as { id?: string; error?: string };
      if (!up.ok || !data.id) {
        setError(data.error ?? "Could not upload volume cover");
        return;
      }
      nextRef = data.id;
    }
    const previous = volume.coverImageRef ?? null;
    setLocalVolumes((cur) => cur.map((v) => (v.id === volume.id ? { ...v, coverImageRef: nextRef } : v)));
    setError(null);
    const res = await bffFetch("/api/comic/volumes/Update", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ keys: [volume.id], data: volumeUpdateData(volume, { coverImageRef: nextRef }) }),
    });
    if (!res.ok) {
      setLocalVolumes((cur) => cur.map((v) => (v.id === volume.id ? volume : v)));
      setError("Could not update volume cover");
      return;
    }
    const pageRefs = await collectPageImageRefs(localChapters.map((c) => c.id));
    if (nextRef) pageRefs.add(nextRef);
    await deleteCoverFileIfOrphan(previous, nextRef, pageRefs);
    router.refresh();
  }

  async function assignChapter(chapterId: string, volumeId: string | null) {
    const chapter = localChapters.find((c) => c.id === chapterId);
    if (!chapter) return;
    const nextVolume = volumeId || null;
    if ((chapter.volumeId ?? null) === nextVolume) return;
    setLocalChapters((cur) => cur.map((c) => (c.id === chapterId ? { ...c, volumeId: nextVolume } : c)));
    setCollapsed((cur) => ({ ...cur, [nextVolume ?? "none"]: false }));
    setError(null);
    const res = await bffFetch("/api/comic/chapters/Update", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ keys: [chapter.id], data: chapterUpdateData(chapter, { volumeId: nextVolume }) }),
    });
    if (!res.ok) {
      setLocalChapters((cur) => cur.map((c) => (c.id === chapterId ? chapter : c)));
      setError("Could not move chapter");
      return;
    }
    router.refresh();
  }

  function toggleGroup(id: string) {
    setCollapsed((cur) => ({ ...cur, [id]: !cur[id] }));
  }

  function chaptersForVolume(volumeId: string) {
    return sortedChapters.filter((ch) => ch.volumeId === volumeId);
  }

  const pickerChapters = useMemo(() => {
    if (!pickerVolumeId) return [] as ComicChapter[];
    const inVol = localChapters.filter((c) => c.volumeId === pickerVolumeId);
    return inVol.length > 0 ? inVol : localChapters;
  }, [pickerVolumeId, localChapters]);

  return (
    <SeriesForm
      series={series}
      chapters={localChapters}
      heading={`Edit ${series.title}`}
      subtitle={<Link href={`/manga/${encodeURIComponent(series.slug)}`}>View public page</Link>}
      actions={
        <Link className="btn btn--ghost" href={`/manage/series/${series.id}/split`}>
          Split series
        </Link>
      }
      below={
        <>
          <section className="catalog-pane catalog-pane--volumes catalog-pane--main">
            <header className="catalog-pane__head">
              <button
                type="button"
                className="catalog-group__toggle catalog-pane__toggle"
                onClick={() => setVolumesOpen((o) => !o)}
                aria-expanded={volumesOpen}
              >
                <span className="catalog-group__chevron" aria-hidden>
                  {volumesOpen ? "▾" : "▸"}
                </span>
                <h2>
                  Volumes <span className="muted">{sortedVolumes.length}</span>
                </h2>
              </button>
              <button type="button" className="btn btn--ghost btn--small" onClick={() => setAddVolumeOpen((o) => !o)}>
                {addVolumeOpen ? "Close" : "Add"}
              </button>
            </header>
            {addVolumeOpen ? (
              <form onSubmit={addVolume} className="catalog-pane__add">
                <div className="field">
                  <label>Number</label>
                  <input value={volNumber} onChange={(e) => setVolNumber(e.target.value)} />
                </div>
                <div className="field">
                  <label>Title</label>
                  <input value={volTitle} onChange={(e) => setVolTitle(e.target.value)} />
                </div>
                <button className="btn btn--small" type="submit">
                  Add volume
                </button>
              </form>
            ) : null}
            {volumesOpen ? (
              <ul className="volume-edit-grid">
                {sortedVolumes.length === 0 ? <li className="muted catalog-pane__empty">No volumes yet.</li> : null}
                {sortedVolumes.map((v) => (
                  <DropTarget key={v.id} className="volume-edit-card" onDropId={(id) => void assignChapter(id, v.id)} active={draggingId != null}>
                    <CoverTile
                      src={comicFileUrl(v.coverImageRef, v.updatedTimestamp)}
                      alt=""
                      onUpload={(file) => void setVolumeCover(v, v.coverImageRef ?? null, file)}
                      onClear={() => void setVolumeCover(v, null)}
                      onPickPage={localChapters.length ? () => setPickerVolumeId(v.id) : undefined}
                    />
                    <div className="volume-edit-card__meta">
                      <span className="catalog-volume-row__title">
                        {v.title || `Vol. ${v.volumeNumber}`}
                        <span className="muted"> · {chaptersForVolume(v.id).length} ch</span>
                      </span>
                      <button
                        type="button"
                        className="icon-btn icon-btn--danger"
                        aria-label={`Delete ${v.title || `volume ${v.volumeNumber}`}`}
                        onClick={() => deleteVolume(v.id)}
                      >
                        <TrashIcon />
                      </button>
                    </div>
                  </DropTarget>
                ))}
              </ul>
            ) : null}
          </section>
          <ChapterPagePicker
            open={pickerVolumeId != null}
            chapters={pickerChapters}
            initialChapterId={pickerChapters[0]?.id}
            onClose={() => setPickerVolumeId(null)}
            onSelect={(imageRef) => {
              const volume = localVolumes.find((v) => v.id === pickerVolumeId);
              setPickerVolumeId(null);
              if (volume) void setVolumeCover(volume, imageRef);
            }}
          />
        </>
      }
    >
      <section className="catalog-pane catalog-pane--chapters">
        <header className="catalog-pane__head">
          <h2>
            Chapters <span className="muted">{sortedChapters.length}</span>
          </h2>
          <button type="button" className="btn btn--ghost btn--small" onClick={() => setAddChapterOpen((o) => !o)}>
            {addChapterOpen ? "Close" : "Add"}
          </button>
        </header>
        {addChapterOpen ? <ChapterIngestForm seriesId={series.id} volumes={sortedVolumes} onAdded={() => setAddChapterOpen(false)} /> : null}
        {error ? <p className="error catalog-pane__error">{error}</p> : null}
        <p className="catalog-pane__hint">Drag a chapter onto a volume to assign it.</p>
        <ul className="catalog-pane__list">
          {sortedChapters.length === 0 && sortedVolumes.length === 0 ? (
            <li className="muted catalog-pane__empty">No chapters yet.</li>
          ) : null}
          {sortedVolumes.map((v) => {
            const volChapters = chaptersForVolume(v.id);
            const open = !collapsed[v.id];
            return (
              <DropTarget key={v.id} className="catalog-group" onDropId={(id) => void assignChapter(id, v.id)} active={draggingId != null}>
                <div className="catalog-group__head">
                  <button type="button" className="catalog-group__toggle" onClick={() => toggleGroup(v.id)} aria-expanded={open}>
                    <span className="catalog-group__chevron" aria-hidden>
                      {open ? "▾" : "▸"}
                    </span>
                    <span>
                      {v.title || `Vol. ${v.volumeNumber}`}
                      <span className="muted"> · {volChapters.length}</span>
                    </span>
                  </button>
                </div>
                {open ? (
                  <ul className="catalog-group__items">
                    {volChapters.length === 0 ? <li className="muted catalog-pane__empty">Drop chapters here</li> : null}
                    {volChapters.map((ch) => (
                      <ChapterRow
                        key={ch.id}
                        seriesId={series.id}
                        chapter={ch}
                        dragging={draggingId === ch.id}
                        onDragStart={() => setDraggingId(ch.id)}
                        onDragEnd={() => setDraggingId(null)}
                        onDelete={deleteChapter}
                      />
                    ))}
                  </ul>
                ) : null}
              </DropTarget>
            );
          })}
          <DropTarget className="catalog-group" onDropId={(id) => void assignChapter(id, null)} active={draggingId != null}>
            <div className="catalog-group__head">
              <button type="button" className="catalog-group__toggle" onClick={() => toggleGroup("none")} aria-expanded={!collapsed.none}>
                <span className="catalog-group__chevron" aria-hidden>
                  {!collapsed.none ? "▾" : "▸"}
                </span>
                <span>
                  Unassigned <span className="muted"> · {unassigned.length}</span>
                </span>
              </button>
            </div>
            {!collapsed.none ? (
              <ul className="catalog-group__items">
                {unassigned.length === 0 ? <li className="muted catalog-pane__empty">Drop chapters here to unassign</li> : null}
                {unassigned.map((ch) => (
                  <ChapterRow
                    key={ch.id}
                    seriesId={series.id}
                    chapter={ch}
                    dragging={draggingId === ch.id}
                    onDragStart={() => setDraggingId(ch.id)}
                    onDragEnd={() => setDraggingId(null)}
                    onDelete={deleteChapter}
                  />
                ))}
              </ul>
            ) : null}
          </DropTarget>
        </ul>
      </section>
    </SeriesForm>
  );
}

function DropTarget({
  children,
  onDropId,
  active,
  className,
}: {
  children: ReactNode;
  onDropId: (id: string) => void;
  active: boolean;
  className?: string;
}) {
  const [over, setOver] = useState(false);

  function allowDrop(e: DragEvent) {
    e.preventDefault();
    e.dataTransfer.dropEffect = "move";
    if (active) setOver(true);
  }

  function leave(e: DragEvent) {
    const next = e.relatedTarget;
    if (next instanceof Node && e.currentTarget.contains(next)) return;
    setOver(false);
  }

  return (
    <li
      className={`catalog-drop${className ? ` ${className}` : ""}${over && active ? " catalog-drop--over" : ""}`}
      onDragOverCapture={allowDrop}
      onDragEnterCapture={allowDrop}
      onDragLeave={leave}
      onDrop={(e) => {
        e.preventDefault();
        e.stopPropagation();
        setOver(false);
        const id = e.dataTransfer.getData(CHAPTER_DRAG) || e.dataTransfer.getData("text/plain");
        if (id) onDropId(id);
      }}
    >
      {children}
    </li>
  );
}

function ChapterRow({
  seriesId,
  chapter,
  dragging,
  onDragStart,
  onDragEnd,
  onDelete,
}: {
  seriesId: string;
  chapter: ComicChapter;
  dragging: boolean;
  onDragStart: () => void;
  onDragEnd: () => void;
  onDelete: (id: string) => void;
}) {
  return (
    <li
      className={`manage-list__row catalog-chapter catalog-chapter--drag${dragging ? " catalog-chapter--dragging" : ""}`}
      draggable
      onDragStart={(e) => {
        e.dataTransfer.setData(CHAPTER_DRAG, chapter.id);
        e.dataTransfer.setData("text/plain", chapter.id);
        e.dataTransfer.effectAllowed = "move";
        onDragStart();
      }}
      onDragEnd={onDragEnd}
    >
      <span className="catalog-chapter__handle" aria-hidden>
        ⋮⋮
      </span>
      <Link href={`/manage/series/${seriesId}/chapters/${chapter.id}`} draggable={false}>
        Ch. {chapter.chapterNumber}
        {chapter.title ? ` · ${chapter.title}` : ""}
      </Link>
      <button
        type="button"
        className="icon-btn icon-btn--danger"
        aria-label={`Delete chapter ${chapter.chapterNumber}`}
        draggable={false}
        onClick={() => onDelete(chapter.id)}
      >
        <TrashIcon />
      </button>
    </li>
  );
}

function ChapterIngestForm({
  seriesId,
  volumes,
  onAdded,
}: {
  seriesId: string;
  volumes: ComicVolumeRes[];
  onAdded: () => void;
}) {
  const router = useRouter();
  const [files, setFiles] = useState<File[]>([]);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setPending(true);
    setError(null);
    const fd = new FormData(e.currentTarget);
    const meta = {
      seriesId,
      volumeId: String(fd.get("volumeId") || "") || null,
      chapterNumber: Number(fd.get("chapterNumber")),
      title: String(fd.get("title") || "") || null,
      language: String(fd.get("language") || "en"),
    };
    const body = new FormData();
    body.set("meta", JSON.stringify(meta));
    for (const file of files) body.append("files", file);
    const res = await bffFetch("/api/manage/chapters", { method: "POST", body });
    const data = (await res.json()) as { error?: string };
    setPending(false);
    if (!res.ok) {
      setError(data.error ?? "Ingest failed");
      return;
    }
    setFiles([]);
    e.currentTarget.reset();
    onAdded();
    router.refresh();
  }

  return (
    <form onSubmit={onSubmit} className="catalog-pane__add">
      <div className="field">
        <label>Chapter number</label>
        <input name="chapterNumber" required inputMode="decimal" />
      </div>
      <div className="field">
        <label>Title</label>
        <input name="title" />
      </div>
      <div className="field">
        <label>Language</label>
        <input name="language" defaultValue="en" required />
      </div>
      <div className="field">
        <label>Volume</label>
        <select name="volumeId" defaultValue="">
          <option value="">None</option>
          {volumes.map((v) => (
            <option key={v.id} value={v.id}>
              {v.title || `Vol. ${v.volumeNumber}`}
            </option>
          ))}
        </select>
      </div>
      <div
        className="dropzone dropzone--compact"
        onDragOver={(ev) => ev.preventDefault()}
        onDrop={(ev) => {
          ev.preventDefault();
          setFiles(Array.from(ev.dataTransfer.files));
        }}
      >
        <p>Drop pages or choose files.</p>
        <input type="file" accept="image/*" multiple onChange={(ev) => setFiles(Array.from(ev.target.files ?? []))} />
        {files.length > 0 ? <p>{files.length} file(s)</p> : null}
      </div>
      {error ? <p className="error">{error}</p> : null}
      <button className="btn btn--small" type="submit" disabled={pending}>
        {pending ? "Uploading…" : "Add chapter"}
      </button>
    </form>
  );
}
