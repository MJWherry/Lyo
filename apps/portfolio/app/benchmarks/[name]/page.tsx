import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { Suspense } from "react";
import { PageHero } from "@/components/PageHero";
import { BenchmarkViewer } from "@/components/benchmarks/BenchmarkViewer";
import { loadLatestReport } from "@/lib/benchmarks/loadReport";
import { getRegistryEntry, benchmarkRegistry } from "@/lib/benchmarks/registry";

type Props = {
  params: Promise<{ name: string }>;
};

export function generateStaticParams() {
  return benchmarkRegistry.map((e) => ({ name: e.name }));
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { name } = await params;
  const entry = getRegistryEntry(name);
  return { title: entry?.title ?? name };
}

export default async function BenchmarkDetailPage({ params }: Props) {
  const { name } = await params;
  const entry = getRegistryEntry(name);
  if (!entry) notFound();

  const latest = loadLatestReport(name);
  if (!latest) notFound();

  return (
    <>
      <PageHero
        kicker={latest.type === "load" ? "Load test (k6)" : "Micro-benchmark (BenchmarkDotNet)"}
        title={latest.title ?? entry.title}
        description={entry.description}
      />
      <section className="section shell">
        <p style={{ marginBottom: "1rem" }}>
          <Link href="/benchmarks" className="muted">
            ← All suites
          </Link>
        </p>
        <Suspense fallback={<p className="muted">Loading viewer…</p>}>
          <BenchmarkViewer suite={name} initialReport={latest} />
        </Suspense>
      </section>
    </>
  );
}
