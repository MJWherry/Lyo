import { Suspense } from "react";
import { getComicApi } from "@/lib/api/serverClient";
import { SearchForm } from "@/components/SearchForm";

export const dynamic = "force-dynamic";

export default async function SearchPage({
  searchParams,
}: {
  searchParams: Promise<{ q?: string }>;
}) {
  const sp = await searchParams;
  const comic = await getComicApi();
  const tagsRes = await comic.getAllSeriesTags();
  const tags = tagsRes.data ?? [];

  return (
    <div className="shell">
      <h1>Browse</h1>
      <p className="muted">Simple filters plus an optional Query where clause. Lists use QueryProject.</p>
      <Suspense fallback={<p className="muted">Loading…</p>}>
        <SearchForm initialQ={sp.q ?? ""} tags={tags} />
      </Suspense>
    </div>
  );
}
