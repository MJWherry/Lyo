import { notFound } from "next/navigation";
import { getComicApi } from "@/lib/api/serverClient";
import { SplitSeriesWizard } from "@/components/manage/SplitSeriesWizard";

export const dynamic = "force-dynamic";

export default async function SplitSeriesPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const comic = await getComicApi();
  let series;
  try {
    series = (await comic.getSeries(id)).data;
  } catch {
    notFound();
  }
  if (!series) notFound();
  const [volumes, chapters] = await Promise.all([comic.getSeriesVolumes(series.id), comic.getSeriesChapters(series.id)]);

  return (
    <div className="shell">
      <SplitSeriesWizard series={series} volumes={volumes.data ?? []} chapters={chapters.data ?? []} />
    </div>
  );
}
