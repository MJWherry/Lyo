namespace Lyo.Config.Web.Components;

public partial class ConfigDefinitionRevisionList
{
    /// <summary>Store used to list and revert definition revisions.</summary>
    [Parameter]
    [EditorRequired]
    public IConfigStore Store { get; set; } = null!;

    /// <summary>Definition shown in history. Null shows the empty hint.</summary>
    [Parameter]
    public Guid? DefinitionId { get; set; }

    /// <summary>Bumped by the shell when the operator applies a new scope or asks for a refresh.</summary>
    [Parameter]
    public int DataVersion { get; set; }

    /// <summary>If set, omits the outer empty-state hint and caption (for embedding in the definition editor).</summary>
    [Parameter]
    public bool Compact { get; set; }

    /// <summary>Fired after a successful revert so sibling tabs can reload.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    private IReadOnlyList<ConfigDefinitionRevisionRecord> _items = [];
    private bool _loading;
    private bool _busy;
    private string? _loadError;
    private Guid? _loadedId;
    private int _loadedVersion = int.MinValue;

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedId == DefinitionId && _loadedVersion == DataVersion)
            return;

        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _loadedId = DefinitionId;
        _loadedVersion = DataVersion;
        _loadError = null;
        _items = [];
        if (DefinitionId is not { } id)
            return;

        _loading = true;
        try {
            _items = await Store.GetDefinitionRevisionsAsync(id);
        }
        catch (Exception ex) {
            _loadError = ex.Message;
        }
        finally {
            _loading = false;
        }
    }

    private async Task RevertAsync(ConfigDefinitionRevisionRecord revision)
    {
        if (DefinitionId is not { } id)
            return;

        if (!await DialogService.ConfirmAsync("Revert definition", $"Revert to revision {revision.Revision}? A new revision is appended so this action is auditable.", "Revert"))
            return;

        _busy = true;
        try {
            await Store.RevertDefinitionToRevisionAsync(id, revision.Revision);
            Snackbar.Add($"Reverted to revision {revision.Revision}.", Severity.Success);
            await ReloadAsync();
            await OnChanged.InvokeAsync();
        }
        catch (InvalidOperationException ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }
}
