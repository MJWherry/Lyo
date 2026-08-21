using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>POST body for <c>direct-upload/{fileId}/complete</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DirectUploadCompleteRequest
{
    /// <summary>When set and different from observed size, finalize fails.</summary>
    public long? ExpectedByteLength { get; init; }

    /// <summary>Overrides original filename captured at begin.</summary>
    public string? OriginalFileName { get; init; }

    /// <inheritdoc />
    public override string ToString()
        => $"DirectUploadCompleteRequest: OriginalFileName={OriginalFileName ?? "(none)"}, ExpectedByteLength={ExpectedByteLength?.ToString() ?? "(none)"}";
}
