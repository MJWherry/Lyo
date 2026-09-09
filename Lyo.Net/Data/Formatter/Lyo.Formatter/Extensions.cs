using Lyo.Exceptions;
using Lyo.Parameters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartFormat;

namespace Lyo.Formatter;

public static class Extensions
{
    /// <param name="services">Service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds the formatter service.</summary>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddFormatterService()
        {
            ArgumentHelpers.ThrowIfNull(services);
            FormatterLyoType.EnsureRegistered();
            services.AddSingleton<FormatterService>();
            services.AddSingleton<IFormatterService>(provider => provider.GetRequiredService<FormatterService>());
            services.AddParameterTemplateResolver();
            return services;
        }

        /// <summary>Adds the formatter service with a caller-supplied SmartFormatter factory.</summary>
        /// <param name="formatterFactory">Builds the SmartFormatter from the service provider.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddFormatterService(Func<IServiceProvider, SmartFormatter> formatterFactory)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(formatterFactory);
            FormatterLyoType.EnsureRegistered();
            services.AddSingleton<FormatterService>(provider => new(formatterFactory(provider)));
            services.AddSingleton<IFormatterService>(provider => provider.GetRequiredService<FormatterService>());
            services.AddParameterTemplateResolver();
            return services;
        }

        /// <summary>
        /// Registers the <see cref="LyoTemplateResolver" /> used by job and report services to resolve expression parameter defaults. Invoked by
        /// <see cref="AddFormatterService()" />; call it directly only when wiring <see cref="IFormatterService" /> by hand.
        /// </summary>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddParameterTemplateResolver()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddSingleton<LyoTemplateResolver>(provider => FormatterParameterDefaults.CreateResolver(provider.GetRequiredService<IFormatterService>()));
            return services;
        }
    }
}