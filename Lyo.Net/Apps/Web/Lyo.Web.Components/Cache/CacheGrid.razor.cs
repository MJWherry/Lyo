using System.Collections;
using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Common.Core.Conversion;
using Lyo.Common.Metadata.Records;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.Cache;

public partial class CacheGrid
{
    private static readonly string[] KeyFields = ["Type", "Name"];
    private static readonly string[] QuickSearchFields = ["Name"];
    private const LyoDataGridFeatureFlags GridFeatures =
        LyoDataGridFeatureFlags.Filterable | LyoDataGridFeatureFlags.Searchable | LyoDataGridFeatureFlags.AutoRefresh
        | LyoDataGridFeatureFlags.BulkMenu | LyoDataGridFeatureFlags.BulkDelete;

    private readonly List<FilterPropertyDefinition> _filters = [
        new("Type", "Type"),
        new("Name", "Name"),
        new("Tags", "Tags"),
        new("Encrypted", "Encrypted"),
        new("Compressed", "Compressed"),
        new("SizeBytes", "Size"),
        new("Created", "Created"),
        new("Expires", "Expires")
    ];

    private LyoDataGridProjected? _dataGrid;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public string Route { get; set; } = "Cache";

    [Parameter]
    public string GridKey { get; set; } = "CacheGrid";

    private static object[] GetKey(object? item) => ProjectedGridKeys.Composite(item, "Type", "Name");

    private static IReadOnlyList<string> GetTags(object? item)
    {
        var raw = ProjectedValueHelper.GetValue(item, "Tags");
        if (raw is null)
            return [];

        var tags = new List<string>();
        foreach (var value in EnumerateTagValues(raw)) {
            if (!string.IsNullOrWhiteSpace(value))
                tags.Add(value);
        }

        return tags;
    }

    private static IEnumerable<string> EnumerateTagValues(object raw)
    {
        if (raw is string s) {
            yield return s;
            yield break;
        }

        if (raw is JsonElement je) {
            if (je.ValueKind == JsonValueKind.String) {
                var text = je.GetString();
                if (text != null)
                    yield return text;
            }
            else if (je.ValueKind == JsonValueKind.Array) {
                foreach (var el in je.EnumerateArray()) {
                    var text = el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
                    if (text != null)
                        yield return text;
                }
            }

            yield break;
        }

        if (raw is IEnumerable enumerable) {
            foreach (var el in enumerable) {
                var text = el?.ToString();
                if (text != null)
                    yield return text;
            }
        }
    }

    private static bool? GetNullableBool(object? item, string field)
    {
        var raw = ProjectedValueHelper.GetValue(item, field);
        if (raw is null)
            return null;
        if (raw is bool b)
            return b;
        if (raw is JsonElement je) {
            if (je.ValueKind is JsonValueKind.True)
                return true;
            if (je.ValueKind is JsonValueKind.False)
                return false;
            if (je.ValueKind is JsonValueKind.Null)
                return null;
        }

        return TypeConversion.TryConvertTo<bool>(raw, out var converted) ? converted : null;
    }

    private static string FormatSize(object? item)
    {
        var raw = ProjectedValueHelper.GetValue(item, "SizeBytes");
        return ProjectedValueHelper.TryGetInt64(raw, out var size)
            ? FileSizeUnitInfo.FormatBestFitAbbreviation(size, lowercaseAbbreviation: false)
            : "—";
    }

    private async Task ClearAllAsync()
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Clear cache", "Remove every L1 entry this API process is tracking?", yesText: "Clear", cancelText: "Cancel");
        if (confirmed != true)
            return;

        var result = await ApiClient.PostAsAsync<CacheMutationRes>($"{Route.TrimEnd('/')}/Clear");
        Snackbar.Add($"Cleared {result.RemovedCount} cache item(s).", Severity.Success);
        if (_dataGrid != null)
            await _dataGrid.RefreshData();
    }

    private async Task DeleteRowAsync(object? row)
    {
        var key = GetKey(row);
        var name = ProjectedValueHelper.GetDisplayValue(row, "Name");
        if (key.Length == 0 || _dataGrid == null)
            return;

        var confirmed = await DialogService.ShowMessageBoxAsync("Delete", $"Remove '{name}'?", yesText: "Delete", cancelText: "Cancel");
        if (confirmed != true)
            return;

        await ApiClient.DeleteAsAsync<DeleteRequest, DeleteResult<object?>>(Route.TrimEnd('/'), new() { Keys = [key] });
        Snackbar.Add("Deleted.", Severity.Success);
        await _dataGrid.RefreshData();
    }
}
