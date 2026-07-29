import {withBearerToken} from "../auth/authHeaders.js";
import {toApiClientError} from "./errors.js";
import {entityMetadataPath, metadataPath} from "./metadata.js";
import {buildUrl} from "./request.js";
import type {
    ApiRequest,
    ApiResponse,
    AsyncApiClientOptions,
    AsyncApiTransport,
} from "../types/common.js";
import type {
    CrudMetadataResponse,
    EndpointMetadataResponse,
    EntityTypeMetadata,
} from "../types/metadata.js";

/** Async API client for Promise-based transports (fetch, axios, undici). */
export interface AsyncApiClient {
    request<TData = unknown, TBody = unknown>(
        request: ApiRequest<TBody>
    ): Promise<ApiResponse<TData>>;

    /**
     * Typed CreateBuilder metadata: `GET {baseRoute}/Metadata`
     * → {@link EndpointMetadataResponse}.
     */
    getMetadata(baseRoute: string): Promise<ApiResponse<EndpointMetadataResponse>>;

    /**
     * Dynamic CRUD registry metadata: `GET {baseRoute}/Metadata`
     * → {@link CrudMetadataResponse}. Same path pattern as {@link getMetadata};
     * the host registration determines the payload.
     */
    getCrudMetadata(baseRoute: string): Promise<ApiResponse<CrudMetadataResponse>>;

    /**
     * Dynamic CRUD per-entity metadata:
     * `GET {baseRoute}/{entityType}/Metadata` → {@link EntityTypeMetadata}.
     */
    getEntityMetadata(
        baseRoute: string,
        entityType: string
    ): Promise<ApiResponse<EntityTypeMetadata>>;
}

/**
 * Creates an async API client. Use this from Node/Next.js/browsers with `fetch`.
 * For sync runtimes (k6), use {@link createApiClient} instead.
 */
export function createAsyncApiClient(options: AsyncApiClientOptions): AsyncApiClient {
    const defaultHeaders = options.defaultHeaders ?? {};
    const transport: AsyncApiTransport = options.transport;

    const client: AsyncApiClient = {
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

        getMetadata(baseRoute: string): Promise<ApiResponse<EndpointMetadataResponse>> {
            return client.request<EndpointMetadataResponse>({
                method: "GET",
                path: metadataPath(baseRoute),
            });
        },

        getCrudMetadata(baseRoute: string): Promise<ApiResponse<CrudMetadataResponse>> {
            return client.request<CrudMetadataResponse>({
                method: "GET",
                path: metadataPath(baseRoute),
            });
        },

        getEntityMetadata(
            baseRoute: string,
            entityType: string
        ): Promise<ApiResponse<EntityTypeMetadata>> {
            return client.request<EntityTypeMetadata>({
                method: "GET",
                path: entityMetadataPath(baseRoute, entityType),
            });
        },
    };

    return client;
}
