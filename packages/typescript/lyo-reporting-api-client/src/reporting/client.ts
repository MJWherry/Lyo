import type {
    ApiClient,
    ApiResponse,
    AsyncApiClient,
    CreateResult,
    DeleteResult,
    ExportDownload,
    ExportRequest,
    PatchRequest,
    ProjectedQueryRes,
    QueryRes,
    UpdateResult,
} from "lyo-api-client";
import {fileNameFromDisposition} from "lyo-api-client";
import type {
    ParameterOptionsResolveReq,
    ParameterOptionsResolveRes,
    ProjectionQueryReq,
    QueryConcreteReq,
} from "lyo-query";
import {defaultQueryOptions} from "lyo-query";
import type {
    GenerateReportReq,
    ReportDefinitionParameterReq,
    ReportDefinitionParameterRes,
    ReportDefinitionReq,
    ReportDefinitionRes,
    ReportGenerationRes,
} from "../models/reporting.js";
import {
    REPORT_DEFINITION_DETAIL_INCLUDES,
    REPORT_DEFINITION_GRID_SELECT,
    REPORT_GENERATION_GRID_SELECT,
} from "../models/reporting.js";
import {
    ReportingRoutes,
    joinRoutePrefix,
    reportingGenerationDownloadPath,
    reportingGenerationRerunPath,
} from "../routes.js";

export interface ReportingClientOptions {
    routePrefix?: string;
}

function pathOf(options: ReportingClientOptions | undefined, relative: string): string {
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
    patch(id: string, body: PatchRequest): Promise<ApiResponse<unknown>>;
    deleteById(id: string): Promise<ApiResponse<DeleteResult<TRes>>>;
}

function crud<TReq, TRes>(api: AsyncApiClient, route: string): CrudResource<TReq, TRes> {
    return {
        queryConcrete(request) {
            return api.queryConcrete<TRes, QueryConcreteReq>(route, request);
        },
        queryProject(request) {
            return api.queryProject(route, request);
        },
        get(id, include) {
            return api.getById<TRes>(route, id, include);
        },
        create(body) {
            return api.create<TReq, TRes>(route, body);
        },
        update(id, body) {
            return api.update<TReq, TRes>(route, [id], body);
        },
        patch(id, body) {
            return api.patch(route, {...body, keys: body.keys ?? [[id]]});
        },
        deleteById(id) {
            return api.deleteById<TRes>(route, id);
        },
    };
}

export interface AsyncReportGenerationsClient {
    queryConcrete(request: QueryConcreteReq): Promise<ApiResponse<QueryRes<ReportGenerationRes>>>;
    queryProject<TRow = Record<string, unknown>>(
        request: ProjectionQueryReq
    ): Promise<ApiResponse<ProjectedQueryRes<TRow>>>;
    get(id: string, include?: readonly string[]): Promise<ApiResponse<ReportGenerationRes>>;
    deleteById(id: string): Promise<ApiResponse<DeleteResult<ReportGenerationRes>>>;
    generate(request: GenerateReportReq): Promise<ApiResponse<ReportGenerationRes>>;
    rerun(id: string, includeReportData?: boolean): Promise<ApiResponse<ReportGenerationRes>>;
    download(id: string): Promise<ExportDownload>;
}

export interface AsyncReportingApiClient {
    definitions: CrudResource<ReportDefinitionReq, ReportDefinitionRes> & {
        export(body: ExportRequest): Promise<ExportDownload>;
    };
    definitionParameters: CrudResource<ReportDefinitionParameterReq, ReportDefinitionParameterRes>;
    generations: AsyncReportGenerationsClient;
    resolveParameterOptions(
        body: ParameterOptionsResolveReq
    ): Promise<ApiResponse<ParameterOptionsResolveRes>>;
}

export function createAsyncReportingApiClient(
    api: AsyncApiClient,
    options?: ReportingClientOptions
): AsyncReportingApiClient {
    const p = (relative: string) => pathOf(options, relative);
    const definitionsRoute = p(ReportingRoutes.definitions);
    const generationsRoute = p(ReportingRoutes.generations);
    const definitions = crud<ReportDefinitionReq, ReportDefinitionRes>(api, definitionsRoute);

    return {
        definitions: {
            ...definitions,
            export: (body) => api.export(definitionsRoute, body),
        },
        definitionParameters: crud(api, p(ReportingRoutes.definitionParameters)),
        generations: {
            queryConcrete(request) {
                return api.queryConcrete<ReportGenerationRes, QueryConcreteReq>(generationsRoute, request);
            },
            queryProject(request) {
                return api.queryProject(generationsRoute, request);
            },
            get(id, include) {
                return api.getById<ReportGenerationRes>(generationsRoute, id, include);
            },
            deleteById(id) {
                return api.deleteById<ReportGenerationRes>(generationsRoute, id);
            },
            generate(request) {
                return api.request<ReportGenerationRes, GenerateReportReq>({
                    method: "POST",
                    path: p(ReportingRoutes.generationsGenerate),
                    body: request,
                });
            },
            rerun(id, includeReportData = false) {
                return api.request<ReportGenerationRes>({
                    method: "POST",
                    path: p(reportingGenerationRerunPath(id)),
                    query: includeReportData ? {includeReportData: true} : undefined,
                });
            },
            async download(id) {
                const response = await api.request<unknown>({
                    method: "GET",
                    path: p(reportingGenerationDownloadPath(id)),
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
        },
        resolveParameterOptions(body) {
            return api.request<ParameterOptionsResolveRes, ParameterOptionsResolveReq>({
                method: "POST",
                path: p(ReportingRoutes.resolveParameterOptions),
                body,
            });
        },
    };
}

export function createReportingApiClient(api: ApiClient, options?: ReportingClientOptions) {
    const p = (relative: string) => pathOf(options, relative);
    const definitionsRoute = p(ReportingRoutes.definitions);
    return {
        definitions: {
            queryConcrete(request: QueryConcreteReq) {
                return api.request<QueryRes<ReportDefinitionRes>, QueryConcreteReq>({
                    method: "POST",
                    path: `${definitionsRoute}/QueryConcrete`,
                    body: request,
                });
            },
            get(id: string, include?: readonly string[]) {
                return api.getById<ReportDefinitionRes>(definitionsRoute, id, include);
            },
            create(body: ReportDefinitionReq) {
                return api.request<CreateResult<ReportDefinitionRes>, ReportDefinitionReq>({
                    method: "POST",
                    path: definitionsRoute,
                    body,
                });
            },
            deleteById(id: string) {
                return api.request<DeleteResult<ReportDefinitionRes>>({
                    method: "DELETE",
                    path: `${definitionsRoute}/${encodeURIComponent(id)}`,
                });
            },
        },
        generations: {
            generate(request: GenerateReportReq) {
                return api.request<ReportGenerationRes, GenerateReportReq>({
                    method: "POST",
                    path: p(ReportingRoutes.generationsGenerate),
                    body: request,
                });
            },
            queryConcrete(request: QueryConcreteReq) {
                return api.request<QueryRes<ReportGenerationRes>, QueryConcreteReq>({
                    method: "POST",
                    path: `${p(ReportingRoutes.generations)}/QueryConcrete`,
                    body: request,
                });
            },
        },
        resolveParameterOptions(body: ParameterOptionsResolveReq) {
            return api.request<ParameterOptionsResolveRes, ParameterOptionsResolveReq>({
                method: "POST",
                path: p(ReportingRoutes.resolveParameterOptions),
                body,
            });
        },
    };
}

export function emptyConcreteQuery(amount = 50): QueryConcreteReq {
    return {Amount: amount, Options: defaultQueryOptions({TotalCountMode: "Exact"})};
}

export function definitionGridQuery(amount = 50): ProjectionQueryReq {
    return {
        Amount: amount,
        Select: [...REPORT_DEFINITION_GRID_SELECT],
        Options: defaultQueryOptions({TotalCountMode: "Exact"}),
    };
}

export function generationGridQuery(amount = 50): ProjectionQueryReq {
    return {
        Amount: amount,
        Select: [...REPORT_GENERATION_GRID_SELECT],
        Options: defaultQueryOptions({TotalCountMode: "Exact"}),
    };
}

export function definitionDetailQuery(id: string): QueryConcreteReq {
    return {
        Keys: [[id]],
        Amount: 1,
        Include: [...REPORT_DEFINITION_DETAIL_INCLUDES],
        Options: defaultQueryOptions(),
    };
}
