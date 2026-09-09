import type {
    ApiClient,
    AsyncApiClient,
    ApiResponse,
    CreateResult,
    DeleteResult,
    ExportDownload,
    ExportRequest,
    PatchRequest,
    ProjectedQueryRes,
    QueryRes,
    UpdateResult,
} from "lyo-api-client";
import type {ProjectionQueryReq, QueryConcreteReq} from "lyo-query";
import {joinRoutePrefix} from "../routes.js";

export interface JobClientOptions {
    routePrefix?: string;
}

export function jobPath(options: JobClientOptions | undefined, relative: string): string {
    return joinRoutePrefix(options?.routePrefix, relative);
}

export interface CrudResource<TReq, TRes> {
    queryConcrete(request: QueryConcreteReq): Promise<ApiResponse<QueryRes<TRes>>>;
    queryProject<TRow = Record<string, unknown>>(
        request: ProjectionQueryReq
    ): Promise<ApiResponse<ProjectedQueryRes<TRow>>>;
    get(id: string, include?: readonly string[]): Promise<ApiResponse<TRes>>;
    create(body: TReq): Promise<ApiResponse<CreateResult<TRes>>>;
    update(id: string, body: TReq): Promise<ApiResponse<UpdateResult<TRes>>>;
    patch(body: PatchRequest): Promise<ApiResponse<unknown>>;
    deleteById(id: string): Promise<ApiResponse<DeleteResult<TRes>>>;
}

export interface SyncCrudResource<TReq, TRes> {
    queryConcrete(request: QueryConcreteReq): ApiResponse<QueryRes<TRes>>;
    queryProject<TRow = Record<string, unknown>>(
        request: ProjectionQueryReq
    ): ApiResponse<ProjectedQueryRes<TRow>>;
    get(id: string, include?: readonly string[]): ApiResponse<TRes>;
    create(body: TReq): ApiResponse<CreateResult<TRes>>;
    update(id: string, body: TReq): ApiResponse<UpdateResult<TRes>>;
    patch(body: PatchRequest): ApiResponse<unknown>;
    deleteById(id: string): ApiResponse<DeleteResult<TRes>>;
}

export function createAsyncCrudResource<TReq, TRes>(
    api: AsyncApiClient,
    route: string,
    options?: {export?: boolean}
): CrudResource<TReq, TRes> & {export?: (body: ExportRequest) => Promise<ExportDownload>} {
    const resource: CrudResource<TReq, TRes> & {
        export?: (body: ExportRequest) => Promise<ExportDownload>;
    } = {
        queryConcrete(request: QueryConcreteReq) {
            return api.queryConcrete<TRes, QueryConcreteReq>(route, request);
        },
        queryProject<TRow = Record<string, unknown>>(request: ProjectionQueryReq) {
            return api.queryProject<TRow, ProjectionQueryReq>(route, request);
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
    route: string
): SyncCrudResource<TReq, TRes> {
    return {
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
        deleteById(id: string) {
            return api.request<DeleteResult<TRes>>({
                method: "DELETE",
                path: `${route}/${encodeURIComponent(id)}`,
            });
        },
    };
}
