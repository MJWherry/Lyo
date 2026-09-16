using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Health;
using Lyo.Music.Enums;
using Lyo.Music.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lyo.Music.Postgres;

/// <summary>Postgres-backed <see cref="IMusicStore" />.</summary>
public sealed class PostgresMusicStore : IMusicStore, IHealth
{
    private readonly IDbContextFactory<MusicDbContext> _contextFactory;

    public PostgresMusicStore(IDbContextFactory<MusicDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public string HealthCheckName => "music-postgres";

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
        => PostgresHealth.CheckAsync(_contextFactory, PostgresMusicOptions.Schema, ct);

    /// <inheritdoc />
    public async Task SaveReleaseAsync(MusicRelease release, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(release);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = release.Id != Guid.Empty
            ? await ctx.Releases
                .Include(r => r.Identifiers)
                .Include(r => r.Credits)
                .Include(r => r.Tracks).ThenInclude(t => t.Credits)
                .Include(r => r.GenreLinks)
                .FirstOrDefaultAsync(r => r.Id == release.Id, ct)
                .ConfigureAwait(false)
            : null;

        var id = existing?.Id ?? (release.Id == Guid.Empty ? LyoGuid.CreateV7() : release.Id);
        if (existing == null)
            ctx.Releases.Add(existing = new MusicReleaseEntity { Id = id, CreatedTimestamp = DateTime.UtcNow });

        ApplyReleaseFields(existing, release);
        existing.PrimaryArtist = ComputePrimaryArtist(release.Credits);

        ctx.Identifiers.RemoveRange(existing.Identifiers);
        foreach (var ident in release.Identifiers)
            ctx.Identifiers.Add(ToIdentifierEntity(ident, id));

        ctx.Credits.RemoveRange(existing.Credits);
        foreach (var credit in release.Credits)
            ctx.Credits.Add(ToCreditEntity(credit, id, null));

        ReplaceTracks(ctx, existing, release.Tracks, id);
        ctx.ReleaseGenres.RemoveRange(existing.GenreLinks);
        foreach (var genreId in release.GenreIds.Distinct())
            ctx.ReleaseGenres.Add(new MusicReleaseGenreEntity { ReleaseId = id, GenreId = genreId });

        await SaveCatchingConflictAsync(ctx, "MusicRelease", ct).ConfigureAwait(false);
        release.Id = id;
        release.PrimaryArtist = existing.PrimaryArtist;
    }

    /// <inheritdoc />
    public async Task<MusicRelease?> GetReleaseByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await LoadReleaseQuery(ctx).FirstOrDefaultAsync(r => r.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : ToRelease(entity);
    }

    /// <inheritdoc />
    public async Task<MusicRelease?> GetReleaseBySlugAsync(string slug, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(slug);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await LoadReleaseQuery(ctx).FirstOrDefaultAsync(r => r.Slug == slug, ct).ConfigureAwait(false);
        return entity == null ? null : ToRelease(entity);
    }

    /// <inheritdoc />
    public async Task<MusicRelease?> GetReleaseByIdentifierAsync(string scheme, string value, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(scheme);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        var normalized = MusicIdentifierSchemes.Normalize(scheme, value);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var releaseId = await ctx.Identifiers.AsNoTracking()
            .Where(i => i.Scheme == scheme && i.NormalizedValue == normalized)
            .Select(i => (Guid?)i.ReleaseId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        return releaseId is null ? null : await GetReleaseByIdAsync(releaseId.Value, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteReleaseAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Releases.FirstOrDefaultAsync(r => r.Id == id, ct).ConfigureAwait(false);
        if (entity == null)
            return;

        ctx.Releases.Remove(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveReleaseGroupAsync(MusicReleaseGroup group, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(group);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = group.Id != Guid.Empty
            ? await ctx.ReleaseGroups.FirstOrDefaultAsync(g => g.Id == group.Id, ct).ConfigureAwait(false)
            : null;
        var id = existing?.Id ?? (group.Id == Guid.Empty ? LyoGuid.CreateV7() : group.Id);
        if (existing == null)
            ctx.ReleaseGroups.Add(existing = new MusicReleaseGroupEntity { Id = id, CreatedTimestamp = DateTime.UtcNow });

        existing.Title = group.Title;
        existing.Slug = group.Slug;
        existing.Kind = group.Kind;
        existing.Status = group.Status;
        await SaveCatchingConflictAsync(ctx, "MusicReleaseGroup", ct).ConfigureAwait(false);
        group.Id = id;
    }

    /// <inheritdoc />
    public async Task<MusicReleaseGroup?> GetReleaseGroupByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.ReleaseGroups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : ToGroup(entity);
    }

    /// <inheritdoc />
    public async Task<MusicReleaseGroup?> GetReleaseGroupBySlugAsync(string slug, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(slug);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.ReleaseGroups.AsNoTracking().FirstOrDefaultAsync(g => g.Slug == slug, ct).ConfigureAwait(false);
        return entity == null ? null : ToGroup(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MusicReleaseGroup>> ListReleaseGroupsAsync(CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await ctx.ReleaseGroups.AsNoTracking().OrderBy(g => g.Title).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ToGroup).ToArray();
    }

    /// <inheritdoc />
    public async Task DeleteReleaseGroupAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.ReleaseGroups.FirstOrDefaultAsync(g => g.Id == id, ct).ConfigureAwait(false);
        if (entity == null)
            return;

        var members = await ctx.Releases.Where(r => r.ReleaseGroupId == id).ToListAsync(ct).ConfigureAwait(false);
        foreach (var release in members)
            release.ReleaseGroupId = null;

        ctx.ReleaseGroups.Remove(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveGenreAsync(MusicGenre genre, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(genre);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var id = genre.Id == Guid.Empty ? LyoGuid.CreateV7() : genre.Id;
        if (genre.ParentGenreId is Guid parent)
            await EnsureNoGenreCycleAsync(ctx, id, parent, ct).ConfigureAwait(false);

        var existing = genre.Id != Guid.Empty ? await ctx.Genres.FirstOrDefaultAsync(g => g.Id == genre.Id, ct).ConfigureAwait(false) : null;
        if (existing == null)
            ctx.Genres.Add(existing = new MusicGenreEntity { Id = id });

        existing.ParentGenreId = genre.ParentGenreId;
        existing.Slug = genre.Slug;
        existing.Name = genre.Name;
        existing.SortOrder = genre.SortOrder;
        await SaveCatchingConflictAsync(ctx, "MusicGenre", ct).ConfigureAwait(false);
        genre.Id = id;
    }

    /// <inheritdoc />
    public async Task<MusicGenre?> GetGenreByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Genres.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : ToGenre(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MusicGenre>> ListGenresAsync(CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await ctx.Genres.AsNoTracking().OrderBy(g => g.Name).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ToGenre).ToArray();
    }

    /// <inheritdoc />
    public async Task DeleteGenreAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (await ctx.Genres.AnyAsync(g => g.ParentGenreId == id, ct).ConfigureAwait(false)
            || await ctx.ReleaseGenres.AnyAsync(g => g.GenreId == id, ct).ConfigureAwait(false))
            throw ConflictException.ForResource("MusicGenre", id);

        var entity = await ctx.Genres.FirstOrDefaultAsync(g => g.Id == id, ct).ConfigureAwait(false);
        if (entity == null)
            return;

        ctx.Genres.Remove(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveFormatAsync(MusicFormat format, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(format);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var id = format.Id == Guid.Empty ? LyoGuid.CreateV7() : format.Id;
        var existing = format.Id != Guid.Empty ? await ctx.Formats.FirstOrDefaultAsync(f => f.Id == format.Id, ct).ConfigureAwait(false) : null;
        if (existing == null)
            ctx.Formats.Add(existing = new MusicFormatEntity { Id = id });

        existing.Slug = format.Slug;
        existing.Name = format.Name;
        existing.SortOrder = format.SortOrder;
        await SaveCatchingConflictAsync(ctx, "MusicFormat", ct).ConfigureAwait(false);
        format.Id = id;
    }

    /// <inheritdoc />
    public async Task<MusicFormat?> GetFormatByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Formats.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : new MusicFormat { Id = entity.Id, Slug = entity.Slug, Name = entity.Name, SortOrder = entity.SortOrder };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MusicFormat>> ListFormatsAsync(CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await ctx.Formats.AsNoTracking().OrderBy(f => f.SortOrder).ThenBy(f => f.Name).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(f => new MusicFormat { Id = f.Id, Slug = f.Slug, Name = f.Name, SortOrder = f.SortOrder }).ToArray();
    }

    /// <inheritdoc />
    public async Task DeleteFormatAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (await ctx.Releases.AnyAsync(r => r.FormatId == id, ct).ConfigureAwait(false))
            throw ConflictException.ForResource("MusicFormat", id);

        var entity = await ctx.Formats.FirstOrDefaultAsync(f => f.Id == id, ct).ConfigureAwait(false);
        if (entity == null)
            return;

        ctx.Formats.Remove(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MusicRelease>> SearchReleasesAsync(MusicReleaseQuery query, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(query);
        if (query.FilterIds is { Count: 0 })
            return [];

        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var q = LoadReleaseQuery(ctx);
        if (!string.IsNullOrWhiteSpace(query.TitleContains)) {
            var needle = query.TitleContains.ToLowerInvariant();
            q = q.Where(r => r.Title.ToLower().Contains(needle));
        }

        if (query.Kind is MusicReleaseKind kind)
            q = q.Where(r => r.Kind == kind);
        if (query.FormatId is Guid formatId)
            q = q.Where(r => r.FormatId == formatId);
        if (query.GenreId is Guid genreId)
            q = q.Where(r => r.GenreLinks.Any(g => g.GenreId == genreId));
        if (query.Year is int year)
            q = q.Where(r => r.ReleaseDate != null && r.ReleaseDate.Value.Year == year);
        if (!string.IsNullOrWhiteSpace(query.IdentifierScheme) && !string.IsNullOrWhiteSpace(query.IdentifierValue)) {
            var normalized = MusicIdentifierSchemes.Normalize(query.IdentifierScheme, query.IdentifierValue);
            q = q.Where(r => r.Identifiers.Any(i => i.Scheme == query.IdentifierScheme && i.NormalizedValue == normalized));
        }

        if (query.FilterIds is not null)
            q = q.Where(r => query.FilterIds.Contains(r.Id));

        var rows = await q.OrderBy(r => r.Title).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ToRelease).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MusicLyricsHit>> SearchLyricsAsync(MusicLyricsQuery query, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.FullText))
            return [];

        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var fullText = query.FullText;
        return await ctx.Tracks.AsNoTracking()
            .Where(t => t.SearchVector.Matches(EF.Functions.PlainToTsQuery("english", fullText)))
            .OrderBy(t => t.ReleaseId)
            .ThenBy(t => t.DiscNumber)
            .ThenBy(t => t.TrackNumber)
            .Select(t => new MusicLyricsHit {
                TrackId = t.Id,
                ReleaseId = t.ReleaseId,
                Snippet = EF.Functions.PlainToTsQuery("english", fullText).GetResultHeadline(t.Lyrics ?? "")
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> GetTrackLyricsAsync(Guid trackId, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var row = await ctx.Tracks.AsNoTracking().Where(t => t.Id == trackId).Select(t => new { t.Lyrics }).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (row == null)
            throw NotFoundException.ForResource("MusicTrack", trackId);
        return row.Lyrics;
    }

    /// <inheritdoc />
    public async Task SaveTrackLyricsAsync(Guid trackId, string? lyrics, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Tracks.FirstOrDefaultAsync(t => t.Id == trackId, ct).ConfigureAwait(false);
        if (entity == null)
            throw NotFoundException.ForResource("MusicTrack", trackId);
        entity.Lyrics = lyrics;
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveRecordingAsync(MusicRecording recording, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(recording);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var id = recording.Id == Guid.Empty ? LyoGuid.CreateV7() : recording.Id;
        var existing = recording.Id != Guid.Empty ? await ctx.Recordings.FirstOrDefaultAsync(r => r.Id == recording.Id, ct).ConfigureAwait(false) : null;
        if (existing == null)
            ctx.Recordings.Add(existing = new MusicRecordingEntity { Id = id });
        existing.Title = recording.Title;
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        recording.Id = id;
    }

    /// <inheritdoc />
    public async Task<MusicRecording?> GetRecordingAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Recordings.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct).ConfigureAwait(false);
        if (entity == null)
            return null;

        var tracks = await ctx.Tracks.AsNoTracking().Where(t => t.RecordingId == id).OrderBy(t => t.Title).ToListAsync(ct).ConfigureAwait(false);
        return new MusicRecording {
            Id = entity.Id,
            Title = entity.Title,
            Tracks = tracks.Select(t => new MusicTrack {
                Id = t.Id,
                ReleaseId = t.ReleaseId,
                DiscNumber = t.DiscNumber,
                TrackNumber = t.TrackNumber,
                Title = t.Title,
                DurationSeconds = t.DurationSeconds,
                Isrc = t.Isrc,
                AudioFileRef = t.AudioFileRef,
                RecordingId = t.RecordingId
            }).ToArray()
        };
    }

    private static async Task EnsureNoGenreCycleAsync(MusicDbContext ctx, Guid genreId, Guid parentId, CancellationToken ct)
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

    private static void ReplaceTracks(MusicDbContext ctx, MusicReleaseEntity existing, IReadOnlyList<MusicTrack> tracks, Guid releaseId)
    {
        var incomingIds = tracks.Where(t => t.Id != Guid.Empty).Select(t => t.Id).ToHashSet();
        foreach (var old in existing.Tracks.Where(t => !incomingIds.Contains(t.Id)).ToList())
            ctx.Tracks.Remove(old);

        foreach (var track in tracks) {
            var match = track.Id != Guid.Empty ? existing.Tracks.FirstOrDefault(t => t.Id == track.Id) : null;
            if (match == null) {
                var trackId = track.Id == Guid.Empty ? LyoGuid.CreateV7() : track.Id;
                match = new MusicTrackEntity { Id = trackId, ReleaseId = releaseId, CreatedTimestamp = DateTime.UtcNow };
                ctx.Tracks.Add(match);
                track.Id = trackId;
            }

            match.DiscNumber = track.DiscNumber <= 0 ? 1 : track.DiscNumber;
            match.TrackNumber = track.TrackNumber;
            match.Title = track.Title;
            match.DurationSeconds = track.DurationSeconds;
            match.Isrc = NormalizeIsrc(track.Isrc);
            match.AudioFileRef = track.AudioFileRef;
            match.RecordingId = track.RecordingId;
            if (track.Lyrics != null)
                match.Lyrics = track.Lyrics;
            ctx.Credits.RemoveRange(match.Credits);
            foreach (var credit in track.Credits)
                ctx.Credits.Add(ToCreditEntity(credit, null, match.Id));
        }
    }

    private static MusicGenre ToGenre(MusicGenreEntity entity)
        => new() { Id = entity.Id, ParentGenreId = entity.ParentGenreId, Slug = entity.Slug, Name = entity.Name, SortOrder = entity.SortOrder };

    private static IQueryable<MusicReleaseEntity> LoadReleaseQuery(MusicDbContext ctx)
        => ctx.Releases.AsNoTracking()
            .Include(r => r.Identifiers)
            .Include(r => r.Credits)
            .Include(r => r.Tracks).ThenInclude(t => t.Credits)
            .Include(r => r.GenreLinks);

    private static async Task SaveCatchingConflictAsync(MusicDbContext ctx, string resource, CancellationToken ct)
    {
        try {
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) {
            throw ConflictException.ForResource(resource, innerException: ex);
        }
    }

    private static void ApplyReleaseFields(MusicReleaseEntity existing, MusicRelease release)
    {
        existing.Title = release.Title;
        existing.Slug = release.Slug;
        existing.ReleaseGroupId = release.ReleaseGroupId;
        existing.Kind = release.Kind;
        existing.Status = release.Status;
        existing.ReleaseDate = release.ReleaseDate.HasValue ? DateOnly.FromDateTime(release.ReleaseDate.Value) : null;
        existing.Label = release.Label;
        existing.Language = release.Language;
        existing.CoverImageRef = release.CoverImageRef;
        existing.FormatId = release.FormatId;
    }

    private static string? ComputePrimaryArtist(IReadOnlyList<MusicCredit> credits)
        => credits
            .Where(c => c.Role is MusicCreditRole.Artist or MusicCreditRole.Featured)
            .OrderBy(c => c.Ordinal)
            .Select(c => c.DisplayName)
            .FirstOrDefault();

    private static string? NormalizeIsrc(string? isrc)
    {
        if (string.IsNullOrWhiteSpace(isrc))
            return null;

        return isrc.Replace("-", "", StringComparison.Ordinal).Trim().ToUpperInvariant();
    }

    private static MusicIdentifierEntity ToIdentifierEntity(MusicIdentifier identifier, Guid releaseId)
        => new() {
            Id = identifier.Id == Guid.Empty ? LyoGuid.CreateV7() : identifier.Id,
            ReleaseId = releaseId,
            Scheme = identifier.Scheme,
            Value = identifier.Value,
            NormalizedValue = string.IsNullOrEmpty(identifier.NormalizedValue)
                ? MusicIdentifierSchemes.Normalize(identifier.Scheme, identifier.Value)
                : identifier.NormalizedValue,
            IsPrimary = identifier.IsPrimary
        };

    private static MusicCreditEntity ToCreditEntity(MusicCredit credit, Guid? releaseId, Guid? trackId)
        => new() {
            Id = credit.Id == Guid.Empty ? LyoGuid.CreateV7() : credit.Id,
            ReleaseId = releaseId,
            TrackId = trackId,
            DisplayName = credit.DisplayName,
            SortName = credit.SortName,
            Role = credit.Role,
            Ordinal = credit.Ordinal,
            PersonRef = credit.PersonRef
        };

    private static MusicRelease ToRelease(MusicReleaseEntity entity)
        => new() {
            Id = entity.Id,
            Title = entity.Title,
            Slug = entity.Slug,
            ReleaseGroupId = entity.ReleaseGroupId,
            Kind = entity.Kind,
            Status = entity.Status,
            ReleaseDate = entity.ReleaseDate?.ToDateTime(TimeOnly.MinValue),
            Label = entity.Label,
            Language = entity.Language,
            CoverImageRef = entity.CoverImageRef,
            FormatId = entity.FormatId,
            PrimaryArtist = entity.PrimaryArtist,
            CreatedTimestamp = entity.CreatedTimestamp,
            UpdatedTimestamp = entity.UpdatedTimestamp,
            Identifiers = entity.Identifiers.Select(i => new MusicIdentifier {
                Id = i.Id,
                ReleaseId = i.ReleaseId,
                Scheme = i.Scheme,
                Value = i.Value,
                NormalizedValue = i.NormalizedValue,
                IsPrimary = i.IsPrimary
            }).ToArray(),
            Credits = entity.Credits.OrderBy(c => c.Ordinal).Select(ToCredit).ToArray(),
            Tracks = entity.Tracks
                .OrderBy(t => t.DiscNumber)
                .ThenBy(t => t.TrackNumber)
                .Select(t => new MusicTrack {
                    Id = t.Id,
                    ReleaseId = t.ReleaseId,
                    DiscNumber = t.DiscNumber,
                    TrackNumber = t.TrackNumber,
                    Title = t.Title,
                    DurationSeconds = t.DurationSeconds,
                    Isrc = t.Isrc,
                    AudioFileRef = t.AudioFileRef,
                    RecordingId = t.RecordingId,
                    Credits = t.Credits.OrderBy(c => c.Ordinal).Select(ToCredit).ToArray()
                })
                .ToArray(),
            GenreIds = entity.GenreLinks.Select(g => g.GenreId).ToArray()
        };

    private static MusicCredit ToCredit(MusicCreditEntity credit)
        => new() {
            Id = credit.Id,
            DisplayName = credit.DisplayName,
            SortName = credit.SortName,
            Role = credit.Role,
            Ordinal = credit.Ordinal,
            PersonRef = credit.PersonRef
        };

    private static MusicReleaseGroup ToGroup(MusicReleaseGroupEntity entity)
        => new() {
            Id = entity.Id,
            Title = entity.Title,
            Slug = entity.Slug,
            Kind = entity.Kind,
            Status = entity.Status,
            CreatedTimestamp = entity.CreatedTimestamp,
            UpdatedTimestamp = entity.UpdatedTimestamp
        };
}
