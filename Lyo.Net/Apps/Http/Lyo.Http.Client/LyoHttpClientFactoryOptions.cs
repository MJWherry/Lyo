namespace Lyo.Http.Client;

/// <summary>Appsettings section <see cref="SectionName" /> for <see cref="ILyoHttpClientFactory" />.</summary>
public sealed class LyoHttpClientFactoryOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "LyoHttpClientFactory";

    /// <summary>Client used when <see cref="ILyoHttpClientFactory.Get" /> is called with an unknown key.</summary>
    public LyoHttpClientSiteConfiguration? Default { get; set; }

    /// <summary>Sites keyed by domain.</summary>
    public Dictionary<string, LyoHttpClientSiteConfiguration> Sites { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Bindable site settings. Token delegates stay in code; <see cref="BearerToken" /> is a fixed secret.</summary>
public sealed class LyoHttpClientSiteConfiguration
{
    /// <summary>Base URL for this site.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>JSON body compression. <see cref="LyoHttpRequestCompressionType.None" /> leaves the client default.</summary>
    public LyoHttpRequestCompressionType RequestCompression { get; set; }

    /// <summary>Minimum JSON size before <see cref="RequestCompression" /> applies.</summary>
    public int RequestCompressionMinBytes { get; set; } = 1024;

    /// <summary><c>Accept</c> media type. Empty skips the header.</summary>
    public string? Accept { get; set; }

    /// <summary>Raw <c>X-Api-Key</c>. Empty skips the header.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Fixed bearer token. Empty skips the header. A code <c>UseBearer</c> delegate replaces it.</summary>
    public string? BearerToken { get; set; }

    /// <summary>Quota for this site. Applied only when <see cref="LyoHttpRateLimitOptions.Enabled" /> is true.</summary>
    public LyoHttpRateLimitOptions RateLimit { get; set; } = new();
}