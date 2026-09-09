"use client";

import {useEffect, useMemo, useState} from "react";
import type {AsyncApiClient} from "lyo-api-client";
import type {AsyncJobApiClient} from "lyo-job-api-client";
import {createAsyncJobApiClient} from "lyo-job-api-client";
import {LyoAlert, LyoCard, LyoProgress, LyoTab, LyoTabs, useLyoSnackbar} from "lyo-web-components";
import {
    JobDefinitionGrid,
    JobRunGrid,
    JobScheduleGrid,
    JobWorkerInstanceGrid,
    JobWorkflowGrid,
} from "./grids.js";
import {JobDefinitionView, JobRunDetailView} from "./dialogs.js";

export type JobManagementTab = "statistics" | "definitions" | "schedules" | "runs" | "workers" | "workflows";

const TAB_INDEX: JobManagementTab[] = [
    "statistics",
    "definitions",
    "schedules",
    "runs",
    "workers",
    "workflows",
];

export type JobManagementProps = {
    apiClient: AsyncApiClient;
    jobs?: AsyncJobApiClient;
    routePrefix?: string;
    /** Host GET path for SpJobStatistic[] (not part of the Job REST group). */
    statisticsPath?: string | null;
    fetchStatistics?: () => Promise<unknown>;
    initialTab?: JobManagementTab | string;
    definitionId?: string | null;
    runId?: string | null;
    createdBy?: string;
};

export function JobStats({
    statisticsPath,
    fetchStatistics,
    apiClient,
}: {
    statisticsPath?: string | null;
    fetchStatistics?: () => Promise<unknown>;
    apiClient: AsyncApiClient;
}) {
    const [data, setData] = useState<unknown>(null);
    const [error, setError] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        if (!statisticsPath && !fetchStatistics) return;
        let cancelled = false;
        setLoading(true);
        const run = fetchStatistics
            ? fetchStatistics()
            : apiClient.request({method: "GET", path: statisticsPath!}).then((r) => r.data);
        run.then((payload) => {
            if (!cancelled) {
                setData(payload);
                setError(null);
            }
        })
            .catch((err: unknown) => {
                if (!cancelled) setError(err instanceof Error ? err.message : "Failed to load statistics");
            })
            .finally(() => {
                if (!cancelled) setLoading(false);
            });
        return () => {
            cancelled = true;
        };
    }, [apiClient, fetchStatistics, statisticsPath]);

    if (!statisticsPath && !fetchStatistics) {
        return <LyoAlert severity="info">Statistics route not configured. Set statisticsPath on JobManagement to enable.</LyoAlert>;
    }
    if (loading) return <LyoProgress />;
    if (error) return <LyoAlert severity="error">{error}</LyoAlert>;
    return (
        <LyoCard sx={{p: 2}}>
            <h3>Statistics</h3>
            <pre style={{whiteSpace: "pre-wrap", fontSize: 12}}>{JSON.stringify(data, null, 2)}</pre>
        </LyoCard>
    );
}

export function JobManagement({
    apiClient,
    jobs: jobsProp,
    routePrefix,
    statisticsPath,
    fetchStatistics,
    initialTab,
    definitionId,
    runId,
    createdBy = "ui",
}: JobManagementProps) {
    const jobs = useMemo(
        () => jobsProp ?? createAsyncJobApiClient(apiClient, {routePrefix}),
        [apiClient, jobsProp, routePrefix]
    );
    const snackbar = useLyoSnackbar();
    const [tab, setTab] = useState(() => resolveTabIndex(initialTab, definitionId, runId));
    const [runsDefinitionId, setRunsDefinitionId] = useState<string | null>(definitionId ?? null);
    const [runsDefinitionName, setRunsDefinitionName] = useState<string | null>(null);
    const [openDef, setOpenDef] = useState<string | null>(null);
    const [openRun, setOpenRun] = useState<string | null>(null);

    useEffect(() => {
        if (runId) {
            setOpenRun(runId);
            setTab(3);
        } else if (definitionId) {
            setOpenDef(definitionId);
            setTab(1);
        }
    }, [definitionId, runId]);

    return (
        <div className="lyo-job-management">
            <LyoTabs value={tab} onChange={(_, value) => setTab(Number(value))}>
                <LyoTab label="Statistics" />
                <LyoTab label="Definitions" />
                <LyoTab label="Schedules" />
                <LyoTab label="Runs" />
                <LyoTab label="Workers" />
                <LyoTab label="Workflows" />
            </LyoTabs>
            <div style={{marginTop: 16}}>
                {tab === 0 ? (
                    <JobStats apiClient={apiClient} statisticsPath={statisticsPath} fetchStatistics={fetchStatistics} />
                ) : null}
                {tab === 1 ? (
                    <JobDefinitionGrid
                        apiClient={apiClient}
                        jobs={jobs}
                        routePrefix={routePrefix}
                        createdBy={createdBy}
                        onViewRuns={(id, name) => {
                            setRunsDefinitionId(id);
                            setRunsDefinitionName(name ?? null);
                            setTab(3);
                            snackbar.show(`Showing runs for ${name ?? id}`, "info");
                        }}
                    />
                ) : null}
                {tab === 2 ? <JobScheduleGrid apiClient={apiClient} routePrefix={routePrefix} /> : null}
                {tab === 3 ? (
                    <JobRunGrid
                        apiClient={apiClient}
                        jobs={jobs}
                        routePrefix={routePrefix}
                        createdBy={createdBy}
                        definitionId={runsDefinitionId}
                        definitionName={runsDefinitionName}
                        onClearDefinition={() => {
                            setRunsDefinitionId(null);
                            setRunsDefinitionName(null);
                        }}
                    />
                ) : null}
                {tab === 4 ? <JobWorkerInstanceGrid apiClient={apiClient} routePrefix={routePrefix} /> : null}
                {tab === 5 ? <JobWorkflowGrid apiClient={apiClient} routePrefix={routePrefix} /> : null}
            </div>
            <JobDefinitionView open={Boolean(openDef)} onClose={() => setOpenDef(null)} jobs={jobs} definitionId={openDef} />
            <JobRunDetailView open={Boolean(openRun)} onClose={() => setOpenRun(null)} jobs={jobs} runId={openRun} />
        </div>
    );
}

function resolveTabIndex(initialTab?: string, definitionId?: string | null, runId?: string | null): number {
    if (runId) return 3;
    if (definitionId) return 1;
    const key = (initialTab ?? "").trim().toLowerCase() as JobManagementTab;
    const idx = TAB_INDEX.indexOf(key);
    return idx >= 0 ? idx : 0;
}
