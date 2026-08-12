using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Hashing;

namespace Lyo.ContentThreatScan;

/// <summary>Helpers for buffering streams used by malware adapters.</summary>
public static class ContentThreatBuffering
{
    /// <returns>Copied bytes capped at maximum length; stream position advances.</returns>
    public static async Task<byte[]> ReadLimitedAsync(Stream stream, int maxBytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        ArgumentHelpers.ThrowIfNotInRange(maxBytes, 0, int.MaxValue);
        if (maxBytes == 0)
            return [];

        var capacity = Math.Min(maxBytes, 8192);
        using var ms = new MemoryStream(capacity);
        var buffer = new byte[8192];
        var totalRead = 0;
        while (totalRead < maxBytes) {
            ct.ThrowIfCancellationRequested();
            var toRead = Math.Min(buffer.Length, maxBytes - totalRead);
#if NETSTANDARD2_0
            var read = await stream.ReadAsync(buffer, 0, toRead, ct).ConfigureAwait(false);
            if (read == 0)
                break;

            await ms.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
#else
            var read = await stream.ReadAsync(buffer.AsMemory(0, toRead), ct).ConfigureAwait(false);
            if (read == 0)
                break;

            await ms.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
#endif
            totalRead += read;
        }

        return ms.ToArray();
    }

    public static byte[] ComputeSha256(ReadOnlySpan<byte> data) => Hasher.ComputeSha256(data);

    public static string Sha256DigestToHexLower(ReadOnlySpan<byte> digest32) => HexEncoding.ToHexString(digest32, TextLetterCase.Lower);
}