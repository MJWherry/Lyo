namespace Lyo.FileStorage.Models;

/// <summary>Input to <see cref="Staged.IStagedFileUploadService.BeginAsync" />.</summary>
public sealed class StagedUploadBeginRequest
{
    /// <summary>Hint for content-type resolution and audit display.</summary>
    public string? OriginalFileName { get; init; }

    /// <summary>Logical folder segment; normalized and traversal-guarded like ordinary saves.</summary>
    public string? PathPrefix { get; init; }

    /// <summary>Declared upper bound on upload size; enforced against <see cref="FileStorageServiceBaseOptions.MaxUploadSizeBytes" />.</summary>
    public required long DeclaredMaxSizeBytes { get; init; }

    /// <summary>Optional MIME type signed into presigned PUT headers when supported by the backend.</summary>
    public string? ContentType { get; init; }

    /// <summary>Overrides operation-context tenant when set (see <see cref="OperationContext.IFileOperationContextAccessor" />).</summary>
    public string? TenantId { get; init; }

    /// <summary>Presigned URL lifetime; default one hour, max seven days.</summary>
    public TimeSpan? UrlExpiration { get; init; }

    /// <summary>Stage row TTL stored in <see cref="Staged.StagedFileUploadRecord.ExpiresUtc" />; default 24 hours.</summary>
    public TimeSpan? SessionTtl { get; init; }
}
