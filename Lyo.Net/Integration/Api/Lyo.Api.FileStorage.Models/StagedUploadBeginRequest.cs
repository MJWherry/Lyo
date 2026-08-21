using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>POST body for <c>stage/begin</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record StagedUploadBeginRequest
{
    /// <summary>Hint for content-type resolution and audit display.</summary>
    public string? OriginalFileName { get; init; }

    /// <summary>Logical folder segment.</summary>
    public string? PathPrefix { get; init; }

    /// <summary>Declared upper bound on upload size.</summary>
    public required long DeclaredMaxSizeBytes { get; init; }

    /// <summary>Optional MIME type signed into presigned PUT headers.</summary>
    public string? ContentType { get; init; }

    /// <summary>Optional tenant identifier.</summary>
    public string? TenantId { get; init; }

    /// <summary>Presigned PUT URL lifetime in hours. Omit for the API default.</summary>
    public double? UrlExpirationHours { get; init; }

    /// <summary>Stage-row TTL in hours. Omit for the API default.</summary>
    public double? SessionTtlHours { get; init; }

    /// <inheritdoc />
    public override string ToString()
        => $"StagedUploadBeginRequest: OriginalFileName={OriginalFileName ?? "(none)"}, PathPrefix={PathPrefix ?? "(none)"}, DeclaredMaxSizeBytes={DeclaredMaxSizeBytes}";
}
