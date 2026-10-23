using Lyo.Exceptions;
using Lyo.FileStorage.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.FileStorage.Ftp;

/// <summary>Factory and single-store registration for <see cref="FtpFileStorageService" />. Existing <c>AddFtpFileStorageService</c> methods are unchanged.</summary>
public static class FtpFileStorageFactoryExtensions
{
    /// <summary>Registers the FTP <see cref="IFileStorageStoreProvider" /> so <c>FileStorage:Stores</c> can build FTP stores. Does not register a store.</summary>
    public static IServiceCollection AddFtpFileStorageProvider(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IFileStorageStoreProvider, FtpFileStorageStoreProvider>());
        return services;
    }

    /// <summary>Registers one unkeyed FTP <see cref="IFileStorageService" />. Throws when an unkeyed file storage service is already registered.</summary>
    public static IServiceCollection AddFtpFileStorage(this IServiceCollection services, Action<FtpFileStorageBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        if (services.Any(descriptor => descriptor.ServiceType == typeof(IFileStorageService) && descriptor.ServiceKey is null))
            throw new InvalidOperationException("An unkeyed IFileStorageService is already registered.");

        var builder = new FtpFileStorageBuilder();
        configure(builder);
        builder.EnsureValid();
        services.AddFtpFileStorageProvider();
        services.AddScoped<IFileStorageService>(provider => builder.ToDelegate("ftp")(provider));
        return services;
    }

    extension(FileStorageFactoryBuilder stores)
    {
        /// <summary>Registers a named FTP store from an inline options edit. The client is built from <see cref="FtpFileStorageOptions.Ftp" />.</summary>
        public FileStorageFactoryBuilder AddFtp(string name, Action<FtpFileStorageOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(configure);
            return stores.AddFtp(name, builder => builder.Configure(configure));
        }

        /// <summary>Registers a named FTP store. The client is constructed when the store is first resolved in a scope.</summary>
        public FileStorageFactoryBuilder AddFtp(string name, Action<FtpFileStorageBuilder> configure)
        {
            ArgumentHelpers.ThrowIfNull(stores);
            ArgumentHelpers.ThrowIfNull(configure);
            var builder = new FtpFileStorageBuilder();
            configure(builder);
            return stores.Add(name, builder);
        }
    }
}