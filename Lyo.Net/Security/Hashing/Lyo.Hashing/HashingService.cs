using System.Security.Cryptography;
using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Hashing.Files;

namespace Lyo.Hashing;

/// <inheritdoc cref="IHashingService" />
/// <seealso cref="Shared" />
public sealed class HashingService(HashingOptions? options = null) : IHashingService
{
    /// <summary>Singleton with <see cref="HashingOptions.Default" /> (<see cref="Random.Shared" /> style).</summary>
    public static HashingService Shared { get; } = new(HashingOptions.Default);

    private readonly HashingOptions _options = options ?? HashingOptions.Default;

    /// <inheritdoc />
    public byte[] Hash(ContentDigestAlgorithm algorithm, byte[] data)
    {
        ArgumentHelpers.ThrowIfNull(data);
        return Hash(algorithm, data.AsSpan());
    }

    /// <inheritdoc />
    public byte[] Hash(ContentDigestAlgorithm algorithm, ReadOnlySpan<byte> data)
        => algorithm switch {
            ContentDigestAlgorithm.Sha256 => Hasher.ComputeSha256(data),
            ContentDigestAlgorithm.Sha384 => Hasher.ComputeSha384(data),
            ContentDigestAlgorithm.Sha512 => Hasher.ComputeSha512(data),
            ContentDigestAlgorithm.Md5 => Hasher.ComputeMd5(data),
            var _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
        };

    /// <inheritdoc />
    public byte[] Hash(ContentDigestAlgorithm algorithm, Stream stream)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        return algorithm switch {
            ContentDigestAlgorithm.Sha256 => Hasher.ComputeSha256(stream),
            ContentDigestAlgorithm.Sha384 => Hasher.ComputeSha384(stream),
            ContentDigestAlgorithm.Sha512 => Hasher.ComputeSha512(stream),
            ContentDigestAlgorithm.Md5 => Hasher.ComputeMd5(stream),
            var _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
        };
    }

    /// <inheritdoc />
    public async Task<byte[]> HashFileAsync(ContentDigestAlgorithm algorithm, string path, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        ArgumentHelpers.ThrowIfFileNotFound(path);
        using var fs = File.OpenRead(path);
#if NET5_0_OR_GREATER
        return algorithm switch {
            ContentDigestAlgorithm.Sha256 => await SHA256.HashDataAsync(fs, ct).ConfigureAwait(false),
            ContentDigestAlgorithm.Sha384 => await SHA384.HashDataAsync(fs, ct).ConfigureAwait(false),
            ContentDigestAlgorithm.Sha512 => await SHA512.HashDataAsync(fs, ct).ConfigureAwait(false),
            ContentDigestAlgorithm.Md5 => await ComputeMd5Async(fs, ct).ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null),
        };
#else
#pragma warning disable CA5394 // synchronous file hash on netstandard2.x
        return Hash(algorithm, fs);
#pragma warning restore CA5394
#endif
    }

#if NET5_0_OR_GREATER
    private static async Task<byte[]> ComputeMd5Async(Stream stream, CancellationToken ct)
    {
        using var md5 = MD5.Create();
#pragma warning disable CA5351 // MD5 — non-security hashing service surface
        return await md5.ComputeHashAsync(stream, ct).ConfigureAwait(false);
#pragma warning restore CA5351
    }
#endif

    /// <inheritdoc />
    public string ToHex(ReadOnlySpan<byte> digest, TextLetterCase? letterCase = null) => HexEncoding.ToHexString(digest, letterCase ?? _options.DefaultHexLetterCase);

    /// <inheritdoc />
    public string ToHex(byte[] digest, TextLetterCase? letterCase = null)
    {
        ArgumentHelpers.ThrowIfNull(digest);
        return ToHex(digest.AsSpan(), letterCase);
    }

    /// <inheritdoc />
    public byte[] ParseHex(ReadOnlySpan<char> hexChars) => HexEncoding.FromHex(hexChars);

    /// <inheritdoc />
    public bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
#if NET5_0_OR_GREATER
        return CryptographicOperations.FixedTimeEquals(left, right);
#else
        if (left.Length != right.Length)
            return false;

        var diff = 0;
        for (var i = 0; i < left.Length; i++)
            diff |= left[i] ^ right[i];

        return diff == 0;
#endif
    }

    /// <inheritdoc />
    public bool EqualsHex(ReadOnlySpan<byte> digest, ReadOnlySpan<char> expectedHex)
    {
        if ((expectedHex.Length & 1) != 0 || expectedHex.Length / 2 != digest.Length)
            return false;

        var parsed = digest.Length <= 512 ? stackalloc byte[digest.Length] : new byte[digest.Length];
        if (!HexEncoding.TryDecodeHex(expectedHex, parsed, out var written) || written != digest.Length)
            return false;

        return FixedTimeEquals(digest, parsed);
    }

    /// <inheritdoc />
    public byte[] HmacSha256(ReadOnlySpan<byte> key, ReadOnlySpan<byte> payload)
    {
#if NET5_0_OR_GREATER
        return HMACSHA256.HashData(key, payload);
#else
        using var hmac = new HMACSHA256(key.ToArray());
        return hmac.ComputeHash(payload.ToArray());
#endif
    }

    /// <inheritdoc />
    public byte[] HmacSha512(ReadOnlySpan<byte> key, ReadOnlySpan<byte> payload)
    {
#if NET5_0_OR_GREATER
        return HMACSHA512.HashData(key, payload);
#else
        using var hmac = new HMACSHA512(key.ToArray());
        return hmac.ComputeHash(payload.ToArray());
#endif
    }

    /// <inheritdoc />
    public Task<byte[]?> FingerprintSampledFileAsync(string path, long fileSize, FileFingerprintOptions? options = null, CancellationToken ct = default)
        => SparseFileFingerprinter.FingerprintAsync(path, fileSize, ct, options ?? _options.FingerprintDefaults);

    /// <inheritdoc />
    public HashingStream CreateHashingStream(Stream inner, ContentDigestAlgorithm algorithm)
    {
        ArgumentHelpers.ThrowIfNull(inner);
#pragma warning disable CA5351
        return algorithm switch {
            ContentDigestAlgorithm.Sha256 => new(inner, SHA256.Create()),
            ContentDigestAlgorithm.Sha384 => new(inner, SHA384.Create()),
            ContentDigestAlgorithm.Sha512 => new(inner, SHA512.Create()),
            ContentDigestAlgorithm.Md5 => new(inner, MD5.Create()),
            var _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
        };
#pragma warning restore CA5351
    }
}