using Bogus;
using Lyo.ChangeTracker.Postgres;
using Lyo.ChangeTracker.Postgres.Database;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.ChangeTracker.Seed;

/// <summary>EF seed of change entries. Schema <c>change_tracker</c>.</summary>
public sealed class ChangeTrackerEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "Change tracker";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresChangeTrackerOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<ChangeTrackerDbContext>().UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema)).Options;
        return new ChangeTrackerDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        graph.Entity(
            options.Count, i => new ChangeEntryEntity {
                Id = LyoGuid.CreateV7(),
                Timestamp = now,
                ChangeType = "Updated",
                Message = faker.Lorem.Sentence(),
                OldValuesJson = "{}",
                ChangedPropertiesJson = """{"title":"renamed"}""",
                SubjectEntityType = "order",
                SubjectEntityId = $"order-{i}",
                ActorEntityType = "person",
                ActorEntityId = $"person-{i}",
                CreatedTimestamp = now
            });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<ChangeTrackerDbContext> ef)
            throw new SeedException("Change tracker OnClear requires EfSeedTransport<ChangeTrackerDbContext>.");

        await ef.Context.Changes.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}