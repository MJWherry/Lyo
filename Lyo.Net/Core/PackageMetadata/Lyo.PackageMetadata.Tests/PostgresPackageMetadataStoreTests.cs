using Lyo.PackageMetadata.Postgres;
using Lyo.PackageMetadata.Postgres.Database;
using Microsoft.EntityFrameworkCore;

namespace Lyo.PackageMetadata.Tests;

public sealed class PostgresPackageMetadataStoreTests
{
    private static readonly string LongRowSha512 =
        "ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f";

    private readonly PackageMetadataPostgresFixture _fixture;

    private IDbContextFactory<PackageMetadataDbContext> Factory => _fixture.Factory;

    public PostgresPackageMetadataStoreTests(PackageMetadataPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task TryGetForFrameAsync_Uses_Longest_Matching_Prefix()
    {
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory);
        var meta = await store.TryGetForFrameAsync("", "Npgsql.Internal.NpgsqlConnector.ReadMessage", TestContext.Current.CancellationToken);
        Assert.NotNull(meta);
        Assert.Equal("LongMatch", meta.Name);
        Assert.Equal("2.0", meta.Version);
        Assert.Equal(PackageEcosystem.NuGet, meta.Ecosystem);
        Assert.Equal(ArtifactDigestAlgorithm.Sha512, meta.ArtifactDigestAlgorithm);
        Assert.Equal(LongRowSha512, meta.ArtifactDigestHex);
        Assert.NotNull(meta.CreatedAt);
        Assert.NotNull(meta.UpdatedAt);
    }

    [Fact]
    public async Task TryGetForFrameAsync_Falls_Back_To_Shorter_Prefix()
    {
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory);
        var meta = await store.TryGetForFrameAsync("", "Npgsql.SomethingElse.Type.Method", TestContext.Current.CancellationToken);
        Assert.NotNull(meta);
        Assert.Equal("ShortMatch", meta.Name);
    }

    [Fact]
    public async Task TryGetForFrameAsync_Returns_Null_When_No_Prefix_Matches()
    {
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory);
        var meta = await store.TryGetForFrameAsync("", "Microsoft.EntityFrameworkCore.DbContext.SaveChanges", TestContext.Current.CancellationToken);
        Assert.Null(meta);
    }

    [Fact]
    public async Task Prefix_catalog_cache_hit_on_second_bulk_lookup()
    {
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory, new());
        Assert.Equal(0, store.PrefixCatalogHitCount);
        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.Internal.X"], TestContext.Current.CancellationToken);
        Assert.Equal(1, store.PrefixCatalogLoadCount);
        Assert.Equal(0, store.PrefixCatalogHitCount);
        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.SomethingElse.Z"], TestContext.Current.CancellationToken);
        Assert.Equal(1, store.PrefixCatalogLoadCount);
        Assert.Equal(1, store.PrefixCatalogHitCount);
    }

    [Fact]
    public async Task ClearPrefixCatalogCache_forces_next_reload()
    {
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory, new());
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
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory, new() { PrefixCatalogCaching = PostgresPrefixCatalogCachingMode.Disabled });
        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.Internal.X"], TestContext.Current.CancellationToken);
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
    public async Task RegisterManyAsync_Persists_LicenseExpressionSyntaxJson_From_LicenseExpression()
    {
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory);
        var id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var reg = new PackageMetadataRegistration(["SpdxDemo."], new(id, PackageEcosystem.NuGet, "SpdxDemo", "1.0", LicenseExpression: "MIT OR Apache-2.0"));
        await store.RegisterManyAsync([reg], TestContext.Current.CancellationToken);
        await using var ctx = await Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var entity = await ctx.Packages.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, TestContext.Current.CancellationToken);
        Assert.NotNull(entity);
        Assert.Equal("MIT OR Apache-2.0", entity.LicenseExpression);
        Assert.NotNull(entity.LicenseExpressionSyntaxJson);
        Assert.Contains("\"kind\":\"or\"", entity.LicenseExpressionSyntaxJson, StringComparison.Ordinal);
        Assert.Contains("MIT", entity.LicenseExpressionSyntaxJson, StringComparison.Ordinal);
        Assert.Contains("Apache-2.0", entity.LicenseExpressionSyntaxJson, StringComparison.Ordinal);
        var meta = await store.TryGetForFrameAsync("", "SpdxDemo.My.Type.M", TestContext.Current.CancellationToken);
        Assert.NotNull(meta);
        Assert.Equal("or", meta.LicenseExpressionSyntax!.Kind);
        Assert.Equal("MIT", meta.LicenseExpressionSyntax.Left!.Identifier);
    }

    [Fact]
    public async Task RegisterManyAsync_Upserts_Package_And_Prefixes()
    {
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory);
        var id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var reg = new PackageMetadataRegistration(["BulkDemo."], new(id, PackageEcosystem.NuGet, "BulkDemo", "1.2.3", ArtifactDigestAlgorithm.Sha512, LongRowSha512));
        await store.RegisterManyAsync([reg], TestContext.Current.CancellationToken);
        await using var ctx = await Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var entity = await ctx.Packages.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, TestContext.Current.CancellationToken);
        Assert.NotNull(entity);
        Assert.Equal("1.2.3", entity.Version);
        Assert.Equal(ArtifactDigestAlgorithm.Sha512, entity.ArtifactDigestAlgorithm);
        Assert.Equal(LongRowSha512, entity.ArtifactDigestHex);
        var prefixes = await ctx.StackPrefixes.AsNoTracking().Where(p => p.PackageMetadataId == id).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(prefixes);
        Assert.Equal("BulkDemo.", prefixes[0].NormalizedPrefix);
        var meta = await store.TryGetForFrameAsync("", "BulkDemo.My.Type.M", TestContext.Current.CancellationToken);
        Assert.NotNull(meta);
        Assert.Equal("BulkDemo", meta.Name);
    }

    [Fact]
    public async Task RegisterManyAsync_invalidates_prefix_catalog_cache()
    {
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory, new());
        var id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.Internal.X"], TestContext.Current.CancellationToken);
        Assert.Equal(1, store.PrefixCatalogLoadCount);
        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.X"], TestContext.Current.CancellationToken);
        Assert.Equal(1, store.PrefixCatalogLoadCount);
        Assert.Equal(1, store.PrefixCatalogHitCount);
        await store.RegisterManyAsync([new(["AfterReg."], new(id, PackageEcosystem.NuGet, "AfterReg", "1"))], TestContext.Current.CancellationToken);
        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["AfterReg.Type.M"], TestContext.Current.CancellationToken);
        Assert.Equal(2, store.PrefixCatalogLoadCount);
        Assert.Equal(1, store.PrefixCatalogHitCount);
    }

    [Fact]
    public async Task TryGetManyForStrippedMethodPrefixesAsync_Single_DbContext_Matches_Individual_Lookups()
    {
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory);
        var keys = new[] { "Npgsql.Internal.NpgsqlConnector.ReadMessage", "Npgsql.SomethingElse.Type.Method", "Microsoft.EntityFrameworkCore.DbContext.SaveChanges" };
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
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory);
        var bulk = await store.TryGetManyForStrippedMethodPrefixesAsync([], TestContext.Current.CancellationToken);
        Assert.Empty(bulk);
    }
}