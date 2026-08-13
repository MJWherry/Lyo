import type {ApiResponse, AsyncApiTransport} from "../types/common.js";

function tryParseJson(body: string): unknown {
    if (!body) return undefined;
    try {
        return JSON.parse(body);
    } catch {
        return undefined;
    }
}

/**
 * `fetch` adapter for {@link createAsyncApiClient}. Forwards `signal` so a disconnected
 * browser / Next.js request aborts the upstream call.
 */
export function createFetchTransport(options?: {cache?: RequestCache}): AsyncApiTransport {
    const cache = options?.cache ?? "no-store";
    return async (request) => {
        const init: RequestInit = {
            method: request.method,
            headers: request.headers,
            body: request.body,
            cache,
        };
        if (request.signal)
            init.signal = request.signal;

        const res = await fetch(request.url, init);

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
}

/** Default Node/Next.js transport (`cache: "no-store"`, abort-aware). */
export const fetchTransport = createFetchTransport();
