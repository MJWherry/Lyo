using System.Diagnostics;

namespace Lyo.Api.FileStorage.Models;

/// <summary>POST body for <c>files/copy</c>. The API maps <see cref="PathPrefix" /> onto the storage-engine copy request.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record CopyFileRequest(Guid SourceFileId, string? PathPrefix = null)
{
    /// <inheritdoc />
    public override string ToString() => $"CopyFileRequest: SourceFileId={SourceFileId}, PathPrefix={PathPrefix ?? "(none)"}";
}
