using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.ParamTable;

/// <summary>
/// Key editor for one parameter row, shared by the table and card layouts of <see cref="LyoParameterEditor" />. Draws a free-text field normally and a select
/// when <see cref="InheritFrom" /> declares the keys an override may use.
/// </summary>
public partial class LyoParameterKeyField
{
    /// <summary>Current parameter key.</summary>
    [Parameter]
    public string Key { get; set; } = "";

    /// <summary>Raised with the new key. Hosts re-apply inherited type and options after this.</summary>
    [Parameter]
    public EventCallback<string?> KeyChanged { get; set; }

    /// <summary>Declared parameters this row may override. When non-empty the key becomes a select of their keys.</summary>
    [Parameter]
    public IReadOnlyList<LyoParameterEditRow>? InheritFrom { get; set; }

    /// <summary>Field label. Null in the table layout, where the column header already names the field.</summary>
    [Parameter]
    public string? Label { get; set; }

    private bool HasUnlistedKey
        => !string.IsNullOrEmpty(Key) && InheritFrom?.All(p => !string.Equals(p.Key, Key, StringComparison.OrdinalIgnoreCase)) == true;

    private Task OnKeyChanged(string? value) => KeyChanged.InvokeAsync(value);
}
