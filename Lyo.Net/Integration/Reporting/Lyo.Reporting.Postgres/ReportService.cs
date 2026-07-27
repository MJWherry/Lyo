using System.Text.Json;
using Lyo.Common.Conversion;
using Lyo.Common.Identifiers;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.IO.Temp;
using Lyo.Metrics;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Profiles;
using Lyo.Reporting.Models.Providers;
using Lyo.Reporting.Models.Rendering;
using Lyo.Reporting.Models.Request;
using Lyo.Reporting.Models.Response;
using Lyo.Reporting.Postgres.Database;
using Lyo.Reporting.Postgres.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportingConstants = Lyo.Reporting.Models.Constants;

namespace Lyo.Reporting.Postgres;

/// <summary>
/// Orchestrates report generation: optional <see cref="IReportDataProvider"/>, render via <see cref="IReportRenderer"/>
/// into IoTemp, then optional consumer hooks (e.g. persist via FileStorage). Reporting does not reference FileStorage.
/// </summary>
public sealed class ReportService(
    IDbContextFactory<ReportingContext> dbFactory,
    IEnumerable<IReportRenderer> renderers,
    IEnumerable<IReportDataProvider> dataProviders,
    IEnumerable<ReportingGenerationProfile> profiles,
    IServiceProvider services,
    IOptions<PostgresReportingOptions> options,
    ILogger<ReportService> logger,
    ReportGenerationThrottle? throttle = null,
    IMetrics? metrics = null)
{
    /// <summary>Max staged output file name length (stem + extension), aligned with common filesystem limits.</summary>
    internal const int MaxFileNameLength = 255;

    private readonly IMetrics _metrics = metrics ?? NullMetrics.Instance;
    private readonly Dictionary<string, IReportDataProvider> _providersByKey =
        ToUniqueKeyMap(dataProviders, p => p.ProfileKey, "IReportDataProvider.ProfileKey");
    private readonly Dictionary<string, ReportingGenerationProfile> _profilesByKey =
        ToUniqueKeyMap(profiles, p => p.Key, "ReportingGenerationProfile.Key");

    /// <summary>Case-insensitive keyed lookup that fails with an actionable message when two registrations share a key.</summary>
    private static Dictionary<string, T> ToUniqueKeyMap<T>(IEnumerable<T> items, Func<T, string> keySelector, string what)
    {
        var map = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items) {
            var key = keySelector(item);
            if (!map.TryAdd(key, item))
                throw new ConflictException($"Duplicate {what} '{key}' registered; keys must be unique (case-insensitive). Remove or rename one of the registrations.");
        }

        return map;
    }

    public Task<ReportGenerationRes> GenerateAsync(GenerateReportReq request, ReportGenerationHooks? hooks = null, CancellationToken ct = default)
        => GenerateCoreAsync(request, hooks, bypassAdHocPolicy: false, ct);

    // bypassAdHocPolicy: true for trusted internal snapshots (rerun) that must generate even when AllowAdHocGeneration is disabled.
    private async Task<ReportGenerationRes> GenerateCoreAsync(GenerateReportReq request, ReportGenerationHooks? hooks, bool bypassAdHocPolicy, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(request);
        hooks ??= services.GetService<ReportGenerationHooks>() ?? new ReportGenerationHooks();
        var opts = options.Value;

        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        ReportDefinition? definition = null;
        List<ReportDefinitionParameter> definitionParameters = [];
        string? profileKey = null;
        string reportDataJson;
        Guid? definitionId = request.ReportDefinitionId;

        if (definitionId is Guid defId) {
            definition = await db.ReportDefinitions.AsNoTracking()
                             .Include(d => d.Parameters)
                             .FirstOrDefaultAsync(d => d.Id == defId, ct)
                             .ConfigureAwait(false)
                         ?? throw new ReportValidationException($"Report definition {defId} was not found.");
            if (!definition.IsActive)
                throw new ReportValidationException($"Report definition {defId} is inactive.");

            definitionParameters = definition.Parameters;
            profileKey = definition.GenerationProfileKey;
            if (!string.IsNullOrWhiteSpace(request.OverrideReportDataJson)) {
                EnsureAdHocAllowed(opts, bypassAdHocPolicy, "OverrideReportDataJson");
                reportDataJson = request.OverrideReportDataJson!;
            }
            else if (!string.IsNullOrWhiteSpace(request.ReportDataJson)) {
                EnsureAdHocAllowed(opts, bypassAdHocPolicy, "ReportDataJson");
                reportDataJson = request.ReportDataJson!;
            }
            else {
                reportDataJson = definition.ReportDataJson;
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.ReportDataJson)) {
            EnsureAdHocAllowed(opts, bypassAdHocPolicy, "ReportDataJson");
            reportDataJson = request.ReportDataJson!;
        }
        else {
            throw new ReportValidationException("Either ReportDefinitionId or ReportDataJson is required.");
        }

        EnsureJsonSize(reportDataJson, opts.MaxReportDataJsonBytes, "ReportDataJson");
        EnsureParseableJson(reportDataJson, "ReportDataJson");

        var mergedParameters = MergeParameters(definitionParameters, request.Parameters);
        var validationErrors = ReportParameterValidator.Validate(definitionParameters, mergedParameters, rejectUnknownKeys: definition is not null);
        if (validationErrors.Count > 0)
            throw new ReportValidationException(string.Join(" ", validationErrors));

        ReportingGenerationProfile? profile = null;
        if (!string.IsNullOrWhiteSpace(profileKey) && _profilesByKey.TryGetValue(profileKey!, out var registered))
            profile = registered;

        var format = ResolveFormat(request, definition, profile);
        var fileName = SanitizeFileName(FirstNonEmpty(request.FileName, definition?.DefaultFileName, profile?.DefaultFileName));
        var pathPrefix = FirstNonEmpty(request.PathPrefix, definition?.DefaultPathPrefix, profile?.DefaultPathPrefix);
        var createdBy = FirstNonEmpty(request.CreatedBy, ReportAuditHelper.GetActorName(services)) ?? "Unknown";
        createdBy = ReportingLyoMapper.TruncateCreatedBy(createdBy);

        var generationParamEntities = mergedParameters.Select(p => {
            var entity = ReportingLyoMapper.ReqToNew(p);
            entity.Id = LyoGuid.CreateCombPostgres();
            return entity;
        }).ToList();

        var paramResList = generationParamEntities.Select(ReportingLyoMapper.ToRes).ToList();
        var parametersJson = SerializeParametersJson(mergedParameters);

        // Slot is held across provider + render work; released in the outer finally.
        using var generationSlot = throttle is not null ? await throttle.AcquireAsync(ct).ConfigureAwait(false) : null;

        var now = DateTime.UtcNow;
        var generation = new ReportGeneration {
            Id = LyoGuid.CreateCombPostgres(),
            ReportDefinitionId = definitionId,
            ReportDataJson = reportDataJson,
            Format = format.ToString(),
            Status = nameof(ReportGenerationStatus.Pending),
            PathPrefix = pathPrefix,
            OriginalFileName = fileName,
            CreatedBy = createdBy,
            CreatedTimestamp = now,
            Parameters = generationParamEntities
        };
        foreach (var p in generation.Parameters)
            p.ReportGenerationId = generation.Id;

        // Fix Res generation ids now that we know them
        paramResList = generation.Parameters.Select(ReportingLyoMapper.ToRes).ToList();

        db.ReportGenerations.Add(generation);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        var effectiveRequest = new GenerateReportReq {
            ReportDefinitionId = request.ReportDefinitionId,
            ReportDataJson = request.ReportDataJson,
            OverrideReportDataJson = request.OverrideReportDataJson,
            Format = format,
            Parameters = mergedParameters,
            FileName = fileName,
            PathPrefix = pathPrefix,
            CreatedBy = createdBy
        };

        var ctx = new ReportGenerateContext {
            GenerationId = generation.Id,
            ReportDefinitionId = definitionId,
            Request = effectiveRequest,
            Format = format,
            ReportDataJson = reportDataJson,
            PathPrefix = pathPrefix,
            FileName = fileName,
            Services = services
        };

        _metrics.IncrementCounter(ReportingConstants.Metrics.GenerationStarted, tags: [("format", format.ToString())]);

        // Wall-clock limit for provider + render + hooks so a slow generation can't hold the request (and a throttle slot) forever.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (opts.GenerationTimeout is { } generationTimeout)
            timeoutCts.CancelAfter(generationTimeout);
        var runCt = timeoutCts.Token;

        string? preRenderedPath = null;

        try {
            if (!string.IsNullOrWhiteSpace(profileKey) && _providersByKey.TryGetValue(profileKey!, out var provider)) {
                var providerResult = await provider.BuildAsync(
                        new ReportDataProviderRequest {
                            ReportDefinitionId = definitionId,
                            Parameters = paramResList,
                            ParametersJson = parametersJson,
                            ReportDataJson = reportDataJson,
                            Services = services
                        },
                        runCt)
                    .ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(providerResult.ReportDataJson)) {
                    reportDataJson = providerResult.ReportDataJson!;
                    EnsureJsonSize(reportDataJson, opts.MaxReportDataJsonBytes, "ReportDataJson");
                    EnsureParseableJson(reportDataJson, "ReportDataJson");
                    ctx.ReportDataJson = reportDataJson;
                }

                if (!string.IsNullOrWhiteSpace(providerResult.PreRenderedFilePath))
                    preRenderedPath = providerResult.PreRenderedFilePath;

                ctx.ContentType = providerResult.ContentType;
                ctx.FileName = FirstNonEmpty(ctx.FileName, SanitizeFileName(providerResult.FileName));
            }

            if (hooks.BeforeGenerateAsync is not null)
                await hooks.BeforeGenerateAsync(ctx, runCt).ConfigureAwait(false);

            generation.Status = nameof(ReportGenerationStatus.Running);
            generation.StartedTimestamp = DateTime.UtcNow;
            await db.SaveChangesAsync(runCt).ConfigureAwait(false);

            var ioTemp = services.GetService<IIOTempService>()
                         ?? throw new InvalidOperationException("IIOTempService is required for report generation staging.");

            using var session = ioTemp.CreateSession();
            var extension = FormatExtension(format);
            var stagedName = SanitizeFileName(ctx.FileName) ?? $"report-{generation.Id:N}{extension}";
            if (!Path.HasExtension(stagedName))
                stagedName += extension;

            string stagedPath;
            if (!string.IsNullOrWhiteSpace(preRenderedPath)) {
                if (!File.Exists(preRenderedPath))
                    throw new ReportValidationException($"Pre-rendered file was not found: {preRenderedPath}");
                EnsureFileSize(preRenderedPath!, opts.MaxOutputFileBytes);
                stagedPath = session.GetFilePath(stagedName);
                File.Copy(preRenderedPath!, stagedPath, overwrite: true);
                ctx.StagedFilePath = stagedPath;
                ctx.ContentType ??= GuessContentType(format);
                ctx.FileName = stagedName;
            }
            else {
                var renderer = renderers.FirstOrDefault(r => r.CanRender(format))
                               ?? throw new InvalidOperationException($"No IReportRenderer registered for format {format}.");

                stagedPath = session.GetFilePath(stagedName);
                var renderResult = await renderer.RenderAsync(
                        new ReportRenderRequest {
                            ReportDataJson = ctx.ReportDataJson,
                            Format = format,
                            OutputFilePath = stagedPath,
                            SuggestedFileName = stagedName,
                            Parameters = paramResList,
                            ParametersJson = parametersJson,
                            Services = services
                        },
                        runCt)
                    .ConfigureAwait(false);

                EnsureFileSize(renderResult.FilePath, opts.MaxOutputFileBytes);
                ctx.StagedFilePath = renderResult.FilePath;
                ctx.ContentType = renderResult.ContentType;
                ctx.FileName = renderResult.FileName;
            }

            if (hooks.AfterRenderAsync is not null)
                await hooks.AfterRenderAsync(ctx, runCt).ConfigureAwait(false);

            if (hooks.AfterSaveAsync is not null)
                await hooks.AfterSaveAsync(ctx, runCt).ConfigureAwait(false);

            generation.Status = nameof(ReportGenerationStatus.Succeeded);
            generation.FinishedTimestamp = DateTime.UtcNow;
            generation.OutputFileId = ctx.OutputFileId;
            generation.OriginalFileName = ctx.FileName;
            generation.ContentType = ctx.ContentType;
            generation.PathPrefix = ctx.PathPrefix ?? pathPrefix;
            generation.ReportDataJson = ctx.ReportDataJson;
            generation.ErrorMessage = null;
            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            _metrics.IncrementCounter(ReportingConstants.Metrics.GenerationSucceeded, tags: [("format", format.ToString())]);
            ReportAuditHelper.RecordGenerated(services, generation.Id);
            return ReportingLyoMapper.ToRes(generation);
        }
        catch (Exception ex) {
            var timedOut = opts.GenerationTimeout is not null && timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested;
            var errorMessage = timedOut ? $"Report generation timed out after {opts.GenerationTimeout}." : ex.Message;
            if (timedOut)
                logger.LogError(ex, "Report generation {GenerationId} timed out after {Timeout}", generation.Id, opts.GenerationTimeout);
            else
                logger.LogError(ex, "Report generation {GenerationId} failed", generation.Id);

            try {
                generation.Status = nameof(ReportGenerationStatus.Failed);
                generation.FinishedTimestamp = DateTime.UtcNow;
                generation.ErrorMessage = errorMessage.Length > 4000 ? errorMessage[..4000] : errorMessage;
                // Keep any uploaded output id on the Failed row so retention's OnCleanupAsync can delete the blob later.
                generation.OutputFileId = ctx.OutputFileId;
                // CancellationToken.None: a client disconnect must not strand the row in Running.
                await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception persistEx) {
                logger.LogError(persistEx, "Failed to persist Failed status for generation {GenerationId}", generation.Id);
            }

            _metrics.IncrementCounter(ReportingConstants.Metrics.GenerationFailed, tags: [("format", format.ToString())]);

            if (hooks.OnFailureAsync is not null) {
                try {
                    var failure = new ReportGenerateFailureContext {
                        GenerationId = ctx.GenerationId,
                        ReportDefinitionId = ctx.ReportDefinitionId,
                        Request = ctx.Request,
                        Format = ctx.Format,
                        ReportDataJson = ctx.ReportDataJson,
                        StagedFilePath = ctx.StagedFilePath,
                        ContentType = ctx.ContentType,
                        FileName = ctx.FileName,
                        OutputFileId = ctx.OutputFileId,
                        PathPrefix = ctx.PathPrefix,
                        Services = ctx.Services,
                        Exception = ex
                    };
                    foreach (var item in ctx.Items)
                        failure.Items[item.Key] = item.Value;
                    await hooks.OnFailureAsync(failure, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception hookEx) {
                    logger.LogError(hookEx, "OnFailure hook failed for generation {GenerationId}", generation.Id);
                }
            }

            throw;
        }
    }

    /// <summary>Re-runs a past generation from its stored snapshot (composition JSON, format, parameters), producing a new generation row.</summary>
    public async Task<ReportGenerationRes> RerunAsync(Guid generationId, string? createdBy = null, ReportGenerationHooks? hooks = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var source = await db.ReportGenerations.AsNoTracking()
                         .Include(g => g.Parameters)
                         .FirstOrDefaultAsync(g => g.Id == generationId, ct)
                         .ConfigureAwait(false)
                     ?? throw new ReportValidationException($"Report generation {generationId} was not found.");

        var request = new GenerateReportReq {
            // Replay the stored snapshot verbatim; skip the definition so a since-changed/inactive definition can't alter the rerun.
            ReportDataJson = source.ReportDataJson,
            Format = TypeConversion.EnumOrDefault(source.Format, ReportFormat.Html),
            FileName = source.OriginalFileName,
            PathPrefix = source.PathPrefix,
            CreatedBy = createdBy,
            Parameters = source.Parameters.Select(p => new ReportGenerationParameterReq {
                Key = p.Key,
                Type = TypeConversion.EnumOrDefault(p.Type, ReportParameterType.Unknown),
                Value = p.Value,
                Description = p.Description,
                EncryptedValue = p.EncryptedValue
            }).ToList()
        };

        // Reruns replay trusted stored snapshots, so they remain possible on hosts with AllowAdHocGeneration disabled.
        return await GenerateCoreAsync(request, hooks, bypassAdHocPolicy: true, ct).ConfigureAwait(false);
    }

    /// <summary>Request values override definition defaults by Key; missing keys get default Value from definition.</summary>
    internal static List<ReportGenerationParameterReq> MergeParameters(
        IReadOnlyList<ReportDefinitionParameter> definitionParameters,
        IReadOnlyList<ReportGenerationParameterReq> requestParameters)
    {
        var requestByKey = requestParameters
            .GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var result = new List<ReportGenerationParameterReq>();

        foreach (var def in definitionParameters) {
            if (requestByKey.TryGetValue(def.Key, out var provided) && provided.Count > 0) {
                foreach (var p in provided) {
                    result.Add(new ReportGenerationParameterReq {
                        Key = def.Key,
                        Type = TypeConversion.EnumOrDefault(def.Type, ReportParameterType.Unknown),
                        Value = p.Value,
                        Description = p.Description ?? def.Description,
                        EncryptedValue = p.EncryptedValue ?? def.EncryptedValue
                    });
                }

                requestByKey.Remove(def.Key);
            }
            else if (!string.IsNullOrEmpty(def.Value) || def.EncryptedValue is not null || def.Required) {
                result.Add(new ReportGenerationParameterReq {
                    Key = def.Key,
                    Type = TypeConversion.EnumOrDefault(def.Type, ReportParameterType.Unknown),
                    Value = def.Value,
                    Description = def.Description,
                    EncryptedValue = def.EncryptedValue
                });
            }
        }

        // Ad-hoc keys not on the definition (allowed for definition-less generate; rejected by validation when a definition exists)
        foreach (var leftover in requestByKey.Values.SelectMany(x => x))
            result.Add(leftover);

        return result;
    }

    /// <summary>Single-value keys serialize as strings; AllowMultiple keys serialize as arrays so no value is lost.</summary>
    internal static string SerializeParametersJson(IReadOnlyList<ReportGenerationParameterReq> parameters)
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in parameters.GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase)) {
            var values = group.Select(p => p.Value).ToList();
            map[group.Key] = values.Count == 1 ? values[0] : values;
        }

        return JsonSerializer.Serialize(map);
    }

    /// <summary>Strips directory segments and invalid/control characters; caps length while preserving the extension. Null when nothing usable remains.</summary>
    internal static string? SanitizeFileName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var candidate = Path.GetFileName(name.Replace('\\', '/'));
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(candidate.Where(c => !invalid.Contains(c) && !char.IsControl(c)).ToArray()).Trim().Trim('.');
        if (cleaned.Length == 0)
            return null;

        if (cleaned.Length > MaxFileNameLength) {
            var ext = Path.GetExtension(cleaned);
            var stem = Path.GetFileNameWithoutExtension(cleaned);
            cleaned = stem[..Math.Min(stem.Length, Math.Max(1, MaxFileNameLength - ext.Length))] + ext;
        }

        return cleaned;
    }

    private static void EnsureAdHocAllowed(PostgresReportingOptions opts, bool bypassAdHocPolicy, string property)
    {
        if (!opts.AllowAdHocGeneration && !bypassAdHocPolicy)
            throw new ReportValidationException($"{property} is not allowed: this host only generates from saved report definitions (AllowAdHocGeneration is disabled).");
    }

    private static ReportFormat ResolveFormat(GenerateReportReq request, ReportDefinition? definition, ReportingGenerationProfile? profile)
    {
        if (request.Format is { } reqFormat)
            return reqFormat;
        if (!string.IsNullOrWhiteSpace(definition?.DefaultFormat)
            && TypeConversion.EnumOrNull<ReportFormat>(definition.DefaultFormat) is { } defFormat)
            return defFormat;
        if (profile?.DefaultFormat is { } profileFormat)
            return profileFormat;
        return ReportFormat.Html;
    }

    private static void EnsureJsonSize(string json, int maxBytes, string name)
    {
        var bytes = System.Text.Encoding.UTF8.GetByteCount(json);
        if (bytes > maxBytes)
            throw new ReportValidationException($"{name} exceeds MaxReportDataJsonBytes ({bytes} > {maxBytes}).");
    }

    private static void EnsureParseableJson(string json, string name)
    {
        try {
            using var _ = JsonDocument.Parse(json);
        }
        catch (JsonException ex) {
            throw new ReportValidationException($"{name} is not valid JSON: {ex.Message}", ex);
        }
    }

    private static void EnsureFileSize(string path, long maxBytes)
    {
        var length = new FileInfo(path).Length;
        if (length > maxBytes)
            throw new ReportValidationException($"Output file exceeds MaxOutputFileBytes ({length} > {maxBytes}).");
    }

    private static string FormatExtension(ReportFormat format)
        => format switch {
            ReportFormat.Pdf => ".pdf",
            ReportFormat.Csv => ".csv",
            ReportFormat.Xlsx => ".xlsx",
            ReportFormat.Json => ".json",
            _ => ".html"
        };

    private static string GuessContentType(ReportFormat format)
        => format switch {
            ReportFormat.Pdf => "application/pdf",
            ReportFormat.Csv => "text/csv; charset=utf-8",
            ReportFormat.Xlsx => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ReportFormat.Json => "application/json; charset=utf-8",
            _ => "text/html; charset=utf-8"
        };

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values) {
            if (!string.IsNullOrWhiteSpace(v))
                return v;
        }

        return null;
    }
}
