export type JobStatusColor = "success" | "error" | "warning" | "info" | "secondary" | "inherit";

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

export function projectedField(row: Record<string, unknown>, field: string): unknown {
    if (field in row) return row[field];
    const camel = field.charAt(0).toLowerCase() + field.slice(1);
    if (camel in row) return row[camel];
    const nested = field.split(".");
    let cur: unknown = row;
    for (const part of nested) {
        if (!cur || typeof cur !== "object") return undefined;
        const rec = cur as Record<string, unknown>;
        cur = rec[part] ?? rec[part.charAt(0).toLowerCase() + part.slice(1)];
    }
    return cur;
}

export function projectedId(row: Record<string, unknown>): string {
    return String(projectedField(row, "Id") ?? "");
}

export function canCancelRun(row: Record<string, unknown>): boolean {
    const state = String(projectedField(row, "State") ?? "").toLowerCase();
    return state === "queued" || state === "running";
}
