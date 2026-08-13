"use client";

import Link from "next/link";
import { memo, useCallback, useEffect, useId, useMemo, useRef, useState, type MutableRefObject, type RefObject } from "react";
import type { ComicChapter, ComicPage, ComicSeriesRes, ComicTypeValue, ComicVolumeRes } from "lyo-comic-api-client";
import { comicFileUrl, isVerticalDefault } from "lyo-comic-api-client";
import { saveProgress } from "@/lib/reading/progress";
import { bffFetch } from "@/lib/api/bffFetch";
import { readHref } from "@/lib/comic/cards";
import { parsePageList } from "@/lib/comic/pageRefs";

type Mode = "paged" | "vertical";

const LOAD_DEBOUNCE_MS = 180;
const SCROLL_DEBOUNCE_MS = 200;

type Props = {
  series: ComicSeriesRes;
  chapters: ComicChapter[];
  volumes?: ComicVolumeRes[];
  initialChapterId?: string;
  initialPage?: number;
};

export function ComicReader({ series, chapters, volumes = [], initialChapterId, initialPage = 1 }: Props) {
  const sorted = useMemo(
    () => [...chapters].sort((a, b) => Number(a.chapterNumber) - Number(b.chapterNumber)),
    [chapters]
  );
  const sortedVolumes = useMemo(
    () => [...volumes].sort((a, b) => Number(a.volumeNumber ?? 0) - Number(b.volumeNumber ?? 0)),
    [volumes]
  );
  const [chapterId, setChapterId] = useState(initialChapterId || sorted[0]?.id || "");
  const [pages, setPages] = useState<ComicPage[]>([]);
  const [page, setPage] = useState(initialPage);
  const [overlay, setOverlay] = useState(true);
  const [mode, setMode] = useState<Mode>(isVerticalDefault(series.comicType as ComicTypeValue) ? "vertical" : "paged");
  const urlMap = useRef<Record<number, string>>({});
  const viewerRef = useRef<HTMLDivElement>(null);
  const stackRef = useRef<HTMLDivElement>(null);
  const pendingPage = useRef<"start" | "end" | null>(null);
  const sliderDragging = useRef(false);
  const applyingScroll = useRef(false);
  const loadTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const scrollTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const pageLabelRef = useRef<HTMLSpanElement>(null);
  const goPageRef = useRef<(delta: number) => void>(() => undefined);
  const chapter = sorted.find((c) => c.id === chapterId) ?? sorted[0];
  const chapterIndex = sorted.findIndex((c) => c.id === chapterId);
  const volumeId = chapter?.volumeId ?? "";
  const unassigned = sorted.filter((c) => !c.volumeId);
  const volumeChapters = sorted.filter((c) => (c.volumeId ?? "") === volumeId);
  const showVolumeSelect = sortedVolumes.length > 0;
  const total = pages.length;
  const committedPage = Math.min(Math.max(1, Number.isFinite(page) ? page : 1), total || 1);
  const hasPrev = committedPage > 1 || chapterIndex > 0;
  const hasNext = (total > 0 && committedPage < total) || (chapterIndex >= 0 && chapterIndex < sorted.length - 1);

  const persist = useCallback(
    (nextPage: number, nextChapter = chapter) => {
      if (!nextChapter) return;
      saveProgress({
        seriesId: series.id,
        seriesSlug: series.slug,
        seriesTitle: series.title,
        coverImageRef: series.coverImageRef,
        chapterId: nextChapter.id,
        chapterNumber: Number(nextChapter.chapterNumber),
        page: nextPage,
        mode,
        updatedAt: Date.now(),
      });
    },
    [chapter, mode, series]
  );

  const prefetch = useCallback((center: number, radius: number) => {
    for (let i = center - radius; i <= center + radius; i++) {
      const url = urlMap.current[i];
      if (!url) continue;
      const img = new Image();
      img.src = url;
    }
  }, []);

  useEffect(() => {
    if (!chapterId) return;
    let cancelled = false;
    (async () => {
      const res = await bffFetch(`/api/comic/chapters/${encodeURIComponent(chapterId)}/pages`);
      if (cancelled) return;
      const list = [...parsePageList(await res.json())].sort((a, b) => a.pageNumber - b.pageNumber);
      setPages(list);
      urlMap.current = {};
      for (const p of list) {
        if (p.imageRef) urlMap.current[p.pageNumber] = comicFileUrl(p.imageRef)!;
      }
      setPage((cur) => {
        const intent = pendingPage.current;
        pendingPage.current = null;
        if (list.length === 0) return 1;
        if (intent === "end") return list.length;
        if (intent === "start") return 1;
        if (!Number.isFinite(cur) || cur < 1) return 1;
        return Math.min(cur, list.length);
      });
    })();
    return () => {
      cancelled = true;
    };
  }, [chapterId]);

  const src = urlMap.current[committedPage] ?? urlMap.current[pages[committedPage - 1]?.pageNumber ?? committedPage];

  useEffect(() => {
    if (pendingPage.current || !Number.isFinite(page) || page < 1 || page > 10_000) return;
    persist(page);
  }, [page, persist]);

  useEffect(() => {
    if (!chapterId) return;
    if (pendingPage.current || !Number.isFinite(page) || page < 1 || page > 10_000) return;
    const href = readHref(series.id, chapterId, page);
    window.history.replaceState(window.history.state, "", href);
  }, [series.id, chapterId, page]);

  useEffect(() => {
    prefetch(committedPage, 5);
  }, [committedPage, pages, prefetch]);

  const scrollToPage = useCallback((n: number) => {
    const el = stackRef.current?.querySelector(`[data-page="${n}"]`);
    if (!el) return;
    applyingScroll.current = true;
    el.scrollIntoView({ block: "start" });
    requestAnimationFrame(() => {
      applyingScroll.current = false;
    });
  }, []);

  const loadPage = useCallback(
    (n: number) => {
      const clamped = Math.min(Math.max(1, Math.round(n)), total || 1);
      setPage((cur) => (cur === clamped ? cur : clamped));
      prefetch(clamped, 8);
      if (mode === "vertical" && !sliderDragging.current) scrollToPage(clamped);
    },
    [mode, prefetch, scrollToPage, total]
  );

  const goToPage = useCallback(
    (n: number) => {
      sliderDragging.current = false;
      if (pageLabelRef.current) pageLabelRef.current.textContent = `${total ? n : 0}/${total}`;
      loadPage(n);
    },
    [loadPage, total]
  );

  useEffect(() => {
    return () => {
      if (loadTimer.current) clearTimeout(loadTimer.current);
      if (scrollTimer.current) clearTimeout(scrollTimer.current);
    };
  }, []);

  useEffect(() => {
    if (mode !== "vertical" || pages.length === 0) return;
    const start = Math.min(Math.max(1, page), pages.length);
    requestAnimationFrame(() => scrollToPage(start));
    // Jump when the chapter, page list, or mode changes — not on every scroll-driven page update.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [mode, pages, chapterId, scrollToPage]);

  useEffect(() => {
    if (mode !== "vertical") return;
    const root = stackRef.current;
    if (!root) return;
    const imgs = [...root.querySelectorAll<HTMLElement>("[data-page]")];
    if (imgs.length === 0) return;

    const obs = new IntersectionObserver(
      (entries) => {
        if (applyingScroll.current || sliderDragging.current) return;
        let best: { page: number; ratio: number } | null = null;
        for (const entry of entries) {
          const n = Number(entry.target.getAttribute("data-page"));
          if (!entry.isIntersecting || !Number.isFinite(n)) continue;
          if (!best || entry.intersectionRatio > best.ratio) best = { page: n, ratio: entry.intersectionRatio };
        }
        if (!best) return;
        const next = best.page;
        if (scrollTimer.current) clearTimeout(scrollTimer.current);
        scrollTimer.current = setTimeout(() => {
          scrollTimer.current = null;
          setPage((cur) => (cur === next ? cur : next));
        }, SCROLL_DEBOUNCE_MS);
      },
      { root, threshold: [0.2, 0.4, 0.6, 0.8] }
    );
    for (const img of imgs) obs.observe(img);
    return () => obs.disconnect();
  }, [mode, pages]);

  const goPage = useCallback(
    (delta: number) => {
      if (pendingPage.current) return;
      if (delta > 0) {
        if (page < total) {
          goToPage(page + 1);
          return;
        }
        if (chapterIndex >= 0 && chapterIndex < sorted.length - 1) {
          pendingPage.current = "start";
          setPage(1);
          setChapterId(sorted[chapterIndex + 1].id);
        }
        return;
      }
      if (page > 1 && page <= (total || 1)) {
        goToPage(page - 1);
        return;
      }
      if (chapterIndex > 0) {
        const prev = sorted[chapterIndex - 1];
        pendingPage.current = "end";
        const count = Number(prev.pageCount);
        setPage(Number.isFinite(count) && count >= 1 && count <= 10_000 ? count : 1);
        setChapterId(prev.id);
      }
    },
    [page, total, chapterIndex, sorted, goToPage]
  );

  goPageRef.current = goPage;

  useEffect(() => {
    const el = viewerRef.current;
    if (!el) return;

    const onKey = (e: KeyboardEvent) => {
      if (mode !== "paged") return;
      if (e.key === "ArrowRight") goPageRef.current(1);
      else if (e.key === "ArrowLeft") goPageRef.current(-1);
    };

    let startX = 0;
    let startY = 0;
    let startT = 0;
    const onStart = (e: TouchEvent) => {
      const t = e.touches[0];
      startX = t.clientX;
      startY = t.clientY;
      startT = Date.now();
    };
    const onEnd = (e: TouchEvent) => {
      if (mode !== "paged") return;
      const t = e.changedTouches[0];
      const dx = t.clientX - startX;
      const dy = t.clientY - startY;
      const dt = Date.now() - startT;
      if (Math.abs(dx) >= 50 && Math.abs(dy) <= 150) {
        goPageRef.current(dx > 0 ? -1 : 1);
        return;
      }
      if (Math.abs(dx) <= 10 && Math.abs(dy) <= 10 && dt <= 300) {
        const rel = t.clientX / el.offsetWidth;
        if (rel < 0.35) goPageRef.current(-1);
        else if (rel > 0.65) goPageRef.current(1);
        else setOverlay((o) => !o);
      }
    };

    document.addEventListener("keydown", onKey);
    el.addEventListener("touchstart", onStart, { passive: true });
    el.addEventListener("touchend", onEnd, { passive: false });
    const active = document.activeElement;
    if (active === document.body || active === el || active == null) el.focus({ preventScroll: true });
    return () => {
      document.removeEventListener("keydown", onKey);
      el.removeEventListener("touchstart", onStart);
      el.removeEventListener("touchend", onEnd);
    };
  }, [mode]);

  const overlayClass = overlay ? "comic-viewer__overlay--visible" : "comic-viewer__overlay--hidden";
  const displayTotal = total || 1;

  return (
    <div
      ref={viewerRef}
      className={mode === "vertical" ? "comic-viewer comic-viewer--vertical" : "comic-viewer"}
      tabIndex={0}
    >
      {mode === "paged" ? (
        <>
          <div className="comic-viewer__page-area">
            {src ? (
              // eslint-disable-next-line @next/next/no-img-element
              <img className="comic-viewer__page-img" src={src} alt={`Page ${committedPage}`} />
            ) : (
              <p className="muted">No page image</p>
            )}
          </div>
          <div
            className={`comic-viewer__tap-zone comic-viewer__tap-zone--left${hasPrev ? "" : " comic-viewer__tap-zone--disabled"}`}
            onClick={() => hasPrev && goPage(-1)}
          >
            {hasPrev ? <div className="comic-viewer__tap-arrow comic-viewer__tap-arrow--left">‹</div> : null}
          </div>
          <div className="comic-viewer__tap-zone comic-viewer__tap-zone--center" onClick={() => setOverlay((o) => !o)} />
          <div
            className={`comic-viewer__tap-zone comic-viewer__tap-zone--right${hasNext ? "" : " comic-viewer__tap-zone--disabled"}`}
            onClick={() => hasNext && goPage(1)}
          >
            {hasNext ? <div className="comic-viewer__tap-arrow comic-viewer__tap-arrow--right">›</div> : null}
          </div>
        </>
      ) : (
        <div ref={stackRef} className="comic-viewer__stack">
          {pages.map((p) => {
            const url = p.imageRef ? comicFileUrl(p.imageRef) : null;
            return url ? (
              // eslint-disable-next-line @next/next/no-img-element
              <img key={p.id} data-page={p.pageNumber} src={url} alt={`Page ${p.pageNumber}`} />
            ) : null;
          })}
          {hasNext && chapterIndex >= 0 && chapterIndex < sorted.length - 1 ? (
            <p style={{ textAlign: "center", padding: "1.5rem" }}>
              <button
                className="btn"
                type="button"
                onClick={() => {
                  pendingPage.current = "start";
                  setPage(1);
                  setChapterId(sorted[chapterIndex + 1].id);
                }}
              >
                Next chapter
              </button>
            </p>
          ) : null}
        </div>
      )}

      <div className={`comic-viewer__top-bar ${overlayClass}`} onPointerDown={(e) => e.stopPropagation()}>
        <div className="comic-viewer__top-inner">
          <Link href={`/manga/${encodeURIComponent(series.slug)}`}>←</Link>
          <span className="pill">{String(series.comicType)}</span>
          {showVolumeSelect ? (
            <select
              aria-label="Volume"
              value={volumeId}
              onChange={(e) => {
                const nextVolume = e.target.value;
                const first = sorted.find((c) => (c.volumeId ?? "") === nextVolume);
                if (!first) return;
                pendingPage.current = "start";
                setChapterId(first.id);
                setPage(1);
              }}
            >
              {sortedVolumes.map((v) => {
                const n = sorted.filter((c) => c.volumeId === v.id).length;
                return (
                  <option key={v.id} value={v.id} disabled={n === 0}>
                    {v.title || `Vol. ${v.volumeNumber ?? "?"}`} ({n} ch)
                  </option>
                );
              })}
              {unassigned.length > 0 ? <option value="">Unassigned ({unassigned.length} ch)</option> : null}
            </select>
          ) : null}
          <select
            aria-label="Chapter"
            value={chapterId}
            onChange={(e) => {
              pendingPage.current = "start";
              setChapterId(e.target.value);
              setPage(1);
            }}
          >
            {(showVolumeSelect ? volumeChapters : sorted).map((c) => (
              <option key={c.id} value={c.id}>
                Ch. {c.chapterNumber}
                {c.title ? ` · ${c.title}` : ""}
              </option>
            ))}
          </select>
          <PageCountLabel page={committedPage} total={total} draggingRef={sliderDragging} labelRef={pageLabelRef} />
          <button className="btn btn--ghost" type="button" onClick={() => setMode(mode === "paged" ? "vertical" : "paged")}>
            {mode === "paged" ? "Vertical" : "Paged"}
          </button>
        </div>
      </div>

      <div className={`comic-viewer__bottom-bar ${overlayClass}`} onPointerDown={(e) => e.stopPropagation()}>
        <div className="comic-viewer__bottom-inner">
          <button type="button" className="btn btn--ghost" onClick={() => goPage(-1)} disabled={!hasPrev}>
            Prev
          </button>
          <PageSlider
            page={committedPage}
            total={displayTotal}
            disabled={total <= 1}
            draggingRef={sliderDragging}
            onPreview={(n) => {
              if (pageLabelRef.current) pageLabelRef.current.textContent = `${total ? n : 0}/${total}`;
            }}
            onLoad={loadPage}
          />
          <button type="button" className="btn btn--ghost" onClick={() => goPage(1)} disabled={!hasNext}>
            Next
          </button>
        </div>
      </div>
    </div>
  );
}

const PageCountLabel = memo(
  function PageCountLabel({
    page,
    total,
    labelRef,
  }: {
    page: number;
    total: number;
    draggingRef: MutableRefObject<boolean>;
    labelRef: RefObject<HTMLSpanElement | null>;
  }) {
    return (
      <span ref={labelRef}>
        {total ? page : 0}/{total}
      </span>
    );
  },
  (prev, next) => next.draggingRef.current || (prev.page === next.page && prev.total === next.total)
);

const PageSlider = memo(
  function PageSlider({
    page,
    total,
    disabled,
    draggingRef,
    onPreview,
    onLoad,
  }: {
    page: number;
    total: number;
    disabled: boolean;
    draggingRef: MutableRefObject<boolean>;
    onPreview: (n: number) => void;
    onLoad: (n: number) => void;
  }) {
    const inputRef = useRef<HTMLInputElement>(null);
    const loadTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
    const tickId = useId();
    const ticks = Math.max(1, total);

    useEffect(() => {
      const el = inputRef.current;
      if (!el) return;
      el.max = String(ticks);
      if (!draggingRef.current) el.value = String(page);
    }, [page, ticks, draggingRef]);

    useEffect(
      () => () => {
        if (loadTimer.current) clearTimeout(loadTimer.current);
      },
      []
    );

    function readValue(target: EventTarget | null): number {
      return Number((target as HTMLInputElement | null)?.value ?? page);
    }

    function scheduleLoad(n: number) {
      if (loadTimer.current) clearTimeout(loadTimer.current);
      loadTimer.current = setTimeout(() => {
        loadTimer.current = null;
        onLoad(n);
      }, LOAD_DEBOUNCE_MS);
    }

    function flushLoad(n: number) {
      if (loadTimer.current) {
        clearTimeout(loadTimer.current);
        loadTimer.current = null;
      }
      draggingRef.current = false;
      onLoad(n);
    }

    return (
      <div className="comic-viewer__slider">
        <input
          ref={inputRef}
          type="range"
          min={1}
          max={ticks}
          step={1}
          defaultValue={page}
          disabled={disabled}
          list={tickId}
          aria-label="Page"
          onPointerDown={(e) => {
            draggingRef.current = true;
            e.currentTarget.focus({ preventScroll: true });
          }}
          onInput={(e) => {
            const n = readValue(e.target);
            onPreview(n);
            scheduleLoad(n);
          }}
          onPointerUp={(e) => flushLoad(readValue(e.target))}
          onPointerCancel={() => flushLoad(readValue(inputRef.current))}
          onKeyUp={(e) => {
            if (e.key === "ArrowLeft" || e.key === "ArrowRight" || e.key === "Home" || e.key === "End") {
              flushLoad(readValue(e.target));
            }
          }}
        />
        <div className="comic-viewer__ticks" aria-hidden>
          {Array.from({ length: ticks }, (_, i) => (
            <span key={i} className="comic-viewer__tick" />
          ))}
        </div>
        <datalist id={tickId}>
          {Array.from({ length: ticks }, (_, i) => (
            <option key={i} value={i + 1} />
          ))}
        </datalist>
      </div>
    );
  },
  (prev, next) => {
    if (next.draggingRef.current) return prev.total === next.total && prev.disabled === next.disabled;
    return prev.page === next.page && prev.total === next.total && prev.disabled === next.disabled;
  }
);
