using Lyo.Common.Core.Net;
using Lyo.Exceptions;

namespace Lyo.Http.Client;

/// <summary>
/// HTTP transport settings for <see cref="LyoHttpClient" />. Vendor option types subclass this so compression, Accept-Encoding, rate limit, and
/// <see cref="BaseUrl" /> bind the same way from configuration.
/// </summary>
public class LyoHttpClientOptions
{
    /// <summary>Default configuration section for <see cref="LyoHttpClientOptions" />.</summary>
    public const string SectionName = "LyoHttpClient";

    /// <summary>Base URL for calls (for example "https://api.example.com/"). When set, relative URIs resolve against this address.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Whether non-success statuses throw <see cref="LyoHttpException" />. When false, the response is returned so the caller can read the body
    /// while still seeing the real status code.
    /// </summary>
    public bool EnsureStatusCode { get; set; } = true;

    /// <summary>Response encodings advertised on Accept-Encoding. Defaults to gzip/deflate, plus br on modern targets.</summary>
#if NETSTANDARD2_0
    public string[] AcceptEncodings { get; set; } = [LyoContentEncodings.GZip, LyoContentEncodings.Deflate];
#else
    public string[] AcceptEncodings { get; set; } = [LyoContentEncodings.GZip, LyoContentEncodings.Deflate, LyoContentEncodings.Brotli];
#endif

    /// <summary>
    /// Whether <see cref="LyoHttpClientHandler" /> turns on <see cref="System.Net.Http.HttpClientHandler.AutomaticDecompression" /> from
    /// <see cref="AcceptEncodings" />. Default true.
    /// </summary>
    public bool EnableAutoResponseDecompression { get; set; } = true;

    /// <summary>Compression used on JSON request bodies.</summary>
    public LyoHttpRequestCompressionType RequestCompression { get; set; } = LyoHttpRequestCompressionType.None;

    /// <summary>Smallest JSON payload size, in bytes, that will be compressed.</summary>
    public int RequestCompressionMinBytes { get; set; } = 1024;

    /// <summary>Directory used when plan download paths are relative.</summary>
    public string? DownloadDirectory { get; set; }

    /// <summary>
    /// Optional HTTP proxy for the primary handler. Flared also sends this to FlareSolverr so solver, StreamBinary, and Replay share the same exit IP
    /// (<c>cf_clearance</c> is IP-bound).
    /// </summary>
    public string? ProxyUrl { get; set; }

    /// <summary>Proxy username when <see cref="ProxyUrl" /> requires auth.</summary>
    public string? ProxyUsername { get; set; }

    /// <summary>Proxy password when <see cref="ProxyUrl" /> requires auth.</summary>
    public string? ProxyPassword { get; set; }

    /// <summary>Client-side rate limit. Default off (generous vendors). Flared turns this on with its own bucket.</summary>
    public LyoHttpRateLimitOptions RateLimit { get; set; } = new();

    /// <summary>Optional delay before each send. Default off. Flared turns this on.</summary>
    public LyoHttpTimingOptions Timing { get; set; } = new();

    /// <summary>User-Agent pool. Default off. Flared uses PerSession then pins the solver UA.</summary>
    public LyoHttpUserAgentOptions UserAgent { get; set; } = new();

    /// <summary>
    /// Validates nested option objects. Call from DI registration; Flared overrides this to reject <see cref="LyoHttpUserAgentRotation.PerRequest" />.
    /// </summary>
    public virtual void Validate()
    {
        ArgumentHelpers.ThrowIf(RequestCompressionMinBytes < 0, "RequestCompressionMinBytes must be >= 0.");
        RateLimit.Validate();
        Timing.Validate();
        UserAgent.Validate();
    }
}
