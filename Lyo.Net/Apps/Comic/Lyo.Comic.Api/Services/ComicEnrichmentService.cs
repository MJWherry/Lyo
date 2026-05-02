using Lyo.Comic.Api.Models.Response;
using Lyo.Comment;
using Lyo.Common.Identifiers;
using Lyo.Favorite;
using Lyo.Rating;
using Lyo.Tag;

namespace Lyo.Comic.Api.Services;

/// <summary>
/// Aggregates the tag, rating, comment, and favorite stores to produce enriched response models
/// for comic domain entities. Each Enrich method runs the cross-domain queries in parallel.
/// </summary>
public sealed class ComicEnrichmentService
{
    private const string SeriesEntityType = "ComicSeries";
    private const string VolumeEntityType = "ComicVolume";
    private const string ChapterEntityType = "ComicChapter";

    private readonly ITagStore _tagStore;
    private readonly IRatingStore _ratingStore;
    private readonly ICommentStore _commentStore;
    private readonly IFavoriteStore _favoriteStore;

    public ComicEnrichmentService(
        ITagStore tagStore,
        IRatingStore ratingStore,
        ICommentStore commentStore,
        IFavoriteStore favoriteStore)
    {
        _tagStore = tagStore;
        _ratingStore = ratingStore;
        _commentStore = commentStore;
        _favoriteStore = favoriteStore;
    }

    /// <summary>Enriches a series domain model with cross-domain metadata.</summary>
    /// <param name="series">The comic series domain model.</param>
    /// <param name="callerRef">Optional entity reference for the caller (used to compute IsFavorited).</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<ComicSeriesRes> EnrichSeriesAsync(
        ComicSeries series,
        EntityRef? callerRef = null,
        CancellationToken ct = default)
    {
        var entityRef = new EntityRef(SeriesEntityType, series.Id.ToString());

        var tagsTask = _tagStore.GetTagsForEntityAsync(entityRef, ct: ct);
        var ratingsTask = _ratingStore.GetForEntityAsync(entityRef, ct);
        var commentsTask = _commentStore.GetForEntityAsync(entityRef, includeReplies: false, ct: ct);
        var favoriteCountTask = _favoriteStore.GetCountForEntityAsync(entityRef, ct);
        var isFavoritedTask = callerRef.HasValue
            ? _favoriteStore.IsFavoritedAsync(entityRef, callerRef.Value, ct).ContinueWith(t => (bool?)t.Result, TaskContinuationOptions.ExecuteSynchronously)
            : Task.FromResult<bool?>(null);

        await Task.WhenAll(tagsTask, ratingsTask, commentsTask, favoriteCountTask, isFavoritedTask).ConfigureAwait(false);

        var ratings = await ratingsTask;
        var ratingValues = ratings.Where(r => r.Value.HasValue).Select(r => r.Value!.Value).ToList();

        return new ComicSeriesRes {
            Id = series.Id,
            Title = series.Title,
            Slug = series.Slug,
            ComicType = series.ComicType,
            Status = series.Status,
            Description = series.Description,
            Language = series.Language,
            PublishedYear = series.PublishedYear,
            Author = series.Author,
            Artist = series.Artist,
            Publisher = series.Publisher,
            Source = series.Source,
            CoverImageRef = series.CoverImageRef,
            Demographic = series.Demographic,
            CreatedTimestamp = series.CreatedTimestamp,
            UpdatedTimestamp = series.UpdatedTimestamp,
            AlternateTitles = series.AlternateTitles
                .Select(a => new ComicAlternateTitleRes { Id = a.Id, Title = a.Title, Language = a.Language })
                .ToList(),
            Tags = (await tagsTask).Select(t => t.Tag).ToList(),
            AverageRating = ratingValues.Count > 0 ? Math.Round(ratingValues.Average(), 2) : null,
            RatingCount = ratings.Count,
            CommentCount = (await commentsTask).Count,
            FavoriteCount = await favoriteCountTask,
            IsFavorited = await isFavoritedTask
        };
    }

    /// <summary>Enriches a volume domain model with cross-domain metadata.</summary>
    /// <param name="volume">The comic volume domain model.</param>
    /// <param name="callerRef">Optional entity reference for the caller (used to compute IsFavorited).</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<ComicVolumeRes> EnrichVolumeAsync(
        ComicVolume volume,
        EntityRef? callerRef = null,
        CancellationToken ct = default)
    {
        var entityRef = new EntityRef(VolumeEntityType, volume.Id.ToString());

        var tagsTask = _tagStore.GetTagsForEntityAsync(entityRef, ct: ct);
        var ratingsTask = _ratingStore.GetForEntityAsync(entityRef, ct);
        var commentsTask = _commentStore.GetForEntityAsync(entityRef, includeReplies: false, ct: ct);
        var favoriteCountTask = _favoriteStore.GetCountForEntityAsync(entityRef, ct);
        var isFavoritedTask = callerRef.HasValue
            ? _favoriteStore.IsFavoritedAsync(entityRef, callerRef.Value, ct).ContinueWith(t => (bool?)t.Result, TaskContinuationOptions.ExecuteSynchronously)
            : Task.FromResult<bool?>(null);

        await Task.WhenAll(tagsTask, ratingsTask, commentsTask, favoriteCountTask, isFavoritedTask).ConfigureAwait(false);

        var ratings = await ratingsTask;
        var ratingValues = ratings.Where(r => r.Value.HasValue).Select(r => r.Value!.Value).ToList();

        return new ComicVolumeRes {
            Id = volume.Id,
            SeriesId = volume.SeriesId,
            VolumeNumber = volume.VolumeNumber,
            Title = volume.Title,
            CoverImageRef = volume.CoverImageRef,
            PublishedDate = volume.PublishedDate,
            CreatedTimestamp = volume.CreatedTimestamp,
            UpdatedTimestamp = volume.UpdatedTimestamp,
            Tags = (await tagsTask).Select(t => t.Tag).ToList(),
            AverageRating = ratingValues.Count > 0 ? Math.Round(ratingValues.Average(), 2) : null,
            RatingCount = ratings.Count,
            CommentCount = (await commentsTask).Count,
            FavoriteCount = await favoriteCountTask,
            IsFavorited = await isFavoritedTask
        };
    }

    /// <summary>Enriches a chapter domain model with cross-domain metadata.</summary>
    /// <param name="chapter">The comic chapter domain model.</param>
    /// <param name="callerRef">Optional entity reference for the caller (used to compute IsFavorited).</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<ComicChapterRes> EnrichChapterAsync(
        ComicChapter chapter,
        EntityRef? callerRef = null,
        CancellationToken ct = default)
    {
        var entityRef = new EntityRef(ChapterEntityType, chapter.Id.ToString());

        var tagsTask = _tagStore.GetTagsForEntityAsync(entityRef, ct: ct);
        var ratingsTask = _ratingStore.GetForEntityAsync(entityRef, ct);
        var commentsTask = _commentStore.GetForEntityAsync(entityRef, includeReplies: false, ct: ct);
        var favoriteCountTask = _favoriteStore.GetCountForEntityAsync(entityRef, ct);
        var isFavoritedTask = callerRef.HasValue
            ? _favoriteStore.IsFavoritedAsync(entityRef, callerRef.Value, ct).ContinueWith(t => (bool?)t.Result, TaskContinuationOptions.ExecuteSynchronously)
            : Task.FromResult<bool?>(null);

        await Task.WhenAll(tagsTask, ratingsTask, commentsTask, favoriteCountTask, isFavoritedTask).ConfigureAwait(false);

        var ratings = await ratingsTask;
        var ratingValues = ratings.Where(r => r.Value.HasValue).Select(r => r.Value!.Value).ToList();

        return new ComicChapterRes {
            Id = chapter.Id,
            SeriesId = chapter.SeriesId,
            VolumeId = chapter.VolumeId,
            ChapterNumber = chapter.ChapterNumber,
            Title = chapter.Title,
            Language = chapter.Language,
            PageCount = chapter.PageCount,
            PublishedDate = chapter.PublishedDate,
            Source = chapter.Source,
            CreatedTimestamp = chapter.CreatedTimestamp,
            UpdatedTimestamp = chapter.UpdatedTimestamp,
            Tags = (await tagsTask).Select(t => t.Tag).ToList(),
            AverageRating = ratingValues.Count > 0 ? Math.Round(ratingValues.Average(), 2) : null,
            RatingCount = ratings.Count,
            CommentCount = (await commentsTask).Count,
            FavoriteCount = await favoriteCountTask,
            IsFavorited = await isFavoritedTask
        };
    }

    /// <summary>Enriches a list of series in parallel.</summary>
    public Task<ComicSeriesRes[]> EnrichSeriesListAsync(
        IReadOnlyList<ComicSeries> items,
        EntityRef? callerRef = null,
        CancellationToken ct = default)
        => Task.WhenAll(items.Select(s => EnrichSeriesAsync(s, callerRef, ct)));

    /// <summary>Enriches a list of volumes in parallel.</summary>
    public Task<ComicVolumeRes[]> EnrichVolumeListAsync(
        IReadOnlyList<ComicVolume> items,
        EntityRef? callerRef = null,
        CancellationToken ct = default)
        => Task.WhenAll(items.Select(v => EnrichVolumeAsync(v, callerRef, ct)));

    /// <summary>Enriches a list of chapters in parallel.</summary>
    public Task<ComicChapterRes[]> EnrichChapterListAsync(
        IReadOnlyList<ComicChapter> items,
        EntityRef? callerRef = null,
        CancellationToken ct = default)
        => Task.WhenAll(items.Select(c => EnrichChapterAsync(c, callerRef, ct)));
}
