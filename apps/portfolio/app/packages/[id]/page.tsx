import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { PageHero } from "@/components/PageHero";
import { PackageDocView } from "@/components/PackageDocView";
import { getPackageDoc, getPackageIndex } from "@/lib/catalog/packages";

type Props = { params: Promise<{ id: string }> };

export function generateStaticParams() {
  return getPackageIndex().map((p) => ({ id: p.id }));
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { id } = await params;
  const pkg = getPackageDoc(decodeURIComponent(id));
  return { title: pkg?.name ?? id };
}

export default async function PackageDetailPage({ params }: Props) {
  const { id } = await params;
  const pkg = getPackageDoc(decodeURIComponent(id));
  if (!pkg) notFound();

  return (
    <>
      <PageHero kicker={pkg.area} title={pkg.name} description={pkg.tagline} />
      <section className="section shell">
        <p style={{ marginBottom: "1rem" }}>
          <Link href="/features" className="muted">
            ← All packages
          </Link>
        </p>
        <PackageDocView pkg={pkg} />
      </section>
    </>
  );
}
