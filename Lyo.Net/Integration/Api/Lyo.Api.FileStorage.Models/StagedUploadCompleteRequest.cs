using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>POST body for <c>stage/{stageId}/complete</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record StagedUploadCompleteRequest
{
    /// <summary>When set, complete fails if observed object size differs.</summary>
    public long? ExpectedByteLength { get; init; }

    /// <summary>Overrides the original filename stored on the stage row.</summary>
    public string? OriginalFileName { get; init; }

    /// <inheritdoc />
    public override string ToString()
        => $"StagedUploadCompleteRequest: OriginalFileName={OriginalFileName ?? "(none)"}, ExpectedByteLength={ExpectedByteLength?.ToString() ?? "(none)"}";
}
