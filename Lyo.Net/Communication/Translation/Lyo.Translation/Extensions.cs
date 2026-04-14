using Lyo.Exceptions;
using Lyo.Translation.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Translation;

/// <summary>Extension methods for translation service registration.</summary>
public static class Extensions
{
    /// <summary>Adds a translation service to the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for the service options.</param>
    /// <typeparam name="TService">The translation service implementation type.</typeparam>
    /// <typeparam name="TOptions">The translation service options type.</typeparam>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTranslationService<TService, TOptions>(this IServiceCollection services, Action<TOptions>? configure = null)
        where TService : class, ITranslationService where TOptions : TranslationServiceOptions, new()
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        var options = new TOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddSingleton<TService>();
        services.AddSingleton<ITranslationService>(provider => provider.GetRequiredService<TService>());
        return services;
    }

    /// <summary>Adds a translation service to the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The translation service options.</param>
    /// <typeparam name="TService">The translation service implementation type.</typeparam>
    /// <typeparam name="TOptions">The translation service options type.</typeparam>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTranslationService<TService, TOptions>(this IServiceCollection services, TOptions options)
        where TService : class, ITranslationService where TOptions : TranslationServiceOptions
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddSingleton(options);
        services.AddSingleton<TService>();
        services.AddSingleton<ITranslationService>(provider => provider.GetRequiredService<TService>());
        return services;
    }
}