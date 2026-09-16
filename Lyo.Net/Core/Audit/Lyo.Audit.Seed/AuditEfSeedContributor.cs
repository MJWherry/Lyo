using Bogus;
using Lyo.Audit.Postgres;
using Lyo.Audit.Postgres.Database;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Audit.Seed;

/// <summary>EF seed of audit events, each paired with one change row. Schema <c>audit</c>.</summary>
public sealed class AuditEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "Audit";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresAuditOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .Options;
        return new AuditDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        var eventIds = Enumerable.Range(0, options.Count).Select(_ => LyoGuid.CreateV7()).ToArray();
        graph.Entity(options.Count, i => new AuditEventEntity {
            Id = eventIds[i],
            EventType = "Updated",
            Timestamp = now,
            Message = faker.Lorem.Sentence(),
            MetadataJson = "{}",
            SubjectEntityType = "order",
            SubjectEntityId = $"order-{i}",
            ActorEntityType = "person",
            ActorEntityId = $"person-{i}",
            CreatedTimestamp = now
        });
        graph.Entity(options.Count, i => new AuditChangeEntity {
            Id = LyoGuid.CreateV7(),
            Timestamp = now,
            OldValuesJson = """{"name":null}""",
            ChangedPropertiesJson = """{"title":"renamed"}""",
            SubjectEntityType = "AuditEvent",
            SubjectEntityId = eventIds[i].ToString(),
            ActorEntityType = "person",
            ActorEntityId = $"person-{i}",
            CreatedTimestamp = now
        });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<AuditDbContext> ef)
            throw new SeedException("Audit OnClear requires EfSeedTransport<AuditDbContext>.");

        await ef.Context.AuditChanges.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await ef.Context.AuditEvents.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
