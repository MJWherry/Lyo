"use client";

import { useEffect, useMemo, useState } from "react";
import type { ComicChapter, ComicPage } from "lyo-comic-api-client";
import { comicFileUrl } from "lyo-comic-api-client";
import { fetchChapterPages } from "@/lib/comic/pageRefs";

export function ChapterPagePicker({
  open,
  chapters,
  initialChapterId,
  onClose,
  onSelect,
}: {
  open: boolean;
  chapters: ComicChapter[];
  initialChapterId?: string;
  onClose: () => void;
  onSelect: (imageRef: string) => void;
}) {
  const sorted = useMemo(
    () => [...chapters].sort((a, b) => Number(a.chapterNumber) - Number(b.chapterNumber)),
    [chapters]
  );
  const [chapterId, setChapterId] = useState(initialChapterId || sorted[0]?.id || "");
  const [pages, setPages] = useState<ComicPage[]>([]);
  const [pending, setPending] = useState(false);

  useEffect(() => {
    if (!open) return;
    setChapterId(initialChapterId || sorted[0]?.id || "");
    // Reset the chapter only when the dialog opens, not when page lists refresh.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  useEffect(() => {
    if (!open || !chapterId) {
      setPages([]);
      return;
    }
    let cancelled = false;
    setPending(true);
    void fetchChapterPages(chapterId).then((list) => {
      if (cancelled) return;
      setPages([...list].sort((a, b) => a.pageNumber - b.pageNumber));
      setPending(false);
    });
    return () => {
      cancelled = true;
    };
  }, [open, chapterId]);

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div className="dialog-backdrop" onClick={onClose} role="presentation">
      <div
        className="dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="page-picker-title"
        onClick={(e) => e.stopPropagation()}
        onKeyDown={(e) => {
          if (e.key === "Enter") e.stopPropagation();
        }}
      >
        <h2 id="page-picker-title">Use a chapter page</h2>
        {sorted.length === 0 ? (
          <p className="muted">Add chapters with pages first.</p>
        ) : (
          <>
            <div className="field">
              <label htmlFor="page-picker-chapter">Chapter</label>
              <select id="page-picker-chapter" value={chapterId} onChange={(e) => setChapterId(e.target.value)}>
                {sorted.map((c) => (
                  <option key={c.id} value={c.id}>
                    Ch. {c.chapterNumber}
                    {c.title ? ` · ${c.title}` : ""}
                    {c.pageCount != null ? ` · ${c.pageCount}p` : ""}
                  </option>
                ))}
              </select>
            </div>
            {pending ? <p className="muted">Loading pages…</p> : null}
            {!pending && pages.length === 0 ? <p className="muted">This chapter has no pages.</p> : null}
            <div className="page-thumbs">
              {pages.map((p) => {
                const src = comicFileUrl(p.imageRef);
                if (!src || !p.imageRef) return null;
                return (
                  <button
                    key={p.id}
                    type="button"
                    className="page-thumb page-thumb--pick"
                    onClick={() => onSelect(p.imageRef!)}
                  >
                    <div className="page-thumb__frame">
                      {/* eslint-disable-next-line @next/next/no-img-element */}
                      <img src={src} alt={`Page ${p.pageNumber}`} />
                    </div>
                    <p className="page-thumb__num">#{p.pageNumber}</p>
                  </button>
                );
              })}
            </div>
          </>
        )}
        <div className="wizard-nav">
          <button type="button" className="btn btn--ghost" onClick={onClose}>
            Cancel
          </button>
        </div>
      </div>
    </div>
  );
}
