using Lyo.ContactUs.Models;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.ContactUs;

/// <summary>Extension methods for contact form service registration.</summary>
public static class Extensions
{
    /// <summary>Adds a contact form service to the service collection.</summary>
    /// <typeparam name="TService">The contact form service implementation type.</typeparam>
    /// <typeparam name="TOptions">The contact form service options type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure the options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddContactUsService<TService, TOptions>(this IServiceCollection services, Action<TOptions>? configure = null)
        where TService : class, IContactUsService where TOptions : ContactUsServiceOptions, new()
    {
        if (configure != null)
            services.Configure(configure);

        services.AddSingleton<IContactUsService, TService>();
        return services;
    }

    /// <summary>Adds a contact form service to the service collection with explicit options.</summary>
    /// <typeparam name="TService">The contact form service implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The contact form service options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddContactUsService<TService>(this IServiceCollection services, ContactUsServiceOptions options)
        where TService : class, IContactUsService
    {
        services.AddSingleton(options);
        services.AddSingleton<IContactUsService, TService>();
        return services;
    }

    /// <summary>Adds contact form service to the service collection using configuration binding.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configSectionName">The configuration section name (defaults to "ContactUsOptions").</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>This method binds configuration from IConfiguration if it's registered. If IConfiguration is not available, the options will use default values.</para>
    /// <para>Example configuration in appsettings.json:</para>
    /// <code>
    /// {
    ///   "ContactUsOptions": {
    ///     "MaxMessageLength": 10000,
    ///     "MinMessageLength": 10,
    ///     "EnableMetrics": false
    ///   }
    /// }
    /// </code>
    /// <para>Note: You must also register a storage implementation (e.g. AddContactUsPostgres) to have a working service.</para>
    /// </remarks>
    public static IServiceCollection AddContactUsFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = ContactUsServiceOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
        if (!services.Any(s => s.ServiceType == typeof(ContactUsServiceOptions))) {
            services.AddSingleton<ContactUsServiceOptions>(_ => {
                var section = configuration.GetSection(configSectionName);
                var options = new ContactUsServiceOptions();
                if (section.Exists())
                    section.Bind(options);

                return options;
            });
        }

        return services;
    }
}