using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.PackageMetadata.Postgres;
using Lyo.PackageMetadata.Postgres.Database;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.PackageMetadata.Seed;

/// <summary>EF seed of NuGet package rows, each with one stack prefix. Schema <c>package_metadata</c>.</summary>
public sealed class PackageMetadataEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "Package metadata";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresPackageMetadataOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<PackageMetadataDbContext>().UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema)).Options;
        return new PackageMetadataDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var now = DateTime.UtcNow;
        graph.Entity(
                options.Count, i => new PackageMetadataEntity {
                    Id = LyoGuid.CreateV7(),
                    Ecosystem = PackageEcosystem.NuGet,
                    Name = $"Lyo.Sample.Library.{i}",
                    Version = "1.0.0",
                    ArtifactDigestAlgorithm = ArtifactDigestAlgorithm.None,
                    Title = $"Sample library {i}",
                    CreatedAt = now,
                    UpdatedAt = now
                })
            .ForEach(_ => 1, (package, _) => new PackageStackPrefixEntity { Id = LyoGuid.CreateV7(), PackageMetadataId = package.Id, NormalizedPrefix = package.Name + "." });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<PackageMetadataDbContext> ef)
            throw new SeedException("Package metadata OnClear requires EfSeedTransport<PackageMetadataDbContext>.");

        var db = ef.Context;
        await db.StackPrefixes.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Packages.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}