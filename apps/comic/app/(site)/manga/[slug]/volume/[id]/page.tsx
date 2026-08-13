import Link from "next/link";
import { notFound } from "next/navigation";
import { getComicApi } from "@/lib/api/serverClient";
import { readHref } from "@/lib/comic/cards";

export const dynamic = "force-dynamic";

export default async function VolumePage({
  params,
}: {
  params: Promise<{ slug: string; id: string }>;
}) {
  const { slug, id } = await params;
  const comic = await getComicApi();
  let volume;
  try {
    volume = (await comic.getVolume(id)).data;
  } catch {
    notFound();
  }
  if (!volume) notFound();
  const chapters = (await comic.getVolumeChapters(id)).data ?? [];

  return (
    <div className="shell">
      <p>
        <Link href={`/manga/${encodeURIComponent(slug)}`}>← Series</Link>
      </p>
      <h1>{volume.title || `Volume ${volume.volumeNumber ?? ""}`}</h1>
      <div className="chapter-list">
        {chapters.map((ch) => (
          <Link key={ch.id} href={readHref(volume.seriesId, ch.id, 1)}>
            <span>
              Ch. {ch.chapterNumber}
              {ch.title ? ` · ${ch.title}` : ""}
            </span>
            <span className="muted">{ch.language}</span>
          </Link>
        ))}
      </div>
    </div>
  );
}
