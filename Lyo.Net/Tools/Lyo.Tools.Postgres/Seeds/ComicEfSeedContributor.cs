using Bogus;
using Lyo.Comic.Enums;
using Lyo.Comic.Postgres.Database;
using Lyo.EntityReference.Models;
using Lyo.Seed;
using Lyo.Tag.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lyo.Tools.Postgres.Seeds;

/// <summary>EF-direct seed of comic series (volumes, chapters, pages, characters) and tags in <c>TagDbContext</c>.</summary>
public sealed class ComicEfSeedContributor(ConnectionStringProvider connStr, ILogger<ComicEfSeedContributor> logger) : SeedContributor
{
    private const string SeriesEntityType = "ComicSeries";

    private static readonly string[] Languages = ["en", "ja", "ko", "zh", "fr", "de", "es", "pt"];
    private static readonly string[] Demographics = ["Shounen", "Shoujo", "Seinen", "Josei", "Kodomomuke", "All Ages"];
    private static readonly string[] CharacterRoles = ["Protagonist", "Antagonist", "Supporting", "Minor"];

    private static readonly string[] GenreTags = [
        "action", "adventure", "comedy", "drama", "fantasy", "horror", "mystery", "romance", "sci-fi", "slice-of-life",
        "sports", "supernatural", "thriller", "historical", "mecha", "psychological", "school", "martial-arts", "isekai", "military",
        "music", "cooking", "magic", "time-travel", "harem"
    ];

    /// <inheritdoc />
    public override string Name => "Comic";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(async (session, ct) => {
            logger.LogInformation("Replacing existing comic rows...");
            if (session.Transport is not EfSeedTransport<ComicDbContext> ef)
                throw new SeedException("Comic OnClear requires EfSeedTransport<ComicDbContext>.");

            await using var tagDb = CreateTagContext();
            await tagDb.Tags.Where(t => t.SubjectEntityType == SeriesEntityType).ExecuteDeleteAsync(ct).ConfigureAwait(false);
            var db = ef.Context;
            await db.Pages.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.Characters.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.Chapters.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.Volumes.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.Series.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        });

        var faker = options.RandomSeed is { } seedValue ? new Faker { Random = new(seedValue) } : new Faker();
        graph.Entity(options.Count, _ => BuildSeries(faker))
            .After(async (session, ct) => {
                var series = session.Generated<SeriesEntity>().ToList();
                var allChapters = series.SelectMany(s => s.Chapters).ToList();
                var allPages = allChapters.SelectMany(c => BuildPages(faker, c)).ToList();
                await session.PersistAsync(allPages, ct).ConfigureAwait(false);

                var allCharacters = series.SelectMany(s => BuildCharacters(faker, s)).ToList();
                foreach (var character in allCharacters) {
                    var seriesVolumes = series.First(s => s.Id == character.SeriesId).Volumes.ToList();
                    if (seriesVolumes.Count == 0)
                        continue;

                    var appearCount = faker.Random.Int(1, Math.Min(3, seriesVolumes.Count));
                    foreach (var vol in faker.Random.ListItems(seriesVolumes, appearCount))
                        character.Volumes.Add(vol);
                }

                await session.PersistAsync(allCharacters, ct).ConfigureAwait(false);
                var tagCount = await SeedTagsAsync(faker, series, ct).ConfigureAwait(false);
                logger.LogInformation(
                    "Seeded {SeriesCount} series, {VolumeCount} volumes, {ChapterCount} chapters, {PageCount} pages, {CharacterCount} characters, {TagCount} tags.",
                    series.Count,
                    series.Sum(s => s.Volumes.Count),
                    allChapters.Count,
                    allPages.Count,
                    allCharacters.Count,
                    tagCount);
            });
    }

    private TagDbContext CreateTagContext()
    {
        var connection = connStr.GetOrThrow();
        var opts = new DbContextOptionsBuilder<TagDbContext>().UseNpgsql(connection, n => n.MigrationsHistoryTable("__EFMigrationsHistory", "tag")).Options;
        return new(opts);
    }

    private async Task<int> SeedTagsAsync(Faker faker, List<SeriesEntity> series, CancellationToken ct)
    {
        await using var tagDb = CreateTagContext();
        var totalTags = 0;
        foreach (var s in series) {
            var tagCount = faker.Random.Int(2, 5);
            var picked = faker.Random.ArrayElements(GenreTags, tagCount);
            foreach (var tag in picked) {
                var exists = await tagDb.Tags.AnyAsync(
                        t => t.SubjectEntityType == SeriesEntityType && t.SubjectEntityId == s.Id.ToString() && t.Name == tag && t.DeletedAt == null, ct)
                    .ConfigureAwait(false);
                if (exists)
                    continue;

                tagDb.Tags.Add(
                    new() {
                        Id = Guid.NewGuid(),
                        SubjectEntityType = SeriesEntityType,
                        SubjectEntityId = s.Id.ToString(),
                        ActorEntityType = EntityRefWellKnown.SystemActorType,
                        ActorEntityId = EntityRefWellKnown.SystemActorId.ToString(),
                        TenantId = EntityRefWellKnown.SingleTenantDefaultId,
                        Name = tag,
                        TagType = "tag",
                        Slug = string.Empty,
                        Visibility = EntityRefVisibility.Private,
                        CreatedAt = DateTime.UtcNow
                    });
                totalTags++;
            }

            await tagDb.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return totalTags;
    }

    private static SeriesEntity BuildSeries(Faker faker)
    {
        var usedSlugs = new HashSet<string>(StringComparer.Ordinal);
        var title = faker.Lorem.Sentence(faker.Random.Int(1, 4)).TrimEnd('.');
        var series = new SeriesEntity {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = UniqueSlug(SlugOf(title), usedSlugs),
            ComicType = faker.PickRandom(ComicType.Manga, ComicType.Manhwa, ComicType.Manhua, ComicType.Webtoon, ComicType.Western),
            Status = faker.PickRandom(ComicStatus.Ongoing, ComicStatus.Completed, ComicStatus.Hiatus, ComicStatus.Cancelled),
            Description = faker.Random.Bool(0.8f) ? faker.Lorem.Paragraphs(faker.Random.Int(1, 3)) : null,
            Language = faker.PickRandom(Languages),
            PublishedYear = faker.Random.Bool(0.7f) ? faker.Random.Int(1985, 2025) : null,
            Author = faker.Name.FullName(),
            Artist = faker.Random.Bool(0.6f) ? faker.Name.FullName() : null,
            Publisher = faker.Random.Bool(0.8f) ? faker.Company.CompanyName() : null,
            Source = faker.Random.Bool(0.3f) ? faker.Internet.Url() : null,
            CoverImageRef = faker.Random.Bool(0.9f) ? faker.Image.PicsumUrl() : null,
            Demographic = faker.Random.Bool(0.6f) ? faker.PickRandom(Demographics) : null,
            CreatedTimestamp = faker.Date.Past(2).ToUniversalTime()
        };
        series.UpdatedTimestamp = series.CreatedTimestamp;
        series.AlternateTitles = BuildAlternateTitles(faker, series);
        BuildVolumesAndChapters(faker, series);
        return series;
    }

    private static List<AlternateTitleEntity> BuildAlternateTitles(Faker f, SeriesEntity series)
    {
        var count = f.Random.Int(0, 3);
        return Enumerable.Range(0, count)
            .Select(_ => new AlternateTitleEntity {
                Id = Guid.NewGuid(),
                SeriesId = series.Id,
                Title = f.Lorem.Sentence(f.Random.Int(1, 3)).TrimEnd('.'),
                Language = f.PickRandom(Languages)
            })
            .ToList();
    }

    private static void BuildVolumesAndChapters(Faker f, SeriesEntity series)
    {
        var volumeCount = f.Random.Int(2, 4);
        var volumes = new List<VolumeEntity>(volumeCount);
        for (var i = 0; i < volumeCount; i++) {
            volumes.Add(
                new() {
                    Id = Guid.NewGuid(),
                    SeriesId = series.Id,
                    Series = series,
                    VolumeNumber = i + 1,
                    Title = f.Random.Bool(0.4f) ? f.Lorem.Sentence(f.Random.Int(1, 3)).TrimEnd('.') : null,
                    CoverImageRef = f.Random.Bool(0.8f) ? f.Image.PicsumUrl() : series.CoverImageRef,
                    PublishedDate = f.Random.Bool(0.7f) ? DateOnly.FromDateTime(f.Date.Between(new(2000, 1, 1), DateTime.UtcNow)) : null,
                    CreatedTimestamp = series.CreatedTimestamp.AddDays(i),
                    UpdatedTimestamp = series.CreatedTimestamp.AddDays(i)
                });
        }

        var chapterCount = f.Random.Int(8, 16);
        var chapters = new List<ChapterEntity>(chapterCount);
        for (var i = 0; i < chapterCount; i++) {
            var vol = volumes[Math.Min(i * volumeCount / chapterCount, volumeCount - 1)];
            var pageCount = f.Random.Int(4, 8);
            var chapter = new ChapterEntity {
                Id = Guid.NewGuid(),
                SeriesId = series.Id,
                Series = series,
                VolumeId = vol.Id,
                Volume = vol,
                ChapterNumber = i + 1,
                Title = f.Random.Bool(0.5f) ? f.Lorem.Sentence(f.Random.Int(2, 5)).TrimEnd('.') : null,
                Language = series.Language ?? f.PickRandom(Languages),
                PageCount = pageCount,
                PublishedDate = f.Random.Bool(0.8f) ? DateOnly.FromDateTime(f.Date.Between(new(2000, 1, 1), DateTime.UtcNow)) : null,
                Source = f.Random.Bool(0.2f) ? f.Internet.Url() : null,
                CoverImageRef = f.Random.Bool(0.6f) ? f.Image.PicsumUrl() : vol.CoverImageRef,
                CreatedTimestamp = series.CreatedTimestamp.AddDays(i),
                UpdatedTimestamp = series.CreatedTimestamp.AddDays(i)
            };
            vol.Chapters.Add(chapter);
            chapters.Add(chapter);
        }

        series.Volumes = volumes;
        series.Chapters = chapters;
    }

    private static List<PageEntity> BuildPages(Faker f, ChapterEntity chapter)
    {
        var count = chapter.PageCount ?? 0;
        return Enumerable.Range(1, count)
            .Select(pageNum => new PageEntity {
                Id = Guid.NewGuid(),
                ChapterId = chapter.Id,
                PageNumber = pageNum,
                ImageRef = f.Image.PicsumUrl(),
                Width = 640,
                Height = 960,
                CreatedTimestamp = chapter.CreatedTimestamp,
                UpdatedTimestamp = chapter.UpdatedTimestamp
            })
            .ToList();
    }

    private static List<CharacterEntity> BuildCharacters(Faker faker, SeriesEntity series)
    {
        var count = faker.Random.Int(2, 6);
        return Enumerable.Range(0, count)
            .Select(_ => new CharacterEntity {
                Id = Guid.NewGuid(),
                SeriesId = series.Id,
                Series = series,
                Name = faker.Name.FirstName(),
                Description = faker.Random.Bool(0.6f) ? faker.Lorem.Sentences(faker.Random.Int(1, 3)) : null,
                ImageRef = faker.Random.Bool(0.5f) ? faker.Image.PicsumUrl() : null,
                Role = faker.PickRandom(CharacterRoles),
                CreatedTimestamp = series.CreatedTimestamp,
                UpdatedTimestamp = series.UpdatedTimestamp,
                Volumes = []
            })
            .ToList();
    }

    private static string UniqueSlug(string baseSlug, HashSet<string> used)
    {
        if (used.Add(baseSlug))
            return baseSlug;

        for (var i = 2;; i++) {
            var candidate = $"{baseSlug}-{i}";
            if (used.Add(candidate))
                return candidate;
        }
    }

    private static string SlugOf(string title)
        => title.ToLowerInvariant().Replace(' ', '-').Replace("'", "").Replace("\"", "").Replace(",", "").Replace(".", "").Trim('-');
}
