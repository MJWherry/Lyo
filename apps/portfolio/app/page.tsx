import Link from "next/link";
import { FeaturedBenchmarks } from "@/components/FeaturedBenchmarks";
import { getFeaturedBenchmarks } from "@/lib/catalog/featuredBenchmarks";
import site from "@/content/site.json";

export default function HomePage() {
  const featured = getFeaturedBenchmarks();

  return (
    <>
      <section className="hero shell">
        <div className="hero-brand">
          Lyo<span>.</span>
        </div>
        <h1>{site.tagline}</h1>
        <p>{site.subtitle}</p>
        <div className="cta-row">
          <Link className="btn btn-primary" href="/features">
            Explore libraries
          </Link>
          <Link className="btn btn-ghost" href="/benchmarks">
            Benchmarks
          </Link>
          <Link className="btn btn-ghost" href="/demos">
            Demos
          </Link>
          <a className="btn btn-ghost" href={site.githubUrl} target="_blank" rel="noreferrer">
            GitHub
          </a>
        </div>
      </section>
      <FeaturedBenchmarks items={featured} />
    </>
  );
}
