using Lyo.Tag;

namespace Lyo.HomeInventory.Web.Components;

/// <summary>Nested tags for one inventory subject.</summary>
public partial class HomeTagsPanel
{
    private List<string> _tagNames = [];

    private List<TagRecord> _tags = [];

    /// <summary>HTTP client.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Full tags collection route, e.g. <c>HomeInventory/Items/{id}/Tags</c>.</summary>
    [Parameter]
    [EditorRequired]
    public string TagsRoute { get; set; } = "";

    protected override Task OnParametersSetAsync() => Reload();

    private async Task Reload()
    {
        var rows = await ApiClient.GetAsAsync<List<TagRecord>>(TagsRoute);
        _tags = rows ?? [];
        _tagNames = _tags.Select(t => t.Name).ToList();
    }

    private async Task OnTagsChanged(IEnumerable<string> values)
    {
        var next = values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var previous = _tagNames;
        var added = next.Except(previous, StringComparer.OrdinalIgnoreCase).ToList();
        var removed = previous.Except(next, StringComparer.OrdinalIgnoreCase).ToList();
        _tagNames = next;
        foreach (var name in added)
            await ApiClient.PostAsAsync<HomeTagAddReq, List<TagRecord>>(TagsRoute, new() { Name = name });

        foreach (var name in removed)
            await ApiClient.DeleteAsAsync<object>($"{TagsRoute}/{Uri.EscapeDataString(name)}");

        await Reload();
    }
}