import {projectedField as readProjectedField, projectedId as readProjectedId} from "lyo-web-components";

export type ReportStatusColor = "success" | "error" | "warning" | "info" | "secondary" | "inherit";

export const projectedField = readProjectedField;
export const projectedId = readProjectedId;

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
