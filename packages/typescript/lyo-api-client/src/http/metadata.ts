/**
 * Path helpers for Lyo.Api metadata endpoints.
 *
 * - Typed CreateBuilder: `GET {baseRoute}/Metadata` → EndpointMetadataResponse
 * - Dynamic CRUD collection: `GET {baseRoute}/Metadata` → CrudMetadataResponse
 * - Dynamic per-entity: `GET {baseRoute}/{entityType}/Metadata` → EntityTypeMetadata
 */

/** Trim slashes; empty string means host root. */
export function normalizeRoutePrefix(baseRoute: string): string {
    const trimmed = (baseRoute ?? "").trim().replace(/^\/+|\/+$/g, "");
    return trimmed ? `/${trimmed}` : "";
}

/** `GET {baseRoute}/Metadata` path for typed or dynamic collection metadata. */
export function metadataPath(baseRoute: string): string {
    return `${normalizeRoutePrefix(baseRoute)}/Metadata`;
}

/** `GET {baseRoute}/{entityType}/Metadata` for a single dynamic entity type. */
export function entityMetadataPath(baseRoute: string, entityType: string): string {
    const type = encodeURIComponent((entityType ?? "").trim());
    return `${normalizeRoutePrefix(baseRoute)}/${type}/Metadata`;
}
