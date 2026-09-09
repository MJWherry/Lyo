using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Lyo.Cache;

[DebuggerDisplay("{ToString(),nq}")]
public class CacheOptions
{
    /// <summary>Section name used when binding from <c>IConfiguration</c>.</summary>
    public const string SectionName = "CacheOptions";

    /// <summary>When false, <see cref="LocalCacheService" /> skips storage and always runs factories.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>True when cache operations emit metrics.</summary>
    public bool EnableMetrics { get; set; } = false;

    /// <summary>TTL for reflected <see cref="System.Reflection.PropertyInfo" /> lookups (for example query comparison helpers).</summary>
    public TimeSpan PropertyInfoExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>TTL for type-metadata entries used by conversion and comparison.</summary>
    public TimeSpan TypeMetadataExpiration { get; set; } = TimeSpan.FromHours(4);

    /// <summary>TTL for compiled property-getter delegates.</summary>
    public TimeSpan PropertyGetterExpiration { get; set; } = TimeSpan.FromHours(4);

    /// <summary>TTL for property-difference plan metadata.</summary>
    public TimeSpan ComparisonInfoExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Absolute TTL used when a call or type does not override it.</summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Defaults for byte-payload APIs (compress/encrypt framing).</summary>
    public CachePayloadOptions Payload { get; set; } = new();

    /// <summary>
    /// Soft byte budget for the backing <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache" />, applied by <c>AddLocalCache</c>. Serialized payloads count their
    /// real length; a live object is charged <see cref="NonPayloadEntrySizeBytes" />, because walking an arbitrary graph is not worth the cost. Set to
    /// <c>null</c> to leave the cache unbounded, which lets a query-result cache grow until the process is OOM-killed.
    /// </summary>
    public long? MaxSizeBytes { get; set; } = 256L * 1024 * 1024;

    /// <summary>Bytes charged against <see cref="MaxSizeBytes" /> when the value is not a byte payload.</summary>
    public long NonPayloadEntrySizeBytes { get; set; } = 1024;

    /// <summary>
    /// How long a distributed cache may keep serving an expired entry while the factory is failing. Only <c>Lyo.Cache.Fusion</c> reads this. Ten minutes covers a deploy or a
    /// brief database blip; the previous 24 hours meant an outage could serve day-old query results long after everything recovered.
    /// </summary>
    public TimeSpan FailSafeMaxDuration { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Wait before retrying a failing factory while stale data is still served. Only <c>Lyo.Cache.Fusion</c> reads this.</summary>
    public TimeSpan FailSafeThrottleDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// When a bulk mutation touches more than this many distinct primary keys, list-query invalidation falls back to tag <c>entity:&lt;type&gt;</c> instead of per-id tags.
    /// Used by Lyo.Api CRUD invalidation helpers. Ignored when <see cref="QueryCacheTagGranularity" /> is <see cref="QueryCacheTagGranularity.Broad" /> (invalidation is always
    /// type-wide).
    /// </summary>
    public int MaxBulkQueryInvalidationByIdCount { get; set; } = 20;

    /// <summary>
    /// How <c>Lyo.Api</c> tags query/GET results. <see cref="QueryCacheTagGranularity.Broad" /> uses only <c>entity:&lt;typename&gt;</c>-style tags (and
    /// scope/shape tags), reducing tag CPU at the cost of coarser invalidation. Default is <see cref="QueryCacheTagGranularity.Broad" /> (non-granular). Set
    /// <see cref="QueryCacheTagGranularity.Granular" /> for per-row instance tags and finer invalidation at higher tagging cost.
    /// </summary>
    public QueryCacheTagGranularity QueryCacheTagGranularity { get; set; } = QueryCacheTagGranularity.Broad;

    /// <summary>Per-type TTLs. Key is a full type name (for example "My.Lib.Class") or pattern (for example "My.Lib.*"); value is minutes.</summary>
    /// <remarks>
    /// Exact and wildcard keys are both accepted: "My.Lib.Class" matches only that name; "My.Lib.*" matches "My.Lib.Class", "My.Lib.Other", and so on.
    /// When several patterns match, the more specific (longer) pattern wins.
    /// </remarks>
    public Dictionary<string, int> TypeExpirations { get; set; } = new();

    /// <summary>TTL for <paramref name="fullTypeName" />, or <see cref="DefaultExpiration" /> when no mapping matches.</summary>
    /// <param name="fullTypeName">Full type name (for example "My.Lib.Class") or a wildcard (for example "My.Lib.*").</param>
    /// <returns>Configured TTL, or <see cref="DefaultExpiration" /> when none matches.</returns>
    /// <remarks>
    /// Exact and wildcard keys are both accepted: "My.Lib.Class" matches only that name; "My.Lib.*" matches "My.Lib.Class", "My.Lib.Other", and so on.
    /// When several patterns match, the more specific (longer) pattern wins.
    /// </remarks>
    public TimeSpan GetExpirationForType(string fullTypeName)
    {
        if (string.IsNullOrWhiteSpace(fullTypeName))
            return DefaultExpiration;

        // Exact key first
        if (TypeExpirations.TryGetValue(fullTypeName, out var minutes))
            return TimeSpan.FromMinutes(minutes);

        // Then wildcards (more specific patterns preferred later)
        var matchingPatterns = new List<(string Pattern, int Minutes)>();
        foreach (var kvp in TypeExpirations) {
            if (kvp.Key.Contains('*') || kvp.Key.Contains('?')) {
                // Wildcard → regex
                var regexPattern = "^" + Regex.Escape(kvp.Key).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                if (Regex.IsMatch(fullTypeName, regexPattern, RegexOptions.IgnoreCase))
                    matchingPatterns.Add((kvp.Key, kvp.Value));
            }
        }

        // Longest matching pattern wins
        if (matchingPatterns.Count <= 0)
            return DefaultExpiration;

        var bestMatch = matchingPatterns.OrderByDescending(p => p.Pattern.Length).First();
        return TimeSpan.FromMinutes(bestMatch.Minutes);
    }

    /// <summary>TTL for <paramref name="type" />, or <see cref="DefaultExpiration" /> when no mapping matches.</summary>
    /// <param name="type">CLR type whose full name is resolved.</param>
    /// <returns>Configured TTL, or <see cref="DefaultExpiration" /> when none matches.</returns>
    public TimeSpan GetExpirationForType(Type type) => GetExpirationForType(type.FullName ?? type.Name);

    public override string ToString()
        => Enabled
            ? $"PropertyInfoExp={PropertyGetterExpiration:g} PropertyGetterExp={PropertyGetterExpiration:g} TypeMetadataExp={TypeMetadataExpiration:g} ComparisonInfoExp={ComparisonInfoExpiration:g} TypeExpirations={TypeExpirations.Count}"
            : "Disabled";
}