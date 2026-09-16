import type {JobRunParameterReq, JobRunReq, JobScheduleReq} from "lyo-job-api-client";
import type {LyoParameterEditRow} from "lyo-web-components";

const STAT_FIELDS = [
    ["unknown", "unknownCount"],
    ["success", "successCount"],
    ["successWithWarnings", "successWithWarningsCount"],
    ["partialSuccess", "partialSuccessCount"],
    ["failure", "failureCount"],
    ["cancelled", "cancelledCount"],
    ["skipped", "skippedCount"],
    ["timeout", "timeoutCount"],
] as const;

export type StatisticSeries = {name: string; count: number};

function readCount(row: Record<string, unknown>, field: string): number {
    const camel = field.charAt(0).toLowerCase() + field.slice(1);
    const pascal = field.charAt(0).toUpperCase() + field.slice(1);
    const raw = row[field] ?? row[camel] ?? row[pascal];
    const n = typeof raw === "number" ? raw : Number(raw ?? 0);
    return Number.isFinite(n) ? n : 0;
}

/** Groups statistic rows into one series per result state. */
export function statisticSeries(rows: readonly Record<string, unknown>[]): StatisticSeries[] {
    const totals = new Map<string, number>();
    for (const row of rows) {
        for (const [name, field] of STAT_FIELDS) {
            const count = readCount(row, field);
            if (count === 0) continue;
            totals.set(name, (totals.get(name) ?? 0) + count);
        }
    }
    return [...totals.entries()].map(([name, count]) => ({name, count}));
}

export type BlackoutKind = "weekday" | "range" | "holiday";

export function blackoutKind(window: {
    holidaySlug?: string | null;
    startDateUtc?: string | null;
    endDateUtc?: string | null;
}): BlackoutKind {
    if (window.holidaySlug?.trim()) return "holiday";
    if (window.startDateUtc || window.endDateUtc) return "range";
    return "weekday";
}

export function linkBlackout(schedule: JobScheduleReq, calendarId: string): JobScheduleReq {
    return {...schedule, jobBlackoutCalendarId: calendarId};
}

export function unlinkBlackout(schedule: JobScheduleReq): JobScheduleReq {
    return {...schedule, jobBlackoutCalendarId: null};
}

export type RunParameterInput = {
    key: string;
    value: string | null;
    required: boolean;
    type?: string;
};

export function runCreateError(definitionId: string | null | undefined, parameters: readonly RunParameterInput[]): string | null {
    if (!definitionId) return "Pick a definition first.";
    const missing = parameters.find((item) => item.required && (!item.value?.trim() || item.value === "null"));
    return missing ? `${missing.key} is required.` : null;
}

export function runCreateRequest(
    definitionId: string,
    createdBy: string,
    parameters: readonly RunParameterInput[],
    jobScheduleId?: string | null
): JobRunReq {
    const jobRunParameters: JobRunParameterReq[] = parameters
        .filter((item) => item.value != null && item.value.trim() !== "" && item.value !== "null")
        .map((item) => ({key: item.key, value: item.value, type: item.type ?? "System.String"}));
    return {
        jobDefinitionId: definitionId,
        createdBy,
        jobScheduleId: jobScheduleId ?? null,
        jobRunParameters,
    };
}

export function parameterRows(
    params: readonly {
        id?: string | null;
        key?: string | null;
        type?: string | null;
        value?: string | null;
        required?: boolean;
        description?: string | null;
        enabled?: boolean;
        options?: string | null;
        allowedValues?: string | null;
        order?: number | null;
        defaultKind?: string | null;
        defaultTemplate?: string | null;
        encryptedValue?: string | null;
    }[]
): LyoParameterEditRow[] {
    return params.map((item, index) => ({
        id: item.id ?? null,
        key: item.key ?? "",
        type: item.type ?? "System.String",
        value: item.value ?? null,
        description: item.description ?? null,
        required: Boolean(item.required),
        enabled: item.enabled ?? true,
        isNew: !item.id,
        isEncrypted: Boolean(item.encryptedValue),
        encryptedValue: item.encryptedValue ?? null,
        allowedValues: item.allowedValues ?? null,
        options: item.options ?? null,
        order: item.order ?? index,
        defaultKind: item.defaultKind === "Expression" ? "Expression" : "Literal",
        defaultTemplate: item.defaultTemplate ?? null,
    }));
}
