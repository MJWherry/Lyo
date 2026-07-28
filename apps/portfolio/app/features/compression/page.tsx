import type { Metadata } from "next";
import Link from "next/link";
import { PageHero } from "@/components/PageHero";
import { FeatureNav } from "@/components/FeatureNav";
import { CodeBlock } from "@/components/CodeBlock";
import { snippets } from "@/content/snippets";

export const metadata: Metadata = {
  title: "Compression",
};

export default function CompressionFeaturePage() {
  return (
    <>
      <PageHero
        kicker="Lyo.Compression"
        title="Pick an algorithm, keep the API"
        description="Ten codecs (LZ4, Zstd, Brotli, GZip, …), streams/files, size limits and bomb protections. June 2026: LZ4 ~128 µs compress @ 1 MB; Zstd ~31× faster streaming compress than GZip @ 100 MB."
      />
      <FeatureNav current="/features/compression" />
      <section className="section shell">
        <p style={{ marginBottom: "1rem" }}>
          <Link className="btn btn-primary" href="/benchmarks/compression">
            Compression benchmarks
          </Link>
        </p>
        <div className="grid-2">
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Service registration</h2>
            <CodeBlock code={snippets.compression} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Bytes + ratio</h2>
            <CodeBlock code={snippets.compressionBytes} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Bounded stream compress</h2>
            <CodeBlock code={snippets.compressionStream} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Resolver (per-algorithm)</h2>
            <CodeBlock code={snippets.compressionResolver} />
          </div>
        </div>
      </section>
    </>
  );
}
