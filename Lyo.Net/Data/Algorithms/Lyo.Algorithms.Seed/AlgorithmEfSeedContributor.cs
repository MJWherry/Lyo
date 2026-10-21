using Lyo.Algorithms.Models;
using Lyo.Algorithms.Postgres;
using Lyo.Algorithms.Postgres.Database;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Algorithms.Seed;

/// <summary>
/// EF seed of expression definitions, one revision, and a latest label. The first row publishes the fishing formula under <c>leaderboard.seed_fishing_0</c>, the same key
/// <c>LeaderboardCatalog.KeyFor("seed-fishing-0")</c> produces. Schema <c>algorithms</c>.
/// </summary>
public sealed class AlgorithmEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <summary>Key for the fishing formula. Matches a leaderboard slug <c>seed-fishing-0</c>.</summary>
    public const string FishingKey = "leaderboard.seed_fishing_0";

    /// <summary>Expression that scores fish type, weight bands, and clamped length. Bass at weight 6 and length 15 is 38.</summary>
    public const string FishingFormula = """
                                         fish_type + (weight >= 5 && weight < 10 ? 3 : (weight >= 0 && weight < 5 ? 1 : 0)) + (length < 10 ? 0 : (length > 20 ? 50 : (length - 10) / 10 * 50))
                                         """;

    /// <inheritdoc />
    public override string Name => "Algorithms";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresAlgorithmOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<AlgorithmDbContext>().UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema)).Options;
        return new AlgorithmDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var now = DateTime.UtcNow;
        var definitions = graph.Entity(
            options.Count,
            i => new AlgorithmDefinitionEntity {
                Id = LyoGuid.CreateV7(),
                Key = i == 0 ? FishingKey : $"scoring.{i}",
                Name = i == 0 ? "Bass derby" : $"Points table {i}",
                Kind = nameof(AlgorithmKind.Expression),
                CurrentRevision = 1,
                CreatedTimestamp = now
            });

        definitions.ForEach(
            _ => 1, (definition, _) => new AlgorithmRevisionEntity {
                DefinitionId = definition.Id,
                Revision = 1,
                Kind = nameof(AlgorithmKind.Expression),
                Source = definition.Key == FishingKey ? FishingFormula : "1",
                CreatedTimestamp = now
            });

        definitions.ForEach(
            _ => 1, (definition, _) => new AlgorithmLabelEntity {
                DefinitionId = definition.Id,
                Label = "latest",
                Revision = 1,
                IsMutable = true
            });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<AlgorithmDbContext> ef)
            throw new SeedException("Algorithms OnClear requires EfSeedTransport<AlgorithmDbContext>.");

        var db = ef.Context;
        await db.Labels.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Revisions.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Definitions.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}