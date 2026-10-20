using Lyo.Exceptions;
using Lyo.FileStorage.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.FileStorage.S3;

/// <summary>Factory and single-store registration for <see cref="S3FileStorageService" />. Existing keyed registration methods are unchanged.</summary>
public static class S3FileStorageFactoryExtensions
{
    /// <summary>Registers the S3 <see cref="IFileStorageStoreProvider" /> so <c>FileStorage:Stores</c> can build S3 stores. Does not register a store.</summary>
    public static IServiceCollection AddS3FileStorageProvider(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IFileStorageStoreProvider, S3FileStorageStoreProvider>());
        return services;
    }

    /// <summary>Registers one unkeyed S3 <see cref="IFileStorageService" /> and unkeyed multipart. Throws when an unkeyed file storage service is already registered.</summary>
    public static IServiceCollection AddS3FileStorage(this IServiceCollection services, Action<S3FileStorageBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        if (services.Any(descriptor => descriptor.ServiceType == typeof(IFileStorageService) && descriptor.ServiceKey is null))
            throw new InvalidOperationException("An unkeyed IFileStorageService is already registered.");

        var builder = new S3FileStorageBuilder();
        configure(builder);
        builder.EnsureValid();
        services.AddS3FileStorageProvider();
        services.AddScoped<IFileStorageService>(provider => builder.ToDelegate("s3")(provider));
        builder.RegisterUnkeyedMultipart(services);
        return services;
    }

    extension(FileStorageFactoryBuilder stores)
    {
        /// <summary>Registers a named S3 store from an inline options edit. Multipart is registered for the same name.</summary>
        public FileStorageFactoryBuilder AddS3(string name, Action<S3FileStorageOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(configure);
            return stores.AddS3(name, builder => builder.Configure(configure));
        }

        /// <summary>Registers a named S3 store. Multipart closes over this store's options and client.</summary>
        public FileStorageFactoryBuilder AddS3(string name, Action<S3FileStorageBuilder> configure)
        {
            ArgumentHelpers.ThrowIfNull(stores);
            ArgumentHelpers.ThrowIfNull(configure);
            var builder = new S3FileStorageBuilder();
            configure(builder);
            return stores.Add(name, builder);
        }
    }
}
