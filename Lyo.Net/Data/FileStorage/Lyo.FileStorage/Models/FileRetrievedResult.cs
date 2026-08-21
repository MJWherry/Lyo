using System.Diagnostics;

namespace Lyo.FileStorage.Models;

/// <summary>Event payload for <c>FileRetrieved</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileRetrievedResult(Guid FileId, long FileSize, bool WasCompressed, bool WasEncrypted)
    : FileStorageResult(FileId, DateTime.UtcNow)
{
    /// <inheritdoc />
    public override string ToString()
        => $"FileRetrievedResult: FileId={FileId}, FileSize={FileSize}, compressed={WasCompressed}, encrypted={WasEncrypted}";
}
