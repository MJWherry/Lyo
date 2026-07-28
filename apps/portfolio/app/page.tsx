import Link from "next/link";
import site from "@/content/site.json";

export default function HomePage() {
  return (
    <section className="hero shell">
      <div className="kicker" style={{ animation: "rise 0.9s ease both" }}>
        {site.fullName}
      </div>
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
        <Link className="btn btn-ghost" href={site.resumePath}>
          Resume
        </Link>
        <a className="btn btn-ghost" href={site.githubUrl} target="_blank" rel="noreferrer">
          GitHub
        </a>
      </div>
    </section>
  );
}
