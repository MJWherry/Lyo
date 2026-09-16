using Microsoft.Net.Http.Headers;

namespace Lyo.Api.FileStorage;

/// <summary>How <c>GET files/{id}/download</c> answers a <c>Range</c> header.</summary>
internal enum StoredDownloadKind
{
    /// <summary>Entire plaintext body, status 200.</summary>
    Full,

    /// <summary>One satisfiable byte range, status 206.</summary>
    Partial,

    /// <summary>A single well-formed range that starts at or past the end, status 416.</summary>
    Unsatisfiable
}

/// <summary>Range decision for one download. <see cref="Start" /> and <see cref="EndInclusive" /> matter only for <see cref="StoredDownloadKind.Partial" />.</summary>
internal readonly record struct StoredDownloadDecision(StoredDownloadKind Kind, bool AcceptRanges, long Start, long EndInclusive);

/// <summary>RFC 9110 single byte-range rules for an uncompressed download the backend can slice.</summary>
internal static class StoredByteRange
{
    /// <summary>
    /// True when the object is not compressed and the backend can read a byte slice. Content type does not matter. An encrypted object is decrypted from the chunk that
    /// holds the requested plaintext offset. A compressed object stays a full body with no <c>Accept-Ranges</c>.
    /// </summary>
    public static bool CanServe(bool isCompressed, bool backendSupportsRange) => !isCompressed && backendSupportsRange;

    /// <summary>
    /// Resolves one <c>Range</c> header against <paramref name="total" /> plaintext bytes. More than one range, a malformed header, or a missing header is a full body.
    /// <paramref name="canServe" /> false ignores the header.
    /// </summary>
    public static StoredDownloadDecision Decide(bool canServe, long total, string? rangeHeader)
    {
        if (!canServe)
            return new(StoredDownloadKind.Full, false, 0, 0);

        if (string.IsNullOrWhiteSpace(rangeHeader)
            || !RangeHeaderValue.TryParse(rangeHeader, out var parsed)
            || parsed.Ranges.Count != 1
            || !parsed.Unit.Equals("bytes", StringComparison.OrdinalIgnoreCase))
            return new(StoredDownloadKind.Full, true, 0, 0);

        var item = parsed.Ranges.Single();
        if (item.From is null && item.To is null)
            return new(StoredDownloadKind.Full, true, 0, 0);

        if (total <= 0)
            return new(StoredDownloadKind.Unsatisfiable, true, 0, 0);

        if (item.From is null) {
            var suffix = item.To!.Value;
            if (suffix <= 0)
                return new(StoredDownloadKind.Full, true, 0, 0);

            var suffixStart = Math.Max(0, total - suffix);
            return new(StoredDownloadKind.Partial, true, suffixStart, total - 1);
        }

        var start = item.From.Value;
        if (start >= total)
            return new(StoredDownloadKind.Unsatisfiable, true, 0, 0);

        var end = item.To ?? (total - 1);
        if (end < start)
            return new(StoredDownloadKind.Full, true, 0, 0);

        if (end >= total)
            end = total - 1;

        return new(StoredDownloadKind.Partial, true, start, end);
    }
}
