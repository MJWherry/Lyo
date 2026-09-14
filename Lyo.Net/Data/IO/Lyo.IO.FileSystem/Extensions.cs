using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.IO.FileSystem;

/// <summary>DI helpers that register <see cref="IFileSystem" />.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers a singleton <see cref="LocalFileSystem" /> as <see cref="IFileSystem" /> from <paramref name="configure" />.</summary>
        public IServiceCollection AddLocalFileSystem(Action<LocalFileSystemOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new LocalFileSystemOptions();
            configure(options);
            return services.AddLocalFileSystem(options);
        }

        /// <summary>Registers a singleton <see cref="LocalFileSystem" /> as <see cref="IFileSystem" />.</summary>
        public IServiceCollection AddLocalFileSystem(LocalFileSystemOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.TryAddSingleton(options);
            services.TryAddSingleton<IFileSystem>(sp => new LocalFileSystem(sp.GetRequiredService<LocalFileSystemOptions>()));
            return services;
        }

        /// <summary>Registers a singleton <see cref="LocalFileSystem" /> with options bound from configuration.</summary>
        public IServiceCollection AddLocalFileSystemFromConfiguration(IConfiguration configuration, string sectionName = LocalFileSystemOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName);
            var options = new LocalFileSystemOptions();
            configuration.GetSection(sectionName).Bind(options);
            return services.AddLocalFileSystem(options);
        }

        /// <summary>Registers a singleton <see cref="MemoryFileSystem" /> as <see cref="IFileSystem" />.</summary>
        public IServiceCollection AddMemoryFileSystem()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddSingleton<IFileSystem>(_ => new MemoryFileSystem());
            return services;
        }
    }
}
