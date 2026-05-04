using System.Security.Cryptography;
using Lyo.Exceptions;

namespace Lyo.Hashing;

/// <summary>
/// SHA-2 and MD5 digest helpers. MD5 is for compatibility and non-security fingerprints only. On modern .NET, SHA-2 over buffers uses one-shot
/// <see cref="SHA256.HashData(System.ReadOnlySpan{byte})" />; streams use <see cref="HashAlgorithm.ComputeHash(System.IO.Stream)" />.
/// </summary>
public static class Hasher
{
    /// <summary>Computes a SHA-2 digest of <paramref name="data" />. Supported <paramref name="digestBits" />: <c>256</c>, <c>384</c>, <c>512</c>.</summary>
    public static byte[] ComputeSha2(int digestBits, byte[] data)
    {
        ArgumentHelpers.ThrowIfNull(data);
#if NET5_0_OR_GREATER
        return ComputeSha2(digestBits, data.AsSpan());
#else
        using var alg = CreateSha2Streaming(digestBits);
        return alg.ComputeHash(data);
#endif
    }

    /// <summary>Computes a SHA-2 digest of <paramref name="data" />. Supported <paramref name="digestBits" />: <c>256</c>, <c>384</c>, <c>512</c>.</summary>
    public static byte[] ComputeSha2(int digestBits, ReadOnlySpan<byte> data)
    {
#if NET5_0_OR_GREATER
        return digestBits switch {
            256 => SHA256.HashData(data),
            384 => SHA384.HashData(data),
            512 => SHA512.HashData(data),
            var _ => throw new ArgumentOutOfRangeException(nameof(digestBits), digestBits, "Supported values: 256, 384, 512."),
        };
#else
        using var alg = CreateSha2Streaming(digestBits);
        return alg.ComputeHash(data.ToArray());
#endif
    }

    /// <summary>Computes a SHA-2 digest of bytes read from <paramref name="stream" /> (current position through end-of-stream). Does not close or dispose <paramref name="stream" />.</summary>
    public static byte[] ComputeSha2(int digestBits, Stream stream)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        using var alg = CreateSha2Streaming(digestBits);
        return alg.ComputeHash(stream);
    }

    /// <summary>SHA-256 digest of <paramref name="data" />.</summary>
    public static byte[] ComputeSha256(byte[] data) => ComputeSha2(256, data);

    /// <inheritdoc cref="ComputeSha256(byte[])" />
    public static byte[] ComputeSha256(ReadOnlySpan<byte> data) => ComputeSha2(256, data);

    /// <summary>SHA-256 digest of bytes read from <paramref name="stream" />.</summary>
    public static byte[] ComputeSha256(Stream stream) => ComputeSha2(256, stream);

    /// <inheritdoc cref="ComputeSha256(byte[])" />
    public static byte[] ComputeSha384(byte[] data) => ComputeSha2(384, data);

    /// <inheritdoc cref="ComputeSha256(ReadOnlySpan{byte})" />
    public static byte[] ComputeSha384(ReadOnlySpan<byte> data) => ComputeSha2(384, data);

    /// <inheritdoc cref="ComputeSha256(Stream)" />
    public static byte[] ComputeSha384(Stream stream) => ComputeSha2(384, stream);

    /// <inheritdoc cref="ComputeSha256(byte[])" />
    public static byte[] ComputeSha512(byte[] data) => ComputeSha2(512, data);

    /// <inheritdoc cref="ComputeSha256(ReadOnlySpan{byte})" />
    public static byte[] ComputeSha512(ReadOnlySpan<byte> data) => ComputeSha2(512, data);

    /// <inheritdoc cref="ComputeSha256(Stream)" />
    public static byte[] ComputeSha512(Stream stream) => ComputeSha2(512, stream);

    /// <summary>MD5 digest. <strong>Not for security</strong> — fingerprints and legacy compatibility only.</summary>
    public static byte[] ComputeMd5(byte[] data)
    {
        ArgumentHelpers.ThrowIfNull(data);
#if NET5_0_OR_GREATER
        return ComputeMd5(data.AsSpan());
#else
        using var md5 = MD5.Create();
        return md5.ComputeHash(data);
#endif
    }

    /// <summary>MD5 digest. <strong>Not for security</strong>.</summary>
    public static byte[] ComputeMd5(ReadOnlySpan<byte> data)
    {
#if NET5_0_OR_GREATER
        return MD5.HashData(data);
#else
        using var md5 = MD5.Create();
        return md5.ComputeHash(data.ToArray());
#endif
    }

    /// <summary>MD5 digest of stream (current position through end). <strong>Not for security</strong>. Does not dispose <paramref name="stream" />.</summary>
    public static byte[] ComputeMd5(Stream stream)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        using var md5 = MD5.Create();
        return md5.ComputeHash(stream);
    }

    private static HashAlgorithm CreateSha2Streaming(int digestBits)
        => digestBits switch {
            256 => SHA256.Create(),
            384 => SHA384.Create(),
            512 => SHA512.Create(),
            var _ => throw new ArgumentOutOfRangeException(nameof(digestBits), digestBits, "Supported values: 256, 384, 512.")
        };
}