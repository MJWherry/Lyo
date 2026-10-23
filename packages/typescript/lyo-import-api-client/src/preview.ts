export const PREVIEW_CAP = 50;

export const HOST_PERSISTS_COPY = "The host persists the returned rows.";

export type ImportFormat = "Json" | "Csv" | "Xlsx" | "Yaml" | "Xml" | "Toml";

export type ImportJsonMode = "Array" | "Wrapped" | "Ndjson";

export interface ImportFieldMap {
    source: string;
    target: string;
    required?: boolean;
}

export interface ImportIssue {
    index: number;
    message: string;
}

export interface ImportPreviewState {
    fields: ImportFieldMap[];
    blockedReason: string | null;
    preview: string[];
    issues: ImportIssue[];
}

export interface ImportRequest {
    format: ImportFormat;
    jsonMode?: ImportJsonMode;
    rowsPath?: string;
    sheetName?: string;
    content?: string;
    contentBytes?: string;
    fields?: ImportFieldMap[];
}

export interface ImportResponse {
    isSuccess: boolean;
    message?: string | null;
    rowsJson?: string;
    issues?: ImportIssue[];
}

export function emptyPreview(): ImportPreviewState {
    return {fields: [], blockedReason: null, preview: [], issues: []};
}

/** A second source aimed at the same target is not stored. */
export function tryAddField(state: ImportPreviewState, field: ImportFieldMap): ImportPreviewState {
    if (state.fields.some((existing) => existing.target === field.target)) {
        return {...state, blockedReason: `Two sources map to '${field.target}'.`};
    }
    return {...state, fields: [...state.fields, field], blockedReason: null};
}

/** Preview shows at most 50 good rows. Every issue is kept. */
export function showPreview<T>(rows: readonly T[], issues: readonly ImportIssue[], label: (row: T) => string): Pick<ImportPreviewState, "preview" | "issues"> {
    return {
        preview: rows.slice(0, PREVIEW_CAP).map(label),
        issues: [...issues],
    };
}

export function issueListText(issues: readonly ImportIssue[]): string {
    return issues.map((issue) => `${issue.index}: ${issue.message}`).join("\n");
}
