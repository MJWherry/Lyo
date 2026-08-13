"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { comicFileUrl } from "lyo-comic-api-client";
import { loadProgress, type ReadingProgress } from "@/lib/reading/progress";
import { readHref } from "@/lib/comic/cards";
import { SectionHeader } from "./SectionHeader";

export function ContinueReading() {
  const [items, setItems] = useState<ReadingProgress[]>([]);

  useEffect(() => {
    setItems(loadProgress());
  }, []);

  if (items.length === 0) return null;

  return (
    <>
      <SectionHeader title="Continue reading" />
      <div className="card-grid">
        {items.map((p) => {
          const href = readHref(p.seriesId, p.chapterId, p.page);
          const cover = comicFileUrl(p.coverImageRef, p.updatedAt);
          return (
            <Link key={p.seriesId} href={href} className="series-card">
              <div className="series-card__cover">
                {cover ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img src={cover} alt="" />
                ) : (
                  <div className="series-card__placeholder" />
                )}
              </div>
              <div className="series-card__meta">
                <h3>{p.seriesTitle}</h3>
                <p>
                  Ch. {p.chapterNumber} · p. {p.page}
                </p>
              </div>
            </Link>
          );
        })}
      </div>
    </>
  );
}
