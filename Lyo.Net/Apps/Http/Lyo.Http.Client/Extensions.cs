using System.Reflection;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.Http.Client.Pipeline;
using Lyo.Http.Client.Session;
using Lyo.Metrics;
using Lyo.RateLimit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Http.Client;

/// <summary>DI helpers that wire <c>IHttpClientFactory</c> for <see cref="LyoHttpClient" /> and vendor subclasses.</summary>
public static class Extensions
{
    /// <param name="services">DI collection being extended.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers typed <typeparamref name="TClient" /> via <c>IHttpClientFactory</c>. Returns <see cref="IHttpClientBuilder" /> so the host can chain resilience, correlation, or
        /// auth handlers. Named client defaults to <c>typeof(TClient).Name</c>. When <paramref name="configure" /> is omitted, options come from a host <c>Configure&lt;TOptions&gt;</c> bind
        /// or defaults — this method does not register an empty <c>IOptions&lt;T&gt;</c> singleton.
        /// </summary>
        public IHttpClientBuilder AddLyoHttpClient<TClient, TOptions>(
            Action<TOptions>? configure = null,
            Func<IServiceProvider, HttpClient, TOptions, TClient>? factory = null,
            string? clientName = null)
            where TClient : class where TOptions : LyoHttpClientOptions, new()
        {
            ArgumentHelpers.ThrowIfNull(services);
            if (configure is null)
                return services.AddLyoHttpClientCore<TClient, TOptions>(null, factory, clientName);

            var options = new TOptions();
            configure(options);
            return services.AddLyoHttpClientCore(options, factory, clientName);
        }

        /// <summary>Registers typed <typeparamref name="TClient" /> from a ready options instance.</summary>
        public IHttpClientBuilder AddLyoHttpClient<TClient, TOptions>(
            TOptions options,
            Func<IServiceProvider, HttpClient, TOptions, TClient>? factory = null,
            string? clientName = null)
            where TClient : class where TOptions : LyoHttpClientOptions, new()
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            return services.AddLyoHttpClientCore(options, factory, clientName);
        }

        /// <summary>Binds <typeparamref name="TOptions" /> from configuration and registers typed <typeparamref name="TClient" />.</summary>
        public IHttpClientBuilder AddLyoHttpClient<TClient, TOptions>(
            IConfiguration configuration,
            string? sectionName = null,
            Func<IServiceProvider, HttpClient, TOptions, TClient>? factory = null,
            string? clientName = null)
            where TClient : class where TOptions : LyoHttpClientOptions, new()
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            sectionName ??= ResolveSectionName<TOptions>();
            var options = new TOptions();
            configuration.GetSection(sectionName).Bind(options);
            return services.AddLyoHttpClientCore(options, factory, clientName);
        }

        /// <summary>Same as <see cref="AddLyoHttpClient{TClient,TOptions}(IConfiguration, string?, Func{IServiceProvider, HttpClient, TOptions, TClient}?, string?)" />.</summary>
        public IHttpClientBuilder AddLyoHttpClientFromConfiguration<TClient, TOptions>(
            IConfiguration configuration,
            string? sectionName = null,
            Func<IServiceProvider, HttpClient, TOptions, TClient>? factory = null,
            string? clientName = null)
            where TClient : class where TOptions : LyoHttpClientOptions, new()
            => services.AddLyoHttpClient<TClient, TOptions>(configuration, sectionName, factory, clientName);

        /// <summary>Typed <see cref="ILyoHttpClient" /> / <see cref="LyoHttpClient" /> registration (generic host client).</summary>
        public IHttpClientBuilder AddLyoHttpClient(Action<LyoHttpClientOptions>? configure = null, string? clientName = null)
            => services.AddLyoHttpClient<LyoHttpClient, LyoHttpClientOptions>(
                configure, (sp, http, options) => new(http, options, sp.GetService<ILogger<LyoHttpClient>>(), session: sp.GetService<ILyoHttpSession>()),
                clientName ?? nameof(ILyoHttpClient));

        /// <summary>Binds <see cref="LyoHttpClientOptions" /> from configuration and registers typed <see cref="LyoHttpClient" />.</summary>
        public IHttpClientBuilder AddLyoHttpClientFromConfiguration(IConfiguration configuration, string? sectionName = null, string? clientName = null)
            => services.AddLyoHttpClient<LyoHttpClient, LyoHttpClientOptions>(
                configuration, sectionName, (sp, http, options) => new(http, options, sp.GetService<ILogger<LyoHttpClient>>(), session: sp.GetService<ILyoHttpSession>()),
                clientName ?? nameof(ILyoHttpClient));

        /// <summary>
        /// Registers <see cref="ILyoHttpClientFactory" />. <paramref name="configure" /> runs once when the factory is created and receives the root provider. Sites can still be
        /// added later with <see cref="ILyoHttpClientFactory.Register" />.
        /// </summary>
        public IServiceCollection AddLyoHttpClientFactory(Action<ILyoHttpClientFactory, IServiceProvider>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton(sp => {
                var factory = new LyoHttpClientFactory(sp, sp.GetService<ILogger<LyoHttpClientFactory>>(), sp.GetService<IRateLimitService>());
                configure?.Invoke(factory, sp);
                return factory;
            });

            services.AddSingleton<ILyoHttpClientFactory>(static sp => sp.GetRequiredService<LyoHttpClientFactory>());
            return services;
        }

        /// <summary>
        /// Binds <see cref="LyoHttpClientFactoryOptions.SectionName" /> and registers <see cref="ILyoHttpClientFactory" />. A missing section leaves the factory empty.
        /// <paramref name="configure" /> runs after the bind, so a <c>Register</c> for the same key replaces that site's static bearer with a delegate.
        /// </summary>
        public IServiceCollection AddLyoHttpClientFactoryFromConfiguration(
            IConfiguration configuration,
            Action<ILyoHttpClientFactory, IServiceProvider>? configure = null,
            string? sectionName = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            sectionName ??= LyoHttpClientFactoryOptions.SectionName;
            services.AddSingleton(sp => {
                var bound = new LyoHttpClientFactoryOptions();
                configuration.GetSection(sectionName).Bind(bound);
                var factory = new LyoHttpClientFactory(sp, sp.GetService<ILogger<LyoHttpClientFactory>>(), sp.GetService<IRateLimitService>());
                factory.LoadConfiguration(bound);
                configure?.Invoke(factory, sp);
                factory.Materialize();
                return factory;
            });

            services.AddSingleton<ILyoHttpClientFactory>(static sp => sp.GetRequiredService<LyoHttpClientFactory>());
            return services;
        }
    }

    /// <summary>
    /// Sets the primary handler to a new <see cref="LyoHttpClientHandler" /> from <typeparamref name="TOptions" /> in DI. Register via factory (the default here), never as a
    /// singleton. A later <c>ConfigurePrimaryHttpMessageHandler</c> replaces it and drops auto-decompress unless the replacement turns it back on.
    /// </summary>
    public static IHttpClientBuilder UseLyoHttpClientHandler<TOptions>(this IHttpClientBuilder builder)
        where TOptions : LyoHttpClientOptions
    {
        ArgumentHelpers.ThrowIfNull(builder);
        return builder.ConfigurePrimaryHttpMessageHandler(provider => {
            var options = provider.GetRequiredService<IOptions<TOptions>>().Value;
            return new LyoHttpClientHandler(options);
        });
    }

    /// <summary>Copies supported Accept-Encoding tokens onto the client.</summary>
    public static void ApplyAcceptEncodingHeaders(HttpClient client, IEnumerable<string>? encodings) => LyoHttpClient.ApplyAcceptEncodingHeaders(client, encodings);

    private static IHttpClientBuilder AddLyoHttpClientCore<TClient, TOptions>(
        this IServiceCollection services,
        TOptions? options,
        Func<IServiceProvider, HttpClient, TOptions, TClient>? factory,
        string? clientName)
        where TClient : class where TOptions : LyoHttpClientOptions, new()
    {
        clientName ??= typeof(TClient).Name;
        if (options is not null) {
            options.Validate();
            services.TryAddSingleton(Options.Create(options));
            services.TryAddSingleton(options);
        }
        else {
            services.AddOptions<TOptions>();
            services.PostConfigure<TOptions>(static o => o.Validate());
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<TOptions>>().Value);
        }

        services.TryAddSingleton<LyoHttpRateLimitGateCache<TOptions>>(sp => {
            var resolved = sp.GetRequiredService<IOptions<TOptions>>().Value;
            return new(new(resolved.RateLimit));
        });

        var builder = services.AddHttpClient(clientName);
        builder.AddTypedClient((http, sp) => {
            var resolved = sp.GetRequiredService<IOptions<TOptions>>().Value;
            if (factory != null)
                return factory(sp, http, resolved);

            return ActivatorUtilities.CreateInstance<TClient>(sp, http, resolved);
        });

        builder.ConfigureHttpClient((provider, client) => {
            var resolved = provider.GetRequiredService<IOptions<TOptions>>().Value;
            ApplyAcceptEncodingHeaders(client, resolved.AcceptEncodings);
            if (!string.IsNullOrWhiteSpace(resolved.BaseUrl))
                client.BaseAddress = new(resolved.BaseUrl!.TrimEnd('/') + "/");

            if (resolved.UserAgent.Enabled && resolved.UserAgent.Rotation is LyoHttpUserAgentRotation.None or LyoHttpUserAgentRotation.PerClient) {
                var agent = resolved.UserAgent.Resolve(clientName.GetHashCode());
                client.DefaultRequestHeaders.Remove(HttpHeaderInfo.UserAgent);
                client.DefaultRequestHeaders.TryAddWithoutValidation(HttpHeaderInfo.UserAgent, agent);
            }
        });

        builder.UseLyoHttpClientHandler<TOptions>();
        AttachPipeline<TOptions>(builder);
        return builder;
    }

    private static string ResolveSectionName<TOptions>()
        where TOptions : LyoHttpClientOptions, new()
    {
        var field = typeof(TOptions).GetField("SectionName", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        return field?.GetValue(null) as string ?? LyoHttpClientOptions.SectionName;
    }

    private static void AttachPipeline<TOptions>(IHttpClientBuilder builder)
        where TOptions : LyoHttpClientOptions
    {
        // First added = outermost: observe → metrics → headers → rate limit → timing (innermost, next to primary).
        builder.AddHttpMessageHandler(sp => new LyoHttpObserveHandler());
        builder.AddHttpMessageHandler(sp => new LyoHttpMetricsHandler(sp.GetService<IMetrics>()));
        builder.AddHttpMessageHandler(sp => {
            var options = sp.GetRequiredService<IOptions<TOptions>>().Value;
            return new LyoHttpHeaderHandler(options, sp);
        });

        builder.AddHttpMessageHandler(sp => {
            var options = sp.GetRequiredService<IOptions<TOptions>>().Value;
            IRateLimitService? limiter = null;
            if (options.RateLimit.Enabled) {
                limiter = sp.GetService<IRateLimitService>();
                if (limiter is null)
                    throw new InvalidOperationException("Rate limiting is enabled but IRateLimitService is not registered. Call AddLocalRateLimit or AddRedisRateLimit.");
            }

            var gate = sp.GetRequiredService<LyoHttpRateLimitGateCache<TOptions>>().Gate;
            var policy = typeof(TOptions).FullName ?? typeof(TOptions).Name;
            return new LyoHttpRateLimitHandler(
                options.RateLimit, limiter, policy, gate, sp.GetService<IMetrics>(), sp.GetService<ILoggerFactory>()?.CreateLogger("Lyo.Http.Client.RateLimit"));
        });

        builder.AddHttpMessageHandler(sp => {
            var options = sp.GetRequiredService<IOptions<TOptions>>().Value;
            return new LyoHttpTimingHandler(options.Timing, sp.GetService<ILoggerFactory>()?.CreateLogger("Lyo.Http.Client.Timing"));
        });
    }
}

/// <summary>Singleton cache so transient handlers share one waiter queue and concurrency cap per options type.</summary>
public sealed class LyoHttpRateLimitGateCache<TOptions>(LyoHttpRateLimitGate gate)
    where TOptions : LyoHttpClientOptions
{
    /// <summary>Shared gate.</summary>
    public LyoHttpRateLimitGate Gate { get; } = gate;
}