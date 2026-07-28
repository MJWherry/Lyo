import {withBearerToken} from "../auth/authHeaders.js";
import {toApiClientError} from "./errors.js";
import {buildUrl} from "./request.js";
import type {
    ApiRequest,
    ApiResponse,
    AsyncApiClientOptions,
    AsyncApiTransport,
} from "../types/common.js";

/** Async API client for Promise-based transports (fetch, axios, undici). */
export interface AsyncApiClient {
    request<TData = unknown, TBody = unknown>(
        request: ApiRequest<TBody>
    ): Promise<ApiResponse<TData>>;
}

/**
 * Creates an async API client. Use this from Node/Next.js/browsers with `fetch`.
 * For sync runtimes (k6), use {@link createApiClient} instead.
 */
export function createAsyncApiClient(options: AsyncApiClientOptions): AsyncApiClient {
    const defaultHeaders = options.defaultHeaders ?? {};
    const transport: AsyncApiTransport = options.transport;

    return {
        async request<TData = unknown, TBody = unknown>(
            request: ApiRequest<TBody>
        ): Promise<ApiResponse<TData>> {
            const url = buildUrl(options.baseUrl, request.path, request.query);
            const headers = withBearerToken(
                {
                    "Content-Type": "application/json",
                    ...defaultHeaders,
                    ...(request.headers ?? {}),
                },
                options.token
            );

            const body =
                request.body === undefined ? undefined : JSON.stringify(request.body);

            const response = await transport({
                method: request.method,
                url,
                body,
                headers,
            });

            if (!response.ok) {
                throw toApiClientError(response.status, response.data ?? response.rawBody);
            }

            return response as ApiResponse<TData>;
        },
    };
}
