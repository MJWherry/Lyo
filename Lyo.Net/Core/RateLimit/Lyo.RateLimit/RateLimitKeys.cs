namespace Lyo.RateLimit;

/// <summary>Redis and in-memory key. The hash tag is <c>{policy:partition}</c> so a quota stays on one cluster slot.</summary>
internal static class RateLimitKeys
{
    internal static string Build(string policy, string partition, RateLimitWindow pattern, long windowMs)
        => $"lyo:rl:{{{Sanitize(policy)}:{Sanitize(partition)}}}:{pattern}:{windowMs.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

    private static string Sanitize(string value)
    {
        if (value.IndexOf('{') < 0 && value.IndexOf('}') < 0)
            return value;

        return value.Replace("{", "").Replace("}", "");
    }
}
