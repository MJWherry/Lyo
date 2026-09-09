using Lyo.Authentication.Models.Format;

namespace Lyo.Authentication.Options;

/// <summary>Top-level authentication options. Bound from configuration section <see cref="SectionName" />.</summary>
public sealed class AuthenticationOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "LyoAuthentication";

    /// <summary>Deployment ring this host is running in. New Format-B tokens are minted with this ring; validators reject mismatched rings.</summary>
    public string Ring { get; set; } = ApiTokenRing.Live;

    /// <summary>Default lifetime for newly minted Personal Access Tokens when the caller does not specify one. Default = 365 days.</summary>
    public TimeSpan DefaultPatLifetime { get; set; } = TimeSpan.FromDays(365);

    /// <summary>
    /// When <c>true</c>, validators intersect the snapshotted scopes on a token with the user's *current* <c>scopes_json</c> at validation time (Option B). When <c>false</c>
    /// (the default), snapshotted scopes are honored as-is (Option C). Set <c>true</c> only in high-security deployments that need instant scope demotion across all active tokens.
    /// </summary>
    public bool EnableDynamicScopeIntersection { get; set; } = false;
}