using Bogus;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Podcast.Enums;
using Lyo.Podcast.Postgres;
using Lyo.Podcast.Postgres.Database;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Podcast.Seed;

/// <summary>EF seed of genres, shows with an iTunes id, one season, and two episodes. Schema <c>podcast</c>.</summary>
public sealed class PodcastEfSeedContributor : SeedContributor, IEfSeedSource
{
    private static readonly string[] GenreNames = ["News", "Comedy", "History"];

    /// <inheritdoc />
    public override string Name => "Podcast";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresPodcastOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<PodcastDbContext>().UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema)).Options;
        return new PodcastDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        var genreId = LyoGuid.CreateV7();
        graph.Entity(
            GenreNames.Length, i => new PodcastGenreEntity {
                Id = i == 0 ? genreId : LyoGuid.CreateV7(),
                Name = GenreNames[i],
                Slug = GenreNames[i].ToLowerInvariant(),
                SortOrder = i
            });

        var shows = graph.Entity(
            options.Count, i => new PodcastShowEntity {
                Id = LyoGuid.CreateV7(),
                Title = faker.Lorem.Sentence(3).TrimEnd('.'),
                Slug = $"show-{i}",
                Status = PodcastShowStatus.Ongoing,
                CreatedTimestamp = now
            });

        shows.ForEach(_ => 1, (show, _) => new PodcastShowGenreEntity { ShowId = show.Id, GenreId = genreId });
        shows.ForEach(
            _ => 1, (show, _) => new PodcastShowIdentifierEntity {
                Id = LyoGuid.CreateV7(),
                ShowId = show.Id,
                Scheme = PodcastIdentifierSchemes.Itunes,
                Value = show.Id.ToString("N"),
                NormalizedValue = show.Id.ToString("N"),
                IsPrimary = true
            });

        shows.ForEach(
                _ => 1, (show, _) => new PodcastSeasonEntity {
                    Id = LyoGuid.CreateV7(),
                    ShowId = show.Id,
                    SeasonNumber = 1,
                    Name = "Season 1",
                    Year = 2020
                })
            .ForEach(
                _ => 2, (season, n) => new PodcastEpisodeEntity {
                    Id = LyoGuid.CreateV7(),
                    SeasonId = season.Id,
                    EpisodeNumber = n + 1,
                    Title = faker.Lorem.Sentence(3).TrimEnd('.'),
                    RuntimeSeconds = 1800,
                    CreatedTimestamp = now
                })
            .ForEach(
                episode => episode.EpisodeNumber == 1 ? 1 : 0,
                (episode, _) => new PodcastEpisodeIdentifierEntity {
                    Id = LyoGuid.CreateV7(),
                    EpisodeId = episode.Id,
                    Scheme = PodcastIdentifierSchemes.EpisodeGuid,
                    Value = episode.Id.ToString("N"),
                    NormalizedValue = episode.Id.ToString("N"),
                    IsPrimary = true
                });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<PodcastDbContext> ef)
            throw new SeedException("Podcast OnClear requires EfSeedTransport<PodcastDbContext>.");

        var db = ef.Context;
        await db.EpisodeIdentifiers.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.ShowIdentifiers.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Credits.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.ShowGenres.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Episodes.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Seasons.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Shows.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Genres.ExecuteUpdateAsync(s => s.SetProperty(g => g.ParentGenreId, (Guid?)null), ct).ConfigureAwait(false);
        await db.Genres.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}