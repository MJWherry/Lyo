using Lyo.Diff;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Drift.Agent;

/// <summary>DI helpers for the Drift agent hosted service.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the agent from a configure delegate.</summary>
        public IServiceCollection AddDriftAgent(Action<DriftAgentOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new DriftAgentOptions();
            configure(options);
            return services.AddDriftAgent(options);
        }

        /// <summary>Registers the agent from configuration section <see cref="DriftAgentOptions.SectionName" />.</summary>
        public IServiceCollection AddDriftAgentFromConfiguration(IConfiguration configuration, string configSectionName = DriftAgentOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new DriftAgentOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddDriftAgent(options);
        }

        /// <summary>Registers <see cref="DriftAgentHostedService" />. Requires <c>IDriftClient</c>.</summary>
        public IServiceCollection AddDriftAgent(DriftAgentOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddLyoDiff();
            services.AddSingleton(Options.Create(options));
            services.TryAddSingleton(options);
            services.TryAddSingleton<DriftAgentHostedService>();
            services.AddHostedService(sp => sp.GetRequiredService<DriftAgentHostedService>());
            return services;
        }
    }
}
