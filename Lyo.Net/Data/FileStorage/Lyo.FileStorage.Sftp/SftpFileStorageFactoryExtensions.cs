using Lyo.Exceptions;
using Lyo.FileStorage.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.FileStorage.Sftp;

/// <summary>Factory and single-store registration for <see cref="SftpFileStorageService" />. Existing <c>AddSftpFileStorageService</c> methods are unchanged.</summary>
public static class SftpFileStorageFactoryExtensions
{
    /// <summary>Registers the SFTP <see cref="IFileStorageStoreProvider" /> so <c>FileStorage:Stores</c> can build SFTP stores. Does not register a store.</summary>
    public static IServiceCollection AddSftpFileStorageProvider(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IFileStorageStoreProvider, SftpFileStorageStoreProvider>());
        return services;
    }

    /// <summary>Registers one unkeyed SFTP <see cref="IFileStorageService" />. Throws when an unkeyed file storage service is already registered.</summary>
    public static IServiceCollection AddSftpFileStorage(this IServiceCollection services, Action<SftpFileStorageBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        if (services.Any(descriptor => descriptor.ServiceType == typeof(IFileStorageService) && descriptor.ServiceKey is null))
            throw new InvalidOperationException("An unkeyed IFileStorageService is already registered.");

        var builder = new SftpFileStorageBuilder();
        configure(builder);
        builder.EnsureValid();
        services.AddSftpFileStorageProvider();
        services.AddScoped<IFileStorageService>(provider => builder.ToDelegate("sftp")(provider));
        return services;
    }

    extension(FileStorageFactoryBuilder stores)
    {
        /// <summary>Registers a named SFTP store from an inline options edit. The client is built from <see cref="SftpFileStorageOptions.Sftp" />.</summary>
        public FileStorageFactoryBuilder AddSftp(string name, Action<SftpFileStorageOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(configure);
            return stores.AddSftp(name, builder => builder.Configure(configure));
        }

        /// <summary>Registers a named SFTP store. The client is constructed when the store is first resolved in a scope.</summary>
        public FileStorageFactoryBuilder AddSftp(string name, Action<SftpFileStorageBuilder> configure)
        {
            ArgumentHelpers.ThrowIfNull(stores);
            ArgumentHelpers.ThrowIfNull(configure);
            var builder = new SftpFileStorageBuilder();
            configure(builder);
            return stores.Add(name, builder);
        }
    }
}
