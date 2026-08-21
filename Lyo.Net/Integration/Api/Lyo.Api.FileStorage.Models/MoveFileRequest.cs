using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>POST body for <c>files/move</c>. The API maps <see cref="PathPrefix" /> onto the storage-engine move request.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record MoveFileRequest(Guid FileId, string? PathPrefix = null)
{
    /// <inheritdoc />
    public override string ToString() => $"MoveFileRequest: FileId={FileId}, PathPrefix={PathPrefix ?? "(none)"}";
}
