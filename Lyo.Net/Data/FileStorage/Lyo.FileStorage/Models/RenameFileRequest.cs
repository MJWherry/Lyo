namespace Lyo.FileStorage.Models;

/// <summary>Parameters for updating the display name of an existing stored file (metadata only; backing bytes unchanged).</summary>
public sealed record RenameFileRequest
{
    /// <summary>New original/display file name. Must be non-whitespace.</summary>
    public required string OriginalFileName { get; init; }
}
