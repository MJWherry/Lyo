using Bogus;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Seed;
using Lyo.Video.Enums;
using Lyo.Video.Postgres;
using Lyo.Video.Postgres.Database;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Video.Seed;

/// <summary>EF seed of genres, one collection, and series titles with seasons, episodes, credits, and identifiers. Schema <c>video</c>.</summary>
public sealed class VideoEfSeedContributor : SeedContributor, IEfSeedSource
{
    private static readonly string[] GenreNames = ["Drama", "Comedy", "Documentary"];

    /// <inheritdoc />
    public override string Name => "Video";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresVideoOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<VideoDbContext>()
            .UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .Options;
        return new VideoDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        var genreId = LyoGuid.CreateV7();
        var collectionId = LyoGuid.CreateV7();
        graph.Entity(GenreNames.Length, i => new VideoGenreEntity {
            Id = i == 0 ? genreId : LyoGuid.CreateV7(),
            Name = GenreNames[i],
            Slug = GenreNames[i].ToLowerInvariant(),
            SortOrder = i
        });
        graph.Entity(1, _ => new VideoCollectionEntity { Id = collectionId, Title = "Sunday Night", Slug = "sunday-night" });
        var seasons = graph.Entity(options.Count, i => new VideoTitleEntity {
            Id = LyoGuid.CreateV7(),
            Title = faker.Lorem.Sentence(3).TrimEnd('.'),
            Slug = $"series-{i}",
            Kind = VideoTitleKind.Series,
            Status = VideoSeriesStatus.Ongoing,
            Year = 2020,
            CollectionId = collectionId,
            CreatedTimestamp = now
        });
        seasons.ForEach(_ => 1, (title, _) => new VideoAlternateTitleEntity {
            Id = LyoGuid.CreateV7(),
            TitleId = title.Id,
            Title = title.Title + " (alt)",
            Language = "es",
            Kind = VideoAlternateTitleKind.Translated,
            SortOrder = 1
        });
        seasons.ForEach(_ => 1, (title, _) => new VideoTitleGenreEntity { TitleId = title.Id, GenreId = genreId });
        seasons.ForEach(_ => 1, (title, _) => new VideoCreditEntity {
            Id = LyoGuid.CreateV7(),
            TitleId = title.Id,
            DisplayName = faker.Name.FullName(),
            Role = VideoCreditRole.Director,
            Ordinal = 1
        });
        seasons.ForEach(_ => 1, (title, _) => new VideoTitleIdentifierEntity {
            Id = LyoGuid.CreateV7(),
            TitleId = title.Id,
            Scheme = "imdb",
            Value = title.Id.ToString("N"),
            NormalizedValue = title.Id.ToString("N"),
            IsPrimary = true
        });
        seasons.ForEach(_ => 1, (title, _) => new VideoYoutubeLinkEntity {
            Id = LyoGuid.CreateV7(),
            TitleId = title.Id,
            VideoId = title.Id.ToString("N")[^16..],
            ChannelId = $"channel{title.Id.ToString("N")[^8..]}"
        });
        seasons.ForEach(_ => 1, (title, _) => new VideoCutEntity {
            Id = LyoGuid.CreateV7(),
            TitleId = title.Id,
            Name = "Theatrical",
            RuntimeSeconds = 3600,
            IsPrimary = true
        });
        seasons.ForEach(_ => 1, (title, _) => new VideoSeasonEntity {
            Id = LyoGuid.CreateV7(),
            TitleId = title.Id,
            SeasonNumber = 1,
            Name = "Season 1",
            Year = 2020
        }).ForEach(_ => 2, (season, n) => new VideoEpisodeEntity {
            Id = LyoGuid.CreateV7(),
            SeasonId = season.Id,
            EpisodeNumber = n + 1,
            Title = faker.Lorem.Sentence(3).TrimEnd('.'),
            RuntimeSeconds = 1800,
            CreatedTimestamp = now
        }).ForEach(_ => 1, (episode, _) => new VideoEpisodeIdentifierEntity {
            Id = LyoGuid.CreateV7(),
            EpisodeId = episode.Id,
            Scheme = "imdb",
            Value = episode.Id.ToString("N"),
            NormalizedValue = episode.Id.ToString("N"),
            IsPrimary = true
        });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<VideoDbContext> ef)
            throw new SeedException("Video OnClear requires EfSeedTransport<VideoDbContext>.");

        var db = ef.Context;
        await db.EpisodeIdentifiers.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.YoutubeLinks.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Credits.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Cuts.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.AlternateTitles.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.TitleGenres.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Episodes.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Seasons.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Titles.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Collections.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Genres.ExecuteUpdateAsync(s => s.SetProperty(g => g.ParentGenreId, (Guid?)null), ct).ConfigureAwait(false);
        await db.Genres.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
