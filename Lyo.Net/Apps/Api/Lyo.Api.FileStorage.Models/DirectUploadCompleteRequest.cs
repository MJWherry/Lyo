using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>JSON body posted to <c>direct-upload/{fileId}/complete</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DirectUploadCompleteRequest
{
    /// <summary>If set and it does not match the observed size, finalize fails.</summary>
    public long? ExpectedByteLength { get; init; }

    /// <summary>Replaces the original file name captured at begin.</summary>
    public string? OriginalFileName { get; init; }

    /// <inheritdoc />
    public override string ToString()
        => $"DirectUploadCompleteRequest: OriginalFileName={OriginalFileName ?? "(none)"}, ExpectedByteLength={ExpectedByteLength?.ToString() ?? "(none)"}";
}
