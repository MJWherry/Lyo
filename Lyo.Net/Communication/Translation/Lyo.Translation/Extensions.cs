using Lyo.Exceptions;
using Lyo.Translation.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Translation;

/// <summary>Extension methods for translation service registration.</summary>
public static class Extensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds a translation service to the service collection.</summary>
        /// <param name="configure">Optional configuration action for the service options.</param>
        /// <typeparam name="TService">The translation service implementation type.</typeparam>
        /// <typeparam name="TOptions">The translation service options type.</typeparam>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddTranslationService<TService, TOptions>(Action<TOptions>? configure = null)
            where TService : class, ITranslationService where TOptions : TranslationServiceOptions, new()
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new TOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);
            services.AddSingleton<TService>();
            services.AddSingleton<ITranslationService>(provider => provider.GetRequiredService<TService>());
            return services;
        }

        /// <summary>Adds a translation service to the service collection.</summary>
        /// <param name="options">The translation service options.</param>
        /// <typeparam name="TService">The translation service implementation type.</typeparam>
        /// <typeparam name="TOptions">The translation service options type.</typeparam>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddTranslationService<TService, TOptions>(TOptions options)
            where TService : class, ITranslationService where TOptions : TranslationServiceOptions
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddSingleton(options);
            services.AddSingleton<TService>();
            services.AddSingleton<ITranslationService>(provider => provider.GetRequiredService<TService>());
            return services;
        }
    }
}