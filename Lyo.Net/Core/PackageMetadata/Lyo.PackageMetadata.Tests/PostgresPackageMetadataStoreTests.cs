using System.Data.Common;
using Lyo.PackageMetadata.Postgres;
using Lyo.PackageMetadata.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lyo.PackageMetadata.Tests;

public sealed class PostgresPackageMetadataStoreTests
{
    private static readonly string LongRowSha512 =
        "ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f";

    private readonly PackageMetadataPostgresFixture _fixture;

    private IDbContextFactory<PackageMetadataDbContext> Factory => _fixture.ContextFactory;

    public PostgresPackageMetadataStoreTests(PackageMetadataPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task TryGetForFrameAsync_Uses_Longest_Matching_Prefix()
    {
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory);
        var meta = await store.TryGetForFrameAsync("", "Npgsql.Internal.NpgsqlConnector.ReadMessage", TestCt);
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
        var meta = await store.TryGetForFrameAsync("", "Npgsql.SomethingElse.Type.Method", TestCt);
        Assert.NotNull(meta);
        Assert.Equal("ShortMatch", meta.Name);
    }

    [Fact]
    public async Task TryGetForFrameAsync_Returns_Null_When_No_Prefix_Matches()
    {
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory);
        var meta = await store.TryGetForFrameAsync("", "Microsoft.EntityFrameworkCore.DbContext.SaveChanges", TestCt);
        Assert.Null(meta);
    }

    [Fact]
    public async Task Prefix_Catalog_Cache_Hit_On_Second_Bulk_Lookup()
    {
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory, new());
        Assert.Equal(0, store.PrefixCatalogHitCount);
        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.Internal.X"], TestCt);
        Assert.Equal(1, store.PrefixCatalogLoadCount);
        Assert.Equal(0, store.PrefixCatalogHitCount);
        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.SomethingElse.Z"], TestCt);
        Assert.Equal(1, store.PrefixCatalogLoadCount);
        Assert.Equal(1, store.PrefixCatalogHitCount);
    }

    [Fact]
    public async Task ClearPrefixCatalogCache_Forces_Next_Reload()
    {
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory, new());
        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.Internal.X"], TestCt);
        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.X"], TestCt);
        Assert.Equal(1, store.PrefixCatalogLoadCount);
        store.ClearPrefixCatalogCache();
        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.Internal.X"], TestCt);
        Assert.Equal(2, store.PrefixCatalogLoadCount);
    }

    [Fact]
    public async Task Prefix_Catalog_Caching_Disabled_Always_Loads_Each_Bulk()
    {
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory, new() { PrefixCatalogCaching = PostgresPrefixCatalogCachingMode.Disabled });
        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.Internal.X"], TestCt);
        Assert.Equal(1, store.PrefixCatalogLoadCount);
        Assert.Equal(0, store.PrefixCatalogHitCount);
        await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.SomethingElse.Z"], TestCt);
        Assert.Equal(2, store.PrefixCatalogLoadCount);
        Assert.Equal(0, store.PrefixCatalogHitCount);
        store.ClearPrefixCatalogCache();
        await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.Internal.X"], TestCt);
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
        await store.RegisterManyAsync([reg], TestCt);
        await using var ctx = await Factory.CreateDbContextAsync(TestCt);
        var entity = await ctx.Packages.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, TestCt);
        Assert.NotNull(entity);
        Assert.Equal("MIT OR Apache-2.0", entity.LicenseExpression);
        Assert.NotNull(entity.LicenseExpressionSyntaxJson);
        Assert.Contains("\"kind\":\"or\"", entity.LicenseExpressionSyntaxJson, StringComparison.Ordinal);
        Assert.Contains("MIT", entity.LicenseExpressionSyntaxJson, StringComparison.Ordinal);
        Assert.Contains("Apache-2.0", entity.LicenseExpressionSyntaxJson, StringComparison.Ordinal);
        var meta = await store.TryGetForFrameAsync("", "SpdxDemo.My.Type.M", TestCt);
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
        await store.RegisterManyAsync([reg], TestCt);
        await using var ctx = await Factory.CreateDbContextAsync(TestCt);
        var entity = await ctx.Packages.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, TestCt);
        Assert.NotNull(entity);
        Assert.Equal("1.2.3", entity.Version);
        Assert.Equal(ArtifactDigestAlgorithm.Sha512, entity.ArtifactDigestAlgorithm);
        Assert.Equal(LongRowSha512, entity.ArtifactDigestHex);
        var prefixes = await ctx.StackPrefixes.AsNoTracking().Where(p => p.PackageMetadataId == id).ToListAsync(TestCt);
        Assert.Single(prefixes);
        Assert.Equal("BulkDemo.", prefixes[0].NormalizedPrefix);
        var meta = await store.TryGetForFrameAsync("", "BulkDemo.My.Type.M", TestCt);
        Assert.NotNull(meta);
        Assert.Equal("BulkDemo", meta.Name);
    }

    [Fact]
    public async Task RegisterManyAsync_Invalidates_Prefix_Catalog_Cache()
    {
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory, new());
        var id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.Internal.X"], TestCt);
        Assert.Equal(1, store.PrefixCatalogLoadCount);
        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.X"], TestCt);
        Assert.Equal(1, store.PrefixCatalogLoadCount);
        Assert.Equal(1, store.PrefixCatalogHitCount);
        await store.RegisterManyAsync([new(["AfterReg."], new(id, PackageEcosystem.NuGet, "AfterReg", "1"))], TestCt);
        _ = await store.TryGetManyForStrippedMethodPrefixesAsync(["AfterReg.Type.M"], TestCt);
        Assert.Equal(2, store.PrefixCatalogLoadCount);
        Assert.Equal(1, store.PrefixCatalogHitCount);
    }

    [Fact]
    public async Task TryGetManyForStrippedMethodPrefixesAsync_Single_DbContext_Matches_Individual_Lookups()
    {
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory);
        var keys = new[] { "Npgsql.Internal.NpgsqlConnector.ReadMessage", "Npgsql.SomethingElse.Type.Method", "Microsoft.EntityFrameworkCore.DbContext.SaveChanges" };
        var bulk = await store.TryGetManyForStrippedMethodPrefixesAsync(keys, TestCt);
        foreach (var key in keys) {
            var single = await store.TryGetForFrameAsync("", key, TestCt);
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
        var bulk = await store.TryGetManyForStrippedMethodPrefixesAsync([], TestCt);
        Assert.Empty(bulk);
    }

    [Fact]
    public async Task RegisterManyAsync_SecondRegister_SamePrefix_ReplacesRow()
    {
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory);
        var id = Guid.Parse("10101010-1010-1010-1010-101010101010");
        await store.RegisterManyAsync([new(["ReReg."], new(id, PackageEcosystem.NuGet, "ReReg", "1"))], TestCt);
        await store.RegisterManyAsync([new(["ReReg."], new(id, PackageEcosystem.NuGet, "ReReg", "2"))], TestCt);
        var meta = await store.TryGetForFrameAsync("", "ReReg.Type.M", TestCt);
        Assert.NotNull(meta);
        Assert.Equal("2", meta.Version);
        await using var ctx = await Factory.CreateDbContextAsync(TestCt);
        var prefixes = await ctx.StackPrefixes.AsNoTracking().Where(p => p.PackageMetadataId == id).ToListAsync(TestCt);
        Assert.Single(prefixes);
        Assert.Equal("ReReg.", prefixes[0].NormalizedPrefix);
    }

    [Fact]
    public async Task RegisterManyAsync_DuplicateIdInBatch_LastRegistrationWins()
    {
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory);
        var id = Guid.Parse("20202020-2020-2020-2020-202020202020");
        await store.RegisterManyAsync(
            [new(["DupBatch."], new(id, PackageEcosystem.NuGet, "First", "1")), new(["DupBatch.Later."], new(id, PackageEcosystem.NuGet, "Second", "2"))], TestCt);

        var dropped = await store.TryGetForFrameAsync("", "DupBatch.Only", TestCt);
        var kept = await store.TryGetForFrameAsync("", "DupBatch.Later.Type.M", TestCt);
        Assert.Null(dropped);
        Assert.NotNull(kept);
        Assert.Equal("Second", kept.Name);
        Assert.Equal("2", kept.Version);
    }

    [Fact]
    public async Task RegisterManyAsync_DuplicateNormalizedPrefixes_StoresOneRow()
    {
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory);
        var id = Guid.Parse("30303030-3030-3030-3030-303030303030");
        await store.RegisterManyAsync([new(["DupPre", "DupPre."], new(id, PackageEcosystem.NuGet, "DupPre", "1"))], TestCt);
        await using var ctx = await Factory.CreateDbContextAsync(TestCt);
        var prefixes = await ctx.StackPrefixes.AsNoTracking().Where(p => p.PackageMetadataId == id).ToListAsync(TestCt);
        Assert.Single(prefixes);
        Assert.Equal("DupPre.", prefixes[0].NormalizedPrefix);
        var meta = await store.TryGetForFrameAsync("", "DupPre.Type.M", TestCt);
        Assert.Equal("DupPre", meta!.Name);
    }

    [Fact]
    public async Task RegisterManyAsync_PrefixClaimedByOtherPackage_MovesToNewOwner()
    {
        Assert.NotNull(Factory);
        var store = new PostgresPackageMetadataStore(Factory);
        var first = Guid.Parse("40404040-4040-4040-4040-404040404040");
        var second = Guid.Parse("40404040-4040-4040-4040-404040404041");
        await store.RegisterManyAsync([new(["StealMe.", "KeepA."], new(first, PackageEcosystem.NuGet, "First", "1"))], TestCt);
        await store.RegisterManyAsync([new(["StealMe."], new(second, PackageEcosystem.NuGet, "Second", "1"))], TestCt);
        var stolen = await store.TryGetForFrameAsync("", "StealMe.Type.M", TestCt);
        var kept = await store.TryGetForFrameAsync("", "KeepA.Type.M", TestCt);
        Assert.Equal("Second", stolen!.Name);
        Assert.Equal("First", kept!.Name);
    }

    [Fact]
    public async Task PrefixCatalogCache_RegisterDuringLoad_DoesNotPublishStaleSnapshot()
    {
        Assert.NotNull(Factory);
        var interceptor = new PauseAfterFirstStackPrefixRead();
        var factory = new InterceptingContextFactory(_fixture.ConnectionString, interceptor);
        var store = new PostgresPackageMetadataStore(factory, new());
        var id = Guid.Parse("50505050-5050-5050-5050-505050505050");
        var lookup = store.TryGetManyForStrippedMethodPrefixesAsync(["Npgsql.Internal.X"], TestCt).AsTask();
        try {
            await interceptor.QueryStarted.WaitAsync(TimeSpan.FromSeconds(20), TestCt);
            await store.RegisterManyAsync([new(["StaleGuard."], new(id, PackageEcosystem.NuGet, "StaleGuard", "9"))], TestCt);
        }
        finally {
            interceptor.Release();
        }

        await lookup;
        var found = await store.TryGetForFrameAsync("", "StaleGuard.Type.M", TestCt);
        Assert.Equal("StaleGuard", found?.Name);
        Assert.Equal("9", found?.Version);
    }

    private sealed class PauseAfterFirstStackPrefixRead : DbCommandInterceptor
    {
        private readonly TaskCompletionSource _queryStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _paused;

        public Task QueryStarted => _queryStarted.Task;

        public void Release() => _release.TrySetResult();

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _paused, 1) == 0 && command.CommandText.Contains("stack_prefix", StringComparison.Ordinal)) {
                _queryStarted.TrySetResult();
                await _release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            return result;
        }
    }

    private sealed class InterceptingContextFactory : IDbContextFactory<PackageMetadataDbContext>
    {
        private readonly DbContextOptions<PackageMetadataDbContext> _options;

        public InterceptingContextFactory(string connectionString, DbCommandInterceptor interceptor)
            => _options = new DbContextOptionsBuilder<PackageMetadataDbContext>().UseNpgsql(
                    connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PostgresPackageMetadataOptions.Schema))
                .AddInterceptors(interceptor)
                .Options;

        public PackageMetadataDbContext CreateDbContext() => new(_options);

        public Task<PackageMetadataDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
}