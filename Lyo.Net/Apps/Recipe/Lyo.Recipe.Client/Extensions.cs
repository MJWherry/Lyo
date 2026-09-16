using Lyo.Api.Client;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Recipe.Client;

/// <summary>DI helpers for <see cref="RecipeClient"/>.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="IRecipeClient"/> on the <typeparamref name="TApiClient"/> already in the container.</summary>
        public IServiceCollection AddRecipeClient<TApiClient>(Action<RecipeClientOptions> configure)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new RecipeClientOptions();
            configure(options);
            return services.AddRecipeClient<TApiClient>(options);
        }

        /// <summary>Adds <see cref="IRecipeClient"/> on the <typeparamref name="TApiClient"/> already in the container.</summary>
        public IServiceCollection AddRecipeClient<TApiClient>(RecipeClientOptions? options = null)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            var resolved = RegisterOptions(services, options);
            services.AddSingleton<IRecipeClient>(sp => new RecipeClient(sp.GetRequiredService<TApiClient>(), resolved));
            return services;
        }

        /// <summary>Adds <see cref="IRecipeClient"/> on the <typeparamref name="TApiClient"/> already in the container, binding route options from configuration.</summary>
        public IServiceCollection AddRecipeClientFromConfiguration<TApiClient>(IConfiguration configuration, string configSectionName = RecipeClientOptions.SectionName)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new RecipeClientOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddRecipeClient<TApiClient>(options);
        }

        /// <summary>Adds <see cref="IRecipeClient"/> on an API client resolved by <paramref name="apiClientFactory"/>.</summary>
        public IServiceCollection AddRecipeClient(Func<IServiceProvider, IApiClient> apiClientFactory, Action<RecipeClientOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new RecipeClientOptions();
            configure(options);
            return services.AddRecipeClient(apiClientFactory, options);
        }

        /// <summary>Adds <see cref="IRecipeClient"/> on an API client resolved by <paramref name="apiClientFactory"/>.</summary>
        public IServiceCollection AddRecipeClient(Func<IServiceProvider, IApiClient> apiClientFactory, RecipeClientOptions? options = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(apiClientFactory);
            var resolved = RegisterOptions(services, options);
            services.AddSingleton<IRecipeClient>(sp => new RecipeClient(apiClientFactory(sp), resolved));
            return services;
        }

        /// <summary>Adds <see cref="IRecipeClient"/> on an API client resolved by <paramref name="apiClientFactory"/>, binding route options from configuration.</summary>
        public IServiceCollection AddRecipeClientFromConfiguration(
            Func<IServiceProvider, IApiClient> apiClientFactory,
            IConfiguration configuration,
            string configSectionName = RecipeClientOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(apiClientFactory);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new RecipeClientOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddRecipeClient(apiClientFactory, options);
        }
    }

    private static RecipeClientOptions RegisterOptions(IServiceCollection services, RecipeClientOptions? options)
    {
        var resolved = options ?? new RecipeClientOptions();
        resolved.Validate();
        services.TryAddSingleton(Options.Create(resolved));
        services.TryAddSingleton(resolved);
        return resolved;
    }
}
