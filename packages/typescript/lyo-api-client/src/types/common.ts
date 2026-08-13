export type HttpMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

export interface ApiRequest<TBody = unknown> {
    method: HttpMethod;
    path: string;
    body?: TBody;
    headers?: Record<string, string>;
    query?: Record<string, string | number | boolean | null | undefined>;
    /** Abort the underlying transport when the caller disconnects or navigates away. */
    signal?: AbortSignal;
}

export interface ApiResponse<TData = unknown> {
    status: number;
    ok: boolean;
    headers?: Record<string, string>;
    data?: TData;
    rawBody?: string;
}

export type TransportRequest = {
    method: HttpMethod;
    url: string;
    body?: string;
    headers: Record<string, string>;
    signal?: AbortSignal;
};

export type ApiTransport = (request: TransportRequest) => ApiResponse<unknown>;

/** Promise-based transport for Node/Next.js/browsers (`fetch`, axios, undici). */
export type AsyncApiTransport = (request: TransportRequest) => Promise<ApiResponse<unknown>>;

export interface ApiClientOptions {
    baseUrl: string;
    defaultHeaders?: Record<string, string>;
    token?: string;
    transport: ApiTransport;
}

export interface AsyncApiClientOptions {
    baseUrl: string;
    defaultHeaders?: Record<string, string>;
    token?: string;
    transport: AsyncApiTransport;
    /** Applied to every request unless the request sets its own `signal`. */
    signal?: AbortSignal;
}
