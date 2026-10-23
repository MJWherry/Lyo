import type { JobParameterRes } from "lyo-job-api-client";
import { chipFromBool, chipOf, entriesFrom, type LyoChipSpec, type LyoParameterEntry } from "lyo-web-components";
import { jobResultColor, jobStateColor, workerStateColor, type JobStatusColor } from "./jobColors.js";

type ParameterSource = {
    jobParameters?: readonly JobParameterRes[] | null;
    JobParameters?: readonly JobParameterRes[] | null;
};

/** Run-dialog entries for a definition. An omitted include yields an empty list. */
export function runParameterEntries(definition: ParameterSource | null | undefined): LyoParameterEntry[] {
    const params = definition?.jobParameters ?? definition?.JobParameters;
    if (!params) return [];
    return entriesFrom(
        params.map((item) => ({
            key: item.key,
            type: item.type ?? "System.String",
            value: item.value,
            required: item.required,
            description: item.description,
            options: item.options,
            allowedValues: item.allowedValues,
            defaultKind: item.defaultKind === "Expression" ? "Expression" : "Literal",
            defaultTemplate: item.defaultTemplate,
            order: item.order ?? 0,
        })),
        (item) => item.order ?? 0
    );
}

export function enabledChip(value: boolean | null | undefined): LyoChipSpec {
    return chipFromBool(value, "Enabled", "Disabled", "success", "default");
}

export function activeChip(value: boolean | null | undefined): LyoChipSpec {
    return chipFromBool(value, "Active", "Off", "success", "default");
}

function statusColor(color: JobStatusColor): LyoChipSpec["color"] {
    return color === "inherit" || color === "secondary" ? "default" : color;
}

export function jobStateChip(state: string | null | undefined): LyoChipSpec {
    return chipOf(state, { color: statusColor(jobStateColor(state)) });
}

export function jobResultChip(result: string | null | undefined): LyoChipSpec {
    return chipOf(result, { color: statusColor(jobResultColor(result)) });
}

export function workerStateChip(state: string | null | undefined): LyoChipSpec {
    return chipOf(state, { color: statusColor(workerStateColor(state)) });
}

type LatestRuns = {
    lastRun?: object | null;
    lastSuccessfulRun?: object | null;
    lastFailedRun?: object | null;
};

/** Sample bag for a formatter-typed job parameter: definition, parameter values, and run paths. */
export function jobFormatterContext(
    definition: object | null | undefined,
    parameters: readonly {key: string; value: string | null}[] = [],
    latest?: LatestRuns | null
): Record<string, unknown> {
    const values: Record<string, string | null> = {};
    for (const parameter of parameters) values[parameter.key] = parameter.value;
    const sample = {State: "Queued", Result: null, Parameters: values};
    const lastRun = latest?.lastRun ?? sample;
    return {
        Definition: definition ?? null,
        Parameters: values,
        Schedule: null,
        LastRun: lastRun,
        LastSuccessfulRun: latest?.lastSuccessfulRun ?? null,
        LastFailedRun: latest?.lastFailedRun ?? null,
        jobrun: {...lastRun, Parameters: values},
    };
}
