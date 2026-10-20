using Lyo.Exceptions;
using Lyo.FileStorage.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.FileStorage;

/// <summary>Registers <see cref="IFileStorageFactory" /> and disk stores.</summary>
public static class FileStorageFactoryExtensions
{
    /// <summary>Records stores from <paramref name="configure" />. A later call appends to the same factory.</summary>
    public static IServiceCollection AddFileStorageFactory(this IServiceCollection services, Action<FileStorageFactoryBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        var catalog = Catalog(services);
        configure(new FileStorageFactoryBuilder(services, catalog));
        return services;
    }

    /// <summary>Binds <c>Default</c> and <c>Stores</c> from <paramref name="sectionName" />. Disk is available without an extra provider call.</summary>
    public static IServiceCollection AddFileStorageFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "FileStorage")
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName);
        return services.AddFileStorageFactory(configuration, _ => { }, sectionName);
    }

    /// <summary>Runs <paramref name="configure" /> first so it can register providers, then binds the section.</summary>
    public static IServiceCollection AddFileStorageFactory(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<FileStorageFactoryBuilder> configure,
        string sectionName = "FileStorage")
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        ArgumentHelpers.ThrowIfNull(configure);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName);
        var catalog = Catalog(services);
        var builder = new FileStorageFactoryBuilder(services, catalog);
        configure(builder);
        FileStorageFactoryConfiguration.Apply(services, catalog, configuration.GetSection(sectionName));
        return services;
    }

    /// <summary>Registers one unkeyed disk <see cref="IFileStorageService" />. Throws when an unkeyed file storage service is already registered.</summary>
    public static IServiceCollection AddDiskFileStorage(this IServiceCollection services, Action<DiskFileStorageBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        if (services.Any(descriptor => descriptor.ServiceType == typeof(IFileStorageService) && descriptor.ServiceKey is null))
            throw new InvalidOperationException("An unkeyed IFileStorageService is already registered.");

        var disk = new DiskFileStorageBuilder();
        configure(disk);
        disk.EnsureValid();
        services.AddScoped<IFileStorageService>(provider => disk.ToDelegate("disk")(provider));
        return services;
    }

    internal static FileStorageFactoryCatalog Catalog(IServiceCollection services)
    {
        var existing = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(FileStorageFactoryCatalog));
        if (existing?.ImplementationInstance is FileStorageFactoryCatalog catalog)
            return catalog;

        catalog = new FileStorageFactoryCatalog();
        services.AddSingleton(catalog);
        services.AddScoped<IFileStorageFactory, FileStorageFactory>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IFileStorageStoreProvider, DiskFileStorageStoreProvider>());
        return catalog;
    }
}
