"use client";

import {useMemo, useState} from "react";
import type {AsyncApiClient} from "lyo-api-client";
import type {AsyncJobApiClient} from "lyo-job-api-client";
import {JobRoutes, joinRoutePrefix} from "lyo-job-api-client";
import {filterProperty} from "lyo-query";
import {
    LyoButton,
    LyoDataGridFeatureFlags,
    LyoDataGridProjected,
    LyoMenuItem,
    LyoStack,
    asLyoQueryClient,
    useLyoSnackbar,
} from "lyo-web-components";
import {jobDefinitionColumns, jobRunColumns, jobScheduleColumns, jobWorkerColumns, jobWorkflowColumns} from "./columns.js";
import {canCancelRun, projectedField, projectedId} from "./jobColors.js";
import {JobDefinitionView, JobRunDetailView, RunJobDialog} from "./dialogs.js";

type Row = Record<string, unknown>;

export function JobDefinitionGrid({
    apiClient,
    jobs,
    routePrefix,
    onViewRuns,
    createdBy,
}: {
    apiClient: AsyncApiClient;
    jobs: AsyncJobApiClient;
    routePrefix?: string;
    onViewRuns?: (id: string, name?: string) => void;
    createdBy?: string;
}) {
    const snackbar = useLyoSnackbar();
    const query = useMemo(() => asLyoQueryClient(apiClient), [apiClient]);
    const route = joinRoutePrefix(routePrefix, JobRoutes.definitions);
    const [runFor, setRunFor] = useState<string | null>(null);
    const [editId, setEditId] = useState<string | null>(null);
    const columns = useMemo(() => jobDefinitionColumns(), []);

    return (
        <>
            <LyoDataGridProjected<Row>
                apiClient={query}
                gridKey="JobDefinitionGrid"
                route={route}
                columns={columns}
                keySelector={(row) => [projectedId(row)]}
                features={LyoDataGridFeatureFlags.All}
                filterPropertyDefinitions={[
                    filterProperty("Name"),
                    filterProperty("Type"),
                    filterProperty("WorkerType", {displayName: "Worker Type"}),
                    filterProperty("Enabled", {type: "Bool"}),
                ]}
                quickSearchProperties={["Name", "Description", "Type"]}
                leftControls={
                    <LyoButton size="small" variant="outlined" onClick={() => snackbar.show("Select a row, then Run.", "info")}>
                        Create Job Run
                    </LyoButton>
                }
                rowMenu={(row) => (
                    <>
                        <LyoMenuItem onClick={() => setEditId(projectedId(row))}>Edit</LyoMenuItem>
                        <LyoMenuItem onClick={() => setRunFor(projectedId(row))}>Run</LyoMenuItem>
                        <LyoMenuItem onClick={() => onViewRuns?.(projectedId(row), String(projectedField(row, "Name") ?? ""))}>
                            View Runs
                        </LyoMenuItem>
                        <LyoMenuItem
                            onClick={async () => {
                                const id = projectedId(row);
                                const enabled = Boolean(projectedField(row, "Enabled"));
                                await jobs.definitions.patch({keys: [[id]], properties: {Enabled: !enabled}});
                                snackbar.show(enabled ? "Disabled." : "Enabled.", "success");
                            }}
                        >
                            Toggle Enabled
                        </LyoMenuItem>
                    </>
                )}
            />
            <RunJobDialog open={Boolean(runFor)} onClose={() => setRunFor(null)} jobs={jobs} definitionId={runFor} createdBy={createdBy} />
            <JobDefinitionView open={Boolean(editId)} onClose={() => setEditId(null)} jobs={jobs} definitionId={editId} />
        </>
    );
}

export function JobRunGrid({
    apiClient,
    jobs,
    routePrefix,
    definitionId,
    definitionName,
    onClearDefinition,
    createdBy,
}: {
    apiClient: AsyncApiClient;
    jobs: AsyncJobApiClient;
    routePrefix?: string;
    definitionId?: string | null;
    definitionName?: string | null;
    onClearDefinition?: () => void;
    createdBy?: string;
}) {
    const snackbar = useLyoSnackbar();
    const query = useMemo(() => asLyoQueryClient(apiClient), [apiClient]);
    const route = joinRoutePrefix(routePrefix, JobRoutes.runs);
    const [detailId, setDetailId] = useState<string | null>(null);
    const [createOpen, setCreateOpen] = useState(false);
    const columns = useMemo(() => jobRunColumns(), []);

    return (
        <>
            <LyoDataGridProjected<Row>
                apiClient={query}
                gridKey="JobRunGrid"
                route={route}
                columns={columns}
                keySelector={(row) => [projectedId(row)]}
                features={LyoDataGridFeatureFlags.All}
                beforeQuery={(req) => {
                    if (!definitionId) return req;
                    return {
                        ...req,
                        whereClause: {
                            $type: "condition",
                            Field: "JobDefinitionId",
                            Comparison: "Equals",
                            Value: definitionId,
                        },
                    };
                }}
                leftControls={
                    <LyoStack direction="row" spacing={1} alignItems="center">
                        <LyoButton size="small" variant="outlined" onClick={() => setCreateOpen(true)}>
                            Create Job Run
                        </LyoButton>
                        <LyoButton
                            size="small"
                            variant="outlined"
                            onClick={async () => {
                                const res = await jobs.runs.resyncQueued(definitionId ?? undefined);
                                snackbar.show(
                                    `Resync queued=${res.data?.queued ?? 0} republished=${res.data?.republished ?? 0}`,
                                    "info"
                                );
                            }}
                        >
                            Resync RabbitMQ
                        </LyoButton>
                        {definitionId ? (
                            <LyoButton size="small" onClick={onClearDefinition}>
                                Definition: {definitionName ?? definitionId.slice(0, 8)}
                            </LyoButton>
                        ) : null}
                    </LyoStack>
                }
                rowMenu={(row) => (
                    <>
                        <LyoMenuItem onClick={() => setDetailId(projectedId(row))}>View</LyoMenuItem>
                        <LyoMenuItem
                            onClick={async () => {
                                await jobs.runs.rerun(projectedId(row));
                                snackbar.show("Re-run created.", "success");
                            }}
                        >
                            Re-run
                        </LyoMenuItem>
                        <LyoMenuItem
                            disabled={!canCancelRun(row)}
                            onClick={async () => {
                                await jobs.runs.cancel(projectedId(row));
                                snackbar.show("Cancel requested.", "success");
                            }}
                        >
                            Cancel
                        </LyoMenuItem>
                    </>
                )}
            />
            <JobRunDetailView open={Boolean(detailId)} onClose={() => setDetailId(null)} jobs={jobs} runId={detailId} />
            <RunJobDialog
                open={createOpen}
                onClose={() => setCreateOpen(false)}
                jobs={jobs}
                definitionId={definitionId}
                createdBy={createdBy}
            />
        </>
    );
}

export function JobScheduleGrid({apiClient, routePrefix}: {apiClient: AsyncApiClient; routePrefix?: string}) {
    const query = useMemo(() => asLyoQueryClient(apiClient), [apiClient]);
    return (
        <LyoDataGridProjected<Row>
            apiClient={query}
            gridKey="JobScheduleGrid"
            route={joinRoutePrefix(routePrefix, JobRoutes.schedules)}
            columns={jobScheduleColumns()}
            keySelector={(row) => [projectedId(row)]}
            features={LyoDataGridFeatureFlags.All}
            filterPropertyDefinitions={[filterProperty("Description"), filterProperty("Enabled", {type: "Bool"})]}
        />
    );
}

export function JobWorkerInstanceGrid({apiClient, routePrefix}: {apiClient: AsyncApiClient; routePrefix?: string}) {
    const query = useMemo(() => asLyoQueryClient(apiClient), [apiClient]);
    return (
        <LyoDataGridProjected<Row>
            apiClient={query}
            gridKey="JobWorkerInstanceGrid"
            route={joinRoutePrefix(routePrefix, JobRoutes.workerInstances)}
            columns={jobWorkerColumns()}
            keySelector={(row) => [projectedId(row)]}
            features={LyoDataGridFeatureFlags.All}
            filterPropertyDefinitions={[filterProperty("WorkerType"), filterProperty("MachineName")]}
        />
    );
}

export function JobWorkflowGrid({apiClient, routePrefix}: {apiClient: AsyncApiClient; routePrefix?: string}) {
    const query = useMemo(() => asLyoQueryClient(apiClient), [apiClient]);
    return (
        <LyoDataGridProjected<Row>
            apiClient={query}
            gridKey="JobWorkflowGrid"
            route={joinRoutePrefix(routePrefix, JobRoutes.workflows)}
            columns={jobWorkflowColumns()}
            keySelector={(row) => [projectedId(row)]}
            features={LyoDataGridFeatureFlags.All}
            filterPropertyDefinitions={[filterProperty("Name"), filterProperty("Enabled", {type: "Bool"})]}
            quickSearchProperties={["Name", "Description"]}
        />
    );
}
