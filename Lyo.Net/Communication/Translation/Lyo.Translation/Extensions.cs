using Lyo.Exceptions;
using Lyo.Translation.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Translation;

/// <summary>DI helpers that register a translation service.</summary>
public static class Extensions
{
    /// <param name="services">Service collection to add registrations to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers a translation service on the collection.</summary>
        /// <param name="configure">Optional callback that mutates the service options.</param>
        /// <typeparam name="TService">Concrete translation service type.</typeparam>
        /// <typeparam name="TOptions">Options type for that service.</typeparam>
        /// <returns>The same collection so further calls can chain.</returns>
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

        /// <summary>Registers a translation service using a prepared options instance.</summary>
        /// <param name="options">Options already constructed for the service.</param>
        /// <typeparam name="TService">Concrete translation service type.</typeparam>
        /// <typeparam name="TOptions">Options type for that service.</typeparam>
        /// <returns>The same collection so further calls can chain.</returns>
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