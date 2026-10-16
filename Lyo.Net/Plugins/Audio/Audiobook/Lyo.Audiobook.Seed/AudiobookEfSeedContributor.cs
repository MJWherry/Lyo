using Bogus;
using Lyo.Audiobook.Enums;
using Lyo.Audiobook.Postgres;
using Lyo.Audiobook.Postgres.Database;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Audiobook.Seed;

/// <summary>EF seed of genres, one series, and works with a narrator, an ISBN, and a chapter. Schema <c>audiobook</c>.</summary>
public sealed class AudiobookEfSeedContributor : SeedContributor, IEfSeedSource
{
    private static readonly string[] GenreNames = ["Fiction", "Mystery", "History"];

    /// <inheritdoc />
    public override string Name => "Audiobook";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresAudiobookOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<AudiobookDbContext>()
            .UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .Options;
        return new AudiobookDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        var genreId = LyoGuid.CreateV7();
        var seriesId = LyoGuid.CreateV7();
        graph.Entity(GenreNames.Length, i => new AudiobookGenreEntity {
            Id = i == 0 ? genreId : LyoGuid.CreateV7(),
            Name = GenreNames[i],
            Slug = GenreNames[i].ToLowerInvariant(),
            SortOrder = i
        });
        graph.Entity(1, _ => new AudiobookSeriesEntity { Id = seriesId, Title = "Night Listen", Slug = "night-listen" });
        var works = graph.Entity(options.Count, i => new AudiobookWorkEntity {
            Id = LyoGuid.CreateV7(),
            Title = faker.Lorem.Sentence(3).TrimEnd('.'),
            Slug = $"work-{i}",
            Year = 2020,
            SeriesId = seriesId,
            SeriesIndex = i + 1,
            CreatedTimestamp = now
        });
        works.ForEach(_ => 1, (work, _) => new AudiobookWorkGenreEntity { WorkId = work.Id, GenreId = genreId });
        works.ForEach(_ => 1, (work, _) => new AudiobookCreditEntity {
            Id = LyoGuid.CreateV7(),
            WorkId = work.Id,
            DisplayName = faker.Name.FullName(),
            Role = AudiobookCreditRole.Narrator,
            Ordinal = 1
        });
        works.ForEach(_ => 1, (work, _) => new AudiobookIdentifierEntity {
            Id = LyoGuid.CreateV7(),
            WorkId = work.Id,
            Scheme = AudiobookIdentifierSchemes.Isbn,
            Value = work.Id.ToString("N"),
            NormalizedValue = work.Id.ToString("N"),
            IsPrimary = true
        });
        works.ForEach(_ => 1, (work, _) => new AudiobookChapterEntity {
            Id = LyoGuid.CreateV7(),
            WorkId = work.Id,
            Number = 1,
            Title = "Chapter 1",
            StartSeconds = 0,
            DurationSeconds = 600,
            CreatedTimestamp = now
        });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<AudiobookDbContext> ef)
            throw new SeedException("Audiobook OnClear requires EfSeedTransport<AudiobookDbContext>.");

        var db = ef.Context;
        await db.Identifiers.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Credits.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.WorkGenres.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Chapters.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Works.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Series.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Genres.ExecuteUpdateAsync(s => s.SetProperty(g => g.ParentGenreId, (Guid?)null), ct).ConfigureAwait(false);
        await db.Genres.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
