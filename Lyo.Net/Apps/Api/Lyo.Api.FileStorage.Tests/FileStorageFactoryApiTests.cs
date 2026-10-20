using System.Net;
using System.Net.Http.Json;
using Lyo.Api.FileStorage.Models;
using Lyo.FileMetadataStore.Postgres;
using Lyo.FileStorage;
using Lyo.Cache;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Models;
using Lyo.IO.Temp.Models;
using Lyo.KeyStore;
using Lyo.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Api.FileStorage.Tests;

public sealed class FileStorageFactoryApiTests
{
    [Fact]
    public async Task GetStores_ListsNames_AndSaveUnderOneStoreMissesTheOther()
    {
        using var session = new IOTempSession();
        var rootA = session.CreateDirectory("a");
        var rootB = session.CreateDirectory("b");
        var payload = new byte[] { 1, 2, 3, 4 };
        await using var app = await Start(services => {
            services.AddLocalCache();
            services.AddFileStorageFactory(stores => {
                stores.AddDisk("b", options => options.RootDirectoryPath = rootB);
                stores.AddDisk("a", options => options.RootDirectoryPath = rootA);
            });
        });
        using var client = Client(app);

        var listed = await client.GetFromJsonAsync<FileStorageStoreResponse[]>("/FileStorage/stores", TestCancellation.TestCt);
        Assert.Equal(["a", "b"], listed!.Select(store => store.Name).ToArray());
        Assert.All(listed, store => Assert.Null(store.FileMetadataRoute));
        Assert.Contains(listed, store => store.Route == "FileStorage/a");

        var saved = await client.PostAsJsonAsync("/FileStorage/a/files/save", new SaveFileRequest(payload), TestCancellation.TestCt);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.True(Contains(rootA, payload));
        Assert.False(Contains(rootB, payload));
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/FileStorage/a/files/" + Guid.NewGuid() + "/access-links", JsonContent.Create(new { }), TestCancellation.TestCt)).StatusCode);
    }

    [Fact]
    public async Task Default_ServesTheBareRoute()
    {
        using var session = new IOTempSession();
        var rootA = session.CreateDirectory("a");
        var rootB = session.CreateDirectory("b");
        var payload = new byte[] { 9, 8, 7 };
        await using var app = await Start(services => {
            services.AddLocalCache();
            services.AddFileStorageFactory(stores => {
                stores.AddDisk("a", options => options.RootDirectoryPath = rootA);
                stores.AddDisk("b", options => options.RootDirectoryPath = rootB);
            });
            var catalog = (FileStorageFactoryCatalog)services.Single(descriptor => descriptor.ServiceType == typeof(FileStorageFactoryCatalog)).ImplementationInstance!;
            catalog.DefaultName = "a";
        }, new() { DirectUploadPath = "" });

        using var client = Client(app);
        var saved = await client.PostAsJsonAsync("/FileStorage/files/save", new SaveFileRequest(payload), TestCancellation.TestCt);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.True(Contains(rootA, payload));
        Assert.False(Contains(rootB, payload));
    }

    [Fact]
    public void ReservedStoreName_ThrowsWhenMapped()
    {
        using var session = new IOTempSession();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [] });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddFileMetadataStoreDbContextFactory(options => options.ConnectionString = "Host=127.0.0.1;Database=none;Username=none;Password=none");
        builder.Services.AddFileStorageFactory(stores => stores.AddDisk("files", options => options.RootDirectoryPath = session.CreateDirectory("files")));
        using var app = builder.Build();
        var ex = Assert.Throws<InvalidOperationException>(() => app.BuildFileStorageApi());
        Assert.Contains("files", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NoFactory_KeepsTheKeyedRoute()
    {
        using var session = new IOTempSession();
        var root = session.CreateDirectory("keyed");
        var payload = new byte[] { 4, 5, 6 };
        await using var app = await Start(services => {
            services.AddLocalCache();
            services.AddKeyedSingleton<IFileStorageService>(FileStorageApiOptions.DefaultServiceKey, new LocalFileStorageService(new DiskFileStorageOptions { RootDirectoryPath = root }));
        });
        using var client = Client(app);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/FileStorage/stores", TestCancellation.TestCt)).StatusCode);
        var saved = await client.PostAsJsonAsync("/FileStorage/files/save", new SaveFileRequest(payload), TestCancellation.TestCt);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.True(Contains(root, payload));
    }

    [Fact]
    public async Task KeyIds_FollowTheStoresEncryptionKey()
    {
        using var session = new IOTempSession();
        await using var app = await Start(services => {
            services.AddLocalCache();
            services.AddFileStorageFactory(stores => {
                stores.AddDisk("locked", disk => disk.Configure(options => options.RootDirectoryPath = session.CreateDirectory("locked")).UseEncryption("kek"));
                stores.AddDisk("open", options => options.RootDirectoryPath = session.CreateDirectory("open"));
            });
            services.AddKeyedSingleton<IKeyStore>("kek", new ListingKeyStore("locked-key"));
            services.AddSingleton<IKeyStore>(new ListingKeyStore("shared-key"));
        });
        using var client = Client(app);

        Assert.Equal(["locked-key"], await client.GetFromJsonAsync<string[]>("/FileStorage/locked/key-ids", TestCancellation.TestCt));
        Assert.Equal(["shared-key"], await client.GetFromJsonAsync<string[]>("/FileStorage/open/key-ids", TestCancellation.TestCt));
    }

    private static async Task<WebApplication> Start(Action<IServiceCollection> configure, FileStorageApiOptions? options = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [] });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddFileMetadataStoreDbContextFactory(options => options.ConnectionString = "Host=127.0.0.1;Database=none;Username=none;Password=none");
        configure(builder.Services);
        var app = builder.Build();
        app.BuildFileStorageApi(options);
        await app.StartAsync();
        return app;
    }

    private static HttpClient Client(WebApplication app) => new() { BaseAddress = new Uri(app.Urls.First()) };

    private static bool Contains(string root, byte[] payload)
    {
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)) {
            if (File.ReadAllBytes(file).AsSpan().SequenceEqual(payload))
                return true;
        }

        return false;
    }

    private sealed class ListingKeyStore(string id) : IKeyStore, IKeyInventoryStore
    {
        public Task<IReadOnlyList<string>> GetAvailableKeyIdsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>([id]);

        public Task<IReadOnlyList<string>> GetAvailableVersionsAsync(string keyId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>([]);

        public byte[]? GetKey(string keyId, string? version = null) => null;

        public Task<byte[]?> GetKeyAsync(string keyId, string? version = null, CancellationToken ct = default) => Task.FromResult<byte[]?>(null);

        public byte[]? GetCurrentKey(string keyId) => null;

        public Task<byte[]?> GetCurrentKeyAsync(string keyId, CancellationToken ct = default) => Task.FromResult<byte[]?>(null);

        public string? GetCurrentVersion(string keyId) => null;

        public Task<string?> GetCurrentVersionAsync(string keyId, CancellationToken ct = default) => Task.FromResult<string?>(null);

        public void AddKey(string keyId, string version, byte[] key) { }

        public Task AddKeyAsync(string keyId, string version, byte[] key, CancellationToken ct = default) => Task.CompletedTask;

        public void AddKeyFromString(string keyId, string version, string keyString) { }

        public Task AddKeyFromStringAsync(string keyId, string version, string keyString, CancellationToken ct = default) => Task.CompletedTask;

        public void SetCurrentVersion(string keyId, string version) { }

        public Task SetCurrentVersionAsync(string keyId, string version, CancellationToken ct = default) => Task.CompletedTask;

        public bool HasKey(string keyId, string? version = null) => false;

        public Task<bool> HasKeyAsync(string keyId, string? version = null, CancellationToken ct = default) => Task.FromResult(false);

        public KeyMetadata? GetKeyMetadata(string keyId, string version) => null;

        public Task<KeyMetadata?> GetKeyMetadataAsync(string keyId, string version, CancellationToken ct = default) => Task.FromResult<KeyMetadata?>(null);

        public void SetKeyMetadata(string keyId, string version, KeyMetadata metadata) { }

        public Task SetKeyMetadataAsync(string keyId, string version, KeyMetadata metadata, CancellationToken ct = default) => Task.CompletedTask;

        public byte[]? GetSaltForVersion(string keyId, string version) => null;

        public Task<byte[]?> GetSaltForVersionAsync(string keyId, string version, CancellationToken ct = default) => Task.FromResult<byte[]?>(null);

        public string UpdateKey(string keyId, byte[] key) => "1";

        public Task<string> UpdateKeyAsync(string keyId, byte[] key, CancellationToken ct = default) => Task.FromResult("1");

        public string UpdateKeyFromString(string keyId, string keyString) => "1";

        public Task<string> UpdateKeyFromStringAsync(string keyId, string keyString, CancellationToken ct = default) => Task.FromResult("1");
    }
}
