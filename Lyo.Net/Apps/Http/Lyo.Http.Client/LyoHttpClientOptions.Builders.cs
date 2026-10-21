using Lyo.Common.Core.Net;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;

namespace Lyo.Http.Client;

public partial class LyoHttpClientOptions
{
    internal Dictionary<string, LyoHttpConfiguredHeader> ConfiguredHeaders { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Sets <see cref="BaseUrl" />.</summary>
    public LyoHttpClientOptions WithBaseUrl(string url)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(url);
        BaseUrl = url;
        return this;
    }

    /// <summary>Sets the proxy URL and optional credentials.</summary>
    public LyoHttpClientOptions UseProxy(string url, string? username = null, string? password = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(url);
        ProxyUrl = url;
        ProxyUsername = username;
        ProxyPassword = password;
        return this;
    }

    /// <summary>Turns on the pre-send delay.</summary>
    public LyoHttpClientOptions UseTiming(TimeSpan min, TimeSpan max, double? jitter = null)
    {
        Timing.Enabled = true;
        Timing.DelayBeforeSendMin = min;
        Timing.DelayBeforeSendMax = max;
        if (jitter is not null)
            Timing.Jitter = jitter.Value;

        return this;
    }

    /// <summary>Turns on User-Agent rotation. <paramref name="agents" /> replaces the pool when any are passed.</summary>
    public LyoHttpClientOptions UseUserAgent(LyoHttpUserAgentRotation rotation, params string[] agents)
    {
        UserAgent.Enabled = true;
        UserAgent.Rotation = rotation;
        if (agents is { Length: > 0 })
            UserAgent.Agents = agents;

        return this;
    }

    /// <summary>Compresses JSON bodies with <paramref name="type" />. <paramref name="minBytes" /> replaces the minimum size when set.</summary>
    public LyoHttpClientOptions CompressRequests(LyoHttpRequestCompressionType type, int? minBytes = null)
    {
        RequestCompression = type;
        if (minBytes is not null) {
            ArgumentHelpers.ThrowIf(minBytes.Value < 0, "RequestCompressionMinBytes must be >= 0.");
            RequestCompressionMinBytes = minBytes.Value;
        }

        return this;
    }

    /// <summary>Replaces <see cref="AcceptEncodings" />. Tokens come from <see cref="LyoContentEncodings" />.</summary>
    public LyoHttpClientOptions UseAcceptEncodings(params string[] encodings)
    {
        ArgumentHelpers.ThrowIfNull(encodings);
        AcceptEncodings = encodings;
        return this;
    }

    /// <summary>Leaves response decompression off.</summary>
    public LyoHttpClientOptions DisableAutomaticDecompression()
    {
        EnableAutoResponseDecompression = false;
        return this;
    }

    /// <summary>Turns on this client's quota. The host must already have registered <c>IRateLimitService</c>.</summary>
    public LyoHttpClientOptions UseRateLimit(int permits, TimeSpan window)
    {
        RateLimit.Enabled = true;
        RateLimit.PermitLimit = permits;
        RateLimit.Window = window;
        return this;
    }

    /// <summary>Sets <see cref="LyoHttpHeaders.Authorization" /> to <c>Bearer {token}</c> on every send.</summary>
    public LyoHttpClientOptions UseBearer(string token)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(token);
        return UseHeader(LyoHttpHeaders.Authorization, "Bearer " + token);
    }

    /// <summary>Resolves a bearer token on every send. A null or empty result does not set the header.</summary>
    public LyoHttpClientOptions UseBearer(Func<IServiceProvider, CancellationToken, ValueTask<string?>> token)
    {
        ArgumentHelpers.ThrowIfNull(token);
        return UseHeader(
            LyoHttpHeaders.Authorization, async (sp, ct) => {
                var value = await token(sp, ct).ConfigureAwait(false);
                return string.IsNullOrEmpty(value) ? null : "Bearer " + value;
            });
    }

    /// <summary>Sets <see cref="LyoHttpHeaders.ApiKey" /> to the raw secret on every send.</summary>
    public LyoHttpClientOptions UseApiKey(string apiKey)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(apiKey);
        return UseHeader(LyoHttpHeaders.ApiKey, apiKey);
    }

    /// <summary>Resolves <see cref="LyoHttpHeaders.ApiKey" /> on every send.</summary>
    public LyoHttpClientOptions UseApiKey(Func<IServiceProvider, CancellationToken, ValueTask<string?>> apiKey)
    {
        ArgumentHelpers.ThrowIfNull(apiKey);
        return UseHeader(LyoHttpHeaders.ApiKey, apiKey);
    }

    /// <summary>Sets <see cref="HttpHeaderInfo.Accept" />.</summary>
    public LyoHttpClientOptions Accept(string mediaType)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(mediaType);
        return UseHeader(HttpHeaderInfo.Accept.Name, mediaType);
    }

    /// <summary>Sets <c>Accept: application/json</c>.</summary>
    public LyoHttpClientOptions AcceptJson() => Accept("application/json");

    /// <summary>Sets <see cref="HttpHeaderInfo.AcceptLanguage" />.</summary>
    public LyoHttpClientOptions AcceptLanguage(string value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        return UseHeader(HttpHeaderInfo.AcceptLanguage.Name, value);
    }

    /// <summary>Sets a static header. Pass <see cref="LyoHttpHeaders" /> when the name is a Lyo contract header.</summary>
    public LyoHttpClientOptions UseHeader(string name, string value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        Slot(name).StaticValue = value;
        return this;
    }

    /// <summary>Resolves a header on every send. A non-empty result replaces the static value for that header.</summary>
    public LyoHttpClientOptions UseHeader(string name, Func<IServiceProvider, CancellationToken, ValueTask<string?>> value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ArgumentHelpers.ThrowIfNull(value);
        Slot(name).Resolve = value;
        return this;
    }

    /// <summary>Sets several static headers.</summary>
    public LyoHttpClientOptions UseHeaders(IEnumerable<KeyValuePair<string, string>> headers)
    {
        ArgumentHelpers.ThrowIfNull(headers);
        foreach (var pair in headers)
            UseHeader(pair.Key, pair.Value);

        return this;
    }

    private LyoHttpConfiguredHeader Slot(string name)
    {
        if (!ConfiguredHeaders.TryGetValue(name, out var header)) {
            header = new();
            ConfiguredHeaders[name] = header;
        }

        return header;
    }
}

/// <summary>One default header for a client. A resolver replaces <see cref="StaticValue" /> when it returns text.</summary>
internal sealed class LyoHttpConfiguredHeader
{
    /// <summary>Value used when no resolver returns text.</summary>
    public string? StaticValue { get; set; }

    /// <summary>Per-send value. Empty leaves <see cref="StaticValue" /> in place.</summary>
    public Func<IServiceProvider, CancellationToken, ValueTask<string?>>? Resolve { get; set; }
}