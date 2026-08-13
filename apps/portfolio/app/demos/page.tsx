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
            <strong>Person query builder</strong>
            <span className="muted">
              Concrete, Projection, root Query, and Get — chip values for In/NotIn, BFF-fixed routes.
            </span>
            <div className="card-meta">
              <span className="badge badge-accent">Live</span>
              <span className="badge">lyo-query-components</span>
            </div>
          </Link>
          <Link href="/demos/datagrid" className="card">
            <strong>Person data grid</strong>
            <span className="muted">
              Root Query, Projection, and Concrete — search, filters, multi-sort, resizable columns.
            </span>
            <div className="card-meta">
              <span className="badge badge-accent">Live</span>
              <span className="badge">lyo-web-components</span>
            </div>
          </Link>
          <Link href="/demos/components" className="card">
            <strong>Component gallery</strong>
            <span className="muted">
              Buttons, form, upload, rich text, JSON editor, diff, and ID generator.
            </span>
            <div className="card-meta">
              <span className="badge">lyo-web-components</span>
            </div>
          </Link>
        </div>
      </section>
    </>
  );
}
