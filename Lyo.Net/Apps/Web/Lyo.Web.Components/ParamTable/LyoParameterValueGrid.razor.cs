using Lyo.Api.Client;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.ParamTable;

public partial class LyoParameterValueGrid
{
    /// <summary>Entries to edit. Nothing draws when the list is empty, so callers do not need their own guard.</summary>
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<LyoParameterEntry> Entries { get; set; } = [];

    /// <summary>Client used by option lists that resolve their choices from a query. Required only when a definition carries query-backed options.</summary>
    [Parameter]
    public IApiClient? ApiClient { get; set; }

    /// <summary>Heading above the grid. Omit for a bare grid.</summary>
    [Parameter]
    public string? Title { get; set; } = "Parameters";

    /// <summary>Draws a divider above the heading, separating the parameters from the form fields above them.</summary>
    [Parameter]
    public bool ShowDivider { get; set; } = true;

    /// <summary>Displays the per-value Encrypt switch. Use where the target stores values encrypted at rest, such as job run parameters.</summary>
    [Parameter]
    public bool ShowEncrypt { get; set; }

    /// <summary>Fired after any value changes, so the host can re-evaluate whether its submit button should be enabled.</summary>
    [Parameter]
    public EventCallback ValueChanged { get; set; }

    /// <summary>
    /// Sample data for autocomplete and live preview on formatter-typed parameters. Run dialogs pass what the template will resolve against at run time so the user
    /// does not have to remember the token names.
    /// </summary>
    [Parameter]
    public object? FormatterContext { get; set; }

    private IReadOnlyDictionary<string, string?> _siblings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    protected override void OnParametersSet() => _siblings = LyoParameterEntry.SiblingMap(Entries);

    private async Task SetValueAsync(LyoParameterEntry entry, string? value)
    {
        entry.Value = value;
        _siblings = LyoParameterEntry.SiblingMap(Entries);
        if (ValueChanged.HasDelegate)
            await ValueChanged.InvokeAsync();
    }
}
