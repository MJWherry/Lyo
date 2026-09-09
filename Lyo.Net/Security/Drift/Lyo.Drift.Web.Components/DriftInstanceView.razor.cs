using System.Text.Json.Nodes;

namespace Lyo.Drift.Web.Components;

public partial class DriftInstanceView
{
    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    /// <summary>Instance row to display.</summary>
    [Parameter]
    [EditorRequired]
    public DriftInstanceRes Instance { get; set; } = null!;

    private Task ShowWatches() => ShowJsonAsync("Watches", Instance.WatchesJson);

    private Task ShowMetadata() => ShowJsonAsync("Metadata", Instance.MetadataJson);

    private async Task ShowJsonAsync(string title, string? json)
    {
        var node = ParseJson(json);
        var parameters = new DialogParameters<JsonViewDialog<JsonNode?>> { { d => d.Data, node } };
        await DialogService.ShowAsync<JsonViewDialog<JsonNode?>>(title, parameters, LyoDialogPresets.Medium);
    }

    private static JsonNode? ParseJson(string? json)
        => string.IsNullOrWhiteSpace(json) ? null : JsonNode.Parse(json);
}
