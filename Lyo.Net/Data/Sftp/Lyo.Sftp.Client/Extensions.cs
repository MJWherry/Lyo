using Lyo.Exceptions;
using Lyo.IO.FileSystem;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lyo.Sftp.Client;

/// <summary>DI helpers that register <see cref="SftpClient" />.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers a singleton <see cref="ISftpClient" /> and runs <paramref name="configure" /> on the options.</summary>
        public IServiceCollection AddSftpClient(Action<SftpClientOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new SftpClientOptions();
            configure(options);
            return services.AddSftpClient(options);
        }

        /// <summary>Registers a singleton <see cref="ISftpClient" /> from an options instance.</summary>
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

        /// <summary>Registers a singleton <see cref="ISftpClient" /> with options bound from configuration.</summary>
        public IServiceCollection AddSftpClientFromConfiguration(IConfiguration configuration, string sectionName = SftpClientOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName);
            var options = new SftpClientOptions();
            configuration.GetSection(sectionName).Bind(options);

            return services.AddSftpClient(options);
        }

        /// <summary>Registers <see cref="SftpFileSystem" /> as <see cref="IFileSystem" /> wrapping the <see cref="ISftpClient" /> already in DI.</summary>
        public IServiceCollection AddSftpFileSystem()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddSingleton<IFileSystem>(sp => new SftpFileSystem(sp.GetRequiredService<ISftpClient>()));
            return services;
        }
    }

    private static SftpClient CreateClient(IServiceProvider sp, SftpClientOptions options)
    {
        var loggerFactory = sp.GetService<ILoggerFactory>();
        var metrics = options.EnableMetrics ? sp.GetService<IMetrics>() ?? NullMetrics.Instance : NullMetrics.Instance;
        return new(options, loggerFactory, metrics);
    }
}