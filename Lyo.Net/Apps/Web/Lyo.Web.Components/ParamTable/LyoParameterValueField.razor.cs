using Lyo.Api.Client;
using Lyo.Web.Components.LyoType;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.ParamTable;

/// <summary>
/// Value editor for a typed parameter: password when encrypting at run time, Options/AllowedValues select, or <see cref="LyoTypeValueInput" />.
/// </summary>
public partial class LyoParameterValueField
{
    /// <summary>Stored CLR FullName (or catalog name) for the JSON payload.</summary>
    [Parameter]
    [EditorRequired]
    public string TypeName { get; set; } = "";

    /// <summary>JSON text bound to the editor.</summary>
    [Parameter]
    public string? Json { get; set; }

    /// <summary>Fired after the JSON string changes.</summary>
    [Parameter]
    public EventCallback<string?> JsonChanged { get; set; }

    /// <summary>Definition parameter Options JSON (static or query). Preferred over <see cref="AllowedValues" /> when set.</summary>
    [Parameter]
    public string? OptionsJson { get; set; }

    /// <summary>JSON-array fallback when <see cref="OptionsJson" /> is empty.</summary>
    [Parameter]
    public string? AllowedValues { get; set; }

    /// <summary>Sibling parameter key → current value for <c>{{Key}}</c> binding in query options.</summary>
    [Parameter]
    public IReadOnlyDictionary<string, string?>? SiblingValues { get; set; }

    /// <summary>Required when rendering Options-backed selects. Typed input still works when null.</summary>
    [Parameter]
    public IApiClient? ApiClient { get; set; }

    /// <summary>Field label (run dialogs use the parameter key; table expander uses "Default").</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Marks the value as required in the inner editor.</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>If true, empty JSON stays unset (definition defaults).</summary>
    [Parameter]
    public bool AllowUnset { get; set; }

    /// <summary>If true, scalar text uses a password-style input for stored-at-rest encryption.</summary>
    [Parameter]
    public bool IsEncrypted { get; set; }

    /// <summary>Show the Encrypt switch (job run dialog). When on, the editor is a password field.</summary>
    [Parameter]
    public bool ShowEncrypt { get; set; }

    /// <summary>Run-time encrypt switch. Replaces the typed/options editor with a password field.</summary>
    [Parameter]
    public bool Encrypted { get; set; }

    /// <summary>Fired when the Encrypt switch changes.</summary>
    [Parameter]
    public EventCallback<bool> EncryptedChanged { get; set; }

    /// <summary>Optional caption under the editor (parameter description on run dialogs).</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>Height of the JSON editor surface when the type needs a JSON tree.</summary>
    [Parameter]
    public string EditorSurfaceHeight { get; set; } = "180px";

    /// <summary>Sample data for autocomplete and live preview in a registered formatter editor. See <see cref="LyoTypeValueInput.FormatterContext" />.</summary>
    [Parameter]
    public object? FormatterContext { get; set; }

    private bool HasPicker => !string.IsNullOrWhiteSpace(OptionsJson) || !string.IsNullOrWhiteSpace(AllowedValues);

    private bool ShowFooter => ShowEncrypt || !string.IsNullOrEmpty(Description);

    private Task OnJsonChanged(string? value) => JsonChanged.InvokeAsync(value);

    private Task OnEncryptedChanged(bool value) => EncryptedChanged.InvokeAsync(value);
}
