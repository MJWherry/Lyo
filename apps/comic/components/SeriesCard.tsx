import Link from "next/link";
import type { ComicCardRow } from "lyo-comic-api-client";
import { comicStatusLabel, comicTypeLabel } from "lyo-comic-api-client";
import { cardCoverSrc, cardHref, cardTitle } from "@/lib/comic/cards";

function countLabel(n: number | null | undefined, unit: string): string | null {
  if (n == null || !Number.isFinite(Number(n))) return null;
  return `${Number(n)} ${unit}`;
}

export function SeriesCard({ row, layout = "grid" }: { row: ComicCardRow; layout?: "grid" | "list" }) {
  const href = cardHref(row);
  const cover = cardCoverSrc(row);
  const title = cardTitle(row);
  const type = comicTypeLabel(row.comicType as never);
  const status = comicStatusLabel(row.status as never);
  const counts = [countLabel(row.volumeCount, "vol"), countLabel(row.chapterCount, "ch")].filter(Boolean);

  return (
    <Link href={href} className={layout === "list" ? "series-card series-card--list" : "series-card"}>
      <div className="series-card__cover">
        {cover ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={cover} alt="" />
        ) : (
          <div className="series-card__placeholder" />
        )}
      </div>
      <div className="series-card__meta">
        <h3>{title}</h3>
        <p>
          {type}
          {type && status ? " · " : ""}
          {status}
          {row.author ? ` · ${row.author}` : ""}
        </p>
        {counts.length > 0 ? <p className="series-card__counts">{counts.join(" · ")}</p> : null}
      </div>
    </Link>
  );
}
