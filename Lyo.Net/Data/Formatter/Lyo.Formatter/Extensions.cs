using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using SmartFormat;

namespace Lyo.Formatter;

public static class Extensions
{
    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds formatter service to the service collection.</summary>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddFormatterService()
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            services.AddSingleton<FormatterService>();
            services.AddSingleton<IFormatterService>(provider => provider.GetRequiredService<FormatterService>());
            return services;
        }

        /// <summary>Adds formatter service to the service collection with a custom SmartFormatter.</summary>
        /// <param name="formatterFactory">Factory that creates the SmartFormatter. Receives the service provider.</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddFormatterService(Func<IServiceProvider, SmartFormatter> formatterFactory)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(formatterFactory, nameof(formatterFactory));
            services.AddSingleton<FormatterService>(provider => new(formatterFactory(provider)));
            services.AddSingleton<IFormatterService>(provider => provider.GetRequiredService<FormatterService>());
            return services;
        }
    }
}