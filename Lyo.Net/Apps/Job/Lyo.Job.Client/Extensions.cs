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

/// <summary>DI helpers for <see cref="JobClient" /> and the scheduler/worker MQ publisher.</summary>
public static class Extensions
{
    /// <summary>Adds a singleton <see cref="IJobClient" /> built from the registered <typeparamref name="TApiClient" />.</summary>
    public static IServiceCollection AddJobClient<TApiClient>(this IServiceCollection services, JobClientOptions? options = null)
        where TApiClient : class, IApiClient
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.AddSingleton<IJobClient>(sp => new JobClient(sp.GetRequiredService<TApiClient>(), options));
        return services;
    }

    /// <summary>Adds a singleton <see cref="IJobClient" /> whose inner <see cref="IApiClient" /> comes from a factory.</summary>
    public static IServiceCollection AddJobClient(this IServiceCollection services, Func<IServiceProvider, IApiClient> apiClientFactory, JobClientOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(apiClientFactory);
        services.AddSingleton<IJobClient>(sp => new JobClient(apiClientFactory(sp), options));
        return services;
    }

    /// <summary>
    /// Adds the scheduler/worker <see cref="MqJobEventPublisher" /> (<see cref="IMqService" /> plus optional <see cref="IJobClient" />). Do not use the Postgres publisher on
    /// these hosts. Needs <see cref="IMqService" /> (for example RabbitMQ).
    /// </summary>
    public static IServiceCollection AddMqJobEventPublisher(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.AddOptions<JobMqOptions>();
        return services.AddMqJobEventPublisherCore();
    }

    /// <summary>Adds the scheduler/worker <see cref="MqJobEventPublisher" /> with inline topology options.</summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Sets the queue topology.</param>
    public static IServiceCollection AddMqJobEventPublisher(this IServiceCollection services, Action<JobMqOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        var options = new JobMqOptions();
        configure(options);
        return services.AddMqJobEventPublisher(options);
    }

    /// <summary>Adds the scheduler/worker <see cref="MqJobEventPublisher" /> with the given topology options.</summary>
    /// <param name="services">Service collection.</param>
    /// <param name="options">Queue topology declared when the host starts.</param>
    public static IServiceCollection AddMqJobEventPublisher(this IServiceCollection services, JobMqOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        services.AddSingleton(Options.Create(options));
        services.TryAddSingleton(options);
        return services.AddMqJobEventPublisherCore();
    }

    /// <summary>
    /// Adds <see cref="MqJobEventPublisher" /> and binds <see cref="JobMqOptions" /> from configuration. For scheduler/worker hosts. API hosts with <c>Lyo.Job.Postgres</c>
    /// should call that package's <c>AddMqJobEventPublisher*</c> instead.
    /// </summary>
    public static IServiceCollection AddMqJobEventPublisherFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = JobMqOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        services.AddOptions<JobMqOptions>();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            services.Configure<JobMqOptions>(section);

        return services.AddMqJobEventPublisherCore();
    }

    private static IServiceCollection AddMqJobEventPublisherCore(this IServiceCollection services)
    {
        services.TryAddSingleton(p => p.GetRequiredService<IOptions<JobMqOptions>>().Value);
        services.AddSingleton<IJobEventPublisher>(sp => new MqJobEventPublisher(
            sp.GetRequiredService<IMqService>(), sp.GetRequiredService<ILogger<MqJobEventPublisher>>(), sp.GetRequiredService<JobMqOptions>(),
            sp.GetService<IJobClient>()));

        services.AddHostedService<JobEventPublisherStartupService>();
        return services;
    }
}