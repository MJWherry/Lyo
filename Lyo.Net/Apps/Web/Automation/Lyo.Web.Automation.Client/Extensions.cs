using Lyo.Api.Client;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Web.Automation.Client;

/// <summary>DI helpers for <see cref="AutomationPlanClient" />.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="IAutomationPlanClient" /> on the <typeparamref name="TApiClient" /> already in the container.</summary>
        public IServiceCollection AddAutomationPlanClient<TApiClient>(Action<AutomationPlanClientOptions> configure)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new AutomationPlanClientOptions();
            configure(options);
            return services.AddAutomationPlanClient<TApiClient>(options);
        }

        /// <summary>Adds <see cref="IAutomationPlanClient" /> on the <typeparamref name="TApiClient" /> already in the container.</summary>
        public IServiceCollection AddAutomationPlanClient<TApiClient>(AutomationPlanClientOptions? options = null)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            options?.Validate();
            services.AddSingleton<IAutomationPlanClient>(sp => new AutomationPlanClient(sp.GetRequiredService<TApiClient>(), options));
            return services;
        }

        /// <summary>Adds <see cref="IAutomationPlanClient" /> on the <typeparamref name="TApiClient" /> already in the container, binding route options from configuration.</summary>
        public IServiceCollection AddAutomationPlanClientFromConfiguration<TApiClient>(IConfiguration configuration, string configSectionName = AutomationPlanClientOptions.SectionName)
            where TApiClient : class, IApiClient
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new AutomationPlanClientOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddAutomationPlanClient<TApiClient>(options);
        }
    }
}
