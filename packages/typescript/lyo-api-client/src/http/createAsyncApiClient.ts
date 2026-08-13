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
import type {
    CreateResult,
    DeleteBulkResult,
    DeleteRequest,
    DeleteResult,
    ExportDownload,
    ExportRequest,
    ProjectedQueryRes,
    QueryRes,
    UpdateRequest,
    UpdateResult,
} from "../types/results.js";

function trimRoute(baseRoute: string): string {
    return baseRoute.replace(/\/+$/, "");
}

function fileNameFromDisposition(header: string | undefined): string | null {
    if (!header) return null;
    const star = /filename\*=UTF-8''([^;]+)/i.exec(header);
    if (star?.[1]) return decodeURIComponent(star[1]);
    const quoted = /filename="([^"]+)"/i.exec(header);
    if (quoted?.[1]) return quoted[1];
    const plain = /filename=([^;]+)/i.exec(header);
    return plain?.[1]?.trim() ?? null;
}

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

    /** `POST {baseRoute}/QueryProject` */
    queryProject<TRow = Record<string, unknown>, TBody = unknown>(
        baseRoute: string,
        body: TBody
    ): Promise<ApiResponse<ProjectedQueryRes<TRow>>>;

    /** `POST {baseRoute}/QueryConcrete` */
    queryConcrete<TItem = unknown, TBody = unknown>(
        baseRoute: string,
        body: TBody
    ): Promise<ApiResponse<QueryRes<TItem>>>;

    /** `POST {baseRoute}/Query` (root From/Joins) */
    query<TRow = Record<string, unknown>, TBody = unknown>(
        baseRoute: string,
        body: TBody
    ): Promise<ApiResponse<ProjectedQueryRes<TRow>>>;

    /** `POST {baseRoute}` → {@link CreateResult} */
    create<TData = unknown, TResult = unknown>(
        baseRoute: string,
        body: TData
    ): Promise<ApiResponse<CreateResult<TResult>>>;

    /** `POST {baseRoute}/Update` with `{ keys, data }` */
    update<TData = unknown, TResult = unknown>(
        baseRoute: string,
        keys: unknown[],
        data: TData
    ): Promise<ApiResponse<UpdateResult<TResult>>>;

    /** `DELETE {baseRoute}/{id}` */
    deleteById<TResult = unknown>(
        baseRoute: string,
        id: string
    ): Promise<ApiResponse<DeleteResult<TResult>>>;

    /** `DELETE {baseRoute}/Bulk` with `{ keys, allowMultiple }` */
    bulkDelete<TResult = unknown>(
        baseRoute: string,
        keys: unknown[][]
    ): Promise<ApiResponse<DeleteBulkResult<TResult>>>;

    /** `POST {baseRoute}/Bulk` patch — `{ keys, data }` */
    bulkPatch<TData = unknown, TResult = unknown>(
        baseRoute: string,
        keys: unknown[][],
        data: TData
    ): Promise<ApiResponse<UpdateResult<TResult>>>;

    /** `POST {baseRoute}/Export` → file download */
    export(baseRoute: string, body: ExportRequest): Promise<ExportDownload>;
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

            const signal = request.signal ?? options.signal;
            const response = await transport({
                method: request.method,
                url,
                body,
                headers,
                ...(signal ? {signal} : {}),
                ...(request.responseType ? {responseType: request.responseType} : {}),
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

        queryProject<TRow = Record<string, unknown>, TBody = unknown>(
            baseRoute: string,
            body: TBody
        ): Promise<ApiResponse<ProjectedQueryRes<TRow>>> {
            return client.request<ProjectedQueryRes<TRow>, TBody>({
                method: "POST",
                path: `${trimRoute(baseRoute)}/QueryProject`,
                body,
            });
        },

        queryConcrete<TItem = unknown, TBody = unknown>(
            baseRoute: string,
            body: TBody
        ): Promise<ApiResponse<QueryRes<TItem>>> {
            return client.request<QueryRes<TItem>, TBody>({
                method: "POST",
                path: `${trimRoute(baseRoute)}/QueryConcrete`,
                body,
            });
        },

        query<TRow = Record<string, unknown>, TBody = unknown>(
            baseRoute: string,
            body: TBody
        ): Promise<ApiResponse<ProjectedQueryRes<TRow>>> {
            return client.request<ProjectedQueryRes<TRow>, TBody>({
                method: "POST",
                path: `${trimRoute(baseRoute)}/Query`,
                body,
            });
        },

        create<TData = unknown, TResult = unknown>(
            baseRoute: string,
            body: TData
        ): Promise<ApiResponse<CreateResult<TResult>>> {
            return client.request<CreateResult<TResult>, TData>({
                method: "POST",
                path: trimRoute(baseRoute),
                body,
            });
        },

        update<TData = unknown, TResult = unknown>(
            baseRoute: string,
            keys: unknown[],
            data: TData
        ): Promise<ApiResponse<UpdateResult<TResult>>> {
            const body: UpdateRequest<TData> = {keys, data};
            return client.request<UpdateResult<TResult>, UpdateRequest<TData>>({
                method: "POST",
                path: `${trimRoute(baseRoute)}/Update`,
                body,
            });
        },

        deleteById<TResult = unknown>(
            baseRoute: string,
            id: string
        ): Promise<ApiResponse<DeleteResult<TResult>>> {
            return client.request<DeleteResult<TResult>>({
                method: "DELETE",
                path: `${trimRoute(baseRoute)}/${encodeURIComponent(id)}`,
            });
        },

        bulkDelete<TResult = unknown>(
            baseRoute: string,
            keys: unknown[][]
        ): Promise<ApiResponse<DeleteBulkResult<TResult>>> {
            const body: DeleteRequest[] = [{keys, allowMultiple: true}];
            return client.request<DeleteBulkResult<TResult>, DeleteRequest[]>({
                method: "DELETE",
                path: `${trimRoute(baseRoute)}/Bulk`,
                body,
            });
        },

        bulkPatch<TData = unknown, TResult = unknown>(
            baseRoute: string,
            keys: unknown[][],
            data: TData
        ): Promise<ApiResponse<UpdateResult<TResult>>> {
            return client.request<UpdateResult<TResult>, {keys: unknown[][]; data: TData}>({
                method: "POST",
                path: `${trimRoute(baseRoute)}/Bulk`,
                body: {keys, data},
            });
        },

        async export(baseRoute: string, body: ExportRequest): Promise<ExportDownload> {
            const response = await client.request<unknown, ExportRequest>({
                method: "POST",
                path: `${trimRoute(baseRoute)}/Export`,
                body,
                responseType: "blob",
            });
            const blob =
                response.blob ??
                new Blob([response.rawBody ?? ""], {
                    type: response.headers?.["content-type"] ?? "application/octet-stream",
                });
            return {
                blob,
                fileName: fileNameFromDisposition(response.headers?.["content-disposition"]),
                contentType: response.headers?.["content-type"] ?? blob.type,
            };
        },
    };

    return client;
}
