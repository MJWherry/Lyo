using Lyo.Exceptions;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileStorage;

/// <summary>Records named stores on <see cref="IFileStorageFactory" />. Call <see cref="Extensions.AddFileStorageFactory" /> rather than constructing this yourself.</summary>
public sealed class FileStorageFactoryBuilder
{
    private readonly FileStorageFactoryCatalog _catalog;
    private readonly IServiceCollection _services;

    internal FileStorageFactoryBuilder(IServiceCollection services, FileStorageFactoryCatalog catalog)
    {
        _services = services;
        _catalog = catalog;
    }

    /// <summary>Keeps <paramref name="service" /> for every scope. The factory does not dispose it.</summary>
    public FileStorageFactoryBuilder Register(string name, IFileStorageService service)
    {
        ArgumentHelpers.ThrowIfNull(service);
        _catalog.Add(new() { Name = RequireName(name), Instance = service, DisposeCreated = false });
        return this;
    }

    /// <summary>Runs <paramref name="create" /> on the first <see cref="IFileStorageFactory.Get" /> in a scope and disposes that instance with the scope.</summary>
    public FileStorageFactoryBuilder Register(string name, Func<IServiceProvider, IFileStorageService> create)
    {
        ArgumentHelpers.ThrowIfNull(create);
        _catalog.Add(new() { Name = RequireName(name), Create = create, DisposeCreated = true });
        return this;
    }

    /// <summary>Registers a disk store from an inline options edit.</summary>
    public FileStorageFactoryBuilder AddDisk(string name, Action<DiskFileStorageOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        return AddDisk(name, disk => disk.Configure(configure));
    }

    /// <summary>Registers a disk store from <paramref name="configure" />.</summary>
    public FileStorageFactoryBuilder AddDisk(string name, Action<DiskFileStorageBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var disk = new DiskFileStorageBuilder();
        configure(disk);
        return Add(name, disk);
    }

    /// <summary>Records <paramref name="builder" /> under <paramref name="name" />. Provider packages call this from their <c>AddS3</c> style extensions.</summary>
    public FileStorageFactoryBuilder Add<TOptions, TBuilder>(string name, FileStorageServiceBuilder<TOptions, TBuilder> builder)
        where TOptions : FileStorageServiceBaseOptions, new()
        where TBuilder : FileStorageServiceBuilder<TOptions, TBuilder>
    {
        ArgumentHelpers.ThrowIfNull(builder);
        var storeName = RequireName(name);
        builder.EnsureValid();
        _catalog.Add(new() {
            Name = storeName,
            Create = builder.ToDelegate(storeName),
            EncryptionServiceKey = builder.EncryptionKey,
            DisposeCreated = true
        });
        if (builder.Archive)
            FileStorageFactoryArchive.Register(_services, storeName);

        builder.RegisterFactoryServices(_services, storeName);
        return this;
    }

    internal static string RequireName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Contains('/') || name.Contains('\\'))
            throw new InvalidOperationException($"File storage name '{name}' must be one path segment.");

        return name.Trim();
    }
}
