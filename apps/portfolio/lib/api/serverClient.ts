import "server-only";

import { createAsyncApiClient } from "lyo-api-client";
import { createAsyncPersonApiClient } from "lyo-person-api-client";
import { fetchTransport } from "./fetchTransport";

function requireApiBaseUrl(): string {
  const baseUrl = process.env.LYO_API_BASE_URL?.trim();
  if (!baseUrl) {
    throw new Error(
      "LYO_API_BASE_URL is not set. Point it at the internal TestApi URL (e.g. http://localhost:5251)."
    );
  }
  return baseUrl.replace(/\/+$/, "");
}

/** Server-only Person API client. Never import from Client Components. */
export function getPersonApi() {
  const api = createAsyncApiClient({
    baseUrl: requireApiBaseUrl(),
    token: process.env.LYO_API_TOKEN?.trim() || undefined,
    transport: fetchTransport,
  });
  return createAsyncPersonApiClient(api);
}

/** Raw fetch against the internal API for health / non-Person routes. */
export async function apiFetch(path: string, init?: RequestInit): Promise<Response> {
  const base = requireApiBaseUrl();
  const url = path.startsWith("http") ? path : `${base}${path.startsWith("/") ? path : `/${path}`}`;
  const headers = new Headers(init?.headers);
  const token = process.env.LYO_API_TOKEN?.trim();
  if (token && !headers.has("Authorization")) {
    headers.set("Authorization", `Bearer ${token}`);
  }
  return fetch(url, { ...init, headers, cache: "no-store" });
}
