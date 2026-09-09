using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.Models;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Enums;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Crud.Create;
using Lyo.Api.Services.Crud.Delete;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Services.Crud.Read.Query.Root;
using Lyo.Api.Services.Crud.Update;
using Lyo.Api.Services.Export;
using Lyo.Common.Json;
using Lyo.Common.Core.Conversion;
using Lyo.Common.Core.Enums;
using Lyo.Exceptions;
using Lyo.Query.Models.Common.Request;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace Lyo.Api.ApiEndpoint.Dynamic;

/// <summary>Maps one set of CRUD endpoints with an {entityType} route parameter. Replaces per-entity endpoint registration.</summary>
public static class DynamicCrudEndpointBuilder
{
    /// <summary>Maps dynamic CRUD endpoints via the fluent config builder. Configure defaults and per-entity overrides.</summary>
    /// <example>
    /// <code>
    /// app.MapDynamicCrudEndpoints&lt;PeopleDbContext&gt;(c => c
    ///     .WithDefaults(d => { d.BaseRoute = "Person"; d.Features = ApiFeatureSet.CoreAll; })
    ///     .For&lt;PersonEntity&gt;(e => e.ExcludeCreate().ForPatch(p => p.Before((ctx, entity) => entity.ModifiedAt = DateTime.UtcNow)))
    /// );
    /// </code>
    /// </example>
    public static WebApplication MapDynamicCrudEndpoints<TContext>(this WebApplication webApp, Action<DynamicEndpointConfigBuilder<TContext>> configure)
        where TContext : DbContext
    {
        var builder = new DynamicEndpointConfigBuilder<TContext>();
        configure(builder);
        return MapDynamicCrudEndpointsCore(webApp, builder.Build());
    }

    /// <summary>Maps dynamic CRUD endpoints: /{baseRoute}/{entityType}/QueryConcrete, /{entityType}/Get/{id}, and so on. Simpler overload using DynamicEndpointOptions.</summary>
    public static WebApplication MapDynamicCrudEndpoints<TContext>(this WebApplication webApp, Action<DynamicEndpointOptions<TContext>>? configure = null)
        where TContext : DbContext
    {
        var options = new DynamicEndpointOptions<TContext>();
        configure?.Invoke(options);
        var config = ConvertDynamicOptionsToConfig(options);
        return MapDynamicCrudEndpointsCore(webApp, config);
    }

    /// <summary>
    /// Maps root From/Joins <c>POST {baseRoute}/Query</c> for a DbContext without requiring full dynamic CRUD. Use when typed <c>CreateBuilder</c> already owns entity routes
    /// (for example Person) but you still want Option A root Query. Pass <paramref name="baseRoute" /> empty for <c>POST /Query</c>.
    /// </summary>
    /// <param name="webApp">Web application to map endpoints on.</param>
    /// <param name="baseRoute">Route prefix before <c>/Query</c>. Empty for <c>POST /Query</c>.</param>
    /// <param name="allowlistedEntityTypes">Optional entity types to expose. Defaults to all DbSet entity types on <typeparamref name="TContext" />.</param>
    /// <param name="configure">Optional endpoint conventions (for example <c>b => b.RequireAuthorization()</c>).</param>
    /// <param name="deniedSelectFields">Optional field names rejected in select, filter, sort, and join paths. Root query reads raw entities and bypasses response mapping.</param>
    /// <param name="auth">Authorization for the route. Required unless <paramref name="configure" /> is supplied and applies its own. Root query reaches every allowlisted entity.</param>
    public static WebApplication MapRootQueryEndpoints<TContext>(
        this WebApplication webApp,
        string baseRoute = "",
        IEnumerable<Type>? allowlistedEntityTypes = null,
        Action<RouteHandlerBuilder>? configure = null,
        IReadOnlyCollection<string>? deniedSelectFields = null,
        EndpointAuth? auth = null)
        where TContext : DbContext
    {
        OperationHelpers.ThrowIf(
            configure is null && auth is null,
            $"Root /Query for '{typeof(TContext).Name}' has no authorization configured. It reads every allowlisted entity type, so pass auth: " +
            "EndpointAuth.RequireAuthorization(), auth: EndpointAuth.Anonymous(), or a configure callback that applies your own convention.");

        using var scope = webApp.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TContext>>();
        using var context = factory.CreateDbContext();
        var types = allowlistedEntityTypes?.ToList() ?? DynamicEndpointMapper.GetEntityTypesFromDbContext<TContext>().ToList();
        var rootQueryRegistry = RootQueryEntityRegistry.FromDbContext(context, types);
        var routePrefix = string.IsNullOrEmpty(baseRoute) ? "" : baseRoute.TrimEnd('/') + "/";
        var endpoint = webApp.MapPost(
                $"{routePrefix}Query", async ([FromBody] QueryReq queryRequest, [FromServices] IRootQueryService<TContext> rootQueryService, HttpContext httpContext, CancellationToken ct) => {
                    var deniedErrors = DeniedSelectFieldPolicy.ValidateRootQuery(queryRequest, deniedSelectFields);
                    if (deniedErrors.Count > 0) {
                        var problem = LyoProblemDetailsBuilder.CreateWithActivity()
                            .WithErrorCode(Constants.ApiErrorCodes.InvalidQuery)
                            .WithMessage("Invalid query.")
                            .AddErrors(deniedErrors)
                            .Build();

                        return ApiErrorResponseFactory.ThrowForError(httpContext, problem);
                    }

                    var result = await rootQueryService.QueryAsync(queryRequest, rootQueryRegistry, ct).ConfigureAwait(false);
                    if (result.IsSuccess)
                        return Results.Json(result);

                    return ApiErrorResponseFactory.ThrowForError(httpContext, result.Error);
                })
            .WithTags("Dynamic")
            .WithName($"RootQuery{typeof(TContext).Name}{(string.IsNullOrEmpty(baseRoute) ? "" : "_" + baseRoute.Replace('/', '_'))}")
            .Produces<ProjectedQueryRes<object?>>()
            .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest);

        EndpointAuthorizationApplier.Apply(endpoint, auth);
        configure?.Invoke(endpoint);
        return webApp;
    }

    private static DynamicEndpointConfig<TContext> ConvertDynamicOptionsToConfig<TContext>(DynamicEndpointOptions<TContext> options)
        where TContext : DbContext
    {
        var defaults = new DynamicEndpointDefaults { Features = options.Features, BaseRoute = options.BaseRoute, Auth = options.Auth, QueryPolicy = options.QueryPolicy };
        defaults.ExcludedTypes.UnionWith(options.ExcludedTypes);
        defaults.IncludedTypes.AddRange(options.IncludedTypes);
        defaults.DeniedSelectFields.AddRange(options.DeniedSelectFields);
        return new(defaults, new Dictionary<Type, EntityEndpointConfig<TContext>>());
    }

    private static WebApplication MapDynamicCrudEndpointsCore<TContext>(WebApplication webApp, DynamicEndpointConfig<TContext> config)
        where TContext : DbContext
    {
        var defaults = config.Defaults;
        var auth = defaults.Auth;
        OperationHelpers.ThrowIf(
            auth is null,
            $"Dynamic CRUD endpoints for '{typeof(TContext).Name}' have no authorization configured. These routes include delete, upsert, and metadata routes that reflect " +
            "every public property of every registered entity, so the default is not anonymous. Call RequireAuthorization(), Auth(...), or AllowAnonymous() on the config builder.");

        ThrowOnPerEntityAuth(config);
        ThrowOnUnsupportedHooks(config);
        var allEntityTypes = DynamicEndpointMapper.GetEntityTypesFromDbContext<TContext>();
        var entityTypes = defaults.IncludedTypes.Count > 0 ? defaults.IncludedTypes : allEntityTypes;
        using var scope = webApp.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TContext>>();
        using var context = factory.CreateDbContext();
        var registry = new Dictionary<string, EntityEndpointMetadata>(StringComparer.OrdinalIgnoreCase);
        var entities = new List<EntityEndpointMetadata>();
        var shortNameOwners = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        var ambiguousShortNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cache = BuildMethodCache<TContext>();
        foreach (var entityType in entityTypes) {
            if (defaults.ExcludedTypes.Contains(entityType))
                continue;

            var pkInfo = GetPrimaryKeyInfo(context, entityType);
            if (pkInfo == null)
                continue;

            var entityConfig = config.GetConfig(entityType);
            var keyType = pkInfo.Value.ClrType;
            var keyName = pkInfo.Value.Name;
            var defaultOrder = DynamicEndpointMapper.BuildDefaultOrderExpression(entityType, keyName);
            var beforeCreate = entityConfig.CreateConfig?.Before ?? WrapDefaultBeforeCreate<TContext>(defaults.BeforeCreate);
            var entityCache = BuildEntityMethodCache(cache, entityType, keyName, beforeCreate);
            var patchCfg = entityConfig.PatchConfig;
            var (adaptedPatchBefore, adaptedPatchAfter) = AdaptPatchDelegates(entityType, patchCfg);
            var entityMetadata = new EntityEndpointMetadata(
                entityType, keyType, keyName, defaultOrder, entityCache, patchCfg?.PropertyAuthorization, adaptedPatchBefore, adaptedPatchAfter);

            entities.Add(entityMetadata);

            // The full name is always addressable. The short name is the friendly route segment, but only while it identifies exactly one entity. Two entities called Widget in
            // different namespaces used to overwrite each other here, so requests for one silently hit the other.
            registry[entityType.FullName ?? entityType.Name] = entityMetadata;
            if (shortNameOwners.TryGetValue(entityType.Name, out var owner) && owner != entityType) {
                ambiguousShortNames.Add(entityType.Name);
                registry.Remove(entityType.Name);
            }
            else {
                shortNameOwners[entityType.Name] = entityType;
                registry[entityType.Name] = entityMetadata;
            }
        }

        if (ambiguousShortNames.Count > 0) {
            webApp.Services.GetService<ILoggerFactory>()
                ?.CreateLogger(typeof(DynamicCrudEndpointBuilder))
                .LogWarning(
                    "Dynamic CRUD entity names {AmbiguousNames} are shared by more than one CLR type; those entities are reachable only by their namespace-qualified full name.",
                    string.Join(", ", ambiguousShortNames.OrderBy(n => n, StringComparer.Ordinal)));
        }

        var baseRoute = defaults.BaseRoute.TrimEnd('/');
        var routePrefix = string.IsNullOrEmpty(baseRoute) ? "" : baseRoute + "/";
        var entityRoute = $"{routePrefix}{{entityType}}";
        var metadataRoute = $"{routePrefix}Metadata";
        var jsonOptions = webApp.Services.GetService<IOptions<JsonOptions>>()?.Value.SerializerOptions ?? LyoJsonSerializerOptions.Create();
        var metadata = BuildMetadata(entities);
        Authorize(webApp.MapGet(metadataRoute, () => Results.Json(metadata)).WithTags("Dynamic").Produces<CrudMetadataResponse>());
        Authorize(
            webApp.MapGet($"{entityRoute}/Metadata", ([FromRoute] string entityType, HttpContext httpContext) => HandleGetEntityMetadata(registry, entityType, httpContext))
                .WithTags("Dynamic")
                .Produces<EntityTypeMetadata>()
                .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound));

        var dynamicContext = new DynamicApiEndpointContributorContext<TContext>(webApp, registry, config);
        foreach (var contributor in webApp.Services.GetServices<IApiEndpointContributor>()) {
            if (defaults.Features.Contains(contributor.Feature))
                contributor.RegisterDynamicRoutes(dynamicContext);
        }

        var deniedFields = defaults.DeniedSelectFields;
        var queryPolicy = defaults.QueryPolicy with { DeniedSelectFields = deniedFields };
        if (defaults.Features.Contains(ApiFeature.Query)) {
            Authorize(
                webApp.MapPost(
                        $"{entityRoute}/QueryConcrete",
                        async (
                            [FromRoute] string entityType, [FromBody] QueryConcreteReq queryRequest, [FromServices] IQueryService<TContext> queryService, HttpContext httpContext,
                            CancellationToken ct) => {
                            var policyErrors = QueryPolicyValidator.Validate(queryRequest, queryPolicy);
                            return policyErrors.Count > 0
                                ? RejectInvalidQuery(httpContext, policyErrors)
                                : await HandleQuery(registry, entityType, queryRequest, queryService, httpContext, SortDirection.Desc, ct);
                        })
                    .WithTags("Dynamic")
                    .Produces<QueryRes<object>>()
                    .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                    .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound));

            var enableComputedFields = defaults.Features.Contains(ApiFeature.ProjectionComputedFields);
            Authorize(
                webApp.MapPost(
                        $"{entityRoute}/QueryProject", async (
                            [FromRoute] string entityType, [FromBody] ProjectionQueryReq queryRequest, [FromServices] IQueryService<TContext> queryService,
                            HttpContext httpContext, CancellationToken ct) => {
                            if (!enableComputedFields && queryRequest.ComputedFields.Count > 0) {
                                return ApiErrorResponseFactory.ThrowForError(
                                    httpContext,
                                    LyoProblemDetails.FromCode(
                                        Constants.ApiErrorCodes.InvalidComputedField, "Computed fields are not enabled. Enable via ApiFeature.ProjectionComputedFields.",
                                        DateTime.UtcNow));
                            }

                            var policyErrors = QueryPolicyValidator.Validate(queryRequest, queryPolicy);
                            return policyErrors.Count > 0
                                ? RejectInvalidQuery(httpContext, policyErrors)
                                : await HandleQueryProjected(registry, entityType, queryRequest, queryService, httpContext, SortDirection.Desc, ct);
                        })
                    .WithTags("Dynamic")
                    .Produces<ProjectedQueryRes<object?>>()
                    .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                    .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound));

            // Option A: root /Query at the dynamic base, not under {entityType}.
            webApp.MapRootQueryEndpoints<TContext>(baseRoute, entities.Select(m => m.EntityType), deniedSelectFields: deniedFields, auth: auth);
        }

        if (defaults.Features.Contains(ApiFeature.Get)) {
            Authorize(
                webApp.MapGet(
                        $"{entityRoute}/{{id}}",
                        async (
                            [FromRoute] string entityType, [FromRoute] string id, [FromQuery] string[] include, [FromServices] IQueryService<TContext> queryService,
                            HttpContext httpContext, CancellationToken ct) => await HandleGet(registry, entityType, id, include, queryService, httpContext, ct))
                    .WithTags("Dynamic")
                    .Produces<object>()
                    .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound));
        }

        if (defaults.Features.Contains(ApiFeature.Create)) {
            Authorize(
                webApp.MapPost(
                        $"{entityRoute}",
                        async (
                                [FromRoute] string entityType, HttpRequest request, [FromServices] ICreateService<TContext> createService, HttpContext httpContext,
                                CancellationToken ct)
                            => await HandleCreate(registry, entityType, request, createService, httpContext, jsonOptions, ct))
                    .WithTags("Dynamic")
                    .Produces(StatusCodes.Status201Created)
                    .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                    .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound));

            if (defaults.Features.Contains(ApiFeature.CreateBulk)) {
                Authorize(
                    webApp.MapPost(
                            $"{entityRoute}/Bulk",
                            async (
                                    [FromRoute] string entityType, HttpRequest request, [FromServices] ICreateService<TContext> createService, HttpContext httpContext,
                                    CancellationToken ct)
                                => await HandleCreateBulk(registry, entityType, request, createService, httpContext, jsonOptions, ct))
                        .WithTags("Dynamic")
                        .Produces<CreateBulkResult<object>>()
                        .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                        .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound));
            }
        }

        if (defaults.Features.Contains(ApiFeature.Patch)) {
            Authorize(
                webApp.MapPatch(
                        $"{entityRoute}",
                        async (
                            [FromRoute] string entityType, [FromBody] PatchRequest patchRequest, [FromServices] IPatchService<TContext> patchService, HttpContext httpContext,
                            CancellationToken ct) => await HandlePatch(registry, entityType, patchRequest, patchService, httpContext, ct))
                    .WithTags("Dynamic")
                    .Produces<PatchResult<object>>()
                    .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                    .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound));

            if (defaults.Features.Contains(ApiFeature.PatchBulk)) {
                Authorize(
                    webApp.MapPatch(
                            $"{entityRoute}/Bulk",
                            async (
                                [FromRoute] string entityType, [FromBody] List<PatchRequest> requests, [FromServices] IPatchService<TContext> patchService,
                                HttpContext httpContext, CancellationToken ct) => await HandlePatchBulk(registry, entityType, requests, patchService, httpContext, ct))
                        .WithTags("Dynamic")
                        .Produces<PatchBulkResult<object>>()
                        .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                        .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound));
            }
        }

        if (defaults.Features.Contains(ApiFeature.Update)) {
            Authorize(
                webApp.MapPost(
                        $"{entityRoute}/Update",
                        async (
                                [FromRoute] string entityType, [FromBody] JsonNode? body, [FromServices] IUpdateService<TContext> updateService,
                                [FromServices] IQueryService<TContext> queryService, HttpContext httpContext, CancellationToken ct)
                            => await HandleUpdate(registry, entityType, body, updateService, queryService, httpContext, jsonOptions, ct))
                    .WithTags("Dynamic")
                    .Produces<UpdateResult<object>>()
                    .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                    .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound));

            if (defaults.Features.Contains(ApiFeature.UpdateBulk)) {
                Authorize(
                    webApp.MapPost(
                            $"{entityRoute}/Bulk/Update",
                            async (
                                [FromRoute] string entityType, [FromBody] JsonNode? body, [FromServices] IUpdateService<TContext> updateService,
                                [FromServices] IQueryService<TContext> queryService, HttpContext httpContext, CancellationToken ct)
                                => await HandleUpdateBulk(registry, entityType, body, updateService, queryService, httpContext, jsonOptions, ct))
                        .WithTags("Dynamic")
                        .Produces<UpdateBulkResult<object>>()
                        .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                        .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound));
            }
        }

        if (defaults.Features.Contains(ApiFeature.Upsert)) {
            Authorize(
                webApp.MapPost(
                        $"{entityRoute}/Upsert",
                        async (
                                [FromRoute] string entityType, [FromBody] JsonNode? body, [FromServices] IUpsertService<TContext> upsertService,
                                [FromServices] IQueryService<TContext> queryService, HttpContext httpContext, CancellationToken ct)
                            => await HandleUpsert(registry, entityType, body, upsertService, queryService, httpContext, jsonOptions, ct))
                    .WithTags("Dynamic")
                    .Produces<UpsertResult<object>>()
                    .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                    .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound));

            if (defaults.Features.Contains(ApiFeature.UpsertBulk)) {
                Authorize(
                    webApp.MapPost(
                            $"{entityRoute}/Bulk/Upsert",
                            async (
                                [FromRoute] string entityType, [FromBody] JsonNode? body, [FromServices] IUpsertService<TContext> upsertService,
                                [FromServices] IQueryService<TContext> queryService, HttpContext httpContext, CancellationToken ct)
                                => await HandleUpsertBulk(registry, entityType, body, upsertService, queryService, httpContext, jsonOptions, ct))
                        .WithTags("Dynamic")
                        .Produces<UpsertBulkResult<object>>()
                        .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                        .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound));
            }
        }

        if (defaults.Features.Contains(ApiFeature.Delete)) {
            Authorize(
                webApp.MapDelete(
                        $"{entityRoute}",
                        async (
                            [FromRoute] string entityType, [FromBody] DeleteRequest deleteRequest, [FromServices] IDeleteService<TContext> deleteService, HttpContext httpContext,
                            CancellationToken ct) => await HandleDeleteByRequest(registry, entityType, deleteRequest, deleteService, httpContext, ct))
                    .WithTags("Dynamic")
                    .Produces<DeleteResult<object>>()
                    .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                    .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound));

            Authorize(
                webApp.MapDelete(
                        $"{entityRoute}/{{id}}",
                        async (
                                [FromRoute] string entityType, [FromRoute] string id, [FromServices] IDeleteService<TContext> deleteService, HttpContext httpContext,
                                CancellationToken ct)
                            => await HandleDelete(registry, entityType, id, deleteService, httpContext, ct))
                    .WithTags("Dynamic")
                    .Produces<DeleteResult<object>>()
                    .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound));

            if (defaults.Features.Contains(ApiFeature.DeleteBulk)) {
                Authorize(
                    webApp.MapDelete(
                            $"{entityRoute}/Bulk",
                            async (
                                [FromRoute] string entityType, [FromBody] List<DeleteRequest> requests, [FromServices] IDeleteService<TContext> deleteService,
                                HttpContext httpContext, CancellationToken ct) => await HandleDeleteBulk(registry, entityType, requests, deleteService, httpContext, ct))
                        .WithTags("Dynamic")
                        .Produces<DeleteBulkResult<object>>()
                        .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                        .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound));
            }
        }

        return webApp;

        void Authorize(RouteHandlerBuilder route) => EndpointAuthorizationApplier.Apply(route, auth);
    }

    /// <summary>
    /// Rejects per-entity hooks that the dynamic builder does not invoke.
    /// </summary>
    /// <remarks>
    /// Only the create <c>Before</c> hook, the patch <c>Before</c>/<c>After</c> hooks, and property authorization survive the reflection dispatch. Everything else was accepted at
    /// configuration time and then passed to the services as <c>null</c>. Map the entity with the typed builder when it needs those hooks.
    /// </remarks>
    private static void ThrowOnUnsupportedHooks<TContext>(DynamicEndpointConfig<TContext> config)
        where TContext : DbContext
    {
        foreach (var (entityType, entityConfig) in config.EntityConfigs) {
            List<string> ignored = [];
            if (entityConfig.CreateConfig is { } create) {
                AddIf(ignored, create.After is not null, "Create.After");
                AddIf(ignored, create.AfterAsync is not null, "Create.AfterAsync");
            }

            if (entityConfig.UpdateConfig is { } update) {
                AddIf(ignored, update.Before is not null, "Update.Before");
                AddIf(ignored, update.After is not null, "Update.After");
            }

            if (entityConfig.DeleteConfig is { } delete) {
                AddIf(ignored, delete.Before is not null, "Delete.Before");
                AddIf(ignored, delete.BeforeAsync is not null, "Delete.BeforeAsync");
                AddIf(ignored, delete.After is not null, "Delete.After");
                AddIf(ignored, delete.Includes is { Length: > 0 }, "Delete.Includes");
            }

            if (entityConfig.UpsertConfig is { } upsert) {
                AddIf(ignored, upsert.Before is not null, "Upsert.Before");
                AddIf(ignored, upsert.BeforeCreate is not null, "Upsert.BeforeCreate");
                AddIf(ignored, upsert.BeforeUpdate is not null, "Upsert.BeforeUpdate");
                AddIf(ignored, upsert.After is not null, "Upsert.After");
                AddIf(ignored, upsert.AfterCreate is not null, "Upsert.AfterCreate");
                AddIf(ignored, upsert.AfterUpdate is not null, "Upsert.AfterUpdate");
            }

            OperationHelpers.ThrowIf(
                ignored.Count > 0,
                $"Entity '{entityType.Name}' configures {string.Join(", ", ignored)}, which dynamic CRUD does not invoke. Map this entity with the typed CreateBuilder, or " +
                "remove the hooks.");
        }

        static void AddIf(List<string> target, bool condition, string name)
        {
            if (condition)
                target.Add(name);
        }
    }

    /// <summary>
    /// Dynamic routes carry <c>{entityType}</c> and serve every registered entity, so a per-entity <see cref="EndpointAuth" /> cannot be expressed as a route convention. Rejecting
    /// it is better than accepting configuration that silently does nothing.
    /// </summary>
    private static void ThrowOnPerEntityAuth<TContext>(DynamicEndpointConfig<TContext> config)
        where TContext : DbContext
    {
        foreach (var (entityType, entityConfig) in config.EntityConfigs) {
            var hasAuth = entityConfig.CreateConfig?.Auth ?? entityConfig.PatchConfig?.Auth ?? entityConfig.UpdateConfig?.Auth ?? entityConfig.DeleteConfig?.Auth ??
                entityConfig.UpsertConfig?.Auth ?? entityConfig.ExportConfig?.Auth;

            OperationHelpers.ThrowIf(
                hasAuth is not null,
                $"Entity '{entityType.Name}' configures per-operation authorization, which dynamic CRUD cannot honour: its routes are keyed on {{entityType}} and shared across " +
                "every registered entity. Set authorization on the dynamic defaults, or map this entity with the typed CreateBuilder instead.");
        }
    }

    private static IResult HandleGetEntityMetadata(IReadOnlyDictionary<string, EntityEndpointMetadata> registry, string entityType, HttpContext httpContext)
    {
        if (!TryGetMetadata(registry, entityType, out var meta))
            return ApiErrorResponseFactory.ThrowNotFound(httpContext, null, $"Unknown entity type: {entityType}");

        var entityMetadata = ToEntityTypeMetadata(meta);
        return Results.Json(entityMetadata);
    }

    private static EntityTypeMetadata ToEntityTypeMetadata(EntityEndpointMetadata m)
    {
        var properties = m.EntityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .Select(ToPropertyMetadata)
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToList();

        return new(m.EntityType.Name, m.KeyPropertyName, m.KeyType.Name, properties);
    }

    private static CrudMetadataResponse BuildMetadata(IReadOnlyList<EntityEndpointMetadata> entities)
        => new(entities.Select(ToEntityTypeMetadata).ToList());

    private static PropertyMetadata ToPropertyMetadata(PropertyInfo p)
    {
        var propType = p.PropertyType;
        var underlying = Nullable.GetUnderlyingType(propType) ?? propType;
        var typeName = underlying.GetFriendlyTypeName();
        var isNullable = !propType.IsValueType || Nullable.GetUnderlyingType(propType) != null;
        return new(p.Name, typeName, isNullable);
    }

    private static Action<CreateContext<object, object, TContext>>? WrapDefaultBeforeCreate<TContext>(Action<CreateContext<object, object, DbContext>>? before)
        where TContext : DbContext
    {
        if (before == null)
            return null;

        return ctx => before(new(ctx.Request, ctx.Entity, ctx.DbContext, ctx.Services));
    }

    private static (Delegate? Before, Delegate? After) AdaptPatchDelegates<TContext>(Type entityType, PatchConfig<object, TContext>? patchConfig)
        where TContext : DbContext
    {
        if (patchConfig == null)
            return (null, null);

        var adapter = typeof(DynamicCrudEndpointBuilder).GetMethod(nameof(AdaptPatchDelegate), BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(
            entityType, typeof(TContext));

        var before = patchConfig.Before == null ? null : (Delegate)adapter.Invoke(null, [patchConfig.Before])!;
        var after = patchConfig.After == null ? null : (Delegate)adapter.Invoke(null, [patchConfig.After])!;
        return (before, after);
    }

    private static Action<PatchContext<TEntity, TContext>> AdaptPatchDelegate<TEntity, TContext>(Action<PatchContext<object, TContext>> inner)
        where TEntity : class where TContext : DbContext
        => ctx => inner(new(ctx.Request, ctx.Entity, ctx.DbContext, ctx.Services));

    private static Delegate? CreateBeforeCreateDelegate<TContext, TEntity>(Action<CreateContext<object, object, TContext>>? before)
        where TContext : DbContext where TEntity : class
    {
        if (before == null)
            return null;

        return (Action<CreateContext<TEntity, TEntity, TContext>>)(ctx => before(new(ctx.Request, ctx.Entity, ctx.DbContext, ctx.Services)));
    }

    private static (string Name, Type ClrType)? GetPrimaryKeyInfo<TContext>(TContext context, Type entityType)
        where TContext : DbContext
    {
        var entityTypeConfig = context.Model.FindEntityType(entityType);
        var pk = entityTypeConfig?.FindPrimaryKey();
        if (pk == null || pk.Properties.Count != 1)
            return null;

        var prop = pk.Properties[0];
        return (prop.Name, prop.ClrType);
    }

    private static (MethodInfo Query, MethodInfo QueryProjected, MethodInfo Get, MethodInfo CreateAsync, MethodInfo CreateBulkAsync, MethodInfo PatchAsync, MethodInfo
        PatchBulkAsync, MethodInfo DeleteAsync, MethodInfo DeleteByRequestAsync, MethodInfo DeleteBulkAsync, MethodInfo UpdateAsync, MethodInfo UpdateBulkAsync, MethodInfo
        UpsertAsync, MethodInfo UpsertBulkAsync) BuildMethodCache<TContext>()
        where TContext : DbContext
    {
        var queryServiceType = typeof(IQueryService<TContext>);
        var createServiceType = typeof(ICreateService<TContext>);
        var patchServiceType = typeof(IPatchService<TContext>);
        var deleteServiceType = typeof(IDeleteService<TContext>);
        var updateServiceType = typeof(IUpdateService<TContext>);
        var upsertServiceType = typeof(IUpsertService<TContext>);
        return (queryServiceType.GetMethods().First(m => m.Name == "Query" && m.GetGenericArguments().Length == 2),
            queryServiceType.GetMethod(nameof(IQueryService<TContext>.QueryProjected))!,
            queryServiceType.GetMethods().First(m => m.Name == "Get" && m.GetGenericArguments().Length == 2),
            createServiceType.GetMethods().First(m => m.Name == "CreateAsync" && m.GetGenericArguments().Length == 3),
            createServiceType.GetMethods().First(m => m.Name == "CreateBulkAsync" && m.GetGenericArguments().Length == 3),
            patchServiceType.GetMethods().First(m => m.Name == "PatchAsync" && m.GetGenericArguments().Length == 2),
            patchServiceType.GetMethods().First(m => m.Name == "PatchBulkAsync" && m.GetGenericArguments().Length == 2),
            deleteServiceType.GetMethods().First(m => m.Name == "DeleteAsync" && m.GetParameters()[0].ParameterType == typeof(object[])),
            deleteServiceType.GetMethods().First(m => m.Name == "DeleteAsync" && m.GetParameters()[0].ParameterType == typeof(DeleteRequest)),
            deleteServiceType.GetMethods().First(m => m.Name == "DeleteBulkAsync" && m.GetGenericArguments().Length == 2),
            updateServiceType.GetMethods().First(m => m.Name == "UpdateAsync" && m.GetGenericArguments().Length == 3),
            updateServiceType.GetMethods().First(m => m.Name == "UpdateBulkAsync" && m.GetGenericArguments().Length == 3),
            upsertServiceType.GetMethods().First(m => m.Name == "UpsertAsync" && m.GetGenericArguments().Length == 3),
            upsertServiceType.GetMethods().First(m => m.Name == "UpsertBulkAsync" && m.GetGenericArguments().Length == 3));
    }

    private static DynamicMethodCache BuildEntityMethodCache<TContext>(
        object baseCache,
        Type entityType,
        string keyPropertyName,
        Action<CreateContext<object, object, TContext>>? beforeCreate)
        where TContext : DbContext
    {
        var (query, queryProjected, get, createAsync, createBulkAsync, patchAsync, patchBulkAsync, deleteAsync, deleteByRequestAsync, deleteBulkAsync, updateAsync, updateBulkAsync,
                upsertAsync, upsertBulkAsync) =
            ((MethodInfo Query, MethodInfo QueryProjected, MethodInfo Get, MethodInfo CreateAsync, MethodInfo CreateBulkAsync, MethodInfo PatchAsync, MethodInfo PatchBulkAsync,
                MethodInfo DeleteAsync, MethodInfo DeleteByRequestAsync, MethodInfo DeleteBulkAsync, MethodInfo UpdateAsync, MethodInfo UpdateBulkAsync, MethodInfo UpsertAsync,
                MethodInfo UpsertBulkAsync))baseCache;

        Delegate? beforeCreateDelegate = null;
        if (beforeCreate != null) {
            var helper = typeof(DynamicCrudEndpointBuilder).GetMethod(nameof(CreateBeforeCreateDelegate), BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(
                typeof(TContext), entityType);

            beforeCreateDelegate = (Delegate?)helper.Invoke(null, [beforeCreate])!;
        }

        var queryResultsType = typeof(QueryRes<>).MakeGenericType(entityType);
        var createResultType = typeof(CreateResult<>).MakeGenericType(entityType);
        var createBulkResultType = typeof(CreateBulkResult<>).MakeGenericType(entityType);
        var patchResultType = typeof(PatchResult<>).MakeGenericType(entityType);
        var patchBulkResultType = typeof(PatchBulkResult<>).MakeGenericType(entityType);
        var deleteResultType = typeof(DeleteResult<>).MakeGenericType(entityType);
        var deleteBulkResultType = typeof(DeleteBulkResult<>).MakeGenericType(entityType);
        var updateResultType = typeof(UpdateResult<>).MakeGenericType(entityType);
        var updateBulkResultType = typeof(UpdateBulkResult<>).MakeGenericType(entityType);
        var upsertResultType = typeof(UpsertResult<>).MakeGenericType(entityType);
        var upsertBulkResultType = typeof(UpsertBulkResult<>).MakeGenericType(entityType);
        return new(
            beforeCreateDelegate, query.MakeGenericMethod(entityType, entityType), queryProjected.MakeGenericMethod(entityType), get.MakeGenericMethod(entityType, entityType),
            createAsync.MakeGenericMethod(entityType, entityType, entityType), createBulkAsync.MakeGenericMethod(entityType, entityType, entityType),
            patchAsync.MakeGenericMethod(entityType, entityType), patchBulkAsync.MakeGenericMethod(entityType, entityType), deleteAsync.MakeGenericMethod(entityType, entityType),
            deleteByRequestAsync.MakeGenericMethod(entityType, entityType), deleteBulkAsync.MakeGenericMethod(entityType, entityType),
            updateAsync.MakeGenericMethod(entityType, entityType, entityType), updateBulkAsync.MakeGenericMethod(entityType, entityType, entityType),
            upsertAsync.MakeGenericMethod(entityType, entityType, entityType), upsertBulkAsync.MakeGenericMethod(entityType, entityType, entityType),
            typeof(Task<>).MakeGenericType(queryResultsType).GetProperty("Result")!,
            typeof(Task<>).MakeGenericType(typeof(ProjectedQueryRes<>).MakeGenericType(typeof(object))).GetProperty("Result")!,
            typeof(Task<>).MakeGenericType(entityType).GetProperty("Result")!, typeof(Task<>).MakeGenericType(createResultType).GetProperty("Result")!,
            typeof(Task<>).MakeGenericType(createBulkResultType).GetProperty("Result")!, typeof(Task<>).MakeGenericType(patchResultType).GetProperty("Result")!,
            typeof(Task<>).MakeGenericType(patchBulkResultType).GetProperty("Result")!, typeof(Task<>).MakeGenericType(deleteResultType).GetProperty("Result")!,
            typeof(Task<>).MakeGenericType(deleteBulkResultType).GetProperty("Result")!, typeof(Task<>).MakeGenericType(updateResultType).GetProperty("Result")!,
            typeof(Task<>).MakeGenericType(updateBulkResultType).GetProperty("Result")!, typeof(Task<>).MakeGenericType(upsertResultType).GetProperty("Result")!,
            typeof(Task<>).MakeGenericType(upsertBulkResultType).GetProperty("Result")!, createResultType.GetProperty("IsSuccess")!, createResultType.GetProperty("Data")!,
            createResultType.GetProperty("Error")!, entityType.GetProperty(keyPropertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)!,
            patchResultType.GetProperty("IsSuccess")!, patchResultType.GetProperty("Error")!, updateResultType.GetProperty("Result")!, updateResultType.GetProperty("Error")!);
    }

    private static bool TryGetMetadata(IReadOnlyDictionary<string, EntityEndpointMetadata> registry, string entityType, out EntityEndpointMetadata meta)
    {
        if (registry.TryGetValue(entityType, out meta!))
            return true;

        meta = null!;
        return false;
    }

    private static object ParseKey(string id, Type keyType) => TypeConversion.ConvertTo(id, keyType)!;

    /// <summary>Sets the key on UpdateRequest.Data from Keys so Mapster does not overwrite it with a default (Guid.Empty) and trigger an EF key-modification error.</summary>
    private static void EnsureKeyOnUpdateData(object request, EntityEndpointMetadata meta)
    {
        var keysProp = request.GetType().GetProperty("Keys");
        var keys = keysProp?.GetValue(request) as object[];
        if (keys == null || keys.Length == 0)
            return;

        var dataProp = request.GetType().GetProperty("Data");
        var data = dataProp?.GetValue(request);
        if (data == null)
            return;

        var keyProp = meta.EntityType.GetProperty(meta.KeyPropertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (keyProp == null || !keyProp.CanWrite)
            return;

        var rawKey = keys.Length == 1 ? keys[0] : null;
        if (rawKey == null)
            return;

        var keyValue = rawKey;
        if (!keyProp.PropertyType.IsInstanceOfType(rawKey)) {
            if (rawKey is JsonElement element) {
                keyValue = element.ValueKind == JsonValueKind.String && keyProp.PropertyType == typeof(Guid)
                    ? Guid.Parse(element.GetString() ?? "")
                    : element.Deserialize(keyProp.PropertyType);
            }
            else if (rawKey is string s && keyProp.PropertyType == typeof(Guid))
                keyValue = Guid.Parse(s);
        }

        if (keyValue != null && keyProp.PropertyType.IsInstanceOfType(keyValue))
            keyProp.SetValue(data, keyValue);
    }

    /// <summary>Sets the key on UpsertRequest.NewData from Keys or Query (when ConditionClause is on a key property), so Mapster does not overwrite it with a default.</summary>
    private static void EnsureKeyOnUpsertData(object request, EntityEndpointMetadata meta)
    {
        object? keyValue = null;
        var keysProp = request.GetType().GetProperty("Keys");
        var keys = keysProp?.GetValue(request) as object[];
        if (keys is { Length: > 0 })
            keyValue = keys[0];
        else {
            var queryProp = request.GetType().GetProperty("Query");
            var query = queryProp?.GetValue(request);
            if (query != null && string.Equals(query.GetType().GetProperty("Field")?.GetValue(query) as string, meta.KeyPropertyName, StringComparison.OrdinalIgnoreCase))
                keyValue = query.GetType().GetProperty("Value")?.GetValue(query);
        }

        if (keyValue == null)
            return;

        var dataProp = request.GetType().GetProperty("NewData");
        var data = dataProp?.GetValue(request);
        if (data == null)
            return;

        var keyProp = meta.EntityType.GetProperty(meta.KeyPropertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (keyProp == null || !keyProp.CanWrite)
            return;

        var convertedKey = keyValue;
        if (!keyProp.PropertyType.IsInstanceOfType(keyValue)) {
            if (keyValue is JsonElement element) {
                convertedKey = element.ValueKind == JsonValueKind.String && keyProp.PropertyType == typeof(Guid)
                    ? Guid.Parse(element.GetString() ?? "")
                    : element.Deserialize(keyProp.PropertyType);
            }
            else if (keyValue is string s && keyProp.PropertyType == typeof(Guid))
                convertedKey = Guid.Parse(s);
        }

        if (convertedKey != null && keyProp.PropertyType.IsInstanceOfType(convertedKey))
            keyProp.SetValue(data, convertedKey);
    }

    /// <summary>
    /// Deserializer and key-parser exception messages name CLR types, JSON paths, and property names the caller was never shown. These two helpers keep the response to what the
    /// caller supplied.
    /// </summary>
    private static IResult RejectUnparsableKey(HttpContext httpContext, string id, string entityType)
        => ApiErrorResponseFactory.ThrowForError(
            httpContext, LyoProblemDetails.FromCode(Constants.ApiErrorCodes.InvalidRequest, $"Id '{id}' is not a valid key for entity '{entityType}'.", DateTime.UtcNow));

    private static IResult RejectUnreadableBody(HttpContext httpContext, string entityType)
        => ApiErrorResponseFactory.ThrowForError(
            httpContext,
            LyoProblemDetails.FromCode(
                Constants.ApiErrorCodes.InvalidRequest, $"Request body could not be read as '{entityType}'. Check the payload against the entity metadata route.",
                DateTime.UtcNow));

    /// <summary>
    /// Runs the entity's patch property rules against a full-replacement write. Update and upsert send the whole body, so the properties being written are derived by diffing it
    /// against the persisted row. Without this, a caller barred from patching a property could set it by switching to update.
    /// </summary>
    private static async Task<LyoProblemDetails?> AuthorizeDynamicWriteAsync<TContext>(
        EntityEndpointMetadata meta,
        HttpContext httpContext,
        IQueryService<TContext> queryService,
        object request,
        string dataPropertyName,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (meta.PatchPropertyAuthorization is null)
            return null;

        var incoming = request.GetType().GetProperty(dataPropertyName)?.GetValue(request);
        if (incoming is null)
            return null;

        object? current = null;
        if (request.GetType().GetProperty("Keys")?.GetValue(request) is object[] { Length: > 0 } keys) {
            var task = (Task)meta.Cache.Get.Invoke(queryService, [keys, null, null, null, ct])!;
            await task.ConfigureAwait(false);
            current = meta.Cache.GetTaskResultProperty.GetValue(task);
        }

        return await PatchPropertyAuthorizationApplier.AuthorizeReplacementAsync(meta.PatchPropertyAuthorization, httpContext, meta.EntityType, incoming, current, ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the result as 200, or maps its <c>Error</c> to the matching status when it reports a failure.
    /// </summary>
    /// <remarks>
    /// The dynamic handlers work through reflection and used to hand the boxed result straight to <c>Results.Json</c>, so a failed query or delete came back as 200 with a problem
    /// document in the body. Clients checking the status code treated those as successes.
    /// </remarks>
    private static IResult ResultOrProblem(HttpContext httpContext, object? result)
    {
        if (result is null)
            return Results.Json(result);

        var resultType = result.GetType();
        if (resultType.GetProperty("IsSuccess")?.GetValue(result) is not false)
            return Results.Json(result);

        return resultType.GetProperty("Error")?.GetValue(result) is LyoProblemDetails problem
            ? ApiErrorResponseFactory.ThrowForError(httpContext, problem)
            : Results.Json(result);
    }

    private static IResult RejectInvalidQuery(HttpContext httpContext, List<ApiError> errors)
        => ApiErrorResponseFactory.ThrowForError(
            httpContext,
            LyoProblemDetailsBuilder.CreateWithActivity().WithErrorCode(Constants.ApiErrorCodes.InvalidQuery).WithMessage("Invalid query.").AddErrors(errors).Build());

    private static async Task<IResult> HandleQuery<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata> registry,
        string entityType,
        QueryConcreteReq queryRequest,
        IQueryService<TContext> queryService,
        HttpContext httpContext,
        SortDirection defaultSortDirection,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata(registry, entityType, out var meta))
            return ApiErrorResponseFactory.ThrowNotFound(httpContext, null, $"Unknown entity type: {entityType}");

        var task = (Task)meta.Cache.Query.Invoke(queryService, [queryRequest, meta.DefaultOrder, defaultSortDirection, ct])!;
        await task.ConfigureAwait(false);
        return ResultOrProblem(httpContext, meta.Cache.QueryTaskResultProperty.GetValue(task));
    }

    private static async Task<IResult> HandleQueryProjected<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata> registry,
        string entityType,
        ProjectionQueryReq queryRequest,
        IQueryService<TContext> queryService,
        HttpContext httpContext,
        SortDirection defaultSortDirection,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata(registry, entityType, out var meta))
            return ApiErrorResponseFactory.ThrowNotFound(httpContext, null, $"Unknown entity type: {entityType}");

        var task = (Task)meta.Cache.QueryProjected.Invoke(queryService, [queryRequest, meta.DefaultOrder, defaultSortDirection, ct])!;
        await task.ConfigureAwait(false);
        return ResultOrProblem(httpContext, meta.Cache.QueryProjectedTaskResultProperty.GetValue(task));
    }

    private static async Task<IResult> HandleGet<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata> registry,
        string entityType,
        string id,
        string[] include,
        IQueryService<TContext> queryService,
        HttpContext httpContext,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata(registry, entityType, out var meta))
            return ApiErrorResponseFactory.ThrowNotFound(httpContext, null, $"Unknown entity type: {entityType}");

        object key;
        try {
            key = ParseKey(id, meta.KeyType);
        }
        catch (Exception) {
            return RejectUnparsableKey(httpContext, id, entityType);
        }

        var task = (Task)meta.Cache.Get.Invoke(queryService, [new[] { key }, include, null, null, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.GetTaskResultProperty.GetValue(task);
        return result != null ? Results.Ok(result) : ApiErrorResponseFactory.ThrowNotFound(httpContext, [key]);
    }

    private static async Task<IResult> HandleCreate<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata> registry,
        string entityType,
        HttpRequest request,
        ICreateService<TContext> createService,
        HttpContext httpContext,
        JsonSerializerOptions jsonOptions,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata(registry, entityType, out var meta))
            return ApiErrorResponseFactory.ThrowNotFound(httpContext, null, $"Unknown entity type: {entityType}");

        object? body;
        try {
            body = await JsonSerializer.DeserializeAsync(request.Body, meta.EntityType, jsonOptions, ct).ConfigureAwait(false);
        }
        catch (Exception) {
            return RejectUnreadableBody(httpContext, entityType);
        }

        if (body == null) {
            return ApiErrorResponseFactory.ThrowForError(httpContext, LyoProblemDetails.FromCode(Constants.ApiErrorCodes.InvalidRequest, "Request body is required", DateTime.UtcNow));
        }

        var task = (Task)meta.Cache.CreateAsync.Invoke(createService, [body, meta.Cache.BeforeCreateDelegate, null, null, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.CreateTaskResultProperty.GetValue(task);
        var isSuccess = meta.Cache.CreateResultIsSuccessProperty.GetValue(result);
        if (Equals(isSuccess, true)) {
            var data = meta.Cache.CreateResultDataProperty.GetValue(result);
            var id = data != null ? meta.Cache.CreateResultKeyProperty.GetValue(data) : null;
            return Results.Created($"{request.Path}/{id}", result);
        }

        var error = meta.Cache.CreateResultErrorProperty.GetValue(result);
        return ApiErrorResponseFactory.ThrowForError(httpContext, (LyoProblemDetails)error!);
    }

    private static async Task<IResult> HandleCreateBulk<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata> registry,
        string entityType,
        HttpRequest request,
        ICreateService<TContext> createService,
        HttpContext httpContext,
        JsonSerializerOptions jsonOptions,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata(registry, entityType, out var meta))
            return ApiErrorResponseFactory.ThrowNotFound(httpContext, null, $"Unknown entity type: {entityType}");

        var listType = typeof(List<>).MakeGenericType(meta.EntityType);
        object? body;
        try {
            body = await JsonSerializer.DeserializeAsync(request.Body, listType, jsonOptions, ct).ConfigureAwait(false);
        }
        catch (Exception) {
            return RejectUnreadableBody(httpContext, entityType);
        }

        if (body == null) {
            return ApiErrorResponseFactory.ThrowForError(httpContext, LyoProblemDetails.FromCode(Constants.ApiErrorCodes.InvalidRequest, "Request body is required", DateTime.UtcNow));
        }

        var task = (Task)meta.Cache.CreateBulkAsync.Invoke(createService, [body, meta.Cache.BeforeCreateDelegate, null, null, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.CreateBulkTaskResultProperty.GetValue(task);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandlePatch<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata> registry,
        string entityType,
        PatchRequest patchRequest,
        IPatchService<TContext> patchService,
        HttpContext httpContext,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata(registry, entityType, out var meta))
            return ApiErrorResponseFactory.ThrowNotFound(httpContext, null, $"Unknown entity type: {entityType}");

        var fieldAuth = await PatchPropertyAuthorizationApplier.ApplyAsync(meta.PatchPropertyAuthorization, httpContext, meta.EntityType, patchRequest, ct).ConfigureAwait(false);
        if (!fieldAuth.Success) {
            return ApiErrorResponseFactory.ThrowForError(httpContext, fieldAuth.Error);
        }

        patchRequest = fieldAuth.Request!;
        var task = (Task)meta.Cache.PatchAsync.Invoke(patchService, [patchRequest, meta.AdaptedPatchBefore, meta.AdaptedPatchAfter, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.PatchTaskResultProperty.GetValue(task);
        var isSuccess = meta.Cache.PatchResultIsSuccessProperty.GetValue(result);
        if (Equals(isSuccess, true))
            return Results.Ok(result);

        var error = meta.Cache.PatchResultErrorProperty.GetValue(result);
        return error != null ? ApiErrorResponseFactory.ThrowForError(httpContext, (LyoProblemDetails)error) : Results.Ok(result);
    }

    private static async Task<IResult> HandlePatchBulk<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata> registry,
        string entityType,
        List<PatchRequest> requests,
        IPatchService<TContext> patchService,
        HttpContext httpContext,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata(registry, entityType, out var meta))
            return ApiErrorResponseFactory.ThrowNotFound(httpContext, null, $"Unknown entity type: {entityType}");

        if (requests.Count == 0) {
            return ApiErrorResponseFactory.ThrowForError(
                    httpContext, LyoProblemDetails.FromCode(Constants.ApiErrorCodes.InvalidPatchRequest, "At least one patch request is required", DateTime.UtcNow));
        }

        if (meta.PatchPropertyAuthorization != null) {
            var sanitized = new List<PatchRequest>(requests.Count);
            foreach (var pr in requests) {
                var fieldAuth = await PatchPropertyAuthorizationApplier.ApplyAsync(meta.PatchPropertyAuthorization, httpContext, meta.EntityType, pr, ct).ConfigureAwait(false);
                if (!fieldAuth.Success) {
                    return ApiErrorResponseFactory.ThrowForError(httpContext, fieldAuth.Error);
                }

                sanitized.Add(fieldAuth.Request!);
            }

            requests = sanitized;
        }

        var task = (Task)meta.Cache.PatchBulkAsync.Invoke(patchService, [requests, meta.AdaptedPatchBefore, meta.AdaptedPatchAfter, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.PatchBulkTaskResultProperty.GetValue(task);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUpdate<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata> registry,
        string entityType,
        JsonNode? body,
        IUpdateService<TContext> updateService,
        IQueryService<TContext> queryService,
        HttpContext httpContext,
        JsonSerializerOptions jsonOptions,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (body == null) {
            return ApiErrorResponseFactory.ThrowForError(httpContext, LyoProblemDetails.FromCode(Constants.ApiErrorCodes.InvalidRequest, "Request body is required", DateTime.UtcNow));
        }

        if (!TryGetMetadata(registry, entityType, out var meta))
            return ApiErrorResponseFactory.ThrowNotFound(httpContext, null, $"Unknown entity type: {entityType}");

        var updateRequestType = typeof(UpdateRequest<>).MakeGenericType(meta.EntityType);
        object? request;
        try {
            request = body.Deserialize(updateRequestType, jsonOptions);
        }
        catch (Exception) {
            return RejectUnreadableBody(httpContext, entityType);
        }

        if (request == null) {
            return ApiErrorResponseFactory.ThrowForError(httpContext, LyoProblemDetails.FromCode(Constants.ApiErrorCodes.InvalidRequest, "Request body is required", DateTime.UtcNow));
        }

        EnsureKeyOnUpdateData(request, meta);
        var propertyDenial = await AuthorizeDynamicWriteAsync(meta, httpContext, queryService, request, "Data", ct).ConfigureAwait(false);
        if (propertyDenial != null)
            return ApiErrorResponseFactory.ThrowForError(httpContext, propertyDenial);

        var task = (Task)meta.Cache.UpdateAsync.Invoke(updateService, [request, null, null, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.UpdateTaskResultProperty.GetValue(task);
        var resultEnum = meta.Cache.UpdateResultResultProperty.GetValue(result);
        if (resultEnum is UpdateResultEnum.Failed) {
            var error = meta.Cache.UpdateResultErrorProperty.GetValue(result);
            return ApiErrorResponseFactory.ThrowForError(httpContext, (LyoProblemDetails)error!);
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUpdateBulk<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata> registry,
        string entityType,
        JsonNode? body,
        IUpdateService<TContext> updateService,
        IQueryService<TContext> queryService,
        HttpContext httpContext,
        JsonSerializerOptions jsonOptions,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (body == null) {
            return ApiErrorResponseFactory.ThrowForError(httpContext, LyoProblemDetails.FromCode(Constants.ApiErrorCodes.InvalidRequest, "Request body is required", DateTime.UtcNow));
        }

        if (!TryGetMetadata(registry, entityType, out var meta))
            return ApiErrorResponseFactory.ThrowNotFound(httpContext, null, $"Unknown entity type: {entityType}");

        var listType = typeof(List<>).MakeGenericType(typeof(UpdateRequest<>).MakeGenericType(meta.EntityType));
        object? requests;
        try {
            requests = body.Deserialize(listType, jsonOptions);
        }
        catch (Exception) {
            return RejectUnreadableBody(httpContext, entityType);
        }

        if (requests == null) {
            return ApiErrorResponseFactory.ThrowForError(httpContext, LyoProblemDetails.FromCode(Constants.ApiErrorCodes.InvalidRequest, "Request body is required", DateTime.UtcNow));
        }

        foreach (var req in (IEnumerable)requests) {
            EnsureKeyOnUpdateData(req!, meta);
            var denial = await AuthorizeDynamicWriteAsync(meta, httpContext, queryService, req!, "Data", ct).ConfigureAwait(false);
            if (denial != null)
                return ApiErrorResponseFactory.ThrowForError(httpContext, denial);
        }

        var task = (Task)meta.Cache.UpdateBulkAsync.Invoke(updateService, [requests, null, null, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.UpdateBulkTaskResultProperty.GetValue(task);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUpsert<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata> registry,
        string entityType,
        JsonNode? body,
        IUpsertService<TContext> upsertService,
        IQueryService<TContext> queryService,
        HttpContext httpContext,
        JsonSerializerOptions jsonOptions,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (body == null) {
            return ApiErrorResponseFactory.ThrowForError(httpContext, LyoProblemDetails.FromCode(Constants.ApiErrorCodes.InvalidRequest, "Request body is required", DateTime.UtcNow));
        }

        if (!TryGetMetadata(registry, entityType, out var meta))
            return ApiErrorResponseFactory.ThrowNotFound(httpContext, null, $"Unknown entity type: {entityType}");

        var upsertRequestType = typeof(UpsertRequest<>).MakeGenericType(meta.EntityType);
        object? request;
        try {
            request = body.Deserialize(upsertRequestType, jsonOptions);
        }
        catch (Exception) {
            return RejectUnreadableBody(httpContext, entityType);
        }

        if (request == null) {
            return ApiErrorResponseFactory.ThrowForError(httpContext, LyoProblemDetails.FromCode(Constants.ApiErrorCodes.InvalidRequest, "Request body is required", DateTime.UtcNow));
        }

        EnsureKeyOnUpsertData(request, meta);
        var propertyDenial = await AuthorizeDynamicWriteAsync(meta, httpContext, queryService, request, "NewData", ct).ConfigureAwait(false);
        if (propertyDenial != null)
            return ApiErrorResponseFactory.ThrowForError(httpContext, propertyDenial);

        var task = (Task)meta.Cache.UpsertAsync.Invoke(upsertService, [request, null, null, null, null, null, null, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.UpsertTaskResultProperty.GetValue(task);
        var resultEnum = result?.GetType().GetProperty("Result")?.GetValue(result);
        if (resultEnum is UpsertResultEnum.Failed) {
            var error = result?.GetType().GetProperty("Error")?.GetValue(result);
            return ApiErrorResponseFactory.ThrowForError(httpContext, (LyoProblemDetails)error!);
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUpsertBulk<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata> registry,
        string entityType,
        JsonNode? body,
        IUpsertService<TContext> upsertService,
        IQueryService<TContext> queryService,
        HttpContext httpContext,
        JsonSerializerOptions jsonOptions,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (body == null) {
            return ApiErrorResponseFactory.ThrowForError(httpContext, LyoProblemDetails.FromCode(Constants.ApiErrorCodes.InvalidRequest, "Request body is required", DateTime.UtcNow));
        }

        if (!TryGetMetadata(registry, entityType, out var meta))
            return ApiErrorResponseFactory.ThrowNotFound(httpContext, null, $"Unknown entity type: {entityType}");

        var listType = typeof(List<>).MakeGenericType(typeof(UpsertRequest<>).MakeGenericType(meta.EntityType));
        object? requests;
        try {
            requests = body.Deserialize(listType, jsonOptions);
        }
        catch (Exception) {
            return RejectUnreadableBody(httpContext, entityType);
        }

        if (requests == null) {
            return ApiErrorResponseFactory.ThrowForError(httpContext, LyoProblemDetails.FromCode(Constants.ApiErrorCodes.InvalidRequest, "Request body is required", DateTime.UtcNow));
        }

        foreach (var req in (IEnumerable)requests) {
            EnsureKeyOnUpsertData(req!, meta);
            var denial = await AuthorizeDynamicWriteAsync(meta, httpContext, queryService, req!, "NewData", ct).ConfigureAwait(false);
            if (denial != null)
                return ApiErrorResponseFactory.ThrowForError(httpContext, denial);
        }

        var task = (Task)meta.Cache.UpsertBulkAsync.Invoke(upsertService, [requests, null, null, null, null, null, null, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.UpsertBulkTaskResultProperty.GetValue(task);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDeleteByRequest<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata> registry,
        string entityType,
        DeleteRequest deleteRequest,
        IDeleteService<TContext> deleteService,
        HttpContext httpContext,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata(registry, entityType, out var meta))
            return ApiErrorResponseFactory.ThrowNotFound(httpContext, null, $"Unknown entity type: {entityType}");

        var task = (Task)meta.Cache.DeleteByRequestAsync.Invoke(deleteService, [deleteRequest, null, null, null, null, ct])!;
        await task.ConfigureAwait(false);
        return ResultOrProblem(httpContext, meta.Cache.DeleteTaskResultProperty.GetValue(task));
    }

    private static async Task<IResult> HandleDelete<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata> registry,
        string entityType,
        string id,
        IDeleteService<TContext> deleteService,
        HttpContext httpContext,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata(registry, entityType, out var meta))
            return ApiErrorResponseFactory.ThrowNotFound(httpContext, null, $"Unknown entity type: {entityType}");

        object key;
        try {
            key = ParseKey(id, meta.KeyType);
        }
        catch (Exception) {
            return RejectUnparsableKey(httpContext, id, entityType);
        }

        var task = (Task)meta.Cache.DeleteAsync.Invoke(deleteService, [new[] { key }, null, null, null, null, ct])!;
        await task.ConfigureAwait(false);
        return ResultOrProblem(httpContext, meta.Cache.DeleteTaskResultProperty.GetValue(task));
    }

    private static async Task<IResult> HandleDeleteBulk<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata> registry,
        string entityType,
        List<DeleteRequest> requests,
        IDeleteService<TContext> deleteService,
        HttpContext httpContext,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata(registry, entityType, out var meta))
            return ApiErrorResponseFactory.ThrowNotFound(httpContext, null, $"Unknown entity type: {entityType}");

        if (requests.Count == 0)
            return ApiErrorResponseFactory.ThrowForError(
                httpContext, LyoProblemDetails.FromCode(Constants.ApiErrorCodes.InvalidDeleteRequest, "At least one delete request is required", DateTime.UtcNow));

        var task = (Task)meta.Cache.DeleteBulkAsync.Invoke(deleteService, [requests, null, null, null, null, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.DeleteBulkTaskResultProperty.GetValue(task);
        return Results.Ok(result);
    }

    internal static async Task<IResult> HandleExport<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata> registry,
        DynamicEndpointConfig<TContext> config,
        string entityType,
        ExportRequest request,
        IExportService<TContext> exportService,
        HttpContext httpContext,
        SortDirection defaultSortDirection,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata(registry, entityType, out var meta))
            return ApiErrorResponseFactory.ThrowNotFound(httpContext, null, $"Unknown entity type: {entityType}");

        var exportFeature = ApiFeature.TryFromName("Export");
        if (exportFeature is null || !config.GetConfig(meta.EntityType).Features.Contains(exportFeature))
            return ApiErrorResponseFactory.ThrowNotFound(httpContext, null, $"Export not enabled for {entityType}");

        try {
            var exportMethod = typeof(IExportService<TContext>).GetMethod(nameof(IExportService<TContext>.ExportAsync))!.MakeGenericMethod(meta.EntityType, meta.EntityType);
            var task = (Task)exportMethod.Invoke(exportService, [request, meta.DefaultOrder, defaultSortDirection, ct])!;
            await task.ConfigureAwait(false);
            var result = task.GetType().GetProperty("Result")!.GetValue(task);
            var (stream, contentType, fileName) = ((Stream, string, string))result!;
            return Results.File(stream, contentType, fileName);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null) {
            throw ex.InnerException;
        }
    }
}