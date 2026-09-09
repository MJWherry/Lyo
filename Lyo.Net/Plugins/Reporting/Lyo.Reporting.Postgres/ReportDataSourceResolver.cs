using System.Text.Json;
using Lyo.Api.Services.Crud;
using Lyo.Common.Core.Conversion;
using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.Parameters;
using Lyo.Query.Models.Parameters;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;
using Lyo.Reporting.Models.Request;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Reporting.Postgres;

/// <summary>Runs Query or Sproc Options to picker items and row dictionaries.</summary>
public sealed class ReportParameterOptionsExecutor(IServiceProvider services)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>Resolves <paramref name="options" /> against sibling values. Fail-closed: <see cref="ParameterOptionsResolveRes.Error" /> is set instead of throwing for missing services.</summary>
    public async Task<ParameterOptionsResolveRes> ResolveAsync(ParameterOptions options, IReadOnlyDictionary<string, string?> siblingValues, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(siblingValues);
        var res = new ParameterOptionsResolveRes();
        try {
            ParameterOptionsJson.Validate(options);
        }
        catch (ArgumentException ex) {
            res.Error = ex.Message;
            return res;
        }

        switch (options.Kind) {
            case ParameterOptionsKind.Static:
                res.Items.AddRange(options.Items.Where(i => !string.IsNullOrEmpty(i.Key)));
                return res;
            case ParameterOptionsKind.Query:
                return await ResolveQueryAsync(options, siblingValues, ct).ConfigureAwait(false);
            case ParameterOptionsKind.Sproc:
                return await ResolveSprocAsync(options, siblingValues, ct).ConfigureAwait(false);
            default:
                res.Error = $"Unsupported options kind '{options.Kind}'.";
                return res;
        }
    }

    private async Task<ParameterOptionsResolveRes> ResolveQueryAsync(ParameterOptions options, IReadOnlyDictionary<string, string?> siblingValues, CancellationToken ct)
    {
        var res = new ParameterOptionsResolveRes();
        if (options.Query is null) {
            res.Error = "Query options are missing a QueryReq template.";
            return res;
        }

        if (!ParameterOptionsBinder.TryBind(options.Query, siblingValues, out var bound, out var missing) || bound is null) {
            res.Error = missing.Count > 0 ? $"Set {string.Join(", ", missing)} first." : "Unable to bind query options.";
            return res;
        }

        var rootQuery = services.GetService<IReportRootQuery>();
        if (rootQuery is null) {
            res.Error = "IReportRootQuery is not registered.";
            return res;
        }

        IReadOnlyList<object?> rows;
        try {
            rows = await rootQuery.QueryAsync(bound, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            res.Error = ex.Message;
            return res;
        }

        foreach (var row in rows) {
            var dict = ToDictionary(row);
            res.Rows.Add(dict);
            if (ParameterOptionsBinder.TryReadKeyValue(row, bound.Select, out var key, out var label))
                res.Items.Add(new(key, string.IsNullOrEmpty(label) ? key : label));
        }

        return res;
    }

    private async Task<ParameterOptionsResolveRes> ResolveSprocAsync(ParameterOptions options, IReadOnlyDictionary<string, string?> siblingValues, CancellationToken ct)
    {
        var res = new ParameterOptionsResolveRes();
        if (!ParameterOptionsJson.IsValidStoredProcName(options.StoredProcName)) {
            res.Error = $"Invalid stored procedure name '{options.StoredProcName}'.";
            return res;
        }

        if (!ParameterOptionsBinder.TryBindSprocParameters(options.SprocParameters, siblingValues, out var args, out var missing)) {
            res.Error = missing.Count > 0 ? $"Set {string.Join(", ", missing)} first." : "Unable to bind sproc options.";
            return res;
        }

        var sproc = services.GetService<ISprocService>();
        if (sproc is null) {
            res.Error = "ISprocService is not registered.";
            return res;
        }

        IReadOnlyList<Dictionary<string, object?>> rows;
        try {
            rows = await sproc.ExecuteStoredProcAsync<Dictionary<string, object?>>(options.StoredProcName!.Trim(), args, ct: ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            res.Error = ex.Message;
            return res;
        }

        foreach (var row in rows) {
            res.Rows.Add(row);
            if (ParameterOptionsBinder.TryReadKeyValue(row, options.EffectiveKeyField, options.EffectiveLabelField, out var key, out var label))
                res.Items.Add(new(key, string.IsNullOrEmpty(label) ? key : label));
        }

        return res;
    }

    internal static Dictionary<string, object?> ToDictionary(object? row)
    {
        if (row is Dictionary<string, object?> dict)
            return dict;

        if (row is IReadOnlyDictionary<string, object?> ro)
            return ro.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

        if (row is JsonElement { ValueKind: JsonValueKind.Object } el) {
            var mapped = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in el.EnumerateObject())
                mapped[prop.Name] = TypeConversion.FromJsonElement(prop.Value);

            return mapped;
        }

        if (row is null)
            return [];

        try {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(row, JsonOptions), JsonOptions)
                ?? [];
        }
        catch (JsonException) {
            return [];
        }
    }
}

/// <inheritdoc />
public sealed class ReportDataSourceResolver(IServiceProvider services) : IReportDataSourceResolver
{
    /// <inheritdoc />
    public async Task<string> ApplyAsync(
        string reportDataJson,
        IReadOnlyList<ReportParameterOptionsRef> definitionParameters,
        IList<ReportGenerationParameterReq> mergedParameters,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(reportDataJson);
        ArgumentHelpers.ThrowIfNull(definitionParameters);
        ArgumentHelpers.ThrowIfNull(mergedParameters);
        var report = ReportJson.Deserialize<object>(reportDataJson);
        var fromParameterKeys = new HashSet<string>(CollectFromParameterKeys(report.Sections), StringComparer.OrdinalIgnoreCase);
        var queryTables = SectionBody.CollectTables(report.Sections)
            .Where(t => t.DataSourceKind is DataSourceKind.Query or DataSourceKind.Sproc)
            .ToList();
        var datasetDefs = definitionParameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Key) && ParameterOptionsJson.TryDeserialize(p.Options, out var o) && o is not null &&
                o.Kind is ParameterOptionsKind.Query or ParameterOptionsKind.Sproc &&
                (IsDataset(p.Type) || fromParameterKeys.Contains(p.Key)))
            .ToList();
        if (fromParameterKeys.Count == 0 && datasetDefs.Count == 0 && queryTables.Count == 0)
            return reportDataJson;
        var siblings = mergedParameters.ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);
        var executor = new ReportParameterOptionsExecutor(services);
        var optionsByKey = definitionParameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Key))
            .ToDictionary(p => p.Key, p => p, StringComparer.OrdinalIgnoreCase);

        foreach (var param in mergedParameters) {
            if (!optionsByKey.TryGetValue(param.Key, out var def) || !ParameterOptionsJson.TryDeserialize(def.Options, out var options) || options is null)
                continue;

            if (options.Kind is not ParameterOptionsKind.Query and not ParameterOptionsKind.Sproc)
                continue;

            if (!IsDataset(def.Type) && !fromParameterKeys.Contains(param.Key))
                continue;

            var resolved = await executor.ResolveAsync(options, siblings, ct).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(resolved.Error))
                throw new ReportValidationException($"Parameter '{param.Key}': {resolved.Error}");

            param.Value = JsonSerializer.Serialize(resolved.Rows);
            siblings[param.Key] = param.Value;
        }

        await FillTablesAsync(report.Sections, siblings, executor, ct).ConfigureAwait(false);
        FillCharts(report.Sections, siblings);
        return ReportJson.Serialize(report);
    }

    internal static bool IsDataset(string? typeName)
    {
        var info = LyoTypeInfo.FromName(typeName);
        return info.IsCollection || info.EditorKind is LyoTypeEditorKind.JsonArray or LyoTypeEditorKind.JsonObject or LyoTypeEditorKind.Collection;
    }

    private static IEnumerable<string> CollectFromParameterKeys(IEnumerable<Section> sections)
    {
        foreach (var control in WalkAllControls(sections)) {
            switch (control) {
                case Table { DataSourceKind: DataSourceKind.FromParameter, DataParameterKey: { Length: > 0 } key }:
                    yield return key;
                    break;
                case Block { ContentType: ContentType.Chart, DataSourceKind: DataSourceKind.FromParameter, DataParameterKey: { Length: > 0 } key }:
                    yield return key;
                    break;
            }
        }
    }

    private static IEnumerable<Control> WalkAllControls(IEnumerable<Section> sections)
    {
        foreach (var section in SectionBody.WalkSections(sections)) {
            foreach (var control in SectionBody.WalkControls(section.Controls))
                yield return control;
        }
    }

    private static async Task FillTablesAsync(
        IEnumerable<Section> sections,
        IReadOnlyDictionary<string, string?> siblings,
        ReportParameterOptionsExecutor executor,
        CancellationToken ct)
    {
        foreach (var table in SectionBody.CollectTables(sections)) {
            switch (table.DataSourceKind) {
                case DataSourceKind.FromParameter:
                    FillFromParameter(table, siblings);
                    break;
                case DataSourceKind.Query:
                case DataSourceKind.Sproc:
                    await FillFromOptionsAsync(table, siblings, executor, ct).ConfigureAwait(false);
                    break;
            }
        }
    }

    private static void FillFromParameter(Table table, IReadOnlyDictionary<string, string?> siblings)
    {
        if (string.IsNullOrWhiteSpace(table.DataParameterKey) || !siblings.TryGetValue(table.DataParameterKey, out var json) || string.IsNullOrWhiteSpace(json)) {
            TableDataBinder.Apply(table, []);
            return;
        }

        TableDataBinder.Apply(table, TableDataBinder.ParseRows(json));
    }

    private static async Task FillFromOptionsAsync(
        Table table,
        IReadOnlyDictionary<string, string?> siblings,
        ReportParameterOptionsExecutor executor,
        CancellationToken ct)
    {
        if (!ParameterOptionsJson.TryDeserialize(table.Options, out var options) || options is null)
            throw new ReportValidationException("Table Query/Sproc options are missing or invalid.");

        var resolved = await executor.ResolveAsync(options, siblings, ct).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(resolved.Error))
            throw new ReportValidationException(resolved.Error);

        TableDataBinder.Apply(table, resolved.Rows);
    }

    private static void FillCharts(IEnumerable<Section> sections, IReadOnlyDictionary<string, string?> siblings)
    {
        foreach (var block in WalkAllControls(sections).OfType<Block>()) {
            if (block.ContentType != ContentType.Chart || block.DataSourceKind != DataSourceKind.FromParameter)
                continue;

            if (string.IsNullOrWhiteSpace(block.DataParameterKey) || !siblings.TryGetValue(block.DataParameterKey, out var json) || string.IsNullOrWhiteSpace(json)) {
                ReportChartMarkup.ApplyFromRows(block, []);
                continue;
            }

            ReportChartMarkup.ApplyFromRows(block, TableDataBinder.ParseRows(json));
        }
    }
}
