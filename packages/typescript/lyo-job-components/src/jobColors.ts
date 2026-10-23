import {projectedField as readProjectedField, projectedId as readProjectedId} from "lyo-web-components";

export type JobStatusColor = "success" | "error" | "warning" | "info" | "secondary" | "inherit";

export const projectedField = readProjectedField;
export const projectedId = readProjectedId;

export function jobStateColor(state: string | null | undefined): JobStatusColor {
    switch ((state ?? "").toLowerCase()) {
        case "queued":
            return "info";
        case "running":
            return "warning";
        case "finished":
            return "success";
        case "cancelled":
        case "cancelling":
            return "secondary";
        default:
            return "inherit";
    }
}

export function jobResultColor(result: string | null | undefined): JobStatusColor {
    switch ((result ?? "").toLowerCase()) {
        case "success":
            return "success";
        case "successwithwarnings":
        case "partialsuccess":
            return "warning";
        case "failure":
        case "timeout":
            return "error";
        case "cancelled":
        case "skipped":
            return "secondary";
        default:
            return "inherit";
    }
}

export function workerStateColor(state: string | null | undefined): JobStatusColor {
    switch ((state ?? "").toLowerCase()) {
        case "running":
            return "success";
        case "draining":
            return "warning";
        case "stopped":
            return "secondary";
        default:
            return "inherit";
    }
}

export function canCancelRun(row: Record<string, unknown>): boolean {
    const state = String(projectedField(row, "State") ?? "").toLowerCase();
    return state === "queued" || state === "running";
}
