import type {ProjectionQueryReq, QueryConcreteReq} from "lyo-query";
import type {ApiResponse} from "../types/common.js";
import type {
    CreateResult,
    DeleteResult,
    ExportDownload,
    ExportRequest,
    PatchRequest,
    ProjectedQueryRes,
    QueryRes,
    QuerySqlRes,
    UpdateResult,
} from "../types/results.js";
import type {ApiClient} from "./createApiClient.js";
import type {AsyncApiClient} from "./createAsyncApiClient.js";
import {fileNameFromDisposition} from "./request.js";

export interface CrudResource<TReq, TRes> {
    queryConcrete(request: QueryConcreteReq): Promise<ApiResponse<QueryRes<TRes>>>;
    queryProject<TRow = Record<string, unknown>>(
        request: ProjectionQueryReq
    ): Promise<ApiResponse<ProjectedQueryRes<TRow>>>;
    querySql(request: QueryConcreteReq | ProjectionQueryReq): Promise<ApiResponse<QuerySqlRes>>;
    get(id: string, include?: readonly string[]): Promise<ApiResponse<TRes>>;
    create(body: TReq): Promise<ApiResponse<CreateResult<TRes>>>;
    update(id: string, body: TReq): Promise<ApiResponse<UpdateResult<TRes>>>;
    /** Forwards the patch body unchanged. Keys stay whatever the caller set. */
    patch(body: PatchRequest): Promise<ApiResponse<unknown>>;
    /** Sets `keys` to `[[id]]` and keeps the rest of the body. */
    patchById(id: string, body: PatchRequest): Promise<ApiResponse<unknown>>;
    deleteById(id: string): Promise<ApiResponse<DeleteResult<TRes>>>;
    export?: (body: ExportRequest) => Promise<ExportDownload>;
}

export interface SyncCrudResource<TReq, TRes> {
    queryConcrete(request: QueryConcreteReq): ApiResponse<QueryRes<TRes>>;
    queryProject<TRow = Record<string, unknown>>(
        request: ProjectionQueryReq
    ): ApiResponse<ProjectedQueryRes<TRow>>;
    querySql(request: QueryConcreteReq | ProjectionQueryReq): ApiResponse<QuerySqlRes>;
    get(id: string, include?: readonly string[]): ApiResponse<TRes>;
    create(body: TReq): ApiResponse<CreateResult<TRes>>;
    update(id: string, body: TReq): ApiResponse<UpdateResult<TRes>>;
    patch(body: PatchRequest): ApiResponse<unknown>;
    patchById(id: string, body: PatchRequest): ApiResponse<unknown>;
    deleteById(id: string): ApiResponse<DeleteResult<TRes>>;
    export?: (body: ExportRequest) => ExportDownload;
}

function downloadOf(response: ApiResponse<unknown>): ExportDownload {
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
}

function withIdKey(id: string, body: PatchRequest): PatchRequest {
    return {...body, keys: [[id]]};
}

export function createAsyncCrudResource<TReq, TRes>(
    api: AsyncApiClient,
    route: string,
    options?: {export?: boolean}
): CrudResource<TReq, TRes> {
    const resource: CrudResource<TReq, TRes> = {
        queryConcrete(request: QueryConcreteReq) {
            return api.queryConcrete<TRes, QueryConcreteReq>(route, request);
        },
        queryProject<TRow = Record<string, unknown>>(request: ProjectionQueryReq) {
            return api.queryProject<TRow, ProjectionQueryReq>(route, request);
        },
        querySql(request: QueryConcreteReq | ProjectionQueryReq) {
            return api.querySql(route, request);
        },
        get(id: string, include?: readonly string[]) {
            return api.getById<TRes>(route, id, include);
        },
        create(body: TReq) {
            return api.create<TReq, TRes>(route, body);
        },
        update(id: string, body: TReq) {
            return api.update<TReq, TRes>(route, [id], body);
        },
        patch(body: PatchRequest) {
            return api.patch(route, body);
        },
        patchById(id: string, body: PatchRequest) {
            return api.patch(route, withIdKey(id, body));
        },
        deleteById(id: string) {
            return api.deleteById<TRes>(route, id);
        },
    };
    if (options?.export) {
        resource.export = (body: ExportRequest) => api.export(route, body);
    }
    return resource;
}

export function createSyncCrudResource<TReq, TRes>(
    api: ApiClient,
    route: string,
    options?: {export?: boolean}
): SyncCrudResource<TReq, TRes> {
    const resource: SyncCrudResource<TReq, TRes> = {
        queryConcrete(request: QueryConcreteReq) {
            return api.request<QueryRes<TRes>, QueryConcreteReq>({
                method: "POST",
                path: `${route}/QueryConcrete`,
                body: request,
            });
        },
        queryProject<TRow = Record<string, unknown>>(request: ProjectionQueryReq) {
            return api.request<ProjectedQueryRes<TRow>, ProjectionQueryReq>({
                method: "POST",
                path: `${route}/QueryProject`,
                body: request,
            });
        },
        querySql(request: QueryConcreteReq | ProjectionQueryReq) {
            return api.request<QuerySqlRes, QueryConcreteReq | ProjectionQueryReq>({
                method: "POST",
                path: `${route}/QuerySql`,
                body: request,
            });
        },
        get(id: string, include?: readonly string[]) {
            return api.getById<TRes>(route, id, include);
        },
        create(body: TReq) {
            return api.request<CreateResult<TRes>, TReq>({
                method: "POST",
                path: route,
                body,
            });
        },
        update(id: string, body: TReq) {
            return api.request<UpdateResult<TRes>, {keys: unknown[]; data: TReq}>({
                method: "POST",
                path: `${route}/Update`,
                body: {keys: [id], data: body},
            });
        },
        patch(body: PatchRequest) {
            return api.patch(route, body);
        },
        patchById(id: string, body: PatchRequest) {
            return api.patch(route, withIdKey(id, body));
        },
        deleteById(id: string) {
            return api.request<DeleteResult<TRes>>({
                method: "DELETE",
                path: `${route}/${encodeURIComponent(id)}`,
            });
        },
    };
    if (options?.export) {
        resource.export = (body: ExportRequest) => {
            const response = api.request<unknown, ExportRequest>({
                method: "POST",
                path: `${route}/Export`,
                body,
                responseType: "blob",
            });
            return downloadOf(response);
        };
    }
    return resource;
}
