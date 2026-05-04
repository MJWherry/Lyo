using Lyo.Comic.Api.Models.Request;
using Lyo.Comic.Api.Services;
using Lyo.Comment;
using Lyo.Favorite;
using Lyo.Rating;
using Lyo.Tag;
using Microsoft.AspNetCore.Mvc;

namespace Lyo.Comic.Api.Endpoints;

public static class VolumeEndpoints
{
    private const string EntityType = "ComicVolume";

    public static IEndpointRouteBuilder MapVolumeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/volumes").WithTags("Volumes");

        // Enriched reads — builder handles Query (POST /Query) and plain CRUD
        group.MapGet("/{id:guid}", GetVolumeById);

        // Cross-domain sub-resources
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

    private static async Task<IResult> GetVolumeById(Guid id, IComicStore store, ComicEnrichmentService enricher, CancellationToken ct = default)
    {
        var volume = await store.GetVolumeByIdAsync(id, ct);
        if (volume == null)
            return Results.NotFound();

        return Results.Ok(await enricher.EnrichVolumeAsync(volume, ct: ct));
    }

    private static async Task<IResult> GetTags(Guid id, ITagStore tagStore, CancellationToken ct = default)
    {
        var tags = await tagStore.GetTagsForEntityAsync(new(EntityType, id.ToString()), ct: ct);
        return Results.Ok(tags.Select(t => t.Name).ToList());
    }

    private static async Task<IResult> AddTag(Guid id, AddTagReq req, ITagStore tagStore, CancellationToken ct = default)
    {
        await tagStore.AddTagAsync(new(EntityType, id.ToString()), req.Name, ResolveTagType(req.TagType), slug: req.Slug, ct: ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveTag(Guid id, string tag, string? tagType, string? slug, ITagStore tagStore, CancellationToken ct = default)
    {
        await tagStore.RemoveTagAsync(new(EntityType, id.ToString()), tag, ResolveTagType(tagType), slug: slug, ct: ct);
        return Results.NoContent();
    }

    private static string ResolveTagType(string? tagType)
        => string.IsNullOrWhiteSpace(tagType) ? "tag" : tagType.Trim();

    private static async Task<IResult> GetRatings(Guid id, IRatingStore ratingStore, CancellationToken ct = default)
    {
        var ratings = await ratingStore.GetForEntityAsync(new(EntityType, id.ToString()), ct);
        return Results.Ok(ratings);
    }

    private static async Task<IResult> AddRating(Guid id, AddRatingReq req, IRatingStore ratingStore, CancellationToken ct = default)
    {
        var record = new RatingRecord {
            Id = Guid.NewGuid(),
            ForEntityType = EntityType,
            ForEntityId = id.ToString(),
            FromEntityType = req.FromEntityType,
            FromEntityId = req.FromEntityId,
            Subject = req.Subject,
            Title = req.Title,
            Value = req.Value,
            Message = req.Message
        };

        await ratingStore.SaveAsync(record, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetComments(Guid id, ICommentStore commentStore, bool includeReplies = true, CancellationToken ct = default)
    {
        var comments = await commentStore.GetForEntityAsync(new(EntityType, id.ToString()), includeReplies, ct);
        return Results.Ok(comments);
    }

    private static async Task<IResult> AddComment(Guid id, AddCommentReq req, ICommentStore commentStore, CancellationToken ct = default)
    {
        var record = new CommentRecord {
            Id = Guid.NewGuid(),
            ForEntityType = EntityType,
            ForEntityId = id.ToString(),
            FromEntityType = req.FromEntityType,
            FromEntityId = req.FromEntityId,
            Content = req.Content,
            ReplyToCommentId = req.ReplyToCommentId
        };

        await commentStore.SaveAsync(record, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> AddFavorite(Guid id, AddFavoriteReq req, IFavoriteStore favoriteStore, CancellationToken ct = default)
    {
        var record = new FavoriteRecord {
            Id = Guid.NewGuid(),
            ForEntityType = EntityType,
            ForEntityId = id.ToString(),
            FromEntityType = req.FromEntityType,
            FromEntityId = req.FromEntityId
        };

        await favoriteStore.SaveAsync(record, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveFavorite(Guid id, [FromBody] RemoveFavoriteReq req, IFavoriteStore favoriteStore, CancellationToken ct = default)
    {
        await favoriteStore.DeleteAsync(new(EntityType, id.ToString()), new(req.FromEntityType, req.FromEntityId), ct);
        return Results.NoContent();
    }
}