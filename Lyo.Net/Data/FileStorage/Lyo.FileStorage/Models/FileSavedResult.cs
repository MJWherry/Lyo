using System.Diagnostics;

namespace Lyo.FileStorage.Models;

/// <summary>
/// Payload raised on <c>FileSaved</c>. <see cref="File" /> is a redacted snapshot. Wrapped DEK and KEK salt are omitted. Subscribers that need those fields should call
/// <c>GetMetadataAsync</c> through an authorized service.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileSavedResult(Guid FileId, FileStoreSnapshot File, long OriginalSize, long FinalSize, bool WasCompressed, bool WasEncrypted)
    : FileStorageResult(FileId, DateTime.UtcNow)
{
    /// <inheritdoc />
    public override string ToString()
        => $"FileSavedResult: FileId={FileId}, OriginalSize={OriginalSize}, FinalSize={FinalSize}, compressed={WasCompressed}, encrypted={WasEncrypted}";
}
