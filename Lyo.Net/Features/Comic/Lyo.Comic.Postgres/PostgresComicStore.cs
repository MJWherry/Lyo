using System.Diagnostics;
using Lyo.Comic.Enums;
using Lyo.Comic.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Health;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Comic.Postgres;

/// <summary>PostgreSQL implementation of <see cref="IComicStore" />.</summary>
public sealed class PostgresComicStore : IComicStore, IHealth
{
    private readonly IDbContextFactory<ComicDbContext> _contextFactory;

    public PostgresComicStore(IDbContextFactory<ComicDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory, nameof(contextFactory));
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task SaveSeriesAsync(ComicSeries series, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(series, nameof(series));
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = series.Id != default ? await ctx.Series.Include(s => s.AlternateTitles).FirstOrDefaultAsync(s => s.Id == series.Id, ct).ConfigureAwait(false) : null;
        if (existing != null) {
            existing.Title = series.Title;
            existing.Slug = series.Slug;
            existing.ComicType = (int)series.ComicType;
            existing.Status = (int)series.Status;
            existing.Description = series.Description;
            existing.Language = series.Language;
            existing.PublishedYear = series.PublishedYear;
            existing.Author = series.Author;
            existing.Artist = series.Artist;
            existing.Publisher = series.Publisher;
            existing.Source = series.Source;
            existing.CoverImageRef = series.CoverImageRef;
            existing.Demographic = series.Demographic;
            ctx.AlternateTitles.RemoveRange(existing.AlternateTitles);
            foreach (var alt in series.AlternateTitles)
                ctx.AlternateTitles.Add(ToAlternateTitleEntity(alt, existing.Id));
        }
        else {
            var id = series.Id == default ? Guid.NewGuid() : series.Id;
            var entity = ToSeriesEntity(series, id);
            ctx.Series.Add(entity);
            foreach (var alt in series.AlternateTitles)
                ctx.AlternateTitles.Add(ToAlternateTitleEntity(alt, id));
        }

        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ComicSeries?> GetSeriesByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Series.Include(s => s.AlternateTitles).FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : ToSeries(entity);
    }

    /// <inheritdoc />
    public async Task<ComicSeries?> GetSeriesBySlugAsync(string slug, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(slug, nameof(slug));
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Series.Include(s => s.AlternateTitles).FirstOrDefaultAsync(s => s.Slug == slug, ct).ConfigureAwait(false);
        return entity == null ? null : ToSeries(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ComicSeries>> SearchSeriesAsync(ComicSeriesQuery query, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(query, nameof(query));
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var q = ctx.Series.Include(s => s.AlternateTitles).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.TitleContains)) {
            var needle = query.TitleContains.ToLower();
            q = q.Where(s => s.Title.ToLower().Contains(needle) || s.AlternateTitles.Any(a => a.Title.ToLower().Contains(needle)));
        }

        if (query.ComicType.HasValue)
            q = q.Where(s => s.ComicType == (int)query.ComicType.Value);

        if (query.Status.HasValue)
            q = q.Where(s => s.Status == (int)query.Status.Value);

        if (!string.IsNullOrWhiteSpace(query.Language))
            q = q.Where(s => s.Language == query.Language);

        q = q.OrderBy(s => s.Title).Skip(query.Skip);
        if (query.Limit.HasValue)
            q = q.Take(query.Limit.Value);

        var results = await q.ToListAsync(ct).ConfigureAwait(false);
        return results.Select(ToSeries).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteSeriesAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Series.FindAsync([id], ct).ConfigureAwait(false);
        if (entity != null) {
            ctx.Series.Remove(entity);
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task SaveVolumeAsync(ComicVolume volume, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(volume, nameof(volume));
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (volume.Id != default) {
            var existing = await ctx.Volumes.FindAsync([volume.Id], ct).ConfigureAwait(false);
            if (existing != null) {
                existing.SeriesId = volume.SeriesId;
                existing.VolumeNumber = volume.VolumeNumber;
                existing.Title = volume.Title;
                existing.CoverImageRef = volume.CoverImageRef;
                existing.PublishedDate = volume.PublishedDate.HasValue ? DateOnly.FromDateTime(volume.PublishedDate.Value) : null;
                await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
                return;
            }
        }

        ctx.Volumes.Add(ToVolumeEntity(volume, volume.Id == default ? Guid.NewGuid() : volume.Id));
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ComicVolume?> GetVolumeByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Volumes.FindAsync([id], ct).ConfigureAwait(false);
        return entity == null ? null : ToVolume(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ComicVolume>> GetVolumesBySeriesAsync(Guid seriesId, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await ctx.Volumes.Where(v => v.SeriesId == seriesId).OrderBy(v => v.VolumeNumber).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToVolume).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteVolumeAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Volumes.FindAsync([id], ct).ConfigureAwait(false);
        if (entity != null) {
            ctx.Volumes.Remove(entity);
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task SaveChapterAsync(ComicChapter chapter, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(chapter, nameof(chapter));
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (chapter.Id != default) {
            var existing = await ctx.Chapters.FindAsync([chapter.Id], ct).ConfigureAwait(false);
            if (existing != null) {
                existing.SeriesId = chapter.SeriesId;
                existing.VolumeId = chapter.VolumeId;
                existing.ChapterNumber = chapter.ChapterNumber;
                existing.Title = chapter.Title;
                existing.Language = chapter.Language;
                existing.PageCount = chapter.PageCount;
                existing.PublishedDate = chapter.PublishedDate.HasValue ? DateOnly.FromDateTime(chapter.PublishedDate.Value) : null;
                existing.Source = chapter.Source;
                await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
                return;
            }
        }

        ctx.Chapters.Add(ToChapterEntity(chapter, chapter.Id == default ? Guid.NewGuid() : chapter.Id));
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ComicChapter?> GetChapterByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Chapters.FindAsync([id], ct).ConfigureAwait(false);
        return entity == null ? null : ToChapter(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ComicChapter>> GetChaptersBySeriesAsync(Guid seriesId, string? language = null, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var q = ctx.Chapters.Where(c => c.SeriesId == seriesId);
        if (!string.IsNullOrWhiteSpace(language))
            q = q.Where(c => c.Language == language);

        var entities = await q.OrderBy(c => c.ChapterNumber).ThenBy(c => c.Language).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToChapter).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ComicChapter>> GetChaptersByVolumeAsync(Guid volumeId, string? language = null, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var q = ctx.Chapters.Where(c => c.VolumeId == volumeId);
        if (!string.IsNullOrWhiteSpace(language))
            q = q.Where(c => c.Language == language);

        var entities = await q.OrderBy(c => c.ChapterNumber).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToChapter).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteChapterAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Chapters.FindAsync([id], ct).ConfigureAwait(false);
        if (entity != null) {
            ctx.Chapters.Remove(entity);
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public string HealthCheckName => "comic-postgres";

    /// <inheritdoc />
    public async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var canConnect = await ctx.Database.CanConnectAsync(ct).ConfigureAwait(false);
            sw.Stop();
            return canConnect
                ? HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["database"] = "comic" })
                : HealthResult.Unhealthy(sw.Elapsed, "Database connection failed");
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }

    private static SeriesEntity ToSeriesEntity(ComicSeries r, Guid id)
        => new() {
            Id = id,
            Title = r.Title,
            Slug = r.Slug,
            ComicType = (int)r.ComicType,
            Status = (int)r.Status,
            Description = r.Description,
            Language = r.Language,
            PublishedYear = r.PublishedYear,
            Author = r.Author,
            Artist = r.Artist,
            Publisher = r.Publisher,
            Source = r.Source,
            CoverImageRef = r.CoverImageRef,
            Demographic = r.Demographic,
            CreatedTimestamp = r.CreatedTimestamp == default ? DateTime.UtcNow : r.CreatedTimestamp
        };

    private static AlternateTitleEntity ToAlternateTitleEntity(ComicAlternateTitle r, Guid seriesId)
        => new() {
            Id = r.Id == default ? Guid.NewGuid() : r.Id,
            SeriesId = seriesId,
            Title = r.Title,
            Language = r.Language
        };

    private static VolumeEntity ToVolumeEntity(ComicVolume r, Guid id)
        => new() {
            Id = id,
            SeriesId = r.SeriesId,
            VolumeNumber = r.VolumeNumber,
            Title = r.Title,
            CoverImageRef = r.CoverImageRef,
            PublishedDate = r.PublishedDate.HasValue ? DateOnly.FromDateTime(r.PublishedDate.Value) : null,
            CreatedTimestamp = r.CreatedTimestamp == default ? DateTime.UtcNow : r.CreatedTimestamp
        };

    private static ChapterEntity ToChapterEntity(ComicChapter r, Guid id)
        => new() {
            Id = id,
            SeriesId = r.SeriesId,
            VolumeId = r.VolumeId,
            ChapterNumber = r.ChapterNumber,
            Title = r.Title,
            Language = r.Language,
            PageCount = r.PageCount,
            PublishedDate = r.PublishedDate.HasValue ? DateOnly.FromDateTime(r.PublishedDate.Value) : null,
            Source = r.Source,
            CreatedTimestamp = r.CreatedTimestamp == default ? DateTime.UtcNow : r.CreatedTimestamp
        };

    private static ComicSeries ToSeries(SeriesEntity e)
        => new() {
            Id = e.Id,
            Title = e.Title,
            Slug = e.Slug,
            ComicType = (ComicType)e.ComicType,
            Status = (ComicStatus)e.Status,
            Description = e.Description,
            Language = e.Language,
            PublishedYear = e.PublishedYear,
            Author = e.Author,
            Artist = e.Artist,
            Publisher = e.Publisher,
            Source = e.Source,
            CoverImageRef = e.CoverImageRef,
            Demographic = e.Demographic,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp,
            AlternateTitles = e.AlternateTitles.Select(ToAlternateTitle).ToList()
        };

    private static ComicAlternateTitle ToAlternateTitle(AlternateTitleEntity e)
        => new() {
            Id = e.Id,
            SeriesId = e.SeriesId,
            Title = e.Title,
            Language = e.Language
        };

    private static ComicVolume ToVolume(VolumeEntity e)
        => new() {
            Id = e.Id,
            SeriesId = e.SeriesId,
            VolumeNumber = e.VolumeNumber,
            Title = e.Title,
            CoverImageRef = e.CoverImageRef,
            PublishedDate = e.PublishedDate.HasValue ? e.PublishedDate.Value.ToDateTime(TimeOnly.MinValue) : null,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp
        };

    private static ComicChapter ToChapter(ChapterEntity e)
        => new() {
            Id = e.Id,
            SeriesId = e.SeriesId,
            VolumeId = e.VolumeId,
            ChapterNumber = e.ChapterNumber,
            Title = e.Title,
            Language = e.Language,
            PageCount = e.PageCount,
            PublishedDate = e.PublishedDate.HasValue ? e.PublishedDate.Value.ToDateTime(TimeOnly.MinValue) : null,
            Source = e.Source,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp
        };
}