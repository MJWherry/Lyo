using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.Mapping;
using Lyo.Common.Core.Identifiers;
using Lyo.EntityReference.Models;
using Lyo.Exceptions;
using Lyo.Note;
using Lyo.Note.Postgres;
using Lyo.Recipe.FileStorage;
using Lyo.Recipe.Models;
using Lyo.Recipe.Models.Enums;
using Lyo.Recipe.Models.Request;
using Lyo.Recipe.Models.Response;
using Lyo.Recipe.Postgres;
using Lyo.Recipe.Postgres.Database;
using Lyo.Tag;
using Lyo.Tag.Postgres;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Recipe.Api;

/// <summary>HTTP mapping for the Lyo Recipe API. Register store DI with <c>AddRecipeApiFromConfiguration</c> first.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the Recipe Postgres store, Note/Tag stores on the same connection, and CRUD services.</summary>
        public IServiceCollection AddRecipeApi(Action<PostgresRecipeOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresRecipeOptions();
            configure(options);
            return services.AddRecipeApi(options);
        }

        /// <summary>Registers Recipe from configuration section <see cref="PostgresRecipeOptions.SectionName" />.</summary>
        public IServiceCollection AddRecipeApiFromConfiguration(IConfiguration configuration, string configSectionName = PostgresRecipeOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new PostgresRecipeOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddRecipeApi(options);
        }

        /// <summary>Registers the Recipe Postgres store, Note/Tag stores on the same connection, and CRUD services.</summary>
        public IServiceCollection AddRecipeApi(PostgresRecipeOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresRecipeStore(options);
            services.AddPostgresNoteStore(new PostgresNoteOptions { ConnectionString = options.ConnectionString, EnableAutoMigrations = options.EnableAutoMigrations });
            services.AddPostgresTagStore(new PostgresTagOptions { ConnectionString = options.ConnectionString, EnableAutoMigrations = options.EnableAutoMigrations });
            services.AddLyoCrudServices<RecipeDbContext>();
            return services;
        }
    }

    /// <summary>Maps Recipe CRUD, line/step replace, notes/tags, and optional attachment upload. Invoke after <c>AddRecipeApi</c>.</summary>
    public static WebApplication BuildRecipeGroup(this WebApplication app)
    {
        var tag = Constants.Rest.Recipe.Route;
        app.CreateBuilder<RecipeDbContext, RecipeCategoryEntity, RecipeCategoryReq, RecipeCategoryRes, Guid>(Constants.Rest.Recipe.Categories, tag)
            .WithCrud(
                ApiFeatureSet.DefaultCrud,
                new() {
                    BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateV7() : ctx.Entity.Id,
                    BeforeDeleteAsync = (ctx, ct) => CascadeNotesAndTagsAsync(ctx.Services, RecipeEntityTypes.RecipeCategory, ctx.Entity.Id, ct)
                })
            .Build();

        app.CreateBuilder<RecipeDbContext, IngredientEntity, IngredientReq, IngredientRes, Guid>(Constants.Rest.Recipe.Ingredients, tag)
            .WithCrud(
                ApiFeatureSet.DefaultCrud,
                new() {
                    BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateV7() : ctx.Entity.Id,
                    BeforeDeleteAsync = (ctx, ct) => CascadeNotesAndTagsAsync(ctx.Services, RecipeEntityTypes.Ingredient, ctx.Entity.Id, ct)
                })
            .Build();

        app.CreateBuilder<RecipeDbContext, RecipeEntity, RecipeReq, RecipeRes, Guid>(Constants.Rest.Recipe.Recipes, tag)
            .WithCrud(
                ApiFeatureSet.DefaultCrud,
                new() {
                    BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateV7() : ctx.Entity.Id,
                    BeforeDeleteAsync = (ctx, ct) => CascadeNotesAndTagsAsync(ctx.Services, RecipeEntityTypes.Recipe, ctx.Entity.Id, ct)
                })
            .Build();

        app.CreateBuilder<RecipeDbContext, RecipeAttachmentEntity, RecipeAttachmentReq, RecipeAttachmentRes, Guid>(Constants.Rest.Recipe.Attachments, tag)
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateV7() : ctx.Entity.Id })
            .Build();

        MapLineAndStepEndpoints(app);
        MapUnitAndUsageEndpoints(app);
        MapNotesAndTags(app, Constants.Rest.Recipe.Categories, RecipeEntityTypes.RecipeCategory);
        MapNotesAndTags(app, Constants.Rest.Recipe.Ingredients, RecipeEntityTypes.Ingredient);
        MapNotesAndTags(app, Constants.Rest.Recipe.Recipes, RecipeEntityTypes.Recipe);
        MapAttachmentUpload(app);
        return app;
    }

    private static void MapLineAndStepEndpoints(WebApplication app)
    {
        var tag = Constants.Rest.Recipe.Route;
        var recipes = Constants.Rest.Recipe.Recipes;
        app.MapGet(
                $"/{recipes}/{{id:guid}}/IngredientLines", async (Guid id, IRecipeStore store, ILyoMapper mapper, Guid? tenantId, CancellationToken ct) => {
                    var rows = await store.GetIngredientLinesAsync(id, tenantId, ct).ConfigureAwait(false);
                    return Results.Ok(rows.Select(mapper.Map<RecipeIngredientLineRes>).ToList());
                })
            .WithTags(tag)
            .WithName("GetRecipeIngredientLines");

        app.MapPut(
                $"/{recipes}/{{id:guid}}/IngredientLines", async (
                    Guid id, List<RecipeIngredientLineReq> req, IRecipeStore store, ILyoMapper mapper, Guid? tenantId, CancellationToken ct) => {
                    ArgumentHelpers.ThrowIfNull(req);
                    var lines = req.Select(r => new RecipeIngredientLineRecord {
                            Id = r.Id,
                            RecipeId = id,
                            IngredientId = r.IngredientId,
                            IngredientText = r.IngredientText,
                            SortOrder = r.SortOrder,
                            Quantity = r.Quantity,
                            Unit = r.Unit,
                            Preparation = r.Preparation,
                            IsOptional = r.IsOptional,
                            GroupLabel = r.GroupLabel,
                            StepIds = r.StepIds ?? []
                        })
                        .ToList();

                    await store.ReplaceIngredientLinesAsync(id, lines, tenantId, ct).ConfigureAwait(false);
                    var rows = await store.GetIngredientLinesAsync(id, tenantId, ct).ConfigureAwait(false);
                    return Results.Ok(rows.Select(mapper.Map<RecipeIngredientLineRes>).ToList());
                })
            .WithTags(tag)
            .WithName("ReplaceRecipeIngredientLines");

        app.MapGet(
                $"/{recipes}/{{id:guid}}/Steps", async (Guid id, IRecipeStore store, ILyoMapper mapper, Guid? tenantId, CancellationToken ct) => {
                    var rows = await store.GetStepsAsync(id, tenantId, ct).ConfigureAwait(false);
                    return Results.Ok(rows.Select(mapper.Map<RecipeStepRes>).ToList());
                })
            .WithTags(tag)
            .WithName("GetRecipeSteps");

        app.MapPut(
                $"/{recipes}/{{id:guid}}/Steps", async (Guid id, List<RecipeStepReq> req, IRecipeStore store, ILyoMapper mapper, Guid? tenantId, CancellationToken ct) => {
                    ArgumentHelpers.ThrowIfNull(req);
                    var steps = req.Select(r => new RecipeStepRecord {
                            Id = r.Id,
                            RecipeId = id,
                            SortOrder = r.SortOrder,
                            Instruction = r.Instruction,
                            DurationMinutes = r.DurationMinutes,
                            GroupLabel = r.GroupLabel
                        })
                        .ToList();

                    await store.ReplaceStepsAsync(id, steps, tenantId, ct).ConfigureAwait(false);
                    var rows = await store.GetStepsAsync(id, tenantId, ct).ConfigureAwait(false);
                    return Results.Ok(rows.Select(mapper.Map<RecipeStepRes>).ToList());
                })
            .WithTags(tag)
            .WithName("ReplaceRecipeSteps");
    }

    private static void MapUnitAndUsageEndpoints(WebApplication app)
    {
        var tag = Constants.Rest.Recipe.Route;
        app.MapGet(
                $"/{Constants.Rest.Recipe.Units}", async (IRecipeStore store, Guid? tenantId, CancellationToken ct) => {
                    var rows = await store.ListUnitsAsync(tenantId, ct).ConfigureAwait(false);
                    return Results.Ok(rows.Select(ToUnitRes).ToList());
                })
            .WithTags(tag)
            .WithName("ListRecipeUnits");

        app.MapPut(
                $"/{Constants.Rest.Recipe.Units}", async (UnitReq req, IRecipeStore store, Guid? tenantId, CancellationToken ct) => {
                    ArgumentHelpers.ThrowIfNull(req);
                    var unit = new UnitRecord {
                        Id = req.Id,
                        Name = req.Name,
                        Abbreviation = req.Abbreviation,
                        SortOrder = req.SortOrder
                    };

                    await store.SaveUnitAsync(unit, tenantId, ct).ConfigureAwait(false);
                    return Results.Ok(ToUnitRes(unit));
                })
            .WithTags(tag)
            .WithName("SaveRecipeUnit");

        app.MapDelete(
                $"/{Constants.Rest.Recipe.Units}/{{id:guid}}", async (Guid id, IRecipeStore store, Guid? tenantId, CancellationToken ct) => {
                    await store.DeleteUnitAsync(id, tenantId, ct).ConfigureAwait(false);
                    return Results.NoContent();
                })
            .WithTags(tag)
            .WithName("DeleteRecipeUnit");

        app.MapGet(
                $"/{Constants.Rest.Recipe.Ingredients}/{{id:guid}}/Usage", async (Guid id, IRecipeStore store, Guid? tenantId, CancellationToken ct) => {
                    var rows = await store.ListIngredientUsageAsync(id, tenantId, ct).ConfigureAwait(false);
                    return Results.Ok(
                        rows.Select(r => new IngredientUsageRes {
                                RecipeId = r.RecipeId,
                                RecipeTitle = r.RecipeTitle,
                                LineId = r.LineId,
                                StepIds = r.StepIds,
                                StepNumbers = r.StepNumbers
                            })
                            .ToList());
                })
            .WithTags(tag)
            .WithName("ListIngredientUsage");
    }

    private static UnitRes ToUnitRes(UnitRecord unit)
        => new() {
            Id = unit.Id,
            Name = unit.Name,
            Abbreviation = unit.Abbreviation,
            SortOrder = unit.SortOrder
        };

    private static void MapNotesAndTags(WebApplication app, string parentRoute, string entityType)
    {
        var tag = Constants.Rest.Recipe.Route;
        app.MapGet(
                $"/{parentRoute}/{{id:guid}}/Notes",
                async (Guid id, INoteStore notes, Guid? tenantId, CancellationToken ct)
                    => Results.Ok(await notes.GetForEntityAsync(EntityRef.ForGuid(entityType, id), tenantId, ct).ConfigureAwait(false)))
            .WithTags(tag);

        app.MapPost(
                $"/{parentRoute}/{{id:guid}}/Notes", async (Guid id, RecipeNoteCreateReq req, INoteStore notes, Guid? tenantId, CancellationToken ct) => {
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
                $"/{parentRoute}/{{id:guid}}/Notes/{{noteId:guid}}", async (Guid id, Guid noteId, INoteStore notes, Guid? tenantId, CancellationToken ct) => {
                    var existing = await notes.GetByIdAsync(noteId, tenantId, ct).ConfigureAwait(false);
                    if (existing == null || !string.Equals(existing.SubjectEntityType, entityType, StringComparison.Ordinal) ||
                        !string.Equals(existing.SubjectEntityId, id.ToString(), StringComparison.Ordinal))
                        return Results.NotFound();

                    await notes.DeleteAsync(noteId, tenantId, ct).ConfigureAwait(false);
                    return Results.NoContent();
                })
            .WithTags(tag);

        app.MapGet(
                $"/{parentRoute}/{{id:guid}}/Tags",
                async (Guid id, ITagStore tags, Guid? tenantId, CancellationToken ct) => Results.Ok(
                    await tags.GetTagsForEntityAsync(EntityRef.ForGuid(entityType, id), tenantId: tenantId, ct: ct).ConfigureAwait(false)))
            .WithTags(tag);

        app.MapPost(
                $"/{parentRoute}/{{id:guid}}/Tags", async (Guid id, RecipeTagAddReq req, ITagStore tags, Guid? tenantId, CancellationToken ct) => {
                    ArgumentHelpers.ThrowIfNull(req);
                    await tags.AddTagAsync(EntityRef.ForGuid(entityType, id), req.Name, req.TagType, slug: req.Slug, tenantId: tenantId, ct: ct).ConfigureAwait(false);
                    return Results.Ok(await tags.GetTagsForEntityAsync(EntityRef.ForGuid(entityType, id), tenantId: tenantId, ct: ct).ConfigureAwait(false));
                })
            .WithTags(tag);

        app.MapDelete(
                $"/{parentRoute}/{{id:guid}}/Tags/{{name}}", async (Guid id, string name, ITagStore tags, string? tagType, Guid? tenantId, CancellationToken ct) => {
                    await tags.RemoveTagAsync(EntityRef.ForGuid(entityType, id), name, tagType ?? "tag", tenantId: tenantId, ct: ct).ConfigureAwait(false);
                    return Results.NoContent();
                })
            .WithTags(tag);
    }

    private static void MapAttachmentUpload(WebApplication app)
    {
        var tag = Constants.Rest.Recipe.Route;
        app.MapPost(
                $"/{Constants.Rest.Recipe.AttachmentsUpload}", async (
                    HttpContext http, [FromForm] string entityType, [FromForm] Guid entityId, [FromForm] string? role, [FromForm] string? caption, [FromForm] bool isPrimary,
                    IFormFile file, ILyoMapper mapper, Guid? tenantId, CancellationToken ct) => {
                    var helper = http.RequestServices.GetService<IRecipeAttachmentFiles>();
                    if (helper is null)
                        return Results.StatusCode(StatusCodes.Status501NotImplemented);

                    ArgumentHelpers.ThrowIfNull(file);
                    var parsedRole = Enum.TryParse<RecipeAttachmentRole>(role, true, out var parsed) ? parsed : RecipeAttachmentRole.Document;
                    await using var stream = file.OpenReadStream();
                    var row = await helper.SaveAsync(entityType, entityId, stream, file.Length, file.FileName, parsedRole, caption, isPrimary, tenantId, ct).ConfigureAwait(false);
                    return Results.Ok(mapper.Map<RecipeAttachmentRes>(row));
                })
            .DisableAntiforgery()
            .WithTags(tag)
            .WithName("UploadRecipeAttachment");
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