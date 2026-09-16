using Lyo.Exceptions;
using Lyo.Http.Client.Pipeline;
using Lyo.RateLimit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Http.Client;

/// <summary>
/// Builds one long-lived <see cref="ILyoHttpClient" /> per site key. The quota policy is the key. A missing key returns the default client.
/// These clients are not recycled by <c>IHttpClientFactory</c>.
/// </summary>
public sealed class LyoHttpClientFactory : ILyoHttpClientFactory, IDisposable
{
    /// <summary>Policy name of the default client.</summary>
    public const string DefaultPolicy = "default";

    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly IRateLimitService? _limiter;
    private readonly object _sync = new();
    private readonly Dictionary<string, SiteEntry> _sites = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, LyoHttpClientSiteConfiguration> _pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _warned = new(StringComparer.OrdinalIgnoreCase);
    private LyoHttpClientSiteConfiguration? _pendingDefault;
    private SiteEntry? _default;
    private bool _defaultRegistered;
    private bool _materialized;

    /// <summary>Creates the factory. <paramref name="limiter" /> is required only for a site whose quota is enabled.</summary>
    public LyoHttpClientFactory(IServiceProvider services, ILogger<LyoHttpClientFactory>? logger = null, IRateLimitService? limiter = null)
    {
        ArgumentHelpers.ThrowIfNull(services);
        _services = services;
        _logger = logger ?? NullLogger<LyoHttpClientFactory>.Instance;
        _limiter = limiter;
    }

    /// <summary>Primary handler for tests. Production builds <see cref="LyoHttpClientHandler" />.</summary>
    internal Func<HttpMessageHandler>? PrimaryHandler { get; set; }

    /// <inheritdoc />
    public void Register(string key, Action<LyoHttpClientOptions> configure)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ArgumentHelpers.ThrowIfNull(configure);
        lock (_sync) {
            if (_sites.ContainsKey(key))
                throw new ArgumentException($"HTTP client site '{key}' is already registered.", nameof(key));

            var options = new LyoHttpClientOptions();
            if (_pending.TryGetValue(key, out var site)) {
                Apply(options, site);
                _pending.Remove(key);
            }

            configure(options);
            _sites[key] = Build(key, options);
        }
    }

    /// <inheritdoc />
    public void RegisterDefault(Action<LyoHttpClientOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        lock (_sync) {
            if (_defaultRegistered)
                throw new ArgumentException("The default HTTP client is already registered.");

            var options = new LyoHttpClientOptions();
            if (_pendingDefault is not null)
                Apply(options, _pendingDefault);

            _pendingDefault = null;
            configure(options);
            _default = Build(DefaultPolicy, options);
            _defaultRegistered = true;
        }
    }

    /// <inheritdoc />
    public ILyoHttpClient Get(string key)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        lock (_sync) {
            Materialize();
            if (_sites.TryGetValue(key, out var site))
                return site.Client;

            if (_warned.Add(key))
                _logger.LogWarning("HTTP client site {Site} is not registered. Using the default client.", key);

            return DefaultClient().Client;
        }
    }

    /// <summary>Stores appsettings sites until <see cref="Materialize" /> or a code <see cref="Register" /> overlays them.</summary>
    internal void LoadConfiguration(LyoHttpClientFactoryOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        lock (_sync) {
            _pendingDefault = options.Default;
            foreach (var pair in options.Sites)
                _pending[pair.Key] = pair.Value;
        }
    }

    /// <summary>Builds sites that code did not overlay. Safe to call more than once.</summary>
    internal void Materialize()
    {
        lock (_sync) {
            if (_materialized)
                return;

            MaterializeCore();
            _materialized = true;
        }
    }

    private void MaterializeCore()
    {
        foreach (var pair in _pending.ToArray()) {
            if (_sites.ContainsKey(pair.Key))
                continue;

            var options = new LyoHttpClientOptions();
            Apply(options, pair.Value);
            _sites[pair.Key] = Build(pair.Key, options);
        }

        _pending.Clear();
        if (_default is null && _pendingDefault is not null) {
            var options = new LyoHttpClientOptions();
            Apply(options, _pendingDefault);
            _default = Build(DefaultPolicy, options);
            _defaultRegistered = true;
            _pendingDefault = null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_sync) {
            foreach (var site in _sites.Values)
                site.Dispose();

            _sites.Clear();
            _default?.Dispose();
            _default = null;
        }
    }

    private SiteEntry DefaultClient()
    {
        if (_default is not null)
            return _default;

        _default = Build(DefaultPolicy, new LyoHttpClientOptions());
        _defaultRegistered = true;
        return _default;
    }

    private static void Apply(LyoHttpClientOptions options, LyoHttpClientSiteConfiguration site)
    {
        if (!string.IsNullOrWhiteSpace(site.BaseUrl))
            options.WithBaseUrl(site.BaseUrl!);
        if (site.RequestCompression != LyoHttpRequestCompressionType.None)
            options.CompressRequests(site.RequestCompression, site.RequestCompressionMinBytes);
        if (!string.IsNullOrWhiteSpace(site.Accept))
            options.Accept(site.Accept!);
        if (!string.IsNullOrWhiteSpace(site.ApiKey))
            options.UseApiKey(site.ApiKey!);
        if (!string.IsNullOrWhiteSpace(site.BearerToken))
            options.UseBearer(site.BearerToken!);
        if (site.RateLimit.Enabled)
            options.RateLimit = site.RateLimit;
    }

    private SiteEntry Build(string policy, LyoHttpClientOptions options)
    {
        if (options.RateLimit.Enabled && _limiter is null)
            throw new InvalidOperationException("Rate limiting is enabled but IRateLimitService is not registered. Call AddLocalRateLimit or AddRedisRateLimit.");

        options.Validate();
        HttpMessageHandler inner = PrimaryHandler?.Invoke() ?? new LyoHttpClientHandler(options);
        DelegatingHandler chain = new LyoHttpTimingHandler(options.Timing) { InnerHandler = inner };
        if (options.RateLimit.Enabled) {
            var gate = new LyoHttpRateLimitGate(options.RateLimit);
            chain = new LyoHttpRateLimitHandler(options.RateLimit, _limiter, policy, gate) { InnerHandler = chain };
        }

        chain = new LyoHttpHeaderHandler(options, _services) { InnerHandler = chain };
        chain = new LyoHttpMetricsHandler { InnerHandler = chain };
        var outer = new LyoHttpObserveHandler { InnerHandler = chain };
        var http = new HttpClient(outer, disposeHandler: true);
        var client = new LyoHttpClient(http, options);
        return new SiteEntry(client, http);
    }

    private sealed class SiteEntry(ILyoHttpClient client, HttpClient http) : IDisposable
    {
        public ILyoHttpClient Client { get; } = client;

        public void Dispose() => http.Dispose();
    }
}
