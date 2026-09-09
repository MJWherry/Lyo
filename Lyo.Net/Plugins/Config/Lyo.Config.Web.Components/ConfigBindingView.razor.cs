namespace Lyo.Config.Web.Components;

public partial class ConfigBindingView
{
    [CascadingParameter]
    IMudDialogInstance MudDialog { get; set; } = null!;

    /// <summary>Store used to save the binding.</summary>
    [Parameter]
    [EditorRequired]
    public IConfigStore Store { get; set; } = null!;

    /// <summary>Owning definition for this binding.</summary>
    [Parameter]
    [EditorRequired]
    public ConfigDefinitionRecord Definition { get; set; } = null!;

    /// <summary>Type of the target entity.</summary>
    [Parameter]
    [EditorRequired]
    public string SubjectEntityType { get; set; } = "";

    /// <summary>Id of the target entity.</summary>
    [Parameter]
    [EditorRequired]
    public string SubjectEntityId { get; set; } = "";

    /// <summary>The definition and entity existing binding to edit. Null creates a new binding.</summary>
    [Parameter]
    public ConfigBindingRecord? Binding { get; set; }

    private ConfigBindingRecord _edit = new();
    private bool _valueDirty;
    private bool _jsonParseError;
    private bool _saving;
    private bool _initialized;
    private int _historyVersion;

    private bool IsNew => Binding == null || Binding.Id == default;

    private bool _canSave => !_saving && !_jsonParseError && (_valueDirty || IsNew);

    protected override void OnParametersSet()
    {
        if (_initialized)
            return;

        _edit = Binding == null ? NewBinding() : Clone(Binding);
        _initialized = true;
    }

    private ConfigBindingRecord NewBinding()
        => new() {
            DefinitionId = Definition.Id,
            Key = Definition.Key,
            SubjectEntityType = SubjectEntityType.Trim(),
            SubjectEntityId = SubjectEntityId.Trim(),
            Value = CloneValue(Definition.DefaultValue) ?? new() { TypeName = Definition.ForValueType, Json = ConfigValueEditor.DefaultJsonForType(Definition.ForValueType) }
        };

    private static ConfigBindingRecord Clone(ConfigBindingRecord src)
        => new() {
            Id = src.Id,
            DefinitionId = src.DefinitionId,
            Key = src.Key,
            SubjectEntityType = src.SubjectEntityType,
            SubjectEntityId = src.SubjectEntityId,
            Value = CloneValue(src.Value) ?? new(),
            CreatedTimestamp = src.CreatedTimestamp,
            UpdatedTimestamp = src.UpdatedTimestamp
        };

    private static ConfigValue? CloneValue(ConfigValue? src)
        => src == null ? null : new() { TypeName = src.TypeName, Json = src.Json };

    private object DebugModel()
    {
        if (!Definition.IsEncrypted)
            return _edit;

        var clone = Clone(_edit);
        clone.Value.Json = ConfigDisplay.Masked;
        return clone;
    }

    private void OnValueChanged(ConfigValue value)
    {
        _edit.Value = value;
        _valueDirty = true;
    }

    private void OnParseErrorChanged(bool hasError) => _jsonParseError = hasError;

    private async Task OnBindingRevertedAsync()
    {
        var fresh = await Store.GetBindingByIdAsync(_edit.Id, null);
        if (fresh == null)
            return;

        _edit = Clone(fresh);
        _historyVersion++;
        StateHasChanged();
    }

    private async Task SaveAsync()
    {
        _edit.DefinitionId = Definition.Id;
        _edit.Key = Definition.Key;
        _edit.SubjectEntityType = SubjectEntityType.Trim();
        _edit.SubjectEntityId = SubjectEntityId.Trim();
        _edit.Value.TypeName = Definition.ForValueType;

        _saving = true;
        try {
            await Store.SaveBindingAsync(_edit, null);
            Snackbar.Add($"Saved binding '{_edit.Key}'.", Severity.Success);
            MudDialog.Close(DialogResult.Ok(_edit.Id));
        }
        catch (InvalidOperationException ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _saving = false;
        }
    }
}
