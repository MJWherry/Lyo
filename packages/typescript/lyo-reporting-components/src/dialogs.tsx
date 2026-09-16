"use client";

import {useCallback, useEffect, useMemo, useState} from "react";
import type {AsyncApiClient} from "lyo-api-client";
import type {ReportDefinitionRes, ReportFormat, ReportGenerationRes} from "lyo-reporting-api-client";
import type {AsyncReportingApiClient} from "lyo-reporting-api-client";
import {REPORT_DEFINITION_DETAIL_INCLUDES, emptyConcreteQuery} from "lyo-reporting-api-client";
import {LyoFormatterValueEditor} from "lyo-formatter";
import {
    LyoButton,
    LyoChip,
    LyoChipHues,
    LyoDialog,
    LyoMenuItem,
    LyoParameterEditor,
    LyoParameterValueGrid,
    LyoStack,
    LyoTextField,
    LyoValueEditorProvider,
    asLyoQueryClient,
    durationMsBetween,
    formatDuration,
    type LyoParameterEditRow,
    type LyoParameterEntry,
    useLyoSnackbar,
} from "lyo-web-components";
import {
    capPreviewRows,
    durationChipColor,
    formatHue,
    reportFormatterContext,
    sheetsFromReportJson,
    statusChipColor,
} from "./reportPreview.js";
import {formatChip, runParameterEntries} from "./gridDisplay.js";
import {triggerBlobDownload} from "./reportColors.js";

const FORMATS: ReportFormat[] = ["html", "pdf", "csv", "xlsx", "json"];

function toRows(params: ReportDefinitionRes["parameters"]): LyoParameterEditRow[] {
    return (params ?? []).map((item, index) => ({
        id: item.id,
        key: item.key,
        type: item.type,
        value: item.value ?? null,
        description: item.description ?? null,
        required: item.required,
        enabled: true,
        isNew: false,
        isEncrypted: Boolean(item.encryptedValue),
        encryptedValue: item.encryptedValue ?? null,
        allowedValues: item.allowedValues ?? null,
        options: item.options ?? null,
        order: index,
        defaultKind: item.defaultKind === "Expression" ? "Expression" : "Literal",
        defaultTemplate: item.defaultTemplate ?? null,
    }));
}

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
    onGenerated?: (generationId: string) => void;
}) {
    const snackbar = useLyoSnackbar();
    const [definitions, setDefinitions] = useState<ReportDefinitionRes[]>([]);
    const [selectedId, setSelectedId] = useState(definitionId ?? "");
    const [format, setFormat] = useState<ReportFormat | "">("");
    const [fileName, setFileName] = useState("");
    const [entries, setEntries] = useState<LyoParameterEntry[]>([]);
    const [busy, setBusy] = useState(false);

    useEffect(() => {
        if (!open) return;
        setSelectedId(definitionId ?? "");
        void reports.definitions
            .queryConcrete({...emptyConcreteQuery(200), Include: ["Parameters"]})
            .then((res) => {
                setDefinitions((res.data?.items ?? []).filter((item) => item.isActive));
            });
    }, [definitionId, open, reports]);

    useEffect(() => {
        const definition = definitions.find((item) => item.id === selectedId);
        setEntries(runParameterEntries(definition));
    }, [definitions, selectedId]);

    const selected = definitions.find((item) => item.id === selectedId) ?? null;
    const editorContext = reportFormatterContext(
        selected ? {Name: selected.name, Id: selected.id} : null,
        entries.map((entry) => ({key: entry.definition.key, value: entry.value}))
    );
    const missing = entries.find((entry) => entry.definition.required && (!entry.value?.trim() || entry.value === "null"));

    const save = useCallback(async () => {
        if (!selectedId) {
            snackbar.show("Pick a definition first.", "warning");
            return;
        }
        if (missing) {
            snackbar.show(`${missing.definition.key} is required.`, "warning");
            return;
        }
        setBusy(true);
        try {
            const definition = definitions.find((item) => item.id === selectedId) ?? null;
            reportFormatterContext(
                definition ? {Name: definition.name, Id: definition.id} : null,
                entries.map((entry) => ({key: entry.definition.key, value: entry.value}))
            );
            const res = await reports.generations.generate({
                reportDefinitionId: selectedId,
                format: format || null,
                fileName: fileName.trim() || null,
                createdBy,
                parameters: entries.map((entry) => ({
                    key: entry.definition.key,
                    type: entry.definition.type,
                    value: entry.value,
                })),
            });
            snackbar.show("Generation started.", "success");
            if (res.data?.id) onGenerated?.(res.data.id);
            onClose();
        } catch (err) {
            snackbar.show(err instanceof Error ? err.message : "Generate failed", "error");
        } finally {
            setBusy(false);
        }
    }, [createdBy, definitions, entries, fileName, format, missing, onClose, onGenerated, reports, selectedId, snackbar]);

    return (
        <LyoValueEditorProvider editors={{Formatter: LyoFormatterValueEditor}}>
        <LyoDialog open={open} onClose={onClose} title="Run Report" onSave={save} busy={busy} saveDisabled={Boolean(missing)} saveText="Generate">
            <LyoStack spacing={2}>
                <LyoTextField select label="Definition" value={selectedId} onChange={(e) => setSelectedId(e.target.value)}>
                    {definitions.map((item) => (
                        <LyoMenuItem key={item.id} value={item.id}>
                            {item.name}
                        </LyoMenuItem>
                    ))}
                </LyoTextField>
                <LyoTextField select label="Format" value={format} onChange={(e) => setFormat(e.target.value as ReportFormat)}>
                    <LyoMenuItem value="">Definition default</LyoMenuItem>
                    {FORMATS.map((item) => (
                        <LyoMenuItem key={item} value={item}>
                            {item}
                        </LyoMenuItem>
                    ))}
                </LyoTextField>
                <LyoTextField label="File name" value={fileName} onChange={(e) => setFileName(e.target.value)} />
                <LyoParameterValueGrid entries={entries} onChange={setEntries} editorContext={editorContext} />
            </LyoStack>
        </LyoDialog>
        </LyoValueEditorProvider>
    );
}

export function ReportDefinitionView({
    open,
    onClose,
    reports,
    definitionId,
    onDesign,
    apiClient,
}: {
    open: boolean;
    onClose: () => void;
    reports: AsyncReportingApiClient;
    definitionId: string | null;
    onDesign?: (definitionId: string) => void;
    apiClient?: AsyncApiClient;
}) {
    const snackbar = useLyoSnackbar();
    const queryClient = useMemo(() => (apiClient ? asLyoQueryClient(apiClient) : undefined), [apiClient]);
    const [definition, setDefinition] = useState<ReportDefinitionRes | null>(null);
    const [rows, setRows] = useState<LyoParameterEditRow[]>([]);
    const [paramDirty, setParamDirty] = useState(false);

    const load = useCallback(async () => {
        if (!definitionId) return;
        const res = await reports.definitions.queryConcrete({
            ...emptyConcreteQuery(1),
            Keys: [[definitionId]],
            Include: [...REPORT_DEFINITION_DETAIL_INCLUDES],
        });
        const item = res.data?.items?.[0] ?? null;
        setDefinition(item);
        setRows(toRows(item?.parameters));
        setParamDirty(false);
    }, [definitionId, reports]);

    useEffect(() => {
        if (open && definitionId) void load().catch((err: unknown) => snackbar.show(err instanceof Error ? err.message : "Load failed", "error"));
    }, [definitionId, load, open, snackbar]);

    const saveParameters = async () => {
        if (!definition) return;
        for (const row of rows) {
            const body = {
                reportDefinitionId: definition.id,
                key: row.key,
                type: row.type,
                value: row.value,
                required: row.required,
                description: row.description,
                options: row.options,
                allowedValues: row.allowedValues,
                defaultKind: row.defaultKind,
                defaultTemplate: row.defaultTemplate,
            };
            if (row.id) await reports.definitionParameters.update(row.id, body);
            else if (row.key.trim()) await reports.definitionParameters.create(body);
        }
        snackbar.show("Parameters saved.", "success");
        await load();
    };

    const editorContext = reportFormatterContext(
        definition ? {Name: definition.name, Id: definition.id} : null,
        rows.map((row) => ({key: row.key, value: row.value}))
    );

    return (
        <LyoValueEditorProvider editors={{Formatter: LyoFormatterValueEditor}}>
        <LyoDialog open={open} onClose={onClose} title={definition?.name ?? "Report Definition"} size="Large">
            <LyoStack spacing={2} sx={{mt: 1}}>
                {definition ? (
                    <LyoStack direction="row" spacing={1} alignItems="center">
                        <LyoChip label={definition.isActive ? "Active" : "Inactive"} color={definition.isActive ? "success" : "default"} />
                        <LyoChip spec={formatChip(definition.defaultFormat)} />
                    </LyoStack>
                ) : null}
                {definition?.description ? <span>{definition.description}</span> : null}
                <LyoParameterEditor
                    items={rows}
                    onChange={(next) => {
                        setRows(next);
                        setParamDirty(true);
                    }}
                    showRequired
                    showEncrypt
                    showOptionsEditor
                    showDefaultKind
                    allowReorder
                    apiClient={queryClient}
                    editorContext={editorContext}
                />
                <LyoStack direction="row" spacing={1}>
                    {paramDirty ? (
                        <LyoButton variant="contained" onClick={() => void saveParameters()}>
                            Save parameters
                        </LyoButton>
                    ) : null}
                    {definition && onDesign ? <LyoButton onClick={() => onDesign(definition.id)}>Design</LyoButton> : null}
                </LyoStack>
            </LyoStack>
        </LyoDialog>
        </LyoValueEditorProvider>
    );
}

export function ReportGenerationView({
    open,
    onClose,
    reports,
    generationId,
    downloadFile,
    viewFileUrl,
}: {
    open: boolean;
    onClose: () => void;
    reports: AsyncReportingApiClient;
    generationId: string | null;
    downloadFile?: (outputFileId: string, fileName?: string | null) => Promise<void>;
    viewFileUrl?: (outputFileId: string) => Promise<string | null>;
}) {
    const snackbar = useLyoSnackbar();
    const [generation, setGeneration] = useState<ReportGenerationRes | null>(null);
    const [previewUrl, setPreviewUrl] = useState<string | null>(null);

    const load = useCallback(async () => {
        if (!generationId) return;
        const res = await reports.generations.get(generationId, ["Parameters"]);
        setGeneration(res.data ?? null);
    }, [generationId, reports]);

    useEffect(() => {
        if (open && generationId) void load().catch((err: unknown) => snackbar.show(err instanceof Error ? err.message : "Load failed", "error"));
    }, [generationId, load, open, snackbar]);

    const sheets =
        generation && (generation.format === "csv" || generation.format === "xlsx")
            ? sheetsFromReportJson(generation.reportDataJson ?? "").map((sheet) => ({
                  ...sheet,
                  rows: capPreviewRows(sheet.rows),
              }))
            : [];
    const hue = formatHue(generation?.format);
    const durationMs = generation ? durationMsBetween(generation.startedTimestamp, generation.finishedTimestamp) : null;
    const statusColor = generation ? statusChipColor(generation.status) : "default";
    const elapsedColor = durationChipColor(durationMs);

    const download = async () => {
        if (!generation) return;
        if (downloadFile && generation.outputFileId) {
            await downloadFile(generation.outputFileId, generation.originalFileName);
            return;
        }
        const file = await reports.generations.download(generation.id);
        triggerBlobDownload(file.blob, file.fileName);
    };

    const view = async () => {
        if (!generation?.outputFileId || !viewFileUrl) return;
        setPreviewUrl(await viewFileUrl(generation.outputFileId));
    };

    return (
        <LyoDialog open={open} onClose={onClose} title="Report Generation">
            {generation ? (
                <LyoStack spacing={1}>
                    {generation.errorMessage ? <span>{generation.errorMessage}</span> : null}
                    <LyoStack direction="row" spacing={1}>
                        <LyoChip label={generation.status} color={statusColor === "inherit" ? "default" : statusColor} />
                        <LyoChip label={generation.format} hue={hue ? LyoChipHues[hue] : undefined} />
                        <LyoChip label={formatDuration(durationMs)} color={elapsedColor === "inherit" ? "default" : elapsedColor} />
                        {generation.contentType ? <LyoChip label={generation.contentType} /> : null}
                    </LyoStack>
                    {(generation.parameters ?? []).map((parameter) => (
                        <span key={parameter.id}>
                            {parameter.key}: {parameter.value ?? ""}
                        </span>
                    ))}
                    {sheets.map((sheet) => (
                        <div key={sheet.title}>
                            <strong>{sheet.title}</strong>
                            <div>{sheet.headers.join(" | ")}</div>
                            {sheet.rows.map((row, index) => (
                                <div key={index}>{row.join(" | ")}</div>
                            ))}
                        </div>
                    ))}
                    {previewUrl ? <a href={previewUrl}>Open output</a> : null}
                    <LyoStack direction="row" spacing={1}>
                        <LyoButton onClick={() => void download()}>Download</LyoButton>
                        {generation.format !== "csv" && generation.format !== "xlsx" ? (
                            <LyoButton onClick={() => void view()}>View</LyoButton>
                        ) : null}
                    </LyoStack>
                </LyoStack>
            ) : null}
        </LyoDialog>
    );
}
