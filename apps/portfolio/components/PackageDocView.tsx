import type { ReactNode } from "react";
import Link from "next/link";
import { CodeBlock } from "./CodeBlock";
import { DocSections } from "./DocSections";
import { resolveFeaturedMetrics } from "@/lib/benchmarks/resolveFeatured";
import { formatDescriptionHtml, inlineFormat, normalizeProse } from "@/lib/catalog/inlineFormat";
import type { CatalogDependency, CatalogPackage, FeatureNode } from "@/lib/catalog/types";

const LEGACY_DEPS_TITLE =
  /^(related\s+packages|related\s+projects|dependencies)(\b| — |-|:)/i;

function DependenciesPanel({ deps }: { deps: CatalogDependency[] }) {
  if (!deps.length) return null;
  return (
    <div className="panel" style={{ marginTop: "1rem" }}>
      <h2 style={{ fontSize: "1.25rem" }}>Dependencies</h2>
      <p className="muted" style={{ marginBottom: "0.65rem" }}>
        From <code>ProjectReference</code> / <code>PackageReference</code> (same model as the
        project graph).
      </p>
      <ul className="dep-list">
        {deps.map((dep) => {
          const isLyo = dep.kind === "lyo" || dep.name.startsWith("Lyo.");
          return (
            <li key={`${dep.kind}:${dep.name}:${(dep.tags || []).join(",")}`}>
              {isLyo ? (
                <Link className="dep-name" href={`/packages/${encodeURIComponent(dep.name)}`}>
                  {dep.name}
                </Link>
              ) : (
                <span className="dep-name">{dep.name}</span>
              )}
              {dep.version ? <span className="dep-version">{dep.version}</span> : null}
              {(dep.tags || []).map((tag) => (
                <span key={tag} className="dep-tag" data-tag={tag}>
                  {tag}
                </span>
              ))}
            </li>
          );
        })}
      </ul>
    </div>
  );
}

/** Hide body description only when it adds nothing beyond the hero tagline. */
function descriptionIsRedundant(tagline: string, description: string): boolean {
  const a = normalizeProse(tagline).replace(/\s+/g, " ").trim().toLowerCase();
  const b = normalizeProse(description).replace(/\s+/g, " ").trim().toLowerCase();
  if (!b) return true;
  if (!a) return false;
  if (a === b) return true;
  // Truncated tagline of a longer description → still show the full body.
  if (b.startsWith(a.replace(/\s*\.?\s*$/, "")) && b.length > a.length + 24) return false;
  return b.startsWith(a) && b.length <= a.length + 24;
}

function isFeatureGroup(node: FeatureNode): node is Exclude<FeatureNode, string> {
  return typeof node !== "string" && Array.isArray(node.items) && node.items.length > 0;
}

function FeatureLeafList({ nodes }: { nodes: FeatureNode[] }) {
  if (!nodes.length) return null;
  return (
    <ul className="feature-list muted">
      {nodes.map((node, i) => (
        <FeatureLeaf key={i} node={node} />
      ))}
    </ul>
  );
}

function FeatureLeaf({ node }: { node: FeatureNode }) {
  if (typeof node === "string") {
    return <li dangerouslySetInnerHTML={{ __html: inlineFormat(node) }} />;
  }

  const title = (node.title ?? "").trim();
  const text = (node.text ?? "").trim();
  const items = node.items ?? [];
  const label = title && text ? `**${title}** — ${text}` : title || text;

  return (
    <li>
      {label ? <span dangerouslySetInnerHTML={{ __html: inlineFormat(label) }} /> : null}
      {items.length > 0 ? <FeatureLeafList nodes={items} /> : null}
    </li>
  );
}

/** Top-level features: groups become subsections; leaf runs stay one list. */
function FeaturesBlock({ features }: { features: FeatureNode[] }) {
  const blocks: ReactNode[] = [];
  let leafRun: FeatureNode[] = [];

  const flushLeaves = () => {
    if (!leafRun.length) return;
    blocks.push(<FeatureLeafList key={`leaves-${blocks.length}`} nodes={[...leafRun]} />);
    leafRun = [];
  };

  features.forEach((node, i) => {
    if (isFeatureGroup(node)) {
      flushLeaves();
      const title = (node.title ?? "").trim();
      const text = (node.text ?? "").trim();
      blocks.push(
        <div key={`group-${i}`} className="feature-group">
          {title ? <h3 className="feature-group-title">{title}</h3> : null}
          {text ? (
            <p
              className="feature-group-text"
              dangerouslySetInnerHTML={{ __html: inlineFormat(text) }}
            />
          ) : null}
          <FeatureLeafList nodes={node.items ?? []} />
        </div>,
      );
      return;
    }
    leafRun.push(node);
  });
  flushLeaves();

  return <>{blocks}</>;
}

/** Renders a full package docs.json the same way README render does. */
export function PackageDocView({ pkg }: { pkg: CatalogPackage }) {
  const tagline = (pkg.tagline ?? "").trim();
  const description = (pkg.description ?? "").trim();
  // PageHero already shows tagline — only render the fuller description once.
  const showDescription = Boolean(description) && !descriptionIsRedundant(tagline, description);
  const sections = (pkg.sections ?? []).filter((s) => !LEGACY_DEPS_TITLE.test(s.title ?? ""));
  const dependencies = pkg.dependencies ?? [];
  const features = pkg.features ?? [];

  return (
    <>
      {showDescription ? (
        <div
          className="panel doc-prose"
          style={{ marginBottom: "1rem" }}
          dangerouslySetInnerHTML={{
            __html: formatDescriptionHtml(description),
          }}
        />
      ) : null}

      {features.length > 0 ? (
        <div className="panel" style={{ marginBottom: "1rem" }}>
          <h2 style={{ fontSize: "1.25rem" }}>Features</h2>
          <FeaturesBlock features={features} />
        </div>
      ) : null}

      {(pkg.examples ?? []).length > 0 ? (
        <div style={{ marginBottom: "1.5rem" }}>
          <h2 style={{ fontSize: "1.35rem", marginBottom: "0.75rem" }}>Examples</h2>
          {pkg.examples.map((ex, i) => (
            <div key={`${ex.title}-${i}`} style={{ marginBottom: "0.85rem" }}>
              {ex.title ? <h3 style={{ fontSize: "1.1rem", marginBottom: "0.45rem" }}>{ex.title}</h3> : null}
              <div className="code-stack">
                <CodeBlock code={ex.code} language={ex.language || "csharp"} />
              </div>
            </div>
          ))}
        </div>
      ) : null}

      {pkg.benchmarks ? (
        <div className="panel" style={{ marginTop: "1rem", marginBottom: "1rem" }}>
          <h2 style={{ fontSize: "1.25rem" }}>Benchmarks</h2>
          {pkg.benchmarks.headline ? (
            <p className="muted">{pkg.benchmarks.headline}</p>
          ) : null}
          {pkg.benchmarks.suite ? (
            <p style={{ marginBottom: "0.5rem" }}>
              <Link className="btn btn-ghost" href={`/benchmarks/${pkg.benchmarks.suite}`}>
                Open suite: {pkg.benchmarks.suite}
              </Link>
            </p>
          ) : null}
          <ul className="doc-list muted">
            {(pkg.benchmarks.items ?? []).map((item) => {
              const resolved = item.featured
                ? resolveFeaturedMetrics(pkg.benchmarks?.suite, item)
                : null;
              return (
                <li key={`${item.label}-${item.href}`}>
                  <span>{item.label}</span>
                  {resolved?.metric ? (
                    <strong style={{ marginLeft: "0.35rem" }}>{resolved.metric}</strong>
                  ) : null}
                  {resolved?.detail
                    ? ` — ${resolved.detail}`
                    : item.note
                      ? ` — ${item.note}`
                      : ""}
                  {item.featured ? (
                    <span className="badge badge-accent" style={{ marginLeft: "0.4rem" }}>
                      featured
                    </span>
                  ) : null}
                  <span className="faint" style={{ display: "block", fontSize: "0.8rem" }}>
                    {resolved?.note ?? item.href}
                  </span>
                </li>
              );
            })}
          </ul>
        </div>
      ) : null}

      {sections.length > 0 ? (
        <div style={{ marginTop: "1rem" }}>
          <DocSections sections={sections} />
        </div>
      ) : null}

      <DependenciesPanel deps={dependencies} />

      {(pkg.links ?? []).length > 0 ? (
        <div className="panel" style={{ marginTop: "1rem" }}>
          <h2 style={{ fontSize: "1.15rem" }}>Links</h2>
          <ul className="doc-list muted">
            {pkg.links.map((l) => (
              <li key={l.href}>
                <a href={l.href}>{l.label}</a>
              </li>
            ))}
          </ul>
        </div>
      ) : null}
    </>
  );
}
