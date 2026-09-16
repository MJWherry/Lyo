using System.Text;
using Lyo.Exceptions;
using Lyo.Hashing;

namespace Lyo.Idempotency;

/// <summary>SHA-256 request hashes. Compare with <see cref="Matches" />, not string equality.</summary>
public static class IdempotencyHash
{
    /// <summary>SHA-256 of <paramref name="data" />.</summary>
    public static byte[] Of(byte[] data)
    {
        ArgumentHelpers.ThrowIfNull(data);
        return HashingService.Shared.Hash(ContentDigestAlgorithm.Sha256, data);
    }

    /// <summary>SHA-256 of the raw query and body. The separator keeps a query suffix from colliding with a body prefix.</summary>
    public static byte[] OfRequest(string? query, byte[] body)
    {
        ArgumentHelpers.ThrowIfNull(body);
        var queryBytes = Encoding.UTF8.GetBytes(query ?? string.Empty);
        var combined = new byte[queryBytes.Length + 1 + body.Length];
        queryBytes.CopyTo(combined, 0);
        combined[queryBytes.Length] = 0x1e;
        body.CopyTo(combined, queryBytes.Length + 1);
        return Of(combined);
    }

    /// <summary>Constant-time equality of two hashes.</summary>
    public static bool Matches(byte[] left, byte[] right)
    {
        ArgumentHelpers.ThrowIfNull(left);
        ArgumentHelpers.ThrowIfNull(right);
        return HashingService.Shared.FixedTimeEquals(left, right);
    }
}
