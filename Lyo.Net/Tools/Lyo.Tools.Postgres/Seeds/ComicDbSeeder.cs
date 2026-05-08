using Bogus;
using Lyo.Comic.Enums;
using Lyo.Comic.Postgres.Database;
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

    /// <summary>Seeds the database with fake comic data. Skips seeding if series already exist.</summary>
    public async Task SeedAsync(int seriesCount = 20, int? seed = null, CancellationToken ct = default)
    {
        await using var db = CreateComicContext();
        if (await db.Series.AnyAsync(ct)) {
            _logger.LogInformation("Comic DB already has data — skipping seed.");
            return;
        }

        var faker = seed.HasValue ? new Faker { Random = new(seed.Value) } : new Faker();
        _logger.LogInformation("Seeding {Count} comic series...", seriesCount);
        var series = BuildSeries(faker, seriesCount);
        db.Series.AddRange(series);
        await db.SaveChangesAsync(ct);
        var allChapters = series.SelectMany(s => s.Chapters).ToList();
        var allPages = allChapters.SelectMany(c => BuildPages(faker, c)).ToList();
        db.Pages.AddRange(allPages);
        await db.SaveChangesAsync(ct);
        var allCharacters = series.SelectMany(s => BuildCharacters(faker, s)).ToList();
        db.Characters.AddRange(allCharacters);
        await db.SaveChangesAsync(ct);
        foreach (var character in allCharacters) {
            var seriesVolumes = series.First(s => s.Id == character.SeriesId).Volumes.ToList();
            if (seriesVolumes.Count == 0)
                continue;

            var appearCount = faker.Random.Int(0, Math.Min(3, seriesVolumes.Count));
            var picked = faker.Random.ListItems(seriesVolumes, appearCount);
            foreach (var vol in picked)
                character.Volumes.Add(vol);
        }

        await db.SaveChangesAsync(ct);
        var tagCount = await SeedTagsAsync(faker, series, ct);
        _logger.LogInformation(
            "Seeded {SeriesCount} series, {VolumeCount} volumes, {ChapterCount} chapters, {PageCount} pages, {CharacterCount} characters, {TagCount} tags.", series.Count,
            series.Sum(s => s.Volumes.Count), allChapters.Count, allPages.Count, allCharacters.Count, tagCount);
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
                var exists = await tagDb.Tags.AnyAsync(t => t.ForEntityType == SeriesEntityType && t.ForEntityId == s.Id.ToString() && t.Name == tag, ct);
                if (exists)
                    continue;

                tagDb.Tags.Add(
                    new() {
                        Id = Guid.NewGuid(),
                        ForEntityType = SeriesEntityType,
                        ForEntityId = s.Id.ToString(),
                        Name = tag,
                        TagType = "tag",
                        CreatedTimestamp = DateTime.UtcNow
                    });

                totalTags++;
            }

            await tagDb.SaveChangesAsync(ct);
        }

        return totalTags;
    }

    private static List<SeriesEntity> BuildSeries(Faker faker, int count)
    {
        var usedSlugs = new HashSet<string>(StringComparer.Ordinal);
        var seriesFaker = new Faker<SeriesEntity>().RuleFor(s => s.Id, _ => Guid.NewGuid())
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
            .RuleFor(s => s.CoverImageRef, f => f.Random.Bool(0.7f) ? f.Image.PicsumUrl() : null)
            .RuleFor(s => s.Demographic, f => f.Random.Bool(0.6f) ? f.PickRandom(Demographics) : null)
            .RuleFor(s => s.AlternateTitles, (f, s) => BuildAlternateTitles(f, s))
            .RuleFor(s => s.Chapters, (f, s) => BuildChapters(f, s));

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

    private static List<ChapterEntity> BuildChapters(Faker f, SeriesEntity series)
    {
        var chapterCount = f.Random.Int(1, 40);
        var chapters = new List<ChapterEntity>(chapterCount);
        for (var i = 0; i < chapterCount; i++) {
            var pageCount = f.Random.Int(8, 50);
            chapters.Add(
                new() {
                    Id = Guid.NewGuid(),
                    SeriesId = series.Id,
                    ChapterNumber = i + 1,
                    Title = f.Random.Bool(0.5f) ? f.Lorem.Sentence(f.Random.Int(2, 5)).TrimEnd('.') : null,
                    Language = f.PickRandom(Languages),
                    PageCount = pageCount,
                    PublishedDate = f.Random.Bool(0.8f) ? DateOnly.FromDateTime(f.Date.Between(new(2000, 1, 1), DateTime.UtcNow)) : null,
                    Source = f.Random.Bool(0.2f) ? f.Internet.Url() : null
                });
        }

        return chapters;
    }

    private static List<PageEntity> BuildPages(Faker f, ChapterEntity chapter)
    {
        var count = chapter.PageCount ?? 0;
        return Enumerable.Range(1, count)
            .Select(pageNum => new PageEntity {
                Id = Guid.NewGuid(),
                ChapterId = chapter.Id,
                PageNumber = pageNum,
                ImageRef = f.Random.Bool(0.6f) ? f.Image.PicsumUrl() : null,
                Width = f.Random.Bool(0.5f) ? f.Random.Int(600, 1800) : null,
                Height = f.Random.Bool(0.5f) ? f.Random.Int(800, 2400) : null
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
                Name = faker.Name.FirstName(),
                Description = faker.Random.Bool(0.6f) ? faker.Lorem.Sentences(faker.Random.Int(1, 3)) : null,
                ImageRef = faker.Random.Bool(0.4f) ? faker.Image.PicsumUrl() : null,
                Role = faker.PickRandom(CharacterRoles),
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