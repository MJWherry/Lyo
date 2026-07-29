import { readdirSync } from "node:fs";
import path from "node:path";
import { resolveFeaturedMetrics } from "@/lib/benchmarks/resolveFeatured";
import { getPackageDoc } from "./packages";
import type { CatalogBenchmarkItem } from "./types";

export type FeaturedBenchmark = CatalogBenchmarkItem & {
  packageId: string;
  suite?: string;
  /** Resolved from live suite JSON — never authored in docs.json. */
  metric: string;
  detail: string;
  resolvedNote: string;
};

/**
 * Home-page highlights from package docs.json `benchmarks.items` where
 * `featured: true`. Metric/detail are resolved from
 * `docs/benchmarks/data/{suite}.json` via `method`+`params` or `scenario`.
 */
export function getFeaturedBenchmarks(): FeaturedBenchmark[] {
  const dir = path.join(process.cwd(), "content", "packages-full");
  let ids: string[] = [];
  try {
    ids = readdirSync(dir)
      .filter((f) => f.endsWith(".json"))
      .map((f) => f.replace(/\.json$/, ""));
  } catch {
    return [];
  }

  const out: FeaturedBenchmark[] = [];
  for (const id of ids) {
    const pkg = getPackageDoc(id);
    if (!pkg?.benchmarks?.items?.length) continue;
    const suite = pkg.benchmarks.suite;
    for (const item of pkg.benchmarks.items) {
      if (!item.featured) continue;
      const resolved = resolveFeaturedMetrics(suite, item);
      if (!resolved) continue;
      out.push({
        ...item,
        packageId: id,
        suite,
        metric: resolved.metric,
        detail: resolved.detail,
        resolvedNote: resolved.note,
      });
    }
  }

  return out;
}
