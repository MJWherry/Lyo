import type { AsyncApiTransport, ApiResponse } from "lyo-api-client";

function tryParseJson(body: string): unknown {
  if (!body) return undefined;
  try {
    return JSON.parse(body);
  } catch {
    return undefined;
  }
}

/**
 * Node/Next.js transport adapter for {@link createAsyncApiClient}.
 * Uses global `fetch` and returns a normalized {@link ApiResponse}.
 */
export const fetchTransport: AsyncApiTransport = async (request) => {
  const res = await fetch(request.url, {
    method: request.method,
    headers: request.headers,
    body: request.body,
    cache: "no-store",
  });

  const rawBody = await res.text();
  const data = tryParseJson(rawBody);
  const headers: Record<string, string> = {};
  res.headers.forEach((value, key) => {
    headers[key] = value;
  });

  const response: ApiResponse<unknown> = {
    status: res.status,
    ok: res.ok,
    headers,
    data,
    rawBody,
  };
  return response;
};
