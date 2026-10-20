using System.Reflection;
using Lyo.Compression;
using Lyo.Compression.Models;
using Lyo.Encryption.TwoKey;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Models;
using Lyo.IO.Temp.Models;
using Lyo.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileStorage.Tests;

public sealed class FileStorageFactoryTests
{
    [Fact]
    public async Task Get_TwoDiskRoots_WritesOnlyToTheNamedRoot()
    {
        using var session = new IOTempSession();
        var rootA = session.CreateDirectory("a");
        var rootB = session.CreateDirectory("b");
        var payloadA = new byte[] { 1, 2, 3, 4 };
        var payloadB = new byte[] { 5, 6, 7, 8 };
        using var provider = Provider(services => services.AddFileStorageFactory(stores => {
            stores.AddDisk("b", options => options.RootDirectoryPath = rootB);
            stores.AddDisk("a", disk => disk.Configure(options => options.RootDirectoryPath = rootA));
        }));
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IFileStorageFactory>();

        await factory.Get("a").SaveFileAsync(payloadA, ct: TestCancellation.TestCt);
        await factory.Get("b").SaveFileAsync(payloadB, ct: TestCancellation.TestCt);

        Assert.Equal(["a", "b"], factory.Names);
        Assert.True(Contains(rootA, payloadA));
        Assert.False(Contains(rootA, payloadB));
        Assert.True(Contains(rootB, payloadB));
        Assert.False(Contains(rootB, payloadA));
        Assert.Null(provider.GetService<DiskFileStorageOptions>());
    }

    [Fact]
    public async Task Configure_AfterFromConfiguration_KeepsInlineRootAndBoundFlag()
    {
        using var session = new IOTempSession();
        var inline = session.CreateDirectory("inline");
        var configured = session.CreateDirectory("configured");
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["DiskUploads:RootDirectoryPath"] = configured,
            ["DiskUploads:ThrowOnFileNotFound"] = "false"
        }).Build();
        using var provider = Provider(services => services.AddFileStorageFactory(stores => stores.AddDisk("uploads", disk => disk
            .FromConfiguration(configuration, "DiskUploads")
            .Configure(options => options.RootDirectoryPath = inline))));
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IFileStorageFactory>().Get("uploads");

        Assert.Empty(await store.GetFileAsync(Guid.NewGuid(), ct: TestCancellation.TestCt));
        await store.SaveFileAsync([9, 9], ct: TestCancellation.TestCt);
        Assert.True(Contains(inline, [9, 9]));
        Assert.False(Contains(configured, [9, 9]));
    }

    [Fact]
    public void Register_ReturnsThatInstance_AndDelegateRunsOncePerScope()
    {
        using var session = new IOTempSession();
        var shared = new LocalFileStorageService(new DiskFileStorageOptions { RootDirectoryPath = session.CreateDirectory("shared") });
        var calls = 0;
        using var provider = Provider(services => services.AddFileStorageFactory(stores => {
            stores.Register("shared", shared);
            stores.Register("made", _ => {
                calls++;
                return Store();
            });
        }));

        using (var first = provider.CreateScope()) {
            var factory = first.ServiceProvider.GetRequiredService<IFileStorageFactory>();
            Assert.Same(shared, factory.Get("shared"));
            factory.Get("made");
            factory.Get("made");
        }

        using (var second = provider.CreateScope())
            second.ServiceProvider.GetRequiredService<IFileStorageFactory>().Get("made");

        Assert.Equal(2, calls);
        Assert.False(Disposed(shared));
        shared.Dispose();
    }

    [Fact]
    public void Get_UnknownName_Throws_AndTryGetReturnsFalse()
    {
        using var provider = Provider(services => services.AddFileStorageFactory(stores => stores.Register("only", Store())));
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IFileStorageFactory>();

        var missing = Assert.Throws<InvalidOperationException>(() => factory.Get("other"));
        Assert.Contains("other", missing.Message, StringComparison.Ordinal);
        Assert.False(factory.TryGet("other", out _));
        Assert.False(factory.TryGet("  ", out _));
        Assert.Throws<InvalidOperationException>(() => factory.Get(""));
    }

    [Fact]
    public void AddDisk_DuplicateName_Throws()
    {
        using var session = new IOTempSession();
        var root = session.CreateDirectory("root");
        var services = new ServiceCollection();
        var ex = Assert.Throws<InvalidOperationException>(() => services.AddFileStorageFactory(stores => {
            stores.AddDisk("uploads", options => options.RootDirectoryPath = root);
            stores.AddDisk("Uploads", options => options.RootDirectoryPath = root);
        }));
        Assert.Contains("Uploads", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddDisk_DoesNotRegisterUnkeyedOptions()
    {
        using var session = new IOTempSession();
        var root = session.CreateDirectory("root");
        using var provider = Provider(services => services.AddFileStorageFactory(stores => stores.AddDisk("uploads", options => options.RootDirectoryPath = root)));
        Assert.Null(provider.GetService<DiskFileStorageOptions>());
    }

    [Fact]
    public async Task UseEncryption_CallsTheKeyedService_AndOmittedEncryptionDoesNot()
    {
        using var session = new IOTempSession();
        var keyed = Recording.Encryption();
        var unkeyed = Recording.Encryption();
        using var provider = Provider(services => {
            services.AddSingleton<ITwoKeyEncryptionService>(unkeyed.Service);
            services.AddKeyedSingleton<ITwoKeyEncryptionService>("a", keyed.Service);
            services.AddFileStorageFactory(stores => {
                stores.AddDisk("secret", disk => disk.Configure(options => options.RootDirectoryPath = session.CreateDirectory("secret")).UseEncryption("a"));
                stores.AddDisk("plain", options => options.RootDirectoryPath = session.CreateDirectory("plain"));
            });
        });
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IFileStorageFactory>();

        await Assert.ThrowsAnyAsync<Exception>(() => factory.Get("secret").SaveFileAsync([1], encrypt: true, keyId: "k", ct: TestCancellation.TestCt));
        Assert.Equal(1, keyed.Calls);
        Assert.Equal(0, unkeyed.Calls);

        var missing = await Assert.ThrowsAsync<InvalidOperationException>(() => factory.Get("plain").SaveFileAsync([1], encrypt: true, keyId: "k", ct: TestCancellation.TestCt));
        Assert.Contains("no encryption service", missing.Message, StringComparison.Ordinal);
        Assert.Equal(0, unkeyed.Calls);
    }

    [Fact]
    public async Task UseCompression_CallsTheNamedService()
    {
        using var session = new IOTempSession();
        var keyed = Recording.Compression();
        var unkeyed = Recording.Compression();
        using var provider = Provider(services => {
            services.AddSingleton<ICompressionService>(unkeyed.Service);
            services.AddKeyedSingleton<ICompressionService>("gzip", keyed.Service);
            services.AddFileStorageFactory(stores => {
                stores.AddDisk("packed", disk => disk.Configure(options => options.RootDirectoryPath = session.CreateDirectory("packed")).UseCompression("gzip"));
                stores.AddDisk("shared", options => options.RootDirectoryPath = session.CreateDirectory("shared"));
            });
        });
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IFileStorageFactory>();

        await factory.Get("packed").SaveFileAsync([1, 2, 3], compress: true, ct: TestCancellation.TestCt);
        await factory.Get("shared").SaveFileAsync([4, 5, 6], compress: true, ct: TestCancellation.TestCt);

        Assert.Equal(1, keyed.Calls);
        Assert.Equal(1, unkeyed.Calls);
    }

    [Fact]
    public async Task UseCompression_OmittedWithoutUnkeyedService_FailsTheCompressedSave()
    {
        using var session = new IOTempSession();
        using var provider = Provider(services => services.AddFileStorageFactory(stores => stores.AddDisk("plain", options => options.RootDirectoryPath = session.CreateDirectory("plain"))));
        using var scope = provider.CreateScope();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IFileStorageFactory>().Get("plain")
            .SaveFileAsync([1], compress: true, ct: TestCancellation.TestCt));
        Assert.Contains("no compression service", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_DisposesCreatedStoresWithTheScope_AndLeavesRegisteredInstances()
    {
        using var session = new IOTempSession();
        var root = session.CreateDirectory("root");
        var shared = new LocalFileStorageService(new DiskFileStorageOptions { RootDirectoryPath = session.CreateDirectory("shared") });
        using var provider = Provider(services => services.AddFileStorageFactory(stores => {
            stores.AddDisk("made", options => options.RootDirectoryPath = root);
            stores.Register("kept", shared);
        }));
        IFileStorageService created;
        using (var scope = provider.CreateScope()) {
            var factory = scope.ServiceProvider.GetRequiredService<IFileStorageFactory>();
            created = factory.Get("made");
            factory.Get("kept");
        }

        Assert.True(Disposed(created));
        Assert.False(Disposed(shared));
        shared.Dispose();
    }

    [Fact]
    public void Get_MissingEncryptionKey_NamesTheStoreAndKey()
    {
        using var session = new IOTempSession();
        using var provider = Provider(services => services.AddFileStorageFactory(stores => stores.AddDisk(
            "secret", disk => disk.Configure(options => options.RootDirectoryPath = session.CreateDirectory("secret")).UseEncryption("missing"))));
        using var scope = provider.CreateScope();
        var ex = Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IFileStorageFactory>().Get("secret"));
        Assert.Contains("secret", ex.Message, StringComparison.Ordinal);
        Assert.Contains("missing", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FromConfiguration_TwoDiskSections_WriteToThoseRoots()
    {
        using var session = new IOTempSession();
        var rootA = session.CreateDirectory("a");
        var rootB = session.CreateDirectory("b");
        var payload = new byte[] { 9, 8, 7 };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["FileStorage:Default"] = "a",
            ["FileStorage:Stores:a:Provider"] = "disk",
            ["FileStorage:Stores:a:RootDirectoryPath"] = rootA,
            ["FileStorage:Stores:b:Provider"] = "Disk",
            ["FileStorage:Stores:b:RootDirectoryPath"] = rootB
        }).Build();
        using var provider = Provider(services => services.AddFileStorageFactoryFromConfiguration(configuration));
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IFileStorageFactory>();

        Assert.Equal("a", factory.DefaultName);
        Assert.Equal(["a", "b"], factory.Names);
        await factory.Get("a").SaveFileAsync(payload, ct: TestCancellation.TestCt);
        Assert.True(Contains(rootA, payload));
        Assert.False(Contains(rootB, payload));
    }

    [Fact]
    public void FromConfiguration_NameThatRepeatsABuilderName_Throws()
    {
        using var session = new IOTempSession();
        var root = session.CreateDirectory("root");
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["FileStorage:Stores:uploads:Provider"] = "Disk",
            ["FileStorage:Stores:uploads:RootDirectoryPath"] = root
        }).Build();
        var ex = Assert.Throws<InvalidOperationException>(() => Provider(services => services.AddFileStorageFactory(
            configuration, stores => stores.AddDisk("Uploads", options => options.RootDirectoryPath = root))));
        Assert.Contains("uploads", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FromConfiguration_MissingProvider_NamesTheStoreAndProvider()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["FileStorage:Stores:archive:Provider"] = "S3"
        }).Build();
        var ex = Assert.Throws<InvalidOperationException>(() => Provider(services => services.AddFileStorageFactoryFromConfiguration(configuration)));
        Assert.Contains("archive", ex.Message, StringComparison.Ordinal);
        Assert.Contains("S3", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddDiskFileStorage_ThrowsWhenAnUnkeyedStoreExists()
    {
        using var session = new IOTempSession();
        var services = new ServiceCollection();
        services.AddSingleton<IFileStorageService>(Store());
        var ex = Assert.Throws<InvalidOperationException>(() => services.AddDiskFileStorage(disk => disk.Configure(options => options.RootDirectoryPath = session.CreateDirectory("root"))));
        Assert.Contains("unkeyed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ServiceProvider Provider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return services.BuildServiceProvider();
    }

    private static IFileStorageService Store() => DispatchProxy.Create<IFileStorageService, UnusedProxy>();

    private static bool Disposed(IFileStorageService service)
        => (bool)typeof(FileStorageServiceBase).GetField("Disposed", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(service)!;

    private static bool Contains(string root, byte[] payload)
    {
        if (!Directory.Exists(root))
            return false;

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)) {
            if (File.ReadAllBytes(file).AsSpan().SequenceEqual(payload))
                return true;
        }

        return false;
    }

    private class UnusedProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => throw new NotSupportedException(targetMethod?.Name);
    }
}
