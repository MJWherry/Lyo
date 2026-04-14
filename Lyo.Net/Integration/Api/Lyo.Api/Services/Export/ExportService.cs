using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Enums;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Crud.Read;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Services.Crud.Validation;
using Lyo.Common.Enums;
using Lyo.Common.Records;
using Lyo.Csv.Models;
using Lyo.Formatter;
using Lyo.Metrics;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Xlsx.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lyo.Api.Services.Export;

/// <summary>
/// Exports query results to CSV, XLSX, or JSON format. When columns contain SmartFormat templates (e.g. "{FirstName} {LastName}"), they are converted to ComputedFields on
/// the query and resolved through the ProjectionService pipeline.
/// </summary>
public class ExportService<TContext>(
    IQueryService<TContext> queryService,
    ICsvService csvService,
    IXlsxService xlsxService,
    QueryOptions queryOptions,
    IFormatterService? formatterService = null,
    ILogger<ExportService<TContext>>? logger = null,
    JsonSerializerOptions? serializerOptions = null,
    IMetrics? metrics = null) : IExportService<TContext>
    where TContext : DbContext
{
    private static readonly (string, string)[] ExportTags = [("operation", "export")];

    /// <summary>Export failed after an inner query error — root summary becomes a transport-specific message while preserving <see cref="LyoProblemDetails.Errors" />.</summary>
    private static LyoProblemDetails AsExportFailure(LyoProblemDetails queryError)
    {
        if (queryError.Errors.Count <= 0) {
            return LyoProblemDetailsBuilder
                .CreateWithActivity()
                .WithErrorCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery)
                .WithMessage("Invalid export request.")
                .AddApiError(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery, queryError.Detail, queryError.Stacktrace)
                .Build();
        }

        var status = LyoProblemDetails.MapErrorCodeToHttpStatus(queryError.Errors[0].Code);
        return queryError with {
            Detail = "Invalid export request.",
            Title = LyoProblemDetails.HttpStatusTitle(status),
            Status = status
        };

    }
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
                "Export paging validation failed: {IssueCount} issue(s). {Details}",
                exportPagingErrors.Count,
                string.Join("; ", exportPagingErrors.Select(static e => $"{e.Code}: {e.Description}")));

            var problem = LyoProblemDetailsBuilder
                .CreateWithActivity()
                .WithErrorCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery)
                .WithMessage("Invalid export request.")
                .AddErrors(exportPagingErrors)
                .Build();

            throw new ApiErrorException(AsExportFailure(problem));
        }

        _metrics.IncrementCounter("api.export.requests", 1, ExportTags);
        using var timer = _metrics.StartTimer("api.export.duration", ExportTags);
        try {
            var requestOptions = request.Query.Options;
            var query = new ProjectionQueryReq {
                Start = request.Query.Start ?? 0,
                Amount = Math.Min(request.Query.Amount ?? queryOptions.MaxExportSize, queryOptions.MaxExportSize),
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
                var projectedResult = await queryService.QueryProjected<TDbEntity>(query, defaultOrder, defaultSortDirection, ct).ConfigureAwait(false);
                if (!projectedResult.IsSuccess) {
                    _metrics.IncrementCounter("api.export.failure", 1, ExportTags);
                    var err = projectedResult.Error
                        ?? LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.Unknown, "Export query failed.");
                    logger?.LogError("Export query failed: {Error}", err);
                    throw new ApiErrorException(AsExportFailure(err));
                }

                var items = projectedResult.Items ?? [];
                _metrics.RecordGauge("api.export.item_count", items.Count, ExportTags);
                var output = await ExportProjectedItemsAsync(items, request.Format, columnPlan, ct).ConfigureAwait(false);
                _metrics.IncrementCounter("api.export.success", 1, ExportTags);
                return output;
            }

            var result = await queryService.Query<TDbEntity, TResponse>(ToQueryReq(query), defaultOrder, defaultSortDirection, ct).ConfigureAwait(false);
            if (!result.IsSuccess) {
                _metrics.IncrementCounter("api.export.failure", 1, ExportTags);
                var err = result.Error ?? LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.Unknown, "Export query failed.");
                logger?.LogError("Export query failed: {Error}", err);
                throw new ApiErrorException(AsExportFailure(err));
            }

            var typedItems = result.Items ?? [];
            _metrics.RecordGauge("api.export.item_count", typedItems.Count, ExportTags);
            var output2 = await ExportTypedItemsAsync(typedItems, request.Format, GetColumnsDictionaryForTypedExport(request), ct).ConfigureAwait(false);
            _metrics.IncrementCounter("api.export.success", 1, ExportTags);
            return output2;
        }
        catch (Exception ex) when (ex is not InvalidOperationException) {
            _metrics.IncrementCounter("api.export.failure", 1, ExportTags);
            _metrics.RecordError("api.export.duration", ex, ExportTags);
            throw;
        }
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
        var extension = format.ToString().ToLowerInvariant();
        var fileName = $"export.{extension}";
        if (columnPlan is { ColumnMappings.Count: > 0 }) {
            var formatters = BuildProjectedColumnExtractors(columnPlan);
            return format switch {
                ExportFormat.Csv => await ExportCsvAsync(items, formatters, ct).ConfigureAwait(false),
                ExportFormat.Xlsx => await ExportXlsxAsync(items, formatters, ct).ConfigureAwait(false),
                ExportFormat.Json => ExportJson(items, fileName, serializerOptions),
                var _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported export format")
            };
        }

        return format switch {
            ExportFormat.Csv => await ExportCsvAsync(items, (Dictionary<string, Func<object?, string>>?)null, ct).ConfigureAwait(false),
            ExportFormat.Xlsx => await ExportXlsxAsync(items, (Dictionary<string, Func<object?, string>>?)null, ct).ConfigureAwait(false),
            ExportFormat.Json => ExportJson(items, fileName, serializerOptions),
            var _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported export format")
        };
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
        var extension = format.ToString().ToLowerInvariant();
        var fileName = $"export.{extension}";
        return format switch {
            ExportFormat.Csv => await ExportCsvAsync(items, columns, ct).ConfigureAwait(false),
            ExportFormat.Xlsx => await ExportXlsxAsync(items, columns, ct).ConfigureAwait(false),
            ExportFormat.Json => ExportJson(items, fileName, serializerOptions),
            var _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported export format")
        };
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

    private async Task<(Stream Stream, string ContentType, string FileName)> ExportCsvAsync<T>(
        IEnumerable<T> items,
        Dictionary<string, Func<T, string>>? formatters,
        CancellationToken ct)
    {
        var stream = new MemoryStream();
        if (formatters is not null)
            await csvService.ExportToCsvStreamAsync(items, formatters, stream, ct).ConfigureAwait(false);
        else {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead).ToList();
            await csvService.ExportToCsvStreamAsync(items, props, stream, ct).ConfigureAwait(false);
        }

        stream.Position = 0;
        return (stream, FileTypeInfo.Csv.MimeType, "export.csv");
    }

    private async Task<(Stream Stream, string ContentType, string FileName)> ExportCsvAsync<T>(IEnumerable<T> items, Dictionary<string, string>? columns, CancellationToken ct)
    {
        var stream = new MemoryStream();
        var resolved = ResolveColumns<T>(columns);
        if (resolved is not null)
            await csvService.ExportToCsvStreamAsync(items, resolved, stream, ct).ConfigureAwait(false);
        else {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead).ToList();
            await csvService.ExportToCsvStreamAsync(items, props, stream, ct).ConfigureAwait(false);
        }

        stream.Position = 0;
        return (stream, FileTypeInfo.Csv.MimeType, "export.csv");
    }

    private async Task<(Stream Stream, string ContentType, string FileName)> ExportXlsxAsync<T>(
        IEnumerable<T> items,
        Dictionary<string, Func<T, string>>? formatters,
        CancellationToken ct)
    {
        var stream = new MemoryStream();
        if (formatters is not null)
            await xlsxService.ExportToXlsxAsync(items, formatters, stream, null, ct).ConfigureAwait(false);
        else {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead).ToList();
            await xlsxService.ExportToXlsxAsync(items, props, stream, null, ct).ConfigureAwait(false);
        }

        stream.Position = 0;
        return (stream, FileTypeInfo.Xlsx.MimeType, "export.xlsx");
    }

    private async Task<(Stream Stream, string ContentType, string FileName)> ExportXlsxAsync<T>(IEnumerable<T> items, Dictionary<string, string>? columns, CancellationToken ct)
    {
        var stream = new MemoryStream();
        var resolved = ResolveColumns<T>(columns);
        if (resolved is not null)
            await xlsxService.ExportToXlsxAsync(items, resolved, stream, null, ct).ConfigureAwait(false);
        else {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead).ToList();
            await xlsxService.ExportToXlsxAsync(items, props, stream, null, ct).ConfigureAwait(false);
        }

        stream.Position = 0;
        return (stream, FileTypeInfo.Xlsx.MimeType, "export.xlsx");
    }

    private static (Stream Stream, string ContentType, string FileName) ExportJson<T>(IEnumerable<T> items, string fileName, JsonSerializerOptions? serializerOptions = null)
    {
        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, items, serializerOptions ?? JsonSerializerOptions.Default);
        stream.Position = 0;
        return (stream, FileTypeInfo.Json.MimeType, fileName);
    }

    private static QueryReq ToQueryReq(ProjectionQueryReq source)
        => new() {
            Start = source.Start,
            Amount = source.Amount,
            Options = new() {
                TotalCountMode = source.Options.TotalCountMode,
                IncludeFilterMode = source.Options.IncludeFilterMode
            },
            WhereClause = source.WhereClause,
            Include = [..source.Include],
            Keys = [..source.Keys.Select(k => k.ToArray())],
            SortBy = [..source.SortBy.Select(s => new SortBy { PropertyName = s.PropertyName, Direction = s.Direction, Priority = s.Priority })]
        };

    private sealed record ExportColumnPlan(HashSet<string> RequiredSelects, List<ComputedField> ComputedFields, List<(string Header, string LookupKey)> ColumnMappings);
}