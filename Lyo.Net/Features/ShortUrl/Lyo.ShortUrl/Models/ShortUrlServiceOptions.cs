using System.Diagnostics;

namespace Lyo.ShortUrl.Models;

/// <summary>Base configuration options for URL shortener service implementations.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ShortUrlServiceOptions
{
    /// <summary>Default configuration section name for binding options.</summary>
    public const string SectionName = "ShortUrlOptions";

    /// <summary>Gets or sets the base URL for short links (e.g., "https://short.ly").</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the default expiration time in days (null for no expiration).</summary>
    public int? DefaultExpirationDays { get; set; }

    /// <summary>Gets or sets the maximum alias length in characters (default: 50).</summary>
    public int MaxAliasLength { get; set; } = 50;

    /// <summary>Gets or sets the minimum alias length in characters (default: 3).</summary>
    public int MinAliasLength { get; set; } = 3;

    /// <summary>Gets or sets whether to allow custom aliases (default: true).</summary>
    public bool AllowCustomAliases { get; set; } = true;

    /// <summary>Enable metrics collection for URL shortener operations. Default: false</summary>
    public bool EnableMetrics { get; set; } = false;

    /// <summary>Gets or sets whether to enforce HTTPS for URLs. When enabled, HTTP URLs will be automatically converted to HTTPS. Default: false</summary>
    public bool EnforceHttps { get; set; } = false;

    public override string ToString()
        => $"BaseUrl: {BaseUrl}, DefaultExpirationDays: {DefaultExpirationDays}, MaxAliasLength: {MaxAliasLength}, MinAliasLength: {MinAliasLength}, AllowCustomAliases: {AllowCustomAliases}, EnableMetrics: {EnableMetrics}, EnforceHttps: {EnforceHttps}";
}