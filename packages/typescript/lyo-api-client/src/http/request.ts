import type {ApiRequest} from "../types/common.js";

/** Append repeated `include` query params (EF-style navigation paths). */
export function withIncludes(path: string, include?: readonly string[]): string {
    const params = new URLSearchParams();
    for (const item of include ?? []) {
        const trimmed = item.trim();
        if (trimmed) params.append("include", trimmed);
    }
    const qs = params.toString();
    if (!qs) return path;
    return `${path}${path.includes("?") ? "&" : "?"}${qs}`;
}

export function trimRoute(baseRoute: string): string {
    return baseRoute.replace(/\/+$/, "");
}

export function fileNameFromDisposition(header: string | undefined): string | null {
    if (!header) return null;
    const star = /filename\*=UTF-8''([^;]+)/i.exec(header);
    if (star?.[1]) return decodeURIComponent(star[1]);
    const quoted = /filename="([^"]+)"/i.exec(header);
    if (quoted?.[1]) return quoted[1];
    const plain = /filename=([^;]+)/i.exec(header);
    return plain?.[1]?.trim() ?? null;
}

export function buildUrl(
    baseUrl: string,
    path: string,
    query?: ApiRequest["query"]
): string {
    const normalizedBase = baseUrl.replace(/\/+$/, "");
    const normalizedPath = path.startsWith("/") ? path : `/${path}`;
    const url = `${normalizedBase}${normalizedPath}`;

    if (!query || Object.keys(query).length === 0) {
        return url;
    }

    const params = new URLSearchParams();
    for (const [key, value] of Object.entries(query)) {
        if (value === null || value === undefined) {
            continue;
        }
        params.set(key, String(value));
    }

    const qs = params.toString();
    return qs ? `${url}?${qs}` : url;
}
