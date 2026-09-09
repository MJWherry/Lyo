using System.Diagnostics;

namespace Lyo.FileStorage.Models;

/// <summary>Inputs for changing the display name of a stored file. Metadata only. Backing bytes stay as they are.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record RenameFileRequest
{
    /// <summary>New original or display file name. Must not be whitespace.</summary>
    public required string OriginalFileName { get; init; }

    /// <inheritdoc />
    public override string ToString() => $"RenameFileRequest: OriginalFileName={OriginalFileName}";
}
