"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import packages from "@/content/packages.json";
import sizes from "@/content/package-sizes.json";
import { formatBytes } from "@/lib/benchmarks/format";
import type { CatalogPackageIndex } from "@/lib/catalog/types";

/** Areas shown on the features catalog (Tools / test hosts stay out of the grid). */
const AREA_ORDER = [
  "Core",
  "Data",
  "Communication",
  "Security",
  "Integration",
  "Features",
  "Apps",
] as const;

const HIDDEN_AREAS = new Set(["Tools", "Other"]);

function shortTfm(tfm: string): string {
  // netstandard2.0 → ns2.0; keep net10.0 / net8.0 as-is
  if (/^netstandard/i.test(tfm)) return tfm.replace(/^netstandard/i, "ns");
  return tfm;
}

function topicOf(pkg: CatalogPackageIndex): string {
  if (pkg.topic) return pkg.topic;
  const parts = pkg.id.split(".");
  return parts.length >= 2 ? `${parts[0]}.${parts[1]}` : pkg.id;
}

function isCatalogExcluded(pkg: CatalogPackageIndex): boolean {
  if (HIDDEN_AREAS.has(pkg.area)) return true;
  const id = pkg.id;
  if (id.endsWith(".Tests") || id.endsWith(".Benchmarks")) return true;
  if (/\.Tests\./.test(id)) return true; // e.g. Lyo.Api.Tests.Host
  if (id === "Lyo.TestApi" || id === "Lyo.TestConsole") return true;
  return false;
}

type SizeById = Map<string, { id: string; bytes: number }>;

function PackageCard({
  pkg,
  area,
  sizeById,
  showSizes,
}: {
  pkg: CatalogPackageIndex;
  area: string;
  sizeById: SizeById;
  showSizes: boolean;
}) {
  const size = sizeById.get(pkg.id);
  const blurb = pkg.tagline || "";
  const tfms = Array.isArray(pkg.targetFrameworks) ? pkg.targetFrameworks : [];
  return (
    <Link href={`/packages/${encodeURIComponent(pkg.id)}`} className="card">
      <strong>{pkg.name}</strong>
      <span className="muted card-blurb">{blurb}</span>
      <div className="card-meta">
        <span className="badge">{area}</span>
        {tfms.map((tfm) => (
          <span key={tfm} className="badge badge-tfm" title={tfm}>
            {shortTfm(tfm)}
          </span>
        ))}
        {showSizes ? (
          <span className="badge">{size && size.bytes > 0 ? formatBytes(size.bytes) : "—"}</span>
        ) : null}
      </div>
    </Link>
  );
}

export function PackageCatalog({ showSizes = true }: { showSizes?: boolean }) {
  const [query, setQuery] = useState("");
  const sizeById = useMemo(() => new Map(sizes.packages.map((p) => [p.id, p])), []);

  const catalog = useMemo(
    () => (packages as CatalogPackageIndex[]).filter((pkg) => !isCatalogExcluded(pkg)),
    [],
  );

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return catalog;
    return catalog.filter((pkg) => {
      const tfms = Array.isArray(pkg.targetFrameworks) ? pkg.targetFrameworks.join(" ") : "";
      const hay = `${pkg.id} ${pkg.name} ${pkg.area} ${topicOf(pkg)} ${pkg.tagline} ${tfms}`.toLowerCase();
      return hay.includes(q);
    });
  }, [catalog, query]);

  const byAreaTopic = useMemo(() => {
    const map = new Map<string, Map<string, CatalogPackageIndex[]>>();
    for (const area of AREA_ORDER) map.set(area, new Map());
    for (const pkg of filtered) {
      const topics = map.get(pkg.area) ?? new Map<string, CatalogPackageIndex[]>();
      const topic = topicOf(pkg);
      const list = topics.get(topic) ?? [];
      list.push(pkg);
      topics.set(topic, list);
      map.set(pkg.area, topics);
    }
    for (const topics of map.values()) {
      for (const list of topics.values()) {
        list.sort((a, b) => a.id.localeCompare(b.id));
      }
    }
    return map;
  }, [filtered]);

  return (
    <>
      <div className="catalog-search" style={{ marginBottom: "1.5rem" }}>
        <label className="faint" htmlFor="package-search" style={{ display: "block", marginBottom: "0.35rem" }}>
          Search packages
        </label>
        <input
          id="package-search"
          type="search"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Name, topic, area, TFM, or keyword…"
          className="catalog-search-input"
        />
        {query.trim() ? (
          <p className="muted" style={{ marginTop: "0.5rem", marginBottom: 0 }}>
            {filtered.length} match{filtered.length === 1 ? "" : "es"}
          </p>
        ) : (
          <p className="muted" style={{ marginTop: "0.5rem", marginBottom: 0 }}>
            Grouped by area → <code>Lyo.&lt;topic&gt;</code>. Tools, tests, and benchmarks are omitted.
          </p>
        )}
      </div>

      {AREA_ORDER.map((area) => {
        const topics = byAreaTopic.get(area);
        if (!topics || topics.size === 0) return null;
        const topicKeys = [...topics.keys()].sort((a, b) => a.localeCompare(b));
        const total = topicKeys.reduce((n, t) => n + (topics.get(t)?.length ?? 0), 0);
        return (
          <div key={area} className="area-section" id={`area-${area.toLowerCase()}`}>
            <h2>
              {area}{" "}
              <span className="faint" style={{ fontSize: "0.85rem", fontWeight: 500 }}>
                ({total} · {topicKeys.length} topic{topicKeys.length === 1 ? "" : "s"})
              </span>
            </h2>
            {topicKeys.map((topic) => {
              const items = topics.get(topic) ?? [];
              return (
                <div key={topic} className="topic-section" id={`topic-${topic.toLowerCase().replace(/\./g, "-")}`}>
                  <h3>
                    {topic}{" "}
                    <span className="faint" style={{ fontSize: "0.8rem", fontWeight: 500 }}>
                      ({items.length})
                    </span>
                  </h3>
                  <div className="card-grid">
                    {items.map((pkg) => (
                      <PackageCard
                        key={pkg.id}
                        pkg={pkg}
                        area={area}
                        sizeById={sizeById}
                        showSizes={showSizes}
                      />
                    ))}
                  </div>
                </div>
              );
            })}
          </div>
        );
      })}

      {filtered.length === 0 ? (
        <p className="muted">No packages match “{query.trim()}”.</p>
      ) : null}
    </>
  );
}
