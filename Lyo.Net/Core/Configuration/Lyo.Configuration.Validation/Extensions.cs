using Lyo.Exceptions;
using Lyo.Query.Services.ValueConversion;
using Lyo.Query.Services.WhereClause;
using Lyo.Validation;
using Lyo.Validation.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Configuration.Validation;

/// <summary>DI registration for schema-driven validation of <see cref="IConfiguration" />.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers configuration validation using options built by <paramref name="configure" />.</summary>
        public IServiceCollection AddConfigurationValidation(Action<ConfigurationValidationOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new ConfigurationValidationOptions();
            configure(options);
            return services.AddConfigurationValidation(options);
        }

        /// <summary>Registers configuration validation using options bound from <paramref name="configuration" />.</summary>
        public IServiceCollection AddConfigurationValidationFromConfiguration(
            IConfiguration configuration,
            string configSectionName = ConfigurationValidationOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = LyoOptions.Bind<ConfigurationValidationOptions>(configuration, configSectionName);
            return services.AddConfigurationValidation(options);
        }

        /// <summary>
        /// Registers <see cref="IConfigurationValidator" />, the schema store and compiler, and a <see cref="ConfigurationClauseEvaluator" /> around the where-clause engine.
        /// </summary>
        /// <remarks>
        /// Self-contained: the lightweight <see cref="WhereClauseEvaluator" /> is registered when the host has no <see cref="IWhereClauseService" /> of its own, so this works
        /// without the full query stack. Hosts that call <c>AddLyoQueryServices</c> keep their cached implementation because every registration here is a <c>TryAdd</c>.
        /// Seed rules through <see cref="IValidationSchemaStore" /> — from an API, for example — before validating.
        /// </remarks>
        public IServiceCollection AddConfigurationValidation(ConfigurationValidationOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.TryAddSingleton(Options.Create(options));
            services.TryAddSingleton(options);

            services.AddValidation();
            services.TryAddSingleton<IValueConversionService, ValueConversionService>();
            services.TryAddSingleton<IWhereClauseService, WhereClauseEvaluator>();
            services.TryAddSingleton<WhereClauseServiceEvaluator>();
            services.TryAddSingleton<IValidationClauseEvaluator>(
                sp => new ConfigurationClauseEvaluator(sp.GetRequiredService<WhereClauseServiceEvaluator>(), sp.GetRequiredService<IOptions<ConfigurationValidationOptions>>().Value));

            services.TryAddSingleton<IConfigurationValidator, ConfigurationValidator>();
            return services;
        }
    }
}
