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

export type CatalogBenchmarks = {
  headline?: string;
  suite?: string;
  items?: Array<{ label: string; href: string; note?: string }>;
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
