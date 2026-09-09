using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>JSON body posted to <c>files/copy</c>. The API copies <see cref="PathPrefix" /> onto the storage-engine copy request.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record CopyFileRequest(Guid SourceFileId, string? PathPrefix = null)
{
    /// <inheritdoc />
    public override string ToString() => $"CopyFileRequest: SourceFileId={SourceFileId}, PathPrefix={PathPrefix ?? "(none)"}";
}
