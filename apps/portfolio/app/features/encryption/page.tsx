import type { Metadata } from "next";
import Link from "next/link";
import { PageHero } from "@/components/PageHero";
import { FeatureNav } from "@/components/FeatureNav";
import { CodeBlock } from "@/components/CodeBlock";
import { snippets } from "@/content/snippets";

export const metadata: Metadata = {
  title: "Encryption",
};

export default function EncryptionFeaturePage() {
  return (
    <>
      <PageHero
        kicker="Lyo.Encryption"
        title="Envelopes you can probe"
        description="AES-GCM, ChaCha20-Poly1305, AES-CCM, AES-SIV, XChaCha — plus RSA/hybrid, two-key DEK/KEK flows, and header-only inspection. June 2026: AES-GCM ~906 µs encrypt @ 1 MB; ~1.2 GB/s streaming @ 100 MB."
      />
      <FeatureNav current="/features/encryption" />
      <section className="section shell">
        <p style={{ marginBottom: "1rem" }}>
          <Link className="btn btn-primary" href="/benchmarks/encryption">
            Encryption benchmarks
          </Link>
        </p>
        <div className="grid-2">
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Keyed two-key (DI)</h2>
            <CodeBlock code={snippets.encryptionKeyed} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Single-key AES-GCM</h2>
            <CodeBlock code={snippets.encryption} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Two-key envelope</h2>
            <CodeBlock code={snippets.encryptionTwoKey} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>RSA / hybrid</h2>
            <CodeBlock code={snippets.encryptionRsa} />
          </div>
        </div>
      </section>
    </>
  );
}
