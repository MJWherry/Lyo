using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Job.Alerts;

/// <summary>Fails option binding at startup with the concrete problem list instead of a generic predicate failure.</summary>
internal sealed class JobAlertsOptionsValidator : IValidateOptions<JobAlertsOptions>
{
    public ValidateOptionsResult Validate(string? name, JobAlertsOptions options)
    {
        var errors = options.GetValidationErrors();
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}

/// <summary>DI registration for consuming job alerts.</summary>
public static class Extensions
{
    /// <summary>Adds <see cref="JobAlertConsumer" /> and binds <see cref="JobAlertsOptions" /> from configuration.</summary>
    public static IServiceCollection AddJobAlerts(this IServiceCollection services, IConfiguration configuration, string configSectionName = JobAlertsOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        services.Configure<JobAlertsOptions>(configuration.GetSection(configSectionName));
        return services.AddJobAlerts();
    }

    /// <summary>Adds <see cref="JobAlertConsumer" /> using a ready-made options instance.</summary>
    public static IServiceCollection AddJobAlerts(this IServiceCollection services, JobAlertsOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        services.AddSingleton(Options.Create(options));
        services.TryAddSingleton(options);
        return services.AddJobAlerts();
    }

    /// <summary>Adds <see cref="JobAlertConsumer" /> with optional <see cref="JobAlertsOptions" /> configuration.</summary>
    public static IServiceCollection AddJobAlerts(this IServiceCollection services, Action<JobAlertsOptions>? configure = null)
    {
        ArgumentHelpers.ThrowIfNull(services);
        if (configure is not null)
            services.Configure(configure);

        // Cap the webhook POST. The HttpClient 100-second default lets one hung endpoint stall the whole alert queue.
        services.AddHttpClient(
            nameof(JobAlertConsumer), (sp, client) => {
                var options = sp.GetRequiredService<IOptions<JobAlertsOptions>>().Value;
                client.Timeout = options.WebhookTimeout;
            });

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<JobAlertsOptions>, JobAlertsOptionsValidator>());
        services.AddOptions<JobAlertsOptions>();
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<JobAlertsOptions>>().Value);
        services.AddHostedService<JobAlertConsumer>();
        return services;
    }
}
