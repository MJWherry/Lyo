"use client";

import {useEffect, useMemo, useState, type ReactNode} from "react";
import EditIcon from "@mui/icons-material/Edit";
import PlayArrowIcon from "@mui/icons-material/PlayArrow";
import VisibilityIcon from "@mui/icons-material/Visibility";
import type {AsyncApiClient} from "lyo-api-client";
import type {AsyncJobApiClient, JobDefinitionRes, JobScheduleRes} from "lyo-job-api-client";
import {emptyConcreteQuery, JobRoutes, joinRoutePrefix} from "lyo-job-api-client";
import {filterProperty} from "lyo-query";
import {
    LyoButton,
    LyoCalendar,
    type CalendarEntry,
    LyoDataGridFeatureFlags,
    LyoDataGridProjected,
    LyoIconButton,
    LyoMenuItem,
    LyoStack,
    LyoToolbarButton,
    LyoTooltip,
    asLyoQueryClient,
    useLyoSnackbar,
} from "lyo-web-components";
import {jobDefinitionColumns, jobRunColumns, jobScheduleColumns, jobWorkerColumns, jobWorkflowColumns} from "./columns.js";
import {canCancelRun, projectedField, projectedId} from "./jobColors.js";
import {JobDefinitionView, JobRunDetailView, RunJobDialog} from "./dialogs.js";
import {schedulesCalendar} from "./scheduleCalendar.js";
import {JobWorkerInstanceView, JobWorkflowView} from "./workflowView.js";

type Row = Record<string, unknown>;

function JobFireDetail({entry, definitions}: {entry: CalendarEntry; definitions: JobDefinitionRes[]}) {
    const match = findSchedule(entry, definitions);
    if (!match) return <span>{entry.title}</span>;
    const {definition, schedule} = match;
    const blackout = schedule.jobBlackoutCalendar;
    return (
        <LyoStack spacing={1}>
            <strong>{definition.name}</strong>
            <span>{schedule.description || "—"}</span>
            <span>Type {String(schedule.type)}</span>
            <span>Schedule {schedule.id.slice(0, 8)}…</span>
            <span>{schedule.timeZoneId || "UTC"}</span>
            <span>{schedule.enabled === false ? "Disabled" : "Enabled"}</span>
            {blackout?.name ? <span>Blackout {blackout.name}</span> : null}
            {entry.muted ? <span>This time is inside a blackout, so the job will not run.</span> : null}
        </LyoStack>
    );
}

function findSchedule(entry: CalendarEntry, definitions: JobDefinitionRes[]): {definition: JobDefinitionRes; schedule: JobScheduleRes} | null {
    for (const definition of definitions) {
        for (const schedule of definition.jobSchedules ?? []) {
            if (entry.key.includes(schedule.id)) return {definition, schedule};
        }
    }
    return null;
}

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
    const [reloadToken, setReloadToken] = useState(0);
    const [runFor, setRunFor] = useState<string | null>(null);
    const [createOpen, setCreateOpen] = useState(false);
    const [editId, setEditId] = useState<string | null>(null);
    const toggleEnabled = async (row: Row) => {
        const id = projectedId(row);
        const enabled = Boolean(projectedField(row, "Enabled"));
        await jobs.definitions.patch({keys: [[id]], properties: {Enabled: !enabled}});
        setReloadToken((token) => token + 1);
        snackbar.show(enabled ? "Disabled." : "Enabled.", "success");
    };
    const columns = useMemo(() => jobDefinitionColumns({onToggleEnabled: (row) => void toggleEnabled(row)}), [jobs, snackbar]);

    return (
        <>
            <LyoDataGridProjected<Row>
                apiClient={query}
                gridKey="JobDefinitionGrid"
                reloadToken={reloadToken}
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
                    <LyoToolbarButton icon={<PlayArrowIcon fontSize="small" />} onClick={() => setCreateOpen(true)}>
                        Create Job Run
                    </LyoToolbarButton>
                }
                rowQuickActions={(row) => (
                    <>
                        <QuickIcon title="Run" onClick={() => setRunFor(projectedId(row))}>
                            <PlayArrowIcon fontSize="small" />
                        </QuickIcon>
                        <QuickIcon title="Edit" onClick={() => setEditId(projectedId(row))}>
                            <EditIcon fontSize="small" />
                        </QuickIcon>
                    </>
                )}
                rowMenu={(row) => (
                    <>
                        <LyoMenuItem onClick={() => setEditId(projectedId(row))}>Edit</LyoMenuItem>
                        <LyoMenuItem onClick={() => setRunFor(projectedId(row))}>Run</LyoMenuItem>
                        <LyoMenuItem onClick={() => onViewRuns?.(projectedId(row), String(projectedField(row, "Name") ?? ""))}>
                            View Runs
                        </LyoMenuItem>
                        <LyoMenuItem onClick={() => void toggleEnabled(row)}>Toggle Enabled</LyoMenuItem>
                    </>
                )}
            />
            <RunJobDialog
                open={createOpen || Boolean(runFor)}
                onClose={() => {
                    setCreateOpen(false);
                    setRunFor(null);
                }}
                jobs={jobs}
                definitionId={runFor}
                createdBy={createdBy}
            />
            <JobDefinitionView open={Boolean(editId)} onClose={() => setEditId(null)} jobs={jobs} definitionId={editId} apiClient={apiClient} />
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
    const [reloadToken, setReloadToken] = useState(0);
    const [detailId, setDetailId] = useState<string | null>(null);
    const [createOpen, setCreateOpen] = useState(false);
    const columns = useMemo(() => jobRunColumns(), []);

    return (
        <>
            <LyoDataGridProjected<Row>
                apiClient={query}
                gridKey="JobRunGrid"
                reloadToken={reloadToken}
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
                        <LyoToolbarButton icon={<PlayArrowIcon fontSize="small" />} onClick={() => setCreateOpen(true)}>
                            Create Job Run
                        </LyoToolbarButton>
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
                rowQuickActions={(row) => (
                    <QuickIcon title="View" onClick={() => setDetailId(projectedId(row))}>
                        <VisibilityIcon fontSize="small" />
                    </QuickIcon>
                )}
                rowMenu={(row) => (
                    <>
                        <LyoMenuItem onClick={() => setDetailId(projectedId(row))}>View</LyoMenuItem>
                        <LyoMenuItem
                            onClick={async () => {
                                await jobs.runs.rerun(projectedId(row));
                                setReloadToken((token) => token + 1);
                                snackbar.show("Re-run created.", "success");
                            }}
                        >
                            Re-run
                        </LyoMenuItem>
                        <LyoMenuItem
                            disabled={!canCancelRun(row)}
                            onClick={async () => {
                                await jobs.runs.cancel(projectedId(row));
                                setReloadToken((token) => token + 1);
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

export function JobScheduleGrid({
    apiClient,
    jobs,
    routePrefix,
    createdBy = "ui",
}: {
    apiClient: AsyncApiClient;
    jobs: AsyncJobApiClient;
    routePrefix?: string;
    createdBy?: string;
}) {
    const snackbar = useLyoSnackbar();
    const query = useMemo(() => asLyoQueryClient(apiClient), [apiClient]);
    const [month, setMonth] = useState(() => {
        const now = new Date();
        return {year: now.getUTCFullYear(), month: now.getUTCMonth() + 1};
    });
    const [zone, setZone] = useState("UTC");
    const [use24Hour, setUse24Hour] = useState(true);
    const [reloadToken, setReloadToken] = useState(0);
    const [definitions, setDefinitions] = useState<JobDefinitionRes[]>([]);
    const [calendar, setCalendar] = useState<ReturnType<typeof schedulesCalendar> | null>(null);

    useEffect(() => {
        let cancelled = false;
        void jobs.definitions
            .queryConcrete({
                ...emptyConcreteQuery(200),
                Include: ["JobSchedules.JobBlackoutCalendar.JobBlackoutWindows"],
            })
            .then((res) => {
                const items = res.data?.items ?? [];
                if (!cancelled) {
                    setDefinitions(items);
                    setCalendar(schedulesCalendar(month.year, month.month, zone, items));
                }
            })
            .catch(() => {
                if (!cancelled) setCalendar(null);
            });
        return () => {
            cancelled = true;
        };
    }, [jobs, month.month, month.year, zone, reloadToken]);

    const toggleEnabled = async (row: Row) => {
        const id = projectedId(row);
        const enabled = Boolean(projectedField(row, "Enabled"));
        await jobs.schedules.patch({keys: [[id]], properties: {Enabled: !enabled}});
        setReloadToken((token) => token + 1);
        snackbar.show(enabled ? "Disabled." : "Enabled.", "success");
    };
    return (
        <LyoStack spacing={2}>
        {calendar ? (
            <LyoCalendar
                calendar={calendar.calendar}
                timeZone={zone}
                onTimeZoneChange={setZone}
                use24Hour={use24Hour}
                onUse24HourChange={setUse24Hour}
                onVisibleMonthChange={(monthStart) => {
                    const [year, monthNumber] = monthStart.split("-").map(Number);
                    setMonth((current) => (current.year === year && current.month === monthNumber ? current : {year, month: monthNumber}));
                }}
                categoryHues={{SetTimes: 228, Interval: 174, IntervalWindow: 174, OneShot: 252, Cron: 292, blackout: 52}}
                renderDetail={(entry) => <JobFireDetail entry={entry} definitions={definitions} />}
            />
        ) : null}
        {calendar && calendar.holidayNames.length > 0 ? <span>Holidays: {calendar.holidayNames.join(", ")}</span> : null}
        <LyoDataGridProjected<Row>
            apiClient={query}
            gridKey="JobScheduleGrid"
            reloadToken={reloadToken}
            route={joinRoutePrefix(routePrefix, JobRoutes.schedules)}
            columns={jobScheduleColumns({onToggleEnabled: (row) => void toggleEnabled(row)})}
            keySelector={(row) => [projectedId(row)]}
            features={LyoDataGridFeatureFlags.All}
            filterPropertyDefinitions={[filterProperty("Description"), filterProperty("Enabled", {type: "Bool"})]}
            rowMenu={(row) => (
                <>
                    <LyoMenuItem onClick={() => void toggleEnabled(row)}>Toggle</LyoMenuItem>
                    <LyoMenuItem
                        onClick={async () => {
                            const definitionId = String(projectedField(row, "JobDefinitionId") ?? "");
                            await jobs.runs.create({
                                jobDefinitionId: definitionId,
                                jobScheduleId: projectedId(row),
                                createdBy,
                            });
                            snackbar.show("Run queued.", "success");
                        }}
                    >
                        Run schedule
                    </LyoMenuItem>
                </>
            )}
        />
        </LyoStack>
    );
}

export function JobWorkerInstanceGrid({apiClient, routePrefix}: {apiClient: AsyncApiClient; routePrefix?: string}) {
    const query = useMemo(() => asLyoQueryClient(apiClient), [apiClient]);
    const [instance, setInstance] = useState<Row | null>(null);
    return (
        <>
            <LyoDataGridProjected<Row>
                apiClient={query}
                gridKey="JobWorkerInstanceGrid"
                route={joinRoutePrefix(routePrefix, JobRoutes.workerInstances)}
                columns={jobWorkerColumns()}
                keySelector={(row) => [projectedId(row)]}
                features={LyoDataGridFeatureFlags.All}
                filterPropertyDefinitions={[filterProperty("WorkerType"), filterProperty("MachineName")]}
                rowQuickActions={(row) => (
                    <QuickIcon title="Details" onClick={() => setInstance(row)}>
                        <VisibilityIcon fontSize="small" />
                    </QuickIcon>
                )}
                rowMenu={(row) => <LyoMenuItem onClick={() => setInstance(row)}>Details</LyoMenuItem>}
            />
            <JobWorkerInstanceView open={Boolean(instance)} onClose={() => setInstance(null)} instance={instance} />
        </>
    );
}

export function JobWorkflowGrid({
    apiClient,
    jobs,
    routePrefix,
    initialWorkflowId,
}: {
    apiClient: AsyncApiClient;
    jobs: AsyncJobApiClient;
    routePrefix?: string;
    initialWorkflowId?: string | null;
}) {
    const query = useMemo(() => asLyoQueryClient(apiClient), [apiClient]);
    const [workflowId, setWorkflowId] = useState<string | null>(initialWorkflowId ?? null);
    useEffect(() => {
        if (initialWorkflowId) setWorkflowId(initialWorkflowId);
    }, [initialWorkflowId]);
    return (
        <>
            <LyoDataGridProjected<Row>
                apiClient={query}
                gridKey="JobWorkflowGrid"
                route={joinRoutePrefix(routePrefix, JobRoutes.workflows)}
                columns={jobWorkflowColumns()}
                keySelector={(row) => [projectedId(row)]}
                features={LyoDataGridFeatureFlags.All}
                filterPropertyDefinitions={[filterProperty("Name"), filterProperty("Enabled", {type: "Bool"})]}
                quickSearchProperties={["Name", "Description"]}
                rowQuickActions={(row) => (
                    <QuickIcon title="Open" onClick={() => setWorkflowId(projectedId(row))}>
                        <EditIcon fontSize="small" />
                    </QuickIcon>
                )}
                rowMenu={(row) => <LyoMenuItem onClick={() => setWorkflowId(projectedId(row))}>Open</LyoMenuItem>}
            />
            <JobWorkflowView open={Boolean(workflowId)} onClose={() => setWorkflowId(null)} jobs={jobs} workflowId={workflowId} />
        </>
    );
}

function QuickIcon({title, onClick, children}: {title: string; onClick: () => void; children: ReactNode}) {
    return (
        <LyoTooltip title={title}>
            <LyoIconButton
                size="small"
                aria-label={title}
                onClick={(event) => {
                    event.stopPropagation();
                    onClick();
                }}
            >
                {children}
            </LyoIconButton>
        </LyoTooltip>
    );
}
