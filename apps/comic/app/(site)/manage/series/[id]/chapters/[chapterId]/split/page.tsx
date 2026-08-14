import { notFound } from "next/navigation";
import { getComicApi } from "@/lib/api/serverClient";
import { SplitChapterWizard } from "@/components/manage/SplitChapterWizard";

export const dynamic = "force-dynamic";

export default async function SplitChapterPage({
  params,
}: {
  params: Promise<{ id: string; chapterId: string }>;
}) {
  const { id, chapterId } = await params;
  const comic = await getComicApi();
  let chapter;
  try {
    chapter = (await comic.getChapter(chapterId)).data;
  } catch {
    notFound();
  }
  if (!chapter) notFound();
  const [pages, volumes] = await Promise.all([comic.getChapterPages(chapterId), comic.getSeriesVolumes(id)]);

  return (
    <div className="shell">
      <SplitChapterWizard seriesId={id} chapter={chapter} pages={pages.data ?? []} volumes={volumes.data ?? []} />
    </div>
  );
}
