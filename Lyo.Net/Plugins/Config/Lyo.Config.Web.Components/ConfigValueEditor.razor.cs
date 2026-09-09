using Lyo.Common.Metadata.Records;
using Lyo.Web.Components.LyoType;

namespace Lyo.Config.Web.Components;

public partial class ConfigValueEditor
{
    /// <summary>Strongly typed JSON wrapper being edited. Required when this editor is shown.</summary>
    [Parameter]
    [EditorRequired]
    public ConfigValue Value { get; set; } = null!;

    /// <summary>Fired after Json changes.</summary>
    [Parameter]
    public EventCallback<ConfigValue> ValueChanged { get; set; }

    /// <summary>If set, the value is read-only.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>If set, scalar text uses a password-style input. The client still edits plaintext; the API encrypts at rest.</summary>
    [Parameter]
    public bool IsEncrypted { get; set; }

    /// <summary>JSON editor height when the type needs a JSON tree.</summary>
    [Parameter]
    public string EditorSurfaceHeight { get; set; } = "180px";

    /// <summary>Fired when the JSON editor parse state changes. True means the current JSON is invalid.</summary>
    [Parameter]
    public EventCallback<bool> ParseErrorChanged { get; set; }

    /// <summary>Default JSON for a CLR type name (catalog <see cref="LyoTypeInfo.DefaultJson" />, or <c>{}</c> for unknown types).</summary>
    public static string DefaultJsonForType(string? typeName)
    {
        var json = LyoTypeValueInput.DefaultJsonForType(typeName);
        return json == "null" && !LyoTypeInfo.TryFromName(typeName, out _) ? "{}" : json;
    }

    private async Task OnJsonChanged(string? json)
    {
        Value.Json = json ?? "null";
        await ValueChanged.InvokeAsync(Value);
    }
}
