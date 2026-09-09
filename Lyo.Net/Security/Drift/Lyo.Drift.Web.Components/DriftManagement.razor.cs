namespace Lyo.Drift.Web.Components;

public partial class DriftManagement
{
    /// <summary>Base route for Drift endpoints (for example <c>Drift</c> or an absolute API URL plus <c>/Drift</c>).</summary>
    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Drift";

    /// <summary>Starting tab: instances, snapshots, diffs, changes.</summary>
    [Parameter]
    public string? InitialTab { get; set; }

    /// <summary>When set, opens the Snapshots tab filtered to this instance.</summary>
    [Parameter]
    public Guid? InstanceId { get; set; }

    private int _tabIndex;
    private bool _tabInitialized;
    private Guid? _snapshotInstanceId;

    protected override void OnParametersSet()
    {
        if (_tabInitialized)
            return;

        if (InstanceId is null && string.IsNullOrWhiteSpace(InitialTab))
            return;

        if (InstanceId is { } id)
            _snapshotInstanceId = id;

        _tabIndex = ResolveTabIndex();
        _tabInitialized = true;
    }

    private int ResolveTabIndex()
    {
        if (InstanceId.HasValue && string.IsNullOrWhiteSpace(InitialTab))
            return 1;

        return InitialTab?.Trim().ToLowerInvariant() switch {
            "instances" or "instance" => 0,
            "snapshots" or "snapshot" => 1,
            "diffs" or "diff" => 2,
            "changes" or "change" => 3,
            var _ => 0
        };
    }

    private void HandleViewSnapshots(Guid instanceId)
    {
        _snapshotInstanceId = instanceId;
        _tabIndex = 1;
        StateHasChanged();
    }
}
