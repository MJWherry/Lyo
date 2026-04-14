using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.QueryRequestBuilder;

public partial class IncludeList
{
    [Parameter]
    public IEnumerable<string> Include { get; set; } = [];

    [Parameter]
    public IEnumerable<string> IncludeAll { get; set; } = [];

    [Parameter]
    public EventCallback<List<string>> IncludeChanged { get; set; }

    [Parameter]
    public EventCallback<List<string>> IncludeAllChanged { get; set; }

    private async Task OnValuesChanged(IEnumerable<string> values)
    {
        var list = values.ToList();
        await IncludeAllChanged.InvokeAsync(list);
    }

    private Task OnSelectedChanged(IEnumerable<string> selected) => IncludeChanged.InvokeAsync(selected.ToList());
}