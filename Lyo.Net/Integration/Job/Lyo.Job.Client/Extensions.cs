using Lyo.Api.Client;
using Lyo.Exceptions;
using Lyo.Job.Models.Events;
using Lyo.MessageQueue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Job.Client;

/// <summary>DI extensions for <see cref="JobClient" /> and the scheduler/worker MQ publisher.</summary>
public static class Extensions
{
    /// <summary>Registers a singleton <see cref="IJobClient" /> built from the registered <typeparamref name="TApiClient" />.</summary>
    public static IServiceCollection AddJobClient<TApiClient>(this IServiceCollection services, JobClientOptions? options = null)
        where TApiClient : class, IApiClient
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.AddSingleton<IJobClient>(sp => new JobClient(sp.GetRequiredService<TApiClient>(), options));
        return services;
    }

    /// <summary>Registers a singleton <see cref="IJobClient" /> using a factory to resolve the underlying <see cref="IApiClient" />.</summary>
    public static IServiceCollection AddJobClient(this IServiceCollection services, Func<IServiceProvider, IApiClient> apiClientFactory, JobClientOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(apiClientFactory);
        services.AddSingleton<IJobClient>(sp => new JobClient(apiClientFactory(sp), options));
        return services;
    }

    /// <summary>
    /// Registers the scheduler/worker <see cref="MqJobEventPublisher" /> (<see cref="IMqService" /> + optional <see cref="IJobClient" />). Do not use the Postgres publisher on
    /// these hosts. Requires <see cref="IMqService" /> (e.g. RabbitMQ).
    /// </summary>
    public static IServiceCollection AddMqJobEventPublisher(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.AddOptions<JobMqOptions>();
        services.TryAddSingleton(p => p.GetRequiredService<IOptions<JobMqOptions>>().Value);
        services.AddSingleton<IJobEventPublisher>(sp => new MqJobEventPublisher(
            sp.GetRequiredService<IMqService>(), sp.GetRequiredService<ILogger<MqJobEventPublisher>>(), sp.GetRequiredService<IOptions<JobMqOptions>>(),
            sp.GetService<IJobClient>()));

        services.AddHostedService<JobEventPublisherStartupService>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="MqJobEventPublisher" /> and binds <see cref="JobMqOptions" /> from configuration. For scheduler/worker hosts — API hosts with <c>Lyo.Job.Postgres</c>
    /// should use that package's <c>AddMqJobEventPublisher*</c> instead.
    /// </summary>
    public static IServiceCollection AddMqJobEventPublisherFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = JobMqOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(configuration);
        services.AddOptions<JobMqOptions>();
        services.TryAddSingleton(p => p.GetRequiredService<IOptions<JobMqOptions>>().Value);
        services.AddSingleton<IJobEventPublisher>(sp => new MqJobEventPublisher(
            sp.GetRequiredService<IMqService>(), sp.GetRequiredService<ILogger<MqJobEventPublisher>>(), sp.GetRequiredService<IOptions<JobMqOptions>>(),
            sp.GetService<IJobClient>()));

        services.AddHostedService<JobEventPublisherStartupService>();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            services.Configure<JobMqOptions>(section);

        return services;
    }
}