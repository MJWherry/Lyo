using System.Diagnostics;

namespace Lyo.FileStorage.Multipart;

[DebuggerDisplay("{ToString(),nq}")]
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
    string? Charset,
    MultipartSessionStatus Status,
    MultipartUploadProviderKind ProviderKind,
    string ProviderStateJson,
    long? DeclaredContentLength,
    int PartSizeBytes)
{
    /// <summary>Opaque caller JSON bag copied onto file metadata at complete. Null when omitted.</summary>
    public string? MetadataJson { get; init; }

    /// <inheritdoc />
    public override string ToString()
        => $"MultipartUploadSessionRecord: SessionId={SessionId}, TargetFileId={TargetFileId}, Status={Status}, ProviderKind={ProviderKind}";
}