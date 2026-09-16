import type {ApiClient, ApiResponse, AsyncApiClient, ExportDownload} from "lyo-api-client";
import {fileNameFromDisposition} from "lyo-api-client";
import type {ProjectionQueryReq, QueryConcreteReq} from "lyo-query";
import type {ProjectedQueryRes, QueryRes} from "lyo-api-client";
import type {
    BeginMultipartRequest,
    CompleteMultipartRequest,
    CopyFileRequest,
    CopyPhysicalObjectRequest,
    CreateDownloadAccessLinkRequest,
    DekMigrationResult,
    DirectUploadBeginRequest,
    DirectUploadBeginResult,
    DirectUploadCompleteRequest,
    DownloadAccessLinkResponse,
    FileStorageCapabilitiesResponse,
    FileStorageFolderListResponse,
    FileContentIntegrity,
    FileStorageHealthResponse,
    FileStoreResult,
    MigrateDeksRequest,
    MoveFileRequest,
    MovePhysicalObjectRequest,
    MultipartBeginResponse,
    MultipartPartUrlResponse,
    PhysicalObjectResult,
    PresignedReadResponse,
    PresignedUploadRequest,
    PresignedUploadResult,
    RenameFileRequest,
    RenamePhysicalObjectRequest,
    RotateDeksRequest,
    SaveFileRequest,
    SaveStreamRequest,
} from "./models.js";
import {assertProviderDownload, assertProviderUpload} from "./providerGuard.js";
import {
    FileStorageRoutes,
    directUploadCompletePath,
    directUploadPutPath,
    fileAccessLinkPath,
    fileDeletePath,
    fileDownloadPath,
    fileIntegrityPath,
    fileMetadataPath,
    filePresignedReadPath,
    joinRoutePrefix,
    multipartAbortPath,
    multipartPartUrlPath,
} from "./routes.js";

export interface FileStorageClientOptions {
    routePrefix?: string;
}

/** Plaintext download. `contentDigest` is the `Content-Digest` header, or null when the response omitted it. */
export interface StoredFileDownload extends ExportDownload {
    contentDigest: string | null;
}

export interface PresignedReadQuery {
    expiresHours?: number;
    pathPrefix?: string;
    contentDisposition?: string;
    contentType?: string;
}

function headerValue(headers: Record<string, string> | undefined, name: string): string | null {
    if (!headers)
        return null;
    const match = Object.keys(headers).find((key) => key.toLowerCase() === name);
    return match ? headers[match] ?? null : null;
}

function toStoredDownload(response: ApiResponse<unknown>): StoredFileDownload {
    return {...toDownload(response), contentDigest: headerValue(response.headers, "content-digest")};
}

function toDownload(response: ApiResponse<unknown>): ExportDownload {
    const blob =
        response.blob ??
        new Blob([response.rawBody ?? ""], {
            type: response.headers?.["content-type"] ?? "application/octet-stream",
        });
    return {
        blob,
        fileName: fileNameFromDisposition(response.headers?.["content-disposition"]),
        contentType: response.headers?.["content-type"] ?? blob.type,
    };
}

function streamForm(args: SaveStreamRequest): FormData {
    const form = new FormData();
    form.append("file", args.file, args.fileName ?? args.originalFileName ?? "file.bin");
    return form;
}

function streamQuery(args: SaveStreamRequest): Record<string, string | number | boolean | null | undefined> {
    return {
        originalFileName: args.originalFileName ?? args.fileName,
        compress: args.compress ?? false,
        encrypt: args.encrypt ?? false,
        keyId: args.keyId,
        pathPrefix: args.pathPrefix,
        chunkSize: args.chunkSize,
        contentType: args.contentType,
        charset: args.charset,
        tenantId: args.tenantId,
    };
}

function archiveRelative(ids: readonly string[], fileName?: string): string {
    const params = new URLSearchParams();
    for (const id of ids) params.append("id", id);
    if (fileName) params.set("fileName", fileName);
    return `${FileStorageRoutes.archive}?${params.toString()}`;
}

function readQuery(query?: PresignedReadQuery): Record<string, string | number | boolean | null | undefined> | undefined {
    if (!query) return undefined;
    return {
        expiresHours: query.expiresHours,
        pathPrefix: query.pathPrefix,
        contentDisposition: query.contentDisposition,
        contentType: query.contentType,
    };
}

function beginBody(request: PresignedUploadRequest): DirectUploadBeginRequest {
    const {compress: _compress, encrypt: _encrypt, ...body} = request;
    return body;
}

export interface AsyncFileStorageApiClient {
    health(): Promise<ApiResponse<FileStorageHealthResponse>>;
    capabilities(): Promise<ApiResponse<FileStorageCapabilitiesResponse>>;
    listKeyIds(): Promise<ApiResponse<string[]>>;
    listFolder(query?: {pathPrefix?: string; maxKeys?: number}): Promise<ApiResponse<FileStorageFolderListResponse>>;
    listStorageKeys(query?: {prefix?: string; maxKeys?: number}): Promise<ApiResponse<string[]>>;
    saveBytes(request: SaveFileRequest): Promise<ApiResponse<FileStoreResult>>;
    saveStream(request: SaveStreamRequest): Promise<ApiResponse<FileStoreResult>>;
    getMetadata(fileId: string, includeDeleted?: boolean): Promise<ApiResponse<FileStoreResult>>;
    getIntegrity(fileId: string): Promise<ApiResponse<FileContentIntegrity>>;
    download(fileId: string, inline?: boolean): Promise<StoredFileDownload>;
    downloadArchive(ids: readonly string[], fileName?: string): Promise<ExportDownload>;
    copy(request: CopyFileRequest): Promise<ApiResponse<FileStoreResult>>;
    move(request: MoveFileRequest): Promise<ApiResponse<FileStoreResult>>;
    rename(request: RenameFileRequest): Promise<ApiResponse<FileStoreResult>>;
    delete(fileId: string): Promise<ApiResponse<boolean>>;
    createAccessLink(fileId: string, request?: CreateDownloadAccessLinkRequest): Promise<ApiResponse<DownloadAccessLinkResponse>>;
    presignedRead(fileId: string, query?: PresignedReadQuery): Promise<ApiResponse<PresignedReadResponse>>;
    presignedDownloadUrl(metadata: FileStoreResult, query?: PresignedReadQuery): Promise<string>;
    beginDirectUpload(request: DirectUploadBeginRequest): Promise<ApiResponse<DirectUploadBeginResult>>;
    presignedUploadUrl(request: PresignedUploadRequest): Promise<PresignedUploadResult>;
    completeDirectUpload(fileId: string, request?: DirectUploadCompleteRequest): Promise<ApiResponse<FileStoreResult>>;
    putDirectUpload(fileId: string, body: Blob): Promise<ApiResponse<unknown>>;
    beginMultipart(request: BeginMultipartRequest): Promise<ApiResponse<MultipartBeginResponse>>;
    partUrl(sessionId: string, partNumber: number): Promise<ApiResponse<MultipartPartUrlResponse>>;
    completeMultipart(request: CompleteMultipartRequest): Promise<ApiResponse<FileStoreResult>>;
    abortMultipart(sessionId: string): Promise<ApiResponse<unknown>>;
    migrateDeks(request: MigrateDeksRequest): Promise<ApiResponse<DekMigrationResult>>;
    rotateDeks(request: RotateDeksRequest): Promise<ApiResponse<DekMigrationResult>>;
    downloadPhysical(physicalKey: string): Promise<ExportDownload>;
    deletePhysical(physicalKey: string): Promise<ApiResponse<boolean>>;
    movePhysical(request: MovePhysicalObjectRequest): Promise<ApiResponse<PhysicalObjectResult>>;
    copyPhysical(request: CopyPhysicalObjectRequest): Promise<ApiResponse<PhysicalObjectResult>>;
    renamePhysical(request: RenamePhysicalObjectRequest): Promise<ApiResponse<PhysicalObjectResult>>;
    queryConcrete(request: QueryConcreteReq): Promise<ApiResponse<QueryRes<FileStoreResult>>>;
    queryProject<TRow = Record<string, unknown>>(request: ProjectionQueryReq): Promise<ApiResponse<ProjectedQueryRes<TRow>>>;
}

export interface FileStorageApiClient {
    health(): ApiResponse<FileStorageHealthResponse>;
    capabilities(): ApiResponse<FileStorageCapabilitiesResponse>;
    listKeyIds(): ApiResponse<string[]>;
    listFolder(query?: {pathPrefix?: string; maxKeys?: number}): ApiResponse<FileStorageFolderListResponse>;
    listStorageKeys(query?: {prefix?: string; maxKeys?: number}): ApiResponse<string[]>;
    saveBytes(request: SaveFileRequest): ApiResponse<FileStoreResult>;
    saveStream(request: SaveStreamRequest): ApiResponse<FileStoreResult>;
    getMetadata(fileId: string, includeDeleted?: boolean): ApiResponse<FileStoreResult>;
    getIntegrity(fileId: string): ApiResponse<FileContentIntegrity>;
    download(fileId: string, inline?: boolean): StoredFileDownload;
    downloadArchive(ids: readonly string[], fileName?: string): ExportDownload;
    copy(request: CopyFileRequest): ApiResponse<FileStoreResult>;
    move(request: MoveFileRequest): ApiResponse<FileStoreResult>;
    rename(request: RenameFileRequest): ApiResponse<FileStoreResult>;
    delete(fileId: string): ApiResponse<boolean>;
    createAccessLink(fileId: string, request?: CreateDownloadAccessLinkRequest): ApiResponse<DownloadAccessLinkResponse>;
    presignedRead(fileId: string, query?: PresignedReadQuery): ApiResponse<PresignedReadResponse>;
    presignedDownloadUrl(metadata: FileStoreResult, query?: PresignedReadQuery): string;
    beginDirectUpload(request: DirectUploadBeginRequest): ApiResponse<DirectUploadBeginResult>;
    presignedUploadUrl(request: PresignedUploadRequest): PresignedUploadResult;
    completeDirectUpload(fileId: string, request?: DirectUploadCompleteRequest): ApiResponse<FileStoreResult>;
    putDirectUpload(fileId: string, body: Blob): ApiResponse<unknown>;
    beginMultipart(request: BeginMultipartRequest): ApiResponse<MultipartBeginResponse>;
    partUrl(sessionId: string, partNumber: number): ApiResponse<MultipartPartUrlResponse>;
    completeMultipart(request: CompleteMultipartRequest): ApiResponse<FileStoreResult>;
    abortMultipart(sessionId: string): ApiResponse<unknown>;
    migrateDeks(request: MigrateDeksRequest): ApiResponse<DekMigrationResult>;
    rotateDeks(request: RotateDeksRequest): ApiResponse<DekMigrationResult>;
    downloadPhysical(physicalKey: string): ExportDownload;
    deletePhysical(physicalKey: string): ApiResponse<boolean>;
    movePhysical(request: MovePhysicalObjectRequest): ApiResponse<PhysicalObjectResult>;
    copyPhysical(request: CopyPhysicalObjectRequest): ApiResponse<PhysicalObjectResult>;
    renamePhysical(request: RenamePhysicalObjectRequest): ApiResponse<PhysicalObjectResult>;
    queryConcrete(request: QueryConcreteReq): ApiResponse<QueryRes<FileStoreResult>>;
    queryProject<TRow = Record<string, unknown>>(request: ProjectionQueryReq): ApiResponse<ProjectedQueryRes<TRow>>;
}

export function createAsyncFileStorageApiClient(api: AsyncApiClient, options?: FileStorageClientOptions): AsyncFileStorageApiClient {
    const p = (relative: string) => joinRoutePrefix(options?.routePrefix, relative);
    const metadataRoute = p(FileStorageRoutes.fileMetadata);
    return {
        health: () => api.request({method: "GET", path: p(FileStorageRoutes.health)}),
        capabilities: () => api.request({method: "GET", path: p(FileStorageRoutes.capabilities)}),
        listKeyIds: () => api.request({method: "GET", path: p(FileStorageRoutes.keyIds)}),
        listFolder: (query) => api.request({method: "GET", path: p(FileStorageRoutes.folder), query}),
        listStorageKeys: (query) => api.request({method: "GET", path: p(FileStorageRoutes.storageKeys), query}),
        saveBytes: (request) => api.request({method: "POST", path: p(FileStorageRoutes.save), body: request}),
        saveStream: (request) =>
            api.request({method: "POST", path: p(FileStorageRoutes.saveStream), query: streamQuery(request), body: streamForm(request)}),
        getMetadata: (fileId, includeDeleted) =>
            api.request({
                method: "GET",
                path: p(fileMetadataPath(fileId)),
                query: includeDeleted ? {includeDeleted: true} : undefined,
            }),
        getIntegrity: (fileId) => api.request({method: "GET", path: p(fileIntegrityPath(fileId))}),
        async download(fileId, inline) {
            const response = await api.request({
                method: "GET",
                path: p(fileDownloadPath(fileId)),
                query: inline ? {inline: true} : undefined,
                responseType: "blob",
            });
            return toStoredDownload(response);
        },
        async downloadArchive(ids, fileName) {
            const response = await api.request({method: "GET", path: p(archiveRelative(ids, fileName)), responseType: "blob"});
            return toDownload(response);
        },
        copy: (request) => api.request({method: "POST", path: p(FileStorageRoutes.copy), body: request}),
        move: (request) => api.request({method: "POST", path: p(FileStorageRoutes.move), body: request}),
        rename: (request) => api.request({method: "POST", path: p(FileStorageRoutes.rename), body: request}),
        delete: (fileId) => api.request({method: "DELETE", path: p(fileDeletePath(fileId))}),
        createAccessLink: (fileId, request) =>
            api.request({method: "POST", path: p(fileAccessLinkPath(fileId)), body: request ?? {}}),
        presignedRead: (fileId, query) => api.request({method: "GET", path: p(filePresignedReadPath(fileId)), query: readQuery(query)}),
        async presignedDownloadUrl(metadata, query) {
            assertProviderDownload(metadata);
            const response = await api.request<PresignedReadResponse>({method: "GET", path: p(filePresignedReadPath(metadata.id)), query: readQuery(query)});
            return response.data?.url ?? "";
        },
        beginDirectUpload: (request) => api.request({method: "POST", path: p(FileStorageRoutes.directUploadBegin), body: request}),
        async presignedUploadUrl(request) {
            assertProviderUpload(request);
            const response = await api.request<DirectUploadBeginResult>({
                method: "POST",
                path: p(FileStorageRoutes.directUploadBegin),
                body: beginBody(request),
            });
            return {fileId: response.data?.fileId ?? "", presignedPutUrl: response.data?.presignedPutUrl ?? ""};
        },
        completeDirectUpload: (fileId, request) =>
            api.request({method: "POST", path: p(directUploadCompletePath(fileId)), body: request ?? {}}),
        putDirectUpload: (fileId, body) => api.request({method: "PUT", path: p(directUploadPutPath(fileId)), body}),
        beginMultipart: (request) => api.request({method: "POST", path: p(FileStorageRoutes.multipartBegin), body: request}),
        partUrl: (sessionId, partNumber) =>
            api.request({method: "GET", path: p(multipartPartUrlPath(sessionId)), query: {partNumber}}),
        completeMultipart: (request) => api.request({method: "POST", path: p(FileStorageRoutes.multipartComplete), body: request}),
        abortMultipart: (sessionId) => api.request({method: "POST", path: p(multipartAbortPath(sessionId))}),
        migrateDeks: (request) => api.request({method: "POST", path: p(FileStorageRoutes.migrateDeks), body: request}),
        rotateDeks: (request) => api.request({method: "POST", path: p(FileStorageRoutes.rotateDeks), body: request}),
        async downloadPhysical(physicalKey) {
            const response = await api.request({
                method: "GET",
                path: p(FileStorageRoutes.physicalDownload),
                query: {physicalKey},
                responseType: "blob",
            });
            return toDownload(response);
        },
        deletePhysical: (physicalKey) => api.request({method: "DELETE", path: p(FileStorageRoutes.physical), query: {physicalKey}}),
        movePhysical: (request) => api.request({method: "POST", path: p(FileStorageRoutes.physicalMove), body: request}),
        copyPhysical: (request) => api.request({method: "POST", path: p(FileStorageRoutes.physicalCopy), body: request}),
        renamePhysical: (request) => api.request({method: "POST", path: p(FileStorageRoutes.physicalRename), body: request}),
        queryConcrete: (request) => api.queryConcrete(metadataRoute, request),
        queryProject: (request) => api.queryProject(metadataRoute, request),
    };
}

export function createFileStorageApiClient(api: ApiClient, options?: FileStorageClientOptions): FileStorageApiClient {
    const p = (relative: string) => joinRoutePrefix(options?.routePrefix, relative);
    const metadataRoute = p(FileStorageRoutes.fileMetadata);
    return {
        health: () => api.request({method: "GET", path: p(FileStorageRoutes.health)}),
        capabilities: () => api.request({method: "GET", path: p(FileStorageRoutes.capabilities)}),
        listKeyIds: () => api.request({method: "GET", path: p(FileStorageRoutes.keyIds)}),
        listFolder: (query) => api.request({method: "GET", path: p(FileStorageRoutes.folder), query}),
        listStorageKeys: (query) => api.request({method: "GET", path: p(FileStorageRoutes.storageKeys), query}),
        saveBytes: (request) => api.request({method: "POST", path: p(FileStorageRoutes.save), body: request}),
        saveStream: (request) =>
            api.request({method: "POST", path: p(FileStorageRoutes.saveStream), query: streamQuery(request), body: streamForm(request)}),
        getMetadata: (fileId, includeDeleted) =>
            api.request({
                method: "GET",
                path: p(fileMetadataPath(fileId)),
                query: includeDeleted ? {includeDeleted: true} : undefined,
            }),
        getIntegrity: (fileId) => api.request({method: "GET", path: p(fileIntegrityPath(fileId))}),
        download(fileId, inline) {
            return toStoredDownload(
                api.request({
                    method: "GET",
                    path: p(fileDownloadPath(fileId)),
                    query: inline ? {inline: true} : undefined,
                    responseType: "blob",
                })
            );
        },
        downloadArchive(ids, fileName) {
            return toDownload(api.request({method: "GET", path: p(archiveRelative(ids, fileName)), responseType: "blob"}));
        },
        copy: (request) => api.request({method: "POST", path: p(FileStorageRoutes.copy), body: request}),
        move: (request) => api.request({method: "POST", path: p(FileStorageRoutes.move), body: request}),
        rename: (request) => api.request({method: "POST", path: p(FileStorageRoutes.rename), body: request}),
        delete: (fileId) => api.request({method: "DELETE", path: p(fileDeletePath(fileId))}),
        createAccessLink: (fileId, request) => api.request({method: "POST", path: p(fileAccessLinkPath(fileId)), body: request ?? {}}),
        presignedRead: (fileId, query) => api.request({method: "GET", path: p(filePresignedReadPath(fileId)), query: readQuery(query)}),
        presignedDownloadUrl(metadata, query) {
            assertProviderDownload(metadata);
            const response = api.request<PresignedReadResponse>({method: "GET", path: p(filePresignedReadPath(metadata.id)), query: readQuery(query)});
            return response.data?.url ?? "";
        },
        beginDirectUpload: (request) => api.request({method: "POST", path: p(FileStorageRoutes.directUploadBegin), body: request}),
        presignedUploadUrl(request) {
            assertProviderUpload(request);
            const response = api.request<DirectUploadBeginResult>({
                method: "POST",
                path: p(FileStorageRoutes.directUploadBegin),
                body: beginBody(request),
            });
            return {fileId: response.data?.fileId ?? "", presignedPutUrl: response.data?.presignedPutUrl ?? ""};
        },
        completeDirectUpload: (fileId, request) =>
            api.request({method: "POST", path: p(directUploadCompletePath(fileId)), body: request ?? {}}),
        putDirectUpload: (fileId, body) => api.request({method: "PUT", path: p(directUploadPutPath(fileId)), body}),
        beginMultipart: (request) => api.request({method: "POST", path: p(FileStorageRoutes.multipartBegin), body: request}),
        partUrl: (sessionId, partNumber) => api.request({method: "GET", path: p(multipartPartUrlPath(sessionId)), query: {partNumber}}),
        completeMultipart: (request) => api.request({method: "POST", path: p(FileStorageRoutes.multipartComplete), body: request}),
        abortMultipart: (sessionId) => api.request({method: "POST", path: p(multipartAbortPath(sessionId))}),
        migrateDeks: (request) => api.request({method: "POST", path: p(FileStorageRoutes.migrateDeks), body: request}),
        rotateDeks: (request) => api.request({method: "POST", path: p(FileStorageRoutes.rotateDeks), body: request}),
        downloadPhysical(physicalKey) {
            return toDownload(
                api.request({
                    method: "GET",
                    path: p(FileStorageRoutes.physicalDownload),
                    query: {physicalKey},
                    responseType: "blob",
                })
            );
        },
        deletePhysical: (physicalKey) => api.request({method: "DELETE", path: p(FileStorageRoutes.physical), query: {physicalKey}}),
        movePhysical: (request) => api.request({method: "POST", path: p(FileStorageRoutes.physicalMove), body: request}),
        copyPhysical: (request) => api.request({method: "POST", path: p(FileStorageRoutes.physicalCopy), body: request}),
        renamePhysical: (request) => api.request({method: "POST", path: p(FileStorageRoutes.physicalRename), body: request}),
        queryConcrete: (request) =>
            api.request({method: "POST", path: `${metadataRoute}/QueryConcrete`, body: request}),
        queryProject: (request) => api.request({method: "POST", path: `${metadataRoute}/QueryProject`, body: request}),
    };
}
