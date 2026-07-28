import type { Metadata } from "next";
import Link from "next/link";
import { PageHero } from "@/components/PageHero";
import { benchmarkRegistry } from "@/lib/benchmarks/registry";

export const metadata: Metadata = {
  title: "Benchmarks",
};

export default function BenchmarksPage() {
  return (
    <>
      <PageHero
        kicker="lyo.bench/v1"
        title="Every suite"
        description="Same reports as the in-repo dashboard — micro-benchmarks and k6 load tests, including encryption, compression, query, and query-api."
      />
      <section className="section shell">
        <div className="card-grid">
          {benchmarkRegistry.map((entry) => (
            <Link key={entry.name} href={`/benchmarks/${entry.name}`} className="card">
              <strong>{entry.title}</strong>
              <span className="muted">{entry.description}</span>
              <div className="card-meta">
                <span className={`badge ${entry.type === "load" ? "badge-accent" : ""}`}>
                  {entry.type === "load" ? "Load test" : "Micro-benchmark"}
                </span>
                <span className="badge">{entry.name}</span>
              </div>
            </Link>
          ))}
        </div>
      </section>
    </>
  );
}
