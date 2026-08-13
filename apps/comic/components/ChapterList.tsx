"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import type { ComicChapter } from "lyo-comic-api-client";
import { ArchiveDownloadLink } from "@/components/ArchiveDownloadLink";
import { formatPageCount, readHref } from "@/lib/comic/cards";

export const CHAPTER_PAGE_SIZE = 20;

type Props = {
  seriesId: string;
  chapters: ComicChapter[];
};

export function ChapterPager({
  page,
  pageCount,
  onPage,
}: {
  page: number;
  pageCount: number;
  onPage: (page: number) => void;
}) {
  if (pageCount <= 1)
    return null;

  return (
    <nav className="pager" aria-label="Chapter list pages">
      <button type="button" className="btn btn--ghost" disabled={page <= 1} onClick={() => onPage(page - 1)}>
        Prev
      </button>
      <span className="pager__status">
        Page {page} of {pageCount}
      </span>
      <button type="button" className="btn btn--ghost" disabled={page >= pageCount} onClick={() => onPage(page + 1)}>
        Next
      </button>
    </nav>
  );
}

export function ChapterList({ seriesId, chapters }: Props) {
  const [page, setPage] = useState(1);
  const pageCount = Math.max(1, Math.ceil(chapters.length / CHAPTER_PAGE_SIZE));
  const current = Math.min(page, pageCount);
  const slice = useMemo(() => {
    const start = (current - 1) * CHAPTER_PAGE_SIZE;
    return chapters.slice(start, start + CHAPTER_PAGE_SIZE);
  }, [chapters, current]);

  return (
    <>
      <div className="chapter-list">
        {slice.map((ch) => (
          <div key={ch.id} className="chapter-list__row">
            <Link href={readHref(seriesId, ch.id, 1)}>
              <span>
                Ch. {ch.chapterNumber}
                {ch.title ? ` · ${ch.title}` : ""}
              </span>
              <span className="muted">{[ch.language, formatPageCount(ch.pageCount)].filter(Boolean).join(" · ")}</span>
            </Link>
            <ArchiveDownloadLink
              className="icon-btn"
              href={`/api/comic/chapters/${encodeURIComponent(ch.id)}/archive`}
              label={`Download chapter ${ch.chapterNumber}`}
            />
          </div>
        ))}
      </div>
      <ChapterPager page={current} pageCount={pageCount} onPage={setPage} />
    </>
  );
}
