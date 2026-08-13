import { notFound } from "next/navigation";
import { getComicApi } from "@/lib/api/serverClient";
import { ComicReader } from "@/components/ComicReader";

export const dynamic = "force-dynamic";

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export default async function ReadPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ chapter?: string; page?: string }>;
}) {
  const { id } = await params;
  const sp = await searchParams;
  const comic = await getComicApi();
  let series;
  try {
    series = GUID.test(id)
      ? (await comic.getSeries(id)).data
      : (await comic.getSeriesBySlug(id)).data;
  } catch {
    notFound();
  }
  if (!series) notFound();
  const [chaptersRes, volumesRes] = await Promise.all([comic.getSeriesChapters(series.id), comic.getSeriesVolumes(series.id)]);
  const chapters = chaptersRes.data ?? [];
  const volumes = volumesRes.data ?? [];
  const initialPage = Math.max(1, Number(sp.page ?? 1) || 1);

  return (
    <ComicReader
      series={series}
      chapters={chapters}
      volumes={volumes}
      initialChapterId={sp.chapter}
      initialPage={initialPage}
    />
  );
}
