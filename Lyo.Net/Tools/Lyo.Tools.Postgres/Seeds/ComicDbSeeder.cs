using Bogus;
using Lyo.Comic.Enums;
using Lyo.Comic.Postgres.Database;
using Lyo.EntityReference.Models;
using Lyo.Tag.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lyo.Tools.Postgres.Seeds;

/// <summary>Seeds the comic database with randomised fake data using Bogus. Creates DbContext instances directly from the active ConnectionStringProvider.</summary>
public sealed class ComicDbSeeder
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

    private readonly ConnectionStringProvider _connStr;
    private readonly ILogger<ComicDbSeeder> _logger;

    public ComicDbSeeder(ConnectionStringProvider connStr, ILogger<ComicDbSeeder> logger)
    {
        _connStr = connStr;
        _logger = logger;
    }

    /// <summary>
    /// Seeds fake series, volumes, chapters, pages, characters (with volume appearances), and tags.
    /// Returns <c>false</c> when series already exist and <paramref name="replaceExisting" /> is false.
    /// </summary>
    public async Task<bool> SeedAsync(int seriesCount = 20, int? seed = null, bool replaceExisting = false, CancellationToken ct = default)
    {
        await using var db = CreateComicContext();
        if (await db.Series.AnyAsync(ct)) {
            if (!replaceExisting) {
                _logger.LogInformation("Comic DB already has data — skipping seed.");
                return false;
            }

            _logger.LogInformation("Replacing existing comic rows...");
            await using var tagDb = CreateTagContext();
            await tagDb.Tags.Where(t => t.SubjectEntityType == SeriesEntityType).ExecuteDeleteAsync(ct);
            await db.Pages.ExecuteDeleteAsync(ct);
            await db.Characters.ExecuteDeleteAsync(ct);
            await db.Chapters.ExecuteDeleteAsync(ct);
            await db.Volumes.ExecuteDeleteAsync(ct);
            await db.Series.ExecuteDeleteAsync(ct);
        }

        _logger.LogInformation("Seeding {Count} comic series...", seriesCount);
        var series = BuildSeries(seed, seriesCount);
        var faker = seed.HasValue ? new Faker { Random = new(seed.Value) } : new Faker();
        db.Series.AddRange(series);
        await db.SaveChangesAsync(ct);

        var allChapters = series.SelectMany(s => s.Chapters).ToList();
        var allPages = allChapters.SelectMany(c => BuildPages(faker, c)).ToList();
        db.Pages.AddRange(allPages);
        await db.SaveChangesAsync(ct);

        var allCharacters = series.SelectMany(s => BuildCharacters(faker, s)).ToList();
        foreach (var character in allCharacters) {
            var seriesVolumes = series.First(s => s.Id == character.SeriesId).Volumes.ToList();
            if (seriesVolumes.Count == 0)
                continue;

            var appearCount = faker.Random.Int(1, Math.Min(3, seriesVolumes.Count));
            foreach (var vol in faker.Random.ListItems(seriesVolumes, appearCount))
                character.Volumes.Add(vol);
        }

        db.Characters.AddRange(allCharacters);
        await db.SaveChangesAsync(ct);

        var tagCount = await SeedTagsAsync(faker, series, ct);
        _logger.LogInformation(
            "Seeded {SeriesCount} series, {VolumeCount} volumes, {ChapterCount} chapters, {PageCount} pages, {CharacterCount} characters, {TagCount} tags.",
            series.Count,
            series.Sum(s => s.Volumes.Count),
            allChapters.Count,
            allPages.Count,
            allCharacters.Count,
            tagCount);
        return true;
    }

    private ComicDbContext CreateComicContext()
    {
        var connStr = _connStr.GetOrThrow();
        var opts = new DbContextOptionsBuilder<ComicDbContext>().UseNpgsql(connStr, n => n.MigrationsHistoryTable("__EFMigrationsHistory", "comic")).Options;
        return new(opts);
    }

    private TagDbContext CreateTagContext()
    {
        var connStr = _connStr.GetOrThrow();
        var opts = new DbContextOptionsBuilder<TagDbContext>().UseNpgsql(connStr, n => n.MigrationsHistoryTable("__EFMigrationsHistory", "tag")).Options;
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

            await tagDb.SaveChangesAsync(ct);
        }

        return totalTags;
    }

    private static List<SeriesEntity> BuildSeries(int? seed, int count)
    {
        var usedSlugs = new HashSet<string>(StringComparer.Ordinal);
        var seriesFaker = new Faker<SeriesEntity>();
        if (seed.HasValue)
            seriesFaker.UseSeed(seed.Value);

        seriesFaker.RuleFor(s => s.Id, _ => Guid.NewGuid())
            .RuleFor(s => s.Title, f => f.Lorem.Sentence(f.Random.Int(1, 4)).TrimEnd('.'))
            .RuleFor(s => s.Slug, (_, s) => UniqueSlug(SlugOf(s.Title), usedSlugs))
            .RuleFor(s => s.ComicType, f => f.PickRandom(ComicType.Manga, ComicType.Manhwa, ComicType.Manhua, ComicType.Webtoon, ComicType.Western))
            .RuleFor(s => s.Status, f => f.PickRandom(ComicStatus.Ongoing, ComicStatus.Completed, ComicStatus.Hiatus, ComicStatus.Cancelled))
            .RuleFor(s => s.Description, f => f.Random.Bool(0.8f) ? f.Lorem.Paragraphs(f.Random.Int(1, 3)) : null)
            .RuleFor(s => s.Language, f => f.PickRandom(Languages))
            .RuleFor(s => s.PublishedYear, f => f.Random.Bool(0.7f) ? f.Random.Int(1985, 2025) : null)
            .RuleFor(s => s.Author, f => f.Name.FullName())
            .RuleFor(s => s.Artist, f => f.Random.Bool(0.6f) ? f.Name.FullName() : null)
            .RuleFor(s => s.Publisher, f => f.Random.Bool(0.8f) ? f.Company.CompanyName() : null)
            .RuleFor(s => s.Source, f => f.Random.Bool(0.3f) ? f.Internet.Url() : null)
            .RuleFor(s => s.CoverImageRef, f => f.Random.Bool(0.9f) ? f.Image.PicsumUrl() : null)
            .RuleFor(s => s.Demographic, f => f.Random.Bool(0.6f) ? f.PickRandom(Demographics) : null)
            .RuleFor(s => s.CreatedTimestamp, f => f.Date.Past(2).ToUniversalTime())
            .RuleFor(s => s.UpdatedTimestamp, (_, s) => s.CreatedTimestamp)
            .RuleFor(s => s.AlternateTitles, (f, s) => BuildAlternateTitles(f, s))
            .FinishWith((f, s) => BuildVolumesAndChapters(f, s));

        return seriesFaker.Generate(count);
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

    private static string SlugOf(string title) => title.ToLowerInvariant().Replace(' ', '-').Replace("'", "").Replace("\"", "").Replace(",", "").Replace(".", "").Trim('-');
}
