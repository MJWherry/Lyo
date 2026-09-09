using Lyo.Common.Core.Enums;
using Lyo.Hashing.Files;

namespace Lyo.Hashing;

/// <summary>Injectable façade for digests, hex formatting, hashing streams, sparse fingerprints, and HMAC helpers.</summary>
/// <remarks>
/// <para>
/// Use <see cref="Hasher" /> / <see cref="HexEncoding" /> at static call sites; inject this when hex casing or fingerprint defaults should be
/// centralized or swapped in tests. <see cref="HashingService.Shared" /> is the singleton <c>AddLyoHashing()</c> uses when no configuration is supplied.
/// </para>
/// <para>MD5 and sparse fingerprints are not a security integrity check; see the package README.</para>
/// </remarks>
public interface IHashingService
{
    /// <summary>Hash a contiguous buffer (SHA-2 or MD5 according to <paramref name="algorithm" />).</summary>
    byte[] Hash(ContentDigestAlgorithm algorithm, ReadOnlySpan<byte> data);

    /// <inheritdoc cref="Hash(ContentDigestAlgorithm, ReadOnlySpan{byte})" />
    byte[] Hash(ContentDigestAlgorithm algorithm, byte[] data);

    /// <summary>Hashes through EOF. Does not close <paramref name="stream" />.</summary>
    byte[] Hash(ContentDigestAlgorithm algorithm, Stream stream);

    /// <summary>Async full-file digest from a path.</summary>
    Task<byte[]> HashFileAsync(ContentDigestAlgorithm algorithm, string path, CancellationToken ct = default);

    /// <summary>Format digest bytes as hex.</summary>
    string ToHex(ReadOnlySpan<byte> digest, TextLetterCase? letterCase = null);

    /// <inheritdoc cref="ToHex(ReadOnlySpan{byte}, TextLetterCase?)" />
    string ToHex(byte[] digest, TextLetterCase? letterCase = null);

    /// <summary>Parse even-length hex (any letter case).</summary>
    byte[] ParseHex(ReadOnlySpan<char> hexChars);

    /// <summary>Timing-safe byte equality when lengths match (.NET uses constant-time primitives when they exist).</summary>
    bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right);

    /// <summary>Parses <paramref name="expectedHex" /> then compares via <see cref="FixedTimeEquals" />.</summary>
    bool EqualsHex(ReadOnlySpan<byte> digest, ReadOnlySpan<char> expectedHex);

    /// <summary>HMAC-SHA-256 (<paramref name="key" /> lifetime is the caller's).</summary>
    byte[] HmacSha256(ReadOnlySpan<byte> key, ReadOnlySpan<byte> payload);

    /// <summary>HMAC-SHA-512.</summary>
    byte[] HmacSha512(ReadOnlySpan<byte> key, ReadOnlySpan<byte> payload);

    /// <summary>Sparse-sample file fingerprint (<see cref="SparseFileFingerprinter" />).</summary>
    Task<byte[]?> FingerprintSampledFileAsync(string path, long fileSize, FileFingerprintOptions? options = null, CancellationToken ct = default);

    /// <summary>Wrap a stream for an incremental digest (algorithm chosen via <paramref name="algorithm" />).</summary>
    HashingStream CreateHashingStream(Stream inner, ContentDigestAlgorithm algorithm);

    /// <summary>Non-cryptographic checksum of a contiguous buffer; big-endian bytes (4 for 32-bit, 8 for CRC-64). <strong>Not</strong> for security — corruption only.</summary>
    byte[] Checksum(ChecksumAlgorithm algorithm, ReadOnlySpan<byte> data);

    /// <inheritdoc cref="Checksum(ChecksumAlgorithm, ReadOnlySpan{byte})" />
    byte[] Checksum(ChecksumAlgorithm algorithm, byte[] data);

    /// <summary>Checksums through EOF. Does not close <paramref name="stream" />.</summary>
    byte[] Checksum(ChecksumAlgorithm algorithm, Stream stream);

    /// <summary>Non-cryptographic checksum of a buffer as a raw number (32-bit results sit in the low bits).</summary>
    ulong ChecksumValue(ChecksumAlgorithm algorithm, ReadOnlySpan<byte> data);

    /// <summary>Async full-file checksum from a path.</summary>
    Task<byte[]> ChecksumFileAsync(ChecksumAlgorithm algorithm, string path, CancellationToken ct = default);

    /// <summary>Wrap a stream for an incremental checksum (same idea as <see cref="CreateHashingStream" />).</summary>
    ChecksumStream CreateChecksumStream(Stream inner, ChecksumAlgorithm algorithm);
}