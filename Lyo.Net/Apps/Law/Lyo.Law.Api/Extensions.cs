using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Law.Models;
using Lyo.Law.Models.Request;
using Lyo.Law.Models.Response;
using Lyo.Law.Postgres;
using Lyo.Law.Postgres.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Law.Api;

/// <summary>HTTP mapping for the state statute API. Register store DI with <c>AddLawApiFromConfiguration</c> first. The host still calls <c>AddLyoQueryServices</c>.</summary>
public static class Extensions
{
    private static readonly string[] NodeDeniedSelectFields = ["Body"];

    extension(IServiceCollection services)
    {
        /// <summary>Registers the law Postgres store and CRUD services.</summary>
        public IServiceCollection AddLawApi(Action<PostgresLawOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresLawOptions();
            configure(options);
            return services.AddLawApi(options);
        }

        /// <summary>Registers the law API from configuration section <see cref="PostgresLawOptions.SectionName" />.</summary>
        public IServiceCollection AddLawApiFromConfiguration(IConfiguration configuration, string configSectionName = PostgresLawOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new PostgresLawOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddLawApi(options);
        }

        /// <summary>Registers the law Postgres store and CRUD services.</summary>
        public IServiceCollection AddLawApi(PostgresLawOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresLawStore(options);
            services.AddLyoCrudServices<LawDbContext>();
            return services;
        }
    }

    /// <summary>Maps law query, jurisdiction and code CRUD, and node upsert. Invoke after <c>AddLawApi</c>.</summary>
    public static WebApplication BuildLawGroup(this WebApplication app)
    {
        ArgumentHelpers.ThrowIfNull(app);
        var tag = Constants.Rest.Law.Route;
        app.CreateBuilder<LawDbContext, LawJurisdictionEntity, LawJurisdictionReq, LawJurisdictionRes, Guid>(Constants.Rest.Law.Jurisdictions, tag)
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => PrepareJurisdiction(ctx.Entity) })
            .Build();

        app.CreateBuilder<LawDbContext, LawCodeEntity, LawCodeReq, LawCodeRes, Guid>(Constants.Rest.Law.Codes, tag)
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateV7() : ctx.Entity.Id })
            .Build();

        app.CreateBuilder<LawDbContext, LawNodeEntity, LawNodeEntity, LawNodeEntity, Guid>(Constants.Rest.Law.Nodes, tag)
            .WithCrud(new(ApiFeature.Query), new() { DeniedSelectFields = NodeDeniedSelectFields })
            .Build();

        app.CreateBuilder<LawDbContext, LawAnnotationEntity, LawAnnotationEntity, LawAnnotationEntity, Guid>(Constants.Rest.Law.Annotations, tag)
            .WithCrud(ApiFeatureSet.ReadOnly, new())
            .Build();

        app.CreateBuilder<LawDbContext, LawRevisionEntity, LawRevisionEntity, LawRevisionEntity, Guid>(Constants.Rest.Law.Revisions, tag)
            .WithCrud(ApiFeatureSet.ReadOnly, new())
            .Build();

        MapNodeRoutes(app, tag);
        return app;
    }

    private static void PrepareJurisdiction(LawJurisdictionEntity entity)
    {
        LawJurisdictionKey.ThrowIfFederal(entity.Key);
        entity.Key = entity.Key.Trim();
        entity.Id = entity.Id == default ? LyoGuid.CreateV7() : entity.Id;
    }

    private static void MapNodeRoutes(WebApplication app, string tag)
    {
        app.MapPut(
                "/" + Constants.Rest.Law.NodesBySource, async (LawNodeUpsertReq request, ILawStore store, CancellationToken ct) => {
                    ArgumentHelpers.ThrowIfNull(request);
                    return Results.Ok(await store.UpsertNodeBySourceKeyAsync(request, ct).ConfigureAwait(false));
                })
            .WithTags(tag)
            .WithName("UpsertLawNodeBySource");

        app.MapGet(
                "/" + Constants.Rest.Law.Nodes + "/{id:guid}", async (Guid id, ILawStore store, CancellationToken ct) => {
                    var node = await store.GetNodeByIdAsync(id, ct).ConfigureAwait(false);
                    return node is null ? Results.NotFound() : Results.Ok(node);
                })
            .WithTags(tag)
            .WithName("GetLawNode");

        app.MapGet(
                "/" + Constants.Rest.Law.Nodes + "/by-citation", async (string jurisdictionKey, string codeKey, string citation, ILawStore store, CancellationToken ct) => {
                    var node = await store.GetNodeByCitationAsync(jurisdictionKey, codeKey, citation, ct).ConfigureAwait(false);
                    return node is null ? Results.NotFound() : Results.Ok(node);
                })
            .WithTags(tag)
            .WithName("GetLawNodeByCitation");

        app.MapGet(
                "/" + Constants.Rest.Law.Nodes + "/{id:guid}/children",
                async (Guid id, ILawStore store, CancellationToken ct) => Results.Ok(await store.GetChildrenAsync(id, ct).ConfigureAwait(false)))
            .WithTags(tag)
            .WithName("GetLawNodeChildren");

        app.MapGet(
                "/" + Constants.Rest.Law.Nodes + "/{id:guid}/ancestors",
                async (Guid id, ILawStore store, CancellationToken ct) => Results.Ok(await store.GetAncestorsAsync(id, ct).ConfigureAwait(false)))
            .WithTags(tag)
            .WithName("GetLawNodeAncestors");
    }
}