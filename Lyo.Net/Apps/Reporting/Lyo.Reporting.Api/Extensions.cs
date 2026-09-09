using Lyo.Api.ApiEndpoint;
using Lyo.Api.Export;
using Lyo.Api.Models.Error;
using Lyo.Configuration;
using Lyo.Common.Core.Identifiers;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Rendering;
using Lyo.Reporting.Models.Request;
using Lyo.Reporting.Models.Response;
using Lyo.Reporting.Postgres;
using Lyo.Reporting.Postgres.Database;
using Lyo.Query.Models.Parameters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ApiErrorCodes = Lyo.Api.Models.Constants.ApiErrorCodes;
using Constants = Lyo.Reporting.Models.Constants;

namespace Lyo.Reporting.Api;

/// <summary>Maps reporting HTTP routes, each surface optionally gated by <see cref="EndpointAuth" />.</summary>
public static class Extensions
{
    /// <summary>
    /// Parameter columns that projected queries and exports must not select. Response mapping hides <c>Value</c> and <c>EncryptedValue</c>, but projections still read the raw
    /// entities.
    /// </summary>
    private static readonly string[] ParameterDeniedSelectFields = ["EncryptedValue", "Value"];

    /// <summary>Adds the Export contributor for ReportingContext. Call once on the API host.</summary>
    public static IServiceCollection AddLyoApiReporting(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.AddLyoApiExport<ReportingContext>();
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        return services;
    }

    /// <summary>
    /// Host registration for the reporting API: <c>AddPostgresReportingManagement</c> (DbContext factory, migrations, CRUD, renderers, <c>ReportService</c>,
    /// retention, throttle) plus <see cref="AddLyoApiReporting" />. Map routes later with <see cref="BuildReportingGroup" />. Persist outputs with
    /// <c>AddReportingGenerationHooks</c>; turn on the background sweeper with <c>AddReportingMaintenanceWorker</c>.
    /// </summary>
    public static IServiceCollection AddReportingApi(this IServiceCollection services, Action<PostgresReportingOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        var options = new PostgresReportingOptions();
        configure(options);
        return services.AddReportingApi(options);
    }

    /// <inheritdoc cref="AddReportingApi(IServiceCollection, Action{PostgresReportingOptions})" />
    public static IServiceCollection AddReportingApi(this IServiceCollection services, PostgresReportingOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(options);
        services.AddPostgresReportingManagement(options);
        services.AddLyoApiReporting();
        return services;
    }

    /// <summary>
    /// Same as <see cref="AddReportingApi(IServiceCollection, PostgresReportingOptions)" />, binding options from <paramref name="configuration" /> (default section
    /// <see cref="PostgresReportingOptions.SectionName" />).
    /// </summary>
    public static IServiceCollection AddReportingApiFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresReportingOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        var options = LyoOptions.Bind<PostgresReportingOptions>(configuration, configSectionName);

        return services.AddReportingApi(options);
    }

    /// <summary>
    /// Maps Definition CRUD (plus Export), Definition Parameter CRUD, Generation query/get/delete, Generate, Rerun, and Download when
    /// <see cref="ReportingApiOptions.DownloadStreamFactory" /> is set. Call after <c>AddPostgresReportingManagement</c> and <see cref="AddLyoApiReporting" />.
    /// </summary>
    public static WebApplication BuildReportingGroup(this WebApplication app, ReportingApiOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(app);
        options ??= new();
        app.CreateBuilder<ReportingContext, ReportDefinition, ReportDefinitionReq, ReportDefinitionRes, Guid>(Constants.Rest.Reporting.Definitions, "Reporting")
            .WithCrud(
                ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance, new() {
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
                    // Cascade delete drops generation rows, not host-stored outputs. Let the host's
                    // OnCleanupAsync hook remove each blob first. A hook failure cancels the delete.
                    BeforeDeleteAsync = (ctx, ct) => CleanupGenerationOutputsAsync(ctx.Entity.Generations, ctx.Services, ct)
                })
            .Build();

        app.CreateBuilder<ReportingContext, ReportDefinitionParameter, ReportDefinitionParameterReq, ReportDefinitionParameterRes,
                Guid>(Constants.Rest.Reporting.DefinitionParameters, "Reporting")
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
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

        app.CreateBuilder<ReportingContext, ReportGeneration, ReportGenerationReq, ReportGenerationRes, Guid>(Constants.Rest.Reporting.Generations, "Reporting")
            .WithCrud(
                ApiFeatureSet.ReadOnly + ApiFeature.Delete + ApiFeature.DeleteBulk, new() {
                    QueryAuth = options.GenerationAuth,
                    GetAuth = options.GenerationAuth,
                    DeleteAuth = options.GenerationAuth,
                    DeleteBulkAuth = options.GenerationAuth,
                    MetadataAuth = options.GenerationAuth,
                    DeniedSelectFields = ParameterDeniedSelectFields,
                    BeforeDeleteAsync = (ctx, ct) => CleanupGenerationOutputsAsync([ctx.Entity], ctx.Services, ct)
                })
            .Build();

        var generate = app.MapPost(
                $"/{Constants.Rest.Reporting.GenerationsGenerate}", async (GenerateReportReq req, ReportService reportService, HttpContext http, CancellationToken ct) => {
                    StampCreatedBy(req, http, options);
                    return await ExecuteGenerationAsync(() => reportService.GenerateAsync(req, ct: ct), ct).ConfigureAwait(false);
                })
            .WithTags("Reporting")
            .WithName("GenerateReport");

        generate.ApplyEndpointAuth(options.GenerateAuth);
        var resolveOptions = app.MapPost(
                $"/{Constants.Rest.Reporting.ResolveParameterOptions}",
                async (ParameterOptionsResolveReq req, ReportParameterOptionsExecutor executor, CancellationToken ct) => {
                    if (req is null || string.IsNullOrWhiteSpace(req.OptionsJson) || !ParameterOptionsJson.TryDeserialize(req.OptionsJson, out var options) || options is null) {
                        return Results.Ok(new ParameterOptionsResolveRes { Error = "Invalid Options JSON." });
                    }

                    var siblings = req.SiblingValues ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                    return Results.Ok(await executor.ResolveAsync(options, siblings, ct).ConfigureAwait(false));
                })
            .WithTags("Reporting")
            .WithName("ResolveParameterOptions");

        resolveOptions.ApplyEndpointAuth(options.DefinitionAuth);
        var rerun = app.MapPost(
                $"/{Constants.Rest.Reporting.Generations}/{{id:guid}}/{Constants.Rest.Reporting.GenerationsRerunSuffix}",
                async (Guid id, ReportService reportService, HttpContext http, CancellationToken ct, bool includeReportData = false) => {
                    var identityName = http.User.Identity?.IsAuthenticated == true ? http.User.Identity.Name : null;
                    return await ExecuteGenerationAsync(() => reportService.RerunAsync(id, identityName, includeReportData: includeReportData, ct: ct), ct).ConfigureAwait(false);
                })
            .WithTags("Reporting")
            .WithName("RerunReportGeneration");

        rerun.ApplyEndpointAuth(options.GenerateAuth);
        if (options.DownloadStreamFactory is { } downloadFactory) {
            var download = app.MapGet(
                    $"/{Constants.Rest.Reporting.Generations}/{{id:guid}}/{Constants.Rest.Reporting.GenerationsDownloadSuffix}", async (
                        Guid id, IDbContextFactory<ReportingContext> dbFactory, HttpContext http, CancellationToken ct) => {
                        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
                        var generation = await db.ReportGenerations.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct).ConfigureAwait(false);
                        if (generation is null)
                            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, $"Report generation '{id}' was not found."));

                        if (generation.Status != nameof(ReportGenerationStatus.Succeeded) || generation.OutputFileId is not Guid outputFileId) {
                            throw ApiErrorException.From(
                                LyoProblemDetails.FromCode(
                                    ApiErrorCodes.Conflict, $"Generation {id} has no downloadable output (status {generation.Status})."));
                        }

                        var stream = await downloadFactory(
                                new() {
                                    GenerationId = generation.Id,
                                    OutputFileId = outputFileId,
                                    ContentType = generation.ContentType,
                                    FileName = generation.OriginalFileName,
                                    PathPrefix = generation.PathPrefix,
                                    Services = http.RequestServices
                                }, ct)
                            .ConfigureAwait(false);

                        if (stream is null)
                            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, $"Report generation '{id}' output could not be located."));

                        return Results.Stream(stream, generation.ContentType ?? FileTypeInfo.Bin.MimeType, generation.OriginalFileName);
                    })
                .WithTags("Reporting")
                .WithName("DownloadReportGeneration");

            download.ApplyEndpointAuth(options.DownloadAuth);
        }

        return app;
    }

    private static Task CleanupGenerationOutputsAsync(IEnumerable<ReportGeneration> generations, IServiceProvider services, CancellationToken ct)
        => ReportGenerationCleanup.InvokeCleanupHooksAsync(generations, services.GetService<ReportGenerationHooks>(), services, ct).AsTask();

    /// <summary>
    /// Maps generation failures to HTTP: validation is 400, busy is 503, the server generation timeout is 504, a client disconnect is 499, and anything else surfaces as 500.
    /// </summary>
    /// <param name="action">The generation call.</param>
    /// <param name="requestAborted">Request token used to tell a caller disconnect from our own generation timeout.</param>
    internal static async Task<IResult> ExecuteGenerationAsync(Func<Task<ReportGenerationRes>> action, CancellationToken requestAborted)
    {
        try {
            return Results.Ok(await action().ConfigureAwait(false));
        }
        catch (ReportValidationException ex) {
            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, ex.Message));
        }
        catch (ReportBusyException ex) {
            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.ServiceUnavailable, ex.Message));
        }
        catch (OperationCanceledException) when (!requestAborted.IsCancellationRequested) {
            throw ApiErrorException.From(
                LyoProblemDetails.FromCode(ApiErrorCodes.GatewayTimeout, "Report generation exceeded the configured GenerationTimeout and was cancelled."));
        }
        catch (OperationCanceledException) {
            throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.Cancelled, "The report generation request was cancelled by the caller."));
        }
    }

    private static void ValidateDefinition(ReportDefinition definition, IServiceProvider services)
    {
        var maxBytes = services.GetRequiredService<IOptions<PostgresReportingOptions>>().Value.MaxReportDataJsonBytes;
        ReportDefinitionWriteValidator.ValidateDefinition(definition, maxBytes);
    }

    /// <summary>
    /// Writes generation audit identity. An authenticated identity always wins. An anonymous caller's <c>CreatedBy</c> is dropped unless the host set
    /// <see cref="ReportingApiOptions.AllowAnonymousCreatedBy" />, so a generation cannot be pinned on someone else.
    /// </summary>
    internal static void StampCreatedBy(GenerateReportReq req, HttpContext http, ReportingApiOptions options)
    {
        var identityName = http.User.Identity?.IsAuthenticated == true ? http.User.Identity.Name : null;
        if (!string.IsNullOrWhiteSpace(identityName)) {
            req.CreatedBy = identityName;
            return;
        }

        if (!options.AllowAnonymousCreatedBy)
            req.CreatedBy = null;

        if (string.IsNullOrWhiteSpace(req.CreatedBy))
            req.CreatedBy = "Anonymous";
    }
}