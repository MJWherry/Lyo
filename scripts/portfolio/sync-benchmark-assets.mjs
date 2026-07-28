#!/usr/bin/env node
/**
 * Best-effort copy of docs/benchmarks/history into apps/portfolio/public/benchmarks/history
 * so the Next.js viewer can serve archived snapshots. History is gitignored — no-op if missing.
 */
import { cpSync, existsSync, mkdirSync, rmSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../..");
const src = path.join(repoRoot, "docs/benchmarks/history");
const dest = path.join(repoRoot, "apps/portfolio/public/benchmarks/history");

if (!existsSync(src)) {
  console.log("sync-benchmark-assets: no docs/benchmarks/history — skipping");
  process.exit(0);
}

rmSync(dest, { recursive: true, force: true });
mkdirSync(path.dirname(dest), { recursive: true });
cpSync(src, dest, { recursive: true });
console.log(`sync-benchmark-assets: copied history → ${dest}`);
