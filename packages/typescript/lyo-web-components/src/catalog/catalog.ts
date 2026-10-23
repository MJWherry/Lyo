/** Package catalog models and loaders for the generated `catalog/` assets (`index.json` and `packages/{id}.json`). */

export type CatalogIndexPackage = {
  id: string;
  area: string;
  name: string;
  tagline: string;
  topic: string;
  targetFrameworks: string[];
};

export type CatalogIndex = {
  /** ISO timestamp, or null when the index has none. */
  generatedAt: string | null;
  packageCount: number;
  packages: CatalogIndexPackage[];
};

export type CatalogDependency = { name: string; kind: string; version: string | null; tags: string[] };
export type CatalogExample = { title: string; language: string; code: string };
export type CatalogBenchmarkItem = { label: string; href: string; note: string | null };
export type CatalogBenchmarks = { headline: string | null; suite: string | null; items: CatalogBenchmarkItem[] };
export type CatalogLink = { label: string; href: string };

/** Section type is `paragraph`, `list`, `code`, or `markdown`. */
export type CatalogSection = {
  type: string;
  title: string | null;
  text: string | null;
  body: string | null;
  code: string | null;
  language: string | null;
  ordered: boolean | null;
  items: string[] | null;
  /** Column titles for a `table` section. */
  headers: string[];
  /** Body rows for a `table` section. */
  rows: string[][];
  /** Text shown above a `table`. */
  lead: string | null;
  /** Text shown below a `table`. */
  trail: string | null;
};

export type CatalogPackageDoc = {
  id: string;
  name: string;
  area: string;
  tagline: string;
  description: string;
  features: string[];
  examples: CatalogExample[];
  benchmarks: CatalogBenchmarks | null;
  sections: CatalogSection[];
  links: CatalogLink[];
  dependencies: CatalogDependency[];
  readmePath: string;
};

export type CatalogDependencyGroup = { kind: string; dependencies: CatalogDependency[] };

export type CatalogFetch = (url: string) => Promise<Response>;

type Json = Record<string, unknown>;

function field(obj: unknown, name: string): unknown {
  if (!obj || typeof obj !== "object") return undefined;
  const lower = name.toLowerCase();
  const key = Object.keys(obj).find((k) => k.toLowerCase() === lower);
  return key === undefined ? undefined : (obj as Json)[key];
}

const str = (obj: unknown, name: string, fallback = "") => {
  const v = field(obj, name);
  return typeof v === "string" ? v : fallback;
};
const strOrNull = (obj: unknown, name: string) => {
  const v = field(obj, name);
  return typeof v === "string" ? v : null;
};
const list = (obj: unknown, name: string): unknown[] => {
  const v = field(obj, name);
  return Array.isArray(v) ? v : [];
};
const strings = (obj: unknown, name: string) => list(obj, name).filter((v): v is string => typeof v === "string");
const rows = (obj: unknown, name: string): string[][] =>
  list(obj, name).map((row) => (Array.isArray(row) ? row.filter((cell): cell is string => typeof cell === "string") : []));

/** Reads `index.json`; property names match case-insensitively and missing values take defaults. */
export function parseCatalogIndex(json: unknown): CatalogIndex {
  const count = field(json, "packageCount");
  return {
    generatedAt: strOrNull(json, "generatedAt"),
    packageCount: typeof count === "number" ? count : 0,
    packages: list(json, "packages").map((p) => ({
      id: str(p, "id"),
      area: str(p, "area"),
      name: str(p, "name"),
      tagline: str(p, "tagline"),
      topic: str(p, "topic"),
      targetFrameworks: strings(p, "targetFrameworks"),
    })),
  };
}

/** Reads `packages/{id}.json`; property names match case-insensitively and missing values take defaults. */
export function parseCatalogPackage(json: unknown): CatalogPackageDoc {
  const benchmarks = field(json, "benchmarks");
  return {
    id: str(json, "id"),
    name: str(json, "name"),
    area: str(json, "area"),
    tagline: str(json, "tagline"),
    description: str(json, "description"),
    features: strings(json, "features"),
    examples: list(json, "examples").map((e) => ({ title: str(e, "title"), language: str(e, "language", "csharp"), code: str(e, "code") })),
    benchmarks:
      benchmarks && typeof benchmarks === "object"
        ? {
            headline: strOrNull(benchmarks, "headline"),
            suite: strOrNull(benchmarks, "suite"),
            items: list(benchmarks, "items").map((i) => ({ label: str(i, "label"), href: str(i, "href"), note: strOrNull(i, "note") })),
          }
        : null,
    sections: list(json, "sections").map((s) => {
      const ordered = field(s, "ordered");
      const items = field(s, "items");
      return {
        type: str(s, "type", "paragraph"),
        title: strOrNull(s, "title"),
        text: strOrNull(s, "text"),
        body: strOrNull(s, "body"),
        code: strOrNull(s, "code"),
        language: strOrNull(s, "language"),
        ordered: typeof ordered === "boolean" ? ordered : null,
        items: Array.isArray(items) ? strings(s, "items") : null,
        headers: strings(s, "headers"),
        rows: rows(s, "rows"),
        lead: strOrNull(s, "lead"),
        trail: strOrNull(s, "trail"),
      };
    }),
    links: list(json, "links").map((l) => ({ label: str(l, "label"), href: str(l, "href") })),
    dependencies: list(json, "dependencies").map((d) => ({ name: str(d, "name"), kind: str(d, "kind"), version: strOrNull(d, "version"), tags: strings(d, "tags") })),
    readmePath: str(json, "readmePath"),
  };
}

const compareText = (a: string, b: string) => a.localeCompare(b, undefined, { sensitivity: "base" });
const equalsText = (a: string, b: string) => a.toLowerCase() === b.toLowerCase();

/** Distinct non-blank areas (case-insensitive, first spelling wins), sorted. */
export function catalogAreas(packages: readonly CatalogIndexPackage[]): string[] {
  const seen = new Map<string, string>();
  for (const p of packages) {
    if (!p.area.trim()) continue;
    const key = p.area.toLowerCase();
    if (!seen.has(key)) seen.set(key, p.area);
  }
  return [...seen.values()].sort(compareText);
}

/** Area match (case-insensitive) and a trimmed search over name, id, tagline, and topic, sorted by name. */
export function filterCatalogPackages(packages: readonly CatalogIndexPackage[], filter: { search?: string | null; area?: string | null }): CatalogIndexPackage[] {
  const area = filter.area?.trim();
  const term = filter.search?.trim().toLowerCase();
  return packages
    .filter((p) => !area || equalsText(p.area, area))
    .filter((p) => !term || [p.name, p.id, p.tagline, p.topic].some((v) => v.toLowerCase().includes(term)))
    .sort((a, b) => compareText(a.name, b.name));
}

/** Groups by kind in first-seen order; blank kinds go under `other`. */
export function groupDependencies(dependencies: readonly CatalogDependency[]): CatalogDependencyGroup[] {
  const groups = new Map<string, CatalogDependency[]>();
  for (const d of dependencies) {
    const kind = d.kind.trim() || "other";
    groups.set(kind, [...(groups.get(kind) ?? []), d]);
  }
  return [...groups].map(([kind, deps]) => ({ kind, dependencies: deps }));
}

export function dependencyLabel(dependency: CatalogDependency): string {
  return dependency.version?.trim() ? `${dependency.name} ${dependency.version}` : dependency.name;
}

const trimBase = (basePath: string) => basePath.replace(/\/+$/, "");

export const catalogIndexUrl = (basePath: string) => `${trimBase(basePath)}/index.json`;
export const catalogPackageUrl = (basePath: string, packageId: string) => `${trimBase(basePath)}/packages/${encodeURIComponent(packageId)}.json`;

async function getJson(url: string, fetchFn: CatalogFetch): Promise<unknown> {
  const res = await fetchFn(url);
  if (!res.ok) throw new Error(`GET ${url} failed with ${res.status}.`);
  return res.json();
}

export async function fetchCatalogIndex(basePath: string, fetchFn: CatalogFetch = fetch): Promise<CatalogIndex> {
  return parseCatalogIndex(await getJson(catalogIndexUrl(basePath), fetchFn));
}

export async function fetchCatalogPackage(basePath: string, packageId: string, fetchFn: CatalogFetch = fetch): Promise<CatalogPackageDoc> {
  return parseCatalogPackage(await getJson(catalogPackageUrl(basePath, packageId), fetchFn));
}
