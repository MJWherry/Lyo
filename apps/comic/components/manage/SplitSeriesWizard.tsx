"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import type { ComicChapter, ComicSeriesReq, ComicSeriesRes, ComicStatusValue, ComicTypeValue, ComicVolumeRes } from "lyo-comic-api-client";
import { ChipInput } from "lyo-query-components";
import { bffFetch } from "@/lib/api/bffFetch";

const TYPES: ComicTypeValue[] = ["manga", "manhwa", "manhua", "webtoon", "western"];
const STATUSES: ComicStatusValue[] = ["ongoing", "completed", "hiatus", "cancelled"];
const STEPS = ["New series", "Choose content", "Review"] as const;

function uniqueSlug(slug: string): string {
  const base = slug.trim().replace(/-split(-\d+)?$/, "");
  return `${base || "series"}-split`;
}

export function SplitSeriesWizard({
  series,
  volumes,
  chapters,
}: {
  series: ComicSeriesRes;
  volumes: ComicVolumeRes[];
  chapters: ComicChapter[];
}) {
  const router = useRouter();
  const sortedVolumes = useMemo(
    () => [...volumes].sort((a, b) => Number(a.volumeNumber ?? 0) - Number(b.volumeNumber ?? 0)),
    [volumes]
  );
  const sortedChapters = useMemo(
    () => [...chapters].sort((a, b) => Number(a.chapterNumber) - Number(b.chapterNumber)),
    [chapters]
  );
  const unassigned = sortedChapters.filter((c) => !c.volumeId);

  const [step, setStep] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const [title, setTitle] = useState(`${series.title} (split)`);
  const [slug, setSlug] = useState(uniqueSlug(series.slug));
  const [comicType, setComicType] = useState(String(series.comicType ?? "manga"));
  const [status, setStatus] = useState(String(series.status ?? "ongoing"));
  const [language, setLanguage] = useState(series.language ?? "");
  const [author, setAuthor] = useState(series.author ?? "");
  const [artist, setArtist] = useState(series.artist ?? "");
  const [description, setDescription] = useState(series.description ?? "");
  const [tags, setTags] = useState<string[]>([...(series.tags ?? [])]);
  const [copyCover, setCopyCover] = useState(true);
  const [selected, setSelected] = useState<Set<string>>(new Set());

  function chaptersForVolume(volumeId: string) {
    return sortedChapters.filter((c) => c.volumeId === volumeId);
  }

  function toggleChapter(id: string) {
    setSelected((cur) => {
      const next = new Set(cur);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  function toggleVolume(volumeId: string) {
    const ids = chaptersForVolume(volumeId).map((c) => c.id);
    setSelected((cur) => {
      const next = new Set(cur);
      const allOn = ids.length > 0 && ids.every((id) => next.has(id));
      for (const id of ids) {
        if (allOn) next.delete(id);
        else next.add(id);
      }
      return next;
    });
  }

  const moving = sortedChapters.filter((c) => selected.has(c.id));
  const pageCount = moving.reduce((sum, c) => sum + (Number(c.pageCount) || 0), 0);
  const volumeCount = new Set(moving.map((c) => c.volumeId).filter(Boolean)).size;

  async function submit() {
    setPending(true);
    setError(null);
    const payload: ComicSeriesReq = {
      title: title.trim(),
      slug: slug.trim(),
      comicType: comicType as ComicTypeValue,
      status: status as ComicStatusValue,
      description: description || null,
      language: language || null,
      publishedYear: series.publishedYear ?? null,
      author: author || null,
      artist: artist || null,
      demographic: series.demographic ?? null,
      publisher: series.publisher ?? null,
      source: series.source ?? null,
      alternateTitles: (series.alternateTitles ?? []).map((a) => ({ title: a.title, language: a.language ?? null })),
    };
    const res = await bffFetch("/api/manage/series/split", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        sourceSeriesId: series.id,
        series: payload,
        copyCover,
        tags,
        chapterIds: [...selected],
      }),
    });
    const data = (await res.json()) as { error?: string; series?: ComicSeriesRes };
    setPending(false);
    if (!res.ok || !data.series?.id) {
      setError(typeof data.error === "string" ? data.error : "Split failed");
      return;
    }
    router.push(`/manage/series/${data.series.id}`);
    router.refresh();
  }

  return (
    <div className="wizard">
      <p>
        <Link href={`/manage/series/${series.id}`}>← Back to {series.title}</Link>
      </p>
      <h1>Split series</h1>
      <p className="muted">
        Create a new series from this one and move selected volumes/chapters (pages stay with their chapter). Metadata is cloned so you can edit it first.
      </p>
      <ol className="stepper">
        {STEPS.map((label, i) => (
          <li key={label} className={i === step ? "stepper__step stepper__step--on" : i < step ? "stepper__step stepper__step--done" : "stepper__step"}>
            <span>{i + 1}</span> {label}
          </li>
        ))}
      </ol>

      {step === 0 ? (
        <div className="form-grid">
          <div className="field">
            <label htmlFor="split-title">Title</label>
            <input id="split-title" value={title} onChange={(e) => setTitle(e.target.value)} required />
          </div>
          <div className="field">
            <label htmlFor="split-slug">Slug</label>
            <input id="split-slug" value={slug} onChange={(e) => setSlug(e.target.value)} required />
          </div>
          <div className="field">
            <label htmlFor="split-type">Type</label>
            <select id="split-type" value={comicType} onChange={(e) => setComicType(e.target.value)}>
              {TYPES.map((t) => (
                <option key={String(t)} value={t}>
                  {t}
                </option>
              ))}
            </select>
          </div>
          <div className="field">
            <label htmlFor="split-status">Status</label>
            <select id="split-status" value={status} onChange={(e) => setStatus(e.target.value)}>
              {STATUSES.map((s) => (
                <option key={String(s)} value={s}>
                  {s}
                </option>
              ))}
            </select>
          </div>
          <div className="field">
            <label htmlFor="split-lang">Language</label>
            <input id="split-lang" value={language} onChange={(e) => setLanguage(e.target.value)} />
          </div>
          <div className="field">
            <label htmlFor="split-author">Author</label>
            <input id="split-author" value={author} onChange={(e) => setAuthor(e.target.value)} />
          </div>
          <div className="field">
            <label htmlFor="split-artist">Artist</label>
            <input id="split-artist" value={artist} onChange={(e) => setArtist(e.target.value)} />
          </div>
          <div className="field" style={{ gridColumn: "1 / -1" }}>
            <label htmlFor="split-desc">Synopsis</label>
            <textarea id="split-desc" rows={4} value={description} onChange={(e) => setDescription(e.target.value)} />
          </div>
          <div className="field" style={{ gridColumn: "1 / -1" }}>
            <label>Tags</label>
            <ChipInput values={tags} onChange={setTags} placeholder="Type a tag and press Enter" />
          </div>
          <label className="wizard-check">
            <input type="checkbox" checked={copyCover} onChange={(e) => setCopyCover(e.target.checked)} />
            Copy cover image
          </label>
        </div>
      ) : null}

      {step === 1 ? (
        <div className="pick-tree">
          <p className="muted">Select volumes or individual chapters to move. Pages move with the chapter.</p>
          {sortedVolumes.map((v) => {
            const volCh = chaptersForVolume(v.id);
            const selectedCount = volCh.filter((c) => selected.has(c.id)).length;
            const allOn = volCh.length > 0 && selectedCount === volCh.length;
            return (
              <details key={v.id} className="pick-group" open>
                <summary>
                  <label onClick={(e) => e.stopPropagation()}>
                    <input type="checkbox" checked={allOn} onChange={() => toggleVolume(v.id)} />
                    {v.title || `Vol. ${v.volumeNumber}`}{" "}
                    <span className="muted">
                      {selectedCount}/{volCh.length}
                    </span>
                  </label>
                </summary>
                <ul>
                  {volCh.map((ch) => (
                    <li key={ch.id}>
                      <label>
                        <input type="checkbox" checked={selected.has(ch.id)} onChange={() => toggleChapter(ch.id)} />
                        Ch. {ch.chapterNumber}
                        {ch.title ? ` · ${ch.title}` : ""}
                        <span className="muted"> · {ch.pageCount ?? 0}p</span>
                      </label>
                    </li>
                  ))}
                </ul>
              </details>
            );
          })}
          {unassigned.length > 0 ? (
            <details className="pick-group" open>
              <summary>
                Unassigned <span className="muted">{unassigned.filter((c) => selected.has(c.id)).length}/{unassigned.length}</span>
              </summary>
              <ul>
                {unassigned.map((ch) => (
                  <li key={ch.id}>
                    <label>
                      <input type="checkbox" checked={selected.has(ch.id)} onChange={() => toggleChapter(ch.id)} />
                      Ch. {ch.chapterNumber}
                      {ch.title ? ` · ${ch.title}` : ""}
                      <span className="muted"> · {ch.pageCount ?? 0}p</span>
                    </label>
                  </li>
                ))}
              </ul>
            </details>
          ) : null}
        </div>
      ) : null}

      {step === 2 ? (
        <div className="wizard-review">
          <h2>New series</h2>
          <p>
            <strong>{title}</strong> <span className="muted">/{slug}</span>
          </p>
          <p className="muted">
            {comicType} · {status}
            {author ? ` · ${author}` : ""}
            {copyCover ? " · cover copied" : ""}
          </p>
          {tags.length > 0 ? <p className="muted">Tags: {tags.join(", ")}</p> : null}
          <h2>Moving</h2>
          <p>
            {moving.length} chapter{moving.length === 1 ? "" : "s"} · {pageCount} page{pageCount === 1 ? "" : "s"}
            {volumeCount ? ` · ${volumeCount} volume${volumeCount === 1 ? "" : "s"} recreated` : ""}
          </p>
          <ul className="manage-list">
            {moving.map((ch) => (
              <li key={ch.id} className="manage-list__row">
                <span>
                  Ch. {ch.chapterNumber}
                  {ch.title ? ` · ${ch.title}` : ""}
                </span>
                <span className="muted">{ch.pageCount ?? 0}p</span>
              </li>
            ))}
          </ul>
          <p className="muted">The original series keeps everything you did not select. Page image files are shared, not duplicated.</p>
        </div>
      ) : null}

      {error ? <p className="error">{error}</p> : null}
      <div className="wizard-nav">
        {step > 0 ? (
          <button type="button" className="btn btn--ghost" disabled={pending} onClick={() => setStep((s) => s - 1)}>
            Back
          </button>
        ) : (
          <span />
        )}
        {step < 2 ? (
          <button
            type="button"
            className="btn"
            disabled={step === 0 ? !title.trim() || !slug.trim() : selected.size === 0}
            onClick={() => {
              setError(null);
              setStep((s) => s + 1);
            }}
          >
            Next
          </button>
        ) : (
          <button type="button" className="btn" disabled={pending || selected.size === 0} onClick={() => void submit()}>
            {pending ? "Splitting…" : "Create series and move"}
          </button>
        )}
      </div>
    </div>
  );
}
