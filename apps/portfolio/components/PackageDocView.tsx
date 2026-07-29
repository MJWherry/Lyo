import Link from "next/link";
import { CodeBlock } from "./CodeBlock";
import { DocSections } from "./DocSections";
import { inlineFormat } from "@/lib/catalog/inlineFormat";
import type { CatalogDependency, CatalogPackage } from "@/lib/catalog/types";

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
  const a = tagline.replace(/\s+/g, " ").trim().toLowerCase();
  const b = description.replace(/\s+/g, " ").trim().toLowerCase();
  if (!b) return true;
  if (!a) return false;
  if (a === b) return true;
  // Truncated tagline of a longer description → still show the full body.
  if (b.startsWith(a.replace(/\s*\.?\s*$/, "")) && b.length > a.length + 24) return false;
  return b.startsWith(a) && b.length <= a.length + 24;
}

/** Renders a full package docs.json the same way README render does. */
export function PackageDocView({ pkg }: { pkg: CatalogPackage }) {
  const tagline = (pkg.tagline ?? "").trim();
  const description = (pkg.description ?? "").trim();
  // PageHero already shows tagline — only render the fuller description once.
  const showDescription = Boolean(description) && !descriptionIsRedundant(tagline, description);
  const sections = (pkg.sections ?? []).filter((s) => !LEGACY_DEPS_TITLE.test(s.title ?? ""));
  const dependencies = pkg.dependencies ?? [];

  return (
    <>
      {showDescription ? (
        <div
          className="panel"
          style={{ marginBottom: "1rem" }}
          dangerouslySetInnerHTML={{
            __html: inlineFormat(description).replace(/\n/g, "<br/>"),
          }}
        />
      ) : null}

      {(pkg.features ?? []).length > 0 ? (
        <div className="panel" style={{ marginBottom: "1rem" }}>
          <h2 style={{ fontSize: "1.25rem" }}>Features</h2>
          <ul className="muted" style={{ paddingLeft: "1.2rem", margin: "0.5rem 0 0" }}>
            {pkg.features.map((f) => (
              <li key={f} dangerouslySetInnerHTML={{ __html: inlineFormat(f) }} />
            ))}
          </ul>
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
          <ul className="muted" style={{ paddingLeft: "1.2rem", margin: 0 }}>
            {(pkg.benchmarks.items ?? []).map((item) => (
              <li key={item.href}>
                <span>{item.label}</span>
                {item.note ? ` — ${item.note}` : ""}
                <span className="faint" style={{ display: "block", fontSize: "0.8rem" }}>
                  {item.href}
                </span>
              </li>
            ))}
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
          <ul className="muted" style={{ paddingLeft: "1.2rem", margin: 0 }}>
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
