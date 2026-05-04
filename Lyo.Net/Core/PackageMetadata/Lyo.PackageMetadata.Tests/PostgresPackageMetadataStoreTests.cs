using Lyo.PackageMetadata.Postgres;
using Lyo.PackageMetadata.Postgres.Database;
using Lyo.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace Lyo.PackageMetadata.Tests;

public sealed class PostgresPackageMetadataStoreTests : IAsyncLifetime
{
    private static readonly string LongRowSha512 =
        "ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly ITestOutputHelper _output;
    private IServiceProvider? _serviceProvider;
    private IDbContextFactory<PackageMetadataDbContext>? _factory;

    public PostgresPackageMetadataStoreTests(ITestOutputHelper output) => _output = output;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        var connectionString = _container.GetConnectionString();
        var services = new ServiceCollection();
        services.AddLogging(b => {
            b.AddProvider(new XunitLoggerProvider(_output));
            b.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddDbContextFactory<PackageMetadataDbContext>(opts => opts.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PostgresPackageMetadataOptions.Schema)));

        _serviceProvider = services.BuildServiceProvider();
        _factory = _serviceProvider.GetRequiredService<IDbContextFactory<PackageMetadataDbContext>>();

        await using var ctx = await _factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        await ctx.Database.MigrateAsync(TestContext.Current.CancellationToken);

        var shortId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-aaaaaaaaaaaa");
        var longId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var t0 = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        ctx.Packages.AddRange(
            new PackageMetadataEntity { Id = shortId, Name = "ShortMatch", CreatedAt = t0, UpdatedAt = t0 },
            new PackageMetadataEntity {
                Id = longId,
                Name = "LongMatch",
                Version = "2.0",
                PackageFileSha512Hex = LongRowSha512,
                CreatedAt = t0,
                UpdatedAt = t0
            });
        ctx.StackPrefixes.AddRange(
            new PackageStackPrefixEntity { Id = Guid.NewGuid(), PackageMetadataId = shortId, NormalizedPrefix = "Npgsql." },
            new PackageStackPrefixEntity { Id = Guid.NewGuid(), PackageMetadataId = longId, NormalizedPrefix = "Npgsql.Internal." });
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_serviceProvider is IDisposable d)
            d.Dispose();

        await _container.DisposeAsync();
    }

    [Fact]
    public async Task TryGetForFrameAsync_Uses_Longest_Matching_Prefix()
    {
        Assert.NotNull(_factory);
        var store = new PostgresPackageMetadataStore(_factory);

        var meta = await store.TryGetForFrameAsync("", "Npgsql.Internal.NpgsqlConnector.ReadMessage", TestContext.Current.CancellationToken);

        Assert.NotNull(meta);
        Assert.Equal("LongMatch", meta!.Name);
        Assert.Equal("2.0", meta.Version);
        Assert.Equal(LongRowSha512, meta.PackageFileSha512Hex);
        Assert.NotNull(meta.CreatedAt);
        Assert.NotNull(meta.UpdatedAt);
    }

    [Fact]
    public async Task TryGetForFrameAsync_Falls_Back_To_Shorter_Prefix()
    {
        Assert.NotNull(_factory);
        var store = new PostgresPackageMetadataStore(_factory);

        var meta = await store.TryGetForFrameAsync("", "Npgsql.SomethingElse.Type.Method", TestContext.Current.CancellationToken);

        Assert.NotNull(meta);
        Assert.Equal("ShortMatch", meta!.Name);
    }

    [Fact]
    public async Task TryGetForFrameAsync_Returns_Null_When_No_Prefix_Matches()
    {
        Assert.NotNull(_factory);
        var store = new PostgresPackageMetadataStore(_factory);

        var meta = await store.TryGetForFrameAsync("", "Microsoft.EntityFrameworkCore.DbContext.SaveChanges", TestContext.Current.CancellationToken);

        Assert.Null(meta);
    }

    [Fact]
    public async Task Prefix_catalog_cache_hit_on_second_bulk_lookup()
    {
        Assert.NotNull(_factory);
        var store =
            new PostgresPackageMetadataStore(_factory, new PostgresPackageMetadataOptions());

        Assert.Equal(0, store.PrefixCatalogHitCount);

        _ =
            await store.TryGetManyForStrippedMethodPrefixesAsync(
                ["Npgsql.Internal.X"],
                TestContext.Current.CancellationToken);

        Assert.Equal(1, store.PrefixCatalogLoadCount);
        Assert.Equal(0, store.PrefixCatalogHitCount);

        _ =
            await store.TryGetManyForStrippedMethodPrefixesAsync(
                ["Npgsql.SomethingElse.Z"],
                TestContext.Current.CancellationToken);

        Assert.Equal(1, store.PrefixCatalogLoadCount);
        Assert.Equal(1, store.PrefixCatalogHitCount);
    }

    [Fact]
    public async Task ClearPrefixCatalogCache_forces_next_reload()
    {
        Assert.NotNull(_factory);
        var store = new PostgresPackageMetadataStore(_factory, new PostgresPackageMetadataOptions());

        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.Internal.X"], TestContext.Current.CancellationToken);
        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.X"], TestContext.Current.CancellationToken);
        Assert.Equal(1, store.PrefixCatalogLoadCount);

        store.ClearPrefixCatalogCache();

        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.Internal.X"], TestContext.Current.CancellationToken);
        Assert.Equal(2, store.PrefixCatalogLoadCount);
    }

    [Fact]
    public async Task Prefix_catalog_caching_disabled_always_loads_each_bulk()
    {
        Assert.NotNull(_factory);
        var store =
            new PostgresPackageMetadataStore(_factory, new PostgresPackageMetadataOptions {
                PrefixCatalogCaching = PostgresPrefixCatalogCachingMode.Disabled
            });

        _ =
            await store.TryGetManyForStrippedMethodPrefixesAsync(
                ["Npgsql.Internal.X"],
                TestContext.Current.CancellationToken);
        Assert.Equal(1, store.PrefixCatalogLoadCount);
        Assert.Equal(0, store.PrefixCatalogHitCount);

        await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.SomethingElse.Z"], TestContext.Current.CancellationToken);

        Assert.Equal(2, store.PrefixCatalogLoadCount);
        Assert.Equal(0, store.PrefixCatalogHitCount);

        store.ClearPrefixCatalogCache();

        await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.Internal.X"], TestContext.Current.CancellationToken);

        Assert.Equal(3, store.PrefixCatalogLoadCount);
        Assert.Equal(0, store.PrefixCatalogHitCount);
    }

    [Fact]
    public async Task RegisterManyAsync_Upserts_Package_And_Prefixes()
    {
        Assert.NotNull(_factory);
        var store = new PostgresPackageMetadataStore(_factory);
        var id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var reg = new PackageMetadataRegistration(
            ["BulkDemo."],
            new PackageMetadata(id, "BulkDemo", Version: "1.2.3", PackageFileSha512Hex: LongRowSha512));

        await store.RegisterManyAsync([reg], TestContext.Current.CancellationToken);

        await using var ctx = await _factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var entity = await ctx.Packages.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, TestContext.Current.CancellationToken);
        Assert.NotNull(entity);
        Assert.Equal("1.2.3", entity!.Version);
        Assert.Equal(LongRowSha512, entity.PackageFileSha512Hex);
        var prefixes = await ctx.StackPrefixes.AsNoTracking().Where(p => p.PackageMetadataId == id).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(prefixes);
        Assert.Equal("BulkDemo.", prefixes[0].NormalizedPrefix);

        var meta = await store.TryGetForFrameAsync("", "BulkDemo.My.Type.M", TestContext.Current.CancellationToken);
        Assert.NotNull(meta);
        Assert.Equal("BulkDemo", meta!.Name);
    }

    [Fact]
    public async Task RegisterManyAsync_invalidates_prefix_catalog_cache()
    {
        Assert.NotNull(_factory);
        var store = new PostgresPackageMetadataStore(_factory, new PostgresPackageMetadataOptions());
        var id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.Internal.X"], TestContext.Current.CancellationToken);
        Assert.Equal(1, store.PrefixCatalogLoadCount);

        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.X"], TestContext.Current.CancellationToken);
        Assert.Equal(1, store.PrefixCatalogLoadCount);
        Assert.Equal(1, store.PrefixCatalogHitCount);

        await store.RegisterManyAsync([
            new PackageMetadataRegistration(["AfterReg."], new PackageMetadata(id, "AfterReg", Version: "1"))
        ], TestContext.Current.CancellationToken);

        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["AfterReg.Type.M"], TestContext.Current.CancellationToken);
        Assert.Equal(2, store.PrefixCatalogLoadCount);
        Assert.Equal(1, store.PrefixCatalogHitCount);
    }

    [Fact]
    public async Task TryGetManyForStrippedMethodPrefixesAsync_Single_DbContext_Matches_Individual_Lookups()
    {
        Assert.NotNull(_factory);
        var store = new PostgresPackageMetadataStore(_factory);

        var keys = new[] {
            "Npgsql.Internal.NpgsqlConnector.ReadMessage",
            "Npgsql.SomethingElse.Type.Method",
            "Microsoft.EntityFrameworkCore.DbContext.SaveChanges"
        };

        var bulk = await store.TryGetManyForStrippedMethodPrefixesAsync(keys, TestContext.Current.CancellationToken);

        foreach (var key in keys) {
            var single = await store.TryGetForFrameAsync("", key, TestContext.Current.CancellationToken);
            Assert.Equal(single?.Id, bulk[key]?.Id);
            Assert.Equal(single?.Name, bulk[key]?.Name);
        }

        Assert.Equal("LongMatch", bulk[keys[0]]!.Name);
        Assert.Equal("ShortMatch", bulk[keys[1]]!.Name);
        Assert.Null(bulk[keys[2]]);
    }

    [Fact]
    public async Task TryGetManyForStrippedMethodPrefixesAsync_Returns_Empty_When_Input_Empty()
    {
        Assert.NotNull(_factory);
        var store = new PostgresPackageMetadataStore(_factory);

        var bulk = await store.TryGetManyForStrippedMethodPrefixesAsync([], TestContext.Current.CancellationToken);

        Assert.Empty(bulk);
    }
}
