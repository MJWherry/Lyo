namespace Lyo.FileStorage.Models;

/// <summary>Optional assertions when finalizing a direct upload.</summary>
public sealed record DirectUploadCompleteRequest
{
    /// <summary>When set and different from HEAD size, finalize fails fast.</summary>
    public long? ExpectedByteLength { get; init; }

    /// <summary>Overrides original filename captured at begin stage.</summary>
    public string? OriginalFileName { get; init; }
}