using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Job.Alerts;

/// <summary>DI registration for job alert consumption.</summary>
public static class Extensions
{
    /// <summary>Registers <see cref="JobAlertConsumer" /> and binds <see cref="JobAlertsOptions" /> from configuration.</summary>
    public static IServiceCollection AddJobAlerts(this IServiceCollection services, IConfiguration configuration, string configSectionName = JobAlertsOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        services.Configure<JobAlertsOptions>(configuration.GetSection(configSectionName));
        return services.AddJobAlerts();
    }

    /// <summary>Registers <see cref="JobAlertConsumer" /> with optional <see cref="JobAlertsOptions" /> configuration.</summary>
    public static IServiceCollection AddJobAlerts(this IServiceCollection services, Action<JobAlertsOptions>? configure = null)
    {
        ArgumentHelpers.ThrowIfNull(services);
        if (configure is not null)
            services.Configure(configure);

        services.AddHttpClient(nameof(JobAlertConsumer));
        services.AddHostedService<JobAlertConsumer>();
        return services;
    }
}
