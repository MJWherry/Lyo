import { notFound } from "next/navigation";
import { getComicApi } from "@/lib/api/serverClient";
import { SeriesEditor } from "@/components/manage/SeriesEditor";

export const dynamic = "force-dynamic";

export default async function EditSeriesPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const comic = await getComicApi();
  let series;
  try {
    series = (await comic.getSeries(id)).data;
  } catch {
    notFound();
  }
  if (!series) notFound();
  const [volumes, chapters] = await Promise.all([
    comic.getSeriesVolumes(series.id),
    comic.getSeriesChapters(series.id),
  ]);

  return (
    <div className="shell shell--wide">
      <SeriesEditor series={series} volumes={volumes.data ?? []} chapters={chapters.data ?? []} />
    </div>
  );
}
