import type { ApiRequest } from "../types/common.js";

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
