using Bogus;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Seed;
using Lyo.StateMachine.Postgres;
using Lyo.StateMachine.Postgres.Database;
using Microsoft.EntityFrameworkCore;

namespace Lyo.StateMachine.Seed;

/// <summary>EF seed of machine definitions, one instance each, and one transition. Schema <c>state_machine</c>.</summary>
public sealed class StateMachineEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "State machine";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresStateMachineOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<StateMachineDbContext>().UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema)).Options;
        return new StateMachineDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        var definitions = graph.Entity(
            options.Count, i => new StateMachineDefinitionEntity {
                Name = $"{faker.Hacker.Noun()}-{i}",
                Revision = 1,
                Document = """{"initial":"Idle"}""",
                CreatedTimestamp = now
            });

        definitions.ForEach(
            _ => 1, (definition, _) => new StateMachineInstanceEntity {
                MachineName = definition.Name,
                InstanceKey = "seed",
                State = "Running",
                Version = 1,
                EnteredTimestamp = now,
                UpdatedTimestamp = now,
                DataJson = "{}"
            });

        definitions.ForEach(
            _ => 1, (definition, _) => new StateMachineTransitionEntity {
                Id = LyoGuid.CreateV7(),
                MachineName = definition.Name,
                InstanceKey = "seed",
                FromState = "Idle",
                ToState = "Running",
                Trigger = "Start",
                Version = 1,
                OccurredTimestamp = now
            });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<StateMachineDbContext> ef)
            throw new SeedException("State machine OnClear requires EfSeedTransport<StateMachineDbContext>.");

        var db = ef.Context;
        await db.Transitions.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Instances.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Definitions.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}