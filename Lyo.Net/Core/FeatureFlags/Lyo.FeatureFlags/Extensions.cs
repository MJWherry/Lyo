using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.FeatureFlags;

/// <summary>Registers <see cref="FeatureFlagService" />. The first registration wins.</summary>
public static class FeatureFlagServiceExtensions
{
    /// <param name="services">Application service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers an in-memory <see cref="IFeatureFlagService" /> when none is registered yet.</summary>
        /// <param name="configure">Optional flag setup after defaults. A flag stays off until a mode method runs.</param>
        public IServiceCollection AddFeatureFlags(Action<FeatureFlagOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new FeatureFlagOptions();
            configure?.Invoke(options);
            return services.AddFeatureFlags(options);
        }

        /// <summary>Registers <paramref name="options" /> and <see cref="IFeatureFlagService" /> when none is registered yet.</summary>
        /// <param name="options">Validated options. Later changes to this instance do not change the service.</param>
        public IServiceCollection AddFeatureFlags(FeatureFlagOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.TryAddSingleton<IOptions<FeatureFlagOptions>>(Options.Create(options));
            services.TryAddSingleton(options);
            services.TryAddSingleton<IFeatureFlagService>(sp => new FeatureFlagService(sp.GetRequiredService<FeatureFlagOptions>()));
            return services;
        }

        /// <summary>
        /// Binds each child of <c>Flags</c> under <paramref name="sectionName" /> and registers the service when none is registered yet.
        /// A missing section leaves every flag off. An unknown property or a bad mode throws <see cref="ConfigurationException" />.
        /// </summary>
        /// <param name="configuration">Configuration root or section parent.</param>
        /// <param name="sectionName">Section to bind. Defaults to <see cref="FeatureFlagOptions.SectionName" />.</param>
        public IServiceCollection AddFeatureFlagsFromConfiguration(IConfiguration configuration, string sectionName = FeatureFlagOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName);
            var options = new FeatureFlagOptions();
            foreach (var child in configuration.GetSection(sectionName).GetSection("Flags").GetChildren())
            {
                var definition = new FeatureFlagDefinition();
                try
                {
                    child.Bind(definition, binder => binder.ErrorOnUnknownConfiguration = true);
                }
                catch (Exception ex)
                {
                    throw new ConfigurationException($"Flag '{child.Key}' could not be bound.", child.Key, ex);
                }

                options.Flags[child.Key] = definition;
            }

            return services.AddFeatureFlags(options);
        }
    }
}
