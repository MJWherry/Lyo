export type ReportStatusColor = "success" | "error" | "warning" | "info" | "secondary" | "inherit";

export function generationStatusColor(status: string | null | undefined): ReportStatusColor {
    switch ((status ?? "").toLowerCase()) {
        case "succeeded":
            return "success";
        case "failed":
            return "error";
        case "running":
            return "info";
        case "pending":
            return "warning";
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

export function canDownloadGeneration(row: Record<string, unknown>): boolean {
    const status = String(projectedField(row, "Status") ?? "").toLowerCase();
    return status === "succeeded" && Boolean(projectedField(row, "OutputFileId") || projectedField(row, "OriginalFileName"));
}

export function triggerBlobDownload(blob: Blob, fileName: string | null): void {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName ?? "report";
    anchor.click();
    URL.revokeObjectURL(url);
}
