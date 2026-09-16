"use client";

import { useEffect, useState } from "react";
import { fetchCatalogPackage, type CatalogFetch, type CatalogPackageDoc } from "./catalog.js";

export type CatalogPackageState = { doc: CatalogPackageDoc | null; loading: boolean; error: string | null };

/** Loads `packages/{id}.json` under `basePath`; a newer id drops the older response. */
export function useCatalogPackage(basePath: string, packageId: string, fetchFn?: CatalogFetch): CatalogPackageState {
  const [state, setState] = useState<CatalogPackageState>({ doc: null, loading: true, error: null });

  useEffect(() => {
    let live = true;
    setState({ doc: null, loading: true, error: null });
    if (!packageId.trim()) {
      setState({ doc: null, loading: false, error: "No package id." });
      return;
    }
    fetchCatalogPackage(basePath, packageId, fetchFn)
      .then((doc) => live && setState({ doc, loading: false, error: null }))
      .catch(() => live && setState({ doc: null, loading: false, error: `Could not load docs for ${packageId}.` }));
    return () => {
      live = false;
    };
  }, [basePath, packageId, fetchFn]);

  return state;
}
