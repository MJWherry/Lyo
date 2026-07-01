namespace Lyo.FileStorage.Models;

/// <summary>Optional hints for <see cref="Staged.IStagedFileUploadService.CompleteAsync" />.</summary>
public sealed class StagedUploadCompleteRequest
{
    /// <summary>When set, complete fails if observed object size differs.</summary>
    public long? ExpectedByteLength { get; init; }

    /// <summary>Overrides the original filename stored on the stage row after successful complete.</summary>
    public string? OriginalFileName { get; init; }
}