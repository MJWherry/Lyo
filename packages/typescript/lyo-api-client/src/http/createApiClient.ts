import {withBearerToken} from "../auth/authHeaders.js";
import {toApiClientError} from "./errors.js";
import {entityMetadataPath, metadataPath} from "./metadata.js";
import {buildUrl} from "./request.js";
import type {ApiClientOptions, ApiRequest, ApiResponse} from "../types/common.js";
import type {
    CrudMetadataResponse,
    EndpointMetadataResponse,
    EntityTypeMetadata,
} from "../types/metadata.js";

export interface ApiClient {
    request<TData = unknown, TBody = unknown>(request: ApiRequest<TBody>): ApiResponse<TData>;

    /**
     * Typed CreateBuilder metadata: `GET {baseRoute}/Metadata`
     * → {@link EndpointMetadataResponse}.
     */
    getMetadata(baseRoute: string): ApiResponse<EndpointMetadataResponse>;

    /**
     * Dynamic CRUD registry metadata: `GET {baseRoute}/Metadata`
     * → {@link CrudMetadataResponse}. Same path pattern as {@link getMetadata};
     * the host registration determines the payload.
     */
    getCrudMetadata(baseRoute: string): ApiResponse<CrudMetadataResponse>;

    /**
     * Dynamic CRUD per-entity metadata:
     * `GET {baseRoute}/{entityType}/Metadata` → {@link EntityTypeMetadata}.
     */
    getEntityMetadata(baseRoute: string, entityType: string): ApiResponse<EntityTypeMetadata>;
}

export function createApiClient(options: ApiClientOptions): ApiClient {
    const defaultHeaders = options.defaultHeaders ?? {};

    const client: ApiClient = {
        request<TData = unknown, TBody = unknown>(request: ApiRequest<TBody>): ApiResponse<TData> {
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

            const response = options.transport({
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

        getMetadata(baseRoute: string): ApiResponse<EndpointMetadataResponse> {
            return client.request<EndpointMetadataResponse>({
                method: "GET",
                path: metadataPath(baseRoute),
            });
        },

        getCrudMetadata(baseRoute: string): ApiResponse<CrudMetadataResponse> {
            return client.request<CrudMetadataResponse>({
                method: "GET",
                path: metadataPath(baseRoute),
            });
        },

        getEntityMetadata(
            baseRoute: string,
            entityType: string
        ): ApiResponse<EntityTypeMetadata> {
            return client.request<EntityTypeMetadata>({
                method: "GET",
                path: entityMetadataPath(baseRoute, entityType),
            });
        },
    };

    return client;
}
