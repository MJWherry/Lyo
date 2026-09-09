"use client";

import {useCallback, useEffect, useState} from "react";
import type {AsyncJobApiClient} from "lyo-job-api-client";
import {JOB_DEFINITION_EDITOR_INCLUDES, emptyConcreteQuery} from "lyo-job-api-client";
import {LyoButton, LyoChip, LyoDialog, LyoStack, useLyoSnackbar} from "lyo-web-components";
import {projectedField, projectedId} from "./jobColors.js";

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

    const save = useCallback(async () => {
        if (!definitionId) {
            snackbar.show("Pick a definition first.", "warning");
            return;
        }
        setBusy(true);
        try {
            await jobs.runs.create({jobDefinitionId: definitionId, createdBy, allowTriggers: true});
            snackbar.show("Run created.", "success");
            onCreated?.();
            onClose();
        } catch (err) {
            snackbar.show(err instanceof Error ? err.message : "Create run failed", "error");
        } finally {
            setBusy(false);
        }
    }, [createdBy, definitionId, jobs, onClose, onCreated, snackbar]);

    return (
        <LyoDialog open={open} onClose={onClose} title="Create Job Run" onSave={save} busy={busy} saveText="Create">
            <p>
                Definition {definitionId ?? "(none selected)"}. The run is queued with allowTriggers.
            </p>
        </LyoDialog>
    );
}

export function JobDefinitionView({
    open,
    onClose,
    jobs,
    definitionId,
}: {
    open: boolean;
    onClose: () => void;
    jobs: AsyncJobApiClient;
    definitionId: string | null;
}) {
    const snackbar = useLyoSnackbar();
    const [json, setJson] = useState<string>("");

    const load = useCallback(async () => {
        if (!definitionId) return;
        try {
            const res = await jobs.definitions.queryConcrete({
                ...emptyConcreteQuery(1),
                Keys: [[definitionId]],
                Include: [...JOB_DEFINITION_EDITOR_INCLUDES],
            });
            setJson(JSON.stringify(res.data?.items?.[0] ?? null, null, 2));
        } catch (err) {
            snackbar.show(err instanceof Error ? err.message : "Load failed", "error");
        }
    }, [definitionId, jobs, snackbar]);

    useEffect(() => {
        if (open && definitionId) void load();
    }, [open, definitionId, load]);

    return (
        <LyoDialog
            open={open}
            onClose={onClose}
            title="Job Definition"
            size="Large"
            extraActions={
                <LyoButton size="small" onClick={load}>
                    Reload
                </LyoButton>
            }
        >
            <LyoStack spacing={1}>
                <span>Id {definitionId}</span>
                <pre style={{whiteSpace: "pre-wrap", fontSize: 12, maxHeight: "50vh", overflow: "auto"}}>
                    {json || "Open and reload to fetch parameters, schedules, and triggers."}
                </pre>
            </LyoStack>
        </LyoDialog>
    );
}

export function JobRunDetailView({
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
    const snackbar = useLyoSnackbar();
    const [json, setJson] = useState<string>("");

    const load = useCallback(async () => {
        if (!runId) return;
        try {
            const res = await jobs.runs.get(runId, [
                "JobDefinition",
                "JobRunLogs",
                "JobRunResults",
                "JobRunParameters",
                "JobSchedule",
            ]);
            setJson(JSON.stringify(res.data ?? null, null, 2));
        } catch (err) {
            snackbar.show(err instanceof Error ? err.message : "Load failed", "error");
        }
    }, [jobs, runId, snackbar]);

    useEffect(() => {
        if (open && runId) void load();
    }, [open, runId, load]);

    return (
        <LyoDialog
            open={open}
            onClose={onClose}
            title="Job Run Details"
            extraActions={
                <LyoButton size="small" onClick={load}>
                    Reload
                </LyoButton>
            }
        >
            <pre style={{whiteSpace: "pre-wrap", fontSize: 12, maxHeight: "50vh", overflow: "auto"}}>
                {json || "Reload to fetch logs, results, and parameters."}
            </pre>
        </LyoDialog>
    );
}

export function StatusChip({label, color}: {label: string; color: "success" | "error" | "warning" | "info" | "secondary" | "inherit"}) {
    const mapped = color === "inherit" ? "default" : color;
    return <LyoChip size="small" label={label} color={mapped === "secondary" ? "default" : mapped} />;
}

export {projectedField, projectedId};
