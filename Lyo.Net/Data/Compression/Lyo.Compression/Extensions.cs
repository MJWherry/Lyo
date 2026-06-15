using Lyo.Compression.Compressors;
using Lyo.Compression.Models;
using Lyo.Compression.Policy;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lyo.Compression;

/// <summary>
/// Registers <see cref="CompressionService" /> with Microsoft.Extensions.DependencyInjection. Use <see cref="Extensions.AddDefaultCompressionService{TConcrete}" /> for
/// unkeyed <see cref="ICompressionService" />.
/// </summary>
public static class Extensions
{
    private static CompressionService CreateCompressionService(IServiceProvider sp, CompressionServiceOptions options, Func<ICompressionResolver>? resolveResolver = null)
        => new(
            sp.GetServices<ICompressorFactory>(),
            sp.GetService<ILogger<CompressionService>>(),
            options,
            sp.GetService<IMetrics>(),
            resolveResolver ?? (() => sp.GetRequiredService<ICompressionResolver>()),
            sp.GetService<ICompressionAlgorithmSelector>());

    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="CompressionServiceOptions" /> (defaults), built-in compressor factories, <see cref="CompressionService" />, and <see cref="ICompressionResolver" /> as singletons.</summary>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddCompressionService()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddBuiltInCompressors();
            services.AddSingleton(_ => new CompressionServiceOptions());
            services.AddCompressionResolver();
            services.AddSingleton(sp => CreateCompressionService(sp, sp.GetRequiredService<CompressionServiceOptions>()));
            return services;
        }

        /// <summary>Adds <see cref="CompressionService" /> with a singleton <see cref="CompressionServiceOptions" /> configured once via <paramref name="configure" />.</summary>
        /// <param name="configure">Mutates options once at registration time.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddCompressionService(Action<CompressionServiceOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddBuiltInCompressors();
            services.AddSingleton(_ => {
                var options = new CompressionServiceOptions();
                configure(options);
                return options;
            });

            services.AddCompressionResolver();
            services.AddSingleton(sp => CreateCompressionService(sp, sp.GetRequiredService<CompressionServiceOptions>()));
            return services;
        }

        /// <summary>
        /// Binds <paramref name="configuration" /><c>.</c><paramref name="configSectionName" /> to a singleton <see cref="CompressionServiceOptions" />, then registers
        /// <see cref="CompressionService" />.
        /// </summary>
        /// <param name="configuration">Application configuration root.</param>
        /// <param name="configSectionName">Section containing <see cref="CompressionServiceOptions" /> keys (default <c>CompressionService</c>).</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddCompressionServiceFromConfiguration(IConfiguration configuration, string configSectionName = "CompressionService")
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            services.AddBuiltInCompressors();
            services.AddSingleton(_ => {
                var options = new CompressionServiceOptions();
                configuration.GetSection(configSectionName).Bind(options);
                return options;
            });

            services.AddCompressionResolver();
            services.AddSingleton(sp => CreateCompressionService(sp, sp.GetRequiredService<CompressionServiceOptions>()));
            return services;
        }

        /// <summary>Registers <see cref="ICompressionResolver" /> backed by <see cref="CompressionService" />. Idempotent; already invoked by <see cref="AddCompressionService" />.</summary>
        public IServiceCollection AddCompressionResolver()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddSingleton<ICompressionResolver>(sp => sp.GetRequiredService<CompressionService>());
            return services;
        }

        /// <summary>
        /// Binds <paramref name="policySectionPath" /> to <see cref="CompressionPolicyOptions" /> and registers <see cref="ICompressionAlgorithmSelector" /> as
        /// <see cref="CompressionPolicyAlgorithmSelector" />.
        /// </summary>
        /// <param name="configuration">Application configuration root.</param>
        /// <param name="policySectionPath">Section path (default <c>CompressionOptions:Policy</c>).</param>
        public IServiceCollection AddCompressionPolicySelector(IConfiguration configuration, string policySectionPath = "CompressionOptions:Policy")
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(policySectionPath);
            services.AddOptions<CompressionPolicyOptions>().Bind(configuration.GetSection(policySectionPath));
            services.TryAddSingleton<ICompressionAlgorithmSelector, CompressionPolicyAlgorithmSelector>();
            return services;
        }

        /// <summary>Registers <see cref="CompressionPolicyAlgorithmSelector" /> with programmatic <see cref="CompressionPolicyOptions" /> configuration.</summary>
        public IServiceCollection AddCompressionPolicySelector(Action<CompressionPolicyOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            if (configure != null)
                services.AddOptions<CompressionPolicyOptions>().Configure(configure);
            else
                services.AddOptions<CompressionPolicyOptions>();

            services.TryAddSingleton<ICompressionAlgorithmSelector, CompressionPolicyAlgorithmSelector>();
            return services;
        }

        /// <summary>Maps unkeyed <see cref="ICompressionService" /> to an already-registered concrete singleton.</summary>
        /// <typeparam name="TConcrete">Concrete compression service type registered earlier in the same service collection.</typeparam>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddDefaultCompressionService<TConcrete>()
            where TConcrete : class, ICompressionService
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<ICompressionService>(sp => sp.GetRequiredService<TConcrete>());
            return services;
        }

        /// <summary>
        /// Registers keyed <see cref="CompressionServiceOptions" />, <see cref="CompressionService" />, and <see cref="ICompressionService" /> for multi-tenant or multi-policy
        /// scenarios.
        /// </summary>
        /// <param name="keyedServiceName">Non-empty DI key shared across the three registrations.</param>
        /// <param name="configure">Optional per-key options mutation.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddCompressionServiceKeyed(string keyedServiceName, Action<CompressionServiceOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyedServiceName);
            services.AddBuiltInCompressors();
            services.AddKeyedSingleton<CompressionServiceOptions>(
                keyedServiceName, (_, _) => {
                    var options = new CompressionServiceOptions();
                    configure?.Invoke(options);
                    return options;
                });

            services.AddKeyedSingleton<CompressionService>(
                keyedServiceName,
                (provider, key) => CreateCompressionService(
                    provider,
                    provider.GetRequiredKeyedService<CompressionServiceOptions>(key),
                    () => provider.GetRequiredKeyedService<CompressionService>(key)));

            services.AddKeyedSingleton<ICompressionService>(keyedServiceName, (provider, _) => provider.GetRequiredKeyedService<CompressionService>(keyedServiceName));
            return services;
        }

        /// <summary>
        /// Registers the built-in <see cref="ICompressorFactory" /> implementations shipped in the base <c>Lyo.Compression</c> package (GZip, Deflate, and on net10+ Brotli/ZLib).
        /// Idempotent.
        /// </summary>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddBuiltInCompressors()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ICompressorFactory, GZipCompressorFactory>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ICompressorFactory, DeflateCompressorFactory>());
#if !NETSTANDARD2_0
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICompressorFactory, BrotliCompressorFactory>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICompressorFactory, ZLibCompressorFactory>());
#endif
            return services;
        }
    }
}
