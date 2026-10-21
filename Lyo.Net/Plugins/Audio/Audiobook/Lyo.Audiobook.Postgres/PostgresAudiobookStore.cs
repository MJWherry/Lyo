using Lyo.Audiobook.Postgres.Database;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Health;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lyo.Audiobook.Postgres;

/// <summary>Postgres-backed <see cref="IAudiobookStore" />.</summary>
public sealed class PostgresAudiobookStore : IAudiobookStore, IHealth
{
    private readonly IDbContextFactory<AudiobookDbContext> _contextFactory;

    public PostgresAudiobookStore(IDbContextFactory<AudiobookDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task SaveWorkAsync(Audiobook work, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(work);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = work.Id != Guid.Empty ? await LoadWorkQuery(ctx, true).FirstOrDefaultAsync(w => w.Id == work.Id, ct).ConfigureAwait(false) : null;
        var id = existing?.Id ?? (work.Id == Guid.Empty ? LyoGuid.CreateV7() : work.Id);
        if (existing == null)
            ctx.Works.Add(existing = new() { Id = id, CreatedTimestamp = DateTime.UtcNow });

        existing.Title = work.Title;
        existing.Subtitle = work.Subtitle;
        existing.Slug = work.Slug;
        existing.Language = work.Language;
        existing.Year = work.Year;
        existing.RuntimeSeconds = work.RuntimeSeconds;
        existing.CoverImageRef = work.CoverImageRef;
        existing.AudioFileRef = work.AudioFileRef;
        existing.Overview = work.Overview;
        existing.SeriesId = work.SeriesId;
        existing.SeriesIndex = work.SeriesIndex;
        if (work.Transcript != null)
            existing.Transcript = work.Transcript;

        ctx.Credits.RemoveRange(existing.Credits);
        foreach (var credit in work.Credits)
            ctx.Credits.Add(ToCredit(credit, id));

        ctx.Identifiers.RemoveRange(existing.Identifiers);
        foreach (var identifier in work.Identifiers)
            ctx.Identifiers.Add(ToIdentifier(identifier, id));

        ReplaceChapters(ctx, existing, work.Chapters, id);
        ctx.WorkGenres.RemoveRange(existing.GenreLinks);
        foreach (var genreId in work.GenreIds.Distinct())
            ctx.WorkGenres.Add(new() { WorkId = id, GenreId = genreId });

        await SaveCatchingConflictAsync(ctx, ct).ConfigureAwait(false);
        work.Id = id;
    }

    /// <inheritdoc />
    public async Task<Audiobook?> GetWorkByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await LoadWorkQuery(ctx, false).FirstOrDefaultAsync(w => w.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : ToWork(entity);
    }

    /// <inheritdoc />
    public async Task<Audiobook?> GetWorkBySlugAsync(string slug, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(slug);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await LoadWorkQuery(ctx, false).FirstOrDefaultAsync(w => w.Slug == slug, ct).ConfigureAwait(false);
        return entity == null ? null : ToWork(entity);
    }

    /// <inheritdoc />
    public async Task<Audiobook?> GetWorkByIdentifierAsync(string scheme, string value, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(scheme);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        var normalized = AudiobookIdentifierSchemes.Normalize(scheme, value);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var workId = await ctx.Identifiers.AsNoTracking()
            .Where(i => i.Scheme == scheme && i.NormalizedValue == normalized)
            .Select(i => (Guid?)i.WorkId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return workId is null ? null : await GetWorkByIdAsync(workId.Value, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteWorkAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Works.FirstOrDefaultAsync(w => w.Id == id, ct).ConfigureAwait(false);
        if (entity == null)
            return;

        ctx.Works.Remove(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Audiobook>> SearchWorksAsync(AudiobookQuery query, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(query);
        if (query.FilterIds is { Count: 0 })
            return [];

        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var q = LoadWorkQuery(ctx, false);
        if (!string.IsNullOrWhiteSpace(query.TitleContains)) {
            var needle = query.TitleContains.ToLowerInvariant();
            q = q.Where(w => w.Title.ToLower().Contains(needle) || (w.Subtitle != null && w.Subtitle.ToLower().Contains(needle)));
        }

        if (query.GenreId is Guid genreId)
            q = q.Where(w => w.GenreLinks.Any(g => g.GenreId == genreId));

        if (query.Year is int year)
            q = q.Where(w => w.Year == year);

        if (!string.IsNullOrWhiteSpace(query.IdentifierScheme) && !string.IsNullOrWhiteSpace(query.IdentifierValue)) {
            var normalized = AudiobookIdentifierSchemes.Normalize(query.IdentifierScheme, query.IdentifierValue);
            q = q.Where(w => w.Identifiers.Any(i => i.Scheme == query.IdentifierScheme && i.NormalizedValue == normalized));
        }

        if (query.FilterIds is not null)
            q = q.Where(w => query.FilterIds.Contains(w.Id));

        var rows = await q.OrderBy(w => w.Title).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ToWork).ToArray();
    }

    /// <inheritdoc />
    public async Task SaveSeriesAsync(AudiobookSeries series, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(series);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var id = series.Id == Guid.Empty ? LyoGuid.CreateV7() : series.Id;
        var existing = series.Id != Guid.Empty ? await ctx.Series.FirstOrDefaultAsync(s => s.Id == series.Id, ct).ConfigureAwait(false) : null;
        if (existing == null)
            ctx.Series.Add(existing = new() { Id = id });

        existing.Title = series.Title;
        existing.Slug = series.Slug;
        await SaveCatchingConflictAsync(ctx, ct).ConfigureAwait(false);
        series.Id = id;
    }

    /// <inheritdoc />
    public async Task<AudiobookSeries?> GetSeriesByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Series.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : new AudiobookSeries { Id = entity.Id, Title = entity.Title, Slug = entity.Slug };
    }

    /// <inheritdoc />
    public async Task DeleteSeriesAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Series.FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);
        if (entity == null)
            return;

        var members = await ctx.Works.Where(w => w.SeriesId == id).ToListAsync(ct).ConfigureAwait(false);
        foreach (var work in members) {
            work.SeriesId = null;
            work.SeriesIndex = null;
        }

        ctx.Series.Remove(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveGenreAsync(AudiobookGenre genre, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(genre);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var id = genre.Id == Guid.Empty ? LyoGuid.CreateV7() : genre.Id;
        if (genre.ParentGenreId is Guid parent)
            await EnsureNoGenreCycleAsync(ctx, id, parent, ct).ConfigureAwait(false);

        var existing = genre.Id != Guid.Empty ? await ctx.Genres.FirstOrDefaultAsync(g => g.Id == genre.Id, ct).ConfigureAwait(false) : null;
        if (existing == null)
            ctx.Genres.Add(existing = new() { Id = id });

        existing.ParentGenreId = genre.ParentGenreId;
        existing.Slug = genre.Slug;
        existing.Name = genre.Name;
        existing.SortOrder = genre.SortOrder;
        await SaveCatchingConflictAsync(ctx, ct).ConfigureAwait(false);
        genre.Id = id;
    }

    /// <inheritdoc />
    public async Task<AudiobookGenre?> GetGenreByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Genres.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct).ConfigureAwait(false);
        return entity == null
            ? null
            : new AudiobookGenre {
                Id = entity.Id,
                ParentGenreId = entity.ParentGenreId,
                Slug = entity.Slug,
                Name = entity.Name,
                SortOrder = entity.SortOrder
            };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AudiobookGenre>> ListGenresAsync(CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await ctx.Genres.AsNoTracking().OrderBy(g => g.Name).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(g => new AudiobookGenre {
                Id = g.Id,
                ParentGenreId = g.ParentGenreId,
                Slug = g.Slug,
                Name = g.Name,
                SortOrder = g.SortOrder
            })
            .ToArray();
    }

    /// <inheritdoc />
    public async Task DeleteGenreAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (await ctx.Genres.AnyAsync(g => g.ParentGenreId == id, ct).ConfigureAwait(false) || await ctx.WorkGenres.AnyAsync(g => g.GenreId == id, ct).ConfigureAwait(false))
            throw ConflictException.ForResource("AudiobookGenre", id);

        var entity = await ctx.Genres.FirstOrDefaultAsync(g => g.Id == id, ct).ConfigureAwait(false);
        if (entity == null)
            return;

        ctx.Genres.Remove(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AudiobookTranscriptHit>> SearchTranscriptsAsync(AudiobookTranscriptQuery query, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.FullText))
            return [];

        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var fullText = query.FullText;
        var works = ctx.Works.AsNoTracking()
            .Where(w => w.SearchVector.Matches(EF.Functions.PlainToTsQuery("english", fullText)))
            .Select(w => new AudiobookTranscriptHit {
                WorkId = w.Id, ChapterId = null, Snippet = EF.Functions.PlainToTsQuery("english", fullText).GetResultHeadline(w.Transcript ?? "")
            });

        var chapters = ctx.Chapters.AsNoTracking()
            .Where(c => c.SearchVector.Matches(EF.Functions.PlainToTsQuery("english", fullText)))
            .Select(c => new AudiobookTranscriptHit {
                WorkId = c.WorkId, ChapterId = c.Id, Snippet = EF.Functions.PlainToTsQuery("english", fullText).GetResultHeadline(c.Transcript ?? "")
            });

        return await works.Concat(chapters).ToListAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> GetWorkTranscriptAsync(Guid workId, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var row = await ctx.Works.AsNoTracking().Where(w => w.Id == workId).Select(w => new { w.Transcript }).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (row == null)
            throw NotFoundException.ForResource("Audiobook", workId);

        return row.Transcript;
    }

    /// <inheritdoc />
    public async Task SaveWorkTranscriptAsync(Guid workId, string? transcript, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Works.FirstOrDefaultAsync(w => w.Id == workId, ct).ConfigureAwait(false);
        if (entity == null)
            throw NotFoundException.ForResource("Audiobook", workId);

        entity.Transcript = transcript;
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> GetChapterTranscriptAsync(Guid chapterId, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var row = await ctx.Chapters.AsNoTracking().Where(c => c.Id == chapterId).Select(c => new { c.Transcript }).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (row == null)
            throw NotFoundException.ForResource("AudiobookChapter", chapterId);

        return row.Transcript;
    }

    /// <inheritdoc />
    public async Task SaveChapterTranscriptAsync(Guid chapterId, string? transcript, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Chapters.FirstOrDefaultAsync(c => c.Id == chapterId, ct).ConfigureAwait(false);
        if (entity == null)
            throw NotFoundException.ForResource("AudiobookChapter", chapterId);

        entity.Transcript = transcript;
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public string HealthCheckName => "audiobook-postgres";

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default) => PostgresHealth.CheckAsync(_contextFactory, PostgresAudiobookOptions.Schema, ct);

    private static async Task EnsureNoGenreCycleAsync(AudiobookDbContext ctx, Guid genreId, Guid parentId, CancellationToken ct)
    {
        if (parentId == genreId)
            throw new UnprocessableEntityException("A genre cannot be its own parent.");

        var current = parentId;
        var guard = 0;
        while (guard++ < 64) {
            var next = await ctx.Genres.AsNoTracking().Where(g => g.Id == current).Select(g => g.ParentGenreId).FirstOrDefaultAsync(ct).ConfigureAwait(false);
            if (next == null)
                return;

            if (next == genreId)
                throw new UnprocessableEntityException("Genre parent would create a cycle.");

            current = next.Value;
        }

        throw new UnprocessableEntityException("Genre parent chain is too deep.");
    }

    private static void ReplaceChapters(AudiobookDbContext ctx, AudiobookWorkEntity existing, IReadOnlyList<AudiobookChapter> chapters, Guid workId)
    {
        var incomingIds = chapters.Where(c => c.Id != Guid.Empty).Select(c => c.Id).ToHashSet();
        foreach (var old in existing.Chapters.Where(c => !incomingIds.Contains(c.Id)).ToList())
            ctx.Chapters.Remove(old);

        foreach (var chapter in chapters) {
            var match = chapter.Id != Guid.Empty ? existing.Chapters.FirstOrDefault(c => c.Id == chapter.Id) : null;
            if (match == null) {
                var chapterId = chapter.Id == Guid.Empty ? LyoGuid.CreateV7() : chapter.Id;
                match = new() { Id = chapterId, WorkId = workId, CreatedTimestamp = DateTime.UtcNow };
                ctx.Chapters.Add(match);
                chapter.Id = chapterId;
            }

            match.Number = chapter.Number;
            match.Title = chapter.Title;
            match.StartSeconds = chapter.StartSeconds;
            match.DurationSeconds = chapter.DurationSeconds;
            match.AudioFileRef = chapter.AudioFileRef;
            if (chapter.Transcript != null)
                match.Transcript = chapter.Transcript;
        }
    }

    private static IQueryable<AudiobookWorkEntity> LoadWorkQuery(AudiobookDbContext ctx, bool tracking)
    {
        var query = ctx.Works.Include(w => w.Credits).Include(w => w.Identifiers).Include(w => w.Chapters).Include(w => w.GenreLinks);
        return tracking ? query : query.AsNoTracking();
    }

    private static async Task SaveCatchingConflictAsync(AudiobookDbContext ctx, CancellationToken ct)
    {
        try {
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) {
            throw ConflictException.ForResource("Audiobook", innerException: ex);
        }
    }

    private static AudiobookCreditEntity ToCredit(AudiobookCredit credit, Guid workId)
        => new() {
            Id = credit.Id == Guid.Empty ? LyoGuid.CreateV7() : credit.Id,
            WorkId = workId,
            DisplayName = credit.DisplayName,
            SortName = credit.SortName,
            Role = credit.Role,
            Ordinal = credit.Ordinal,
            PersonRef = credit.PersonRef
        };

    private static AudiobookIdentifierEntity ToIdentifier(AudiobookIdentifier identifier, Guid workId)
        => new() {
            Id = identifier.Id == Guid.Empty ? LyoGuid.CreateV7() : identifier.Id,
            WorkId = workId,
            Scheme = identifier.Scheme,
            Value = identifier.Value,
            NormalizedValue = string.IsNullOrEmpty(identifier.NormalizedValue)
                ? AudiobookIdentifierSchemes.Normalize(identifier.Scheme, identifier.Value)
                : identifier.NormalizedValue,
            IsPrimary = identifier.IsPrimary
        };

    private static Audiobook ToWork(AudiobookWorkEntity entity)
        => new() {
            Id = entity.Id,
            Title = entity.Title,
            Subtitle = entity.Subtitle,
            Slug = entity.Slug,
            Language = entity.Language,
            Year = entity.Year,
            RuntimeSeconds = entity.RuntimeSeconds,
            CoverImageRef = entity.CoverImageRef,
            AudioFileRef = entity.AudioFileRef,
            Overview = entity.Overview,
            SeriesId = entity.SeriesId,
            SeriesIndex = entity.SeriesIndex,
            GenreIds = entity.GenreLinks.Select(g => g.GenreId).ToArray(),
            CreatedTimestamp = entity.CreatedTimestamp,
            UpdatedTimestamp = entity.UpdatedTimestamp,
            Credits = entity.Credits.OrderBy(c => c.Ordinal)
                .Select(c => new AudiobookCredit {
                    Id = c.Id,
                    DisplayName = c.DisplayName,
                    SortName = c.SortName,
                    Role = c.Role,
                    Ordinal = c.Ordinal,
                    PersonRef = c.PersonRef
                })
                .ToArray(),
            Identifiers = entity.Identifiers.Select(i => new AudiobookIdentifier {
                    Id = i.Id,
                    Scheme = i.Scheme,
                    Value = i.Value,
                    NormalizedValue = i.NormalizedValue,
                    IsPrimary = i.IsPrimary
                })
                .ToArray(),
            Chapters = entity.Chapters.OrderBy(c => c.Number)
                .Select(c => new AudiobookChapter {
                    Id = c.Id,
                    WorkId = c.WorkId,
                    Number = c.Number,
                    Title = c.Title,
                    StartSeconds = c.StartSeconds,
                    DurationSeconds = c.DurationSeconds,
                    AudioFileRef = c.AudioFileRef
                })
                .ToArray()
        };
}