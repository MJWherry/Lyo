"use client";

import {
    Fragment,
    createContext,
    useContext,
    useEffect,
    useMemo,
    useRef,
    useState,
    type CSSProperties,
    type DragEvent,
    type KeyboardEvent,
    type ReactNode,
    type RefObject,
} from "react";
import type {AsyncApiClient} from "lyo-api-client";
import type {AsyncReportingApiClient, ReportDefinitionRes, ReportFormat} from "lyo-reporting-api-client";
import {createAsyncReportingApiClient, emptyConcreteQuery} from "lyo-reporting-api-client";
import {LyoFormatterValueEditor} from "lyo-formatter";
import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import CreditCardIcon from "@mui/icons-material/CreditCard";
import DashboardCustomizeIcon from "@mui/icons-material/DashboardCustomize";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import DescriptionIcon from "@mui/icons-material/Description";
import DragIndicatorIcon from "@mui/icons-material/DragIndicator";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import GridViewIcon from "@mui/icons-material/GridView";
import NotesIcon from "@mui/icons-material/Notes";
import PlayArrowIcon from "@mui/icons-material/PlayArrow";
import RedoIcon from "@mui/icons-material/Redo";
import SaveIcon from "@mui/icons-material/Save";
import SaveAsIcon from "@mui/icons-material/SaveAs";
import TableChartIcon from "@mui/icons-material/TableChart";
import UndoIcon from "@mui/icons-material/Undo";
import ViewAgendaIcon from "@mui/icons-material/ViewAgenda";
import WidgetsIcon from "@mui/icons-material/Widgets";
import {
    LyoButton,
    LyoChip,
    LyoDialog,
    LyoIconButton,
    LyoMenuItem,
    LyoPaper,
    LyoParameterEditor,
    LyoParameterValueGrid,
    LyoStack,
    LyoSwitch,
    LyoTab,
    LyoTabs,
    LyoTextField,
    LyoToolbar,
    LyoToolbarButton,
    LyoToolbarField,
    LyoToolbarIconButton,
    LyoToolbarMenu,
    LyoValueEditorProvider,
    asLyoQueryClient,
    entriesFrom,
    type LyoParameterEditRow,
    type LyoParameterEntry,
    useLyoSnackbar,
} from "lyo-web-components";
import {formatChip} from "./gridDisplay.js";
import {reportDesignCss} from "./designStyles.js";
import {
    COMPOSITION_TEMPLATES,
    addChild,
    blankComposition,
    compositionJson,
    deleteNode,
    duplicateNode,
    findNode,
    historyOf,
    isDimmed,
    moveNode,
    paintComposition,
    parseComposition,
    pushHistory,
    redoHistory,
    reportFormatterContext,
    siblingIndex,
    undoHistory,
    updateNode,
    updatePayload,
    type CompositionNode,
    type DesignKind,
    type History,
    type PaintNode,
    type PaintReport,
} from "./reportPreview.js";

const FORMATS: ReportFormat[] = ["html", "pdf", "csv", "xlsx", "json"];

const KIND_LABEL: Record<string, string> = {
    report: "Report",
    section: "Section",
    card: "Card",
    table: "Table",
    block: "Text",
    grid: "Grid",
};

type PreviewParameter = {key: string; value: string | null};

type CanvasActions = {
    selectedId: string;
    draggingId: string | null;
    overSlot: string | null;
    setDraggingId: (id: string | null) => void;
    setOverSlot: (slot: string | null) => void;
    onSelect: (id: string) => void;
    onDrop: (id: string, parentId: string, index: number) => void;
    onDelete?: (id: string) => void;
};

const CanvasContext = createContext<CanvasActions | null>(null);

function useCanvas(): CanvasActions {
    const actions = useContext(CanvasContext);
    if (!actions) throw new Error("Report design canvas is missing its provider.");
    return actions;
}

export function ReportDesignWorkbench({
    apiClient,
    reports: reportsProp,
    routePrefix,
    definitionId,
}: {
    apiClient: AsyncApiClient;
    reports?: AsyncReportingApiClient;
    routePrefix?: string;
    definitionId?: string | null;
}) {
    const reports = useMemo(
        () => reportsProp ?? createAsyncReportingApiClient(apiClient, {routePrefix}),
        [apiClient, reportsProp, routePrefix]
    );
    const queryClient = useMemo(() => asLyoQueryClient(apiClient), [apiClient]);
    const snackbar = useLyoSnackbar();
    const inspectorRef = useRef<HTMLDivElement>(null);
    const [definitions, setDefinitions] = useState<ReportDefinitionRes[]>([]);
    const [name, setName] = useState("Untitled report");
    const [currentId, setCurrentId] = useState<string | null>(definitionId ?? null);
    const [history, setHistory] = useState<History<CompositionNode>>(() => historyOf(blankComposition()));
    const [selectedId, setSelectedId] = useState("root");
    const [dirty, setDirty] = useState(false);
    const [pendingId, setPendingId] = useState<string | null>(null);
    const [format, setFormat] = useState<ReportFormat>("html");
    const [tab, setTab] = useState(0);
    const [rows, setRows] = useState<LyoParameterEditRow[]>([]);
    const [examples, setExamples] = useState<LyoParameterEntry[]>([]);

    const loadList = async () => {
        const res = await reports.definitions.queryConcrete({...emptyConcreteQuery(100), Include: ["Parameters"]});
        setDefinitions(res.data?.items ?? []);
    };

    useEffect(() => {
        void loadList();
        // The list is refreshed again after save.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [reports]);

    useEffect(() => {
        setExamples((prev) =>
            entriesFrom(rows).map((entry) => {
                const existing = prev.find((item) => item.definition.key === entry.definition.key);
                return existing ? {...entry, value: existing.value} : entry;
            })
        );
    }, [rows]);

    useEffect(() => {
        if (!definitionId) return;
        let cancelled = false;
        void reports.definitions.get(definitionId).then((res) => {
            if (cancelled || !res.data) return;
            applyDefinition(res.data);
        });
        return () => {
            cancelled = true;
        };
        // applyDefinition closes over setters only.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [definitionId, reports]);

    const applyDefinition = (definition: ReportDefinitionRes) => {
        const next = parseComposition(definition.reportDataJson);
        setCurrentId(definition.id);
        setName(definition.name);
        setHistory(historyOf(next));
        setSelectedId(next.id);
        setDirty(false);
        const schema = (definition.parameters ?? []).map((item, index) => ({
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
            defaultKind: item.defaultKind === "Expression" ? ("Expression" as const) : ("Literal" as const),
            defaultTemplate: item.defaultTemplate ?? null,
        }));
        setRows(schema);
        setExamples(entriesFrom(schema));
    };

    const edit = (next: CompositionNode) => {
        setHistory(pushHistory(history, next));
        setDirty(true);
    };

    const applyTemplate = (templateName: string) => {
        const template = COMPOSITION_TEMPLATES[templateName];
        if (!template) return;
        const next = parseComposition(compositionJson(template));
        edit(next);
        setSelectedId(next.id);
        setName(template.title || templateName);
    };

    const save = async (asNew: boolean) => {
        const reportDataJson = compositionJson(history.present);
        const definitionName = name || history.present.title || "Report";
        let id = asNew ? null : currentId;
        if (!id) {
            const created = await reports.definitions.create({
                name: definitionName,
                reportDataJson,
                isActive: true,
                defaultFormat: format,
            });
            id = created.data?.data?.id ?? null;
            setCurrentId(id);
        } else {
            await reports.definitions.update(id, {
                name: definitionName,
                reportDataJson,
                isActive: true,
                defaultFormat: format,
            });
        }
        if (id) {
            for (const row of rows) {
                if (!row.key.trim()) continue;
                const body = {
                    reportDefinitionId: id,
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
                if (row.id && !asNew) await reports.definitionParameters.update(row.id, body);
                else await reports.definitionParameters.create(body);
            }
        }
        setDirty(false);
        snackbar.show(asNew ? "Saved as new." : "Saved.", "success");
        await loadList();
    };

    const generate = async () => {
        if (!currentId) {
            snackbar.show("Save the definition before generating.", "warning");
            return;
        }
        await reports.generations.generate({
            reportDefinitionId: currentId,
            format,
            parameters: examples.map((entry) => ({
                key: entry.definition.key,
                type: entry.definition.type,
                value: entry.value,
            })),
        });
        snackbar.show("Generation started.", "success");
    };

    const switchTo = (id: string, force = false) => {
        if (dirty && !force) {
            setPendingId(id);
            return;
        }
        const definition = definitions.find((item) => item.id === id);
        if (definition) applyDefinition(definition);
    };

    const selected = findNode(history.present, selectedId) ?? history.present;
    const place = siblingIndex(history.present, selected.id);
    const parent = place ? findNode(history.present, place.parentId) : null;
    const atStart = !place || place.index === 0;
    const atEnd = !place || !parent || place.index >= parent.children.length - 1;
    const previewParameters = useMemo<PreviewParameter[]>(
        () =>
            rows.map((row) => ({
                key: row.key,
                value: examples.find((entry) => entry.definition.key === row.key)?.value ?? row.value,
            })),
        [rows, examples]
    );
    const editorContext = reportFormatterContext({Name: name, Id: currentId ?? ""}, previewParameters);

    useEffect(() => {
        if (inspectorRef.current) inspectorRef.current.scrollTop = 0;
    }, [selected.id]);

    const add = (kind: DesignKind) => {
        const parentId =
            kind === "section" ? history.present.id : selected.kind === "section" ? selected.id : history.present.children[0]?.id ?? history.present.id;
        const next = addChild(history.present, parentId, kind);
        const created = newestId(history.present, next);
        edit(next);
        if (created) setSelectedId(created);
    };

    const onDesignKey = (event: KeyboardEvent<HTMLDivElement>) => {
        const target = event.target;
        if (target instanceof HTMLElement && target.closest("input, textarea, select, [contenteditable='true']")) return;
        if (!(event.metaKey || event.ctrlKey)) return;
        const key = event.key.toLowerCase();
        if (key === "z") {
            event.preventDefault();
            setHistory(event.shiftKey ? redoHistory(history) : undoHistory(history));
        } else if (key === "y") {
            event.preventDefault();
            setHistory(redoHistory(history));
        }
    };

    const onCanvasDrop = (id: string, parentId: string, index = 0) => {
        const dragged = findNode(history.present, id);
        if (!dragged || dragged.id === history.present.id) return;
        if (id === parentId || findNode(dragged, parentId)) return;
        const from = siblingIndex(history.present, id);
        const slot = from && from.parentId === parentId && from.index < index ? index - 1 : index;
        if (from && from.parentId === parentId && from.index === slot) return;
        edit(moveNode(history.present, id, parentId, slot));
    };

    const onCanvasDelete = (id: string) => {
        if (id === history.present.id) return;
        const from = siblingIndex(history.present, id);
        edit(deleteNode(history.present, id));
        setSelectedId(from?.parentId ?? history.present.id);
    };

    return (
        <LyoValueEditorProvider editors={{Formatter: LyoFormatterValueEditor}}>
            <div className="lyo-report-design" onKeyDown={onDesignKey}>
                <style>{reportDesignCss}</style>
                <LyoPaper className="lyo-report-design-bar" variant="outlined">
                    <LyoToolbar dense>
                        <LyoToolbarField width={200}>
                            <LyoTextField
                                size="small"
                                fullWidth
                                margin="none"
                                label="Definition name"
                                value={name}
                                onChange={(event) => {
                                    setName(event.target.value);
                                    setDirty(true);
                                }}
                            />
                        </LyoToolbarField>
                        <LyoToolbarField width={220}>
                            <LyoTextField
                                select
                                size="small"
                                fullWidth
                                margin="none"
                                label="Load definition"
                                value={currentId ?? ""}
                                onChange={(event) => switchTo(event.target.value)}
                            >
                                <LyoMenuItem value="">New</LyoMenuItem>
                                {definitions.map((item) => (
                                    <LyoMenuItem key={item.id} value={item.id}>
                                        {item.name}
                                    </LyoMenuItem>
                                ))}
                            </LyoTextField>
                        </LyoToolbarField>
                        <LyoToolbarMenu label="Templates" icon={<DashboardCustomizeIcon fontSize="small" />}>
                            {(close) =>
                                Object.keys(COMPOSITION_TEMPLATES).map((templateName) => (
                                    <LyoMenuItem
                                        key={templateName}
                                        onClick={() => {
                                            applyTemplate(templateName);
                                            close();
                                        }}
                                    >
                                        {templateName}
                                    </LyoMenuItem>
                                ))
                            }
                        </LyoToolbarMenu>
                        <LyoToolbarButton variant="contained" color="primary" icon={<SaveIcon fontSize="small" />} onClick={() => void save(false)}>
                            Save
                        </LyoToolbarButton>
                        <LyoToolbarButton icon={<SaveAsIcon fontSize="small" />} onClick={() => void save(true)}>
                            Save as
                        </LyoToolbarButton>
                        <LyoToolbarField width={128}>
                            <LyoTextField
                                select
                                size="small"
                                fullWidth
                                margin="none"
                                label="Format"
                                value={format}
                                onChange={(event) => setFormat(event.target.value as ReportFormat)}
                            >
                                {FORMATS.map((item) => (
                                    <LyoMenuItem key={item} value={item}>
                                        <LyoChip spec={formatChip(item)} />
                                    </LyoMenuItem>
                                ))}
                            </LyoTextField>
                        </LyoToolbarField>
                        <LyoToolbarButton variant="contained" color="primary" icon={<PlayArrowIcon fontSize="small" />} onClick={() => void generate()}>
                            Generate
                        </LyoToolbarButton>
                        {dirty ? <span className="lyo-design-unsaved">Unsaved</span> : null}
                    </LyoToolbar>
                </LyoPaper>
                <div className="lyo-report-design-body">
                    <LyoPaper className="lyo-report-design-rail" variant="outlined">
                        <LyoTabs className="lyo-design-rail-tabs" value={tab} onChange={(_, value) => setTab(Number(value))}>
                            <LyoTab label="Design" />
                            <LyoTab label="Parameters" />
                        </LyoTabs>
                        {tab === 0 ? (
                            <div className="lyo-design-editor">
                                <div className="lyo-design-tools">
                                    <LyoToolbarButton onClick={() => add("section")}>Section</LyoToolbarButton>
                                    <LyoToolbarButton onClick={() => add("card")}>Card</LyoToolbarButton>
                                    <LyoToolbarButton onClick={() => add("table")}>Table</LyoToolbarButton>
                                    <LyoToolbarButton onClick={() => add("block")}>Text</LyoToolbarButton>
                                    <span className="lyo-design-tools-sep" />
                                    <LyoToolbarIconButton
                                        tooltip="Move up"
                                        icon={<ArrowUpwardIcon fontSize="small" />}
                                        disabled={atStart}
                                        onClick={() => place && edit(moveNode(history.present, selected.id, place.parentId, place.index - 1))}
                                    />
                                    <LyoToolbarIconButton
                                        tooltip="Move down"
                                        icon={<ArrowDownwardIcon fontSize="small" />}
                                        disabled={atEnd}
                                        onClick={() => place && edit(moveNode(history.present, selected.id, place.parentId, place.index + 1))}
                                    />
                                    <LyoToolbarIconButton
                                        tooltip="Duplicate"
                                        icon={<ContentCopyIcon fontSize="small" />}
                                        disabled={selected.id === history.present.id}
                                        onClick={() => {
                                            const next = duplicateNode(history.present, selected.id);
                                            const created = newestId(history.present, next);
                                            edit(next);
                                            if (created) setSelectedId(created);
                                        }}
                                    />
                                    <LyoToolbarIconButton
                                        tooltip="Delete"
                                        icon={<DeleteOutlineIcon fontSize="small" />}
                                        color="error"
                                        disabled={selected.id === history.present.id}
                                        onClick={() => {
                                            edit(deleteNode(history.present, selected.id));
                                            setSelectedId(place?.parentId ?? history.present.id);
                                        }}
                                    />
                                    <span className="lyo-design-tools-sep" />
                                    <LyoToolbarIconButton
                                        tooltip="Undo (Ctrl+Z)"
                                        icon={<UndoIcon fontSize="small" />}
                                        disabled={history.past.length === 0}
                                        onClick={() => setHistory(undoHistory(history))}
                                    />
                                    <LyoToolbarIconButton
                                        tooltip="Redo (Ctrl+Y)"
                                        icon={<RedoIcon fontSize="small" />}
                                        disabled={history.future.length === 0}
                                        onClick={() => setHistory(redoHistory(history))}
                                    />
                                </div>
                                <div className="lyo-design-outline" role="tree">
                                    <Outline node={history.present} depth={0} selectedId={selected.id} onSelect={setSelectedId} />
                                </div>
                                <Inspector
                                    paneRef={inspectorRef}
                                    root={history.present}
                                    selected={selected}
                                    previewParameters={previewParameters}
                                    commit={edit}
                                />
                            </div>
                        ) : (
                            <div className="lyo-design-parameters">
                                <LyoParameterEditor
                                    title="Parameter schema"
                                    items={rows}
                                    onChange={(next) => {
                                        setRows(next);
                                        setDirty(true);
                                    }}
                                    showRequired
                                    showEncrypt
                                    showOptionsEditor
                                    showDefaultKind
                                    allowReorder
                                    apiClient={queryClient}
                                    editorContext={editorContext}
                                />
                                <LyoParameterValueGrid
                                    title="Example values (preview)"
                                    entries={examples}
                                    onChange={setExamples}
                                    apiClient={queryClient}
                                    editorContext={editorContext}
                                />
                            </div>
                        )}
                    </LyoPaper>
                    <div className="lyo-report-design-stage">
                        <div className="lyo-report-design-stage-bar">
                            Live preview
                            <span>Bound with example parameters</span>
                        </div>
                        <div className="lyo-report-design-stage-scroll">
                            <ReportDesignCanvas
                                node={history.present}
                                selectedId={selected.id}
                                parameters={previewParameters}
                                onSelect={setSelectedId}
                                onDrop={onCanvasDrop}
                                onDelete={onCanvasDelete}
                            />
                        </div>
                    </div>
                </div>
            </div>
            <LyoDialog
                open={Boolean(pendingId)}
                onClose={() => setPendingId(null)}
                title="Discard changes?"
                onSave={() => {
                    const id = pendingId;
                    setDirty(false);
                    setPendingId(null);
                    if (id) switchTo(id, true);
                }}
                saveText="Discard"
            >
                The design has unsaved changes.
            </LyoDialog>
        </LyoValueEditorProvider>
    );
}

function Outline({
    node,
    depth,
    selectedId,
    onSelect,
}: {
    node: CompositionNode;
    depth: number;
    selectedId: string;
    onSelect: (id: string) => void;
}) {
    const [folded, setFolded] = useState<ReadonlySet<string>>(new Set());
    const open = !folded.has(node.id);
    const selected = node.id === selectedId;
    const rowRef = useRef<HTMLDivElement>(null);
    useEffect(() => {
        if (selected) rowRef.current?.scrollIntoView({block: "nearest"});
    }, [selected]);

    return (
        <>
            <div
                ref={rowRef}
                role="treeitem"
                aria-selected={selected}
                aria-expanded={node.children.length > 0 ? open : undefined}
                className={`lyo-design-outline-row${selected ? " is-selected" : ""}${isDimmed(node) ? " is-dimmed" : ""}`}
                style={{paddingLeft: 4 + depth * 14}}
            >
                <button
                    type="button"
                    className="lyo-design-outline-twist"
                    disabled={node.children.length === 0}
                    aria-label={open ? "Collapse" : "Expand"}
                    onClick={() =>
                        setFolded((current) => {
                            const next = new Set(current);
                            if (next.has(node.id)) next.delete(node.id);
                            else next.add(node.id);
                            return next;
                        })
                    }
                >
                    {open ? <ExpandMoreIcon fontSize="small" /> : <ChevronRightIcon fontSize="small" />}
                </button>
                <button type="button" className="lyo-design-outline-hit" onClick={() => onSelect(node.id)}>
                    <OutlineIcon kind={node.kind} />
                    <span>{node.title || node.kind}</span>
                </button>
            </div>
            {open
                ? node.children.map((child) => (
                      <Outline key={child.id} node={child} depth={depth + 1} selectedId={selectedId} onSelect={onSelect} />
                  ))
                : null}
        </>
    );
}

function OutlineIcon({kind}: {kind: string}) {
    if (kind === "report") return <DescriptionIcon fontSize="inherit" />;
    if (kind === "section") return <ViewAgendaIcon fontSize="inherit" />;
    if (kind === "card") return <CreditCardIcon fontSize="inherit" />;
    if (kind === "table") return <TableChartIcon fontSize="inherit" />;
    if (kind === "block") return <NotesIcon fontSize="inherit" />;
    if (kind === "grid") return <GridViewIcon fontSize="inherit" />;
    return <WidgetsIcon fontSize="inherit" />;
}

function Inspector({
    paneRef,
    root,
    selected,
    previewParameters,
    commit,
}: {
    paneRef: RefObject<HTMLDivElement | null>;
    root: CompositionNode;
    selected: CompositionNode;
    previewParameters: readonly PreviewParameter[];
    commit: (next: CompositionNode) => void;
}) {
    const setNode = (patch: Partial<Pick<CompositionNode, "title" | "visibleWhen" | "collapsed">>) => commit(updateNode(root, selected.id, patch));
    const setPayload = (patch: Record<string, unknown>) => commit(updatePayload(root, selected.id, patch));
    const painted = selected.kind === "block" ? paintComposition(selected, previewParameters) : null;
    const rawContent = payloadString(selected, "content");
    const blockPreview = painted?.kind === "block" && painted.content !== rawContent ? painted.content : null;
    const columnCount = typeof selected.payload.columnCount === "number" ? selected.payload.columnCount : Math.max(selected.children.length, 1);

    return (
        <div className="lyo-design-inspector" ref={paneRef}>
            <div className="lyo-design-inspector-kicker">{KIND_LABEL[selected.kind] ?? selected.kind}</div>
            <LyoStack spacing={1.25}>
                {selected.kind === "card" ? (
                    <LyoTextField
                        size="small"
                        fullWidth
                        margin="none"
                        label="Label"
                        value={payloadString(selected, "label").trim() ? payloadString(selected, "label") : selected.title}
                        onChange={(event) => commit(updatePayload(updateNode(root, selected.id, {title: event.target.value}), selected.id, {label: event.target.value}))}
                    />
                ) : (
                    <LyoTextField size="small" fullWidth margin="none" label="Title" value={selected.title} onChange={(event) => setNode({title: event.target.value})} />
                )}
                {selected.kind === "report" ? (
                    <>
                        <LyoTextField size="small" fullWidth margin="none" label="Subtitle" value={payloadString(selected, "subtitle")} onChange={(event) => setPayload({subtitle: event.target.value})} />
                        <LyoTextField
                            size="small"
                            fullWidth
                            margin="none"
                            label="Description"
                            multiline
                            minRows={2}
                            value={payloadString(selected, "description")}
                            onChange={(event) => setPayload({description: event.target.value})}
                        />
                        <LyoTextField size="small" fullWidth margin="none" label="Footer" value={payloadString(selected, "footer")} onChange={(event) => setPayload({footer: event.target.value})} />
                    </>
                ) : null}
                {selected.kind === "section" ? (
                    <LyoTextField size="small" fullWidth margin="none" label="Subtitle" value={payloadString(selected, "subtitle")} onChange={(event) => setPayload({subtitle: event.target.value})} />
                ) : null}
                {selected.kind === "card" ? (
                    <LyoTextField size="small" fullWidth margin="none" label="Value" value={payloadString(selected, "value")} onChange={(event) => setPayload({value: event.target.value})} />
                ) : null}
                {selected.kind === "grid" ? (
                    <LyoTextField
                        size="small"
                        fullWidth
                        margin="none"
                        type="number"
                        label="Columns"
                        value={columnCount}
                        onChange={(event) => setPayload({columnCount: Math.max(1, Number(event.target.value) || 1)})}
                    />
                ) : null}
                {selected.kind === "block" ? (
                    <LyoTextField
                        size="small"
                        fullWidth
                        margin="none"
                        label="Content"
                        multiline
                        minRows={4}
                        value={rawContent}
                        onChange={(event) => setPayload({content: event.target.value})}
                    />
                ) : null}
                {blockPreview ? <p className="lyo-design-token-preview">{blockPreview}</p> : null}
                {selected.kind === "table" ? <TableFields node={selected} onChange={setPayload} /> : null}
                {selected.kind !== "report" ? (
                    <LyoTextField
                        size="small"
                        fullWidth
                        margin="none"
                        label="Visible when"
                        helperText="Key, !Key, Key=value, or Key!=value"
                        value={selected.visibleWhen ?? ""}
                        onChange={(event) => setNode({visibleWhen: event.target.value})}
                    />
                ) : null}
                {selected.kind === "section" ? (
                    <label className="lyo-design-check">
                        <LyoSwitch size="small" checked={Boolean(selected.collapsed)} onChange={(_, checked) => setNode({collapsed: checked})} />
                        Collapsed
                    </label>
                ) : null}
            </LyoStack>
        </div>
    );
}

function TableFields({node, onChange}: {node: CompositionNode; onChange: (patch: Record<string, unknown>) => void}) {
    const columns = columnList(node);
    const rows = rowList(node);
    const width = Math.max(columns.length, ...rows.map((row) => cellList(row).length), 0);

    const write = (nextColumns: unknown[], nextRows: unknown[]) => onChange({columns: nextColumns, rows: nextRows});

    return (
        <>
            <div className="lyo-design-field-label">Columns</div>
            {columns.map((column, index) => (
                <div className="lyo-design-edit-row" key={`column-${index}`}>
                    <LyoTextField
                        size="small"
                        margin="none"
                        label={`Column ${index + 1}`}
                        value={headerText(column, index)}
                        onChange={(event) => {
                            const next = columns.slice();
                            next[index] = withHeader(column, event.target.value);
                            write(next, rows);
                        }}
                    />
                    <LyoIconButton
                        size="small"
                        aria-label="Remove column"
                        onClick={() =>
                            write(
                                columns.filter((_, columnIndex) => columnIndex !== index),
                                rows.map((row) => withCells(row, cellList(row).filter((_, cellIndex) => cellIndex !== index)))
                            )
                        }
                    >
                        <DeleteOutlineIcon fontSize="small" />
                    </LyoIconButton>
                </div>
            ))}
            <LyoButton size="small" onClick={() => write([...columns, {header: `Column ${columns.length + 1}`}], rows.map((row) => withCells(row, [...cellList(row), ""])))}>
                Add column
            </LyoButton>
            <div className="lyo-design-field-label">Rows</div>
            {rows.map((row, rowIndex) => {
                const cells = paddedCells(row, width);
                return (
                    <div className="lyo-design-edit-row" key={`row-${rowIndex}`}>
                        {cells.map((cell, cellIndex) => (
                            <LyoTextField
                                key={`cell-${cellIndex}`}
                                size="small"
                                margin="none"
                                label={headerText(columns[cellIndex], cellIndex)}
                                value={cell}
                                onChange={(event) => {
                                    const nextCells = cells.slice();
                                    nextCells[cellIndex] = event.target.value;
                                    const nextRows = rows.slice();
                                    nextRows[rowIndex] = withCells(row, nextCells);
                                    write(columns, nextRows);
                                }}
                            />
                        ))}
                        <LyoIconButton
                            size="small"
                            aria-label="Remove row"
                            onClick={() => write(columns, rows.filter((_, index) => index !== rowIndex))}
                        >
                            <DeleteOutlineIcon fontSize="small" />
                        </LyoIconButton>
                    </div>
                );
            })}
            <LyoButton size="small" onClick={() => write(columns, [...rows, {cells: Array.from({length: Math.max(width, 1)}, () => "")}])}>
                Add row
            </LyoButton>
        </>
    );
}

export function ReportDesignCanvas({
    node,
    selectedId,
    onSelect,
    onDrop,
    onDelete,
    parameters = [],
}: {
    node: CompositionNode;
    selectedId: string;
    onSelect: (id: string) => void;
    onDrop: (id: string, parentId: string, index?: number) => void;
    onDelete?: (id: string) => void;
    parameters?: readonly PreviewParameter[];
}) {
    const [draggingId, setDraggingId] = useState<string | null>(null);
    const [overSlot, setOverSlot] = useState<string | null>(null);
    const paint = paintComposition(node, parameters);
    const actions: CanvasActions = {
        selectedId,
        draggingId,
        overSlot,
        setDraggingId,
        setOverSlot,
        onSelect,
        onDrop: (id, parentId, index) => onDrop(id, parentId, index),
        onDelete,
    };

    return (
        <CanvasContext.Provider value={actions}>
            {paint.kind === "report" ? (
                <ReportPage paint={paint} />
            ) : (
                <div className={`lyo-report-design-page${draggingId ? " is-dragging" : ""}`}>
                    <DesignBlock paint={paint} />
                </div>
            )}
        </CanvasContext.Provider>
    );
}

function ReportPage({paint}: {paint: PaintReport}) {
    const {selectedId, draggingId, onSelect} = useCanvas();
    const selected = paint.id === selectedId;
    return (
        <div
            className={`lyo-report-design-page${draggingId ? " is-dragging" : ""}`}
            onClick={() => onSelect(paint.id)}
            onDragOver={(event) => {
                if (draggingId) event.preventDefault();
            }}
        >
            <header
                className={`lyo-design-title${selected ? " is-selected" : ""}`}
                onClick={(event) => {
                    event.stopPropagation();
                    onSelect(paint.id);
                }}
            >
                <h1>{paint.title || "Untitled report"}</h1>
                {paint.subtitle ? <p>{paint.subtitle}</p> : null}
            </header>
            {paint.description ? <div className="lyo-design-description">{paint.description}</div> : null}
            <DesignChildren paint={paint} emptyText="Add a section" groupCards />
            {paint.footer ? <footer className="lyo-design-footer">{paint.footer}</footer> : null}
        </div>
    );
}

function DesignChildren({
    paint,
    emptyText,
    groupCards,
    className,
    style,
}: {
    paint: PaintNode;
    emptyText?: string;
    groupCards: boolean;
    className?: string;
    style?: CSSProperties;
}) {
    const children = childList(paint);
    const {draggingId, overSlot, setOverSlot, onDrop} = useCanvas();
    if (children.length === 0) {
        if (!emptyText) return null;
        const slotKey = `${paint.id}:0`;
        return (
            <div
                className={`lyo-design-empty${overSlot === slotKey ? " is-over" : ""}`}
                onDragOver={(event) => {
                    if (!draggingId) return;
                    event.preventDefault();
                    event.stopPropagation();
                    setOverSlot(slotKey);
                }}
                onDrop={(event) => {
                    event.preventDefault();
                    event.stopPropagation();
                    const id = event.dataTransfer.getData("text/plain");
                    setOverSlot(null);
                    if (id) onDrop(id, paint.id, 0);
                }}
            >
                {emptyText}
            </div>
        );
    }

    const items: ReactNode[] = [<DropSlot key={`${paint.id}:0`} parentId={paint.id} index={0} />];
    let index = 0;
    while (index < children.length) {
        const child = children[index]!;
        if (groupCards && child.kind === "card") {
            const start = index;
            const cards: PaintNode[] = [];
            while (index < children.length && children[index]!.kind === "card") cards.push(children[index++]!);
            items.push(
                <div className="lyo-design-card-row" key={`${paint.id}:cards:${start}`}>
                    {cards.map((card, offset) => (
                        <Fragment key={card.id}>
                            {offset > 0 ? <DropSlot parentId={paint.id} index={start + offset} vertical /> : null}
                            <DesignBlock paint={card} />
                        </Fragment>
                    ))}
                </div>
            );
        } else {
            items.push(<DesignBlock key={child.id} paint={child} />);
            index += 1;
        }
        items.push(<DropSlot key={`${paint.id}:${index}`} parentId={paint.id} index={index} />);
    }

    return (
        <div className={className ?? "lyo-design-flow"} style={style}>
            {items}
        </div>
    );
}

function DesignBlock({paint}: {paint: PaintNode}) {
    const {selectedId, onSelect} = useCanvas();
    if (paint.kind === "report") return null;
    const selected = paint.id === selectedId;
    return (
        <div
            className={`lyo-design-block lyo-design-${paint.kind}${selected ? " is-selected" : ""}${paint.dimmed ? " is-dimmed" : ""}`}
            style={paint.kind === "section" ? {marginLeft: paint.depth * 20} : undefined}
            data-lyo-design={paint.id}
            onClick={(event) => {
                event.stopPropagation();
                onSelect(paint.id);
            }}
        >
            <HoverBar id={paint.id} />
            {paint.kind === "section" ? (
                <>
                    <h2 className="lyo-design-section-title">{paint.title || "Section"}</h2>
                    {paint.subtitle ? <p className="lyo-design-section-subtitle">{paint.subtitle}</p> : null}
                    <DesignChildren paint={paint} emptyText="Add a card, table, or text block" groupCards />
                </>
            ) : null}
            {paint.kind === "card" ? (
                <>
                    <div className="lyo-design-card-label">{paint.label || "Card"}</div>
                    <div className="lyo-design-card-value">{paint.value || "—"}</div>
                    {paint.children.length > 0 ? <DesignChildren paint={paint} groupCards /> : null}
                </>
            ) : null}
            {paint.kind === "table" ? (
                <div className="lyo-design-table-wrap">
                    {paint.title ? <div className="lyo-design-table-title">{paint.title}</div> : null}
                    {paint.headers.length === 0 ? (
                        <p className="lyo-design-muted">Add columns in the inspector.</p>
                    ) : (
                        <table className="lyo-design-table">
                            <thead>
                                <tr>
                                    {paint.headers.map((header, index) => (
                                        <th key={index}>{header}</th>
                                    ))}
                                </tr>
                            </thead>
                            <tbody>
                                {paint.rows.length === 0 ? (
                                    <tr>
                                        <td colSpan={paint.headers.length}>No rows</td>
                                    </tr>
                                ) : (
                                    paint.rows.map((row, rowIndex) => (
                                        <tr key={rowIndex}>
                                            {paint.headers.map((_, cellIndex) => (
                                                <td key={cellIndex}>{row[cellIndex] ?? ""}</td>
                                            ))}
                                        </tr>
                                    ))
                                )}
                            </tbody>
                        </table>
                    )}
                </div>
            ) : null}
            {paint.kind === "block" ? (
                paint.content ? <p className="lyo-design-copy">{paint.content}</p> : <p className="lyo-design-muted">{paint.title || "Empty text"}</p>
            ) : null}
            {paint.kind === "grid" ? (
                <>
                    {paint.title ? <div className="lyo-design-block-title">{paint.title}</div> : null}
                    <DesignChildren
                        paint={paint}
                        emptyText="Add a card, table, or text block"
                        groupCards={false}
                        className="lyo-design-grid"
                        style={{gridTemplateColumns: `repeat(${Math.max(paint.columns, 1)}, minmax(0, 1fr))`}}
                    />
                </>
            ) : null}
            {paint.kind === "group" ? (
                <>
                    <div className="lyo-design-block-title">{paint.title || paint.sourceKind}</div>
                    <DesignChildren paint={paint} groupCards={false} />
                </>
            ) : null}
        </div>
    );
}

function HoverBar({id}: {id: string}) {
    const {setDraggingId, setOverSlot, onDelete} = useCanvas();
    const startDrag = (event: DragEvent<HTMLElement>) => {
        event.stopPropagation();
        event.dataTransfer.setData("text/plain", id);
        event.dataTransfer.effectAllowed = "move";
        setDraggingId(id);
    };
    return (
        <div className="lyo-design-hoverbar" onClick={(event) => event.stopPropagation()}>
            <span
                className="lyo-design-drag"
                draggable
                title="Move"
                onDragStart={startDrag}
                onDragEnd={() => {
                    setDraggingId(null);
                    setOverSlot(null);
                }}
            >
                <DragIndicatorIcon fontSize="small" />
            </span>
            {onDelete ? (
                <LyoIconButton
                    size="small"
                    color="error"
                    aria-label="Delete"
                    onClick={(event) => {
                        event.stopPropagation();
                        onDelete(id);
                    }}
                >
                    <DeleteOutlineIcon fontSize="small" />
                </LyoIconButton>
            ) : null}
        </div>
    );
}

function DropSlot({parentId, index, vertical = false}: {parentId: string; index: number; vertical?: boolean}) {
    const {draggingId, overSlot, setDraggingId, setOverSlot, onDrop} = useCanvas();
    const slotKey = `${parentId}:${index}`;
    return (
        <div
            className={`lyo-design-slot${vertical ? " is-vertical" : ""}${overSlot === slotKey ? " is-over" : ""}`}
            onDragOver={(event) => {
                if (!draggingId) return;
                event.preventDefault();
                event.stopPropagation();
                setOverSlot(slotKey);
            }}
            onDrop={(event) => {
                event.preventDefault();
                event.stopPropagation();
                const id = event.dataTransfer.getData("text/plain");
                setOverSlot(null);
                setDraggingId(null);
                if (id) onDrop(id, parentId, index);
            }}
        />
    );
}

function childList(paint: PaintNode): PaintNode[] {
    if (paint.kind === "table" || paint.kind === "block") return [];
    return paint.children;
}

function payloadString(node: CompositionNode, key: string): string {
    const value = node.payload[key];
    if (typeof value === "string") return value;
    if (typeof value === "number" || typeof value === "boolean") return String(value);
    return "";
}

function columnList(node: CompositionNode): unknown[] {
    return Array.isArray(node.payload.columns) ? node.payload.columns : [];
}

function rowList(node: CompositionNode): unknown[] {
    return Array.isArray(node.payload.rows) ? node.payload.rows : [];
}

function headerText(column: unknown, index: number): string {
    if (typeof column === "string") return column;
    const record = recordOf(column);
    if (record && typeof record.header === "string") return record.header;
    return `Column ${index + 1}`;
}

function cellList(row: unknown): string[] {
    if (Array.isArray(row)) return row.map((cell) => (cell == null ? "" : String(cell)));
    const record = recordOf(row);
    if (record && Array.isArray(record.cells)) return record.cells.map((cell) => (cell == null ? "" : String(cell)));
    return [];
}

function paddedCells(row: unknown, width: number): string[] {
    const cells = cellList(row);
    if (cells.length >= width) return cells;
    return [...cells, ...Array<string>(width - cells.length).fill("")];
}

function withHeader(column: unknown, header: string): unknown {
    const record = recordOf(column);
    if (!record) return {header};
    return {...record, header};
}

function withCells(row: unknown, cells: string[]): unknown {
    if (Array.isArray(row)) return cells;
    return {...(recordOf(row) ?? {}), cells};
}

function recordOf(value: unknown): Record<string, unknown> | null {
    return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}

function collectIds(node: CompositionNode, into: Set<string>) {
    into.add(node.id);
    for (const child of node.children) collectIds(child, into);
}

function newestId(before: CompositionNode, after: CompositionNode): string | null {
    const known = new Set<string>();
    collectIds(before, known);
    const walk = (node: CompositionNode): string | null => {
        if (!known.has(node.id)) return node.id;
        for (const child of node.children) {
            const found = walk(child);
            if (found) return found;
        }
        return null;
    };
    return walk(after);
}
