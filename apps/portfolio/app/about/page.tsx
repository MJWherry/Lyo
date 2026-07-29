import type { Metadata } from "next";
import Link from "next/link";
import { PageHero } from "@/components/PageHero";
import site from "@/content/site.json";

export const metadata: Metadata = {
  title: "About",
  description: `About ${site.fullName} — ${site.brand}`,
};

export default function AboutPage() {
  const about = site.about;

  return (
    <>
      <PageHero
        kicker={site.location}
        title={site.fullName}
        description={about.headline}
      />
      <section className="section shell">
        <div className="panel about-panel">
          {about.paragraphs.map((p) => (
            <p key={p.slice(0, 48)} className="muted" style={{ fontSize: "1.05rem" }}>
              {p}
            </p>
          ))}
          <div className="cta-row" style={{ marginTop: "1.25rem" }}>
            <Link className="btn btn-primary" href={site.resumePath}>
              Resume
            </Link>
            <a className="btn btn-ghost" href={site.githubUrl} target="_blank" rel="noreferrer">
              GitHub
            </a>
            <Link className="btn btn-ghost" href="/benchmarks">
              Benchmarks
            </Link>
          </div>
        </div>
      </section>
    </>
  );
}
