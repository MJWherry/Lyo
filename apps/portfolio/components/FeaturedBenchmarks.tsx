import Link from "next/link";
import type { FeaturedBenchmark } from "@/lib/catalog/featuredBenchmarks";

export function FeaturedBenchmarks({ items }: { items: FeaturedBenchmark[] }) {
  if (!items.length) return null;

  return (
    <section className="section shell featured-benchmarks" style={{ paddingTop: 0 }}>
      <div className="kicker">Highlighted benchmarks</div>
      <h2 style={{ fontSize: "1.55rem", marginBottom: "0.35rem" }}>Numbers that hold up</h2>
      <p className="muted" style={{ maxWidth: "40rem", marginBottom: "1.25rem" }}>
        Figures resolved live from suite JSON via package docs selectors (
        <code>method</code>/<code>scenario</code> + <code>featured: true</code>).
      </p>
      <div className="featured-bench-grid">
        {items.map((item) => {
          const href = item.href.startsWith("/")
            ? item.href
            : item.suite
              ? `/benchmarks/${item.suite}`
              : "/benchmarks";
          return (
            <Link key={`${item.packageId}-${item.label}`} href={href} className="featured-bench-card">
              <span className="featured-bench-label">{item.label}</span>
              <span className="featured-bench-metric">{item.metric}</span>
              {item.detail ? (
                <span className="featured-bench-detail">
                  {renderDetailParts(item.detail)}
                </span>
              ) : null}
              <span className="featured-bench-note">{item.resolvedNote}</span>
            </Link>
          );
        })}
      </div>
      <p style={{ marginTop: "1.1rem" }}>
        <Link className="btn btn-ghost" href="/benchmarks">
          All benchmarks
        </Link>
      </p>
    </section>
  );
}

/** Emphasize allocation / size fragments in "a · b · c" detail strings. */
function renderDetailParts(detail: string) {
  return detail.split(" · ").map((part, i, parts) => {
    const emphasize = /allocat|MB|GB|KB|rows|payload/i.test(part);
    return (
      <span key={`${i}-${part}`}>
        {emphasize ? <strong className="featured-bench-detail-em">{part}</strong> : part}
        {i < parts.length - 1 ? " · " : null}
      </span>
    );
  });
}
