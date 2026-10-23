import type {
    ApiClient,
    ApiResponse,
    AsyncApiClient,
    CrudResource,
    DeleteResult,
    ExportDownload,
    ExportRequest,
    ProjectedQueryRes,
    QueryRes,
    SyncCrudResource,
} from "lyo-api-client";
import {createAsyncCrudResource, createSyncCrudResource, fileNameFromDisposition} from "lyo-api-client";
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

function toExportDownload(response: ApiResponse<unknown>): ExportDownload {
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

export interface SyncReportGenerationsClient {
    queryConcrete(request: QueryConcreteReq): ApiResponse<QueryRes<ReportGenerationRes>>;
    queryProject<TRow = Record<string, unknown>>(
        request: ProjectionQueryReq
    ): ApiResponse<ProjectedQueryRes<TRow>>;
    get(id: string, include?: readonly string[]): ApiResponse<ReportGenerationRes>;
    deleteById(id: string): ApiResponse<DeleteResult<ReportGenerationRes>>;
    generate(request: GenerateReportReq): ApiResponse<ReportGenerationRes>;
    rerun(id: string, includeReportData?: boolean): ApiResponse<ReportGenerationRes>;
    download(id: string): ExportDownload;
}

export interface SyncReportingApiClient {
    definitions: SyncCrudResource<ReportDefinitionReq, ReportDefinitionRes> & {
        export(body: ExportRequest): ExportDownload;
    };
    definitionParameters: SyncCrudResource<ReportDefinitionParameterReq, ReportDefinitionParameterRes>;
    generations: SyncReportGenerationsClient;
    resolveParameterOptions(body: ParameterOptionsResolveReq): ApiResponse<ParameterOptionsResolveRes>;
}

export function createAsyncReportingApiClient(
    api: AsyncApiClient,
    options?: ReportingClientOptions
): AsyncReportingApiClient {
    const p = (relative: string) => pathOf(options, relative);
    const definitionsRoute = p(ReportingRoutes.definitions);
    const generationsRoute = p(ReportingRoutes.generations);
    const definitions = createAsyncCrudResource<ReportDefinitionReq, ReportDefinitionRes>(api, definitionsRoute, {
        export: true,
    });

    return {
        definitions: {
            ...definitions,
            export: (body) => api.export(definitionsRoute, body),
        },
        definitionParameters: createAsyncCrudResource(api, p(ReportingRoutes.definitionParameters)),
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
                return toExportDownload(response);
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

export function createReportingApiClient(api: ApiClient, options?: ReportingClientOptions): SyncReportingApiClient {
    const p = (relative: string) => pathOf(options, relative);
    const definitionsRoute = p(ReportingRoutes.definitions);
    const generationsRoute = p(ReportingRoutes.generations);
    const definitions = createSyncCrudResource<ReportDefinitionReq, ReportDefinitionRes>(api, definitionsRoute, {
        export: true,
    });

    return {
        definitions: {
            ...definitions,
            export: (body: ExportRequest) => definitions.export!(body),
        },
        definitionParameters: createSyncCrudResource<ReportDefinitionParameterReq, ReportDefinitionParameterRes>(
            api,
            p(ReportingRoutes.definitionParameters)
        ),
        generations: {
            queryConcrete(request: QueryConcreteReq) {
                return api.request<QueryRes<ReportGenerationRes>, QueryConcreteReq>({
                    method: "POST",
                    path: `${generationsRoute}/QueryConcrete`,
                    body: request,
                });
            },
            queryProject<TRow = Record<string, unknown>>(request: ProjectionQueryReq) {
                return api.request<ProjectedQueryRes<TRow>, ProjectionQueryReq>({
                    method: "POST",
                    path: `${generationsRoute}/QueryProject`,
                    body: request,
                });
            },
            get(id: string, include?: readonly string[]) {
                return api.getById<ReportGenerationRes>(generationsRoute, id, include);
            },
            deleteById(id: string) {
                return api.request<DeleteResult<ReportGenerationRes>>({
                    method: "DELETE",
                    path: `${generationsRoute}/${encodeURIComponent(id)}`,
                });
            },
            generate(request: GenerateReportReq) {
                return api.request<ReportGenerationRes, GenerateReportReq>({
                    method: "POST",
                    path: p(ReportingRoutes.generationsGenerate),
                    body: request,
                });
            },
            rerun(id: string, includeReportData = false) {
                return api.request<ReportGenerationRes>({
                    method: "POST",
                    path: p(reportingGenerationRerunPath(id)),
                    query: includeReportData ? {includeReportData: true} : undefined,
                });
            },
            download(id: string) {
                const response = api.request<unknown>({
                    method: "GET",
                    path: p(reportingGenerationDownloadPath(id)),
                    responseType: "blob",
                });
                return toExportDownload(response);
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
