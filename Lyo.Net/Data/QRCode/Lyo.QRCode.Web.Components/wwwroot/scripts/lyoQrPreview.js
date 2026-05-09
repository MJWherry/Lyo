/**
 * Blob URLs for SVG previews: same-origin navigation to data: SVG is often blocked;
 * opening blob: URLs in a new tab is widely supported.
 */
export function createBlobUrlFromBase64(base64, mimeType) {
    const binary = atob(base64);
    const len = binary.length;
    const bytes = new Uint8Array(len);
    for (let i = 0; i < len; i++)
        bytes[i] = binary.charCodeAt(i);

    const type = mimeType && mimeType.length > 0 ? mimeType : "application/octet-stream";
    return URL.createObjectURL(new Blob([bytes], { type }));
}

export function revokeBlobUrl(url) {
    if (url && typeof url === "string" && url.startsWith("blob:"))
        URL.revokeObjectURL(url);
}
