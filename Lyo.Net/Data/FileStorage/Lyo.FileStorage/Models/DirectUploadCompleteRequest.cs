using System.Diagnostics;

namespace Lyo.FileStorage.Models;

/// <summary>Optional checks applied when a direct upload is finalized.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DirectUploadCompleteRequest
{
    /// <summary>If set and it does not match the HEAD size, finalize fails immediately.</summary>
    public long? ExpectedByteLength { get; init; }

    /// <summary>Replaces the original filename captured at begin.</summary>
    public string? OriginalFileName { get; init; }

    /// <inheritdoc />
    public override string ToString()
        => $"DirectUploadCompleteRequest: OriginalFileName={OriginalFileName ?? "(none)"}, ExpectedByteLength={ExpectedByteLength?.ToString() ?? "(none)"}";
}
