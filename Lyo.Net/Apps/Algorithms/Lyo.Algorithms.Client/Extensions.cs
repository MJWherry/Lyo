using Lyo.Api.Client;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Algorithms.Client;

/// <summary>DI helpers for <see cref="AlgorithmClient" />.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="IAlgorithmClient" /> on the <typeparamref name="TApiClient" /> already in the container.</summary>
        /// <typeparam name="TApiClient">Registered API client type used for catalog calls.</typeparam>
        /// <param name="configure">Sets route options.</param>
        public IServiceCollection AddAlgorithmClient<TApiClient>(Action<AlgorithmClientOptions> configure)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new AlgorithmClientOptions();
            configure(options);
            return services.AddAlgorithmClient<TApiClient>(options);
        }

        /// <summary>Adds <see cref="IAlgorithmClient" /> on the <typeparamref name="TApiClient" /> already in the container.</summary>
        /// <typeparam name="TApiClient">Registered API client type used for catalog calls.</typeparam>
        /// <param name="options">Route options. Null keeps routes relative to the client's base address.</param>
        public IServiceCollection AddAlgorithmClient<TApiClient>(AlgorithmClientOptions? options = null)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            options?.Validate();
            services.AddSingleton<IAlgorithmClient>(sp => new AlgorithmClient(sp.GetRequiredService<TApiClient>(), options));
            return services;
        }

        /// <summary>Adds <see cref="IAlgorithmClient" /> on the <typeparamref name="TApiClient" /> already in the container, binding route options from configuration.</summary>
        /// <typeparam name="TApiClient">Registered API client type used for catalog calls.</typeparam>
        /// <param name="configuration">Configuration root (for example <c>builder.Configuration</c>).</param>
        /// <param name="configSectionName">Section to bind. Defaults to <see cref="AlgorithmClientOptions.SectionName" />.</param>
        public IServiceCollection AddAlgorithmClientFromConfiguration<TApiClient>(IConfiguration configuration, string configSectionName = AlgorithmClientOptions.SectionName)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new AlgorithmClientOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddAlgorithmClient<TApiClient>(options);
        }

        /// <summary>Adds <see cref="IAlgorithmClient" /> on an API client resolved by <paramref name="apiClientFactory" />.</summary>
        /// <param name="apiClientFactory">Resolves the API client when it is not registered under its own type.</param>
        /// <param name="configure">Sets route options.</param>
        public IServiceCollection AddAlgorithmClient(Func<IServiceProvider, IApiClient> apiClientFactory, Action<AlgorithmClientOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new AlgorithmClientOptions();
            configure(options);
            return services.AddAlgorithmClient(apiClientFactory, options);
        }

        /// <summary>Adds <see cref="IAlgorithmClient" /> on an API client resolved by <paramref name="apiClientFactory" />.</summary>
        /// <param name="apiClientFactory">Resolves the API client when it is not registered under its own type.</param>
        /// <param name="options">Route options. Null keeps routes relative to the client's base address.</param>
        public IServiceCollection AddAlgorithmClient(Func<IServiceProvider, IApiClient> apiClientFactory, AlgorithmClientOptions? options = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(apiClientFactory);
            options?.Validate();
            services.AddSingleton<IAlgorithmClient>(sp => new AlgorithmClient(apiClientFactory(sp), options));
            return services;
        }

        /// <summary>Adds <see cref="IAlgorithmClient" /> on an API client resolved by <paramref name="apiClientFactory" />, binding route options from configuration.</summary>
        /// <param name="apiClientFactory">Resolves the API client when it is not registered under its own type.</param>
        /// <param name="configuration">Configuration root (for example <c>builder.Configuration</c>).</param>
        /// <param name="configSectionName">Section to bind. Defaults to <see cref="AlgorithmClientOptions.SectionName" />.</param>
        public IServiceCollection AddAlgorithmClientFromConfiguration(
            Func<IServiceProvider, IApiClient> apiClientFactory,
            IConfiguration configuration,
            string configSectionName = AlgorithmClientOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(apiClientFactory);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new AlgorithmClientOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddAlgorithmClient(apiClientFactory, options);
        }
    }
}
