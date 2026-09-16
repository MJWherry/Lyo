import {previewTemplate} from "lyo-formatter";
import {durationColor, type LyoDurationColor} from "lyo-web-components";
import {LyoChipHues, type LyoChipHueName} from "lyo-web-components";
import {generationStatusColor, type ReportStatusColor} from "./reportColors.js";

export const CARD_PREVIEW_ROW_CAP = 200;

export type ReportSheet = {title: string; headers: string[]; rows: string[][]};

export function reportFormatterContext(
    definition: Record<string, unknown> | null,
    parameters: readonly {key: string; value: string | null}[] = []
): Record<string, unknown> {
    const values: Record<string, string | null> = {};
    for (const parameter of parameters) values[parameter.key] = parameter.value;
    const data: Record<string, unknown> = {
        Definition: definition,
        Parameters: values,
    };
    if (definition) {
        for (const [key, value] of Object.entries(definition)) data[key] = value;
    }
    for (const [key, value] of Object.entries(values)) data[`Parameter_${key}`] = value;
    return data;
}

function cellText(value: unknown): string {
    if (value == null) return "";
    if (typeof value === "object" && value && "text" in value) return String((value as {text: unknown}).text ?? "");
    return String(value);
}

function tableOf(raw: Record<string, unknown>, index: number): ReportSheet {
    const columns = Array.isArray(raw.columns) ? raw.columns : [];
    const headers = columns.map((column, columnIndex) => {
        if (typeof column === "string") return column;
        if (column && typeof column === "object" && "header" in column) return String((column as {header: unknown}).header ?? "");
        return `Col ${columnIndex + 1}`;
    });
    const sourceRows = Array.isArray(raw.rows) ? raw.rows : [];
    const rows = sourceRows.map((row) => {
        if (Array.isArray(row)) return row.map(cellText);
        if (row && typeof row === "object" && Array.isArray((row as {cells?: unknown}).cells)) {
            return (row as {cells: unknown[]}).cells.map(cellText);
        }
        return [];
    });
    const title = typeof raw.title === "string" && raw.title.trim() ? raw.title.trim() : `Table ${index + 1}`;
    return {title, headers, rows};
}

function collectTables(node: unknown, into: ReportSheet[]): void {
    if (!node || typeof node !== "object") return;
    const record = node as Record<string, unknown>;
    if (Array.isArray(record.columns) || Array.isArray(record.rows)) into.push(tableOf(record, into.length));
    for (const value of Object.values(record)) {
        if (Array.isArray(value)) value.forEach((item) => collectTables(item, into));
        else if (value && typeof value === "object") collectTables(value, into);
    }
}

export function sheetsFromReportJson(json: string): ReportSheet[] {
    if (!json.trim()) return [];
    const parsed = JSON.parse(json) as unknown;
    const sheets: ReportSheet[] = [];
    collectTables(parsed, sheets);
    return sheets;
}

export function capPreviewRows<T>(rows: readonly T[], cap = CARD_PREVIEW_ROW_CAP): T[] {
    return rows.slice(0, cap);
}

const FORMAT_HUES: Record<string, LyoChipHueName> = {
    html: "Blue",
    pdf: "Rose",
    csv: "Teal",
    xlsx: "Lime",
    json: "Violet",
};

export function formatHue(format: string | null | undefined): LyoChipHueName | null {
    if (!format) return null;
    return FORMAT_HUES[format.trim().toLowerCase()] ?? null;
}

export function formatHueDegrees(format: string | null | undefined): number | null {
    const name = formatHue(format);
    return name ? LyoChipHues[name] : null;
}

export function statusChipColor(status: string | null | undefined): ReportStatusColor {
    return generationStatusColor(status);
}

export function durationChipColor(ms: number | null | undefined): LyoDurationColor | "inherit" {
    if (ms == null) return "inherit";
    return durationColor(ms);
}

export type CompositionNode = {
    id: string;
    kind: string;
    title: string;
    visibleWhen?: string | null;
    collapsed?: boolean;
    /** Fields other than id, title, visibility, and child lists. Written back into the composition JSON. */
    payload: Record<string, unknown>;
    children: CompositionNode[];
};

const CHILD_KEYS = new Set(["controls", "sections", "subsections"]);
const OWNED_KEYS = new Set(["id", "title", "visibleWhen", "collapsed", "kind", ...CHILD_KEYS]);

function recordOf(value: unknown): Record<string, unknown> | null {
    return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}

function payloadOf(raw: Record<string, unknown>): Record<string, unknown> {
    const payload: Record<string, unknown> = {};
    for (const [key, value] of Object.entries(raw)) {
        if (!OWNED_KEYS.has(key)) payload[key] = value;
    }
    return payload;
}

function nodeFrom(
    raw: Record<string, unknown>,
    kind: string,
    fallbackId: string,
    children: CompositionNode[]
): CompositionNode {
    return {
        id: typeof raw.id === "string" && raw.id.trim() ? raw.id : fallbackId,
        kind,
        title: typeof raw.title === "string" ? raw.title : kind,
        visibleWhen: typeof raw.visibleWhen === "string" ? raw.visibleWhen : null,
        collapsed: raw.collapsed === true,
        payload: payloadOf(raw),
        children,
    };
}

function readControl(raw: Record<string, unknown>, fallbackId: string): CompositionNode {
    const kind = typeof raw.kind === "string" && raw.kind.trim() ? raw.kind : "block";
    const nested = kind === "grid" || kind === "card" ? readControls(raw.controls, fallbackId) : [];
    const title =
        typeof raw.title === "string" && raw.title.trim()
            ? raw.title
            : typeof raw.contentType === "string"
              ? raw.contentType
              : kind;
    return {...nodeFrom(raw, kind, fallbackId, nested), title};
}

function readControls(value: unknown, parentId: string): CompositionNode[] {
    if (!Array.isArray(value)) return [];
    return value.flatMap((item, index) => {
        const raw = recordOf(item);
        return raw ? [readControl(raw, `${parentId}-c${index}`)] : [];
    });
}

function readSection(raw: Record<string, unknown>, fallbackId: string): CompositionNode {
    const id = typeof raw.id === "string" && raw.id.trim() ? raw.id : fallbackId;
    const controls = readControls(raw.controls, id);
    const subsections = readSections(raw.subsections, id);
    return nodeFrom(raw, "section", id, [...controls, ...subsections]);
}

function readSections(value: unknown, parentId: string): CompositionNode[] {
    if (!Array.isArray(value)) return [];
    return value.flatMap((item, index) => {
        const raw = recordOf(item);
        return raw ? [readSection(raw, `${parentId}-s${index}`)] : [];
    });
}

export function blankComposition(title = "Untitled report"): CompositionNode {
    return parseComposition(
        JSON.stringify({
            title,
            subtitle: "Designed in the report workbench",
            footer: "Generated by Lyo Reporting",
            sections: [
                {
                    id: "summary",
                    title: "Summary",
                    controls: [{kind: "block", id: "intro", contentType: "Text", content: "Replace this text."}],
                },
            ],
        })
    );
}

export const COMPOSITION_TEMPLATES: Record<string, CompositionNode> = {
    Blank: blankComposition("Untitled report"),
    Gallery: parseComposition(
        JSON.stringify({
            title: "Gallery",
            sections: [{id: "overview", title: "Overview", controls: [{kind: "block", id: "hero", contentType: "Image", title: "Cover"}]}],
        })
    ),
    Sales: parseComposition(
        JSON.stringify({
            title: "Sales",
            sections: [{id: "sales", title: "Sales", controls: [{kind: "table", id: "sales-table", title: "Sales", columns: [], rows: []}]}],
        })
    ),
    Invoice: parseComposition(
        JSON.stringify({
            title: "Invoice",
            sections: [{id: "lines", title: "Lines", controls: [{kind: "table", id: "line-items", title: "Lines", columns: [], rows: []}]}],
        })
    ),
    Ops: parseComposition(
        JSON.stringify({
            title: "Ops",
            sections: [
                {
                    id: "health",
                    title: "Health",
                    controls: [{kind: "block", id: "health-text", contentType: "Text", content: "Health"}],
                },
            ],
        })
    ),
};

export function parseComposition(json: string): CompositionNode {
    let parsed: unknown = null;
    try {
        parsed = json.trim() ? JSON.parse(json) : null;
    } catch {
        parsed = null;
    }
    const raw = recordOf(parsed);
    if (!raw) return blankComposition();
    return nodeFrom(raw, "report", "root", readSections(raw.sections, "root"));
}

export function compositionJson(node: CompositionNode): string {
    return JSON.stringify(writeNode(node));
}

function writeNode(node: CompositionNode): Record<string, unknown> {
    const out: Record<string, unknown> = {...node.payload};
    if (node.kind !== "report") out.id = node.id;
    if (node.title) out.title = node.title;
    if (node.visibleWhen?.trim()) out.visibleWhen = node.visibleWhen;
    if (node.collapsed) out.collapsed = true;
    if (node.kind === "report") {
        out.sections = node.children.map(writeNode);
        return out;
    }
    if (node.kind === "section") {
        out.controls = node.children.filter((child) => child.kind !== "section").map(writeNode);
        out.subsections = node.children.filter((child) => child.kind === "section").map(writeNode);
        return out;
    }
    out.kind = node.kind;
    if (node.kind === "grid" || node.kind === "card") out.controls = node.children.map(writeNode);
    return out;
}

export function findNode(root: CompositionNode, id: string): CompositionNode | null {
    if (root.id === id) return root;
    for (const child of root.children) {
        const found = findNode(child, id);
        if (found) return found;
    }
    return null;
}

export function updateNode(
    root: CompositionNode,
    id: string,
    patch: Partial<Pick<CompositionNode, "title" | "visibleWhen" | "collapsed">>
): CompositionNode {
    if (root.id === id) return {...root, ...patch};
    return {...root, children: root.children.map((child) => updateNode(child, id, patch))};
}

export function isDimmed(node: CompositionNode): boolean {
    return Boolean(node.collapsed) || Boolean(node.visibleWhen?.trim());
}

type PaintBase = {id: string; dimmed: boolean};

export type PaintReport = PaintBase & {
    kind: "report";
    title: string;
    subtitle: string;
    description: string;
    footer: string;
    empty: boolean;
    children: PaintNode[];
};

export type PaintSection = PaintBase & {
    kind: "section";
    title: string;
    subtitle: string;
    depth: number;
    empty: boolean;
    children: PaintNode[];
};

export type PaintCard = PaintBase & {
    kind: "card";
    label: string;
    value: string;
    children: PaintNode[];
};

export type PaintTable = PaintBase & {
    kind: "table";
    title: string;
    headers: string[];
    rows: string[][];
};

export type PaintBlock = PaintBase & {
    kind: "block";
    title: string;
    content: string;
};

export type PaintGrid = PaintBase & {
    kind: "grid";
    title: string;
    columns: number;
    children: PaintNode[];
};

export type PaintGroup = PaintBase & {
    kind: "group";
    sourceKind: string;
    title: string;
    children: PaintNode[];
};

export type PaintNode = PaintReport | PaintSection | PaintCard | PaintTable | PaintBlock | PaintGrid | PaintGroup;

function payloadText(value: unknown): string {
    if (typeof value === "string") return value;
    if (typeof value === "number" || typeof value === "boolean") return String(value);
    if (value && typeof value === "object" && "text" in value) return String((value as {text: unknown}).text ?? "");
    return "";
}

function fill(value: unknown, context: Record<string, unknown>): string {
    return previewTemplate(payloadText(value), context);
}

function paintContext(parameters: readonly {key: string; value: string | null}[]): Record<string, unknown> {
    const context = reportFormatterContext(null, parameters);
    for (const parameter of parameters) context[parameter.key] = parameter.value;
    return context;
}

function paintChildren(node: CompositionNode, context: Record<string, unknown>, depth: number): PaintNode[] {
    return node.children.map((child) => paintNode(child, context, depth));
}

function paintNode(node: CompositionNode, context: Record<string, unknown>, depth: number): PaintNode {
    const dimmed = isDimmed(node);
    const title = fill(node.title, context);
    if (node.kind === "report") {
        return {
            kind: "report",
            id: node.id,
            dimmed,
            title,
            subtitle: fill(node.payload.subtitle, context),
            description: fill(node.payload.description, context),
            footer: fill(node.payload.footer, context),
            empty: node.children.length === 0,
            children: paintChildren(node, context, 0),
        };
    }
    if (node.kind === "section") {
        return {
            kind: "section",
            id: node.id,
            dimmed,
            title,
            subtitle: fill(node.payload.subtitle, context),
            depth,
            empty: node.children.length === 0,
            children: paintChildren(node, context, depth + 1),
        };
    }
    if (node.kind === "card") {
        const rawLabel = payloadText(node.payload.label);
        const label = rawLabel.trim() ? rawLabel : node.title;
        return {
            kind: "card",
            id: node.id,
            dimmed,
            label: fill(label, context),
            value: fill(node.payload.value, context),
            children: paintChildren(node, context, depth),
        };
    }
    if (node.kind === "table") {
        const sheet = tableOf({...node.payload, title: node.title}, 0);
        return {
            kind: "table",
            id: node.id,
            dimmed,
            title,
            headers: sheet.headers.map((header) => fill(header, context)),
            rows: sheet.rows.map((row) => row.map((cell) => fill(cell, context))),
        };
    }
    if (node.kind === "block") {
        return {
            kind: "block",
            id: node.id,
            dimmed,
            title,
            content: fill(node.payload.content, context),
        };
    }
    if (node.kind === "grid") {
        const count = node.payload.columnCount;
        const columns = typeof count === "number" && count > 0 ? count : Math.max(node.children.length, 1);
        return {
            kind: "grid",
            id: node.id,
            dimmed,
            title,
            columns,
            children: paintChildren(node, context, depth),
        };
    }
    return {
        kind: "group",
        sourceKind: node.kind,
        id: node.id,
        dimmed,
        title,
        children: paintChildren(node, context, depth),
    };
}

/** Document view of a composition. `{Key}` tokens resolve from example parameters. */
export function paintComposition(
    node: CompositionNode,
    parameters: readonly {key: string; value: string | null}[] = []
): PaintNode {
    return paintNode(node, paintContext(parameters), 0);
}

export type History<T> = {past: T[]; present: T; future: T[]};

export function historyOf<T>(present: T): History<T> {
    return {past: [], present, future: []};
}

export function pushHistory<T>(history: History<T>, next: T): History<T> {
    return {past: [...history.past, history.present], present: next, future: []};
}

export function undoHistory<T>(history: History<T>): History<T> {
    const previous = history.past[history.past.length - 1];
    if (previous === undefined) return history;
    return {past: history.past.slice(0, -1), present: previous, future: [history.present, ...history.future]};
}

export function redoHistory<T>(history: History<T>): History<T> {
    const next = history.future[0];
    if (next === undefined) return history;
    return {past: [...history.past, history.present], present: next, future: history.future.slice(1)};
}

export type DesignKind = "section" | "card" | "table" | "block";

function freshId(kind: string): string {
    return `${kind}-${Math.random().toString(36).slice(2, 8)}`;
}

function cloneWithNewIds(node: CompositionNode): CompositionNode {
    return {...node, id: freshId(node.kind), children: node.children.map(cloneWithNewIds)};
}

/** Adds a section, card, table, or text block under `parentId`. */
export function addChild(root: CompositionNode, parentId: string, kind: DesignKind): CompositionNode {
    const node: CompositionNode = {
        id: freshId(kind),
        kind,
        title: kind === "section" ? "Section" : kind === "block" ? "Text" : kind === "card" ? "Card" : "Table",
        payload: kind === "block" ? {contentType: "Text", content: ""} : kind === "table" ? {columns: [], rows: []} : {},
        children: [],
    };
    return insertNode(root, parentId, Number.MAX_SAFE_INTEGER, node);
}

/** Removes a node. The report root stays. */
export function deleteNode(root: CompositionNode, id: string): CompositionNode {
    if (root.id === id) return root;
    return removeNode(root, id);
}

/** Inserts a copy with new ids immediately after `id`. */
export function duplicateNode(root: CompositionNode, id: string): CompositionNode {
    const node = findNode(root, id);
    if (!node || node.id === root.id) return root;
    return insertAfter(root, id, cloneWithNewIds(node));
}

export function updatePayload(root: CompositionNode, id: string, patch: Record<string, unknown>): CompositionNode {
    if (root.id === id) return {...root, payload: {...root.payload, ...patch}};
    return {...root, children: root.children.map((child) => updatePayload(child, id, patch))};
}

function insertAfter(root: CompositionNode, id: string, node: CompositionNode): CompositionNode {
    const index = root.children.findIndex((child) => child.id === id);
    if (index >= 0) {
        const children = root.children.slice();
        children.splice(index + 1, 0, node);
        return {...root, children};
    }
    return {...root, children: root.children.map((child) => insertAfter(child, id, node))};
}

export function siblingIndex(root: CompositionNode, id: string): {parentId: string; index: number} | null {
    const index = root.children.findIndex((child) => child.id === id);
    if (index >= 0) return {parentId: root.id, index};
    for (const child of root.children) {
        const found = siblingIndex(child, id);
        if (found) return found;
    }
    return null;
}

export function moveNode(root: CompositionNode, id: string, parentId: string, index: number): CompositionNode {
    const node = findNode(root, id);
    if (!node || node.id === root.id) return root;
    const without = removeNode(root, id);
    return insertNode(without, parentId, index, node);
}

function removeNode(root: CompositionNode, id: string): CompositionNode {
    return {...root, children: root.children.filter((child) => child.id !== id).map((child) => removeNode(child, id))};
}

function insertNode(root: CompositionNode, parentId: string, index: number, node: CompositionNode): CompositionNode {
    if (root.id === parentId) {
        const children = root.children.slice();
        children.splice(Math.max(0, Math.min(index, children.length)), 0, node);
        return {...root, children};
    }
    return {...root, children: root.children.map((child) => insertNode(child, parentId, index, node))};
}
