import type { Metadata } from "next";
import Link from "next/link";
import { PageHero } from "@/components/PageHero";
import { FeatureNav } from "@/components/FeatureNav";
import { CodeBlock } from "@/components/CodeBlock";
import { snippets } from "@/content/snippets";

export const metadata: Metadata = {
  title: "Query",
};

export default function QueryFeaturePage() {
  return (
    <>
      <PageHero
        kicker="Lyo.Api · Lyo.Query"
        title="EF Core API wrapper with a full query engine"
        description="Minimal APIs and CRUD on Entity Framework Core — not a thin scaffold. Typed and dynamic builders, result caching with auto-invalidation, nested WhereClause filters, projection, property-level patch, bulk with per-item fallback, and CSV/XLSX/JSON export. Same request shapes in C#, TypeScript, and Python."
      />
      <FeatureNav current="/features/query" />
      <section className="section shell">
        <div className="panel" style={{ marginBottom: "1rem" }}>
          <h2 style={{ fontSize: "1.25rem" }}>What you get</h2>
          <ul className="muted" style={{ margin: "0.5rem 0 0", paddingLeft: "1.2rem" }}>
            <li>
              <strong style={{ color: "var(--ink)" }}>EF-backed CRUD</strong> —{" "}
              <code>AddLyoQueryServices</code> / <code>AddLyoCrudServices</code> +{" "}
              <code>ApiEndpointBuilder</code> (typed) or <code>MapDynamicCrudEndpoints</code>
            </li>
            <li>
              <strong style={{ color: "var(--ink)" }}>Query engine</strong> — nested And/Or{" "}
              <code>whereClause</code>, 16 comparators, includes, multi-sort, keys,{" "}
              <code>TotalCountMode</code>, optional in-memory <code>subClause</code>
            </li>
            <li>
              <strong style={{ color: "var(--ink)" }}>Projection</strong> — sparse{" "}
              <code>QueryProject</code>, wildcards, SQL-level project when possible, computed
              SmartFormat columns
            </li>
            <li>
              <strong style={{ color: "var(--ink)" }}>Caching built in</strong> — FusionCache /
              Lyo.Cache on Query + QueryProject; optional UTF-8 payload entries; invalidate on
              Create/Update/Patch/Delete/Upsert
            </li>
            <li>
              <strong style={{ color: "var(--ink)" }}>Partial patch & bulk</strong> — property-level
              Patch (optional allowlists); bulk batch with individual fallback and partial success
            </li>
            <li>
              <strong style={{ color: "var(--ink)" }}>Delete / Upsert / Export</strong> — delete by
              keys or WhereClause; upsert inherit create/update; CSV, XLSX, JSON export
            </li>
            <li>
              <strong style={{ color: "var(--ink)" }}>Blazor grids</strong> —{" "}
              <code>LyoDataGrid</code> / projected variant wired to the same Query API
            </li>
          </ul>
          <p style={{ marginTop: "1rem", marginBottom: 0 }}>
            <Link className="btn btn-primary" href="/demos/query">
              Open Query demo
            </Link>{" "}
            <Link className="btn btn-ghost" href="/benchmarks/query-api">
              Query API load benches
            </Link>
          </p>
        </div>
        <div className="grid-2">
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Host registration</h2>
            <CodeBlock code={snippets.querySetup} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Concrete query</h2>
            <CodeBlock code={snippets.queryConcrete} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Projection</h2>
            <CodeBlock code={snippets.queryProject} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Root From/Joins</h2>
            <CodeBlock code={snippets.queryRoot} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Patch + bulk</h2>
            <CodeBlock code={snippets.queryPatchBulk} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>TypeScript BFF</h2>
            <CodeBlock code={snippets.bffTs} language="typescript" />
          </div>
        </div>
      </section>
    </>
  );
}
