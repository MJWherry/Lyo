using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Health;
using Lyo.Podcast.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lyo.Podcast.Postgres;

/// <summary>Postgres-backed <see cref="IPodcastStore" />.</summary>
public sealed class PostgresPodcastStore : IPodcastStore, IHealth
{
    private readonly IDbContextFactory<PodcastDbContext> _contextFactory;

    public PostgresPodcastStore(IDbContextFactory<PodcastDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public string HealthCheckName => "podcast-postgres";

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
        => PostgresHealth.CheckAsync(_contextFactory, PostgresPodcastOptions.Schema, ct);

    /// <inheritdoc />
    public async Task SaveShowAsync(PodcastShow show, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(show);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = show.Id != Guid.Empty
            ? await LoadShowQuery(ctx, tracking: true).FirstOrDefaultAsync(s => s.Id == show.Id, ct).ConfigureAwait(false)
            : null;

        var id = existing?.Id ?? (show.Id == Guid.Empty ? LyoGuid.CreateV7() : show.Id);
        if (existing == null)
            ctx.Shows.Add(existing = new PodcastShowEntity { Id = id, CreatedTimestamp = DateTime.UtcNow });

        existing.Title = show.Title;
        existing.Subtitle = show.Subtitle;
        existing.Slug = show.Slug;
        existing.Status = show.Status;
        existing.Language = show.Language;
        existing.CoverImageRef = show.CoverImageRef;
        existing.Overview = show.Overview;

        ctx.Credits.RemoveRange(existing.Credits);
        foreach (var credit in show.Credits)
            ctx.Credits.Add(ToCredit(credit, id, null));

        ctx.ShowIdentifiers.RemoveRange(existing.Identifiers);
        foreach (var identifier in show.Identifiers)
            ctx.ShowIdentifiers.Add(ToShowIdentifier(identifier, id));

        ReplaceSeasons(ctx, existing, show.Seasons, id);
        ctx.ShowGenres.RemoveRange(existing.GenreLinks);
        foreach (var genreId in show.GenreIds.Distinct())
            ctx.ShowGenres.Add(new PodcastShowGenreEntity { ShowId = id, GenreId = genreId });

        await SaveCatchingConflictAsync(ctx, ct).ConfigureAwait(false);
        show.Id = id;
    }

    /// <inheritdoc />
    public async Task<PodcastShow?> GetShowByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await LoadShowQuery(ctx, tracking: false).FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : ToShow(entity);
    }

    /// <inheritdoc />
    public async Task<PodcastShow?> GetShowBySlugAsync(string slug, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(slug);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await LoadShowQuery(ctx, tracking: false).FirstOrDefaultAsync(s => s.Slug == slug, ct).ConfigureAwait(false);
        return entity == null ? null : ToShow(entity);
    }

    /// <inheritdoc />
    public async Task<PodcastShow?> GetShowByIdentifierAsync(string scheme, string value, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(scheme);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        var normalized = PodcastIdentifierSchemes.Normalize(scheme, value);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var showId = await ctx.ShowIdentifiers.AsNoTracking()
            .Where(i => i.Scheme == scheme && i.NormalizedValue == normalized)
            .Select(i => (Guid?)i.ShowId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        return showId is null ? null : await GetShowByIdAsync(showId.Value, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteShowAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Shows.FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);
        if (entity == null)
            return;

        ctx.Shows.Remove(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PodcastShow>> SearchShowsAsync(PodcastShowQuery query, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(query);
        if (query.FilterIds is { Count: 0 })
            return [];

        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var q = LoadShowQuery(ctx, tracking: false);
        if (!string.IsNullOrWhiteSpace(query.TitleContains)) {
            var needle = query.TitleContains.ToLowerInvariant();
            q = q.Where(s => s.Title.ToLower().Contains(needle) || (s.Subtitle != null && s.Subtitle.ToLower().Contains(needle)));
        }

        if (query.Status is { } status)
            q = q.Where(s => s.Status == status);
        if (query.GenreId is Guid genreId)
            q = q.Where(s => s.GenreLinks.Any(g => g.GenreId == genreId));
        if (query.FilterIds is not null)
            q = q.Where(s => query.FilterIds.Contains(s.Id));

        var rows = await q.OrderBy(s => s.Title).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ToShow).ToArray();
    }

    /// <inheritdoc />
    public async Task SaveGenreAsync(PodcastGenre genre, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(genre);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var id = genre.Id == Guid.Empty ? LyoGuid.CreateV7() : genre.Id;
        if (genre.ParentGenreId is Guid parent)
            await EnsureNoGenreCycleAsync(ctx, id, parent, ct).ConfigureAwait(false);
        var existing = genre.Id != Guid.Empty ? await ctx.Genres.FirstOrDefaultAsync(g => g.Id == genre.Id, ct).ConfigureAwait(false) : null;
        if (existing == null)
            ctx.Genres.Add(existing = new PodcastGenreEntity { Id = id });
        existing.ParentGenreId = genre.ParentGenreId;
        existing.Slug = genre.Slug;
        existing.Name = genre.Name;
        existing.SortOrder = genre.SortOrder;
        await SaveCatchingConflictAsync(ctx, ct).ConfigureAwait(false);
        genre.Id = id;
    }

    /// <inheritdoc />
    public async Task<PodcastGenre?> GetGenreByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Genres.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : new PodcastGenre { Id = entity.Id, ParentGenreId = entity.ParentGenreId, Slug = entity.Slug, Name = entity.Name, SortOrder = entity.SortOrder };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PodcastGenre>> ListGenresAsync(CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await ctx.Genres.AsNoTracking().OrderBy(g => g.Name).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(g => new PodcastGenre { Id = g.Id, ParentGenreId = g.ParentGenreId, Slug = g.Slug, Name = g.Name, SortOrder = g.SortOrder }).ToArray();
    }

    /// <inheritdoc />
    public async Task DeleteGenreAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (await ctx.Genres.AnyAsync(g => g.ParentGenreId == id, ct).ConfigureAwait(false)
            || await ctx.ShowGenres.AnyAsync(g => g.GenreId == id, ct).ConfigureAwait(false))
            throw ConflictException.ForResource("PodcastGenre", id);
        var entity = await ctx.Genres.FirstOrDefaultAsync(g => g.Id == id, ct).ConfigureAwait(false);
        if (entity == null)
            return;
        ctx.Genres.Remove(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PodcastTranscriptHit>> SearchTranscriptsAsync(PodcastTranscriptQuery query, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.FullText))
            return [];

        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var fullText = query.FullText;
        return await ctx.Episodes.AsNoTracking()
            .Where(e => e.SearchVector.Matches(EF.Functions.PlainToTsQuery("english", fullText)))
            .Select(e => new PodcastTranscriptHit {
                ShowId = e.Season!.ShowId,
                EpisodeId = e.Id,
                Snippet = EF.Functions.PlainToTsQuery("english", fullText).GetResultHeadline(e.Transcript ?? "")
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> GetEpisodeTranscriptAsync(Guid episodeId, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var row = await ctx.Episodes.AsNoTracking().Where(e => e.Id == episodeId).Select(e => new { e.Transcript }).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (row == null)
            throw NotFoundException.ForResource("PodcastEpisode", episodeId);
        return row.Transcript;
    }

    /// <inheritdoc />
    public async Task SaveEpisodeTranscriptAsync(Guid episodeId, string? transcript, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Episodes.FirstOrDefaultAsync(e => e.Id == episodeId, ct).ConfigureAwait(false);
        if (entity == null)
            throw NotFoundException.ForResource("PodcastEpisode", episodeId);
        entity.Transcript = transcript;
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static async Task EnsureNoGenreCycleAsync(PodcastDbContext ctx, Guid genreId, Guid parentId, CancellationToken ct)
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

    private static void ReplaceSeasons(PodcastDbContext ctx, PodcastShowEntity existing, IReadOnlyList<PodcastSeason> seasons, Guid showId)
    {
        var incomingIds = seasons.Where(s => s.Id != Guid.Empty).Select(s => s.Id).ToHashSet();
        foreach (var old in existing.Seasons.Where(s => !incomingIds.Contains(s.Id)).ToList())
            ctx.Seasons.Remove(old);

        foreach (var season in seasons) {
            var match = season.Id != Guid.Empty ? existing.Seasons.FirstOrDefault(s => s.Id == season.Id) : null;
            if (match == null) {
                var seasonId = season.Id == Guid.Empty ? LyoGuid.CreateV7() : season.Id;
                match = new PodcastSeasonEntity { Id = seasonId, ShowId = showId };
                ctx.Seasons.Add(match);
                season.Id = seasonId;
            }

            match.SeasonNumber = season.SeasonNumber;
            match.Name = season.Name;
            match.Year = season.Year;
            ReplaceEpisodes(ctx, match, season.Episodes);
        }
    }

    private static void ReplaceEpisodes(PodcastDbContext ctx, PodcastSeasonEntity season, IReadOnlyList<PodcastEpisode> episodes)
    {
        var incomingIds = episodes.Where(e => e.Id != Guid.Empty).Select(e => e.Id).ToHashSet();
        foreach (var old in season.Episodes.Where(e => !incomingIds.Contains(e.Id)).ToList())
            ctx.Episodes.Remove(old);

        foreach (var episode in episodes) {
            var match = episode.Id != Guid.Empty ? season.Episodes.FirstOrDefault(e => e.Id == episode.Id) : null;
            if (match == null) {
                var episodeId = episode.Id == Guid.Empty ? LyoGuid.CreateV7() : episode.Id;
                match = new PodcastEpisodeEntity { Id = episodeId, SeasonId = season.Id, CreatedTimestamp = DateTime.UtcNow };
                ctx.Episodes.Add(match);
                episode.Id = episodeId;
            }

            match.EpisodeNumber = episode.EpisodeNumber;
            match.Title = episode.Title;
            match.RuntimeSeconds = episode.RuntimeSeconds;
            match.PublishedDate = episode.PublishedDate.HasValue ? DateOnly.FromDateTime(episode.PublishedDate.Value) : null;
            match.AudioFileRef = episode.AudioFileRef;
            match.Description = episode.Description;
            if (episode.Transcript != null)
                match.Transcript = episode.Transcript;
            ctx.Credits.RemoveRange(match.Credits);
            foreach (var credit in episode.Credits)
                ctx.Credits.Add(ToCredit(credit, null, match.Id));
            ctx.EpisodeIdentifiers.RemoveRange(match.Identifiers);
            foreach (var identifier in episode.Identifiers)
                ctx.EpisodeIdentifiers.Add(ToEpisodeIdentifier(identifier, match.Id));
        }
    }

    private static IQueryable<PodcastShowEntity> LoadShowQuery(PodcastDbContext ctx, bool tracking)
    {
        var query = ctx.Shows
            .Include(s => s.Credits)
            .Include(s => s.Identifiers)
            .Include(s => s.Seasons).ThenInclude(season => season.Episodes).ThenInclude(e => e.Credits)
            .Include(s => s.Seasons).ThenInclude(season => season.Episodes).ThenInclude(e => e.Identifiers)
            .Include(s => s.GenreLinks);
        return tracking ? query : query.AsNoTracking();
    }

    private static async Task SaveCatchingConflictAsync(PodcastDbContext ctx, CancellationToken ct)
    {
        try {
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) {
            throw ConflictException.ForResource("PodcastShow", innerException: ex);
        }
    }

    private static PodcastCreditEntity ToCredit(PodcastCredit credit, Guid? showId, Guid? episodeId)
        => new() {
            Id = credit.Id == Guid.Empty ? LyoGuid.CreateV7() : credit.Id,
            ShowId = showId,
            EpisodeId = episodeId,
            DisplayName = credit.DisplayName,
            SortName = credit.SortName,
            Role = credit.Role,
            Ordinal = credit.Ordinal,
            PersonRef = credit.PersonRef
        };

    private static PodcastShowIdentifierEntity ToShowIdentifier(PodcastIdentifier identifier, Guid showId)
        => new() {
            Id = identifier.Id == Guid.Empty ? LyoGuid.CreateV7() : identifier.Id,
            ShowId = showId,
            Scheme = identifier.Scheme,
            Value = identifier.Value,
            NormalizedValue = Normalize(identifier),
            IsPrimary = identifier.IsPrimary
        };

    private static PodcastEpisodeIdentifierEntity ToEpisodeIdentifier(PodcastIdentifier identifier, Guid episodeId)
        => new() {
            Id = identifier.Id == Guid.Empty ? LyoGuid.CreateV7() : identifier.Id,
            EpisodeId = episodeId,
            Scheme = identifier.Scheme,
            Value = identifier.Value,
            NormalizedValue = Normalize(identifier),
            IsPrimary = identifier.IsPrimary
        };

    private static string Normalize(PodcastIdentifier identifier)
        => string.IsNullOrEmpty(identifier.NormalizedValue)
            ? PodcastIdentifierSchemes.Normalize(identifier.Scheme, identifier.Value)
            : identifier.NormalizedValue;

    private static PodcastShow ToShow(PodcastShowEntity entity)
        => new() {
            Id = entity.Id,
            Title = entity.Title,
            Subtitle = entity.Subtitle,
            Slug = entity.Slug,
            Status = entity.Status,
            Language = entity.Language,
            CoverImageRef = entity.CoverImageRef,
            Overview = entity.Overview,
            GenreIds = entity.GenreLinks.Select(g => g.GenreId).ToArray(),
            CreatedTimestamp = entity.CreatedTimestamp,
            UpdatedTimestamp = entity.UpdatedTimestamp,
            Credits = entity.Credits.OrderBy(c => c.Ordinal).Select(ToCreditModel).ToArray(),
            Identifiers = entity.Identifiers.Select(i => ToIdentifierModel(i.Id, i.Scheme, i.Value, i.NormalizedValue, i.IsPrimary)).ToArray(),
            Seasons = entity.Seasons.OrderBy(s => s.SeasonNumber).Select(s => new PodcastSeason {
                Id = s.Id,
                ShowId = s.ShowId,
                SeasonNumber = s.SeasonNumber,
                Name = s.Name,
                Year = s.Year,
                Episodes = s.Episodes.OrderBy(e => e.EpisodeNumber).Select(e => new PodcastEpisode {
                    Id = e.Id,
                    SeasonId = e.SeasonId,
                    EpisodeNumber = e.EpisodeNumber,
                    Title = e.Title,
                    RuntimeSeconds = e.RuntimeSeconds,
                    PublishedDate = e.PublishedDate?.ToDateTime(TimeOnly.MinValue),
                    AudioFileRef = e.AudioFileRef,
                    Description = e.Description,
                    Credits = e.Credits.OrderBy(c => c.Ordinal).Select(ToCreditModel).ToArray(),
                    Identifiers = e.Identifiers.Select(i => ToIdentifierModel(i.Id, i.Scheme, i.Value, i.NormalizedValue, i.IsPrimary)).ToArray()
                }).ToArray()
            }).ToArray()
        };

    private static PodcastCredit ToCreditModel(PodcastCreditEntity credit)
        => new() {
            Id = credit.Id,
            DisplayName = credit.DisplayName,
            SortName = credit.SortName,
            Role = credit.Role,
            Ordinal = credit.Ordinal,
            PersonRef = credit.PersonRef
        };

    private static PodcastIdentifier ToIdentifierModel(Guid id, string scheme, string value, string normalized, bool isPrimary)
        => new() {
            Id = id,
            Scheme = scheme,
            Value = value,
            NormalizedValue = normalized,
            IsPrimary = isPrimary
        };
}
