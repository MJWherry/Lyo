import type {ApiClient, ApiResponse, CreateResult, QueryRes} from "lyo-api-client";
import {withIncludes} from "lyo-api-client";
import type {ProjectionQueryReq, QueryConcreteReq} from "lyo-query";
import type {
    JobCreateChildRunsReq,
    JobDefinitionLatestRunsRes,
    JobDefinitionReq,
    JobDefinitionRes,
    JobDefinitionStatsRes,
    JobRunHeartbeatReq,
    JobRunLogReq,
    JobRunLogRes,
    JobRunReq,
    JobRunRes,
    JobRunResultReq,
    JobRunResyncRes,
    JobRunStartedReq,
    JobWorkerInstanceReq,
    JobWorkerInstanceRes,
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
import {createSyncCrudResource, jobPath, type JobClientOptions, type SyncCrudResource} from "./resource.js";
import {emptyConcreteQuery} from "./queryBuilders.js";

export interface JobApiClient {
    definitions: SyncCrudResource<JobDefinitionReq, JobDefinitionRes> & {
        stats(definitionId: string, days?: number): ApiResponse<JobDefinitionStatsRes>;
        nextRuns(definitionId: string, count?: number): ApiResponse<string[]>;
        latestRuns(definitionIds: string[]): ApiResponse<JobDefinitionLatestRunsRes[]>;
        getDistinctWorkerTypes(): string[];
    };
    runs: {
        queryConcrete(request: QueryConcreteReq): ApiResponse<QueryRes<JobRunRes>>;
        queryProject<TRow = Record<string, unknown>>(
            request: ProjectionQueryReq
        ): ApiResponse<import("lyo-api-client").ProjectedQueryRes<TRow>>;
        get(id: string, include?: readonly string[]): ApiResponse<JobRunRes>;
        create(request: JobRunReq): ApiResponse<CreateResult<JobRunRes>>;
        start(runId: string, request?: JobRunStartedReq | null, include?: readonly string[]): ApiResponse<JobRunRes>;
        finish(runId: string, results: JobRunResultReq[]): ApiResponse<JobRunRes>;
        cancel(runId: string): ApiResponse<JobRunRes>;
        requeue(runId: string): ApiResponse<JobRunRes>;
        rerun(runId: string): ApiResponse<CreateResult<JobRunRes>>;
        resyncQueued(definitionId?: string | null): ApiResponse<JobRunResyncRes>;
        log(runId: string, request: JobRunLogReq): ApiResponse<CreateResult<JobRunLogRes>>;
        heartbeat(runId: string, request?: JobRunHeartbeatReq | null): ApiResponse<JobRunRes>;
        createChildren(parentRunId: string, request: JobCreateChildRunsReq): ApiResponse<JobRunRes[]>;
    };
    workerInstances: SyncCrudResource<JobWorkerInstanceReq, JobWorkerInstanceRes> & {
        register(request: JobWorkerInstanceReq): ApiResponse<CreateResult<JobWorkerInstanceRes>>;
        stop(instanceId: string): ApiResponse<unknown>;
    };
    schedules: SyncCrudResource<unknown, unknown>;
    triggers: SyncCrudResource<unknown, unknown>;
    workflows: SyncCrudResource<unknown, unknown>;
}

export function createJobApiClient(api: ApiClient, options?: JobClientOptions): JobApiClient {
    const p = (relative: string) => jobPath(options, relative);
    const definitionsCrud = createSyncCrudResource<JobDefinitionReq, JobDefinitionRes>(
        api,
        p(JobRoutes.definitions)
    );
    const workers = createSyncCrudResource<JobWorkerInstanceReq, JobWorkerInstanceRes>(
        api,
        p(JobRoutes.workerInstances)
    );
    const runsRoute = p(JobRoutes.runs);

    return {
        definitions: {
            ...definitionsCrud,
            stats(definitionId, days) {
                return api.request<JobDefinitionStatsRes>({
                    method: "GET",
                    path: p(jobDefinitionStatsPath(definitionId)),
                    query: days == null ? undefined : {days},
                });
            },
            nextRuns(definitionId, count) {
                return api.request<string[]>({
                    method: "GET",
                    path: p(jobDefinitionNextRunsPath(definitionId)),
                    query: count == null ? undefined : {count},
                });
            },
            latestRuns(definitionIds) {
                return api.request<JobDefinitionLatestRunsRes[], string[]>({
                    method: "POST",
                    path: p(JobRoutes.definitionsLatestRuns),
                    body: definitionIds,
                });
            },
            getDistinctWorkerTypes() {
                const res = definitionsCrud.queryConcrete(emptyConcreteQuery(500));
                const seen = new Set<string>();
                for (const item of res.data?.items ?? []) {
                    const type = item.workerType?.trim();
                    if (type) seen.add(type);
                }
                return [...seen];
            },
        },
        runs: {
            queryConcrete(request) {
                return api.request<QueryRes<JobRunRes>, QueryConcreteReq>({
                    method: "POST",
                    path: `${runsRoute}/QueryConcrete`,
                    body: request,
                });
            },
            queryProject(request) {
                return api.request({
                    method: "POST",
                    path: `${runsRoute}/QueryProject`,
                    body: request,
                });
            },
            get(id, include) {
                return api.getById<JobRunRes>(runsRoute, id, include);
            },
            create(request) {
                return api.request<CreateResult<JobRunRes>, JobRunReq>({
                    method: "POST",
                    path: p(JobRoutes.runsCreate),
                    body: request,
                });
            },
            start(runId, request, include) {
                const path = withIncludes(p(jobRunStartedPath(runId)), include);
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
            createChildren(parentRunId, request) {
                return api.request<JobRunRes[], JobCreateChildRunsReq>({
                    method: "POST",
                    path: p(jobRunChildrenPath(parentRunId)),
                    body: request,
                });
            },
        },
        workerInstances: {
            ...workers,
            register: workers.create,
            stop(instanceId) {
                return api.patch(p(JobRoutes.workerInstances), {
                    keys: [[instanceId]],
                    properties: {
                        State: "stopped",
                        InFlightCount: 0,
                        LastHeartbeatUtc: new Date().toISOString(),
                    },
                });
            },
        },
        schedules: createSyncCrudResource(api, p(JobRoutes.schedules)),
        triggers: createSyncCrudResource(api, p(JobRoutes.triggers)),
        workflows: createSyncCrudResource(api, p(JobRoutes.workflows)),
    };
}
