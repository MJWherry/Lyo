using Lyo.Configuration;
using Lyo.ContactUs.Models;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.ContactUs;

/// <summary>DI helpers for contact form service registration.</summary>
public static class Extensions
{
    /// <param name="services">DI collection being extended.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers a contact form service to the service collection.</summary>
        /// <typeparam name="TService">Concrete contact-form service type.</typeparam>
        /// <typeparam name="TOptions">Options type for the contact-form service.</typeparam>
        /// <param name="configure">Callback that fills the options object.</param>
        /// <returns>Same collection so registration can be chained.</returns>
        public IServiceCollection AddContactUsService<TService, TOptions>(Action<TOptions>? configure = null)
            where TService : class, IContactUsService where TOptions : ContactUsServiceOptions, new()
        {
            if (configure != null)
                services.Configure(configure);

            services.AddSingleton<IContactUsService, TService>();
            return services;
        }

        /// <summary>Registers a contact form service to the service collection with explicit options.</summary>
        /// <typeparam name="TService">Concrete contact-form service type.</typeparam>
        /// <param name="options">Options for the contact-form service.</param>
        /// <returns>Same collection so registration can be chained.</returns>
        public IServiceCollection AddContactUsService<TService>(ContactUsServiceOptions options)
            where TService : class, IContactUsService
        {
            services.AddSingleton(options);
            services.AddSingleton<IContactUsService, TService>();
            return services;
        }

        /// <summary>Registers contact-form service to the service collection using configuration binding.</summary>
        /// <param name="configuration">Configuration object being read.</param>
        /// <param name="configSectionName">Configuration section name; defaults to "ContactUsOptions".</param>
        /// <returns>Same collection so registration can be chained.</returns>
        /// <remarks>
        /// <para>When IConfiguration is registered, this overload binds from it. Otherwise the options keep their defaults.</para>
        /// <para>Sample appsettings.json fragment:</para>
        /// <code>
        /// {
        ///   "ContactUsOptions": {
        ///     "MaxMessageLength": 10000,
        ///     "MinMessageLength": 10,
        ///     "EnableMetrics": false
        ///   }
        /// }
        /// </code>
        /// <para>A storage registration such as AddContactUsPostgres is still required before the service can run.</para>
        /// </remarks>
        public IServiceCollection AddContactUsFromConfiguration(IConfiguration configuration, string configSectionName = ContactUsServiceOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            if (!services.Any(s => s.ServiceType == typeof(ContactUsServiceOptions))) {
                services.AddSingleton<ContactUsServiceOptions>(_ => {
                    var options = LyoOptions.Bind<ContactUsServiceOptions>(configuration, configSectionName);

                    return options;
                });
            }

            return services;
        }
    }
}