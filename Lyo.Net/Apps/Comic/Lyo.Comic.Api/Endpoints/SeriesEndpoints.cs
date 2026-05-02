using Lyo.Comic.Api.Models.Request;
using Lyo.Comic.Api.Services;
using Lyo.Comment;
using Lyo.Common.Identifiers;
using Lyo.Favorite;
using Lyo.Rating;
using Lyo.Tag;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lyo.Comic.Api.Endpoints;

public static class SeriesEndpoints
{
    private const string EntityType = "ComicSeries";

    public static IEndpointRouteBuilder MapSeriesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/series").WithTags("Series");

        // Enriched reads — builder handles Query (POST /Query) and plain CRUD
        group.MapGet("/{id:guid}", GetSeriesById);
        group.MapGet("/slug/{slug}", GetSeriesBySlug);

        // Search
        group.MapPost("/search", Search);

        // Chapters by series
        group.MapGet("/{id:guid}/chapters", GetChapters);

        // Cross-domain sub-resources
        group.MapGet("/tags", GetAllTags);
        group.MapGet("/{id:guid}/tags", GetTags);
        group.MapPost("/{id:guid}/tags", AddTag);
        group.MapDelete("/{id:guid}/tags/{tag}", RemoveTag);

        group.MapGet("/{id:guid}/ratings", GetRatings);
        group.MapPost("/{id:guid}/ratings", AddRating);

        group.MapGet("/{id:guid}/comments", GetComments);
        group.MapPost("/{id:guid}/comments", AddComment);

        group.MapPost("/{id:guid}/favorites", AddFavorite);
        group.MapDelete("/{id:guid}/favorites", RemoveFavorite);

        return app;
    }

    private static async Task<IResult> GetSeriesById(
        Guid id,
        IComicStore store,
        ComicEnrichmentService enricher,
        CancellationToken ct = default)
    {
        var series = await store.GetSeriesByIdAsync(id, ct);
        if (series == null) return Results.NotFound();
        return Results.Ok(await enricher.EnrichSeriesAsync(series, ct: ct));
    }

    private static async Task<IResult> GetSeriesBySlug(
        string slug,
        IComicStore store,
        ComicEnrichmentService enricher,
        CancellationToken ct = default)
    {
        var series = await store.GetSeriesBySlugAsync(slug, ct);
        if (series == null) return Results.NotFound();
        return Results.Ok(await enricher.EnrichSeriesAsync(series, ct: ct));
    }

    private static async Task<IResult> Search(
        ComicSeriesQuery query,
        IComicStore store,
        ITagStore tagStore,
        ComicEnrichmentService enricher,
        CancellationToken ct = default)
    {
        if (query.Tags is { Count: > 0 })
        {
            HashSet<Guid>? seriesIds = null;
            foreach (var tag in query.Tags)
            {
                var records = await tagStore.GetEntitiesWithTagAsync(tag, EntityType, ct: ct);
                var ids     = records.Select(r => Guid.Parse(r.ForEntityId)).ToHashSet();
                if (seriesIds is null) seriesIds = ids;
                else seriesIds.IntersectWith(ids);
            }
            query.FilterSeriesIds = seriesIds?.ToList() ?? [];
        }

        var results  = await store.SearchSeriesAsync(query, ct);
        var enriched = await enricher.EnrichSeriesListAsync(results, ct: ct);
        return Results.Ok(enriched);
    }

    private static async Task<IResult> GetChapters(
        Guid id,
        IComicStore store,
        string? language = null,
        CancellationToken ct = default)
    {
        var chapters = await store.GetChaptersBySeriesAsync(id, language, ct);
        return Results.Ok(chapters);
    }

    private static async Task<IResult> GetAllTags(
        ITagStore tagStore,
        CancellationToken ct = default)
    {
        var tags = await tagStore.GetAllTagsForEntityTypeAsync(EntityType, ct: ct);
        return Results.Ok(tags);
    }

    private static async Task<IResult> GetTags(
        Guid id,
        ITagStore tagStore,
        CancellationToken ct = default)
    {
        var tags = await tagStore.GetTagsForEntityAsync(new EntityRef(EntityType, id.ToString()), ct: ct);
        return Results.Ok(tags.Select(t => t.Tag).ToList());
    }

    private static async Task<IResult> AddTag(
        Guid id,
        AddTagReq req,
        ITagStore tagStore,
        CancellationToken ct = default)
    {
        await tagStore.AddTagAsync(new EntityRef(EntityType, id.ToString()), req.Tag, ct: ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveTag(
        Guid id,
        string tag,
        ITagStore tagStore,
        CancellationToken ct = default)
    {
        await tagStore.RemoveTagAsync(new EntityRef(EntityType, id.ToString()), tag, ct: ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetRatings(
        Guid id,
        IRatingStore ratingStore,
        CancellationToken ct = default)
    {
        var ratings = await ratingStore.GetForEntityAsync(new EntityRef(EntityType, id.ToString()), ct);
        return Results.Ok(ratings);
    }

    private static async Task<IResult> AddRating(
        Guid id,
        AddRatingReq req,
        IRatingStore ratingStore,
        CancellationToken ct = default)
    {
        var record = new RatingRecord {
            Id              = Guid.NewGuid(),
            ForEntityType   = EntityType,
            ForEntityId     = id.ToString(),
            FromEntityType  = req.FromEntityType,
            FromEntityId    = req.FromEntityId,
            Subject         = req.Subject,
            Title           = req.Title,
            Value           = req.Value,
            Message         = req.Message
        };
        await ratingStore.SaveAsync(record, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetComments(
        Guid id,
        ICommentStore commentStore,
        bool includeReplies = true,
        CancellationToken ct = default)
    {
        var comments = await commentStore.GetForEntityAsync(new EntityRef(EntityType, id.ToString()), includeReplies, ct);
        return Results.Ok(comments);
    }

    private static async Task<IResult> AddComment(
        Guid id,
        AddCommentReq req,
        ICommentStore commentStore,
        CancellationToken ct = default)
    {
        var record = new CommentRecord {
            Id              = Guid.NewGuid(),
            ForEntityType   = EntityType,
            ForEntityId     = id.ToString(),
            FromEntityType  = req.FromEntityType,
            FromEntityId    = req.FromEntityId,
            Content         = req.Content,
            ReplyToCommentId = req.ReplyToCommentId
        };
        await commentStore.SaveAsync(record, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> AddFavorite(
        Guid id,
        AddFavoriteReq req,
        IFavoriteStore favoriteStore,
        CancellationToken ct = default)
    {
        var record = new FavoriteRecord {
            Id             = Guid.NewGuid(),
            ForEntityType  = EntityType,
            ForEntityId    = id.ToString(),
            FromEntityType = req.FromEntityType,
            FromEntityId   = req.FromEntityId
        };
        await favoriteStore.SaveAsync(record, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveFavorite(
        Guid id,
        [FromBody] RemoveFavoriteReq req,
        IFavoriteStore favoriteStore,
        CancellationToken ct = default)
    {
        await favoriteStore.DeleteAsync(
            new EntityRef(EntityType, id.ToString()),
            new EntityRef(req.FromEntityType, req.FromEntityId),
            ct);
        return Results.NoContent();
    }
}
