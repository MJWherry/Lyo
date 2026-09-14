using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.ShortUrl.Models;

/// <summary>Shared settings for URL shortener service implementations.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ShortUrlServiceOptions
{
    /// <summary>Default options-section name for binding options.</summary>
    public const string SectionName = "ShortUrlOptions";

    /// <summary>Base URL for short links (e.g., "https://short.ly").</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Default expiration time in days (null for no expiration).</summary>
    public int? DefaultExpirationDays { get; set; }

    /// <summary>Value of the maximum alias length in characters (default: 50).</summary>
    public int MaxAliasLength { get; set; } = 50;

    /// <summary>Minimum alias length in characters (default: 3) for this record.</summary>
    public int MinAliasLength { get; set; } = 3;

    /// <summary>Flag for whether to allow custom aliases (default: true).</summary>
    public bool AllowCustomAliases { get; set; } = true;

    /// <summary>Collect metrics for shortener calls. Default: false</summary>
    public bool EnableMetrics { get; set; } = false;

    /// <summary>Flag for whether to enforce HTTPS for URLs. When enabled, HTTP URLs will be automatically converted to HTTPS. Default: false.</summary>
    public bool EnforceHttps { get; set; } = false;

    /// <summary>Throws when alias length or expiration defaults are invalid.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNegativeOrZero(MinAliasLength);
        ArgumentHelpers.ThrowIfLessThan(MaxAliasLength, MinAliasLength);
        if (DefaultExpirationDays is { } days)
            ArgumentHelpers.ThrowIfNegativeOrZero(days);
    }

    public override string ToString()
        => $"BaseUrl: {BaseUrl}, DefaultExpirationDays: {DefaultExpirationDays}, MaxAliasLength: {MaxAliasLength}, MinAliasLength: {MinAliasLength}, AllowCustomAliases: {AllowCustomAliases}, EnableMetrics: {EnableMetrics}, EnforceHttps: {EnforceHttps}";
}