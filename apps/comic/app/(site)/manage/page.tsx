import Link from "next/link";
import { getComicApi } from "@/lib/api/serverClient";
import { buildProjectionQuery } from "@/lib/search/buildQuery";
import { normalizeComicCardRows } from "lyo-comic-api-client";
import { ManageSeriesTable } from "@/components/manage/ManageSeriesTable";

export const dynamic = "force-dynamic";

export default async function ManagePage() {
  const comic = await getComicApi();
  const req = buildProjectionQuery("series", undefined, null, 0, 48);
  const res = await comic.queryProjected("series", req);
  const items = normalizeComicCardRows(res.data?.items);

  return (
    <div className="shell">
      <div className="section-header">
        <h1>Library</h1>
        <Link className="btn" href="/manage/series/new">
          New series
        </Link>
      </div>
      <ManageSeriesTable initial={items} />
    </div>
  );
}
