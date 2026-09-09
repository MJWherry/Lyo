"use client";

import {useCallback, useEffect, useState} from "react";
import type {AsyncReportingApiClient} from "lyo-reporting-api-client";
import {REPORT_DEFINITION_DETAIL_INCLUDES, emptyConcreteQuery} from "lyo-reporting-api-client";
import {LyoButton, LyoDialog, LyoStack, useLyoSnackbar} from "lyo-web-components";

export function RunReportDialog({
    open,
    onClose,
    reports,
    definitionId,
    createdBy = "ui",
    onGenerated,
}: {
    open: boolean;
    onClose: () => void;
    reports: AsyncReportingApiClient;
    definitionId?: string | null;
    createdBy?: string;
    onGenerated?: () => void;
}) {
    const snackbar = useLyoSnackbar();
    const [format, setFormat] = useState("pdf");
    const [busy, setBusy] = useState(false);

    const save = useCallback(async () => {
        if (!definitionId) {
            snackbar.show("Pick a definition first.", "warning");
            return;
        }
        setBusy(true);
        try {
            await reports.generations.generate({
                reportDefinitionId: definitionId,
                format: format as "html" | "pdf" | "csv" | "xlsx" | "json",
                createdBy,
            });
            snackbar.show("Generation started.", "success");
            onGenerated?.();
            onClose();
        } catch (err) {
            snackbar.show(err instanceof Error ? err.message : "Generate failed", "error");
        } finally {
            setBusy(false);
        }
    }, [createdBy, definitionId, format, onClose, onGenerated, reports, snackbar]);

    return (
        <LyoDialog open={open} onClose={onClose} title="Run Report" onSave={save} busy={busy} saveText="Generate">
            <LyoStack spacing={2} sx={{pt: 1}}>
                <div>Definition {definitionId ?? "(none selected)"}</div>
                <label>
                    Format{" "}
                    <select value={format} onChange={(e) => setFormat(e.target.value)}>
                        <option value="html">html</option>
                        <option value="pdf">pdf</option>
                        <option value="csv">csv</option>
                        <option value="xlsx">xlsx</option>
                        <option value="json">json</option>
                    </select>
                </label>
            </LyoStack>
        </LyoDialog>
    );
}

export function ReportDefinitionView({
    open,
    onClose,
    reports,
    definitionId,
}: {
    open: boolean;
    onClose: () => void;
    reports: AsyncReportingApiClient;
    definitionId: string | null;
}) {
    const snackbar = useLyoSnackbar();
    const [json, setJson] = useState("");

    const load = useCallback(async () => {
        if (!definitionId) return;
        try {
            const res = await reports.definitions.queryConcrete({
                ...emptyConcreteQuery(1),
                Keys: [[definitionId]],
                Include: [...REPORT_DEFINITION_DETAIL_INCLUDES],
            });
            setJson(JSON.stringify(res.data?.items?.[0] ?? null, null, 2));
        } catch (err) {
            snackbar.show(err instanceof Error ? err.message : "Load failed", "error");
        }
    }, [definitionId, reports, snackbar]);

    useEffect(() => {
        if (open && definitionId) void load();
    }, [open, definitionId, load]);

    return (
        <LyoDialog
            open={open}
            onClose={onClose}
            title="Report Definition"
            size="Large"
            extraActions={
                <LyoButton size="small" onClick={load}>
                    Reload
                </LyoButton>
            }
        >
            <pre style={{whiteSpace: "pre-wrap", fontSize: 12, maxHeight: "50vh", overflow: "auto"}}>
                {json || "Reload to fetch parameters and composition JSON."}
            </pre>
        </LyoDialog>
    );
}

export function ReportGenerationView({
    open,
    onClose,
    reports,
    generationId,
}: {
    open: boolean;
    onClose: () => void;
    reports: AsyncReportingApiClient;
    generationId: string | null;
}) {
    const snackbar = useLyoSnackbar();
    const [json, setJson] = useState("");

    const load = useCallback(async () => {
        if (!generationId) return;
        try {
            const res = await reports.generations.get(generationId, ["Parameters"]);
            setJson(JSON.stringify(res.data ?? null, null, 2));
        } catch (err) {
            snackbar.show(err instanceof Error ? err.message : "Load failed", "error");
        }
    }, [generationId, reports, snackbar]);

    useEffect(() => {
        if (open && generationId) void load();
    }, [open, generationId, load]);

    return (
        <LyoDialog
            open={open}
            onClose={onClose}
            title="Report Generation"
            extraActions={
                <LyoButton size="small" onClick={load}>
                    Reload
                </LyoButton>
            }
        >
            <pre style={{whiteSpace: "pre-wrap", fontSize: 12, maxHeight: "50vh", overflow: "auto"}}>
                {json || "Reload to fetch generation details."}
            </pre>
        </LyoDialog>
    );
}
