namespace Lyo.Config.Web.Components;

public partial class ConfigResolvedView
{
    /// <summary>Store used to resolve and mutate bindings.</summary>
    [Parameter]
    [EditorRequired]
    public IConfigStore Store { get; set; } = null!;

    /// <summary>Entity type being resolved.</summary>
    [Parameter]
    public string SubjectEntityType { get; set; } = "";

    /// <summary>Entity id being resolved.</summary>
    [Parameter]
    public string SubjectEntityId { get; set; } = "";

    /// <summary>Bumped by the shell when the operator applies a new scope or asks for a refresh.</summary>
    [Parameter]
    public int DataVersion { get; set; }

    /// <summary>Fired after a successful binding save or delete.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    /// <summary>Fired when the operator opens value history for a binding.</summary>
    [Parameter]
    public EventCallback<Guid> OnViewRevisions { get; set; }

    private IReadOnlyList<ResolvedConfigItemRecord> _items = [];
    private bool _loading;
    private string? _resolveError;
    private string? _loadedType;
    private string? _loadedId;
    private int _loadedVersion = int.MinValue;

    protected override async Task OnParametersSetAsync()
    {
        if (string.Equals(_loadedType, SubjectEntityType, StringComparison.Ordinal)
            && string.Equals(_loadedId, SubjectEntityId, StringComparison.Ordinal)
            && _loadedVersion == DataVersion)
            return;

        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _loadedType = SubjectEntityType;
        _loadedId = SubjectEntityId;
        _loadedVersion = DataVersion;
        _resolveError = null;
        _items = [];
        if (string.IsNullOrWhiteSpace(SubjectEntityType) || string.IsNullOrWhiteSpace(SubjectEntityId))
            return;

        _loading = true;
        try {
            var entity = EntityRef.ForKey(SubjectEntityType.Trim(), SubjectEntityId.Trim());
            try {
                var resolved = await Store.LoadConfigAsync(entity, null);
                _items = resolved.Items;
            }
            catch (InvalidOperationException ex) {
                _resolveError = ex.Message;
                _items = await BuildFallbackItemsAsync(entity);
            }
        }
        catch (Exception ex) {
            _resolveError = ex.Message;
            _items = [];
        }
        finally {
            _loading = false;
        }
    }

    private async Task<IReadOnlyList<ResolvedConfigItemRecord>> BuildFallbackItemsAsync(EntityRef entity)
    {
        var defs = await Store.GetDefinitionsAsync(entity.EntityType);
        var bindings = await Store.GetBindingsAsync(entity, null);
        return defs.Select(d => new ResolvedConfigItemRecord {
            Definition = d,
            Binding = bindings.FirstOrDefault(b => b.DefinitionId == d.Id)
        }).ToList();
    }

    private async Task OpenBindingAsync(ResolvedConfigItemRecord item)
    {
        var parameters = new DialogParameters<ConfigBindingView> {
            { d => d.Store, Store },
            { d => d.Definition, item.Definition },
            { d => d.SubjectEntityType, SubjectEntityType.Trim() },
            { d => d.SubjectEntityId, SubjectEntityId.Trim() },
            { d => d.Binding, item.Binding }
        };
        var dialog = await DialogService.ShowAsync<ConfigBindingView>($"Binding · {item.Definition.Key}", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is { Canceled: false }) {
            await NotifyChangedAsync();
            if (result.Data is Guid id && id != default)
                await OnViewRevisions.InvokeAsync(id);
        }
    }

    private async Task ResetToDefaultAsync(ResolvedConfigItemRecord item)
    {
        if (item.Binding == null || item.Definition.DefaultValue == null)
            return;

        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Reset to default",
            $"Remove the binding for '{item.Definition.Key}' so the definition default is used?",
            yesText: "Reset",
            cancelText: "Cancel");
        if (confirmed != true)
            return;

        try {
            await Store.DeleteBindingAsync(item.Binding.Id, null);
            Snackbar.Add($"Reset '{item.Definition.Key}' to the definition default.", Severity.Success);
            await NotifyChangedAsync();
        }
        catch (InvalidOperationException ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private async Task ClearBindingAsync(ResolvedConfigItemRecord item)
    {
        if (item.Binding == null)
            return;

        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Clear binding",
            $"Remove the binding for '{item.Definition.Key}'? The resolved value falls back to the definition default when one exists.",
            yesText: "Clear",
            cancelText: "Cancel");
        if (confirmed != true)
            return;

        try {
            await Store.DeleteBindingAsync(item.Binding.Id, null);
            Snackbar.Add($"Cleared binding '{item.Definition.Key}'.", Severity.Success);
            await NotifyChangedAsync();
        }
        catch (InvalidOperationException ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private Task ViewRevisionsAsync(ResolvedConfigItemRecord item)
        => item.Binding == null ? Task.CompletedTask : OnViewRevisions.InvokeAsync(item.Binding.Id);

    private async Task NotifyChangedAsync()
    {
        await ReloadAsync();
        await OnChanged.InvokeAsync();
    }
}
