using System.Diagnostics;
using Lyo.FileMetadataStore.Models;

namespace Lyo.FileStorage.Models;

/// <summary>Size and checksum a backend reports for an uploaded object without reading it. See <see cref="Abstractions.IDirectUploadVerification" />.</summary>
/// <param name="Length">Stored object length in bytes.</param>
/// <param name="Hash">Digest of the stored bytes.</param>
/// <param name="Algorithm">Algorithm that produced <paramref name="Hash" />. Recorded on the finalized row, so it may differ from the service's configured default.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DirectUploadObjectAttributes(long Length, byte[] Hash, HashAlgorithm Algorithm)
{
    /// <summary>Length, algorithm, and digest length. Digest bytes are omitted.</summary>
    public override string ToString() => $"{Algorithm} Length={Length} HashBytes={Hash.Length}";
}