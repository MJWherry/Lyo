namespace Lyo.Config.Web.Components;

public partial class ConfigRevisionList
{
    /// <summary>Store used to list and revert revisions.</summary>
    [Parameter]
    [EditorRequired]
    public IConfigStore Store { get; set; } = null!;

    /// <summary>Binding shown in history. Null shows the empty hint.</summary>
    [Parameter]
    public Guid? BindingId { get; set; }

    /// <summary>Bumped by the shell when the operator applies a new scope or asks for a refresh.</summary>
    [Parameter]
    public int DataVersion { get; set; }

    /// <summary>If set, omits the outer empty-state hint and caption (for embedding in the binding editor).</summary>
    [Parameter]
    public bool Compact { get; set; }

    /// <summary>If set, value JSON is shown as <see cref="ConfigDisplay.Masked" /> (encrypted keys).</summary>
    [Parameter]
    public bool MaskValues { get; set; }

    /// <summary>Fired after a successful revert so sibling tabs can reload.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    private IReadOnlyList<ConfigBindingRevisionRecord> _items = [];
    private bool _loading;
    private bool _busy;
    private string? _loadError;
    private Guid? _loadedId;
    private int _loadedVersion = int.MinValue;

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedId == BindingId && _loadedVersion == DataVersion)
            return;

        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _loadedId = BindingId;
        _loadedVersion = DataVersion;
        _loadError = null;
        _items = [];
        if (BindingId is not { } id)
            return;

        _loading = true;
        try {
            _items = await Store.GetBindingRevisionsAsync(id, null);
        }
        catch (Exception ex) {
            _loadError = ex.Message;
        }
        finally {
            _loading = false;
        }
    }

    private async Task RevertAsync(ConfigBindingRevisionRecord revision)
    {
        if (BindingId is not { } id)
            return;

        if (!await DialogService.ConfirmAsync("Revert binding", $"Revert to revision {revision.Revision}? A new revision is appended so this action is auditable.", "Revert"))
            return;

        _busy = true;
        try {
            await Store.RevertBindingToRevisionAsync(id, revision.Revision, null);
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
