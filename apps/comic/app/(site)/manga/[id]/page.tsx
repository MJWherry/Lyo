import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { comicFileUrl, comicStatusLabel, comicTypeLabel } from "lyo-comic-api-client";
import { getComicApi } from "@/lib/api/serverClient";
import { isComicGuid, seriesHref, volumeHref } from "@/lib/comic/cards";
import { ArchiveDownloadLink } from "@/components/ArchiveDownloadLink";
import { ChapterList } from "@/components/ChapterList";

export const dynamic = "force-dynamic";

export default async function MangaPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const comic = await getComicApi();
  let series;
  try {
    series = isComicGuid(id)
      ? (await comic.getSeries(id)).data
      : (await comic.getSeriesBySlug(id)).data;
  } catch {
    notFound();
  }
  if (!series) notFound();
  if (!isComicGuid(id))
    redirect(seriesHref(series.id));

  const [volumesRes, chaptersRes] = await Promise.all([
    comic.getSeriesVolumes(series.id),
    comic.getSeriesChapters(series.id),
  ]);
  const volumes = volumesRes.data ?? [];
  const chapters = [...(chaptersRes.data ?? [])].sort((a, b) => Number(b.chapterNumber) - Number(a.chapterNumber));
  const cover = comicFileUrl(series.coverImageRef, series.updatedTimestamp);

  return (
    <div className="shell">
      <div className="hero">
        {cover ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={cover} alt="" />
        ) : (
          <div className="cover-ph" />
        )}
        <div>
          <div className="chip-row" style={{ marginBottom: "0.6rem" }}>
            <span className="pill">{comicTypeLabel(series.comicType)}</span>
            <span className="pill">{comicStatusLabel(series.status)}</span>
            {series.language ? <span className="pill">{series.language}</span> : null}
          </div>
          <h1>{series.title}</h1>
          {series.author ? <p className="muted">{[series.author, series.artist].filter(Boolean).join(" · ")}</p> : null}
          {series.description ? <p>{series.description}</p> : null}
          {series.tags && series.tags.length > 0 ? (
            <div className="chip-row">
              {series.tags.map((t) => (
                <span key={t} className="chip">
                  {t}
                </span>
              ))}
            </div>
          ) : null}
          <p style={{ marginTop: "1rem" }} className="chip-row">
            <Link className="btn" href={`/manage/series/${series.id}`}>
              Edit in library
            </Link>
            <ArchiveDownloadLink href={`/api/comic/series/${encodeURIComponent(series.id)}/archive`} className="btn btn--ghost" label="Download series">
              Download series
            </ArchiveDownloadLink>
          </p>
        </div>
      </div>

      {volumes.length > 0 ? (
        <>
          <h2 style={{ marginTop: "2rem" }}>Volumes</h2>
          <div className="volume-grid">
            {volumes.map((v) => (
              <Link key={v.id} href={volumeHref(series.id, v.id)} className="series-card">
                <div className="series-card__cover">
                  {comicFileUrl(v.coverImageRef, v.updatedTimestamp) ? (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img src={comicFileUrl(v.coverImageRef, v.updatedTimestamp)!} alt="" />
                  ) : (
                    <div className="series-card__placeholder" />
                  )}
                </div>
                <div className="series-card__meta">
                  <h3>{v.title || `Vol. ${v.volumeNumber ?? "?"}`}</h3>
                  <p className="series-card__counts">
                    {chapters.filter((c) => c.volumeId === v.id).length} ch
                  </p>
                </div>
              </Link>
            ))}
          </div>
        </>
      ) : null}

      <h2 style={{ marginTop: "2rem" }}>Chapters</h2>
      <ChapterList seriesId={series.id} chapters={chapters} />
    </div>
  );
}
