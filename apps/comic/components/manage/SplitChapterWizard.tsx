"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import type { ComicChapterReq, ComicChapterRes, ComicPage, ComicVolumeRes } from "lyo-comic-api-client";
import { comicFileUrl } from "lyo-comic-api-client";
import { bffFetch } from "@/lib/api/bffFetch";

const STEPS = ["New chapter", "Choose pages", "Review"] as const;

export function SplitChapterWizard({
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
  const sortedPages = useMemo(() => [...pages].sort((a, b) => a.pageNumber - b.pageNumber), [pages]);

  const [step, setStep] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const [chapterNumber, setChapterNumber] = useState(String(Math.round((Number(chapter.chapterNumber) + 0.5) * 100) / 100));
  const [title, setTitle] = useState(chapter.title ? `${chapter.title} (split)` : "");
  const [language, setLanguage] = useState(chapter.language || "en");
  const [volumeId, setVolumeId] = useState(chapter.volumeId ?? "");
  const [copyCover, setCopyCover] = useState(true);
  const [selected, setSelected] = useState<Set<string>>(new Set());

  const moving = sortedPages.filter((p) => selected.has(p.id));
  const remainingCount = sortedPages.length - moving.length;
  const numberOk = Number.isFinite(Number(chapterNumber));

  function togglePage(id: string) {
    setSelected((cur) => {
      const next = new Set(cur);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  function selectAll() {
    setSelected(new Set(sortedPages.map((p) => p.id)));
  }

  function selectNone() {
    setSelected(new Set());
  }

  function selectFrom(id: string) {
    const idx = sortedPages.findIndex((p) => p.id === id);
    if (idx < 0) return;
    setSelected(new Set(sortedPages.slice(idx).map((p) => p.id)));
  }

  async function submit() {
    setPending(true);
    setError(null);
    const payload: ComicChapterReq = {
      seriesId,
      volumeId: volumeId || null,
      chapterNumber: Number(chapterNumber),
      title: title.trim() || null,
      language: language.trim() || "en",
      publishedDate: chapter.publishedDate ?? null,
      source: chapter.source ?? null,
    };
    const res = await bffFetch("/api/manage/chapters/split", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        sourceChapterId: chapter.id,
        chapter: payload,
        copyCover,
        pageIds: [...selected],
      }),
    });
    const data = (await res.json()) as { error?: string; chapter?: ComicChapterRes };
    setPending(false);
    if (!res.ok || !data.chapter?.id) {
      setError(typeof data.error === "string" ? data.error : "Split failed");
      return;
    }
    router.push(`/manage/series/${seriesId}/chapters/${data.chapter.id}`);
    router.refresh();
  }

  return (
    <div className="wizard">
      <p>
        <Link href={`/manage/series/${seriesId}/chapters/${chapter.id}`}>
          ← Back to chapter {chapter.chapterNumber}
          {chapter.title ? ` · ${chapter.title}` : ""}
        </Link>
      </p>
      <h1>Split chapter</h1>
      <p className="muted">
        Create a new chapter from this one and move selected pages. Image files are shared, not duplicated. The original chapter keeps everything you did not select.
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
            <label htmlFor="split-ch-number">Number</label>
            <input id="split-ch-number" value={chapterNumber} onChange={(e) => setChapterNumber(e.target.value)} required inputMode="decimal" />
          </div>
          <div className="field">
            <label htmlFor="split-ch-title">Title</label>
            <input id="split-ch-title" value={title} onChange={(e) => setTitle(e.target.value)} />
          </div>
          <div className="field">
            <label htmlFor="split-ch-lang">Language</label>
            <input id="split-ch-lang" value={language} onChange={(e) => setLanguage(e.target.value)} required />
          </div>
          <div className="field">
            <label htmlFor="split-ch-volume">Volume</label>
            <select id="split-ch-volume" value={volumeId} onChange={(e) => setVolumeId(e.target.value)}>
              <option value="">None</option>
              {volumes.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.title || `Vol. ${v.volumeNumber}`}
                </option>
              ))}
            </select>
          </div>
          <label className="wizard-check">
            <input type="checkbox" checked={copyCover} onChange={(e) => setCopyCover(e.target.checked)} />
            Copy cover image
          </label>
        </div>
      ) : null}

      {step === 1 ? (
        <div>
          <p className="muted">Select pages to move into the new chapter. Remaining pages stay here and are renumbered.</p>
          <div className="pick-toolbar">
            <button type="button" className="btn btn--ghost" onClick={selectAll}>
              Select all
            </button>
            <button type="button" className="btn btn--ghost" onClick={selectNone}>
              Select none
            </button>
            <span className="muted">
              {selected.size}/{sortedPages.length}
            </span>
          </div>
          {sortedPages.length === 0 ? <p className="muted">This chapter has no pages.</p> : null}
          <div className="page-thumbs">
            {sortedPages.map((p) => {
              const src = comicFileUrl(p.imageRef);
              const on = selected.has(p.id);
              return (
                <div key={p.id} className={`page-thumb${on ? " page-thumb--selected" : ""}`}>
                  <label className="page-thumb--pick">
                    <input type="checkbox" checked={on} onChange={() => togglePage(p.id)} />
                    <div className="page-thumb__frame">
                      {src ? (
                        // eslint-disable-next-line @next/next/no-img-element
                        <img src={src} alt={`Page ${p.pageNumber}`} />
                      ) : (
                        <div className="page-thumb__placeholder" />
                      )}
                    </div>
                    <p className="page-thumb__num">#{p.pageNumber}</p>
                  </label>
                  <button type="button" className="page-thumb__from" onClick={() => selectFrom(p.id)}>
                    From here
                  </button>
                </div>
              );
            })}
          </div>
        </div>
      ) : null}

      {step === 2 ? (
        <div className="wizard-review">
          <h2>New chapter</h2>
          <p>
            <strong>Ch. {chapterNumber}</strong>
            {title.trim() ? ` · ${title.trim()}` : ""}
          </p>
          <p className="muted">
            {language || "en"}
            {volumeId ? ` · ${volumes.find((v) => v.id === volumeId)?.title || "volume"}` : " · no volume"}
            {copyCover ? " · cover copied" : ""}
          </p>
          <h2>Moving</h2>
          <p>
            {moving.length} page{moving.length === 1 ? "" : "s"}
            {remainingCount ? ` · ${remainingCount} stay on ch. ${chapter.chapterNumber}` : " · original chapter will be empty"}
          </p>
          <ul className="manage-list">
            {moving.map((p) => (
              <li key={p.id} className="manage-list__row">
                <span>Page {p.pageNumber}</span>
              </li>
            ))}
          </ul>
          <p className="muted">Page image files are shared, not duplicated.</p>
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
            disabled={step === 0 ? !numberOk || !language.trim() : selected.size === 0}
            onClick={() => {
              setError(null);
              setStep((s) => s + 1);
            }}
          >
            Next
          </button>
        ) : (
          <button type="button" className="btn" disabled={pending || selected.size === 0} onClick={() => void submit()}>
            {pending ? "Splitting…" : "Create chapter and move"}
          </button>
        )}
      </div>
    </div>
  );
}
