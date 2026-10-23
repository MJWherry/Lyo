using Lyo.Api.Client;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Billing.Client;

/// <summary>DI helpers for <see cref="BillingClient" />.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="IBillingClient" /> on the <typeparamref name="TApiClient" /> already in the container.</summary>
        /// <typeparam name="TApiClient">Registered API client type used for billing calls.</typeparam>
        /// <param name="configure">Sets route options.</param>
        public IServiceCollection AddBillingClient<TApiClient>(Action<BillingClientOptions> configure)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new BillingClientOptions();
            configure(options);
            return services.AddBillingClient<TApiClient>(options);
        }

        /// <summary>Adds <see cref="IBillingClient" /> on the <typeparamref name="TApiClient" /> already in the container.</summary>
        /// <typeparam name="TApiClient">Registered API client type used for billing calls.</typeparam>
        /// <param name="options">Route options. Null keeps routes relative to the client's base address.</param>
        public IServiceCollection AddBillingClient<TApiClient>(BillingClientOptions? options = null)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            options?.Validate();
            services.AddSingleton<IBillingClient>(sp => new BillingClient(sp.GetRequiredService<TApiClient>(), options));
            return services;
        }

        /// <summary>Adds <see cref="IBillingClient" /> on the <typeparamref name="TApiClient" /> already in the container, binding route options from configuration.</summary>
        /// <typeparam name="TApiClient">Registered API client type used for billing calls.</typeparam>
        /// <param name="configuration">Configuration root (for example <c>builder.Configuration</c>).</param>
        /// <param name="configSectionName">Section to bind. Defaults to <see cref="BillingClientOptions.SectionName" />.</param>
        public IServiceCollection AddBillingClientFromConfiguration<TApiClient>(IConfiguration configuration, string configSectionName = BillingClientOptions.SectionName)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new BillingClientOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddBillingClient<TApiClient>(options);
        }

        /// <summary>Adds <see cref="IBillingClient" /> on an API client resolved by <paramref name="apiClientFactory" />.</summary>
        /// <param name="apiClientFactory">Resolves the API client when it is not registered under its own type.</param>
        /// <param name="configure">Sets route options.</param>
        public IServiceCollection AddBillingClient(Func<IServiceProvider, IApiClient> apiClientFactory, Action<BillingClientOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new BillingClientOptions();
            configure(options);
            return services.AddBillingClient(apiClientFactory, options);
        }

        /// <summary>Adds <see cref="IBillingClient" /> on an API client resolved by <paramref name="apiClientFactory" />.</summary>
        /// <param name="apiClientFactory">Resolves the API client when it is not registered under its own type.</param>
        /// <param name="options">Route options. Null keeps routes relative to the client's base address.</param>
        public IServiceCollection AddBillingClient(Func<IServiceProvider, IApiClient> apiClientFactory, BillingClientOptions? options = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(apiClientFactory);
            options?.Validate();
            services.AddSingleton<IBillingClient>(sp => new BillingClient(apiClientFactory(sp), options));
            return services;
        }

        /// <summary>Adds <see cref="IBillingClient" /> on an API client resolved by <paramref name="apiClientFactory" />, binding route options from configuration.</summary>
        /// <param name="apiClientFactory">Resolves the API client when it is not registered under its own type.</param>
        /// <param name="configuration">Configuration root (for example <c>builder.Configuration</c>).</param>
        /// <param name="configSectionName">Section to bind. Defaults to <see cref="BillingClientOptions.SectionName" />.</param>
        public IServiceCollection AddBillingClientFromConfiguration(
            Func<IServiceProvider, IApiClient> apiClientFactory,
            IConfiguration configuration,
            string configSectionName = BillingClientOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new BillingClientOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddBillingClient(apiClientFactory, options);
        }
    }
}