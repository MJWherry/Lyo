using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>JSON body posted to <c>files/move</c>. The API copies <see cref="PathPrefix" /> onto the storage-engine move request.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record MoveFileRequest(Guid FileId, string? PathPrefix = null)
{
    /// <inheritdoc />
    public override string ToString() => $"MoveFileRequest: FileId={FileId}, PathPrefix={PathPrefix ?? "(none)"}";
}
