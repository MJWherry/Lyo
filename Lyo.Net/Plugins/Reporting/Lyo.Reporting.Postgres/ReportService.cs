using System.Text;
using System.Text.Json;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Cache;
using Lyo.Common.Core.Conversion;
using Lyo.Common.Core.Identifiers;
using Lyo.Common.Core.Pathing;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.IO.Temp;
using Lyo.Metrics;
using Lyo.Parameters;
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
using ReportingConstants = Lyo.Reporting.Models.Constants;

namespace Lyo.Reporting.Postgres;

/// <summary>
/// Orchestrates report generation: optional <see cref="IReportDataProvider" />, render via <see cref="IReportRenderer" /> into IoTemp, then optional consumer hooks (for example
/// persist via FileStorage). Reporting does not reference FileStorage.
/// </summary>
public sealed class ReportService(
    IDbContextFactory<ReportingContext> dbFactory,
    IEnumerable<IReportRenderer> renderers,
    IEnumerable<IReportDataProvider> dataProviders,
    IEnumerable<ReportingGenerationProfile> profiles,
    IServiceProvider services,
    PostgresReportingOptions options,
    ILogger<ReportService> logger,
    ReportGenerationThrottle? throttle = null,
    IMetrics? metrics = null,
    ICacheService? cache = null,
    CacheOptions? cacheOptions = null,
    ReportGenerationLiveTracker? liveTracker = null)
{
    /// <summary>Longest staged output file name (stem + extension), aligned with common filesystem limits.</summary>
    internal const int MaxFileNameLength = 255;

    private readonly IMetrics _metrics = metrics ?? NullMetrics.Instance;

    private readonly Dictionary<string, ReportingGenerationProfile> _profilesByKey = ToUniqueKeyMap(profiles, p => p.Key, "ReportingGenerationProfile.Key");

    private readonly Dictionary<string, IReportDataProvider> _providersByKey = ToUniqueKeyMap(dataProviders, p => p.ProfileKey, "IReportDataProvider.ProfileKey");

    /// <summary>Case-insensitive keyed lookup that fails with an actionable message when two registrations share one key.</summary>
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
        => GenerateCoreAsync(request, hooks, false, ct);

    // bypassAdHocPolicy: true for trusted internal snapshots (rerun) that must generate even when AllowAdHocGeneration is turned off.
    private async Task<ReportGenerationRes> GenerateCoreAsync(GenerateReportReq request, ReportGenerationHooks? hooks, bool bypassAdHocPolicy, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(request);
        hooks ??= services.GetService<ReportGenerationHooks>() ?? new ReportGenerationHooks();
        var opts = options;
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        ReportDefinition? definition = null;
        List<ReportDefinitionParameter> definitionParameters = [];
        string? profileKey = null;
        string reportDataJson;
        var definitionId = request.ReportDefinitionId;
        if (definitionId is Guid defId) {
            definition = await db.ReportDefinitions.AsNoTracking().Include(d => d.Parameters).FirstOrDefaultAsync(d => d.Id == defId, ct).ConfigureAwait(false) ??
                throw new ReportValidationException($"Report definition {defId} was not found.");

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
            else
                reportDataJson = definition.ReportDataJson;
        }
        else if (!string.IsNullOrWhiteSpace(request.ReportDataJson)) {
            EnsureAdHocAllowed(opts, bypassAdHocPolicy, "ReportDataJson");
            reportDataJson = request.ReportDataJson!;
        }
        else
            throw new ReportValidationException("Either ReportDefinitionId or ReportDataJson is required.");

        EnsureJsonSize(reportDataJson, opts.MaxReportDataJsonBytes, "ReportDataJson");
        EnsureParseableJson(reportDataJson, "ReportDataJson");
        var mergeErrors = new List<string>();
        var mergedParameters = MergeParameters(definitionParameters, request.Parameters, services.GetService<LyoTemplateResolver>(), mergeErrors);
        var validationErrors = mergeErrors.Concat(ReportParameterValidator.Validate(definitionParameters, mergedParameters, definition is not null)).ToList();
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
            })
            .ToList();

        var paramResList = generationParamEntities.Select(ReportingLyoMapper.ToRes).ToList();
        var parametersJson = SerializeParametersJson(mergedParameters);

        // Acquired before the row exists so a busy host rejects the request outright instead of persisting a generation it will immediately fail. The slot covers the provider and
        // render work only. It is released as soon as the output is staged, so storage uploads, the terminal status write, and the failure path no longer occupy a slot.
        var generationSlot = throttle is not null ? await throttle.AcquireAsync(ct).ConfigureAwait(false) : null;
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

        // Fill Res generation ids now that they are known.
        paramResList = generation.Parameters.Select(ReportingLyoMapper.ToRes).ToList();
        db.ReportGenerations.Add(generation);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await InvalidateGenerationCacheAsync(generation.Id).ConfigureAwait(false);
        var effectiveRequest = new GenerateReportReq {
            ReportDefinitionId = request.ReportDefinitionId,
            ReportDataJson = request.ReportDataJson,
            OverrideReportDataJson = request.OverrideReportDataJson,
            Format = format,
            Parameters = mergedParameters,
            FileName = fileName,
            PathPrefix = pathPrefix,
            CreatedBy = createdBy,
            IncludeReportData = request.IncludeReportData
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

        // Wall-clock limit for provider, render, and hooks so a slow generation cannot hold the request (and a throttle slot) forever.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (opts.GenerationTimeout is { } generationTimeout)
            timeoutCts.CancelAfter(generationTimeout);

        var runCt = timeoutCts.Token;
        string? preRenderedPath = null;

        // Marks the row live for stuck-run recovery, which otherwise cannot tell a slow provider from a crashed host.
        using var liveScope = liveTracker?.Track(generation.Id);
        try {
            // Provider and render work run under the throttle slot taken above.
            if (!string.IsNullOrWhiteSpace(profileKey) && _providersByKey.TryGetValue(profileKey!, out var provider)) {
                var providerResult = await provider.BuildAsync(
                        new() {
                            ReportDefinitionId = definitionId,
                            Parameters = paramResList,
                            ParametersJson = parametersJson,
                            ReportDataJson = reportDataJson,
                            Services = services
                        }, runCt)
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

            var resolver = services.GetRequiredService<IReportDataSourceResolver>();
            var optionsRefs = definitionParameters.Select(p => new ReportParameterOptionsRef(p.Key, p.Type, p.Options)).ToList();
            reportDataJson = await resolver.ApplyAsync(reportDataJson, optionsRefs, mergedParameters, runCt).ConfigureAwait(false);
            ctx.ReportDataJson = reportDataJson;
            parametersJson = SerializeParametersJson(mergedParameters);
            paramResList = mergedParameters.Zip(generationParamEntities, (req, entity) => ReportingLyoMapper.ToRes(entity) with { Value = req.Value }).ToList();

            if (hooks.BeforeGenerateAsync is not null)
                await hooks.BeforeGenerateAsync(ctx, runCt).ConfigureAwait(false);

            generation.Status = nameof(ReportGenerationStatus.Running);
            generation.StartedTimestamp = DateTime.UtcNow;
            await db.SaveChangesAsync(runCt).ConfigureAwait(false);
            var ioTemp = services.GetService<IIOTempService>() ?? throw new InvalidOperationException("IIOTempService is required for report generation staging.");
            using var session = ioTemp.CreateSession();
            var extension = format.Extension;
            var stagedName = SanitizeFileName(ctx.FileName) ?? $"report-{generation.Id:N}{extension}";
            if (!Path.HasExtension(stagedName))
                stagedName += extension;

            string stagedPath;
            if (!string.IsNullOrWhiteSpace(preRenderedPath)) {
                var confinedPath = ResolveConfinedPreRenderedPath(preRenderedPath!, opts);
                if (!File.Exists(confinedPath))
                    throw new ReportValidationException($"Pre-rendered file was not found: {preRenderedPath}");

                EnsureFileSize(confinedPath, opts.MaxOutputFileBytes);
                stagedPath = session.GetFilePath(stagedName);
                File.Copy(confinedPath, stagedPath, true);
                ctx.StagedFilePath = stagedPath;
                ctx.ContentType ??= format.ContentType;
                ctx.FileName = stagedName;
            }
            else {
                var renderer = renderers.FirstOrDefault(r => r.CanRender(format)) ?? throw new InvalidOperationException($"No IReportRenderer registered for format {format}.");
                stagedPath = session.GetFilePath(stagedName);
                var renderResult = await renderer.RenderAsync(
                        new() {
                            ReportDataJson = ctx.ReportDataJson,
                            Format = format,
                            OutputFilePath = stagedPath,
                            SuggestedFileName = stagedName,
                            Parameters = paramResList,
                            ParametersJson = parametersJson,
                            Services = services
                        }, runCt)
                    .ConfigureAwait(false);

                EnsureFileSize(renderResult.FilePath, opts.MaxOutputFileBytes);
                ctx.StagedFilePath = renderResult.FilePath;
                ctx.ContentType = renderResult.ContentType;
                ctx.FileName = renderResult.FileName;
            }

            if (hooks.AfterRenderAsync is not null)
                await hooks.AfterRenderAsync(ctx, runCt).ConfigureAwait(false);

            generationSlot?.Dispose();
            generationSlot = null;
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
            await InvalidateGenerationCacheAsync(generation.Id).ConfigureAwait(false);
            _metrics.IncrementCounter(ReportingConstants.Metrics.GenerationSucceeded, tags: [("format", format.ToString())]);
            ReportAuditHelper.RecordGenerated(services, generation.Id);
            return ReportingLyoMapper.ToRes(generation, request.IncludeReportData);
        }
        catch (Exception ex) {
            generationSlot?.Dispose();
            generationSlot = null;
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
                // Keep any uploaded output id on the Failed row so retention OnCleanupAsync can delete the blob later.
                generation.OutputFileId = ctx.OutputFileId;
                // CancellationToken.None: a client disconnect must not leave the row stranded in Running.
                await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
                await InvalidateGenerationCacheAsync(generation.Id).ConfigureAwait(false);
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
        finally {
            generationSlot?.Dispose();
        }
    }

    /// <summary>Re-runs a past generation from its stored snapshot (composition JSON, format, parameters) and produces a new generation row.</summary>
    /// <param name="generationId">Generation being replayed.</param>
    /// <param name="createdBy">Actor stamp for the new generation.</param>
    /// <param name="hooks">Generation hooks. Falls back to the registered instance.</param>
    /// <param name="includeReportData">Echo the composition JSON on the response. Off by default, matching <see cref="GenerateAsync" />.</param>
    /// <param name="ct">Token used to cancel the rerun.</param>
    public async Task<ReportGenerationRes> RerunAsync(
        Guid generationId,
        string? createdBy = null,
        ReportGenerationHooks? hooks = null,
        bool includeReportData = false,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var source = await db.ReportGenerations.AsNoTracking().Include(g => g.Parameters).FirstOrDefaultAsync(g => g.Id == generationId, ct).ConfigureAwait(false) ??
            throw new ReportValidationException($"Report generation {generationId} was not found.");

        var request = new GenerateReportReq {
            // Replay the stored snapshot verbatim. Skip the definition so a since-changed or inactive definition cannot alter the rerun.
            ReportDataJson = source.ReportDataJson,
            Format = TypeConversion.EnumOrDefault(source.Format, ReportFormat.Html),
            FileName = source.OriginalFileName,
            PathPrefix = source.PathPrefix,
            CreatedBy = createdBy,
            IncludeReportData = includeReportData,
            Parameters = source.Parameters.Select(p => new ReportGenerationParameterReq {
                    Key = p.Key,
                    Type = LyoTypeInfo.NormalizeFullName(p.Type),
                    Value = p.Value,
                    Description = p.Description,
                    EncryptedValue = p.EncryptedValue
                })
                .ToList()
        };

        // Reruns replay trusted stored snapshots, so they remain possible on hosts with AllowAdHocGeneration turned off.
        return await GenerateCoreAsync(request, hooks, true, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Generate writes via EF, not CRUD services, so list/GET query-cache tags are not invalidated automatically. Definition grids that project last-generation columns are tagged
    /// as <c>entity:reportdefinition</c>, not <c>entity:reportgeneration</c>.
    /// </summary>
    private async Task InvalidateGenerationCacheAsync(Guid generationId)
    {
        if (cache is null)
            return;

        try {
            var options = cacheOptions ?? new CacheOptions();
            await Task.WhenAll(
                    QueryCacheInvalidation.InvalidateQueryCachesForBroadEntityTypeAsync<ReportGeneration, ReportDefinition>(cache),
                    QueryCacheInvalidation.InvalidateQueryCachesForEntityKeysAsync(cache, options, typeof(ReportGeneration), [new object?[] { generationId }]))
                .ConfigureAwait(false);
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "Failed to invalidate reporting query cache after generation {GenerationId}", generationId);
        }
    }

    /// <summary>Request values override definition defaults by Key. Missing keys get the default Value from the definition.</summary>
    /// <summary>
    /// Overlays the supplied values on the definition's declared parameters, back-filling every declared parameter the caller omitted that carries a default. Expression defaults
    /// are rendered through <paramref name="resolver" /> and normalized to the declared type, so the generation snapshot records a typed value instead of a template.
    /// </summary>
    /// <param name="definitionParameters">Parameters declared on the definition. Empty for an ad-hoc generation.</param>
    /// <param name="requestParameters">Values supplied by the caller. Keys not on the definition are carried through unchanged.</param>
    /// <param name="resolver">Template renderer for expression defaults, typically resolved from DI. Null only fails parameters that declare an expression default.</param>
    /// <param name="errors">List collecting default-resolution failures. Null discards them, leaving the parameter unset for the type check to catch.</param>
    internal static List<ReportGenerationParameterReq> MergeParameters(
        IReadOnlyList<ReportDefinitionParameter> definitionParameters,
        IReadOnlyList<ReportGenerationParameterReq> requestParameters,
        LyoTemplateResolver? resolver = null,
        List<string>? errors = null)
    {
        var requestByKey = requestParameters.GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        // Two values for one key means the caller expected something we cannot honor. Keys are unique per generation, so the second value would be dropped silently.
        foreach (var duplicate in requestByKey.Where(kvp => kvp.Value.Count > 1))
            errors?.Add($"Parameter '{duplicate.Key}' was supplied {duplicate.Value.Count} times; keys must be unique (case-insensitive).");

        var result = new List<ReportGenerationParameterReq>();
        foreach (var def in definitionParameters) {
            if (requestByKey.TryGetValue(def.Key, out var provided) && provided.Count > 0) {
                var p = provided[0];
                result.Add(
                    new() {
                        Key = def.Key,
                        Type = LyoTypeInfo.NormalizeFullName(def.Type),
                        Value = p.Value,
                        Description = p.Description ?? def.Description,
                        EncryptedValue = p.EncryptedValue ?? def.EncryptedValue
                    });

                requestByKey.Remove(def.Key);
            }
            else {
                var spec = ReportParameterValidator.ToSpec(def);
                if (!def.Required && !LyoParameterDefaults.HasDefault(spec, def.Value, def.EncryptedValue is not null))
                    continue;

                if (!LyoParameterDefaults.TryResolve(spec, def.Value, resolver, out var value, out var error)) {
                    errors?.Add(error!);
                    continue;
                }

                result.Add(
                    new() {
                        Key = def.Key,
                        Type = LyoTypeInfo.NormalizeFullName(def.Type),
                        Value = value,
                        Description = def.Description,
                        EncryptedValue = def.EncryptedValue
                    });
            }
        }

        // Ad-hoc keys not on the definition (allowed for definition-less generate. Rejected by validation when a definition exists).
        foreach (var leftover in requestByKey.Values.SelectMany(x => x))
            result.Add(leftover);

        return result;
    }

    /// <summary>One row per key. Collection values are already JSON arrays and are written through as one payload.</summary>
    /// <exception cref="ReportValidationException">Two parameters share a key, which would silently drop one of the values.</exception>
    internal static string SerializeParametersJson(IReadOnlyList<ReportGenerationParameterReq> parameters)
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in parameters) {
            if (map.ContainsKey(parameter.Key))
                throw new ReportValidationException($"Parameter '{parameter.Key}' appears more than once; keys must be unique (case-insensitive).");

            map[parameter.Key] = ParseStoredJson(parameter.Value);
        }

        return JsonSerializer.Serialize(map);
    }

    private static object? ParseStoredJson(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        try {
            return JsonSerializer.Deserialize<JsonElement>(value);
        }
        catch (JsonException) {
            return value;
        }
    }

    /// <summary>Strips directory segments and invalid or control characters. Caps length at <see cref="MaxFileNameLength" /> while preserving the extension.</summary>
    /// <remarks>Invalid characters are dropped rather than replaced, so a caller-supplied path never becomes a name padded with underscores.</remarks>
    internal static string? SanitizeFileName(string? name)
        => PathHelpers.SanitizeFileName(name, null, true, true, MaxFileNameLength);

    /// <summary>
    /// Resolves a provider-supplied pre-rendered path and confirms it sits under an allowed root, so a provider cannot hand generation an arbitrary readable file (for example
    /// <c>/etc/passwd</c>) to publish as report output. Symlinks are resolved first, because a link inside an allowed root can point anywhere.
    /// </summary>
    /// <exception cref="ReportValidationException">The path is malformed or resolves outside every allowed root.</exception>
    internal static string ResolveConfinedPreRenderedPath(string path, PostgresReportingOptions opts)
    {
        string resolved;
        try {
            resolved = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or IOException) {
            throw new ReportValidationException($"Pre-rendered file path is not a usable path: {path}", ex);
        }

        try {
            resolved = new FileInfo(resolved).ResolveLinkTarget(true)?.FullName ?? resolved;
        }
        catch (IOException) {
            // Nothing to follow (missing file, or a broken link). The caller's File.Exists check reports that far more clearly than a confinement error would.
        }

        var roots = opts.PreRenderedFileRoots.Count > 0 ? opts.PreRenderedFileRoots : [Path.GetTempPath()];
        foreach (var root in roots) {
            if (PathHelpers.IsUnderRoot(PathStyle.Host, root, resolved))
                return resolved;
        }

        throw new ReportValidationException(
            $"Pre-rendered file path is outside the allowed roots ({string.Join(", ", roots)}). Configure PostgresReportingOptions.PreRenderedFileRoots to permit it.");
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

        if (!string.IsNullOrWhiteSpace(definition?.DefaultFormat) && TypeConversion.EnumOrNull<ReportFormat>(definition.DefaultFormat) is { } defFormat)
            return defFormat;

        if (profile?.DefaultFormat is { } profileFormat)
            return profileFormat;

        return ReportFormat.Html;
    }

    private static void EnsureJsonSize(string json, int maxBytes, string name)
    {
        var bytes = Encoding.UTF8.GetByteCount(json);
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

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values) {
            if (!string.IsNullOrWhiteSpace(v))
                return v;
        }

        return null;
    }
}