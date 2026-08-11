using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Hashing;

namespace Lyo.Cli.Services;

/// <summary>Digest / checksum / HMAC helpers over <see cref="HashingService.Shared" />.</summary>
internal static class CliHashing
{
    public static ContentDigestAlgorithm ParseDigest(string name)
        => name.Trim().ToLowerInvariant() switch {
            "sha256" or "sha-256" => ContentDigestAlgorithm.Sha256,
            "sha384" or "sha-384" => ContentDigestAlgorithm.Sha384,
            "sha512" or "sha-512" => ContentDigestAlgorithm.Sha512,
            "md5" => ContentDigestAlgorithm.Md5,
            var _ => throw new ArgumentException($"Unknown digest '{name}'. Use sha256, sha384, sha512, or md5.")
        };

    public static ChecksumAlgorithm ParseChecksum(string name)
        => name.Trim().ToLowerInvariant() switch {
            "crc32" => ChecksumAlgorithm.Crc32,
            "crc32c" => ChecksumAlgorithm.Crc32C,
            "crc64" => ChecksumAlgorithm.Crc64,
            "adler32" or "adler" => ChecksumAlgorithm.Adler32,
            var _ => throw new ArgumentException($"Unknown checksum '{name}'. Use crc32, crc32c, crc64, or adler32.")
        };

    public static async Task<string> HashAsync(ContentDigestAlgorithm algorithm, Stream input, bool upper, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(input);
        var digest = await Task.Run(() => HashingService.Shared.Hash(algorithm, input), ct).ConfigureAwait(false);
        return HashingService.Shared.ToHex(digest, upper ? TextLetterCase.Upper : TextLetterCase.Lower);
    }

    public static async Task<string> HashFileAsync(ContentDigestAlgorithm algorithm, string path, bool upper, CancellationToken ct)
    {
        var digest = await HashingService.Shared.HashFileAsync(algorithm, path, ct).ConfigureAwait(false);
        return HashingService.Shared.ToHex(digest, upper ? TextLetterCase.Upper : TextLetterCase.Lower);
    }

    public static async Task<string> ChecksumAsync(ChecksumAlgorithm algorithm, Stream input, bool upper, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(input);
        var digest = await Task.Run(() => HashingService.Shared.Checksum(algorithm, input), ct).ConfigureAwait(false);
        return HashingService.Shared.ToHex(digest, upper ? TextLetterCase.Upper : TextLetterCase.Lower);
    }

    public static async Task<string> HmacSha256Async(byte[] key, Stream input, bool upper, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(key);
        ArgumentHelpers.ThrowIfNull(input);
        await using var ms = new MemoryStream();
        await input.CopyToAsync(ms, ct).ConfigureAwait(false);
        var digest = HashingService.Shared.HmacSha256(key, ms.TryGetBuffer(out var seg) ? seg.AsSpan() : ms.ToArray());
        return HashingService.Shared.ToHex(digest, upper ? TextLetterCase.Upper : TextLetterCase.Lower);
    }

    public static async Task<string> FingerprintFileAsync(string path, bool upper, CancellationToken ct)
    {
        var info = new FileInfo(path);
        ArgumentHelpers.ThrowIf(!info.Exists, $"File not found: {path}");
        var digest = await HashingService.Shared.FingerprintSampledFileAsync(path, info.Length, ct: ct).ConfigureAwait(false);
        ArgumentHelpers.ThrowIf(digest is null, "Fingerprint failed (empty or unreadable file).");
        return HashingService.Shared.ToHex(digest!, upper ? TextLetterCase.Upper : TextLetterCase.Lower);
    }
}
