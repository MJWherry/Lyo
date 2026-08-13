import { getComicApi } from "@/lib/api/serverClient";
import { SeriesCard } from "@/components/SeriesCard";
import { SectionHeader } from "@/components/SectionHeader";
import { ContinueReading } from "@/components/ContinueReading";
import { buildProjectionQuery } from "@/lib/search/buildQuery";
import { comicCardRowKey, normalizeComicCardRows } from "lyo-comic-api-client";

export const dynamic = "force-dynamic";

export default async function HomePage() {
  const comic = await getComicApi();
  const req = buildProjectionQuery("series", undefined, null, 0, 24);
  const latest = await comic.queryProjected("series", req);
  const items = normalizeComicCardRows(latest.data?.items);

  return (
    <div className="shell">
      <h1>Library</h1>
      <p className="muted">Recently updated series. Search from the header or open Browse for filters.</p>
      <ContinueReading />
      <SectionHeader title="Latest" href="/search" action="Browse" />
      <div className="card-grid">
        {items.map((row, i) => (
          <SeriesCard key={comicCardRowKey(row, i)} row={row} />
        ))}
      </div>
      {items.length === 0 ? <p className="muted">No series yet. Add some in Library.</p> : null}
    </div>
  );
}
