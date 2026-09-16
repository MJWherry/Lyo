"use client";

import {useCallback, useEffect, useMemo, useState} from "react";
import type {AsyncApiClient} from "lyo-api-client";
import type {JobDefinitionLatestRunsRes, JobDefinitionRes, JobRunRes} from "lyo-job-api-client";
import type {AsyncJobApiClient} from "lyo-job-api-client";
import {JOB_DEFINITION_EDITOR_INCLUDES, JOB_RUN_DETAIL_INCLUDES, emptyConcreteQuery} from "lyo-job-api-client";
import {LyoFormatterValueEditor} from "lyo-formatter";
import {
    LyoButton,
    LyoChip,
    LyoDialog,
    LyoMenuItem,
    LyoParameterEditor,
    LyoParameterValueGrid,
    LyoStack,
    LyoTab,
    LyoTabs,
    LyoTextField,
    LyoValueEditorProvider,
    asLyoQueryClient,
    type LyoParameterEntry,
    useLyoSnackbar,
} from "lyo-web-components";
import {enabledChip, jobFormatterContext, runParameterEntries} from "./gridDisplay.js";
import {parameterRows, runCreateError, runCreateRequest, type RunParameterInput} from "./jobEditors.js";
import {JobSchedulePanel} from "./scheduleEditor.js";
import {jobResultColor, jobStateColor, projectedField, projectedId} from "./jobColors.js";
import {JobActivity} from "./activity.js";

export function RunJobDialog({
    open,
    onClose,
    jobs,
    createdBy = "ui",
    definitionId,
    onCreated,
}: {
    open: boolean;
    onClose: () => void;
    jobs: AsyncJobApiClient;
    createdBy?: string;
    definitionId?: string | null;
    onCreated?: () => void;
}) {
    const snackbar = useLyoSnackbar();
    const [busy, setBusy] = useState(false);
    const [definitions, setDefinitions] = useState<JobDefinitionRes[]>([]);
    const [selectedId, setSelectedId] = useState(definitionId ?? "");
    const [entries, setEntries] = useState<LyoParameterEntry[]>([]);

    useEffect(() => {
        if (!open) return;
        setSelectedId(definitionId ?? "");
        void jobs.definitions
            .queryConcrete({...emptyConcreteQuery(200), Include: ["JobParameters"]})
            .then((res) => {
                setDefinitions(res.data?.items ?? []);
            });
    }, [definitionId, jobs, open]);

    useEffect(() => {
        const def = definitions.find((item) => item.id === selectedId);
        setEntries(runParameterEntries(def));
    }, [definitions, selectedId]);

    const parameters: RunParameterInput[] = entries.map((entry) => ({
        key: entry.definition.key,
        value: entry.value,
        required: Boolean(entry.definition.required),
        type: entry.definition.type,
    }));
    const error = runCreateError(selectedId, parameters);
    const selected = definitions.find((item) => item.id === selectedId) ?? null;
    const editorContext = jobFormatterContext(selected, parameters);

    const save = useCallback(async () => {
        const message = runCreateError(selectedId, parameters);
        if (message || !selectedId) {
            snackbar.show(message ?? "Pick a definition first.", "warning");
            return;
        }
        setBusy(true);
        try {
            await jobs.runs.create(runCreateRequest(selectedId, createdBy, parameters));
            snackbar.show("Run created.", "success");
            onCreated?.();
            onClose();
        } catch (err) {
            snackbar.show(err instanceof Error ? err.message : "Create run failed", "error");
        } finally {
            setBusy(false);
        }
    }, [createdBy, jobs, onClose, onCreated, parameters, selectedId, snackbar]);

    return (
        <LyoValueEditorProvider editors={{Formatter: LyoFormatterValueEditor}}>
            <LyoDialog open={open} onClose={onClose} title="Create Job Run" onSave={save} busy={busy} saveDisabled={Boolean(error)} saveText="Create">
                <LyoStack spacing={2}>
                    <LyoTextField select label="Definition" value={selectedId} onChange={(e) => setSelectedId(e.target.value)}>
                        {definitions.map((item) => (
                            <LyoMenuItem key={item.id} value={item.id}>
                                {item.name}
                            </LyoMenuItem>
                        ))}
                    </LyoTextField>
                    <LyoParameterValueGrid entries={entries} onChange={setEntries} editorContext={editorContext} />
                    {error ? <span>{error}</span> : null}
                </LyoStack>
            </LyoDialog>
        </LyoValueEditorProvider>
    );
}

export function JobDefinitionView({
    open,
    onClose,
    jobs,
    definitionId,
    apiClient,
}: {
    open: boolean;
    onClose: () => void;
    jobs: AsyncJobApiClient;
    definitionId: string | null;
    apiClient?: AsyncApiClient;
}) {
    const snackbar = useLyoSnackbar();
    const queryClient = useMemo(() => (apiClient ? asLyoQueryClient(apiClient) : undefined), [apiClient]);
    const [tab, setTab] = useState(0);
    const [definition, setDefinition] = useState<JobDefinitionRes | null>(null);
    const [latestRuns, setLatestRuns] = useState<JobDefinitionLatestRunsRes | null>(null);
    const [rows, setRows] = useState(parameterRows([]));
    const [paramDirty, setParamDirty] = useState(false);

    const load = useCallback(async () => {
        if (!definitionId) return;
        const res = await jobs.definitions.queryConcrete({
            ...emptyConcreteQuery(1),
            Keys: [[definitionId]],
            Include: [...JOB_DEFINITION_EDITOR_INCLUDES],
        });
        const item = res.data?.items?.[0] ?? null;
        setDefinition(item);
        setRows(parameterRows(item?.jobParameters ?? []));
        setParamDirty(false);
        const runs = await jobs.definitions.latestRuns([definitionId]);
        setLatestRuns(runs.data?.find((entry) => entry.jobDefinitionId === definitionId) ?? runs.data?.[0] ?? null);
    }, [definitionId, jobs]);

    useEffect(() => {
        if (open && definitionId) void load().catch((err: unknown) => snackbar.show(err instanceof Error ? err.message : "Load failed", "error"));
    }, [definitionId, load, open, snackbar]);

    const saveBasic = async () => {
        if (!definition) return;
        await jobs.definitions.patchById(definition.id, {
            properties: {
                Name: definition.name,
                WorkerType: definition.workerType,
                MaxRetryCount: definition.maxRetryCount,
                TimeoutMinutes: definition.timeoutMinutes,
                MaxConcurrentRuns: definition.maxConcurrentRuns,
                Enabled: definition.enabled,
            },
        });
        snackbar.show("Definition saved.", "success");
    };

    const saveParameters = async () => {
        if (!definition) return;
        for (const row of rows) {
            const body = {
                jobDefinitionId: definition.id,
                key: row.key,
                type: row.type,
                value: row.value,
                required: row.required,
                description: row.description,
                enabled: row.enabled,
                options: row.options,
                allowedValues: row.allowedValues,
                order: row.order,
                defaultKind: row.defaultKind,
                defaultTemplate: row.defaultTemplate,
            };
            if (row.id) await jobs.definitionParameters.update(row.id, body);
            else if (row.key.trim()) await jobs.definitionParameters.create(body);
        }
        snackbar.show("Parameters saved.", "success");
        await load();
    };

    const editorContext = jobFormatterContext(
        definition,
        rows.map((row) => ({key: row.key, value: row.value})),
        latestRuns
    );

    return (
        <LyoValueEditorProvider editors={{Formatter: LyoFormatterValueEditor}}>
        <LyoDialog open={open} onClose={onClose} title="Job Definition" size="Large">
            <LyoTabs value={tab} onChange={(_, value) => setTab(Number(value))}>
                <LyoTab label="Basic" />
                <LyoTab label="Parameters" />
                <LyoTab label="Schedules" />
                <LyoTab label="Activity" />
            </LyoTabs>
            {tab === 0 && definition ? (
                <LyoStack spacing={1} sx={{mt: 2}}>
                    <LyoChip
                        spec={enabledChip(definition.enabled)}
                        onClick={() => setDefinition({...definition, enabled: !definition.enabled})}
                        sx={{cursor: "pointer", alignSelf: "flex-start"}}
                    />
                    <LyoTextField label="Name" value={definition.name} onChange={(e) => setDefinition({...definition, name: e.target.value})} />
                    <LyoTextField label="Worker type" value={definition.workerType} onChange={(e) => setDefinition({...definition, workerType: e.target.value})} />
                    <LyoTextField label="Retries" type="number" value={definition.maxRetryCount ?? 0} onChange={(e) => setDefinition({...definition, maxRetryCount: Number(e.target.value)})} />
                    <LyoTextField label="Timeout minutes" type="number" value={definition.timeoutMinutes ?? 0} onChange={(e) => setDefinition({...definition, timeoutMinutes: Number(e.target.value)})} />
                    <LyoTextField label="Max concurrent" type="number" value={definition.maxConcurrentRuns ?? 0} onChange={(e) => setDefinition({...definition, maxConcurrentRuns: Number(e.target.value)})} />
                    <LyoButton variant="contained" onClick={() => void saveBasic()}>Save</LyoButton>
                </LyoStack>
            ) : null}
            {tab === 1 ? (
                <LyoStack spacing={1} sx={{mt: 2}}>
                    <LyoParameterEditor
                        items={rows}
                        onChange={(next) => {
                            setRows(next);
                            setParamDirty(true);
                        }}
                        showRequired
                        showEnabled
                        showEncrypt
                        showOptionsEditor
                        showDefaultKind
                        allowReorder
                        apiClient={queryClient}
                        editorContext={editorContext}
                    />
                    {paramDirty ? (
                        <LyoButton variant="contained" onClick={() => void saveParameters()}>
                            Save parameters
                        </LyoButton>
                    ) : null}
                </LyoStack>
            ) : null}
            {tab === 2 && definition ? <JobSchedulePanel jobs={jobs} definition={definition} /> : null}
            {tab === 3 && definitionId ? <JobActivity jobs={jobs} definitionId={definitionId} /> : null}
        </LyoDialog>
        </LyoValueEditorProvider>
    );
}

export function JobRunDetailView({
    open,
    onClose,
    jobs,
    runId,
    onChanged,
}: {
    open: boolean;
    onClose: () => void;
    jobs: AsyncJobApiClient;
    runId: string | null;
    onChanged?: () => void;
}) {
    const snackbar = useLyoSnackbar();
    const [tab, setTab] = useState(0);
    const [run, setRun] = useState<JobRunRes | null>(null);

    const load = useCallback(async () => {
        if (!runId) return;
        const res = await jobs.runs.get(runId, [...JOB_RUN_DETAIL_INCLUDES]);
        setRun(res.data ?? null);
    }, [jobs, runId]);

    useEffect(() => {
        if (open && runId) void load().catch((err: unknown) => snackbar.show(err instanceof Error ? err.message : "Load failed", "error"));
    }, [load, open, runId, snackbar]);

    const act = async (kind: "cancel" | "rerun") => {
        if (!runId) return;
        if (kind === "cancel") await jobs.runs.cancel(runId);
        else await jobs.runs.rerun(runId);
        snackbar.show(kind === "cancel" ? "Cancelled." : "Rerun queued.", "success");
        onChanged?.();
        await load();
    };

    const logs = (run?.jobRunLogs ?? []) as Array<{message?: string; level?: string}>;
    const results = (run?.jobRunResults ?? []) as Array<{key?: string; value?: string | null}>;
    const parameters = (run?.jobRunParameters ?? []) as Array<{key?: string; value?: string | null}>;

    return (
        <LyoDialog
            open={open}
            onClose={onClose}
            title="Job Run Details"
            titleChips={
                run ? (
                    <>
                        <StatusChip label={String(run.state ?? "")} color={jobStateColor(String(run.state))} />
                        <StatusChip label={String(run.result ?? "")} color={jobResultColor(String(run.result))} />
                    </>
                ) : null
            }
            extraActions={
                <>
                    <LyoButton size="small" onClick={() => void act("rerun")}>Rerun</LyoButton>
                    <LyoButton size="small" onClick={() => void act("cancel")}>Cancel</LyoButton>
                </>
            }
        >
            <LyoTabs value={tab} onChange={(_, value) => setTab(Number(value))}>
                <LyoTab label="Overview" />
                <LyoTab label="Parameters" />
                <LyoTab label="Results" />
                <LyoTab label="Logs" />
            </LyoTabs>
            {tab === 0 && run ? (
                <LyoStack spacing={1} sx={{mt: 2}}>
                    <span>Progress {run.progressPercent ?? 0}%</span>
                    <span>{run.progressMessage ?? ""}</span>
                </LyoStack>
            ) : null}
            {tab === 1 ? <KeyTable rows={parameters} /> : null}
            {tab === 2 ? <KeyTable rows={results} /> : null}
            {tab === 3 ? (
                <LyoStack spacing={0.5} sx={{mt: 2}}>
                    {logs.map((log, index) => (
                        <span key={index}>{log.level}: {log.message}</span>
                    ))}
                </LyoStack>
            ) : null}
        </LyoDialog>
    );
}

function KeyTable({rows}: {rows: Array<{key?: string; value?: string | null}>}) {
    return (
        <LyoStack spacing={0.5} sx={{mt: 2}}>
            {rows.map((row, index) => (
                <span key={index}>{row.key}: {row.value ?? ""}</span>
            ))}
        </LyoStack>
    );
}

export function StatusChip({label, color}: {label: string; color: "success" | "error" | "warning" | "info" | "secondary" | "inherit"}) {
    const mapped = color === "inherit" || color === "secondary" ? "default" : color;
    return <LyoChip size="small" label={label} color={mapped} />;
}

export function useDefinitionName(row: Record<string, unknown>): string {
    return String(projectedField(row, "Name") ?? projectedId(row));
}

export {projectedField, projectedId};
