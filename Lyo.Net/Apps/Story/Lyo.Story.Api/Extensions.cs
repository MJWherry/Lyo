using Lyo.Api.ApiEndpoint;
using Lyo.Api.ApiEndpoint.Config;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Story.Models;
using Lyo.Story.Models.Request;
using Lyo.Story.Models.Response;
using Lyo.Story.Postgres;
using Lyo.Story.Postgres.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Story.Api;

/// <summary>HTTP mapping for the Lyo Story API. Register store DI with <c>AddStoryApiFromConfiguration</c> first. Row CRUD uses Req/Res. List and filter use Query.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the Story Postgres store, which also registers CRUD services and <c>StoryLyoMapper</c>.</summary>
        public IServiceCollection AddStoryApi(Action<PostgresStoryOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresStoryOptions();
            configure(options);
            return services.AddStoryApi(options);
        }

        /// <summary>Registers Story from configuration section <see cref="PostgresStoryOptions.SectionName" />.</summary>
        public IServiceCollection AddStoryApiFromConfiguration(IConfiguration configuration, string configSectionName = PostgresStoryOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddPostgresStoryStoreFromConfiguration(configuration, configSectionName);
            return services;
        }

        /// <summary>Registers the Story Postgres store, which also registers CRUD services and <c>StoryLyoMapper</c>.</summary>
        public IServiceCollection AddStoryApi(PostgresStoryOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresStoryStore(options);
            return services;
        }
    }

    /// <summary>
    /// Maps Story Req/Res CRUD, graph save and load, and playthrough routes. Invoke after <c>AddStoryApi</c>. The graph and playthrough routes require an authenticated caller
    /// and use <paramref name="routeOptions" /> for ownership and tenant binding. Pass <paramref name="allowAnonymous" /> only on a trusted local workbench.
    /// </summary>
    public static WebApplication BuildStoryGroup(this WebApplication app, StoryRouteOptions? routeOptions = null, bool allowAnonymous = false)
    {
        var tag = Constants.Rest.Story.Route;
        app.CreateBuilder<StoryDbContext, StoryEntity, StoryReq, StoryRes, Guid>(Constants.Rest.Story.Stories, tag)
            .WithCrud(
                ApiFeatureSet.DefaultCrud,
                new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id, BeforeDeleteAsync = ClearStartNodeAsync })
            .Build();

        app.CreateBuilder<StoryDbContext, StorySceneEntity, StorySceneReq, StorySceneRes, Guid>(Constants.Rest.Story.Scenes, tag)
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id })
            .Build();

        app.CreateBuilder<StoryDbContext, StoryNodeEntity, StoryNodeReq, StoryNodeRes, Guid>(Constants.Rest.Story.Nodes, tag)
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id })
            .Build();

        app.CreateBuilder<StoryDbContext, StoryVariableEntity, StoryVariableReq, StoryVariableRes, Guid>(Constants.Rest.Story.Variables, tag)
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id })
            .Build();

        app.CreateBuilder<StoryDbContext, StoryActionEntity, StoryActionReq, StoryActionRes, Guid>(Constants.Rest.Story.Actions, tag)
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id })
            .Build();

        app.CreateBuilder<StoryDbContext, StoryChoiceEntity, StoryChoiceReq, StoryChoiceRes, Guid>(Constants.Rest.Story.Choices, tag)
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id })
            .Build();

        app.CreateBuilder<StoryDbContext, StoryReactionEntity, StoryReactionReq, StoryReactionRes, Guid>(Constants.Rest.Story.Reactions, tag)
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id })
            .Build();

        app.CreateBuilder<StoryDbContext, StoryEffectEntity, StoryEffectReq, StoryEffectRes, Guid>(Constants.Rest.Story.Effects, tag)
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id })
            .Build();

        app.CreateReadOnlyBuilder<StoryDbContext, StoryPlaythroughEntity, StoryPlaythroughRes, Guid>(Constants.Rest.Story.Playthroughs, tag)
            .WithCrud(ApiFeatureSet.ReadOnly, new())
            .Build();

        var group = app.MapGroup(string.Empty);
        if (allowAnonymous)
            group.AllowAnonymous();
        else
            group.RequireAuthorization();

        group.MapStoryRoutes(routeOptions);
        return app;
    }

    private static Task ClearStartNodeAsync(DeleteContext<StoryEntity, StoryDbContext> ctx, CancellationToken ct)
    {
        ctx.Entity.StartNodeId = null;
        return ctx.DbContext.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Maps the graph (save, get, by slug, list) and playthrough routes over any registered <see cref="IStoryStore" /> (Postgres, SQLite, or a client store), without the Req/Res
    /// CRUD routes and without group authorization: apply the host's policy to <paramref name="routes" />. <see cref="StoryRouteOptions" /> sets the author policy for graph save, list,
    /// and variable writes, and binds the player and tenant from the caller so players only reach their own playthroughs and their tenant's stories. Errors are problem details.
    /// </summary>
    public static IEndpointRouteBuilder MapStoryRoutes(this IEndpointRouteBuilder routes, StoryRouteOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(routes);
        options ??= new();
        var tag = Constants.Rest.Story.Route;
        routes.MapPost(
                "/" + Constants.Rest.Story.Graph, async (StoryGraphReq req, HttpContext http, IStoryStore store, CancellationToken ct) => {
                    ArgumentHelpers.ThrowIfNull(req);
                    if (options.TenantId != null && req.TenantId != options.TenantId(http))
                        return Problem(StatusCodes.Status403Forbidden, "The story belongs to another tenant.");

                    var story = StoryGraphMapping.ToDomain(req);
                    try {
                        await store.SaveStoryGraphAsync(story, ct).ConfigureAwait(false);
                        return Results.Ok(StoryGraphMapping.ToRes(story));
                    }
                    catch (StoryException ex) {
                        return Problem(StatusCodes.Status400BadRequest, ex.Message);
                    }
                })
            .WithTags(tag)
            .WithName("SaveStoryGraph")
            .RequireAuthorizationWhen(options.AuthorPolicy);

        routes.MapGet(
                "/" + Constants.Rest.Story.Graph + "/{id:guid}", async (Guid id, HttpContext http, IStoryStore store, CancellationToken ct) => {
                    var story = await store.GetStoryByIdAsync(id, ct).ConfigureAwait(false);
                    return story is null || !Visible(story, http, options) ? Results.NotFound() : Results.Ok(StoryGraphMapping.ToRes(story));
                })
            .WithTags(tag)
            .WithName("GetStoryGraph");

        routes.MapGet(
                "/" + Constants.Rest.Story.Graph + "/by-slug/{slug}", async (string slug, HttpContext http, IStoryStore store, CancellationToken ct) => {
                    var story = await store.GetStoryBySlugAsync(slug, ct).ConfigureAwait(false);
                    return story is null || !Visible(story, http, options) ? Results.NotFound() : Results.Ok(StoryGraphMapping.ToRes(story));
                })
            .WithTags(tag)
            .WithName("GetStoryGraphBySlug");

        routes.MapPost(
                "/" + Constants.Rest.Story.GraphList, async (StoryListReq req, HttpContext http, IStoryStore store, CancellationToken ct) => {
                    ArgumentHelpers.ThrowIfNull(req);
                    var tenant = options.TenantId != null ? options.TenantId(http) : req.TenantId;
                    var stories = await store.ListStoriesAsync(tenant, req.GameKey, ct).ConfigureAwait(false);
                    return Results.Ok(stories.Select(StoryGraphMapping.ToRes).ToArray());
                })
            .WithTags(tag)
            .WithName("ListStoryGraphs")
            .RequireAuthorizationWhen(options.AuthorPolicy);

        routes.MapPost(
                "/" + Constants.Rest.Story.PlayStart, async (StoryStartPlaythroughReq req, HttpContext http, IStoryStore store, CancellationToken ct) => {
                    ArgumentHelpers.ThrowIfNull(req);
                    var playerRef = req.PlayerRef;
                    if (options.PlayerRef != null && (playerRef = options.PlayerRef(http)) == null)
                        return Problem(StatusCodes.Status401Unauthorized, "The caller has no player identity.");

                    if (options.TenantId != null && await store.GetStoryByIdAsync(req.StoryId, ct).ConfigureAwait(false) is var story &&
                        (story is null || !Visible(story, http, options)))
                        return Results.NotFound();

                    try {
                        var state = await store.StartPlaythroughAsync(req.StoryId, playerRef, ct).ConfigureAwait(false);
                        return Results.Ok(StoryGraphMapping.ToRes(state));
                    }
                    catch (StoryException ex) {
                        return Problem(StatusCodes.Status400BadRequest, ex.Message);
                    }
                })
            .WithTags(tag)
            .WithName("StartStoryPlaythrough");

        routes.MapGet(
                "/Story/playthrough/{id:guid}", async (Guid id, HttpContext http, IStoryStore store, CancellationToken ct) => {
                    var state = await store.GetPlaythroughAsync(id, ct).ConfigureAwait(false);
                    if (state is null)
                        return Results.NotFound();

                    return Owns(state, http, options) ? Results.Ok(StoryGraphMapping.ToRes(state)) : NotYours();
                })
            .WithTags(tag)
            .WithName("GetStoryPlayState");

        routes.MapGet(
                "/Story/playthrough/{id:guid}/moves", async (Guid id, HttpContext http, IStoryStore store, CancellationToken ct) => {
                    var state = await store.GetPlaythroughAsync(id, ct).ConfigureAwait(false);
                    if (state is null)
                        return Results.NotFound();

                    return Owns(state, http, options)
                        ? Results.Ok(
                            state.Moves.Select(m => new StoryMoveRes {
                                    Kind = m.Kind,
                                    ChoiceKey = m.ChoiceKey,
                                    Prompt = m.Prompt,
                                    Enabled = m.Enabled
                                })
                                .ToArray())
                        : NotYours();
                })
            .WithTags(tag)
            .WithName("GetStoryPlaythroughMoves");

        routes.MapPost(
                "/Story/playthrough/{id:guid}/choose", async (Guid id, StoryChooseReq req, HttpContext http, IStoryStore store, CancellationToken ct) => {
                    ArgumentHelpers.ThrowIfNull(req);
                    return await DenyAsync(id, http, store, options, ct).ConfigureAwait(false) ??
                        await PlayAsync(() => store.TakeChoiceAsync(id, req.ChoiceKey, ct)).ConfigureAwait(false);
                })
            .WithTags(tag)
            .WithName("TakeStoryChoice");

        routes.MapPost(
                "/Story/playthrough/{id:guid}/advance",
                async (Guid id, HttpContext http, IStoryStore store, CancellationToken ct) => await DenyAsync(id, http, store, options, ct).ConfigureAwait(false) ??
                    await PlayAsync(() => store.AdvanceAsync(id, ct)).ConfigureAwait(false))
            .WithTags(tag)
            .WithName("AdvanceStoryPlaythrough");

        routes.MapPost(
                "/Story/playthrough/{id:guid}/variables", async (Guid id, StorySetVariablesReq req, IStoryStore store, CancellationToken ct) => {
                    ArgumentHelpers.ThrowIfNull(req);
                    return await PlayAsync(() => store.SetVariablesAsync(id, req.Values, ct)).ConfigureAwait(false);
                })
            .WithTags(tag)
            .WithName("SetStoryPlaythroughVariables")
            .RequireAuthorizationWhen(options.AuthorPolicy);

        routes.MapGet(
                "/Story/playthrough/{id:guid}/events",
                async (Guid id, HttpContext http, IStoryStore store, CancellationToken ct) => await DenyAsync(id, http, store, options, ct).ConfigureAwait(false) ??
                    Results.Ok((await store.GetEventsAsync(id, ct).ConfigureAwait(false)).Select(StoryGraphMapping.ToRes).ToArray()))
            .WithTags(tag)
            .WithName("GetStoryPlaythroughEvents");

        routes.MapGet(
                "/Story/playthrough/{id:guid}/causes/{variableKey}",
                async (Guid id, string variableKey, HttpContext http, IStoryStore store, CancellationToken ct)
                    => await DenyAsync(id, http, store, options, ct).ConfigureAwait(false) ??
                    Results.Ok((await store.GetCausesAsync(id, variableKey, ct).ConfigureAwait(false)).Select(StoryGraphMapping.ToRes).ToArray()))
            .WithTags(tag)
            .WithName("GetStoryPlaythroughCauses");

        return routes;
    }

    private static bool Visible(Story story, HttpContext http, StoryRouteOptions options) => options.TenantId == null || story.TenantId == options.TenantId(http);

    private static bool Owns(StoryPlayState state, HttpContext http, StoryRouteOptions options)
        => options.PlayerRef == null || string.Equals(state.Playthrough.PlayerRef, options.PlayerRef(http), StringComparison.Ordinal);

    private static async Task<IResult?> DenyAsync(Guid id, HttpContext http, IStoryStore store, StoryRouteOptions options, CancellationToken ct)
    {
        if (options.PlayerRef == null)
            return null;

        var state = await store.GetPlaythroughAsync(id, ct).ConfigureAwait(false);
        if (state is null)
            return Results.NotFound();

        return Owns(state, http, options) ? null : NotYours();
    }

    private static async Task<IResult> PlayAsync(Func<Task<StoryPlayState>> play)
    {
        try {
            return Results.Ok(StoryGraphMapping.ToRes(await play().ConfigureAwait(false)));
        }
        catch (StoryException ex) {
            return Problem(StatusCodes.Status400BadRequest, ex.Message);
        }
    }

    private static IResult NotYours() => Problem(StatusCodes.Status403Forbidden, "The playthrough belongs to another player.");

    private static IResult Problem(int status, string detail) => Results.Problem(detail, statusCode: status);

    private static RouteHandlerBuilder RequireAuthorizationWhen(this RouteHandlerBuilder builder, string? policy)
        => policy == null ? builder : builder.RequireAuthorization(policy);
}