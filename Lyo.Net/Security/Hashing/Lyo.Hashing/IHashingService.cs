using Lyo.Common.Enums;
using Lyo.Hashing.Files;

namespace Lyo.Hashing;

/// <summary>Injectable façade over digesting, hex formatting, hashing streams, sparse fingerprints, and HMAC helpers.</summary>
public interface IHashingService
{
    /// <summary>Hash a contiguous buffer (SHA-2 or MD5 per <paramref name="algorithm" />).</summary>
    byte[] Hash(ContentDigestAlgorithm algorithm, ReadOnlySpan<byte> data);

    /// <inheritdoc cref="Hash(ContentDigestAlgorithm, ReadOnlySpan{byte})" />
    byte[] Hash(ContentDigestAlgorithm algorithm, byte[] data);

    /// <summary>Hashes to end-of-stream. Does not close <paramref name="stream" />.</summary>
    byte[] Hash(ContentDigestAlgorithm algorithm, Stream stream);

    /// <summary>Full-file async digest from path.</summary>
    Task<byte[]> HashFileAsync(ContentDigestAlgorithm algorithm, string path, CancellationToken ct = default);

    /// <summary>Encode digest bytes as hex.</summary>
    string ToHex(ReadOnlySpan<byte> digest, TextLetterCase? letterCase = null);

    /// <inheritdoc cref="ToHex(ReadOnlySpan{byte}, TextLetterCase?)" />
    string ToHex(byte[] digest, TextLetterCase? letterCase = null);

    /// <summary>Parse even-length hexadecimal (any casing).</summary>
    byte[] ParseHex(ReadOnlySpan<char> hexChars);

    /// <summary>Byte-for-byte timing-safe equality when lengths match (.NET uses constant-time primitives when available).</summary>
    bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right);

    /// <summary>Parses <paramref name="expectedHex" /> then compares using <see cref="FixedTimeEquals" />.</summary>
    bool EqualsHex(ReadOnlySpan<byte> digest, ReadOnlySpan<char> expectedHex);

    /// <summary>HMAC-SHA-256 (<paramref name="key" /> semantics are caller-managed).</summary>
    byte[] HmacSha256(ReadOnlySpan<byte> key, ReadOnlySpan<byte> payload);

    /// <summary>HMAC-SHA-512.</summary>
    byte[] HmacSha512(ReadOnlySpan<byte> key, ReadOnlySpan<byte> payload);

    /// <summary>Sparse-sample file fingerprint (<see cref="SparseFileFingerprinter" />).</summary>
    Task<byte[]?> FingerprintSampledFileAsync(string path, long fileSize, FileFingerprintOptions? options = null, CancellationToken ct = default);

    /// <summary>Wrap stream for incremental digest (caller owns algorithms via <paramref name="algorithm" />).</summary>
    HashingStream CreateHashingStream(Stream inner, ContentDigestAlgorithm algorithm);
}