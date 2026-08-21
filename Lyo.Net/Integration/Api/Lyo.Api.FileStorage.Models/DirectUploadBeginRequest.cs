using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>POST body for <c>direct-upload/begin</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DirectUploadBeginRequest
{
    /// <summary>Original filename hint for metadata.</summary>
    public string? OriginalFileName { get; init; }

    /// <summary>Optional path prefix for shard layout.</summary>
    public string? PathPrefix { get; init; }

    /// <summary>Declared maximum size hint for policy validation.</summary>
    public required long DeclaredMaxSizeBytes { get; init; }

    /// <summary>MIME type for metadata and scans.</summary>
    public string? ContentType { get; init; }

    /// <summary>Optional tenant identifier.</summary>
    public string? TenantId { get; init; }

    /// <summary>Presigned PUT URL lifetime in hours. Omit for the API default.</summary>
    public double? UrlExpirationHours { get; init; }

    /// <inheritdoc />
    public override string ToString()
        => $"DirectUploadBeginRequest: OriginalFileName={OriginalFileName ?? "(none)"}, PathPrefix={PathPrefix ?? "(none)"}, DeclaredMaxSizeBytes={DeclaredMaxSizeBytes}";
}
