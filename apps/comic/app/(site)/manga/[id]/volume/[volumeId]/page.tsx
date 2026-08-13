import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { getComicApi } from "@/lib/api/serverClient";
import { isComicGuid, seriesHref, volumeHref } from "@/lib/comic/cards";
import { ChapterList } from "@/components/ChapterList";

export const dynamic = "force-dynamic";

export default async function VolumePage({
  params,
}: {
  params: Promise<{ id: string; volumeId: string }>;
}) {
  const { id, volumeId } = await params;
  const comic = await getComicApi();
  let volume;
  try {
    volume = (await comic.getVolume(volumeId)).data;
  } catch {
    notFound();
  }
  if (!volume) notFound();
  if (!isComicGuid(id) || id !== volume.seriesId)
    redirect(volumeHref(volume.seriesId, volume.id));

  const chapters = (await comic.getVolumeChapters(volumeId)).data ?? [];

  return (
    <div className="shell">
      <p>
        <Link href={seriesHref(volume.seriesId)}>← Series</Link>
      </p>
      <h1>{volume.title || `Volume ${volume.volumeNumber ?? ""}`}</h1>
      <ChapterList seriesId={volume.seriesId} chapters={chapters} />
    </div>
  );
}
