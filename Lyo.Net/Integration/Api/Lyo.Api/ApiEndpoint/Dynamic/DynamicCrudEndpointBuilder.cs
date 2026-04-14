using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Enums;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Crud.Create;
using Lyo.Api.Services.Crud.Delete;
using Lyo.Api.Services.Crud.Read;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Services.Crud.Update;
using Lyo.Api.Services.Export;
using Lyo.Common.Enums;
using Lyo.Query.Models.Common.Request;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace Lyo.Api.ApiEndpoint.Dynamic;

/// <summary>Maps a single set of CRUD endpoints with {entityType} route parameter. Replaces per-entity endpoint registration.</summary>
public static class DynamicCrudEndpointBuilder
{
    /// <summary>Maps dynamic CRUD endpoints using the fluent config builder. Configure defaults and per-entity overrides.</summary>
    /// <example>
    /// <code>
    /// app.MapDynamicCrudEndpoints&lt;PeopleDbContext&gt;(c => c
    ///     .WithDefaults(d => { d.BaseRoute = "Person"; d.Features = ApiFeatureFlag.All; })
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

    /// <summary>Maps dynamic CRUD endpoints: /{baseRoute}/{entityType}/Query, /{entityType}/Get/{id}, etc. Simpler overload using DynamicEndpointOptions.</summary>
    public static WebApplication MapDynamicCrudEndpoints<TContext>(this WebApplication webApp, Action<DynamicEndpointOptions<TContext>>? configure = null)
        where TContext : DbContext
    {
        var options = new DynamicEndpointOptions<TContext>();
        configure?.Invoke(options);
        var config = ConvertDynamicOptionsToConfig(options);
        return MapDynamicCrudEndpointsCore(webApp, config);
    }

    private static DynamicEndpointConfig<TContext> ConvertDynamicOptionsToConfig<TContext>(DynamicEndpointOptions<TContext> options)
        where TContext : DbContext
    {
        var defaults = new DynamicEndpointDefaults { Features = options.Features, BaseRoute = options.BaseRoute };
        defaults.ExcludedTypes.UnionWith(options.ExcludedTypes);
        defaults.IncludedTypes.AddRange(options.IncludedTypes);
        return new(defaults, new Dictionary<Type, EntityEndpointConfig<TContext>>());
    }

    private static WebApplication MapDynamicCrudEndpointsCore<TContext>(WebApplication webApp, DynamicEndpointConfig<TContext> config)
        where TContext : DbContext
    {
        var defaults = config.Defaults;
        var allEntityTypes = DynamicEndpointMapper.GetEntityTypesFromDbContext<TContext>();
        var entityTypes = defaults.IncludedTypes.Count > 0 ? defaults.IncludedTypes : allEntityTypes;
        using var scope = webApp.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TContext>>();
        using var context = factory.CreateDbContext();
        var registry = new Dictionary<string, EntityEndpointMetadata<TContext>>(StringComparer.OrdinalIgnoreCase);
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
            var (adaptedPatchBefore, adaptedPatchAfter) = AdaptPatchDelegates<TContext>(entityType, patchCfg);
            registry[entityType.Name] = new(
                entityType, keyType, keyName, defaultOrder, entityCache, patchCfg?.PropertyAuthorization, adaptedPatchBefore, adaptedPatchAfter);
        }

        var baseRoute = defaults.BaseRoute.TrimEnd('/');
        var routePrefix = string.IsNullOrEmpty(baseRoute) ? "" : baseRoute + "/";
        var entityRoute = $"{routePrefix}{{entityType}}";
        var metadataRoute = $"{routePrefix}Metadata";
        var jsonOptions = webApp.Services.GetService<IOptions<JsonOptions>>()?.Value.SerializerOptions ?? new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var metadata = BuildMetadata<TContext>(registry);
        webApp.MapGet(metadataRoute, () => Results.Json(metadata)).WithTags("Dynamic").Produces<CrudMetadataResponse>();
        webApp.MapGet($"{entityRoute}/Metadata", ([FromRoute] string entityType, HttpContext httpContext) => HandleGetEntityMetadata<TContext>(registry, entityType, httpContext))
            .WithTags("Dynamic")
            .Produces<EntityTypeMetadata>()
            .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound);

        if (defaults.Features.HasFlag(ApiFeatureFlag.Export)) {
            webApp.MapPost(
                    $"{entityRoute}/Export",
                    async (
                        [FromRoute] string entityType,
                        [FromBody] ExportRequest request,
                        [FromServices] IExportService<TContext> exportService,
                        HttpContext httpContext,
                        CancellationToken ct) => await HandleExport(registry, config, entityType, request, exportService, httpContext, SortDirection.Desc, ct))
                .WithTags("Dynamic")
                .Produces(StatusCodes.Status200OK)
                .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound);
        }

        if (defaults.Features.HasFlag(ApiFeatureFlag.Query)) {
            webApp.MapPost(
                    $"{entityRoute}/Query",
                    async (
                        [FromRoute] string entityType,
                        [FromBody] QueryReq queryRequest,
                        [FromServices] IQueryService<TContext> queryService,
                        HttpContext httpContext,
                        CancellationToken ct) => await HandleQuery(registry, entityType, queryRequest, queryService, httpContext, SortDirection.Desc, ct))
                .WithTags("Dynamic")
                .Produces<QueryRes<object>>()
                .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound);

            var enableComputedFields = defaults.Features.HasFlag(ApiFeatureFlag.ProjectionComputedFields);
            webApp.MapPost(
                    $"{entityRoute}/QueryProject", async (
                        [FromRoute] string entityType,
                        [FromBody] ProjectionQueryReq queryRequest,
                        [FromServices] IQueryService<TContext> queryService,
                        HttpContext httpContext,
                        CancellationToken ct) => {
                        if (!enableComputedFields && queryRequest.ComputedFields.Count > 0) {
                            var error = ApiErrorResponseFactory.CreateForError(
                                httpContext,
                                LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, "Computed fields are not enabled. Enable via ApiFeatureFlag.ProjectionComputedFields.", DateTime.UtcNow));

                            return Results.Json(error, statusCode: error.Status);
                        }

                        return await HandleQueryProjected(registry, entityType, queryRequest, queryService, httpContext, SortDirection.Desc, ct);
                    })
                .WithTags("Dynamic")
                .Produces<ProjectedQueryRes<object?>>()
                .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound);
        }

        if (defaults.Features.HasFlag(ApiFeatureFlag.Get)) {
            webApp.MapGet(
                    $"{entityRoute}/{{id}}",
                    async (
                        [FromRoute] string entityType,
                        [FromRoute] string id,
                        [FromQuery] string[] include,
                        [FromServices] IQueryService<TContext> queryService,
                        HttpContext httpContext,
                        CancellationToken ct) => await HandleGet(registry, entityType, id, include, queryService, httpContext, ct))
                .WithTags("Dynamic")
                .Produces<object>()
                .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound);
        }

        if (defaults.Features.HasFlag(ApiFeatureFlag.Create)) {
            webApp.MapPost(
                    $"{entityRoute}",
                    async ([FromRoute] string entityType, HttpRequest request, [FromServices] ICreateService<TContext> createService, HttpContext httpContext, CancellationToken ct)
                        => await HandleCreate(registry, entityType, request, createService, httpContext, jsonOptions, ct))
                .WithTags("Dynamic")
                .Produces(StatusCodes.Status201Created)
                .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound);

            if (defaults.Features.HasFlag(ApiFeatureFlag.CreateBulk)) {
                webApp.MapPost(
                        $"{entityRoute}/Bulk",
                        async (
                                [FromRoute] string entityType,
                                HttpRequest request,
                                [FromServices] ICreateService<TContext> createService,
                                HttpContext httpContext,
                                CancellationToken ct)
                            => await HandleCreateBulk(registry, entityType, request, createService, httpContext, jsonOptions, ct))
                    .WithTags("Dynamic")
                    .Produces<CreateBulkResult<object>>()
                    .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                    .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound);
            }
        }

        if (defaults.Features.HasFlag(ApiFeatureFlag.Patch)) {
            webApp.MapPatch(
                    $"{entityRoute}",
                    async (
                        [FromRoute] string entityType,
                        [FromBody] PatchRequest patchRequest,
                        [FromServices] IPatchService<TContext> patchService,
                        HttpContext httpContext,
                        CancellationToken ct) => await HandlePatch(registry, entityType, patchRequest, patchService, httpContext, ct))
                .WithTags("Dynamic")
                .Produces<PatchResult<object>>()
                .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound);

            if (defaults.Features.HasFlag(ApiFeatureFlag.PatchBulk)) {
                webApp.MapPatch(
                        $"{entityRoute}/Bulk",
                        async (
                            [FromRoute] string entityType,
                            [FromBody] List<PatchRequest> requests,
                            [FromServices] IPatchService<TContext> patchService,
                            HttpContext httpContext,
                            CancellationToken ct) => await HandlePatchBulk(registry, entityType, requests, patchService, httpContext, ct))
                    .WithTags("Dynamic")
                    .Produces<PatchBulkResult<object>>()
                    .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                    .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound);
            }
        }

        if (defaults.Features.HasFlag(ApiFeatureFlag.Update)) {
            webApp.MapPost(
                    $"{entityRoute}/Update",
                    async (
                        [FromRoute] string entityType,
                        [FromBody] JsonNode? body,
                        [FromServices] IUpdateService<TContext> updateService,
                        HttpContext httpContext,
                        CancellationToken ct) => await HandleUpdate(registry, entityType, body, updateService, httpContext, jsonOptions, ct))
                .WithTags("Dynamic")
                .Produces<UpdateResult<object>>()
                .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound);

            if (defaults.Features.HasFlag(ApiFeatureFlag.UpdateBulk)) {
                webApp.MapPost(
                        $"{entityRoute}/Bulk/Update",
                        async (
                            [FromRoute] string entityType,
                            [FromBody] JsonNode? body,
                            [FromServices] IUpdateService<TContext> updateService,
                            HttpContext httpContext,
                            CancellationToken ct) => await HandleUpdateBulk(registry, entityType, body, updateService, httpContext, jsonOptions, ct))
                    .WithTags("Dynamic")
                    .Produces<UpdateBulkResult<object>>()
                    .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                    .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound);
            }
        }

        if (defaults.Features.HasFlag(ApiFeatureFlag.Upsert)) {
            webApp.MapPost(
                    $"{entityRoute}/Upsert",
                    async (
                        [FromRoute] string entityType,
                        [FromBody] JsonNode? body,
                        [FromServices] IUpsertService<TContext> upsertService,
                        HttpContext httpContext,
                        CancellationToken ct) => await HandleUpsert(registry, entityType, body, upsertService, httpContext, jsonOptions, ct))
                .WithTags("Dynamic")
                .Produces<UpsertResult<object>>()
                .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound);

            if (defaults.Features.HasFlag(ApiFeatureFlag.UpsertBulk)) {
                webApp.MapPost(
                        $"{entityRoute}/Bulk/Upsert",
                        async (
                            [FromRoute] string entityType,
                            [FromBody] JsonNode? body,
                            [FromServices] IUpsertService<TContext> upsertService,
                            HttpContext httpContext,
                            CancellationToken ct) => await HandleUpsertBulk(registry, entityType, body, upsertService, httpContext, jsonOptions, ct))
                    .WithTags("Dynamic")
                    .Produces<UpsertBulkResult<object>>()
                    .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                    .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound);
            }
        }

        if (defaults.Features.HasFlag(ApiFeatureFlag.Delete)) {
            webApp.MapDelete(
                    $"{entityRoute}",
                    async (
                        [FromRoute] string entityType,
                        [FromBody] DeleteRequest deleteRequest,
                        [FromServices] IDeleteService<TContext> deleteService,
                        HttpContext httpContext,
                        CancellationToken ct) => await HandleDeleteByRequest(registry, entityType, deleteRequest, deleteService, httpContext, ct))
                .WithTags("Dynamic")
                .Produces<DeleteResult<object>>()
                .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound);

            webApp.MapDelete(
                    $"{entityRoute}/{{id}}",
                    async (
                            [FromRoute] string entityType,
                            [FromRoute] string id,
                            [FromServices] IDeleteService<TContext> deleteService,
                            HttpContext httpContext,
                            CancellationToken ct)
                        => await HandleDelete(registry, entityType, id, deleteService, httpContext, ct))
                .WithTags("Dynamic")
                .Produces<DeleteResult<object>>()
                .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound);

            if (defaults.Features.HasFlag(ApiFeatureFlag.DeleteBulk)) {
                webApp.MapDelete(
                        $"{entityRoute}/Bulk",
                        async (
                            [FromRoute] string entityType,
                            [FromBody] List<DeleteRequest> requests,
                            [FromServices] IDeleteService<TContext> deleteService,
                            HttpContext httpContext,
                            CancellationToken ct) => await HandleDeleteBulk(registry, entityType, requests, deleteService, httpContext, ct))
                    .WithTags("Dynamic")
                    .Produces<DeleteBulkResult<object>>()
                    .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
                    .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound);
            }
        }

        return webApp;
    }

    private static IResult HandleGetEntityMetadata<TContext>(IReadOnlyDictionary<string, EntityEndpointMetadata<TContext>> registry, string entityType, HttpContext httpContext)
        where TContext : DbContext
    {
        if (!TryGetMetadata<TContext>(registry, entityType, out var meta))
            return Results.Json(ApiErrorResponseFactory.CreateNotFound(httpContext, null, $"Unknown entity type: {entityType}"), statusCode: 404);

        var entityMetadata = ToEntityTypeMetadata<TContext>(meta);
        return Results.Json(entityMetadata);
    }

    private static EntityTypeMetadata ToEntityTypeMetadata<TContext>(EntityEndpointMetadata<TContext> m)
        where TContext : DbContext
    {
        var properties = m.EntityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .Select(ToPropertyMetadata)
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToList();

        return new(m.EntityType.Name, m.KeyPropertyName, m.KeyType.Name, properties);
    }

    private static CrudMetadataResponse BuildMetadata<TContext>(IReadOnlyDictionary<string, EntityEndpointMetadata<TContext>> registry)
        where TContext : DbContext
    {
        var entityTypes = registry.Values.Select(m => ToEntityTypeMetadata<TContext>(m)).ToList();
        return new(entityTypes);
    }

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

        Delegate? before = patchConfig.Before == null ? null : (Delegate)adapter.Invoke(null, [patchConfig.Before])!;
        Delegate? after = patchConfig.After == null ? null : (Delegate)adapter.Invoke(null, [patchConfig.After])!;
        return (before, after);
    }

    private static Action<PatchContext<TEntity, TContext>> AdaptPatchDelegate<TEntity, TContext>(Action<PatchContext<object, TContext>> inner)
        where TEntity : class where TContext : DbContext
        => ctx => inner(new PatchContext<object, TContext>(ctx.Request, ctx.Entity, ctx.DbContext, ctx.Services));

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
            typeof(Task<>).MakeGenericType(upsertBulkResultType).GetProperty("Result")!,
            createResultType.GetProperty("IsSuccess")!, createResultType.GetProperty("Data")!, createResultType.GetProperty("Error")!,
            entityType.GetProperty(keyPropertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)!, patchResultType.GetProperty("IsSuccess")!,
            patchResultType.GetProperty("Error")!, updateResultType.GetProperty("Result")!, updateResultType.GetProperty("Error")!);
    }

    private static bool TryGetMetadata<TContext>(IReadOnlyDictionary<string, EntityEndpointMetadata<TContext>> registry, string entityType, out EntityEndpointMetadata<TContext> meta)
        where TContext : DbContext
    {
        if (registry.TryGetValue(entityType, out meta!))
            return true;

        meta = null!;
        return false;
    }

    private static object ParseKey(string id, Type keyType)
    {
        if (keyType == typeof(Guid))
            return Guid.Parse(id);

        if (keyType == typeof(int))
            return int.Parse(id);

        if (keyType == typeof(long))
            return long.Parse(id);

        if (keyType == typeof(string))
            return id;

        return Convert.ChangeType(id, keyType);
    }

    /// <summary>Sets the key on UpdateRequest.Data from Keys so Mapster does not overwrite with default (Guid.Empty) and trigger EF key-modification error.</summary>
    private static void EnsureKeyOnUpdateData<TContext>(object request, EntityEndpointMetadata<TContext> meta)
        where TContext : DbContext
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

    /// <summary>Sets the key on UpsertRequest.NewData from Keys or Query (when ConditionClause on key property), so Mapster does not overwrite with default.</summary>
    private static void EnsureKeyOnUpsertData<TContext>(object request, EntityEndpointMetadata<TContext> meta)
        where TContext : DbContext
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

    private static async Task<IResult> HandleQuery<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata<TContext>> registry,
        string entityType,
        QueryReq queryRequest,
        IQueryService<TContext> queryService,
        HttpContext httpContext,
        SortDirection defaultSortDirection,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata<TContext>(registry, entityType, out var meta))
            return Results.Json(ApiErrorResponseFactory.CreateNotFound(httpContext, null, $"Unknown entity type: {entityType}"), statusCode: 404);

        var task = (Task)meta.Cache.Query.Invoke(queryService, [queryRequest, meta.DefaultOrder, defaultSortDirection, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.QueryTaskResultProperty.GetValue(task);
        return Results.Json(result);
    }

    private static async Task<IResult> HandleQueryProjected<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata<TContext>> registry,
        string entityType,
        ProjectionQueryReq queryRequest,
        IQueryService<TContext> queryService,
        HttpContext httpContext,
        SortDirection defaultSortDirection,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata<TContext>(registry, entityType, out var meta))
            return Results.Json(ApiErrorResponseFactory.CreateNotFound(httpContext, null, $"Unknown entity type: {entityType}"), statusCode: 404);

        var task = (Task)meta.Cache.QueryProjected.Invoke(queryService, [queryRequest, meta.DefaultOrder, defaultSortDirection, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.QueryProjectedTaskResultProperty.GetValue(task);
        return Results.Json(result);
    }

    private static async Task<IResult> HandleGet<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata<TContext>> registry,
        string entityType,
        string id,
        string[] include,
        IQueryService<TContext> queryService,
        HttpContext httpContext,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata<TContext>(registry, entityType, out var meta))
            return Results.Json(ApiErrorResponseFactory.CreateNotFound(httpContext, null, $"Unknown entity type: {entityType}"), statusCode: 404);

        object key;
        try {
            key = ParseKey(id, meta.KeyType);
        }
        catch (Exception ex) {
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, ex.Message, DateTime.UtcNow)), statusCode: 400);
        }

        try {
            var task = (Task)meta.Cache.Get.Invoke(queryService, [new[] { key }, include, null, null, ct])!;
            await task.ConfigureAwait(false);
            var result = meta.Cache.GetTaskResultProperty.GetValue(task);
            return result != null ? Results.Ok(result) : Results.Json(ApiErrorResponseFactory.CreateNotFound(httpContext, [key]), statusCode: 404);
        }
        catch (ApiErrorException ex) {
            var error = ApiErrorResponseFactory.CreateForError(httpContext, ex.ProblemDetails);
            return Results.Json(error, statusCode: error.Status);
        }
    }

    private static async Task<IResult> HandleCreate<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata<TContext>> registry,
        string entityType,
        HttpRequest request,
        ICreateService<TContext> createService,
        HttpContext httpContext,
        JsonSerializerOptions jsonOptions,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata<TContext>(registry, entityType, out var meta))
            return Results.Json(ApiErrorResponseFactory.CreateNotFound(httpContext, null, $"Unknown entity type: {entityType}"), statusCode: 404);

        object? body;
        try {
            body = await JsonSerializer.DeserializeAsync(request.Body, meta.EntityType, jsonOptions, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, ex.Message, DateTime.UtcNow)), statusCode: 400);
        }

        if (body == null)
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, "Request body is required", DateTime.UtcNow)), statusCode: 400);

        var task = (Task)meta.Cache.CreateAsync.Invoke(createService, [body, meta.Cache.BeforeCreateDelegate, null, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.CreateTaskResultProperty.GetValue(task);
        var isSuccess = meta.Cache.CreateResultIsSuccessProperty.GetValue(result);
        if (Equals(isSuccess, true)) {
            var data = meta.Cache.CreateResultDataProperty.GetValue(result);
            var id = data != null ? meta.Cache.CreateResultKeyProperty.GetValue(data) : null;
            return Results.Created($"{request.Path}/{id}", result);
        }

        var error = meta.Cache.CreateResultErrorProperty.GetValue(result);
        return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, (LyoProblemDetails)error!), statusCode: 400);
    }

    private static async Task<IResult> HandleCreateBulk<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata<TContext>> registry,
        string entityType,
        HttpRequest request,
        ICreateService<TContext> createService,
        HttpContext httpContext,
        JsonSerializerOptions jsonOptions,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata<TContext>(registry, entityType, out var meta))
            return Results.Json(ApiErrorResponseFactory.CreateNotFound(httpContext, null, $"Unknown entity type: {entityType}"), statusCode: 404);

        var listType = typeof(List<>).MakeGenericType(meta.EntityType);
        object? body;
        try {
            body = await JsonSerializer.DeserializeAsync(request.Body, listType, jsonOptions, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, ex.Message, DateTime.UtcNow)), statusCode: 400);
        }

        if (body == null)
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, "Request body is required", DateTime.UtcNow)), statusCode: 400);

        var task = (Task)meta.Cache.CreateBulkAsync.Invoke(createService, [body, meta.Cache.BeforeCreateDelegate, null, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.CreateBulkTaskResultProperty.GetValue(task);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandlePatch<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata<TContext>> registry,
        string entityType,
        PatchRequest patchRequest,
        IPatchService<TContext> patchService,
        HttpContext httpContext,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata<TContext>(registry, entityType, out var meta))
            return Results.Json(ApiErrorResponseFactory.CreateNotFound(httpContext, null, $"Unknown entity type: {entityType}"), statusCode: 404);

        var fieldAuth = await PatchPropertyAuthorizationApplier
            .ApplyAsync(meta.PatchPropertyAuthorization, httpContext, meta.EntityType, patchRequest, ct)
            .ConfigureAwait(false);

        if (!fieldAuth.Success) {
            var err = ApiErrorResponseFactory.CreateForError(httpContext, fieldAuth.Error);
            return Results.Json(err, statusCode: fieldAuth.Error!.Status);
        }

        patchRequest = fieldAuth.Request!;
        var task = (Task)meta.Cache.PatchAsync.Invoke(patchService, [patchRequest, meta.AdaptedPatchBefore, meta.AdaptedPatchAfter, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.PatchTaskResultProperty.GetValue(task);
        var isSuccess = meta.Cache.PatchResultIsSuccessProperty.GetValue(result);
        if (Equals(isSuccess, true))
            return Results.Ok(result);

        var error = meta.Cache.PatchResultErrorProperty.GetValue(result);
        return error != null ? Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, (LyoProblemDetails)error), statusCode: 404) : Results.Ok(result);
    }

    private static async Task<IResult> HandlePatchBulk<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata<TContext>> registry,
        string entityType,
        List<PatchRequest> requests,
        IPatchService<TContext> patchService,
        HttpContext httpContext,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata<TContext>(registry, entityType, out var meta))
            return Results.Json(ApiErrorResponseFactory.CreateNotFound(httpContext, null, $"Unknown entity type: {entityType}"), statusCode: 404);

        if (requests == null || requests.Count == 0) {
            return Results.Json(
                ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, "At least one patch request is required", DateTime.UtcNow)), statusCode: 400);
        }

        if (meta.PatchPropertyAuthorization != null) {
            var sanitized = new List<PatchRequest>(requests.Count);
            foreach (var pr in requests) {
                var fieldAuth = await PatchPropertyAuthorizationApplier
                    .ApplyAsync(meta.PatchPropertyAuthorization, httpContext, meta.EntityType, pr, ct)
                    .ConfigureAwait(false);

                if (!fieldAuth.Success) {
                    var err = ApiErrorResponseFactory.CreateForError(httpContext, fieldAuth.Error);
                    return Results.Json(err, statusCode: fieldAuth.Error!.Status);
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
        IReadOnlyDictionary<string, EntityEndpointMetadata<TContext>> registry,
        string entityType,
        JsonNode? body,
        IUpdateService<TContext> updateService,
        HttpContext httpContext,
        JsonSerializerOptions jsonOptions,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (body == null)
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, "Request body is required", DateTime.UtcNow)), statusCode: 400);

        if (!TryGetMetadata<TContext>(registry, entityType, out var meta))
            return Results.Json(ApiErrorResponseFactory.CreateNotFound(httpContext, null, $"Unknown entity type: {entityType}"), statusCode: 404);

        var updateRequestType = typeof(UpdateRequest<>).MakeGenericType(meta.EntityType);
        object? request;
        try {
            request = body.Deserialize(updateRequestType, jsonOptions);
        }
        catch (Exception ex) {
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, ex.Message, DateTime.UtcNow)), statusCode: 400);
        }

        if (request == null)
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, "Request body is required", DateTime.UtcNow)), statusCode: 400);

        EnsureKeyOnUpdateData<TContext>(request, meta);
        var task = (Task)meta.Cache.UpdateAsync.Invoke(updateService, [request, null, null, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.UpdateTaskResultProperty.GetValue(task);
        var resultEnum = meta.Cache.UpdateResultResultProperty.GetValue(result);
        if (resultEnum is UpdateResultEnum.Failed) {
            var error = meta.Cache.UpdateResultErrorProperty.GetValue(result);
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, (LyoProblemDetails)error!), statusCode: 404);
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUpdateBulk<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata<TContext>> registry,
        string entityType,
        JsonNode? body,
        IUpdateService<TContext> updateService,
        HttpContext httpContext,
        JsonSerializerOptions jsonOptions,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (body == null)
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, "Request body is required", DateTime.UtcNow)), statusCode: 400);

        if (!TryGetMetadata<TContext>(registry, entityType, out var meta))
            return Results.Json(ApiErrorResponseFactory.CreateNotFound(httpContext, null, $"Unknown entity type: {entityType}"), statusCode: 404);

        var listType = typeof(List<>).MakeGenericType(typeof(UpdateRequest<>).MakeGenericType(meta.EntityType));
        object? requests;
        try {
            requests = body.Deserialize(listType, jsonOptions);
        }
        catch (Exception ex) {
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, ex.Message, DateTime.UtcNow)), statusCode: 400);
        }

        if (requests == null)
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, "Request body is required", DateTime.UtcNow)), statusCode: 400);

        foreach (var req in (IEnumerable)requests)
            EnsureKeyOnUpdateData<TContext>(req!, meta);

        var task = (Task)meta.Cache.UpdateBulkAsync.Invoke(updateService, [requests, null, null, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.UpdateBulkTaskResultProperty.GetValue(task);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUpsert<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata<TContext>> registry,
        string entityType,
        JsonNode? body,
        IUpsertService<TContext> upsertService,
        HttpContext httpContext,
        JsonSerializerOptions jsonOptions,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (body == null)
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, "Request body is required", DateTime.UtcNow)), statusCode: 400);

        if (!TryGetMetadata<TContext>(registry, entityType, out var meta))
            return Results.Json(ApiErrorResponseFactory.CreateNotFound(httpContext, null, $"Unknown entity type: {entityType}"), statusCode: 404);

        var upsertRequestType = typeof(UpsertRequest<>).MakeGenericType(meta.EntityType);
        object? request;
        try {
            request = body.Deserialize(upsertRequestType, jsonOptions);
        }
        catch (Exception ex) {
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, ex.Message, DateTime.UtcNow)), statusCode: 400);
        }

        if (request == null)
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, "Request body is required", DateTime.UtcNow)), statusCode: 400);

        EnsureKeyOnUpsertData<TContext>(request, meta);
        var task = (Task)meta.Cache.UpsertAsync.Invoke(upsertService, [request, null, null, null, null, null, null, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.UpsertTaskResultProperty.GetValue(task);
        var resultEnum = result?.GetType().GetProperty("Result")?.GetValue(result);
        if (resultEnum is UpsertResultEnum.Failed) {
            var error = result?.GetType().GetProperty("Error")?.GetValue(result);
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, (LyoProblemDetails)error!), statusCode: 500);
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUpsertBulk<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata<TContext>> registry,
        string entityType,
        JsonNode? body,
        IUpsertService<TContext> upsertService,
        HttpContext httpContext,
        JsonSerializerOptions jsonOptions,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (body == null)
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, "Request body is required", DateTime.UtcNow)), statusCode: 400);

        if (!TryGetMetadata<TContext>(registry, entityType, out var meta))
            return Results.Json(ApiErrorResponseFactory.CreateNotFound(httpContext, null, $"Unknown entity type: {entityType}"), statusCode: 404);

        var listType = typeof(List<>).MakeGenericType(typeof(UpsertRequest<>).MakeGenericType(meta.EntityType));
        object? requests;
        try {
            requests = body.Deserialize(listType, jsonOptions);
        }
        catch (Exception ex) {
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, ex.Message, DateTime.UtcNow)), statusCode: 400);
        }

        if (requests == null)
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, "Request body is required", DateTime.UtcNow)), statusCode: 400);

        foreach (var req in (IEnumerable)requests)
            EnsureKeyOnUpsertData<TContext>(req!, meta);

        var task = (Task)meta.Cache.UpsertBulkAsync.Invoke(upsertService, [requests, null, null, null, null, null, null, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.UpsertBulkTaskResultProperty.GetValue(task);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDeleteByRequest<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata<TContext>> registry,
        string entityType,
        DeleteRequest deleteRequest,
        IDeleteService<TContext> deleteService,
        HttpContext httpContext,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata<TContext>(registry, entityType, out var meta))
            return Results.Json(ApiErrorResponseFactory.CreateNotFound(httpContext, null, $"Unknown entity type: {entityType}"), statusCode: 404);

        if (deleteRequest == null) {
            return Results.Json(
                ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, "Delete request body is required", DateTime.UtcNow)), statusCode: 400);
        }

        var task = (Task)meta.Cache.DeleteByRequestAsync.Invoke(deleteService, [deleteRequest, null, null, null, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.DeleteTaskResultProperty.GetValue(task);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDelete<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata<TContext>> registry,
        string entityType,
        string id,
        IDeleteService<TContext> deleteService,
        HttpContext httpContext,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata<TContext>(registry, entityType, out var meta))
            return Results.Json(ApiErrorResponseFactory.CreateNotFound(httpContext, null, $"Unknown entity type: {entityType}"), statusCode: 404);

        object key;
        try {
            key = ParseKey(id, meta.KeyType);
        }
        catch (Exception ex) {
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, ex.Message, DateTime.UtcNow)), statusCode: 400);
        }

        var task = (Task)meta.Cache.DeleteAsync.Invoke(deleteService, [new[] { key }, null, null, null, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.DeleteTaskResultProperty.GetValue(task);
        var error = result?.GetType().GetProperty("Error")?.GetValue(result);
        if (error is LyoProblemDetails apiError)
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, apiError), statusCode: apiError.Status);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDeleteBulk<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata<TContext>> registry,
        string entityType,
        List<DeleteRequest> requests,
        IDeleteService<TContext> deleteService,
        HttpContext httpContext,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata<TContext>(registry, entityType, out var meta))
            return Results.Json(ApiErrorResponseFactory.CreateNotFound(httpContext, null, $"Unknown entity type: {entityType}"), statusCode: 404);

        if (requests == null || requests.Count == 0) {
            return Results.Json(
                ApiErrorResponseFactory.CreateForError(httpContext, LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, "At least one delete request is required", DateTime.UtcNow)), statusCode: 400);
        }

        var task = (Task)meta.Cache.DeleteBulkAsync.Invoke(deleteService, [requests, null, null, null, ct])!;
        await task.ConfigureAwait(false);
        var result = meta.Cache.DeleteBulkTaskResultProperty.GetValue(task);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleExport<TContext>(
        IReadOnlyDictionary<string, EntityEndpointMetadata<TContext>> registry,
        DynamicEndpointConfig<TContext> config,
        string entityType,
        ExportRequest request,
        IExportService<TContext> exportService,
        HttpContext httpContext,
        SortDirection defaultSortDirection,
        CancellationToken ct)
        where TContext : DbContext
    {
        if (!TryGetMetadata<TContext>(registry, entityType, out var meta))
            return Results.Json(ApiErrorResponseFactory.CreateNotFound(httpContext, null, $"Unknown entity type: {entityType}"), statusCode: 404);

        if (!config.GetConfig(meta.EntityType).Features.HasFlag(ApiFeatureFlag.Export))
            return Results.Json(ApiErrorResponseFactory.CreateNotFound(httpContext, null, $"Export not enabled for {entityType}"), statusCode: 404);

        try {
            var exportMethod = typeof(IExportService<TContext>).GetMethod(nameof(IExportService<TContext>.ExportAsync))!.MakeGenericMethod(meta.EntityType, meta.EntityType);
            var task = (Task)exportMethod.Invoke(exportService, [request, meta.DefaultOrder, defaultSortDirection, ct])!;
            await task.ConfigureAwait(false);
            var result = task.GetType().GetProperty("Result")!.GetValue(task);
            var (stream, contentType, fileName) = ((Stream, string, string))result!;
            return Results.File(stream, contentType, fileName);
        }
        catch (ApiErrorException ex) {
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, ex.ProblemDetails), statusCode: ex.ProblemDetails.Status);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ApiErrorException inner) {
            return Results.Json(ApiErrorResponseFactory.CreateForError(httpContext, inner.ProblemDetails), statusCode: inner.ProblemDetails.Status);
        }
    }
}