using Microsoft.AspNetCore.Components;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

public partial class FileStoreKeyIdField
{
    [CascadingParameter]
    public FileStorageManagement? Host { get; set; }

    /// <summary>Optional explicit list. When omitted, uses <see cref="FileStorageManagement.EncryptionKeyIds" />.</summary>
    [Parameter]
    public IReadOnlyList<string>? KeyIds { get; set; }

    /// <summary>Field label.</summary>
    [Parameter]
    public string Label { get; set; } = "Key id";

    /// <summary>Optional helper under the field. Starts as a keystore hint.</summary>
    [Parameter]
    public string? HelperText { get; set; }

    /// <summary>Inline CSS applied to the field.</summary>
    [Parameter]
    public string Style { get; set; } = "min-width: 180px; max-width: min(100%, 280px);";

    /// <summary>Currently selected key id.</summary>
    [Parameter]
    public string Value { get; set; } = string.Empty;

    /// <summary>Fired when the selected key id changes.</summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>When set and the API returned exactly one id, select it automatically.</summary>
    [Parameter]
    public bool AutoSelectSingle { get; set; }

    private IReadOnlyList<string> Ids => KeyIds ?? Host?.EncryptionKeyIds ?? [];

    private string ResolvedHelper
        => !string.IsNullOrWhiteSpace(HelperText)
            ? HelperText
            : Ids.Count > 0
                ? "Pick a key from the API keystore."
                : "No key ids from the API. Create a key in the keystore, then refresh.";

    protected override async Task OnParametersSetAsync()
    {
        if (!AutoSelectSingle || !string.IsNullOrWhiteSpace(Value) || Ids.Count != 1)
            return;

        await OnValueChanged(Ids[0]);
    }

    private Task OnValueChanged(string value) => ValueChanged.InvokeAsync(value ?? string.Empty);
}
