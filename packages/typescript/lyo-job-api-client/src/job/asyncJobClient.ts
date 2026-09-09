import type {
    ApiResponse,
    AsyncApiClient,
    CreateResult,
    DeleteResult,
    ExportDownload,
    ExportRequest,
    ProjectedQueryRes,
    QueryRes,
} from "lyo-api-client";
import type {ProjectionQueryReq, QueryConcreteReq} from "lyo-query";
import {emptyConcreteQuery} from "./queryBuilders.js";
import {createAsyncCrudResource, jobPath, type CrudResource, type JobClientOptions} from "./resource.js";
import type {
    JobBlackoutCalendarReq,
    JobBlackoutCalendarRes,
    JobBlackoutWindowReq,
    JobBlackoutWindowRes,
    JobCreateChildRunsReq,
    JobDefinitionLatestRunsRes,
    JobDefinitionReq,
    JobDefinitionRes,
    JobDefinitionStatsRes,
    JobParameterReq,
    JobParameterRes,
    JobRunHeartbeatReq,
    JobRunLogReq,
    JobRunLogRes,
    JobRunParameterReq,
    JobRunParameterRes,
    JobRunReq,
    JobRunRes,
    JobRunResultReq,
    JobRunResultRes,
    JobRunResyncRes,
    JobRunStartedReq,
    JobScheduleParameterReq,
    JobScheduleParameterRes,
    JobScheduleReq,
    JobScheduleRes,
    JobTriggerReq,
    JobTriggerRes,
    JobWorkerInstanceReq,
    JobWorkerInstanceRes,
    JobWorkflowReq,
    JobWorkflowRes,
    JobWorkflowRunReq,
    JobWorkflowRunRes,
    JobWorkflowRunStepReq,
    JobWorkflowRunStepRes,
    JobWorkflowStepReq,
    JobWorkflowStepRes,
} from "../models/job.js";
import {
    JobRoutes,
    jobDefinitionNextRunsPath,
    jobDefinitionStatsPath,
    jobRunCancelPath,
    jobRunChildrenPath,
    jobRunFinishedPath,
    jobRunHeartbeatPath,
    jobRunLogPath,
    jobRunRequeuePath,
    jobRunRerunPath,
    jobRunStartedPath,
} from "../routes.js";

export interface AsyncJobDefinitionsClient
    extends CrudResource<JobDefinitionReq, JobDefinitionRes> {
    export(body: ExportRequest): Promise<ExportDownload>;
    stats(definitionId: string, days?: number): Promise<ApiResponse<JobDefinitionStatsRes>>;
    nextRuns(definitionId: string, count?: number): Promise<ApiResponse<string[]>>;
    latestRuns(definitionIds: string[]): Promise<ApiResponse<JobDefinitionLatestRunsRes[]>>;
    getDistinctWorkerTypes(): Promise<string[]>;
}

export interface AsyncJobRunsClient {
    queryConcrete(request: QueryConcreteReq): Promise<ApiResponse<QueryRes<JobRunRes>>>;
    queryProject<TRow = Record<string, unknown>>(
        request: ProjectionQueryReq
    ): Promise<ApiResponse<ProjectedQueryRes<TRow>>>;
    get(id: string, include?: readonly string[]): Promise<ApiResponse<JobRunRes>>;
    deleteById(id: string): Promise<ApiResponse<DeleteResult<JobRunRes>>>;
    create(request: JobRunReq): Promise<ApiResponse<CreateResult<JobRunRes>>>;
    start(runId: string, request?: JobRunStartedReq | null, include?: readonly string[]): Promise<ApiResponse<JobRunRes>>;
    finish(runId: string, results: JobRunResultReq[]): Promise<ApiResponse<JobRunRes>>;
    cancel(runId: string): Promise<ApiResponse<JobRunRes>>;
    requeue(runId: string): Promise<ApiResponse<JobRunRes>>;
    rerun(runId: string): Promise<ApiResponse<CreateResult<JobRunRes>>>;
    resyncQueued(definitionId?: string | null): Promise<ApiResponse<JobRunResyncRes>>;
    log(runId: string, request: JobRunLogReq): Promise<ApiResponse<CreateResult<JobRunLogRes>>>;
    heartbeat(runId: string, request?: JobRunHeartbeatReq | null): Promise<ApiResponse<JobRunRes>>;
    patchProgress(runId: string, percent: number, message?: string | null): Promise<ApiResponse<unknown>>;
    createChildren(parentRunId: string, request: JobCreateChildRunsReq): Promise<ApiResponse<JobRunRes[]>>;
}

export interface AsyncJobWorkerInstancesClient extends CrudResource<JobWorkerInstanceReq, JobWorkerInstanceRes> {
    register(request: JobWorkerInstanceReq): Promise<ApiResponse<CreateResult<JobWorkerInstanceRes>>>;
    heartbeat(
        instanceId: string,
        inFlightCount: number,
        metadata?: Record<string, string | null> | null
    ): Promise<ApiResponse<unknown>>;
    stop(instanceId: string): Promise<ApiResponse<unknown>>;
}

export interface AsyncJobApiClient {
    definitions: AsyncJobDefinitionsClient;
    definitionParameters: CrudResource<JobParameterReq, JobParameterRes>;
    runs: AsyncJobRunsClient;
    runParameters: CrudResource<JobRunParameterReq, JobRunParameterRes>;
    runResults: CrudResource<JobRunResultReq, JobRunResultRes>;
    runLogs: CrudResource<JobRunLogReq, JobRunLogRes>;
    schedules: CrudResource<JobScheduleReq, JobScheduleRes>;
    scheduleParameters: CrudResource<JobScheduleParameterReq, JobScheduleParameterRes>;
    triggers: CrudResource<JobTriggerReq, JobTriggerRes>;
    workerInstances: AsyncJobWorkerInstancesClient;
    blackoutCalendars: CrudResource<JobBlackoutCalendarReq, JobBlackoutCalendarRes>;
    blackoutWindows: CrudResource<JobBlackoutWindowReq, JobBlackoutWindowRes>;
    workflows: CrudResource<JobWorkflowReq, JobWorkflowRes>;
    workflowSteps: CrudResource<JobWorkflowStepReq, JobWorkflowStepRes>;
    workflowRuns: CrudResource<JobWorkflowRunReq, JobWorkflowRunRes>;
    workflowRunSteps: CrudResource<JobWorkflowRunStepReq, JobWorkflowRunStepRes>;
}

export function createAsyncJobApiClient(
    api: AsyncApiClient,
    options?: JobClientOptions
): AsyncJobApiClient {
    const p = (relative: string) => jobPath(options, relative);
    const definitionsRoute = p(JobRoutes.definitions);
    const runsRoute = p(JobRoutes.runs);
    const workersRoute = p(JobRoutes.workerInstances);

    const definitionsCrud = createAsyncCrudResource<JobDefinitionReq, JobDefinitionRes>(
        api,
        definitionsRoute,
        {export: true}
    );

    const workerCrud = createAsyncCrudResource<JobWorkerInstanceReq, JobWorkerInstanceRes>(
        api,
        workersRoute
    );

    const definitions: AsyncJobDefinitionsClient = {
        ...definitionsCrud,
        export: (body: ExportRequest) => api.export(definitionsRoute, body),
        stats(definitionId: string, days?: number) {
            return api.request<JobDefinitionStatsRes>({
                method: "GET",
                path: p(jobDefinitionStatsPath(definitionId)),
                query: days == null ? undefined : {days},
            });
        },
        nextRuns(definitionId: string, count?: number) {
            return api.request<string[]>({
                method: "GET",
                path: p(jobDefinitionNextRunsPath(definitionId)),
                query: count == null ? undefined : {count},
            });
        },
        latestRuns(definitionIds: string[]) {
            return api.request<JobDefinitionLatestRunsRes[], string[]>({
                method: "POST",
                path: p(JobRoutes.definitionsLatestRuns),
                body: definitionIds,
            });
        },
        async getDistinctWorkerTypes() {
            const res = await definitionsCrud.queryConcrete(emptyConcreteQuery(500));
            const items = res.data?.items ?? [];
            const seen = new Set<string>();
            for (const item of items) {
                const type = item.workerType?.trim();
                if (type) seen.add(type);
            }
            return [...seen];
        },
    };

    const runs: AsyncJobRunsClient = {
        queryConcrete(request) {
            return api.queryConcrete<JobRunRes, QueryConcreteReq>(runsRoute, request);
        },
        queryProject(request) {
            return api.queryProject(runsRoute, request);
        },
        get(id, include) {
            return api.getById<JobRunRes>(runsRoute, id, include);
        },
        deleteById(id) {
            return api.deleteById<JobRunRes>(runsRoute, id);
        },
        create(request) {
            return api.request<CreateResult<JobRunRes>, JobRunReq>({
                method: "POST",
                path: p(JobRoutes.runsCreate),
                body: request,
            });
        },
        start(runId, request, include) {
            const path = include?.length
                ? `${p(jobRunStartedPath(runId))}?${include.map((i) => `include=${encodeURIComponent(i)}`).join("&")}`
                : p(jobRunStartedPath(runId));
            return request
                ? api.request<JobRunRes, JobRunStartedReq>({method: "POST", path, body: request})
                : api.request<JobRunRes>({method: "POST", path});
        },
        finish(runId, results) {
            return api.request<JobRunRes, JobRunResultReq[]>({
                method: "POST",
                path: p(jobRunFinishedPath(runId)),
                body: results,
            });
        },
        cancel(runId) {
            return api.request<JobRunRes>({method: "POST", path: p(jobRunCancelPath(runId))});
        },
        requeue(runId) {
            return api.request<JobRunRes>({method: "POST", path: p(jobRunRequeuePath(runId))});
        },
        rerun(runId) {
            return api.request<CreateResult<JobRunRes>>({
                method: "POST",
                path: p(jobRunRerunPath(runId)),
            });
        },
        resyncQueued(definitionId) {
            return api.request<JobRunResyncRes>({
                method: "POST",
                path: p(JobRoutes.runsResync),
                query: definitionId ? {definitionId} : undefined,
            });
        },
        log(runId, request) {
            return api.request<CreateResult<JobRunLogRes>, JobRunLogReq>({
                method: "POST",
                path: p(jobRunLogPath(runId)),
                body: request,
            });
        },
        heartbeat(runId, request) {
            return api.request<JobRunRes, JobRunHeartbeatReq | undefined>({
                method: "PATCH",
                path: p(jobRunHeartbeatPath(runId)),
                body: request ?? undefined,
            });
        },
        patchProgress(runId, percent, message) {
            const properties: Record<string, unknown> = {
                ProgressPercent: percent,
                LastHeartbeatUtc: new Date().toISOString(),
            };
            if (message != null) properties.ProgressMessage = message;
            return api.patch(p(`${JobRoutes.runs}/${runId}`), {
                keys: [[runId]],
                properties,
            });
        },
        createChildren(parentRunId, request) {
            return api.request<JobRunRes[], JobCreateChildRunsReq>({
                method: "POST",
                path: p(jobRunChildrenPath(parentRunId)),
                body: request,
            });
        },
    };

    const workerInstances: AsyncJobWorkerInstancesClient = {
        ...workerCrud,
        register: workerCrud.create,
        heartbeat(instanceId, inFlightCount, metadata) {
            const properties: Record<string, unknown> = {
                LastHeartbeatUtc: new Date().toISOString(),
                InFlightCount: inFlightCount,
            };
            if (metadata && Object.keys(metadata).length > 0) {
                properties.MetadataJson = JSON.stringify(metadata);
            }
            return api.patch(workersRoute, {keys: [[instanceId]], properties});
        },
        stop(instanceId) {
            return api.patch(workersRoute, {
                keys: [[instanceId]],
                properties: {
                    State: "stopped",
                    InFlightCount: 0,
                    LastHeartbeatUtc: new Date().toISOString(),
                },
            });
        },
    };

    return {
        definitions,
        definitionParameters: createAsyncCrudResource(api, p(JobRoutes.definitionParameters)),
        runs,
        runParameters: createAsyncCrudResource(api, p(JobRoutes.runParameters)),
        runResults: createAsyncCrudResource(api, p(JobRoutes.runResults)),
        runLogs: createAsyncCrudResource(api, p(JobRoutes.runLogs)),
        schedules: createAsyncCrudResource(api, p(JobRoutes.schedules)),
        scheduleParameters: createAsyncCrudResource(api, p(JobRoutes.scheduleParameters)),
        triggers: createAsyncCrudResource(api, p(JobRoutes.triggers)),
        workerInstances,
        blackoutCalendars: createAsyncCrudResource(api, p(JobRoutes.blackoutCalendars)),
        blackoutWindows: createAsyncCrudResource(api, p(JobRoutes.blackoutWindows)),
        workflows: createAsyncCrudResource(api, p(JobRoutes.workflows)),
        workflowSteps: createAsyncCrudResource(api, p(JobRoutes.workflowSteps)),
        workflowRuns: createAsyncCrudResource(api, p(JobRoutes.workflowRuns)),
        workflowRunSteps: createAsyncCrudResource(api, p(JobRoutes.workflowRunSteps)),
    };
}
