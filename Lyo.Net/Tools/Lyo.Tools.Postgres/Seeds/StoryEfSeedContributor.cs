using Lyo.Common.Core.Identifiers;
using Lyo.Seed;
using Lyo.Story;
using Lyo.Story.Enums;
using Lyo.Story.Postgres;
using Lyo.Story.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Tools.Postgres.Seeds;

/// <summary>EF seed of <see cref="StorySamples.TheHeist" /> via <see cref="IStoryStore" />, plus one playthrough, variable, and start event.</summary>
public sealed class StoryEfSeedContributor(ILogger<StoryEfSeedContributor> logger) : SeedContributor
{
    /// <summary>Id assigned to <see cref="StorySamples.TheHeist" /> and the seeded playthrough.</summary>
    public static readonly Guid SeedStoryId = new("22222222-2222-7222-8222-222222222222");

    /// <inheritdoc />
    public override string Name => "Story";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.SkipWhen(async (transport, ct) => {
            if (transport is not EfSeedTransport<StoryDbContext> ef)
                return false;

            return await ef.Context.Stories.AsNoTracking().AnyAsync(ct).ConfigureAwait(false);
        });

        graph.OnClear(async (session, ct) => {
            if (session.Transport is not EfSeedTransport<StoryDbContext> ef)
                throw new SeedException("Story OnClear requires EfSeedTransport<StoryDbContext>.");

            var db = ef.Context;
            await db.PlaythroughEvents.ExecuteUpdateAsync(s => s.SetProperty(x => x.CausedByEventId, (Guid?)null), ct).ConfigureAwait(false);
            await db.PlaythroughEvents.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.PlaythroughVariables.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.Playthroughs.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.Stories.ExecuteUpdateAsync(s => s.SetProperty(x => x.StartNodeId, (Guid?)null), ct).ConfigureAwait(false);
            await db.Stories.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        });

        graph.After(async (session, ct) => {
            if (session.Transport is EfSeedTransport<StoryDbContext> ef) {
                var connection = ef.Context.Database.GetConnectionString() ?? throw new SeedException("Story seed context has no connection string.");
                var services = new ServiceCollection();
                services.AddLogging();
                services.AddPostgresStoryStore(new PostgresStoryOptions { ConnectionString = connection, EnableAutoMigrations = false });
                await using var sp = services.BuildServiceProvider();
                var store = sp.GetRequiredService<IStoryStore>();
                var story = StorySamples.TheHeist();
                story.Id = SeedStoryId;
                await store.SaveStoryGraphAsync(story, ct).ConfigureAwait(false);
                logger.LogInformation("Seeded story {Slug} ({Id}).", story.Slug, story.Id);
            }

            var now = DateTime.UtcNow;
            var playthroughId = LyoGuid.CreateV7();
            await session.PersistAsync<StoryPlaythroughEntity>(
                [
                    new() {
                        Id = playthroughId,
                        StoryId = SeedStoryId,
                        PlayerRef = "Ada",
                        Status = StoryPlaythroughStatus.InProgress,
                        StartedAt = now
                    }
                ], ct)
                .ConfigureAwait(false);

            await session.PersistAsync<StoryPlaythroughVariableEntity>(
                [
                    new() {
                        PlaythroughId = playthroughId,
                        VariableKey = "alarm",
                        Value = "false",
                        UpdatedAt = now
                    }
                ], ct)
                .ConfigureAwait(false);

            await session.PersistAsync<StoryPlaythroughEventEntity>(
                [
                    new() {
                        Id = LyoGuid.CreateV7(),
                        PlaythroughId = playthroughId,
                        Sequence = 1,
                        Kind = StoryPlaythroughEventKind.StoryStarted,
                        OccurredAt = now
                    }
                ], ct)
                .ConfigureAwait(false);
        });
    }
}