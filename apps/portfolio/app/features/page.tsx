import type { Metadata } from "next";
import Link from "next/link";
import { PageHero } from "@/components/PageHero";
import { PackageCatalog } from "@/components/PackageCatalog";

export const metadata: Metadata = {
  title: "Features",
};

const deepDives = [
  {
    href: "/features/query",
    name: "API & Query",
    body: "EF Core wrapper: CRUD, caching, filters, projection, patch, bulk, export.",
  },
  {
    href: "/features/jobs",
    name: "Jobs",
    body: "Definitions, schedules, workers, MQ events, alerts, and SignalR UI.",
  },
  {
    href: "/features/reporting",
    name: "Reporting",
    body: "Definitions, multi-format generate, retention, and Blazor management.",
  },
  {
    href: "/features/file-storage",
    name: "File storage",
    body: "Local / S3 / Blob with compress, encrypt, staged and multipart upload.",
  },
  {
    href: "/features/file-system-watcher",
    name: "File system watcher",
    body: "Snapshot-based change detection, debounce, and hash move/rename.",
  },
  {
    href: "/features/encryption",
    name: "Encryption",
    body: "Authenticated envelopes, DEK/KEK, keystore integration.",
  },
  {
    href: "/features/compression",
    name: "Compression",
    body: "Ten codecs, streams, size limits, and bomb protections.",
  },
  {
    href: "/features/temp-io",
    name: "Temp IO",
    body: "Session-scoped scratch files with cleanup policies.",
  },
  {
    href: "/features/platform",
    name: "Platform",
    body: "Cache, locks, privacy, resilience, diagnostics, MQ, auth.",
  },
];

export default function FeaturesHubPage() {
  return (
    <>
      <PageHero
        kicker="Lyo.Net"
        title="Libraries by area"
        description="Deep dives for the capability surfaces, then every documented package grouped by taxonomy — Core, Data, Communication, Security, Integration, Features, Apps, and Tools."
      />
      <section className="section shell">
        <h2 style={{ fontSize: "1.35rem", marginBottom: "0.85rem" }}>Deep dives</h2>
        <div className="card-grid" style={{ marginBottom: "2.5rem" }}>
          {deepDives.map((f) => (
            <Link key={f.href} href={f.href} className="card">
              <strong>{f.name}</strong>
              <span className="muted">{f.body}</span>
            </Link>
          ))}
          <Link href="/demos/query" className="card">
            <strong>Live Query demo</strong>
            <span className="muted">Where-clause builder against Person QueryConcrete.</span>
            <div className="card-meta">
              <span className="badge badge-accent">Demo</span>
            </div>
          </Link>
        </div>

        <h2 style={{ fontSize: "1.35rem", marginBottom: "0.35rem" }}>All packages</h2>
        <p className="muted" style={{ marginBottom: "1.5rem" }}>
          Sourced from the monorepo README catalog. NuGet sizes appear when measured for that id.
        </p>
        <PackageCatalog />
      </section>
    </>
  );
}
