/** Wire JSON for Lyo.Api CRUD + Query results (`LyoJsonSerializerOptions` camelCase). */

export interface LyoProblemDetails {
    title?: string;
    detail?: string;
    status?: number;
    errors?: Array<{
        code?: string;
        description?: string;
    }>;

    [key: string]: unknown;
}

export interface QueryRes<TItem, TRequest = unknown> {
    queryRequest: TRequest;
    isSuccess: boolean;
    items?: TItem[] | null;
    start?: number | null;
    amount?: number | null;
    total?: number | null;
    hasMore?: boolean | null;
    queryScore: number;
    error?: LyoProblemDetails | null;
}

export interface ProjectedQueryRes<TRow = Record<string, unknown>, TRequest = unknown> {
    queryRequest: TRequest;
    isSuccess: boolean;
    items?: TRow[] | null;
    start?: number | null;
    amount?: number | null;
    total?: number | null;
    hasMore?: boolean | null;
    queryScore: number;
    error?: LyoProblemDetails | null;
    entityTypes?: string[] | null;
}

export interface CreateResult<T> {
    isSuccess: boolean;
    data?: T | null;
    error?: LyoProblemDetails | null;
}

export type UpdateResultEnum = "Updated" | "NoChange" | "Failed" | number;

export interface UpdateResult<T> {
    result: UpdateResultEnum;
    keys?: unknown[] | null;
    oldData?: T | null;
    newData?: T | null;
    error?: LyoProblemDetails | null;
}

export interface DeleteResult<T> {
    isSuccess: boolean;
    data?: T | null;
    error?: LyoProblemDetails | null;
}

export interface UpdateRequest<T> {
    keys: unknown[];
    data: T;
}

export interface DeleteRequest {
    keys?: unknown[][] | null;
    query?: unknown;
    allowMultiple?: boolean;
}

export interface DeleteBulkResult<T = unknown> {
    results?: Array<DeleteResult<T>> | null;
    deletedCount: number;
    failedCount: number;
}

export type ExportFormat = "Csv" | "Xlsx" | "Json";

export interface ExportColumnMapping {
    /** Column header in the file. */
    header: string;
    /** Property path or SmartFormat template (C# `ExportColumnMapping.Value`). */
    value: string;
}

export interface ExportRequest {
    query: unknown;
    format: ExportFormat;
    columns?: Record<string, string> | null;
    columnList?: ExportColumnMapping[] | null;
}

export interface ExportDownload {
    blob: Blob;
    fileName: string | null;
    contentType: string;
}
