/** nginx / common BFF status when the client disconnected before the upstream finished. */
export const CLIENT_CLOSED_REQUEST_STATUS = 499;

export function isAbortError(err: unknown): boolean {
    return typeof err === "object" && err !== null && "name" in err && (err as {name: string}).name === "AbortError";
}

/** True when fetch was aborted or the incoming request signal already fired. */
export function isClientAborted(err: unknown, signal?: AbortSignal): boolean {
    return Boolean(signal?.aborted) || isAbortError(err);
}

/** Attach a caller abort signal to `fetch` / `apiFetch` init. */
export function withAbortSignal(signal: AbortSignal, init?: RequestInit): RequestInit {
    return {...init, signal};
}
