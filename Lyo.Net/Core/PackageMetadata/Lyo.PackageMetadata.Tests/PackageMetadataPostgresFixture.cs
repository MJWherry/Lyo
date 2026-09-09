using Lyo.PackageMetadata.Postgres;
using Lyo.PackageMetadata.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.PackageMetadata.Tests;

public sealed class PackageMetadataPostgresFixture : PostgresServiceFixtureBase<PackageMetadataDbContext>
{
    private static readonly string LongRowSha512 =
        "ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f";

    protected override void ConfigureServices(IServiceCollection services, string connectionString) =>
        services.AddDbContextFactory<PackageMetadataDbContext>(
            opts => opts.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PostgresPackageMetadataOptions.Schema)));

    protected override async ValueTask OnMigratedAsync(CancellationToken cancellationToken)
    {
        await using var ctx = await ContextFactory.CreateDbContextAsync(cancellationToken);
        var shortId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-aaaaaaaaaaaa");
        var longId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var t0 = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        ctx.Packages.AddRange(
            new PackageMetadataEntity {
                Id = shortId,
                Ecosystem = PackageEcosystem.NuGet,
                Name = "ShortMatch",
                ArtifactDigestAlgorithm = ArtifactDigestAlgorithm.None,
                CreatedAt = t0,
                UpdatedAt = t0
            }, new PackageMetadataEntity {
                Id = longId,
                Ecosystem = PackageEcosystem.NuGet,
                Name = "LongMatch",
                Version = "2.0",
                ArtifactDigestAlgorithm = ArtifactDigestAlgorithm.Sha512,
                ArtifactDigestHex = LongRowSha512,
                CreatedAt = t0,
                UpdatedAt = t0
            });

        ctx.StackPrefixes.AddRange(
            new PackageStackPrefixEntity { Id = Guid.NewGuid(), PackageMetadataId = shortId, NormalizedPrefix = "Npgsql." },
            new PackageStackPrefixEntity { Id = Guid.NewGuid(), PackageMetadataId = longId, NormalizedPrefix = "Npgsql.Internal." });

        await ctx.SaveChangesAsync(cancellationToken);
    }
}
