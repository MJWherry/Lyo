using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Pdf.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Pdf;

/// <summary>Extension methods for registering PDF service with dependency injection.</summary>
public static class Extensions
{
    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds PDF service to the service collection with default options.</summary>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddPdfService()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<PdfServiceOptions>(_ => new());
            services.AddScoped<PdfService>(provider => {
                var metrics = provider.GetService<IMetrics>();
                var options = provider.GetRequiredService<PdfServiceOptions>();
                var httpClientFactory = provider.GetService<IHttpClientFactory>();
                var httpClient = httpClientFactory?.CreateClient(nameof(PdfService));
                return new(provider.GetRequiredService<ILoggerFactory>(), metrics, httpClient, options);
            });

            services.AddScoped<IPdfService>(provider => provider.GetRequiredService<PdfService>());
            return services;
        }

        /// <summary>Adds PDF service to the service collection with custom options.</summary>
        /// <param name="configure">Action to configure the options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddPdfService(Action<PdfServiceOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddSingleton<PdfServiceOptions>(_ => {
                var options = new PdfServiceOptions();
                configure(options);
                return options;
            });

            services.AddScoped<PdfService>(provider => {
                var metrics = provider.GetService<IMetrics>();
                var options = provider.GetRequiredService<PdfServiceOptions>();
                var httpClientFactory = provider.GetService<IHttpClientFactory>();
                var httpClient = httpClientFactory?.CreateClient(nameof(PdfService));
                return new(provider.GetRequiredService<ILoggerFactory>(), metrics, httpClient, options);
            });

            services.AddScoped<IPdfService>(provider => provider.GetRequiredService<PdfService>());
            return services;
        }

        /// <summary>Adds PDF service to the service collection using configuration binding.</summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="configSectionName">The configuration section name (defaults to "PdfServiceOptions")</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddPdfServiceFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PdfServiceOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            services.AddSingleton<PdfServiceOptions>(_ => {
                var options = new PdfServiceOptions();
                var section = configuration.GetSection(configSectionName);
                if (section.Exists())
                    section.Bind(options);

                return options;
            });

            services.AddScoped<PdfService>(provider => {
                var metrics = provider.GetService<IMetrics>();
                var options = provider.GetRequiredService<PdfServiceOptions>();
                var httpClientFactory = provider.GetService<IHttpClientFactory>();
                var httpClient = httpClientFactory?.CreateClient(nameof(PdfService));
                return new(provider.GetRequiredService<ILoggerFactory>(), metrics, httpClient, options);
            });

            services.AddScoped<IPdfService>(provider => provider.GetRequiredService<PdfService>());
            return services;
        }

        /// <summary>Adds PDF service to the service collection with custom HttpClient.</summary>
        /// <param name="httpClientFactory">Factory function to create HttpClient instance</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddPdfService(Func<IServiceProvider, HttpClient> httpClientFactory)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(httpClientFactory);
            services.AddSingleton<PdfServiceOptions>(_ => new());
            services.AddScoped<PdfService>(provider => {
                var metrics = provider.GetService<IMetrics>();
                var options = provider.GetRequiredService<PdfServiceOptions>();
                var httpClient = httpClientFactory(provider);
                return new(provider.GetRequiredService<ILoggerFactory>(), metrics, httpClient, options);
            });

            services.AddScoped<IPdfService>(provider => provider.GetRequiredService<PdfService>());
            return services;
        }

        /// <summary>Adds PDF service to the service collection using a named HttpClient.</summary>
        /// <param name="httpClientName">The name of the HttpClient to use</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddPdfService(string httpClientName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(httpClientName);
            services.AddHttpClient(httpClientName);
            services.AddSingleton<PdfServiceOptions>(_ => new());
            services.AddScoped<PdfService>(provider => {
                var metrics = provider.GetService<IMetrics>();
                var options = provider.GetRequiredService<PdfServiceOptions>();
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(httpClientName);
                return new(provider.GetRequiredService<ILoggerFactory>(), metrics, httpClient, options);
            });

            services.AddScoped<IPdfService>(provider => provider.GetRequiredService<PdfService>());
            return services;
        }

        /// <summary>Adds PDF service to the service collection as a keyed service.</summary>
        /// <param name="keyedServiceName">The key name for the keyed service</param>
        /// <param name="httpClientFactory">Optional factory function to create HttpClient instance</param>
        /// <param name="configure">Optional action to configure the options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddPdfServiceKeyed(
            string keyedServiceName,
            Func<IServiceProvider, HttpClient>? httpClientFactory = null,
            Action<PdfServiceOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyedServiceName);
            services.AddKeyedSingleton<PdfServiceOptions>(
                keyedServiceName, (_, _) => {
                    var options = new PdfServiceOptions();
                    configure?.Invoke(options);
                    return options;
                });

            services.AddKeyedScoped<PdfService>(
                keyedServiceName, (provider, _) => {
                    var metrics = provider.GetService<IMetrics>();
                    var options = provider.GetRequiredKeyedService<PdfServiceOptions>(keyedServiceName);
                    HttpClient? httpClient = null;
                    if (httpClientFactory != null)
                        httpClient = httpClientFactory(provider);

                    return new(provider.GetRequiredService<ILoggerFactory>(), metrics, httpClient, options);
                });

            services.AddKeyedScoped<IPdfService>(keyedServiceName, (provider, _) => provider.GetRequiredKeyedService<PdfService>(keyedServiceName));
            return services;
        }
    }
}