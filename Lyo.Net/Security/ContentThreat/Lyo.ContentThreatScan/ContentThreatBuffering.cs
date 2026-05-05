using System.Text;
using Lyo.Exceptions;

namespace Lyo.ContentThreatScan;

/// <summary>Helpers for buffering streams used by malware adapters.</summary>
public static class ContentThreatBuffering
{
    /// <returns>Copied bytes capped at maximum length; stream position advances.</returns>
    public static async Task<byte[]> ReadLimitedAsync(Stream stream, int maxBytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(stream);

        if (maxBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(maxBytes));

        if (maxBytes == 0)
            return Array.Empty<byte>();

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

#if NET5_0_OR_GREATER
    public static byte[] ComputeSha256(ReadOnlySpan<byte> data) => global::System.Security.Cryptography.SHA256.HashData(data);

    public static string Sha256DigestToHexLower(ReadOnlySpan<byte> digest32) => Convert.ToHexString(digest32).ToLowerInvariant();
#else
    public static byte[] ComputeSha256(ReadOnlySpan<byte> data)
    {
        var leased = data.ToArray();
        using var sha256 = global::System.Security.Cryptography.SHA256.Create();
        return sha256.ComputeHash(leased);
    }

    public static string Sha256DigestToHexLower(ReadOnlySpan<byte> digest32)
    {
        var arr = digest32.ToArray();
        const string hexChars = "0123456789abcdef";
        var sb = new StringBuilder(arr.Length * 2);
        foreach (var b in arr) {
            sb.Append(hexChars[b >> 4]);
            sb.Append(hexChars[b & 0xf]);
        }

        return sb.ToString();
    }
#endif
}
