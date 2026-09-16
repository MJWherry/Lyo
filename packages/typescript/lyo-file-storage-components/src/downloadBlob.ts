export function triggerBlobDownload(blob: Blob, fileName: string | null): void {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName ?? "file";
    anchor.click();
    URL.revokeObjectURL(url);
}
