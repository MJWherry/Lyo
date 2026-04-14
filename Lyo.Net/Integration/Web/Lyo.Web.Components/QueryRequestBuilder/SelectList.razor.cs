using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.QueryRequestBuilder;

public partial class SelectList
{
    [Parameter]
    public IEnumerable<string> Select { get; set; } = [];

    [Parameter]
    public IEnumerable<string> SelectAll { get; set; } = [];

    [Parameter]
    public EventCallback<List<string>> SelectChanged { get; set; }

    [Parameter]
    public EventCallback<List<string>> SelectAllChanged { get; set; }

    private async Task OnValuesChanged(IEnumerable<string> values)
    {
        var list = values.ToList();
        await SelectAllChanged.InvokeAsync(list);
    }

    private Task OnSelectedChanged(IEnumerable<string> selected) => SelectChanged.InvokeAsync(selected.ToList());
}