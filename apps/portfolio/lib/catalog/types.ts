export type DocSection =
  | { type: "paragraph"; title?: string; text: string }
  | { type: "list"; title?: string; ordered?: boolean; items: string[] }
  | { type: "code"; title?: string; language: string; code: string }
  | {
      type: "table";
      title?: string;
      headers: string[];
      rows: string[][];
      lead?: string;
      trail?: string;
    }
  | { type: "markdown"; title?: string; body: string };

export type CatalogExample = {
  title: string;
  language: string;
  code: string;
};

export type CatalogBenchmarkItem = {
  label: string;
  href: string;
  note?: string;
  /** When true, surfaced on the portfolio home page. */
  featured?: boolean;
  /**
   * Micro suite: BenchmarkDotNet method name to resolve from
   * `docs/benchmarks/data/{suite}.json`.
   */
  method?: string;
  /** Micro suite: `[Params]` values that must match the measurement. */
  params?: Record<string, string>;
  /** Load (k6) suite: scenario name (e.g. `query_load`). */
  scenario?: string;
  /**
   * Which live figure is the hero number. Defaults to throughput (when present)
   * / mean for micro, p95 for load.
   */
  primary?: "mean" | "throughput" | "p95" | "checks";
};

export type CatalogBenchmarks = {
  headline?: string;
  suite?: string;
  items?: CatalogBenchmarkItem[];
};

export type CatalogPackageIndex = {
  id: string;
  name: string;
  area: string;
  /** Family key, e.g. ``Lyo.Email`` for ``Lyo.Email.Postgres``. */
  topic?: string;
  tagline: string;
  readme: string;
  targetFrameworks?: string[];
};

export type CatalogDependency = {
  name: string;
  kind: "lyo" | "package" | string;
  version?: string | null;
  tags: string[];
};

/** Full package doc (project `docs.json` shape; portfolio copy under content/packages-full). */
export type CatalogPackage = {
  id: string;
  name: string;
  area: string;
  tagline: string;
  description: string;
  features: string[];
  examples: CatalogExample[];
  benchmarks?: CatalogBenchmarks;
  sections: DocSection[];
  dependencies?: CatalogDependency[];
  targetFrameworks?: string[];
  links: Array<{ label: string; href: string }>;
  readmePath: string;
};

export type Capability = {
  id: string;
  title: string;
  summary: string;
  kicker?: string;
  packages?: string[];
  links?: Array<{ label: string; href: string }>;
  sections?: DocSection[];
};

export type CapabilitiesDoc = {
  capabilities: Capability[];
};
