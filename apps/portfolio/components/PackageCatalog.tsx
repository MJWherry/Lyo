import packages from "@/content/packages.json";
import sizes from "@/content/package-sizes.json";
import { formatBytes } from "@/lib/benchmarks/format";

const AREA_ORDER = [
  "Core",
  "Data",
  "Communication",
  "Security",
  "Integration",
  "Features",
  "Apps",
  "Tools",
] as const;

export function PackageCatalog({ showSizes = true }: { showSizes?: boolean }) {
  const sizeById = new Map(sizes.packages.map((p) => [p.id, p]));
  const byArea = new Map<string, typeof packages>();

  for (const area of AREA_ORDER) byArea.set(area, []);
  for (const pkg of packages) {
    const list = byArea.get(pkg.area) ?? [];
    list.push(pkg);
    byArea.set(pkg.area, list);
  }

  return (
    <>
      {AREA_ORDER.map((area) => {
        const items = byArea.get(area) ?? [];
        if (items.length === 0) return null;
        return (
          <div key={area} className="area-section" id={`area-${area.toLowerCase()}`}>
            <h2>
              {area}{" "}
              <span className="faint" style={{ fontSize: "0.85rem", fontWeight: 500 }}>
                ({items.length})
              </span>
            </h2>
            <div className="card-grid">
              {items.map((pkg) => {
                const size = sizeById.get(pkg.id);
                return (
                  <article key={pkg.id} className="card">
                    <strong>{pkg.name}</strong>
                    <span className="muted">{pkg.summary}</span>
                    <div className="card-meta">
                      <span className="badge">{area}</span>
                      {showSizes ? (
                        <span className="badge">
                          {size && size.bytes > 0 ? formatBytes(size.bytes) : "—"}
                        </span>
                      ) : null}
                    </div>
                  </article>
                );
              })}
            </div>
          </div>
        );
      })}
    </>
  );
}
