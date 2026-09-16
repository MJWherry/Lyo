namespace Lyo.Music;

/// <summary>Persistence for the music catalog: release groups, releases, tracks, credits, and identifiers.</summary>
public interface IMusicStore
{
    /// <summary>Upserts a release and replaces its credits, identifiers, and tracks. Recomputes <see cref="MusicRelease.PrimaryArtist" />.</summary>
    Task SaveReleaseAsync(MusicRelease release, CancellationToken ct = default);

    /// <summary>Loads a release with credits, identifiers, and tracks ordered by disc then track number.</summary>
    Task<MusicRelease?> GetReleaseByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads a release by its unique slug.</summary>
    Task<MusicRelease?> GetReleaseBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>Loads a release by scheme and value. Barcodes are matched on digits only.</summary>
    Task<MusicRelease?> GetReleaseByIdentifierAsync(string scheme, string value, CancellationToken ct = default);

    /// <summary>Deletes a release and its tracks, credits, and identifiers.</summary>
    Task DeleteReleaseAsync(Guid id, CancellationToken ct = default);

    /// <summary>Upserts a release group.</summary>
    Task SaveReleaseGroupAsync(MusicReleaseGroup group, CancellationToken ct = default);

    /// <summary>Loads a release group by id.</summary>
    Task<MusicReleaseGroup?> GetReleaseGroupByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads a release group by its unique slug.</summary>
    Task<MusicReleaseGroup?> GetReleaseGroupBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>Lists release groups ordered by title.</summary>
    Task<IReadOnlyList<MusicReleaseGroup>> ListReleaseGroupsAsync(CancellationToken ct = default);

    /// <summary>Deletes a release group and clears <see cref="MusicRelease.ReleaseGroupId" /> on its releases. Releases stay.</summary>
    Task DeleteReleaseGroupAsync(Guid id, CancellationToken ct = default);

    /// <summary>Upserts a genre. A parent cycle is rejected.</summary>
    Task SaveGenreAsync(MusicGenre genre, CancellationToken ct = default);

    /// <summary>Loads a genre by id.</summary>
    Task<MusicGenre?> GetGenreByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lists genres ordered by name.</summary>
    Task<IReadOnlyList<MusicGenre>> ListGenresAsync(CancellationToken ct = default);

    /// <summary>Deletes a genre. Fails when a release or child genre still references it.</summary>
    Task DeleteGenreAsync(Guid id, CancellationToken ct = default);

    /// <summary>Upserts a release format.</summary>
    Task SaveFormatAsync(MusicFormat format, CancellationToken ct = default);

    /// <summary>Loads a format by id.</summary>
    Task<MusicFormat?> GetFormatByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lists formats ordered by sort order then name.</summary>
    Task<IReadOnlyList<MusicFormat>> ListFormatsAsync(CancellationToken ct = default);

    /// <summary>Deletes a format. Fails when a release still references it.</summary>
    Task DeleteFormatAsync(Guid id, CancellationToken ct = default);

    /// <summary>Searches releases. <see cref="MusicReleaseQuery.FilterIds" /> null means no id filter. An empty list returns no rows.</summary>
    Task<IReadOnlyList<MusicRelease>> SearchReleasesAsync(MusicReleaseQuery query, CancellationToken ct = default);

    /// <summary>Upserts a recording. Tracks point at it from their releases.</summary>
    Task SaveRecordingAsync(MusicRecording recording, CancellationToken ct = default);

    /// <summary>Loads a recording and every track that points at it.</summary>
    Task<MusicRecording?> GetRecordingAsync(Guid id, CancellationToken ct = default);

    /// <summary>Searches stored lyrics. Release reads omit the lyrics column.</summary>
    Task<IReadOnlyList<MusicLyricsHit>> SearchLyricsAsync(MusicLyricsQuery query, CancellationToken ct = default);

    /// <summary>Loads lyrics for one track. Throws when the track does not exist.</summary>
    Task<string?> GetTrackLyricsAsync(Guid trackId, CancellationToken ct = default);

    /// <summary>Replaces lyrics for one track and refreshes the full-text index.</summary>
    Task SaveTrackLyricsAsync(Guid trackId, string? lyrics, CancellationToken ct = default);
}
