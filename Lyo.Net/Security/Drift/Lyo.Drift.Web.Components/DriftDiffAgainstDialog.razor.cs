namespace Lyo.Drift.Web.Components;

public partial class DriftDiffAgainstDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    /// <summary>HTTP client used to query candidates and POST DiffAgainst.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Drift route prefix.</summary>
    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Drift";

    /// <summary>The snapshot on the left of the comparison.</summary>
    [Parameter]
    [EditorRequired]
    public DriftSnapshotRes FromSnapshot { get; set; } = null!;

    private Guid? _otherId;
    private bool _busy;
    private string? _error;
    private List<DriftSnapshotRes> _candidates = [];

    private string SnapshotRoute => $"{BaseRoute.TrimEnd('/')}/Snapshot";

    protected override async Task OnInitializedAsync()
    {
        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<DriftSnapshotRes>>(
            $"{SnapshotRoute}/QueryConcrete", new() { Amount = 100 });
        var items = res?.Items ?? [];
        _candidates = items
            .Where(s => s.Id != FromSnapshot.Id
                && s.Kind == FromSnapshot.Kind
                && s.InstanceId == FromSnapshot.InstanceId
                && string.Equals(s.WatchRoot ?? "", FromSnapshot.WatchRoot ?? "", StringComparison.Ordinal))
            .OrderByDescending(s => s.TakenAtUtc)
            .ToList();
        if (_candidates.Count > 0)
            _otherId = _candidates[0].Id;
    }

    private async Task ComputeAsync()
    {
        if (_otherId is not { } otherId || otherId == Guid.Empty)
            return;

        _busy = true;
        _error = null;
        try {
            var result = await ApiClient.PostAsAsync<CreateResult<DriftDiffRes>>(
                $"{SnapshotRoute}/{FromSnapshot.Id}/DiffAgainst/{otherId}");
            if (result is { IsSuccess: true, Data: { } diff }) {
                MudDialog.Close(DialogResult.Ok(diff));
                return;
            }

            _error = result?.Error?.GetFullMessage() ?? "DiffAgainst failed.";
        }
        catch (Exception ex) {
            _error = ex.Message;
        }
        finally {
            _busy = false;
        }
    }
}
