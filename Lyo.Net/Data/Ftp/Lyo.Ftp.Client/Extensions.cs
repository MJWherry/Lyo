using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Ftp.Client;

/// <summary>DI registration for <see cref="FtpClient" />.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds a singleton <see cref="IFtpClient" /> using the configure callback.</summary>
        public IServiceCollection AddFtpClient(Action<FtpClientOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new FtpClientOptions();
            configure(options);
            return services.AddFtpClient(options);
        }

        /// <summary>Adds a singleton <see cref="IFtpClient" /> from the given options instance.</summary>
        public IServiceCollection AddFtpClient(FtpClientOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(options);
            services.AddSingleton<IFtpClient>(sp => CreateClient(sp, options));
            services.AddSingleton(sp => (FtpClient)sp.GetRequiredService<IFtpClient>());
            return services;
        }

        /// <summary>Adds a singleton <see cref="IFtpClient" /> bound from configuration.</summary>
        public IServiceCollection AddFtpClientFromConfiguration(IConfiguration configuration, string sectionName = FtpClientOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName);
            var options = new FtpClientOptions();
            var section = configuration.GetSection(sectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddFtpClient(options);
        }
    }

    private static FtpClient CreateClient(IServiceProvider sp, FtpClientOptions options)
    {
        var loggerFactory = sp.GetService<ILoggerFactory>();
        var metrics = options.EnableMetrics ? sp.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
        return new FtpClient(options, loggerFactory, metrics);
    }
}
