using System.Diagnostics;

namespace Lyo.FileStorage;

/// <summary>
/// Operations this storage service can perform. Multipart, encryption, archive, and download-access links are separate host services and are not included.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileStorageCapabilities(bool PresignedRead, bool DirectUpload, bool Copy, bool Move, bool Physical)
{
    /// <inheritdoc />
    public override string ToString()
        => $"FileStorageCapabilities: PresignedRead={PresignedRead} DirectUpload={DirectUpload} Copy={Copy} Move={Move} Physical={Physical}";
}
