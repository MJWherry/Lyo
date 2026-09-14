using Lyo.Api.Client;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Reporting.Client;

/// <summary>DI helpers for <see cref="ReportingClient" />.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="IReportingClient" /> on the <typeparamref name="TApiClient" /> already in the container.</summary>
        /// <typeparam name="TApiClient">Registered API client type used for reporting calls.</typeparam>
        /// <param name="configure">Sets route options.</param>
        public IServiceCollection AddReportingClient<TApiClient>(Action<ReportingClientOptions> configure)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new ReportingClientOptions();
            configure(options);
            return services.AddReportingClient<TApiClient>(options);
        }

        /// <summary>Adds <see cref="IReportingClient" /> on the <typeparamref name="TApiClient" /> already in the container.</summary>
        /// <typeparam name="TApiClient">Registered API client type used for reporting calls.</typeparam>
        /// <param name="options">Route options. Null keeps routes relative to the client's base address.</param>
        public IServiceCollection AddReportingClient<TApiClient>(ReportingClientOptions? options = null)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            options?.Validate();
            services.AddSingleton<IReportingClient>(sp => new ReportingClient(sp.GetRequiredService<TApiClient>(), options));
            return services;
        }

        /// <summary>Adds <see cref="IReportingClient" /> on the <typeparamref name="TApiClient" /> already in the container, binding route options from configuration.</summary>
        /// <typeparam name="TApiClient">Registered API client type used for reporting calls.</typeparam>
        /// <param name="configuration">Configuration root (for example <c>builder.Configuration</c>).</param>
        /// <param name="configSectionName">Section to bind. Defaults to <see cref="ReportingClientOptions.SectionName" />.</param>
        public IServiceCollection AddReportingClientFromConfiguration<TApiClient>(IConfiguration configuration, string configSectionName = ReportingClientOptions.SectionName)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new ReportingClientOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddReportingClient<TApiClient>(options);
        }

        /// <summary>Adds <see cref="IReportingClient" /> on an API client resolved by <paramref name="apiClientFactory" />.</summary>
        /// <param name="apiClientFactory">Resolves the API client when it is not registered under its own type.</param>
        /// <param name="configure">Sets route options.</param>
        public IServiceCollection AddReportingClient(Func<IServiceProvider, IApiClient> apiClientFactory, Action<ReportingClientOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new ReportingClientOptions();
            configure(options);
            return services.AddReportingClient(apiClientFactory, options);
        }

        /// <summary>Adds <see cref="IReportingClient" /> on an API client resolved by <paramref name="apiClientFactory" />.</summary>
        /// <param name="apiClientFactory">Resolves the API client when it is not registered under its own type.</param>
        /// <param name="options">Route options. Null keeps routes relative to the client's base address.</param>
        public IServiceCollection AddReportingClient(Func<IServiceProvider, IApiClient> apiClientFactory, ReportingClientOptions? options = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(apiClientFactory);
            options?.Validate();
            services.AddSingleton<IReportingClient>(sp => new ReportingClient(apiClientFactory(sp), options));
            return services;
        }

        /// <summary>Adds <see cref="IReportingClient" /> on an API client resolved by <paramref name="apiClientFactory" />, binding route options from configuration.</summary>
        /// <param name="apiClientFactory">Resolves the API client when it is not registered under its own type.</param>
        /// <param name="configuration">Configuration root (for example <c>builder.Configuration</c>).</param>
        /// <param name="configSectionName">Section to bind. Defaults to <see cref="ReportingClientOptions.SectionName" />.</param>
        public IServiceCollection AddReportingClientFromConfiguration(
            Func<IServiceProvider, IApiClient> apiClientFactory,
            IConfiguration configuration,
            string configSectionName = ReportingClientOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new ReportingClientOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddReportingClient(apiClientFactory, options);
        }
    }
}
