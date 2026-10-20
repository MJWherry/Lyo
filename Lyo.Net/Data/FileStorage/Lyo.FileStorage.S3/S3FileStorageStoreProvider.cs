using Lyo.FileStorage.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileStorage.S3;

/// <summary>Creates an S3 store from a <c>Provider: S3</c> section and registers multipart for that store.</summary>
public sealed class S3FileStorageStoreProvider : IFileStorageStoreProvider, IFileStorageProviderServices
{
    /// <inheritdoc />
    public string ProviderName => "S3";

    /// <inheritdoc />
    public Func<IServiceProvider, IFileStorageService> Create(string storeName, IConfiguration section) => Build(null, storeName, section);

    /// <inheritdoc />
    public Func<IServiceProvider, IFileStorageService> Create(IServiceCollection services, string storeName, IConfiguration section) => Build(services, storeName, section);

    private static Func<IServiceProvider, IFileStorageService> Build(IServiceCollection? services, string storeName, IConfiguration section)
    {
        var builder = new S3FileStorageBuilder();
        builder.ApplySection(section);
        builder.EnsureValid();
        if (services != null)
            builder.RegisterFactoryServices(services, storeName);

        return builder.ToDelegate(storeName);
    }
}
