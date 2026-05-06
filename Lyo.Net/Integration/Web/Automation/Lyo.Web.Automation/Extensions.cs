using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Plan;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Web.Automation;

/// <summary>Service registration extensions for automation plan execution.</summary>
public static class Extensions
{
    /// <summary>Registers <see cref="IAutomationPlanRunner" /> and default collaborators.</summary>
    public static IServiceCollection AddWebAutomationPlanRunner(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        services.AddSingleton<HttpClient>();
        services.AddSingleton<IAutomationPlanDataSink, NullAutomationPlanDataSink>();
        services.AddSingleton<IAutomationPlanFileStorage, NullAutomationPlanFileStorage>();
        services.AddScoped<IAutomationPlanRunner, AutomationPlanRunner>();
        return services;
    }
}
