import { readFileSync, existsSync } from "node:fs";
import path from "node:path";
import packagesIndex from "@/content/packages.json";
import type { CatalogPackage, CatalogPackageIndex } from "./types";

export function getPackageIndex(): CatalogPackageIndex[] {
  return packagesIndex as CatalogPackageIndex[];
}

export function getPackageDoc(id: string): CatalogPackage | null {
  const fullPath = path.join(process.cwd(), "content", "packages-full", `${id}.json`);
  if (!existsSync(fullPath)) return null;
  try {
    return JSON.parse(readFileSync(fullPath, "utf8")) as CatalogPackage;
  } catch {
    return null;
  }
}
