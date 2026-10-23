using System.Net;
using Lyo.Common.Core.Net;
using Lyo.Common.Metadata.Records;
using Lyo.RateLimit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Http.Client.Tests;

public sealed class LyoHttpClientFactoryTests
{
    [Fact]
    public void CompressRequests_SetsTypeAndMinBytes()
    {
        var options = new LyoHttpClientOptions().CompressRequests(LyoHttpRequestCompressionType.Gzip, 2048);
        Assert.Equal(LyoHttpRequestCompressionType.Gzip, options.RequestCompression);
        Assert.Equal(2048, options.RequestCompressionMinBytes);
        options.CompressRequests(LyoHttpRequestCompressionType.Deflate);
        Assert.Equal(LyoHttpRequestCompressionType.Deflate, options.RequestCompression);
        Assert.Equal(2048, options.RequestCompressionMinBytes);
    }

    [Fact]
    public void UseRateLimit_EnablesQuota()
    {
        var options = new LyoHttpClientOptions().UseRateLimit(5, TimeSpan.FromSeconds(1));
        Assert.True(options.RateLimit.Enabled);
        Assert.Equal(5, options.RateLimit.PermitLimit);
        Assert.Equal(TimeSpan.FromSeconds(1), options.RateLimit.Window);
    }

    [Fact]
    public async Task Send_AddLyoHttpClient_UseBearer_SetsAuthorization()
    {
        var capture = new CaptureHandler();
        var services = new ServiceCollection();
        services.AddLyoHttpClient(o => o.UseBearer("abc")).ConfigurePrimaryHttpMessageHandler(() => capture);
        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<LyoHttpClient>();
        using var response = await client.GetClient().GetAsync("https://example.test/", TestCt);
        Assert.Equal("Bearer abc", capture.Authorization);
    }

    [Fact]
    public async Task AddLyoHttpClient_QuotaOffWithoutStore_Succeeds()
    {
        var services = new ServiceCollection();
        services.AddLyoHttpClient(o => o.WithBaseUrl("https://example.test")).ConfigurePrimaryHttpMessageHandler(() => new CaptureHandler());
        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<LyoHttpClient>();
        using var response = await client.GetClient().GetAsync("/", TestCt);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddLyoHttpClient_EnabledWithoutStore_Throws()
    {
        var services = new ServiceCollection();
        services.AddLyoHttpClient(o => o.UseRateLimit(1, TimeSpan.FromSeconds(1)));
        await using var provider = services.BuildServiceProvider();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetRequiredService<LyoHttpClient>().GetClient().GetAsync("https://example.test/", TestCt));
        Assert.Contains("IRateLimitService", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_UnknownKey_LogsWarningAndReturnsDefault()
    {
        var logger = new ListLogger();
        var factory = Factory(logger: logger);
        var first = factory.Get("missing.example");
        var second = factory.Get("missing.example");
        var other = factory.Get("other.example");
        Assert.Same(first, second);
        Assert.Same(first, other);
        Assert.Equal(2, logger.Warnings.Count);
        Assert.Contains("missing.example", logger.Warnings[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("other.example", logger.Warnings[1], StringComparison.OrdinalIgnoreCase);
        using var response = await first.GetClient().GetAsync("https://example.test/", TestCt);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void Get_UnknownKey_ReturnsRegisteredDefault()
    {
        var factory = Factory();
        factory.RegisterDefault(o => o.WithBaseUrl("https://fallback.test/"));
        var client = factory.Get("missing.example");
        Assert.Equal(new("https://fallback.test/"), client.GetClient().BaseAddress);
        Assert.Same(client, factory.Get("another.example"));
    }

    [Fact]
    public void Get_SameKey_ReturnsSameClient()
    {
        var factory = Factory();
        factory.Register("mySite.com", o => o.WithBaseUrl("https://mySite.com/"));
        Assert.Same(factory.Get("mySite.com"), factory.Get("MYSITE.COM"));
    }

    [Fact]
    public void Register_DuplicateKey_Throws()
    {
        var factory = Factory();
        factory.Register("mySite.com", o => o.WithBaseUrl("https://mySite.com/"));
        Assert.Throws<ArgumentException>(() => factory.Register("mysite.com", _ => { }));
    }

    [Fact]
    public void Register_QuotaOffWithoutStore_Succeeds()
    {
        var factory = Factory();
        factory.Register("plain.example", o => o.WithBaseUrl("https://plain.example/"));
        Assert.NotNull(factory.Get("plain.example"));
    }

    [Fact]
    public void Register_EnabledWithoutStore_Throws()
    {
        var factory = Factory();
        var ex = Assert.Throws<InvalidOperationException>(() => factory.Register("limited.example", o => o.UseRateLimit(1, TimeSpan.FromSeconds(1))));
        Assert.Contains("IRateLimitService", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Send_UseBearer_SetsAuthorizationFromDelegate()
    {
        var capture = new CaptureHandler();
        var calls = 0;
        var factory = Factory(primary: () => capture);
        factory.Register(
            "mySite.com", o => {
                o.WithBaseUrl("https://mySite.com/");
                o.UseBearer((_, _) => {
                    calls++;
                    return new("secret");
                });
            });

        var client = factory.Get("mySite.com");
        using var first = await client.GetClient().GetAsync("/", TestCt);
        using var kept = new HttpRequestMessage(HttpMethod.Get, "/");
        kept.Headers.TryAddWithoutValidation(LyoHttpHeaders.Authorization, "Bearer existing");
        using var second = await client.GetClient().SendAsync(kept, TestCt);
        Assert.Equal(2, calls);
        Assert.Equal("Bearer secret", capture.Requests[0].Headers.GetValues(LyoHttpHeaders.Authorization).Single());
        Assert.Equal("Bearer existing", capture.Requests[1].Headers.GetValues(LyoHttpHeaders.Authorization).Single());
    }

    [Fact]
    public async Task Send_UseApiKeyAndAccept_SetsStaticHeaders()
    {
        var capture = new CaptureHandler();
        var factory = Factory(primary: () => capture);
        factory.Register(
            "mySite.com", o => {
                o.WithBaseUrl("https://mySite.com/");
                o.UseApiKey("site-key");
                o.AcceptJson();
            });

        using var response = await factory.Get("mySite.com").GetClient().GetAsync("/", TestCt);
        var request = capture.Requests[0];
        Assert.Equal("site-key", request.Headers.GetValues(LyoHttpHeaders.ApiKey).Single());
        Assert.Equal("application/json", request.Headers.GetValues(HttpHeaderInfo.Accept.Name).Single());
    }

    [Fact]
    public async Task Send_TwoSites_DoNotShareQuota()
    {
        var limiter = new PerPolicyLimiter(TimeSpan.FromSeconds(30));
        var factory = Factory(limiter);
        factory.Register(
            "a.example", o => {
                o.WithBaseUrl("https://a.example/");
                o.UseRateLimit(1, TimeSpan.FromSeconds(10));
            });

        factory.Register(
            "b.example", o => {
                o.WithBaseUrl("https://b.example/");
                o.UseRateLimit(1, TimeSpan.FromSeconds(10));
            });

        using var a = await factory.Get("a.example").GetClient().GetAsync("/", TestCt);
        using var b = await factory.Get("b.example").GetClient().GetAsync("/", TestCt);
        Assert.Equal(HttpStatusCode.OK, a.StatusCode);
        Assert.Equal(HttpStatusCode.OK, b.StatusCode);
        Assert.Equal(["a.example", "b.example"], limiter.Policies);
    }

    [Fact]
    public async Task FromConfiguration_BindsSiteCompressionQuotaAndApiKey()
    {
        var capture = new CaptureHandler();
        var limiter = new RecordingLimiter();
        var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> {
                    ["LyoHttpClientFactory:Sites:mySite.com:BaseUrl"] = "https://mySite.com/",
                    ["LyoHttpClientFactory:Sites:mySite.com:RequestCompression"] = "Gzip",
                    ["LyoHttpClientFactory:Sites:mySite.com:ApiKey"] = "site-key",
                    ["LyoHttpClientFactory:Sites:mySite.com:Accept"] = "application/json",
                    ["LyoHttpClientFactory:Sites:mySite.com:RateLimit:Enabled"] = "true",
                    ["LyoHttpClientFactory:Sites:mySite.com:RateLimit:PermitLimit"] = "5",
                    ["LyoHttpClientFactory:Sites:mySite.com:RateLimit:Window"] = "00:00:01"
                })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IRateLimitService>(limiter);
        services.AddLyoHttpClientFactoryFromConfiguration(config, (factory, _) => ((LyoHttpClientFactory)factory).PrimaryHandler = () => capture);
        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ILyoHttpClientFactory>().Get("mySite.com");
        var body = new string('x', 2048);
        _ = await client.PostAsBinaryAsync("/", body, ct: TestCt);
        Assert.Equal(new("https://mySite.com/"), client.GetClient().BaseAddress);
        Assert.Equal("site-key", capture.Requests[0].Headers.GetValues(LyoHttpHeaders.ApiKey).Single());
        Assert.Equal("application/json", capture.Requests[0].Headers.GetValues(HttpHeaderInfo.Accept.Name).Single());
        Assert.Equal(LyoContentEncodings.GZip, capture.ContentEncodings[0]);
        Assert.Equal("mySite.com", limiter.Last.Policy);
        Assert.Equal(5, limiter.Last.PermitLimit);
        Assert.Equal(TimeSpan.FromSeconds(1), limiter.Last.Window);
    }

    private static LyoHttpClientFactory Factory(IRateLimitService? limiter = null, ListLogger? logger = null, Func<HttpMessageHandler>? primary = null)
        => new(new ServiceCollection().BuildServiceProvider(), logger ?? new ListLogger(), limiter) { PrimaryHandler = primary ?? (() => new CaptureHandler()) };

    private sealed class RecordingLimiter : IRateLimitService
    {
        public RateLimitRequest Last { get; private set; }

        public ValueTask<RateLimitDecision> TryAcquireAsync(RateLimitRequest request, CancellationToken ct = default)
        {
            Last = request;
            return new(new RateLimitDecision(true, null, request.PermitLimit, 0));
        }
    }

    private sealed class PerPolicyLimiter(TimeSpan retry) : IRateLimitService
    {
        private readonly Dictionary<string, int> _counts = new(StringComparer.OrdinalIgnoreCase);

        public List<string> Policies { get; } = [];

        public ValueTask<RateLimitDecision> TryAcquireAsync(RateLimitRequest request, CancellationToken ct = default)
        {
            Policies.Add(request.Policy);
            _counts.TryGetValue(request.Policy, out var count);
            count++;
            _counts[request.Policy] = count;
            if (count > request.PermitLimit)
                return new(new RateLimitDecision(false, retry, request.PermitLimit, 0));

            return new(new RateLimitDecision(true, null, request.PermitLimit, 0));
        }
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public List<string> ContentEncodings { get; } = [];

        public string? Authorization => Requests.Count == 0 ? null : Requests[0].Headers.TryGetValues(LyoHttpHeaders.Authorization, out var values) ? values.Single() : null;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request);
            ContentEncodings.Add(request.Content?.Headers.ContentEncoding.FirstOrDefault() ?? "");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class ListLogger : ILogger<LyoHttpClientFactory>
    {
        public List<string> Warnings { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
                Warnings.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose() { }
        }
    }
}