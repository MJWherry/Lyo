import { describe, expect, it } from "vitest";
import {
  catalogAreas,
  catalogIndexUrl,
  catalogPackageUrl,
  dependencyLabel,
  fetchCatalogIndex,
  fetchCatalogPackage,
  filterCatalogPackages,
  groupDependencies,
  parseCatalogIndex,
  parseCatalogPackage,
  type CatalogIndexPackage,
} from "./catalog.js";

const row = (id: string, area: string, extra: Partial<CatalogIndexPackage> = {}): CatalogIndexPackage => ({
  id,
  area,
  name: id,
  tagline: "",
  topic: "",
  targetFrameworks: [],
  ...extra,
});

describe("parseCatalogIndex", () => {
  it("reads camelCase index.json and fills defaults", () => {
    const index = parseCatalogIndex({
      generatedAt: "2026-09-23T18:05:48.195696+00:00",
      packageCount: 2,
      packages: [{ id: "Lyo.Email", area: "Communication", name: "Lyo.Email", tagline: "t", topic: "Lyo.Email", targetFrameworks: ["net10.0"] }, { id: "Lyo.Seed" }],
    });
    expect(index.generatedAt).toBe("2026-09-23T18:05:48.195696+00:00");
    expect(index.packageCount).toBe(2);
    expect(index.packages[0]).toEqual({ id: "Lyo.Email", area: "Communication", name: "Lyo.Email", tagline: "t", topic: "Lyo.Email", targetFrameworks: ["net10.0"] });
    expect(index.packages[1]).toEqual(row("Lyo.Seed", "", { name: "" }));
  });

  it("matches property names case-insensitively", () => {
    const index = parseCatalogIndex({ PackageCount: 1, Packages: [{ ID: "Lyo.Cache", Area: "Data" }] });
    expect(index.packageCount).toBe(1);
    expect(index.generatedAt).toBeNull();
    expect(index.packages[0]?.id).toBe("Lyo.Cache");
    expect(index.packages[0]?.area).toBe("Data");
  });
});

describe("parseCatalogPackage", () => {
  it("reads a package doc with defaults for missing lists and section fields", () => {
    const doc = parseCatalogPackage({
      $schema: "../schema.json",
      id: "Lyo.Seed",
      name: "Lyo.Seed",
      area: "Data",
      features: ["a"],
      examples: [{ title: "Use", code: "x" }],
      sections: [{ title: "Notes", text: "hi" }, { type: "list", ordered: true, items: ["one"] }],
      dependencies: [{ name: "Lyo.Exceptions", kind: "lyo", tags: ["core"] }, { name: "Bogus" }],
      benchmarks: { headline: "fast", items: [{ label: "Run", href: "/b" }] },
      links: [{ label: "Repo", href: "https://x" }],
    });
    expect(doc.tagline).toBe("");
    expect(doc.description).toBe("");
    expect(doc.readmePath).toBe("");
    expect(doc.features).toEqual(["a"]);
    expect(doc.examples).toEqual([{ title: "Use", language: "csharp", code: "x" }]);
    expect(doc.sections[0]).toEqual({
      type: "paragraph",
      title: "Notes",
      text: "hi",
      body: null,
      code: null,
      language: null,
      ordered: null,
      items: null,
      headers: [],
      rows: [],
      lead: null,
      trail: null,
    });
    expect(doc.sections[1]?.type).toBe("list");
    expect(doc.sections[1]?.ordered).toBe(true);
    expect(doc.sections[1]?.items).toEqual(["one"]);
    expect(doc.dependencies).toEqual([
      { name: "Lyo.Exceptions", kind: "lyo", version: null, tags: ["core"] },
      { name: "Bogus", kind: "", version: null, tags: [] },
    ]);
    expect(doc.benchmarks).toEqual({ headline: "fast", suite: null, items: [{ label: "Run", href: "/b", note: null }] });
    expect(doc.links).toEqual([{ label: "Repo", href: "https://x" }]);
  });

  it("leaves benchmarks null when absent", () => {
    expect(parseCatalogPackage({ id: "Lyo.X" }).benchmarks).toBeNull();
  });

  it("reads table headers, rows, lead, and trail", () => {
    const doc = parseCatalogPackage({
      id: "Lyo.Tts",
      sections: [
        {
          type: "table",
          title: "Contracts",
          headers: ["Interface", "Methods"],
          rows: [["ITtsService", "SynthesizeAsync"]],
          lead: "before",
          trail: "after",
        },
      ],
    });
    expect(doc.sections[0]).toMatchObject({
      type: "table",
      title: "Contracts",
      headers: ["Interface", "Methods"],
      rows: [["ITtsService", "SynthesizeAsync"]],
      lead: "before",
      trail: "after",
    });
  });
});

describe("catalogAreas", () => {
  it("returns distinct non-blank areas, case-insensitive, sorted", () => {
    expect(catalogAreas([row("a", "data"), row("b", "Core"), row("c", "Data"), row("d", " ")])).toEqual(["Core", "data"]);
  });
});

describe("filterCatalogPackages", () => {
  const rows = [
    row("Lyo.Seed", "Data", { tagline: "Seeder" }),
    row("Lyo.Email", "Communication", { topic: "Mail" }),
    row("Lyo.Cache", "data", { name: "cache" }),
  ];

  it("filters by area case-insensitively and sorts by name", () => {
    expect(filterCatalogPackages(rows, { area: "DATA" }).map((p) => p.id)).toEqual(["Lyo.Cache", "Lyo.Seed"]);
  });

  it("matches the trimmed term against name, id, tagline, and topic", () => {
    expect(filterCatalogPackages(rows, { search: " seeder " }).map((p) => p.id)).toEqual(["Lyo.Seed"]);
    expect(filterCatalogPackages(rows, { search: "mail" }).map((p) => p.id)).toEqual(["Lyo.Email"]);
    expect(filterCatalogPackages(rows, { search: "lyo.cache" }).map((p) => p.id)).toEqual(["Lyo.Cache"]);
  });

  it("returns everything sorted when the filters are blank", () => {
    expect(filterCatalogPackages(rows, { search: "  ", area: null }).map((p) => p.name)).toEqual(["cache", "Lyo.Email", "Lyo.Seed"]);
  });
});

describe("groupDependencies", () => {
  it("groups by kind in first-seen order, with blank kinds under other", () => {
    const groups = groupDependencies([
      { name: "Lyo.A", kind: "lyo", version: null, tags: [] },
      { name: "Bogus", kind: "", version: "35.0", tags: [] },
      { name: "Lyo.B", kind: "lyo", version: null, tags: [] },
      { name: "Npgsql", kind: "nuget", version: "9", tags: [] },
    ]);
    expect(groups.map((g) => [g.kind, g.dependencies.map((d) => d.name)])).toEqual([
      ["lyo", ["Lyo.A", "Lyo.B"]],
      ["other", ["Bogus"]],
      ["nuget", ["Npgsql"]],
    ]);
  });
});

describe("dependencyLabel", () => {
  it("appends the version when set", () => {
    expect(dependencyLabel({ name: "Npgsql", kind: "nuget", version: "9.0.1", tags: [] })).toBe("Npgsql 9.0.1");
    expect(dependencyLabel({ name: "Lyo.A", kind: "lyo", version: " ", tags: [] })).toBe("Lyo.A");
  });
});

describe("catalog urls", () => {
  it("trims the trailing slash and escapes the id", () => {
    expect(catalogIndexUrl("/catalog/")).toBe("/catalog/index.json");
    expect(catalogPackageUrl("/catalog//", "Lyo Odd/Id")).toBe("/catalog/packages/Lyo%20Odd%2FId.json");
  });
});

describe("catalog loaders", () => {
  const fakeFetch = (body: unknown, ok = true) => {
    const calls: string[] = [];
    const fn = async (url: string) => {
      calls.push(url);
      return { ok, status: ok ? 200 : 404, json: async () => body } as Response;
    };
    return { fn, calls };
  };

  it("fetches and parses the index", async () => {
    const { fn, calls } = fakeFetch({ packageCount: 0, packages: [] });
    const index = await fetchCatalogIndex("/c", fn);
    expect(calls).toEqual(["/c/index.json"]);
    expect(index.packageCount).toBe(0);
  });

  it("fetches and parses a package", async () => {
    const { fn, calls } = fakeFetch({ id: "Lyo.Seed", name: "Lyo.Seed" });
    const doc = await fetchCatalogPackage("/c", "Lyo.Seed", fn);
    expect(calls).toEqual(["/c/packages/Lyo.Seed.json"]);
    expect(doc.name).toBe("Lyo.Seed");
  });

  it("throws on a non-OK response", async () => {
    const { fn } = fakeFetch(null, false);
    await expect(fetchCatalogPackage("/c", "Missing", fn)).rejects.toThrow("404");
  });
});
