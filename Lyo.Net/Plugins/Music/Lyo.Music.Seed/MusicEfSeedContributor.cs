using Bogus;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Music.Enums;
using Lyo.Music.Postgres;
using Lyo.Music.Postgres.Database;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Music.Seed;

/// <summary>EF seed of genres, formats, and release groups with a release, tracks, recordings, and credits. Schema <c>music</c>.</summary>
public sealed class MusicEfSeedContributor : SeedContributor, IEfSeedSource
{
    private static readonly string[] GenreNames = ["Rock", "Jazz", "Electronic"];
    private static readonly string[] FormatNames = ["CD", "Vinyl"];

    /// <inheritdoc />
    public override string Name => "Music";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresMusicOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<MusicDbContext>()
            .UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .Options;
        return new MusicDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        var genreIds = GenreNames.Select(_ => LyoGuid.CreateV7()).ToArray();
        var formatId = LyoGuid.CreateV7();
        var recordingIds = Enumerable.Range(0, options.Count * 2).Select(_ => LyoGuid.CreateV7()).ToArray();
        graph.Entity(recordingIds.Length, i => new MusicRecordingEntity { Id = recordingIds[i], Title = faker.Lorem.Sentence(2).TrimEnd('.') });
        graph.Entity(genreIds.Length, i => new MusicGenreEntity {
            Id = genreIds[i],
            Name = GenreNames[i],
            Slug = GenreNames[i].ToLowerInvariant(),
            SortOrder = i
        });
        graph.Entity(FormatNames.Length, i => new MusicFormatEntity {
            Id = i == 0 ? formatId : LyoGuid.CreateV7(),
            Name = FormatNames[i],
            Slug = FormatNames[i].ToLowerInvariant(),
            SortOrder = i
        });
        var releases = graph.Entity(options.Count, i => new MusicReleaseGroupEntity {
            Id = LyoGuid.CreateV7(),
            Title = faker.Lorem.Sentence(2).TrimEnd('.'),
            Slug = $"album-{i}",
            Kind = MusicReleaseKind.Album,
            Status = MusicReleaseStatus.Released,
            CreatedTimestamp = now
        }).ForEach(_ => 1, (group, _) => new MusicReleaseEntity {
            Id = LyoGuid.CreateV7(),
            ReleaseGroupId = group.Id,
            Title = group.Title,
            Slug = group.Slug + "-release",
            Kind = MusicReleaseKind.Album,
            Status = MusicReleaseStatus.Released,
            FormatId = formatId,
            PrimaryArtist = faker.Name.FullName(),
            ReleaseDate = new DateOnly(2020, 1, 1),
            CreatedTimestamp = now
        });
        releases.ForEach(_ => 2, (release, n) => new MusicReleaseGenreEntity { ReleaseId = release.Id, GenreId = genreIds[n] });
        releases.ForEach(_ => 1, (release, _) => new MusicIdentifierEntity {
            Id = LyoGuid.CreateV7(),
            ReleaseId = release.Id,
            Scheme = "upc",
            Value = release.Id.ToString("N"),
            NormalizedValue = release.Id.ToString("N"),
            IsPrimary = true
        });
        releases.ForEach(_ => 1, (release, _) => new MusicCreditEntity {
            Id = LyoGuid.CreateV7(),
            ReleaseId = release.Id,
            DisplayName = release.PrimaryArtist ?? "Unknown artist",
            Role = MusicCreditRole.Artist,
            Ordinal = 1
        });
        var releaseIndex = new Dictionary<Guid, int>();
        releases.ForEach(_ => 2, (release, n) => {
            if (!releaseIndex.ContainsKey(release.Id))
                releaseIndex[release.Id] = releaseIndex.Count;
            var index = releaseIndex[release.Id];
            return new MusicTrackEntity {
                Id = LyoGuid.CreateV7(),
                ReleaseId = release.Id,
                RecordingId = recordingIds[index * 2 + n],
                DiscNumber = 1,
                TrackNumber = n + 1,
                Title = faker.Lorem.Sentence(2).TrimEnd('.'),
                DurationSeconds = 180,
                CreatedTimestamp = now
            };
        }).ForEach(_ => 1, (track, _) => new MusicCreditEntity {
            Id = LyoGuid.CreateV7(),
            TrackId = track.Id,
            DisplayName = faker.Name.FullName(),
            Role = MusicCreditRole.Performer,
            Ordinal = 1
        });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<MusicDbContext> ef)
            throw new SeedException("Music OnClear requires EfSeedTransport<MusicDbContext>.");

        var db = ef.Context;
        await db.Credits.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Identifiers.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.ReleaseGenres.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Tracks.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Releases.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Recordings.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.ReleaseGroups.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Formats.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Genres.ExecuteUpdateAsync(s => s.SetProperty(g => g.ParentGenreId, (Guid?)null), ct).ConfigureAwait(false);
        await db.Genres.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
