namespace Lyo.FileStorage.Multipart;

public sealed record MultipartUploadSessionRecord(
    Guid SessionId,
    string? TenantId,
    DateTime CreatedUtc,
    DateTime ExpiresUtc,
    Guid TargetFileId,
    string? PathPrefix,
    bool Compress,
    bool Encrypt,
    string? KeyId,
    string? OriginalFileName,
    string? ContentType,
    MultipartSessionStatus Status,
    MultipartUploadProviderKind ProviderKind,
    string ProviderStateJson,
    long? DeclaredContentLength,
    int PartSizeBytes);