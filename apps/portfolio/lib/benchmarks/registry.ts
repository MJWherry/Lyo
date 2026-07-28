import type { RegistryEntry } from "./types";

/** Mirror of docs/benchmarks/data/registry.js — all suites shown in the portfolio viewer. */
export const benchmarkRegistry: RegistryEntry[] = [
  {
    name: "query-api",
    title: "Query API (k6)",
    type: "load",
    description:
      "k6 load/stress/spike/soak against the Lyo person API (QueryConcrete, QueryProject, root Query).",
  },
  {
    name: "csv",
    title: "CSV",
    type: "micro",
    description: "CSV write, read, split, and merge paths for Lyo.Csv.",
  },
  {
    name: "cache",
    title: "Caching",
    type: "micro",
    description: "Local and FusionCache hot paths, including Redis backplane cases.",
  },
  {
    name: "compression",
    title: "Compression",
    type: "micro",
    description: "Compress/decompress throughput across Lyo.Compression algorithms.",
  },
  {
    name: "encryption",
    title: "Encryption",
    type: "micro",
    description: "Symmetric authenticated-encryption throughput for Lyo.Encryption.",
  },
  {
    name: "hashing",
    title: "Hashing",
    type: "micro",
    description: "SHA/HMAC/CRC digests and hex encode/decode for Lyo.Hashing.",
  },
  {
    name: "lock",
    title: "Locking",
    type: "micro",
    description: "Local and Redis lock acquire/release and contended scenarios.",
  },
  {
    name: "query",
    title: "Query & CRUD",
    type: "micro",
    description: "Where-clause engine, mapping, projection, and Postgres CRUD.",
  },
  {
    name: "xlsx",
    title: "XLSX",
    type: "micro",
    description: "XLSX write, read, convert, split, and merge for Lyo.Xlsx.",
  },
];

export function getRegistryEntry(name: string): RegistryEntry | undefined {
  return benchmarkRegistry.find((e) => e.name === name);
}
