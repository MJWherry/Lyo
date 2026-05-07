using System.Security.Cryptography;
using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Hashing;

#pragma warning disable CA5350 // SHA-1 for Maven-style checksum parity; not a security boundary here.

namespace Lyo.PackageMetadata;

/// <summary>Hex digest helpers for package artifact bytes (e.g. <c>.nupkg</c>, <c>.jar</c>). NuGet feed <c>packageHash</c> uses SHA-512 lowercase hex.</summary>
public static class PackageArtifactDigest
{
    /// <summary>Computes a lowercase hex digest of <paramref name="contents" /> using <paramref name="algorithm" />.</summary>
    public static string ComputeHex(ArtifactDigestAlgorithm algorithm, byte[] contents)
    {
        ArgumentHelpers.ThrowIfNull(contents);
        return algorithm switch {
            ArtifactDigestAlgorithm.None => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Specify a hash algorithm."),
            ArtifactDigestAlgorithm.Sha512 => HexEncoding.ToHexString(Hasher.ComputeSha512(contents), TextLetterCase.Lower),
            ArtifactDigestAlgorithm.Sha256 => HexEncoding.ToHexString(Hasher.ComputeSha256(contents), TextLetterCase.Lower),
            ArtifactDigestAlgorithm.Sha1 => HexEncoding.ToHexString(ComputeSha1Bytes(contents), TextLetterCase.Lower),
            var _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
        };
    }

    /// <summary>Computes a lowercase hex digest of the remainder of <paramref name="stream" /> from its current position.</summary>
    public static string ComputeHex(ArtifactDigestAlgorithm algorithm, Stream stream, bool leaveOpen = false)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        try {
            return algorithm switch {
                ArtifactDigestAlgorithm.None => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Specify a hash algorithm."),
                ArtifactDigestAlgorithm.Sha512 => HexEncoding.ToHexString(Hasher.ComputeSha512(stream), TextLetterCase.Lower),
                ArtifactDigestAlgorithm.Sha256 => HexEncoding.ToHexString(Hasher.ComputeSha256(stream), TextLetterCase.Lower),
                ArtifactDigestAlgorithm.Sha1 => HexEncoding.ToHexString(ComputeSha1Stream(stream), TextLetterCase.Lower),
                var _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
            };
        }
        finally {
            if (!leaveOpen)
                stream.Dispose();
        }
    }

    /// <inheritdoc cref="ComputeHex(ArtifactDigestAlgorithm, byte[])" />
    public static string ComputeHexSha512(byte[] packageContents) => ComputeHex(ArtifactDigestAlgorithm.Sha512, packageContents);

    /// <inheritdoc cref="ComputeHex(ArtifactDigestAlgorithm, Stream, bool)" />
    public static string ComputeHexSha512(Stream packageStream, bool leaveOpen = false) => ComputeHex(ArtifactDigestAlgorithm.Sha512, packageStream, leaveOpen);

    private static byte[] ComputeSha1Bytes(byte[] data)
    {
#if NET5_0_OR_GREATER
        return SHA1.HashData(data);
#else
        using var sha1 = SHA1.Create();
        return sha1.ComputeHash(data);
#endif
    }

    private static byte[] ComputeSha1Stream(Stream stream)
    {
        using var sha1 = SHA1.Create();
        return sha1.ComputeHash(stream);
    }
}

#pragma warning restore CA5350