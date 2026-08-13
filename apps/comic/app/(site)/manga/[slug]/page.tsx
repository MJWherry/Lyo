import Link from "next/link";
import { notFound } from "next/navigation";
import { comicFileUrl, comicStatusLabel, comicTypeLabel } from "lyo-comic-api-client";
import { getComicApi } from "@/lib/api/serverClient";
import { readHref } from "@/lib/comic/cards";

export const dynamic = "force-dynamic";

export default async function MangaPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const comic = await getComicApi();
  let series;
  try {
    series = (await comic.getSeriesBySlug(slug)).data;
  } catch {
    notFound();
  }
  if (!series) notFound();

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
          <p style={{ marginTop: "1rem" }}>
            <Link className="btn" href={`/manage/series/${series.id}`}>
              Edit in library
            </Link>
          </p>
        </div>
      </div>

      {volumes.length > 0 ? (
        <>
          <h2 style={{ marginTop: "2rem" }}>Volumes</h2>
          <div className="volume-grid">
            {volumes.map((v) => (
              <Link key={v.id} href={`/manga/${encodeURIComponent(slug)}/volume/${v.id}`} className="series-card">
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
      <div className="chapter-list">
        {chapters.map((ch) => (
          <Link key={ch.id} href={readHref(series.id, ch.id, 1)}>
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
