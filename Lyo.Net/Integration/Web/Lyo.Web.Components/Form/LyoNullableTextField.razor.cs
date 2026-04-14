using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lyo.Web.Components.Form;

public partial class LyoNullableTextField
{
    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public Variant Variant { get; set; } = Variant.Outlined;

    [Parameter]
    public Margin Margin { get; set; } = Margin.Dense;

    [Parameter]
    public string? Placeholder { get; set; }

    private Task HandleValueChanged(string? value) => ValueChanged.InvokeAsync(string.IsNullOrWhiteSpace(value) ? "" : value);

    private async Task HandleClearClick(MouseEventArgs _)
    {
        if (ReadOnly || Disabled)
            return;

        await ValueChanged.InvokeAsync(null);
    }
}