using Lyo.Api.Client;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Story.Client;

/// <summary>DI helpers for <see cref="StoryClient" />.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="IStoryClient" /> on the <typeparamref name="TApiClient" /> already in the container.</summary>
        /// <typeparam name="TApiClient">Registered API client type used for story calls.</typeparam>
        /// <param name="configure">Sets route options.</param>
        public IServiceCollection AddStoryClient<TApiClient>(Action<StoryClientOptions> configure)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new StoryClientOptions();
            configure(options);
            return services.AddStoryClient<TApiClient>(options);
        }

        /// <summary>Adds <see cref="IStoryClient" /> on the <typeparamref name="TApiClient" /> already in the container.</summary>
        /// <typeparam name="TApiClient">Registered API client type used for story calls.</typeparam>
        /// <param name="options">Route options. Null keeps routes relative to the client's base address.</param>
        public IServiceCollection AddStoryClient<TApiClient>(StoryClientOptions? options = null)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            var resolved = RegisterOptions(services, options);
            services.AddSingleton<IStoryClient>(sp => new StoryClient(sp.GetRequiredService<TApiClient>(), resolved));
            return services;
        }

        /// <summary>Adds <see cref="IStoryClient" /> on the <typeparamref name="TApiClient" /> already in the container, binding route options from configuration.</summary>
        /// <typeparam name="TApiClient">Registered API client type used for story calls.</typeparam>
        /// <param name="configuration">Configuration root (for example <c>builder.Configuration</c>).</param>
        /// <param name="configSectionName">Section to bind. Defaults to <see cref="StoryClientOptions.SectionName" />.</param>
        public IServiceCollection AddStoryClientFromConfiguration<TApiClient>(IConfiguration configuration, string configSectionName = StoryClientOptions.SectionName)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new StoryClientOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddStoryClient<TApiClient>(options);
        }

        /// <summary>Adds <see cref="IStoryClient" /> on an API client resolved by <paramref name="apiClientFactory" />.</summary>
        /// <param name="apiClientFactory">Resolves the API client when it is not registered under its own type.</param>
        /// <param name="configure">Sets route options.</param>
        public IServiceCollection AddStoryClient(Func<IServiceProvider, IApiClient> apiClientFactory, Action<StoryClientOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new StoryClientOptions();
            configure(options);
            return services.AddStoryClient(apiClientFactory, options);
        }

        /// <summary>Adds <see cref="IStoryClient" /> on an API client resolved by <paramref name="apiClientFactory" />.</summary>
        /// <param name="apiClientFactory">Resolves the API client when it is not registered under its own type.</param>
        /// <param name="options">Route options. Null keeps routes relative to the client's base address.</param>
        public IServiceCollection AddStoryClient(Func<IServiceProvider, IApiClient> apiClientFactory, StoryClientOptions? options = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(apiClientFactory);
            var resolved = RegisterOptions(services, options);
            services.AddSingleton<IStoryClient>(sp => new StoryClient(apiClientFactory(sp), resolved));
            return services;
        }

        /// <summary>Adds <see cref="IStoryClient" /> on an API client resolved by <paramref name="apiClientFactory" />, binding route options from configuration.</summary>
        /// <param name="apiClientFactory">Resolves the API client when it is not registered under its own type.</param>
        /// <param name="configuration">Configuration root (for example <c>builder.Configuration</c>).</param>
        /// <param name="configSectionName">Section to bind. Defaults to <see cref="StoryClientOptions.SectionName" />.</param>
        public IServiceCollection AddStoryClientFromConfiguration(
            Func<IServiceProvider, IApiClient> apiClientFactory,
            IConfiguration configuration,
            string configSectionName = StoryClientOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(apiClientFactory);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new StoryClientOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddStoryClient(apiClientFactory, options);
        }
    }

    private static StoryClientOptions RegisterOptions(IServiceCollection services, StoryClientOptions? options)
    {
        var resolved = options ?? new StoryClientOptions();
        resolved.Validate();
        services.TryAddSingleton(Options.Create(resolved));
        services.TryAddSingleton(resolved);
        return resolved;
    }
}
