using Lyo.Exceptions;
using Lyo.IO.FileSystem;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lyo.Ftp.Client;

/// <summary>DI helpers that register <see cref="FtpClient" />.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers a singleton <see cref="IFtpClient" /> and runs <paramref name="configure" /> on the options.</summary>
        public IServiceCollection AddFtpClient(Action<FtpClientOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new FtpClientOptions();
            configure(options);
            return services.AddFtpClient(options);
        }

        /// <summary>Registers a singleton <see cref="IFtpClient" /> from an options instance.</summary>
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

        /// <summary>Registers a singleton <see cref="IFtpClient" /> with options bound from configuration.</summary>
        public IServiceCollection AddFtpClientFromConfiguration(IConfiguration configuration, string sectionName = FtpClientOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName);
            var options = new FtpClientOptions();
            configuration.GetSection(sectionName).Bind(options);

            return services.AddFtpClient(options);
        }

        /// <summary>Registers <see cref="FtpFileSystem" /> as <see cref="IFileSystem" /> wrapping the <see cref="IFtpClient" /> already in DI.</summary>
        public IServiceCollection AddFtpFileSystem()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddSingleton<IFileSystem>(sp => new FtpFileSystem(sp.GetRequiredService<IFtpClient>()));
            return services;
        }
    }

    private static FtpClient CreateClient(IServiceProvider sp, FtpClientOptions options)
    {
        var loggerFactory = sp.GetService<ILoggerFactory>();
        var metrics = options.EnableMetrics ? sp.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
        return new(options, loggerFactory, metrics);
    }
}