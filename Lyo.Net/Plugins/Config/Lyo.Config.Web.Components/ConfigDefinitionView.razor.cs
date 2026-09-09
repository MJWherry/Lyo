using System.Text.Json.Nodes;
using Lyo.Common.Metadata.Records;

namespace Lyo.Config.Web.Components;

public partial class ConfigDefinitionView
{
    [CascadingParameter]
    IMudDialogInstance MudDialog { get; set; } = null!;

    /// <summary>Store used to save the definition.</summary>
    [Parameter]
    [EditorRequired]
    public IConfigStore Store { get; set; } = null!;

    /// <summary>Definition being edited. Null creates a new one.</summary>
    [Parameter]
    public ConfigDefinitionRecord? Definition { get; set; }

    /// <summary>Seed values for new definitions (the toolbar entity type).</summary>
    [Parameter]
    public string SubjectEntityType { get; set; } = AppConfigEntity.AppEntityType;

    private LyoForm<ConfigDefinitionRecord>? _form;
    private ConfigDefinitionRecord _edit = new();
    private bool _formHasChanges;
    private bool _valueDirty;
    private bool _typeDirty;
    private bool _jsonParseError;
    private bool _includeDefault;
    private bool _saving;
    private bool _initialized;
    private int _historyVersion;

    private bool IsNew => Definition == null || Definition.Id == default;

    private bool _canSave => !_saving && !_jsonParseError && (_formHasChanges || _valueDirty || _typeDirty || IsNew);

    protected override void OnParametersSet()
    {
        if (_initialized)
            return;

        _edit = Definition == null ? NewDefinition() : Clone(Definition);
        _includeDefault = _edit.DefaultValue != null;
        _initialized = true;
    }

    private ConfigDefinitionRecord NewDefinition()
        => new() {
            SubjectEntityType = string.IsNullOrWhiteSpace(SubjectEntityType) ? AppConfigEntity.AppEntityType : SubjectEntityType.Trim(),
            ForValueType = ConfigValue.GetTypeName(typeof(JsonObject))
        };

    private static ConfigDefinitionRecord Clone(ConfigDefinitionRecord src)
        => new() {
            Id = src.Id,
            SubjectEntityType = src.SubjectEntityType,
            Key = src.Key,
            ForValueType = src.ForValueType,
            Description = src.Description,
            IsRequired = src.IsRequired,
            IsEncrypted = src.IsEncrypted,
            DefaultValue = CloneValue(src.DefaultValue),
            CreatedTimestamp = src.CreatedTimestamp,
            UpdatedTimestamp = src.UpdatedTimestamp
        };

    private static ConfigValue? CloneValue(ConfigValue? src)
        => src == null ? null : new() { TypeName = src.TypeName, Json = src.Json };

    private object DebugModel()
    {
        if (!_edit.IsEncrypted)
            return _edit;

        var clone = Clone(_edit);
        if (clone.DefaultValue != null)
            clone.DefaultValue.Json = ConfigDisplay.Masked;
        return clone;
    }

    private void OnTypeChanged(string value)
    {
        _edit.ForValueType = value;
        _typeDirty = true;
        SyncDefaultTypeName();
        ResetDefaultJsonForType();
    }

    private void SyncDefaultTypeName()
    {
        if (_edit.DefaultValue != null)
            _edit.DefaultValue.TypeName = _edit.ForValueType;
    }

    private void ResetDefaultJsonForType()
    {
        if (_edit.DefaultValue == null)
            return;

        _edit.DefaultValue.Json = ConfigValueEditor.DefaultJsonForType(_edit.ForValueType);
        _valueDirty = true;
    }

    private void OnIncludeDefaultChanged(bool include)
    {
        _includeDefault = include;
        _valueDirty = true;
        if (include) {
            _edit.DefaultValue ??= new() { TypeName = _edit.ForValueType, Json = ConfigValueEditor.DefaultJsonForType(_edit.ForValueType) };
            SyncDefaultTypeName();
        }
        else {
            _edit.DefaultValue = null;
            _jsonParseError = false;
        }
    }

    private void OnDefaultValueChanged(ConfigValue value)
    {
        _edit.DefaultValue = value;
        _valueDirty = true;
    }

    private void OnParseErrorChanged(bool hasError) => _jsonParseError = hasError;

    private async Task OnDefinitionRevertedAsync()
    {
        var fresh = await Store.GetDefinitionByIdAsync(_edit.Id);
        if (fresh == null)
            return;

        _edit = Clone(fresh);
        _includeDefault = _edit.DefaultValue != null;
        _historyVersion++;
        StateHasChanged();
    }

    private async Task SaveAsync()
    {
        if (!_includeDefault)
            _edit.DefaultValue = null;

        try {
            _edit.Validate();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or ArgumentNullException or FormatException) {
            Snackbar.Add(ex.Message, Severity.Error);
            return;
        }

        _saving = true;
        try {
            await Store.SaveDefinitionAsync(_edit);
            Snackbar.Add($"Saved '{_edit.Key}'.", Severity.Success);
            MudDialog.Close(DialogResult.Ok(_edit.Id));
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _saving = false;
        }
    }
}
