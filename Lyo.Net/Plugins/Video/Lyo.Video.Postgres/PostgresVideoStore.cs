using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Google.Youtube.Models;
using Lyo.Health;
using Lyo.Postgres;
using Lyo.Video.Enums;
using Lyo.Video.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lyo.Video.Postgres;

/// <summary>Postgres-backed <see cref="IVideoStore" />. Does not call the YouTube Data API.</summary>
public sealed class PostgresVideoStore : IVideoStore, IHealth
{
    private readonly IDbContextFactory<VideoDbContext> _contextFactory;

    public PostgresVideoStore(IDbContextFactory<VideoDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public string HealthCheckName => "video-postgres";

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
        => PostgresHealth.CheckAsync(_contextFactory, PostgresVideoOptions.Schema, ct);

    /// <inheritdoc />
    public async Task SaveTitleAsync(VideoTitle title, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(title);
        if (title.Kind == VideoTitleKind.Movie && title.Seasons.Count > 0)
            throw new UnprocessableEntityException("A movie cannot own a season.");

        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = title.Id != Guid.Empty
            ? await LoadTitleQuery(ctx, tracking: true).FirstOrDefaultAsync(t => t.Id == title.Id, ct).ConfigureAwait(false)
            : null;

        var id = existing?.Id ?? (title.Id == Guid.Empty ? LyoGuid.CreateV7() : title.Id);
        if (existing == null)
            ctx.Titles.Add(existing = new VideoTitleEntity { Id = id, CreatedTimestamp = DateTime.UtcNow });

        existing.Title = title.Title;
        existing.Slug = title.Slug;
        existing.Kind = title.Kind;
        existing.Status = title.Status;
        existing.Year = title.Year;
        existing.Language = title.Language;
        existing.RuntimeSeconds = title.RuntimeSeconds;
        existing.CoverImageRef = title.CoverImageRef;
        existing.VideoFileRef = title.VideoFileRef;
        existing.Overview = title.Overview;
        if (title.Transcript != null)
            existing.Transcript = title.Transcript;
        existing.CollectionId = title.CollectionId;

        ctx.Credits.RemoveRange(existing.Credits);
        foreach (var credit in title.Credits)
            ctx.Credits.Add(ToCredit(credit, id, null));

        ctx.TitleIdentifiers.RemoveRange(existing.Identifiers);
        foreach (var identifier in title.Identifiers)
            ctx.TitleIdentifiers.Add(ToTitleIdentifier(identifier, id));

        ReplaceYoutube(ctx, existing.Youtube, title.Youtube, id, null);
        ReplaceSeasons(ctx, existing, title.Seasons, id);
        ctx.AlternateTitles.RemoveRange(existing.AlternateTitles);
        foreach (var alt in title.AlternateTitles)
            ctx.AlternateTitles.Add(new VideoAlternateTitleEntity {
                Id = alt.Id == Guid.Empty ? LyoGuid.CreateV7() : alt.Id,
                TitleId = id,
                Title = alt.Title,
                Language = alt.Language,
                Kind = alt.Kind,
                SortOrder = alt.SortOrder
            });
        ctx.TitleGenres.RemoveRange(existing.GenreLinks);
        foreach (var genreId in title.GenreIds.Distinct())
            ctx.TitleGenres.Add(new VideoTitleGenreEntity { TitleId = id, GenreId = genreId });

        await SaveCatchingConflictAsync(ctx, ct).ConfigureAwait(false);
        title.Id = id;
    }

    /// <inheritdoc />
    public async Task<VideoTitle?> GetTitleByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await LoadTitleQuery(ctx, tracking: false).FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : ToTitle(entity);
    }

    /// <inheritdoc />
    public async Task<VideoTitle?> GetTitleBySlugAsync(string slug, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(slug);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await LoadTitleQuery(ctx, tracking: false).FirstOrDefaultAsync(t => t.Slug == slug, ct).ConfigureAwait(false);
        return entity == null ? null : ToTitle(entity);
    }

    /// <inheritdoc />
    public async Task<VideoTitle?> GetTitleByIdentifierAsync(string scheme, string value, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(scheme);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        var normalized = VideoIdentifierSchemes.Normalize(scheme, value);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var titleId = await ctx.TitleIdentifiers.AsNoTracking()
            .Where(i => i.Scheme == scheme && i.NormalizedValue == normalized)
            .Select(i => (Guid?)i.TitleId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        return titleId is null ? null : await GetTitleByIdAsync(titleId.Value, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteTitleAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Titles.FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false);
        if (entity == null)
            return;

        ctx.Titles.Remove(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveGenreAsync(VideoGenre genre, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(genre);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var id = genre.Id == Guid.Empty ? LyoGuid.CreateV7() : genre.Id;
        if (genre.ParentGenreId is Guid parent)
            await EnsureNoGenreCycleAsync(ctx, id, parent, ct).ConfigureAwait(false);
        var existing = genre.Id != Guid.Empty ? await ctx.Genres.FirstOrDefaultAsync(g => g.Id == genre.Id, ct).ConfigureAwait(false) : null;
        if (existing == null)
            ctx.Genres.Add(existing = new VideoGenreEntity { Id = id });
        existing.ParentGenreId = genre.ParentGenreId;
        existing.Slug = genre.Slug;
        existing.Name = genre.Name;
        existing.SortOrder = genre.SortOrder;
        await SaveCatchingConflictAsync(ctx, ct).ConfigureAwait(false);
        genre.Id = id;
    }

    /// <inheritdoc />
    public async Task<VideoGenre?> GetGenreByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Genres.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : new VideoGenre { Id = entity.Id, ParentGenreId = entity.ParentGenreId, Slug = entity.Slug, Name = entity.Name, SortOrder = entity.SortOrder };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VideoGenre>> ListGenresAsync(CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await ctx.Genres.AsNoTracking().OrderBy(g => g.Name).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(g => new VideoGenre { Id = g.Id, ParentGenreId = g.ParentGenreId, Slug = g.Slug, Name = g.Name, SortOrder = g.SortOrder }).ToArray();
    }

    /// <inheritdoc />
    public async Task DeleteGenreAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (await ctx.Genres.AnyAsync(g => g.ParentGenreId == id, ct).ConfigureAwait(false)
            || await ctx.TitleGenres.AnyAsync(g => g.GenreId == id, ct).ConfigureAwait(false))
            throw ConflictException.ForResource("VideoGenre", id);
        var entity = await ctx.Genres.FirstOrDefaultAsync(g => g.Id == id, ct).ConfigureAwait(false);
        if (entity == null)
            return;
        ctx.Genres.Remove(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VideoTitle>> SearchTitlesAsync(VideoTitleQuery query, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(query);
        if (query.FilterIds is { Count: 0 })
            return [];

        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var q = LoadTitleQuery(ctx, tracking: false);
        if (!string.IsNullOrWhiteSpace(query.TitleContains)) {
            var needle = query.TitleContains.ToLowerInvariant();
            q = q.Where(t => t.Title.ToLower().Contains(needle) || t.AlternateTitles.Any(a => a.Title.ToLower().Contains(needle)));
        }

        if (query.Kind is VideoTitleKind kind)
            q = q.Where(t => t.Kind == kind);
        if (query.GenreId is Guid genreId)
            q = q.Where(t => t.GenreLinks.Any(g => g.GenreId == genreId));
        if (query.Year is int year)
            q = q.Where(t => t.Year == year);
        if (!string.IsNullOrWhiteSpace(query.IdentifierScheme) && !string.IsNullOrWhiteSpace(query.IdentifierValue)) {
            var normalized = VideoIdentifierSchemes.Normalize(query.IdentifierScheme, query.IdentifierValue);
            q = q.Where(t => t.Identifiers.Any(i => i.Scheme == query.IdentifierScheme && i.NormalizedValue == normalized));
        }

        if (query.FilterIds is not null)
            q = q.Where(t => query.FilterIds.Contains(t.Id));

        var rows = await q.OrderBy(t => t.Title).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ToTitle).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VideoTranscriptHit>> SearchTranscriptsAsync(VideoTranscriptQuery query, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.FullText))
            return [];

        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var fullText = query.FullText;
        var movies = ctx.Titles.AsNoTracking()
            .Where(t => t.Kind == VideoTitleKind.Movie && t.SearchVector.Matches(EF.Functions.PlainToTsQuery("english", fullText)))
            .Select(t => new VideoTranscriptHit {
                TitleId = t.Id,
                EpisodeId = null,
                Snippet = EF.Functions.PlainToTsQuery("english", fullText).GetResultHeadline(t.Transcript ?? "")
            });
        var episodes = ctx.Episodes.AsNoTracking()
            .Where(e => e.SearchVector.Matches(EF.Functions.PlainToTsQuery("english", fullText)))
            .Select(e => new VideoTranscriptHit {
                TitleId = e.Season!.TitleId,
                EpisodeId = e.Id,
                Snippet = EF.Functions.PlainToTsQuery("english", fullText).GetResultHeadline(e.Transcript ?? "")
            });
        return await movies.Concat(episodes).ToListAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> GetTitleTranscriptAsync(Guid titleId, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var row = await ctx.Titles.AsNoTracking().Where(t => t.Id == titleId).Select(t => new { t.Transcript }).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (row == null)
            throw NotFoundException.ForResource("VideoTitle", titleId);
        return row.Transcript;
    }

    /// <inheritdoc />
    public async Task SaveTitleTranscriptAsync(Guid titleId, string? transcript, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Titles.FirstOrDefaultAsync(t => t.Id == titleId, ct).ConfigureAwait(false);
        if (entity == null)
            throw NotFoundException.ForResource("VideoTitle", titleId);
        entity.Transcript = transcript;
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> GetEpisodeTranscriptAsync(Guid episodeId, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var row = await ctx.Episodes.AsNoTracking().Where(e => e.Id == episodeId).Select(e => new { e.Transcript }).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (row == null)
            throw NotFoundException.ForResource("VideoEpisode", episodeId);
        return row.Transcript;
    }

    /// <inheritdoc />
    public async Task SaveEpisodeTranscriptAsync(Guid episodeId, string? transcript, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Episodes.FirstOrDefaultAsync(e => e.Id == episodeId, ct).ConfigureAwait(false);
        if (entity == null)
            throw NotFoundException.ForResource("VideoEpisode", episodeId);
        entity.Transcript = transcript;
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveCollectionAsync(VideoCollection collection, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(collection);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var id = collection.Id == Guid.Empty ? LyoGuid.CreateV7() : collection.Id;
        var existing = collection.Id != Guid.Empty ? await ctx.Collections.FirstOrDefaultAsync(c => c.Id == collection.Id, ct).ConfigureAwait(false) : null;
        if (existing == null)
            ctx.Collections.Add(existing = new VideoCollectionEntity { Id = id });
        existing.Title = collection.Title;
        existing.Slug = collection.Slug;
        await SaveCatchingConflictAsync(ctx, ct).ConfigureAwait(false);
        collection.Id = id;
    }

    /// <inheritdoc />
    public async Task DeleteCollectionAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Collections.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);
        if (entity == null)
            return;

        var members = await ctx.Titles.Where(t => t.CollectionId == id).ToListAsync(ct).ConfigureAwait(false);
        foreach (var title in members)
            title.CollectionId = null;

        ctx.Collections.Remove(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveCutAsync(VideoCut cut, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(cut);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var id = cut.Id == Guid.Empty ? LyoGuid.CreateV7() : cut.Id;
        var existing = cut.Id != Guid.Empty ? await ctx.Cuts.FirstOrDefaultAsync(c => c.Id == cut.Id, ct).ConfigureAwait(false) : null;
        if (existing == null)
            ctx.Cuts.Add(existing = new VideoCutEntity { Id = id, TitleId = cut.TitleId });
        existing.TitleId = cut.TitleId;
        existing.Name = cut.Name;
        existing.RuntimeSeconds = cut.RuntimeSeconds;
        existing.VideoFileRef = cut.VideoFileRef;
        existing.IsPrimary = cut.IsPrimary;
        await SaveCatchingConflictAsync(ctx, ct).ConfigureAwait(false);
        cut.Id = id;
    }

    private static async Task EnsureNoGenreCycleAsync(VideoDbContext ctx, Guid genreId, Guid parentId, CancellationToken ct)
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

    private static void ReplaceSeasons(VideoDbContext ctx, VideoTitleEntity existing, IReadOnlyList<VideoSeason> seasons, Guid titleId)
    {
        var incomingIds = seasons.Where(s => s.Id != Guid.Empty).Select(s => s.Id).ToHashSet();
        foreach (var old in existing.Seasons.Where(s => !incomingIds.Contains(s.Id)).ToList())
            ctx.Seasons.Remove(old);

        foreach (var season in seasons) {
            var match = season.Id != Guid.Empty ? existing.Seasons.FirstOrDefault(s => s.Id == season.Id) : null;
            if (match == null) {
                var seasonId = season.Id == Guid.Empty ? LyoGuid.CreateV7() : season.Id;
                match = new VideoSeasonEntity { Id = seasonId, TitleId = titleId };
                ctx.Seasons.Add(match);
                season.Id = seasonId;
            }

            match.SeasonNumber = season.SeasonNumber;
            match.Name = season.Name;
            match.Year = season.Year;
            ReplaceEpisodes(ctx, match, season.Episodes);
        }
    }

    private static void ReplaceEpisodes(VideoDbContext ctx, VideoSeasonEntity season, IReadOnlyList<VideoEpisode> episodes)
    {
        var incomingIds = episodes.Where(e => e.Id != Guid.Empty).Select(e => e.Id).ToHashSet();
        foreach (var old in season.Episodes.Where(e => !incomingIds.Contains(e.Id)).ToList())
            ctx.Episodes.Remove(old);

        foreach (var episode in episodes) {
            var match = episode.Id != Guid.Empty ? season.Episodes.FirstOrDefault(e => e.Id == episode.Id) : null;
            if (match == null) {
                var episodeId = episode.Id == Guid.Empty ? LyoGuid.CreateV7() : episode.Id;
                match = new VideoEpisodeEntity { Id = episodeId, SeasonId = season.Id, CreatedTimestamp = DateTime.UtcNow };
                ctx.Episodes.Add(match);
                episode.Id = episodeId;
            }

            match.EpisodeNumber = episode.EpisodeNumber;
            match.Title = episode.Title;
            match.RuntimeSeconds = episode.RuntimeSeconds;
            match.AirDate = episode.AirDate.HasValue ? DateOnly.FromDateTime(episode.AirDate.Value) : null;
            match.VideoFileRef = episode.VideoFileRef;
            match.StillImageRef = episode.StillImageRef;
            if (episode.Transcript != null)
                match.Transcript = episode.Transcript;
            ctx.Credits.RemoveRange(match.Credits);
            foreach (var credit in episode.Credits)
                ctx.Credits.Add(ToCredit(credit, null, match.Id));
            ctx.EpisodeIdentifiers.RemoveRange(match.Identifiers);
            foreach (var identifier in episode.Identifiers)
                ctx.EpisodeIdentifiers.Add(ToEpisodeIdentifier(identifier, match.Id));
            ReplaceYoutube(ctx, match.Youtube, episode.Youtube, null, match.Id);
        }
    }

    private static void ReplaceYoutube(VideoDbContext ctx, VideoYoutubeLinkEntity? existing, YoutubeVideoLink? link, Guid? titleId, Guid? episodeId)
    {
        if (link == null || string.IsNullOrWhiteSpace(link.VideoId)) {
            if (existing != null)
                ctx.YoutubeLinks.Remove(existing);
            return;
        }

        if (existing == null) {
            ctx.YoutubeLinks.Add(existing = new VideoYoutubeLinkEntity { Id = LyoGuid.CreateV7(), TitleId = titleId, EpisodeId = episodeId });
        }

        existing.VideoId = link.VideoId.Trim();
        existing.ChannelId = link.ChannelId.Trim();
        existing.PlaylistId = string.IsNullOrWhiteSpace(link.PlaylistId) ? null : link.PlaylistId.Trim();
    }

    private static IQueryable<VideoTitleEntity> LoadTitleQuery(VideoDbContext ctx, bool tracking)
    {
        var query = ctx.Titles
            .Include(t => t.Credits)
            .Include(t => t.Identifiers)
            .Include(t => t.Youtube)
            .Include(t => t.Seasons).ThenInclude(s => s.Episodes).ThenInclude(e => e.Credits)
            .Include(t => t.Seasons).ThenInclude(s => s.Episodes).ThenInclude(e => e.Identifiers)
            .Include(t => t.Seasons).ThenInclude(s => s.Episodes).ThenInclude(e => e.Youtube)
            .Include(t => t.GenreLinks)
            .Include(t => t.AlternateTitles);
        return tracking ? query : query.AsNoTracking();
    }

    private static async Task SaveCatchingConflictAsync(VideoDbContext ctx, CancellationToken ct)
    {
        try {
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) {
            throw ConflictException.ForResource("VideoTitle", innerException: ex);
        }
    }

    private static VideoCreditEntity ToCredit(VideoCredit credit, Guid? titleId, Guid? episodeId)
        => new() {
            Id = credit.Id == Guid.Empty ? LyoGuid.CreateV7() : credit.Id,
            TitleId = titleId,
            EpisodeId = episodeId,
            DisplayName = credit.DisplayName,
            SortName = credit.SortName,
            Role = credit.Role,
            Ordinal = credit.Ordinal,
            CharacterName = credit.CharacterName,
            PersonRef = credit.PersonRef
        };

    private static VideoTitleIdentifierEntity ToTitleIdentifier(VideoIdentifier identifier, Guid titleId)
        => new() {
            Id = identifier.Id == Guid.Empty ? LyoGuid.CreateV7() : identifier.Id,
            TitleId = titleId,
            Scheme = identifier.Scheme,
            Value = identifier.Value,
            NormalizedValue = string.IsNullOrEmpty(identifier.NormalizedValue)
                ? VideoIdentifierSchemes.Normalize(identifier.Scheme, identifier.Value)
                : identifier.NormalizedValue,
            IsPrimary = identifier.IsPrimary
        };

    private static VideoEpisodeIdentifierEntity ToEpisodeIdentifier(VideoIdentifier identifier, Guid episodeId)
        => new() {
            Id = identifier.Id == Guid.Empty ? LyoGuid.CreateV7() : identifier.Id,
            EpisodeId = episodeId,
            Scheme = identifier.Scheme,
            Value = identifier.Value,
            NormalizedValue = string.IsNullOrEmpty(identifier.NormalizedValue)
                ? VideoIdentifierSchemes.Normalize(identifier.Scheme, identifier.Value)
                : identifier.NormalizedValue,
            IsPrimary = identifier.IsPrimary
        };

    private static VideoTitle ToTitle(VideoTitleEntity entity)
        => new() {
            Id = entity.Id,
            Title = entity.Title,
            Slug = entity.Slug,
            Kind = entity.Kind,
            Status = entity.Status,
            Year = entity.Year,
            Language = entity.Language,
            RuntimeSeconds = entity.RuntimeSeconds,
            CoverImageRef = entity.CoverImageRef,
            VideoFileRef = entity.VideoFileRef,
            Overview = entity.Overview,
            CollectionId = entity.CollectionId,
            GenreIds = entity.GenreLinks.Select(g => g.GenreId).ToArray(),
            AlternateTitles = entity.AlternateTitles.OrderBy(a => a.SortOrder).Select(a => new VideoAlternateTitle {
                Id = a.Id,
                Title = a.Title,
                Language = a.Language,
                Kind = a.Kind,
                SortOrder = a.SortOrder
            }).ToArray(),
            CreatedTimestamp = entity.CreatedTimestamp,
            UpdatedTimestamp = entity.UpdatedTimestamp,
            Youtube = ToLink(entity.Youtube),
            Credits = entity.Credits.OrderBy(c => c.Ordinal).Select(ToCreditModel).ToArray(),
            Identifiers = entity.Identifiers.Select(ToIdentifierModel).ToArray(),
            Seasons = entity.Seasons.OrderBy(s => s.SeasonNumber).Select(s => new VideoSeason {
                Id = s.Id,
                TitleId = s.TitleId,
                SeasonNumber = s.SeasonNumber,
                Name = s.Name,
                Year = s.Year,
                Episodes = s.Episodes.OrderBy(e => e.EpisodeNumber).Select(e => new VideoEpisode {
                    Id = e.Id,
                    SeasonId = e.SeasonId,
                    EpisodeNumber = e.EpisodeNumber,
                    Title = e.Title,
                    RuntimeSeconds = e.RuntimeSeconds,
                    AirDate = e.AirDate?.ToDateTime(TimeOnly.MinValue),
                    VideoFileRef = e.VideoFileRef,
                    StillImageRef = e.StillImageRef,
                    Youtube = ToLink(e.Youtube),
                    Credits = e.Credits.OrderBy(c => c.Ordinal).Select(ToCreditModel).ToArray(),
                    Identifiers = e.Identifiers.Select(ToIdentifierModel).ToArray()
                }).ToArray()
            }).ToArray()
        };

    private static YoutubeVideoLink? ToLink(VideoYoutubeLinkEntity? link)
        => link == null ? null : new YoutubeVideoLink { VideoId = link.VideoId, ChannelId = link.ChannelId, PlaylistId = link.PlaylistId };

    private static VideoCredit ToCreditModel(VideoCreditEntity credit)
        => new() {
            Id = credit.Id,
            DisplayName = credit.DisplayName,
            SortName = credit.SortName,
            Role = credit.Role,
            Ordinal = credit.Ordinal,
            CharacterName = credit.CharacterName,
            PersonRef = credit.PersonRef
        };

    private static VideoIdentifier ToIdentifierModel(VideoTitleIdentifierEntity identifier)
        => ToIdentifierModel(identifier.Id, identifier.Scheme, identifier.Value, identifier.NormalizedValue, identifier.IsPrimary);

    private static VideoIdentifier ToIdentifierModel(VideoEpisodeIdentifierEntity identifier)
        => ToIdentifierModel(identifier.Id, identifier.Scheme, identifier.Value, identifier.NormalizedValue, identifier.IsPrimary);

    private static VideoIdentifier ToIdentifierModel(Guid id, string scheme, string value, string normalized, bool isPrimary)
        => new() {
            Id = id,
            Scheme = scheme,
            Value = value,
            NormalizedValue = normalized,
            IsPrimary = isPrimary
        };
}
