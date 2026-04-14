using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.QueryRequestBuilder;

public partial class StartAmountFields
{
    [Parameter]
    public int? Start { get; set; }

    [Parameter]
    public int? Amount { get; set; }

    [Parameter]
    public EventCallback<(int? Start, int? Amount)> ValueChanged { get; set; }

    private Task OnStartChanged(int? value) => ValueChanged.InvokeAsync((value, Amount));

    private Task OnAmountChanged(int? value) => ValueChanged.InvokeAsync((Start, value));
}