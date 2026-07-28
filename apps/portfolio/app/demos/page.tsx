import type { Metadata } from "next";
import Link from "next/link";
import { PageHero } from "@/components/PageHero";

export const metadata: Metadata = {
  title: "Demos",
};

export default function DemosPage() {
  return (
    <>
      <PageHero
        kicker="Live"
        title="Demos"
        description="Interactive runners against the private TestApi via the Next.js BFF. More demos will land here over time."
      />
      <section className="section shell">
        <div className="card-grid">
          <Link href="/demos/query" className="card">
            <strong>Person Query</strong>
            <span className="muted">
              Where-clause builder + QueryConcrete against seeded people data.
            </span>
            <div className="card-meta">
              <span className="badge badge-accent">Live</span>
              <span className="badge">lyo-person-api-client</span>
            </div>
          </Link>
        </div>
      </section>
    </>
  );
}
