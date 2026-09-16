using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.Mapping;
using Lyo.Common.Core.Identifiers;
using Lyo.EntityReference.Models;
using Lyo.Exceptions;
using Lyo.HomeInventory;
using Lyo.HomeInventory.FileStorage;
using Lyo.HomeInventory.Models;
using Lyo.HomeInventory.Models.Enums;
using Lyo.HomeInventory.Models.Request;
using Lyo.HomeInventory.Models.Response;
using Lyo.HomeInventory.Postgres;
using Lyo.HomeInventory.Postgres.Database;
using Lyo.Note;
using Lyo.Note.Postgres;
using Lyo.Tag;
using Lyo.Tag.Postgres;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.HomeInventory.Api;

/// <summary>HTTP mapping for the Lyo HomeInventory API. Register store DI with <c>AddHomeInventoryApiFromConfiguration</c> first.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the HomeInventory Postgres store, Note/Tag stores on the same connection, CRUD services, and the Req/Res mapper.</summary>
        public IServiceCollection AddHomeInventoryApi(Action<PostgresHomeInventoryOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresHomeInventoryOptions();
            configure(options);
            return services.AddHomeInventoryApi(options);
        }

        /// <summary>Registers HomeInventory from configuration section <see cref="PostgresHomeInventoryOptions.SectionName" />.</summary>
        public IServiceCollection AddHomeInventoryApiFromConfiguration(IConfiguration configuration, string configSectionName = PostgresHomeInventoryOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new PostgresHomeInventoryOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddHomeInventoryApi(options);
        }

        /// <summary>Registers the HomeInventory Postgres store, Note/Tag stores on the same connection, CRUD services, and the Req/Res mapper.</summary>
        public IServiceCollection AddHomeInventoryApi(PostgresHomeInventoryOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresHomeInventoryStore(options);
            services.AddPostgresNoteStore(new PostgresNoteOptions { ConnectionString = options.ConnectionString, EnableAutoMigrations = options.EnableAutoMigrations });
            services.AddPostgresTagStore(new PostgresTagOptions { ConnectionString = options.ConnectionString, EnableAutoMigrations = options.EnableAutoMigrations });
            services.AddLyoCrudServices<HomeInventoryDbContext>();
            return services;
        }
    }

    /// <summary>Maps HomeInventory CRUD, stock, notes/tags, and optional attachment upload. Invoke after <c>AddHomeInventoryApi</c>.</summary>
    public static WebApplication BuildHomeInventoryGroup(this WebApplication app)
    {
        var tag = Constants.Rest.HomeInventory.Route;
        app.CreateBuilder<HomeInventoryDbContext, HomeCategoryEntity, HomeCategoryReq, HomeCategoryRes, Guid>(Constants.Rest.HomeInventory.Categories, tag)
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id,
                    BeforeDeleteAsync = (ctx, ct) => CascadeNotesAndTagsAsync(ctx.Services, HomeInventoryEntityTypes.Category, ctx.Entity.Id, ct)
                })
            .Build();
        app.CreateBuilder<HomeInventoryDbContext, HomeLocationEntity, HomeLocationReq, HomeLocationRes, Guid>(Constants.Rest.HomeInventory.Locations, tag)
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id,
                    BeforeDeleteAsync = (ctx, ct) => CascadeNotesAndTagsAsync(ctx.Services, HomeInventoryEntityTypes.Location, ctx.Entity.Id, ct)
                })
            .Build();
        app.CreateBuilder<HomeInventoryDbContext, HomeItemEntity, HomeItemReq, HomeItemRes, Guid>(Constants.Rest.HomeInventory.Items, tag)
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id,
                    BeforeDeleteAsync = (ctx, ct) => CascadeNotesAndTagsAsync(ctx.Services, HomeInventoryEntityTypes.Item, ctx.Entity.Id, ct)
                })
            .Build();
        app.CreateReadOnlyBuilder<HomeInventoryDbContext, HomeItemMovementEntity, HomeItemMovementRes, Guid>(Constants.Rest.HomeInventory.Movements, tag)
            .WithCrud(ApiFeatureSet.ReadOnly, new())
            .Build();
        app.CreateBuilder<HomeInventoryDbContext, HomeAttachmentEntity, HomeAttachmentReq, HomeAttachmentRes, Guid>(Constants.Rest.HomeInventory.Attachments, tag)
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id })
            .Build();
        MapStockEndpoints(app);
        MapNotesAndTags(app, Constants.Rest.HomeInventory.Items, HomeInventoryEntityTypes.Item);
        MapNotesAndTags(app, Constants.Rest.HomeInventory.Locations, HomeInventoryEntityTypes.Location);
        MapNotesAndTags(app, Constants.Rest.HomeInventory.Categories, HomeInventoryEntityTypes.Category);
        MapAttachmentUpload(app);
        return app;
    }

    private static void MapStockEndpoints(WebApplication app)
    {
        var tag = Constants.Rest.HomeInventory.Route;
        var items = Constants.Rest.HomeInventory.Items;
        app.MapGet(
                $"/{items}/{{id:guid}}/Stock",
                async (Guid id, IHomeInventoryStore store, ILyoMapper mapper, Guid? tenantId, CancellationToken ct) => {
                    var rows = await store.GetStockForItemAsync(id, tenantId, ct).ConfigureAwait(false);
                    return Results.Ok(rows.Select(mapper.Map<HomeItemStockRes>).ToList());
                })
            .WithTags(tag)
            .WithName("GetHomeInventoryItemStock");
        app.MapPost(
                $"/{items}/{{id:guid}}/Stock",
                async (Guid id, HomeItemStockReq req, IHomeInventoryStore store, ILyoMapper mapper, Guid? tenantId, CancellationToken ct) => {
                    ArgumentHelpers.ThrowIfNull(req);
                    await store.UpsertStockAsync(id, req.LocationId, req.QuantityOnHand, req.QuantityReserved, req.ReorderPoint, tenantId, ct).ConfigureAwait(false);
                    var row = await store.GetStockAsync(id, req.LocationId, tenantId, ct).ConfigureAwait(false);
                    return Results.Ok(row == null ? null : mapper.Map<HomeItemStockRes>(row));
                })
            .WithTags(tag)
            .WithName("UpsertHomeInventoryItemStock");
        app.MapPost(
                $"/{items}/{{id:guid}}/Stock/Adjust",
                async (Guid id, HomeItemStockAdjustReq req, IHomeInventoryStore store, ILyoMapper mapper, Guid? tenantId, CancellationToken ct) => {
                    ArgumentHelpers.ThrowIfNull(req);
                    await store.AdjustStockAsync(id, req.LocationId, req.QuantityDelta, req.MovementType, req.ReferenceNumber, req.CreatedBy, tenantId, ct)
                        .ConfigureAwait(false);
                    var row = await store.GetStockAsync(id, req.LocationId, tenantId, ct).ConfigureAwait(false);
                    return Results.Ok(row == null ? null : mapper.Map<HomeItemStockRes>(row));
                })
            .WithTags(tag)
            .WithName("AdjustHomeInventoryItemStock");
        app.MapPost(
                $"/{items}/{{id:guid}}/Stock/Transfer",
                async (Guid id, HomeItemStockTransferReq req, IHomeInventoryStore store, ILyoMapper mapper, Guid? tenantId, CancellationToken ct) => {
                    ArgumentHelpers.ThrowIfNull(req);
                    await store.TransferStockAsync(id, req.FromLocationId, req.ToLocationId, req.Quantity, req.ReferenceNumber, req.CreatedBy, tenantId, ct)
                        .ConfigureAwait(false);
                    var rows = await store.GetStockForItemAsync(id, tenantId, ct).ConfigureAwait(false);
                    return Results.Ok(rows.Select(mapper.Map<HomeItemStockRes>).ToList());
                })
            .WithTags(tag)
            .WithName("TransferHomeInventoryItemStock");
    }

    private static void MapNotesAndTags(WebApplication app, string parentRoute, string entityType)
    {
        var tag = Constants.Rest.HomeInventory.Route;
        app.MapGet(
                $"/{parentRoute}/{{id:guid}}/Notes",
                async (Guid id, INoteStore notes, Guid? tenantId, CancellationToken ct)
                    => Results.Ok(await notes.GetForEntityAsync(EntityRef.ForGuid(entityType, id), tenantId, ct).ConfigureAwait(false)))
            .WithTags(tag);
        app.MapPost(
                $"/{parentRoute}/{{id:guid}}/Notes",
                async (Guid id, HomeNoteCreateReq req, INoteStore notes, Guid? tenantId, CancellationToken ct) => {
                    ArgumentHelpers.ThrowIfNull(req);
                    var note = new NoteRecord {
                        Id = LyoGuid.CreateV7(),
                        SubjectEntityType = entityType,
                        SubjectEntityId = id.ToString(),
                        ActorEntityType = EntityRefWellKnown.SystemActorType,
                        ActorEntityId = EntityRefWellKnown.SystemActorId.ToString(),
                        Content = req.Content
                    };
                    await notes.SaveAsync(note, tenantId, ct).ConfigureAwait(false);
                    return Results.Ok(note);
                })
            .WithTags(tag);
        app.MapDelete(
                $"/{parentRoute}/{{id:guid}}/Notes/{{noteId:guid}}",
                async (Guid id, Guid noteId, INoteStore notes, Guid? tenantId, CancellationToken ct) => {
                    var existing = await notes.GetByIdAsync(noteId, tenantId, ct).ConfigureAwait(false);
                    if (existing == null || !string.Equals(existing.SubjectEntityType, entityType, StringComparison.Ordinal)
                        || !string.Equals(existing.SubjectEntityId, id.ToString(), StringComparison.Ordinal))
                        return Results.NotFound();

                    await notes.DeleteAsync(noteId, tenantId, ct).ConfigureAwait(false);
                    return Results.NoContent();
                })
            .WithTags(tag);
        app.MapGet(
                $"/{parentRoute}/{{id:guid}}/Tags",
                async (Guid id, ITagStore tags, Guid? tenantId, CancellationToken ct)
                    => Results.Ok(await tags.GetTagsForEntityAsync(EntityRef.ForGuid(entityType, id), tenantId: tenantId, ct: ct).ConfigureAwait(false)))
            .WithTags(tag);
        app.MapPost(
                $"/{parentRoute}/{{id:guid}}/Tags",
                async (Guid id, HomeTagAddReq req, ITagStore tags, Guid? tenantId, CancellationToken ct) => {
                    ArgumentHelpers.ThrowIfNull(req);
                    await tags.AddTagAsync(EntityRef.ForGuid(entityType, id), req.Name, req.TagType, slug: req.Slug, tenantId: tenantId, ct: ct).ConfigureAwait(false);
                    return Results.Ok(await tags.GetTagsForEntityAsync(EntityRef.ForGuid(entityType, id), tenantId: tenantId, ct: ct).ConfigureAwait(false));
                })
            .WithTags(tag);
        app.MapDelete(
                $"/{parentRoute}/{{id:guid}}/Tags/{{name}}",
                async (Guid id, string name, ITagStore tags, string? tagType, Guid? tenantId, CancellationToken ct) => {
                    await tags.RemoveTagAsync(EntityRef.ForGuid(entityType, id), name, tagType ?? "tag", tenantId: tenantId, ct: ct).ConfigureAwait(false);
                    return Results.NoContent();
                })
            .WithTags(tag);
    }

    private static void MapAttachmentUpload(WebApplication app)
    {
        var tag = Constants.Rest.HomeInventory.Route;
        app.MapPost(
                $"/{Constants.Rest.HomeInventory.Attachments}/upload",
                async (
                    HttpContext http, [FromForm] string entityType, [FromForm] Guid entityId, [FromForm] string? role, [FromForm] string? caption, [FromForm] bool isPrimary,
                    IFormFile file, Guid? tenantId, CancellationToken ct) => {
                    var helper = http.RequestServices.GetService<IHomeInventoryAttachmentFiles>();
                    if (helper is null)
                        return Results.StatusCode(StatusCodes.Status501NotImplemented);

                    ArgumentHelpers.ThrowIfNull(file);
                    var parsedRole = Enum.TryParse<HomeAttachmentRole>(role, ignoreCase: true, out var parsed) ? parsed : HomeAttachmentRole.Document;
                    await using var stream = file.OpenReadStream();
                    var row = await helper.SaveAsync(entityType, entityId, stream, file.Length, file.FileName, parsedRole, caption, isPrimary, tenantId, ct)
                        .ConfigureAwait(false);
                    return Results.Ok(row);
                })
            .DisableAntiforgery()
            .WithTags(tag)
            .WithName("UploadHomeInventoryAttachment");
    }

    private static async Task CascadeNotesAndTagsAsync(IServiceProvider services, string entityType, Guid id, CancellationToken ct)
    {
        var subject = EntityRef.ForGuid(entityType, id);
        var notes = services.GetRequiredService<INoteStore>();
        var tags = services.GetRequiredService<ITagStore>();
        await notes.DeleteForEntityAsync(subject, ct: ct).ConfigureAwait(false);
        await tags.RemoveAllTagsForEntityAsync(subject, ct: ct).ConfigureAwait(false);
    }
}
