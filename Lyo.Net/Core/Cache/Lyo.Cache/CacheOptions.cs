using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Lyo.Cache;

[DebuggerDisplay("{ToString(),nq}")]
public class CacheOptions
{
    public const string SectionName = "CacheOptions";

    public bool Enabled { get; set; } = true;

    /// <summary>Enables metrics collection for cache operations.</summary>
    public bool EnableMetrics { get; set; } = false;

    public TimeSpan PropertyInfoExpiration { get; set; } = TimeSpan.FromHours(1);

    public TimeSpan TypeMetadataExpiration { get; set; } = TimeSpan.FromHours(4);

    public TimeSpan PropertyGetterExpiration { get; set; } = TimeSpan.FromHours(4);

    public TimeSpan ComparisonInfoExpiration { get; set; } = TimeSpan.FromHours(1);

    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Defaults for byte payload cache APIs (compress/encrypt framing).</summary>
    public CachePayloadOptions Payload { get; set; } = new();

    /// <summary>
    /// When bulk mutations affect more than this many distinct primary keys, list-query cache invalidation falls back to tag <c>entity:&lt;type&gt;</c> instead of per-id tags.
    /// Used by Lyo.Api CRUD invalidation helpers.
    /// </summary>
    public int MaxBulkQueryInvalidationByIdCount { get; set; } = 20;

    /// <summary>Type-specific expiration timeouts. Key is the full type name (e.g., "My.Lib.Class") or pattern (e.g., "My.Lib.*"), value is expiration time in minutes.</summary>
    /// <remarks>
    /// Supports exact matches and wildcard patterns: - Exact: "My.Lib.Class" matches exactly "My.Lib.Class" - Wildcard: "My.Lib.*" matches "My.Lib.Class", "My.Lib.Other", etc. -
    /// Multiple patterns: More specific patterns take precedence over wildcards
    /// </remarks>
    public Dictionary<string, int> TypeExpirations { get; set; } = new();

    /// <summary>Gets the expiration timeout for a specific type, or returns the default expiration if not configured.</summary>
    /// <param name="fullTypeName">The full type name (e.g., "My.Lib.Class") or pattern (e.g., "My.Lib.*")</param>
    /// <returns>The expiration timeout for the type, or DefaultExpiration if not found</returns>
    /// <remarks>
    /// Supports exact matches and wildcard patterns: - Exact: "My.Lib.Class" matches exactly "My.Lib.Class" - Wildcard: "My.Lib.*" matches "My.Lib.Class", "My.Lib.Other", etc. -
    /// Multiple patterns: More specific patterns take precedence over wildcards
    /// </remarks>
    public TimeSpan GetExpirationForType(string fullTypeName)
    {
        if (string.IsNullOrWhiteSpace(fullTypeName))
            return DefaultExpiration;

        // First, try exact match
        if (TypeExpirations.TryGetValue(fullTypeName, out var minutes))
            return TimeSpan.FromMinutes(minutes);

        // Then try wildcard patterns (more specific patterns first)
        var matchingPatterns = new List<(string Pattern, int Minutes)>();
        foreach (var kvp in TypeExpirations) {
            if (kvp.Key.Contains('*') || kvp.Key.Contains('?')) {
                // Convert wildcard pattern to regex
                var regexPattern = "^" + Regex.Escape(kvp.Key).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                if (Regex.IsMatch(fullTypeName, regexPattern, RegexOptions.IgnoreCase))
                    matchingPatterns.Add((kvp.Key, kvp.Value));
            }
        }

        // Return the most specific pattern (longest pattern string)
        if (matchingPatterns.Count > 0) {
            var bestMatch = matchingPatterns.OrderByDescending(p => p.Pattern.Length).First();
            return TimeSpan.FromMinutes(bestMatch.Minutes);
        }

        return DefaultExpiration;
    }

    /// <summary>Gets the expiration timeout for a specific type, or returns the default expiration if not configured.</summary>
    /// <param name="type">The type to get expiration for</param>
    /// <returns>The expiration timeout for the type, or DefaultExpiration if not found</returns>
    public TimeSpan GetExpirationForType(Type type) => GetExpirationForType(type.FullName ?? type.Name);

    public override string ToString()
        => Enabled
            ? $"PropertyInfoExp={PropertyGetterExpiration:g} PropertyGetterExp={PropertyGetterExpiration:g} TypeMetadataExp={TypeMetadataExpiration:g} ComparisonInfoExp={ComparisonInfoExpiration:g} TypeExpirations={TypeExpirations.Count}"
            : "Disabled";
}