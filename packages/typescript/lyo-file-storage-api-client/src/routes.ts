/** REST paths from `Lyo.Api.FileStorage` (no leading slash). */
export const FileStorageRoutes = {
    route: "FileStorage",
    fileMetadata: "FileStorage/FileMetadata",
    health: "FileStorage/health",
    capabilities: "FileStorage/capabilities",
    keyIds: "FileStorage/key-ids",
    folder: "FileStorage/files/folder",
    storageKeys: "FileStorage/diagnostics/storage-keys",
    save: "FileStorage/files/save",
    saveStream: "FileStorage/files/save-stream",
    copy: "FileStorage/files/copy",
    move: "FileStorage/files/move",
    rename: "FileStorage/files/rename",
    archive: "FileStorage/files/archive",
    directUploadBegin: "FileStorage/direct-upload/begin",
    multipartBegin: "FileStorage/multipart/begin",
    multipartComplete: "FileStorage/multipart/complete",
    migrateDeks: "FileStorage/files/migrate-deks",
    rotateDeks: "FileStorage/files/rotate-deks",
    physicalDownload: "FileStorage/files/physical/download",
    physical: "FileStorage/files/physical",
    physicalMove: "FileStorage/files/physical/move",
    physicalCopy: "FileStorage/files/physical/copy",
    physicalRename: "FileStorage/files/physical/rename",
} as const;

export function fileMetadataPath(fileId: string): string {
    return `${FileStorageRoutes.route}/files/${fileId}/metadata`;
}

export function fileDownloadPath(fileId: string): string {
    return `${FileStorageRoutes.route}/files/${fileId}/download`;
}

export function fileIntegrityPath(fileId: string): string {
    return `${FileStorageRoutes.route}/files/${fileId}/integrity`;
}

export function fileDeletePath(fileId: string): string {
    return `${FileStorageRoutes.route}/files/${fileId}`;
}

export function fileAccessLinkPath(fileId: string): string {
    return `${FileStorageRoutes.route}/files/${fileId}/access-links`;
}

export function filePresignedReadPath(fileId: string): string {
    return `${FileStorageRoutes.route}/files/${fileId}/presigned-read`;
}

export function directUploadCompletePath(fileId: string): string {
    return `${FileStorageRoutes.route}/direct-upload/${fileId}/complete`;
}

export function directUploadPutPath(fileId: string): string {
    return `${FileStorageRoutes.route}/direct-upload/${fileId}/put`;
}

export function multipartPartUrlPath(sessionId: string): string {
    return `${FileStorageRoutes.route}/multipart/${sessionId}/part-url`;
}

export function multipartAbortPath(sessionId: string): string {
    return `${FileStorageRoutes.route}/multipart/${sessionId}/abort`;
}

export function joinRoutePrefix(routePrefix: string | undefined, relativePath: string): string {
    const queryAt = relativePath.indexOf("?");
    const pathOnly = queryAt >= 0 ? relativePath.slice(0, queryAt) : relativePath;
    const query = queryAt >= 0 ? relativePath.slice(queryAt) : "";
    const relative = pathOnly.replace(/^\/+/, "");
    const prefix = (routePrefix ?? "").replace(/\/+$/, "").replace(/^\/+/, "");
    return `${prefix ? `/${prefix}/${relative}` : `/${relative}`}${query}`;
}
