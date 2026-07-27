using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.Export;
using Lyo.Common.Identifiers;
using Lyo.Exceptions;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Rendering;
using Lyo.Reporting.Models.Request;
using Lyo.Reporting.Models.Response;
using Lyo.Reporting.Postgres;
using Lyo.Reporting.Postgres.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Constants = Lyo.Reporting.Models.Constants;

namespace Lyo.Api.Reporting;

/// <summary>Maps reporting HTTP endpoints with optional per-surface <see cref="EndpointAuth"/>.</summary>
public static class Extensions
{
    /// <summary>
    /// Sensitive parameter columns that projected queries and exports may never select:
    /// response mapping masks <c>Value</c>/<c>EncryptedValue</c>, but projections read raw entities.
    /// </summary>
    private static readonly string[] ParameterDeniedSelectFields = ["EncryptedValue", "Value"];

    /// <summary>Registers Export contributor for ReportingContext (call once on the API host).</summary>
    public static IServiceCollection AddLyoApiReporting(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.AddLyoApiExport<ReportingContext>();
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        return services;
    }

    /// <summary>
    /// One-call host registration for the reporting API: <c>AddPostgresReportingManagement</c>
    /// (DbContext factory, migrations, CRUD services, renderers, <c>ReportService</c>, retention, throttle)
    /// plus <see cref="AddLyoApiReporting"/>. Map endpoints afterwards with <see cref="BuildReportingGroup"/>.
    /// Persist outputs via <c>AddReportingGenerationHooks</c>; opt into the background sweeper with
    /// <c>AddReportingMaintenanceWorker</c>.
    /// </summary>
    public static IServiceCollection AddReportingApi(this IServiceCollection services, Action<PostgresReportingOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        var options = new PostgresReportingOptions();
        configure(options);
        return services.AddReportingApi(options);
    }

    /// <inheritdoc cref="AddReportingApi(IServiceCollection, Action{PostgresReportingOptions})"/>
    public static IServiceCollection AddReportingApi(this IServiceCollection services, PostgresReportingOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(options);
        services.AddPostgresReportingManagement(options);
        services.AddLyoApiReporting();
        return services;
    }

    /// <summary>
    /// <see cref="AddReportingApi(IServiceCollection, PostgresReportingOptions)"/> with options bound from
    /// <paramref name="configuration"/> (section <see cref="PostgresReportingOptions.SectionName"/> by default).
    /// </summary>
    public static IServiceCollection AddReportingApiFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresReportingOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        var options = new PostgresReportingOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddReportingApi(options);
    }

    /// <summary>
    /// Maps Definition CRUD (+ Export), Definition Parameter CRUD, Generation read-only, Generate, Rerun,
    /// and (when <see cref="ReportingApiOptions.DownloadStreamFactory"/> is set) Download.
    /// Call after <c>AddPostgresReportingManagement</c> and <see cref="AddLyoApiReporting"/>.
    /// </summary>
    public static WebApplication BuildReportingGroup(this WebApplication app, ReportingApiOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(app);
        options ??= new ReportingApiOptions();

        app.CreateBuilder<ReportingContext, ReportDefinition, ReportDefinitionReq, ReportDefinitionRes, Guid>(
                Constants.Rest.Reporting.Definitions, "Reporting")
            .WithCrud(
                ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance,
                new() {
                    QueryAuth = options.DefinitionAuth,
                    GetAuth = options.DefinitionAuth,
                    CreateAuth = options.DefinitionAuth,
                    CreateBulkAuth = options.DefinitionAuth,
                    UpdateAuth = options.DefinitionAuth,
                    UpdateBulkAuth = options.DefinitionAuth,
                    PatchAuth = options.DefinitionAuth,
                    PatchBulkAuth = options.DefinitionAuth,
                    UpsertAuth = options.DefinitionAuth,
                    UpsertBulkAuth = options.DefinitionAuth,
                    DeleteAuth = options.DefinitionAuth,
                    DeleteBulkAuth = options.DefinitionAuth,
                    ExportAuth = options.DefinitionAuth,
                    MetadataAuth = options.DefinitionAuth,
                    DeniedSelectFields = ParameterDeniedSelectFields,
                    DeleteIncludes = ["Generations"],
                    BeforeCreate = ctx => {
                        ctx.Entity.Id = LyoGuid.CreateCombPostgres();
                        var now = DateTime.UtcNow;
                        ctx.Entity.CreatedTimestamp = now;
                        ctx.Entity.UpdatedTimestamp = now;
                        var actor = ReportAuditHelper.GetActor(ctx.Services);
                        ctx.Entity.CreatedBy = actor?.EntityId;
                        foreach (var parameter in ctx.Entity.Parameters) {
                            if (parameter.Id == default)
                                parameter.Id = LyoGuid.CreateCombPostgres();
                            parameter.ReportDefinitionId = ctx.Entity.Id;
                            parameter.CreatedTimestamp = now;
                        }

                        ValidateDefinition(ctx.Entity, ctx.Services);
                    },
                    AfterCreate = ctx => ReportAuditHelper.RecordCreated(ctx.Services, "ReportDefinition", ctx.Entity.Id),
                    BeforeUpdate = ctx => {
                        ctx.Entity.UpdatedTimestamp = DateTime.UtcNow;
                        ValidateDefinition(ctx.Entity, ctx.Services);
                    },
                    AfterUpdate = ctx => ReportAuditHelper.RecordUpdated(ctx.Services, "ReportDefinition", ctx.Entity.Id),
                    BeforePatch = ctx => {
                        ctx.Entity.UpdatedTimestamp = DateTime.UtcNow;
                        ValidateDefinition(ctx.Entity, ctx.Services);
                    },
                    AfterPatch = ctx => ReportAuditHelper.RecordUpdated(ctx.Services, "ReportDefinition", ctx.Entity.Id),
                    // Cascade delete removes generation rows but not host-persisted outputs; give the host's
                    // OnCleanupAsync hook a chance to delete each stored blob first. A hook failure aborts the delete.
                    BeforeDeleteAsync = (ctx, ct) => ReportGenerationCleanup.InvokeCleanupHooksAsync(
                            ctx.Entity.Generations,
                            ctx.Services.GetService<ReportGenerationHooks>(),
                            ctx.Services,
                            ct)
                        .AsTask()
                })
            .Build();

        app.CreateBuilder<ReportingContext, ReportDefinitionParameter, ReportDefinitionParameterReq, ReportDefinitionParameterRes, Guid>(
                Constants.Rest.Reporting.DefinitionParameters, "Reporting")
            .WithCrud(
                ApiFeatureSet.DefaultCrud,
                new() {
                    QueryAuth = options.DefinitionAuth,
                    GetAuth = options.DefinitionAuth,
                    CreateAuth = options.DefinitionAuth,
                    CreateBulkAuth = options.DefinitionAuth,
                    UpdateAuth = options.DefinitionAuth,
                    UpdateBulkAuth = options.DefinitionAuth,
                    PatchAuth = options.DefinitionAuth,
                    PatchBulkAuth = options.DefinitionAuth,
                    UpsertAuth = options.DefinitionAuth,
                    UpsertBulkAuth = options.DefinitionAuth,
                    DeleteAuth = options.DefinitionAuth,
                    DeleteBulkAuth = options.DefinitionAuth,
                    MetadataAuth = options.DefinitionAuth,
                    DeniedSelectFields = ParameterDeniedSelectFields,
                    BeforeCreate = ctx => {
                        ctx.Entity.Id = LyoGuid.CreateCombPostgres();
                        ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
                        ReportDefinitionWriteValidator.ValidateParameter(ctx.Entity);
                    },
                    AfterCreate = ctx => ReportAuditHelper.RecordCreated(ctx.Services, "ReportDefinitionParameter", ctx.Entity.Id),
                    BeforeUpdate = ctx => {
                        ctx.Entity.UpdatedTimestamp = DateTime.UtcNow;
                        ReportDefinitionWriteValidator.ValidateParameter(ctx.Entity);
                    },
                    AfterUpdate = ctx => ReportAuditHelper.RecordUpdated(ctx.Services, "ReportDefinitionParameter", ctx.Entity.Id),
                    BeforePatch = ctx => {
                        ctx.Entity.UpdatedTimestamp = DateTime.UtcNow;
                        ReportDefinitionWriteValidator.ValidateParameter(ctx.Entity);
                    }
                })
            .Build();

        app.CreateBuilder<ReportingContext, ReportGeneration, ReportGenerationReq, ReportGenerationRes, Guid>(
                Constants.Rest.Reporting.Generations, "Reporting")
            .WithCrud(
                ApiFeatureSet.ReadOnly,
                new() {
                    QueryAuth = options.GenerationAuth,
                    GetAuth = options.GenerationAuth,
                    MetadataAuth = options.GenerationAuth,
                    DeniedSelectFields = ParameterDeniedSelectFields
                })
            .Build();

        var generate = app.MapPost(
                $"/{Constants.Rest.Reporting.GenerationsGenerate}",
                async (GenerateReportReq req, ReportService reportService, HttpContext http, CancellationToken ct) => {
                    StampCreatedBy(req, http);
                    return await ExecuteGenerationAsync(() => reportService.GenerateAsync(req, ct: ct)).ConfigureAwait(false);
                })
            .WithTags("Reporting")
            .WithName("GenerateReport");

        generate.ApplyEndpointAuth(options.GenerateAuth);

        var rerun = app.MapPost(
                $"/{Constants.Rest.Reporting.Generations}/{{id:guid}}/{Constants.Rest.Reporting.GenerationsRerunSuffix}",
                async (Guid id, ReportService reportService, HttpContext http, CancellationToken ct) => {
                    var identityName = http.User.Identity?.IsAuthenticated == true ? http.User.Identity.Name : null;
                    return await ExecuteGenerationAsync(() => reportService.RerunAsync(id, identityName, ct: ct)).ConfigureAwait(false);
                })
            .WithTags("Reporting")
            .WithName("RerunReportGeneration");

        rerun.ApplyEndpointAuth(options.GenerateAuth);

        if (options.DownloadStreamFactory is { } downloadFactory) {
            var download = app.MapGet(
                    $"/{Constants.Rest.Reporting.Generations}/{{id:guid}}/{Constants.Rest.Reporting.GenerationsDownloadSuffix}",
                    async (Guid id, IDbContextFactory<ReportingContext> dbFactory, HttpContext http, CancellationToken ct) => {
                        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
                        var generation = await db.ReportGenerations.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct).ConfigureAwait(false);
                        if (generation is null)
                            return Results.Problem($"Report generation '{id}' was not found.", statusCode: StatusCodes.Status404NotFound, title: "Not Found");

                        if (generation.Status != nameof(ReportGenerationStatus.Succeeded) || generation.OutputFileId is not Guid outputFileId) {
                            return Results.Problem(
                                $"Generation {id} has no downloadable output (status {generation.Status}).",
                                statusCode: StatusCodes.Status409Conflict,
                                title: "Report output not available.");
                        }

                        var stream = await downloadFactory(
                                new ReportDownloadContext {
                                    GenerationId = generation.Id,
                                    OutputFileId = outputFileId,
                                    ContentType = generation.ContentType,
                                    FileName = generation.OriginalFileName,
                                    PathPrefix = generation.PathPrefix,
                                    Services = http.RequestServices
                                },
                                ct)
                            .ConfigureAwait(false);

                        if (stream is null)
                            return Results.Problem($"Report generation '{id}' output could not be located.", statusCode: StatusCodes.Status404NotFound, title: "Not Found");

                        return Results.Stream(stream, generation.ContentType ?? "application/octet-stream", generation.OriginalFileName);
                    })
                .WithTags("Reporting")
                .WithName("DownloadReportGeneration");

            download.ApplyEndpointAuth(options.DownloadAuth);
        }

        return app;
    }

    /// <summary>Maps generation exceptions to HTTP: validation → 400, busy → 503, anything else bubbles as 500.</summary>
    private static async Task<IResult> ExecuteGenerationAsync(Func<Task<ReportGenerationRes>> action)
    {
        try {
            return Results.Ok(await action().ConfigureAwait(false));
        }
        catch (ReportValidationException ex) {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid report request.");
        }
        catch (ReportBusyException ex) {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable, title: "Reporting is busy.");
        }
    }

    private static void ValidateDefinition(ReportDefinition definition, IServiceProvider services)
    {
        var maxBytes = services.GetRequiredService<IOptions<PostgresReportingOptions>>().Value.MaxReportDataJsonBytes;
        ReportDefinitionWriteValidator.ValidateDefinition(definition, maxBytes);
    }

    /// <summary>Authenticated identity always wins; client-supplied CreatedBy is honored only for unauthenticated/service callers.</summary>
    private static void StampCreatedBy(GenerateReportReq req, HttpContext http)
    {
        var identityName = http.User.Identity?.IsAuthenticated == true ? http.User.Identity.Name : null;
        if (!string.IsNullOrWhiteSpace(identityName))
            req.CreatedBy = identityName;
        else if (string.IsNullOrWhiteSpace(req.CreatedBy))
            req.CreatedBy = "Unknown";
    }
}
