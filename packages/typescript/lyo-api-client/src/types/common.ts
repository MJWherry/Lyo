export type HttpMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

export interface ApiRequest<TBody = unknown> {
    method: HttpMethod;
    path: string;
    body?: TBody;
    headers?: Record<string, string>;
    query?: Record<string, string | number | boolean | null | undefined>;
}

export interface ApiResponse<TData = unknown> {
    status: number;
    ok: boolean;
    headers?: Record<string, string>;
    data?: TData;
    rawBody?: string;
}

export type ApiTransport = (request: {
    method: HttpMethod;
    url: string;
    body?: string;
    headers: Record<string, string>;
}) => ApiResponse<unknown>;

export interface ApiClientOptions {
    baseUrl: string;
    defaultHeaders?: Record<string, string>;
    token?: string;
    transport: ApiTransport;
}
