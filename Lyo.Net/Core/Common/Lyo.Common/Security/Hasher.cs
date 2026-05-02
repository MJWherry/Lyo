using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Lyo.Exceptions;

namespace Lyo.Common.Security;

/// <summary>SHA-2 digest helpers for <c>netstandard2.0</c> and modern .NET (one-shot <see cref="SHA256.HashData(ReadOnlySpan{byte})" /> APIs when available).</summary>
public static class Hasher
{
    /// <summary>Computes a SHA-2 digest of <paramref name="data" />. Supported <paramref name="digestBits" />: <c>256</c>, <c>384</c>, <c>512</c>.</summary>
    /// <param name="digestBits">256 → SHA-256, 384 → SHA-384, 512 → SHA-512.</param>
    [return: NotNull]
    public static byte[] ComputeSha2(int digestBits, byte[] data)
    {
        ArgumentHelpers.ThrowIfNull(data);
#if NET5_0_OR_GREATER
        return ComputeSha2(digestBits, data.AsSpan());
#else
        using var alg = CreateSha2(digestBits);
        return alg.ComputeHash(data);
#endif
    }

    /// <summary>Computes a SHA-2 digest of <paramref name="data" />. Supported <paramref name="digestBits" />: <c>256</c>, <c>384</c>, <c>512</c>.</summary>
    /// <param name="digestBits">256 → SHA-256, 384 → SHA-384, 512 → SHA-512.</param>
    [return: NotNull]
    public static byte[] ComputeSha2(int digestBits, ReadOnlySpan<byte> data)
    {
#if NET5_0_OR_GREATER
        return digestBits switch {
            256 => SHA256.HashData(data),
            384 => SHA384.HashData(data),
            512 => SHA512.HashData(data),
            var _ => throw new ArgumentOutOfRangeException(nameof(digestBits), digestBits, "Supported values: 256, 384, 512.")
        };
#else
        using var alg = CreateSha2(digestBits);
        return alg.ComputeHash(data.ToArray());
#endif
    }

#if !NET5_0_OR_GREATER
    private static HashAlgorithm CreateSha2(int digestBits) =>
        digestBits switch {
            256 => SHA256.Create(),
            384 => SHA384.Create(),
            512 => SHA512.Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(digestBits), digestBits, "Supported values: 256, 384, 512."),
        };
#endif
}