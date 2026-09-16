using Lyo.Api.Client;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Law.Client;

/// <summary>DI helpers for <see cref="LawClient"/>.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="ILawClient"/> on the <typeparamref name="TApiClient"/> already in the container.</summary>
        public IServiceCollection AddLawClient<TApiClient>(Action<LawClientOptions> configure)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new LawClientOptions();
            configure(options);
            return services.AddLawClient<TApiClient>(options);
        }

        /// <summary>Adds <see cref="ILawClient"/> on the <typeparamref name="TApiClient"/> already in the container.</summary>
        public IServiceCollection AddLawClient<TApiClient>(LawClientOptions? options = null)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            var resolved = RegisterOptions(services, options);
            services.AddSingleton<ILawClient>(sp => new LawClient(sp.GetRequiredService<TApiClient>(), resolved));
            return services;
        }

        /// <summary>Adds <see cref="ILawClient"/> on the <typeparamref name="TApiClient"/> already in the container, binding route options from configuration.</summary>
        public IServiceCollection AddLawClientFromConfiguration<TApiClient>(IConfiguration configuration, string configSectionName = LawClientOptions.SectionName)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new LawClientOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddLawClient<TApiClient>(options);
        }

        /// <summary>Adds <see cref="ILawClient"/> on an API client resolved by <paramref name="apiClientFactory"/>.</summary>
        public IServiceCollection AddLawClient(Func<IServiceProvider, IApiClient> apiClientFactory, Action<LawClientOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new LawClientOptions();
            configure(options);
            return services.AddLawClient(apiClientFactory, options);
        }

        /// <summary>Adds <see cref="ILawClient"/> on an API client resolved by <paramref name="apiClientFactory"/>.</summary>
        public IServiceCollection AddLawClient(Func<IServiceProvider, IApiClient> apiClientFactory, LawClientOptions? options = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(apiClientFactory);
            var resolved = RegisterOptions(services, options);
            services.AddSingleton<ILawClient>(sp => new LawClient(apiClientFactory(sp), resolved));
            return services;
        }

        /// <summary>Adds <see cref="ILawClient"/> on an API client resolved by <paramref name="apiClientFactory"/>, binding route options from configuration.</summary>
        public IServiceCollection AddLawClientFromConfiguration(
            Func<IServiceProvider, IApiClient> apiClientFactory,
            IConfiguration configuration,
            string configSectionName = LawClientOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(apiClientFactory);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new LawClientOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddLawClient(apiClientFactory, options);
        }
    }

    private static LawClientOptions RegisterOptions(IServiceCollection services, LawClientOptions? options)
    {
        var resolved = options ?? new LawClientOptions();
        resolved.Validate();
        services.TryAddSingleton(Options.Create(resolved));
        services.TryAddSingleton(resolved);
        return resolved;
    }
}
