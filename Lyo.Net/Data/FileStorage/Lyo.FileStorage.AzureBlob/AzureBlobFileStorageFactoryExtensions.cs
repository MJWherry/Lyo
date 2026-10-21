using Lyo.Exceptions;
using Lyo.FileStorage.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.FileStorage.AzureBlob;

/// <summary>Factory and single-store registration for <see cref="AzureBlobFileStorageService" />. Existing <c>AddAzureBlobFileStorageService</c> methods are unchanged.</summary>
public static class AzureBlobFileStorageFactoryExtensions
{
    /// <summary>Registers the Azure Blob <see cref="IFileStorageStoreProvider" /> so <c>FileStorage:Stores</c> can build blob stores. Does not register a store.</summary>
    public static IServiceCollection AddAzureBlobFileStorageProvider(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IFileStorageStoreProvider, AzureBlobFileStorageStoreProvider>());
        return services;
    }

    /// <summary>Registers one unkeyed Azure Blob <see cref="IFileStorageService" />. Throws when an unkeyed file storage service is already registered.</summary>
    public static IServiceCollection AddAzureBlobFileStorage(this IServiceCollection services, Action<AzureBlobFileStorageBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        if (services.Any(descriptor => descriptor.ServiceType == typeof(IFileStorageService) && descriptor.ServiceKey is null))
            throw new InvalidOperationException("An unkeyed IFileStorageService is already registered.");

        var builder = new AzureBlobFileStorageBuilder();
        configure(builder);
        builder.EnsureValid();
        services.AddAzureBlobFileStorageProvider();
        services.AddScoped<IFileStorageService>(provider => builder.ToDelegate("azure")(provider));
        builder.RegisterUnkeyedMultipart(services);
        return services;
    }

    extension(FileStorageFactoryBuilder stores)
    {
        /// <summary>Registers a named Azure Blob store from an inline options edit. Multipart is not registered.</summary>
        public FileStorageFactoryBuilder AddAzureBlob(string name, Action<AzureBlobFileStorageOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(configure);
            return stores.AddAzureBlob(name, builder => builder.Configure(configure));
        }

        /// <summary>Registers a named Azure Blob store. Call <see cref="AzureBlobFileStorageBuilder.WithMultipart" /> to add multipart for this name.</summary>
        public FileStorageFactoryBuilder AddAzureBlob(string name, Action<AzureBlobFileStorageBuilder> configure)
        {
            ArgumentHelpers.ThrowIfNull(stores);
            ArgumentHelpers.ThrowIfNull(configure);
            var builder = new AzureBlobFileStorageBuilder();
            configure(builder);
            return stores.Add(name, builder);
        }
    }
}