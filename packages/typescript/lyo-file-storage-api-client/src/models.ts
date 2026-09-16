/** CamelCase mirror of `FileStoreResult`. `byte[]` fields are base64 strings. */
export interface FileStoreResult {
    id: string;
    originalFileName?: string | null;
    originalFileSize?: number;
    originalFileHash?: string | null;
    sourceFileName?: string | null;
    sourceFileSize?: number;
    sourceFileHash?: string | null;
    isCompressed?: boolean;
    compressionAlgorithm?: string | null;
    compressedFileSize?: number | null;
    compressedFileHash?: string | null;
    isEncrypted?: boolean;
    dataEncryptionKeyAlgorithm?: string | null;
    keyEncryptionKeyAlgorithm?: string | null;
    encryptedFileSize?: number | null;
    encryptedFileHash?: string | null;
    encryptedDataEncryptionKey?: string | null;
    dataEncryptionKeyId?: string | null;
    dataEncryptionKeyVersion?: string | null;
    keyEncryptionKeySalt?: string | null;
    timestamp?: string;
    pathPrefix?: string | null;
    hashAlgorithm?: string | null;
    contentType?: string | null;
    charset?: string | null;
    tenantId?: string | null;
    availability?: string | null;
    deletedAt?: string | null;
    ownerId?: string | null;
    metadata?: unknown;
}

/** Plaintext hash returned by `GET files/{id}/integrity`. `originalFileHash` is base64. */
export interface FileContentIntegrity {
    hashAlgorithm: string;
    originalFileHash: string;
    originalFileSize: number;
}

export interface SaveFileRequest {
    data: string;
    originalFileName?: string | null;
    compress?: boolean;
    encrypt?: boolean;
    keyId?: string | null;
    pathPrefix?: string | null;
    chunkSize?: number | null;
    contentType?: string | null;
    charset?: string | null;
    tenantId?: string | null;
    metadata?: unknown;
}

export interface SaveStreamRequest {
    file: Blob;
    fileName?: string;
    originalFileName?: string | null;
    compress?: boolean;
    encrypt?: boolean;
    keyId?: string | null;
    pathPrefix?: string | null;
    chunkSize?: number | null;
    contentType?: string | null;
    charset?: string | null;
    tenantId?: string | null;
}

export interface DirectUploadBeginRequest {
    originalFileName?: string | null;
    pathPrefix?: string | null;
    declaredMaxSizeBytes: number;
    contentType?: string | null;
    charset?: string | null;
    tenantId?: string | null;
    metadata?: unknown;
    urlExpirationHours?: number | null;
    expectedSha256?: string | null;
    expectedContentMd5?: string | null;
}

export interface PresignedUploadRequest extends DirectUploadBeginRequest {
    compress?: boolean;
    encrypt?: boolean;
}

export interface DirectUploadBeginResult {
    fileId: string;
    presignedPutUrl: string;
    urlExpiresUtc: string;
    storageLocation: string;
    requiredPutHeaders?: Record<string, string> | null;
}

export interface DirectUploadCompleteRequest {
    expectedByteLength?: number | null;
    originalFileName?: string | null;
}

export interface PresignedReadResponse {
    url: string;
}

export interface FileStorageHealthResponse {
    isHealthy: boolean;
    message?: string | null;
}

export interface FileStorageCapabilitiesResponse {
    presignedRead: boolean;
    directUpload: boolean;
    multipart: boolean;
    presignedMultipartParts: boolean;
    copy: boolean;
    move: boolean;
    physical: boolean;
    encryption: boolean;
    archive: boolean;
    accessLinks: boolean;
}

export interface FileStorageFolderEntry {
    name: string;
    isDirectory: boolean;
    pathPrefix?: string | null;
    fileId?: string | null;
    presence?: string | number;
    originalFileName?: string | null;
    originalFileSize?: number;
    physicalKey?: string | null;
}

export interface FileStorageFolderListResponse {
    entries: FileStorageFolderEntry[];
    truncated: boolean;
    backendKind?: string | number;
}

export interface CopyFileRequest {
    sourceFileId: string;
    pathPrefix?: string | null;
}

export interface MoveFileRequest {
    fileId: string;
    pathPrefix?: string | null;
}

export interface RenameFileRequest {
    fileId: string;
    originalFileName: string;
}

export interface CreateDownloadAccessLinkRequest {
    notBeforeUtc?: string | null;
    expiresAtUtc?: string | null;
    windowStartUtc?: string | null;
    windowEndUtc?: string | null;
    maxDownloads?: number | null;
    tenantId?: string | null;
}

export interface DownloadAccessLinkResponse {
    linkId: string;
    token: string;
    downloadUrl: string;
    presignedReadUrl: string;
    createdUtc: string;
    expiresAtUtc?: string | null;
}

export interface BeginMultipartRequest {
    partSizeBytes?: number;
    compress?: boolean;
    encrypt?: boolean;
    keyId?: string | null;
    pathPrefix?: string | null;
    contentType?: string | null;
    charset?: string | null;
    originalFileName?: string | null;
    tenantId?: string | null;
    declaredContentLength?: number | null;
    sessionTtlHours?: number | null;
    metadata?: unknown;
}

export interface MultipartBeginResponse {
    sessionId: string;
    targetFileId: string;
    partSizeBytes: number;
    expiresUtc: string;
    providerKind: string;
}

export interface CompletedPart {
    partNumber: number;
    eTagOrBlockId: string;
}

export interface CompleteMultipartRequest {
    sessionId: string;
    parts: CompletedPart[];
}

export interface MultipartPartUrlResponse {
    partNumber: number;
    presignedPutUrl?: string | null;
    httpMethod?: string;
}

export interface MigrateDeksRequest {
    sourceKeyId: string;
    sourceKeyVersion?: string | null;
    targetKeyId?: string | null;
    targetKeyVersion?: string | null;
    batchSize?: number;
}

export interface RotateDeksRequest {
    fileIds: string[];
    targetKeyId?: string | null;
    targetKeyVersion?: string | null;
    batchSize?: number;
}

export interface DekMigrationResult {
    totalFilesFound: number;
    successfullyMigrated: number;
    failed: number;
    failedFileIds: string[];
    errors: string[];
    skipped?: number;
}

export interface MovePhysicalObjectRequest {
    physicalKey: string;
    destinationPathPrefix?: string | null;
}

export interface CopyPhysicalObjectRequest {
    physicalKey: string;
    destinationPathPrefix?: string | null;
}

export interface RenamePhysicalObjectRequest {
    physicalKey: string;
    newFileName: string;
}

export interface PhysicalObjectResult {
    physicalKey: string;
}

export interface PresignedUploadResult {
    fileId: string;
    presignedPutUrl: string;
}
