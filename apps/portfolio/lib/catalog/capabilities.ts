import capabilitiesDoc from "@/content/capabilities.json";
import site from "@/content/site.json";
import type { Capability, CapabilitiesDoc } from "./types";

const doc = capabilitiesDoc as CapabilitiesDoc;

export function getAllCapabilities(): Capability[] {
  return doc.capabilities ?? [];
}

export function getCapability(id: string): Capability | undefined {
  return getAllCapabilities().find((c) => c.id === id);
}

/** Capability ids the portfolio surfaces as deep dives (order from site config). */
export function getDeepDiveIds(): string[] {
  return (site as { deepDives?: string[] }).deepDives ?? [];
}

export function getDeepDives(): Capability[] {
  return getDeepDiveIds()
    .map((id) => getCapability(id))
    .filter((c): c is Capability => Boolean(c));
}
