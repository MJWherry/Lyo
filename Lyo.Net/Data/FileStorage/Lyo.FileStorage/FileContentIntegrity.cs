using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;

namespace Lyo.FileStorage;

/// <summary>Plaintext hash a downloader can check. This is <see cref="FileStoreResult.OriginalFileHash" /> plus the algorithm and size. Wrapped key material is not included.</summary>
/// <param name="HashAlgorithm">Algorithm that produced <see cref="OriginalFileHash" />. SHA-256 when the row did not record one.</param>
/// <param name="OriginalFileHash">Digest of the plaintext bytes.</param>
/// <param name="OriginalFileSize">Plaintext length in bytes.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileContentIntegrity(HashAlgorithm HashAlgorithm, byte[] OriginalFileHash, long OriginalFileSize)
{
    /// <summary>Hash algorithm, plaintext size, and digest length. Digest bytes are omitted.</summary>
    public override string ToString() => $"{HashAlgorithm} Size={OriginalFileSize} HashBytes={OriginalFileHash.Length}";

    /// <summary>Projects the plaintext hash from <paramref name="metadata" />. Throws when that hash is missing.</summary>
    /// <param name="metadata">Stored file row.</param>
    public static FileContentIntegrity From(FileStoreResult metadata)
    {
        ArgumentHelpers.ThrowIfNull(metadata);
        return metadata.OriginalFileHash is { Length: > 0 }
            ? new(metadata.HashAlgorithm ?? HashAlgorithm.Sha256, metadata.OriginalFileHash, metadata.OriginalFileSize)
            : throw new InvalidOperationException($"File {metadata.Id} has no OriginalFileHash.");
    }
}