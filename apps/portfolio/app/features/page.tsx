import type { Metadata } from "next";
import Link from "next/link";
import { PageHero } from "@/components/PageHero";
import { PackageCatalog } from "@/components/PackageCatalog";

export const metadata: Metadata = {
  title: "Features",
};

export default function FeaturesHubPage() {
  return (
    <>
      <PageHero
        kicker="Lyo.Net"
        title="Libraries by area"
        description="Documented libraries grouped by taxonomy area, then by Lyo.<topic> family. Tools, tests, and benchmarks are left out of this grid. Content comes from each project’s docs.json."
      />
      <section className="section shell">
        <p style={{ marginBottom: "1.25rem" }}>
          <Link href="/demos/query" className="btn btn-ghost">
            Live Query demo
          </Link>
        </p>
        <PackageCatalog />
      </section>
    </>
  );
}
