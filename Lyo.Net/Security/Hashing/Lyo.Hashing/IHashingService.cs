using Lyo.Common.Enums;
using Lyo.Hashing.Files;

namespace Lyo.Hashing;

/// <summary>Injectable façade over digesting, hex formatting, hashing streams, sparse fingerprints, and HMAC helpers.</summary>
/// <remarks>
/// <para>
/// Prefer <see cref="Hasher" /> / <see cref="HexEncoding" /> for static call sites; inject this interface when hashing policy (hex casing, fingerprint defaults) should be
/// centralized or test-doubled. <see cref="HashingService.Shared" /> is the default singleton used when <c>AddLyoHashing()</c> is called without configuration.
/// </para>
/// <para>MD5 and sparse fingerprints are not suitable for security-sensitive integrity; see package README.</para>
/// </remarks>
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

    /// <summary>Non-cryptographic checksum of a contiguous buffer; big-endian bytes (4 for 32-bit checksums, 8 for CRC-64). <strong>Not</strong> for security — corruption detection only.</summary>
    byte[] Checksum(ChecksumAlgorithm algorithm, ReadOnlySpan<byte> data);

    /// <inheritdoc cref="Checksum(ChecksumAlgorithm, ReadOnlySpan{byte})" />
    byte[] Checksum(ChecksumAlgorithm algorithm, byte[] data);

    /// <summary>Checksums to end-of-stream. Does not close <paramref name="stream" />.</summary>
    byte[] Checksum(ChecksumAlgorithm algorithm, Stream stream);

    /// <summary>Non-cryptographic checksum of a buffer as a raw numeric value (32-bit results occupy the low bits).</summary>
    ulong ChecksumValue(ChecksumAlgorithm algorithm, ReadOnlySpan<byte> data);

    /// <summary>Full-file async checksum from path.</summary>
    Task<byte[]> ChecksumFileAsync(ChecksumAlgorithm algorithm, string path, CancellationToken ct = default);

    /// <summary>Wrap stream for incremental checksum (parallels <see cref="CreateHashingStream" />).</summary>
    ChecksumStream CreateChecksumStream(Stream inner, ChecksumAlgorithm algorithm);
}