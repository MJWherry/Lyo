using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Sftp.Client;

/// <summary>DI registration for <see cref="SftpClient" />.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds a singleton <see cref="ISftpClient" /> using the configure callback.</summary>
        public IServiceCollection AddSftpClient(Action<SftpClientOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new SftpClientOptions();
            configure(options);
            return services.AddSftpClient(options);
        }

        /// <summary>Adds a singleton <see cref="ISftpClient" /> from the given options instance.</summary>
        public IServiceCollection AddSftpClient(SftpClientOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(options);
            services.AddSingleton<ISftpClient>(sp => CreateClient(sp, options));
            services.AddSingleton(sp => (SftpClient)sp.GetRequiredService<ISftpClient>());
            return services;
        }

        /// <summary>Adds a singleton <see cref="ISftpClient" /> bound from configuration.</summary>
        public IServiceCollection AddSftpClientFromConfiguration(IConfiguration configuration, string sectionName = SftpClientOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName);
            var options = new SftpClientOptions();
            var section = configuration.GetSection(sectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddSftpClient(options);
        }
    }

    private static SftpClient CreateClient(IServiceProvider sp, SftpClientOptions options)
    {
        var loggerFactory = sp.GetService<ILoggerFactory>();
        var metrics = options.EnableMetrics ? sp.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
        return new SftpClient(options, loggerFactory, metrics);
    }
}
