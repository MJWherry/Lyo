using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Pdf.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Pdf;

/// <summary>DI helpers that register <see cref="PdfService" /> and <see cref="IPdfService" />.</summary>
public static class Extensions
{
    /// <param name="services">Service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds the PDF service with built-in option defaults.</summary>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddPdfService()
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new PdfServiceOptions();
            options.Validate();
            services.AddSingleton(options);
            services.AddSingleton<PdfService>(provider => {
                var metrics = provider.GetService<IMetrics>();
                var options = provider.GetRequiredService<PdfServiceOptions>();
                var httpClientFactory = provider.GetService<IHttpClientFactory>();
                var httpClient = httpClientFactory?.CreateClient(nameof(PdfService));
                return new(provider.GetRequiredService<ILoggerFactory>(), metrics, httpClient, options);
            });

            services.AddSingleton<IPdfService>(provider => provider.GetRequiredService<PdfService>());
            return services;
        }

        /// <summary>Adds the PDF service configured by the given <see cref="PdfServiceOptions" /> action.</summary>
        /// <param name="configure">Callback that mutates options.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddPdfService(Action<PdfServiceOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PdfServiceOptions();
            configure(options);
            options.Validate();
            services.AddSingleton(options);

            services.AddSingleton<PdfService>(provider => {
                var metrics = provider.GetService<IMetrics>();
                var options = provider.GetRequiredService<PdfServiceOptions>();
                var httpClientFactory = provider.GetService<IHttpClientFactory>();
                var httpClient = httpClientFactory?.CreateClient(nameof(PdfService));
                return new(provider.GetRequiredService<ILoggerFactory>(), metrics, httpClient, options);
            });

            services.AddSingleton<IPdfService>(provider => provider.GetRequiredService<PdfService>());
            return services;
        }

        /// <summary>Adds the PDF service from configuration (section <see cref="PdfServiceOptions.SectionName" /> when omitted).</summary>
        /// <param name="configuration">Configuration root.</param>
        /// <param name="configSectionName">Section name; starts as "PdfServiceOptions".</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddPdfServiceFromConfiguration(IConfiguration configuration, string configSectionName = PdfServiceOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PdfServiceOptions();
            configuration.GetSection(configSectionName).Bind(options);
            options.Validate();
            services.AddSingleton(options);

            services.AddSingleton<PdfService>(provider => {
                var metrics = provider.GetService<IMetrics>();
                var options = provider.GetRequiredService<PdfServiceOptions>();
                var httpClientFactory = provider.GetService<IHttpClientFactory>();
                var httpClient = httpClientFactory?.CreateClient(nameof(PdfService));
                return new(provider.GetRequiredService<ILoggerFactory>(), metrics, httpClient, options);
            });

            services.AddSingleton<IPdfService>(provider => provider.GetRequiredService<PdfService>());
            return services;
        }

        /// <summary>Adds the PDF service using a caller-supplied HttpClient factory.</summary>
        /// <param name="httpClientFactory">Builds the HttpClient from the service provider.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddPdfService(Func<IServiceProvider, HttpClient> httpClientFactory)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(httpClientFactory);
            var options = new PdfServiceOptions();
            options.Validate();
            services.AddSingleton(options);
            services.AddSingleton<PdfService>(provider => {
                var metrics = provider.GetService<IMetrics>();
                var options = provider.GetRequiredService<PdfServiceOptions>();
                var httpClient = httpClientFactory(provider);
                return new(provider.GetRequiredService<ILoggerFactory>(), metrics, httpClient, options);
            });

            services.AddSingleton<IPdfService>(provider => provider.GetRequiredService<PdfService>());
            return services;
        }

        /// <summary>Adds the PDF service using a named HttpClient.</summary>
        /// <param name="httpClientName">HttpClient registration name.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddPdfService(string httpClientName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(httpClientName);
            services.AddHttpClient(httpClientName);
            var options = new PdfServiceOptions();
            options.Validate();
            services.AddSingleton(options);
            services.AddSingleton<PdfService>(provider => {
                var metrics = provider.GetService<IMetrics>();
                var options = provider.GetRequiredService<PdfServiceOptions>();
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(httpClientName);
                return new(provider.GetRequiredService<ILoggerFactory>(), metrics, httpClient, options);
            });

            services.AddSingleton<IPdfService>(provider => provider.GetRequiredService<PdfService>());
            return services;
        }

        /// <summary>Adds a keyed PDF service.</summary>
        /// <param name="keyedServiceName">DI key.</param>
        /// <param name="httpClientFactory">Optional HttpClient factory.</param>
        /// <param name="configure">Optional options callback.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddPdfServiceKeyed(
            string keyedServiceName,
            Func<IServiceProvider, HttpClient>? httpClientFactory = null,
            Action<PdfServiceOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyedServiceName);
            var options = new PdfServiceOptions();
            configure?.Invoke(options);
            options.Validate();
            services.AddKeyedSingleton(keyedServiceName, options);

            services.AddKeyedSingleton<PdfService>(
                keyedServiceName, (provider, _) => {
                    var metrics = provider.GetService<IMetrics>();
                    var options = provider.GetRequiredKeyedService<PdfServiceOptions>(keyedServiceName);
                    HttpClient? httpClient = null;
                    if (httpClientFactory != null)
                        httpClient = httpClientFactory(provider);

                    return new(provider.GetRequiredService<ILoggerFactory>(), metrics, httpClient, options);
                });

            services.AddKeyedSingleton<IPdfService>(keyedServiceName, (provider, _) => provider.GetRequiredKeyedService<PdfService>(keyedServiceName));
            return services;
        }
    }
}