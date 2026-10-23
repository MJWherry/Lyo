"use client";

import {useEffect, useMemo, useState} from "react";
import type {AsyncApiClient} from "lyo-api-client";
import type {AsyncJobApiClient, JobWorkflowRunRes} from "lyo-job-api-client";
import {JobRoutes, joinRoutePrefix} from "lyo-job-api-client";
import {LyoButton, LyoDataGridFeatureFlags, LyoDataGridProjected, LyoDialog, LyoMenuItem, LyoStack, asLyoQueryClient} from "lyo-web-components";
import {jobRunColumns} from "./columns.js";
import {projectedId} from "./jobColors.js";
import {layoutSteps, type WorkflowStepDraft} from "./workflowGraph.js";
import {JobRunDetailView} from "./dialogs.js";

type Row = Record<string, unknown>;

export function JobWorkflowRunGrid({
    apiClient,
    jobs,
    routePrefix,
    workflowId,
    onOpen,
}: {
    apiClient: AsyncApiClient;
    jobs: AsyncJobApiClient;
    routePrefix?: string;
    workflowId?: string | null;
    onOpen: (id: string) => void;
}) {
    const query = useMemo(() => asLyoQueryClient(apiClient), [apiClient]);
    return (
        <LyoDataGridProjected<Row>
            apiClient={query}
            gridKey="JobWorkflowRunGrid"
            route={joinRoutePrefix(routePrefix, JobRoutes.workflowRuns)}
            columns={jobRunColumns()}
            keySelector={(row) => [projectedId(row)]}
            features={LyoDataGridFeatureFlags.All}
            quickSearchProperties={workflowId ? ["JobWorkflowId"] : undefined}
            rowMenu={(row) => <LyoMenuItem onClick={() => onOpen(projectedId(row))}>Open</LyoMenuItem>}
        />
    );
}

export function JobWorkflowRunDetail({
    open,
    onClose,
    jobs,
    runId,
}: {
    open: boolean;
    onClose: () => void;
    jobs: AsyncJobApiClient;
    runId: string | null;
}) {
    const [run, setRun] = useState<JobWorkflowRunRes | null>(null);
    const [jobRunId, setJobRunId] = useState<string | null>(null);

    useEffect(() => {
        if (!open || !runId) return;
        void jobs.workflowRuns.getGraph(runId).then((res) => setRun(res.data ?? null));
    }, [jobs, open, runId]);

    const steps = Array.isArray(run?.jobWorkflowRunSteps) ? (run.jobWorkflowRunSteps as Array<Record<string, unknown>>) : [];
    const drafts: WorkflowStepDraft[] = steps.map((step, index) => ({
        id: String(step.id ?? index),
        stepName: String(step.stepName ?? step.state ?? `Step ${index + 1}`),
        dependsOn: [],
    }));
    const nodes = layoutSteps(drafts);

    return (
        <LyoDialog open={open} onClose={onClose} title="Workflow run">
            <LyoStack spacing={1}>
                {nodes.map((node) => (
                    <LyoButton key={node.id} size="small" onClick={() => setJobRunId(node.id)}>
                        {node.name}
                    </LyoButton>
                ))}
            </LyoStack>
            <JobRunDetailView open={Boolean(jobRunId)} onClose={() => setJobRunId(null)} jobs={jobs} runId={jobRunId} />
        </LyoDialog>
    );
}
