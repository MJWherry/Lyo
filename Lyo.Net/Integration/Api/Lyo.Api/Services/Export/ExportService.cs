using System.Linq.Expressions;
using System.Reflection;
using Lyo.Api.Models;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Enums;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Crud.Read;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Services.Crud.Validation;
using Lyo.Common.Enums;
using Lyo.Formatter;
using Lyo.Metrics;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lyo.Api.Services.Export;

/// <inheritdoc cref="IExportService{TContext}" />
/// <remarks>
/// When columns contain SmartFormat templates (e.g. "{FirstName} {LastName}"), they are converted to ComputedFields on the query and resolved through the ProjectionService
/// pipeline.
/// </remarks>
public class ExportService<TContext>(
    IQueryService<TContext> queryService,
    IEnumerable<IExportFormatHandler> formatHandlers,
    QueryOptions queryOptions,
    IFormatterService? formatterService = null,
    ILogger<ExportService<TContext>>? logger = null,
    IMetrics? metrics = null) : IExportService<TContext>
    where TContext : DbContext
{
    private static readonly (string, string)[] ExportTags = [("operation", "export")];
    private readonly Dictionary<ExportFormat, IExportFormatHandler> _formatHandlers = formatHandlers.ToDictionary(h => h.Format);
    private readonly IMetrics _metrics = metrics ?? NullMetrics.Instance;

    public async Task<(Stream Stream, string ContentType, string FileName)> ExportAsync<TDbEntity, TResponse>(
        ExportRequest request,
        Expression<Func<TDbEntity, object?>> defaultOrder,
        SortDirection defaultSortDirection = SortDirection.Desc,
        CancellationToken ct = default)
        where TDbEntity : class
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Query);
        var exportPagingErrors = QueryPagingBoundsValidator.Validate(request.Query, queryOptions, queryOptions.MaxExportSize);
        if (exportPagingErrors.Count > 0) {
            logger?.LogWarning(
                "Export paging validation failed: {IssueCount} issue(s). {Details}", exportPagingErrors.Count,
                string.Join("; ", exportPagingErrors.Select(static e => $"{e.Code}: {e.Description}")));

            var problem = LyoProblemDetailsBuilder.CreateWithActivity()
                .WithErrorCode(Constants.ApiErrorCodes.InvalidQuery)
                .WithMessage("Invalid export request.")
                .AddErrors(exportPagingErrors)
                .Build();

            throw new ApiErrorException(AsExportFailure(problem));
        }

        _metrics.IncrementCounter("api.export.requests", 1, ExportTags);
        using var timer = _metrics.StartTimer("api.export.duration", ExportTags);
        try {
            var requestOptions = request.Query.Options;
            var amount = Math.Min(request.Query.Amount ?? queryOptions.MaxExportSize, queryOptions.MaxExportSize);
            // Keyed export should page exactly to the key set — oversized Amount trips include page-size validation.
            if (request.Query.Keys.Count > 0)
                amount = Math.Min(Math.Max(request.Query.Keys.Count, queryOptions.MinPagingAmount), queryOptions.MaxExportSize);
            // Projected selects with navigations derive includes; keep under MaxIncludePageSize when not key-scoped.
            else if (queryOptions.MaxIncludePageSize > 0 && (request.Query.Include.Count > 0 || request.Query.Select.Any(static s => s.Contains('.', StringComparison.Ordinal))))
                amount = Math.Min(amount, queryOptions.MaxIncludePageSize);

            var query = new ProjectionQueryReq {
                Start = request.Query.Start ?? 0,
                Amount = amount,
                Keys = request.Query.Keys,
                WhereClause = request.Query.WhereClause,
                Include = request.Query.Include,
                Select = request.Query.Select.ToList(),
                ComputedFields = request.Query.ComputedFields.Select(c => new ComputedField(c.Name, c.Template)).ToList(),
                SortBy = request.Query.SortBy,
                Options = new() {
                    TotalCountMode = requestOptions.TotalCountMode,
                    IncludeFilterMode = requestOptions.IncludeFilterMode,
                    ZipSiblingCollectionSelections = requestOptions.ZipSiblingCollectionSelections
                }
            };

            var columnRows = GetExportColumnRows(request);
            var columnPlan = BuildColumnPlan(columnRows);
            if (columnPlan != null) {
                foreach (var selectField in columnPlan.RequiredSelects) {
                    if (!query.Select.Contains(selectField, StringComparer.OrdinalIgnoreCase))
                        query.Select.Add(selectField);
                }

                foreach (var cf in columnPlan.ComputedFields)
                    query.ComputedFields.Add(cf);
            }

            if (query.Select.Count > 0 || query.ComputedFields.Count > 0) {
                var projectedResult = await queryService.QueryProjected(query, defaultOrder, defaultSortDirection, ct).ConfigureAwait(false);
                if (!projectedResult.IsSuccess) {
                    _metrics.IncrementCounter("api.export.failure", 1, ExportTags);
                    var err = projectedResult.Error ?? LyoProblemDetails.FromCode(Constants.ApiErrorCodes.Unknown, "Export query failed.");
                    logger?.LogError("Export query failed: {Error}", err);
                    throw new ApiErrorException(AsExportFailure(err));
                }

                var items = projectedResult.Items ?? [];
                _metrics.RecordGauge("api.export.item_count", items.Count, ExportTags);
                var output = await ExportProjectedItemsAsync(items, request.Format, columnPlan, ct).ConfigureAwait(false);
                _metrics.IncrementCounter("api.export.success", 1, ExportTags);
                return output;
            }

            var result = await queryService.Query<TDbEntity, TResponse>(ToQueryConcreteReq(query), defaultOrder, defaultSortDirection, ct).ConfigureAwait(false);
            if (!result.IsSuccess) {
                _metrics.IncrementCounter("api.export.failure", 1, ExportTags);
                var err = result.Error ?? LyoProblemDetails.FromCode(Constants.ApiErrorCodes.Unknown, "Export query failed.");
                logger?.LogError("Export query failed: {Error}", err);
                throw new ApiErrorException(AsExportFailure(err));
            }

            var typedItems = result.Items ?? [];
            _metrics.RecordGauge("api.export.item_count", typedItems.Count, ExportTags);
            var output2 = await ExportTypedItemsAsync(typedItems, request.Format, GetColumnsDictionaryForTypedExport(request), ct).ConfigureAwait(false);
            _metrics.IncrementCounter("api.export.success", 1, ExportTags);
            return output2;
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not NotSupportedException) {
            _metrics.IncrementCounter("api.export.failure", 1, ExportTags);
            _metrics.RecordError("api.export.duration", ex, ExportTags);
            throw;
        }
    }

    /// <summary>Export failed after an inner query error — root summary becomes a transport-specific message while preserving <see cref="LyoProblemDetails.Errors" />.</summary>
    private static LyoProblemDetails AsExportFailure(LyoProblemDetails queryError)
    {
        if (queryError.Errors.Count <= 0) {
            return LyoProblemDetailsBuilder.CreateWithActivity()
                .WithErrorCode(Constants.ApiErrorCodes.InvalidQuery)
                .WithMessage("Invalid export request.")
                .AddApiError(Constants.ApiErrorCodes.InvalidQuery, queryError.Detail, queryError.Stacktrace)
                .Build();
        }

        var status = LyoProblemDetails.MapErrorCodeToHttpStatus(queryError.Errors[0].Code);
        return queryError with { Detail = "Invalid export request.", Title = LyoProblemDetails.HttpStatusTitle(status), Status = status };
    }

    private static IReadOnlyList<(string Header, string Value)>? GetExportColumnRows(ExportRequest request)
    {
        if (request.ColumnList is { Count: > 0 }) {
            var list = new List<(string Header, string Value)>();
            foreach (var c in request.ColumnList) {
                if (string.IsNullOrWhiteSpace(c.Value))
                    continue;

                var header = string.IsNullOrWhiteSpace(c.Header) ? c.Value.Trim() : c.Header.Trim();
                list.Add((header, c.Value.Trim()));
            }

            return list.Count > 0 ? list : null;
        }

        if (request.Columns is not { Count: > 0 })
            return null;

        return request.Columns.Where(kv => !string.IsNullOrWhiteSpace(kv.Value)).Select(kv => (kv.Key.Trim(), kv.Value.Trim())).ToList();
    }

    private static Dictionary<string, string>? GetColumnsDictionaryForTypedExport(ExportRequest request)
    {
        var rows = GetExportColumnRows(request);
        if (rows is null or { Count: 0 })
            return request.Columns;

        return rows.ToDictionary(t => t.Header, t => t.Value, StringComparer.Ordinal);
    }

    /// <summary>Analyzes export columns to determine which are simple property lookups vs SmartFormat templates. Returns null when no columns are specified.</summary>
    private ExportColumnPlan? BuildColumnPlan(IReadOnlyList<(string Header, string Value)>? columnRows)
    {
        if (columnRows is null or { Count: 0 })
            return null;

        var requiredSelects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var computedFields = new List<ComputedField>();
        var columnMappings = new List<(string Header, string LookupKey)>();
        foreach (var (header, value) in columnRows) {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            var isTemplate = value.Contains('{');
            if (isTemplate && formatterService != null) {
                var placeholders = formatterService.GetPlaceholders(value);
                foreach (var p in placeholders)
                    requiredSelects.Add(p);

                computedFields.Add(new(header, value));
                columnMappings.Add((header, header));
            }
            else {
                requiredSelects.Add(value);
                columnMappings.Add((header, value));
            }
        }

        return new(requiredSelects, computedFields, columnMappings);
    }

    private async Task<(Stream Stream, string ContentType, string FileName)> ExportProjectedItemsAsync(
        IReadOnlyList<object?> items,
        ExportFormat format,
        ExportColumnPlan? columnPlan,
        CancellationToken ct)
    {
        var handler = GetFormatHandler(format);
        if (columnPlan is { ColumnMappings.Count: > 0 }) {
            var formatters = BuildProjectedColumnExtractors(columnPlan);
            return await handler.WriteProjectedAsync(items, formatters, ct).ConfigureAwait(false);
        }

        return await handler.WriteProjectedAsync(items, null, ct).ConfigureAwait(false);
    }

    private static Dictionary<string, Func<object?, string>> BuildProjectedColumnExtractors(ExportColumnPlan plan)
    {
        // Ordinal headers so export columns whose titles differ only by case stay distinct (Csv uses this key order).
        var result = new Dictionary<string, Func<object?, string>>(StringComparer.Ordinal);
        foreach (var (header, lookupKey) in plan.ColumnMappings) {
            var capturedKey = lookupKey;
            result[header] = item => {
                if (item is not IReadOnlyDictionary<string, object?> dict)
                    return string.Empty;

                if (dict.TryGetValue(capturedKey, out var val))
                    return val?.ToString() ?? string.Empty;

                return string.Empty;
            };
        }

        return result;
    }

    private async Task<(Stream Stream, string ContentType, string FileName)> ExportTypedItemsAsync<T>(
        IReadOnlyList<T> items,
        ExportFormat format,
        Dictionary<string, string>? columns,
        CancellationToken ct)
    {
        var handler = GetFormatHandler(format);
        var resolved = ResolveColumns<T>(columns);
        return await handler.WriteTypedAsync(items, resolved, ct).ConfigureAwait(false);
    }

    private Dictionary<string, PropertyInfo>? ResolveColumns<T>(Dictionary<string, string>? columns)
    {
        if (columns is null or { Count: 0 })
            return null;

        var responseType = typeof(T);
        var resolved = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in columns) {
            var prop = responseType.GetProperty(kv.Value, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop is not null && prop.CanRead)
                resolved[kv.Key] = prop;
            else
                logger?.LogWarning("Skipping unknown or unreadable property {Property} for export", kv.Value);
        }

        return resolved.Count > 0 ? resolved : null;
    }

    private IExportFormatHandler GetFormatHandler(ExportFormat format)
    {
        if (_formatHandlers.TryGetValue(format, out var handler))
            return handler;

        var addonHint = format switch {
            ExportFormat.Csv => "AddCsvExport()",
            ExportFormat.Xlsx => "AddXlsxExport()",
            var _ => "register an IExportFormatHandler"
        };

        throw new NotSupportedException($"Export format '{format}' is not supported. Call {addonHint} on the service collection.");
    }

    private static QueryConcreteReq ToQueryConcreteReq(ProjectionQueryReq source)
        => new() {
            Start = source.Start,
            Amount = source.Amount,
            Options = new() { TotalCountMode = source.Options.TotalCountMode, IncludeFilterMode = source.Options.IncludeFilterMode },
            WhereClause = source.WhereClause,
            Include = [..source.Include],
            Keys = [..source.Keys.Select(k => k.ToArray())],
            SortBy = [..source.SortBy.Select(s => new SortBy { PropertyName = s.PropertyName, Direction = s.Direction, Priority = s.Priority })]
        };

    private sealed record ExportColumnPlan(HashSet<string> RequiredSelects, List<ComputedField> ComputedFields, List<(string Header, string LookupKey)> ColumnMappings);
}