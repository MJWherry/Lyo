import type { Metadata } from "next";
import { PageHero } from "@/components/PageHero";
import { FeatureNav } from "@/components/FeatureNav";
import { CodeBlock } from "@/components/CodeBlock";
import { snippets } from "@/content/snippets";

export const metadata: Metadata = {
  title: "Platform",
};

const items = [
  {
    name: "Cache",
    body: "Local and Fusion-backed ICacheService, typed byte payloads, query cache tags.",
  },
  {
    name: "Locks",
    body: "Local and Redis locks plus keyed semaphores for contention control.",
  },
  {
    name: "Privacy",
    body: "Redaction presets and policy builders for text/JSON before logs leave the process.",
  },
  {
    name: "Resilience",
    body: "Configuration-driven Polly pipelines for vendor HTTP and database calls.",
  },
  {
    name: "Diagnostics",
    body: "Stack decoding, breadcrumbs, error inbox, optional package-metadata enrichment.",
  },
  {
    name: "Message queue",
    body: "IMqService abstractions with RabbitMQ, delayed/priority helpers, and workers.",
  },
  {
    name: "Auth",
    body: "JWKS, token endpoints, optional Google/Keycloak OIDC BFF on TestApi.",
  },
  {
    name: "Taxonomy",
    body: "Core vs Communication/Security vs Integration — Core stays free of vendor SDKs.",
  },
];

export default function PlatformFeaturePage() {
  return (
    <>
      <PageHero
        kicker="Cross-cutting"
        title="Platform primitives"
        description="The quiet packages that show up in every host — cache, locks, privacy, resilience, diagnostics, MQ, and auth."
      />
      <FeatureNav current="/features/platform" />
      <section className="section shell">
        <div className="card-grid" style={{ marginBottom: "1.5rem" }}>
          {items.map((item) => (
            <article key={item.name} className="card">
              <strong>{item.name}</strong>
              <span className="muted">{item.body}</span>
            </article>
          ))}
        </div>
        <div className="grid-2">
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Cache</h2>
            <CodeBlock code={snippets.cache} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Locks</h2>
            <CodeBlock code={snippets.lockSnippet} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Privacy redaction</h2>
            <CodeBlock code={snippets.privacy} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Resilience</h2>
            <CodeBlock code={snippets.resilience} />
          </div>
        </div>
      </section>
    </>
  );
}
