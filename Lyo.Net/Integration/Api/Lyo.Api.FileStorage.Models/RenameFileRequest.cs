using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>POST body for <c>files/rename</c>. The API maps <see cref="OriginalFileName" /> onto the storage-engine rename request.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record RenameFileRequest(Guid FileId, string OriginalFileName)
{
    /// <inheritdoc />
    public override string ToString() => $"RenameFileRequest: FileId={FileId}, OriginalFileName={OriginalFileName}";
}
